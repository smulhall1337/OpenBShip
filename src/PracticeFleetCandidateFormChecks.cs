using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeFleetCandidateFormChecks
    {
        static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
        static T Find<T>(Form form,string name)where T:Control{return(T)form.Controls.Find(name,true)[0];}
        static void Show(Form form,Form owner){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var input=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));Func<uint,PracticeMapStartup> map=delegate(uint seed){return PracticeMapStartup.CreateBssb202(programs,input,seed);};
            check("Candidate form separates pending inputs applied reset and view selection",delegate
            {
                using(var form=new PracticeFleetCandidateStudy(programs,map(731)))
                {
                    Show(form,owner);var first=form.StudyState;Need(first!=null&&first.Candidates.Count==0,"Default scan");Find<ComboBox>(form,"candidateEligibility").SelectedIndex=1;Need(Object.ReferenceEquals(first,form.StudyState),"Pending eligibility changed data");Find<Button>(form,"candidateLoad").PerformClick();Need(form.StudyState.Candidates.Count==6&&Find<ListBox>(form,"candidateTargets").Items.Count==6,"Supplied eligibility scan");
                    var applied=form.StudyState;Find<ListBox>(form,"candidateTargets").SelectedIndex=5;Need(Object.ReferenceEquals(applied,form.StudyState)&&Find<TextBox>(form,"candidateDetails").Text.Contains("Viewed fleet 14"),"Inspection mutated scan");Find<ComboBox>(form,"candidateEquipment").SelectedIndex=1;Find<Button>(form,"candidateReset").PerformClick();Need(form.StudyState.EquipmentProfile==0&&Find<ComboBox>(form,"candidateEquipment").SelectedIndex==1&&form.StudyState.Candidates.Count==6,"Reset used pending inputs");
                    Find<Button>(form,"candidateLoad").PerformClick();Need(form.StudyState.TaskKind==11&&form.StudyState.SelectedWaypoint==-1&&form.StudyState.RouteUiRequests==1&&Find<ListBox>(form,"candidateTargets").Items.Count==0,"No-equipment side effect");Find<Button>(form,"candidateReset").PerformClick();Need(form.StudyState.RouteUiRequests==1,"Reset accumulated task requests");form.Close();
                }
            });
            check("Candidate form retains aircraft exception and base candidate list",delegate
            {
                using(var form=new PracticeFleetCandidateStudy(programs,map(731)))
                {
                    Show(form,owner);Find<ComboBox>(form,"candidateCase").SelectedIndex=2;Find<ComboBox>(form,"candidateEquipment").SelectedIndex=1;Find<ComboBox>(form,"candidateEligibility").SelectedIndex=1;Find<Button>(form,"candidateLoad").PerformClick();Need(!form.StudyState.EquipmentFound&&form.StudyState.TaskKind==0&&form.StudyState.RouteUiRequests==0,"Stored aircraft service exception");Find<ComboBox>(form,"candidateCase").SelectedIndex=1;Find<ComboBox>(form,"candidateEquipment").SelectedIndex=0;Find<Button>(form,"candidateLoad").PerformClick();Need(form.StudyState.Candidates.Count==1&&form.StudyState.Candidates[0]==24,"Base alignment list");form.Close();
                }
            });
            check("Candidate form keyboard edges minimum layout and disposal remain bounded",delegate
            {
                using(var form=new PracticeFleetCandidateStudy(programs,map(731)))
                {
                    Show(form,owner);form.ActiveControl=Find<Button>(form,"candidateLoad");var flags=BindingFlags.Instance|BindingFlags.NonPublic;var down=typeof(PracticeFleetCandidateStudy).GetMethod("OnKeyDown",flags);var up=typeof(Form).GetMethod("OnKeyUp",flags);var before=form.StudyState;down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});var first=form.StudyState;Need(!Object.ReferenceEquals(before,first),"First confirmation missing");down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(Object.ReferenceEquals(first,form.StudyState),"Held confirmation repeated");up.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(!Object.ReferenceEquals(first,form.StudyState),"Released confirmation missing");
                    form.Size=form.MinimumSize;Application.DoEvents();Need(Find<Control>(form,"candidateCanvas").Width>500&&Find<Panel>(form,"candidateScroll").AutoScroll,"Minimum candidate layout");form.Close();
                }
            });
            check("Startup candidate entry opens an independent copy and rejects other seeds",delegate
            {
                using(var parent=new PracticeMapStartupStudy(programs,input))
                {
                    Show(parent,owner);var before=parent.StudyState;var button=Find<Button>(parent,"mapStartupCandidates");Need(button.Enabled,"Candidate entry disabled");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=0;Need(!button.Enabled,"Early candidate stage admitted");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=3;bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100})
                    {
                        timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Candidate child missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var scan=child as PracticeFleetCandidateStudy;if(scan==null)continue;timer.Stop();try{Find<ComboBox>(scan,"candidateEquipment").SelectedIndex=1;Find<Button>(scan,"candidateLoad").PerformClick();Need(scan.StudyState.TaskKind==11,"Child copied scan");saw=true;}catch(Exception e){failure=e;}finally{scan.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();
                    }
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(before,parent.StudyState),"Candidate child mutated parent");parent.Close();
                }
                using(var rejected=new PracticeFleetCandidateStudy(programs,map(0))){Show(rejected,owner);Need(rejected.StudyState==null&&!Find<Button>(rejected,"candidateReset").Enabled&&Find<Label>(rejected,"candidateFeedback").Text.Contains("seed731"),"Unrecorded candidate seed accepted");rejected.Close();}
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new candidate preview directory.");Directory.CreateDirectory(destination);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            using(var form=new PracticeFleetCandidateStudy(programs,map))
            {
                Show(form,null);Action<string> capture=delegate(string name){form.PerformLayout();Application.DoEvents();using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
                capture("01-Original-Eligibility");Find<ComboBox>(form,"candidateEligibility").SelectedIndex=1;Find<Button>(form,"candidateLoad").PerformClick();capture("02-Supplied-Candidates");Find<ComboBox>(form,"candidateEquipment").SelectedIndex=1;Find<Button>(form,"candidateLoad").PerformClick();capture("03-No-Equipment-Task11");Find<ComboBox>(form,"candidateCase").SelectedIndex=1;Find<ComboBox>(form,"candidateEquipment").SelectedIndex=0;Find<Button>(form,"candidateLoad").PerformClick();capture("04-Base-Alignment");form.Size=form.MinimumSize;Find<Panel>(form,"candidateScroll").AutoScrollPosition=Point.Empty;capture("05-Minimum-Inputs");Find<Panel>(form,"candidateScroll").ScrollControlIntoView(Find<TextBox>(form,"candidateDetails"));capture("06-Minimum-Details");form.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Six actual-form candidate scan previews: original and supplied eligibility,missing-equipment task11,base alignment,minimum inputs/details. Raw recovered list only;full AI/member orders/attacks and natural visibility excluded. Positions in test profiles supplied. Physical input/scaling/audio separate.\r\n");return 0;
        }
    }
}
