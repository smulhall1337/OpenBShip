using System;using System.IO;using System.Drawing;using System.Windows.Forms;using System.Reflection;using System.Diagnostics;using System.Collections.Generic;using FleetLab;
// Actual production timers, no skipped stages. SILENT audio is lifecycle evidence only.
class NaturalResultCheck {
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static T Field<T>(object o,string n){return (T)o.GetType().GetField(n,Flags).GetValue(o);}
 static T Prop<T>(object o,string n){return (T)o.GetType().GetProperty(n,Flags).GetValue(o,null);}
 static void Require(bool b,string s){if(!b)throw new Exception(s);}
 sealed class Silent:IClipOutput {readonly WaveOutput wave=new WaveOutput();public int Starts,Pauses,Resumes;public bool Active{get{return wave.Active;}}public int Samples{get{return wave.Samples;}}public void Start(short[] p,int at){wave.Start(new short[p.Length],at);Starts++;}public void Pause(){wave.Pause();Pauses++;}public void Resume(){wave.Resume();Resumes++;}public void Stop(){wave.Stop();}public void Dispose(){wave.Dispose();}}
 [STAThread]static int Main(string[] args){if(args.Length!=3)return 2;string assets=Path.GetFullPath(args[0]),dest=Path.GetFullPath(args[1]);bool silent=args[2]=="silent-device";Directory.CreateDirectory(dest);var rows=new List<string>{"Outcome,Phase,ElapsedMs,Factor"};var total=Stopwatch.StartNew();try{
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  using(var form=new Form())using(var player=new EndingPlayer(assets)){
   form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.ClientSize=new Size(900,700);form.Controls.Add(player);
   ClipStudy movie=Field<ClipStudy>(player,"Movie");var device=new Silent();if(silent){Field<IClipOutput>(movie,"output").Dispose();typeof(ClipStudy).GetField("output",Flags).SetValue(movie,device);}player.SetSound(silent);form.Show();Application.DoEvents();
   foreach(bool victory in new[]{true,false}){
    string name=victory?"VICTORY":"LOSE";player.StartOutcome(victory);var timeline=Prop<EndingTimeline>(player,"Timeline");var watch=Stopwatch.StartNew();EndingPhase previous=EndingPhase.Idle;long resumeAt=0;bool held=false,pausedMovie=false;int hold=0,frame=0;var phases=new List<EndingPhase>();
    while(timeline.Phase!=EndingPhase.Complete){
     Application.DoEvents();Require(player.Problem==""&&movie.Problem=="","Visible fallback: "+player.Problem+movie.Problem);
     if(previous!=timeline.Phase){previous=timeline.Phase;phases.Add(previous);rows.Add(name+","+previous+","+watch.ElapsedMilliseconds+","+timeline.Factor);using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));image.Save(Path.Combine(dest,name+"-"+previous+".png"));}File.WriteAllLines(Path.Combine(dest,"Phases.csv"),rows.ToArray());}
     if(resumeAt>0){Require(!timeline.Playing&&timeline.RemainingHold==hold&&(Prop<ClipTimeline>(movie,"Timeline")==null?frame==-1:Prop<ClipTimeline>(movie,"Timeline").Frame==frame),"Paused result advanced");if(watch.ElapsedMilliseconds>=resumeAt){Field<Button>(player,"PlayButton").PerformClick();resumeAt=0;}}
     else if((timeline.Phase==EndingPhase.Hold&&!held&&timeline.RemainingHold<4700)||(timeline.Phase==EndingPhase.Movie&&!pausedMovie&&Prop<ClipTimeline>(movie,"Timeline").Frame>=10)){
      if(timeline.Phase==EndingPhase.Hold)held=true;else pausedMovie=true;player.Pause();hold=timeline.RemainingHold;frame=Prop<ClipTimeline>(movie,"Timeline")==null?-1:Prop<ClipTimeline>(movie,"Timeline").Frame;resumeAt=watch.ElapsedMilliseconds+350;
     }
     Require(watch.ElapsedMilliseconds<55000,"Result timeout");System.Threading.Thread.Sleep(8);
    }
    if(phases[phases.Count-1]!=EndingPhase.Complete)phases.Add(EndingPhase.Complete);Require(phases.Count==6&&phases[0]==EndingPhase.Reveal&&phases[1]==EndingPhase.Hold&&phases[2]==EndingPhase.CardFade&&phases[3]==EndingPhase.Movie&&phases[4]==EndingPhase.MovieFade,"Result phase order");
    Require(timeline.Trace.Count==54&&!timeline.Playing&&Prop<Bitmap>(player,"CurrentImage")==null&&!Prop<bool>(movie,"AudioActive")&&held&&pausedMovie,"Final clear, trace or lifecycle");
    Require(watch.ElapsedMilliseconds>=(victory?33000:33950)+6240+680+700-100,"Sequence ended early");
    int count=timeline.Trace.Count;for(int i=0;i<20;i++){Application.DoEvents();System.Threading.Thread.Sleep(8);}Require(timeline.Trace.Count==count,"Completion repeated");
    File.WriteAllText(Path.Combine(dest,name+"-Result.txt"),"PASS natural result "+name+"; elapsedMs="+watch.ElapsedMilliseconds+"; 54 native-prefix trace events; two350ms pauses; once-only completion and final clear.");player.Cancel();Require(Prop<EndingTimeline>(player,"Timeline")==null&&!Prop<bool>(movie,"AudioActive"),"Cancel retained audio/sequence");
   }
   Require(!silent||(device.Starts==2&&device.Pauses==2&&device.Resumes==2),"Silent device calls");form.Close();
   File.WriteAllText(Path.Combine(dest,"Result.txt"),"PASS two natural result sequences; audio="+(silent?"actual SILENT waveOut":"muted")+"; elapsedSeconds="+total.ElapsedMilliseconds/1000+". No skip, seek or accelerated time. Source-confirmed prefix order;40ms fade cadence is modern. Offscreen, not audible/physical acceptance or native menu execution.");
  }return 0;
 }catch(Exception e){File.WriteAllText(Path.Combine(dest,"Result.txt"),"FAIL "+e);return 1;}}
}

