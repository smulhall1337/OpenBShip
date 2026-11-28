using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FleetLab
{
    internal static class PracticeNavigationClockFormChecks
    {
        static T Find<T>(Control c,string name)where T:Control{return(T)c.Controls.Find(name,true)[0];}
        static void Need(bool v,string why){if(!v)throw new InvalidDataException(why);}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        static void Click(Form f,string name){Find<Button>(f,name).PerformClick();Application.DoEvents();}
        static void Load(PracticeNavigationMovementStudy f,int preset){Find<ComboBox>(f,"navigationMoveProfile").SelectedIndex=preset;Click(f,"navigationMoveLoad");}
        static void ThreeBatches(PracticeNavigationMovementStudy f){for(int n=0;n<3;n++)Click(f,"navigationMoveMany");}
        static void ScrollTo(Form f,Control c){Find<Panel>(f,"navigationMoveScroll").ScrollControlIntoView(c);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);};
            check("Navigation clock form shows collision replan before the zero reset and preserves pending load/reset",delegate{
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    Show(f,owner);Load(f,15);var clock=Find<TextBox>(f,"navigationMoveClock");Need(clock.Visible&&clock.ReadOnly&&f.StudyState.Polls==0&&clock.Text.Contains("12305"),"Prepared recovered-clock copy");
                    ThreeBatches(f);Need(f.StudyState.Polls==96&&f.StudyState.TerrainCollisionVisits==1&&f.StudyState.ReplanEntries==1&&f.StudyState.Clock.GameMilliseconds==1&&clock.Text.Contains("Game: 16105 / 16105 / 1")&&clock.Text.Contains("Task time: 16105"),"Whole body must precede clock zero reset");
                    Click(f,"navigationMoveOne");Need(f.StudyState.Polls==97&&clock.Text.Contains("Delta: 1 / 1 / 16184"),"Next body sees carried delta1");
                    Find<ComboBox>(f,"navigationMoveProfile").SelectedIndex=0;Need(f.StudyState.Preset==15&&clock.Visible,"Pending selector changed applied copy");
                    Click(f,"navigationMoveReset");Need(f.StudyState.Preset==15&&f.StudyState.Polls==0&&f.StudyState.Clock.CurrentMilliseconds==12305&&Find<ComboBox>(f,"navigationMoveProfile").SelectedIndex==0,"Reset must retain applied preset and pending choice");
                    Click(f,"navigationMoveLoad");Need(f.StudyState.Preset==0&&!clock.Visible&&!f.StudyState.UsesRecoveredClock,"Legacy study must keep its presentation");f.Close();
                }
            });
            check("Navigation clock form exposes phase32 neutral clear and bounded fleet collision then replan",delegate{
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    Show(f,owner);Load(f,14);Click(f,"navigationMoveMany");Need(f.StudyState.Polls==32&&f.StudyState.NeutralPasses==1&&Find<TextBox>(f,"navigationMoveClock").Text.Contains("visibility/contact: 0 / 0"),"Phase32 neutral clear");
                    Click(f,"navigationMoveAll");Need(f.StudyState.Polls==128&&f.StudyState.NeutralPasses==4&&!Find<Button>(f,"navigationMoveOne").Enabled&&!Find<Button>(f,"navigationMoveWatch").Enabled,"128-frame window");
                    Load(f,16);Click(f,"navigationMoveCollision");Need(f.StudyState.Polls==95&&f.StudyState.FleetCollisionVisits==1&&f.StudyState.ReplanEntries==0,"Fleet collision must remain observable");
                    Click(f,"navigationMoveCollision");Need(f.StudyState.Polls==96&&f.StudyState.ReplanEntries==1&&f.StudyState.Clock.GameMilliseconds==16145&&Find<TextBox>(f,"navigationMoveClock").Text.Contains("Task time: 16145"),"Following whole driver replan");f.Close();
                }
            });
            check("Navigation clock minimum scrolling and bounded watch stop preserve usable close",delegate{
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    Show(f,owner);Load(f,15);f.Size=f.MinimumSize;Click(f,"navigationMoveWatch");Need(f.Watching,"Watch not started");f.StepWatchForCheck();
                    Find<ComboBox>(f,"navigationMoveProfile").SelectedIndex=14;Need(!f.Watching&&f.StudyState.Preset==15,"Pending change must stop bounded watch");int held=f.StudyState.Polls;f.StepWatchForCheck();Need(f.StudyState.Polls==held,"Stopped watch advanced");
                    var clock=Find<TextBox>(f,"navigationMoveClock");ScrollTo(f,clock);Need(clock.Visible&&clock.Width>=300&&clock.Text.Contains("BEFORE / BODY / AFTER"),"Clock diagnostics at minimum size");
                    var close=Find<Button>(f,"navigationMoveClose");ScrollTo(f,close);var bounds=new Rectangle(f.PointToClient(close.PointToScreen(Point.Empty)),close.Size);Need(f.ClientRectangle.Contains(bounds),"Scrolled Close must fit the visible client");Click(f,"navigationMoveClose");Need(f.IsDisposed||!f.Visible,"Close must complete");
                }
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a fresh navigation-clock preview directory.");Directory.CreateDirectory(destination);
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var startup=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            using(var f=new PracticeNavigationMovementStudy(programs,startup))
            {
                Show(f,null);Action<string> capture=delegate(string name){Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(destination,name+".png"));}};
                Load(f,15);capture("01-Prepared");ThreeBatches(f);ScrollTo(f,Find<TextBox>(f,"navigationMoveClock"));capture("02-Zero-Reset-Replan-96");Click(f,"navigationMoveOne");capture("03-Carried-Delta-97");
                Load(f,14);Click(f,"navigationMoveMany");ScrollTo(f,Find<TextBox>(f,"navigationMoveClock"));capture("04-Phase32-Clear");
                Load(f,16);Click(f,"navigationMoveCollision");ScrollTo(f,Find<TextBox>(f,"navigationMoveClock"));capture("05-Fleet-Collision-95");Click(f,"navigationMoveCollision");capture("06-Fleet-Replan-96");
                Load(f,15);ThreeBatches(f);f.Size=f.MinimumSize;ScrollTo(f,Find<TextBox>(f,"navigationMoveClock"));capture("07-Minimum-Clock");ScrollTo(f,Find<Button>(f,"navigationMoveClose"));capture("08-Minimum-Close");f.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Eight actual maintained-form software renders. Original964/965/966 clock+neutral-driver+two-batch profiles,not natural pacing/fullworld. Silent copied data;no battle/settings changes. Physical scaling/input remains separate.\r\n");return 0;
        }
    }
}
