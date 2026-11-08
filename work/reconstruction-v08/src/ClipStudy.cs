using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
namespace FleetLab {
    // Independent study library. No turn/RNG changes or unimplemented travel/impact stages.
    public sealed class ClipStudy : UserControl {
        internal readonly Button PlayButton=new Button(),RestartButton=new Button(),PreviousButton=new Button(),NextButton=new Button(),EndButton=new Button();
        internal readonly ComboBox Selector=new ComboBox();
        internal ClipTimeline Timeline {get;private set;}
        readonly Label status=new Label(),title=new Label();readonly Timer timer=new Timer();readonly Stopwatch clock=Stopwatch.StartNew();
        readonly string assets;readonly IClipOutput output;
        readonly MovieCanvas canvas=new MovieCanvas();List<Bitmap> frames=new List<Bitmap>();short[] pcm;
        long last,lastAudioProgress;int lastAudioSample;bool disposed,changing,soundEnabled;string clipName="",audioProblem="",loadProblem="";
        public string Problem {get{return loadProblem!=""?loadProblem:audioProblem;}}
        public ClipStudy(string assetRoot) : this(assetRoot,new WaveOutput()) { }
        internal ClipStudy(string assetRoot,IClipOutput backend){
            assets=assetRoot;output=backend;Dock=DockStyle.Fill;BackColor=Color.FromArgb(10,22,34);ForeColor=Color.WhiteSmoke;
            var header=new TableLayoutPanel{Dock=DockStyle.Top,Height=140,ColumnCount=1,RowCount=4,Padding=new Padding(12,7,12,2)};
            foreach(int height in new[]{25,34,25,42})header.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
            title.Dock=DockStyle.Fill;title.ForeColor=Color.FromArgb(237,200,128);title.Font=new Font("Segoe UI",12,FontStyle.Bold);header.Controls.Add(title);
            Selector.DropDownStyle=ComboBoxStyle.DropDownList;Selector.Width=190;Selector.Items.AddRange(FiringClips.Names);Selector.AccessibleName="Original firing clip";header.Controls.Add(Selector);
            status.Dock=DockStyle.Fill;status.AutoEllipsis=true;header.Controls.Add(status);
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};header.Controls.Add(buttons);
            Add(buttons,PlayButton,"Play",delegate{if(Timeline.Playing)Pause();else Play();});
            Add(buttons,RestartButton,"Restart",delegate{Reset();});
            Add(buttons,PreviousButton,"Previous frame",delegate{Step(-1);});
            Add(buttons,NextButton,"Next frame",delegate{Step(1);});
            Add(buttons,EndButton,"Skip to end",delegate{StopAudio();Timeline.End();SyncControls();});
            canvas.Dock=DockStyle.Fill;Controls.Add(canvas);Controls.Add(header);
            Selector.SelectedIndexChanged+=delegate{if(!changing)SelectClip(Selector.SelectedItem.ToString());};Selector.SelectedIndex=0;
            timer.Interval=16;timer.Tick+=delegate{Tick();};timer.Start();
        }
        static void Add(Control parent,Button button,string text,EventHandler click){button.Text=text;button.AutoSize=true;button.Height=33;button.BackColor=Color.FromArgb(31,56,75);button.FlatStyle=FlatStyle.Flat;button.ForeColor=Color.WhiteSmoke;button.Click+=click;parent.Controls.Add(button);}
        void SelectClip(string name){
            Pause();StopAudio();var next=new List<Bitmap>();
            try{
                var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAT")));
                Color[] colors=PatMovie.ClipColors(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAL")),File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")),false);
                foreach(var frame in movie.Frames)next.Add(new IndexedImage{Width=160,Height=120,Pixels=frame.Pixels,Palette=colors}.Bitmap());
                short[] mixed=null;string soundError="";try{List<SoundCue> cues;mixed=ClipSound.Load(assets,name,movie.Frames.Count,out cues);}catch(Exception e){soundError="Sound unavailable: "+e.Message;}
                var old=frames;frames=next;next=new List<Bitmap>();pcm=mixed;Timeline=new ClipTimeline(frames.Count);clipName=name;audioProblem=soundError;loadProblem="";
                canvas.Image=frames[0];foreach(var image in old)image.Dispose();last=clock.ElapsedMilliseconds;
            }catch(Exception e){foreach(var image in next)image.Dispose();if(Timeline==null)throw;loadProblem="Clip unavailable: "+e.Message;changing=true;Selector.SelectedItem=clipName;changing=false;}
            SyncControls();
        }
        void StopAudio(){try{output.Stop();}catch(Exception e){audioProblem=e.Message;}}
        void StartAudio(){
            if(!soundEnabled||pcm==null)return;
            try{int sample=(int)Math.Min(pcm.Length-1,Timeline.PositionMilliseconds*ClipSound.Rate/1000);output.Start(pcm,sample);lastAudioSample=sample;lastAudioProgress=clock.ElapsedMilliseconds;audioProblem="";}
            catch(Exception e){audioProblem=e.Message+" Continuing silently.";output.Dispose();}
        }
        void Play(){Timeline.Play();last=clock.ElapsedMilliseconds;if(soundEnabled&&pcm!=null){if(output.Active){try{output.Resume();lastAudioProgress=last;}catch(Exception e){audioProblem=e.Message;StopAudio();}}else StartAudio();}SyncControls();}
        bool AudioTick(){
            int sample=output.Samples;long now=clock.ElapsedMilliseconds;
            if(sample>lastAudioSample){lastAudioSample=sample;lastAudioProgress=now;}else if(now-lastAudioProgress>2000)throw new InvalidOperationException("Audio device stopped advancing; continuing silently.");
            bool changed=Timeline.Synchronize((long)sample*1000/ClipSound.Rate);if(Timeline.Completed)StopAudio();return changed;
        }
        void Tick(){
            long now=clock.ElapsedMilliseconds,elapsed=now-last;last=now;if(Timeline==null||!Timeline.Playing)return;
            bool changed=false;
            if(output.Active){try{changed=AudioTick();}catch(Exception e){audioProblem=e.Message+" Continuing silently.";StopAudio();changed=Timeline.Advance(elapsed);SyncControls();}}
            else changed=Timeline.Advance(elapsed);
            if(changed)SyncControls();
        }
        void Step(int direction){Pause();StopAudio();Timeline.Step(direction);SyncControls();}
        void SyncControls(){if(Timeline==null)return;canvas.Image=frames[Timeline.Frame];canvas.Invalidate();PlayButton.Text=Timeline.Playing?"Pause":"Play";title.Text=clipName+" / original firing clips — study library";
            status.Text=Problem!=""?Problem:"Frame "+(Timeline.Frame+1)+" / "+Timeline.Count+" · 25 fps · "+(Timeline.Completed?"Complete":Timeline.Playing?"Playing":"Paused")+" · "+(soundEnabled?"Original cues":"Muted")+" · Battle integration pending";
            PreviousButton.Enabled=Timeline.Frame>0;NextButton.Enabled=Timeline.Frame+1<Timeline.Count;
        }
        public void SetSound(bool enabled){if(enabled==soundEnabled)return;if(output.Active&&Timeline.Playing){try{AudioTick();}catch(Exception e){audioProblem=e.Message;}}StopAudio();soundEnabled=enabled;last=clock.ElapsedMilliseconds;if(Timeline!=null&&Timeline.Playing)StartAudio();SyncControls();}
        public void Pause(){if(Timeline==null)return;if(output.Active){try{output.Pause();if(Timeline.Playing)AudioTick();}catch(Exception e){audioProblem=e.Message;StopAudio();}}Timeline.Pause();SyncControls();}
        public void Reset(){if(Timeline==null)return;StopAudio();Timeline.Reset();last=clock.ElapsedMilliseconds;SyncControls();}
        internal void PreviewFrame(int index){Reset();for(int i=0;i<index;i++)Timeline.Step(1);SyncControls();}
        internal Bitmap CurrentImage {get{return frames[Timeline.Frame];}}
        internal bool AudioActive {get{return output.Active;}}
        protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;timer.Stop();timer.Dispose();output.Dispose();foreach(var image in frames)image.Dispose();}base.Dispose(disposing);}
        sealed class MovieCanvas : Control {
            public Bitmap Image;
            public MovieCanvas(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.FromArgb(4,10,16);}
            protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Image==null)return;float scale=Math.Min(Width/160f,Height/120f);var bounds=new RectangleF((Width-160*scale)/2,(Height-120*scale)/2,160*scale,120*scale);e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(Image,bounds);}
        }
    }
}
