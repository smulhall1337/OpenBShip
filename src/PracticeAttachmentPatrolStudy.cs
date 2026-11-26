using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FleetLab
{
    public sealed class PracticeAttachmentPatrolStudy : Form
    {
        internal static readonly string[] Profiles = {
            "Stored: timer 4,999 + 1 (activate)", "Stored: timer 4,999 + 0 (hold)",
            "Active: phase 0, empty queue", "Active: phase 2, existing queue",
            "Active: invalid phase 4, existing queue", "Active: state 1 stops the visit",
            "Stored: signed timer overflow (diagnostic)", "Active: WORD overflow (diagnostic)"
        };
        readonly PracticeAttachmentPatrolStudyBasis basis;
        readonly ComboBox profile = new ComboBox(), parent = new ComboBox(), scope = new ComboBox(), child = new ComboBox();
        readonly List<int> viewRecords = new List<int>();
        readonly Button load = new Button(), reset = new Button(), apply = new Button();
        readonly Label summary = new Label(), feedback = new Label();
        readonly ListBox groups = new ListBox(), queue = new ListBox();
        readonly TextBox details = new TextBox();
        readonly PatrolCanvas canvas;
        readonly HashSet<Keys> held = new HashSet<Keys>();
        PracticeAttachmentPatrolStudyState state; bool changing; int appliedProfile;
        internal PracticeAttachmentPatrolStudyState StudyState { get { return state; } }
        int ViewedRecord { get { return child.SelectedIndex < 0 || child.SelectedIndex >= viewRecords.Count ? -1 : viewRecords[child.SelectedIndex]; } }
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
        public PracticeAttachmentPatrolStudy(string programs, PracticeMapStartup startup)
        {
            Name = "attachmentPatrolStudy"; Text = "Carrier patrol · copied original rules";
            StartPosition = FormStartPosition.CenterParent; Size = new Size(1180, 860); MinimumSize = new Size(980, 740);
            Font = new Font("Segoe UI", 9); BackColor = Color.FromArgb(12, 23, 35); ForeColor = Color.WhiteSmoke; KeyPreview = true;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(8) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 385)); Controls.Add(layout);
            canvas = new PatrolCanvas(this) { Name = "patrolCanvas", Dock = DockStyle.Fill }; layout.Controls.Add(canvas, 0, 0);
            var scroll = new Panel { Name = "patrolScroll", Dock = DockStyle.Fill, AutoScroll = true }; layout.Controls.Add(scroll, 1, 0);
            var side = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(4) }; scroll.Controls.Add(side);
            Action<Control, int> add = delegate(Control c, int height)
            {
                c.Width = 350; c.Height = height; c.Margin = new Padding(0, 3, 0, 3);
                if (c is ComboBox || c is Button || c is ListBox) { c.BackColor = Color.FromArgb(28, 55, 76); c.ForeColor = ForeColor; }
                var button = c as Button;
                if (button != null) { button.UseVisualStyleBackColor = false; button.FlatStyle = FlatStyle.Flat; button.EnabledChanged += delegate { if (button.IsDisposed || button.Disposing) return; button.BackColor = button.Enabled ? Color.FromArgb(28, 55, 76) : Color.LightGray; button.ForeColor = button.Enabled ? ForeColor : Color.Black; }; }
                side.Controls.Add(c);
            };
            add(new Label { Text = "Choose one carrier visit or a whole 250-record pass on a fixed copied map. Aircraft activation and queued targets are numeric results; origins stay fixed. Targets are hidden in this fixture. UI refreshes are counted data; no running world clock." }, 94);
            add(new Label { Text = "Pending fixture / carrier / scope (apply with Load)" }, 20);
            profile.Name = "patrolProfile"; profile.DropDownStyle = ComboBoxStyle.DropDownList; profile.DropDownWidth = 470; profile.Items.AddRange(Profiles); profile.SelectedIndex = 0; add(profile, 30);
            parent.Name = "patrolParent"; parent.DropDownStyle = ComboBoxStyle.DropDownList; parent.Items.AddRange(new object[] { "Fleet 15 · unit 27 · four attached groups", "Fleet 17 · unit 32 · two attached groups" }); parent.SelectedIndex = 0; add(parent, 30);
            scope.Name = "patrolScope"; scope.DropDownStyle = ComboBoxStyle.DropDownList; scope.DropDownWidth = 470; scope.Items.AddRange(new object[] { "One carrier visit (leaf comparison)", "Whole pass · no idle returns", "Whole pass · fleet 16 idle return", "Whole pass · fleet 16 + record 249 returns" }); scope.SelectedIndex = 0; add(scope, 30);
            load.Name = "patrolLoad"; load.Text = "Load selected patrol copy"; add(load, 34);
            reset.Name = "patrolReset"; reset.Text = "Reset applied copy before visit"; add(reset, 34);
            apply.Name = "patrolApply"; apply.Text = "Apply one carrier policy visit"; add(apply, 40);
            summary.Name = "patrolSummary"; add(summary, 148); feedback.Name = "patrolFeedback"; add(feedback, 64);
            add(new Label { Text = "Attached groups: kind / timer / phase / target" }, 20);
            groups.Name = "patrolGroups"; groups.HorizontalScrollbar = true; add(groups, 90);
            add(new Label { Text = "View group / supplied idle record (selection only)" }, 20);
            child.Name = "patrolChild"; child.DropDownStyle = ComboBoxStyle.DropDownList; add(child, 30);
            queue.Name = "patrolQueue"; queue.HorizontalScrollbar = true; add(queue, 100);
            details.Name = "patrolDetails"; details.Multiline = true; details.ReadOnly = true; details.ScrollBars = ScrollBars.Vertical; details.BackColor = BackColor; details.ForeColor = ForeColor; add(details, 310);
            var close = new Button { Name = "patrolClose", Text = "Close" }; add(close, 34); close.Click += delegate { Close(); };
            profile.SelectedIndexChanged += delegate { Pending(); }; parent.SelectedIndexChanged += delegate { Pending(); }; scope.SelectedIndexChanged += delegate { Pending(); };
            child.SelectedIndexChanged += delegate { if (!changing) RefreshChild(); };
            load.Click += delegate { LoadCopy(); };
            reset.Click += delegate { if (state != null) { state = state.Reset(); Feedback("Applied copy restored. Pending selections remain pending."); Sync(false); } };
            apply.Click += delegate
            {
                if (state == null) return;
                try { state = state.Execute(); Feedback("Visit complete. Activation and queued intent are shown; origins have not moved. Reset to repeat."); Sync(false); }
                catch (InvalidOperationException e) { Reject(e.Message); } catch (NotSupportedException e) { Reject(e.Message); } catch (InvalidDataException e) { Reject(e.Message); }
            };
            KeyUp += delegate(object sender, KeyEventArgs e) { held.Remove(e.KeyCode); };
            Deactivate += delegate { foreach (Keys key in new[] { Keys.Enter, Keys.Space, Keys.Escape }) if ((GetAsyncKeyState((int)key) & 0x8000) != 0) held.Add(key); };
            Activated += delegate { foreach (Keys key in new[] { Keys.Enter, Keys.Space, Keys.Escape }) { if ((GetAsyncKeyState((int)key) & 0x8000) != 0) held.Add(key); else held.Remove(key); } };
            try { basis = PracticeAttachmentPatrolStudyBasis.Create(programs, startup); LoadCopy(); }
            catch (ArgumentException e) { Reject(e.Message); } catch (NotSupportedException e) { Reject(e.Message); } catch (IOException e) { Reject(e.Message); } catch (UnauthorizedAccessException e) { Reject(e.Message); }
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
        void Feedback(string text) { if (IsDisposed || Disposing) return; feedback.ForeColor = Color.LightSteelBlue; feedback.Text = text; }
        void Reject(string text) { if (IsDisposed || Disposing) return; feedback.ForeColor = Color.Orange; feedback.Text = "Rejected; applied copy preserved. " + text; Sync(false); }
        void Pending() { if (!changing && state != null) Feedback("Selections pending. Load applies them; Reset keeps the applied fixture."); }
        void LoadCopy()
        {
            if (basis == null) { Reject("No supported copied map basis."); return; }
            int p = profile.SelectedIndex; bool active = p >= 2 && p <= 5 || p == 7;
            var next = basis.CreateFixture(parent.SelectedIndex == 0 ? 15 : 17, active, p == 6 ? int.MaxValue : active ? 1234 : 4999,
                p == 1 ? 0 : active ? 40 : 1, p == 3 || p == 5 ? 2 : p == 4 ? 4 : p == 7 ? 3 : 0,
                p == 3 || p == 4, p == 7 ? 2 : 0, false, p == 5);
            if (scope.SelectedIndex > 0) next = next.ForWholePass(scope.SelectedIndex - 1);
            state = next; appliedProfile = p; Feedback("Copied carrier and supplied policy inputs loaded. Apply one visit to compare."); Sync(true);
        }
        void Sync(bool choose)
        {
            if (IsDisposed || Disposing) return; apply.Enabled = reset.Enabled = child.Enabled = state != null;
            if (state == null) { summary.Text = "No admitted patrol copy."; load.Enabled = false; return; }
            apply.Enabled = !state.Executed;
            apply.Text = state.WholePass ? "Apply whole 250-record pass" : "Apply one carrier policy visit";
            summary.Text = "Applied: " + Profiles[appliedProfile] + "\r\nParent fleet " + state.ParentFleet + " · visit " + (state.Executed ? "complete" : "prepared") +
                "\r\nScope " + (state.WholePass ? "whole pass · " + state.Visits + " records · " + state.PolicyVisits + " policies" : "one carrier helper") + " · idle " + state.IdleReturns +
                "\r\nActivated " + state.ActivatedAircraft.Count + " · member visits " + state.VisitedAircraft.Count +
                "\r\nUI requests " + state.RouteUiRequests + " · selection 4 -> " + state.SelectedWaypoint +
                "\r\nEarly stop at state 1: " + (state.StoppedAtStateOne < 0 ? "none" : "group " + state.StoppedAtStateOne) + "\r\nSupplied delta " + state.RawDelta + "; world clock unchanged.";
            var f = state.CopyFleetPool(); groups.Items.Clear();
            foreach (int id in state.Children) { var task = state.Backing.CopyTaskRecord(id); groups.Items.Add(id + ": kind " + BitConverter.ToInt32(f, id * 492) + " / t " + BitConverter.ToInt32(task, 252) + " / p " + BitConverter.ToInt32(task, 200) + " / " + BitConverter.ToInt16(f, id * 492 + 40) + "," + BitConverter.ToInt16(f, id * 492 + 50)); }
            changing = true;
            int expected = state.Children.Count + (state.WholePass ? state.IdleProfile : 0);
            if (choose || child.Items.Count != expected)
            {
                child.Items.Clear(); viewRecords.Clear();
                foreach (int id in state.Children) { child.Items.Add("Attached group " + id); viewRecords.Add(id); }
                if (state.WholePass && state.IdleProfile > 0) { child.Items.Add("Supplied idle fleet 16"); viewRecords.Add(16); }
                if (state.WholePass && state.IdleProfile > 1) { child.Items.Add("Supplied inactive record 249"); viewRecords.Add(249); }
                child.SelectedIndex = child.Items.Count - 1;
            }
            changing = false; RefreshChild();
        }
        void RefreshChild()
        {
            if (state == null || IsDisposed || Disposing || child.SelectedIndex < 0) return;
            int id = ViewedRecord, at = id * 492; var initial = state.Reset(); var before = initial.CopyFleetPool(); var after = state.CopyFleetPool(); var bt = initial.Backing.CopyTaskRecord(id); var t = state.Backing.CopyTaskRecord(id);
            queue.Items.Clear(); for (int n = 0; n < 5; n++) queue.Items.Add(n + ": X " + BitConverter.ToInt16(before, at + 40 + n * 2) + " -> " + BitConverter.ToInt16(after, at + 40 + n * 2) + " · Y " + BitConverter.ToInt16(before, at + 50 + n * 2) + " -> " + BitConverter.ToInt16(after, at + 50 + n * 2));
            details.Text = (id == 16 || id == 249 ? "Viewed idle record " : "Viewed group ") + id + " · kind " + BitConverter.ToInt32(before, at) + " -> " + BitConverter.ToInt32(after, at) +
                "\r\nOrigin " + BitConverter.ToInt16(after, at + 12) + "," + BitConverter.ToInt16(after, at + 14) + " (unchanged)" +
                "\r\nTimer " + BitConverter.ToInt32(bt, 252) + " -> " + BitConverter.ToInt32(t, 252) + "\r\nPhase " + BitConverter.ToInt32(bt, 200) + " -> " + BitConverter.ToInt32(t, 200) +
                "\r\nFirst member state " + BitConverter.ToInt32(bt, 212) + " -> " + BitConverter.ToInt32(t, 212) +
                "\r\nNavigation activity " + BitConverter.ToInt32(bt, 304) + " -> " + BitConverter.ToInt32(t, 304) +
                "\r\n\r\nGold ring = carrier origin. Purple ring = viewed record origin. Cyan cross/line = queued target after the visit, not travel. Source coordinates increase right/down. All markers are schematic." +
                "\r\n\r\nStored groups activate at signed timer >= 5,000. Attachments visit from slot 3 down to 0. Empty-target callbacks clear six member states. State 1 clears timer/phase and stops later attachment visits. Valid phases keep a queued first target; invalid phases replace it." +
                "\r\n\r\nWhole-pass scope visits all 250 records in source order: policy, then idle return, then navigation. Newly activated flagged aircraft are visited later in that pass. Supplied idle records clear activity before navigation; they do not move. The world clock stays fixed." +
                "\r\n\r\nThe copied BSSB202 seed731 basis is original-derived. Visibility, collision flags, timers, phases, returns and diagnostic coordinates are supplied fixtures. No moving aircraft, visible-target combat, source renderer, flag2/3 policy, pathfinding or complete Practice world runs. Closing preserves the active battle and settings.";
            canvas.Invalidate();
        }
        sealed class PatrolCanvas : Control
        {
            readonly PracticeAttachmentPatrolStudy owner;
            internal PatrolCanvas(PracticeAttachmentPatrolStudy owner) { this.owner = owner; DoubleBuffered = true; ResizeRedraw = true; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e); if (owner.state == null) return; var s = owner.state; var f = s.CopyFleetPool();
                float scale = Math.Min(Math.Max(1, ClientSize.Width - 76) / 640f, Math.Max(1, ClientSize.Height - 160) / 640f);
                Func<int, int, PointF> point = delegate(int x, int y) { return new PointF(38 + x * scale, 68 + y * scale); };
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.DrawString("Copied carrier and patrol targets · schematic", owner.Font, Brushes.WhiteSmoke, 16, 20);
                using (var pen = new Pen(Color.FromArgb(35, 65, 85))) for (int n = 0; n <= 640; n += 80) { e.Graphics.DrawLine(pen, point(n, 0), point(n, 640)); e.Graphics.DrawLine(pen, point(0, n), point(640, n)); }
                var corner = point(0, 0); using (var pen = new Pen(Color.SlateGray)) e.Graphics.DrawRectangle(pen, corner.X, corner.Y, 640 * scale, 640 * scale);
                int pa = s.ParentFleet * 492, px = BitConverter.ToInt16(f, pa + 12), py = BitConverter.ToInt16(f, pa + 14); bool parentInside = px >= 0 && px <= 640 && py >= 0 && py <= 640;
                if (parentInside) { var p = point(px, py); using (var pen = new Pen(Color.Gold, 2)) e.Graphics.DrawEllipse(pen, p.X - 9, p.Y - 9, 18, 18); e.Graphics.DrawString("Carrier " + s.ParentFleet, owner.Font, Brushes.Gold, p.X + 12, p.Y - 20); }
                else e.Graphics.DrawString("Diagnostic carrier outside world: " + px + "," + py, owner.Font, Brushes.Orange, 16, 43);
                int selected = owner.ViewedRecord;
                foreach (int id in s.Children)
                {
                    int at = id * 492, x = BitConverter.ToInt16(f, at + 12), y = BitConverter.ToInt16(f, at + 14), tx = BitConverter.ToInt16(f, at + 40), ty = BitConverter.ToInt16(f, at + 50);
                    bool originInside = x >= 0 && x <= 640 && y >= 0 && y <= 640;
                    if (originInside) { var p = point(x, y); using (var pen = new Pen(id == selected ? Color.MediumPurple : Color.SlateBlue, id == selected ? 3 : 1)) e.Graphics.DrawEllipse(pen, p.X - 6, p.Y - 6, 12, 12); }
                    if (!s.Executed || tx < 0 || tx > 640 || ty < 0 || ty > 640) continue;
                    var target = point(tx, ty); using (var pen = new Pen(id == selected ? Color.Cyan : Color.Teal, id == selected ? 2 : 1)) { if (originInside) e.Graphics.DrawLine(pen, point(x, y), target); e.Graphics.DrawLine(pen, target.X - 5, target.Y, target.X + 5, target.Y); e.Graphics.DrawLine(pen, target.X, target.Y - 5, target.X, target.Y + 5); }
                    if (id == selected)
                    {
                        string label = id + ": " + tx + "," + ty;
                        float width = e.Graphics.MeasureString(label, owner.Font).Width;
                        e.Graphics.DrawString(label, owner.Font, Brushes.Cyan, Math.Max(2, target.X - width - 8), target.Y + 8);
                    }
                }
                if (selected == 16 || selected == 249)
                {
                    int at = selected * 492, x = BitConverter.ToInt16(f, at + 12), y = BitConverter.ToInt16(f, at + 14), tx = BitConverter.ToInt16(f, at + 40), ty = BitConverter.ToInt16(f, at + 50); var origin = point(x, y);
                    using (var pen = new Pen(Color.MediumPurple, 3)) e.Graphics.DrawEllipse(pen, origin.X - 7, origin.Y - 7, 14, 14);
                    e.Graphics.DrawString("Idle " + selected, owner.Font, Brushes.MediumPurple, origin.X + 9, origin.Y + 9);
                    if (s.Executed && tx >= 0 && tx <= 640 && ty >= 0 && ty <= 640)
                    {
                        var goal = point(tx, ty); using (var pen = new Pen(Color.Cyan, 2)) { e.Graphics.DrawLine(pen, origin, goal); e.Graphics.DrawLine(pen, goal.X - 5, goal.Y, goal.X + 5, goal.Y); e.Graphics.DrawLine(pen, goal.X, goal.Y - 5, goal.X, goal.Y + 5); }
                        e.Graphics.DrawString("Queued " + tx + "," + ty, owner.Font, Brushes.Cyan, Math.Max(2, goal.X - 100), goal.Y + 9);
                    }
                }
                e.Graphics.DrawString("One numeric visit changes activation and queues; origins stay fixed.\r\nUse the group selector and scrollable details to inspect each result.", owner.Font, Brushes.LightSteelBlue, 16, Math.Max(70, ClientSize.Height - 58));
            }
        }
    }
}
