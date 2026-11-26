using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeFleetRequestFlightFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            Func<int,PracticeFleetRequestLaunch> launch=delegate(int family){return PracticeFleetRequestLaunch.Create(programs,map,family,0).Execute();};
            check("Flight form advances copied points stops before impact and resets without changing launch",delegate
            {
                var parent=launch(0);using(var f=new PracticeFleetRequestFlightStudy(parent))
                {
                    Show(f,owner);Find<Button>(f,"memberFlightBatch").PerformClick();Need(f.StudyState.Clock==16125&&f.StudyState.Projectiles[0].X==242&&f.StudyState.Projectiles[1].Y==426,"32-update flight");
                    Find<Button>(f,"memberFlightBoundary").PerformClick();Need(f.StudyState.Clock==16344&&f.StudyState.AtBoundary&&!Find<Button>(f,"memberFlightStep").Enabled&&!Find<Button>(f,"memberFlightBatch").Enabled&&!Find<Button>(f,"memberFlightBoundary").Enabled&&Find<Label>(f,"memberFlightFeedback").Text.Contains("No expiry"),"Impact stop feedback");
                    Find<Button>(f,"memberFlightReset").PerformClick();Need(f.StudyState.Updates==0&&f.StudyState.Clock==14845&&Find<Button>(f,"memberFlightStep").Enabled&&parent.Projectiles[0].X==231&&parent.Ammunition(0)==6,"Reset/parent preservation");f.Close();
                }
            });
            check("F3 flight inspection retains two live records at guarded boundary and scrolls at minimum size",delegate
            {
                using(var f=new PracticeFleetRequestFlightStudy(launch(1)))
                {
                    Show(f,owner);Find<Button>(f,"memberFlightBoundary").PerformClick();Need(f.StudyState.Clock==28944&&f.StudyState.Projectiles[0].X==307&&f.StudyState.Projectiles[1].X==325&&f.StudyState.LastStoppedAtRecord==1,"F3 boundary");var before=f.StudyState;Find<ListBox>(f,"memberFlightObjects").SelectedIndex=1;Need(Object.ReferenceEquals(before,f.StudyState)&&Find<TextBox>(f,"memberFlightDetails").Text.Contains("unit 31"),"Selection mutated flight");
                    f.Size=f.MinimumSize;Application.DoEvents();Need(Find<Control>(f,"memberFlightCanvas").Width>500&&Find<Panel>(f,"memberFlightScroll").AutoScroll,"Minimum flight layout");f.Close();
                }
            });
            check("Flight form suppresses held confirmation and rejects unavailable launch contexts",delegate
            {
                using(var f=new PracticeFleetRequestFlightStudy(launch(0)))
                {
                    Show(f,owner);f.ActiveControl=Find<Button>(f,"memberFlightStep");var down=typeof(PracticeFleetRequestFlightStudy).GetMethod("OnKeyDown",BindingFlags.Instance|BindingFlags.NonPublic);down.Invoke(f,new object[]{new KeyEventArgs(Keys.Enter)});var first=f.StudyState;down.Invoke(f,new object[]{new KeyEventArgs(Keys.Enter)});Need(first.Updates==1&&Object.ReferenceEquals(first,f.StudyState),"Held flight confirmation");f.Close();
                }
                using(var bad=new PracticeFleetRequestFlightStudy(PracticeFleetRequestLaunch.Create(programs,map,0,1).Execute())){Show(bad,owner);Need(bad.StudyState==null&&!Find<Button>(bad,"memberFlightStep").Enabled&&Find<Label>(bad,"memberFlightFeedback").Text.Contains("Capacity-only"),"Capacity records admitted to flight");bad.Close();}
            });
            check("Local launch exposes an independent flight child only after an empty-pool allocation",delegate
            {
                using(var parent=new PracticeFleetRequestLaunchStudy(programs,map))
                {
                    Show(parent,owner);var button=Find<Button>(parent,"requestLaunchFlight");Need(!button.Enabled,"Unlaunched flight enabled");Find<Button>(parent,"requestLaunchExecute").PerformClick();Need(button.Enabled,"Completed empty launch disabled");var before=parent.StudyState;bool saw=false;Exception failure=null;int ticks=0;
                    using(var timer=new Timer{Interval=100})
                    {
                        timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Flight child missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var flight=child as PracticeFleetRequestFlightStudy;if(flight==null)continue;timer.Stop();try{Find<Button>(flight,"memberFlightBatch").PerformClick();Need(flight.StudyState.Updates==32,"Child flight batch");saw=true;}catch(Exception e){failure=e;}finally{flight.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();
                    }
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(before,parent.StudyState)&&parent.StudyState.Projectiles[0].X==231,"Flight modified parent");Find<ComboBox>(parent,"requestLaunchCapacity").SelectedIndex=1;Find<Button>(parent,"requestLaunchLoad").PerformClick();Find<Button>(parent,"requestLaunchExecute").PerformClick();Need(!button.Enabled,"Capacity-only flight enabled");parent.Close();
                }
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new copied flight preview directory.");Directory.CreateDirectory(destination);
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            for(int family=0;family<2;family++)using(var form=new PracticeFleetRequestFlightStudy(PracticeFleetRequestLaunch.Create(programs,map,family,0).Execute()))
            {
                Show(form,null);Action<string> capture=delegate(string name){form.PerformLayout();Application.DoEvents();using(var b=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(b,new Rectangle(Point.Empty,form.Size));b.Save(Path.Combine(destination,(family==0?"F2-":"F3-")+name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
                capture("01-Launch");Find<Button>(form,"memberFlightBatch").PerformClick();capture("02-Thirty-Two");Find<Button>(form,"memberFlightBoundary").PerformClick();capture("03-Boundary");form.Size=form.MinimumSize;Find<Panel>(form,"memberFlightScroll").ScrollControlIntoView(Find<TextBox>(form,"memberFlightDetails"));capture("04-Minimum-Details");form.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Eight actual-form copied flight previews: F2/F3 launch,32 updates,firstdeadline-1 and minimum details. Future-only numeric updates; no impacts,damage,further firing,media/audio or natural AI pacing. Physical input/scaling acceptance separate.\r\n");return 0;
        }
    }
}
