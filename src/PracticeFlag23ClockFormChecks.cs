using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace FleetLab
{
    internal static class PracticeFlag23ClockFormChecks
    {
        static void Need(bool v,string why){if(!v)throw new InvalidDataException(why);}
        static T Find<T>(Control f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        static void Load(PracticeFlag23ClockStudy f,int p){Find<ComboBox>(f,"clockPolicyPreset").SelectedIndex=p;Find<Button>(f,"clockPolicyLoad").PerformClick();}
        static void Step(PracticeFlag23ClockStudy f){Find<Button>(f,"clockPolicyOne").PerformClick();}
        static void Key(Form f,bool down,Keys key){typeof(Control).GetMethod(down?"OnKeyDown":"OnKeyUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,new object[]{new KeyEventArgs(key)});}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var raw=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,raw,731);};
            check("Clock policy form keeps presets pending and shows source before body after clock order",delegate{
                using(var f=new PracticeFlag23ClockStudy(programs,startup()))
                {
                    Show(f,owner);Need(f.StudyState.Preset==0&&f.StudyState.Frames==0&&Find<Button>(f,"clockPolicyOne").Enabled,"Prepared copy");
                    Find<ComboBox>(f,"clockPolicyPreset").SelectedIndex=3;Need(f.StudyState.Preset==0&&Find<Label>(f,"clockPolicyNext").Text.Contains("Pending"),"Selection must stay pending");
                    Find<Button>(f,"clockPolicyLoad").PerformClick();Step(f);Need(f.StudyState.Frames==1&&f.StudyState.Timer==4984&&f.StudyState.Kind==6&&Find<Label>(f,"clockPolicyClock3_1").Text=="0"&&Find<Label>(f,"clockPolicyClock3_2").Text=="16","Body sees previous0delta;header later computes16");
                }
            });
            check("Clock policy form exact threshold retains invalidated selection and bounded reset behavior",delegate{
                using(var f=new PracticeFlag23ClockStudy(programs,startup()))
                {
                    Show(f,owner);Load(f,3);Step(f);Step(f);Need(f.StudyState.Kind==3&&f.StudyState.Timer==5000&&f.StudyState.UiRequests==1&&f.StudyState.State.SelectedWaypoint==-1,"Exact activation frame");
                    Find<ComboBox>(f,"clockPolicyPreset").SelectedIndex=2;Step(f);Need(!f.StudyState.CanAdvance&&f.StudyState.Frames==3&&f.StudyState.State.SelectedWaypoint==-1&&f.StudyState.UiRequests==1&&!Find<Button>(f,"clockPolicyOne").Enabled&&!Find<Button>(f,"clockPolicyAll").Enabled,"Carryselection/endlimit/pendinginput");
                    Find<Button>(f,"clockPolicyReset").PerformClick();Need(f.StudyState.Preset==3&&f.StudyState.Frames==0&&Find<ComboBox>(f,"clockPolicyPreset").SelectedIndex==2,"Reset applied copy keeps pending choice");
                    Find<Button>(f,"clockPolicyLoad").PerformClick();Find<Button>(f,"clockPolicyAll").PerformClick();Need(f.StudyState.Preset==2&&f.StudyState.Frames==4&&f.StudyState.Timer==1021,"Wrapping source frame window");
                }
            });
            check("Clock policy form minimum scroll close and map menu entry preserve copied state",delegate{
                using(var f=new PracticeFlag23ClockStudy(programs,startup()))
                {
                    Show(f,owner);f.ClientSize=new Size(800,580);Application.DoEvents();var scroll=Find<Panel>(f,"clockPolicyScroll");scroll.AutoScrollPosition=new Point(0,400);Application.DoEvents();var close=Find<Button>(f,"clockPolicyClose");var visibleBounds=new Rectangle(f.PointToClient(close.PointToScreen(Point.Empty)),close.Size);Need(Find<TextBox>(f,"clockPolicyDetails").ReadOnly&&f.StudyState.Frames==0&&close.Visible&&f.ClientRectangle.Contains(visibleBounds),"Readable copied view and fully visible close without automatic advancement");
                    Need(f.GetChildAtPoint(new Point(visibleBounds.Left+visibleBounds.Width/2,visibleBounds.Top+visibleBounds.Height/2))==close.Parent&&!scroll.Bounds.IntersectsWith(visibleBounds),"Footer must own the close hit point without an overlapping scroll panel");
                    Need(owner.Controls.Find("openClockPolicyStudy",true).Length==1,"Direct recovered maps entry");Find<Button>(f,"clockPolicyClose").PerformClick();Application.DoEvents();Need(f.IsDisposed||!f.Visible,"Close must complete");
                }
            });
            check("Clock policy form keyboard edges suppress held confirmation until release",delegate{
                using(var f=new PracticeFlag23ClockStudy(programs,startup()))
                {
                    Show(f,owner);Load(f,3);var button=Find<Button>(f,"clockPolicyOne");button.Focus();Key(f,false,Keys.Enter);Key(f,true,Keys.Enter);Key(f,true,Keys.Enter);Need(f.StudyState.Frames==1,"Held Enter repeated a source frame");Key(f,false,Keys.Enter);Key(f,true,Keys.Enter);Need(f.StudyState.Frames==2&&f.StudyState.Activations==1,"Released Enter must admit the next frame once");Key(f,false,Keys.Enter);
                    Find<Button>(f,"clockPolicyReset").Focus();Key(f,false,Keys.Space);Key(f,true,Keys.Space);var copy=f.StudyState;Key(f,true,Keys.Space);Need(f.StudyState.Frames==0&&object.ReferenceEquals(copy,f.StudyState),"Held Space reset repeatedly");Key(f,false,Keys.Space);
                }
            });
        }
        internal static int SavePreviews(string assets,string output)
        {
            if(Directory.Exists(output)||File.Exists(output))throw new IOException("Choose a fresh clock policy preview directory.");Directory.CreateDirectory(output);
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var startup=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var log=new StringBuilder();
            using(var owner=new Form())using(var f=new PracticeFlag23ClockStudy(programs,startup))
            {
                owner.ShowInTaskbar=false;owner.StartPosition=FormStartPosition.Manual;owner.Location=new Point(-22000,-22000);owner.Show();Show(f,owner);
                Action<string> save=delegate(string name){Application.DoEvents();using(var b=new Bitmap(f.ClientSize.Width,f.ClientSize.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.ClientSize));b.Save(Path.Combine(output,name+".png"));}log.AppendLine(name+": "+f.ClientSize+" frames="+f.StudyState.Frames+" timer="+f.StudyState.Timer+" kind="+f.StudyState.Kind+" selected="+f.StudyState.State.SelectedWaypoint);};
                save("01-Prepared");Find<Button>(f,"clockPolicyAll").PerformClick();save("02-Variable-Final");Load(f,3);save("03-Threshold-Prepared");Step(f);Step(f);save("04-Exact-5000");Step(f);save("05-Selection-Carried");Load(f,1);Step(f);Step(f);save("06-Zero-Reset");Load(f,2);Find<Button>(f,"clockPolicyAll").PerformClick();save("07-Wrapping");Load(f,3);Step(f);Step(f);f.ClientSize=new Size(800,580);Application.DoEvents();Find<Panel>(f,"clockPolicyScroll").AutoScrollPosition=new Point(0,400);save("08-Minimum-Details");f.Close();owner.Close();
            }
            File.WriteAllText(Path.Combine(output,"Preview.txt"),log.ToString()+"Actual copied study controls/rendering,not physical display/input or fullworld acceptance. No timer/media/settings changes.\r\n");return 0;
        }
    }
}
