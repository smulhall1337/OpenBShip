using System;using System.IO;using System.Reflection;using System.Windows.Forms;using System.Drawing;using System.Collections.Generic;using FleetLab;
// Owned offscreen forms only. No settings writes, physical input or user processes.
class CheckShutdown {
 sealed class SilentOutput:IClipOutput {readonly WaveOutput wave=new WaveOutput();public bool Disposed;public int Starts;public readonly List<int> Peaks=new List<int>(),Offsets=new List<int>();public bool Active{get{return wave.Active;}}public int Samples{get{return wave.Samples;}}public void Start(short[] pcm,int offset){int peak=0;foreach(short value in pcm)peak=Math.Max(peak,Math.Abs((int)value));Peaks.Add(peak);Offsets.Add(offset);wave.Start(new short[pcm.Length],offset);Starts++;}public void Pause(){wave.Pause();}public void Resume(){wave.Resume();}public void Stop(){wave.Stop();}public void Dispose(){wave.Dispose();Disposed=true;}}
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Field(object o,string n){return o.GetType().GetField(n,F).GetValue(o);}
 static object Property(object o,string n){return o.GetType().GetProperty(n,F).GetValue(o,null);}
 static void Invoke(object o,string n,params object[] args){o.GetType().GetMethod(n,F).Invoke(o,args);}
 static void Require(bool value,string text){if(!value)throw new Exception(text);}
 static void Prepare(Form f){((ClipStudy)Field(f,"archive")).SetLevel(100);((DisplaySettings)Field(f,"preferences")).FxVolume=100;((Audio)Field(f,"sound")).SetLevel(100);((ClipStudy)Field(f,"clip")).SetLevel(100);((ClipStudy)Field(f,"battleMovie")).SetLevel(100);((EndingPlayer)Field(f,"ending")).SetLevel(100);typeof(MainForm).GetField("previewMode",F).SetValue(f,true);f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);}
 [STAThread]static int Main(string[] args){if(args.Length<2)return 2;string assets=Path.GetFullPath(args[0]),report=Path.GetFullPath(args[1]);bool old=args.Length==3&&args[2]=="expect-v015-failure";var rows=new List<string>();int errors=0;Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=delegate(object s,System.Threading.ThreadExceptionEventArgs e){errors++;rows.Add("UNHANDLED "+e.Exception);};Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  try{
   var probe=new EndingPlayer(assets);probe.SetSound(false);probe.Dispose();
   if(old){try{probe.Pause();throw new Exception("Expected supplied shutdown failure was absent");}catch(NullReferenceException e){rows.Add("REPRODUCED v0.15: EndingPlayer.Pause after Dispose -> "+e.GetType().Name+" / "+e.StackTrace);File.WriteAllLines(report,rows.ToArray());return 0;}}
   probe.Pause();probe.Cancel();probe.SetSound(false);probe.SetLevel(50);probe.Dispose();rows.Add("PASS late result pause/reset/sound and repeated disposal");
   using(var clip=new ClipStudy(assets)){clip.Dispose();clip.Pause();clip.Reset();clip.SetSound(false);clip.SetLevel(50);Invoke(clip,"UnloadBattle");Invoke(clip,"Tick");Invoke(clip,"SyncControls");Require(Property(clip,"CurrentImage")==null,"Disposed clip retained frame");}rows.Add("PASS late clip timer/sync/pause/reset/sound do not touch disposed resources");
   foreach(string stage in new[]{"idle","reveal","hold","cardfade","movie","paused-movie","moviefade","complete","combat","silent-movie","title-menu","title-quit","solo-setup","solo-start","fleet-selected","fleet-start","entry-paused","entry-resumed","end-confirm","end-title","silent-end-title","preferences-entry","preferences-ready","preferences-changed","preferences-battle","silent-preference-gain","credits-entry","credits-playing","credits-paused","credits-exit","silent-credits","archive-playing","archive-paused","archive-partial","silent-archive","credits-partial","silent-credits-partial"}){
    using(var form=new MainForm(assets)){
     Prepare(form);((Audio)Field(form,"sound")).Enabled=false;((ClipStudy)Field(form,"clip")).SetSound(false);((ClipStudy)Field(form,"battleMovie")).SetSound(false);var player=(EndingPlayer)Field(form,"ending");player.SetSound(false);SilentOutput device=null;
     if(stage=="silent-movie"){var movie=Field(player,"Movie");((IClipOutput)Field(movie,"output")).Dispose();device=new SilentOutput();movie.GetType().GetField("output",F).SetValue(movie,device);player.SetSound(true);}
     form.Show();Application.DoEvents();var tabs=(TabControl)Field(form,"tabs");
     if(stage.StartsWith("archive-")||stage=="silent-archive"){
      var movie=(ClipStudy)Field(form,"archive");movie.SetSound(false);if(stage=="silent-archive"){((IClipOutput)Field(movie,"output")).Dispose();device=new SilentOutput();movie.GetType().GetField("output",F).SetValue(movie,device);movie.SetSound(true);}tabs.SelectedIndex=5;((ComboBox)Field(movie,"Selector")).SelectedItem=stage=="archive-partial"||stage=="silent-archive"?"ISL_DSHS":"F18LAND";((Button)Field(movie,"PlayButton")).PerformClick();if(stage=="archive-paused")movie.Pause();if(stage=="archive-partial"||stage=="silent-archive")Require(movie.Problem.Contains("EXP02"),"Missing-cue warning absent");if(device!=null)Require(device.Active&&device.Starts==1&&device.Peaks[0]>0,"Silent archive lost surviving cues");
     }
     else
     if(stage.StartsWith("credits-")||stage=="silent-credits"||stage=="silent-credits-partial"){
      object page=Field(form,"creditsPage");var movie=(ClipStudy)Field(page,"Movie");movie.SetSound(false);if(stage=="silent-credits"||stage=="silent-credits-partial"){((IClipOutput)Field(movie,"output")).Dispose();device=new SilentOutput();movie.GetType().GetField("output",F).SetValue(movie,device);movie.SetSound(true);}
      tabs.SelectedIndex=4;Invoke(Field(form,"front"),"FinishReveal");Invoke(Field(form,"front"),"Activate",2);Invoke(Field(form,"prefsPage"),"FinishReveal");Invoke(Field(form,"prefsPage"),"Activate",10);if(stage=="credits-partial"||stage=="silent-credits-partial"){object pref=Field(form,"prefsPage");var snapshot=(IndexedImage)pref.GetType().GetMethod("SnapshotIndexed",F).Invoke(pref,null);uint seed=0;while(new NativeRandom(seed).Next()%137!=99)seed++;Invoke(page,"Begin",snapshot,false,(uint?)seed);}if(stage!="credits-entry"){Invoke(page,"FinishEntry");Invoke(page,"Advance",50L);Invoke(page,"Advance",100L);Require(((ClipTimeline)Property(movie,"Timeline")).FrameMilliseconds==50,"Credits frame cadence");}if(stage=="credits-paused")Invoke(page,"Pause");if(stage=="credits-exit")Invoke(page,"Back");if(device!=null)Require(device.Active&&device.Starts==1,"Silent credits buffer not active");
     }
     else
     if(stage.StartsWith("preferences-")||stage=="silent-preference-gain"){
      if(stage=="preferences-battle"||stage=="silent-preference-gain"){
       ((DisplaySettings)Field(form,"preferences")).MovieTurns=true;
       if(stage=="silent-preference-gain"){var movie=Field(form,"battleMovie");((IClipOutput)Field(movie,"output")).Dispose();device=new SilentOutput();movie.GetType().GetField("output",F).SetValue(movie,device);((Audio)Field(form,"sound")).Enabled=true;((ClipStudy)movie).SetSound(true);}
       Invoke(form,"NewGame");((Button)Field(form,"auto")).PerformClick();((Button)Field(form,"start")).PerformClick();form.State.Fire(form.State.Enemy.Ships[0].Cells[0]);Invoke(form,"UpdateView");
      }
      tabs.SelectedIndex=4;Invoke(Field(form,"front"),"FinishReveal");Invoke(Field(form,"front"),"Activate",2);object page=Field(form,"prefsPage");
      if(stage!="preferences-entry")Invoke(page,"FinishReveal");
      if(stage=="preferences-changed")Invoke(form,"ApplyPreferences",new PreferenceValues{Quality=2,Radar=1,Difficulty=2,Volume=50});
      if(stage=="silent-preference-gain"){
       Require(device.Active&&device.Starts==1,"Silent gain buffer did not start");int pausedAt=device.Samples;
       Invoke(page,"Bar",258);Require(!device.Active&&form.State.Paused&&device.Starts==1,"Paused gain unexpectedly restarted output");((Button)Field(page,"BackButton")).PerformClick();((Button)Field(Field(form,"front"),"BackButton")).PerformClick();((Button)Field(form,"pause")).PerformClick();
       Require(device.Active&&device.Starts==2&&device.Peaks[0]>device.Peaks[1]&&device.Offsets[1]>=pausedAt,"Gain failed to attenuate/restart at preserved actual device position");
      }
     }
     else if(stage=="end-confirm"||stage=="end-title"||stage=="silent-end-title"){
      ((DisplaySettings)Field(form,"preferences")).MovieTurns=true;
      if(stage=="silent-end-title"){var movie=Field(form,"battleMovie");((IClipOutput)Field(movie,"output")).Dispose();device=new SilentOutput();movie.GetType().GetField("output",F).SetValue(movie,device);((Audio)Field(form,"sound")).Enabled=true;((ClipStudy)movie).SetSound(true);}
      Invoke(form,"NewGame");((Button)Field(form,"auto")).PerformClick();((Button)Field(form,"start")).PerformClick();form.State.Fire(form.State.Enemy.Ships[0].Cells[0]);Invoke(form,"UpdateView");if(device!=null)Require(device.Active&&device.Starts==1,"Silent battle buffer did not start");form.State.Paused=true;Invoke(form,"UpdateView");var actions=Field(form,"pauseActions");((Button)Field(actions,"AskButton")).PerformClick();if(stage!="end-confirm"){((Button)Field(actions,"EndButton")).PerformClick();Require(form.State.Phase==Phase.Placement&&tabs.SelectedIndex==4,"Confirmed end failed");if(device!=null)Require(!device.Active&&!device.Disposed,"End did not stop reusable silent device");}
     }
     else if(stage.StartsWith("solo-")||stage.StartsWith("fleet-")||stage.StartsWith("entry-")){tabs.SelectedIndex=4;Invoke(Field(form,"front"),"FinishReveal");Invoke(Field(form,"front"),"Activate",0);Invoke(Field(form,"solo"),"FinishReveal");if(stage=="solo-start"||stage.StartsWith("fleet-")||stage.StartsWith("entry-")){Invoke(Field(form,"solo"),"Activate",6);Invoke(Field(form,"solo"),"Activate",10);if(stage=="fleet-selected"){object fleet=Field(form,"fleet");Game draft=(Game)Property(fleet,"Draft");Invoke(fleet,"Cell",draft.Player.Ships[0].Cells[2]);}if(stage=="fleet-start"||stage.StartsWith("entry-")){Invoke(Field(form,"fleet"),"Activate",2);if(stage=="fleet-start")Invoke(Field(form,"entry"),"Advance",560L);else{Invoke(Field(form,"entry"),"Pause");if(stage=="entry-resumed")Invoke(Field(form,"entry"),"Resume");}}}}
     else if(stage.StartsWith("title-")){tabs.SelectedIndex=4;Invoke(Field(form,"front"),"FinishReveal");if(stage=="title-quit")Invoke(Field(form,"front"),"Activate",3);}
     else if(stage=="combat"){var prefs=(DisplaySettings)Field(form,"preferences");prefs.MovieTurns=true;Invoke(form,"NewGame");((Button)Field(form,"auto")).PerformClick();((Button)Field(form,"start")).PerformClick();form.State.Fire(form.State.Enemy.Ships[0].Cells[0]);Invoke(form,"UpdateView");}
     else if(stage!="idle") {tabs.SelectedIndex=3;player.StartOutcome(stage!="hold");int skips=stage=="hold"?1:stage=="cardfade"?2:stage=="movie"||stage=="paused-movie"||stage=="silent-movie"?3:stage=="moviefade"?4:stage=="complete"?5:0;for(int i=0;i<skips;i++)((Button)Field(player,"SkipButton")).PerformClick();if(stage=="paused-movie")player.Pause();}
     if(stage=="silent-movie")Require(device.Active&&device.Starts==1,"Silent device did not start finite movie buffer");
     if(stage=="silent-archive"){
      var movie=(ClipStudy)Field(form,"archive");var watch=System.Diagnostics.Stopwatch.StartNew();while(watch.ElapsedMilliseconds<120){Application.DoEvents();System.Threading.Thread.Sleep(4);}movie.Pause();int frozen=((ClipTimeline)Property(movie,"Timeline")).Frame;watch.Restart();while(watch.ElapsedMilliseconds<80){Application.DoEvents();System.Threading.Thread.Sleep(4);}Require(((ClipTimeline)Property(movie,"Timeline")).Frame==frozen,"Actual archive pause advanced");Invoke(movie,"ResumePlayback");watch.Restart();while(!((ClipTimeline)Property(movie,"Timeline")).Completed&&watch.ElapsedMilliseconds<10000){Application.DoEvents();System.Threading.Thread.Sleep(4);}Require(((ClipTimeline)Property(movie,"Timeline")).Completed&&!device.Active&&movie.Problem.Contains("EXP02")&&!movie.Problem.Contains("stopped advancing"),"20fps actual archive endpoint/warning failed");
     }
     // WinForms can send focus loss while base.Dispose is tearing down handles,
     // after MainForm has already disposed its manually-owned ending player.
     form.Disposed+=delegate{Invoke(form,"OnDeactivate",EventArgs.Empty);Invoke(Field(form,"tabs"),"OnSelectedIndexChanged",EventArgs.Empty);};
     if(stage=="title-quit")((Button)Field(Field(form,"front"),"QuitButton")).PerformClick();else form.Close();Application.DoEvents();Invoke(form,"OnDeactivate",EventArgs.Empty);form.Dispose();
     Require(form.IsDisposed&&player.IsDisposed&&Property(player,"CurrentImage")==null,"Owned form/player retained resources");if(device!=null)Require(device.Disposed&&!device.Active,"Closing retained waveOut buffer/device");Require(errors==0,"ThreadException during "+stage);rows.Add("PASS close and late Deactivate: "+stage);
    }
   }
   rows.Add("PASS shutdown checks; ThreadException count="+errors+". Thirty-seven owned form states, plus two component lifecycle cases. Active SILENT waveOut buffer closes on movie exit. Offscreen; user desktop focus and audible output remain hands-on checks.");File.WriteAllLines(report,rows.ToArray());return 0;
  }catch(Exception e){rows.Add("FAIL "+e);File.WriteAllLines(report,rows.ToArray());return 1;}
 }
}
