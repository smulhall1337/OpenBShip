using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
namespace FleetLab {
    public sealed class PreferencesPage : UserControl {
        internal readonly PreferencesProgram Program;
        internal readonly PreferencesCanvas Canvas=new PreferencesCanvas();
        internal readonly Button BackButton=new Button(),SaveButton=new Button(),FadeButton=new Button();
        readonly Label note=new Label();readonly Timer timer=new Timer();readonly Stopwatch clock=Stopwatch.StartNew();
        readonly IndexedImage card;readonly List<IndexedImage> frames;Bitmap image;
        long last,elapsed;bool disposed;int selected;
        internal PreferenceValues Values {get;private set;}
        internal bool HasOpened {get;private set;}internal bool Revealing {get;private set;}internal int Factor {get;private set;}
        internal bool CanChangeBattleSettings {get;private set;}internal bool KeyboardMode;
        internal Bitmap CurrentImage {get{return image;}}
        public event Action<PreferenceValues> ChangeRequested;
        public event Action SaveRequested,ReturnRequested;
        public PreferencesPage(string assets){
            Dock=DockStyle.Fill;BackColor=Color.FromArgb(10,22,34);ForeColor=Color.WhiteSmoke;
            card=Assets.Pcx(Path.Combine(assets,"Graphics","E_PREFS.PCX"));
            var reserved=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")));for(int i=0;i<256;i++)if(i<10||i>245)card.Palette[i]=reserved[i];
            frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_PREFS.SPH"),card.Palette);
            Program=PreferencesProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","Preferences.seqbin")),frames);
            Canvas.Owner=this;Canvas.Dock=DockStyle.Fill;Canvas.AccessibleName="Preferences: arrows choose, Enter applies, Escape returns; left/right adjust selected volume";Controls.Add(Canvas);
            var header=new TableLayoutPanel{Dock=DockStyle.Top,Height=112,ColumnCount=1,RowCount=3,Padding=new Padding(10,4,10,2)};
            foreach(int height in new[]{28,40,38})header.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
            header.Controls.Add(new Label{Text="Original Preferences · arrows choose · Enter applies · Escape returns",Dock=DockStyle.Fill},0,0);
            note.Dock=DockStyle.Fill;note.Font=new Font("Segoe UI",9);header.Controls.Add(note,0,1);
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};header.Controls.Add(buttons,0,2);
            Add(buttons,BackButton,"Back",delegate{Back();});Add(buttons,SaveButton,"Save as default",delegate{if(!disposed&&HasOpened&&SaveRequested!=null)SaveRequested();});Add(buttons,FadeButton,"Pause fade",delegate{if(Revealing)Pause();else Resume();});Controls.Add(header);
            timer.Interval=16;timer.Tick+=delegate{long now=clock.ElapsedMilliseconds,dt=now-last;last=now;if(disposed||!Revealing||!Visible)return;elapsed+=dt;bool changed=false;while(elapsed>=40&&Revealing){elapsed-=40;if(Factor==64)Revealing=false;else Factor+=4;changed=true;}if(changed)Render();};timer.Start();Reset();
        }
        static void Add(Control parent,Button b,string text,EventHandler click){b.Text=text;b.AutoSize=true;b.Height=32;b.FlatStyle=FlatStyle.Flat;b.BackColor=Color.FromArgb(31,56,75);b.ForeColor=Color.WhiteSmoke;b.Click+=click;parent.Controls.Add(b);}
        internal void Begin(PreferenceValues values,bool allowed){if(disposed)return;values.Validate();Values=values.Copy();CanChangeBattleSettings=allowed;HasOpened=true;Factor=12;elapsed=0;last=clock.ElapsedMilliseconds;Revealing=true;selected=0;KeyboardMode=false;Canvas.ClearPress();DefaultNote();Render();Canvas.Focus();}
        internal void SetValues(PreferenceValues values,bool allowed){if(disposed)return;values.Validate();bool changed=Values==null||Values.Quality!=values.Quality||Values.Radar!=values.Radar||Values.Difficulty!=values.Difficulty||Values.Volume!=values.Volume||CanChangeBattleSettings!=allowed;Values=values.Copy();CanChangeBattleSettings=allowed;if(changed&&HasOpened)Render();}
        void DefaultNote(){note.Text=CanChangeBattleSettings?"Quality applies before battle; difficulty changes computer targeting and resets deployment. Radar views are not available yet. Save keeps preferences for next time. 0% is silent.":"Battle paused: quality/difficulty are locked. Volume remains adjustable; radar views are not available yet. Save keeps current preferences.";}
        internal void Notice(string text){if(!disposed)note.Text=text;}
        public void Pause(){if(disposed)return;Revealing=false;Canvas.ClearPress();UpdateFade();}
        void Resume(){if(disposed||!HasOpened||Factor==64)return;last=clock.ElapsedMilliseconds;Revealing=true;UpdateFade();Canvas.Focus();}
        internal void FinishReveal(){if(disposed||!HasOpened)return;Factor=64;Revealing=false;Render();}
        void UpdateFade(){FadeButton.Enabled=HasOpened&&Factor<64;FadeButton.Text=Revealing?"Pause fade":"Resume fade";}
        void Render(){if(disposed||!HasOpened)return;var indexed=Program.Compose(card,frames,Values,selected);indexed.Palette=ClassicPalette.FadeDisplay(card.Palette,Factor);Bitmap next=indexed.Bitmap(),previous=image;image=next;Canvas.Invalidate();if(previous!=null)previous.Dispose();UpdateFade();}
        internal void Select(int index,bool keyboard){if(disposed||!HasOpened||index< -1||index>=13)return;selected=index;KeyboardMode=keyboard;Render();}
        internal void Activate(int index){
            if(disposed||!HasOpened||Factor<64||index<0||index>=13)return;Select(index,KeyboardMode);int action=Program.Buttons[index].Action;
            if(action==97){if(SaveRequested!=null)SaveRequested();return;}if(action==99){Back();return;}
            if(action==10){Notice("Credits are not available in this build yet. Your preferences are unchanged.");return;}
            if(!CanChangeBattleSettings&&(action==1||action==2||(action>=5&&action<=8))){Notice("End the current battle or reset deployment before changing quality or difficulty.");return;}
            DefaultNote();var next=PreferencesProgram.Change(Values,action);if(ChangeRequested!=null)ChangeRequested(next);
        }
        internal void Bar(int x){if(disposed||!HasOpened||Factor<64)return;var next=Values.Copy();next.Volume=PreferencesProgram.BarValue(x);DefaultNote();if(ChangeRequested!=null)ChangeRequested(next);}
        void Back(){if(!disposed&&HasOpened&&ReturnRequested!=null)ReturnRequested();}
        internal bool Key(Keys key){
            if(disposed||!HasOpened)return false;if(key==Keys.Escape){Back();return true;}if(key==Keys.P){if(Revealing)Pause();else Resume();return true;}if(Factor<64)return false;
            int[] order={0,1,2,3,4,5,6,7,8,11,10,12};int position=Array.IndexOf(order,selected);if(position<0)position=0;
            if((key==Keys.Left||key==Keys.Right)&&selected==8){var next=Values.Copy();next.Volume=Math.Max(0,Math.Min(100,next.Volume+(key==Keys.Left?-5:5)));KeyboardMode=true;if(ChangeRequested!=null)ChangeRequested(next);return true;}
            if(key==Keys.Up||key==Keys.Down||key==Keys.Left||key==Keys.Right){Select(order[(position+(key==Keys.Up||key==Keys.Left?order.Length-1:1))%order.Length],true);return true;}
            if(key==Keys.Enter||key==Keys.Space){Activate(selected<0?0:selected);return true;}return false;
        }
        public void Reset(){if(disposed)return;HasOpened=false;Revealing=false;Canvas.ClearPress();var previous=image;image=null;Canvas.Invalidate();if(previous!=null)previous.Dispose();}
        protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;timer.Stop();timer.Dispose();HasOpened=Revealing=false;Canvas.ClearPress();var previous=image;image=null;if(previous!=null)previous.Dispose();}base.Dispose(disposing);}
        internal sealed class PreferencesCanvas:Control {
            internal PreferencesPage Owner;int pressed=-1,barX=-1;bool right;
            public PreferencesCanvas(){DoubleBuffered=true;ResizeRedraw=true;TabStop=true;BackColor=Color.Black;}
            internal RectangleF Bounds4x3 {get{float scale=Math.Min(Width/640f,Height/480f);return new RectangleF((Width-640*scale)/2,(Height-480*scale)/2,640*scale,480*scale);}}
            Point? Source(int x,int y){var b=Bounds4x3;if(b.Width<=0||x<b.Left||x>=b.Right||y<b.Top||y>=b.Bottom)return null;return new Point((int)((x-b.X)*640/b.Width),(int)((y-b.Y)*480/b.Height));}
            internal int Hit(int x,int y,bool alternate){var p=Source(x,y);return p.HasValue?Owner.Program.Hit(p.Value.X,p.Value.Y,alternate):-1;}
            internal void ClearPress(){pressed=barX=-1;}
            protected override bool IsInputKey(Keys key){return key==Keys.Up||key==Keys.Down||key==Keys.Left||key==Keys.Right||base.IsInputKey(key);}
            protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);ClearPress();if(!Owner.HasOpened||Owner.Factor<64||(e.Button!=MouseButtons.Left&&e.Button!=MouseButtons.Right))return;Focus();right=e.Button==MouseButtons.Right;var p=Source(e.X,e.Y);if(!right&&p.HasValue&&PreferencesProgram.VolumeBar.Contains(p.Value)){barX=p.Value.X;return;}pressed=Hit(e.X,e.Y,right);}
            protected override void OnMouseUp(MouseEventArgs e){int old=pressed,bar=barX;bool alternate=right;base.OnMouseUp(e);ClearPress();if(e.Button!=(alternate?MouseButtons.Right:MouseButtons.Left)||!Owner.HasOpened||Owner.Factor<64)return;var p=Source(e.X,e.Y);if(bar>=0){if(p.HasValue&&PreferencesProgram.VolumeBar.Contains(p.Value))Owner.Bar(p.Value.X);return;}if(old>=0&&Hit(e.X,e.Y,alternate)==old)Owner.Activate(old);}
            protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!Owner.HasOpened||Owner.Factor<64)return;int hover=Hit(e.X,e.Y,false);if(hover!=Owner.selected||Owner.KeyboardMode)Owner.Select(hover,false);}
            protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);if(!Owner.KeyboardMode)Owner.Select(-1,false);}
            protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)ClearPress();}
            protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Owner==null||Owner.image==null)return;var b=Bounds4x3;e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(Owner.image,b);if(!Owner.CanChangeBattleSettings){using(var brush=new SolidBrush(Color.FromArgb(100,0,0,0)))foreach(var r in new[]{new Rectangle(139,109,338,34),new Rectangle(139,271,338,34)})e.Graphics.FillRectangle(brush,b.X+r.X*b.Width/640,b.Y+r.Y*b.Height/480,r.Width*b.Width/640,r.Height*b.Height/480);}if(Owner.KeyboardMode&&Focused&&Owner.selected>=0){var r=Owner.Program.Buttons[Owner.selected].Bounds;using(var pen=new Pen(Color.Gold,2))e.Graphics.DrawRectangle(pen,b.X+r.X*b.Width/640,b.Y+r.Y*b.Height/480,r.Width*b.Width/640,r.Height*b.Height/480);}}
        }
    }
}
