using System;using System.IO;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;using System.Collections.Generic;using System.Diagnostics;
namespace FleetLab {
 public sealed class FrontMenu:UserControl {
  internal readonly TitleProgram Program;internal readonly QuitProgram Quit;internal readonly MenuCanvas Canvas=new MenuCanvas();
  internal readonly Button ReplayButton=new Button(),IntroButton=new Button(),FadeButton=new Button(),BackButton=new Button(),QuitButton=new Button(),CancelQuitButton=new Button();
  readonly Label note=new Label();readonly FlowLayoutPanel confirmation=new FlowLayoutPanel();readonly Timer timer=new Timer();readonly Stopwatch clock=Stopwatch.StartNew();
  readonly IndexedImage card,quitCard;readonly List<IndexedImage> frames,quitFrames;Bitmap image;long last,elapsed;bool disposed,quitPending,fadingOut,exitSent;
  public event Action NewBattleRequested,SettingsRequested,QuitRequested,ReturnRequested,IntroRequested;
  internal int Selected{get;private set;}internal int QuitSelected{get;private set;}internal int Factor{get;private set;}internal bool Revealing{get;private set;}internal bool HasOpened{get;private set;}
  internal bool QuitPending{get{return quitPending;}}internal bool FadingOut{get{return fadingOut;}}internal Bitmap CurrentImage{get{return image;}}
  public FrontMenu(string assets){
   Dock=DockStyle.Fill;BackColor=Color.FromArgb(10,22,34);ForeColor=Color.WhiteSmoke;
   card=Assets.Pcx(Path.Combine(assets,"Graphics","E_TITLE.PCX"));quitCard=Assets.Pcx(Path.Combine(assets,"Graphics","E_QUIT.PCX"));
   var reserved=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")));foreach(var c in new[]{card,quitCard})for(int i=0;i<256;i++)if(i<10||i>245)c.Palette[i]=reserved[i];
   frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_TITLE.SPH"),card.Palette);quitFrames=Assets.Sph(Path.Combine(assets,"Graphics","EO_QUIT.SPH"),quitCard.Palette);
   string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");Program=TitleProgram.Read(File.ReadAllBytes(Path.Combine(programs,"MenuEntry.seqbin")),File.ReadAllBytes(Path.Combine(programs,"MenuTitle.seqbin")),frames);Quit=QuitProgram.Read(File.ReadAllBytes(Path.Combine(programs,"MenuQuit.seqbin")),quitFrames);
   Canvas.Dock=DockStyle.Fill;Canvas.Owner=this;Canvas.AccessibleName="Original title menu; arrow keys select, Enter activates";Controls.Add(Canvas);
   var header=new Panel{Dock=DockStyle.Top,Height=96,Padding=new Padding(12,4,12,2)};Controls.Add(header);note.Dock=DockStyle.Fill;header.Controls.Add(note);
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=39,WrapContents=false};header.Controls.Add(buttons);Add(buttons,ReplayButton,"Replay entry",delegate{Begin();});Add(buttons,IntroButton,"Replay intro",delegate{if(!disposed&&!fadingOut&&IntroRequested!=null)IntroRequested();});Add(buttons,FadeButton,"Pause fade",delegate{if(Revealing)Pause();else Resume();});Add(buttons,BackButton,"Return to duel",delegate{Pause();if(ReturnRequested!=null)ReturnRequested();});
   confirmation.AutoSize=true;confirmation.Height=34;confirmation.WrapContents=false;confirmation.Visible=false;Add(confirmation,CancelQuitButton,"Back",delegate{CancelQuit();});Add(confirmation,QuitButton,"Quit",delegate{ConfirmQuit();});buttons.Controls.Add(confirmation);
   timer.Interval=16;timer.Tick+=delegate{long now=clock.ElapsedMilliseconds,dt=now-last;last=now;if(Visible)Advance(dt);};timer.Start();Reset();
  }
  static void Add(Control parent,Button b,string text,EventHandler click){b.Text=text;b.AutoSize=true;b.Height=32;b.BackColor=Color.FromArgb(31,56,75);b.ForeColor=Color.WhiteSmoke;b.FlatStyle=FlatStyle.Flat;b.Click+=click;parent.Controls.Add(b);}
  public void Begin(){if(disposed)return;HasOpened=true;Selected=0;QuitSelected=0;Factor=12;elapsed=0;last=clock.ElapsedMilliseconds;Revealing=true;quitPending=fadingOut=exitSent=false;confirmation.Visible=false;note.Text="Original title artwork. Solo opens recovered Practice setup; Preferences opens original settings.";Canvas.AccessibleName="Original title menu; arrow keys select, Enter activates";Canvas.ClearPress();Render();Canvas.Focus();}
  public void Reset(){if(disposed)return;HasOpened=Revealing=quitPending=fadingOut=exitSent=false;Selected=QuitSelected=0;Factor=64;confirmation.Visible=false;Canvas.ClearPress();var old=image;image=null;Canvas.Invalidate();if(old!=null)old.Dispose();}
  public void Pause(){if(disposed)return;Revealing=false;Canvas.ClearPress();UpdateFade();}
  void Resume(){if(disposed||!HasOpened||Factor==64&&!fadingOut)return;last=clock.ElapsedMilliseconds;Revealing=true;UpdateFade();Canvas.Focus();}
  internal void Advance(long ms){
   if(disposed||!HasOpened||!Revealing)return;if(ms<0)throw new ArgumentOutOfRangeException("ms");elapsed+=Math.Min(ms,500);
   while(elapsed>=40&&Revealing){elapsed-=40;if(fadingOut){if(Factor==0){Revealing=false;Render();if(!exitSent){exitSent=true;if(QuitRequested!=null)QuitRequested();}return;}Factor-=4;}else if(Factor==64)Revealing=false;else Factor+=4;Render();}
  }
  internal void FinishReveal(){if(disposed||fadingOut)return;Factor=64;Revealing=false;Render();}
  void UpdateFade(){FadeButton.Enabled=HasOpened&&(Factor<64||fadingOut)&&!exitSent;FadeButton.Text=Revealing?"Pause fade":"Resume fade";ReplayButton.Enabled=IntroButton.Enabled=!fadingOut;BackButton.Enabled=!fadingOut;CancelQuitButton.Enabled=QuitButton.Enabled=!fadingOut;}
  void Render(){if(disposed||!HasOpened)return;if(quitPending){Canvas.AccessibleName="Quit Battleship confirmation; "+(QuitSelected==0?"Back":"Quit")+" selected";if(!fadingOut)note.Text="Original Quit screen. "+(QuitSelected==0?"Back":"Quit")+" is selected. Left/right selects; Enter confirms; Escape returns.";}var indexed=quitPending?QuitProgram.Compose(quitCard,quitFrames,Quit,QuitSelected):TitleProgram.Compose(card,frames,Program,Selected);indexed.Palette=ClassicPalette.FadeDisplay((quitPending?quitCard:card).Palette,Factor);Bitmap next=indexed.Bitmap(),previous=image;image=next;Canvas.Invalidate();if(previous!=null)previous.Dispose();UpdateFade();}
  internal void Select(int index){if(disposed||!HasOpened||fadingOut)return;if(quitPending){if(index<0||index>=2)return;QuitSelected=index;}else{if(index<0||index>=Program.Buttons.Count)return;Selected=index;}Render();}
  internal int Hit(int x,int y){return quitPending?Quit.Hit(x,y):Program.Hit(x,y);}
  internal bool Key(Keys key){if(disposed||!HasOpened||Factor<64||fadingOut)return false;if(quitPending){if(key==Keys.Escape){CancelQuit();return true;}if(key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down){Select(1-QuitSelected);return true;}if(key==Keys.Enter||key==Keys.Space){Activate(QuitSelected);return true;}return false;}if(key==Keys.Up||key==Keys.Down){Select((Selected+(key==Keys.Up?3:1))%4);return true;}if(key==Keys.Enter||key==Keys.Space){Activate(Selected);return true;}return false;}
  internal void Activate(int index){if(disposed||!HasOpened||Factor<64||fadingOut)return;if(quitPending){if(index==0)CancelQuit();else if(index==1)ConfirmQuit();return;}if(index<0||index>=4)return;Select(index);switch(Program.Buttons[index].Action){case 1:if(NewBattleRequested!=null)NewBattleRequested();break;case 2:note.Text="Original multiplayer is not implemented. Use Solo > Classic for the reconstructed duel.";break;case 5:if(SettingsRequested!=null)SettingsRequested();break;case 4:quitPending=true;QuitSelected=0;confirmation.Visible=true;Canvas.ClearPress();note.Text="Original Quit screen. Back is selected. Left/right selects; Enter confirms; Escape returns.";Canvas.AccessibleName="Quit Battleship confirmation; Back selected; Left/right selects, Enter confirms, Escape returns";Render();Canvas.Focus();break;}}
  void CancelQuit(){if(disposed||!quitPending||fadingOut)return;quitPending=false;confirmation.Visible=false;Canvas.AccessibleName="Original title menu; arrow keys select, Enter activates";Selected=0;Factor=64;Revealing=false;Canvas.ClearPress();note.Text="Returned to original title. Solo opens recovered Practice setup; Preferences opens original settings.";Render();Canvas.Focus();}
  void ConfirmQuit(){if(disposed||!quitPending||fadingOut)return;QuitSelected=1;fadingOut=Revealing=true;Factor=64;elapsed=0;last=clock.ElapsedMilliseconds;Canvas.ClearPress();note.Text="Quitting Battleship...";Render();Canvas.Focus();}
  protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;timer.Stop();timer.Dispose();Revealing=HasOpened=quitPending=fadingOut=false;Canvas.ClearPress();var old=image;image=null;if(old!=null)old.Dispose();}base.Dispose(disposing);}
  internal sealed class MenuCanvas:Control {
   internal FrontMenu Owner;int pressed=-1;
   public MenuCanvas(){DoubleBuffered=true;ResizeRedraw=true;TabStop=true;BackColor=Color.Black;}
   internal RectangleF Bounds4x3{get{float scale=Math.Min(Width/640f,Height/480f);return new RectangleF((Width-640*scale)/2,(Height-480*scale)/2,640*scale,480*scale);}}
   internal int Hit(int x,int y){var b=Bounds4x3;if(b.Width<=0||x<b.Left||y<b.Top||x>=b.Right||y>=b.Bottom)return -1;return Owner.Hit((int)((x-b.X)*640/b.Width),(int)((y-b.Y)*480/b.Height));}
   internal void ClearPress(){pressed=-1;}
   protected override bool IsInputKey(Keys key){return key==Keys.Up||key==Keys.Down||key==Keys.Left||key==Keys.Right||base.IsInputKey(key);}
   protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left&&Owner.HasOpened&&Owner.Factor==64&&!Owner.fadingOut){Focus();pressed=Hit(e.X,e.Y);}}
   protected override void OnMouseUp(MouseEventArgs e){int at=pressed;base.OnMouseUp(e);ClearPress();if(e.Button==MouseButtons.Left&&at>=0&&Hit(e.X,e.Y)==at)Owner.Activate(at);}
   protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)ClearPress();}
   protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Owner==null||Owner.image==null)return;e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(Owner.image,Bounds4x3);}
  }
 }
}


