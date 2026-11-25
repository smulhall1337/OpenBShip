using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeFleetMemberPreparationFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}static T Find<T>(Form f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var input=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));Func<uint,PracticeMapStartup> map=delegate(uint seed){return PracticeMapStartup.CreateBssb202(programs,input,seed);};
            check("Member preparation form reaches due boundary and resets applied inputs",delegate
            {
                using(var form=new PracticeFleetMemberPreparationStudy(programs,map(731)))
                {
                    Show(form,owner);Need(form.StudyState.Visits==0&&!form.StudyState.HasScanned,"Initial prefix already visited");var button=Find<Button>(form,"memberPrepareNext");button.PerformClick();Need(form.StudyState.MemberTarget(0)==15&&form.StudyState.MemberTarget(1)==14&&form.StudyState.MemberDue(0)==14845,"Default member choices");button.PerformClick();Need(form.StudyState.Clock==14844&&form.StudyState.MemberState(0)==1,"Due-1 prefix");button.PerformClick();Need(form.StudyState.Clock==14845&&form.StudyState.FiringBoundary&&!button.Enabled&&button.Text.Contains("Stopped"),"Firing boundary not guarded");
                    Find<ComboBox>(form,"memberPrepareDifficulty").SelectedIndex=0;Find<Button>(form,"memberPrepareReset").PerformClick();Need(form.StudyState.Visits==0&&form.StudyState.Difficulty==2&&Find<ComboBox>(form,"memberPrepareDifficulty").SelectedIndex==0&&button.Enabled,"Reset applied/pending separation");Find<Button>(form,"memberPrepareLoad").PerformClick();button.PerformClick();Need(form.StudyState.Difficulty==0&&form.StudyState.MemberState(1)==0,"Pending difficulty not loaded");form.Close();
                }
            });
            check("Member preparation form shows supplied stack comparison and player override",delegate
            {
                using(var form=new PracticeFleetMemberPreparationStudy(programs,map(731)))
                {
                    Show(form,owner);Find<ComboBox>(form,"memberPrepareEligibility").SelectedIndex=1;Find<ComboBox>(form,"memberPrepareDifficulty").SelectedIndex=0;Find<ComboBox>(form,"memberPrepareComparison").SelectedIndex=1;Find<Button>(form,"memberPrepareLoad").PerformClick();var next=Find<Button>(form,"memberPrepareNext");next.PerformClick();next.PerformClick();Need(form.StudyState.MemberState(1)==0,"Comparison1 waiting path");Find<ComboBox>(form,"memberPrepareComparison").SelectedIndex=0;Find<Button>(form,"memberPrepareLoad").PerformClick();next.PerformClick();next.PerformClick();Need(form.StudyState.MemberState(1)==1,"Comparison0 waiting path");
                    Find<ComboBox>(form,"memberPrepareDifficulty").SelectedIndex=2;Find<ComboBox>(form,"memberPrepareEligibility").SelectedIndex=2;Find<ComboBox>(form,"memberPreparePlayer").SelectedIndex=1;Find<Button>(form,"memberPrepareLoad").PerformClick();next.PerformClick();next.PerformClick();Need(form.StudyState.FiringBoundary&&form.StudyState.MemberDue(0)==2&&!next.Enabled,"Player override boundary");form.Close();
                }
            });
            check("Member preparation keyboard edges minimum scrolling and close remain usable",delegate
            {
                using(var form=new PracticeFleetMemberPreparationStudy(programs,map(731)))
                {
                    Show(form,owner);form.ActiveControl=Find<Button>(form,"memberPrepareNext");var flags=BindingFlags.Instance|BindingFlags.NonPublic;var down=typeof(PracticeFleetMemberPreparationStudy).GetMethod("OnKeyDown",flags);var up=typeof(Form).GetMethod("OnKeyUp",flags);down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.StudyState.Visits==1,"Held preparation confirmation repeated");up.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.StudyState.Visits==2,"Released preparation confirmation missing");form.Size=form.MinimumSize;Application.DoEvents();Need(Find<Control>(form,"memberPrepareCanvas").Width>500&&Find<Panel>(form,"memberPrepareScroll").AutoScroll,"Preparation minimum layout");form.Close();
                }
            });
            check("Startup member preparation entry is isolated and other seeds reject",delegate
            {
                using(var parent=new PracticeMapStartupStudy(programs,input))
                {
                    Show(parent,owner);var before=parent.StudyState;var button=Find<Button>(parent,"mapStartupMemberPreparation");Need(button.Enabled,"Preparation entry disabled");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=0;Need(!button.Enabled,"Early stage admitted preparation");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=3;bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100})
                    {
                        timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Preparation child missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var preparation=child as PracticeFleetMemberPreparationStudy;if(preparation==null)continue;timer.Stop();try{Find<Button>(preparation,"memberPrepareNext").PerformClick();Need(preparation.StudyState.Visits==1,"Child visit missing");saw=true;}catch(Exception e){failure=e;}finally{preparation.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();
                    }
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(before,parent.StudyState),"Preparation child modified parent");parent.Close();
                }
                using(var bad=new PracticeFleetMemberPreparationStudy(programs,map(0))){Show(bad,owner);Need(bad.StudyState==null&&!Find<Button>(bad,"memberPrepareNext").Enabled&&Find<Label>(bad,"memberPrepareFeedback").Text.Contains("seed731"),"Unsupported preparation seed admitted");bad.Close();}
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new member preparation preview directory.");Directory.CreateDirectory(destination);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            using(var form=new PracticeFleetMemberPreparationStudy(programs,map))
            {
                Show(form,null);Action<string> capture=delegate(string name){form.PerformLayout();Application.DoEvents();using(var b=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(b,new Rectangle(Point.Empty,form.Size));b.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
                capture("01-Ready");var next=Find<Button>(form,"memberPrepareNext");next.PerformClick();capture("02-Choices");next.PerformClick();capture("03-Before-Deadline");next.PerformClick();capture("04-Firing-Boundary-Stopped");Find<ComboBox>(form,"memberPrepareEligibility").SelectedIndex=1;Find<ComboBox>(form,"memberPrepareDifficulty").SelectedIndex=0;Find<Button>(form,"memberPrepareLoad").PerformClick();next.PerformClick();next.PerformClick();capture("05-Comparison-Zero");Find<ComboBox>(form,"memberPrepareComparison").SelectedIndex=1;Find<Button>(form,"memberPrepareLoad").PerformClick();next.PerformClick();next.PerformClick();capture("06-Comparison-One");form.Size=form.MinimumSize;Find<Panel>(form,"memberPrepareScroll").AutoScrollPosition=Point.Empty;capture("07-Minimum-Inputs");Find<Panel>(form,"memberPrepareScroll").ScrollControlIntoView(Find<TextBox>(form,"memberPrepareDetails"));capture("08-Minimum-Details");form.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Eight actual-form member preparation previews: initial copy,equipment/target choice,deadline-1,deadline stop,supplied comparison0/1,andminimum inputs/details. No firing state entered,shots queued or natural caller stack value inferred. Supplied clock/eligibility/task/player inputs;full AI policy excluded. Physical scaling/input/audio separate.\r\n");return 0;
        }
    }
}
