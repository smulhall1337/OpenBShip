using System;
using System.IO;
using System.Drawing;

namespace FleetLab {
    public enum PracticeMovementOutcome {
        NoWaypoint, Moved, BlockedByFleet, BlockedByTerrain, ReachedWaypoint, ReachedLockedWaypoint
    }

    public sealed class PracticeMovementUpdate {
        public PracticeMovementState State { get; private set; }
        public PracticeMovementOutcome Outcome { get; private set; }
        public Point Before { get; private set; }
        public Point After { get; private set; }
        public int CollisionCode { get; private set; }
        public bool QueueConsumed { get; private set; }
        internal PracticeMovementUpdate(PracticeMovementState state, PracticeMovementOutcome outcome,
                                        Point before, Point after, int collision, bool consumed) {
            State = state; Outcome = outcome; Before = before; After = after;
            CollisionCode = collision; QueueConsumed = consumed;
        }
    }

    // One admitted kind1 fleet update, not an original frame or scheduler.
    // B9A8...6A569:482 reset at4276A9;427CC7..42822B movement/arrival.
    // Network0,state472=0,flags485=2/3,speed1..7. Kind12 interaction is
    // deliberately unsupported. Inputs/outputs are owned immutable snapshots.
    public sealed class PracticeMovementState {
        public const int FleetSlots = 250, FleetBytes = 492;
        public const int TerrainBytes = 500 * FleetBytes, AuxiliaryBytes = 0x13200;
        readonly byte[] fleets, terrain, auxiliary;
        public int SelectedFleet { get; private set; }
        public int SelectedWaypoint { get; private set; }

        public PracticeMovementState(byte[] fleetPool, byte[] terrainPool, byte[] auxiliaryData,
                                     int selectedFleet, int selectedWaypoint) {
            if (fleetPool == null || fleetPool.Length != FleetSlots * FleetBytes ||
                terrainPool == null || terrainPool.Length != TerrainBytes ||
                auxiliaryData == null || auxiliaryData.Length != AuxiliaryBytes)
                throw new InvalidDataException("Movement requires complete fleet, terrain and auxiliary pools.");
            Slot(selectedFleet);
            if (selectedWaypoint < -1 || selectedWaypoint > 4)
                throw new ArgumentOutOfRangeException("selectedWaypoint");
            fleets = (byte[])fleetPool.Clone();
            terrain = (byte[])terrainPool.Clone();
            auxiliary = (byte[])auxiliaryData.Clone();
            for (int i = 0; i < FleetSlots; i++)
                if (Short(fleets, i * FleetBytes + 4) != i)
                    throw new InvalidDataException("Movement fleet IDs must match their pool slots.");
            SelectedFleet = selectedFleet; SelectedWaypoint = selectedWaypoint;
        }

        public static PracticeMovementState ForStudy(PracticeDeployment deployment, int selectedFleet) {
            if (deployment == null || !deployment.IncludesMapObjects || deployment.Terrain == null)
                throw new InvalidDataException("Movement study requires the first Practice startup.");
            // Explicit study context: no application/UI initialization or AI.
            return new PracticeMovementState(deployment.CopyFleetPool(), deployment.Terrain.CopyRecords(),
                new byte[AuxiliaryBytes], selectedFleet, -1);
        }

        static void Slot(int slot) { if (slot < 0 || slot >= FleetSlots) throw new ArgumentOutOfRangeException("slot"); }
        static int Short(byte[] data, int at) { return BitConverter.ToInt16(data, at); }
        static void Short(byte[] data, int at, int value) { BitConverter.GetBytes((short)value).CopyTo(data, at); }
        static int Integer(byte[] data, int at) { return BitConverter.ToInt32(data, at); }
        static bool Overlaps(int x, int y, int otherX, int otherY) {
            return otherX + 10 > x && x + 10 > otherX && otherY + 10 > y && y + 10 > otherY;
        }
        static bool Collidable(int kind) { return kind == 1 || kind == 2 || kind == 10 || kind == 11 || kind == 12; }
        static int Component(int delta, float denominator) {
            // Source x87CW027F plus explicit single-precision stores. Preserve
            // signed half adjustment, truncation and final overshoot clamp.
            float value = (float)(delta / (double)denominator);
            if (value > 0) value = (float)(value + .5);
            if (value < 0) value = (float)(value - .5);
            if (Math.Abs((double)value) > Math.Abs(delta)) value = delta;
            return (int)value;
        }
        public Point Origin(int slot) {
            Slot(slot); return new Point(Short(fleets, slot * FleetBytes + 12), Short(fleets, slot * FleetBytes + 14));
        }
        public byte[] CopyFleetRecord(int slot) {
            Slot(slot); var record = new byte[FleetBytes]; Array.Copy(fleets, slot * FleetBytes, record, 0, record.Length); return record;
        }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
        public byte[] CopyTerrainPool() { return (byte[])terrain.Clone(); }
        public byte[] CopyAuxiliary() { return (byte[])auxiliary.Clone(); }

        public PracticeMovementState WithFleetRecord(int slot, byte[] record, int selectedWaypoint) {
            Slot(slot);
            if (record == null || record.Length != FleetBytes || Short(record, 4) != slot)
                throw new InvalidDataException("Movement queue record has the wrong slot or length.");
            byte[] next = (byte[])fleets.Clone(); Array.Copy(record, 0, next, slot * FleetBytes, FleetBytes);
            return new PracticeMovementState(next, terrain, auxiliary, slot, selectedWaypoint);
        }

        public PracticeMovementUpdate Step(int slot) {
            Slot(slot); int at = slot * FleetBytes, flag = fleets[at + 485];
            if (Integer(fleets, at) != 1 || Integer(fleets, at + 472) != 0 || (flag != 2 && flag != 3))
                throw new NotSupportedException("This fleet mode is outside the recovered movement subset.");
            Point before = Origin(slot);
            if (before.X < 0 || before.X >= 640 || before.Y < 0 || before.Y >= 640)
                throw new InvalidDataException("Movement origin is outside the world.");
            byte[] next = (byte[])fleets.Clone(), aux = (byte[])auxiliary.Clone();
            next[at + 482] = 255; // Admitted original per-fleet preamble.
            int targetX = Short(next, at + 40), targetY = Short(next, at + 50);
            if (targetX == -1)
                return Finish(next, aux, SelectedWaypoint, PracticeMovementOutcome.NoWaypoint, before, before, 255, false);
            int speed = Short(next, at + 60);
            if (speed < 1 || speed > 7)
                throw new NotSupportedException("This fleet speed is outside the verified movement range.");
            for (int i = 0; i < 5; i++) {
                int x = Short(next, at + 40 + i * 2), y = Short(next, at + 50 + i * 2);
                if (x != -1 && (x < 0 || x >= 640 || y < 0 || y >= 640))
                    throw new InvalidDataException("Movement waypoint is outside the world.");
            }
            int dx = targetX - before.X, dy = targetY - before.Y;
            int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
            float denominator = (float)(distance / (speed * (flag == 3 ? 1 : .5)));
            int px = before.X, py = before.Y, collision = 255;
            if (denominator != 0) {
                px += Component(dx, denominator); py += Component(dy, denominator);
                for (int other = 0; other < FleetSlots; other++) {
                    if (other == slot) continue;
                    int offset = other * FleetBytes, kind = Integer(next, offset);
                    if (!Collidable(kind) || !Overlaps(px, py, Short(next, offset + 12), Short(next, offset + 14))) continue;
                    if (kind == 12)
                        throw new NotSupportedException("Kind 12 interactions are not reconstructed; movement was preserved.");
                    collision = other; px = before.X; py = before.Y; break;
                }
                for (int other = 0; other < 500; other++) {
                    int offset = other * FleetBytes;
                    if (Integer(terrain, offset) != 1 ||
                        !Overlaps(px, py, Short(terrain, offset + 12), Short(terrain, offset + 14))) continue;
                    collision = 254; px = before.X; py = before.Y; break;
                }
                next[at + 482] = (byte)collision;
                Short(next, at + 12, px); Short(next, at + 14, py);
                next[at + 62] = (byte)(Math.Abs(dy) < Math.Abs(dx) ? (dx > 0 ? 1 : 3) : (dy > 0 ? 2 : 0));
            }
            bool arrived = px == targetX && py == targetY, consumed = false;
            int selectedPoint = SelectedWaypoint;
            if (arrived && Integer(aux, 0x50 + slot * 312) != 1) {
                // The source clears the SELECTED fleet's fields, which may
                // differ from the arriving fleet. Preserve that observed rule.
                BitConverter.GetBytes(0).CopyTo(aux, 0x50 + SelectedFleet * 312);
                aux[0x3c + SelectedFleet * 312] = 0;
                for (int i = 0; i < 4; i++) {
                    Short(next, at + 40 + i * 2, Short(next, at + 42 + i * 2));
                    Short(next, at + 50 + i * 2, Short(next, at + 52 + i * 2));
                }
                Short(next, at + 48, -1);
                if (SelectedFleet == slot && selectedPoint != -1) selectedPoint--;
                consumed = true;
            }
            var outcome = arrived ? (consumed ? PracticeMovementOutcome.ReachedWaypoint : PracticeMovementOutcome.ReachedLockedWaypoint) :
                collision == 254 ? PracticeMovementOutcome.BlockedByTerrain :
                collision != 255 ? PracticeMovementOutcome.BlockedByFleet : PracticeMovementOutcome.Moved;
            return Finish(next, aux, selectedPoint, outcome, before, new Point(px, py), collision, consumed);
        }

        PracticeMovementUpdate Finish(byte[] next, byte[] aux, int selectedPoint, PracticeMovementOutcome outcome,
                                       Point before, Point after, int collision, bool consumed) {
            var state = new PracticeMovementState(next, terrain, aux, SelectedFleet, selectedPoint);
            return new PracticeMovementUpdate(state, outcome, before, after, collision, consumed);
        }
    }
}
