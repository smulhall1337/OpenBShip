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
        Bitmap backdropForeground,legacyArt;ClassicHud hudImages;
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
        readonly ClipStudy clip,battleMovie,archive;
        readonly EndingPlayer ending;readonly FrontMenu front;readonly SoloSetup solo;readonly FleetSetup fleet;readonly EntryPlayer entry;readonly PreferencesPage prefsPage;readonly CreditsPage creditsPage;readonly StartupPage startupPage;
        readonly TableLayoutPanel battleField=new TableLayoutPanel();
        readonly PauseActions pauseActions=new PauseActions();TableLayoutPanel battleSidebar;
        Game returnConfirmationGame;
        PendingShot shownPending;ShotStage shownStage;Game shownGame;
        bool loadingBattle,advancingBattle,battleFailure;
        bool validationFailMovieLoad; // Only --ui-check injects a failed loader; no user setting.
        readonly ToolStripMenuItem movies=new ToolStripMenuItem("Battle movies (experimental)");
        readonly ToolStripMenuItem highMovies=new ToolStripMenuItem("High-detail original movies (320×240)");
        readonly ToolStripMenuItem restartBattle=new ToolStripMenuItem("Restart this battle...");
        readonly ToolStripMenuItem saveBattle=new ToolStripMenuItem("Save battle checkpoint"),loadBattle=new ToolStripMenuItem("Load saved battle..."),loadPreviousBattle=new ToolStripMenuItem("Load previous checkpoint...");
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
        ClassicVisualSprites visualSprites;
        readonly IndexedImage classicImage;
        readonly DisplaySettings preferences;
        int seed=Environment.TickCount;
        bool disposed,previewMode;
        bool changingOpponent,loadingSavedBattle;
        string validationCheckpointPath;
        int recoveredOpponent;
        readonly Color panel=Color.FromArgb(20,36,52),gold=Color.FromArgb(237,200,128);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
        // --ui-check only: dispatch pointer messages to this form's own canvas.
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window,int message,IntPtr w,IntPtr l);
        public MainForm(string assetDir):this(assetDir,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"UserData","settings.txt")) { }
        internal MainForm(string assetDir,string configurationPath) {
            assets=assetDir;
            board.LoadCursors(Path.Combine(Path.GetDirectoryName(assets),"Programs"));
            settingsPath=Path.GetFullPath(configurationPath);
            preferences=DisplaySettings.Load(settingsPath);
            recoveredOpponent=preferences.Difficulty;
            classicImage=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));
            board.Messages=new ClassicMessages(File.ReadAllBytes(Path.Combine(assets,"Graphics","ENGLISH.TXT")),File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","MessageShipNames.seqbin")));
            RebuildArt(preferences.RecoveredColors);
            sound=new Audio(assets);
            sound.Enabled=preferences.Sound;sound.SetLevel(preferences.FxVolume);
            battleMovie=new ClipStudy(assets,preferences.HighMovies);battleMovie.ConfigureBattle();battleMovie.SetSound(sound.Enabled);battleMovie.Visible=false;
            battleMovie.SetLevel(preferences.FxVolume);
            battleMovie.PlaybackCompleted+=delegate{if(!loadingBattle&&!State.Paused)CompleteBattleStage();};
            battleMovie.SkipRequested+=delegate{SkipBattleStage();};
            battleMovie.FrameChanged+=delegate{SyncBackdropFrame();};
            ending=new EndingPlayer(assets);ending.SetSound(sound.Enabled);if(creditsPage!=null)creditsPage.SetSound(sound.Enabled);if(startupPage!=null)startupPage.SetSound(sound.Enabled);if(archive!=null)archive.SetSound(sound.Enabled);ending.SetLevel(preferences.FxVolume);ending.ReturnRequested+=delegate{tabs.SelectedIndex=0;};
            front=new FrontMenu(assets);front.NewBattleRequested+=delegate{OpenSolo();};front.SettingsRequested+=delegate{OpenPreferences();};front.ReturnRequested+=delegate{tabs.SelectedIndex=0;};front.QuitRequested+=delegate{Close();};front.IntroRequested+=delegate{OpenStartup();};
            creditsPage=new CreditsPage(assets);creditsPage.Visible=false;creditsPage.SetSound(sound.Enabled);if(archive!=null)archive.SetSound(sound.Enabled);creditsPage.SetLevel(preferences.FxVolume);if(startupPage!=null)startupPage.SetLevel(preferences.FxVolume);creditsPage.ReturnRequested+=delegate{CloseCredits();};
            prefsPage=new PreferencesPage(assets);prefsPage.Visible=false;prefsPage.ChangeRequested+=delegate(PreferenceValues values){ApplyPreferences(values);};prefsPage.ReturnRequested+=delegate{ClosePreferences(false);};prefsPage.SaveRequested+=delegate{ClosePreferences(true);};prefsPage.CreditsRequested+=delegate{OpenCredits();};
            startupPage=new StartupPage(assets);startupPage.Visible=false;startupPage.SetSound(sound.Enabled);startupPage.SetLevel(preferences.FxVolume);startupPage.ReturnRequested+=delegate{CloseStartup();};
            solo=new SoloSetup(assets);solo.Visible=false;solo.StartClassicRequested+=delegate{OpenFleet();};solo.ReturnRequested+=delegate{solo.Pause();solo.Visible=false;front.Visible=true;front.Begin();SuppressHeldControls();};
            fleet=new FleetSetup(assets);fleet.Visible=false;fleet.ReturnRequested+=delegate{fleet.Reset();fleet.Visible=false;solo.Visible=true;solo.BringToFront();solo.Canvas.Focus();SuppressHeldControls();UpdateView();};entry=new EntryPlayer(assets);entry.Visible=false;entry.Ready+=delegate{CommitFleet();};entry.ReturnRequested+=delegate{entry.Reset();entry.Visible=false;fleet.Visible=true;fleet.BringToFront();fleet.Canvas.Focus();SuppressHeldControls();UpdateView();};fleet.StartRequested+=delegate{OpenEntry();};
            ending.MenuRequested+=delegate{if(!disposed&&tabs.SelectedIndex==3){startupPage.Reset();startupPage.Visible=false;creditsPage.Reset();creditsPage.Visible=false;prefsPage.Reset();prefsPage.Visible=false;entry.Reset();entry.Visible=false;fleet.Reset();fleet.Visible=false;solo.Reset();solo.Visible=false;front.Visible=true;front.Reset();tabs.SelectedIndex=4;}};
            Text="Battleship Fleet Lab 0.49 — original Fleet screen";
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
            var display=displayButton;display.Text="Options";display.Width=105;display.Height=33;display.Anchor=AnchorStyles.Top|AnchorStyles.Right;display.FlatStyle=FlatStyle.Flat;display.BackColor=panel;display.ForeColor=gold;
            header.Controls.Add(display,1,0);
            display.Click+=delegate {displayMenu.Show(display,0,display.Height);};
            displayMenu.Items.Add(acceptedColors);displayMenu.Items.Add(recoveredColors);displayMenu.Items.Add(new ToolStripSeparator());displayMenu.Items.Add(motion);
            displayMenu.Items.Add(new ToolStripSeparator());displayMenu.Items.Add(movies);
            movies.Click+=delegate{if(State.Phase!=Phase.Placement&&State.Phase!=Phase.Finished)return;preferences.MovieTurns=!movies.Checked;SavePreferences();NewGame();};
            displayMenu.Items.Add(highMovies);highMovies.ToolTipText="Original higher-detail assets. Change before battle; available in the clip library too.";
            displayMenu.Items.Add(new ToolStripSeparator());displayMenu.Items.Add(restartBattle);
            restartBattle.ToolTipText="Restore this battle's starting deployment for another attempt.";
            restartBattle.Click+=delegate{RequestBattleRestart();};
            displayMenu.Items.Add(saveBattle);displayMenu.Items.Add(loadBattle);displayMenu.Items.Add(loadPreviousBattle);
            saveBattle.ToolTipText="Save between turns in Classic with an original opponent. Keeps one previous file as a backup.";
            loadBattle.ToolTipText="Validate and restore the local checkpoint, paused. Original game saves are unsupported.";
            saveBattle.Click+=delegate{SaveCurrentBattle();};loadBattle.Click+=delegate{RequestSavedBattle();};
            loadPreviousBattle.ToolTipText="Recover the checkpoint retained before the latest successful save.";
            loadPreviousBattle.Click+=delegate{RequestBattleCheckpoint(BattleCheckpointPath+".bak",true);};
            highMovies.Click+=delegate{SetMovieDetail(!preferences.HighMovies,true);};
            acceptedColors.Click+=delegate {SelectColors(false,true);};
            recoveredColors.Click+=delegate {SelectColors(true,true);};
            motion.Click+=delegate {preferences.Motion=!preferences.Motion;SavePreferences();UpdateDisplayChecks();};
            displayMenu.Opening+=delegate {
                if(clip!=null)clip.Pause();if(ending!=null)ending.Pause();if(front!=null)front.Pause();if(solo!=null)solo.Pause();if(fleet!=null)fleet.Pause();if(entry!=null)entry.Pause();prefsPage.Pause();creditsPage.Pause();startupPage.Pause();if(archive!=null)archive.Pause();
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
                sound.Enabled=!sound.Enabled;clip.SetSound(sound.Enabled);battleMovie.SetSound(sound.Enabled);ending.SetSound(sound.Enabled);if(creditsPage!=null)creditsPage.SetSound(sound.Enabled);if(startupPage!=null)startupPage.SetSound(sound.Enabled);if(archive!=null)archive.SetSound(sound.Enabled);
                preferences.Sound=sound.Enabled;
                SavePreferences();
                UpdateView();
            });
            opponent.DropDownStyle=ComboBoxStyle.DropDownList;
            opponent.Width=148;
            opponent.Margin=new Padding(3,3,3,0);
            opponent.Items.AddRange(new object[]{"Original target 1","Original target 2","Original target 3","Original target 4","Prototype pursuit"});
            opponent.SelectedIndex=preferences.Difficulty;
            toolbar.Controls.Add(opponent);
            opponent.SelectedIndexChanged+=delegate {
                if(changingOpponent)return;
                recoveredOpponent=opponent.SelectedIndex;
                if(recoveredOpponent>=0&&recoveredOpponent<=3)preferences.Difficulty=recoveredOpponent;
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
                Dock=DockStyle.Right,Width=267,Padding=new Padding(14),ColumnCount=1,RowCount=8,BackColor=panel
            };
            battleSidebar=sidebar;
            foreach(int height in new[]{32,28,44,0,92,140,38})sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
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
                if(loadingSavedBattle)return;
                changingOpponent=true;
                opponent.SelectedIndex=rules.SelectedIndex==1?4:recoveredOpponent;
                changingOpponent=false;
                NewGame();
            };
            AddLabel(sidebar,phase,38,16,gold);
            sidebar.Controls.Add(pauseActions);
            pauseActions.ConfirmationOpened+=delegate{returnConfirmationGame=State;};
            pauseActions.ConfirmationCanceled+=delegate{returnConfirmationGame=null;};
            pauseActions.EndConfirmed+=delegate{EndBattleToTitle();};
            AddLabel(sidebar,hint,82,10,Color.LightSteelBlue);hint.LinkColor=gold;hint.ActiveLinkColor=Color.White;
            hint.LinkClicked+=delegate{if(State.Phase==Phase.Finished){tabs.SelectedIndex=3;ending.StartOutcome(State.Enemy.AllSunk);}};
            firingFleet.Dock=DockStyle.Fill;firingFleet.IntegralHeight=false;
            firingFleet.BackColor=panel;firingFleet.ForeColor=Color.WhiteSmoke;
            firingFleet.BorderStyle=BorderStyle.FixedSingle;firingFleet.Font=new Font("Segoe UI",10);
            firingFleet.AccessibleName="Your firing ship";
            firingFleet.KeyDown+=delegate(object sender,KeyEventArgs e) {
                if(board.Revealing||!State.CanSelectShooter)return;
                int direction=e.KeyCode==Keys.Up||e.KeyCode==Keys.Home?-1:e.KeyCode==Keys.Down||e.KeyCode==Keys.End?1:0;
                if(direction==0)return;
                int candidate=e.KeyCode==Keys.Home?0:e.KeyCode==Keys.End?State.Player.Ships.Count-1:State.PlayerShooter+direction;
                if(e.KeyCode==Keys.Home)direction=1;if(e.KeyCode==Keys.End)direction=-1;
                while(candidate>=0&&candidate<State.Player.Ships.Count&&State.Player.Ships[candidate].Sunk)candidate+=direction;
                if(candidate>=0&&candidate<State.Player.Ships.Count)State.SelectShooter(candidate);
                UpdateView();e.Handled=true;e.SuppressKeyPress=true;
            };
            firingFleet.SelectedIndexChanged+=delegate {
                if(updatingFleet||State==null||board.Revealing)return;
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
            // Hidden pause controls must keep their cell; automatic placement
            // otherwise shifts the fleet list into the shorter instruction row.
            for(int i=0;i<sidebar.Controls.Count;i++)sidebar.SetCellPosition(sidebar.Controls[i],new TableLayoutPanelCellPosition(0,i));
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
            clip=new ClipStudy(assets,preferences.HighMovies);clip.SetSound(sound.Enabled);clip.SetLevel(preferences.FxVolume);
            archive=new ClipStudy(assets,preferences.HighMovies);archive.ConfigureArchive(CreditsCatalog.Load(assets));archive.SetSound(sound.Enabled);archive.SetLevel(preferences.FxVolume);
            var movieTab=new TabPage("Combat movies"){BackColor=BackColor};movieTab.Controls.Add(clip);tabs.TabPages.Add(movieTab);
            var resultTab=new TabPage("Result sequences"){BackColor=BackColor};resultTab.Controls.Add(ending);tabs.TabPages.Add(resultTab);
            var frontTab=new TabPage("Original menu"){BackColor=BackColor};frontTab.Controls.Add(front);frontTab.Controls.Add(solo);frontTab.Controls.Add(fleet);frontTab.Controls.Add(entry);frontTab.Controls.Add(prefsPage);frontTab.Controls.Add(creditsPage);frontTab.Controls.Add(startupPage);tabs.TabPages.Add(frontTab);
            var archiveTab=new TabPage("Movie archive"){BackColor=BackColor};archiveTab.Controls.Add(archive);tabs.TabPages.Add(archiveTab);
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
                clip.Pause();ending.Pause();front.Pause();solo.Pause();fleet.Pause();entry.Pause();prefsPage.Pause();creditsPage.Pause();startupPage.Pause();if(archive!=null)archive.Pause();
                if(tabs.SelectedIndex==4&&!front.HasOpened&&!solo.Visible&&!fleet.Visible&&!entry.Visible&&!prefsPage.Visible&&!creditsPage.Visible&&!startupPage.Visible)front.Begin();SuppressHeldControls();
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
                pauseActions.CancelConfirmation();
                clip.Pause();ending.Pause();front.Pause();solo.Pause();fleet.Pause();entry.Pause();prefsPage.Pause();creditsPage.Pause();startupPage.Pause();if(archive!=null)archive.Pause();
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
            bool wasRevealing=board.Revealing;
            AdvanceBoardReveal(elapsed);
            bool finishing=State!=null&&State.Phase==Phase.Finished&&board.VisualEffectCount>0&&ContainsFocus&&tabs.SelectedIndex==0;
            bool running=preferences.Motion&&State!=null&&!State.Paused&&!wasRevealing&&(State.Phase==Phase.PlayerTurn||State.Phase==Phase.OpponentTurn||finishing);
            if(effectClock.Advance(elapsed,running)){board.DamageTick=effectClock.Tick;board.SynchronizeVisualEffects();board.Invalidate();}
        }
        void UpdateDisplayChecks() {
            acceptedColors.Checked=!preferences.RecoveredColors;recoveredColors.Checked=preferences.RecoveredColors;motion.Checked=preferences.Motion;
            board.AnimateVisualEffects=preferences.Motion;board.SynchronizeVisualEffects();board.Invalidate();
            movies.Checked=State!=null&&State.Phase!=Phase.Placement?State.MovieTurns:preferences.MovieTurns;movies.Enabled=State!=null&&State.Rules.FixedY&&(State.Phase==Phase.Placement||State.Phase==Phase.Finished);
            highMovies.Checked=preferences.HighMovies;highMovies.Enabled=State!=null&&(State.Phase==Phase.Placement||State.Phase==Phase.Finished);
        }
        void AdvanceBoardReveal(long milliseconds){
            var reveal=board.Reveal;if(reveal==null)return;
            if(reveal.Advance(milliseconds,tabs.SelectedIndex==0&&!State.Paused))board.Invalidate();
            if(reveal.Complete){var held=new Keys[down.Count];down.CopyTo(held);board.Reveal=null;SuppressHeldControls();foreach(Keys key in held)down.Add(key);UpdateView();}
        }
        void SetMovieDetail(bool high,bool persist){
            if(State==null||(State.Phase!=Phase.Placement&&State.Phase!=Phase.Finished))return;
            bool previous=preferences.HighMovies;
            if(!clip.SetDetail(high)||!battleMovie.SetDetail(high)||!archive.SetDetail(high)){
                string problem=clip.Problem!=""?clip.Problem:battleMovie.Problem!=""?battleMovie.Problem:archive.Problem;clip.SetDetail(previous);battleMovie.SetDetail(previous);archive.SetDetail(previous);preferences.Problem="Movie detail change failed: "+problem;
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
            Bitmap nextArt=null,nextForeground=null,nextLegacyArt=null;var nextRoster=new List<Bitmap>();ClassicShips nextShips=null;ClassicDamage nextDamage=null;ClassicHud nextHud=null;ClassicSpriteFont nextFont=null;ClassicVisualSprites nextVisuals=null;
            try {
                var grid=ClassicGrid.Compose(classicImage);grid.Palette=palette;nextArt=grid.Bitmap();
                nextLegacyArt=new IndexedImage{Width=classicImage.Width,Height=classicImage.Height,Pixels=classicImage.Pixels,Palette=palette}.Bitmap();
                nextForeground=ClassicBackdrop.Foreground(new IndexedImage{Width=classicImage.Width,Height=classicImage.Height,Pixels=grid.Pixels,Palette=palette});
                foreach(var frame in Assets.Sph(Path.Combine(assets,"Graphics","EO_CLASS.SPH"),palette))nextRoster.Add(frame==null?null:frame.Bitmap());
                nextShips=new ClassicShips(Path.Combine(assets,"Graphics"),palette);
                nextFont=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),palette);
                nextDamage=new ClassicDamage(Path.Combine(assets,"Graphics"),palette);nextHud=new ClassicHud(Path.Combine(assets,"Graphics"),palette);
                nextVisuals=new ClassicVisualSprites(Path.Combine(assets,"Graphics"),File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","ClassicVisuals.seqbin")),palette);
            }catch {
                if(nextArt!=null)nextArt.Dispose();if(nextLegacyArt!=null)nextLegacyArt.Dispose();if(nextForeground!=null)nextForeground.Dispose();foreach(var image in nextRoster)if(image!=null)image.Dispose();
                if(nextShips!=null)nextShips.Dispose();if(nextDamage!=null)nextDamage.Dispose();if(nextHud!=null)nextHud.Dispose();if(nextFont!=null)nextFont.Dispose();if(nextVisuals!=null)nextVisuals.Dispose();throw;
            }
            ClassicSpriteFont previousFont=messageFont;messageFont=nextFont;board.MessageFont=messageFont;
            ClassicVisualSprites previousVisuals=visualSprites;visualSprites=nextVisuals;board.VisualSprites=visualSprites;
            ClassicHud previousHud=hudImages;Bitmap previousArt=art,previousForeground=backdropForeground,previousLegacyArt=legacyArt;ClassicShips previousShips=shipImages;ClassicDamage previousDamage=damageImages;var previousRoster=rosterImages.ToArray();
            hudImages=nextHud;board.Hud=hudImages;art=nextArt;legacyArt=nextLegacyArt;board.LegacyArt=legacyArt;backdropForeground=nextForeground;board.ForegroundArt=backdropForeground;shipImages=nextShips;damageImages=nextDamage;rosterImages.Clear();rosterImages.AddRange(nextRoster);
            board.Art=art;board.Ships=shipImages;board.Damage=damageImages;board.Roster=rosterImages.ToArray();
            if(previousArt!=null)previousArt.Dispose();if(previousLegacyArt!=null)previousLegacyArt.Dispose();if(previousForeground!=null)previousForeground.Dispose();if(previousShips!=null)previousShips.Dispose();if(previousDamage!=null)previousDamage.Dispose();if(previousHud!=null)previousHud.Dispose();if(previousFont!=null)previousFont.Dispose();if(previousVisuals!=null)previousVisuals.Dispose();
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
        PreferenceValues CurrentPreferences(){return new PreferenceValues{Quality=preferences.HighMovies?2:1,Radar=preferences.Radar,Difficulty=preferences.Difficulty,Volume=preferences.FxVolume};}
        void OpenPreferences(){
            if(disposed||tabs.SelectedIndex!=4||!front.Visible||solo.Visible||fleet.Visible||entry.Visible)return;
            front.Pause();front.Visible=false;prefsPage.Visible=true;prefsPage.BringToFront();prefsPage.Begin(CurrentPreferences(),State.Phase==Phase.Placement||State.Phase==Phase.Finished);SuppressHeldControls();UpdateView();
        }
        void ClosePreferences(bool save){
            if(disposed||tabs.SelectedIndex!=4||!prefsPage.Visible||!prefsPage.HasOpened)return;
            if(save){SavePreferences();if(!previewMode&&preferences.Problem!=""){prefsPage.Notice(preferences.Problem);return;}}
            var heldKeys=new Keys[down.Count];down.CopyTo(heldKeys);prefsPage.Reset();prefsPage.Visible=false;front.Visible=true;front.BringToFront();front.Begin();SuppressHeldControls();foreach(Keys key in heldKeys)down.Add(key);UpdateView();
            if(save&&!previewMode)status.Text="Preferences saved as default.";
        }
        void OpenStartup(){
            if(disposed)return;var heldKeys=new Keys[down.Count];down.CopyTo(heldKeys);front.Pause();front.Visible=false;startupPage.Visible=true;startupPage.BringToFront();try{startupPage.Begin();}catch(Exception e){startupPage.Reset();startupPage.Visible=false;front.Visible=true;front.Begin();status.Text="Intro unavailable: "+e.Message;}SuppressHeldControls();foreach(Keys key in heldKeys)down.Add(key);UpdateView();
        }
        void CloseStartup(){
            if(disposed||!startupPage.Visible)return;var heldKeys=new Keys[down.Count];down.CopyTo(heldKeys);startupPage.Reset();startupPage.Visible=false;front.Visible=true;front.BringToFront();front.Begin();SuppressHeldControls();foreach(Keys key in heldKeys)down.Add(key);UpdateView();
        }
        void OpenCredits(){
            if(disposed||tabs.SelectedIndex!=4||!prefsPage.Visible||!prefsPage.HasOpened)return;
            var snapshot=prefsPage.SnapshotIndexed();var heldKeys=new Keys[down.Count];down.CopyTo(heldKeys);prefsPage.Pause();prefsPage.Visible=false;creditsPage.Visible=true;creditsPage.BringToFront();
            try{creditsPage.Begin(snapshot,preferences.HighMovies);}catch(Exception e){creditsPage.Reset();creditsPage.Visible=false;prefsPage.Visible=true;prefsPage.Notice("Credits unavailable: "+e.Message);}
            SuppressHeldControls();foreach(Keys key in heldKeys)down.Add(key);UpdateView();
        }
        void CloseCredits(){
            if(disposed||!creditsPage.Visible)return;var heldKeys=new Keys[down.Count];down.CopyTo(heldKeys);creditsPage.Reset();creditsPage.Visible=false;prefsPage.Reset();prefsPage.Visible=false;front.Visible=true;front.BringToFront();front.Begin();SuppressHeldControls();foreach(Keys key in heldKeys)down.Add(key);UpdateView();
        }
        void ApplyPreferences(PreferenceValues values){
            if(disposed||!prefsPage.Visible||!prefsPage.HasOpened||tabs.SelectedIndex!=4)return;values.Validate();
            bool allowed=State.Phase==Phase.Placement||State.Phase==Phase.Finished;
            if(allowed&&preferences.HighMovies!=(values.Quality==2))SetMovieDetail(values.Quality==2,false);
            if(allowed&&preferences.Difficulty!=values.Difficulty){
                preferences.Difficulty=recoveredOpponent=values.Difficulty;
                if(rules.SelectedIndex==0){changingOpponent=true;opponent.SelectedIndex=values.Difficulty;changingOpponent=false;if(State.Phase==Phase.Placement)CreateBattleState();}
            }
            preferences.Radar=values.Radar;
            if(preferences.FxVolume!=values.Volume){preferences.FxVolume=values.Volume;sound.SetLevel(values.Volume);clip.SetLevel(values.Volume);battleMovie.SetLevel(values.Volume);ending.SetLevel(values.Volume);creditsPage.SetLevel(values.Volume);startupPage.SetLevel(values.Volume);archive.SetLevel(values.Volume);if(!previewMode&&sound.Enabled)sound.Play("place");}
            UpdateView();if(preferences.Problem!="")prefsPage.Notice(preferences.Problem);
        }
        void OpenSolo(){if(disposed)return;front.Pause();front.Visible=false;solo.Visible=true;solo.BringToFront();solo.Begin();SuppressHeldControls();}
        void OpenFleet(){if(disposed)return;int ai=rules.SelectedIndex==1?recoveredOpponent:opponent.SelectedIndex;var draft=new Game(seed,RuleProfile.Recovered,ai==4?OpponentKind.Prototype:OpponentKind.Original,ai<4?ai:0,preferences.MovieTurns);draft.AutoPlace();solo.Pause();solo.Visible=false;fleet.Visible=true;fleet.BringToFront();fleet.Begin(draft);SuppressHeldControls();UpdateView();}
        void OpenEntry(){if(disposed||entry.HasOpened||fleet.Draft==null||fleet.SelectedShip>=0)return;entry.Begin(fleet.CurrentImage);fleet.Pause();fleet.Visible=false;entry.Visible=true;entry.BringToFront();entry.Canvas.Focus();SuppressHeldControls();UpdateView();}
        void CommitFleet(){if(disposed||fleet.Draft==null||fleet.SelectedShip>=0)return;var heldKeys=new Keys[down.Count];down.CopyTo(heldKeys);Game accepted=fleet.Draft;if(rules.SelectedIndex!=0)rules.SelectedIndex=0;else NewGame();State=accepted;board.Game=State;if(!State.Start())throw new InvalidOperationException("Fleet commit failed");board.KeyboardCell=new Point(0,0);board.Reveal=new BoardRevealTimeline();visualLast=visualTime.ElapsedMilliseconds;UpdateView();board.Focus();SuppressHeldControls();foreach(Keys key in heldKeys)down.Add(key);}
        void ClearBattlePresentation() {
            board.Reveal=null;
            board.ResetVisualEffects();
            pauseActions.CancelConfirmation();returnConfirmationGame=null;
            if(creditsPage!=null){creditsPage.Reset();creditsPage.Visible=false;}if(startupPage!=null){startupPage.Reset();startupPage.Visible=false;}
            if(prefsPage!=null){prefsPage.Reset();prefsPage.Visible=false;}
            if(entry!=null){entry.Reset();entry.Visible=false;}
            if(fleet!=null){fleet.Reset();fleet.Visible=false;}
            if(solo!=null){solo.Reset();solo.Visible=false;}if(front!=null)front.Visible=true;
            if(front!=null)front.Reset();
            if(archive!=null)archive.Reset();if(clip!=null)clip.Reset();if(ending!=null)ending.Cancel();
            board.MovieFrame=null;battleMovie.CancelPlayback();battleMovie.Visible=false;shownPending=null;shownGame=null;battleFailure=false;
        }
        void NewGame() {
            ClearBattlePresentation();CreateBattleState();
            tabs.SelectedIndex=0;
            UpdateView();
        }
        void RequestBattleRestart() {
            if(disposed||tabs.SelectedIndex!=0||board.Revealing||!State.CanRestartBattle)return;
            Game expected=State;
            if(State.Phase==Phase.PlayerTurn||State.Phase==Phase.OpponentTurn){State.Paused=true;UpdateView();}
            var answer=MessageBox.Show(this,"Restart this battle from its original fleet deployment? Your current progress will be cleared.","Restart this battle",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2);
            if(!disposed&&answer==DialogResult.Yes)RestartBattleConfirmed(expected);
            SuppressHeldControls();
        }
        bool RestartBattleConfirmed(Game expected) {
            if(disposed||tabs.SelectedIndex!=0||!object.ReferenceEquals(State,expected)||!State.CanRestartBattle)return false;
            Game restarted=State.RestartBattle();
            var heldKeys=new Keys[down.Count];down.CopyTo(heldKeys);
            ClearBattlePresentation();battleMovie.UnloadBattle();State=restarted;State.Message="Battle restarted from its original deployment.";board.Game=State;board.KeyboardCell=new Point(0,0);
            effectClock.Reset();board.DamageTick=0;visualLast=visualTime.ElapsedMilliseconds;
            tabs.SelectedIndex=0;UpdateView();board.Focus();SuppressHeldControls();
            foreach(Keys key in heldKeys)down.Add(key);return true;
        }
        string BattleCheckpointPath {get{return validationCheckpointPath??Path.Combine(Path.GetDirectoryName(settingsPath),"battle.obship");}}
        bool SaveCurrentBattle() {
            if(disposed||tabs.SelectedIndex!=0||board.Revealing||!State.CanSaveBattle)return false;
            State.Paused=true;
            try {State.SaveBattle(BattleCheckpointPath);State.Message="Battle checkpoint saved at turn "+State.Turn+". P or Resume continues.";return true;}
            catch(Exception e) {State.Message="Battle checkpoint was not saved: "+e.Message;return false;}
            finally {UpdateView();SuppressHeldControls();}
        }
        void RequestSavedBattle() {
            RequestBattleCheckpoint(BattleCheckpointPath,false);
        }
        void RequestBattleCheckpoint(string path,bool previous) {
            if(disposed||tabs.SelectedIndex!=0||board.Revealing)return;
            Game expected=State,restored;
            if(State.Phase==Phase.PlayerTurn||State.Phase==Phase.OpponentTurn){State.Paused=true;UpdateView();}
            try {restored=Game.LoadBattle(path);}
            catch(Exception e) {State.Message="Saved battle was not loaded: "+e.Message;UpdateView();SuppressHeldControls();return;}
            // Validate the complete file/replay before offering to replace the
            // current deployment or game. Cancel leaves its state and media intact.
            var answer=MessageBox.Show(this,"Load "+(previous?"the previous checkpoint at turn ":"saved turn ")+restored.Turn+"? This replaces the current deployment or battle. The saved battle will remain paused.","Load saved battle",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2);
            if(answer==DialogResult.Yes)RestoreBattleConfirmed(expected,restored);
            SuppressHeldControls();
        }
        bool RestoreBattleConfirmed(Game expected,Game restored) {
            if(disposed||tabs.SelectedIndex!=0||!object.ReferenceEquals(State,expected)||restored==null||!restored.CanSaveBattle||!restored.Paused)return false;
            var heldKeys=new Keys[down.Count];down.CopyTo(heldKeys);
            ClearBattlePresentation();battleMovie.UnloadBattle();
            loadingSavedBattle=changingOpponent=true;
            try {rules.SelectedIndex=0;opponent.SelectedIndex=restored.OpponentLevel;}
            finally {loadingSavedBattle=changingOpponent=false;}
            State=restored;board.Game=State;board.KeyboardCell=new Point(0,0);
            effectClock.Reset();board.DamageTick=0;visualLast=visualTime.ElapsedMilliseconds;
            tabs.SelectedIndex=0;UpdateView();board.Focus();SuppressHeldControls();
            foreach(Keys key in heldKeys)down.Add(key);return true;
        }
        void CreateBattleState(){
            bool legacy=rules.SelectedIndex==1;
            State=new Game(unchecked(seed++),legacy?RuleProfile.Legacy:RuleProfile.Recovered,legacy||opponent.SelectedIndex==4?OpponentKind.Prototype:OpponentKind.Original,opponent.SelectedIndex<4?opponent.SelectedIndex:0,preferences.MovieTurns);
            board.Game=State;
            board.KeyboardCell=new Point(0,State.Rules.FixedY?State.NextLength/2:0);
            effectClock.Reset();board.DamageTick=0;visualLast=visualTime.ElapsedMilliseconds;
        }
        void Rotate() {
            if(State.Rotate())UpdateView();
        }
        void EndBattleToTitle(){
            if(disposed||tabs.SelectedIndex!=0||!State.Paused||!pauseActions.Confirming||!object.ReferenceEquals(State,returnConfirmationGame)||(State.Phase!=Phase.PlayerTurn&&State.Phase!=Phase.OpponentTurn))return;
            // A fresh deployment detaches the old pending shot and releases its
            // playback before entering title. The original also tears down battle
            // resources; confirmation and the retained Escape binding are modern.
            var heldKeys=new Keys[down.Count];down.CopyTo(heldKeys);
            NewGame();battleMovie.UnloadBattle();tabs.SelectedIndex=4;front.Canvas.Focus();SuppressHeldControls();foreach(Keys key in heldKeys)down.Add(key);
        }
        void OnKey(object sender,KeyEventArgs e) {
            if(disposed)return;
            if(displayMenu.Visible)return;
            if(tabs.SelectedIndex==4&&startupPage.ContainsFocus&&!startupPage.Canvas.ContainsFocus&&(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space)){if(!down.Add(e.KeyCode))e.SuppressKeyPress=true;return;}
            if(tabs.SelectedIndex==4&&creditsPage.ContainsFocus&&!creditsPage.Canvas.ContainsFocus&&(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space)){if(!down.Add(e.KeyCode))e.SuppressKeyPress=true;return;}
            if(tabs.SelectedIndex==4&&prefsPage.ContainsFocus&&!prefsPage.Canvas.ContainsFocus&&(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space)){if(!down.Add(e.KeyCode))e.SuppressKeyPress=true;return;}
            if(pauseActions.ContainsFocus&&(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space)){if(!down.Add(e.KeyCode))e.SuppressKeyPress=true;return;}
            if((rules.ContainsFocus||opponent.ContainsFocus)&&e.KeyCode!=Keys.F2)return;
            if(firingFleet.ContainsFocus&&e.KeyCode!=Keys.F2&&e.KeyCode!=Keys.P&&e.KeyCode!=Keys.Escape)return;
            if(displayButton.ContainsFocus&&(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space)) {
                if(!down.Add(e.KeyCode))e.SuppressKeyPress=true;
                return; // Let the focused native button receive its first confirmation.
            }
            if(tabs.SelectedIndex==4&&(front.Canvas.ContainsFocus||solo.Canvas.ContainsFocus||fleet.Canvas.ContainsFocus||entry.Canvas.ContainsFocus||prefsPage.Canvas.ContainsFocus||creditsPage.Canvas.ContainsFocus||startupPage.Canvas.ContainsFocus)&&e.KeyCode!=Keys.F2){if(!down.Add(e.KeyCode)){e.SuppressKeyPress=true;return;}bool handledMenu=startupPage.Visible?startupPage.Key(e.KeyCode):creditsPage.Visible?creditsPage.Key(e.KeyCode):prefsPage.Visible?prefsPage.Key(e.KeyCode):entry.Visible?entry.Key(e.KeyCode):fleet.Visible?fleet.Key(e.KeyCode):solo.Visible?solo.Key(e.KeyCode):front.Key(e.KeyCode);down.Add(e.KeyCode);if(handledMenu)e.SuppressKeyPress=true;return;}
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
            restartBattle.Enabled=tabs.SelectedIndex==0&&!board.Revealing&&State.CanRestartBattle;
            saveBattle.Enabled=tabs.SelectedIndex==0&&!board.Revealing&&State.CanSaveBattle;
            loadBattle.Enabled=tabs.SelectedIndex==0&&!board.Revealing&&File.Exists(BattleCheckpointPath);
            loadPreviousBattle.Enabled=tabs.SelectedIndex==0&&!board.Revealing&&File.Exists(BattleCheckpointPath+".bak");
            bool showPauseActions=tabs.SelectedIndex==0&&State.Paused&&(State.Phase==Phase.PlayerTurn||State.Phase==Phase.OpponentTurn);
            pauseActions.SetAvailable(showPauseActions);battleSidebar.RowStyles[3].Height=showPauseActions?76:0;
            battleSidebar.RowStyles[4].Height=showPauseActions?48:92;hint.Height=showPauseActions?44:82;
            front.BackButton.Text=State.Phase==Phase.Placement?"Return to deployment":State.Phase==Phase.Finished?"Return to result":"Return to duel";
            prefsPage.SetValues(CurrentPreferences(),State.Phase==Phase.Placement||State.Phase==Phase.Finished);
            SyncBattleMovie();
            board.AnimateVisualEffects=preferences.Motion;board.SynchronizeVisualEffects();
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
            else if(placement&&State.Rules.FixedY)hint.Text+=" Options offers optional battle movies.";
            if(showPauseActions)hint.Text="P or Escape resumes. End battle returns to title after confirmation.";
            if(board.Revealing)hint.Text=State.Paused?"Board reveal paused. P or Resume continues; F2 resets.":"Revealing the battle board. Targeting becomes available when the reveal finishes. P pauses; F2 resets.";
            hint.LinkArea=State.Phase==Phase.Finished?new LinkArea(0,21):new LinkArea(0,0);
            updatingFleet=true;firingFleet.BeginUpdate();firingFleet.Items.Clear();
            for(int i=0; i<State.Rules.Fleet.Length; i++) {
                Ship item=i<State.Player.Ships.Count?State.Player.Ships[i]:null;
                ShipSpec spec=State.Rules.Fleet[i];
                firingFleet.Items.Add((i==State.PlayerShooter?"> ":"  ")+spec.Name+"  "+(item==null?spec.Length+" / unplaced":item.Sunk?"SUNK":(item.Cells.Count-item.Hits)+" / "+item.Cells.Count));
            }
            firingFleet.SelectedIndex=State.PlayerShooter;
            firingFleet.Enabled=active&&!board.Revealing&&State.CanSelectShooter;
            firingFleet.EndUpdate();updatingFleet=false;
            int sunk=0;
            foreach(var item in State.Enemy.Ships)if(item.Sunk)sunk++;
            enemy.Text="OPPONENT: "+sunk+" / 5 ships sunk";
            status.Text=sound.Problem!=""?sound.Problem:preferences.Problem!=""?preferences.Problem:tabs.SelectedIndex==4&&entry.Visible?"Preparing Classic battle. Return to fleet cancels this entry.":tabs.SelectedIndex==4&&fleet.Visible?"Review your fleet. Start Game begins the battle; Back keeps the previous game paused.":tabs.SelectedIndex==2?"Original clip study. Return to Classic duel to play.":State.Message;
            int from=Math.Max(0,State.Events.Count-9);
            history.Text=string.Join(Environment.NewLine,State.Events.GetRange(from,State.Events.Count-from).ToArray());
            board.RefreshInteractionCursor();board.Invalidate();UpdateDisplayChecks();
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
            previewMode=true;sound.Enabled=false;clip.SetSound(false);battleMovie.SetSound(false);ending.SetSound(false);creditsPage.SetSound(false);startupPage.SetSound(false);if(archive!=null)archive.SetSound(false);
            opponentTimer.Stop();visualTimer.Stop();
            ShowInTaskbar=false;
            StartPosition=FormStartPosition.Manual;
            Location=new Point(-20000,-20000);
            preferences.MovieTurns=false;preferences.HighMovies=false;clip.SetDetail(false);battleMovie.SetDetail(false);seed=731; // Stable comparison scene; normal games still use a fresh seed.
            NewGame();
            if(mode=="navigation-title"||mode=="navigation"||mode=="navigation-small"||mode=="message"||mode=="message-small"||mode=="pause"||mode=="pause-small"){if(mode.EndsWith("small"))Size=MinimumSize;preferences.MovieTurns=true;seed=731;NewGame();State.AutoPlace();State.Start();State.SelectShooter(3);State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();State.Paused=true;UpdateView();battleMovie.PreviewFrame(20);if(mode.StartsWith("navigation"))pauseActions.AskButton.PerformClick();}
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
            if(mode=="archive"||mode=="archive-small"){if(mode.EndsWith("small"))Size=MinimumSize;tabs.SelectedIndex=5;archive.Selector.SelectedItem="ISL_DSHS";archive.PreviewFrame(40);Application.DoEvents();}
            if(mode=="intro"||mode=="intro-small"||mode=="startup-logo"){if(mode.EndsWith("small"))Size=MinimumSize;tabs.SelectedIndex=4;front.FinishReveal();front.IntroButton.PerformClick();if(mode!="startup-logo")for(int movie=0;movie<2;movie++){startupPage.Skip();for(int f=0;f<17;f++)startupPage.Advance(40);}for(int f=0;f<(mode=="startup-logo"?150:300);f++)startupPage.Advance(50);startupPage.Pause();Application.DoEvents();}
            if(mode=="credits"||mode=="credits-small"){if(mode.EndsWith("small"))Size=MinimumSize;tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);prefsPage.FinishReveal();prefsPage.Activate(10);creditsPage.Begin(prefsPage.SnapshotIndexed(),preferences.HighMovies,731u);creditsPage.FinishEntry();for(int n=0;n<30;n++)creditsPage.Advance(50);creditsPage.Pause();startupPage.Pause();if(archive!=null)archive.Pause();Application.DoEvents();}
            if(mode=="preferences"||mode=="preferences-small"){if(mode.EndsWith("small"))Size=MinimumSize;tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);prefsPage.FinishReveal();ApplyPreferences(new PreferenceValues{Quality=2,Radar=1,Difficulty=2,Volume=80});Application.DoEvents();}
            if(mode.StartsWith("navigation")){pauseActions.AskButton.PerformClick();if(mode=="navigation-title"){pauseActions.EndButton.PerformClick();front.FinishReveal();}Application.DoEvents();}
            if(mode=="clip"||mode=="clip-small"||mode=="library") {if(mode=="clip-small")Size=MinimumSize;tabs.SelectedIndex=2;if(mode=="library")clip.Selector.SelectedIndex=8;clip.PreviewFrame(mode=="library"?50:20);Application.DoEvents();}
            if(mode=="map") {
                tabs.SelectedIndex=1;
                Application.DoEvents();
                if(!State.Paused)throw new InvalidOperationException("Inspection did not pause the active battle");
            }
            if(mode=="victory"||mode=="lose"||mode=="ending-small"){if(mode=="ending-small")Size=MinimumSize;tabs.SelectedIndex=2;clip.Selector.SelectedItem=mode=="victory"?"VICTORY":"LOSE";clip.PreviewFrame(mode=="victory"?300:400);Application.DoEvents();}
            if(mode=="entry"||mode=="entry-small"){if(mode=="entry-small")Size=MinimumSize;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);fleet.Activate(2);entry.Pause();entry.Timeline.Resume();entry.Advance(1200);entry.Pause();Application.DoEvents();}
            if(mode=="fleet-config"||mode=="fleet-config-small"){if(mode=="fleet-config-small")Size=MinimumSize;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);Application.DoEvents();}
            if(mode=="solo"||mode=="solo-small"){if(mode=="solo-small")Size=MinimumSize;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);Application.DoEvents();}
            if(mode=="quit"||mode=="quit-small"){if(mode=="quit-small")Size=MinimumSize;tabs.SelectedIndex=4;front.FinishReveal();front.Activate(3);Application.DoEvents();}
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
            validationCheckpointPath=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)),"ui-checkpoint-"+Guid.NewGuid().ToString("N"),"battle.obship");
            previewMode=true;sound.Enabled=false;clip.SetSound(false);battleMovie.SetSound(false);ending.SetSound(false);creditsPage.SetSound(false);startupPage.SetSound(false);if(archive!=null)archive.SetSound(false);
            preferences.FxVolume=100;preferences.Difficulty=recoveredOpponent=0;preferences.Radar=0;changingOpponent=true;opponent.SelectedIndex=0;changingOpponent=false;sound.SetLevel(100);clip.SetLevel(100);battleMovie.SetLevel(100);ending.SetLevel(100);
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
            check("Local save/load menu eligibility tracks stable Classic turns and file availability",delegate {
                tabs.SelectedIndex=0;rules.SelectedIndex=0;changingOpponent=true;opponent.SelectedIndex=0;changingOpponent=false;preferences.MovieTurns=true;NewGame();
                require(!saveBattle.Enabled&&!loadBattle.Enabled,"Unsaved deployment checkpoint menu");auto.PerformClick();start.PerformClick();require(saveBattle.Enabled,"Ready Classic save menu");
                State.Fire(new Point(0,0));UpdateView();require(!saveBattle.Enabled,"Firing stage save menu");while(State.Pending!=null){State.CompleteMovie(State.Pending,State.Pending.Stage);UpdateView();require(!saveBattle.Enabled,"Pending/opponent save menu");}
                require(State.AdvanceOpponent(),"Save menu opponent fixture");while(State.Pending!=null)State.CompleteMovie(State.Pending,State.Pending.Stage);UpdateView();
                saveBattle.PerformClick();saveBattle.PerformClick();require(loadPreviousBattle.Enabled&&State.Paused&&loadBattle.Enabled&&File.Exists(BattleCheckpointPath)&&Game.LoadBattle(BattleCheckpointPath).Turn==2,"Save menu did not create paused checkpoint");
                tabs.SelectedIndex=1;UpdateView();require(!saveBattle.Enabled&&!loadBattle.Enabled&&!loadPreviousBattle.Enabled,"Off-duel checkpoint actions enabled");tabs.SelectedIndex=0;
            });
            check("Validated load restores paused battle from legacy mode without changing preferences",delegate {
                Game restored=Game.LoadBattle(BattleCheckpointPath);rules.SelectedIndex=1;auto.PerformClick();start.PerformClick();Game previous=State;
                string choices=preferences.Serialize();require(RestoreBattleConfirmed(previous,restored),"Load replacement rejected");
                require(State==restored&&State.Paused&&State.Phase==Phase.PlayerTurn&&State.Turn==2&&rules.SelectedIndex==0&&opponent.SelectedIndex==0&&State.MovieTurns&&movies.Checked,"Loaded game/UI state");
                require(preferences.Serialize()==choices&&State.CanRestartBattle&&board.MovieFrame==null&&board.VisualEffectCount==0&&effectClock.Tick==0,"Load changed preferences or retained presentation");
                State.Paused=false;require(State.Fire(new Point(1,0)),"Loaded next shot rejected");UpdateView();var pending=State.Pending;require(pending!=null&&board.CinematicBackdropActive,"Loaded movie did not play");
                pause.PerformClick();require(State.Paused,"Loaded movie pause");
                var again=Game.LoadBattle(BattleCheckpointPath);require(RestoreBattleConfirmed(State,again)&&State.Pending==null&&!State.CompleteMovie(pending,pending.Stage)&&!battleMovie.Visible&&board.MovieFrame==null,"Load retained old callback/movie");
            });
            check("Saved battle replacement rejects stale ownership and keeps rejected loads intact",delegate {
                Game previous=State;string fingerprint=Convert.ToBase64String(State.SerializeBattle());var loaded=Game.LoadBattle(BattleCheckpointPath);NewGame();Game current=State;
                require(!RestoreBattleConfirmed(previous,loaded)&&State==current,"Stale load replaced current game");require(!RestoreBattleConfirmed(current,null),"Null load accepted");
                auto.PerformClick();start.PerformClick();State.Paused=true;UpdateView();Game live=State;byte[] saved=File.ReadAllBytes(BattleCheckpointPath);File.WriteAllBytes(BattleCheckpointPath,new byte[8193]);
                try {RequestSavedBattle();require(State==live&&State.Paused&&State.Message.StartsWith("Saved battle was not loaded:"),"Corrupt file changed current game");}finally{File.WriteAllBytes(BattleCheckpointPath,saved);}
                require(Convert.ToBase64String(previous.SerializeBattle())==fingerprint,"Load mutated detached prior game");
            });
            check("Checkpoint menu fits minimum window and resume requires a fresh key edge",delegate {
                var restored=Game.LoadBattle(BattleCheckpointPath);require(RestoreBattleConfirmed(State,restored),"Minimum checkpoint restore");Size=MinimumSize;PerformLayout();
                down.Add(Keys.P);OnKey(this,new KeyEventArgs(Keys.P));require(State.Paused,"Held resume key escaped load pause");down.Remove(Keys.P);OnKey(this,new KeyEventArgs(Keys.P));require(!State.Paused,"Fresh resume edge ignored");down.Remove(Keys.P);
                displayMenu.Show(displayButton,0,displayButton.Height);Application.DoEvents();require(saveBattle.Available&&loadBattle.Available&&loadPreviousBattle.Bounds.Bottom<=displayMenu.ClientSize.Height&&displayMenu.Width<Width,"Checkpoint menu clipped");displayMenu.Close();preferences.MovieTurns=false;NewGame();Size=new Size(1236,879);
            });
            check("Movie toggle after a loaded result follows the displayed battle mode",delegate {
                preferences.MovieTurns=false;var loaded=Game.LoadBattle(BattleCheckpointPath);require(RestoreBattleConfirmed(State,loaded)&&loaded.MovieTurns&&!preferences.MovieTurns,"Loaded mode/default fixture");
                State.Phase=Phase.Finished;UpdateView();require(movies.Enabled&&movies.Checked,"Loaded result movie indicator");movies.PerformClick();require(!preferences.MovieTurns&&!State.MovieTurns&&!movies.Checked&&State.Phase==Phase.Placement,"Movie toggle followed stale default instead of displayed mode");
            });
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
                tabs.SelectedIndex=3;ending.SetSound(false);creditsPage.SetSound(false);startupPage.SetSound(false);if(archive!=null)archive.SetSound(false);ending.StartOutcome(true);var game=State;int events=State.Events.Count;
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
                    for(int i=0;i<3;i++)probe.SkipButton.PerformClick();for(int f=0;f<66;f++)probe.Advance(500);require(probe.Timeline.Phase==EndingPhase.MovieFade&&!backend.Active&&probe.Problem=="","Finite audio result handoff");probe.Cancel();require(probe.CurrentImage==null&&!backend.Active&&probe.Timeline==null,"Cancel retained result");
                }require(backend.Disposed,"Result backend not disposed");
            });
            check("Result source EOF excludes extra dwell, snapshots final raw frame and releases decoded media",delegate {
                foreach(bool victory in new[]{true,false}){var backend=new SimulatedClipOutput();using(var probe=new EndingPlayer(assets,backend)){tabs.SelectedIndex=3;tabs.TabPages[3].Controls.Add(probe);probe.BringToFront();probe.SetSound(true);probe.StartOutcome(victory);for(int i=0;i<3;i++)probe.SkipButton.PerformClick();int raw=probe.Movie.RawFrameCount;require(probe.Movie.Timeline.Frame==0&&probe.Movie.Timeline.Count==raw-1&&backend.LastPcm.Length==ClipSound.FrameSample(raw-1,50),"Result initial frame/PCM prefix");long remain=(raw-2)*50;while(remain>0){long step=Math.Min(remain,500);probe.Advance(step);remain-=step;}require(probe.Timeline.Phase==EndingPhase.Movie&&probe.Movie.Timeline.Frame==raw-2,"Result visible last-frame hold");probe.Advance(49);require(probe.Timeline.Phase==EndingPhase.Movie,"Result ended before native EOF interval");var expected=probe.Movie.IndexedFrame(raw-1);Bitmap borrowed=probe.CurrentImage;probe.Advance(1);require(probe.Timeline.Phase==EndingPhase.MovieFade&&probe.ExitRawFrame==raw-1&&!backend.Active,"Result source EOF snapshot/stop");for(int i=0;i<expected.Pixels.Length;i++)require(expected.Pixels[i]==probe.ExitSnapshot.Pixels[i],"Result raw last frame lost before fade");probe.Advance(320);require(probe.Timeline.Factor==32,"Result half fade");for(int i=0;i<expected.Pixels.Length;i+=991){var c=expected.Palette[expected.Pixels[i]];int index=expected.Pixels[i];Color shown=probe.CurrentImage.GetPixel(i%320,i/320);require(shown.R==(index>=10&&index<=245?c.R/2:c.R)&&shown.G==(index>=10&&index<=245?c.G/2:c.G)&&shown.B==(index>=10&&index<=245?c.B/2:c.B),"Result fade multiplied a previous faded palette");}probe.Advance(360);require(probe.Timeline.Phase==EndingPhase.Complete&&probe.Movie.Timeline==null&&probe.CurrentImage==null&&probe.ExitSnapshot==null,"Result completion retained decoded media");bool freed=false;try{int width=borrowed.Width;}catch(ArgumentException){freed=true;}require(freed,"Result borrowed frame survived final unload");}}
            });
            check("Result skip polls next raw frame while audio completion cannot advance source clock",delegate {
                var backend=new SimulatedClipOutput();using(var probe=new EndingPlayer(assets,backend)){tabs.SelectedIndex=3;tabs.TabPages[3].Controls.Add(probe);probe.BringToFront();probe.SetSound(true);probe.StartOutcome(true);for(int i=0;i<3;i++)probe.SkipButton.PerformClick();backend.Samples=backend.LastPcm.Length;probe.Advance(100);require(probe.Timeline.Phase==EndingPhase.Movie&&probe.Movie.Timeline.Frame==2,"Audio endpoint overrode native visual clock");probe.SkipButton.PerformClick();require(probe.ExitRawFrame==3&&probe.Timeline.Phase==EndingPhase.MovieFade&&!backend.Active,"Native skip did not poll its next frame");probe.Cancel();require(probe.Movie.Timeline==null&&probe.CurrentImage==null&&probe.ExitSnapshot==null,"Result Cancel retained movie/frame");}
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
                using(var probe=new FrontMenu(assets)){int settings=0,quits=0;tabs.SelectedIndex=4;tabs.TabPages[4].Controls.Add(probe);probe.BringToFront();probe.SettingsRequested+=delegate{settings++;};probe.QuitRequested+=delegate{quits++;};probe.Begin();probe.FinishReveal();probe.Activate(2);require(settings==1&&quits==0,"Preferences bridge");probe.Activate(3);probe.QuitButton.PerformClick();for(int q=0;q<17;q++)probe.Advance(40);require(quits==1,"Confirmed quit event");}tabs.SelectedIndex=0;
            });
            check("Startup replay runs original order/fades and returns with battle/settings intact",delegate {
                NewGame();auto.PerformClick();start.PerformClick();var game=State;var settings=preferences.Serialize();tabs.SelectedIndex=4;front.FinishReveal();front.IntroButton.PerformClick();require(startupPage.Visible&&startupPage.HasOpened&&startupPage.Index==0&&startupPage.Movie.Timeline.Frame==0&&startupPage.Movie.Timeline.Count==250&&startupPage.Movie.Problem==""&&State==game&&game.Paused,"Startup first-frame bridge/audio decode");
                byte[] startupPalette=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Evidence","Startup-Palette-HASBRO-0.bin"));var startupFrame=startupPage.Movie.IndexedFrame(0);for(int c=10;c<=245;c++)require(startupFrame.Palette[c].R==startupPalette[c*3]&&startupFrame.Palette[c].G==startupPalette[c*3+1]&&startupFrame.Palette[c].B==startupPalette[c*3+2],"Startup used Credits partial palette");
                for(int i=0;i<3;i++){require(startupPage.Index==i&&startupPage.Stage==1&&startupPage.Factor==64&&startupPage.Movie.Timeline.FrameMilliseconds==50,"Startup source order/cadence");startupPage.Advance(50);require(startupPage.Movie.Timeline.Frame==1,"Startup first wait");startupPage.SkipButton.PerformClick();require(startupPage.Stage==2&&startupPage.Movie.Timeline==null,"Startup skip did not stop/unload movie");for(int t=1;t<=16;t++){startupPage.Advance(40);require(startupPage.Factor==64-4*t,"Startup source fade factor");}startupPage.Advance(40);}
                require(!startupPage.HasOpened&&!startupPage.Visible&&front.Visible&&front.HasOpened&&State==game&&game.Paused&&preferences.Serialize()==settings,"Startup title return changed battle/settings");
            });
            check("Startup held skip, focus/tab pause, minimum controls and F2 release",delegate {
                NewGame();tabs.SelectedIndex=4;front.FinishReveal();down.Add(Keys.Space);front.IntroButton.PerformClick();OnKey(this,new KeyEventArgs(Keys.Space));require(startupPage.Stage==1,"Held confirm skipped initial logo");OnKeyUp(new KeyEventArgs(Keys.Space));OnKey(this,new KeyEventArgs(Keys.Space));OnKey(this,new KeyEventArgs(Keys.Space));require(startupPage.Stage==2,"Released Space did not skip");for(int i=0;i<17;i++)startupPage.Advance(40);OnKey(this,new KeyEventArgs(Keys.Space));require(startupPage.Stage==1&&startupPage.Index==1,"Held skip consumed the next movie");OnKeyUp(new KeyEventArgs(Keys.Space));startupPage.Advance(100);int frame=startupPage.Movie.Timeline.Frame;typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(this,new object[]{EventArgs.Empty});startupPage.Advance(500);require(!startupPage.Playing&&startupPage.Movie.Timeline.Frame==frame,"Startup focus pause");startupPage.Resume();tabs.SelectedIndex=1;PumpVisuals(70);tabs.SelectedIndex=4;require(startupPage.Visible&&!startupPage.Playing&&startupPage.Movie.Timeline.Frame==frame,"Startup tab loss resumed or replaced movie");Size=MinimumSize;PerformLayout();Application.DoEvents();foreach(var b in new[]{startupPage.BackButton,startupPage.PauseButton,startupPage.SkipButton,startupPage.RestartButton})require(b.Visible&&b.Right<=b.Parent.ClientSize.Width,"Startup controls clipped");Bitmap image=startupPage.CurrentImage;down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));bool freed=false;try{int width=image.Width;}catch(ArgumentException){freed=true;}require(freed&&!startupPage.HasOpened&&startupPage.Movie.Timeline==null&&tabs.SelectedIndex==0,"Startup F2 retained resources");
            });
            check("Startup audio gain/mute/pause and failed device keep finite visual exit",delegate {
                var backend=new SimulatedClipOutput();using(var probe=new StartupPage(assets,backend)){tabs.SelectedIndex=4;tabs.TabPages[4].Controls.Add(probe);probe.BringToFront();probe.SetSound(true);probe.Begin();require(backend.Active&&backend.LastPcm.Length==137812&&probe.Movie.Problem=="","Converted stereo jingle finite buffer");probe.Advance(100);probe.Pause();probe.SetLevel(0);probe.Resume();foreach(short sample in backend.LastPcm)require(sample==0,"Startup mute submitted audible samples");probe.SetSound(false);require(!backend.Active,"Startup sound toggle retained output");probe.Skip();for(int i=0;i<17;i++)probe.Advance(40);require(probe.Index==1&&probe.Movie.Problem=="","Silent NMS sequence policy");probe.Back();require(!probe.HasOpened&&!backend.Active,"Startup Back audio lifetime");}require(backend.Disposed,"Startup output disposal");
                var failedBackend=new SimulatedClipOutput{FailStart=true};using(var probe=new StartupPage(assets,failedBackend)){tabs.TabPages[4].Controls.Add(probe);probe.BringToFront();probe.SetSound(true);probe.Begin();probe.Advance(150);require(probe.Movie.Timeline.Frame==3&&probe.Movie.Problem.Contains("injected"),"Startup failed audio trapped movie");probe.Skip();for(int i=0;i<17;i++)probe.Advance(40);require(probe.Index==1,"Startup failed audio trapped next movie");probe.Back();}
            });
            check("Original Quit artwork hotspots, Back/Escape and held confirmation preserve battle",delegate {
                NewGame();auto.PerformClick();start.PerformClick();var game=State;tabs.SelectedIndex=4;front.FinishReveal();front.Select(3);front.Canvas.Focus();down.Remove(Keys.Enter);OnKey(this,new KeyEventArgs(Keys.Enter));OnKey(this,new KeyEventArgs(Keys.Enter));require(front.QuitPending&&front.QuitSelected==0&&!front.FadingOut&&game==State&&game.Paused,"Held title confirm skipped Back default");OnKeyUp(new KeyEventArgs(Keys.Enter));PerformLayout();Application.DoEvents();var bounds=front.Canvas.Bounds4x3;int x=(int)(bounds.X+400*bounds.Width/640),y=(int)(bounds.Y+390*bounds.Height/480);require(front.Canvas.Hit(x,y)==0&&front.Hit(473,390)==-1&&front.Hit(271,390)==-1,"Quit viewport/native right edges");IntPtr point=new IntPtr(x|(y<<16));SendMessage(front.Canvas.Handle,0x201,new IntPtr(1),point);SendMessage(front.Canvas.Handle,0x202,IntPtr.Zero,point);require(!front.QuitPending&&front.Selected==0&&front.Factor==64&&State==game,"Original Back did not restore title immediately");front.Activate(3);down.Remove(Keys.Escape);OnKey(this,new KeyEventArgs(Keys.Escape));OnKeyUp(new KeyEventArgs(Keys.Escape));require(!front.QuitPending&&State==game&&game.Paused,"Quit Escape lost pending battle");front.Activate(3);Size=MinimumSize;PerformLayout();Application.DoEvents();foreach(var b in new[]{front.CancelQuitButton,front.QuitButton})require(b.Visible&&b.Right<=b.Parent.ClientSize.Width,"Quit accessible button mirror clipped");front.CancelQuitButton.PerformClick();
            });
            check("Quit native fade factors, focus pause, single exit and F2 cancellation",delegate {
                using(var probe=new FrontMenu(assets)){int exits=0;tabs.SelectedIndex=4;tabs.TabPages[4].Controls.Add(probe);probe.BringToFront();probe.QuitRequested+=delegate{exits++;};probe.Begin();probe.FinishReveal();probe.Activate(3);probe.Key(Keys.Left);require(probe.QuitSelected==1,"Quit keyboard navigation");probe.Key(Keys.Enter);require(probe.FadingOut&&exits==0&&!probe.CancelQuitButton.Enabled,"Quit skipped palette fade");probe.Advance(40);require(probe.Factor==60,"Native first fade step");probe.Pause();probe.Advance(500);require(probe.Factor==60&&exits==0,"Paused exit advanced");probe.FadeButton.PerformClick();for(int i=2;i<=16;i++){probe.Advance(40);require(probe.Factor==64-4*i&&exits==0,"Native fade order/early close");}probe.Advance(40);probe.Advance(500);probe.QuitButton.PerformClick();require(exits==1&&probe.Factor==0,"Repeated exit event");probe.Reset();require(!probe.QuitPending&&!probe.FadingOut&&probe.CurrentImage==null,"Quit Reset retained resources");}
                front.Begin();front.FinishReveal();front.Activate(3);front.QuitButton.PerformClick();front.Advance(80);down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));require(!front.HasOpened&&!front.QuitPending&&!front.FadingOut&&State.Phase==Phase.Placement,"F2 did not cancel exit fade");
            });
            check("Practice screen uses the verified sprite-font scene and single-page arrows",delegate {
                NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();var comparison=new ClassicPracticeScene(assets);
                foreach(int type in new[]{1,2,3,20})foreach(int players in type==20?new[]{2}:new[]{2,3,4}){
                    solo.Activate(type==20?6:type+6);solo.Activate(players+1);require(solo.Canvas.AccessibleDescription==solo.Program.Description(type),"Practice accessible description");
                    using(var expected=comparison.Compose(type,players,0,solo.HoveredIndex).Bitmap())require(FleetOverviewFingerprint(solo.CurrentImage)==FleetOverviewFingerprint(expected),"Practice source-derived full production image");
                    string before=FleetOverviewFingerprint(solo.CurrentImage);solo.Activate(13);solo.Activate(12);require(solo.DescriptionPage==0&&FleetOverviewFingerprint(solo.CurrentImage)==before,"Single-page arrows changed the original description");
                }
                solo.Pause();solo.Reset();require(solo.CurrentImage==null,"Practice reset retained its source-font image");
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
            check("Fleet original hover footer follows real control events",delegate {
                NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);var renderer=new ClassicFleetScene(assets);var bounds=fleet.Canvas.Bounds4x3;require(bounds.Width>0,"Fleet viewport");
                foreach(int index in new[]{2,3}){var button=fleet.Program.Buttons[index];int x=(int)(bounds.X+(button.X+button.Bounds.Width/2)*bounds.Width/640),y=(int)(bounds.Y+(button.Y+button.Bounds.Height/2)*bounds.Height/480);fleet.Canvas.GetType().GetMethod("OnMouseMove",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(fleet.Canvas,new object[]{new MouseEventArgs(MouseButtons.None,0,x,y,0)});require(fleet.HoveredIndex==index&&!entry.Visible,"Hover activated or missed its button");using(var expected=renderer.Compose(fleet.Draft.Player,index).Bitmap())require(BitmapRegionFingerprint(fleet.CurrentImage,new Rectangle(0,420,640,60))==BitmapRegionFingerprint(expected,new Rectangle(0,420,640,60)),"Production footer differs from native software scene");}
                fleet.Canvas.GetType().GetMethod("OnMouseLeave",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(fleet.Canvas,new object[]{EventArgs.Empty});require(fleet.HoveredIndex==-1,"Mouse leave retained hover");fleet.Hover(2);fleet.Pause();require(fleet.HoveredIndex==-1,"Focus pause retained hover");
            });
            check("Fleet matching release and pause suppress accidental Start",delegate {
                NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);var canvas=fleet.Canvas;var bounds=canvas.Bounds4x3;var button=fleet.Program.Buttons[2];int x=(int)(bounds.X+(button.X+button.Bounds.Width/2)*bounds.Width/640),y=(int)(bounds.Y+(button.Y+button.Bounds.Height/2)*bounds.Height/480);Action<string,int,int> mouse=delegate(string name,int px,int py){canvas.GetType().GetMethod(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,px,py,0)});};
                mouse("OnMouseDown",x,y);mouse("OnMouseUp",-1,-1);require(!entry.Visible&&fleet.Visible,"Mismatched release started battle");mouse("OnMouseDown",x,y);fleet.Pause();mouse("OnMouseUp",x,y);require(!entry.Visible&&fleet.Visible,"Release after pause started battle");var ship=fleet.Draft.Player.Ships[0];fleet.Cell(ship.Cells[ship.Cells.Count/2]);fleet.Hover(2);require(fleet.SelectedShip>=0&&fleet.HoveredIndex==-1,"Held preview showed Start hover");mouse("OnMouseDown",x,y);mouse("OnMouseUp",x,y);require(!entry.Visible&&fleet.Visible,"Held preview started battle");fleet.Key(Keys.Escape);fleet.Hover(2);string footer=BitmapRegionFingerprint(fleet.CurrentImage,new Rectangle(0,420,640,60));mouse("OnMouseDown",x,y);mouse("OnMouseUp",x,y);require(entry.Visible&&!fleet.Visible&&entry.HasFleetSnapshot,"Matched Start did not enter loading transition");entry.Pause();require(BitmapRegionFingerprint(entry.CurrentImage,new Rectangle(0,420,640,60))==footer,"Starting cleared hover before capturing the visible fleet");
            });
            check("Fleet overview retains previews and follows committed moves",delegate {
                NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);
                var draft=fleet.Draft;string before=FleetOverviewFingerprint(fleet.CurrentImage);var ship=draft.Player.Ships[0];Point original=ship.Cells[0];
                fleet.Cell(ship.Cells[ship.Cells.Count/2]);require(fleet.SelectedShip==0&&FleetOverviewFingerprint(fleet.CurrentImage)==before,"Picking a ship hid or changed its retained overview");
                fleet.Cell(new Point(0,0));require(ship.Cells[0]==original&&FleetOverviewFingerprint(fleet.CurrentImage)==before,"Rejected move changed overview or deployment");
                require(fleet.Key(Keys.Escape)&&fleet.SelectedShip<0&&FleetOverviewFingerprint(fleet.CurrentImage)==before,"Cancel changed overview");
                Point? target=null;for(int x=0;x<10&&!target.HasValue;x++)for(int y=0;y<10&&!target.HasValue;y++){var p=new Point(x,y);if(draft.Player.CanMoveClassic(0,p)&&p!=new Point(original.X,original.Y+ship.Cells.Count/2))target=p;}
                require(target.HasValue,"No valid committed overview move");fleet.Cell(ship.Cells[ship.Cells.Count/2]);fleet.Cell(target.Value);
                require(fleet.SelectedShip<0&&ship.Cells[0]!=original&&FleetOverviewFingerprint(fleet.CurrentImage)!=before,"Accepted move did not redraw miniature fleet");
                require(fleet.Draft==draft&&draft.Phase==Phase.Placement,"Overview changed game ownership/phase");
            });
            check("Fleet overview refreshes after shuffle and reset releases its scene",delegate {
                NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);var draft=fleet.Draft;
                string before=FleetOverviewFingerprint(fleet.CurrentImage);int attempts=0;do{fleet.ShuffleButton.PerformClick();attempts++;}while(FleetOverviewFingerprint(fleet.CurrentImage)==before&&attempts<4);
                require(FleetOverviewFingerprint(fleet.CurrentImage)!=before&&fleet.Draft==draft&&draft.Player.Ships.Count==5&&fleet.SelectedShip<0,"Shuffle retained stale overview or replaced draft");
                Bitmap prior=fleet.CurrentImage;down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));bool freed=false;try{int width=prior.Width;}catch(ArgumentException){freed=true;}
                require(freed&&fleet.CurrentImage==null&&fleet.Draft==null,"Reset retained overview scene");
            });
            check("Fleet move rollback, Back preserves battle, F2 releases draft and minimum layout",delegate{NewGame();auto.PerformClick();start.PerformClick();var prior=State;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);var draft=fleet.Draft;require(State==prior&&prior.Paused&&draft!=prior,"Draft changed active game");var ship=draft.Player.Ships[0];var before=ship.Cells[0];fleet.Cell(ship.Cells[2]);fleet.Cell(new Point(0,0));require(ship.Cells[0]==before&&fleet.SelectedShip==0,"Invalid move mutated fleet");Point? target=null;for(int x=0;x<10;x++)for(int y=0;y<10;y++){var at=new Point(x,y);if(draft.Player.CanMoveClassic(0,at)&&at!=new Point(before.X,before.Y+2))target=at;}require(target.HasValue,"No move fixture");fleet.Cell(target.Value);require(ship.Cells[0]==new Point(target.Value.X,target.Value.Y-2)&&fleet.SelectedShip==-1,"Valid move failed");Size=MinimumSize;PerformLayout();Application.DoEvents();require(fleet.ShuffleButton.Right<=fleet.ShuffleButton.Parent.ClientSize.Width,"Fleet toolbar clipped");fleet.BackButton.PerformClick();require(State==prior&&prior.Paused&&solo.Visible&&!fleet.Visible&&fleet.Draft==null,"Back lost paused battle");solo.Activate(10);Bitmap old=fleet.CurrentImage;down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));bool released=false;try{int width=old.Width;}catch(ArgumentException){released=true;}require(released&&fleet.CurrentImage==null&&fleet.Draft==null&&!fleet.Visible,"Reset retained fleet image/draft");});
            check("Fleet pointer entry epoch and focus loss clear pending actions",delegate{NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);PerformLayout();Application.DoEvents();var game=State;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var press=fleet.Canvas.GetType().GetMethod("OnMouseDown",flags);var release=fleet.Canvas.GetType().GetMethod("OnMouseUp",flags);var box=fleet.Canvas.Bounds4x3;Point at=new Point((int)(box.X+50*box.Width/640),(int)(box.Y+445*box.Height/480));press.Invoke(fleet.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});fleet.Pause();entry.Pause();release.Invoke(fleet.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});require(State==game&&fleet.Visible,"Stale press started battle");var draft=fleet.Draft;fleet.Cell(draft.Player.Ships[0].Cells[2]);typeof(Form).GetMethod("OnDeactivate",flags).Invoke(this,new object[]{EventArgs.Empty});require(fleet.Draft==draft&&fleet.SelectedShip==0,"Focus loss lost draft");fleet.Key(Keys.Escape);require(fleet.SelectedShip==-1&&fleet.Visible,"Escape did not cancel move");press.Invoke(fleet.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});release.Invoke(fleet.Canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0)});FinishEntryForCheck();require(State==draft&&State.Phase==Phase.PlayerTurn&&!fleet.Visible,"Fresh mapped Start failed");});
            check("Result handoff cancels an earlier fleet draft and opens the title",delegate{NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);require(fleet.HasOpened,"Missing draft fixture");Bitmap old=fleet.CurrentImage;var game=State;tabs.SelectedIndex=3;ending.StartOutcome(true);for(int i=0;i<5;i++)ending.SkipButton.PerformClick();bool freed=false;try{int width=old.Width;}catch(ArgumentException){freed=true;}require(freed&&fleet.Draft==null&&fleet.CurrentImage==null&&!fleet.Visible&&front.Visible&&front.HasOpened&&tabs.SelectedIndex==4&&State==game,"Result handoff retained draft or missed title");});
            check("Visible Fleet Start accepts native pointer messages at normal and minimum sizes",delegate{foreach(Size size in new[]{new Size(1236,879),MinimumSize}){NewGame();Size=size;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);PerformLayout();Application.DoEvents();Bitmap rendered=fleet.CurrentImage;require(rendered.Width==640&&rendered.Height==480,"Fleet bitmap must match native viewport");var box=fleet.Canvas.Bounds4x3;int px=(int)(box.X+60*box.Width/rendered.Width),py=(int)(box.Y+445*box.Height/rendered.Height);IntPtr point=new IntPtr(px|(py<<16));SendMessage(fleet.Canvas.Handle,0x201,new IntPtr(1),point);SendMessage(fleet.Canvas.Handle,0x202,IntPtr.Zero,point);PumpVisuals(2700);require(State.Phase==Phase.PlayerTurn&&tabs.SelectedIndex==0&&State.Player.Ships.Count==5&&!fleet.HasOpened&&!State.Paused,"Click at displayed Start failed at "+size);Point target=State.Enemy.Ships[0].Cells[0];board.KeyboardCell=target;board.Act();require(State.Pending!=null||State.Enemy.Shots[target.X,target.Y],"First shot after Fleet Start was blocked");}});
            check("Loading entry preserves draft, pauses on focus and cancels without replacing battle",delegate{NewGame();auto.PerformClick();start.PerformClick();var prior=State;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);var draft=fleet.Draft;fleet.Activate(2);require(entry.HasOpened&&entry.Visible&&!fleet.Visible&&State==prior&&prior.Paused,"Entry ownership");entry.Pause();int factor=entry.Timeline.Factor;PumpVisuals(100);require(entry.Timeline.Factor==factor&&State==prior,"Paused entry advanced");entry.BackButton.PerformClick();require(!entry.HasOpened&&fleet.Visible&&fleet.Draft==draft&&State==prior,"Entry cancel lost draft/game");fleet.Activate(2);typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(this,new object[]{EventArgs.Empty});factor=entry.Timeline.Factor;PumpVisuals(100);require(!entry.Timeline.Playing&&entry.Timeline.Factor==factor,"Entry focus pause");entry.PauseButton.PerformClick();PumpVisuals(2700);require(State==draft&&State.Phase==Phase.PlayerTurn&&entry.CurrentImage==null&&!entry.HasOpened,"Entry resume commit");});
            check("Loading exit remains cancellable and preserves the earlier game and fleet",delegate {
                NewGame();var old=State;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);var draft=fleet.Draft;fleet.Activate(2);entry.Pause();entry.Timeline.Resume();entry.Advance(1880);entry.Pause();require(entry.Timeline.LeavingCard&&entry.Timeline.Factor==0&&!entry.Timeline.Complete&&State==old&&draft.Phase==Phase.Placement,"Loading exit committed early");entry.BackButton.PerformClick();require(State==old&&fleet.Draft==draft&&fleet.Visible&&!entry.HasOpened,"Loading exit cancel lost draft");fleet.Activate(2);FinishEntryForCheck();require(State==draft&&State.Phase==Phase.PlayerTurn&&!board.Revealing,"Restart after loading cancellation");
            });
            check("Board reveal blocks firing, ship selection, saves and held confirmation",delegate {
                NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);var draft=fleet.Draft;fleet.Activate(2);entry.Pause();entry.Resume();entry.Advance(1920);require(State==draft&&board.Revealing&&board.Reveal.Factor==12&&!entry.HasOpened,"Battle reveal handoff");var target=State.Enemy.Ships[0].Cells[0];board.KeyboardCell=target;int shooter=State.PlayerShooter;board.Act();require(State.Pending==null&&!State.Enemy.Shots[target.X,target.Y]&&!SaveCurrentBattle()&&!firingFleet.Enabled&&!saveBattle.Enabled&&!restartBattle.Enabled&&State.PlayerShooter==shooter,"Reveal accepted gameplay/save");down.Remove(Keys.Enter);OnKey(this,new KeyEventArgs(Keys.Enter));AdvanceBoardReveal(560);OnKey(this,new KeyEventArgs(Keys.Enter));require(!board.Revealing&&State.Pending==null,"Held confirm fired at reveal completion");OnKeyUp(new KeyEventArgs(Keys.Enter));OnKey(this,new KeyEventArgs(Keys.Enter));OnKeyUp(new KeyEventArgs(Keys.Enter));require(State.Pending!=null||State.Enemy.Shots[target.X,target.Y],"Fresh confirmation remained blocked");
            });
            check("Board reveal pauses on focus and tab changes, resumes, and resets safely",delegate {
                NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);fleet.Activate(2);entry.Pause();entry.Resume();entry.Advance(1920);AdvanceBoardReveal(120);int factor=board.Reveal.Factor;typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(this,new object[]{EventArgs.Empty});AdvanceBoardReveal(10000);require(State.Paused&&board.Reveal.Factor==factor,"Focus loss advanced reveal");tabs.SelectedIndex=1;tabs.SelectedIndex=0;AdvanceBoardReveal(10000);require(board.Reveal.Factor==factor&&State.Paused,"Tab return resumed reveal");State.TogglePause();AdvanceBoardReveal(440);require(!board.Revealing&&!State.Paused,"Resume did not finish reveal");NewGame();require(board.Reveal==null&&State.Phase==Phase.Placement,"Reset retained reveal");
            });
            check("Mouse press during reveal cannot fire on release after it finishes",delegate {
                NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);fleet.Activate(2);entry.Pause();entry.Resume();entry.Advance(1920);var point=ClassicGeometry.Project(true,State.Enemy.Ships[0].Cells[0].X+.5f,State.Enemy.Ships[0].Cells[0].Y+.5f);var box=board.ImageBounds;int x=(int)(box.X+point.X*box.Width/640),y=(int)(box.Y+point.Y*box.Height/480);var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var press=typeof(BoardView).GetMethod("OnMouseDown",flags);var release=typeof(BoardView).GetMethod("OnMouseClick",flags);var args=new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)};press.Invoke(board,args);AdvanceBoardReveal(560);release.Invoke(board,args);require(State.Pending==null,"Stale reveal press fired");press.Invoke(board,args);release.Invoke(board,args);var cell=State.Enemy.Ships[0].Cells[0];require(State.Pending!=null||State.Enemy.Shots[cell.X,cell.Y],"Fresh reveal release stayed blocked");
            });
            check("Fleet exit snapshot holds black frame then releases at loading-card handoff",delegate {
                NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);var draft=fleet.Draft;string scene=FleetOverviewFingerprint(fleet.CurrentImage);fleet.Activate(2);entry.Pause();
                require(entry.HasFleetSnapshot&&entry.Timeline.LeavingFleet&&entry.Timeline.Factor==64&&FleetOverviewFingerprint(entry.CurrentImage)==scene,"Initial snapshot differs from visible fleet");entry.Timeline.Resume();entry.Advance(640);entry.Pause();require(entry.HasFleetSnapshot&&entry.Timeline.LeavingFleet&&entry.Timeline.Factor==0&&fleet.Draft==draft&&draft.Phase==Phase.Placement,"Black frame lost draft/snapshot");
                for(int y=0;y<480;y++)for(int x=0;x<640;x++)require((entry.CurrentImage.GetPixel(x,y).ToArgb()&0xffffff)==0,"Exit black frame retained colored pixels");entry.Timeline.Resume();entry.Advance(39);entry.Pause();require(entry.Timeline.LeavingFleet,"Black hold shortened");entry.Timeline.Resume();entry.Advance(1);entry.Pause();require(!entry.HasFleetSnapshot&&!entry.Timeline.LeavingFleet&&entry.Timeline.Factor==12&&entry.CurrentImage.Size==new Size(640,480)&&draft.Phase==Phase.Placement,"Loading handoff retained snapshot or committed early");FinishEntryForCheck();require(State==draft&&State.Phase==Phase.PlayerTurn&&!entry.HasOpened&&!entry.HasFleetSnapshot,"Final transition did not commit/release exactly once");
            });
            check("Cancelling fleet exit preserves draft and restarting takes a fresh full transition",delegate {
                NewGame();var prior=State;tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);var draft=fleet.Draft;string scene=FleetOverviewFingerprint(fleet.CurrentImage);fleet.Activate(2);entry.Pause();entry.Timeline.Resume();entry.Advance(320);entry.Pause();require(entry.Timeline.Factor==32&&entry.HasFleetSnapshot,"Half-fade fixture");entry.BackButton.PerformClick();require(!entry.HasOpened&&!entry.HasFleetSnapshot&&entry.CurrentImage==null&&fleet.Visible&&fleet.Draft==draft&&State==prior&&FleetOverviewFingerprint(fleet.CurrentImage)==scene,"Cancel changed deployment/game or retained fade");fleet.Activate(2);entry.Pause();require(entry.Timeline.LeavingFleet&&entry.Timeline.Factor==64&&entry.HasFleetSnapshot,"Restart reused partial fade");FinishEntryForCheck();require(State==draft&&!entry.HasOpened&&!entry.HasFleetSnapshot,"Restarted transition failed");
            });
            check("Entry tab loss/reset and result handoff release loading resources",delegate{NewGame();tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);fleet.Activate(2);var timeline=entry.Timeline;tabs.SelectedIndex=0;PumpVisuals(100);tabs.SelectedIndex=4;require(entry.Timeline==timeline&&!timeline.Playing,"Tab loss advanced entry");Size=MinimumSize;PerformLayout();Application.DoEvents();foreach(var b in new[]{entry.PauseButton,entry.BackButton})require(b.Right<=b.Parent.ClientSize.Width,"Entry toolbar clipped");Bitmap old=entry.CurrentImage;down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));bool freed=false;try{int width=old.Width;}catch(ArgumentException){freed=true;}entry.Advance(10000);require(freed&&!entry.HasOpened&&fleet.Draft==null&&tabs.SelectedIndex==0,"Reset retained entry/callback");tabs.SelectedIndex=4;OpenSolo();solo.FinishReveal();solo.Activate(6);solo.Activate(10);fleet.Activate(2);tabs.SelectedIndex=3;ending.StartOutcome(true);for(int i=0;i<5;i++)ending.SkipButton.PerformClick();require(!entry.HasOpened&&!entry.Visible&&fleet.Draft==null&&front.Visible&&front.HasOpened,"Result handoff retained entry");});
            check("Original battle messages follow ready/fire/travel/sink and palette changes",delegate {
                rules.SelectedIndex=0;preferences.MovieTurns=true;NewGame();auto.PerformClick();start.PerformClick();require(board.CurrentMessages.Count==1&&board.CurrentMessages[0].Text=="Select Target Square","Native ready message");
                State.Enemy=new Board();State.Enemy.Place(new Point(0,0),1,true,"Fixture",1);State.Fire(new Point(0,0));UpdateView();require(board.CurrentMessages[0].Text=="Your Battleship Firing at The Computer","Native firing message");battleMovie.EndButton.PerformClick();require(board.CurrentMessages[0].Text=="Your Battleship Firing at The Computer","Native travel message");battleMovie.EndButton.PerformClick();require(board.CurrentMessages[0].Text=="You Sunk The Computer's Battleship","Native sinking message");
                string text=board.CurrentMessages[0].Text;Bitmap glyph=messageFont.GlyphBitmap(0);SelectColors(!preferences.RecoveredColors,false);bool freed=false;try{int width=glyph.Width;}catch(ArgumentException){freed=true;}require(freed&&board.CurrentMessages[0].Text==text&&board.MessageFont==messageFont,"Palette change retained glyph cache or changed stage text");down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));require(board.CurrentMessages==null,"Reset retained battle messages");
            });
            check("Pause sprite display preserves P/Escape edges and freezes a live movie",delegate{
                rules.SelectedIndex=0;preferences.MovieTurns=true;NewGame();auto.PerformClick();start.PerformClick();tabs.SelectedIndex=0;board.Focus();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();var pending=State.Pending;down.Remove(Keys.P);OnKey(this,new KeyEventArgs(Keys.P));OnKey(this,new KeyEventArgs(Keys.P));require(State.Paused&&board.PauseMessages.Count==3&&board.PauseMessages[1].Text=="Escape Also Resumes","Pause edge/display");int frame=battleMovie.Timeline.Frame;PumpVisuals(120);require(battleMovie.Timeline.Frame==frame&&State.Pending==pending,"Paused movie advanced");OnKeyUp(new KeyEventArgs(Keys.P));down.Remove(Keys.Escape);OnKey(this,new KeyEventArgs(Keys.Escape));OnKey(this,new KeyEventArgs(Keys.Escape));require(!State.Paused&&board.PauseMessages==null&&State.Pending==pending,"Escape resumed repeatedly or quit");OnKeyUp(new KeyEventArgs(Keys.Escape));OnKey(this,new KeyEventArgs(Keys.Escape));require(State.Paused,"Escape cannot pause");OnKeyUp(new KeyEventArgs(Keys.Escape));Bitmap glyph=messageFont.GlyphBitmap(94);SelectColors(!preferences.RecoveredColors,false);bool freed=false;try{int width=glyph.Width;}catch(ArgumentException){freed=true;}require(freed&&board.PauseMessages.Count==3,"Pause font cache ownership");down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));require(board.PauseMessages==null&&State.Pending==null,"Reset retained pause overlay/shot");
            });
            check("End-battle confirmation cancels safely and pause keys retain their action",delegate{
                rules.SelectedIndex=0;preferences.MovieTurns=true;NewGame();auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();State.Paused=true;UpdateView();var game=State;var pending=State.Pending;int frame=battleMovie.Timeline.Frame;pauseActions.AskButton.PerformClick();require(pauseActions.Confirming&&returnConfirmationGame==game&&pauseActions.CancelButton.ContainsFocus,"Confirmation must focus Keep battle");PumpVisuals(100);pauseActions.CancelButton.PerformClick();require(State==game&&State.Pending==pending&&State.Paused&&battleMovie.Timeline.Frame==frame&&!pauseActions.Confirming,"Cancel changed battle/movie");pauseActions.AskButton.PerformClick();down.Remove(Keys.Escape);OnKey(this,new KeyEventArgs(Keys.Escape));OnKeyUp(new KeyEventArgs(Keys.Escape));require(State==game&&!State.Paused&&!pauseActions.Confirming&&returnConfirmationGame==null,"Escape no longer resumes/cancels confirmation");
            });
            check("Confirmed end releases each shot stage and prevents held Enter at title",delegate{
                foreach(int stage in new[]{-1,0,1,2}){preferences.MovieTurns=stage>=0;NewGame();auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();for(int n=0;n<stage;n++)battleMovie.EndButton.PerformClick();State.Paused=true;UpdateView();var old=State;Bitmap image=battleMovie.CurrentImage;string settings=preferences.Serialize();pauseActions.AskButton.PerformClick();down.Add(Keys.Enter);pauseActions.EndButton.PerformClick();require(State!=old&&State.Phase==Phase.Placement&&State.Player.Ships.Count==0&&State.Pending==null&&board.MovieFrame==null&&battleMovie.Timeline==null,"Ended shot/state retained");bool freed=false;try{int width=image.Width;}catch(ArgumentException){freed=true;}require(freed&&tabs.SelectedIndex==4&&front.HasOpened&&front.Visible&&!entry.HasOpened&&fleet.Draft==null,"End/title resource ownership");require(preferences.Serialize()==settings&&front.BackButton.Text=="Return to deployment","End changed settings or advertised resume");CompleteBattleStage();front.FinishReveal();OnKey(this,new KeyEventArgs(Keys.Enter));require(front.Visible&&!solo.Visible,"Held Enter activated Solo");OnKeyUp(new KeyEventArgs(Keys.Enter));OnKey(this,new KeyEventArgs(Keys.Enter));OnKeyUp(new KeyEventArgs(Keys.Enter));require(solo.Visible&&!front.Visible,"Released Enter could not open Solo");if(stage==2){solo.FinishReveal();solo.Activate(6);solo.Activate(10);fleet.Activate(2);FinishEntryForCheck();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();require(State.Phase==Phase.PlayerTurn&&State.Pending!=null&&battleMovie.CurrentImage!=null&&!battleFailure,"Title-to-new-battle replay failed");}}
            });
            check("End confirmation rejects stale focus/tab/reset callbacks and fits both sizes",delegate{
                foreach(Size size in new[]{new Size(1236,879),MinimumSize}){NewGame();Size=size;auto.PerformClick();start.PerformClick();State.Paused=true;UpdateView();pauseActions.AskButton.PerformClick();PerformLayout();Application.DoEvents();foreach(var button in new[]{pauseActions.CancelButton,pauseActions.EndButton})require(button.Right<=button.Parent.ClientSize.Width&&button.Bottom<=button.Parent.ClientSize.Height,"Confirmation buttons clipped");var game=State;typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(this,new object[]{EventArgs.Empty});pauseActions.EndButton.PerformClick();EndBattleToTitle();require(State==game&&!pauseActions.Confirming&&returnConfirmationGame==null,"Stale focus confirmation ended battle");pauseActions.AskButton.PerformClick();tabs.SelectedIndex=1;EndBattleToTitle();require(State==game&&!pauseActions.Confirming,"Tab loss kept destructive confirmation");tabs.SelectedIndex=0;pauseActions.AskButton.PerformClick();NewGame();EndBattleToTitle();require(State.Phase==Phase.Placement&&!pauseActions.Confirming&&returnConfirmationGame==null,"Reset kept stale owner");}
            });
            check("Unloaded battle playback tolerates settings, failed reload and a new battle",delegate{
                battleMovie.UnloadBattle();battleMovie.Pause();battleMovie.Reset();battleMovie.ResumePlayback();battleMovie.SetSound(false);battleMovie.SetDetail(!battleMovie.HighDetail);battleMovie.PreviewFrame(3);require(battleMovie.CurrentImage==null&&battleMovie.Timeline==null&&!battleMovie.EndButton.Enabled,"Unloaded movie retained state");require(!battleMovie.LoadBattle("missing")&&battleMovie.Problem.StartsWith("Clip unavailable:"),"Unloaded load failure escaped");rules.SelectedIndex=0;preferences.MovieTurns=true;NewGame();auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();require(battleMovie.Timeline!=null&&battleMovie.CurrentImage!=null&&!battleFailure,"Could not reload battle after unload");
            });
            check("Original Preferences preserves a paused battle and locks its quality/difficulty",delegate{
                rules.SelectedIndex=0;preferences.MovieTurns=true;NewGame();SetMovieDetail(false,false);auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();var game=State;var pending=State.Pending;int frame=battleMovie.Timeline.Frame;Bitmap boardArt=art;tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);prefsPage.FinishReveal();require(prefsPage.Visible&&!front.Visible&&prefsPage.HasOpened&&!prefsPage.CanChangeBattleSettings&&State.Paused,"Preferences menu bridge/lock");int difficulty=State.OpponentLevel;prefsPage.Activate(1);prefsPage.Activate(7);require(!preferences.HighMovies&&State==game&&State.OpponentLevel==difficulty,"Locked preferences changed battle");prefsPage.Bar(258);prefsPage.Activate(3);require(preferences.FxVolume==50&&preferences.Radar==1&&sound.Level==50&&clip.Level==50&&battleMovie.Level==50&&ending.Movie.Level==50&&art==boardArt,"Preferences did not apply scoped settings");prefsPage.BackButton.PerformClick();require(front.Visible&&!prefsPage.HasOpened&&State==game&&State.Pending==pending&&State.Paused&&battleMovie.Timeline.Frame==frame,"Back changed paused battle/movie");
            });
            check("Preferences quality uses high assets and targeting change resets only deployment",delegate{
                NewGame();auto.PerformClick();int count=State.Player.Ships.Count;tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);prefsPage.FinishReveal();prefsPage.Activate(1);require(preferences.HighMovies&&clip.HighDetail&&battleMovie.HighDetail&&State.Player.Ships.Count==count,"Quality lost deployment or remained low");prefsPage.Activate(7);require(State.Phase==Phase.Placement&&State.Player.Ships.Count==0&&State.OpponentLevel==3&&preferences.Difficulty==3&&opponent.SelectedIndex==3&&prefsPage.Visible,"Targeting preference bridge");prefsPage.BackButton.PerformClick();front.FinishReveal();front.Activate(0);solo.FinishReveal();solo.Activate(6);solo.Activate(10);fleet.Activate(2);FinishEntryForCheck();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();require(State.Pending!=null&&State.OpponentLevel==3&&battleMovie.CurrentImage.Width==320&&battleMovie.Level==50,"Preferences did not reach playable high-detail battle");
            });
            check("Preferences native pointer coordinates work at normal/minimum sizes",delegate{
                foreach(Size size in new[]{new Size(1236,879),MinimumSize}){NewGame();Size=size;tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);prefsPage.FinishReveal();ApplyPreferences(new PreferenceValues{Quality=1,Radar=0,Difficulty=0,Volume=80});PerformLayout();Application.DoEvents();require(prefsPage.CurrentImage.Size==new Size(640,480),"Preferences storage was squeezed instead of cropped");Action<int,int,bool> click=delegate(int x,int y,bool right){var b=prefsPage.Canvas.Bounds4x3;int px=(int)(b.X+x*b.Width/640),py=(int)(b.Y+y*b.Height/480);IntPtr p=new IntPtr(px|(py<<16));SendMessage(prefsPage.Canvas.Handle,right?0x204:0x201,new IntPtr(right?2:1),p);SendMessage(prefsPage.Canvas.Handle,right?0x205:0x202,IntPtr.Zero,p);};click(350,126,false);require(preferences.HighMovies,"Displayed High quality click missed");click(430,365,false);require(preferences.FxVolume==85,"Percentage left click did not increment");click(430,365,true);require(preferences.FxVolume==80,"Percentage right click did not decrement");click(150,365,false);require(preferences.FxVolume==0,"Volume bar Off missed");click(403,365,false);require(preferences.FxVolume==100,"Volume bar maximum missed");foreach(var button in new[]{prefsPage.BackButton,prefsPage.SaveButton,prefsPage.FadeButton})require(button.Right<=button.Parent.ClientSize.Width,"Preference toolbar clipped");}
            });
            check("Preferences focus/tab pause, input generation and F2 release their image",delegate{
                NewGame();tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);PumpVisuals(80);typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(this,new object[]{EventArgs.Empty});int factor=prefsPage.Factor;PumpVisuals(90);require(!prefsPage.Revealing&&prefsPage.Factor==factor,"Preference focus fade advanced");prefsPage.FadeButton.PerformClick();PumpVisuals(650);require(prefsPage.Factor==64,"Preference fade resume");var b=prefsPage.Canvas.Bounds4x3;int x=(int)(b.X+170*b.Width/640),y=(int)(b.Y+126*b.Height/480);IntPtr point=new IntPtr(x|(y<<16));SendMessage(prefsPage.Canvas.Handle,0x201,new IntPtr(1),point);prefsPage.Begin(CurrentPreferences(),true);prefsPage.FinishReveal();bool high=preferences.HighMovies;SendMessage(prefsPage.Canvas.Handle,0x202,IntPtr.Zero,point);require(preferences.HighMovies==high,"Stale pointer activated a new preference entry");tabs.SelectedIndex=1;tabs.SelectedIndex=4;require(prefsPage.Visible&&prefsPage.HasOpened&&!prefsPage.Revealing,"Tab transition lost/resumed preferences");Bitmap image=prefsPage.CurrentImage;down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));bool freed=false;try{int width=image.Width;}catch(ArgumentException){freed=true;}require(freed&&!prefsPage.HasOpened&&prefsPage.CurrentImage==null&&State.Phase==Phase.Placement,"F2 retained preference image");
            });
            check("Volume changes preserve exact paused audio offset and keep clip frames",delegate{
                tabs.SelectedIndex=2;var backend=new SimulatedClipOutput();using(var probe=new ClipStudy(assets,backend)){tabs.TabPages[2].Controls.Add(probe);probe.BringToFront();probe.SetSound(true);probe.PlayButton.PerformClick();backend.Samples=1001;probe.Pause();var timeline=probe.Timeline;Bitmap image=probe.CurrentImage;var original=backend.LastPcm;int frame=timeline.Frame;probe.SetLevel(0);probe.SetLevel(50);require(!backend.Active&&!timeline.Playing&&timeline.Frame==frame&&probe.CurrentImage==image,"Paused gain moved video or restarted output");probe.ResumePlayback();require(backend.Active&&backend.Starts[backend.Starts.Count-1]==1001&&probe.Timeline==timeline&&probe.CurrentImage==image,"Gain resumed at a rounded/stale audio offset");probe.SetLevel(0);foreach(short sample in backend.LastPcm)require(sample==0,"Off submitted nonzero PCM");probe.Pause();backend.FailPosition=true;probe.SetLevel(20);require(probe.Problem.Contains("injected position")&&!probe.Timeline.Playing,"Position failure escaped gain change");backend.FailPosition=false;probe.NextButton.PerformClick();probe.ResumePlayback();require(backend.Starts[backend.Starts.Count-1]==probe.Timeline.Frame*441,"Step retained previous gain seek");require(original!=null&&original.Length==backend.LastPcm.Length,"Volume changed sample count");}require(!backend.Active&&backend.Disposed,"Volume probe retained device");
            });
            check("Preference Save default writes, Back preserves disk, failures stay visible and reload applies",delegate{
                string directory=Path.Combine(Path.GetTempPath(),"BattleshipPrefs-"+Guid.NewGuid().ToString("N")),path=Path.Combine(directory,"settings.txt");Directory.CreateDirectory(directory);try{
                    File.WriteAllText(path,"sound=false\nfxvolume=80\ndifficulty=2\nradar=0\ncustomConfirm=Enter\n");string disk=File.ReadAllText(path);
                    using(var owned=new MainForm(assets,path)){owned.previewMode=true;owned.ShowInTaskbar=false;owned.StartPosition=FormStartPosition.Manual;owned.Location=new Point(-20000,-20000);owned.Show();Application.DoEvents();require(owned.State.OpponentLevel==2&&owned.sound.Level==80&&owned.ending.Movie.Level==80,"Cold configuration not applied");owned.tabs.SelectedIndex=4;owned.front.FinishReveal();owned.front.Activate(2);owned.prefsPage.FinishReveal();owned.ApplyPreferences(new PreferenceValues{Quality=2,Radar=1,Difficulty=3,Volume=35});owned.prefsPage.BackButton.PerformClick();require(File.ReadAllText(path)==disk,"Back saved unsaved preferences");owned.front.FinishReveal();owned.front.Activate(2);owned.prefsPage.FinishReveal();owned.previewMode=false;using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){owned.prefsPage.SaveButton.PerformClick();require(owned.prefsPage.Visible&&owned.preferences.Problem.Contains("Could not save"),"Save failure exited or hid error");}owned.prefsPage.SaveButton.PerformClick();require(owned.front.Visible&&!owned.prefsPage.HasOpened&&owned.preferences.Problem=="","Successful Save did not return to title");}
                    using(var cold=new MainForm(assets,path)){require(cold.State.OpponentLevel==3&&cold.preferences.HighMovies&&cold.preferences.Radar==1&&cold.sound.Level==35&&cold.clip.Level==35&&cold.battleMovie.Level==35&&cold.ending.Movie.Level==35&&File.ReadAllText(path).Contains("customConfirm=Enter"),"Saved configuration or accepted binding lost on cold reload");}
                }finally{foreach(string file in new[]{path,path+".new"})if(File.Exists(file))File.Delete(file);Directory.Delete(directory);}
            });
            check("Preferences keyboard edges and Credits handoff preserve their actual scope",delegate{
                NewGame();SetMovieDetail(true,false);tabs.SelectedIndex=4;front.FinishReveal();front.Select(2);front.Canvas.Focus();down.Remove(Keys.Enter);OnKey(this,new KeyEventArgs(Keys.Enter));OnKey(this,new KeyEventArgs(Keys.Enter));prefsPage.FinishReveal();OnKey(this,new KeyEventArgs(Keys.Enter));require(prefsPage.Visible&&preferences.HighMovies,"Held title confirm changed quality");OnKeyUp(new KeyEventArgs(Keys.Enter));prefsPage.Select(8,true);ApplyPreferences(new PreferenceValues{Quality=2,Radar=0,Difficulty=0,Volume=80});OnKey(this,new KeyEventArgs(Keys.Enter));OnKey(this,new KeyEventArgs(Keys.Enter));require(preferences.FxVolume==85,"Held volume confirm repeated");OnKeyUp(new KeyEventArgs(Keys.Enter));down.Remove(Keys.Left);OnKey(this,new KeyEventArgs(Keys.Left));OnKeyUp(new KeyEventArgs(Keys.Left));require(preferences.FxVolume==80,"Keyboard volume adjustment");prefsPage.Select(10,true);prefsPage.Activate(10);require(creditsPage.Visible&&!prefsPage.Visible&&creditsPage.HasOpened&&preferences.FxVolume==80,"Credits handoff changed preferences");down.Remove(Keys.Escape);OnKey(this,new KeyEventArgs(Keys.Escape));OnKey(this,new KeyEventArgs(Keys.Escape));OnKeyUp(new KeyEventArgs(Keys.Escape));PumpVisuals(750);require(front.Visible&&!prefsPage.HasOpened&&!creditsPage.HasOpened,"Escape did not return safely to title");
            });
            check("Credits preserves paused battle ownership and releases frames on return",delegate{
                NewGame();SetMovieDetail(false,false);auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();var game=State;var pending=State.Pending;int battleFrame=battleMovie.Timeline.Frame;tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);prefsPage.FinishReveal();prefsPage.Activate(10);require(creditsPage.Visible&&creditsPage.HasOpened&&creditsPage.Stage==0&&State==game&&State.Paused,"Credits entry owns battle");creditsPage.FinishEntry();creditsPage.Advance(50);require(creditsPage.Roll.Ticks==1&&creditsPage.Movie.Timeline.FrameMilliseconds==50&&creditsPage.Movie.CurrentImage!=null,"Credits first movie/tick");creditsPage.Pause();startupPage.Pause();if(archive!=null)archive.Pause();long tick=creditsPage.Roll.Ticks;int frame=creditsPage.Movie.Timeline.Frame;PumpVisuals(120);require(creditsPage.Roll.Ticks==tick&&creditsPage.Movie.Timeline.Frame==frame,"Paused credits advanced");creditsPage.Resume();creditsPage.Advance(100);creditsPage.Pause();startupPage.Pause();if(archive!=null)archive.Pause();require(creditsPage.Movie.Timeline.Frame==frame+2,"Credits 20fps poll");Bitmap image=creditsPage.CurrentImage;creditsPage.BackButton.PerformClick();PumpVisuals(750);bool freed=false;try{int width=image.Width;}catch(ArgumentException){freed=true;}require(freed&&front.Visible&&!creditsPage.HasOpened&&creditsPage.Movie.CurrentImage==null&&State==game&&State.Pending==pending&&State.Paused&&battleMovie.Timeline.Frame==battleFrame,"Credits return retained resources or altered battle");
            });
            check("Credits focus/tab/reset paths pause both clocks and release images",delegate{
                NewGame();tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);prefsPage.FinishReveal();prefsPage.Activate(10);creditsPage.FinishEntry();creditsPage.Advance(100);typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(this,new object[]{EventArgs.Empty});long tick=creditsPage.Roll.Ticks;PumpVisuals(120);require(!creditsPage.Playing&&creditsPage.Roll.Ticks==tick&&!creditsPage.Movie.Timeline.Playing,"Credits focus pause");creditsPage.Resume();tabs.SelectedIndex=1;PumpVisuals(80);tabs.SelectedIndex=4;require(creditsPage.Visible&&creditsPage.Roll.Ticks==tick&&!creditsPage.Playing,"Credits tab loss");Bitmap image=creditsPage.CurrentImage;down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));bool freed=false;try{int width=image.Width;}catch(ArgumentException){freed=true;}creditsPage.Advance(10000);require(freed&&!creditsPage.HasOpened&&creditsPage.CurrentImage==null&&creditsPage.Movie.Timeline==null&&tabs.SelectedIndex==0,"Credits F2 leaked state");
            });
            check("Credits toolbar fits normal/minimum sizes and text restart leaves the movie",delegate{
                foreach(Size size in new[]{new Size(1236,879),MinimumSize}){NewGame();Size=size;tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);prefsPage.FinishReveal();prefsPage.Activate(10);creditsPage.FinishEntry();creditsPage.Advance(400);creditsPage.Pause();startupPage.Pause();if(archive!=null)archive.Pause();PerformLayout();Application.DoEvents();require(creditsPage.CurrentImage.Size==new Size(640,480),"Credits native viewport");foreach(var button in new[]{creditsPage.BackButton,creditsPage.PauseButton,creditsPage.RestartButton})require(button.Right<=button.Parent.ClientSize.Width,"Credits toolbar clipped");var timeline=creditsPage.Movie.Timeline;int frame=timeline.Frame;creditsPage.RestartButton.PerformClick();require(creditsPage.Roll.Ticks==0&&creditsPage.Movie.Timeline==timeline&&timeline.Frame==frame&&!creditsPage.Playing,"Text restart changed movie/pause");}
            });
            check("Credits finite sound uses 20fps cues and failed output does not trap text",delegate{
                var backend=new SimulatedClipOutput{FailStart=true};using(var probe=new CreditsPage(assets,backend)){tabs.SelectedIndex=4;tabs.TabPages[4].Controls.Add(probe);probe.BringToFront();probe.Visible=true;probe.SetSound(true);probe.Begin(prefsPage.SnapshotIndexed(),false);probe.FinishEntry();probe.Advance(50);probe.Advance(300);require(probe.Roll.Ticks==7&&probe.Movie.Timeline.FrameMilliseconds==50&&probe.Movie.Timeline.Frame==7&&probe.Movie.Problem.Contains("injected"),"Credits failed sound blocked fixed movie/text");probe.Pause();probe.SetLevel(50);probe.SetSound(false);probe.Back();probe.Advance(500);probe.Advance(200);require(!probe.HasOpened&&!backend.Active,"Credits failed-audio exit");}require(backend.Disposed,"Credits output lifetime");
            });
            check("Fleet Configuration Random uses native placement and keeps the draft/roster",delegate{
                NewGame();tabs.SelectedIndex=4;front.FinishReveal();front.Activate(0);solo.FinishReveal();solo.Activate(6);solo.Activate(10);var draft=fleet.Draft;int calls=draft.PlacementCalls;var rng=new NativeRandom(draft.PlacementState);var expected=OriginalPlacement.Generate(new[]{5,5,3,4,2},rng.Next);fleet.ShuffleButton.PerformClick();require(fleet.Draft==draft&&draft.PlacementCalls==calls+expected.Calls&&draft.PlacementState==rng.State&&fleet.SelectedShip==-1,"Fleet Random replaced draft or used provisional RNG");for(int i=0;i<5;i++)require(draft.Player.Ships[i].Cells[0]==expected.Positions[i],"Fleet Random native position");fleet.Activate(2);FinishEntryForCheck();require(State==draft&&State.Phase==Phase.PlayerTurn&&State.Player.Ships.Count==5&&State.Enemy.Ships.Count==5,"Native fleet Start handoff");
            });
            check("Movie archive preserves a paused duel through playback and tab/focus changes",delegate{
                NewGame();preferences.MovieTurns=true;NewGame();auto.PerformClick();start.PerformClick();State.Fire(State.Enemy.Ships[0].Cells[0]);UpdateView();var game=State;var pending=State.Pending;int battleFrame=battleMovie.Timeline.Frame;tabs.SelectedIndex=5;archive.Selector.SelectedItem="F18LAND";require(archive.Selector.Items.Count==137&&archive.Timeline.FrameMilliseconds==50&&State.Paused,"Archive catalog/scene bridge");archive.PlayButton.PerformClick();PumpVisuals(120);require(archive.Timeline.Frame>0&&State==game&&State.Pending==pending&&battleMovie.Timeline.Frame==battleFrame,"Archive changed paused duel");typeof(Form).GetMethod("OnDeactivate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(this,new object[]{EventArgs.Empty});int frame=archive.Timeline.Frame;PumpVisuals(80);require(!archive.Timeline.Playing&&archive.Timeline.Frame==frame,"Archive focus loss advanced");archive.PlayButton.PerformClick();tabs.SelectedIndex=0;PumpVisuals(80);require(!archive.Timeline.Playing&&State==game&&State.Paused&&State.Pending==pending,"Archive tab loss resumed battle");
            });
            check("Archive quality, volume and mute follow Preferences without losing selected media",delegate{
                NewGame();SetMovieDetail(false,false);tabs.SelectedIndex=5;archive.Selector.SelectedItem="ISL_DSHS";archive.PreviewFrame(20);Bitmap previous=archive.CurrentImage;tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);prefsPage.FinishReveal();ApplyPreferences(new PreferenceValues{Quality=2,Radar=0,Difficulty=0,Volume=35});bool freed=false;try{int width=previous.Width;}catch(ArgumentException){freed=true;}require(freed&&archive.HighDetail&&archive.CurrentImage.Width==320&&archive.Timeline.Frame==20&&archive.Level==35&&archive.ClipName=="ISL_DSHS"&&archive.Problem.Contains("FIRE45F"),"Archive settings reset media or lost warning");archive.Pause();bool before=sound.Enabled;audio.PerformClick();require(archive.SoundEnabled==!before,"Global mute missed archive");audio.PerformClick();require(archive.SoundEnabled==before,"Global sound restore missed archive");
            });
            check("Archive missing-cue warning survives playback, gain, stepping and clears on another movie",delegate{
                tabs.SelectedIndex=5;var backend=new SimulatedClipOutput();using(var probe=new ClipStudy(assets,backend)){tabs.TabPages[5].Controls.Add(probe);probe.BringToFront();probe.ConfigureArchive(CreditsCatalog.Load(assets));probe.Selector.SelectedItem="ISL_DSHS";foreach(string name in new[]{"SHELIN_A","FIRE45F","EXP01","EXP02"})require(probe.Problem.Contains(name),"Missing cue warning incomplete");probe.SetSound(true);probe.PlayButton.PerformClick();require(backend.Active&&probe.Problem.Contains("EXP02"),"Successful output erased content warning");backend.Samples=2757;probe.Pause();probe.SetLevel(50);probe.ResumePlayback();require(backend.Starts[backend.Starts.Count-1]==2757&&probe.Problem.Contains("SHELIN_A"),"Gain lost sample position/warning");bool nonzero=false;foreach(short value in backend.LastPcm)nonzero|=value!=0;require(nonzero,"Missing cues erased surviving audio");probe.Pause();probe.NextButton.PerformClick();probe.RestartButton.PerformClick();require(probe.Timeline.Frame==0&&probe.Problem.Contains("EXP01"),"Step/restart erased warning");probe.Selector.SelectedItem="CL_HIT";require(probe.Problem==""&&probe.Timeline.Frame==0,"Stale warning leaked to healthy resource");}require(backend.Disposed&&!backend.Active,"Archive retained output");archive.BringToFront();
            });
            check("Full-catalog Credits can play the incomplete source movie and unload it safely",delegate{
                NewGame();tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);prefsPage.FinishReveal();var snapshot=prefsPage.SnapshotIndexed();uint seed=0;while(new NativeRandom(seed).Next()%137!=99)seed++;prefsPage.Activate(10);creditsPage.Begin(snapshot,false,seed);creditsPage.FinishEntry();creditsPage.Advance(50);require(creditsPage.Movie.ClipName=="ISL_DSHS"&&creditsPage.Movie.Problem.Contains("EXP02"),"Credits did not use full native catalog");creditsPage.Advance(200);creditsPage.Pause();startupPage.Pause();require(creditsPage.Roll.Ticks==5&&creditsPage.Movie.Timeline.Frame==5,"Partial source audio trapped Credits progression");creditsPage.BackButton.PerformClick();PumpVisuals(750);require(front.Visible&&!creditsPage.HasOpened&&creditsPage.Movie.Timeline==null,"Credits partial source exit");
            });
            check("Archive fits both window sizes and F2 resets playback and battle",delegate{
                foreach(Size size in new[]{new Size(1236,879),MinimumSize}){NewGame();Size=size;tabs.SelectedIndex=5;archive.Selector.SelectedItem="CL_HIT";archive.PreviewFrame(7);PerformLayout();Application.DoEvents();foreach(var button in new[]{archive.PlayButton,archive.RestartButton,archive.PreviousButton,archive.NextButton,archive.EndButton})require(button.Right<=button.Parent.ClientSize.Width,"Archive controls clipped");down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));require(tabs.SelectedIndex==0&&State.Phase==Phase.Placement&&archive.Timeline.Frame==0&&!archive.Timeline.Playing,"F2 retained archive playback");}
            });
            check("Credits frame0 precedes the first tick and EOF replaces the movie in the same poll",delegate{
                NewGame();tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);prefsPage.FinishReveal();var card=prefsPage.SnapshotIndexed();uint seed=0;while(new NativeRandom(seed).Next()%137!=0)seed++;prefsPage.Activate(10);creditsPage.Begin(card,false,seed);creditsPage.FinishEntry();var first=creditsPage.Movie.Timeline;int raw=creditsPage.Movie.RawFrameCount;require(creditsPage.Roll.Ticks==0&&creditsPage.Movie.ClipName=="BATFIRE1"&&raw==46&&first.Count==45&&first.Frame==0&&creditsPage.CurrentImage!=null,"Native initial decode/visible duration");creditsPage.Advance(50);require(first.Frame==1,"First native Credits poll repeated frame0");for(int i=1;i<raw-2;i++)creditsPage.Advance(50);require(creditsPage.Movie.Timeline==first&&first.Frame==raw-2&&!first.Completed,"Credits ended before final visible frame");creditsPage.Advance(50);require(creditsPage.Movie.Timeline!=first&&creditsPage.Movie.Timeline.Frame==0&&creditsPage.Roll.Ticks==raw-1,"EOF left an extra final-frame interval");var expected=CreditsPresentation.Scale(creditsPage.Movie.IndexedFrame(0));using(var font=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL"))),true)){foreach(var line in creditsPage.Roll.Commands(font))font.RasterInto(expected.Pixels,640,480,line.At.X,line.At.Y,line.Text,4);}for(int i=0;i<expected.Pixels.Length;i++)require(creditsPage.CurrentIndexed.Pixels[i]==expected.Pixels[i],"Credits EOF frame/text composite");creditsPage.Pause();startupPage.Pause();
            });
            check("Credits skip snapshots advance once and fade indexed colors with native truncation",delegate{
                foreach(bool paused in new[]{false,true}){NewGame();tabs.SelectedIndex=4;front.FinishReveal();front.Activate(2);prefsPage.FinishReveal();var card=prefsPage.SnapshotIndexed();prefsPage.Activate(10);creditsPage.Begin(card,false,731u);creditsPage.FinishEntry();creditsPage.Advance(100);if(paused)creditsPage.Pause();startupPage.Pause();long ticks=creditsPage.Roll.Ticks;int frame=creditsPage.Movie.Timeline.Frame;var expected=CreditsPresentation.Scale(creditsPage.Movie.IndexedFrame(paused?frame:CreditsPresentation.SkipFrame(frame,creditsPage.Movie.RawFrameCount)));var roll=new CreditsRoll(creditsPage.Roll.Text);roll.Advance((ticks+(paused?0:1))*50);using(var font=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL"))),true)){foreach(var line in roll.Commands(font))font.RasterInto(expected.Pixels,640,480,line.At.X,line.At.Y,line.Text,4);}creditsPage.BackButton.PerformClick();require(creditsPage.Stage==2&&creditsPage.Roll.Ticks==ticks+(paused?0:1),"Skip/pause text tick");for(int i=0;i<expected.Pixels.Length;i++)require(creditsPage.CurrentIndexed.Pixels[i]==expected.Pixels[i],"Skip final source frame");creditsPage.Advance(320);creditsPage.Pause();startupPage.Pause();require(creditsPage.Factor==32&&creditsPage.CurrentIndexed.Palette[10].R==127,"Indexed fade rounded255half to128");var palette=ClassicPalette.FadeDisplay(expected.Palette,32);for(int i=0;i<256;i++)require(creditsPage.CurrentIndexed.Palette[i]==palette[i],"Indexed exit palette mismatch");}
            });
            check("Restart restores the existing battle without consuming the next fresh seed",delegate {
                rules.SelectedIndex=0;preferences.MovieTurns=true;NewGame();require(!restartBattle.Enabled,"Restart enabled during deployment");auto.PerformClick();start.PerformClick();UpdateView();require(restartBattle.Enabled,"Started battle cannot restart");
                var previous=State;int nextSeed=seed;Point own=State.Player.Ships[0].Cells[0],enemyCell=State.Enemy.Ships[0].Cells[0];State.SelectShooter(3);State.Fire(enemyCell);UpdateView();var old=State.Pending;State.Paused=true;UpdateView();require(battleMovie.Timeline!=null,"Pending restart fixture missing movie");
                down.Add(Keys.Enter);require(RestartBattleConfirmed(previous),"Confirmed restart rejected");require(State!=previous&&State.Seed==previous.Seed&&seed==nextSeed&&State.Phase==Phase.PlayerTurn&&!State.Paused&&State.Pending==null&&State.PlayerShooter==1&&firingFleet.SelectedIndex==1,"Restart state/seed/shooter mismatch");
                require(State.Player.Ships[0].Cells[0]==own&&State.Enemy.Ships[0].Cells[0]==enemyCell&&State.Enemy.Ships[0].Hits==0&&board.KeyboardCell==Point.Empty,"Restart changed deployment or retained damage/cursor");
                require(board.MovieFrame==null&&!board.CinematicBackdropActive&&battleMovie.Timeline==null&&!battleFailure&&!State.CompleteMovie(old,old.Stage),"Restart retained old playback/token");
                OnKey(this,new KeyEventArgs(Keys.Enter));require(State.Pending==null&&State.LastPlayerShot==null,"Held confirmation fired after restart");OnKeyUp(new KeyEventArgs(Keys.Enter));
                var current=State;require(!RestartBattleConfirmed(previous)&&State==current,"Stale confirmation restarted a different battle");down.Remove(Keys.F2);OnKey(this,new KeyEventArgs(Keys.F2));OnKeyUp(new KeyEventArgs(Keys.F2));require(State.Phase==Phase.Placement&&State.Seed==nextSeed&&!restartBattle.Enabled,"F2 fresh battle was replaced by replay");
            });
            check("Restart is scoped to the duel and preserves settings",delegate {
                preferences.MovieTurns=false;NewGame();auto.PerformClick();start.PerformClick();UpdateView();var previous=State;string settings=preferences.Serialize();tabs.SelectedIndex=5;UpdateView();require(!restartBattle.Enabled&&!RestartBattleConfirmed(previous),"Archive restarted a battle without duel context");tabs.SelectedIndex=0;State.Phase=Phase.Finished;UpdateView();require(restartBattle.Enabled&&RestartBattleConfirmed(previous)&&State.Phase==Phase.PlayerTurn&&preferences.Serialize()==settings,"Finished replay changed settings or lost starting fleet");
            });
            check("Board cursors distinguish targets, selection, playback, pause and resolved cells",delegate{
                rules.SelectedIndex=0;preferences.MovieTurns=true;NewGame();auto.PerformClick();start.PerformClick();UpdateView();require(board.HasOriginalCursors,"Original cursor resources were not loaded");
                Point cell=State.Enemy.Ships[0].Cells[0],own=State.Player.Ships[0].Cells[0];PointF targetAt=ClassicGeometry.Project(true,cell.X+.5f,cell.Y+.5f),ownAt=ClassicGeometry.Project(false,own.X+.5f,own.Y+.5f);
                require(board.PointerAt(targetAt)==BoardPointer.Target&&board.PointerAt(ownAt)==BoardPointer.Hand,"Target/ship pointer policy");
                State.Paused=true;require(board.PointerAt(targetAt)==BoardPointer.Arrow,"Pause retained active target pointer");State.Paused=false;
                require(State.Fire(cell),"Pointer shot fixture rejected");UpdateView();require(board.PointerAt(targetAt)==BoardPointer.Wait&&board.PointerAt(ownAt)==BoardPointer.Wait,"Pending movie did not signal wait");
                while(State.Pending!=null)State.CompleteMovie(State.Pending,State.Pending.Stage);require(State.Phase==Phase.OpponentTurn&&board.PointerAt(targetAt)==BoardPointer.Wait,"Opponent turn did not signal wait");
                State.Phase=Phase.PlayerTurn;require(board.PointerAt(targetAt)==BoardPointer.Arrow,"Previously resolved cell looked targetable");State.Phase=Phase.Finished;require(board.PointerAt(ownAt)==BoardPointer.Arrow,"Finished battle retained a hand");NewGame();require(board.PointerAt(targetAt)==BoardPointer.Arrow,"Deployment retained a battle cursor");
            });
            check("Stationary pointer refresh, system fallback and owned cursor disposal",delegate{
                preferences.MovieTurns=true;NewGame();auto.PerformClick();start.PerformClick();UpdateView();Point cell=State.Enemy.Ships[0].Cells[0];PointF artAt=ClassicGeometry.Project(true,cell.X+.5f,cell.Y+.5f);RectangleF r=board.ImageBounds;
                var move=typeof(BoardView).GetMethod("OnMouseMove",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);Point clientAt=Point.Round(new PointF(r.Left+artAt.X*r.Width/640,r.Top+artAt.Y*r.Height/480));
                move.Invoke(board,new object[]{new MouseEventArgs(MouseButtons.None,0,clientAt.X,clientAt.Y,0)});require(board.Cursor.HotSpot==new Point(15,15),"Native target hotspot binding");State.Fire(cell);UpdateView();require(board.Cursor==Cursors.WaitCursor,"Stationary pointer stayed target during movie");State.Paused=true;UpdateView();require(board.Cursor==Cursors.Default,"Pause did not refresh stationary pointer");
                var leave=typeof(BoardView).GetMethod("OnMouseLeave",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);leave.Invoke(board,new object[]{EventArgs.Empty});require(board.Cursor==Cursors.Default,"Mouse leave retained owned cursor");
                NewGame();auto.PerformClick();start.PerformClick();UpdateView();using(var local=new BoardView()){local.Size=new Size(640,480);local.Game=State;local.LoadCursors(Path.Combine(Path.GetDirectoryName(assets),"NoCursorFixture"));Point direct=Point.Round(artAt);move.Invoke(local,new object[]{new MouseEventArgs(MouseButtons.None,0,direct.X,direct.Y,0)});require(!local.HasOriginalCursors&&local.Cursor==Cursors.Cross,"Prepared-folder system fallback");local.LoadCursors(Path.Combine(Path.GetDirectoryName(assets),"Programs"));require(local.HasOriginalCursors&&local.Cursor.HotSpot==new Point(15,15),"Loading original resources retained fallback cursor");local.Dispose();local.RefreshInteractionCursor();local.Dispose();require(local.IsDisposed,"Late cursor refresh/disposal failed");}
                NewGame();
            });
            Func<Board,Point> visualWater=delegate(Board victim){for(int y=0;y<10;y++)for(int x=0;x<10;x++){var p=new Point(x,y);if(victim.ShipAt(p)==null&&!victim.Shots[x,y])return p;}throw new Exception("No water fixture");};
            check("Finite miss visuals start at resolved impact and respect pause",delegate {
                rules.SelectedIndex=0;preferences.MovieTurns=true;preferences.Motion=true;NewGame();auto.PerformClick();start.PerformClick();UpdateView();
                Point target=visualWater(State.Enemy);State.Fire(target);UpdateView();require(board.VisualEffectCount==0,"Firing spawned an early impact");
                State.CompleteMovie(State.Pending,ShotStage.Firing);UpdateView();require(board.VisualEffectCount==0,"Travel spawned an early impact");
                State.CompleteMovie(State.Pending,ShotStage.Travel);UpdateView();opponentTimer.Stop();
                var entries=new List<ClassicVisualEntry>(board.VisualEntries);require(entries.Count==1&&entries[0].Spec.Type==27&&entries[0].Counter==1&&entries[0].Enemy&&entries[0].Start==target,"Resolved miss entry");
                var pending=State.Pending;int turn=State.Turn;Shot shot=State.LastPlayerShot;State.Paused=true;board.DamageTick+=10;board.SynchronizeVisualEffects();require(entries[0].Counter==1,"Paused visual advanced");
                State.Paused=false;board.DamageTick+=3;board.SynchronizeVisualEffects();require(entries[0].Counter==4&&State.Pending==pending&&State.Turn==turn&&State.LastPlayerShot==shot,"Visual step changed gameplay");
            });
            check("Sinking visuals preserve victim start and the empty terminal tick",delegate {
                preferences.MovieTurns=false;preferences.Motion=true;NewGame();auto.PerformClick();start.PerformClick();UpdateView();Ship ship=State.Enemy.Ships[0];
                for(int i=0;i<ship.Cells.Count-1;i++)State.Enemy.Shoot(ship.Cells[i]);State.Fire(ship.Cells[ship.Cells.Count-1]);State.Paused=true;UpdateView();opponentTimer.Stop();
                var entries=new List<ClassicVisualEntry>(board.VisualEntries);require(entries.Count==1&&entries[0].Spec.Type==ship.SourceType&&entries[0].Start==ship.Cells[0],"Sinking identity/start lost");
                var entry=entries[0];State.Paused=false;board.DamageTick+=entry.Spec.LastCounter-1;board.SynchronizeVisualEffects();require(board.VisualEffectCount==1&&entry.Counter==entry.Spec.LastCounter&&!visualSprites.HasFrame(entry.Spec.Type,entry.Counter),"Empty terminal tick lost");
                board.DamageTick++;board.SynchronizeVisualEffects();require(board.VisualEffectCount==0,"Expired sinking sprite retained");State.Paused=true;
            });
            check("Motion disable clears finite effects and palette changes preserve active entries",delegate {
                preferences.MovieTurns=false;preferences.Motion=true;NewGame();auto.PerformClick();start.PerformClick();UpdateView();State.Fire(visualWater(State.Enemy));State.Paused=true;UpdateView();opponentTimer.Stop();
                require(board.VisualEffectCount==1,"Miss fixture absent");var entry=new List<ClassicVisualEntry>(board.VisualEntries)[0];int counter=entry.Counter;
                SelectColors(!preferences.RecoveredColors,false);require(board.VisualEffectCount==1&&entry.Counter==counter&&visualSprites.HasFrame(27,counter),"Palette swap reset or disposed active clip");
                preferences.Motion=false;UpdateDisplayChecks();require(board.VisualEffectCount==0,"Reduced motion retained frozen clip");preferences.Motion=true;UpdateDisplayChecks();require(board.VisualEffectCount==0,"Re-enabling motion replayed a historical shot");
                SelectColors(false,false);
            });
            check("Restart and return presentation clear finite effects without replay",delegate {
                preferences.MovieTurns=false;preferences.Motion=true;NewGame();auto.PerformClick();start.PerformClick();UpdateView();State.Fire(visualWater(State.Enemy));State.Paused=true;UpdateView();opponentTimer.Stop();
                require(board.VisualEffectCount==1&&RestartBattleConfirmed(State)&&board.VisualEffectCount==0,"Restart retained clip state");
                State.Fire(visualWater(State.Enemy));State.Paused=true;UpdateView();opponentTimer.Stop();require(board.VisualEffectCount==1,"Restarted impact missing");ClearBattlePresentation();UpdateView();require(board.VisualEffectCount==0,"Return/reset replayed historical impact");
            });
            check("Finished battle finite effects expire through presentation steps",delegate {
                preferences.MovieTurns=false;preferences.Motion=true;NewGame();auto.PerformClick();start.PerformClick();UpdateView();Ship last=State.Enemy.Ships[4];Point target=last.Cells[last.Cells.Count-1];
                foreach(Ship ship in State.Enemy.Ships)foreach(Point p in ship.Cells)if(p!=target)State.Enemy.Shoot(p);
                State.Fire(target);UpdateView();require(State.Phase==Phase.Finished&&board.VisualEffectCount==1,"Final sinking visual missing");
                var entry=new List<ClassicVisualEntry>(board.VisualEntries)[0];board.DamageTick+=entry.Spec.LastCounter;board.SynchronizeVisualEffects();require(board.VisualEffectCount==0,"Finished entry never retired");NewGame();
            });
            results.Add("UI checks passed="+(results.Count-failed)+" failed="+failed);
            results.Add("These checks use the real form and button/tab events offscreen. Key events are simulated; desktop focus and physical input remain separate checks.");
            File.WriteAllLines(report,results.ToArray());
            Close();
            return failed==0?0:1;
        }
        void FinishEntryForCheck(){if(entry.Timeline!=null){entry.Resume();entry.Advance(EntryTimeline.PreparationMilliseconds);}AdvanceBoardReveal(BoardRevealTimeline.DurationMilliseconds);}
        void PumpVisuals(int milliseconds) {
            var watch=Stopwatch.StartNew();
            while(watch.ElapsedMilliseconds<milliseconds){Application.DoEvents();System.Threading.Thread.Sleep(1);}
        }
        static string FleetOverviewFingerprint(Bitmap image) {
            return BitmapRegionFingerprint(image,new Rectangle(0,112,104,74));
        }
        static string BitmapRegionFingerprint(Bitmap image,Rectangle region) {
            var data=image.LockBits(region,System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try {
                var bytes=new byte[region.Width*region.Height*4];for(int y=0;y<region.Height;y++)System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),bytes,y*region.Width*4,region.Width*4);
                using(var hash=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes));
            }
            finally {image.UnlockBits(data);}
        }
        sealed class SimulatedClipOutput : IClipOutput {
            public bool FailStart,FailPosition,Paused,Disposed;public bool Active {get;private set;}int samples;public short[] LastPcm;public readonly List<int> Starts=new List<int>();public int Samples {get{if(FailPosition)throw new InvalidOperationException("injected position failure");return samples;}set{samples=value;}}
            public void Start(short[] pcm,int offset){if(FailStart)throw new InvalidOperationException("injected audio open failure");Samples=offset;LastPcm=pcm;Starts.Add(offset);Active=true;Paused=false;}
            public void Pause(){Paused=true;}public void Resume(){Paused=false;}public void Stop(){Active=false;Paused=false;}public void Dispose(){Stop();Disposed=true;}
        }
        protected override void Dispose(bool disposing) {
            if(disposing&&!disposed) {
                board.Reveal=null;disposed=true;
                opponentTimer.Stop();
                opponentTimer.Dispose();
                visualTimer.Stop();visualTimer.Dispose();displayMenu.Dispose();
                sound.Dispose();
                art.Dispose();legacyArt.Dispose();backdropForeground.Dispose();
                shipImages.Dispose();
                board.VisualSprites=null;if(visualSprites!=null)visualSprites.Dispose();
                damageImages.Dispose();hudImages.Dispose();messageFont.Dispose();ending.Dispose();front.Dispose();solo.Dispose();fleet.Dispose();entry.Dispose();prefsPage.Dispose();creditsPage.Dispose();startupPage.Dispose();archive.Dispose();
                foreach(var bitmap in rosterImages)if(bitmap!=null)bitmap.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}






















