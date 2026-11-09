using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using FleetLab;
// Plays all61combat clips through the production study player without skipping.
// Separate from complete-duel coverage and from audible/physical acceptance.
class NaturalEndingCheck {
 sealed class SilentOutput : IClipOutput {
  readonly WaveOutput output=new WaveOutput();public int Starts,Pauses,Resumes;
  public bool Active {get{return output.Active;}}public int Samples {get{return output.Samples;}}
  public void Start(short[] pcm,int offset){output.Start(new short[pcm.Length],offset);Starts++;}
  public void Pause(){output.Pause();Pauses++;}public void Resume(){output.Resume();Resumes++;}public void Stop(){output.Stop();}public void Dispose(){output.Dispose();}
 }
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static T Field<T>(object value,string name){return (T)value.GetType().GetField(name,Flags).GetValue(value);}
 static ClipTimeline Timeline(ClipStudy clip){return (ClipTimeline)typeof(ClipStudy).GetProperty("Timeline",Flags).GetValue(clip,null);}
 static void Require(bool ok,string text){if(!ok)throw new Exception(text);}
 [STAThread] static int Main(string[] args){
  if(args.Length!=4)return 2;string assets=Path.GetFullPath(args[0]),report=Path.GetFullPath(args[1]);bool high=args[2]=="high",silent=args[3]=="silent-device";Directory.CreateDirectory(report);var rows=new List<string>{"Clip,Frames,ElapsedMs,ExpectedMovieMs,PauseMs,CompletionCount"};var total=Stopwatch.StartNew();
  try{
   var expected=new Dictionary<string,int>();foreach(string line in File.ReadAllLines(Path.Combine(Path.GetDirectoryName(assets),"Evidence","Ending-Inventory.csv"))){string[] p=line.Replace("\"","").Split(',');if(p[0]!="Clip")expected.Add(p[0],int.Parse(p[1]));}
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   using(var form=new Form())using(var clip=new ClipStudy(assets,high)){
    form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Size=new Size(900,680);form.Controls.Add(clip);
    var device=new SilentOutput();if(silent){Field<IClipOutput>(clip,"output").Dispose();typeof(ClipStudy).GetField("output",Flags).SetValue(clip,device);}clip.SetSound(silent);
    int completed=0;Action completion=delegate{completed++;};typeof(ClipStudy).GetEvent("PlaybackCompleted",Flags).GetAddMethod(true).Invoke(clip,new object[]{completion});form.Show();Application.DoEvents();
    var selector=Field<ComboBox>(clip,"Selector");int frames=0;
    for(int i=0;i<MovieCatalog.Endings.Length;i++){
     string name=MovieCatalog.Endings[i];selector.SelectedItem=name;ClipTimeline timeline=Timeline(clip);Require(clip.Problem==""&&timeline.Count==expected[name]&&Field<string>(clip,"clipName")==name,"Library identity/load: "+name);
     var watch=Stopwatch.StartNew();bool paused=false;long resumeAt=0;int frozen=-1;Field<Button>(clip,"PlayButton").PerformClick();
     while(!timeline.Completed){
      Application.DoEvents();Require((int)typeof(ClipStudy).GetProperty("CachedBitmapCount",Flags).GetValue(clip,null)==1,"Bitmap cache grew");Require(clip.Problem=="","Playback fallback: "+name+"/"+clip.Problem);
      if(name=="VICTORY"&&!paused&&resumeAt==0&&timeline.Frame>=10){clip.Pause();frozen=timeline.Frame;resumeAt=watch.ElapsedMilliseconds+650;}
      if(resumeAt>0){Require(timeline.Frame==frozen&&!timeline.Playing,"Library pause advanced: "+name);if(watch.ElapsedMilliseconds>=resumeAt){Field<Button>(clip,"PlayButton").PerformClick();paused=true;resumeAt=0;}}
      Require(watch.ElapsedMilliseconds<60000,"Library timeout: "+name);System.Threading.Thread.Sleep(8);
     }
     long pauseMs=paused?650:0;Require(completed==i+1&&!Field<IClipOutput>(clip,"output").Active&&watch.ElapsedMilliseconds>=timeline.Count*50+pauseMs-100,"Finite completion/count/duration: "+name);frames+=timeline.Count;
     rows.Add(name+","+timeline.Count+","+watch.ElapsedMilliseconds+","+(timeline.Count*50)+","+pauseMs+","+completed);File.WriteAllLines(Path.Combine(report,"Clips.csv"),rows.ToArray());File.WriteAllText(Path.Combine(report,"Progress.txt"),"CompletedEndings="+(i+1)+"/2\nLast="+name+"\nElapsedSeconds="+(total.ElapsedMilliseconds/1000));
    }
    Require(completed==2&&frames==1341&&(!silent||(device.Starts==2&&device.Pauses==1&&device.Resumes==1)),"Library total/device calls");clip.Reset();Require(!Timeline(clip).Completed&&Timeline(clip).Frame==0&&!Field<IClipOutput>(clip,"output").Active,"Library reset");form.Close();
    File.WriteAllLines(Path.Combine(report,"Result.txt"),new[]{"PASS natural production library playback for2endings/1341frames. Native320x240; combat quality preference="+(high?"high":"low")+", audio="+(silent?"actual SILENT waveOut":"muted")+", buffers="+device.Starts+", elapsed seconds="+(total.ElapsedMilliseconds/1000)+".","One pause/resume in VICTORY;2once-only completions, natural finite endings and reset. No skip/step calls. Timing check allows100msdevice/sampling tolerance, not an audible-sync claim.","Offscreen standalone library, not2battles. Source/native inventories bound frame counts; all original media unchanged. Physical input and audible/perceived sync remain separate checks."});
   }
   return 0;
  }catch(Exception e){File.WriteAllText(Path.Combine(report,"Result.txt"),"FAIL "+e);return 1;}
 }
}

