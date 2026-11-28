using System;using System.Drawing;using System.IO;using System.Reflection;using System.Text;using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeMixedNavigationFormChecks
    {
        static void Need(bool v,string why){if(!v)throw new InvalidDataException(why);}
        static T Find<T>(Control f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        static void Load(PracticeMixedNavigationStudy f,int index){Find<ComboBox>(f,"mixedNavigationPreset").SelectedIndex=index;Click(f,"mixedNavigationLoad");}
        static void Click(Form f,string name){Find<Button>(f,name).PerformClick();Application.DoEvents();}
        static void Key(Form f,bool down,Keys key){typeof(Control).GetMethod(down?"OnKeyDown":"OnKeyUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,new object[]{new KeyEventArgs(key)});}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var raw=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,raw,731);};
            check("Mixed navigation form keeps choices pending and shows masked initial collision alias",delegate{
                using(var f=new PracticeMixedNavigationStudy(programs,startup())){Show(f,owner);Find<ComboBox>(f,"mixedNavigationPreset").SelectedIndex=1;Need(f.StudyState.Preset==0&&f.StudyState.Visits==0&&Find<Label>(f,"mixedNavigationSummary").Text.Contains("Pending"),"Choice must remain pending");Click(f,"mixedNavigationLoad");Click(f,"mixedNavigationOne");Need(f.StudyState.Preset==1&&f.StudyState.Visits==1&&f.StudyState.CounterBeforeParent==11&&f.StudyState.CallerWordAfter==6&&Find<Label>(f,"mixedNavigationSummary").Text.Contains("Patrol phase 2")&&Find<TextBox>(f,"mixedNavigationDetails").Text.Contains("before parent: 11"),"Source alias and child phase visible");Load(f,2);Click(f,"mixedNavigationOne");Need(f.StudyState.CounterBeforeParent==0&&f.StudyState.CallerWordAfter==6&&Find<Label>(f,"mixedNavigationSummary").Text.Contains("Patrol phase 3"),"Equal final6 must retain different leg");}
            });
            check("Mixed navigation form reset,end window,replans and source selection carry stay bounded",delegate{
                using(var f=new PracticeMixedNavigationStudy(programs,startup())){Show(f,owner);Load(f,4);Click(f,"mixedNavigationAll");Need(f.StudyState.Visits==8&&f.StudyState.TotalActivations==1&&f.StudyState.SelectedWaypoint==-1&&f.StudyState.UiRequests==0&&!Find<Button>(f,"mixedNavigationOne").Enabled&&!Find<Button>(f,"mixedNavigationAll").Enabled,"Bounded eightvisit selectioncarry");Find<ComboBox>(f,"mixedNavigationPreset").SelectedIndex=5;Click(f,"mixedNavigationReset");Need(f.StudyState.Preset==4&&f.StudyState.Visits==0&&Find<ComboBox>(f,"mixedNavigationPreset").SelectedIndex==5,"Reset applied copy retains pendingchoice");Click(f,"mixedNavigationLoad");Click(f,"mixedNavigationOne");Need(f.StudyState.Preset==5&&f.StudyState.NavigationDescription=="Replanned"&&f.StudyState.InitialCollisionAliasWrites==0,"Active terrainreplan no initialaliaswrite");Load(f,3);Click(f,"mixedNavigationOne");Need(f.StudyState.NavigationDescription=="Replanned"&&f.StudyState.CounterBeforeParent==0,"Active primaryreplan");}
            });
            check("Mixed navigation minimum scrolling keeps Close visible and owns its hit point",delegate{
                using(var f=new PracticeMixedNavigationStudy(programs,startup())){Show(f,owner);f.ClientSize=new Size(800,580);Application.DoEvents();var scroll=Find<Panel>(f,"mixedNavigationScroll");scroll.AutoScrollPosition=new Point(0,1000);Application.DoEvents();var close=Find<Button>(f,"mixedNavigationClose");var bounds=new Rectangle(f.PointToClient(close.PointToScreen(Point.Empty)),close.Size);Need(f.ClientRectangle.Contains(bounds)&&close.Visible&&!scroll.RectangleToScreen(scroll.ClientRectangle).IntersectsWith(close.RectangleToScreen(close.ClientRectangle)),"Minimum scroll must not cover footer");var hit=new Point(bounds.Left+bounds.Width/2,bounds.Top+bounds.Height/2);Need(f.GetChildAtPoint(hit)!=scroll&&Find<TextBox>(f,"mixedNavigationDetails").ReadOnly&&Find<TextBox>(f,"mixedNavigationDetails").Width>=700,"Readable details and Close hitownership");Need(owner.Controls.Find("openMixedNavigationStudy",true).Length==1,"Direct mapmenu entry");Click(f,"mixedNavigationClose");Need(f.IsDisposed||!f.Visible,"Clean close");}
            });
            check("Mixed navigation keyboard press edges suppress held Enter and Space",delegate{
                using(var f=new PracticeMixedNavigationStudy(programs,startup())){Show(f,owner);Load(f,1);Find<Button>(f,"mixedNavigationOne").Focus();Key(f,false,Keys.Enter);Key(f,true,Keys.Enter);Key(f,true,Keys.Enter);Need(f.StudyState.Visits==1,"HeldEnter repeatedvisit");Key(f,false,Keys.Enter);Key(f,true,Keys.Enter);Need(f.StudyState.Visits==2,"ReleasedEnter admitsonevisit");Key(f,false,Keys.Enter);Find<Button>(f,"mixedNavigationReset").Focus();Key(f,false,Keys.Space);Key(f,true,Keys.Space);var state=f.StudyState;Key(f,true,Keys.Space);Need(state.Visits==0&&object.ReferenceEquals(state,f.StudyState),"HeldSpace repeatedreset");Key(f,false,Keys.Space);}
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a fresh mixed navigation preview directory.");Directory.CreateDirectory(destination);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var startup=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var log=new StringBuilder();
            using(var f=new PracticeMixedNavigationStudy(programs,startup))
            {
                Show(f,null);Action<string> save=delegate(string name){Application.DoEvents();using(var bitmap=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bitmap,new Rectangle(Point.Empty,f.Size));bitmap.Save(Path.Combine(destination,name+".png"));}log.AppendLine(name+":profile="+f.StudyState.Preset+" visits="+f.StudyState.Visits+" parentword="+f.StudyState.CounterBeforeParent+" final="+f.StudyState.CallerWordAfter);};
                Load(f,1);save("01-Blocked-Prepared");Click(f,"mixedNavigationOne");save("02-Blocked-Activation-Phase2");Click(f,"mixedNavigationAll");save("03-Blocked-Window8");Load(f,2);Click(f,"mixedNavigationOne");save("04-Route-Activation-Phase3");Load(f,3);Click(f,"mixedNavigationOne");save("05-Active-Fleet-Replan");Load(f,4);Click(f,"mixedNavigationOne");save("06-Policy-First-Activation");f.ClientSize=new Size(800,580);Application.DoEvents();save("07-Minimum-Summary");Find<Panel>(f,"mixedNavigationScroll").AutoScrollPosition=new Point(0,1000);save("08-Minimum-Details-Close");f.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),log+"Eight actual-form software renders;source-backed fixed copied profiles/schematic map. No actual movement/airflight/naturalcaller/fullworld or physical input/display/audio acceptance.\r\n");return 0;
        }
    }
}
