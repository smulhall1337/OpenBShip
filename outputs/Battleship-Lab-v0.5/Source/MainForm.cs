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
        readonly MapView map=new MapView();
        readonly Timer opponentTimer=new Timer();
        readonly Timer visualTimer=new Timer();
        readonly Stopwatch visualTime=Stopwatch.StartNew();
        readonly EffectClock effectClock=new EffectClock();
        long visualLast;
        readonly Audio sound;
        readonly Label phase=new Label(),hint=new Label(),fleet=new Label(),enemy=new Label();
        readonly TextBox history=new TextBox();
        readonly ToolStripStatusLabel status=new ToolStripStatusLabel();
        readonly TabControl tabs=new TabControl();
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
        ClassicDamage damageImages;
        readonly IndexedImage classicImage;
        readonly DisplaySettings preferences;
        int seed=Environment.TickCount;
        bool disposed,previewMode;
        bool changingOpponent;
        int recoveredOpponent;
        readonly Color panel=Color.FromArgb(20,36,52),gold=Color.FromArgb(237,200,128);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
        public MainForm(string assetDir) {
            assets=assetDir;
            settingsPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"UserData","settings.txt");
            preferences=DisplaySettings.Load(settingsPath);
            classicImage=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));
            RebuildArt(preferences.RecoveredColors);
            sound=new Audio(assets);
            sound.Enabled=preferences.Sound;
            Text="Battleship Fleet Lab 0.5 — Classic damage and display options";
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
                Text="Recovered Classic targeting and ship art · original damage effects · selectable gameplay colors",Dock=DockStyle.Bottom,Height=22,ForeColor=Color.LightSteelBlue
            };
            header.Controls.Add(title,0,0);
            header.Controls.Add(subtitle,0,1);
            var display=displayButton;display.Text="Display";display.Width=105;display.Height=33;display.Anchor=AnchorStyles.Top|AnchorStyles.Right;display.FlatStyle=FlatStyle.Flat;display.BackColor=panel;display.ForeColor=gold;
            header.Controls.Add(display,1,0);
            display.Click+=delegate {displayMenu.Show(display,0,display.Height);};
            displayMenu.Items.Add(acceptedColors);displayMenu.Items.Add(recoveredColors);displayMenu.Items.Add(new ToolStripSeparator());displayMenu.Items.Add(motion);
            acceptedColors.Click+=delegate {SelectColors(false,true);};
            recoveredColors.Click+=delegate {SelectColors(true,true);};
            motion.Click+=delegate {preferences.Motion=!preferences.Motion;SavePreferences();UpdateDisplayChecks();};
            displayMenu.Opening+=delegate {
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
                sound.Enabled=!sound.Enabled;
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
            play.Controls.Add(board);
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
            AddLabel(sidebar,hint,82,10,Color.LightSteelBlue);
            AddLabel(sidebar,fleet,154,10,Color.WhiteSmoke);
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
            var strip=new StatusStrip {
                Dock=DockStyle.Fill,BackColor=panel
            };
            status.ForeColor=Color.WhiteSmoke;
            strip.Items.Add(status);
            layout.Controls.Add(strip,0,3);
            board.ActionDone+=delegate(string kind) {
                sound.Play(kind);
                UpdateView();
            };
            tabs.SelectedIndexChanged+=delegate {
                if(State==null)return;
                if(tabs.SelectedIndex!=0&&(State.Phase==Phase.PlayerTurn||State.Phase==Phase.OpponentTurn))State.Paused=true;
                UpdateView();
            };
            opponentTimer.Interval=600;
            opponentTimer.Tick+=delegate {
                bool wasPaused=State.Paused;
                if(State.AdvanceOpponent()) {
                    sound.Play("fire");
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
            Bitmap nextArt=null;var nextRoster=new List<Bitmap>();ClassicShips nextShips=null;ClassicDamage nextDamage=null;
            try {
                nextArt=new IndexedImage {Width=classicImage.Width,Height=classicImage.Height,Pixels=classicImage.Pixels,Palette=palette}.Bitmap();
                foreach(var frame in Assets.Sph(Path.Combine(assets,"Graphics","EO_CLASS.SPH"),palette))nextRoster.Add(frame==null?null:frame.Bitmap());
                nextShips=new ClassicShips(Path.Combine(assets,"Graphics"),palette);
                nextDamage=new ClassicDamage(Path.Combine(assets,"Graphics"),palette);
            }catch {
                if(nextArt!=null)nextArt.Dispose();foreach(var image in nextRoster)if(image!=null)image.Dispose();
                if(nextShips!=null)nextShips.Dispose();if(nextDamage!=null)nextDamage.Dispose();throw;
            }
            Bitmap previousArt=art;ClassicShips previousShips=shipImages;ClassicDamage previousDamage=damageImages;var previousRoster=rosterImages.ToArray();
            art=nextArt;shipImages=nextShips;damageImages=nextDamage;rosterImages.Clear();rosterImages.AddRange(nextRoster);
            board.Art=art;board.Ships=shipImages;board.Damage=damageImages;board.Roster=rosterImages.ToArray();
            if(previousArt!=null)previousArt.Dispose();if(previousShips!=null)previousShips.Dispose();if(previousDamage!=null)previousDamage.Dispose();
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
        void NewGame() {
            bool legacy=rules.SelectedIndex==1;
            State=new Game(unchecked(seed++),legacy?RuleProfile.Legacy:RuleProfile.Recovered,legacy||opponent.SelectedIndex==4?OpponentKind.Prototype:OpponentKind.Original,opponent.SelectedIndex<4?opponent.SelectedIndex:0);
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
            if(displayMenu.Visible)return;
            if((rules.ContainsFocus||opponent.ContainsFocus)&&e.KeyCode!=Keys.F2)return;
            if(displayButton.ContainsFocus&&(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space)) {
                if(!down.Add(e.KeyCode))e.SuppressKeyPress=true;
                return; // Let the focused native button receive its first confirmation.
            }
            if(tabs.SelectedIndex!=0&&e.KeyCode!=Keys.F2)return;
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
            hint.Text=placement?(State.Rules.FixedY?"Click the lower-left board at each ship's center. Ships follow the original fixed axis. Or use Auto-place, then Start battle.":"Click the lower-left board to place ships. Rotate with R. Or choose Auto-place, then Start battle."):"Click the upper-right board to fire. Arrows + Enter also work. Each shot ends your turn. P pauses; F2 starts over.";
            string roster="YOUR FLEET\r\n";
            for(int i=0; i<State.Rules.Fleet.Length; i++) {
                Ship item=i<State.Player.Ships.Count?State.Player.Ships[i]:null;
                ShipSpec spec=State.Rules.Fleet[i];
                roster+=spec.Name+"  "+(item==null?spec.Length+" / unplaced":item.Sunk?"SUNK":(item.Cells.Count-item.Hits)+" / "+item.Cells.Count)+"\r\n";
            }
            fleet.Text=roster;
            int sunk=0;
            foreach(var item in State.Enemy.Ships)if(item.Sunk)sunk++;
            enemy.Text="OPPONENT: "+sunk+" / 5 ships sunk";
            status.Text=sound.Problem!=""?sound.Problem:preferences.Problem!=""?preferences.Problem:State.Message;
            int from=Math.Max(0,State.Events.Count-9);
            history.Text=string.Join(Environment.NewLine,State.Events.GetRange(from,State.Events.Count-from).ToArray());
            board.Invalidate();
        }
        protected override bool ShowWithoutActivation {
            get {
                return previewMode;
            }
        }
        public void SavePreview(string path,string mode) {
            previewMode=true;
            opponentTimer.Stop();visualTimer.Stop();
            ShowInTaskbar=false;
            StartPosition=FormStartPosition.Manual;
            Location=new Point(-20000,-20000);
            seed=731; // Stable comparison scene; normal games still use a fresh seed.
            NewGame();
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
            if(mode=="map") {
                tabs.SelectedIndex=1;
                Application.DoEvents();
                if(!State.Paused)throw new InvalidOperationException("Inspection did not pause the active battle");
            }
            PerformLayout();
            using(var bmp=new Bitmap(Width,Height)) {
                DrawToBitmap(bmp,new Rectangle(0,0,Width,Height));
                bmp.Save(path,System.Drawing.Imaging.ImageFormat.Png);
            }
            Close();
        }
        public int CheckUi(string report) {
            previewMode=true;
            ShowInTaskbar=false;
            StartPosition=FormStartPosition.Manual;
            Location=new Point(-20000,-20000);
            sound.Enabled=false;
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
                    int px=(int)(bounds.X+164*bounds.Width/640),py=(int)(bounds.Y+331*bounds.Height/512);
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
            results.Add("UI checks passed="+(9-failed)+" failed="+failed);
            results.Add("These checks use the real form and button/tab events offscreen. Key events are simulated; desktop focus and physical input remain separate checks.");
            File.WriteAllLines(report,results.ToArray());
            Close();
            return failed==0?0:1;
        }
        void PumpVisuals(int milliseconds) {
            var watch=Stopwatch.StartNew();
            while(watch.ElapsedMilliseconds<milliseconds){Application.DoEvents();System.Threading.Thread.Sleep(1);}
        }
        protected override void Dispose(bool disposing) {
            if(disposing&&!disposed) {
                disposed=true;
                opponentTimer.Stop();
                opponentTimer.Dispose();
                visualTimer.Stop();visualTimer.Dispose();displayMenu.Dispose();
                sound.Dispose();
                art.Dispose();
                shipImages.Dispose();
                damageImages.Dispose();
                foreach(var bitmap in rosterImages)if(bitmap!=null)bitmap.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
