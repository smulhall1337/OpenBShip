using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using FleetLab;
// Explicit near-defeat fixture in copied runtime state. Original assets stay intact.
// Natural production timers, no skips. No physical or audible acceptance claim.
class NaturalDefeatCheck {
 sealed class SilentOutput : IClipOutput {
  readonly WaveOutput output=new WaveOutput();public int Starts,Pauses,Resumes;
  public bool Active {get{return output.Active;}}public int Samples {get{return output.Samples;}}
  public void Start(short[] pcm,int offset){output.Start(new short[pcm.Length],offset);Starts++;}
  public void Pause(){output.Pause();Pauses++;}public void Resume(){output.Resume();Resumes++;}
  public void Stop(){output.Stop();}public void Dispose(){output.Dispose();}
 }
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static T Field<T>(object value,string name){return (T)value.GetType().GetField(name,Flags).GetValue(value);}
 static void Set(object value,string name,object field){value.GetType().GetField(name,Flags).SetValue(value,field);}
 static void Update(MainForm form){typeof(MainForm).GetMethod("UpdateView",Flags).Invoke(form,null);}
 static void Require(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Capture(Form form,string file){using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));bitmap.Save(file);}}
 [STAThread] static int Main(string[] args){
  if(args.Length!=4)return 2;string assets=Path.GetFullPath(args[0]),report=Path.GetFullPath(args[1]);bool high=args[2]=="high",silent=args[3]=="silent-device";Directory.CreateDirectory(report);var watch=Stopwatch.StartNew();var lines=new List<string>();
  try{
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   using(var form=new MainForm(assets)){
    Set(form,"previewMode",true);form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);
    DisplaySettings prefs=Field<DisplaySettings>(form,"preferences");prefs.Sound=false;prefs.MovieTurns=true;prefs.HighMovies=high;Field<Audio>(form,"sound").Enabled=false;
    var library=Field<ClipStudy>(form,"clip");var movie=Field<ClipStudy>(form,"battleMovie");library.SetSound(false);movie.SetSound(false);
    if(high)foreach(var clip in new[]{library,movie})Require((bool)typeof(ClipStudy).GetMethod("SetDetail",Flags).Invoke(clip,new object[]{true}),"High detail load");
    var device=new SilentOutput();if(silent){Field<IClipOutput>(movie,"output").Dispose();Set(movie,"output",device);movie.SetSound(true);}
    Set(form,"seed",731);typeof(MainForm).GetMethod("NewGame",Flags).Invoke(form,null);form.Show();Application.DoEvents();Field<Button>(form,"auto").PerformClick();Field<Button>(form,"start").PerformClick();
    Game game=form.State;Ship survivor=game.Player.Ships[4];Point target=survivor.Cells[survivor.Cells.Count-1];
    // Reachable board condition prepared directly for a bounded terminal experiment.
    for(int y=0;y<10;y++)for(int x=0;x<10;x++)if(new Point(x,y)!=target)game.Player.Shoot(new Point(x,y));
    Require(!game.Player.AllSunk&&survivor.Hits==survivor.Cells.Count-1,"Fixture must leave exactly one HP");game.Phase=Phase.OpponentTurn;Require(game.AdvanceOpponent()&&game.Pending.Cell==target,"Original target selector must choose the sole remaining cell");Update(form);
    var board=Field<BoardView>(form,"board");int stage=-1,stages=0,frozen=-1;long resumeAt=0;bool checkedPause=false,sawSink=false;long stageStart=0;
    while(game.Phase!=Phase.Finished){
     Application.DoEvents();if(game.Phase==Phase.Finished)break;Require(movie.Problem=="","Media/audio fallback: "+movie.Problem);var timeline=(ClipTimeline)typeof(ClipStudy).GetProperty("Timeline",Flags).GetValue(movie,null);var pending=game.Pending;Require(pending!=null,"Defeat lost pending stage");
     if((int)pending.Stage!=stage){stage=(int)pending.Stage;stages++;stageStart=watch.ElapsedMilliseconds;Require(Field<string>(movie,"clipName")==pending.MovieName,"Stale movie identity");lines.Add("Stage="+pending.StageLabel+", clip="+pending.MovieName+", frames="+timeline.Count);}
     if(pending.Stage==ShotStage.Feedback){Require(game.Player.AllSunk&&pending.Result.Sunk&&game.Phase==Phase.OpponentTurn,"Defeat must wait for the sinking movie");sawSink=true;
      if(!checkedPause&&resumeAt==0&&timeline.Frame>=10){Field<Button>(form,"pause").PerformClick();frozen=timeline.Frame;resumeAt=watch.ElapsedMilliseconds+650;Capture(form,Path.Combine(report,"Paused-Sinking.png"));}
     }
     if(resumeAt>0){Require(game.Paused&&timeline.Frame==frozen&&game.Player.AllSunk,"Paused sink state changed");if(watch.ElapsedMilliseconds>=resumeAt){Field<Button>(form,"pause").PerformClick();resumeAt=0;checkedPause=true;}}
     Require(watch.ElapsedMilliseconds-stageStart<20000&&watch.ElapsedMilliseconds<90000,"Natural terminal scenario timed out");System.Threading.Thread.Sleep(8);
    }
    Require(stages==3&&sawSink&&checkedPause&&game.Player.AllSunk&&!game.Enemy.AllSunk&&game.Pending==null&&board.MovieFrame==null&&!movie.Visible&&!Field<IClipOutput>(movie,"output").Active,"Terminal defeat/restoration failed");Capture(form,Path.Combine(report,"Defeat.png"));
    Field<HashSet<Keys>>(form,"down").Remove(Keys.F2);typeof(MainForm).GetMethod("OnKey",Flags).Invoke(form,new object[]{form,new KeyEventArgs(Keys.F2)});Require(form.State.Phase==Phase.Placement&&form.State.Pending==null&&board.MovieFrame==null&&!Field<IClipOutput>(movie,"output").Active,"F2 retained terminal media/state");
    Require(!silent||(device.Starts==3&&device.Pauses==1&&device.Resumes==1),"Actual silent device call totals");lines.Add("PASS natural defeat after final sinking, pause/resume, opaque restoration and F2 reset. Detail="+(high?"320x240":"160x120")+", audio="+(silent?"actual SILENT waveOut":"muted")+", stages="+stages+", seconds="+(watch.ElapsedMilliseconds/1000)+", buffers="+device.Starts+".");form.Close();
   }
   lines.Add("Fixture prepared near-terminal board state directly; this is not a complete played defeat or original-gameplay acceptance. No stages skipped; no physical or audible claim.");File.WriteAllLines(Path.Combine(report,"Result.txt"),lines.ToArray());return 0;
  }catch(Exception e){lines.Add("FAIL "+e);File.WriteAllLines(Path.Combine(report,"Result.txt"),lines.ToArray());return 1;}
 }
}

