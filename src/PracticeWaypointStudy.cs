using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace FleetLab {
    // Modern, isolated data editor. It does not execute a Practice mission.
    public sealed class PracticeWaypointStudy : Form {
        readonly PracticeWaypointQueue queue;
        readonly RouteCanvas canvas;
        readonly NumericUpDown worldX = new NumericUpDown(), worldY = new NumericUpDown();
        readonly ListBox points = new ListBox();
        readonly Label result = new Label();
        readonly Button append = new Button(), deselect = new Button(), clear = new Button(), reset = new Button();
        readonly Button stepFleet = new Button(), stepTen = new Button();
        readonly Button pollFleet = new Button(), pollThirtyTwo = new Button();
        readonly Label pollData = new Label();
        readonly Label fleetData = new Label();
        readonly int fleetSlot;
        readonly PracticeMovementState initialMovement;
        PracticeMovementState movement;
        int updates;
        int polls;
        PracticeFleetScheduler schedule = new PracticeFleetScheduler(0, 0);
        readonly HashSet<Keys> held = new HashSet<Keys>();
        internal PracticeWaypointQueue Queue { get { return queue; } }

        public PracticeWaypointStudy(Scenario scene, PracticeFleetSnapshot fleet) : this(scene, fleet, null) { }
        public PracticeWaypointStudy(Scenario scene, PracticeFleetSnapshot fleet, PracticeDeployment deployment) {
            if (fleet == null) throw new ArgumentNullException("fleet");
            if ((fleet.Kind != 1 && fleet.Kind != 9) || fleet.Owner != 0)
                throw new ArgumentException("Choose a player 1 grouped fleet for this study.", "fleet");
            queue = new PracticeWaypointQueue(fleet.CopyFleetRecord(), scene, 1);
            fleetSlot = fleet.FleetSlot;
            if (deployment != null) movement = initialMovement = PracticeMovementState.ForStudy(deployment, fleetSlot);
            Text = "Recovered waypoints and fleet polls · fleet " + (fleet.FleetSlot + 1);
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(980, 800); MinimumSize = new Size(820, 640);
            BackColor = Color.FromArgb(12, 23, 35); ForeColor = Color.WhiteSmoke;
            Font = new Font("Segoe UI", 9);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(12) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 292));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            Controls.Add(layout);
            canvas = new RouteCanvas(scene, queue, deployment, fleetSlot) { Dock = DockStyle.Fill, TabStop = true, AccessibleName = "Schematic waypoint map" };
            layout.Controls.Add(canvas, 0, 0);
            canvas.Picked += delegate(Point p) { worldX.Value = p.X; worldY.Value = p.Y; AddPoint(); };
            canvas.KeyDown += delegate(object sender, KeyEventArgs e) {
                if (e.KeyCode != Keys.Enter && e.KeyCode != Keys.Left && e.KeyCode != Keys.Right && e.KeyCode != Keys.Up && e.KeyCode != Keys.Down) return;
                e.Handled = true; e.SuppressKeyPress = true;
                if (!held.Add(e.KeyCode)) return;
                if (e.KeyCode == Keys.Enter) AddPoint();
                else {
                    int x = (int)worldX.Value, y = (int)worldY.Value;
                    if (e.KeyCode == Keys.Left) x -= 10; if (e.KeyCode == Keys.Right) x += 10;
                    if (e.KeyCode == Keys.Up) y -= 10; if (e.KeyCode == Keys.Down) y += 10;
                    worldX.Value = Math.Max(0, Math.Min(639, x)); worldY.Value = Math.Max(0, Math.Min(639, y));
                }
            };
            canvas.KeyUp += delegate(object sender, KeyEventArgs e) { held.Remove(e.KeyCode); };
            canvas.LostFocus += delegate { held.Clear(); };
            Deactivate += delegate { held.Clear(); };
            var sideScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var side = new TableLayoutPanel { Dock = DockStyle.Top, Height = 650, ColumnCount = 1, RowCount = 13, Padding = new Padding(10, 0, 0, 0) };
            side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach(int height in new[]{42,62,32,32,112,32,32,32,34,34,38,52,54})side.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
            sideScroll.Controls.Add(side); layout.Controls.Add(sideScroll, 1, 0);
            fleetData.Dock = DockStyle.Fill; side.Controls.Add(fleetData, 0, 0);
            side.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Click a map cell, or enter world X/Y (0–639). Points snap to multiples of 10.\r\nMap focus: arrows move cursor; Enter adds." }, 0, 1);
            var inputs = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            foreach(var control in new[]{worldX,worldY}) { control.Maximum = 639; control.Width = 83; control.Increment = 10; }
            worldX.AccessibleName = "Waypoint world X"; worldY.AccessibleName = "Waypoint world Y";
            inputs.Controls.Add(new Label { Text = "X", AutoSize = true, Padding = new Padding(0,5,0,0) }); inputs.Controls.Add(worldX);
            inputs.Controls.Add(new Label { Text = "Y", AutoSize = true, Padding = new Padding(0,5,0,0) }); inputs.Controls.Add(worldY);
            side.Controls.Add(inputs, 0, 2);
            append.Text = "Add / select point"; append.Dock = DockStyle.Fill; side.Controls.Add(append, 0, 3);
            points.Dock = DockStyle.Fill; points.IntegralHeight = false; points.AccessibleName = "Five original waypoint slots"; side.Controls.Add(points, 0, 4);
            deselect.Text = "Deselect point"; clear.Text = "Clear queue"; reset.Text = "Reset study";
            foreach(var button in new[]{deselect,clear,reset})button.Dock=DockStyle.Fill;
            side.Controls.Add(deselect, 0, 5); side.Controls.Add(clear, 0, 6); side.Controls.Add(reset, 0, 7);
            var stepping = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            stepping.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); stepping.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            stepFleet.Text = "Step fleet"; stepTen.Text = "10 updates";
            stepFleet.Dock = stepTen.Dock = DockStyle.Fill;
            stepFleet.AccessibleName = "Advance selected fleet once"; stepTen.AccessibleName = "Advance selected fleet up to ten times";
            stepping.Controls.Add(stepFleet,0,0); stepping.Controls.Add(stepTen,1,0); side.Controls.Add(stepping,0,8);
            var polling = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            polling.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); polling.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            pollFleet.Text = "1 source poll"; pollThirtyTwo.Text = "32 source polls";
            pollFleet.Dock = pollThirtyTwo.Dock = DockStyle.Fill;
            pollFleet.AccessibleName = "Advance one normal source poll"; pollThirtyTwo.AccessibleName = "Advance up to thirty-two normal source polls";
            polling.Controls.Add(pollFleet,0,0); polling.Controls.Add(pollThirtyTwo,1,0); side.Controls.Add(polling,0,9);
            pollData.Dock = DockStyle.Fill; pollData.ForeColor = Color.LightSteelBlue; pollData.AccessibleName = "Source poll phase"; side.Controls.Add(pollData,0,10);
            result.Dock = DockStyle.Fill; result.ForeColor = Color.LightCyan; result.AccessibleName = "Waypoint result"; side.Controls.Add(result, 0, 11);
            side.Controls.Add(new Label { Dock = DockStyle.Fill, ForeColor = Color.LightSteelBlue, Text = "Only the selected fleet moves.\r\nOther fleets are fixed blockers.\r\nEach source poll visits two batches." }, 0, 12);
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,88));
            footer.Controls.Add(new Label { Dock = DockStyle.Fill, ForeColor = Color.LightSteelBlue, Text = "Manual study with schematic terrain; update buttons leave the poll cursor unchanged. No wall-clock pacing, AI or missions. Reset restores position, queue and poll phase. Closing discards the study." }, 0, 0);
            var close = new Button { Text = "Close", Dock = DockStyle.Fill, DialogResult = DialogResult.Cancel };
            footer.Controls.Add(close,1,0); layout.Controls.Add(footer,0,1); layout.SetColumnSpan(footer,2); CancelButton=close;
            append.Click += delegate { AddPoint(); };
            deselect.Click += delegate { queue.Deselect(); Sync("Selection cleared; queue preserved."); };
            clear.Click += delegate { queue.Clear(); Sync("Queue cleared; position and poll phase preserved."); };
            reset.Click += delegate { queue.Reset(); movement = initialMovement; updates = polls = 0; schedule = new PracticeFleetScheduler(0,0); canvas.BlockingCode = 255; Sync("Copied startup, queue and poll phase restored."); };
            stepFleet.Click += delegate { AdvanceFleet(1); };
            stepTen.Click += delegate { AdvanceFleet(10); };
            pollFleet.Click += delegate { AdvancePolls(1); };
            pollThirtyTwo.Click += delegate { AdvancePolls(32); };
            worldX.ValueChanged += delegate { UpdateCursorPosition(); }; worldY.ValueChanged += delegate { UpdateCursorPosition(); };
            worldX.Value = queue.Origin.X; worldY.Value = queue.Origin.Y;
            Sync("Ready · 5 waypoint slots.");
        }

        void UpdateCursorPosition() { canvas.CursorWorld = new Point((int)worldX.Value,(int)worldY.Value); canvas.Invalidate(); }
        internal void AddPoint() {
            var outcome = queue.AppendOrSelect(new Point((int)worldX.Value,(int)worldY.Value));
            Sync(outcome == PracticeWaypointResult.Added ? "Point added · " + queue.Count + " / 5 slots." :
                 outcome == PracticeWaypointResult.Selected ? "Existing point selected; deselect to append." :
                 outcome == PracticeWaypointResult.TerrainBlocked ? "This fleet cannot queue terrain kind 2." :
                 outcome == PracticeWaypointResult.Full ? "All 5 slots are occupied." : "Deselect the current point before appending.");
        }
        void Sync(string message) {
            points.Items.Clear();
            for(int i=0;i<5;i++)points.Items.Add((i+1)+": "+(queue.HasWaypoint(i)?queue.Waypoint(i).X+", "+queue.Waypoint(i).Y:"empty"));
            points.SelectedIndex=queue.Selected; deselect.Enabled=queue.Selected!=-1; result.Text=message; canvas.Invalidate();
            fleetData.Text="Fleet "+(fleetSlot+1)+" · player 1\r\nOrigin "+queue.Origin.X+", "+queue.Origin.Y+" · updates "+updates;
            stepFleet.Enabled=stepTen.Enabled=movement!=null&&queue.HasWaypoint(0);
            pollFleet.Enabled=pollThirtyTwo.Enabled=movement!=null&&queue.HasWaypoint(0);
            pollData.Text="Source polls "+polls+" · next batch "+schedule.BatchStart+"\r\nCompleted cycles "+schedule.CompletedCycles;
        }
        bool ApplyUpdate(PracticeMovementUpdate update, out string message) {
            queue.ApplyMovementRecord(update.State.CopyFleetRecord(fleetSlot),update.State.SelectedWaypoint);
            movement=update.State; updates++; canvas.BlockingCode=update.CollisionCode;
            message=update.Outcome==PracticeMovementOutcome.BlockedByFleet?"Blocked by fleet "+(update.CollisionCode+1)+". Clear queue to reroute.":
                update.Outcome==PracticeMovementOutcome.BlockedByTerrain?"Blocked by terrain. Clear queue to reroute.":
                update.Outcome==PracticeMovementOutcome.ReachedLockedWaypoint?"Reached point; source auxiliary state retains it.":
                update.QueueConsumed?(queue.HasWaypoint(0)?"Point reached; next point is ready.":"Queue complete."):"Fleet advanced; "+updates+" updates.";
            return update.Outcome==PracticeMovementOutcome.BlockedByFleet||update.Outcome==PracticeMovementOutcome.BlockedByTerrain||
                   update.Outcome==PracticeMovementOutcome.ReachedLockedWaypoint;
        }
        internal void AdvancePolls(int limit) {
            if(limit<1||limit>32)throw new ArgumentOutOfRangeException("limit");
            if(movement==null){Sync("Open this study from the first Practice startup to step.");return;}
            int before=updates, advanced=0;bool stopped=false;string message="Queue a point before stepping.";
            try {
                for(int i=0;i<limit&&queue.HasWaypoint(0);i++){
                    var current=movement.WithFleetRecord(fleetSlot,queue.CopyRecord(),queue.Selected);
                    var poll=current.StepSelectedFleetPoll(schedule,fleetSlot);
                    if(poll.FleetUpdate!=null)stopped=ApplyUpdate(poll.FleetUpdate,out message);
                    movement=poll.State;schedule=poll.NextSchedule;polls++;advanced++;
                    if(stopped)break;
                }
                if(advanced>0&&!stopped&&queue.HasWaypoint(0))message="Advanced "+advanced+" source "+(advanced==1?"poll":"polls")+"; "+(updates-before)+" fleet "+(updates-before==1?"update.":"updates.");
            }catch(NotSupportedException e){message=e.Message;}
             catch(System.IO.InvalidDataException e){message=e.Message;}
            Sync(message);
        }
        internal void AdvanceFleet(int limit) {
            if(limit<1||limit>10)throw new ArgumentOutOfRangeException("limit");
            if(movement==null){Sync("Open this study from the first Practice startup to step.");return;}
            string message="Queue a point before stepping.";
            try {
                for(int i=0;i<limit&&queue.HasWaypoint(0);i++){
                    var current=movement.WithFleetRecord(fleetSlot,queue.CopyRecord(),queue.Selected);
                    var update=current.Step(fleetSlot);
                    if(ApplyUpdate(update,out message))break;
                }
            }catch(NotSupportedException e){message=e.Message;}
             catch(System.IO.InvalidDataException e){message=e.Message;}
            Sync(message);
        }

        sealed class RouteCanvas : Control {
            struct FixedFleet { public int Slot,Owner;public Point Origin; }
            readonly byte[] kinds = new byte[4096]; readonly PracticeWaypointQueue queue;
            readonly List<FixedFleet> fixedFleets = new List<FixedFleet>();
            readonly ToolTip tooltip = new ToolTip();
            string tooltipText;
            bool tooltipDisposed;
            public int BlockingCode = 255;
            public Point CursorWorld; public event Action<Point> Picked; Point? pressed;
            RectangleF MapBounds { get { float size=Math.Max(0,Math.Min(Width-24,Height-38));return new RectangleF((Width-size)/2,12,size,size); } }
            public RouteCanvas(Scenario scene,PracticeWaypointQueue state,PracticeDeployment deployment,int slot) {
                queue=state;for(int i=0;i<4096;i++)kinds[i]=scene.Cells[i].Kind;DoubleBuffered=true;ResizeRedraw=true;
                if(deployment!=null)foreach(var fleet in deployment.Fleets)if(fleet.FleetSlot!=slot)
                    fixedFleets.Add(new FixedFleet{Slot=fleet.FleetSlot,Owner=fleet.Owner,Origin=fleet.WorldOrigin});
            }
            internal Point Pixel(Point world) {var b=MapBounds;return new Point((int)(b.Left+(world.X+5)*b.Width/640),(int)(b.Top+(world.Y+5)*b.Height/640));}
            Point? World(Point pixel) {var b=MapBounds;if(b.Width<1||pixel.X<b.Left||pixel.X>=b.Right||pixel.Y<b.Top||pixel.Y>=b.Bottom)return null;return new Point(Math.Min(630,(int)((pixel.X-b.Left)*64/b.Width)*10),Math.Min(630,(int)((pixel.Y-b.Top)*64/b.Height)*10));}
            protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();pressed=World(e.Location);}}
            protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);var point=World(e.Location);if(e.Button==MouseButtons.Left&&pressed.HasValue&&point==pressed&&Picked!=null)Picked(point.Value);pressed=null;}
            protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);pressed=null;}
            protected override void OnLostFocus(EventArgs e){pressed=null;base.OnLostFocus(e);}
            protected override void OnMouseMove(MouseEventArgs e){
                base.OnMouseMove(e);if(tooltipDisposed)return;string text=null;
                foreach(var fleet in fixedFleets){Point p=Pixel(fleet.Origin);if(Math.Abs(p.X-e.X)<7&&Math.Abs(p.Y-e.Y)<7){text="Fixed fleet "+(fleet.Slot+1)+" · player "+(fleet.Owner+1)+" · origin "+fleet.Origin.X+", "+fleet.Origin.Y;break;}}
                if(text!=tooltipText){tooltipText=text;tooltip.SetToolTip(this,text);}
            }
            protected override void Dispose(bool disposing){if(disposing&&!tooltipDisposed){tooltipDisposed=true;tooltip.Dispose();}base.Dispose(disposing);}
            protected override bool IsInputKey(Keys keyData){Keys key=keyData&Keys.KeyCode;return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||key==Keys.Enter||base.IsInputKey(keyData);}
            protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var b=MapBounds;if(b.Width<1)return;var g=e.Graphics;float step=b.Width/64;
                for(int y=0;y<64;y++)for(int x=0;x<64;x++){byte kind=kinds[y*64+x];Color color=kind==2?Color.FromArgb(123,145,91):kind==7?Color.Teal:kind==4?Color.FromArgb(40,84,106):Color.FromArgb(24,55,77);using(var brush=new SolidBrush(color))g.FillRectangle(brush,b.Left+x*step,b.Top+y*step,step+.1f,step+.1f);}
                foreach(var fleet in fixedFleets){Point p=Pixel(fleet.Origin);using(var brush=new SolidBrush(fleet.Owner==0?Color.LightSteelBlue:Color.Orange))g.FillEllipse(brush,p.X-3,p.Y-3,6,6);if(BlockingCode==fleet.Slot){using(var pen=new Pen(Color.Gold,2))g.DrawEllipse(pen,p.X-7,p.Y-7,14,14);g.DrawString("F"+(fleet.Slot+1),Font,Brushes.Gold,p.X+7,p.Y-7);}}
                Point last=Pixel(queue.Origin);using(var pen=new Pen(Color.Gold,2))g.DrawRectangle(pen,last.X-4,last.Y-4,8,8);
                var labels=new List<Point>();
                Point[] offsets={Point.Empty,new Point(0,-20),new Point(0,20),new Point(-20,0),new Point(20,0),new Point(-20,-20),new Point(20,-20),new Point(-20,20),new Point(20,20)};
                using(var pen=new Pen(Color.LightCyan,2))for(int i=0;i<5;i++)if(queue.HasWaypoint(i)){
                    Point p=Pixel(queue.Waypoint(i));g.DrawLine(pen,last,p);last=p;
                    Point label=p;
                    foreach(Point offset in offsets){
                        Point candidate=new Point((int)Math.Max(b.Left+9,Math.Min(b.Right-9,p.X+offset.X)),(int)Math.Max(b.Top+9,Math.Min(b.Bottom-9,p.Y+offset.Y)));
                        bool available=true;foreach(Point prior in labels)if(Math.Abs(candidate.X-prior.X)<18&&Math.Abs(candidate.Y-prior.Y)<18)available=false;
                        if(available){label=candidate;break;}
                    }
                    labels.Add(label);if(label!=p)g.DrawLine(pen,p,label);
                    g.FillEllipse(Brushes.LightCyan,p.X-2,p.Y-2,4,4);
                    using(var brush=new SolidBrush(queue.Selected==i?Color.Gold:Color.LightCyan))g.FillEllipse(brush,label.X-7,label.Y-7,14,14);
                    g.DrawString((i+1).ToString(),Font,Brushes.Black,label.X-5,label.Y-7);
                }
                Point cursor=Pixel(new Point(CursorWorld.X/10*10,CursorWorld.Y/10*10));using(var pen=new Pen(Focused?Color.White:Color.Silver))g.DrawRectangle(pen,cursor.X-step/2,cursor.Y-step/2,step,step);
                g.DrawString(fixedFleets.Count==0?"64 × 64 schematic · lines show queue order":"Dots: fixed fleets · gold: active fleet · lines: queue",Font,Brushes.LightSteelBlue,new RectangleF(8,Height-25,Math.Max(0,Width-16),24));
            }
        }
    }
}
