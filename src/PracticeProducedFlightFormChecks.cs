using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeProducedFlightFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var joined=PracticeVisibleCandidates.Create(programs,map,PracticeOwnerVisibilityStudyState.Create(programs,map,0,1,0,1,0,0,0).Execute());var prep=PracticeFleetMemberPreparation.CreateFromVisibility(programs,map,joined,8,2,false,0).Advance().Advance().Advance();var requests=PracticeFleetMemberRequests.CreateFromPreparation(prep,0,0).Execute();Func<PracticeProducedLaunch> launch=delegate{return PracticeProducedLaunch.Create(programs,requests,1,0).Execute();};
            check("Original-map weapon22 flight form moves resets and stops before its first impact",delegate
            {
                using(var f=new PracticeProducedFlightStudy(launch())){Show(f,owner);Find<Button>(f,"producedFlightBatch").PerformClick();Need(f.StudyState.Clock==23625&&f.StudyState.Projectiles[0].X==240&&f.StudyState.Projectiles[1].X==241,"Source22 batch motion");Find<ListBox>(f,"producedFlightObjects").SelectedIndex=1;Need(Find<TextBox>(f,"producedFlightDetails").Text.Contains("unit 31"),"Viewed moving record");Find<Button>(f,"producedFlightBoundary").PerformClick();Need(f.StudyState.Clock==35210&&f.StudyState.AtBoundary&&!Find<Button>(f,"producedFlightStep").Enabled&&!Find<Button>(f,"producedFlightBoundary").Enabled,"Source22 pre-impact boundary");Find<Button>(f,"producedFlightReset").PerformClick();Need(f.StudyState.Clock==22345&&f.StudyState.Updates==0&&Find<Button>(f,"producedFlightStep").Enabled,"Reset source22 flight");f.Close();}
            });
            check("Source22 flight suppresses held confirmation scrolls at minimum and rejects held-timer input",delegate
            {
                using(var f=new PracticeProducedFlightStudy(launch())){Show(f,owner);f.ActiveControl=Find<Button>(f,"producedFlightStep");var down=typeof(PracticeProducedFlightStudy).GetMethod("OnKeyDown",BindingFlags.Instance|BindingFlags.NonPublic);down.Invoke(f,new object[]{new KeyEventArgs(Keys.Enter)});var first=f.StudyState;down.Invoke(f,new object[]{new KeyEventArgs(Keys.Enter)});Need(first.Updates==1&&Object.ReferenceEquals(first,f.StudyState),"Held source22 confirmation");f.Size=f.MinimumSize;Application.DoEvents();Need(Find<Panel>(f,"producedFlightScroll").AutoScroll&&Find<Control>(f,"producedFlightCanvas").Width>500,"Minimum source22 layout");f.Close();}
                using(var bad=new PracticeProducedFlightStudy(PracticeProducedLaunch.Create(programs,requests,0,0).Execute())){Show(bad,owner);Need(bad.StudyState==null&&!Find<Button>(bad,"producedFlightStep").Enabled,"Held timer admitted to motion");bad.Close();}
            });
            check("Natural-distance consumer flight child requires exact allocated context and preserves parent",delegate
            {
                using(var parent=new PracticeProducedLaunchStudy(programs,requests))
                {
                    Show(parent,owner);var button=Find<Button>(parent,"producedLaunchFlight");Need(!button.Enabled,"Pending/held timer flight enabled");Find<ComboBox>(parent,"producedLaunchTimer").SelectedIndex=1;Find<Button>(parent,"producedLaunchLoad").PerformClick();Find<Button>(parent,"producedLaunchExecute").PerformClick();Need(button.Enabled,"Captured source22 flight unavailable");var before=parent.StudyState;bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100}){timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Source22 flight child missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var f=child as PracticeProducedFlightStudy;if(f==null)continue;timer.Stop();try{Find<Button>(f,"producedFlightBatch").PerformClick();Need(f.StudyState.Clock==23625,"Child source22 motion");saw=true;}catch(Exception e){failure=e;}finally{f.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();}
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(before,parent.StudyState)&&parent.StudyState.Clock==22345,"Flight changed launch parent");Find<ComboBox>(parent,"producedLaunchCapacity").SelectedIndex=1;Find<Button>(parent,"producedLaunchLoad").PerformClick();Find<Button>(parent,"producedLaunchExecute").PerformClick();Need(!button.Enabled,"Capacity fixture flight enabled");parent.Close();
                }
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new source22 flight preview directory.");Directory.CreateDirectory(destination);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var joined=PracticeVisibleCandidates.Create(programs,map,PracticeOwnerVisibilityStudyState.Create(programs,map,0,1,0,1,0,0,0).Execute());var prep=PracticeFleetMemberPreparation.CreateFromVisibility(programs,map,joined,8,2,false,0).Advance().Advance().Advance();var requests=PracticeFleetMemberRequests.CreateFromPreparation(prep,0,0).Execute();var launch=PracticeProducedLaunch.Create(programs,requests,1,0).Execute();Action<Form,string> capture=delegate(Form f,string name){f.PerformLayout();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
            using(var parent=new PracticeProducedLaunchStudy(programs,requests)){Show(parent,null);Find<ComboBox>(parent,"producedLaunchTimer").SelectedIndex=1;Find<Button>(parent,"producedLaunchLoad").PerformClick();Find<Button>(parent,"producedLaunchExecute").PerformClick();Find<Panel>(parent,"producedLaunchScroll").ScrollControlIntoView(Find<Button>(parent,"producedLaunchFlight"));capture(parent,"01-Consumer-Flight-Entry");parent.Close();}
            using(var f=new PracticeProducedFlightStudy(launch)){Show(f,null);capture(f,"02-Launch");Find<Button>(f,"producedFlightBatch").PerformClick();capture(f,"03-32-Updates");Find<Button>(f,"producedFlightBoundary").PerformClick();capture(f,"04-Before-Impact");Find<Button>(f,"producedFlightReset").PerformClick();capture(f,"05-Reset");f.Size=f.MinimumSize;capture(f,"06-Minimum-Controls");Find<Panel>(f,"producedFlightScroll").ScrollControlIntoView(Find<TextBox>(f,"producedFlightDetails"));capture(f,"07-Minimum-Details");f.Close();}
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Seven actual-form natural-map weapon22 future-only flight previews. Sourceclock/priorDelta40 supplied;lastdeadline-1 diagnostic retainsdelta. Noimpact/expiry/ammo/request/RNGchanges,fullAI/world,media/audio orphysicalacceptance.\r\n");return 0;
        }
    }
}
