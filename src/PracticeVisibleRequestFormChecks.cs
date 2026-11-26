using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeVisibleRequestFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var joined=PracticeVisibleCandidates.Create(programs,map,PracticeOwnerVisibilityStudyState.Create(programs,map,0,1,0,1,0,0,0).Execute());Func<int,PracticeFleetMemberPreparation> prefix=delegate(int task){return PracticeFleetMemberPreparation.CreateFromVisibility(programs,map,joined,task,2,false,0).Advance().Advance().Advance();};
            check("Produced original-equipment request form locks prefix inputs queues cells and keeps reset selections pending",delegate
            {
                using(var f=new PracticeFleetMemberRequestStudy(programs,map,prefix(0))){Show(f,owner);foreach(string name in new[]{"memberRequestEquipment","memberRequestTask","memberRequestDifficulty","memberRequestComparison","memberRequestVisibility"})Need(!Find<ComboBox>(f,name).Enabled,"Editable held prefix "+name);Need(Find<ComboBox>(f,"memberRequestGlobalWeapon").Items.Count==4,"Original global-field choices");Find<Button>(f,"memberRequestExecute").PerformClick();Need(f.StudyState.QueuedCells.Count==20&&f.StudyState.WeaponField(0)==18&&!Find<Button>(f,"memberRequestExecute").Enabled,"Natural F2 requests");Find<ListBox>(f,"memberRequestCells").SelectedIndex=19;Need(Find<TextBox>(f,"memberRequestDetails").Text.Contains("Unit 31 -> fleet 15"),"Viewed original request");Find<ComboBox>(f,"memberRequestGlobalWeapon").SelectedIndex=1;Find<Button>(f,"memberRequestReset").PerformClick();Need(f.StudyState.GlobalWeapon==0&&!f.StudyState.Executed&&Find<ComboBox>(f,"memberRequestGlobalWeapon").SelectedIndex==1,"Pending global/reset");Find<Button>(f,"memberRequestLoad").PerformClick();Find<Button>(f,"memberRequestExecute").PerformClick();Need(f.StudyState.GlobalWeapon==17&&f.StudyState.MemberState(0)==0&&f.StudyState.MemberState(1)==1,"Global17 state display");f.Close();}
            });
            check("Produced request form preserves F3 one-attempt and full-queue diagnostics at minimum size",delegate
            {
                using(var f=new PracticeFleetMemberRequestStudy(programs,map,prefix(8))){Show(f,owner);Find<Button>(f,"memberRequestExecute").PerformClick();Need(f.StudyState.QueuedCells.Count==2&&f.StudyState.WeaponField(0)==22,"Original F3 requests");Find<ComboBox>(f,"memberRequestQueue").SelectedIndex=2;Find<ComboBox>(f,"memberRequestGlobalWeapon").SelectedIndex=3;Find<Button>(f,"memberRequestLoad").PerformClick();Find<Button>(f,"memberRequestExecute").PerformClick();Need(f.StudyState.QueuedCells.Count==20&&f.StudyState.RandomState==4052711439u&&f.StudyState.MemberState(0)==1&&f.StudyState.MemberState(1)==1,"Full queue/global22");f.Size=f.MinimumSize;Application.DoEvents();Need(Find<Panel>(f,"memberRequestScroll").AutoScroll&&Find<Control>(f,"memberRequestCanvas").Width>500,"Minimum original request layout");f.Close();}
            });
            check("Joined preparation request child requires exact completed prefix and preserves parent",delegate
            {
                using(var parent=new PracticeFleetMemberPreparationStudy(programs,map,joined))
                {
                    Show(parent,owner);var button=Find<Button>(parent,"memberPrepareRequests");Need(!button.Enabled,"Pending preparation admitted");for(int n=0;n<3;n++)Find<Button>(parent,"memberPrepareNext").PerformClick();Need(button.Enabled,"Completed exact prefix disabled");var before=parent.StudyState;bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100}){timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Produced request child missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var q=child as PracticeFleetMemberRequestStudy;if(q==null)continue;timer.Stop();try{Find<Button>(q,"memberRequestExecute").PerformClick();Need(q.StudyState.UsesProducedVisibility&&q.StudyState.QueuedCells.Count==20,"Child produced requests");saw=true;}catch(Exception e){failure=e;}finally{q.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();}
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(before,parent.StudyState)&&parent.StudyState.MemberState(0)==2,"Request child changed preparation");Find<Button>(parent,"memberPrepareReset").PerformClick();Need(!button.Enabled,"Reset child gate");Find<ComboBox>(parent,"memberPrepareDifficulty").SelectedIndex=0;Find<Button>(parent,"memberPrepareLoad").PerformClick();for(int n=0;n<3;n++)Find<Button>(parent,"memberPrepareNext").PerformClick();Need(parent.StudyState.FiringBoundary&&!button.Enabled,"Uncaptured difficulty admitted");parent.Close();
                }
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new produced-request preview directory.");Directory.CreateDirectory(destination);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var joined=PracticeVisibleCandidates.Create(programs,map,PracticeOwnerVisibilityStudyState.Create(programs,map,0,1,0,1,0,0,0).Execute());Func<int,PracticeFleetMemberPreparation> prefix=delegate(int task){return PracticeFleetMemberPreparation.CreateFromVisibility(programs,map,joined,task,2,false,0).Advance().Advance().Advance();};
            Action<Form,string> capture=delegate(Form f,string name){f.PerformLayout();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
            using(var parent=new PracticeFleetMemberPreparationStudy(programs,map,joined)){Show(parent,null);for(int n=0;n<3;n++)Find<Button>(parent,"memberPrepareNext").PerformClick();Find<Panel>(parent,"memberPrepareScroll").ScrollControlIntoView(Find<Button>(parent,"memberPrepareRequests"));capture(parent,"01-Preparation-Entry");parent.Close();}
            using(var f=new PracticeFleetMemberRequestStudy(programs,map,prefix(0))){Show(f,null);capture(f,"02-Initial");Find<Button>(f,"memberRequestExecute").PerformClick();Find<ListBox>(f,"memberRequestCells").SelectedIndex=19;capture(f,"03-F2-Requests");Find<ComboBox>(f,"memberRequestGlobalWeapon").SelectedIndex=1;Find<Button>(f,"memberRequestLoad").PerformClick();Find<Button>(f,"memberRequestExecute").PerformClick();capture(f,"04-Global17");Find<ComboBox>(f,"memberRequestQueue").SelectedIndex=2;Find<Button>(f,"memberRequestLoad").PerformClick();Find<Button>(f,"memberRequestExecute").PerformClick();capture(f,"05-Full-Queue");f.Close();}
            using(var f=new PracticeFleetMemberRequestStudy(programs,map,prefix(8))){Show(f,null);Find<Button>(f,"memberRequestExecute").PerformClick();capture(f,"06-F3-Requests");f.Size=f.MinimumSize;capture(f,"07-Minimum-Controls");Find<Panel>(f,"memberRequestScroll").ScrollControlIntoView(Find<TextBox>(f,"memberRequestDetails"));capture(f,"08-Minimum-Details");f.Close();}
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Eight actual-form original-equipment request previews. Source-produced visibility and completed preparation held;globalfield/taggedqueues supplied. Numeric requests,not shots/hits;no ammo use,consumer/launch/impact,fullAI/world,media/audio or physical acceptance.\r\n");return 0;
        }
    }
}
