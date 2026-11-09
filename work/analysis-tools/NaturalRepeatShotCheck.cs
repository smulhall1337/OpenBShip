using System;using System.IO;using System.Drawing;using System.Windows.Forms;using System.Reflection;using System.Diagnostics;using System.Collections.Generic;using FleetLab;
// Explicit fixture, actual production form/timers. Synthetic pre-shot board;
// no physical input or audible acceptance. Device mode queues only zeros.
class NaturalRepeatShotCheck {
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static T Field<T>(object o,string n){return (T)o.GetType().GetField(n,Flags).GetValue(o);}
 static T Prop<T>(object o,string n){return (T)o.GetType().GetProperty(n,Flags).GetValue(o,null);}
 static void Set(object o,string n,object v){o.GetType().GetField(n,Flags).SetValue(o,v);}
 static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
 sealed class Silent:IClipOutput{readonly WaveOutput wave=new WaveOutput();public int Starts;public bool Active{get{return wave.Active;}}public int Samples{get{return wave.Samples;}}public void Start(short[] p,int at){wave.Start(new short[p.Length],at);Starts++;}public void Pause(){wave.Pause();}public void Resume(){wave.Resume();}public void Stop(){wave.Stop();}public void Dispose(){wave.Dispose();}}
 static Game Fixture(out int selectedSeed){
  for(int seed=0;seed<512;seed++){
   var g=new Game(seed,RuleProfile.Recovered,OpponentKind.Original,0,true);g.AutoPlace();g.Start();g.Player=new Board();g.Player.Place(new Point(6,2),5,true,"Sunk battleship",1);g.Player.Place(new Point(8,0),2,true,"Survivor",4);
   for(int x=0;x<10;x++)for(int y=0;y<10;y++){var p=new Point(x,y);if(g.Player.ShipAt(p)!=g.Player.Ships[1])g.Player.Shoot(p);}
   g.Phase=Phase.OpponentTurn;if(!g.AdvanceOpponent()||g.Pending==null||g.Player.ShipAt(g.Pending.Cell)!=g.Player.Ships[0])continue;
   int weapon=g.Pending.Firing.WeaponId;if(!((weapon>=14&&weapon<=18)||weapon==25||weapon==26))continue;
   selectedSeed=seed;return g;
  }throw new Exception("No reachable repeated-sunk fixture found");
 }
 [STAThread]static int Main(string[] args){if(args.Length!=3)return 2;string dest=Path.GetFullPath(args[1]);Directory.CreateDirectory(dest);bool silent=args[2]=="silent-device";int exceptions=0;Application.ThreadException+=delegate(object s,System.Threading.ThreadExceptionEventArgs e){exceptions++;};var watch=Stopwatch.StartNew();
  try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);int seed;Game g=Fixture(out seed);int hits=g.Player.Ships[0].Hits;int expected=ClassicImpact.Miss(g.Pending.Firing.WeaponId,new NativeRandom(unchecked((uint)seed)^0x42AC1u).Next());var stages=new List<string>();bool paused=false;long resumeAt=0;int frame=0,feedback=0;
   using(var form=new MainForm(Path.GetFullPath(args[0]))){Set(form,"previewMode",true);form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);Field<Audio>(form,"sound").Enabled=false;
    var preferences=Field<DisplaySettings>(form,"preferences");preferences.MovieTurns=true;preferences.Sound=false;var movie=Field<ClipStudy>(form,"battleMovie");movie.SetSound(false);var device=new Silent();if(silent){Field<IClipOutput>(movie,"output").Dispose();Set(movie,"output",device);movie.SetSound(true);}
    form.Show();Application.DoEvents();Field<TabControl>(form,"tabs").SelectedIndex=0;form.State=g;Field<BoardView>(form,"board").Game=g;typeof(MainForm).GetMethod("UpdateView",Flags).Invoke(form,null);
    ShotStage previous=ShotStage.Firing;bool first=true;
    while(g.Pending!=null){Application.DoEvents();Need(exceptions==0&&movie.Problem=="","Production form/media error");Need(watch.ElapsedMilliseconds<60000,"Controlled shot timeout");Need(g.Player.Ships[0].Hits==hits,"Repeated shot applied new damage");
     PendingShot pending=g.Pending;if(pending==null)break;var timeline=Prop<ClipTimeline>(movie,"Timeline");
     if(first||pending.Stage!=previous){first=false;previous=pending.Stage;stages.Add(pending.Stage+":"+pending.MovieName);if(pending.Stage==ShotStage.Feedback){feedback=pending.FeedbackId;Need(feedback==expected&&g.LastEnemyShot.Repeated&&!g.LastEnemyShot.Hit,"Repeated-sunk feedback mismatch");using(var bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,new Rectangle(0,0,form.Width,form.Height));bmp.Save(Path.Combine(dest,"Repeated-Sunk-Feedback.png"));}}}
     if(resumeAt>0){Need(g.Paused&&timeline.Frame==frame,"Paused feedback advanced");if(watch.ElapsedMilliseconds>=resumeAt){Field<Button>(form,"pause").PerformClick();resumeAt=0;}}
     else if(!paused&&pending.Stage==ShotStage.Feedback&&timeline.Frame>=5){Field<Button>(form,"pause").PerformClick();frame=timeline.Frame;resumeAt=watch.ElapsedMilliseconds+300;paused=true;}
     System.Threading.Thread.Sleep(4);
    }
    Need(stages.Count==3&&paused&&g.Phase==Phase.PlayerTurn&&g.Turn==2&&feedback==expected,"Stage/turn completion mismatch");Need(!Prop<bool>(movie,"AudioActive"),"Completed shot retained audio");form.Close();Application.DoEvents();Need(exceptions==0,"Close raised ThreadException");
    File.WriteAllText(Path.Combine(dest,"Check.txt"),"PASS actual production form: "+string.Join(" -> ",stages.ToArray())+"; no skipped/accelerated movies;300ms feedback pause; no new damage; next player turn; clean close. ElapsedMs="+watch.ElapsedMilliseconds+";seed="+seed+";feedback="+feedback+";SILENT="+silent+";deviceStarts="+device.Starts+";ThreadException="+exceptions+". Synthetic pre-shot board; simulated controls, not physical or audible acceptance.\r\n");
   }return 0;
  }catch(Exception e){File.WriteAllText(Path.Combine(dest,"Check.txt"),"FAIL "+e);return 1;}
 }
}
