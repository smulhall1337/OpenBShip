using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeFleetServicePhaseFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        static int Parent(PracticeFleetServicePhaseClock c,int unit){return BitConverter.ToInt16(c.CopyShipPool(),unit*268+12);}
        static int Gold(Form f,bool upper)
        {
            var canvas=Find<Control>(f,"servicePhaseCanvas");using(var b=new Bitmap(canvas.Width,canvas.Height)){canvas.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));int count=0;for(int y=upper?0:b.Height/2;y<(upper?b.Height/2:b.Height);y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).ToArgb()==Color.Gold.ToArgb())count++;return count;}
        }
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var input=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));Func<uint,PracticeMapStartup> map=delegate(uint seed){return PracticeMapStartup.CreateBssb202(programs,input,seed);};
            check("Service phase form distinguishes initial support admission return and task completion",delegate
            {
                using(var form=new PracticeFleetServicePhaseStudy(programs,map(731)))
                {
                    Show(form,owner);Need(form.StudyState.PhaseCounter==0&&Gold(form,true)>20,"Clock initial view");Find<Button>(form,"servicePhaseThirteen").PerformClick();Need(form.StudyState.PhaseCounter==13&&form.StudyState.WorkerCalls==1&&form.StudyState.TaskVisits==0&&Parent(form.StudyState,30)==16,"Pre-admission worker");Find<Button>(form,"servicePhaseNext").PerformClick();Need(form.StudyState.PhaseCounter==32&&Parent(form.StudyState,30)==3&&Gold(form,false)>20,"Task32 admission");
                    int visits=0;while(form.StudyState.PhaseCounter<333&&visits++<30)Find<Button>(form,"servicePhaseNext").PerformClick();Need(form.StudyState.PhaseCounter==333&&form.StudyState.TaskKind==12&&Parent(form.StudyState,30)==16&&Gold(form,true)>20&&Gold(form,false)==0,"Return333 view");Find<Button>(form,"servicePhaseNext").PerformClick();Need(form.StudyState.PhaseCounter==352&&form.StudyState.TaskKind==0&&Find<Button>(form,"servicePhaseBatch").Enabled,"Queue0 stopped clock");Find<Button>(form,"servicePhaseBatch").PerformClick();Need(form.StudyState.Polls==384&&form.StudyState.TaskVisits==12&&!Find<Button>(form,"servicePhaseOne").Enabled&&Find<Label>(form,"servicePhaseSummary").Text.Contains("ammo 138"),"Clock window end");form.Close();
                }
            });
            check("Service phase pause and reset preserve pending choices and source data",delegate
            {
                using(var form=new PracticeFleetServicePhaseStudy(programs,map(731)))
                {
                    Show(form,owner);var initial=form.StudyState;Find<Button>(form,"servicePhaseThirteen").PerformClick();Find<CheckBox>(form,"servicePhasePause").Checked=true;Find<Button>(form,"servicePhaseBatch").PerformClick();Need(form.StudyState.PhaseCounter==13&&form.StudyState.WorkerCalls==1&&form.StudyState.PausedPolls==32&&!Find<Button>(form,"servicePhaseNext").Enabled,"Paused form repeated support");
                    Find<ComboBox>(form,"servicePhaseCounter").SelectedIndex=2;Find<ComboBox>(form,"servicePhaseCase").SelectedIndex=1;Find<Button>(form,"servicePhaseReset").PerformClick();Need(Object.ReferenceEquals(initial,form.StudyState)&&Find<ComboBox>(form,"servicePhaseCounter").SelectedIndex==2&&Find<CheckBox>(form,"servicePhasePause").Checked,"Reset consumed pending counter/pause");Find<Button>(form,"servicePhaseLoad").PerformClick();Find<CheckBox>(form,"servicePhasePause").Checked=false;Find<Button>(form,"servicePhaseNext").PerformClick();Need(form.StudyState.CaseId=="30-2-firstworse"&&form.StudyState.InitialCounter==31&&form.StudyState.PhaseCounter==32&&Parent(form.StudyState,31)==2,"Loaded clock choices");form.Close();
                }
            });
            check("Service phase lone task12 and blocked contact remain distinct copied cases",delegate
            {
                using(var form=new PracticeFleetServicePhaseStudy(programs,map(731)))
                {
                    Show(form,owner);Find<ComboBox>(form,"servicePhaseKind").SelectedIndex=1;Find<Button>(form,"servicePhaseLoad").PerformClick();Find<Button>(form,"servicePhaseBatch").PerformClick();Need(form.StudyState.TaskKind==0&&Parent(form.StudyState,30)==16&&Find<Button>(form,"servicePhaseOne").Enabled,"Lone12 handler boundary");
                    Find<ComboBox>(form,"servicePhaseKind").SelectedIndex=0;Find<ComboBox>(form,"servicePhaseCase").SelectedIndex=2;Find<Button>(form,"servicePhaseLoad").PerformClick();for(int n=0;n<12;n++)Find<Button>(form,"servicePhaseBatch").PerformClick();Need(form.StudyState.TaskKind==11&&Parent(form.StudyState,30)==16&&form.StudyState.WorkerCalls==6,"Blocked service case changed");form.Close();
                }
            });
            check("Service phase keyboard edges and minimum scrolling controls remain usable",delegate
            {
                using(var form=new PracticeFleetServicePhaseStudy(programs,map(731)))
                {
                    Show(form,owner);form.ActiveControl=Find<Button>(form,"servicePhaseOne");var flags=BindingFlags.Instance|BindingFlags.NonPublic;var down=typeof(PracticeFleetServicePhaseStudy).GetMethod("OnKeyDown",flags);var up=typeof(Form).GetMethod("OnKeyUp",flags);down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.StudyState.Polls==1,"Held phase confirmation repeated");up.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.StudyState.Polls==2,"Released phase confirmation missing");form.Size=form.MinimumSize;Application.DoEvents();Need(Find<Control>(form,"servicePhaseCanvas").Width>500&&Find<Panel>(form,"servicePhaseScroll").AutoScroll,"Clock minimum layout");form.Close();
                }
            });
            check("Startup source-phase entry is independent and rejects unrecorded seeds",delegate
            {
                using(var parent=new PracticeMapStartupStudy(programs,input))
                {
                    Show(parent,owner);var baseline=parent.StudyState;var button=Find<Button>(parent,"mapStartupServicePhase");Need(button.Enabled,"Phase entry disabled");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=0;Need(!button.Enabled,"Early phase entry");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=3;bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100})
                    {
                        timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Clock child missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var phase=child as PracticeFleetServicePhaseStudy;if(phase==null)continue;timer.Stop();try{Find<Button>(phase,"servicePhaseBatch").PerformClick();Need(phase.StudyState.TaskVisits==1,"Clock child update");saw=true;}catch(Exception e){failure=e;}finally{phase.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();
                    }
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(baseline,parent.StudyState),"Clock child modified parent");parent.Close();
                }
                using(var bad=new PracticeFleetServicePhaseStudy(programs,map(0))){Show(bad,owner);Need(bad.StudyState==null&&!Find<Button>(bad,"servicePhaseOne").Enabled&&Find<Label>(bad,"servicePhaseFeedback").Text.Contains("seed731"),"Unrecorded seed not rejected");bad.Close();}
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new service-phase preview directory.");string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);Directory.CreateDirectory(destination);
            using(var form=new PracticeFleetServicePhaseStudy(programs,map))
            {
                Show(form,null);Action<string> capture=delegate(string name){form.PerformLayout();Application.DoEvents();using(var b=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(b,new Rectangle(Point.Empty,form.Size));b.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
                capture("01-Ready");Find<Button>(form,"servicePhaseThirteen").PerformClick();capture("02-Worker13-NoAdmission");Find<Button>(form,"servicePhaseNext").PerformClick();capture("03-Task32-Admitted");int n=0;while(form.StudyState.PhaseCounter<333&&n++<30)Find<Button>(form,"servicePhaseNext").PerformClick();capture("04-Return333-Task12");Find<Button>(form,"servicePhaseNext").PerformClick();capture("05-Queue0-Counter352");Find<CheckBox>(form,"servicePhasePause").Checked=true;Find<Button>(form,"servicePhaseBatch").PerformClick();capture("06-Paused-CounterHeld");
                Find<ComboBox>(form,"servicePhaseCounter").SelectedIndex=3;Find<Button>(form,"servicePhaseLoad").PerformClick();Find<CheckBox>(form,"servicePhasePause").Checked=false;Find<Button>(form,"servicePhaseNext").PerformClick();form.Size=form.MinimumSize;Find<Panel>(form,"servicePhaseScroll").AutoScrollPosition=Point.Empty;capture("07-Minimum-Wrapped");Find<Panel>(form,"servicePhaseScroll").ScrollControlIntoView(Find<TextBox>(form,"servicePhaseDetails"));capture("08-Minimum-Details");form.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Eight actual-form service-phase previews. Original gates/dispatcher/support subset;384 requested poll window. Supplied owner/contact/counter/context inputs;no outer AI driver,policy,movement,AA or natural wall clock. Return333 and task completion352 distinguished;paused holds and uint wrap shown. No settings saved;physical input/scaling/audio separate.\r\n");return 0;
        }
    }
}
