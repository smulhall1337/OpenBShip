using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeOwnerVisibilityFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var source=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));var map=PracticeMapStartup.CreateBssb202(programs,source,731);
            check("Owner visibility form applies natural source pass keeps selectors pending and resets",delegate
            {
                using(var f=new PracticeOwnerVisibilityStudy(programs,map)){Show(f,owner);Need(f.StudyState.Owner==1&&!f.StudyState.Executed,"Default visibility inputs");Find<Button>(f,"ownerVisibilityExecute").PerformClick();Need(f.StudyState.Fleet(14).Level==0&&f.StudyState.Fleet(15).Level==1&&!Find<Button>(f,"ownerVisibilityExecute").Enabled,"Natural visibility pass");Find<ComboBox>(f,"ownerVisibilityOwner").SelectedIndex=1;Find<Button>(f,"ownerVisibilityReset").PerformClick();Need(f.StudyState.Owner==1&&!f.StudyState.Executed&&Find<ComboBox>(f,"ownerVisibilityOwner").SelectedIndex==1,"Pending owner/reset");Find<Button>(f,"ownerVisibilityLoad").PerformClick();Need(f.StudyState.Owner==0,"Owner load");f.Close();}
            });
            check("Owner visibility form shows no-selection early return and controlled local range",delegate
            {
                using(var f=new PracticeOwnerVisibilityStudy(programs,map)){Show(f,owner);Find<ComboBox>(f,"ownerVisibilityPreset").SelectedIndex=2;Find<Button>(f,"ownerVisibilityLoad").PerformClick();Find<Button>(f,"ownerVisibilityExecute").PerformClick();Need(f.StudyState.Skipped&&f.StudyState.ChangedCount==0&&Find<Label>(f,"ownerVisibilityFeedback").Text.Contains("before clearing"),"No-selection presentation");Find<ComboBox>(f,"ownerVisibilityPreset").SelectedIndex=4;Find<ComboBox>(f,"ownerVisibilityWeapon").SelectedIndex=1;Find<ComboBox>(f,"ownerVisibilityDistance").SelectedIndex=1;Find<Button>(f,"ownerVisibilityLoad").PerformClick();Find<Button>(f,"ownerVisibilityExecute").PerformClick();Need(!f.StudyState.Skipped&&f.StudyState.Preset==4&&f.StudyState.Distance==20&&f.StudyState.RadiusProfile==3&&Find<ComboBox>(f,"ownerVisibilityObserver").Items.Count==2,"Local range fixture");f.Close();}
            });
            check("Owner visibility suppresses held confirmation scrolls at minimum and rejects unverified seed",delegate
            {
                using(var f=new PracticeOwnerVisibilityStudy(programs,map)){Show(f,owner);f.ActiveControl=Find<Button>(f,"ownerVisibilityExecute");var down=typeof(PracticeOwnerVisibilityStudy).GetMethod("OnKeyDown",BindingFlags.Instance|BindingFlags.NonPublic);down.Invoke(f,new object[]{new KeyEventArgs(Keys.Enter)});var first=f.StudyState;down.Invoke(f,new object[]{new KeyEventArgs(Keys.Enter)});Need(first.Executed&&Object.ReferenceEquals(first,f.StudyState),"Held visibility confirmation");f.Size=f.MinimumSize;Application.DoEvents();Need(Find<Control>(f,"ownerVisibilityCanvas").Width>500&&Find<Panel>(f,"ownerVisibilityScroll").AutoScroll,"Minimum visibility layout");f.Close();}
                using(var bad=new PracticeOwnerVisibilityStudy(programs,PracticeMapStartup.CreateBssb202(programs,source,732))){Show(bad,owner);Need(bad.StudyState==null&&!Find<Button>(bad,"ownerVisibilityExecute").Enabled&&Find<Label>(bad,"ownerVisibilityFeedback").Text.Contains("731"),"Unsupported seed admitted");bad.Close();}
            });
            check("Startup owner visibility child is independent and gated by stage and seed",delegate
            {
                using(var parent=new PracticeMapStartupStudy(programs,source))
                {
                    Show(parent,owner);var button=Find<Button>(parent,"mapStartupOwnerVisibility");Need(button.Enabled,"Visibility child unavailable");var before=parent.StudyState;bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100}){timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Visibility child missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var v=child as PracticeOwnerVisibilityStudy;if(v==null)continue;timer.Stop();try{Find<Button>(v,"ownerVisibilityExecute").PerformClick();Need(v.StudyState.Executed,"Child pass");saw=true;}catch(Exception e){failure=e;}finally{v.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();}
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(before,parent.StudyState),"Visibility child altered parent");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=2;Need(!button.Enabled,"Wrong stage enabled");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=3;Find<NumericUpDown>(parent,"mapStartupSeed").Value=732;Find<Button>(parent,"mapStartupLoad").PerformClick();Need(!button.Enabled,"Wrong seed enabled");parent.Close();
                }
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new visibility preview directory.");Directory.CreateDirectory(destination);
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            using(var f=new PracticeOwnerVisibilityStudy(programs,map))
            {
                Show(f,null);Action<string> capture=delegate(string name){f.PerformLayout();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
                capture("01-Before");Find<Button>(f,"ownerVisibilityExecute").PerformClick();capture("02-Natural-Pass");Find<ComboBox>(f,"ownerVisibilityPreset").SelectedIndex=2;Find<Button>(f,"ownerVisibilityLoad").PerformClick();Find<Button>(f,"ownerVisibilityExecute").PerformClick();capture("03-No-Selection");Find<ComboBox>(f,"ownerVisibilityPreset").SelectedIndex=3;Find<ComboBox>(f,"ownerVisibilityRadii").SelectedIndex=1;Find<ComboBox>(f,"ownerVisibilityDistance").SelectedIndex=2;Find<Button>(f,"ownerVisibilityLoad").PerformClick();Find<Button>(f,"ownerVisibilityExecute").PerformClick();capture("04-Sensor-Fixture");Find<ComboBox>(f,"ownerVisibilityPreset").SelectedIndex=4;Find<ComboBox>(f,"ownerVisibilityWeapon").SelectedIndex=1;Find<ComboBox>(f,"ownerVisibilityDistance").SelectedIndex=1;Find<Button>(f,"ownerVisibilityLoad").PerformClick();Find<Button>(f,"ownerVisibilityExecute").PerformClick();capture("05-Local-Range");f.Size=f.MinimumSize;Find<Panel>(f,"ownerVisibilityScroll").AutoScrollPosition=Point.Empty;capture("06-Minimum-Controls");Find<Panel>(f,"ownerVisibilityScroll").ScrollControlIntoView(Find<TextBox>(f,"ownerVisibilityDetails"));capture("07-Minimum-Details");f.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Seven actual-form owner-visibility previews. Source-produced bytes467/480/481; copied map separate from supplied sensor/range/history fixtures. Schematic marker colors,not original fog graphics. No firing,world/owner loop,original media/audio or physical acceptance.\r\n");return 0;
        }
    }
}
