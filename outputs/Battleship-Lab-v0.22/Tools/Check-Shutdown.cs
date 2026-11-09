using System;using System.IO;using System.Reflection;using System.Windows.Forms;using System.Drawing;using System.Collections.Generic;using FleetLab;
// Owned offscreen forms only. No settings writes, physical input or user processes.
class CheckShutdown {
 sealed class SilentOutput:IClipOutput {readonly WaveOutput wave=new WaveOutput();public bool Disposed;public int Starts;public bool Active{get{return wave.Active;}}public int Samples{get{return wave.Samples;}}public void Start(short[] pcm,int offset){wave.Start(new short[pcm.Length],offset);Starts++;}public void Pause(){wave.Pause();}public void Resume(){wave.Resume();}public void Stop(){wave.Stop();}public void Dispose(){wave.Dispose();Disposed=true;}}
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Field(object o,string n){return o.GetType().GetField(n,F).GetValue(o);}
 static object Property(object o,string n){return o.GetType().GetProperty(n,F).GetValue(o,null);}
 static void Invoke(object o,string n,params object[] args){o.GetType().GetMethod(n,F).Invoke(o,args);}
 static void Require(bool value,string text){if(!value)throw new Exception(text);}
 static void Prepare(Form f){typeof(MainForm).GetField("previewMode",F).SetValue(f,true);f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);}
 [STAThread]static int Main(string[] args){if(args.Length<2)return 2;string assets=Path.GetFullPath(args[0]),report=Path.GetFullPath(args[1]);bool old=args.Length==3&&args[2]=="expect-v015-failure";var rows=new List<string>();int errors=0;Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=delegate(object s,System.Threading.ThreadExceptionEventArgs e){errors++;rows.Add("UNHANDLED "+e.Exception);};Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  try{
   var probe=new EndingPlayer(assets);probe.SetSound(false);probe.Dispose();
   if(old){try{probe.Pause();throw new Exception("Expected supplied shutdown failure was absent");}catch(NullReferenceException e){rows.Add("REPRODUCED v0.15: EndingPlayer.Pause after Dispose -> "+e.GetType().Name+" / "+e.StackTrace);File.WriteAllLines(report,rows.ToArray());return 0;}}
   probe.Pause();probe.Cancel();probe.SetSound(false);probe.Dispose();rows.Add("PASS late result pause/reset/sound and repeated disposal");
   using(var clip=new ClipStudy(assets)){clip.Dispose();clip.Pause();clip.Reset();clip.SetSound(false);Invoke(clip,"Tick");Invoke(clip,"SyncControls");Require(Property(clip,"CurrentImage")==null,"Disposed clip retained frame");}rows.Add("PASS late clip timer/sync/pause/reset/sound do not touch disposed resources");
   foreach(string stage in new[]{"idle","reveal","hold","cardfade","movie","paused-movie","moviefade","complete","combat","silent-movie","title-menu","title-quit","solo-setup","solo-start","fleet-selected","fleet-start","entry-paused","entry-resumed","end-confirm","end-title","silent-end-title"}){
    using(var form=new MainForm(assets)){
     Prepare(form);((Audio)Field(form,"sound")).Enabled=false;((ClipStudy)Field(form,"clip")).SetSound(false);((ClipStudy)Field(form,"battleMovie")).SetSound(false);var player=(EndingPlayer)Field(form,"ending");player.SetSound(false);SilentOutput device=null;
     if(stage=="silent-movie"){var movie=Field(player,"Movie");((IClipOutput)Field(movie,"output")).Dispose();device=new SilentOutput();movie.GetType().GetField("output",F).SetValue(movie,device);player.SetSound(true);}
     form.Show();Application.DoEvents();var tabs=(TabControl)Field(form,"tabs");
     if(stage=="end-confirm"||stage=="end-title"||stage=="silent-end-title"){
      ((DisplaySettings)Field(form,"preferences")).MovieTurns=true;
      if(stage=="silent-end-title"){var movie=Field(form,"battleMovie");((IClipOutput)Field(movie,"output")).Dispose();device=new SilentOutput();movie.GetType().GetField("output",F).SetValue(movie,device);((Audio)Field(form,"sound")).Enabled=true;((ClipStudy)movie).SetSound(true);}
      Invoke(form,"NewGame");((Button)Field(form,"auto")).PerformClick();((Button)Field(form,"start")).PerformClick();form.State.Fire(form.State.Enemy.Ships[0].Cells[0]);Invoke(form,"UpdateView");if(device!=null)Require(device.Active&&device.Starts==1,"Silent battle buffer did not start");form.State.Paused=true;Invoke(form,"UpdateView");var actions=Field(form,"pauseActions");((Button)Field(actions,"AskButton")).PerformClick();if(stage!="end-confirm"){((Button)Field(actions,"EndButton")).PerformClick();Require(form.State.Phase==Phase.Placement&&tabs.SelectedIndex==4,"Confirmed end failed");if(device!=null)Require(!device.Active&&!device.Disposed,"End did not stop reusable silent device");}
     }
     else if(stage.StartsWith("solo-")||stage.StartsWith("fleet-")||stage.StartsWith("entry-")){tabs.SelectedIndex=4;Invoke(Field(form,"front"),"FinishReveal");Invoke(Field(form,"front"),"Activate",0);Invoke(Field(form,"solo"),"FinishReveal");if(stage=="solo-start"||stage.StartsWith("fleet-")||stage.StartsWith("entry-")){Invoke(Field(form,"solo"),"Activate",6);Invoke(Field(form,"solo"),"Activate",10);if(stage=="fleet-selected"){object fleet=Field(form,"fleet");Game draft=(Game)Property(fleet,"Draft");Invoke(fleet,"Cell",draft.Player.Ships[0].Cells[2]);}if(stage=="fleet-start"||stage.StartsWith("entry-")){Invoke(Field(form,"fleet"),"Activate",2);if(stage=="fleet-start")Invoke(Field(form,"entry"),"Advance",560L);else{Invoke(Field(form,"entry"),"Pause");if(stage=="entry-resumed")Invoke(Field(form,"entry"),"Resume");}}}}
     else if(stage.StartsWith("title-")){tabs.SelectedIndex=4;Invoke(Field(form,"front"),"FinishReveal");if(stage=="title-quit")Invoke(Field(form,"front"),"Activate",3);}
     else if(stage=="combat"){var prefs=(DisplaySettings)Field(form,"preferences");prefs.MovieTurns=true;Invoke(form,"NewGame");((Button)Field(form,"auto")).PerformClick();((Button)Field(form,"start")).PerformClick();form.State.Fire(form.State.Enemy.Ships[0].Cells[0]);Invoke(form,"UpdateView");}
     else if(stage!="idle") {tabs.SelectedIndex=3;player.StartOutcome(stage!="hold");int skips=stage=="hold"?1:stage=="cardfade"?2:stage=="movie"||stage=="paused-movie"||stage=="silent-movie"?3:stage=="moviefade"?4:stage=="complete"?5:0;for(int i=0;i<skips;i++)((Button)Field(player,"SkipButton")).PerformClick();if(stage=="paused-movie")player.Pause();}
     if(stage=="silent-movie")Require(device.Active&&device.Starts==1,"Silent device did not start finite movie buffer");
     // WinForms can send focus loss while base.Dispose is tearing down handles,
     // after MainForm has already disposed its manually-owned ending player.
     form.Disposed+=delegate{Invoke(form,"OnDeactivate",EventArgs.Empty);Invoke(Field(form,"tabs"),"OnSelectedIndexChanged",EventArgs.Empty);};
     if(stage=="title-quit")((Button)Field(Field(form,"front"),"QuitButton")).PerformClick();else form.Close();Application.DoEvents();Invoke(form,"OnDeactivate",EventArgs.Empty);form.Dispose();
     Require(form.IsDisposed&&player.IsDisposed&&Property(player,"CurrentImage")==null,"Owned form/player retained resources");if(device!=null)Require(device.Disposed&&!device.Active,"Closing retained waveOut buffer/device");Require(errors==0,"ThreadException during "+stage);rows.Add("PASS close and late Deactivate: "+stage);
    }
   }
   rows.Add("PASS shutdown checks; ThreadException count="+errors+". Twenty-one owned form states, plus two component lifecycle cases. Active SILENT waveOut buffer closes on movie exit. Offscreen; user desktop focus and audible output remain hands-on checks.");File.WriteAllLines(report,rows.ToArray());return 0;
  }catch(Exception e){rows.Add("FAIL "+e);File.WriteAllLines(report,rows.ToArray());return 1;}
 }
}
