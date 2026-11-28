using System;using System.Drawing;using System.IO;using System.Text;using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeMixedClockFormChecks
    {
        static void Need(bool v,string why){if(!v)throw new InvalidDataException(why);}
        static T Find<T>(Control f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        static void Click(Form f,string name){Find<Button>(f,name).PerformClick();Application.DoEvents();}
        static void Load(PracticeMixedNavigationStudy f,int h){Find<ComboBox>(f,"mixedNavigationPreset").SelectedIndex=h;Click(f,"mixedNavigationLoad");}
        static void Advance(PracticeMixedNavigationStudy f,int n){var one=Find<Button>(f,"mixedNavigationOne");for(int k=0;k<n;k++)one.PerformClick();Application.DoEvents();}
        static void ScrollClock(PracticeMixedNavigationStudy f){Find<Panel>(f,"mixedNavigationScroll").ScrollControlIntoView(Find<TextBox>(f,"mixedNavigationClock"));Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var raw=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,raw,731);};
            check("Clock-moving form displays phase30hold and phase32resume without repeated held simulation",delegate{
                using(var f=new PracticeMixedNavigationStudy(programs,startup(),2)){Show(f,owner);Need(f.ClockStudyState.Visits==0&&f.ClockStudyState.Phase.Value==0&&f.StudyState==null&&f.MovementStudyState==null,"Separate clock-mode initial");Click(f,"mixedNavigationBoundary");Need(f.ClockStudyState.Phase.Value==30&&f.ClockStudyState.HeldFrames==1&&Find<TextBox>(f,"mixedNavigationClock").Text.Contains("Driver: held")&&Find<TextBox>(f,"mixedNavigationClock").Text.Contains("Pause WORD: 1"),"Source clock hold readout");Click(f,"mixedNavigationBoundary");Need(f.ClockStudyState.Visits==35&&f.ClockStudyState.Phase.Value==32&&f.ClockStudyState.DriverVisits==32&&f.ClockStudyState.NeutralPasses==1,"Source resume readout");Load(f,3);Advance(f,35);Need(f.ClockStudyState.Phase.Value==32&&f.ClockStudyState.HeldFrames==3&&f.ClockStudyState.NeutralPasses==1&&Find<TextBox>(f,"mixedNavigationClock").Text.Contains("Pause WORD: -32768"),"Alignedhold doesnotrepeatneutralpass");}
            });
            check("Clock-moving short input shows zero reset,carried15024 and actual frame69activation",delegate{
                using(var f=new PracticeMixedNavigationStudy(programs,startup(),2)){Show(f,owner);Load(f,6);Click(f,"mixedNavigationBoundary");var box=Find<TextBox>(f,"mixedNavigationClock");Need(f.ClockStudyState.Visits==67&&f.ClockStudyState.Phase.Value==64&&f.ClockStudyState.Clock.CurrentMilliseconds==1&&f.ClockStudyState.SelectedFleetFlag==0&&box.Text.Contains("Delta: 40 / 40 / 1"),"Sourcebodybeforezeroepilogue");Advance(f,1);Need(f.ClockStudyState.Visits==68&&box.Text.Contains("Delta: 1 / 1 / 15024")&&BitConverter.ToInt32(f.ClockStudyState.CopyFleetPool(),27*492)==6,"Preceding1delta retained");Advance(f,1);Need(f.ClockStudyState.Visits==69&&box.Text.Contains("Delta: 15024 / 15024 / 40")&&BitConverter.ToInt32(f.ClockStudyState.CopyFleetPool(),27*492)==3&&f.ClockStudyState.UiRequests==1&&f.ClockStudyState.SelectedWaypoint==-1,"Sourceactualactivationafterbigcarrieddelta");}
            });
            check("Clock-moving pending/reset,bounded96frames,minimumclockscroll and Close preserve copied state",delegate{
                using(var f=new PracticeMixedNavigationStudy(programs,startup(),2)){Show(f,owner);Load(f,6);Click(f,"mixedNavigationAll");Need(f.ClockStudyState.Visits==96&&!Find<Button>(f,"mixedNavigationOne").Enabled&&!Find<Button>(f,"mixedNavigationAll").Enabled,"Clockwindowstopsat96");Find<ComboBox>(f,"mixedNavigationPreset").SelectedIndex=3;Click(f,"mixedNavigationReset");Need(f.ClockStudyState.Preset==6&&f.ClockStudyState.Visits==0&&f.ClockStudyState.Phase.Value==0&&Find<ComboBox>(f,"mixedNavigationPreset").SelectedIndex==3,"Appliedclockreset retains pendingchoice");f.ClientSize=new Size(800,580);ScrollClock(f);var box=Find<TextBox>(f,"mixedNavigationClock");Need(box.ReadOnly&&box.Width>=700&&box.Text.Contains("BEFORE / BODY / AFTER"),"Clocktable readableatminimum");var close=Find<Button>(f,"mixedNavigationClose");var bounds=new Rectangle(f.PointToClient(close.PointToScreen(Point.Empty)),close.Size);Need(f.ClientRectangle.Contains(bounds)&&owner.Controls.Find("openMixedClockStudy",true).Length==1,"VisibleClose anddirectclockmenuentry");Click(f,"mixedNavigationClose");Need(f.IsDisposed||!f.Visible,"Clockcloseclean");}
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a fresh clock-moving preview directory.");Directory.CreateDirectory(destination);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var startup=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var log=new StringBuilder();
            using(var f=new PracticeMixedNavigationStudy(programs,startup,2))
            {
                Show(f,null);Action<string> save=delegate(string name){Application.DoEvents();using(var bitmap=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bitmap,new Rectangle(Point.Empty,f.Size));bitmap.Save(Path.Combine(destination,name+".png"));}var s=f.ClockStudyState;log.AppendLine(name+":profile="+s.Preset+" frames="+s.Visits+" phase="+s.Phase.Value+" pause="+s.PauseWord+" delta="+s.BodyClock.DeltaMilliseconds+"/"+s.Clock.DeltaMilliseconds);};
                save("01-Clock-Prepared");Advance(f,31);ScrollClock(f);save("02-Phase30-Held31");Load(f,3);Advance(f,35);ScrollClock(f);save("03-Phase32-Held35");Load(f,6);Click(f,"mixedNavigationBoundary");ScrollClock(f);save("04-Zero-Reset67");Advance(f,1);save("05-Delta-Carry68");Advance(f,1);save("06-Activation69");f.ClientSize=new Size(800,580);ScrollClock(f);save("07-Minimum-Clock");Find<Panel>(f,"mixedNavigationScroll").AutoScrollPosition=new Point(0,2000);save("08-Minimum-Close");f.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),log+"Eight actual-form renders of source-backed clock/phase/pause/moving slice. Suppliedsamples/words/caller/zerocolumns;notfull410D40loop/naturalpacing/ownerAI/world/audio/physicalacceptance.\r\n");return 0;
        }
    }
}
