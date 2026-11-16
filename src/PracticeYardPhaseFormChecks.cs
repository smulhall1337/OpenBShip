using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeYardPhaseFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form form,string name)where T:Control{return(T)form.Controls.Find(name,true)[0];}
        static void Show(Form form,Form owner){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);};
            check("Entered yard opens a separate phase-clock copy and closes safely with its owner",delegate
            {
                int errors=0;var exceptionHandler=new System.Threading.ThreadExceptionEventHandler(delegate{errors++;});Application.ThreadException+=exceptionHandler;
                try
                {
                    using(var parent=new PracticeMapYardStudy(programs,startup(),30))
                    {
                        Show(parent,owner);Need(!Find<Button>(parent,"mapYardPhaseStudy").Enabled,"Clock enabled before entry");Find<Button>(parent,"mapYardEnter").PerformClick();var initial=parent.StudyState;
                        bool saw=false;Exception failure=null;int ticks=0;
                        using(var timer=new Timer{Interval=100})
                        {
                            timer.Tick+=delegate
                            {
                                if(++ticks>30){timer.Stop();failure=new Exception("Clock child did not open");parent.Close();return;}
                                foreach(Form form in parent.OwnedForms)
                                {
                                    var child=form as PracticeMapYardStudy;if(child==null||child.StudyClock==null)continue;timer.Stop();
                                    try
                                    {
                                        Need(child.StudyClock.PhaseCounter==12&&child.StudyState.Entered,"Received entered clock");Find<Button>(child,"mapYardPhaseOne").PerformClick();
                                        Find<CheckBox>(child,"mapYardPhasePaused").Checked=true;Find<Button>(child,"mapYardPhase64").PerformClick();
                                        Need(child.StudyClock.PhaseCounter==13&&child.StudyClock.WorkerPasses==1&&child.StudyClock.PausedPolls==64,"Paused clock child");saw=true;parent.Close();
                                    }
                                    catch(Exception e){failure=e;}
                                    finally{if(!child.IsDisposed)child.Close();}return;
                                }
                            };
                            timer.Start();Find<Button>(parent,"mapYardPhaseStudy").PerformClick();timer.Stop();
                        }
                        if(failure!=null)throw failure;
                        Need(saw&&parent.IsDisposed&&Object.ReferenceEquals(initial,parent.StudyState)&&parent.StudyState.WorkerCalls==0,"Clock mutated parent or owner closure failed");
                    }
                }
                finally{Application.ThreadException-=exceptionHandler;}
                Need(errors==0,"Thread exception on clock owner exit");
            });
            check("Yard clock controls return at poll257 and allow stable post-return observation",delegate
            {
                var map=startup();var entered=PracticeMapYardState.Create(programs,map,30,1).Enter();
                using(var form=new PracticeMapYardStudy(programs,map,30,entered,true))
                {
                    Show(form,owner);Need(form.Controls.Find("mapYardOne",true).Length==0&&form.Controls.Find("mapYardUntil",true).Length==0,"Clock exposed ungated manual worker");
                    Find<Button>(form,"mapYardPhaseOne").PerformClick();for(int n=0;n<4;n++)Find<Button>(form,"mapYardPhase64").PerformClick();
                    Need(form.StudyClock.Polls==257&&form.StudyClock.PhaseCounter==269&&form.StudyClock.WorkerPasses==5&&form.StudyState.Returned&&form.StudyState.TargetAmmo==138,"Clock UI return boundary");
                    Find<Button>(form,"mapYardPhase64").PerformClick();Need(form.StudyClock.PhaseCounter==333&&form.StudyClock.WorkerPasses==6&&form.StudyState.TargetAmmo==138&&form.StudyState.DepotAmmo==348,"Post-return clock UI");
                    form.Size=form.MinimumSize;Application.DoEvents();Need(Find<Panel>(form,"mapYardScroll").AutoScroll&&Find<Control>(form,"mapYardCanvas").Width>500,"Clock minimum layout");form.Close();
                }
            });
            check("Yard clock pending counter reset pause and uint wrap use explicit controls",delegate
            {
                var map=startup();using(var form=new PracticeMapYardStudy(programs,map,30,PracticeMapYardState.Create(programs,map,30,1).Enter(),true))
                {
                    Show(form,owner);var counter=Find<NumericUpDown>(form,"mapYardPhaseCounter");counter.Value=0;Find<Button>(form,"mapYardPhaseOne").PerformClick();Need(form.StudyClock.PhaseCounter==13,"Pending counter applied early");
                    Find<Button>(form,"mapYardReset").PerformClick();Need(form.StudyClock.PhaseCounter==12&&form.StudyClock.Polls==0&&counter.Value==0&&form.StudyState.WorkerCalls==0,"Reset consumed pending counter");
                    Find<Button>(form,"mapYardPhaseLoad").PerformClick();Need(form.StudyClock.InitialCounter==0&&form.StudyClock.PhaseCounter==0,"Counter Load");
                    counter.Value=uint.MaxValue;Find<Button>(form,"mapYardPhaseLoad").PerformClick();Find<Button>(form,"mapYardPhaseOne").PerformClick();Need(form.StudyClock.PhaseCounter==0&&form.StudyClock.WorkerPasses==0,"Clock UI wrap");
                    Find<CheckBox>(form,"mapYardPhasePaused").Checked=true;Find<Button>(form,"mapYardPhase64").PerformClick();Need(form.StudyClock.PhaseCounter==0&&form.StudyClock.PausedPolls==64,"Pause checkbox gate");
                    Find<CheckBox>(form,"mapYardPhasePaused").Checked=false;Find<Button>(form,"mapYardPhase64").PerformClick();Need(form.StudyClock.PhaseCounter==64&&form.StudyClock.WorkerPasses==1,"Resumed wrapped phase13");form.Close();
                }
            });
            check("Yard clock poll actions require released confirmation edges",delegate
            {
                var map=startup();using(var form=new PracticeMapYardStudy(programs,map,30,PracticeMapYardState.Create(programs,map,30,1).Enter(),true))
                {
                    Show(form,owner);var button=Find<Button>(form,"mapYardPhaseOne");Find<Panel>(form,"mapYardScroll").ScrollControlIntoView(button);form.ActiveControl=button;
                    var flags=BindingFlags.Instance|BindingFlags.NonPublic;var down=typeof(PracticeMapYardStudy).GetMethod("OnKeyDown",flags);var up=typeof(Form).GetMethod("OnKeyUp",flags);
                    down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.StudyClock.Polls==1&&form.StudyClock.WorkerPasses==1,"Held poll repeated");
                    up.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.StudyClock.Polls==2&&form.StudyClock.WorkerPasses==1,"Released poll edge");form.Close();
                }
            });
            check("Approach-derived yard clock preserves the received world pose through reset",delegate
            {
                var map=startup();var entered=PracticeYardApproachState.Create(programs,map,30,1).UntilAdmission().CreateYardCopy().Enter();
                using(var form=new PracticeMapYardStudy(programs,map,30,entered,true))
                {
                    Show(form,owner);Find<Button>(form,"mapYardPhaseOne").PerformClick();Find<Button>(form,"mapYardReset").PerformClick();var fleets=form.StudyState.CopyFleetPool();
                    Need(form.StudyState.WorkerCalls==0&&BitConverter.ToInt16(fleets,16*492+12)==620&&BitConverter.ToInt16(fleets,16*492+14)==600&&!Find<ComboBox>(form,"mapYardTarget").Enabled,"Clock reset lost approach pose/fixed target");form.Close();
                }
                bool rejected=false;try{using(var form=new PracticeMapYardStudy(programs,map,30,PracticeMapYardState.Create(programs,map,30,1),true)){} }catch(InvalidOperationException){rejected=true;}
                Need(rejected,"Clock view accepted unentered copy");
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new yard phase preview directory.");
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);Directory.CreateDirectory(destination);
            using(var form=new PracticeMapYardStudy(programs,map,30,PracticeMapYardState.Create(programs,map,30,1).Enter(),true))
            {
                Show(form,null);Action<string> capture=delegate(string name){form.PerformLayout();Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
                capture("01-Phase-Ready");Find<Button>(form,"mapYardPhaseOne").PerformClick();capture("02-Phase13");
                Find<CheckBox>(form,"mapYardPhasePaused").Checked=true;Find<Button>(form,"mapYardPhase64").PerformClick();capture("03-Paused-Phase13");
                Find<CheckBox>(form,"mapYardPhasePaused").Checked=false;for(int n=0;n<4;n++)Find<Button>(form,"mapYardPhase64").PerformClick();Need(form.StudyState.Returned,"Clock preview did not return");capture("04-Clock-Returned");
                form.Size=form.MinimumSize;Find<Panel>(form,"mapYardScroll").AutoScrollPosition=Point.Empty;capture("05-Minimum");
                Find<Panel>(form,"mapYardScroll").ScrollControlIntoView(Find<Button>(form,"mapYardPhase64"));capture("06-Minimum-Polls");
                Find<Panel>(form,"mapYardScroll").ScrollControlIntoView(Find<TextBox>(form,"mapYardDetails"));capture("07-Minimum-Details");form.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Seven actual-form DrawToBitmap previews. Entered-yard copy, counter12,one normal poll,64paused polls,256normal polls:counter269/321total polls/64paused/five worker passes/returned. Source phase and worker are composed;no full original world replay,AI or wall-clock playback. No settings saved;physical DPI/input/audio remain separate.\r\n");return 0;
        }
    }
}
