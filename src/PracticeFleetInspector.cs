using System;
using System.IO;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;

namespace FleetLab {
    // Modern diagnostic presentation of reconstructed data, not a mission UI.
    public sealed class PracticeFleetInspector : UserControl {
        sealed class RecordChoice {
            internal string Label;
            internal byte[] Payload;
            internal int Owner;
            internal PracticeFleetSnapshot Snapshot;
            public override string ToString() { return Label; }
        }

        readonly ComboBox records = new ComboBox();
        readonly ListBox units = new ListBox();
        readonly TextBox details = new TextBox();
        readonly Label note = new Label();
        readonly FleetCanvas canvas;
        readonly ClassicMessages names;
        PracticeUnitCatalog catalog;
        string importError;
        bool changing;
        internal PracticeFleetSnapshot CurrentFleet { get; private set; }
        internal int RecordCount { get { return records.Items.Count; } }
        internal int SelectedUnit { get { return units.SelectedIndex; } }
        internal bool CanDeploy { get { return catalog != null; } }
        internal bool CanIncludeMapObjects { get { return catalog != null && catalog.HasMapObjectTables; } }
        internal event Action<int> FleetChanged;

        public PracticeFleetInspector(string programs, ClassicMessages messages) {
            names = messages;
            Width = 370;
            MinimumSize = new Size(300, 260);
            BackColor = Color.FromArgb(12, 23, 35);
            ForeColor = Color.WhiteSmoke;
            Padding = new Padding(8);
            try { catalog = PracticeUnitCatalog.Load(programs); }
            catch (IOException e) { importError = e.Message; }
            catch (UnauthorizedAccessException e) { importError = e.Message; }
            catch (System.Security.SecurityException e) { importError = e.Message; }

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            Controls.Add(layout);
            layout.Controls.Add(new Label { Text = "Recovered fleet records", Dock = DockStyle.Fill, AutoSize = false }, 0, 0);
            records.DropDownStyle = ComboBoxStyle.DropDownList;
            records.Dock = DockStyle.Fill;
            records.AccessibleName = "Fleet record";
            layout.Controls.Add(records, 0, 1);
            units.Dock = DockStyle.Fill;
            units.IntegralHeight = false;
            units.AccessibleName = "Recovered units";
            layout.Controls.Add(units, 0, 2);
            details.Dock = DockStyle.Fill;
            details.Multiline = true;
            details.ReadOnly = true;
            details.ScrollBars = ScrollBars.Vertical;
            details.BorderStyle = BorderStyle.None;
            details.BackColor = BackColor;
            details.ForeColor = ForeColor;
            details.AccessibleName = "Selected unit data";
            layout.Controls.Add(details, 0, 3);
            canvas = new FleetCanvas(this) { Dock = DockStyle.Fill, AccessibleName = "Schematic fleet occupancy" };
            layout.Controls.Add(canvas, 0, 4);
            note.Dock = DockStyle.Fill;
            note.ForeColor = Color.LightSteelBlue;
            note.Text = "Schematic roster grid; world spawning and mission rules are not playable. Embedded entities remain separate work.";
            layout.Controls.Add(note, 0, 5);
            records.SelectedIndexChanged += delegate { LoadRecord(); };
            units.SelectedIndexChanged += delegate { UpdateDetails(); };
        }

        internal void ChooseRecord(int index) {
            if (index < 0 || index >= records.Items.Count) throw new ArgumentOutOfRangeException("index");
            records.SelectedIndex = index;
        }

        internal PracticeDeployment BuildDeployment(Scenario scene, uint seed) {
            if (catalog == null) throw new InvalidDataException("Run Setup again to import the recovered unit tables.");
            return new PracticeDeployment(catalog, scene, seed);
        }
        internal PracticeDeployment BuildFirstPractice(byte[] source, uint seed) {
            return PracticeDeployment.FirstPractice(catalog, source, seed);
        }

        public void SetScenario(Scenario scene) { SetScenario(scene, null); }

        internal void SetScenario(Scenario scene, PracticeDeployment deployment) {
            changing = true;
            records.Items.Clear();
            units.Items.Clear();
            CurrentFleet = null;
            if (deployment != null && deployment.IncludesMapObjects) {
                for (int i = 0; i < deployment.MapObjectCount; i++) {
                    var item = deployment.Fleets[i];
                    records.Items.Add(new RecordChoice {
                        Label = "Map cell " + (item.WorldOrigin.X / 10 + 1) + "," + (item.WorldOrigin.Y / 10 + 1) +
                                " · player " + (item.Owner + 1) + " · type 25",
                        Owner = item.Owner, Snapshot = item
                    });
                }
            }
            if (scene != null) {
                for (int player = 0; player < scene.Groups.Length; player++) {
                    for (int group = 0; group < scene.Groups[player].Length; group++) {
                        for (int record = 0; record < scene.Groups[player][group].Count; record++) {
                            int index = records.Items.Count;
                            if (deployment != null && index >= deployment.Fleets.Count)
                                throw new InvalidDataException("Deployment does not match scenario records.");
                            records.Items.Add(new RecordChoice {
                                Label = "Player " + (player + 1) + " · group " + (group + 1) + " · fleet " + (record + 1),
                                Payload = scene.Groups[player][group][record], Owner = player,
                                Snapshot = deployment == null ? null : deployment.Fleets[index]
                            });
                        }
                    }
                }
            }
            if (deployment != null && deployment.Fleets.Count != records.Items.Count)
                throw new InvalidDataException("Deployment has extra scenario records.");
            note.Text = deployment != null && deployment.IncludesMapObjects ?
                "First Practice startup data, including kind-7/type-25 records. Other terrain buffers, local UI and mission behavior remain unfinished." : deployment == null ?
                "Schematic kind-2 record fixture. Enable seeded deployment to inspect original grouped kinds and world origins. Missions remain unavailable." :
                "Seeded grouped deployment in fresh pools. Earlier startup, embedded entities and mission rules remain unfinished. Terrain is schematic.";
            changing = false;
            if (records.Items.Count != 0) records.SelectedIndex = 0;
            else { details.Text = "No grouped fleet records."; canvas.Invalidate(); }
        }

        string NameOf(int type) {
            return names != null && type >= 0 && type < 13 ? names.ShipName(type) : "Unit type " + type;
        }

        void LoadRecord() {
            if (changing) return;
            units.Items.Clear();
            CurrentFleet = null;
            var choice = records.SelectedItem as RecordChoice;
            if (choice == null) { UpdateDetails(); return; }
            if (catalog == null) {
                details.Text = "Run Setup again to import the recovered unit tables.\n" + importError;
                canvas.Invalidate();
                return;
            }
            try {
                CurrentFleet = choice.Snapshot ?? PracticeFleetSnapshot.Create(catalog, choice.Payload, choice.Owner, Point.Empty, 0);
                for (int i = 0; i < CurrentFleet.Units.Count; i++)
                    units.Items.Add((i + 1) + ". " + NameOf(CurrentFleet.Units[i].Type));
                if (units.Items.Count != 0) units.SelectedIndex = 0;
                else UpdateDetails();
            }
            catch (InvalidDataException e) { details.Text = "Unsupported fleet record: " + e.Message; }
            canvas.Invalidate();
            if (FleetChanged != null) FleetChanged(records.SelectedIndex);
        }

        void UpdateDetails() {
            int index = units.SelectedIndex;
            if (CurrentFleet == null || index < 0 || index >= CurrentFleet.Units.Count) {
                details.Text = CurrentFleet == null ? "Choose a fleet record." : "Empty fleet record.";
                canvas.Invalidate();
                return;
            }
            var unit = CurrentFleet.Units[index];
            var text = new StringBuilder();
            text.Append(NameOf(unit.Type)).Append(" · HP ").Append(unit.HitPoints).Append(" · length ").Append(unit.Length).AppendLine();
            text.Append("Kind ").Append(CurrentFleet.Kind).Append(" · world origin ")
                .Append(CurrentFleet.WorldOrigin.X).Append(',').Append(CurrentFleet.WorldOrigin.Y)
                .Append(" · speed ").Append(unit.Speed).AppendLine();
            text.Append("Local cell ").Append(unit.X + 1).Append(',').Append(unit.Y + 1).AppendLine();
            text.Append("Equipment selection A/B/C: ").Append(unit.SelectedEquipmentA).Append('/')
                .Append(unit.SelectedEquipmentB).Append('/').Append(unit.SelectedEquipmentC).AppendLine();
            text.Append("Ammo (equipment ID:value): ");
            bool any = false;
            for (int weapon = 0; weapon < 35; weapon++) {
                int amount = unit.Ammunition(weapon);
                if (amount == 0) continue;
                if (any) text.Append(", ");
                text.Append(weapon).Append(':').Append(amount);
                any = true;
            }
            if (!any) text.Append("none");
            details.Text = text.ToString();
            details.AccessibleDescription = details.Text;
            canvas.Invalidate();
        }

        void Pick(Point location) {
            if (CurrentFleet == null) return;
            var bounds = canvas.GridBounds;
            if (!bounds.Contains(location)) return;
            float step = bounds.Width / 10;
            int x = (int)((location.X - bounds.X) / step), y = (int)((location.Y - bounds.Y) / step);
            for (int i = 0; i < CurrentFleet.Units.Count; i++) {
                var unit = CurrentFleet.Units[i];
                if (x == unit.X && y >= unit.Y && y < unit.Y + unit.Length) { units.SelectedIndex = i; return; }
            }
        }

        sealed class FleetCanvas : Control {
            readonly PracticeFleetInspector owner;
            internal FleetCanvas(PracticeFleetInspector value) { owner = value; DoubleBuffered = true; ResizeRedraw = true; }
            internal RectangleF GridBounds {
                get { float size = Math.Max(0, Math.Min(Width - 12, Height - 26)); return new RectangleF((Width - size) / 2, 22, size, size); }
            }
            protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) owner.Pick(e.Location); }
            protected override void OnPaint(PaintEventArgs e) {
                base.OnPaint(e);
                var fleet = owner.CurrentFleet;
                if (fleet == null) return;
                var bounds = GridBounds;
                if (bounds.Width < 10) return;
                float step = bounds.Width / 10;
                e.Graphics.DrawString("Fleet occupancy · schematic", SystemFonts.MessageBoxFont, Brushes.LightSteelBlue, 3, 2);
                using (var water = new SolidBrush(Color.FromArgb(23, 49, 67))) e.Graphics.FillRectangle(water, bounds);
                for (int i = 0; i < fleet.Units.Count; i++) {
                    var unit = fleet.Units[i];
                    var hull = new RectangleF(bounds.X + unit.X * step + 2, bounds.Y + unit.Y * step + 2, Math.Max(1, step - 4), Math.Max(1, unit.Length * step - 4));
                    using (var brush = new SolidBrush(i == owner.units.SelectedIndex ? Color.Gold : Color.LightSteelBlue)) e.Graphics.FillRectangle(brush, hull);
                    e.Graphics.DrawString((i + 1).ToString(), SystemFonts.MessageBoxFont, Brushes.Black, hull.X + 3, hull.Y + 3);
                }
                using (var grid = new Pen(Color.FromArgb(63, 97, 115))) {
                    for (int i = 0; i <= 10; i++) {
                        e.Graphics.DrawLine(grid, bounds.X + i * step, bounds.Y, bounds.X + i * step, bounds.Bottom);
                        e.Graphics.DrawLine(grid, bounds.X, bounds.Y + i * step, bounds.Right, bounds.Y + i * step);
                    }
                }
            }
        }
    }
}
