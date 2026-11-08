using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
namespace FleetLab {
    // Explicit study surface: does not consume shots, change RNG, execute .SEQ,
    // or pretend that the original blocking movie/impact phase is implemented.
    public sealed class ClipStudy : UserControl {
        internal readonly Button PlayButton=new Button(),RestartButton=new Button(),PreviousButton=new Button(),NextButton=new Button(),EndButton=new Button();
        internal readonly ClipTimeline Timeline;
        readonly Label status=new Label();readonly Timer timer=new Timer();readonly Stopwatch clock=Stopwatch.StartNew();
        readonly List<Bitmap> frames=new List<Bitmap>();readonly MovieCanvas canvas=new MovieCanvas();long last;bool disposed;
        public ClipStudy(string assets){
            var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS","BATFIRE1.PAT")));
            Color[] colors=PatMovie.ClipColors(File.ReadAllBytes(Path.Combine(assets,"LWEBS","BATFIRE1.PAL")),File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")),false);
            try{foreach(var frame in movie.Frames)frames.Add(new IndexedImage{Width=160,Height=120,Pixels=frame.Pixels,Palette=colors}.Bitmap());}catch{foreach(var image in frames)image.Dispose();throw;}
            Timeline=new ClipTimeline(frames.Count);Dock=DockStyle.Fill;BackColor=Color.FromArgb(10,22,34);ForeColor=Color.WhiteSmoke;
            var header=new TableLayoutPanel{Dock=DockStyle.Top,Height=106,ColumnCount=1,RowCount=3,Padding=new Padding(12,7,12,2)};
            header.RowStyles.Add(new RowStyle(SizeType.Absolute,25));header.RowStyles.Add(new RowStyle(SizeType.Absolute,25));header.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
            header.Controls.Add(new Label{Text="BATFIRE1 / original firing clip — silent study",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(237,200,128),Font=new Font("Segoe UI",12,FontStyle.Bold)});
            status.Dock=DockStyle.Fill;header.Controls.Add(status);
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};header.Controls.Add(buttons);
            Add(buttons,PlayButton,"Play",delegate{if(Timeline.Playing)Pause();else{Timeline.Play();last=clock.ElapsedMilliseconds;SyncControls();}});
            Add(buttons,RestartButton,"Restart",delegate{Reset();});
            Add(buttons,PreviousButton,"Previous frame",delegate{Timeline.Step(-1);SyncControls();});
            Add(buttons,NextButton,"Next frame",delegate{Timeline.Step(1);SyncControls();});
            Add(buttons,EndButton,"Skip to end",delegate{Timeline.End();SyncControls();});
            canvas.Dock=DockStyle.Fill;Controls.Add(canvas);Controls.Add(header);
            timer.Interval=16;timer.Tick+=delegate{long now=clock.ElapsedMilliseconds,elapsed=now-last;last=now;if(Timeline.Advance(elapsed))SyncControls();};timer.Start();SyncControls();
        }
        static void Add(Control parent,Button button,string text,EventHandler click){button.Text=text;button.AutoSize=true;button.Height=33;button.BackColor=Color.FromArgb(31,56,75);button.FlatStyle=FlatStyle.Flat;button.ForeColor=Color.WhiteSmoke;button.Click+=click;parent.Controls.Add(button);}
        void SyncControls(){canvas.Image=frames[Timeline.Frame];canvas.Invalidate();PlayButton.Text=Timeline.Playing?"Pause":"Play";status.Text="Frame "+(Timeline.Frame+1)+" / "+Timeline.Count+" · 25 fps · "+(Timeline.Completed?"Complete":Timeline.Playing?"Playing":"Paused")+" · Soundtrack and battle sequencing pending";PreviousButton.Enabled=Timeline.Frame>0;NextButton.Enabled=Timeline.Frame+1<Timeline.Count;}
        public void Pause(){Timeline.Pause();SyncControls();}
        public void Reset(){Timeline.Reset();last=clock.ElapsedMilliseconds;SyncControls();}
        internal void PreviewFrame(int index){Reset();for(int i=0;i<index;i++)Timeline.Step(1);SyncControls();}
        internal Bitmap CurrentImage {get{return frames[Timeline.Frame];}}
        protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;timer.Stop();timer.Dispose();foreach(var image in frames)image.Dispose();}base.Dispose(disposing);}
        sealed class MovieCanvas : Control {
            public Bitmap Image;
            public MovieCanvas(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.FromArgb(4,10,16);}
            protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Image==null)return;float scale=Math.Min(Width/160f,Height/120f);var bounds=new RectangleF((Width-160*scale)/2,(Height-120*scale)/2,160*scale,120*scale);e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(Image,bounds);}
        }
    }
}
