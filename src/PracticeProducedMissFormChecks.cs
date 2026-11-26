using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeProducedMissFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var boundary=PracticeProducedMissChecks.Boundary(assets,0);
            check("Natural water form advances both effects completes and resets",delegate
            {
                using(var f=new PracticeProducedMissStudy(programs,boundary)){Show(f,owner);Find<Button>(f,"producedMissTransition").PerformClick();Need(f.StudyState.Clock==35211&&f.StudyState.Projectiles.Count==1&&f.StudyState.Effects.Count==1&&Find<ListBox>(f,"producedMissEffects").Items.Count==1,"First source22 effect");Find<Button>(f,"producedMissTransition").PerformClick();Need(f.StudyState.Clock==35291&&f.StudyState.Effects.Count==2,"Second source22 effect");Find<Button>(f,"producedMissFinish").PerformClick();Need(f.StudyState.Complete&&f.StudyState.Clock==36571&&!Find<Button>(f,"producedMissStep").Enabled,"Complete source22 lifetime");Find<Button>(f,"producedMissReset").PerformClick();Need(f.StudyState.Clock==35210&&f.StudyState.Updates==0&&Find<Button>(f,"producedMissStep").Enabled,"Reset source22 boundary");f.Close();}
            });
            check("Natural water form holds pending selection and shows whole-pass blocking diagnostic",delegate
            {
                using(var f=new PracticeProducedMissStudy(programs,boundary)){Show(f,owner);Find<ComboBox>(f,"producedMissMode").SelectedIndex=2;Find<Button>(f,"producedMissReset").PerformClick();Need(f.StudyState.Mode==0&&Find<ComboBox>(f,"producedMissMode").SelectedIndex==2,"Pending selector changed applied mode");Find<Button>(f,"producedMissLoad").PerformClick();Find<Button>(f,"producedMissStep").PerformClick();Need(f.StudyState.Clock==35250&&f.StudyState.Projectiles.Count==2&&f.StudyState.Effects.Count==0,"Original whole-pass blocking");Find<Button>(f,"producedMissStep").PerformClick();Need(f.StudyState.Clock==35290&&f.StudyState.Projectiles[0].Slot==1&&f.StudyState.Effects.Count==1,"Blocked later record order");Find<Button>(f,"producedMissFinish").PerformClick();Need(f.StudyState.Clock==36610&&f.StudyState.Complete,"Diagnostic effect end");f.Close();}
            });
            check("Natural water form suppresses held confirmation scrolls and rejects unfinished flight",delegate
            {
                using(var f=new PracticeProducedMissStudy(programs,boundary)){Show(f,owner);f.ActiveControl=Find<Button>(f,"producedMissStep");var down=typeof(PracticeProducedMissStudy).GetMethod("OnKeyDown",BindingFlags.NonPublic|BindingFlags.Instance);down.Invoke(f,new object[]{new KeyEventArgs(Keys.Enter)});var first=f.StudyState;down.Invoke(f,new object[]{new KeyEventArgs(Keys.Enter)});Need(first.Updates==1&&Object.ReferenceEquals(first,f.StudyState),"Held source22 water update");f.Size=f.MinimumSize;Application.DoEvents();Need(Find<Panel>(f,"producedMissScroll").AutoScroll&&Find<Control>(f,"producedMissCanvas").Width>500,"Minimum water layout");f.Close();}
                using(var bad=new PracticeProducedMissStudy(programs,PracticeProducedFlight.Create(boundary.Launch))){Show(bad,owner);Need(bad.StudyState==null&&!Find<Button>(bad,"producedMissStep").Enabled,"Unfinished flight entered water study");bad.Close();}
            });
            check("Flight water child requires completed boundary and preserves parent",delegate
            {
                using(var parent=new PracticeProducedFlightStudy(boundary.Launch))
                {
                    Show(parent,owner);var button=Find<Button>(parent,"producedFlightMisses");Need(!button.Enabled,"Early water child enabled");Find<Button>(parent,"producedFlightBoundary").PerformClick();Need(button.Enabled,"Boundary child disabled");var before=parent.StudyState;bool saw=false;Exception error=null;int ticks=0;
                    using(var timer=new Timer{Interval=100}){timer.Tick+=delegate{if(++ticks>30){timer.Stop();error=new Exception("Water child missing");parent.Close();return;}foreach(Form c in parent.OwnedForms){var f=c as PracticeProducedMissStudy;if(f==null)continue;timer.Stop();try{Find<Button>(f,"producedMissFinish").PerformClick();Need(f.StudyState.Complete,"Child water lifetime");saw=true;}catch(Exception e){error=e;}finally{f.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();}
                    if(error!=null)throw error;Need(saw&&Object.ReferenceEquals(before,parent.StudyState)&&parent.StudyState.Clock==35210,"Water child changed parent");Find<Button>(parent,"producedFlightReset").PerformClick();Need(!button.Enabled,"Reset retained water gate");parent.Close();
                }
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new water22 preview directory.");Directory.CreateDirectory(destination);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var boundary=PracticeProducedMissChecks.Boundary(assets,0);
            Action<Form,string> capture=delegate(Form f,string name){f.PerformLayout();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
            using(var f=new PracticeProducedFlightStudy(boundary.Launch)){Show(f,null);Find<Button>(f,"producedFlightBoundary").PerformClick();Find<Panel>(f,"producedFlightScroll").ScrollControlIntoView(Find<Button>(f,"producedFlightMisses"));capture(f,"01-Boundary-Entry");f.Close();}
            using(var f=new PracticeProducedMissStudy(programs,boundary)){Show(f,null);Find<Button>(f,"producedMissTransition").PerformClick();capture(f,"02-First-Water");Find<Button>(f,"producedMissTransition").PerformClick();capture(f,"03-Two-Effects");Find<Button>(f,"producedMissFinish").PerformClick();capture(f,"04-Complete");Find<ComboBox>(f,"producedMissMode").SelectedIndex=2;Find<Button>(f,"producedMissLoad").PerformClick();Find<Button>(f,"producedMissStep").PerformClick();capture(f,"05-Blocked-Second-Record");f.Size=f.MinimumSize;capture(f,"06-Minimum-Controls");Find<Panel>(f,"producedMissScroll").ScrollControlIntoView(Find<TextBox>(f,"producedMissDetails"));capture(f,"07-Minimum-Details");f.Close();}
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Seven actual-form weapon22 water/effect previews. Original numeric counters; blue rings are schematic. Both cells water; no damage/RNG draws/media/audio/fullAI/world or physical acceptance.\r\n");return 0;
        }
    }
}
