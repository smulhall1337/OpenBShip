using System;using System.Drawing;using System.IO;using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeNavigationPhaseFormChecks
    {
        static T Find<T>(Control c,string name)where T:Control{return(T)c.Controls.Find(name,true)[0];}
        static void Need(bool v,string why){if(!v)throw new InvalidDataException(why);}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        static void Click(Form f,string name){Find<Button>(f,name).PerformClick();Application.DoEvents();}
        static void Load(PracticeNavigationMovementStudy f,int p){Find<ComboBox>(f,"navigationMoveProfile").SelectedIndex=p;Click(f,"navigationMoveLoad");}
        static void Advance(PracticeNavigationMovementStudy f,int count){while(count>=32){Click(f,"navigationMoveMany");count-=32;}while(count-->0)Click(f,"navigationMoveOne");}
        static void ScrollTo(Form f,Control c){Find<Panel>(f,"navigationMoveScroll").ScrollControlIntoView(c);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);};
            check("Phase navigation form holds phase30 for three frames then clears visibility on produced phase32",delegate{
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    Show(f,owner);Load(f,17);Advance(f,30);int cursor=f.StudyState.State.Schedule.BatchStart;uint clock=f.StudyState.Clock.CurrentMilliseconds;
                    Click(f,"navigationMoveOne");var box=Find<TextBox>(f,"navigationMoveClock");Need(f.StudyState.Polls==31&&f.StudyState.Phase.Value==30&&f.StudyState.PausedFrames==1&&f.StudyState.DriverVisits==30&&f.StudyState.State.Schedule.BatchStart==cursor&&f.StudyState.Clock.CurrentMilliseconds>clock&&box.Text.Contains("Produced phase: 30 / 30 / 30")&&box.Text.Contains("SIMULATION HELD")&&box.Text.Contains("Source gate held both scheduler batches."),"Phase/scheduler hold with live header");
                    Advance(f,2);Need(f.StudyState.Polls==33&&f.StudyState.Phase.Value==30&&f.StudyState.PausedFrames==3&&f.StudyState.NeutralPasses==0,"Three held frames");
                    Advance(f,2);Need(f.StudyState.Polls==35&&f.StudyState.Phase.Value==32&&f.StudyState.NeutralPasses==1&&box.Text.Contains("visibility/contact: 0 / 0"),"Neutral clear follows produced phase,not frame index");f.Close();
                }
            });
            check("Phase navigation form holds aligned phase without repeats and shows delayed replan before zero reset",delegate{
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    Show(f,owner);Load(f,19);Advance(f,32);int cursor=f.StudyState.State.Schedule.BatchStart;Advance(f,3);
                    Need(f.StudyState.Polls==35&&f.StudyState.Phase.Value==32&&f.StudyState.NeutralPasses==1&&f.StudyState.DriverVisits==32&&f.StudyState.PausedFrames==3&&f.StudyState.State.Schedule.BatchStart==cursor&&Find<TextBox>(f,"navigationMoveClock").Text.Contains("Pause WORD: -32768"),"No repeated aligned source driver");
                    Find<ComboBox>(f,"navigationMoveProfile").SelectedIndex=18;Click(f,"navigationMoveReset");Need(f.StudyState.Preset==19&&f.StudyState.Phase.Value==0&&f.StudyState.Polls==0&&Find<ComboBox>(f,"navigationMoveProfile").SelectedIndex==18,"Applied reset preserves pending choice");
                    Click(f,"navigationMoveLoad");Click(f,"navigationMoveCollision");Need(f.StudyState.Polls==98&&f.StudyState.Phase.Value==95&&f.StudyState.TerrainCollisionVisits==1&&f.StudyState.ReplanEntries==0,"Delayed collision remains observable");
                    Click(f,"navigationMoveCollision");var box=Find<TextBox>(f,"navigationMoveClock");Need(f.StudyState.Polls==99&&f.StudyState.Phase.Value==96&&f.StudyState.ReplanEntries==1&&f.StudyState.Clock.GameMilliseconds==1&&box.Text.Contains("Task time: 16225")&&box.Text.Contains("Game: 16225 / 16225 / 1"),"Produced-phase source replan before epilogue");
                    Click(f,"navigationMoveOne");Need(f.StudyState.Polls==100&&box.Text.Contains("Delta: 1 / 1 / 16304"),"Carried zero reset");f.Close();
                }
            });
            check("Phase navigation bounded watch minimum scroll and end-window close remain usable",delegate{
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    Show(f,owner);Load(f,18);Click(f,"navigationMoveWatch");Need(f.Watching,"Bounded watch start");f.StepWatchForCheck();Find<ComboBox>(f,"navigationMoveProfile").SelectedIndex=17;Need(!f.Watching&&f.StudyState.Preset==18,"Pending selection stops watch");
                    int held=f.StudyState.Polls;f.StepWatchForCheck();Need(f.StudyState.Polls==held,"No automatic resume");Click(f,"navigationMoveAll");Need(f.StudyState.Polls==128&&f.StudyState.Phase.Value==125&&f.StudyState.DriverVisits==125&&f.StudyState.PausedFrames==3&&!Find<Button>(f,"navigationMoveOne").Enabled,"Bounded phase end");
                    f.Size=f.MinimumSize;var box=Find<TextBox>(f,"navigationMoveClock");ScrollTo(f,box);Need(box.Visible&&box.Width>=300&&box.Text.Contains("Driver visits: 125; held frames: 3"),"Phase details at minimum size");var close=Find<Button>(f,"navigationMoveClose");ScrollTo(f,close);var bounds=new Rectangle(f.PointToClient(close.PointToScreen(Point.Empty)),close.Size);Need(f.ClientRectangle.Contains(bounds),"Visible minimum Close");Click(f,"navigationMoveClose");Need(f.IsDisposed||!f.Visible,"Clean Close");
                }
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a fresh phase-navigation preview directory.");Directory.CreateDirectory(destination);
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var startup=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            using(var f=new PracticeNavigationMovementStudy(programs,startup))
            {
                Show(f,null);Action<string> capture=delegate(string name){Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(destination,name+".png"));}};
                Load(f,17);capture("01-Prepared");Advance(f,31);ScrollTo(f,Find<TextBox>(f,"navigationMoveClock"));capture("02-Phase30-Held-31");Advance(f,4);capture("03-Phase32-Resume-35");
                Load(f,19);Advance(f,35);ScrollTo(f,Find<TextBox>(f,"navigationMoveClock"));capture("04-Aligned-Phase32-Held");
                Load(f,18);Click(f,"navigationMoveCollision");ScrollTo(f,Find<TextBox>(f,"navigationMoveClock"));capture("05-Delayed-Collision-98");Click(f,"navigationMoveCollision");capture("06-Replan-Zero-99");
                Click(f,"navigationMoveOne");f.Size=f.MinimumSize;ScrollTo(f,Find<TextBox>(f,"navigationMoveClock"));capture("07-Minimum-Carried-100");ScrollTo(f,Find<Button>(f,"navigationMoveClose"));capture("08-Minimum-Close");f.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Eight actual-form software renders of copied original phase/pause gate/clock/neutral navigation contexts. Phase produced from supplied WORDs,samples/caller/obstacle timing remain supplied. Not fullworld/fullpause/naturalpacing;physical display/input separate.\r\n");return 0;
        }
    }
}
