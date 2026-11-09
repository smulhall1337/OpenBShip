using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Media;
using System.Diagnostics;
namespace FleetLab {
    public sealed class MainForm : Form {
        public Game State;
        readonly string assets,settingsPath;
        readonly BoardView board=new BoardView();
        Bitmap backdropForeground;ClassicHud hudImages;
        readonly MapView map=new MapView();
        readonly Timer opponentTimer=new Timer();
        readonly Timer visualTimer=new Timer();
        readonly Stopwatch visualTime=Stopwatch.StartNew();
        readonly EffectClock effectClock=new EffectClock();
        long visualLast;
        readonly Audio sound;
        readonly Label phase=new Label(),enemy=new Label();
        readonly LinkLabel hint=new LinkLabel{LinkArea=new LinkArea(0,0)};
        readonly ListBox firingFleet=new ListBox();
        bool updatingFleet;
        readonly TextBox history=new TextBox();
        readonly ToolStripStatusLabel status=new ToolStripStatusLabel();
        readonly TabControl tabs=new TabControl();
        readonly ClipStudy clip,battleMovie;
        readonly EndingPlayer ending;readonly FrontMenu front;readonly SoloSetup solo;readonly FleetSetup fleet;readonly EntryPlayer entry;
        readonly TableLayoutPanel battleField=new TableLayoutPanel();
        PendingShot shownPending;ShotStage shownStage;Game shownGame;
        bool loadingBattle,advancingBattle,battleFailure;
        bool validationFailMovieLoad; // Only --ui-check injects a failed loader; no user setting.
        readonly ToolStripMenuItem movies=new ToolStripMenuItem("Battle movies (experimental)");
        readonly ToolStripMenuItem highMovies=new ToolStripMenuItem("High-detail original movies (320×240)");
        readonly ComboBox rules=new ComboBox();
        readonly ComboBox opponent=new ComboBox();
        readonly ContextMenuStrip displayMenu=new ContextMenuStrip();
        readonly Button displayButton=new Button();
        readonly ToolStripMenuItem acceptedColors=new ToolStripMenuItem("Colors: v0.4 appearance"),recoveredColors=new ToolStripMenuItem("Colors: recovered gameplay"),motion=new ToolStripMenuItem("Animated damage effects");
        readonly Button auto=new Button(),clear=new Button(),rotate=new Button(),start=new Button(),pause=new Button(),audio=new Button();
        readonly HashSet<Keys> down=new HashSet<Keys>();
        Bitmap art;
        readonly List<Bitmap> rosterImages=new List<Bitmap>();
        ClassicShips shipImages;
        ClassicDamage damageImages;ClassicSpriteFont messageFont;
        readonly IndexedImage classicImage;
        readonly DisplaySettings preferences;
        int seed=Environment.TickCount;
        bool disposed,previewMode;
        bool changingOpponent;
        int recoveredOpponent;
        readonly Color panel=Color.FromArgb(20,36,52),gold=Color.FromArgb(237,200,128);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
        // --ui-check only: dispatch pointer messages to this form's own canvas.
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window,int message,IntPtr w,IntPtr l);
        public MainForm(string assetDir) {
            assets=assetDir;
            settingsPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"UserData","settings.txt");
            preferences=DisplaySettings.Load(settingsPath);
            classicImage=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));
            board.Messages=new ClassicMessages(File.ReadAllBytes(Path.Combine(assets,"Graphics","ENGLISH.TXT")),File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","MessageShipNames.seqbin")));
            RebuildArt(preferences.RecoveredColors);
            sound=new Audio(assets);
            sound.Enabled=preferences.Sound;
            battleMovie=new ClipStudy(assets,preferences.HighMovies);battleMovie.ConfigureBattle();battleMovie.SetSound(sound.Enabled);battleMovie.Visible=false;
            battleMovie.PlaybackCompleted+=delegate{if(!loadingBattle&&!State.Paused)CompleteBattleStage();};
            battleMovie.SkipRequested+=delegate{SkipBattleStage();};
            battleMovie.FrameChanged+=delegate{SyncBackdropFrame();};
            ending=new EndingPlayer(assets);ending.SetSound(sound.Enabled);ending.ReturnRequested+=delegate{tabs.SelectedIndex=0;};
            front=new FrontMenu(assets);front.NewBattleRequested+=delegate{OpenSolo();};front.SettingsRequested+=delegate{displayMenu.Show(displayButton,0,displayButton.Height);};front.ReturnRequested+=delegate{tabs.SelectedIndex=0;};front.QuitRequested+=delegate{Close();};
            solo=new SoloSetup(assets);solo.Visible=false;solo.StartClassicRequested+=delegate{OpenFleet();};solo.ReturnRequested+=delegate{solo.Pause();solo.Visible=false;front.Visible=true;front.Begin();SuppressHeldControls();};
            fleet=new FleetSetup(assets);fleet.Visible=false;fleet.ReturnRequested+=delegate{fleet.Reset();fleet.Visible=false;solo.Visible=true;solo.BringToFront();solo.Canvas.Focus();SuppressHeldControls();UpdateView();};entry=new EntryPlayer(assets);entry.Visible=false;entry.Ready+=delegate{CommitFleet();};entry.ReturnRequested+=delegate{entry.Reset();entry.Visible=false;fleet.Visible=true;fleet.BringToFront();fleet.Canvas.Focus();SuppressHeldControls();UpdateView();};fleet.StartRequested+=delegate{OpenEntry();};
            ending.MenuRequested+=delegate{if(!disposed&&tabs.SelectedIndex==3){entry.Reset();entry.Visible=false;fleet.Reset();fleet.Visible=false;solo.Reset();solo.Visible=false;front.Visible=true;front.Reset();tabs.SelectedIndex=4;}};
            Text="Battleship Fleet Lab 0.20 — Original battle messages";
            ClientSize=new Size(1220,840);
            MinimumSize=new Size(1040,780);
            StartPosition=FormStartPosition.CenterScreen;
            BackColor=Color.FromArgb(10,22,34);
            ForeColor=Color.WhiteSmoke;
            Font=new Font("Segoe UI",10);
            KeyPreview=true;
            var layout=new TableLayoutPanel {
                Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(0),BackColor=BackColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,82));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,26));
            Controls.Add(layout);
            var header=new TableLayoutPanel {
                Dock=DockStyle.Fill,Padding=new Padding(18,10,18,8),BackColor=panel,ColumnCount=2,RowCount=2
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,122));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute,40));header.RowStyles.Add(new RowStyle(SizeType.Absolute,24));
            var title=new Label {
                Text="BATTLESHIP / FLEET LAB",Font=new Font("Segoe UI",23,FontStyle.Bold),ForeColor=gold,Dock=DockStyle.Top,Height=40
            };
            var subtitle=new Label {
                Text="Recovered Classic duel · original cinematic HUD · low/high detail · translucent boards",Dock=DockStyle.Bottom,Height=22,ForeColor=Color.LightSteelBlue
            };
            header.Controls.Add(title,0,0);
            header.Controls.Add(subtitle,0,1);
            var display=displayButton;display.Text="Display";display.Width=105;display.Height=33;display.Anchor=AnchorStyles.Top|AnchorStyles.Right;display.FlatStyle=FlatStyle.Flat;display.BackColor=panel;display.ForeColor=gold;
            header.Controls.Add(display,1,0);
            display.Click+=delegate {displayMenu.Show(display,0,display.Height);};
            displayMenu.Items.Add(acceptedColors);displayMenu.Items.Add(recoveredColors);displayMenu.Items.Add(new ToolStripSeparator());displayMenu.Items.Add(motion);
            displayMenu.Items.Add(new ToolStripSeparator());displayMenu.Items.Add(movies);
            movies.Click+=delegate{if(State.Phase!=Phase.Placement&&State.Phase!=Phase.Finished)return;preferences.MovieTurns=!preferences.MovieTurns;SavePreferences();NewGame();};
            displayMenu.Items.Add(highMovies);highMovies.ToolTipText="Original higher-detail assets. Change before battle; available in the clip library too.";
            highMovies.Click+=delegate{SetMovieDetail(!preferences.HighMovies,true);};
            acceptedColors.Click+=delegate {SelectColors(false,true);};
            recoveredColors.Click+=delegate {SelectColors(true,true);};
            motion.Click+=delegate {preferences.Motion=!preferences.Motion;SavePreferences();UpdateDisplayChecks();};
            displayMenu.Opening+=delegate {
                if(clip!=null)clip.Pause();if(ending!=null)ending.Pause();if(front!=null)front.Pause();if(solo!=null)solo.Pause();if(fleet!=null)fleet.Pause();if(entry!=null)entry.Pause();
                if(State!=null&&(State.Phase==Phase.PlayerTurn||State.Phase==Phase.OpponentTurn))State.Paused=true;
                UpdateDisplayChecks();if(State!=null)UpdateView();
            };
            displayMenu.Closed+=delegate {SuppressHeldControls();};
            layout.Controls.Add(header,0,0);
            var toolbar=new FlowLayoutPanel {
                Dock=DockStyle.Fill,Padding=new Padding(10,4,10,4),BackColor=panel,WrapContents=false
            };
            layout.Controls.Add(toolbar,0,1);
            Button(toolbar,auto,"Auto-place",delegate {
                State.AutoPlace();
                UpdateView();
            });
            Button(toolbar,clear,"Clear fleet",delegate {
                State.ClearPlacement();
                UpdateView();
            });
            Button(toolbar,rotate,"Rotate (R)",delegate {
                Rotate();
            });
            Button(toolbar,start,"Start battle",delegate {
                if(State.Start())UpdateView();
            });
            Button(toolbar,pause,"Pause (P)",delegate {
                State.TogglePause();
                UpdateView();
            });
            Button(toolbar,new Button(),"New battle (F2)",delegate {
                NewGame();
            });
            Button(toolbar,audio,"Sound",delegate {
                sound.Enabled=!sound.Enabled;clip.SetSound(sound.Enabled);battleMovie.SetSound(sound.Enabled);ending.SetSound(sound.Enabled);
                preferences.Sound=sound.Enabled;
                SavePreferences();
                UpdateView();
            });
            opponent.DropDownStyle=ComboBoxStyle.DropDownList;
            opponent.Width=148;
            opponent.Margin=new Padding(3,3,3,0);
            opponent.Items.AddRange(new object[]{"Original target 1","Original target 2","Original target 3","Original target 4","Prototype pursuit"});
            opponent.SelectedIndex=0;
            toolbar.Controls.Add(opponent);
            opponent.SelectedIndexChanged+=delegate {
                if(changingOpponent)return;
                recoveredOpponent=opponent.SelectedIndex;
                NewGame();
            };
            Button(toolbar,new Button(),"Quit",delegate {
                Close();
            });
            tabs.Dock=DockStyle.Fill;
            var play=new TabPage("Classic duel") {
                BackColor=BackColor
            };
            var inspect=new TabPage("Recovered maps") {
                BackColor=BackColor
            };
            tabs.TabPages.Add(play);
            tabs.TabPages.Add(inspect);
            layout.Controls.Add(tabs,0,2);
            var sidebar=new TableLayoutPanel {
                Dock=DockStyle.Right,Width=267,Padding=new Padding(14),ColumnCount=1,RowCount=7,BackColor=panel
            };
            foreach(int height in new[]{32,28,44,92,140,38})sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
            sidebar.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            board.Dock=DockStyle.Fill;
            board.Art=art;
            board.Roster=rosterImages.ToArray();
            board.Ships=shipImages;
            board.Damage=damageImages;
            battleField.Dock=DockStyle.Fill;battleField.ColumnCount=1;battleField.RowCount=2;battleField.RowStyles.Add(new RowStyle(SizeType.Percent,100));battleField.RowStyles.Add(new RowStyle(SizeType.Absolute,0));board.Margin=Padding.Empty;battleMovie.Margin=Padding.Empty;battleMovie.Dock=DockStyle.Fill;battleField.Controls.Add(board,0,0);battleField.Controls.Add(battleMovie,0,1);
            play.Controls.Add(battleField);
            play.Controls.Add(sidebar);
            rules.Width=224;
            rules.DropDownStyle=ComboBoxStyle.DropDownList;
            rules.Items.AddRange(new object[] {"Recovered Classic (19 cells)","v0.1 prototype (17 cells)"});
            rules.SelectedIndex=0;
            sidebar.Controls.Add(rules);
            AddLabel(sidebar,new Label {Text="Settings changes reset deployment."},25,9,Color.LightSteelBlue);
            rules.SelectedIndexChanged+=delegate {
                changingOpponent=true;
                opponent.SelectedIndex=rules.SelectedIndex==1?4:recoveredOpponent;
                changingOpponent=false;
                NewGame();
            };
            AddLabel(sidebar,phase,38,16,gold);
            AddLabel(sidebar,hint,82,10,Color.LightSteelBlue);hint.LinkColor=gold;hint.ActiveLinkColor=Color.White;
            hint.LinkClicked+=delegate{if(State.Phase==Phase.Finished){tabs.SelectedIndex=3;ending.StartOutcome(State.Enemy.AllSunk);}};
            firingFleet.Dock=DockStyle.Fill;firingFleet.IntegralHeight=false;
            firingFleet.BackColor=panel;firingFleet.ForeColor=Color.WhiteSmoke;
            firingFleet.BorderStyle=BorderStyle.FixedSingle;firingFleet.Font=new Font("Segoe UI",10);
            firingFleet.AccessibleName="Your firing ship";
            firingFleet.KeyDown+=delegate(object sender,KeyEventArgs e) {
                if(!State.CanSelectShooter)return;
                int direction=e.KeyCode==Keys.Up||e.KeyCode==Keys.Home?-1:e.KeyCode==Keys.Down||e.KeyCode==Keys.End?1:0;
                if(direction==0)return;
                int candidate=e.KeyCode==Keys.Home?0:e.KeyCode==Keys.End?State.Player.Ships.Count-1:State.PlayerShooter+direction;
                if(e.KeyCode==Keys.Home)direction=1;if(e.KeyCode==Keys.End)direction=-1;
                while(candidate>=0&&candidate<State.Player.Ships.Count&&State.Player.Ships[candidate].Sunk)candidate+=direction;
                if(candidate>=0&&candidate<State.Player.Ships.Count)State.SelectShooter(candidate);
                UpdateView();e.Handled=true;e.SuppressKeyPress=true;
            };
            firingFleet.SelectedIndexChanged+=delegate {
                if(updatingFleet||State==null)return;
                State.SelectShooter(firingFleet.SelectedIndex);UpdateView();
            };
            sidebar.Controls.Add(firingFleet);
            AddLabel(sidebar,enemy,36,10,gold);
            history.Width=224;
            history.Dock=DockStyle.Fill;
            history.Multiline=true;
            history.ReadOnly=true;
            history.BorderStyle=BorderStyle.None;
            history.BackColor=panel;
            history.ForeColor=Color.LightSteelBlue;
            history.ScrollBars=ScrollBars.Vertical;
            history.Font=new Font("Segoe UI",9);
            sidebar.Controls.Add(history);
            var mapSelect=new ComboBox {
                DropDownStyle=ComboBoxStyle.DropDownList,Width=180
            };
            mapSelect.Items.AddRange(new object[] {
                "BS201.SCN","BS202.SCN","BS204.SCN"
            });
            var mapHeader=new FlowLayoutPanel {
                Dock=DockStyle.Top,Height=42,Padding=new Padding(12,7,0,0)
            };
            mapHeader.Controls.Add(mapSelect);
            mapHeader.Controls.Add(new Label {
                Text="Read-only scenario inspection",AutoSize=true,Padding=new Padding(12,4,0,0)
            });
            map.Dock=DockStyle.Fill;
            inspect.Controls.Add(map);
            inspect.Controls.Add(mapHeader);
            mapSelect.SelectedIndexChanged+=delegate {
                try {
                    map.Scene=Scenario.Load(Path.Combine(assets,"Graphics",mapSelect.SelectedItem.ToString()));
                    map.Invalidate();
                }
                catch(Exception e) {
                    MessageBox.Show(this,e.Message,"Unsupported scenario");
                }
            };
            mapSelect.SelectedIndex=0;
            clip=new ClipStudy(assets,preferences.HighMovies);clip.SetSound(sound.Enabled);
            var movieTab=new TabPage("Combat movies"){BackColor=BackColor};movieTab.Controls.Add(clip);tabs.TabPages.Add(movieTab);
            var resultTab=new TabPage("Result sequences"){BackColor=BackColor};resultTab.Controls.Add(ending);tabs.TabPages.Add(resultTab);
            var frontTab=new TabPage("Original menu"){BackColor=BackColor};frontTab.Controls.Add(front);frontTab.Controls.Add(solo);frontTab.Controls.Add(fleet);frontTab.Controls.Add(entry);tabs.TabPages.Add(frontTab);
            var strip=new StatusStrip {
                Dock=DockStyle.Fill,BackColor=panel
            };
            status.ForeColor=Color.WhiteSmoke;
            strip.Items.Add(status);
            layout.Controls.Add(strip,0,3);
            board.ActionDone+=delegate(string kind) {
                if(State.Pending==null)sound.Play(kind);
                UpdateView();
            };
            tabs.SelectedIndexChanged+=delegate {
                if(disposed||State==null)return;
                clip.Pause();ending.Pause();front.Pause();solo.Pause();fleet.Pause();entry.Pause();
                if(tabs.SelectedIndex==4&&!front.HasOpened&&!solo.Visible&&!fleet.Visible&&!entry.Visible)front.Begin();SuppressHeldControls();
                if(tabs.SelectedIndex!=0&&(State.Phase==Phase.PlayerTurn||State.Phase==Phase.OpponentTurn))State.Paused=true;
                UpdateView();
            };
            opponentTimer.Interval=600;
            opponentTimer.Tick+=delegate {
                bool wasPaused=State.Paused;
                if(State.AdvanceOpponent()) {
                    if(State.Pending==null)sound.Play("fire");
                    UpdateView();
                }
                else if(wasPaused!=State.Paused)UpdateView();
            };
            opponentTimer.Start();
            KeyDown+=OnKey;
            KeyUp+=delegate(object s,KeyEventArgs e) {
                down.Remove(e.KeyCode);
            };
            Deactivate+=delegate {
                if(disposed||Disposing||IsDisposed)return;
                clip.Pause();ending.Pause();front.Pause();solo.Pause();fleet.Pause();entry.Pause();
                if(State!=null&&(State.Phase==Phase.PlayerTurn||State.Phase==Phase.OpponentTurn)) {
                    State.Paused=true;
                    UpdateView();
                }
            };
            Activated+=delegate {SuppressHeldControls();};
            NewGame();
            visualTimer.Interval=16;
            visualTimer.Tick+=delegate {TickVisuals();};
            visualTimer.Start();
        }
        void SuppressHeldControls() {
            if(disposed)return;
            foreach(Keys key in new[]{Keys.Enter,Keys.Space,Keys.R,Keys.P,Keys.Escape,Keys.F2,Keys.Left,Keys.Right,Keys.Up,Keys.Down}) {
                if((GetAsyncKeyState((int)key)&0x8000)!=0)down.Add(key);else down.Remove(key);
            }
        }
        void TickVisuals() {
            long now=visualTime.ElapsedMilliseconds,elapsed=now-visualLast;visualLast=now;
            bool running=preferences.Motion&&State!=null&&!State.Paused&&(State.Phase==Phase.PlayerTurn||State.Phase==Phase.OpponentTurn);
            if(effectClock.Advance(elapsed,running)){board.DamageTick=effectClock.Tick;board.Invalidate();}
        }
        void UpdateDisplayChecks() {
            acceptedColors.Checked=!preferences.RecoveredColors;recoveredColors.Checked=preferences.RecoveredColors;motion.Checked=preferences.Motion;
            movies.Checked=preferences.MovieTurns;movies.Enabled=State!=null&&State.Rules.FixedY&&(State.Phase==Phase.Placement||State.Phase==Phase.Finished);
            highMovies.Checked=preferences.HighMovies;highMovies.Enabled=State!=null&&(State.Phase==Phase.Placement||State.Phase==Phase.Finished);
        }
        void SetMovieDetail(bool high,bool persist){
            if(State==null||(State.Phase!=Phase.Placement&&State.Phase!=Phase.Finished))return;
            bool previous=preferences.HighMovies;
            if(!clip.SetDetail(high)||!battleMovie.SetDetail(high)){
                string problem=clip.Problem!=""?clip.Problem:battleMovie.Problem;clip.SetDetail(previous);battleMovie.SetDetail(previous);preferences.Problem="Movie detail change failed: "+problem;
            }else{preferences.HighMovies=high;if(persist)SavePreferences();}
            UpdateView();
        }
        void SavePreferences() {
            if(previewMode)return;
            try{preferences.Save(settingsPath);preferences.Problem="";}
            catch(Exception e){preferences.Problem="Could not save settings: "+e.Message;}
        }
        void SelectColors(bool recovered,bool persist) {
            try {
                if(preferences.RecoveredColors!=recovered)RebuildArt(recovered);
                preferences.RecoveredColors=recovered;
                if(persist)SavePreferences();
                UpdateDisplayChecks();if(State!=null)UpdateView();
            }catch(Exception e){preferences.Problem="Display change failed: "+e.Message;if(State!=null)UpdateView();}
        }
        void RebuildArt(bool recovered) {
            Color[] palette=recovered?ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL"))):classicImage.Palette;
            Bitmap nextArt=null,nextForeground=null;var nextRoster=new List<Bitmap>();ClassicShips nextShips=null;ClassicDamage nextDamage=null;ClassicHud nextHud=null;ClassicSpriteFont nextFont=null;
            try {
                nextArt=new IndexedImage {Width=classicImage.Width,Height=classicImage.Height,Pixels=classicImage.Pixels,Palette=palette}.Bitmap();
                nextForeground=ClassicBackdrop.Foreground(new IndexedImage{Width=classicImage.Width,Height=classicImage.Height,Pixels=classicImage.Pixels,Palette=palette});
                foreach(var frame in Assets.Sph(Path.Combine(assets,"Graphics","EO_CLASS.SPH"),palette))nextRoster.Add(frame==null?null:frame.Bitmap());
                nextShips=new ClassicShips(Path.Combine(assets,"Graphics"),palette);
                nextFont=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),palette);
                nextDamage=new ClassicDamage(Path.Combine(assets,"Graphics"),palette);nextHud=new ClassicHud(Path.Combine(assets,"Graphics"),palette);
            }catch {
                if(nextArt!=null)nextArt.Dispose();if(nextForeground!=null)nextForeground.Dispose();foreach(var image in nextRoster)if(image!=null)image.Dispose();
                if(nextShips!=null)nextShips.Dispose();if(nextDamage!=null)nextDamage.Dispose();if(nextHud!=null)nextHud.Dispose();if(nextFont!=null)nextFont.Dispose();throw;
            }
            ClassicSpriteFont previousFont=messageFont;messageFont=nextFont;board.MessageFont=messageFont;
            ClassicHud previousHud=hudImages;Bitmap previousArt=art,previousForeground=backdropForeground;ClassicShips previousShips=shipImages;ClassicDamage previousDamage=damageImages;var previousRoster=rosterImages.ToArray();
            hudImages=nextHud;board.Hud=hudImages;art=nextArt;backdropForeground=nextForeground;board.ForegroundArt=backdropForeground;shipImages=nextShips;damageImages=nextDamage;rosterImages.Clear();rosterImages.AddRange(nextRoster);
            board.Art=art;board.Ships=shipImages;board.Damage=damageImages;board.Roster=rosterImages.ToArray();
            if(previousArt!=null)previousArt.Dispose();if(previousForeground!=null)previousForeground.Dispose();if(previousShips!=null)previousShips.Dispose();if(previousDamage!=null)previousDamage.Dispose();if(previousHud!=null)previousHud.Dispose();if(previousFont!=null)previousFont.Dispose();
            foreach(var image in previousRoster)if(image!=null)image.Dispose();
        }
        void Button(FlowLayoutPanel parent,Button button,string text,EventHandler action) {
            button.Text=text;
            button.AutoSize=true;
            button.Height=33;
            button.FlatStyle=FlatStyle.Flat;
            button.FlatAppearance.BorderColor=Color.FromArgb(65,94,113);
            button.BackColor=Color.FromArgb(31,56,75);
            button.ForeColor=Color.WhiteSmoke;
            button.Padding=new Padding(5,0,5,0);
            button.Margin=new Padding(3,0,3,0);
            button.Click+=action;
            parent.Controls.Add(button);
        }
        static void AddLabel(Control parent,Label label,int height,float size,Color color) {
            label.Width=224;
            label.Height=height;
            label.Font=new Font("Segoe UI",size,size>=15?FontStyle.Bold:FontStyle.Regular);
            label.ForeColor=color;
            label.Margin=new Padding(0,0,0,6);
            parent.Controls.Add(label);
        }
        void OpenSolo(){if(disposed)return;front.Pause();front.Visible=false;solo.Visible=true;solo.BringToFront();solo.Begin();SuppressHeldControls();}
        void OpenFleet(){if(disposed)return;int ai=rules.SelectedIndex==1?recoveredOpponent:opponent.SelectedIndex;var draft=new Game(seed,RuleProfile.Recovered,ai==4?OpponentKind.Prototype:OpponentKind.Original,ai<4?ai:0,preferences.MovieTurns);draft.AutoPlace();solo.Pause();solo.Visible=false;fleet.Visible=true;fleet.BringToFront();fleet.Begin(draft);SuppressHeldControls();UpdateView();}
        void OpenEntry(){if(disposed||entry.HasOpened||fleet.Draft==null||fleet.SelectedShip>=0)return;fleet.Pause();fleet.Visible=false;entry.Visible=true;entry.BringToFront();entry.Begin();SuppressHeldControls();UpdateView();}
        void CommitFleet(){if(disposed||fleet.Draft==null||fleet.SelectedShip>=0)return;var heldKeys=new Keys[down.Count];down.CopyTo(heldKeys);Game accepted=fleet.Draft;if(rules.SelectedIndex!=0)rules.SelectedIndex=0;else NewGame();State=accepted;board.Game=State;if(!State.Start())throw new InvalidOperationException("Fleet commit failed");board.KeyboardCell=new Point(0,0);UpdateView();board.Focus();SuppressHeldControls();foreach(Keys key in heldKeys)down.Add(key);}
        void NewGame() {
            if(entry!=null){entry.Reset();entry.Visible=false;}
            if(fleet!=null){fleet.Reset();fleet.Visible=false;}
            if(solo!=null){solo.Reset();solo.Visible=false;}if(front!=null)front.Visible=true;
            if(front!=null)front.Reset();
            if(clip!=null)clip.Reset();if(ending!=null)ending.Cancel();
            board.MovieFrame=null;battleMovie.CancelPlayback();battleMovie.Visible=false;shownPending=null;shownGame=null;battleFailure=false;
            bool legacy=rules.SelectedIndex==1;
            State=new Game(unchecked(seed++),legacy?RuleProfile.Legacy:RuleProfile.Recovered,legacy||opponent.SelectedIndex==4?OpponentKind.Prototype:OpponentKind.Original,opponent.SelectedIndex<4?opponent.SelectedIndex:0,preferences.MovieTurns);
            board.Game=State;
            board.KeyboardCell=new Point(0,State.Rules.FixedY?State.NextLength/2:0);
            effectClock.Reset();board.DamageTick=0;visualLast=visualTime.ElapsedMilliseconds;
            tabs.SelectedIndex=0;
            UpdateView();
        }
        void Rotate() {
            if(State.Rotate())UpdateView();
        }
        void OnKey(object sender,KeyEventArgs e) {
            if(disposed)return;
            if(displayMenu.Visible)return;
            if((rules.ContainsFocus||opponent.ContainsFocus)&&e.KeyCode!=Keys.F2)return;
            if(firingFleet.ContainsFocus&&e.KeyCode!=Keys.F2&&e.KeyCode!=Keys.P&&e.KeyCode!=Keys.Escape)return;
            if(displayButton.ContainsFocus&&(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space)) {
                if(!down.Add(e.KeyCode))e.SuppressKeyPress=true;
                return; // Let the focused native button receive its first confirmation.
            }
            if(tabs.SelectedIndex==4&&(front.Canvas.ContainsFocus||solo.Canvas.ContainsFocus||fleet.Canvas.ContainsFocus||entry.Canvas.ContainsFocus)&&e.KeyCode!=Keys.F2){if(!down.Add(e.KeyCode)){e.SuppressKeyPress=true;return;}bool handledMenu=entry.Visible?entry.Key(e.KeyCode):fleet.Visible?fleet.Key(e.KeyCode):solo.Visible?solo.Key(e.KeyCode):front.Key(e.KeyCode);down.Add(e.KeyCode);if(handledMenu)e.SuppressKeyPress=true;return;}
            if(tabs.SelectedIndex!=0&&e.KeyCode!=Keys.F2)return;
            if(State.Pending!=null&&(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space)) {
                if(!down.Add(e.KeyCode)){e.SuppressKeyPress=true;return;}
                if(board.ContainsFocus||battleMovie.ContainsFocus){SkipBattleStage();e.SuppressKeyPress=true;}
                return;
            }
            if(!down.Add(e.KeyCode)) {
                e.SuppressKeyPress=true;
                return;
            }
            bool handled=true;
            switch(e.KeyCode) {
                case Keys.Left:board.MoveCursor(-1,0);
                break;
                case Keys.Right:board.MoveCursor(1,0);
                break;
                case Keys.Up:board.MoveCursor(0,-1);
                break;
                case Keys.Down:board.MoveCursor(0,1);
                break;
                case Keys.Enter:board.Act();
                break;
                case Keys.R:Rotate();
                break;
                case Keys.P:case Keys.Escape:State.TogglePause();
                UpdateView();
                break;
                case Keys.F2:NewGame();
                break;
                default:handled=false;
                break;
            }
            if(handled)e.SuppressKeyPress=true;
        }
        void UpdateView() {
            if(disposed)return;
            SyncBattleMovie();
            bool placement=State.Phase==Phase.Placement;
            bool active=tabs.SelectedIndex==0;
            auto.Enabled=clear.Enabled=placement&&active;
            rotate.Enabled=placement&&active&&!State.Rules.FixedY;
            rules.Enabled=active&&(placement||State.Phase==Phase.Finished);
            opponent.Enabled=rules.Enabled&&State.Rules.FixedY;
            start.Enabled=placement&&active&&State.Player.Ships.Count==State.Rules.Fleet.Length;
            pause.Enabled=active&&(State.Phase==Phase.PlayerTurn||State.Phase==Phase.OpponentTurn);
            pause.Text=State.Paused?"Resume (P)":"Pause (P)";
            rotate.Text=State.Rules.FixedY?"Fixed axis":State.Vertical?"Vertical (R)":"Horizontal (R)";
            audio.Text=sound.Enabled?"Sound: on":"Sound: off";
            phase.Text=State.Paused?"Paused":placement?"Deploy your fleet":State.Phase==Phase.PlayerTurn?"Your turn / "+State.Turn:State.Phase==Phase.OpponentTurn?"Opponent's turn":"Battle complete";
            if(State.Pending!=null&&!State.Paused)phase.Text=(State.Pending.Opponent?"Opponent / ":"Your shot / ")+State.Pending.StageLabel;
            hint.Text=placement?(State.Rules.FixedY?"Click the lower-left board at each ship's center. Ships follow the original fixed axis. Or use Auto-place, then Start battle.":"Click the lower-left board to place ships. Rotate with R. Or choose Auto-place, then Start battle."):State.Rules.FixedY?"Select a firing ship below or click your ship. Fire on the upper-right board (arrows + Enter). One cell per shot. P pauses; F2 resets.":"Click the upper-right board to fire. Arrows + Enter also work. Each shot ends your turn. P pauses; F2 starts over.";
            if(State.Pending!=null)hint.Text="Firing → travel → damage. Skip stage / Space advances once. P pauses; F2 resets. Movies stay behind the board.";
            else if(State.Phase==Phase.Finished)hint.Text="Watch result sequence (VICTORY/LOSE), or use New battle (F2).";
            else if(placement&&State.Rules.FixedY)hint.Text+=" Display offers optional battle movies.";
            hint.LinkArea=State.Phase==Phase.Finished?new LinkArea(0,21):new LinkArea(0,0);
            updatingFleet=true;firingFleet.BeginUpdate();firingFleet.Items.Clear();
            for(int i=0; i<State.Rules.Fleet.Length; i++) {
                Ship item=i<State.Player.Ships.Count?State.Player.Ships[i]:null;
                ShipSpec spec=State.Rules.Fleet[i];
                firingFleet.Items.Add((i==State.PlayerShooter?"> ":"  ")+spec.Name+"  "+(item==null?spec.Length+" / unplaced":item.Sunk?"SUNK":(item.Cells.Count-item.Hits)+" / "+item.Cells.Count));
            }
            firingFleet.SelectedIndex=State.PlayerShooter;
            firingFleet.Enabled=active&&State.CanSelectShooter;
            firingFleet.EndUpdate();updatingFleet=false;
            int sunk=0;
            foreach(var item in State.Enemy.Ships)if(item.Sunk)sunk++;
            enemy.Text="OPPONENT: "+sunk+" / 5 ships sunk";
            status.Text=sound.Problem!=""?sound.Problem:preferences.Problem!=""?preferences.Problem:tabs.SelectedIndex==4&&entry.Visible?"Preparing Classic battle. Return to fleet cancels this entry.":tabs.SelectedIndex==4&&fleet.Visible?"Review your fleet. Start Game begins the battle; Back keeps the previous game paused.":tabs.SelectedIndex==2?"Original clip study. Return to Classic duel to play.":State.Message;
            int from=Math.Max(0,State.Events.Count-9);
            history.Text=string.Join(Environment.NewLine,State.Events.GetRange(from,State.Events.Count-from).ToArray());
            board.Invalidate();UpdateDisplayChecks();
        }
        void SyncBattleMovie(){
            PendingShot pending=State.Pending;
            if(pending==null){bool focused=battleMovie.ContainsFocus;if(shownPending!=null)battleMovie.CancelPlayback();battleMovie.Visible=false;battleField.RowStyles[1].Height=0;board.MovieFrame=null;board.Visible=true;if(focused&&tabs.SelectedIndex==0)board.Focus();shownPending=null;return;}
            bool boardFocused=board.ContainsFocus;board.Visible=true;
            battleField.RowStyles[1].Height=106;battleMovie.Visible=true;battleField.PerformLayout();
            if(!object.ReferenceEquals(shownPending,pending)||shownStage!=pending.Stage||shownGame!=State){
                shownPending=pending;shownStage=pending.Stage;shownGame=State;
                battleMovie.StageCaption=(pending.Opponent?"Opponent shot — ":"Your shot — ")+pending.StageLabel;
                loadingBattle=true;try{battleFailure=validationFailMovieLoad||!battleMovie.LoadBattle(pending.MovieName);}finally{loadingBattle=false;}
                if(battleFailure){State.Paused=true;State.Message="Movie unavailable: "+(battleMovie.Problem==""?"resource validation failed":battleMovie.Problem)+". Skip this stage explicitly or use F2.";}
            }
            SyncBackdropFrame();
            battleMovie.EndButton.Enabled=battleFailure||!State.Paused;
            if(State.Paused)battleMovie.Pause();
            else if(!battleFailure&&battleMovie.Timeline.Completed)CompleteBattleStage();
            else if(!battleFailure)battleMovie.ResumePlayback();
            if(boardFocused&&tabs.SelectedIndex==0)battleMovie.EndButton.Focus();
        }
        void SyncBackdropFrame(){
            board.MovieFrame=State!=null&&State.Pending!=null&&!battleFailure&&object.ReferenceEquals(shownPending,State.Pending)?battleMovie.CurrentImage:null;board.Invalidate();
        }
        void CompleteBattleStage(){
            if(advancingBattle||loadingBattle||shownGame!=State||shownPending==null)return;
            advancingBattle=true;try{if(State.CompleteMovie(shownPending,shownStage))UpdateView();}finally{advancingBattle=false;}
        }
        void SkipBattleStage(){
            if(State.Pending==null||(State.Paused&&!battleFailure))return;
            if(battleFailure)State.Paused=false;
            CompleteBattleStage();
        }
        protected override bool ShowWithoutActivation {
            get {
                return previewMode;
            }
        }
        public void SavePreview(string path,string mode) {
            previewMode=true;sound.Enabled=false;clip.SetSound(false);battleMovie.SetSound(false);ending.SetSound(false);
            opponentTimer.Stop();visualTimer.Stop();
            ShowInTaskbar=false;
            StartPosition=FormStartPosition.Manual;
            Location=new Point(-20000,-20000);
            preferences.MovieTurns=false;preferences.HighMovies=false;clip.SetDetail(false);battleMovie.SetDetail(false);seed=731; // Stable comparison scene; normal games still use a fresh seed.
            NewGame();
            if(mode=="message"||mode=="message-small"){if(mode=="message-small")Size=MinimumSize;preferences.MovieTurns=true;seed=731;NewGame();State.AutoPlace();State.Start();State.SelectShooter(3);State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();State.Paused=true;UpdateView();battleMovie.PreviewFrame(20);}
            if(mode=="high"||mode=="high-small"){SetMovieDetail(true,false);mode=mode=="high"?"backdrop":"backdrop-small";}
            if(mode=="shot"||mode=="shot-small"){
                preferences.MovieTurns=true;seed=731;NewGame();State.AutoPlace();State.Start();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();State.Paused=true;UpdateView();battleMovie.PreviewFrame(20);if(mode=="shot-small")Size=MinimumSize;
            }
            if(mode=="impact"||mode=="impact-small"||mode=="miss"||mode=="sink"||mode=="backdrop"||mode=="backdrop-small"){
                preferences.MovieTurns=true;opponent.SelectedIndex=0;seed=731;NewGame();State.AutoPlace();State.Start();Point target=State.Enemy.Ships[0].Cells[0];
                if(mode=="miss"){for(int x=0;x<10;x++)for(int y=0;y<10;y++)if(State.Enemy.ShipAt(new Point(x,y))==null)target=new Point(x,y);}
                if(mode=="sink"){var ship=State.Enemy.Ships[0];for(int i=1;i<ship.Cells.Count;i++)State.Enemy.Shoot(ship.Cells[i]);}
                State.Fire(target);var pending=State.Pending;State.CompleteMovie(pending,ShotStage.Firing);State.CompleteMovie(pending,ShotStage.Travel);if(State.Pending==null||pending.Stage!=ShotStage.Feedback)throw new InvalidOperationException("Impact preview did not reach feedback");UpdateView();State.Paused=true;UpdateView();battleMovie.PreviewFrame(Math.Min(35,battleMovie.Timeline.Count-1));if(mode=="impact-small"||mode=="backdrop-small")Size=MinimumSize;if(mode=="backdrop"||mode=="backdrop-small"){State.Paused=false;UpdateView();battleMovie.Pause();}
            }
            if(mode=="damage"||mode=="recovered") {
                SelectColors(mode=="recovered",false);
                State.AutoPlace();State.Start();
                State.LastEnemyShot=State.Player.Shoot(State.Player.Ships[1].Cells[2]);
                effectClock.Advance(160,true); // Fixed tick 4 -> segment-2 frame 20.
                board.DamageTick=effectClock.Tick;
                State.Message="Damage preview: one controlled own-ship hit.";State.Events.Add(State.Message);UpdateView();
            }
            if(mode=="small") {
                Size=MinimumSize;
                State.AutoPlace(); State.Start();
                State.Fire(State.Enemy.Ships[2].Cells[0]); State.AdvanceOpponent();
                UpdateView();
            }
            if(mode=="legacy")rules.SelectedIndex=1;
            if(mode=="fleet") { State.AutoPlace(); UpdateView(); }
            if(mode=="firing") {
                State.AutoPlace();State.Start();State.SelectShooter(3);
                if(!State.Fire(new Point(0,0))||!State.AdvanceOpponent())throw new InvalidOperationException("Firing preview turn failed");
                State.Message="Firing preview: scripted instant-mode actions; enable battle movies for playback.";
                State.Events.Add(State.Message);UpdateView();
            }
            if(mode=="battle") {
                State.AutoPlace();
                State.Start();
                foreach(Point target in State.Enemy.Ships[0].Cells) {
                    if(!State.Fire(target)||!State.AdvanceOpponent())throw new InvalidOperationException("Preview turn failed");
                }
                if(!State.Enemy.Ships[0].Sunk)throw new InvalidOperationException("Preview sink failed");
                UpdateView();
            }
            if(mode=="map") {
                State.AutoPlace();
                State.Start();
            }
            Show();
            Application.DoEvents();
            if(mode=="clip"||mode=="clip-small"||mode=="library") {if(mode=="clip-small")Size=MinimumSize;tabs.SelectedIndex=2;if(mode=="library")clip.Selector.SelectedIndex=8;clip.PreviewFrame(mode=="library"?50:20);Application.DoEvents();}
            if(mode=="map") {
                tabs.SelectedIndex=1;
                Application.DoEvents();
                if(!State.Paused)throw new InvalidOperationException("Inspection did not pause the active battle");
            }
            if(mode=="victory"||mode=="lose"||mode=="ending-small"){if(mode=="ending-small")Size=MinimumSize;tabs.SelectedIndex=2;clip.Selector.SelectedItem=mode=="victory"?"VICTORY":"LOSE";clip.PreviewFrame(mode=="victory"?300:400);Application.DoEvents();}
            if(mode=="entry"||mode=="entry-small"){if(mode=="entry-small")Size=MinimumSize;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);fleet.Activate(2);entry.Pause();entry.Timeline.Resume();entry.Advance(520);entry.Pause();Application.DoEvents();}
            if(mode=="fleet-config"||mode=="fleet-config-small"){if(mode=="fleet-config-small")Size=MinimumSize;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);Application.DoEvents();}
            if(mode=="solo"||mode=="solo-small"){if(mode=="solo-small")Size=MinimumSize;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);Application.DoEvents();}
            if(mode=="title"||mode=="title-small"){if(mode=="title-small")Size=MinimumSize;tabs.SelectedIndex=4;front.FinishReveal();Application.DoEvents();}
            if(mode=="result-card"||mode=="result-movie"){tabs.SelectedIndex=3;ending.StartOutcome(true);if(mode=="result-card"){ending.SkipButton.PerformClick();ending.Pause();}else{ending.SkipButton.PerformClick();ending.SkipButton.PerformClick();ending.SkipButton.PerformClick();ending.Pause();}Application.DoEvents();}
            PerformLayout();
            using(var bmp=new Bitmap(Width,Height)) {
                DrawToBitmap(bmp,new Rectangle(0,0,Width,Height));
                bmp.Save(path,System.Drawing.Imaging.ImageFormat.Png);
            }
            Close();
        }
        public int CheckUi(string report) {
            previewMode=true;sound.Enabled=false;clip.SetSound(false);battleMovie.SetSound(false);ending.SetSound(false);
            // Keep diagnostics independent of saved user choices, without saving.
            preferences.MovieTurns=false;SetMovieDetail(false,false);SelectColors(false,false);NewGame();
            ShowInTaskbar=false;
            StartPosition=FormStartPosition.Manual;
            Location=new Point(-20000,-20000);
            sound.Enabled=false;clip.SetSound(false);battleMovie.SetSound(false);
            Show();
            Application.DoEvents();
            var results=new List<string>();
            int failed=0;
            Action<string,Action> check=delegate(string name,Action action) {
                try { action(); results.Add("PASS "+name); }
                catch(Exception e) { failed++; results.Add("FAIL "+name+": "+e.Message); }
            };
            Action<bool,string> require=delegate(bool ok,string message) { if(!ok)throw new Exception(message); };
            check("Preset switch resets deployment and exposes the matching rotation control",delegate {
                require(State.Rules==RuleProfile.Recovered&&!rotate.Enabled,"Default recovered preset");
                auto.PerformClick();
                require(State.Player.Ships.Count==5,"Auto-place button");
                rules.SelectedIndex=1;
                require(State.Rules==RuleProfile.Legacy&&State.Player.Ships.Count==0&&rotate.Enabled,"Switch reset");
                rotate.PerformClick();
                require(State.PlacementVertical,"Rotate button");
            });
            check("Battle locks preset changes; F2 retains the selected preset",delegate {
                auto.PerformClick(); start.PerformClick();
                require(State.Phase==Phase.PlayerTurn&&!rules.Enabled&&!rotate.Enabled,"Battle controls");
                down.Remove(Keys.F2);
                OnKey(this,new KeyEventArgs(Keys.F2));
                require(State.Phase==Phase.Placement&&State.Rules==RuleProfile.Legacy&&rules.Enabled,"F2 reset/preset");
            });
            check("Map transition pauses battle and disables deployment controls",delegate {
                rules.SelectedIndex=0;
                auto.PerformClick(); start.PerformClick();
                tabs.SelectedIndex=1;
                Application.DoEvents();
                require(State.Paused&&!auto.Enabled&&!start.Enabled&&!rotate.Enabled&&!rules.Enabled,"Map gating");
                tabs.SelectedIndex=0;
                require(State.Paused&&pause.Enabled,"Return keeps explicit pause");
                pause.PerformClick();
                require(!State.Paused,"Resume button");
            });
            check("Simulated held Enter produces one deployment action",delegate {
                NewGame();
                board.Focus();
                down.Remove(Keys.Enter);
                OnKey(this,new KeyEventArgs(Keys.Enter));
                OnKey(this,new KeyEventArgs(Keys.Right));
                OnKey(this,new KeyEventArgs(Keys.Enter));
                require(State.Player.Ships.Count==1,"Held confirmation repeated");
                down.Remove(Keys.Enter);
                OnKey(this,new KeyEventArgs(Keys.Enter));
                require(State.Player.Ships.Count==2,"Released confirmation not accepted");
            });
            check("Simulated native-coordinate mouse placement at normal and minimum sizes",delegate {
                foreach(bool small in new[]{false,true}) {
                    NewGame();
                    if(small)Size=MinimumSize;
                    PerformLayout(); Application.DoEvents();
                    RectangleF bounds=board.ImageBounds;
                    // Independently calculated native pixel (164,331) selects X=2,Y=4.
                    int px=(int)(bounds.X+164*bounds.Width/640),py=(int)(bounds.Y+331*bounds.Height/board.ArtHeight);
                    var method=typeof(BoardView).GetMethod("OnMouseClick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    method.Invoke(board,new object[]{new MouseEventArgs(MouseButtons.Left,1,px,py,0)});
                    require(State.Player.Ships.Count==1&&State.Player.Ships[0].Cells[0]==new Point(2,2),"Native mouse axis/center placement");
                }
            });
            check("Opponent settings reset deployment, lock during battle and survive F2",delegate {
                Size=new Size(1236,879);
                NewGame();
                auto.PerformClick();
                opponent.SelectedIndex=2;
                require(State.Player.Ships.Count==0&&State.Opponent==OpponentKind.Original&&State.OpponentLevel==2,"Opponent setting reset");
                auto.PerformClick(); start.PerformClick();
                require(!opponent.Enabled,"Opponent selector changed during battle");
                down.Remove(Keys.F2); OnKey(this,new KeyEventArgs(Keys.F2));
                require(State.OpponentLevel==2&&opponent.SelectedIndex==2&&opponent.Enabled,"F2 lost setting");
                rules.SelectedIndex=1;
                require(State.Opponent==OpponentKind.Prototype&&opponent.SelectedIndex==4&&!opponent.Enabled,"Legacy opponent mapping");
                rules.SelectedIndex=0;
                require(State.Opponent==OpponentKind.Original&&State.OpponentLevel==2,"Recovered setting was not retained");
            });
            check("Minimum-size paused toolbar keeps all controls visible in both presets",delegate {
                Size=MinimumSize;
                foreach(int profile in new[]{0,1}) {
                    rules.SelectedIndex=profile; NewGame();
                    auto.PerformClick();start.PerformClick();pause.PerformClick();
                    PerformLayout();Application.DoEvents();
                    foreach(Control control in opponent.Parent.Controls)require(control.Right<=opponent.Parent.ClientSize.Width,"Toolbar clipped "+control.Text);
                    require(TextRenderer.MeasureText(opponent.Text,opponent.Font).Width+SystemInformation.VerticalScrollBarWidth<=opponent.ClientSize.Width,"Opponent choice text clipped");
                }
            });
            check("Display controls remain visible and preserve battle state while changing colors",delegate {
                rules.SelectedIndex=0;NewGame();auto.PerformClick();start.PerformClick();
                var sameGame=State;int turn=State.Turn,events=State.Events.Count;
                require(displayButton.Visible&&displayButton.Right<=displayButton.Parent.ClientSize.Width&&displayButton.Bottom<=displayButton.Parent.ClientSize.Height,"Display button clipped");
                var opening=typeof(ContextMenuStrip).GetMethod("OnOpening",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                opening.Invoke(displayMenu,new object[]{new System.ComponentModel.CancelEventArgs()});
                require(State.Paused,"Display menu did not pause battle");
                SelectColors(false,false);Bitmap old=art;
                recoveredColors.PerformClick();
                require(preferences.RecoveredColors&&recoveredColors.Checked&&State==sameGame&&State.Turn==turn&&State.Events.Count==events,"Palette switch altered game state");
                Color[] expected=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")));
                bool compared=false;
                for(int i=0;i<classicImage.Pixels.Length&&!compared;i++) {
                    int index=classicImage.Pixels[i];
                    if(expected[index].ToArgb()==classicImage.Palette[index].ToArgb())continue;
                    require(art.GetPixel(i%classicImage.Width,i/classicImage.Width).ToArgb()==expected[index].ToArgb(),"Displayed palette did not change");compared=true;
                }
                require(compared,"No visible palette comparison pixel");
                bool disposedOld=false;try{int width=old.Width;}catch(ArgumentException){disposedOld=true;}
                require(disposedOld,"Old palette resources were retained");
                acceptedColors.PerformClick();
                require(!preferences.RecoveredColors&&acceptedColors.Checked&&State.Paused,"Accepted palette/pause not restored");
                NewGame();displayButton.Focus();down.Remove(Keys.Enter);
                var first=new KeyEventArgs(Keys.Enter);OnKey(this,first);
                var held=new KeyEventArgs(Keys.Enter);OnKey(this,held);
                require(!first.SuppressKeyPress&&held.SuppressKeyPress&&State.Player.Ships.Count==0,"Focused Display confirmation reached the board or repeated");
                down.Remove(Keys.Enter);
            });
            check("Live visual timer advances, freezes for pause/still and resets with F2",delegate {
                rules.SelectedIndex=0;NewGame();auto.PerformClick();start.PerformClick();preferences.Motion=true;
                State.Player.Shoot(State.Player.Ships[1].Cells[2]);
                PumpVisuals(130);
                require(effectClock.Tick>0&&board.DamageTick==effectClock.Tick,"Live form timer did not advance damage phase");
                State.Paused=true;long paused=effectClock.Tick;PumpVisuals(100);require(effectClock.Tick==paused,"Pause did not freeze effect");
                State.Paused=false;preferences.Motion=false;PumpVisuals(100);require(effectClock.Tick==paused,"Still-effects mode advanced");
                preferences.Motion=true;NewGame();require(effectClock.Tick==0&&board.DamageTick==0,"New-game damage phase");
            });
            check("Own-ship and original roster clicks select without firing at both sizes",delegate {
                opponentTimer.Stop();rules.SelectedIndex=0;
                foreach(bool small in new[]{false,true}) {
                    seed=731;NewGame();auto.PerformClick();start.PerformClick();
                    if(small)Size=MinimumSize;else ClientSize=new Size(1220,840);
                    board.Focus();PerformLayout();Application.DoEvents();State.Paused=false;
                    RectangleF bounds=board.ImageBounds;
                    Action<PointF,MouseButtons> click=delegate(PointF at,MouseButtons button) {
                        int px=(int)Math.Round(bounds.X+at.X*bounds.Width/640),py=(int)Math.Round(bounds.Y+at.Y*bounds.Height/board.ArtHeight);
                        var method=typeof(BoardView).GetMethod("OnMouseClick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                        method.Invoke(board,new object[]{new MouseEventArgs(button,1,px,py,0)});
                    };
                    Point cell=State.Player.Ships[3].Cells[1];
                    click(ClassicGeometry.Project(false,cell.X+.5f,cell.Y+.5f),MouseButtons.Left);
                    require(State.PlayerShooter==3&&firingFleet.SelectedIndex==3&&State.Phase==Phase.PlayerTurn&&State.LastPlayerShot==null,"Own-grid selection fired or missed");
                    Point roster=BoardView.RosterPosition(false,0);click(new PointF(roster.X+40,roster.Y+14),MouseButtons.Left);
                    require(State.PlayerShooter==0&&State.Turn==1&&State.Events.Count==2,"Roster selection consumed action");
                    Point other=BoardView.RosterPosition(false,4);click(new PointF(other.X+40,other.Y+14),MouseButtons.Right);
                    require(State.PlayerShooter==0,"Right click selected a ship");
                    State.Paused=true;click(new PointF(other.X+40,other.Y+14),MouseButtons.Left);
                    require(State.PlayerShooter==0&&State.LastPlayerShot==null,"Paused mouse changed battle");
                }
            });
            check("Fleet-list keyboard skips sunk ships and confirmation stays out of the grid",delegate {
                rules.SelectedIndex=0;NewGame();auto.PerformClick();start.PerformClick();State.Paused=false;
                State.SelectShooter(0);foreach(int slot in new[]{1,2})foreach(Point cell in State.Player.Ships[slot].Cells)State.Player.Shoot(cell);
                UpdateView();firingFleet.Focus();
                var key=typeof(ListBox).GetMethod("OnKeyDown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                key.Invoke(firingFleet,new object[]{new KeyEventArgs(Keys.Down)});
                require(State.PlayerShooter==3&&firingFleet.SelectedIndex==3,"Down did not skip sunk ships");
                key.Invoke(firingFleet,new object[]{new KeyEventArgs(Keys.Up)});
                require(State.PlayerShooter==0,"Up did not skip sunk ships");
                firingFleet.SelectedIndex=1;require(State.PlayerShooter==0&&firingFleet.SelectedIndex==0,"Mouse selected sunk list row");
                down.Remove(Keys.Enter);OnKey(this,new KeyEventArgs(Keys.Enter));
                require(State.LastPlayerShot==null&&State.Phase==Phase.PlayerTurn,"List confirmation fired a grid shot");
                down.Remove(Keys.P);OnKey(this,new KeyEventArgs(Keys.P));
                require(State.Paused&&!firingFleet.Enabled,"Pause/list gate");
                down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));
                require(State.Phase==Phase.Placement&&State.PlayerShooter==-1&&!firingFleet.Enabled,"Reset left stale selection");
                auto.PerformClick();start.PerformClick();require(State.PlayerShooter==1&&firingFleet.SelectedIndex==1,"New battle did not restore native initial shooter");
            });
            check("Fleet list and wrapped firing results fit the minimum layout",delegate {
                Size=MinimumSize;rules.SelectedIndex=0;NewGame();auto.PerformClick();start.PerformClick();
                State.SelectShooter(3);State.Fire(new Point(0,0));State.AdvanceOpponent();UpdateView();PerformLayout();Application.DoEvents();
                require(firingFleet.Bottom<=firingFleet.Parent.ClientSize.Height&&firingFleet.ClientSize.Height>=5*firingFleet.ItemHeight,"Fleet rows clipped");
                foreach(object item in firingFleet.Items)require(TextRenderer.MeasureText(item.ToString(),firingFleet.Font).Width+4<firingFleet.ClientSize.Width,"Fleet name/status clipped");
                require(history.WordWrap&&history.ClientSize.Width>200&&history.ClientSize.Height>80,"Firing log clipped");
                require(history.Text.Contains("Missile Submarine / "),"Selected ship/weapon absent from visible log");
                State.Paused=true;UpdateView();require(!firingFleet.Enabled,"Paused list remained active");
            });
            check("Clip study pauses battle and does not consume game actions",delegate {
                rules.SelectedIndex=0;NewGame();auto.PerformClick();start.PerformClick();
                var game=State;int turn=State.Turn,events=State.Events.Count;
                tabs.SelectedIndex=2;Application.DoEvents();
                require(State.Paused&&!firingFleet.Enabled&&!start.Enabled,"Clip inspection did not pause/lock battle");
                clip.PlayButton.PerformClick();PumpVisuals(130);
                require(clip.Timeline.Frame>0&&clip.Timeline.Playing,"Live clip timer did not advance");
                require(State==game&&State.Turn==turn&&State.Events.Count==events&&State.LastPlayerShot==null,"Clip changed battle state");
                tabs.SelectedIndex=0;int frame=clip.Timeline.Frame;PumpVisuals(100);
                require(State.Paused&&!clip.Timeline.Playing&&clip.Timeline.Frame==frame,"Leaving clip did not freeze playback/preserve battle pause");
            });
            check("Clip buttons pause, step, restart, finish and replay",delegate {
                tabs.SelectedIndex=2;clip.RestartButton.PerformClick();
                require(clip.Timeline.Frame==0&&!clip.Timeline.Playing,"Restart state");
                clip.NextButton.PerformClick();require(clip.Timeline.Frame==1&&!clip.Timeline.Playing,"Next frame");
                clip.PreviousButton.PerformClick();require(clip.Timeline.Frame==0,"Previous frame");
                clip.PlayButton.PerformClick();PumpVisuals(90);clip.PlayButton.PerformClick();
                int frame=clip.Timeline.Frame;PumpVisuals(80);require(!clip.Timeline.Playing&&clip.Timeline.Frame==frame,"Pause button");
                clip.EndButton.PerformClick();require(clip.Timeline.Completed&&clip.Timeline.Frame==45&&!clip.Timeline.Playing,"Skip completion");
                clip.PlayButton.PerformClick();require(clip.Timeline.Playing&&clip.Timeline.Frame==0&&!clip.Timeline.Completed,"Replay did not reset");
                var deactivate=typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);deactivate.Invoke(this,new object[]{EventArgs.Empty});
                require(!clip.Timeline.Playing,"Focus-loss event did not pause clip");
                down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));
                require(State.Phase==Phase.Placement&&tabs.SelectedIndex==0&&clip.Timeline.Frame==0&&!clip.Timeline.Playing,"F2 did not reset/leave clip");
            });
            check("Clip controls fit minimum size and disposed images release resources",delegate {
                Size=MinimumSize;tabs.SelectedIndex=2;PerformLayout();Application.DoEvents();
                foreach(var button in new[]{clip.PlayButton,clip.RestartButton,clip.PreviousButton,clip.NextButton,clip.EndButton})require(button.Right<=button.Parent.ClientSize.Width&&button.Bottom<=button.Parent.ClientSize.Height,"Clip button clipped: "+button.Text);
                using(var probe=new ClipStudy(assets)){Bitmap image=probe.CurrentImage;probe.Dispose();bool released=false;try{int width=image.Width;}catch(ArgumentException){released=true;}require(probe.IsDisposed&&released,"Study bitmap resources were not disposed");}
            });
            check("Library selector loads all 61 clips and releases previous frames",delegate {
                tabs.SelectedIndex=2;clip.SetSound(false);int total=0;Bitmap old=null;
                for(int i=0;i<FiringClips.Names.Length;i++){clip.Selector.SelectedIndex=i;require(clip.Problem==""&&!clip.Timeline.Playing&&!clip.AudioActive,"Library clip failed to load: "+FiringClips.Names[i]);total+=clip.Timeline.Count;if(old!=null){bool freed=false;try{int width=old.Width;}catch(ArgumentException){freed=true;}require(freed,"Previous clip image retained");}old=clip.CurrentImage;}
                require(total==5641,"Library frame count");clip.Selector.SelectedIndex=0;
            });
            check("Simulated audio clock controls video, pause, mute, seek and completion",delegate {
                var backend=new SimulatedClipOutput();using(var probe=new ClipStudy(assets,backend)){
                    tabs.TabPages[2].Controls.Add(probe);probe.BringToFront();probe.SetSound(true);probe.PlayButton.PerformClick();backend.Samples=12*441;PumpVisuals(100);
                    require(probe.Timeline.Frame==12&&probe.Timeline.Playing,"Video did not follow device samples");
                    probe.Pause();int frame=probe.Timeline.Frame;PumpVisuals(60);require(probe.Timeline.Frame==frame&&backend.Paused,"Audio/video pause");
                    probe.PlayButton.PerformClick();probe.SetSound(false);require(!backend.Active&&probe.Timeline.Playing&&probe.Timeline.Frame==12,"Mute changed video or left audio running");
                    probe.SetSound(true);require(backend.Active&&backend.Samples==12*441,"Unmute did not seek to current position");
                    probe.NextButton.PerformClick();require(probe.Timeline.Frame==13&&!probe.Timeline.Playing&&!backend.Active,"Frame step did not stop/seek audio");
                    probe.PlayButton.PerformClick();require(backend.Samples==13*441,"Playback after stepping used stale audio");backend.Samples=46*441;PumpVisuals(60);
                    require(probe.Timeline.Completed&&!backend.Active,"Device completion did not end/clean up player");
                    probe.PlayButton.PerformClick();require(backend.Active&&backend.Samples==0,"Replay did not restart soundtrack");probe.Selector.SelectedIndex=1;require(!backend.Active&&!probe.Timeline.Playing&&probe.Timeline.Frame==0,"Switch left old soundtrack active");
                }require(!backend.Active&&backend.Disposed,"Dispose did not release audio backend");
            });
            check("Simulated audio failure is visible and visual playback can finish",delegate {
                var backend=new SimulatedClipOutput{FailStart=true};using(var probe=new ClipStudy(assets,backend)){
                    tabs.TabPages[2].Controls.Add(probe);probe.BringToFront();probe.SetSound(true);probe.PlayButton.PerformClick();PumpVisuals(130);
                    require(probe.Problem.Contains("injected")&&probe.Timeline.Playing&&probe.Timeline.Frame>0&&!probe.AudioActive,"Failed playback trapped progress or hid error");
                    probe.EndButton.PerformClick();require(probe.Timeline.Completed&&!probe.Timeline.Playing,"Skip failed after audio error");
                }
            });
            check("Global Sound control applies to the study and preserves paused state",delegate {
                clip.SetSound(false);sound.Enabled=false;UpdateView();tabs.SelectedIndex=2;
                audio.PerformClick();require(sound.Enabled&&preferences.Sound&&!clip.Timeline.Playing&&!clip.AudioActive,"Sound on autoplayed paused clip");
                audio.PerformClick();require(!sound.Enabled&&!preferences.Sound&&!clip.AudioActive,"Sound off retained audio");
                require(clip.Selector.Right<=clip.Selector.Parent.ClientSize.Width,"Library selector clipped");
            });
            check("Opt-in battle movies reserve shots and held confirmations skip only once",delegate {
                opponentTimer.Stop();rules.SelectedIndex=0;opponent.SelectedIndex=1;preferences.MovieTurns=true;seed=940;NewGame();auto.PerformClick();start.PerformClick();
                Point target=State.Enemy.Ships[0].Cells[0];Ship ship=State.Enemy.Ships[0];board.KeyboardCell=target;board.Focus();down.Remove(Keys.Enter);OnKey(this,new KeyEventArgs(Keys.Enter));
                require(State.Pending!=null&&State.Pending.Stage==ShotStage.Firing&&battleMovie.Visible&&board.Visible&&board.MovieFrame!=null&&ship.Hits==0&&!firingFleet.Enabled&&!movies.Enabled,"Movie reservation/overlay/gates");
                OnKey(this,new KeyEventArgs(Keys.Enter));require(State.Pending.Stage==ShotStage.Firing&&ship.Hits==0,"Held firing confirmation skipped");
                down.Remove(Keys.Space);OnKey(this,new KeyEventArgs(Keys.Space));require(State.Pending.Stage==ShotStage.Travel&&ship.Hits==0,"First skip did not enter travel");
                OnKey(this,new KeyEventArgs(Keys.Space));require(State.Pending.Stage==ShotStage.Travel&&ship.Hits==0,"Held skip applied impact");
                down.Remove(Keys.Space);OnKey(this,new KeyEventArgs(Keys.Space));require(ship.Hits==1&&State.Pending.Stage==ShotStage.Feedback,"Travel did not apply one impact");
                OnKey(this,new KeyEventArgs(Keys.Space));require(State.Pending.Stage==ShotStage.Feedback&&ship.Hits==1,"Held skip advanced feedback");
                down.Remove(Keys.Space);OnKey(this,new KeyEventArgs(Keys.Space));require(State.Pending==null&&State.Phase==Phase.OpponentTurn&&!battleMovie.Visible,"Final handoff");
            });
            check("Live last-frame completion and paused completion advance the right stage",delegate {
                seed=941;NewGame();auto.PerformClick();start.PerformClick();Point target=State.Enemy.Ships[0].Cells[0];Ship ship=State.Enemy.Ships[0];State.Fire(target);UpdateView();
                battleMovie.PreviewFrame(battleMovie.Timeline.Count-1);UpdateView();PumpVisuals(120);require(State.Pending.Stage==ShotStage.Travel&&ship.Hits==0,"Live firing completion");
                State.Paused=true;battleMovie.Timeline.End();UpdateView();require(State.Pending.Stage==ShotStage.Travel&&ship.Hits==0,"Paused completion applied damage");
                pause.PerformClick();require(State.Pending.Stage==ShotStage.Feedback&&ship.Hits==1,"Resume lost deferred completion");
                down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));CompleteBattleStage();require(State.Pending==null&&State.Phase==Phase.Placement&&!battleMovie.Visible&&!battleMovie.AudioActive,"F2 left a pending movie/callback");
            });
            check("Mouse press from a prior movie epoch cannot skip the next stage",delegate {
                NewGame();auto.PerformClick();start.PerformClick();Point target=State.Enemy.Ships[0].Cells[0];State.Fire(target);UpdateView();var pending=State.Pending;
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var mouseDown=typeof(ClipStudy.EpochButton).GetMethod("OnMouseDown",flags);var mouseUp=typeof(ClipStudy.EpochButton).GetMethod("OnMouseUp",flags);
                mouseDown.Invoke(battleMovie.EndButton,new object[]{new MouseEventArgs(MouseButtons.Left,1,5,5,0)});
                require(State.CompleteMovie(pending,ShotStage.Firing),"Controlled stage completion");UpdateView();battleMovie.EndButton.PerformClick();require(pending.Stage==ShotStage.Travel&&pending.Result==null,"Old mouse press skipped new stage");
                mouseUp.Invoke(battleMovie.EndButton,new object[]{new MouseEventArgs(MouseButtons.Left,1,5,5,0)});battleMovie.EndButton.PerformClick();require(pending.Stage==ShotStage.Feedback&&pending.Result.Hit,"Fresh mouse action did not advance");
            });
            check("Failed movie loads pause visibly and require explicit skip or reset",delegate {
                NewGame();auto.PerformClick();start.PerformClick();Point target=State.Enemy.Ships[0].Cells[0];validationFailMovieLoad=true;
                try{State.Fire(target);UpdateView();require(State.Paused&&battleFailure&&State.Pending.Result==null&&State.Message.Contains("Movie unavailable")&&board.MovieFrame==null&&!board.CinematicBackdropActive,"Missing media silently advanced");}
                finally{validationFailMovieLoad=false;}
                battleMovie.EndButton.PerformClick();require(!State.Paused&&State.Pending.Stage==ShotStage.Travel&&State.Pending.Result==null,"Explicit failed-stage skip");
                down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));require(State.Pending==null&&!battleMovie.AudioActive,"Failed-media reset");
            });
            check("Map/focus/display transitions preserve pending shots and frozen audio",delegate {
                NewGame();auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();var pending=State.Pending;
                tabs.SelectedIndex=1;require(State.Paused&&State.Pending==pending&&!battleMovie.Timeline.Playing,"Inspection lost pending shot");tabs.SelectedIndex=0;require(State.Paused,"Return resumed automatically");
                SelectColors(!preferences.RecoveredColors,false);require(State.Pending==pending&&pending.Result==null,"Palette swap changed pending impact");
                pause.PerformClick();require(!State.Paused&&battleMovie.Timeline.Playing,"Resume did not continue stage");
                var deactivate=typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);deactivate.Invoke(this,new object[]{EventArgs.Empty});require(State.Paused&&!battleMovie.Timeline.Playing&&State.Pending==pending,"Focus loss did not pause movie/shot");
            });
            check("Default-setting hit and miss movies load, pause and hand off after completion",delegate {
                preferences.MovieTurns=true;opponent.SelectedIndex=0;NewGame();auto.PerformClick();start.PerformClick();Point target=State.Enemy.Ships[0].Cells[0];State.Fire(target);UpdateView();battleMovie.EndButton.PerformClick();battleMovie.EndButton.PerformClick();var pending=State.Pending;
                require(pending!=null&&pending.Stage==ShotStage.Feedback&&pending.StageLabel=="Hit"&&pending.FeedbackId!=137&&battleMovie.Selector.SelectedItem.ToString()==pending.MovieName&&battleMovie.Problem==""&&!battleFailure,"Default hit movie was not loaded");
                pause.PerformClick();long position=battleMovie.Timeline.PositionMilliseconds;PumpVisuals(100);require(State.Pending==pending&&battleMovie.Timeline.PositionMilliseconds==position,"Paused impact advanced");pause.PerformClick();battleMovie.EndButton.PerformClick();require(State.Pending==null&&State.Phase==Phase.OpponentTurn,"Hit movie did not hand off");
                NewGame();auto.PerformClick();start.PerformClick();for(int x=0;x<10;x++)for(int y=0;y<10;y++)if(State.Enemy.ShipAt(new Point(x,y))==null)target=new Point(x,y);State.Fire(target);UpdateView();battleMovie.EndButton.PerformClick();battleMovie.EndButton.PerformClick();require(State.Pending.StageLabel=="Miss"&&State.Pending.FeedbackId!=136&&!battleFailure&&battleMovie.Problem=="","Default miss movie was not loaded");battleMovie.PreviewFrame(battleMovie.Timeline.Count-1);UpdateView();PumpVisuals(120);require(State.Pending==null&&State.Phase==Phase.OpponentTurn,"Natural miss completion failed");
            });
            check("Final sinking movie defers victory and F2 cancels it safely",delegate {
                NewGame();auto.PerformClick();start.PerformClick();State.Enemy=new Board();State.Enemy.Place(new Point(0,0),1,true,"Last battleship",1);State.Fire(new Point(0,0));UpdateView();battleMovie.EndButton.PerformClick();battleMovie.EndButton.PerformClick();require(State.Pending.StageLabel=="Sinking"&&State.Pending.MovieName=="BATTSINK"&&State.Phase==Phase.PlayerTurn&&!battleFailure,"Final sink movie omitted");battleMovie.EndButton.PerformClick();require(State.Phase==Phase.Finished&&State.Pending==null&&!battleMovie.Visible,"Victory after sink movie");
                NewGame();auto.PerformClick();start.PerformClick();State.Enemy=new Board();State.Enemy.Place(new Point(0,0),1,true,"Last battleship",1);State.Fire(new Point(0,0));UpdateView();battleMovie.EndButton.PerformClick();battleMovie.EndButton.PerformClick();down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));CompleteBattleStage();require(State.Pending==null&&State.Phase==Phase.Placement&&!battleMovie.AudioActive,"Reset retained sink callback");
            });
            check("Movie backdrop changes behind intact grids and playback controls never overlap",delegate {
                preferences.MovieTurns=true;opponent.SelectedIndex=0;NewGame();auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();State.Paused=true;UpdateView();Bitmap borrowed=board.MovieFrame;
                using(var red=new Bitmap(160,120))using(var green=new Bitmap(160,120)){
                    using(Graphics g=Graphics.FromImage(red))g.Clear(Color.Red);using(Graphics g=Graphics.FromImage(green))g.Clear(Color.Green);
                    try{foreach(Size size in new[]{new Size(1236,879),MinimumSize}){
                        Size=size;PerformLayout();battleField.PerformLayout();Application.DoEvents();require(board.Visible&&battleMovie.Visible&&!board.Bounds.IntersectsWith(battleMovie.Bounds)&&battleMovie.Bottom<=battleField.ClientSize.Height,"Board/playback overlap");RectangleF r=board.ImageBounds;Point open=new Point((int)(r.Left+r.Width*300/640),(int)(r.Top+r.Height*80/board.ArtHeight)),grid=new Point((int)(r.Left+r.Width*223/640),(int)(r.Top+r.Height*305/board.ArtHeight));
                        State.Paused=false;using(var a=new Bitmap(board.Width,board.Height))using(var b=new Bitmap(board.Width,board.Height)){board.MovieFrame=red;board.DrawToBitmap(a,board.ClientRectangle);board.MovieFrame=green;board.DrawToBitmap(b,board.ClientRectangle);require(a.GetPixel(open.X,open.Y).ToArgb()==Color.Red.ToArgb()&&b.GetPixel(open.X,open.Y).ToArgb()==Color.Green.ToArgb(),"Background frame did not render");require(a.GetPixel(grid.X,grid.Y).ToArgb()==b.GetPixel(grid.X,grid.Y).ToArgb(),"Video covered the opaque grid");Point surface=new Point((int)(r.Left+r.Width*359/640),(int)(r.Top+r.Height*175/board.ArtHeight));Color first=a.GetPixel(surface.X,surface.Y),second=b.GetPixel(surface.X,surface.Y);require(first.R-second.R>=73&&first.R-second.R<=79&&second.G-first.G>=35&&second.G-first.G<=41,"Surface did not blend at the approved 30% transparency");}State.Paused=true;
                    }}finally{State.Paused=true;board.MovieFrame=borrowed;board.Invalidate();}
                }
            });
            check("Backdrop survives palette replacement and reset clears borrowed video safely",delegate {
                Bitmap old=backdropForeground,frame=board.MovieFrame;var pending=State.Pending;SelectColors(!preferences.RecoveredColors,false);bool disposed=false;try{int width=old.Width;}catch(ArgumentException){disposed=true;}require(disposed&&board.ForegroundArt==backdropForeground&&board.MovieFrame==frame&&State.Pending==pending&&State.Paused,"Palette replacement lost backdrop/lifetime state");
                down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));CompleteBattleStage();require(State.Pending==null&&board.MovieFrame==null&&!battleMovie.Visible&&board.Visible&&battleField.RowStyles[1].Height==0,"Reset left movie/backdrop row");
            });
            check("Cinematic transparency persists on pause and restores after completion/reset/failure",delegate {
                preferences.MovieTurns=true;opponent.SelectedIndex=0;NewGame();auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();require(board.CinematicBackdropActive,"Cinematic backdrop not active");pause.PerformClick();Bitmap frozen=board.MovieFrame;PumpVisuals(100);require(State.Paused&&board.MovieFrame==frozen&&board.CinematicBackdropActive,"Pause removed cinematic transparency");pause.PerformClick();while(State.Pending!=null){var pending=State.Pending;require(State.CompleteMovie(pending,pending.Stage),"Controlled cinematic completion");}UpdateView();require(board.MovieFrame==null&&!board.CinematicBackdropActive&&!battleMovie.Visible,"Completion retained translucent board");
                NewGame();auto.PerformClick();start.PerformClick();validationFailMovieLoad=true;try{State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();require(battleFailure&&State.Paused&&board.MovieFrame==null&&!board.CinematicBackdropActive,"Failed media retained transparency");}finally{validationFailMovieLoad=false;}
                down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));require(State.Phase==Phase.Placement&&board.MovieFrame==null&&!board.CinematicBackdropActive,"Reset retained transparency");
            });
            check("High-detail selection preserves deployment/study frame and loads all 61 original clips",delegate {
                NewGame();auto.PerformClick();Board placed=State.Player;tabs.SelectedIndex=2;clip.Selector.SelectedIndex=0;clip.PreviewFrame(20);Bitmap previous=clip.CurrentImage;SetMovieDetail(true,false);bool freed=false;try{int width=previous.Width;}catch(ArgumentException){freed=true;}require(preferences.HighMovies&&highMovies.Checked&&clip.HighDetail&&battleMovie.HighDetail&&clip.CurrentImage.Width==320&&clip.CurrentImage.Height==240&&clip.Timeline.Frame==20&&State.Player==placed&&State.Player.Ships.Count==5&&freed,"Detail switch changed deployment/frame or retained image");
                int frames=0;for(int i=0;i<FiringClips.Names.Length;i++){clip.Selector.SelectedIndex=i;require(clip.Problem==""&&clip.CurrentImage.Width==320&&clip.CurrentImage.Height==240&&!clip.Timeline.Playing,"High library clip failed: "+FiringClips.Names[i]);frames+=clip.Timeline.Count;}require(frames==5641,"High library frame totals");
            });
            check("High-detail battle keeps transparent background, locks quality and survives F2",delegate {
                tabs.SelectedIndex=0;preferences.MovieTurns=true;NewGame();auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();Bitmap firing=board.MovieFrame;require(!highMovies.Enabled&&preferences.HighMovies&&board.CinematicBackdropActive&&firing.Width==320&&battleMovie.CurrentImage.Height==240,"High cinematic not in background");SetMovieDetail(false,false);require(preferences.HighMovies&&board.MovieFrame==firing,"Mid-battle quality changed");battleMovie.EndButton.PerformClick();battleMovie.EndButton.PerformClick();require(State.Pending.Stage==ShotStage.Feedback&&board.MovieFrame.Width==320&&board.CinematicBackdropActive,"High feedback failed");down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));require(State.Phase==Phase.Placement&&preferences.HighMovies&&highMovies.Enabled&&highMovies.Checked&&board.MovieFrame==null,"F2 lost detail/restoration");SetMovieDetail(false,false);require(!preferences.HighMovies&&clip.CurrentImage.Width==160&&battleMovie.CurrentImage.Width==160,"Low comparison did not restore");
            });
            check("Native HUD follows firing, feedback and opposing-turn phases without stale results",delegate {
                preferences.MovieTurns=true;NewGame();auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();State.Paused=true;UpdateView();
                require(ClassicHud.Phase(State)==1&&board.CurrentHudCommands.FindAll(c=>c.Panel).Count==4&&board.CurrentHudCommands.FindAll(c=>!c.Panel&&(c.Frame==2||c.Frame==3)).Count==0,"Firing HUD phase/panels");
                State.Paused=false;State.CompleteMovie(State.Pending,ShotStage.Firing);State.CompleteMovie(State.Pending,ShotStage.Travel);State.Paused=true;UpdateView();require(ClassicHud.Phase(State)==3&&board.CurrentHudCommands.Exists(c=>!c.Panel&&c.Frame==3&&c.At==new Point(512,272)),"Hit card absent from feedback");
                State.Paused=false;State.CompleteMovie(State.Pending,ShotStage.Feedback);State.Paused=true;UpdateView();require(ClassicHud.Phase(State)==4&&board.CurrentHudCommands.Exists(c=>!c.Panel&&c.Frame==1&&c.At==new Point(382,392))&&board.CurrentHudCommands.Exists(c=>!c.Panel&&c.Frame==3&&c.At==new Point(512,272)),"Opposing turn HUD handoff");
                State.Paused=false;State.AdvanceOpponent();UpdateView();State.Paused=true;UpdateView();require(ClassicHud.Phase(State)==5&&!board.CurrentHudCommands.Exists(c=>!c.Panel&&(c.Frame==2||c.Frame==3)),"Prior result persisted into opposing firing");
                down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));require(board.CurrentHudCommands==null&&board.MovieFrame==null,"Reset retained native battle HUD");
            });
            check("Recovered viewport stays 4:3 and native HUD backing remains opaque during video",delegate {
                preferences.MovieTurns=true;NewGame();auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();State.Paused=true;UpdateView();Bitmap borrowed=board.MovieFrame;
                using(var red=new Bitmap(160,120))using(var green=new Bitmap(160,120)){using(Graphics g=Graphics.FromImage(red))g.Clear(Color.Red);using(Graphics g=Graphics.FromImage(green))g.Clear(Color.Green);try{foreach(Size size in new[]{new Size(1236,879),MinimumSize}){Size=size;PerformLayout();Application.DoEvents();RectangleF r=board.ImageBounds;require(board.ArtHeight==480&&Math.Abs(r.Width/r.Height-4f/3)<.001,"Recovered viewport ratio");Point backing=new Point((int)(r.Left+r.Width*10/640),(int)(r.Top+r.Height*20/480));State.Paused=false;using(var a=new Bitmap(board.Width,board.Height))using(var b=new Bitmap(board.Width,board.Height)){board.MovieFrame=red;board.DrawToBitmap(a,board.ClientRectangle);board.MovieFrame=green;board.DrawToBitmap(b,board.ClientRectangle);require(a.GetPixel(backing.X,backing.Y).ToArgb()==b.GetPixel(backing.X,backing.Y).ToArgb(),"Video changed opaque HUD backing");}State.Paused=true;}}finally{State.Paused=true;board.MovieFrame=borrowed;}}
                Bitmap old=hudImages.Panel(0);SelectColors(!preferences.RecoveredColors,false);bool disposed=false;try{int width=old.Width;}catch(ArgumentException){disposed=true;}require(disposed&&board.Hud==hudImages,"HUD palette resource lifetime");
                NewGame();rules.SelectedIndex=1;require(board.ArtHeight==512,"Legacy comparison viewport changed");rules.SelectedIndex=0;
            });
            check("Battle mode remains opt-in, retained by F2, disabled for prototype and fits minimum",delegate {
                NewGame();require(State.MovieTurns&&movies.Checked,"Mode preference not retained");rules.SelectedIndex=1;require(!State.MovieTurns&&!movies.Enabled,"Prototype acquired movie mode");rules.SelectedIndex=0;require(State.MovieTurns,"Recovered mode lost on preset change");
                auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();Size=MinimumSize;PerformLayout();Application.DoEvents();
                require(battleMovie.Right<=battleField.ClientSize.Width&&battleMovie.Width==board.Width&&board.Bottom<=battleMovie.Top&&battleMovie.EndButton.Right<=battleMovie.EndButton.Parent.ClientSize.Width,"Battle movie layout clipped/covered sidebar");
                preferences.MovieTurns=false;NewGame();require(!State.MovieTurns&&!battleMovie.Visible,"Instant comparison not restored");
            });
            check("Native ending movies keep20fps and320x240 independently of combat quality",delegate {
                tabs.SelectedIndex=0;NewGame();auto.PerformClick();Board placed=State.Player;tabs.SelectedIndex=2;
                foreach(string name in MovieCatalog.Endings){clip.Selector.SelectedItem=name;require(clip.Problem==""&&clip.Timeline.FrameMilliseconds==50&&clip.Timeline.Count==(name=="VICTORY"?661:680)&&clip.CurrentImage.Width==320&&clip.CachedBitmapCount==1,"Ending load/context");clip.PreviewFrame(400);SetMovieDetail(true,false);SetMovieDetail(false,false);require(clip.Timeline.Frame==400&&clip.CurrentImage.Width==320&&clip.Timeline.PositionMilliseconds==20000&&State.Player==placed&&placed.Ships.Count==5&&clip.CachedBitmapCount==1,"Ending quality toggle changed deployment/frame/native detail");}
                clip.Selector.SelectedIndex=0;require(clip.Timeline.FrameMilliseconds==40&&clip.CurrentImage.Width==160,"Combat25fps/low comparison did not return");
            });
            check("Bitmap handoff preserves borrowed frame during callbacks then releases it",delegate {
                using(var probe=new ClipStudy(assets)){probe.Selector.SelectedItem="VICTORY";Bitmap previous=probe.CurrentImage;bool called=false;probe.FrameChanged+=delegate{if(!called&&probe.Timeline.Frame==1){require(previous.Width==320&&probe.CurrentImage!=previous&&probe.CachedBitmapCount==2,"Borrowed frame was invalid during handoff");called=true;}};probe.NextButton.PerformClick();bool freed=false;try{int width=previous.Width;}catch(ArgumentException){freed=true;}require(called&&freed&&probe.CachedBitmapCount==1,"Retired frame retained after callback");Bitmap frozen=probe.CurrentImage;probe.Pause();require(probe.CurrentImage==frozen,"Pause replaced the displayed bitmap");probe.Selector.SelectedIndex=0;require(probe.CachedBitmapCount==1&&probe.CurrentImage.Width==160,"Clip change retained long-movie bitmaps");}
            });
            check("20fps finite audio completes fractional-sample endings without false fallback",delegate {
                var backend=new SimulatedClipOutput();using(var probe=new ClipStudy(assets,backend)){tabs.SelectedIndex=2;tabs.TabPages[2].Controls.Add(probe);probe.BringToFront();probe.SetSound(true);int completions=0;probe.PlaybackCompleted+=delegate{completions++;};
                    foreach(string name in MovieCatalog.Endings){probe.Selector.SelectedItem=name;probe.PlayButton.PerformClick();int samples=ClipSound.FrameSample(probe.Timeline.Count,50);backend.Samples=samples-1;PumpVisuals(80);require(!probe.Timeline.Completed&&probe.Timeline.Frame==probe.Timeline.Count-1,"Ending last frame ended early");probe.Pause();int frame=probe.Timeline.Frame;PumpVisuals(60);require(probe.Timeline.Frame==frame&&backend.Paused,"Ending pause advanced");probe.PlayButton.PerformClick();backend.Samples=samples;PumpVisuals(80);require(probe.Timeline.Completed&&!probe.AudioActive&&probe.Problem==""&&probe.CachedBitmapCount==1,"Finite fractional sample caused stall/fallback");int once=completions;PumpVisuals(80);require(completions==once,"Ending completion repeated");probe.RestartButton.PerformClick();require(probe.Timeline.Frame==0&&!probe.Timeline.Completed&&!probe.AudioActive,"Ending restart retained completion/audio");}require(completions==2,"Ending completion count");}
            });
            check("Finished battle points to ending study and resets without shooting advice",delegate {
                tabs.SelectedIndex=0;preferences.MovieTurns=false;NewGame();auto.PerformClick();start.PerformClick();State.Enemy=new Board();State.Enemy.Place(new Point(0,0),1,true,"Terminal fixture",1);State.Fire(new Point(0,0));UpdateView();require(State.Phase==Phase.Finished&&hint.Text.Contains("VICTORY")&&hint.Text.Contains("F2")&&!hint.Text.Contains("Fire on")&&tabs.TabPages[2].Text=="Combat movies"&&!pause.Enabled,"Terminal guidance or movie tab label");down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));require(State.Phase==Phase.Placement&&hint.Text.Contains("place")&&board.MovieFrame==null,"Reset retained terminal guidance/media");clip.Selector.SelectedIndex=0;
            });
            check("Result cards pause, skip one stage, return and reset without changing battle",delegate {
                tabs.SelectedIndex=3;ending.SetSound(false);ending.StartOutcome(true);var game=State;int events=State.Events.Count;
                require(ending.Problem==""&&ending.Timeline.Phase==EndingPhase.Reveal&&ending.CurrentImage.Width==640,"Result card load");
                ending.SkipButton.PerformClick();require(ending.Timeline.Phase==EndingPhase.Hold,"Reveal skip");ending.PlayButton.PerformClick();int hold=ending.Timeline.RemainingHold;PumpVisuals(100);require(!ending.Timeline.Playing&&ending.Timeline.RemainingHold==hold,"Hold pause advanced");
                ending.PlayButton.PerformClick();ending.SkipButton.PerformClick();require(ending.Timeline.Phase==EndingPhase.CardFade,"Hold skip crossed stage");ending.SkipButton.PerformClick();require(ending.Timeline.Phase==EndingPhase.Movie&&ending.Movie.Timeline.Playing&&ending.CurrentImage.Width==320,"Movie did not begin");
                ending.SkipButton.PerformClick();require(ending.Timeline.Phase==EndingPhase.MovieFade&&!ending.Movie.AudioActive,"Movie skip crossed fade");ending.SkipButton.PerformClick();require(ending.Timeline.Phase==EndingPhase.Complete&&ending.CurrentImage==null,"Final clear");
                require(tabs.SelectedIndex==4&&front.HasOpened,"Ending did not hand off to title");front.BackButton.PerformClick();require(tabs.SelectedIndex==0&&State==game&&State.Events.Count==events,"Return changed battle");
                tabs.SelectedIndex=3;ending.StartOutcome(false);ending.SkipButton.PerformClick();ending.SkipButton.PerformClick();ending.SkipButton.PerformClick();tabs.SelectedIndex=0;require(!ending.Timeline.Playing&&!ending.Movie.Timeline.Playing,"Leaving result resumed movie");
                tabs.SelectedIndex=3;Size=MinimumSize;PerformLayout();Application.DoEvents();foreach(var button in new[]{ending.PlayButton,ending.RestartButton,ending.SkipButton,ending.BackButton})require(button.Right<=button.Parent.ClientSize.Width&&button.Bottom<=button.Parent.ClientSize.Height,"Result control clipped: "+button.Text);
                down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));require(ending.Timeline==null&&ending.CurrentImage==null&&!ending.Movie.AudioActive&&tabs.SelectedIndex==0&&State.Phase==Phase.Placement,"F2 retained result/media");
            });
            check("Finished Watch result link selects the outcome and preserves terminal battle",delegate {
                preferences.MovieTurns=false;NewGame();auto.PerformClick();start.PerformClick();State.Enemy=new Board();State.Enemy.Place(new Point(0,0),1,true,"Terminal fixture",1);State.Fire(new Point(0,0));UpdateView();var game=State;
                typeof(LinkLabel).GetMethod("OnLinkClicked",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(hint,new object[]{new LinkLabelLinkClickedEventArgs(hint.Links[0])});
                require(tabs.SelectedIndex==3&&ending.Selector.SelectedItem.ToString()=="VICTORY"&&ending.Timeline.Playing&&State==game&&State.Phase==Phase.Finished,"Watch link did not preserve terminal state");
                var deactivate=typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);deactivate.Invoke(this,new object[]{EventArgs.Empty});require(!ending.Timeline.Playing,"Focus loss did not pause result");
                down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));require(ending.Timeline==null&&State.Phase==Phase.Placement,"Terminal result reset");
            });
            check("Result movie completion waits through pause and advances once on resume",delegate {
                var backend=new SimulatedClipOutput();using(var probe=new EndingPlayer(assets,backend)){tabs.SelectedIndex=3;tabs.TabPages[3].Controls.Add(probe);probe.BringToFront();probe.SetSound(true);probe.StartOutcome(true);for(int i=0;i<3;i++)probe.SkipButton.PerformClick();
                    require(probe.Timeline.Phase==EndingPhase.Movie&&backend.Active,"Result audio start");probe.Pause();probe.Movie.Timeline.End();PumpVisuals(80);require(probe.Timeline.Phase==EndingPhase.Movie&&!probe.Timeline.Playing,"Paused result completion advanced");probe.PlayButton.PerformClick();require(probe.Timeline.Phase==EndingPhase.MovieFade&&!probe.Movie.Timeline.Playing,"Resume replayed ending");
                    probe.SkipButton.PerformClick();require(probe.Timeline.Phase==EndingPhase.Complete,"Result fade skip");probe.RestartButton.PerformClick();require(probe.Timeline.Phase==EndingPhase.Reveal&&!backend.Active,"Restart retained finite completion");
                    for(int i=0;i<3;i++)probe.SkipButton.PerformClick();backend.Samples=ClipSound.FrameSample(661,50);PumpVisuals(100);require(probe.Timeline.Phase==EndingPhase.MovieFade&&!backend.Active&&probe.Problem=="","Finite audio result handoff");probe.Cancel();require(probe.CurrentImage==null&&!backend.Active&&probe.Timeline==null,"Cancel retained result");
                }require(backend.Disposed,"Result backend not disposed");
            });
            check("Result audio failure remains visible while visual playback and skip work",delegate {
                var backend=new SimulatedClipOutput{FailStart=true};using(var probe=new EndingPlayer(assets,backend)){tabs.SelectedIndex=3;tabs.TabPages[3].Controls.Add(probe);probe.BringToFront();probe.SetSound(true);probe.StartOutcome(false);for(int i=0;i<3;i++)probe.SkipButton.PerformClick();PumpVisuals(100);require(probe.Movie.Problem.Contains("injected")&&probe.Movie.Timeline.Frame>0&&probe.Timeline.Phase==EndingPhase.Movie,"Failed result audio trapped visual progress");probe.SkipButton.PerformClick();require(probe.Timeline.Phase==EndingPhase.MovieFade,"Failed-audio explicit skip");probe.Cancel();}
                tabs.SelectedIndex=0;
            });
            check("Result completion opens title once and preserves the finished battle",delegate {
                NewGame();auto.PerformClick();start.PerformClick();var game=State;int events=State.Events.Count;tabs.SelectedIndex=3;ending.StartOutcome(true);for(int i=0;i<5;i++)ending.SkipButton.PerformClick();require(tabs.SelectedIndex==4&&front.HasOpened&&front.Factor==12,"Missing ending menu handoff");PumpVisuals(650);require(front.Factor==64&&!front.Revealing&&State==game&&State.Events.Count==events&&State.Paused,"Menu entry changed battle or did not finish reveal");int factor=front.Factor;ending.Pause();require(front.Factor==factor&&tabs.SelectedIndex==4,"Completion dispatched twice");front.BackButton.PerformClick();require(tabs.SelectedIndex==0&&State==game&&State.Paused,"Menu return changed/resumed battle");
            });
            check("Title keyboard edges select once and Solo opens current Classic setup",delegate {
                rules.SelectedIndex=0;NewGame();var game=State;tabs.SelectedIndex=4;front.FinishReveal();front.Canvas.Focus();down.Remove(Keys.Down);OnKey(this,new KeyEventArgs(Keys.Down));OnKey(this,new KeyEventArgs(Keys.Down));require(front.Selected==1,"Held title arrow repeated");down.Remove(Keys.Enter);OnKey(this,new KeyEventArgs(Keys.Enter));OnKey(this,new KeyEventArgs(Keys.Enter));require(State==game&&tabs.SelectedIndex==4,"Unavailable Multiplayer changed battle");front.Select(0);down.Remove(Keys.Enter);OnKey(this,new KeyEventArgs(Keys.Enter));require(tabs.SelectedIndex==4&&solo.Visible&&State==game,"Solo did not open Practice setup");solo.FinishReveal();solo.Activate(6);solo.Activate(10);fleet.Activate(2);FinishEntryForCheck();require(tabs.SelectedIndex==0&&State!=game&&State.Phase==Phase.PlayerTurn&&!solo.HasOpened&&State.Rules.FixedY,"Classic setup bridge");
            });
            check("Title pointer maps letterboxing and ignores releases from an earlier entry",delegate {
                NewGame();tabs.SelectedIndex=4;front.FinishReveal();PerformLayout();Application.DoEvents();var game=State;var bounds=front.Canvas.Bounds4x3;Point at=new Point((int)(bounds.X+200*bounds.Width/640),(int)(bounds.Y+245*bounds.Height/480));require(front.Canvas.Hit(at.X,at.Y)==0&&front.Canvas.Hit(-1,-1)==-1,"Title viewport mapping");var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var mouseDown=front.Canvas.GetType().GetMethod("OnMouseDown",flags);var mouseUp=front.Canvas.GetType().GetMethod("OnMouseUp",flags);
                mouseDown.Invoke(front.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});front.Begin();front.FinishReveal();mouseUp.Invoke(front.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});require(State==game&&tabs.SelectedIndex==4,"Old press activated new menu entry");mouseDown.Invoke(front.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});mouseUp.Invoke(front.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});require(State==game&&tabs.SelectedIndex==4&&solo.Visible,"Fresh pointer press did not open Solo");solo.FinishReveal();solo.Activate(6);solo.Activate(10);fleet.Activate(2);FinishEntryForCheck();require(State!=game&&tabs.SelectedIndex==0,"Classic Start after fresh Solo press");
            });
            check("Title fade pauses on focus loss and F2 cancels inline quit and resources",delegate {
                tabs.SelectedIndex=4;front.Begin();PumpVisuals(100);typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(this,new object[]{EventArgs.Empty});int factor=front.Factor;PumpVisuals(100);require(front.Factor==factor&&!front.Revealing,"Focus loss advanced title reveal");front.FadeButton.PerformClick();PumpVisuals(650);require(front.Factor==64,"Explicit title resume");front.Activate(3);require(front.QuitPending&&!IsDisposed,"Quit bypassed inline confirmation");front.CancelQuitButton.PerformClick();require(!front.QuitPending&&!IsDisposed,"Quit cancel closed form");Size=MinimumSize;PerformLayout();Application.DoEvents();foreach(var b in new[]{front.ReplayButton,front.FadeButton,front.BackButton})require(b.Right<=b.Parent.ClientSize.Width,"Title toolbar clipped");Bitmap old=front.CurrentImage;front.Activate(3);down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));bool released=false;try{int width=old.Width;}catch(ArgumentException){released=true;}require(released&&front.CurrentImage==null&&!front.HasOpened&&!front.QuitPending&&tabs.SelectedIndex==0,"F2 retained title resources/quit state");
                using(var probe=new FrontMenu(assets)){int settings=0,quits=0;tabs.SelectedIndex=4;tabs.TabPages[4].Controls.Add(probe);probe.BringToFront();probe.SettingsRequested+=delegate{settings++;};probe.QuitRequested+=delegate{quits++;};probe.Begin();probe.FinishReveal();probe.Activate(2);require(settings==1&&quits==0,"Preferences bridge");probe.Activate(3);probe.QuitButton.PerformClick();require(quits==1,"Confirmed quit event");}tabs.SelectedIndex=0;
            });
            check("Solo defaults and unsupported previews preserve the paused battle",delegate {
                rules.SelectedIndex=0;NewGame();auto.PerformClick();start.PerformClick();var game=State;int events=game.Events.Count;tabs.SelectedIndex=4;front.FinishReveal();front.Activate(0);solo.FinishReveal();require(solo.Visible&&!front.Visible&&solo.State.Type==1&&solo.State.Players==2&&State==game&&State.Paused,"Original Solo default/ownership");solo.Activate(10);require(State==game&&tabs.SelectedIndex==4,"Unsupported mission started a game");solo.Activate(5);require(solo.State.Players==4,"Three-opponent preview");solo.Activate(6);require(solo.State.Type==20&&solo.State.Players==2&&solo.State.Playable,"Classic player clamp");solo.Activate(4);require(solo.State.Players==2,"Classic admitted extra opponent");solo.Activate(1);solo.Activate(2);require(solo.State.Type==20&&State==game&&game.Events.Count==events,"Unavailable setup silently changed mode/game");solo.BackButton.PerformClick();front.FinishReveal();require(front.Visible&&!solo.Visible&&State==game&&State.Paused,"Back lost battle");
            });
            check("Solo Classic Start restores recovered rules and held confirm cannot place a ship",delegate {
                rules.SelectedIndex=1;NewGame();var previous=State;int ai=opponent.SelectedIndex;bool high=preferences.HighMovies,movie=preferences.MovieTurns;tabs.SelectedIndex=4;front.FinishReveal();front.Activate(0);solo.FinishReveal();solo.Activate(6);solo.Select(10);solo.Canvas.Focus();down.Remove(Keys.Enter);OnKey(this,new KeyEventArgs(Keys.Enter));OnKey(this,new KeyEventArgs(Keys.Enter));require(tabs.SelectedIndex==4&&State==previous&&fleet.Visible&&fleet.Draft.Player.Ships.Count==5&&fleet.SelectedShip==-1,"Held confirm entered a fleet move");fleet.Canvas.StartFocused=true;down.Remove(Keys.Enter);OnKey(this,new KeyEventArgs(Keys.Enter));OnKey(this,new KeyEventArgs(Keys.Enter));FinishEntryForCheck();OnKey(this,new KeyEventArgs(Keys.Enter));require(tabs.SelectedIndex==0&&State!=previous&&State.Rules.FixedY&&State.Phase==Phase.PlayerTurn&&State.Player.Ships.Count==5&&State.Pending==null&&State.Enemy.ObservedShots()[0,0]==0,"Classic bridge/held firing gate");require(preferences.HighMovies==high&&preferences.MovieTurns==movie,"Classic Start changed display settings");OnKeyUp(new KeyEventArgs(Keys.Enter));OnKey(this,new KeyEventArgs(Keys.Enter));require(State.Pending!=null||State.Enemy.Shots[0,0],"Released confirmation stayed blocked");
            });
            check("Solo mapped pointer and prior-entry release cannot activate a new setup",delegate {
                NewGame();tabs.SelectedIndex=4;front.FinishReveal();front.Activate(0);solo.FinishReveal();PerformLayout();Application.DoEvents();var box=solo.Canvas.Bounds4x3;Point at=new Point((int)(box.X+500*box.Width/640),(int)(box.Y+100*box.Height/480));require(solo.Canvas.Hit(at.X,at.Y)==6&&solo.Canvas.Hit(-1,-1)==-1,"Solo coordinate mapping");var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var press=solo.Canvas.GetType().GetMethod("OnMouseDown",flags);var release=solo.Canvas.GetType().GetMethod("OnMouseUp",flags);press.Invoke(solo.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});solo.Begin();solo.FinishReveal();release.Invoke(solo.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});require(solo.State.Type==1,"Old press selected new setup");press.Invoke(solo.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});release.Invoke(solo.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});require(solo.State.Type==20,"Fresh pointer failed Classic selection");
            });
            check("Solo focus/tab pause, minimum layout and F2 release setup images",delegate {
                solo.Begin();PumpVisuals(100);typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(this,new object[]{EventArgs.Empty});int factor=solo.Factor;PumpVisuals(90);require(solo.Factor==factor&&!solo.Revealing,"Solo focus pause");solo.FadeButton.PerformClick();PumpVisuals(650);require(solo.Factor==64,"Solo explicit resume");tabs.SelectedIndex=0;tabs.SelectedIndex=4;require(solo.Visible&&solo.State.Type==1,"Tab return lost setup");Size=MinimumSize;PerformLayout();Application.DoEvents();foreach(var b in new[]{solo.BackButton,solo.FadeButton})require(b.Right<=b.Parent.ClientSize.Width,"Solo toolbar clipped");Bitmap old=solo.CurrentImage;down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));bool released=false;try{int width=old.Width;}catch(ArgumentException){released=true;}require(released&&solo.CurrentImage==null&&!solo.HasOpened&&!solo.Visible&&tabs.SelectedIndex==0,"F2 retained Solo bitmap/state");
            });
            check("Fleet move rollback, Back preserves battle, F2 releases draft and minimum layout",delegate{NewGame();auto.PerformClick();start.PerformClick();var prior=State;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);var draft=fleet.Draft;require(State==prior&&prior.Paused&&draft!=prior,"Draft changed active game");var ship=draft.Player.Ships[0];var before=ship.Cells[0];fleet.Cell(ship.Cells[2]);fleet.Cell(new Point(0,0));require(ship.Cells[0]==before&&fleet.SelectedShip==0,"Invalid move mutated fleet");Point? target=null;for(int x=0;x<10;x++)for(int y=0;y<10;y++){var at=new Point(x,y);if(draft.Player.CanMoveClassic(0,at)&&at!=new Point(before.X,before.Y+2))target=at;}require(target.HasValue,"No move fixture");fleet.Cell(target.Value);require(ship.Cells[0]==new Point(target.Value.X,target.Value.Y-2)&&fleet.SelectedShip==-1,"Valid move failed");Size=MinimumSize;PerformLayout();Application.DoEvents();require(fleet.ShuffleButton.Right<=fleet.ShuffleButton.Parent.ClientSize.Width,"Fleet toolbar clipped");fleet.BackButton.PerformClick();require(State==prior&&prior.Paused&&solo.Visible&&!fleet.Visible&&fleet.Draft==null,"Back lost paused battle");solo.Activate(10);Bitmap old=fleet.CurrentImage;down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));bool released=false;try{int width=old.Width;}catch(ArgumentException){released=true;}require(released&&fleet.CurrentImage==null&&fleet.Draft==null&&!fleet.Visible,"Reset retained fleet image/draft");});
            check("Fleet pointer entry epoch and focus loss clear pending actions",delegate{NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);PerformLayout();Application.DoEvents();var game=State;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var press=fleet.Canvas.GetType().GetMethod("OnMouseDown",flags);var release=fleet.Canvas.GetType().GetMethod("OnMouseUp",flags);var box=fleet.Canvas.Bounds4x3;Point at=new Point((int)(box.X+50*box.Width/640),(int)(box.Y+445*box.Height/480));press.Invoke(fleet.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});fleet.Pause();entry.Pause();release.Invoke(fleet.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});require(State==game&&fleet.Visible,"Stale press started battle");var draft=fleet.Draft;fleet.Cell(draft.Player.Ships[0].Cells[2]);typeof(Form).GetMethod("OnDeactivate",flags).Invoke(this,new object[]{EventArgs.Empty});require(fleet.Draft==draft&&fleet.SelectedShip==0,"Focus loss lost draft");fleet.Key(Keys.Escape);require(fleet.SelectedShip==-1&&fleet.Visible,"Escape did not cancel move");press.Invoke(fleet.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});release.Invoke(fleet.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});FinishEntryForCheck();require(State==draft&&State.Phase==Phase.PlayerTurn&&!fleet.Visible,"Fresh mapped Start failed");});
            check("Result handoff cancels an earlier fleet draft and opens the title",delegate{NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);require(fleet.HasOpened,"Missing draft fixture");Bitmap old=fleet.CurrentImage;var game=State;tabs.SelectedIndex=3;ending.StartOutcome(true);for(int i=0;i<5;i++)ending.SkipButton.PerformClick();bool freed=false;try{int width=old.Width;}catch(ArgumentException){freed=true;}require(freed&&fleet.Draft==null&&fleet.CurrentImage==null&&!fleet.Visible&&front.Visible&&front.HasOpened&&tabs.SelectedIndex==4&&State==game,"Result handoff retained draft or missed title");});
            check("Visible Fleet Start accepts native pointer messages at normal and minimum sizes",delegate{foreach(Size size in new[]{new Size(1236,879),MinimumSize}){NewGame();Size=size;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);PerformLayout();Application.DoEvents();Bitmap rendered=fleet.CurrentImage;require(rendered.Width==640&&rendered.Height==480,"Fleet bitmap must match native viewport");var box=fleet.Canvas.Bounds4x3;int px=(int)(box.X+60*box.Width/rendered.Width),py=(int)(box.Y+445*box.Height/rendered.Height);IntPtr point=new IntPtr(px|(py<<16));SendMessage(fleet.Canvas.Handle,0x201,new IntPtr(1),point);SendMessage(fleet.Canvas.Handle,0x202,IntPtr.Zero,point);PumpVisuals(700);require(State.Phase==Phase.PlayerTurn&&tabs.SelectedIndex==0&&State.Player.Ships.Count==5&&!fleet.HasOpened&&!State.Paused,"Click at displayed Start failed at "+size);Point target=State.Enemy.Ships[0].Cells[0];board.KeyboardCell=target;board.Act();require(State.Pending!=null||State.Enemy.Shots[target.X,target.Y],"First shot after Fleet Start was blocked");}});
            check("Loading entry preserves draft, pauses on focus and cancels without replacing battle",delegate{NewGame();auto.PerformClick();start.PerformClick();var prior=State;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);var draft=fleet.Draft;fleet.Activate(2);require(entry.HasOpened&&entry.Visible&&!fleet.Visible&&State==prior&&prior.Paused,"Entry ownership");entry.Pause();int factor=entry.Timeline.Factor;PumpVisuals(100);require(entry.Timeline.Factor==factor&&State==prior,"Paused entry advanced");entry.BackButton.PerformClick();require(!entry.HasOpened&&fleet.Visible&&fleet.Draft==draft&&State==prior,"Entry cancel lost draft/game");fleet.Activate(2);typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(this,new object[]{EventArgs.Empty});factor=entry.Timeline.Factor;PumpVisuals(100);require(!entry.Timeline.Playing&&entry.Timeline.Factor==factor,"Entry focus pause");entry.PauseButton.PerformClick();PumpVisuals(700);require(State==draft&&State.Phase==Phase.PlayerTurn&&entry.CurrentImage==null&&!entry.HasOpened,"Entry resume commit");});
            check("Entry tab loss/reset and result handoff release loading resources",delegate{NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);fleet.Activate(2);var timeline=entry.Timeline;tabs.SelectedIndex=0;PumpVisuals(100);tabs.SelectedIndex=4;require(entry.Timeline==timeline&&!timeline.Playing,"Tab loss advanced entry");Size=MinimumSize;PerformLayout();Application.DoEvents();foreach(var b in new[]{entry.PauseButton,entry.BackButton})require(b.Right<=b.Parent.ClientSize.Width,"Entry toolbar clipped");Bitmap old=entry.CurrentImage;down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));bool freed=false;try{int width=old.Width;}catch(ArgumentException){freed=true;}entry.Advance(10000);require(freed&&!entry.HasOpened&&fleet.Draft==null&&tabs.SelectedIndex==0,"Reset retained entry/callback");tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);fleet.Activate(2);tabs.SelectedIndex=3;ending.StartOutcome(true);for(int i=0;i<5;i++)ending.SkipButton.PerformClick();require(!entry.HasOpened&&!entry.Visible&&fleet.Draft==null&&front.Visible&&front.HasOpened,"Result handoff retained entry");});
            check("Original battle messages follow ready/fire/travel/sink and palette changes",delegate {
                rules.SelectedIndex=0;preferences.MovieTurns=true;NewGame();auto.PerformClick();start.PerformClick();require(board.CurrentMessages.Count==1&&board.CurrentMessages[0].Text=="Select Target Square","Native ready message");
                State.Enemy=new Board();State.Enemy.Place(new Point(0,0),1,true,"Fixture",1);State.Fire(new Point(0,0));UpdateView();require(board.CurrentMessages[0].Text=="Your Battleship Firing at The Computer","Native firing message");battleMovie.EndButton.PerformClick();require(board.CurrentMessages[0].Text=="Your Battleship Firing at The Computer","Native travel message");battleMovie.EndButton.PerformClick();require(board.CurrentMessages[0].Text=="You Sunk The Computer's Battleship","Native sinking message");
                string text=board.CurrentMessages[0].Text;Bitmap glyph=messageFont.GlyphBitmap(0);SelectColors(!preferences.RecoveredColors,false);bool freed=false;try{int width=glyph.Width;}catch(ArgumentException){freed=true;}require(freed&&board.CurrentMessages[0].Text==text&&board.MessageFont==messageFont,"Palette change retained glyph cache or changed stage text");down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));require(board.CurrentMessages==null,"Reset retained battle messages");
            });
            results.Add("UI checks passed="+(results.Count-failed)+" failed="+failed);
            results.Add("These checks use the real form and button/tab events offscreen. Key events are simulated; desktop focus and physical input remain separate checks.");
            File.WriteAllLines(report,results.ToArray());
            Close();
            return failed==0?0:1;
        }
        void FinishEntryForCheck(){if(entry.Timeline!=null){entry.Resume();entry.Advance(560);}}
        void PumpVisuals(int milliseconds) {
            var watch=Stopwatch.StartNew();
            while(watch.ElapsedMilliseconds<milliseconds){Application.DoEvents();System.Threading.Thread.Sleep(1);}
        }
        sealed class SimulatedClipOutput : IClipOutput {
            public bool FailStart,Paused,Disposed;public bool Active {get;private set;}public int Samples {get;set;}
            public void Start(short[] pcm,int offset){if(FailStart)throw new InvalidOperationException("injected audio open failure");Samples=offset;Active=true;Paused=false;}
            public void Pause(){Paused=true;}public void Resume(){Paused=false;}public void Stop(){Active=false;Paused=false;}public void Dispose(){Stop();Disposed=true;}
        }
        protected override void Dispose(bool disposing) {
            if(disposing&&!disposed) {
                disposed=true;
                opponentTimer.Stop();
                opponentTimer.Dispose();
                visualTimer.Stop();visualTimer.Dispose();displayMenu.Dispose();
                sound.Dispose();
                art.Dispose();backdropForeground.Dispose();
                shipImages.Dispose();
                damageImages.Dispose();hudImages.Dispose();messageFont.Dispose();ending.Dispose();front.Dispose();solo.Dispose();fleet.Dispose();entry.Dispose();
                foreach(var bitmap in rosterImages)if(bitmap!=null)bitmap.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}














