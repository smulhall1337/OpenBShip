using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FleetLab
{
    public sealed class PracticeIdleReturnStudy : Form
    {
        internal static readonly string[] Profiles = {
            "No idle returns (baseline)", "Fleet16: return toward referenced fleet0",
            "Inactive record249: cached return (diagnostic)", "Mixed returns: fleet16 and record249",
            "All250 records: cached returns (diagnostic)", "Cross-referenced returns16 /249 (diagnostic)"
        };
        readonly string programs; readonly PracticeMapStartup startup;
        readonly ComboBox profile = new ComboBox(), history = new ComboBox(), waypoint = new ComboBox(), record = new ComboBox();
        readonly Button load = new Button(), reset = new Button(), execute = new Button();
        readonly Label summary = new Label(), feedback = new Label();
        readonly ListBox queue = new ListBox(); readonly TextBox details = new TextBox();
        readonly IdleCanvas canvas; readonly HashSet<Keys> held = new HashSet<Keys>();
        PracticeIdleStudyState state; bool changing;
        internal PracticeIdleStudyState StudyState { get { return state; } }
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);

        public PracticeIdleReturnStudy(string programs, PracticeMapStartup startup)
        {
            this.programs = programs; this.startup = startup;
            Name = "idleReturnStudy"; Text = "Idle returns · original-map numeric copy";
            StartPosition = FormStartPosition.CenterParent; Size = new Size(1180, 860); MinimumSize = new Size(980, 740);
            Font = new Font("Segoe UI", 9); BackColor = Color.FromArgb(12, 23, 35); ForeColor = Color.WhiteSmoke; KeyPreview = true;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(8) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 370)); Controls.Add(layout);
            canvas = new IdleCanvas(this) { Name = "idleReturnCanvas", Dock = DockStyle.Fill }; layout.Controls.Add(canvas, 0, 0);
            var scroll = new Panel { Name = "idleReturnScroll", Dock = DockStyle.Fill, AutoScroll = true }; layout.Controls.Add(scroll, 1, 0);
            var side = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(4) }; scroll.Controls.Add(side);
            Action<Control, int> add = delegate(Control c, int height)
            {
                c.Width = 336; c.Height = height; c.Margin = new Padding(0, 3, 0, 3);
                if (c is ComboBox || c is Button || c is ListBox) { c.BackColor = Color.FromArgb(28, 55, 76); c.ForeColor = ForeColor; }
                var b = c as Button;
                if (b != null) { b.UseVisualStyleBackColor = false; b.FlatStyle = FlatStyle.Flat; b.EnabledChanged += delegate { if (b.IsDisposed || b.Disposing) return; b.BackColor = b.Enabled ? Color.FromArgb(28, 55, 76) : Color.LightGray; b.ForeColor = b.Enabled ? ForeColor : Color.Black; }; }
                side.Controls.Add(c);
            };
            add(new Label { Text = "One copied idle-return + navigation pass. It changes queued destinations, not fleet positions. Return tasks and context are supplied fixtures. UI refreshes are counted; no source renderer, clock, shots or full battle runs." }, 94);
            add(new Label { Text = "Return fixture (pending until Load)" }, 20);
            profile.Name = "idleReturnProfile"; profile.DropDownStyle = ComboBoxStyle.DropDownList; profile.DropDownWidth = 520; profile.Items.AddRange(Profiles); profile.SelectedIndex = 1; add(profile, 30);
            history.Name = "idleReturnHistory"; history.DropDownStyle = ComboBoxStyle.DropDownList; history.Items.AddRange(new object[] { "Copied map: normal history", "Copied map: prefilled history (fixture)" }); history.SelectedIndex = 0; add(history, 30);
            waypoint.Name = "idleReturnWaypoint"; waypoint.DropDownStyle = ComboBoxStyle.DropDownList; waypoint.Items.AddRange(new object[] { "Supplied selected waypoint: -1 (none)", "Supplied selected waypoint: 4" }); waypoint.SelectedIndex = 1; add(waypoint, 30);
            load.Name = "idleReturnLoad"; load.Text = "Load selected return copy"; add(load, 34);
            reset.Name = "idleReturnReset"; reset.Text = "Reset applied copy before pass"; add(reset, 34);
            execute.Name = "idleReturnExecute"; execute.Text = "Apply one idle + navigation pass"; add(execute, 40);
            summary.Name = "idleReturnSummary"; add(summary, 142); feedback.Name = "idleReturnFeedback"; add(feedback, 82);
            add(new Label { Text = "View fleet record (selection only)" }, 20);
            record.Name = "idleReturnRecord"; record.DropDownStyle = ComboBoxStyle.DropDownList; record.DropDownWidth = 420; add(record, 30);
            add(new Label { Text = "Waypoint queue: before -> after" }, 20); queue.Name = "idleReturnQueue"; queue.HorizontalScrollbar = true; add(queue, 100);
            details.Name = "idleReturnDetails"; details.Multiline = true; details.ReadOnly = true; details.ScrollBars = ScrollBars.Vertical; details.BackColor = BackColor; details.ForeColor = ForeColor; add(details, 300);
            var close = new Button { Name = "idleReturnClose", Text = "Close" }; add(close, 34); close.Click += delegate { Close(); };
            profile.SelectedIndexChanged += delegate { Pending(); }; history.SelectedIndexChanged += delegate { Pending(); }; waypoint.SelectedIndexChanged += delegate { Pending(); };
            record.SelectedIndexChanged += delegate { if (!changing) RefreshRecord(); };
            load.Click += delegate { LoadCopy(); };
            reset.Click += delegate { if (state != null) { state = state.Reset(); Feedback("Applied return copy restored. Pending selections are unchanged."); Sync(false); } };
            execute.Click += delegate
            {
                if (state == null) return;
                try { state = state.Execute(); Feedback("Applied " + state.IdleReturns + " return(s); " + state.RouteUiRequests + " UI refresh request(s) reported as data. Fleet origins stay fixed. Reset to repeat."); Sync(false); }
                catch (InvalidOperationException e) { Reject(e.Message); } catch (NotSupportedException e) { Reject(e.Message); } catch (InvalidDataException e) { Reject(e.Message); }
            };
            KeyUp += delegate(object sender, KeyEventArgs e) { held.Remove(e.KeyCode); };
            Deactivate += delegate { foreach (Keys key in new[] { Keys.Enter, Keys.Space, Keys.Escape }) if ((GetAsyncKeyState((int)key) & 0x8000) != 0) held.Add(key); };
            Activated += delegate { foreach (Keys key in new[] { Keys.Enter, Keys.Space, Keys.Escape }) { if ((GetAsyncKeyState((int)key) & 0x8000) != 0) held.Add(key); else held.Remove(key); } };
            LoadCopy();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                var button = ActiveControl as Button;
                if (e.KeyCode == Keys.Escape || button != null)
                { e.Handled = e.SuppressKeyPress = true; if (held.Add(e.KeyCode)) { if (e.KeyCode == Keys.Escape) Close(); else if (button.Enabled) button.PerformClick(); } return; }
            }
            base.OnKeyDown(e);
        }
        void Pending() { if (!changing && state != null) Feedback("Selections pending. Load applies them; Reset keeps the applied fixture."); }
        void Feedback(string message) { if (IsDisposed || Disposing) return; feedback.ForeColor = Color.LightSteelBlue; feedback.Text = message; }
        void Reject(string message) { if (IsDisposed || Disposing) return; feedback.ForeColor = Color.Orange; feedback.Text = "Rejected; applied copy preserved. " + message; Sync(false); }
        void LoadCopy()
        {
            try { var next = PracticeIdleStudyState.Create(programs, startup, history.SelectedIndex, profile.SelectedIndex, waypoint.SelectedIndex == 0 ? -1 : 4); state = next; Feedback("Copied map and supplied return tasks loaded. Apply once to inspect queue changes."); Sync(true); }
            catch (ArgumentException e) { Reject(e.Message); } catch (NotSupportedException e) { Reject(e.Message); } catch (IOException e) { Reject(e.Message); } catch (UnauthorizedAccessException e) { Reject(e.Message); }
        }
        void Sync(bool chooseDefault)
        {
            if (IsDisposed || Disposing) return; execute.Enabled = state != null && !state.Executed; reset.Enabled = record.Enabled = state != null;
            if (state == null) { summary.Text = "No admitted copied return fixture."; return; }
            summary.Text = "Applied fixture " + state.Profile + " · history " + state.HistoryFixture + "\r\nPass " + (state.Executed ? "complete" : "prepared") + " · visits " + state.Visits + "\r\nIdle returns " + state.IdleReturns + " · UI requests " + state.RouteUiRequests + "\r\nSelected waypoint " + state.InitialSelectedWaypoint + " -> " + state.SelectedWaypoint + "\r\nNo fleet movement or clock advancement.\r\nInactive records are included in the source pass.";
            changing = true;
            if (record.Items.Count != 250) { var f = state.CopyFleetPool(); record.Items.Clear(); for (int g = 0; g < 250; g++) { int kind = BitConverter.ToInt32(f, g * 492); record.Items.Add("Record " + g + " · kind " + kind + (kind == 0 ? " (inactive)" : "")); } }
            if (chooseDefault || record.SelectedIndex < 0) record.SelectedIndex = state.Profile == 2 ? 249 : 16;
            changing = false; RefreshRecord();
        }
        void RefreshRecord()
        {
            if (state == null || IsDisposed || Disposing || record.SelectedIndex < 0) return;
            int slot = record.SelectedIndex, at = slot * 492; var before = state.CopyInitialFleetPool(); var after = state.CopyFleetPool(); var bt = state.InitialBacking.CopyTaskRecord(slot); var task = state.Backing.CopyTaskRecord(slot);
            queue.BeginUpdate(); queue.Items.Clear();
            for (int n = 0; n < 5; n++) queue.Items.Add(n + ": X " + BitConverter.ToInt16(before, at + 40 + n * 2) + " -> " + BitConverter.ToInt16(after, at + 40 + n * 2) + " · Y " + BitConverter.ToInt16(before, at + 50 + n * 2) + " -> " + BitConverter.ToInt16(after, at + 50 + n * 2));
            queue.EndUpdate();
            details.Text = "Viewed record " + slot + " · kind " + BitConverter.ToInt32(after, at) + "\r\nOrigin " + BitConverter.ToInt16(after, at + 12) + "," + BitConverter.ToInt16(after, at + 14) + " (unchanged)\r\nReturn state " + BitConverter.ToInt32(after, at + 472) + " · referenced fleet " + BitConverter.ToInt16(after, at + 488) + "\r\nTask destination " + BitConverter.ToInt32(bt, 8) + "," + BitConverter.ToInt32(bt, 12) + " -> " + BitConverter.ToInt32(task, 8) + "," + BitConverter.ToInt32(task, 12) + "\r\nCached position " + BitConverter.ToInt32(bt, 28) + "," + BitConverter.ToInt32(bt, 32) + " -> " + BitConverter.ToInt32(task, 28) + "," + BitConverter.ToInt32(task, 32) + "\r\nShared navigation activity " + BitConverter.ToInt32(bt, 304) + " -> " + BitConverter.ToInt32(task, 304) +
                "\r\n\r\nGold ring = viewed origin. Cyan cross = supplied return destination; dotted before the pass, solid after. The arrow shows queue intent, not simulated travel. Other map markers are schematic." +
                "\r\n\r\nState5 caches the origin and uses the referenced fleet's position. State6 uses cached coordinates. Both clear X queue slots; later Y slots stay opaque. The numeric UI refresh count does not execute the original renderer." +
                "\r\n\r\nReturn modes, queues, collision flags and diagnostic contexts are supplied fixtures. Inactive-record/all250 presets do not establish naturally valid gameplay states. Active policy, pathfinding, networking, actual clock pacing and the full Practice world remain outside this study. Closing preserves the active battle and settings.";
            canvas.Invalidate();
        }

        sealed class IdleCanvas : Control
        {
            readonly PracticeIdleReturnStudy owner;
            internal IdleCanvas(PracticeIdleReturnStudy owner) { this.owner = owner; DoubleBuffered = true; ResizeRedraw = true; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e); if (owner.state == null || owner.record.SelectedIndex < 0) return;
                var state = owner.state; int slot = owner.record.SelectedIndex, at = slot * 492; var f = state.CopyFleetPool(); int dx, dy; bool destination = state.TryReturnDestination(slot, out dx, out dy);
                double minX = destination ? Math.Min(0, dx - 20) : 0, minY = destination ? Math.Min(0, dy - 20) : 0;
                double maxX = destination ? Math.Max(640, dx + 20) : 640, maxY = destination ? Math.Max(640, dy + 20) : 640;
                float width = Math.Max(1, ClientSize.Width - 76), height = Math.Max(1, ClientSize.Height - 140);
                double scale = Math.Min(width / (maxX - minX), height / (maxY - minY));
                Func<int, int, PointF> point = delegate(int x, int y) { return new PointF(38 + (float)((x - minX) * scale), 66 + (float)((y - minY) * scale)); };
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawString("Copied origins and queued destinations · schematic", owner.Font, Brushes.WhiteSmoke, 16, 20);
                using (var pen = new Pen(Color.FromArgb(35, 65, 85)))
                    for (int n = 0; n <= 640; n += 80) { e.Graphics.DrawLine(pen, point(n, 0), point(n, 640)); e.Graphics.DrawLine(pen, point(0, n), point(640, n)); }
                var corner = point(0, 0); using (var pen = new Pen(Color.SlateGray)) e.Graphics.DrawRectangle(pen, corner.X, corner.Y, (float)(640 * scale), (float)(640 * scale));
                for (int g = 0; g < 250; g++)
                {
                    int ga = g * 492, kind = BitConverter.ToInt32(f, ga); if (kind == 0 && g != slot) continue;
                    var p = point(BitConverter.ToInt16(f, ga + 12), BitConverter.ToInt16(f, ga + 14));
                    using (var brush = new SolidBrush(kind == 0 ? Color.Gray : kind == 1 ? Color.Goldenrod : kind == 2 || kind == 11 ? Color.SeaGreen : Color.SlateBlue)) e.Graphics.FillRectangle(brush, p.X - 3, p.Y - 3, 6, 6);
                }
                var origin = point(BitConverter.ToInt16(f, at + 12), BitConverter.ToInt16(f, at + 14));
                using (var pen = new Pen(Color.Gold, 2)) e.Graphics.DrawEllipse(pen, origin.X - 8, origin.Y - 8, 16, 16);
                e.Graphics.DrawString("Record " + slot, owner.Font, Brushes.Gold, origin.X + 10, origin.Y + 7);
                if (destination)
                {
                    var target = point(dx, dy); using (var pen = new Pen(Color.Cyan, 2)) { if (!state.Executed) pen.DashStyle = DashStyle.Dot; e.Graphics.DrawLine(pen, origin, target); e.Graphics.DrawLine(pen, target.X - 7, target.Y, target.X + 7, target.Y); e.Graphics.DrawLine(pen, target.X, target.Y - 7, target.X, target.Y + 7); }
                    e.Graphics.DrawString((state.Executed ? "Queued " : "Preview ") + dx + "," + dy, owner.Font, Brushes.Cyan, Math.Max(2, target.X - 100), target.Y + 10);
                }
                e.Graphics.DrawString("One numeric pass updates the queue; fleet origins do not move.\r\nGrid marks the source0..640 world; diagnostic goals may lie outside it.", owner.Font, Brushes.LightSteelBlue, 16, Math.Max(50, ClientSize.Height - 58));
            }
        }
    }
}
