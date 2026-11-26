using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeVisibleCandidateFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            Func<int,int,PracticeOwnerVisibilityStudyState> input=delegate(int preset,int who){return PracticeOwnerVisibilityStudyState.Create(programs,map,preset,who,0,1,0,0,0).Execute();};
            check("Produced visibility candidate child shows original natural owner results",delegate
            {
                foreach(int who in new[]{0,1})using(var f=new PracticeVisibleCandidateStudy(programs,map,input(0,who))){Show(f,owner);Need(f.StudyState!=null&&Find<ListBox>(f,"visibleCandidateList").Items.Count==1&&Find<TextBox>(f,"visibleCandidateDetails").Text.Contains(who==0?"Candidate 9":"Candidate 15")&&Find<Label>(f,"visibleCandidateSummary").Text.Contains(who==0?"actor 14":"actor 16"),"Natural candidate display");f.Close();}
            });
            check("Candidate child retains cached list selection scrolls and rejects local-mode fixtures",delegate
            {
                using(var f=new PracticeVisibleCandidateStudy(programs,map,input(1,0))){Show(f,owner);var list=Find<ListBox>(f,"visibleCandidateList");Need(list.Items.Count==4,"Cached candidates missing");list.SelectedIndex=3;Need(Find<TextBox>(f,"visibleCandidateDetails").Text.StartsWith("Candidate 0"),"Cached viewed candidate");f.Size=f.MinimumSize;Application.DoEvents();Need(Find<Panel>(f,"visibleCandidateScroll").AutoScroll&&Find<Control>(f,"visibleCandidateCanvas").Width>500,"Minimum candidate layout");f.Close();}
                using(var bad=new PracticeVisibleCandidateStudy(programs,map,input(2,1))){Show(bad,owner);Need(bad.StudyState==null&&Find<Label>(bad,"visibleCandidateFeedback").Text.Contains("rejected"),"Local-mode join admitted");bad.Close();}
            });
            check("Visibility parent enables candidate child only after admitted pass and remains unchanged",delegate
            {
                using(var parent=new PracticeOwnerVisibilityStudy(programs,map))
                {
                    Show(parent,owner);var button=Find<Button>(parent,"ownerVisibilityCandidates");Need(!button.Enabled,"Pending visibility child enabled");Find<Button>(parent,"ownerVisibilityExecute").PerformClick();Need(button.Enabled,"Produced visibility child unavailable");var before=parent.StudyState;bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100}){timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Candidate child missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var c=child as PracticeVisibleCandidateStudy;if(c==null)continue;timer.Stop();try{Need(c.StudyState.Scan.Candidates.Count==1,"Child candidate result");saw=true;}catch(Exception e){failure=e;}finally{c.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();}
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(before,parent.StudyState),"Candidate child altered visibility parent");Find<Button>(parent,"ownerVisibilityReset").PerformClick();Need(!button.Enabled,"Reset candidate gate");Find<ComboBox>(parent,"ownerVisibilityOwner").SelectedIndex=3;Find<Button>(parent,"ownerVisibilityLoad").PerformClick();Find<Button>(parent,"ownerVisibilityExecute").PerformClick();Need(!button.Enabled,"Owner2 candidate join enabled");parent.Close();
                }
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new joined-candidate preview directory.");Directory.CreateDirectory(destination);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            Action<Form,string> capture=delegate(Form f,string name){f.PerformLayout();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
            using(var parent=new PracticeOwnerVisibilityStudy(programs,map)){Show(parent,null);Find<Button>(parent,"ownerVisibilityExecute").PerformClick();Find<Panel>(parent,"ownerVisibilityScroll").ScrollControlIntoView(Find<Button>(parent,"ownerVisibilityCandidates"));capture(parent,"01-Parent-Entry");parent.Close();}
            foreach(int who in new[]{1,0})using(var f=new PracticeVisibleCandidateStudy(programs,map,PracticeOwnerVisibilityStudyState.Create(programs,map,0,who,0,1,0,0,0).Execute())){Show(f,null);capture(f,who==1?"02-Natural-Owner1":"03-Natural-Owner0");f.Close();}
            using(var f=new PracticeVisibleCandidateStudy(programs,map,PracticeOwnerVisibilityStudyState.Create(programs,map,1,0,0,1,0,0,0).Execute())){Show(f,null);Find<ListBox>(f,"visibleCandidateList").SelectedIndex=3;capture(f,"04-Cached-History");f.Size=f.MinimumSize;capture(f,"05-Minimum-Controls");Find<Panel>(f,"visibleCandidateScroll").ScrollControlIntoView(Find<TextBox>(f,"visibleCandidateDetails"));capture(f,"06-Minimum-Details");f.Close();}
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Six actual-form joined visibility/candidate previews. Natural owner results and explicit prefilled history. No supplied visibility replaces producer output;candidate is not firing admission. Schematic markers,not source fog/UI. No fullAI/world,media/audio or physical acceptance.\r\n");return 0;
        }
    }
}
