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
        readonly Button auto=new Button(),clear=new Button(),rotate=new Button(),start=new Button(),pause=new Button(),audio=new Button();
        readonly HashSet<Keys> down=new HashSet<Keys>();
        Bitmap art,ship;
        int seed=Environment.TickCount;
        bool disposed,previewMode;
        readonly Color panel=Color.FromArgb(20,36,52),gold=Color.FromArgb(237,200,128);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
        public MainForm(string assetDir) {
            assets=assetDir;
            settingsPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"UserData","settings.txt");
            art=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Bitmap();
            var frames=Assets.Sph(Path.Combine(assets,"Graphics","BATTSHIP.SPH"),Assets.Palette(Path.Combine(assets,"Graphics","ISLAND.COL")));
            ship=frames[0].Bitmap();
            sound=new Audio(assets);
            try {
                if(File.Exists(settingsPath))sound.Enabled=File.ReadAllText(settingsPath).Trim()!="sound=false";
            }
            catch(Exception e) {
                sound.Problem="Settings unavailable: "+e.Message;
            }
            Text="Battleship Fleet Lab 0.1 — Classic reconstruction prototype";
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
                Text="Classic duel prototype · recovered board art and sprites · experimental rules",Dock=DockStyle.Bottom,Height=22,ForeColor=Color.LightSteelBlue
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
            var sidebar=new FlowLayoutPanel {
                Dock=DockStyle.Right,Width=267,Padding=new Padding(14),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=panel
            };
            board.Dock=DockStyle.Fill;
            board.Art=art;
            play.Controls.Add(board);
            play.Controls.Add(sidebar);
            AddLabel(sidebar,phase,38,16,gold);
            AddLabel(sidebar,hint,82,10,Color.LightSteelBlue);
            AddLabel(sidebar,fleet,154,10,Color.WhiteSmoke);
            AddLabel(sidebar,enemy,36,10,gold);
            var picture=new PictureBox {
                Width=224,Height=110,Image=ship,SizeMode=PictureBoxSizeMode.Zoom,BackColor=panel
            };
            sidebar.Controls.Add(picture);
            AddLabel(sidebar,new Label {
                Text="Original BATTSHIP.SPH sprite"
            },23,9,Color.LightSteelBlue);
            history.Width=224;
            history.Height=155;
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
            State=new Game(unchecked(seed++));
            board.Game=State;
            board.KeyboardCell=new Point(0,0);
            tabs.SelectedIndex=0;
            UpdateView();
        }
        void Rotate() {
            if(State.Phase==Phase.Placement) {
                State.Vertical=!State.Vertical;
                UpdateView();
            }
        }
        void OnKey(object sender,KeyEventArgs e) {
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
            auto.Enabled=clear.Enabled=rotate.Enabled=placement&&active;
            start.Enabled=placement&&active&&State.Player.Ships.Count==5;
            pause.Enabled=active&&(State.Phase==Phase.PlayerTurn||State.Phase==Phase.OpponentTurn);
            pause.Text=State.Paused?"Resume (P)":"Pause (P)";
            rotate.Text=State.Vertical?"Vertical (R)":"Horizontal (R)";
            audio.Text=sound.Enabled?"Sound: on":"Sound: off";
            phase.Text=State.Paused?"Paused":placement?"Deploy your fleet":State.Phase==Phase.PlayerTurn?"Your turn / "+State.Turn:State.Phase==Phase.OpponentTurn?"Opponent's turn":"Battle complete";
            hint.Text=placement?"Click the lower-left board to place ships. Rotate with R. Or choose Auto-place, then Start battle.":"Click the upper-right board to fire. Arrows + Enter also work. Each shot ends your turn. P pauses; F2 starts over.";
            string roster="YOUR FLEET\r\n";
            for(int i=0; i<5; i++) {
                Ship item=i<State.Player.Ships.Count?State.Player.Ships[i]:null;
                roster+=Game.Names[i]+"  "+(item==null?Game.Lengths[i]+" cells / unplaced":item.Sunk?"SUNK":(item.Cells.Count-item.Hits)+" / "+item.Cells.Count)+"\r\n";
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
            if(mode=="battle") {
                State.AutoPlace();
                State.Start();
                State.Fire(new Point(4,4));
                State.AdvanceOpponent();
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
        protected override void Dispose(bool disposing) {
            if(disposing&&!disposed) {
                disposed=true;
                opponentTimer.Stop();
                opponentTimer.Dispose();
                sound.Dispose();
                art.Dispose();
                ship.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
