using System;
using System.IO;
using System.Drawing;

namespace FleetLab {
    public enum PracticeWaypointResult { Added, Selected, TerrainBlocked, Full, SelectionActive }

    // Supported Bs.exe B9A8...6A569:41377C..4138BA append/duplicate branch.
    // A copied fleet record only. Dragging, deletion, movement, network and UI
    // lifecycle are separate systems; this class never updates a deployment.
    public sealed class PracticeWaypointQueue {
        readonly byte[] initial, record, kinds = new byte[4096];
        readonly int sourceType, kind;
        public int Selected { get; private set; }
        public Point Origin { get { return new Point(Short(12), Short(14)); } }
        public int Count { get { int n = 0; for (int i = 0; i < 5; i++) if (HasWaypoint(i)) n++; return n; } }

        public PracticeWaypointQueue(byte[] fleetRecord, Scenario scene, int originalType, int selectedWaypoint = -1) {
            if (fleetRecord == null || fleetRecord.Length != 492)
                throw new InvalidDataException("A waypoint study requires one 492-byte fleet record.");
            if (scene == null || scene.Cells == null || scene.Cells.Length != 4096)
                throw new InvalidDataException("A waypoint study requires a 64 by 64 map.");
            if (originalType != 0 && originalType != 1)
                throw new ArgumentOutOfRangeException("originalType");
            if (selectedWaypoint < -1 || selectedWaypoint > 4) throw new ArgumentOutOfRangeException("selectedWaypoint");
            record = (byte[])fleetRecord.Clone();
            kind = ValidateRecord(record);
            for (int i = 0; i < kinds.Length; i++) {
                if (scene.Cells[i] == null) throw new InvalidDataException("Missing waypoint terrain cell.");
                kinds[i] = scene.Cells[i].Kind;
            }
            sourceType = originalType;
            initial = (byte[])record.Clone();
            Selected = selectedWaypoint;
        }

        int Short(int at) { return BitConverter.ToInt16(record, at); }
        void Short(int at, int value) { BitConverter.GetBytes((short)value).CopyTo(record, at); }
        static void Index(int index) { if (index < 0 || index >= 5) throw new ArgumentOutOfRangeException("index"); }
        public bool HasWaypoint(int index) { Index(index); return Short(40 + index * 2) != -1; }
        public Point Waypoint(int index) {
            Index(index);
            if (!HasWaypoint(index)) throw new InvalidOperationException("This waypoint slot is empty.");
            return new Point(Short(40 + index * 2), Short(50 + index * 2));
        }
        public byte[] CopyRecord() { return (byte[])record.Clone(); }
        static int ValidateRecord(byte[] data) {
            int type = BitConverter.ToInt32(data, 0);
            if (type != 1 && type != 2 && type != 9 && type != 11)
                throw new InvalidDataException("Unsupported waypoint-study fleet kind.");
            int ox = BitConverter.ToInt16(data, 12), oy = BitConverter.ToInt16(data, 14);
            if (ox < 0 || ox >= 640 || oy < 0 || oy >= 640)
                throw new InvalidDataException("Fleet origin is outside the world.");
            for (int i = 0; i < 5; i++) {
                int x = BitConverter.ToInt16(data, 40 + i * 2), y = BitConverter.ToInt16(data, 50 + i * 2);
                if (x != -1 && (x < 0 || x >= 640 || y < 0 || y >= 640))
                    throw new InvalidDataException("Waypoint is outside the world.");
            }
            return type;
        }
        internal void ApplyMovementRecord(byte[] data, int selectedWaypoint) {
            if (data == null || data.Length != 492 || selectedWaypoint < -1 || selectedWaypoint > 4)
                throw new InvalidDataException("Invalid movement queue update.");
            data = (byte[])data.Clone();
            if (ValidateRecord(data) != kind || BitConverter.ToInt16(data, 4) != Short(4))
                throw new InvalidDataException("Movement queue changed fleet identity.");
            Array.Copy(data, record, record.Length);
            Selected = selectedWaypoint;
        }

        public PracticeWaypointResult AppendOrSelect(Point world) {
            if (world.X < 0 || world.X >= 640 || world.Y < 0 || world.Y >= 640)
                throw new ArgumentOutOfRangeException("world");
            if (Selected != -1) return PracticeWaypointResult.SelectionActive;
            var point = new Point(world.X / 10 * 10, world.Y / 10 * 10);
            for (int i = 0; i < 5; i++) if (HasWaypoint(i) && Waypoint(i) == point) {
                Selected = i;
                return PracticeWaypointResult.Selected;
            }
            if (sourceType != 0 && (kind == 1 || kind == 9) && kinds[world.Y / 10 * 64 + world.X / 10] == 2)
                return PracticeWaypointResult.TerrainBlocked;
            for (int i = 0; i < 5; i++) if (!HasWaypoint(i)) {
                Short(40 + i * 2, point.X);
                Short(50 + i * 2, point.Y);
                return PracticeWaypointResult.Added;
            }
            return PracticeWaypointResult.Full;
        }

        // Source428960 clears X sentinels and current selection, preserving Y.
        public void Clear() { for (int i = 0; i < 5; i++) Short(40 + i * 2, -1); Selected = -1; }
        // Explicit modern study controls; not reconstructed original gestures.
        public void Deselect() { Selected = -1; }
        public void Reset() { Array.Copy(initial, record, record.Length); Selected = -1; }
    }
}

