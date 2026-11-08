using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Media;
namespace FleetLab {
    public sealed class MainForm : Form {
        public Game State;
        readonly string assets,settingsPath;
        readonly BoardView board=new BoardView();
        readonly MapView map=new MapView();
        readonly Timer opponentTimer=new Timer();
        readonly Audio sound;
        readonly Label phase=new Label(),hint=new Label(),fleet=new Label(),enemy=new Label();
        readonly TextBox history=new TextBox();
        readonly ToolStripStatusLabel status=new ToolStripStatusLabel();
        readonly TabControl tabs=new TabControl();
        readonly ComboBox rules=new ComboBox();
        readonly Button auto=new Button(),clear=new Button(),rotate=new Button(),start=new Button(),pause=new Button(),audio=new Button();
        readonly HashSet<Keys> down=new HashSet<Keys>();
        Bitmap art;
        readonly List<Bitmap> rosterImages=new List<Bitmap>();
        int seed=Environment.TickCount;
        bool disposed,previewMode;
        readonly Color panel=Color.FromArgb(20,36,52),gold=Color.FromArgb(237,200,128);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
        public MainForm(string assetDir) {
            assets=assetDir;
            settingsPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"UserData","settings.txt");
            var classic=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));
            art=classic.Bitmap();
            foreach(var frame in Assets.Sph(Path.Combine(assets,"Graphics","EO_CLASS.SPH"),classic.Palette))rosterImages.Add(frame==null?null:frame.Bitmap());
            sound=new Audio(assets);
            try {
                if(File.Exists(settingsPath))sound.Enabled=File.ReadAllText(settingsPath).Trim()!="sound=false";
            }
            catch(Exception e) {
                sound.Problem="Settings unavailable: "+e.Message;
            }
            Text="Battleship Fleet Lab 0.2 — Recovered Classic fleet";
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
            var header=new Panel {
                Dock=DockStyle.Fill,Padding=new Padding(18,10,18,8),BackColor=panel
            };
            var title=new Label {
                Text="BATTLESHIP / FLEET LAB",Font=new Font("Segoe UI",23,FontStyle.Bold),ForeColor=gold,Dock=DockStyle.Top,Height=40
            };
            var subtitle=new Label {
                Text="Recovered Classic fleet and shot rules · original status sprites · modern opponent and controls",Dock=DockStyle.Bottom,Height=22,ForeColor=Color.LightSteelBlue
            };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
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
                try {
                    Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                    File.WriteAllText(settingsPath,sound.Enabled?"sound=true":"sound=false");
                }
                catch(Exception e) {
                    sound.Problem="Could not save settings: "+e.Message;
                }
                UpdateView();
            });
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
            play.Controls.Add(board);
            play.Controls.Add(sidebar);
            rules.Width=224;
            rules.DropDownStyle=ComboBoxStyle.DropDownList;
            rules.Items.AddRange(new object[] {"Recovered Classic (19 cells)","v0.1 prototype (17 cells)"});
            rules.SelectedIndex=0;
            sidebar.Controls.Add(rules);
            AddLabel(sidebar,new Label {Text="Preset changes reset deployment."},25,9,Color.LightSteelBlue);
            rules.SelectedIndexChanged+=delegate { NewGame(); };
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
                if(State.AdvanceOpponent()) {
                    sound.Play("fire");
                    UpdateView();
                }
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
            Activated+=delegate {
                foreach(Keys key in new[] {
                    Keys.Enter,Keys.R,Keys.P,Keys.Escape,Keys.F2,Keys.Left,Keys.Right,Keys.Up,Keys.Down
                }) {
                    if((GetAsyncKeyState((int)key)&0x8000)!=0)down.Add(key);
                    else down.Remove(key);
                }
            };
            NewGame();
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
            State=new Game(unchecked(seed++),rules.SelectedIndex==1?RuleProfile.Legacy:RuleProfile.Recovered);
            board.Game=State;
            board.KeyboardCell=new Point(0,0);
            tabs.SelectedIndex=0;
            UpdateView();
        }
        void Rotate() {
            if(State.Rotate())UpdateView();
        }
        void OnKey(object sender,KeyEventArgs e) {
            if(rules.ContainsFocus&&e.KeyCode!=Keys.F2)return;
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
            start.Enabled=placement&&active&&State.Player.Ships.Count==State.Rules.Fleet.Length;
            pause.Enabled=active&&(State.Phase==Phase.PlayerTurn||State.Phase==Phase.OpponentTurn);
            pause.Text=State.Paused?"Resume (P)":"Pause (P)";
            rotate.Text=State.Rules.FixedY?"Fixed axis":State.Vertical?"Vertical (R)":"Horizontal (R)";
            audio.Text=sound.Enabled?"Sound: on":"Sound: off";
            phase.Text=State.Paused?"Paused":placement?"Deploy your fleet":State.Phase==Phase.PlayerTurn?"Your turn / "+State.Turn:State.Phase==Phase.OpponentTurn?"Opponent's turn":"Battle complete";
            hint.Text=placement?(State.Rules.FixedY?"Place ships on the lower-left board along the original fixed axis. Or choose Auto-place, then Start battle.":"Click the lower-left board to place ships. Rotate with R. Or choose Auto-place, then Start battle."):"Click the upper-right board to fire. Arrows + Enter also work. Each shot ends your turn. P pauses; F2 starts over.";
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
            status.Text=sound.Problem!=""?sound.Problem:State.Message;
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
            ShowInTaskbar=false;
            StartPosition=FormStartPosition.Manual;
            Location=new Point(-20000,-20000);
            if(mode=="small")Size=MinimumSize;
            if(mode=="legacy")rules.SelectedIndex=1;
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
            results.Add("UI checks passed="+(4-failed)+" failed="+failed);
            results.Add("These checks use the real form and button/tab events offscreen. Key events are simulated; desktop focus and physical input remain separate checks.");
            File.WriteAllLines(report,results.ToArray());
            Close();
            return failed==0?0:1;
        }
        protected override void Dispose(bool disposing) {
            if(disposing&&!disposed) {
                disposed=true;
                opponentTimer.Stop();
                opponentTimer.Dispose();
                sound.Dispose();
                art.Dispose();
                foreach(var bitmap in rosterImages)if(bitmap!=null)bitmap.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
