using System;using System.IO;using System.Drawing;using System.Drawing.Drawing2D;using System.Diagnostics;using System.Windows.Forms;
namespace FleetLab {
    public sealed class EndingPlayer : UserControl {
        internal readonly ComboBox Selector=new ComboBox();internal readonly Button PlayButton=new Button(),RestartButton=new Button(),BackButton=new Button();internal readonly ClipStudy.EpochButton SkipButton=new ClipStudy.EpochButton();
        internal readonly ClipStudy Movie;internal EndingTimeline Timeline {get;private set;}
        public event Action ReturnRequested;internal event Action MenuRequested;bool menuSent;
        readonly string assets;readonly Timer timer=new Timer();readonly Stopwatch clock=Stopwatch.StartNew();
        readonly Label status=new Label();readonly EndingCanvas canvas=new EndingCanvas();
        IndexedImage card;Bitmap owned;EndingPhase shownPhase,uiPhase;int shownFactor=-1;long last;bool loading,disposed,sound;string problem="";
        public string Problem {get{return problem;}}
        internal Bitmap CurrentImage {get{return canvas.Image;}}
        public EndingPlayer(string root):this(root,null){}
        internal EndingPlayer(string root,IClipOutput backend){assets=root;Dock=DockStyle.Fill;BackColor=Color.FromArgb(10,22,34);ForeColor=Color.WhiteSmoke;
            Movie=backend==null?new ClipStudy(root):new ClipStudy(root,backend);Movie.ConfigureBattle();Movie.FrameChanged+=delegate{if(!disposed&&!loading&&Timeline!=null&&Timeline.Phase==EndingPhase.Movie)Sync();};
            Movie.PlaybackCompleted+=delegate{if(!disposed&&Timeline!=null&&Timeline.CompleteMovie())Sync();};
            var header=new TableLayoutPanel{Dock=DockStyle.Top,Height=136,RowCount=3,ColumnCount=1,Padding=new Padding(12,8,12,2)};
            header.RowStyles.Add(new RowStyle(SizeType.Absolute,31));header.RowStyles.Add(new RowStyle(SizeType.Absolute,42));header.RowStyles.Add(new RowStyle(SizeType.Absolute,45));
            Selector.DropDownStyle=ComboBoxStyle.DropDownList;Selector.Items.AddRange(MovieCatalog.Endings);Selector.Width=190;Selector.AccessibleName="Result sequence";header.Controls.Add(Selector);Selector.SelectedIndex=0;
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};header.Controls.Add(buttons);
            Add(buttons,PlayButton,"Play sequence",delegate{if(Timeline==null||Timeline.Phase==EndingPhase.Complete)Start();else if(Timeline.Playing)Pause();else{Timeline.Resume();if(Timeline.Phase==EndingPhase.Movie){if(Movie.Timeline.Completed)Timeline.CompleteMovie();else Movie.ResumePlayback();}last=clock.ElapsedMilliseconds;Sync();}});
            Add(buttons,RestartButton,"Restart",delegate{Start();});Add(buttons,SkipButton,"Skip stage",delegate{if(Timeline!=null&&Timeline.Playing){if(Timeline.Phase==EndingPhase.Movie){Movie.StopAtCurrent();if(Timeline.Phase==EndingPhase.Movie)Timeline.CompleteMovie();}else Timeline.SkipStage();Sync();}});
            Add(buttons,BackButton,"Return to duel",delegate{Pause();if(ReturnRequested!=null)ReturnRequested();});status.Dock=DockStyle.Fill;status.AutoEllipsis=true;header.Controls.Add(status);
            canvas.Dock=DockStyle.Fill;Controls.Add(canvas);Controls.Add(header);timer.Interval=16;timer.Tick+=delegate{if(disposed)return;long now=clock.ElapsedMilliseconds,dt=now-last;last=now;if(Timeline!=null&&Timeline.Advance(dt))Sync();};timer.Start();Sync();
        }
        static void Add(Control parent,Button button,string text,EventHandler action){button.Text=text;button.AutoSize=true;button.Height=33;button.BackColor=Color.FromArgb(31,56,75);button.ForeColor=Color.WhiteSmoke;button.FlatStyle=FlatStyle.Flat;button.Click+=action;parent.Controls.Add(button);}
        public void StartOutcome(bool victory){if(disposed)return;Selector.SelectedItem=victory?"VICTORY":"LOSE";Start();}
        void Start(){if(disposed)return;Cancel();try{
            string name=Selector.SelectedItem.ToString();var program=EndingProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs",name+".seqbin")));if(program.Movie!=name)throw new InvalidDataException("Ending program name mismatch");
            var original=Assets.Pcx(Path.Combine(assets,"Graphics",program.Card));if(original.Width!=640||original.Height<480||original.Height>512)throw new InvalidDataException("Unsupported result card geometry");
            var colors=(Color[])original.Palette.Clone();var reserved=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")));for(int i=0;i<256;i++)if(i<10||i>245)colors[i]=reserved[i];byte[] pixels=new byte[640*480];Array.Copy(original.Pixels,pixels,pixels.Length);card=new IndexedImage{Width=640,Height=480,Pixels=pixels,Palette=colors};Timeline=new EndingTimeline(program);Timeline.Start();last=clock.ElapsedMilliseconds;shownFactor=-1;Sync();
            }catch(Exception e){Fail(e.Message);}}
        void Fail(string message){problem=message;if(Timeline!=null)Timeline.Pause();Movie.StopAtCurrent();status.Text="Sequence unavailable: "+message+". Use Restart or Return to duel.";PlayButton.Enabled=SkipButton.Enabled=false;Selector.Enabled=true;}
        public void SetSound(bool enabled){if(disposed)return;sound=enabled;Movie.SetSound(enabled);}
        public void SetLevel(int percent){if(disposed)return;Movie.SetLevel(percent);}
        public void Pause(){if(disposed)return;if(Timeline!=null)Timeline.Pause();Movie.Pause();Sync();}
        public void Cancel(){if(disposed)return;menuSent=false;Timeline=null;card=null;problem="";Movie.CancelPlayback();ShowImage(null,null);shownFactor=-1;Sync();}
        void ShowImage(Bitmap image,Bitmap owner){Bitmap previous=owned;owned=owner;canvas.Image=image;canvas.Invalidate();if(previous!=null)previous.Dispose();}
        void Render(){if(Timeline==null){ShowImage(null,null);return;}if(Timeline.Phase==EndingPhase.Movie){ShowImage(Movie.CurrentImage,null);return;}
            if(Timeline.Phase==EndingPhase.Complete){ShowImage(null,null);return;}
            if(shownPhase==Timeline.Phase&&shownFactor==Timeline.Factor&&owned!=null)return;
            Bitmap next=Timeline.Phase==EndingPhase.MovieFade?Movie.FadedFrame(Timeline.Factor):new IndexedImage{Width=card.Width,Height=card.Height,Pixels=card.Pixels,Palette=ClassicPalette.FadeDisplay(card.Palette,Timeline.Factor)}.Bitmap();ShowImage(next,next);shownPhase=Timeline.Phase;shownFactor=Timeline.Factor;
        }
        void Sync(){if(disposed)return;bool movie=Timeline!=null&&Timeline.Phase==EndingPhase.Movie;
            EndingPhase phase=Timeline==null?EndingPhase.Idle:Timeline.Phase;if(phase!=uiPhase){uiPhase=phase;SkipButton.Epoch++;}
            if(movie&&shownPhase!=EndingPhase.Movie){loading=true;try{if(!Movie.LoadBattle(Selector.SelectedItem.ToString())){Fail(Movie.Problem);return;}Movie.SetSound(sound);}finally{loading=false;}shownPhase=EndingPhase.Movie;if(Timeline.Playing)Movie.ResumePlayback();}
            Render();bool active=Timeline!=null&&Timeline.Phase!=EndingPhase.Complete;Selector.Enabled=!active;PlayButton.Enabled=problem=="";SkipButton.Enabled=active&&Timeline.Playing&&problem=="";PlayButton.Text=active?(Timeline.Playing?"Pause":"Resume"):"Play sequence";
            status.Text=problem!=""?"Sequence unavailable: "+problem:Timeline==null?"Original result card, five-second hold and ending movie. Choose a result and play.":Timeline.Phase==EndingPhase.Complete?"Sequence complete. Return to duel or replay.":Timeline.Phase==EndingPhase.Movie?"Ending movie · 20 fps · "+(Timeline.Playing?"Playing":"Paused"):Timeline.Phase==EndingPhase.Hold?"Result card · "+(Timeline.RemainingHold/1000.0).ToString("0.0")+" seconds remaining · "+(Timeline.Playing?"Playing":"Paused"):"Result transition · "+(Timeline.Playing?"Playing":"Paused");
            if(movie&&Movie.Problem!="")status.Text+=" · "+Movie.Problem;
            if(Timeline!=null&&Timeline.Phase==EndingPhase.Complete&&!menuSent){menuSent=true;if(MenuRequested!=null)MenuRequested();}
        }
        protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;timer.Stop();timer.Dispose();if(Timeline!=null)Timeline.Pause();Timeline=null;card=null;canvas.Image=null;if(owned!=null)owned.Dispose();owned=null;Movie.Dispose();}base.Dispose(disposing);}
        sealed class EndingCanvas:Control{public Bitmap Image;public EndingCanvas(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.Black;}protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Image==null)return;float s=Math.Min(Width/(float)Image.Width,Height/(float)Image.Height);e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(Image,new RectangleF((Width-Image.Width*s)/2,(Height-Image.Height*s)/2,Image.Width*s,Image.Height*s));}}
    }
}
