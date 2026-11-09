using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FleetLab;
// Explicit diagnostic: offscreen scripted targets, production timers at 25 fps,
// muted. No physical-input, audible-output or original-gameplay acceptance claim.
class NaturalSoloDuelCheck {
 sealed class SilentDeviceOutput : IClipOutput {
  public static int Starts,Pauses,Resumes;
  readonly WaveOutput output=new WaveOutput();public bool Active {get{return output.Active;}}public int Samples {get{return output.Samples;}}
  public void Start(short[] pcm,int offset){output.Start(new short[pcm.Length],offset);Starts++;}public void Pause(){output.Pause();Pauses++;}public void Resume(){output.Resume();Resumes++;}public void Stop(){output.Stop();}public void Dispose(){output.Dispose();}
 }
 const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 [DllImport("user32.dll")] static extern int GetGuiResources(IntPtr process,uint flags);
 static T Field<T>(object owner,string name){return (T)owner.GetType().GetField(name,Fields).GetValue(owner);}
 static void Set(object owner,string name,object value){owner.GetType().GetField(name,Fields).SetValue(owner,value);}
 static void Update(MainForm form){typeof(MainForm).GetMethod("UpdateView",Fields).Invoke(form,null);}
 static int Hits(Board board){int value=0;foreach(Ship ship in board.Ships)value+=ship.Hits;return value;}
 static void Screenshot(Form form,string file){using(var bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,new Rectangle(0,0,form.Width,form.Height));bmp.Save(file);}}
 [STAThread] static int Main(string[] args){
  if(args.Length<2||args.Length>5){Console.Error.WriteLine("NaturalCinematicCheck <assets> <report directory> [level 0..3] [low|high] [muted|silent-device]");return 2;}
  string destination=Path.GetFullPath(args[1]);Directory.CreateDirectory(destination);var lines=new List<string>{"ElapsedMs,Turn,Side,Stage,Clip,Frames,PlayerHits,EnemyHits,GDIHandles,PrivateBytes"};var watch=Stopwatch.StartNew();int level=args.Length>=3?int.Parse(args[2]):0;bool high=args.Length>=4&&args[3]=="high",silent=args.Length>=5&&args[4]=="silent-device";
  var messageRows=new List<string>{"Stage,Side,Lines,TextBase64"};
  var expectedFrames=new Dictionary<string,int>();
  int stages=0,shots=0,maxGdi=0;bool pauseChecked=false,hitPreview=false,sinkPreview=false;PendingShot last=null;ShotStage lastStage=ShotStage.Firing;long stageStart=0,lastProgress=0,pauseUntil=0;int frozenFrame=-1,frozenPlayer=0,frozenEnemy=0;
  try{
   foreach(string line in File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0])),"Evidence",high?"HWEBS-Inventory.csv":"Impact-Clip-Inventory.csv"))){string[] fields=line.Replace("\"","").Split(',');if(fields[0]=="Clip")continue;expectedFrames.Add(fields[0],int.Parse(fields[high?1:2]));}
   if(level<0||level>3)throw new ArgumentOutOfRangeException("level");
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   using(var form=new MainForm(Path.GetFullPath(args[0])))using(var process=Process.GetCurrentProcess()){
    Set(form,"previewMode",true);form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);
    DisplaySettings prefs=Field<DisplaySettings>(form,"preferences");prefs.MovieTurns=true;prefs.Sound=false;Field<Audio>(form,"sound").Enabled=false;
    ClipStudy library=Field<ClipStudy>(form,"clip"),movie=Field<ClipStudy>(form,"battleMovie");library.SetSound(false);movie.SetSound(false);Field<ComboBox>(form,"opponent").SelectedIndex=level;
    if(high){var setting=typeof(DisplaySettings).GetField("HighMovies");if(setting==null)throw new Exception("High detail unavailable in this build");setting.SetValue(prefs,true);foreach(ClipStudy player in new[]{library,movie})if(!(bool)typeof(ClipStudy).GetMethod("SetDetail",Fields).Invoke(player,new object[]{true}))throw new Exception("High detail failed to load");}
    if(silent){Field<IClipOutput>(movie,"output").Dispose();Set(movie,"output",new SilentDeviceOutput());movie.SetSound(true);}
    Set(form,"seed",731);typeof(MainForm).GetMethod("NewGame",Fields).Invoke(form,null);form.Show();Application.DoEvents();Field<TabControl>(form,"tabs").SelectedIndex=4;object front=Field<object>(form,"front"),solo=Field<object>(form,"solo");front.GetType().GetMethod("FinishReveal",Fields).Invoke(front,null);front.GetType().GetMethod("Activate",Fields).Invoke(front,new object[]{0});solo.GetType().GetMethod("FinishReveal",Fields).Invoke(solo,null);solo.GetType().GetMethod("Activate",Fields).Invoke(solo,new object[]{6});Set(form,"seed",731);solo.GetType().GetMethod("Activate",Fields).Invoke(solo,new object[]{10});object fleet=Field<object>(form,"fleet");fleet.GetType().GetMethod("Activate",Fields).Invoke(fleet,new object[]{2});var entryWait=Stopwatch.StartNew();while(Field<TabControl>(form,"tabs").SelectedIndex==4&&entryWait.ElapsedMilliseconds<2500){Application.DoEvents();System.Threading.Thread.Sleep(2);}if(form.State.Phase!=Phase.PlayerTurn||!form.State.Rules.FixedY||Field<TabControl>(form,"tabs").SelectedIndex!=0)throw new Exception("Solo Classic Fleet bridge failed");
    BoardView board=Field<BoardView>(form,"board");
    while(form.State.Phase!=Phase.Finished){
     Application.DoEvents();var timeline=(ClipTimeline)typeof(ClipStudy).GetProperty("Timeline",Fields).GetValue(movie,null);Game game=form.State;
     if(silent&&movie.Problem!="")throw new Exception("Silent device test encountered a fallback: "+movie.Problem);
     if(pauseUntil>0){
      if(timeline.Frame!=frozenFrame||Hits(game.Player)!=frozenPlayer||Hits(game.Enemy)!=frozenEnemy)throw new Exception("Paused frame or damage changed");
      if(watch.ElapsedMilliseconds>=pauseUntil){Field<Button>(form,"pause").PerformClick();pauseUntil=0;pauseChecked=true;}
     }else if(game.Paused)throw new Exception("Unexpected pause: "+game.Message);
     if(game.Phase==Phase.PlayerTurn&&game.Pending==null){
      Point? target=null;foreach(Ship ship in game.Enemy.Ships)foreach(Point cell in ship.Cells)if(!game.Enemy.Shots[cell.X,cell.Y]&&!target.HasValue)target=cell;
      if(!target.HasValue)throw new Exception("No scripted target before victory");board.KeyboardCell=target.Value;board.Act();shots++;timeline=(ClipTimeline)typeof(ClipStudy).GetProperty("Timeline",Fields).GetValue(movie,null);
     }
     PendingShot pending=game.Pending;
     if(pending!=null&&(!object.ReferenceEquals(last,pending)||lastStage!=pending.Stage)){
      if(timeline.Count!=expectedFrames[pending.MovieName]||Field<string>(movie,"clipName")!=pending.MovieName)throw new Exception("Stage metadata is not bound to its current clip");last=pending;lastStage=pending.Stage;stageStart=watch.ElapsedMilliseconds;stages++;process.Refresh();int gdi=GetGuiResources(process.Handle,0);maxGdi=Math.Max(maxGdi,gdi);if(gdi>1600)throw new Exception("Unexpected GDI resource growth: "+gdi);
            var textLines=(List<MessageLine>)typeof(BoardView).GetProperty("CurrentMessages",Fields).GetValue(board,null);string message="";foreach(var item in textLines)message+=(message.Length==0?"":" ")+item.Text;
      bool show=pending.Stage!=ShotStage.Feedback||pending.Result.Sunk;
      if(show&&(textLines.Count==0||!message.StartsWith(pending.Stage==ShotStage.Feedback?(pending.Opponent?"The Computer Sunk Your":"You Sunk The Computer"):pending.Opponent?"The Computer Is Firing":"Your ",StringComparison.Ordinal)))throw new Exception("Current battle message is not bound to its shot stage: "+message);
      if(!show&&textLines.Count!=0)throw new Exception("Hit/miss text replaced native icon-only feedback");messageRows.Add(pending.Stage+","+(pending.Opponent?"Opponent":"Player")+","+textLines.Count+","+Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(message)));File.WriteAllLines(Path.Combine(destination,"Messages.csv"),messageRows.ToArray());
lines.Add(watch.ElapsedMilliseconds+","+game.Turn+","+(pending.Opponent?"Opponent":"Player")+","+pending.Stage+","+pending.MovieName+","+timeline.Count+","+Hits(game.Player)+","+Hits(game.Enemy)+","+gdi+","+process.PrivateMemorySize64);
      File.WriteAllLines(Path.Combine(destination,"Stages.csv"),lines.ToArray());
     }
     if(pending!=null){
      if(watch.ElapsedMilliseconds-stageStart>20000)throw new Exception("Stage exceeded 20 seconds: "+pending.MovieName);
      if(!pauseChecked&&pauseUntil==0&&shots==2&&pending.Stage==ShotStage.Travel&&timeline.Frame>=10){Field<Button>(form,"pause").PerformClick();frozenFrame=timeline.Frame;frozenPlayer=Hits(game.Player);frozenEnemy=Hits(game.Enemy);pauseUntil=watch.ElapsedMilliseconds+650;}
      if(pending.Stage==ShotStage.Feedback&&pending.Result.Hit&&!hitPreview){Screenshot(form,Path.Combine(destination,"First-Hit.png"));hitPreview=true;}
      if(pending.Stage==ShotStage.Feedback&&pending.Result.Sunk&&!sinkPreview){Screenshot(form,Path.Combine(destination,"First-Sink.png"));sinkPreview=true;}
     }
     if(watch.ElapsedMilliseconds-lastProgress>=2000){lastProgress=watch.ElapsedMilliseconds;File.WriteAllLines(Path.Combine(destination,"Progress.txt"),new[]{"Status=running","ElapsedSeconds="+(watch.ElapsedMilliseconds/1000),"PlayerShots="+shots,"MovieStages="+stages,"Phase="+game.Phase,"Stage="+(pending==null?"None":pending.StageLabel),"Clip="+(pending==null?"None":pending.MovieName),"Frame="+timeline.Frame,"MaxGDI="+maxGdi});}
     if(watch.ElapsedMilliseconds>1200000)throw new Exception("Twenty-minute scenario timeout");System.Threading.Thread.Sleep(8);
    }
    if(!form.State.Enemy.AllSunk||form.State.Pending!=null||!pauseChecked||!hitPreview||!sinkPreview||(silent&&SilentDeviceOutput.Starts==0))throw new Exception("Incomplete terminal/pause/preview/device checks");
    Screenshot(form,Path.Combine(destination,"Victory.png"));form.Close();
   }
   File.WriteAllLines(Path.Combine(destination,"Result.txt"),new[]{"PASS one scripted perfect-target duel entered through original title -> Solo Practice -> Classic -> Fleet Configuration -> loading fade -> Start Game, using production form/timers, natural movie completion and original opponent level "+level+".","Detail="+(high?"320x240":"160x120")+", audio="+(silent?"actual waveOut with SILENT samples":"muted visual clock")+".","Player shots="+shots+", movie stages="+stages+", elapsed seconds="+(watch.ElapsedMilliseconds/1000)+", max observed GDI handles="+maxGdi+".","Actual-device buffer starts="+SilentDeviceOutput.Starts+", pause calls="+SilentDeviceOutput.Pauses+", resumes="+SilentDeviceOutput.Resumes+".","Pause froze frame and damage; current native messages match every firing/travel/sink stage; non-sinking hit/miss stays icon-only. Hit/sink captures and terminal victory completed. No stages were skipped.","Form offscreen. Targets use hidden-board knowledge for testing. No physical-input, audible-playback or full original-gameplay acceptance claim."});
   File.WriteAllText(Path.Combine(destination,"Progress.txt"),"Status=complete\n");return 0;
  }catch(Exception e){File.WriteAllText(Path.Combine(destination,"Result.txt"),"FAIL "+e+"\nElapsedSeconds="+(watch.ElapsedMilliseconds/1000)+"\nPlayerShots="+shots+"\nMovieStages="+stages);File.WriteAllText(Path.Combine(destination,"Progress.txt"),"Status=failed\n");return 1;}
 }
}

