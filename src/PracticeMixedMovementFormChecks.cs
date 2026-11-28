using System;using System.Drawing;using System.IO;using System.Reflection;using System.Text;using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeMixedMovementFormChecks
    {
        static void Need(bool v,string why){if(!v)throw new InvalidDataException(why);}
        static T Find<T>(Control f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        static void Click(Form f,string name){Find<Button>(f,name).PerformClick();Application.DoEvents();}
        static void Load(PracticeMixedNavigationStudy f,int n){Find<ComboBox>(f,"mixedNavigationPreset").SelectedIndex=n;Click(f,"mixedNavigationLoad");}
        static void Advance(PracticeMixedNavigationStudy f,int count){var button=Find<Button>(f,"mixedNavigationOne");for(int n=0;n<count;n++)button.PerformClick();Application.DoEvents();}
        static void Key(Form f,bool down,Keys key){typeof(Control).GetMethod(down?"OnKeyDown":"OnKeyUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,new object[]{new KeyEventArgs(key)});}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var raw=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,raw,731);};
            check("Mixed moving view exposes seven pending scenarios and source-scheduled ship and aircraft motion",delegate{
                using(var f=new PracticeMixedNavigationStudy(programs,startup(),true)){Show(f,owner);Need(f.StudyState==null&&f.MovementStudyState.Visits==0&&Find<ComboBox>(f,"mixedNavigationPreset").Items.Count==7,"Separate moving comparison");Find<ComboBox>(f,"mixedNavigationPreset").SelectedIndex=1;Need(f.MovementStudyState.Preset==0&&Find<Label>(f,"mixedNavigationSummary").Text.Contains("Pending"),"Moving choice pending");Click(f,"mixedNavigationLoad");Advance(f,1);Need(f.MovementStudyState.Visits==1&&BitConverter.ToInt16(f.MovementStudyState.CopyFleetPool(),16*492+12)==107&&f.MovementStudyState.TotalShipMoves==1,"Source scheduledship moves7");Advance(f,1);var fleet=f.MovementStudyState.CopyFleetPool();Need(f.MovementStudyState.Visits==2&&BitConverter.ToInt16(fleet,27*492+12)==227&&BitConverter.ToInt16(fleet,27*492+14)==250&&f.MovementStudyState.TotalAircraftMoves==1&&Find<Label>(f,"mixedNavigationSummary").Text.Contains("Polls 2 / 96"),"Source aircraft movesonitsownscheduledvisit");}
            });
            check("Mixed moving short-route view shows selected-fleet writeback at64 and resets applied copy",delegate{
                using(var f=new PracticeMixedNavigationStudy(programs,startup(),true)){Show(f,owner);Load(f,6);Click(f,"mixedNavigationBoundary");Need(f.MovementStudyState.SelectedFleetFlag==109&&f.MovementStudyState.Visits==63,"Source beforewriteback");Advance(f,1);Need(f.MovementStudyState.SelectedFleetFlag==0&&f.MovementStudyState.MovementChangedSharedBacking&&f.MovementStudyState.SelectedWaypoint==4,"Source clear selected249 at64");Find<ComboBox>(f,"mixedNavigationPreset").SelectedIndex=2;Click(f,"mixedNavigationReset");Need(f.MovementStudyState.Preset==6&&f.MovementStudyState.Visits==0&&f.MovementStudyState.SelectedFleetFlag==109&&Find<ComboBox>(f,"mixedNavigationPreset").SelectedIndex==2,"Applied reset keeps pendingchoice");}
            });
            check("Mixed moving keyboard edges,bounded96polls,minimum scroll and close remain usable",delegate{
                using(var f=new PracticeMixedNavigationStudy(programs,startup(),true)){Show(f,owner);Load(f,1);Find<Button>(f,"mixedNavigationOne").Focus();Key(f,false,Keys.Enter);Key(f,true,Keys.Enter);Key(f,true,Keys.Enter);Need(f.MovementStudyState.Visits==1,"NoheldEnter repeats");Key(f,false,Keys.Enter);Key(f,true,Keys.Enter);Key(f,false,Keys.Enter);Need(f.MovementStudyState.Visits==2,"Release admitsnextpoll");Click(f,"mixedNavigationAll");Need(f.MovementStudyState.Visits==96&&!Find<Button>(f,"mixedNavigationOne").Enabled&&!Find<Button>(f,"mixedNavigationAll").Enabled,"96pollwindowstops");f.ClientSize=new Size(800,580);Application.DoEvents();var scroll=Find<Panel>(f,"mixedNavigationScroll");scroll.AutoScrollPosition=new Point(0,1000);Application.DoEvents();var close=Find<Button>(f,"mixedNavigationClose");var bounds=new Rectangle(f.PointToClient(close.PointToScreen(Point.Empty)),close.Size);Need(f.ClientRectangle.Contains(bounds)&&!scroll.RectangleToScreen(scroll.ClientRectangle).IntersectsWith(close.RectangleToScreen(close.ClientRectangle))&&Find<TextBox>(f,"mixedNavigationDetails").ReadOnly,"Visible independentClose andscrollingdetails");Need(owner.Controls.Find("openMixedMovingStudy",true).Length==1,"Direct movingmapmenu entry");Click(f,"mixedNavigationClose");Need(f.IsDisposed||!f.Visible,"Movingcloseclean");}
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a fresh moving-fleet preview directory.");Directory.CreateDirectory(destination);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var startup=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var log=new StringBuilder();
            using(var f=new PracticeMixedNavigationStudy(programs,startup,true))
            {
                Show(f,null);Action<string> save=delegate(string name){Application.DoEvents();using(var bitmap=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bitmap,new Rectangle(Point.Empty,f.Size));bitmap.Save(Path.Combine(destination,name+".png"));}var s=f.MovementStudyState;log.AppendLine(name+":profile="+s.Preset+" polls="+s.Visits+" cursor="+s.Schedule.BatchStart+" cycles="+s.Schedule.CompletedCycles+" selectedFlag="+s.SelectedFleetFlag);};
                save("01-Moving-Prepared");Load(f,1);Advance(f,2);save("02-Aircraft-Moved-2");Load(f,0);Advance(f,1);save("03-Ship-Moved-1");Load(f,2);Click(f,"mixedNavigationAll");save("04-Window96");Load(f,6);Click(f,"mixedNavigationBoundary");save("05-Short-Before-63");Advance(f,1);save("06-Short-Writeback-64");f.ClientSize=new Size(800,580);Application.DoEvents();save("07-Minimum-Summary");Find<Panel>(f,"mixedNavigationScroll").AutoScrollPosition=new Point(0,1000);save("08-Minimum-Details-Close");f.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),log+"Eight actual-form software renders of copied source-backed policy/navigation/two-batch moving scenarios. Suppliedphase/caller/clock/zerocolumns;not fullworld/naturalpacing/render/audio/physicalacceptance.\r\n");return 0;
        }
    }
}
