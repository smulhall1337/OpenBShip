using System;using System.IO;using System.Drawing;using System.Drawing.Drawing2D;using System.Diagnostics;using System.Windows.Forms;
namespace FleetLab {
    public sealed class EntryPlayer:UserControl {
        internal readonly EntryCanvas Canvas=new EntryCanvas();internal readonly Button PauseButton=new Button(),BackButton=new Button();
        internal EntryTimeline Timeline {get;private set;}internal Bitmap CurrentImage {get{return image;}}internal bool HasOpened {get{return Timeline!=null;}}
        public event Action Ready,ReturnRequested;
        readonly EntryProgram program;readonly IndexedImage card;readonly Timer timer=new Timer();readonly Stopwatch clock=Stopwatch.StartNew();Bitmap image;long last;bool disposed,dispatched;
        public EntryPlayer(string assets){Dock=DockStyle.Fill;BackColor=Color.FromArgb(10,22,34);ForeColor=Color.WhiteSmoke;
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");program=EntryProgram.Read(File.ReadAllBytes(Path.Combine(programs,"BattleEntry.seqbin")),-1);
            card=FleetProgram.Viewport(Assets.Pcx(Path.Combine(assets,"Graphics",EntryProgram.Card)));var reserved=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")));for(int i=0;i<256;i++)if(i<10||i>245)card.Palette[i]=reserved[i];
            Canvas.Owner=this;Canvas.Dock=DockStyle.Fill;Canvas.AccessibleName="Preparing Classic battle; P pauses or resumes; Escape returns to fleet";Controls.Add(Canvas);
            var header=new Panel{Dock=DockStyle.Top,Height=96,Padding=new Padding(12,4,12,2)};Controls.Add(header);header.Controls.Add(new Label{Dock=DockStyle.Fill,Text="Preparing your battle. Focus or tab changes pause this transition; Resume continues."});var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=39};header.Controls.Add(bar);Add(bar,PauseButton,"Pause entry (P)",delegate{if(Timeline!=null&&Timeline.Playing)Pause();else Resume();Canvas.Focus();});Add(bar,BackButton,"Return to fleet",delegate{Back();});
            timer.Interval=16;timer.Tick+=delegate{long now=clock.ElapsedMilliseconds,dt=now-last;last=now;if(!disposed&&Visible)Advance(dt);};timer.Start();Sync();
        }
        static void Add(Control p,Button b,string text,EventHandler action){b.Text=text;b.AutoSize=true;b.Height=32;b.BackColor=Color.FromArgb(31,56,75);b.ForeColor=Color.WhiteSmoke;b.FlatStyle=FlatStyle.Flat;b.Click+=action;p.Controls.Add(b);}
        public void Begin(){if(disposed)return;Reset();Timeline=new EntryTimeline(program);last=clock.ElapsedMilliseconds;Render();Sync();Canvas.Focus();}
        public void Pause(){if(disposed)return;if(Timeline!=null)Timeline.Pause();Sync();}
        public void Resume(){if(disposed||Timeline==null)return;last=clock.ElapsedMilliseconds;Timeline.Resume();Sync();}
        public void Reset(){if(disposed)return;Timeline=null;dispatched=false;var old=image;image=null;Canvas.Invalidate();if(old!=null)old.Dispose();Sync();}
        internal void Advance(long milliseconds){if(disposed||Timeline==null)return;var active=Timeline;if(active.Advance(milliseconds))Render();Sync();if(Timeline==active&&active.Complete&&!dispatched){dispatched=true;if(Ready!=null)Ready();}}
        internal bool Key(Keys key){if(disposed||Timeline==null)return false;if(key==Keys.Escape){Back();return true;}if(key==Keys.P){if(Timeline.Playing)Pause();else Resume();return true;}if(key==Keys.Enter||key==Keys.Space){if(!Timeline.Playing)Resume();return true;}return false;}
        void Back(){if(disposed)return;Pause();if(ReturnRequested!=null)ReturnRequested();}
        void Sync(){if(disposed)return;PauseButton.Enabled=Timeline!=null&&!Timeline.Complete;PauseButton.Text=Timeline!=null&&Timeline.Playing?"Pause entry (P)":"Resume entry (P)";BackButton.Enabled=Timeline!=null;}
        void Render(){if(disposed||Timeline==null)return;var frame=new IndexedImage{Width=640,Height=480,Pixels=card.Pixels,Palette=ClassicPalette.FadeDisplay(card.Palette,Timeline.Factor)};Bitmap next=frame.Bitmap();var old=image;image=next;Canvas.Invalidate();if(old!=null)old.Dispose();}
        protected override void Dispose(bool disposing){if(disposing&&!disposed){timer.Stop();timer.Dispose();Reset();disposed=true;}base.Dispose(disposing);}
        internal sealed class EntryCanvas:Control {
            internal EntryPlayer Owner;public EntryCanvas(){DoubleBuffered=true;ResizeRedraw=true;TabStop=true;BackColor=Color.Black;}
            protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Owner==null||Owner.image==null)return;float scale=Math.Min(Width/640f,Height/480f);e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(Owner.image,new RectangleF((Width-640*scale)/2,(Height-480*scale)/2,640*scale,480*scale));}
        }
    }
}
