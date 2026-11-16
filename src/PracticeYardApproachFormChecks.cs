using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeYardApproachFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form form,string name)where T:Control{return(T)form.Controls.Find(name,true)[0];}
        static void Show(Form form,Form owner){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");byte[] input=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));
            Func<uint,PracticeMapStartup> startup=delegate(uint seed){return PracticeMapStartup.CreateBssb202(programs,input,seed);};
            check("Nearby yard form derives admission after two manual movement visits",delegate
            {
                using(var form=new PracticeYardApproachStudy(programs,startup(731),30))
                {
                    Show(form,owner);Need(form.StudyState.Collision==-1&&!Find<Button>(form,"yardApproachOpen").Enabled,"Approach starts admitted");
                    Find<Button>(form,"yardApproachOne").PerformClick();Need(form.StudyState.Visits==1&&form.StudyState.Position.X==620&&!Find<Button>(form,"yardApproachOpen").Enabled,"First touching visit");
                    Find<Button>(form,"yardApproachOne").PerformClick();Need(form.StudyState.Visits==2&&form.StudyState.Collision==3&&Find<Button>(form,"yardApproachOpen").Enabled&&!Find<Button>(form,"yardApproachOne").Enabled,"Second contact admission/buttons");
                    form.Size=form.MinimumSize;Application.DoEvents();Need(Find<Control>(form,"yardApproachCanvas").Width>500&&Find<Panel>(form,"yardApproachScroll").AutoScroll,"Minimum approach layout");form.Close();
                }
            });
            check("Nearby yard selectors stay pending and reset preserves pending choices",delegate
            {
                using(var form=new PracticeYardApproachStudy(programs,startup(731),30))
                {
                    Show(form,owner);var initial=form.StudyState;Find<Button>(form,"yardApproachUntil").PerformClick();
                    Find<ComboBox>(form,"yardApproachTarget").SelectedIndex=2;Find<ComboBox>(form,"yardApproachProfile").SelectedIndex=0;
                    Need(form.StudyState.TargetUnit==30&&form.StudyState.Visits==2,"Pending approach selectors mutated state");
                    Find<Button>(form,"yardApproachReset").PerformClick();Need(Object.ReferenceEquals(initial,form.StudyState)&&Find<ComboBox>(form,"yardApproachTarget").SelectedIndex==2&&Find<ComboBox>(form,"yardApproachProfile").SelectedIndex==0,"Reset consumed pending choices");
                    Find<Button>(form,"yardApproachLoad").PerformClick();Find<Button>(form,"yardApproachUntil").PerformClick();Need(form.StudyState.TargetUnit==34&&form.StudyState.Profile==0&&form.StudyState.Visits==1&&form.StudyState.CheckAdmission().Available,"Faster applied target approach");form.Close();
                }
            });
            check("Nearby yard movement requires released confirmation edges",delegate
            {
                using(var form=new PracticeYardApproachStudy(programs,startup(731),30))
                {
                    Show(form,owner);form.ActiveControl=Find<Button>(form,"yardApproachOne");var flags=BindingFlags.Instance|BindingFlags.NonPublic;var down=typeof(PracticeYardApproachStudy).GetMethod("OnKeyDown",flags);var up=typeof(Form).GetMethod("OnKeyUp",flags);
                    down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.StudyState.Visits==1,"Held movement repeated");
                    up.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.StudyState.Visits==2&&form.StudyState.CheckAdmission().Available,"Released movement edge");form.Close();
                }
            });
            check("Nearby approach opens a fixed independent admitted yard copy and preserves its parent",delegate
            {
                var map=startup(731);using(var parent=new PracticeYardApproachStudy(programs,map,30))
                {
                    Show(parent,owner);Find<Button>(parent,"yardApproachUntil").PerformClick();var initial=parent.StudyState;bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100})
                    {
                        timer.Tick+=delegate
                        {
                            ticks++;
                            foreach(Form child in parent.OwnedForms)
                            {
                                var yard=child as PracticeMapYardStudy;if(yard==null)continue;
                                try
                                {
                                    Need(!Find<ComboBox>(yard,"mapYardTarget").Enabled&&!Find<ComboBox>(yard,"mapYardProfile").Enabled&&!Find<Button>(yard,"mapYardLoad").Enabled,"Approach target not fixed in child");
                                    Need(!yard.StudyState.Entered&&yard.StudyState.CheckAdmission().SelectedYard==3,"Received approach admission");
                                    Find<Button>(yard,"mapYardEnter").PerformClick();Find<Button>(yard,"mapYardUntil").PerformClick();Need(yard.StudyState.Returned&&yard.StudyState.WorkerCalls==5,"Received copy return");
                                    Find<Button>(yard,"mapYardReset").PerformClick();Need(!yard.StudyState.Entered&&yard.StudyState.TargetAmmo==0,"Received copy reset");saw=true;
                                }
                                catch(Exception e){failure=e;}
                                finally{timer.Stop();yard.Close();}return;
                            }
                            if(ticks>30){timer.Stop();failure=new Exception("Admitted yard child did not open");parent.Close();}
                        };
                        timer.Start();Find<Button>(parent,"yardApproachOpen").PerformClick();timer.Stop();
                    }
                    if(failure!=null)throw failure;
                    Need(saw&&Object.ReferenceEquals(initial,parent.StudyState)&&parent.StudyState.Visits==2&&map.Stages[3].NonemptyFleets==29,"Yard child changed approach/startup");parent.Close();
                }
            });
            check("Nearby yard form rejects unsupported seed and leaves transfer disabled",delegate
            {
                using(var form=new PracticeYardApproachStudy(programs,startup(0),30))
                {
                    Show(form,owner);Need(form.StudyState==null&&!Find<Button>(form,"yardApproachOpen").Enabled&&!Find<Button>(form,"yardApproachOne").Enabled&&Find<Label>(form,"yardApproachFeedback").Text.Contains("seed731"),"Unsupported approach failed clearly");form.Close();
                }
            });
            check("Startup approach button follows applied seed and final stage with terrain feedback",delegate
            {
                using(var parent=new PracticeMapStartupStudy(programs,input))
                {
                    Show(parent,owner);var button=Find<Button>(parent,"mapStartupApproach");Need(button.Enabled&&Find<Label>(parent,"mapStartupSummary").Text.Contains("Terrain: 53"),"Default terrain/approach entry");
                    Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=1;Need(!button.Enabled,"Earlier stage admitted approach");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=3;
                    Find<NumericUpDown>(parent,"mapStartupSeed").Value=0;Need(button.Enabled,"Pending seed changed approach eligibility");Find<Button>(parent,"mapStartupLoad").PerformClick();Need(!button.Enabled,"Unsupported applied seed admitted approach");parent.Close();
                }
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new approach preview directory.");
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var startup=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);Directory.CreateDirectory(destination);
            Action<Form,string> capture=delegate(Form form,string name)
            {
                form.PerformLayout();Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}
            };
            using(var form=new PracticeYardApproachStudy(programs,startup,30))
            {
                Show(form,null);capture(form,"01-Nearby-Start");Find<Button>(form,"yardApproachOne").PerformClick();capture(form,"02-Touching");Find<Button>(form,"yardApproachUntil").PerformClick();capture(form,"03-Admitted");
                form.Size=form.MinimumSize;Find<Panel>(form,"yardApproachScroll").AutoScrollPosition=Point.Empty;capture(form,"04-Minimum");Find<Panel>(form,"yardApproachScroll").ScrollControlIntoView(Find<Button>(form,"yardApproachOpen"));capture(form,"05-Minimum-Actions");
                using(var yard=new PracticeMapYardStudy(programs,startup,30,form.StudyState.CreateYardCopy()))
                {
                    Show(yard,form);Find<Button>(yard,"mapYardEnter").PerformClick();capture(yard,"06-Received-Entry");Find<Button>(yard,"mapYardUntil").PerformClick();Need(yard.StudyState.Returned,"Approach preview return");capture(yard,"07-Received-Return");yard.Close();
                }
                form.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Seven actual-form DrawToBitmap previews of nearby composed approach and received yard copy. Supplied pose/waypoint, recovered terrain/movement/gate/yard routines;not a whole-original approach replay. No settings saved;physical DPI/input/audio remain separate.\r\n");return 0;
        }
    }
}
