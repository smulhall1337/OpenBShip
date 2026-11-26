using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeFleetRequestLaunchFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form form,string name)where T:Control{return(T)form.Controls.Find(name,true)[0];}
        static void Show(Form form,Form owner){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var input=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));
            Func<uint,PracticeMapStartup> map=delegate(uint seed){return PracticeMapStartup.CreateBssb202(programs,input,seed);};
            check("Member launch form allocates records and restores applied inputs without changing pending selectors",delegate
            {
                using(var form=new PracticeFleetRequestLaunchStudy(programs,map(731)))
                {
                    Show(form,owner);Need(!form.StudyState.Executed&&form.StudyState.RequestCount(0)==10&&form.StudyState.Ammunition(0)==7,"Launch initial state");
                    var button=Find<Button>(form,"requestLaunchExecute");button.PerformClick();Need(form.StudyState.CreatedObjects==2&&form.StudyState.Ammunition(0)==6&&form.StudyState.RequestCount(1)==9&&!button.Enabled&&Find<ListBox>(form,"requestLaunchObjects").Items.Count==2,"Empty-pool launch");
                    var before=form.StudyState;Find<ListBox>(form,"requestLaunchObjects").SelectedIndex=1;Need(Object.ReferenceEquals(before,form.StudyState)&&Find<TextBox>(form,"requestLaunchDetails").Text.Contains("unit 31"),"Record selection changed state");
                    Find<ComboBox>(form,"requestLaunchCapacity").SelectedIndex=1;Find<Button>(form,"requestLaunchReset").PerformClick();Need(!form.StudyState.Executed&&form.StudyState.Capacity==0&&Find<ComboBox>(form,"requestLaunchCapacity").SelectedIndex==1&&button.Enabled,"Pending/applied reset");
                    Find<Button>(form,"requestLaunchLoad").PerformClick();button.PerformClick();Need(form.StudyState.CreatedObjects==1&&form.StudyState.Projectiles[0].Slot==79&&form.StudyState.Ammunition(1)==7,"One-free case load");form.Close();
                }
            });
            check("Member launch form distinguishes full-pool loss from F3 successful allocation",delegate
            {
                using(var form=new PracticeFleetRequestLaunchStudy(programs,map(731)))
                {
                    Show(form,owner);Find<ComboBox>(form,"requestLaunchCapacity").SelectedIndex=2;Find<Button>(form,"requestLaunchLoad").PerformClick();Find<Button>(form,"requestLaunchExecute").PerformClick();Need(form.StudyState.ConsumedRequests==2&&form.StudyState.CreatedObjects==0&&form.StudyState.Ammunition(0)==7&&Find<Label>(form,"requestLaunchFeedback").Text.Contains("full pool"),"Full-pool feedback");
                    Find<ComboBox>(form,"requestLaunchFamily").SelectedIndex=1;Find<ComboBox>(form,"requestLaunchCapacity").SelectedIndex=0;Find<Button>(form,"requestLaunchLoad").PerformClick();Need(form.StudyState.RequestCount(0)==1,"F3 request prefix");Find<Button>(form,"requestLaunchExecute").PerformClick();Need(form.StudyState.RequestCount(0)==0&&form.StudyState.CreatedObjects==2&&form.StudyState.Projectiles[0].Weapon==19,"F3 launch");form.Close();
                }
            });
            check("Member launch keyboard edges minimum scrolling and close are bounded",delegate
            {
                using(var form=new PracticeFleetRequestLaunchStudy(programs,map(731)))
                {
                    Show(form,owner);form.ActiveControl=Find<Button>(form,"requestLaunchExecute");var down=typeof(PracticeFleetRequestLaunchStudy).GetMethod("OnKeyDown",BindingFlags.Instance|BindingFlags.NonPublic);
                    down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});var first=form.StudyState;down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(first.Executed&&Object.ReferenceEquals(first,form.StudyState)&&first.CreatedObjects==2,"Held launch button");
                    form.Size=form.MinimumSize;Application.DoEvents();Need(Find<Control>(form,"requestLaunchCanvas").Width>500&&Find<Panel>(form,"requestLaunchScroll").AutoScroll,"Minimum launch layout");form.Close();
                }
            });
            check("Startup launch child is independent stage-gated and rejects unsupported seeds",delegate
            {
                using(var parent=new PracticeMapStartupStudy(programs,input))
                {
                    Show(parent,owner);var before=parent.StudyState;var button=Find<Button>(parent,"mapStartupRequestLaunch");Need(button.Enabled,"Launch entry disabled");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=0;Need(!button.Enabled,"Early startup launch admitted");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=3;bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100})
                    {
                        timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Launch child missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var launch=child as PracticeFleetRequestLaunchStudy;if(launch==null)continue;timer.Stop();try{Find<Button>(launch,"requestLaunchExecute").PerformClick();Need(launch.StudyState.CreatedObjects==2,"Child launch");saw=true;}catch(Exception e){failure=e;}finally{launch.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();
                    }
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(before,parent.StudyState),"Launch child changed parent");parent.Close();
                }
                using(var bad=new PracticeFleetRequestLaunchStudy(programs,map(0))){Show(bad,owner);Need(bad.StudyState==null&&!Find<Button>(bad,"requestLaunchExecute").Enabled&&Find<Label>(bad,"requestLaunchFeedback").Text.Contains("seed731"),"Unsupported launch seed accepted");bad.Close();}
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new member launch preview directory.");Directory.CreateDirectory(destination);
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            using(var form=new PracticeFleetRequestLaunchStudy(programs,map))
            {
                Show(form,null);Action<string> capture=delegate(string name){form.PerformLayout();Application.DoEvents();using(var b=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(b,new Rectangle(Point.Empty,form.Size));b.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
                capture("01-Ready");Find<Button>(form,"requestLaunchExecute").PerformClick();capture("02-Two-Allocated");
                Find<ComboBox>(form,"requestLaunchCapacity").SelectedIndex=1;Find<Button>(form,"requestLaunchLoad").PerformClick();Find<Button>(form,"requestLaunchExecute").PerformClick();capture("03-Last-Free-Slot");
                Find<ComboBox>(form,"requestLaunchCapacity").SelectedIndex=2;Find<Button>(form,"requestLaunchLoad").PerformClick();Find<Button>(form,"requestLaunchExecute").PerformClick();capture("04-Full-Pool");
                Find<ComboBox>(form,"requestLaunchFamily").SelectedIndex=1;Find<ComboBox>(form,"requestLaunchCapacity").SelectedIndex=0;Find<Button>(form,"requestLaunchLoad").PerformClick();Find<Button>(form,"requestLaunchExecute").PerformClick();capture("05-F3-Allocated");
                form.Size=form.MinimumSize;Find<Panel>(form,"requestLaunchScroll").AutoScrollPosition=Point.Empty;capture("06-Minimum-Inputs");
                Find<Panel>(form,"requestLaunchScroll").ScrollControlIntoView(Find<TextBox>(form,"requestLaunchDetails"));capture("07-Minimum-Details");form.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Seven actual-form previews: ready,two allocated,last free slot,full pool,F3,and minimum inputs/details. Numeric allocations and ammo only; dashed lines are stored destinations,not moving projectiles. No impact,damage,audio,movie or full AI lifecycle. Physical input/display scaling remain separate.\r\n");return 0;
        }
    }
}
