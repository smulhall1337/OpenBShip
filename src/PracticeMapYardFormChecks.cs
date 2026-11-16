using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeMapYardFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form form,string name)where T:Control{return(T)form.Controls.Find(name,true)[0];}
        static void Show(Form form,Form owner){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show(owner);Application.DoEvents();}
        // Export layout previews through the normal application, rather than
        // compiling another standalone research executable. No settings are saved.
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);
            if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new map yard preview directory; existing output is preserved.");
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            var startup=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            Directory.CreateDirectory(destination);
            using(var form=new PracticeMapYardStudy(programs,startup,30))
            {
                Show(form,null);
                Action<string> capture=delegate(string name)
                {
                    form.PerformLayout();Application.DoEvents();
                    Need(!Find<Panel>(form,"mapYardScroll").HorizontalScroll.Visible,"Map yard horizontal clipping");
                    using(var image=new Bitmap(form.Width,form.Height))
                    {
                        form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));
                        image.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);
                    }
                };
                capture("01-Ready");Find<Button>(form,"mapYardEnter").PerformClick();capture("02-Entered");
                Find<Button>(form,"mapYardOne").PerformClick();capture("03-One-Pass");
                Find<Button>(form,"mapYardUntil").PerformClick();Need(form.StudyState.Returned,"Map yard preview did not return");capture("04-Returned");
                form.Size=form.MinimumSize;Find<Panel>(form,"mapYardScroll").AutoScrollPosition=Point.Empty;capture("05-Minimum");
                Find<Panel>(form,"mapYardScroll").ScrollControlIntoView(Find<TextBox>(form,"mapYardDetails"));capture("06-Minimum-Details");
                Find<ComboBox>(form,"mapYardTarget").SelectedIndex=2;Find<ComboBox>(form,"mapYardProfile").SelectedIndex=2;
                Find<Button>(form,"mapYardLoad").PerformClick();Find<Button>(form,"mapYardEnter").PerformClick();
                Find<Panel>(form,"mapYardScroll").AutoScrollPosition=Point.Empty;capture("07-Short-Ship-Mixed");form.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Seven actual-form DrawToBitmap layout previews from the normal production application. Default1160x820 and minimum960x700;ready,entered,onepass,return,details and shorter-ship profiles. These previews do not establish physical DPI/input/audio or natural Practice gameplay. No settings or parent snapshots were changed.\r\n");
            return 0;
        }
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var input=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));
            Func<uint,PracticeMapStartup> startup=delegate(uint seed){return PracticeMapStartup.CreateBssb202(programs,input,seed);};
            check("Copied map yard form exposes admission manual work and return with stock feedback",delegate
            {
                using(var form=new PracticeMapYardStudy(programs,startup(731),30))
                {
                    Show(form,owner);Need(form.StudyState.TargetUnit==30&&form.StudyState.Profile==1&&Find<Button>(form,"mapYardEnter").Enabled&&!Find<Button>(form,"mapYardOne").Enabled,"Default copy/buttons");
                    Find<Button>(form,"mapYardEnter").PerformClick();Need(form.StudyState.Entered&&form.StudyState.SelectedUnit==31&&!Find<Button>(form,"mapYardEnter").Enabled,"Copied entry/escort");
                    Find<Button>(form,"mapYardOne").PerformClick();Need(form.StudyState.WorkerCalls==1&&form.StudyState.TargetHP==5&&!form.StudyState.Returned,"Manual pass/HP");
                    Find<Button>(form,"mapYardUntil").PerformClick();Need(form.StudyState.Returned&&form.StudyState.WorkerCalls==5&&form.StudyState.TargetAmmo==138&&!Find<Button>(form,"mapYardOne").Enabled&&!Find<Button>(form,"mapYardUntil").Enabled,"Bounded return/buttons");
                    Need(Find<Label>(form,"mapYardSummary").Text.Contains("Returned")&&Find<TextBox>(form,"mapYardDetails").Text.Contains("text 2, map 1, refresh 3"),"Readable return/context feedback");
                    form.Size=form.MinimumSize;Application.DoEvents();Need(Find<Control>(form,"mapYardCanvas").Width>500&&Find<Panel>(form,"mapYardScroll").AutoScroll,"Minimum layout/scroll");form.Close();
                }
            });
            check("Map yard selectors stay pending and reset restores the applied copy",delegate
            {
                using(var form=new PracticeMapYardStudy(programs,startup(731),30))
                {
                    Show(form,owner);var initial=form.StudyState;Find<Button>(form,"mapYardEnter").PerformClick();
                    Find<ComboBox>(form,"mapYardTarget").SelectedIndex=1;Find<ComboBox>(form,"mapYardProfile").SelectedIndex=0;
                    Need(form.StudyState.TargetUnit==30&&form.StudyState.Profile==1&&form.StudyState.Entered,"Pending selectors changed active copy");
                    Find<Button>(form,"mapYardReset").PerformClick();Need(Object.ReferenceEquals(initial,form.StudyState)&&!form.StudyState.Entered&&Find<ComboBox>(form,"mapYardTarget").SelectedIndex==1&&Find<ComboBox>(form,"mapYardProfile").SelectedIndex==0,"Reset consumed pending selection");
                    Find<Button>(form,"mapYardLoad").PerformClick();Need(form.StudyState.TargetUnit==31&&form.StudyState.Profile==0&&!form.StudyState.Entered,"Explicit pending Load");form.Close();
                }
            });
            check("Map yard form admits and returns all nine copied target and damage profiles",delegate
            {
                using(var form=new PracticeMapYardStudy(programs,startup(731),30))
                {
                    Show(form,owner);int[] targets={30,31,34},passes={5,4,3};
                    for(int t=0;t<3;t++)for(int p=0;p<3;p++)
                    {
                        Find<ComboBox>(form,"mapYardTarget").SelectedIndex=t;Find<ComboBox>(form,"mapYardProfile").SelectedIndex=p;Find<Button>(form,"mapYardLoad").PerformClick();
                        Find<Button>(form,"mapYardEnter").PerformClick();Find<Button>(form,"mapYardUntil").PerformClick();
                        Need(form.StudyState.TargetUnit==targets[t]&&form.StudyState.Returned&&form.StudyState.WorkerCalls==(p==0?1:passes[t]),"Copied UI return boundary "+t+"/"+p);
                    }
                    form.Close();
                }
            });
            check("Map yard form rejects unsupported applied seeds with clear disabled controls",delegate
            {
                using(var form=new PracticeMapYardStudy(programs,startup(0),30))
                {
                    Show(form,owner);Need(form.StudyState==null&&!Find<Button>(form,"mapYardEnter").Enabled&&!Find<Button>(form,"mapYardOne").Enabled&&!Find<Button>(form,"mapYardReset").Enabled&&Find<Label>(form,"mapYardFeedback").Text.Contains("seed731"),"Unsupported copy failed clearly");form.Close();
                }
            });
            check("Map yard manual worker requires released confirmation edges",delegate
            {
                using(var form=new PracticeMapYardStudy(programs,startup(731),30))
                {
                    Show(form,owner);Find<Button>(form,"mapYardEnter").PerformClick();form.ActiveControl=Find<Button>(form,"mapYardOne");var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                    var down=typeof(PracticeMapYardStudy).GetMethod("OnKeyDown",flags);var up=typeof(Form).GetMethod("OnKeyUp",flags);
                    down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.StudyState.WorkerCalls==1,"Held worker confirmation repeated");
                    up.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.StudyState.WorkerCalls==2,"Released worker edge did not apply");form.Close();
                }
            });
            check("Startup parent gates copied map yard by applied stage and seed and preserves snapshots",delegate
            {
                using(var parent=new PracticeMapStartupStudy(programs,input))
                {
                    Show(parent,owner);var original=parent.StudyState;var button=Find<Button>(parent,"mapStartupYard");var stage=Find<ComboBox>(parent,"mapStartupStage");
                    Need(button.Enabled,"Default yard entry disabled");stage.SelectedIndex=0;Need(!button.Enabled,"Earlier stage admitted");stage.SelectedIndex=3;
                    Find<NumericUpDown>(parent,"mapStartupSeed").Value=0;Need(button.Enabled,"Pending seed changed eligibility");
                    int selectedFleet=parent.SelectedFleet,selectedUnit=parent.SelectedUnit;
                    bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100})
                    {
                        timer.Tick+=delegate
                        {
                            ticks++;
                            foreach(Form child in parent.OwnedForms)
                            {
                                var yard=child as PracticeMapYardStudy;if(yard==null)continue;
                                try{Find<Button>(yard,"mapYardEnter").PerformClick();Find<Button>(yard,"mapYardUntil").PerformClick();Need(yard.StudyState.Returned,"Nested copied yard did not return");saw=true;}
                                catch(Exception e){failure=e;}
                                finally{timer.Stop();yard.Close();}return;
                            }
                            if(ticks>30){timer.Stop();failure=new Exception("Nested map yard did not open");parent.Close();}
                        };
                        timer.Start();button.PerformClick();timer.Stop();
                    }
                    if(failure!=null)throw failure;
                    Need(saw&&Object.ReferenceEquals(original,parent.StudyState)&&parent.AppliedStage.NonemptyFleets==29&&parent.SelectedFleet==selectedFleet&&parent.SelectedUnit==selectedUnit,"Child mutated parent copy/selection");
                    Find<Button>(parent,"mapStartupLoad").PerformClick();Need(parent.StudyState.Seed==0&&!button.Enabled,"Applied unsupported seed admitted");parent.Close();
                }
            });
        }
    }
}
