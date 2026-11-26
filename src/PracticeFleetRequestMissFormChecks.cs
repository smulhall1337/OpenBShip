using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeFleetRequestMissFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            Func<int,int,PracticeFleetRequestLaunch> launch=delegate(int family,int capacity){return PracticeFleetRequestLaunch.Create(programs,map,family,capacity).Execute();};
            check("F2 miss form visits water effects completes their lifetime and resets",delegate
            {
                using(var f=new PracticeFleetRequestMissStudy(programs,launch(0,0)))
                {
                    Show(f,owner);Find<Button>(f,"memberMissTransition").PerformClick();Need(f.StudyState.Clock==16365&&f.StudyState.Projectiles.Count==1&&f.StudyState.Effects.Count==1&&Find<ListBox>(f,"memberMissEffects").Items.Count==1,"First water miss");
                    Find<ListBox>(f,"memberMissEffects").SelectedIndex=0;Need(Find<TextBox>(f,"memberMissDetails").Text.Contains("Type 27"),"Effect view details");Find<Button>(f,"memberMissTransition").PerformClick();Need(f.StudyState.Clock==17245&&f.StudyState.Effects.Count==2,"Second water miss");Find<Button>(f,"memberMissFinish").PerformClick();Need(f.StudyState.Complete&&f.StudyState.Clock==18525&&!Find<Button>(f,"memberMissStep").Enabled&&!Find<Button>(f,"memberMissFinish").Enabled,"Finished UI boundary");Find<Button>(f,"memberMissReset").PerformClick();Need(f.StudyState.Updates==0&&f.StudyState.Projectiles.Count==2&&Find<Button>(f,"memberMissStep").Enabled,"Reset water history");f.Close();
                }
            });
            check("Miss form keeps timing selection pending until Load and distinguishes first-clock diagnostics",delegate
            {
                using(var f=new PracticeFleetRequestMissStudy(programs,launch(0,0)))
                {
                    Show(f,owner);Find<ComboBox>(f,"memberMissMode").SelectedIndex=3;Find<Button>(f,"memberMissReset").PerformClick();Need(f.StudyState.Mode==0&&Find<ComboBox>(f,"memberMissMode").SelectedIndex==3,"Pending/applied mode reset");Find<Button>(f,"memberMissLoad").PerformClick();Find<Button>(f,"memberMissStep").PerformClick();Need(f.StudyState.Clock==17245&&f.StudyState.Projectiles.Count==1&&f.StudyState.Projectiles[0].Slot==1,"Later deadline first visit");Find<Button>(f,"memberMissFinish").PerformClick();Need(f.StudyState.Clock==18565&&f.StudyState.Complete,"Diagnostic complete timing");f.Close();
                }
            });
            check("Miss form suppresses held confirmation scrolls at minimum and rejects F3",delegate
            {
                using(var f=new PracticeFleetRequestMissStudy(programs,launch(0,0)))
                {
                    Show(f,owner);f.ActiveControl=Find<Button>(f,"memberMissStep");var down=typeof(PracticeFleetRequestMissStudy).GetMethod("OnKeyDown",BindingFlags.Instance|BindingFlags.NonPublic);down.Invoke(f,new object[]{new KeyEventArgs(Keys.Enter)});var first=f.StudyState;down.Invoke(f,new object[]{new KeyEventArgs(Keys.Enter)});Need(first.Updates==1&&Object.ReferenceEquals(first,f.StudyState),"Held miss confirmation");f.Size=f.MinimumSize;Application.DoEvents();Need(Find<Control>(f,"memberMissCanvas").Width>500&&Find<Panel>(f,"memberMissScroll").AutoScroll,"Minimum miss layout");f.Close();
                }
                using(var bad=new PracticeFleetRequestMissStudy(programs,launch(1,0))){Show(bad,owner);Need(bad.StudyState==null&&!Find<Button>(bad,"memberMissStep").Enabled&&Find<Label>(bad,"memberMissFeedback").Text.Contains("F3"),"F3 impact admitted");bad.Close();}
            });
            check("Launch miss child is independent and enabled only for completed empty F2 allocation",delegate
            {
                using(var parent=new PracticeFleetRequestLaunchStudy(programs,map))
                {
                    Show(parent,owner);var button=Find<Button>(parent,"requestLaunchMisses");Need(!button.Enabled,"Unlaunched miss child enabled");Find<Button>(parent,"requestLaunchExecute").PerformClick();Need(button.Enabled,"Empty F2 child disabled");var before=parent.StudyState;bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100})
                    {
                        timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Miss child missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var miss=child as PracticeFleetRequestMissStudy;if(miss==null)continue;timer.Stop();try{Find<Button>(miss,"memberMissTransition").PerformClick();Need(miss.StudyState.Effects.Count==1,"Child water effect");saw=true;}catch(Exception e){failure=e;}finally{miss.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();
                    }
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(before,parent.StudyState)&&parent.StudyState.Projectiles.Count==2,"Miss child changed launch");Find<ComboBox>(parent,"requestLaunchFamily").SelectedIndex=1;Find<Button>(parent,"requestLaunchLoad").PerformClick();Find<Button>(parent,"requestLaunchExecute").PerformClick();Need(!button.Enabled&&Find<Button>(parent,"requestLaunchFlight").Enabled,"F3 child gate");parent.Close();
                }
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new F2 miss preview directory.");Directory.CreateDirectory(destination);
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            using(var f=new PracticeFleetRequestMissStudy(programs,PracticeFleetRequestLaunch.Create(programs,map,0,0).Execute()))
            {
                Show(f,null);Action<string> capture=delegate(string name){f.PerformLayout();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
                capture("01-Launch");Find<Button>(f,"memberMissTransition").PerformClick();Find<ListBox>(f,"memberMissEffects").SelectedIndex=0;capture("02-First-Water-Effect");Find<Button>(f,"memberMissTransition").PerformClick();capture("03-Two-Effects");Find<Button>(f,"memberMissFinish").PerformClick();capture("04-Complete");Find<ComboBox>(f,"memberMissMode").SelectedIndex=3;Find<Button>(f,"memberMissLoad").PerformClick();Find<Button>(f,"memberMissStep").PerformClick();capture("05-Later-Deadline-First");f.Size=f.MinimumSize;Find<Panel>(f,"memberMissScroll").AutoScrollPosition=Point.Empty;capture("06-Minimum-Controls");Find<Panel>(f,"memberMissScroll").ScrollControlIntoView(Find<TextBox>(f,"memberMissDetails"));capture("07-Minimum-Details");f.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Seven actual-form F2 water-miss previews: launch,first effect,two effects,complete,clockjumpdiagnostic,minimum controls/details. Source numeric impacts/effect counters;blue ring geometry schematic,not original sprites. Remaining requests held;no damage,weapon19,media/audio or full AI battle. Physical input/scaling separate.\r\n");return 0;
        }
    }
}
