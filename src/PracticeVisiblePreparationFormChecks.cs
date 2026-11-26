using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeVisiblePreparationFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var visibility=PracticeOwnerVisibilityStudyState.Create(programs,map,0,1,0,1,0,0,0).Execute();var joined=PracticeVisibleCandidates.Create(programs,map,visibility);
            check("Produced-visibility preparation form holds inputs visits deadlines and resets pending choices",delegate
            {
                using(var f=new PracticeFleetMemberPreparationStudy(programs,map,joined)){Show(f,owner);Need(f.StudyState.UsesProducedVisibility&&!Find<ComboBox>(f,"memberPrepareEquipment").Enabled&&!Find<ComboBox>(f,"memberPrepareEligibility").Enabled,"Joined selectors editable");var next=Find<Button>(f,"memberPrepareNext");next.PerformClick();Need(f.StudyState.MemberTarget(0)==15&&f.StudyState.MemberTarget(1)==15&&f.StudyState.MemberDue(0)==14845,"Joined target preparation");next.PerformClick();Need(f.StudyState.Clock==14844,"Before deadline");next.PerformClick();Need(f.StudyState.FiringBoundary&&!next.Enabled,"Firing guard UI");Find<ComboBox>(f,"memberPrepareDifficulty").SelectedIndex=0;Find<Button>(f,"memberPrepareReset").PerformClick();Need(f.StudyState.Difficulty==2&&f.StudyState.Visits==0&&Find<ComboBox>(f,"memberPrepareDifficulty").SelectedIndex==0,"Pending difficulty/reset");Find<Button>(f,"memberPrepareLoad").PerformClick();Need(f.StudyState.Difficulty==0&&f.StudyState.UsesProducedVisibility,"Joined load lost source flags");f.Close();}
            });
            check("Joined preparation form exposes caller-word and task8 differences at minimum size",delegate
            {
                using(var f=new PracticeFleetMemberPreparationStudy(programs,map,joined)){Show(f,owner);Find<ComboBox>(f,"memberPrepareDifficulty").SelectedIndex=0;Find<ComboBox>(f,"memberPrepareComparison").SelectedIndex=1;Find<Button>(f,"memberPrepareLoad").PerformClick();Find<Button>(f,"memberPrepareNext").PerformClick();Find<Button>(f,"memberPrepareNext").PerformClick();Need(f.StudyState.MemberState(1)==0&&f.StudyState.Clock==22344,"Caller-word presentation");Find<ComboBox>(f,"memberPrepareDifficulty").SelectedIndex=2;Find<ComboBox>(f,"memberPrepareTask").SelectedIndex=1;Find<Button>(f,"memberPrepareLoad").PerformClick();Find<Button>(f,"memberPrepareNext").PerformClick();Need(f.StudyState.WeaponField(0)==22&&f.StudyState.MemberDue(0)==22345,"Original task8 equipment");f.Size=f.MinimumSize;Application.DoEvents();Need(Find<Panel>(f,"memberPrepareScroll").AutoScroll&&Find<Control>(f,"memberPrepareCanvas").Width>500,"Joined minimum layout");f.Close();}
            });
            check("Produced candidate preparation child preserves parent and excludes owner0",delegate
            {
                using(var parent=new PracticeVisibleCandidateStudy(programs,map,visibility))
                {
                    Show(parent,owner);var before=parent.StudyState;var button=Find<Button>(parent,"visibleCandidatePrepare");Need(button.Enabled,"Owner1 preparation entry disabled");bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100}){timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Joined preparation child missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var p=child as PracticeFleetMemberPreparationStudy;if(p==null)continue;timer.Stop();try{Find<Button>(p,"memberPrepareNext").PerformClick();Need(p.StudyState.UsesProducedVisibility&&p.StudyState.MemberTarget(0)==15,"Joined child prefix");saw=true;}catch(Exception e){failure=e;}finally{p.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();}
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(before,parent.StudyState)&&parent.StudyState.Scan.Candidates[0]==15,"Preparation changed candidate parent");parent.Close();
                }
                using(var other=new PracticeVisibleCandidateStudy(programs,map,PracticeOwnerVisibilityStudyState.Create(programs,map,0,0,0,1,0,0,0).Execute())){Show(other,owner);Need(!Find<Button>(other,"visibleCandidatePrepare").Enabled,"Owner0 preparation enabled");other.Close();}
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new produced-preparation preview directory.");Directory.CreateDirectory(destination);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var visibility=PracticeOwnerVisibilityStudyState.Create(programs,map,0,1,0,1,0,0,0).Execute();var joined=PracticeVisibleCandidates.Create(programs,map,visibility);
            Action<Form,string> capture=delegate(Form f,string name){f.PerformLayout();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
            using(var parent=new PracticeVisibleCandidateStudy(programs,map,visibility)){Show(parent,null);Find<Panel>(parent,"visibleCandidateScroll").ScrollControlIntoView(Find<Button>(parent,"visibleCandidatePrepare"));capture(parent,"01-Candidate-Entry");parent.Close();}
            using(var f=new PracticeFleetMemberPreparationStudy(programs,map,joined)){Show(f,null);capture(f,"02-Initial");Find<Button>(f,"memberPrepareNext").PerformClick();capture(f,"03-Prepared");Find<Button>(f,"memberPrepareNext").PerformClick();capture(f,"04-Before-Deadline");Find<Button>(f,"memberPrepareNext").PerformClick();capture(f,"05-Firing-Boundary");f.Size=f.MinimumSize;Find<Panel>(f,"memberPrepareScroll").AutoScrollPosition=Point.Empty;capture(f,"06-Minimum-Controls");Find<Panel>(f,"memberPrepareScroll").ScrollControlIntoView(Find<TextBox>(f,"memberPrepareDetails"));capture(f,"07-Minimum-Details");f.Close();}
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Seven actual-form produced-visibility preparation previews. Original equipment and produced flags held;explicit caller word/task/difficulty/override/manual clock. Stop before firing;no request/shot/ammo use,fullAI/world,media/audio or physical acceptance.\r\n");return 0;
        }
    }
}
