using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
namespace FleetLab {
    // Finite media player owns decoded frames and audio. Game-stage transitions are external.
    public sealed class ClipStudy : UserControl {
        internal readonly Button PlayButton=new Button(),RestartButton=new Button(),PreviousButton=new Button(),NextButton=new Button();
        internal readonly EpochButton EndButton=new EpochButton();
        internal event Action PlaybackCompleted,SkipRequested;
        internal event Action FrameChanged;
        internal readonly ComboBox Selector=new ComboBox();
        internal ClipTimeline Timeline {get;private set;}
        readonly Label status=new Label(),title=new Label();readonly Timer timer=new Timer();readonly Stopwatch clock=Stopwatch.StartNew();
        readonly TableLayoutPanel header;
        readonly string assets;readonly IClipOutput output;
        bool highDetail;int frameOverride;bool externalClock;CreditsCatalog catalog;
        readonly MovieCanvas canvas=new MovieCanvas();ClipFrames frames;short[] pcm;ClipMix mix;int level=100;
        long last,lastAudioProgress;int lastAudioSample,levelResumeSample=-1;bool disposed,changing,soundEnabled,completionRaised,automatic;string clipName="",audioProblem="",loadProblem="",cueProblem="";
        internal string StageCaption="";
        public string Problem {get{return loadProblem!=""?loadProblem:audioProblem+(audioProblem!=""&&cueProblem!=""?" · ":"")+cueProblem;}}
        public ClipStudy(string assetRoot) : this(assetRoot,new WaveOutput()) { }
        public ClipStudy(string assetRoot,bool high) : this(assetRoot,new WaveOutput(),high) { }
        internal ClipStudy(string assetRoot,IClipOutput backend,bool high=false){
            highDetail=high;
            assets=assetRoot;output=backend;Dock=DockStyle.Fill;BackColor=Color.FromArgb(10,22,34);ForeColor=Color.WhiteSmoke;
            header=new TableLayoutPanel{Dock=DockStyle.Top,Height=140,ColumnCount=1,RowCount=4,Padding=new Padding(12,7,12,2)};
            foreach(int height in new[]{25,34,25,42})header.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
            title.Dock=DockStyle.Fill;title.ForeColor=Color.FromArgb(237,200,128);title.Font=new Font("Segoe UI",12,FontStyle.Bold);header.Controls.Add(title);
            Selector.DropDownStyle=ComboBoxStyle.DropDownList;Selector.Width=190;Selector.Items.AddRange(MovieCatalog.Names);Selector.AccessibleName="Original combat or ending movie";header.Controls.Add(Selector);
            status.Dock=DockStyle.Fill;status.AutoEllipsis=true;header.Controls.Add(status);
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};header.Controls.Add(buttons);
            Add(buttons,PlayButton,"Play",delegate{if(Timeline.Playing)Pause();else Play();});
            Add(buttons,RestartButton,"Restart",delegate{Reset();});
            Add(buttons,PreviousButton,"Previous frame",delegate{Step(-1);});
            Add(buttons,NextButton,"Next frame",delegate{Step(1);});
            Add(buttons,EndButton,"Skip to end",delegate{if(disposed)return;StopAudio();Timeline.End();if(automatic){completionRaised=true;SyncControls();if(SkipRequested!=null)SkipRequested();}else SyncControls();});
            canvas.Dock=DockStyle.Fill;Controls.Add(canvas);Controls.Add(header);
            Selector.SelectedIndexChanged+=delegate{if(!changing)SelectClip(Selector.SelectedItem.ToString());};Selector.SelectedIndex=0;
            timer.Interval=16;timer.Tick+=delegate{Tick();};timer.Start();
        }
        static void Add(Control parent,Button button,string text,EventHandler click){button.Text=text;button.AutoSize=true;button.Height=33;button.BackColor=Color.FromArgb(31,56,75);button.FlatStyle=FlatStyle.Flat;button.ForeColor=Color.WhiteSmoke;button.Click+=click;parent.Controls.Add(button);}
        bool SelectClip(string name){if(disposed)return false;
            Pause();StopAudio();ClipFrames next=null,previous=null;bool loaded=false;
            try{
                int frameMilliseconds=frameOverride==0?MovieCatalog.FrameMilliseconds(name):frameOverride;
                var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,(catalog==null?MovieCatalog.Directory(name,highDetail):catalog.Directory(name,highDetail)),name+".PAT")));
                Color[] colors=PatMovie.ClipColors(File.ReadAllBytes(Path.Combine(assets,(catalog==null?MovieCatalog.PaletteDirectory(name):catalog.Directory(name,highDetail)),name+".PAL")),File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")),catalog!=null);
                int displayFrames=externalClock?CreditsPresentation.VisibleFrames(movie.Frames.Count):movie.Frames.Count;next=new ClipFrames(movie,colors);next.Frame(0);
                short[] mixed=null;ClipMix nextMix=null;string soundError="",cueWarning="";try{List<SoundCue> cues;if(catalog==null)nextMix=ClipSound.LoadMix(assets,name,movie.Frames.Count,frameMilliseconds,out cues);else{List<string> missing;nextMix=ClipSound.LoadMixWithGaps(assets,name,movie.Frames.Count,frameMilliseconds,out cues,out missing);if(missing.Count>0)cueWarning="Missing original cue: "+string.Join(", ",missing.ToArray())+". Other cues remain available.";}if(externalClock)nextMix=nextMix.Prefix(ClipSound.FrameSample(displayFrames,frameMilliseconds));mixed=nextMix.Pcm(level);}catch(Exception e){soundError="Sound unavailable: "+e.Message;}
                previous=frames;frames=next;next=null;mix=nextMix;pcm=mixed;levelResumeSample=-1;Timeline=new ClipTimeline(displayFrames,frameMilliseconds);clipName=name;audioProblem=soundError;cueProblem=cueWarning;loadProblem="";
                completionRaised=false;EndButton.Epoch++;loaded=true;last=clock.ElapsedMilliseconds;
            }catch(Exception e){if(next!=null)next.Dispose();if(Timeline==null&&!automatic)throw;loadProblem="Clip unavailable: "+e.Message;changing=true;Selector.SelectedItem=clipName;changing=false;}
            try{SyncControls();}finally{if(previous!=null)previous.Dispose();}return loaded;
        }
        internal void ConfigureBattle(){
            automatic=true;Control buttons=EndButton.Parent;header.Controls.Remove(Selector);
            foreach(Button button in new[]{PlayButton,RestartButton,PreviousButton,NextButton})buttons.Controls.Remove(button);
            header.RowStyles.Clear();header.RowCount=3;foreach(int height in new[]{25,25,42})header.RowStyles.Add(new RowStyle(SizeType.Absolute,height));header.SetRow(title,0);header.SetRow(status,1);header.SetRow(buttons,2);header.Height=106;
            canvas.Visible=false;Dock=DockStyle.Bottom;Height=106;
            EndButton.Text="Skip stage (Space)";SyncControls();
        }
        internal void ConfigureCredits(CreditsCatalog values){catalog=values;ConfigureBattle();frameOverride=50;externalClock=true;UnloadBattle();}
        internal void ConfigureArchive(CreditsCatalog values){if(disposed)return;catalog=values;frameOverride=50;changing=true;Selector.Items.Clear();Selector.Items.AddRange(values.Names);Selector.SelectedIndex=0;changing=false;Selector.AccessibleName="Original movie archive, Credits cadence20fps";SelectClip(values.Name(1));}
        internal bool LoadBattle(string name){if(disposed)return false;EndButton.Epoch++;bool ok=SelectClip(name);changing=true;Selector.SelectedItem=name;changing=false;return ok;}
        internal bool SetDetail(bool high){
            if(disposed)return false;if(highDetail==high)return true;if(Timeline==null){highDetail=high;return true;}bool previous=highDetail,playing=Timeline.Playing,completed=Timeline.Completed;int frame=Timeline.Frame;highDetail=high;
            if(!SelectClip(clipName)){highDetail=previous;return false;}
            for(int i=0;i<Math.Min(frame,Timeline.Count-1);i++)Timeline.Step(1);
            if(completed)Timeline.End();if(playing)Play();else SyncControls();return true;
        }
        internal bool HighDetail {get{return highDetail;}}
        internal void ResumePlayback(){if(!disposed&&Timeline!=null&&!Timeline.Playing)Play();}
        internal void CancelPlayback(){Reset();}
        internal void UnloadBattle(){
            if(disposed||!automatic)return;audioProblem=loadProblem=cueProblem="";StopAudio();if(Timeline!=null)Timeline.Pause();Timeline=null;canvas.Image=null;canvas.Invalidate();
            ClipFrames previous=frames;frames=null;pcm=null;mix=null;levelResumeSample=-1;clipName="";completionRaised=false;EndButton.Epoch++;
            foreach(var button in new[]{PlayButton,RestartButton,PreviousButton,NextButton,EndButton})button.Enabled=false;
            title.Text="No active battle movie";status.Text=Problem;if(previous!=null)previous.Dispose();
        }
        internal void StopAtCurrent(){if(disposed)return;Pause();StopAudio();}
        void StopAudio(){try{output.Stop();}catch(Exception e){audioProblem=e.Message;}}
        void StartAudio(){
            if(!soundEnabled||pcm==null)return;
            try{int sample=levelResumeSample>=0?Math.Min(pcm.Length-1,levelResumeSample):(int)Math.Min(pcm.Length-1,Timeline.PositionMilliseconds*ClipSound.Rate/1000);levelResumeSample=-1;output.Start(pcm,sample);lastAudioSample=sample;lastAudioProgress=clock.ElapsedMilliseconds;audioProblem="";}
            catch(Exception e){audioProblem=e.Message+" Continuing silently.";output.Dispose();}
        }
        void Play(){if(disposed||Timeline==null)return;if(Timeline.Completed)completionRaised=false;Timeline.Play();last=clock.ElapsedMilliseconds;if(soundEnabled&&pcm!=null){if(output.Active){try{output.Resume();lastAudioProgress=last;}catch(Exception e){audioProblem=e.Message;StopAudio();}}else StartAudio();}SyncControls();}
        bool AudioTick(){
            int sample=output.Samples;long now=clock.ElapsedMilliseconds;
            if(sample>lastAudioSample){lastAudioSample=sample;lastAudioProgress=now;}else if(now-lastAudioProgress>2000)throw new InvalidOperationException("Audio device stopped advancing; continuing silently.");
            // A20fps duration can end at a fractional11025Hz sample. A completed
            // finite buffer owns the exact visual endpoint, avoiding a false stall.
            long position=sample>=pcm.Length?Timeline.DurationMilliseconds:(long)sample*1000/ClipSound.Rate;
            bool changed=Timeline.Synchronize(position);if(Timeline.Completed)StopAudio();return changed;
        }
        internal void AdvanceCredits(long milliseconds){if(disposed||!externalClock||Timeline==null||!Timeline.Playing)return;if(Timeline.Advance(milliseconds)){if(Timeline.Completed)StopAudio();SyncControls();}}
        void Tick(){if(externalClock)return;
            long now=clock.ElapsedMilliseconds,elapsed=now-last;last=now;if(disposed||Timeline==null||!Timeline.Playing)return;
            bool changed=false;
            if(output.Active){try{changed=AudioTick();}catch(Exception e){audioProblem=e.Message+" Continuing silently.";StopAudio();changed=Timeline.Advance(elapsed);SyncControls();}}
            else {if(elapsed>0)levelResumeSample=-1;changed=Timeline.Advance(elapsed);}
            if(changed)SyncControls();
        }
        void Step(int direction){if(disposed||Timeline==null)return;Pause();StopAudio();levelResumeSample=-1;Timeline.Step(direction);SyncControls();}
        void SyncControls(){if(disposed||Timeline==null||frames==null)return;ClipFrames shown=frames;Bitmap image=shown.Frame(Timeline.Frame);canvas.Image=image;canvas.Invalidate();PlayButton.Text=Timeline.Playing?"Pause":"Play";title.Text=automatic?StageCaption:clipName+(catalog!=null?" / movie archive · Credits cadence":MovieCatalog.IsEnding(clipName)?" / original ending — study playback":" / original combat clips — study library");
            status.Text=Problem!=""?Problem:"Frame "+(Timeline.Frame+1)+" / "+Timeline.Count+" · "+(1000/Timeline.FrameMilliseconds)+" fps · "+(Timeline.Completed?"Complete":Timeline.Playing?"Playing":"Paused")+" · "+(soundEnabled?(level==0?"Volume: off":"Original cues · "+level+"% "):"Muted")+" · "+image.Width+"×"+image.Height+(automatic?" · "+clipName:"");
            PlayButton.Enabled=RestartButton.Enabled=true;PreviousButton.Enabled=Timeline.Frame>0;NextButton.Enabled=Timeline.Frame+1<Timeline.Count;
            try{if(FrameChanged!=null)FrameChanged();}finally{shown.ReleaseRetired();}
            if(!disposed&&Timeline!=null&&Timeline.Completed&&!completionRaised){completionRaised=true;if(PlaybackCompleted!=null)PlaybackCompleted();}
        }
        public void SetSound(bool enabled){if(disposed)return;if(enabled==soundEnabled)return;if(output.Active&&Timeline!=null&&Timeline.Playing){try{if(!externalClock)AudioTick();}catch(Exception e){audioProblem=e.Message;}}StopAudio();soundEnabled=enabled;last=clock.ElapsedMilliseconds;if(Timeline!=null&&Timeline.Playing)StartAudio();SyncControls();}
        public void SetLevel(int percent){
            if(disposed)return;SoundLevel.Validate(percent);if(level==percent)return;
            short[] next=mix==null?null:mix.Pcm(percent);
            if(output.Active&&Timeline!=null&&Timeline.Playing){try{if(!externalClock)AudioTick();}catch(Exception e){audioProblem=e.Message;}}
            if(output.Active&&Timeline!=null)try{levelResumeSample=output.Samples;}catch(Exception e){levelResumeSample=-1;audioProblem=e.Message;}
            StopAudio();level=percent;pcm=next;last=clock.ElapsedMilliseconds;
            if(soundEnabled&&Timeline!=null&&Timeline.Playing)StartAudio();SyncControls();
        }
        internal string ClipName{get{return clipName;}}internal bool SoundEnabled{get{return soundEnabled;}}
        internal int Level {get{return level;}}
        public void Pause(){if(disposed||Timeline==null)return;if(output.Active){try{output.Pause();if(Timeline.Playing&&!externalClock)AudioTick();}catch(Exception e){audioProblem=e.Message;StopAudio();}}Timeline.Pause();SyncControls();}
        public void Reset(){if(disposed||Timeline==null)return;StopAudio();levelResumeSample=-1;completionRaised=false;Timeline.Reset();last=clock.ElapsedMilliseconds;SyncControls();}
        internal void PreviewFrame(int index){if(disposed||Timeline==null)return;Reset();for(int i=0;i<index;i++)Timeline.Step(1);SyncControls();}
        internal int RawFrameCount{get{return frames==null?0:frames.Count;}}
        internal IndexedImage IndexedFrame(int rawFrame){return disposed||frames==null?null:frames.Indexed(rawFrame);}
        internal Bitmap CurrentImage {get{return disposed||frames==null||Timeline==null?null:frames.Frame(Timeline.Frame);}}
        internal int CachedBitmapCount {get{return frames==null?0:frames.CachedBitmaps;}}
        internal Bitmap FadedFrame(int factor){return frames.FadedFrame(Timeline.Frame,factor);}
        internal bool AudioActive {get{return output.Active;}}
        internal sealed class EpochButton : Button {
            public int Epoch;int pressedEpoch;bool armed;
            protected override void OnMouseDown(MouseEventArgs e){pressedEpoch=Epoch;armed=true;base.OnMouseDown(e);}
            protected override void OnMouseUp(MouseEventArgs e){try{base.OnMouseUp(e);}finally{armed=false;}}
            protected override void OnClick(EventArgs e){if(!armed||pressedEpoch==Epoch)base.OnClick(e);}
        }
        protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;timer.Stop();timer.Dispose();if(Timeline!=null)Timeline.Pause();Timeline=null;canvas.Image=null;output.Dispose();if(frames!=null)frames.Dispose();frames=null;pcm=null;mix=null;if(automatic){Selector.Dispose();foreach(Button button in new[]{PlayButton,RestartButton,PreviousButton,NextButton})button.Dispose();}}base.Dispose(disposing);}
        sealed class MovieCanvas : Control {
            public Bitmap Image;
            public MovieCanvas(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.FromArgb(4,10,16);}
            protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Image==null)return;float scale=Math.Min(Width/(float)Image.Width,Height/(float)Image.Height);var bounds=new RectangleF((Width-Image.Width*scale)/2,(Height-Image.Height*scale)/2,Image.Width*scale,Image.Height*scale);e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(Image,bounds);}
        }
    }
}

