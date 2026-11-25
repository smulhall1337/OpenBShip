using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeFleetMemberRequestFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}static T Find<T>(Form f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var input=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));Func<uint,PracticeMapStartup> map=delegate(uint seed){return PracticeMapStartup.CreateBssb202(programs,input,seed);};
            check("Member request form stores one visit and preserves pending reset inputs",delegate
            {
                using(var form=new PracticeFleetMemberRequestStudy(programs,map(731)))
                {
                    Show(form,owner);Need(form.StudyState.PreparedState(0)==2&&!form.StudyState.Executed&&form.StudyState.QueuedCells.Count==0,"Initial request prefix");var button=Find<Button>(form,"memberRequestExecute");button.PerformClick();Need(form.StudyState.Executed&&form.StudyState.QueuedCells.Count==20&&!button.Enabled&&Find<ListBox>(form,"memberRequestCells").Items.Count==20,"One queued visit");var before=form.StudyState;Find<ListBox>(form,"memberRequestCells").SelectedIndex=19;Need(Object.ReferenceEquals(before,form.StudyState)&&Find<TextBox>(form,"memberRequestDetails").Text.Contains("Unit 31"),"Viewed slot mutated source");Find<ComboBox>(form,"memberRequestGlobalWeapon").SelectedIndex=1;Find<Button>(form,"memberRequestReset").PerformClick();Need(!form.StudyState.Executed&&form.StudyState.GlobalWeapon==0&&Find<ComboBox>(form,"memberRequestGlobalWeapon").SelectedIndex==1&&button.Enabled,"Applied/pending reset");Find<Button>(form,"memberRequestLoad").PerformClick();button.PerformClick();Need(form.StudyState.MemberState(0)==1&&form.StudyState.MemberState(1)==1,"Global input load");form.Close();
                }
            });
            check("Member request form distinguishes F3 negative controls and full queues",delegate
            {
                using(var form=new PracticeFleetMemberRequestStudy(programs,map(731)))
                {
                    Show(form,owner);Find<ComboBox>(form,"memberRequestEquipment").SelectedIndex=1;Find<Button>(form,"memberRequestLoad").PerformClick();Find<Button>(form,"memberRequestExecute").PerformClick();Need(form.StudyState.QueuedCells.Count==0,"F3 task0 prepared");Find<ComboBox>(form,"memberRequestTask").SelectedIndex=1;Find<Button>(form,"memberRequestLoad").PerformClick();Find<Button>(form,"memberRequestExecute").PerformClick();Need(form.StudyState.QueuedCells.Count==2,"F3 one attempt each");Find<ComboBox>(form,"memberRequestQueue").SelectedIndex=2;Find<Button>(form,"memberRequestLoad").PerformClick();Need(form.StudyState.QueuedCells.Count==20,"Full input row display");Find<Button>(form,"memberRequestExecute").PerformClick();Need(form.StudyState.QueuedCells.Count==20,"Full queue overflow");form.Close();
                }
            });
            check("Member request keyboard edges minimum scrolling and disposal remain bounded",delegate
            {
                using(var form=new PracticeFleetMemberRequestStudy(programs,map(731)))
                {
                    Show(form,owner);form.ActiveControl=Find<Button>(form,"memberRequestExecute");var down=typeof(PracticeFleetMemberRequestStudy).GetMethod("OnKeyDown",BindingFlags.Instance|BindingFlags.NonPublic);down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});var first=form.StudyState;down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(first.Executed&&Object.ReferenceEquals(first,form.StudyState)&&first.QueuedCells.Count==20,"Held/repeated request visit");form.Size=form.MinimumSize;Application.DoEvents();Need(Find<Control>(form,"memberRequestCanvas").Width>500&&Find<Panel>(form,"memberRequestScroll").AutoScroll,"Minimum request layout");form.Close();
                }
            });
            check("Startup member request entry is independent and rejects other seeds",delegate
            {
                using(var parent=new PracticeMapStartupStudy(programs,input))
                {
                    Show(parent,owner);var before=parent.StudyState;var button=Find<Button>(parent,"mapStartupMemberRequests");Need(button.Enabled,"Request entry disabled");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=0;Need(!button.Enabled,"Early stage admitted request study");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=3;bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100})
                    {
                        timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Request child missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var request=child as PracticeFleetMemberRequestStudy;if(request==null)continue;timer.Stop();try{Find<Button>(request,"memberRequestExecute").PerformClick();Need(request.StudyState.QueuedCells.Count==20,"Child requests");saw=true;}catch(Exception e){failure=e;}finally{request.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();
                    }
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(before,parent.StudyState),"Request child modified parent");parent.Close();
                }
                using(var bad=new PracticeFleetMemberRequestStudy(programs,map(0))){Show(bad,owner);Need(bad.StudyState==null&&!Find<Button>(bad,"memberRequestExecute").Enabled&&Find<Label>(bad,"memberRequestFeedback").Text.Contains("seed731"),"Other seed accepted");bad.Close();}
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new member request preview directory.");Directory.CreateDirectory(destination);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            using(var form=new PracticeFleetMemberRequestStudy(programs,map))
            {
                Show(form,null);Action<string> capture=delegate(string name){form.PerformLayout();Application.DoEvents();using(var b=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(b,new Rectangle(Point.Empty,form.Size));b.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
                capture("01-Prepared");Find<Button>(form,"memberRequestExecute").PerformClick();capture("02-Queued-Twenty");Find<ComboBox>(form,"memberRequestVisibility").SelectedIndex=1;Find<Button>(form,"memberRequestLoad").PerformClick();Find<Button>(form,"memberRequestExecute").PerformClick();capture("03-Midpoint-Requests");Find<ComboBox>(form,"memberRequestEquipment").SelectedIndex=1;Find<ComboBox>(form,"memberRequestTask").SelectedIndex=1;Find<Button>(form,"memberRequestLoad").PerformClick();Find<Button>(form,"memberRequestExecute").PerformClick();capture("04-F3-One-Per-Unit");form.Size=form.MinimumSize;Find<Panel>(form,"memberRequestScroll").AutoScrollPosition=Point.Empty;capture("05-Minimum-Inputs");Find<Panel>(form,"memberRequestScroll").ScrollControlIntoView(Find<ListBox>(form,"memberRequestCells"));capture("06-Minimum-Requests");form.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Six actual-form member request previews: prepared copy,twenty F2 requests,midpoint selector,F3 one per unit,andminimum inputs/request list. Colored crosses are queued requests,not shots or hits. No order execution,ammo use,projectiles or damage. Raw visibility/globalweapon/callerword/queue fixtures explicit. Physical input/scaling/audio separate.\r\n");return 0;
        }
    }
}
