using System;
using System.IO;
using System.Security.Cryptography;

namespace FleetLab {
    public sealed class PracticeDestructionTables {
        readonly byte[] points;
        public PracticeDestructionTables(byte[] sourcePoints) {
            if (sourcePoints == null || sourcePoints.Length != 10)
                throw new InvalidDataException("Destruction requires the supported ten-byte unit score table.");
            string hash; using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(sourcePoints)).Replace("-", "");
            if (hash != "539F67BD3B2B7F16023F02BB4BBB2F561C9E618A00BBB3CD30D53A77C2ED72EF")
                throw new InvalidDataException("Unsupported destruction score table identity.");
            points = (byte[])sourcePoints.Clone();
        }
        internal short Points(int type) { return BitConverter.ToInt16(points, type * 2); }
        public static PracticeDestructionTables Load(string programs) {
            return new PracticeDestructionTables(File.ReadAllBytes(Path.Combine(programs, "UnitDestructionPoints.seqbin")));
        }
    }

    public sealed class PracticeDestructionResult {
        readonly byte[] ships, fleets, scores, effects;
        public int Points { get; private set; }
        public int EffectSlot { get; private set; }
        public int MovieCounter { get; private set; }
        public bool FleetRetired { get; private set; }
        internal PracticeDestructionResult(byte[] s, byte[] f, byte[] score, byte[] e,
                int points, int slot, int movieCounter, bool retired) {
            ships = s; fleets = f; scores = score; effects = e;
            Points = points; EffectSlot = slot; MovieCounter = movieCounter; FleetRetired = retired;
        }
        public byte[] CopyShipPool() { return (byte[])ships.Clone(); }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
        public byte[] CopyScoreRegion() { return (byte[])scores.Clone(); }
        public byte[] CopyEffectPool() { return (byte[])effects.Clone(); }
    }

    // Supported RIP B9A8...6A569, complete418BD0 in a bounded data context:
    // unit types1..4, fleet kinds1/9, HP0, mode0/network0/playercount0,
    // local owner-2 and another selected fleet. Mission counters/endings,
    // live UI/media and transport/attached-object callbacks remain excluded.
    // The optional manual study uses this explicit context, without missions.
    public static class PracticeUnitDestruction {
        static int W(byte[] b, int at) { return BitConverter.ToInt16(b, at); }
        static int D(byte[] b, int at) { return BitConverter.ToInt32(b, at); }
        static void I(byte[] b, int at, int n) { BitConverter.GetBytes(n).CopyTo(b, at); }
        static int Reference(byte[] ships, int value) {
            int delta = value - 0x4FC340;
            if (delta < 0 || delta >= 402000 || delta % 268 != 0)
                throw new InvalidDataException("Destruction fleet member is not an original ship-pool reference.");
            int slot = delta / 268;
            if (D(ships, delta) != 0 && W(ships, delta + 4) != slot)
                throw new InvalidDataException("Destruction fleet member identity differs from its pool slot.");
            return delta;
        }
        public static PracticeDestructionResult Resolve(PracticeDestructionTables tables, byte[] visualRecords,
                byte[] shipPool, byte[] fleetPool, byte[] scoreRegion, byte[] effectPool,
                int victimShip, int fleetIndex, bool clearHull, int attackerOwner, int movieCounter) {
            if (tables == null) throw new ArgumentNullException("tables");
            if (shipPool == null || shipPool.Length != 402000 || fleetPool == null || fleetPool.Length != 123000 ||
                scoreRegion == null || scoreRegion.Length != 176 || effectPool == null || effectPool.Length != 800)
                throw new InvalidDataException("Destruction requires complete original ship/fleet/score/effect pools.");
            // Validates and owns the exact table, without stepping existing effects.
            new PracticeVisualPool(effectPool, visualRecords);
            if (victimShip < 0 || victimShip >= 1500 || fleetIndex < 0 || fleetIndex >= 6 ||
                attackerOwner < -1 || attackerOwner > 3 || movieCounter < short.MinValue || movieCounter > short.MaxValue)
                throw new ArgumentOutOfRangeException("victimShip", "Unsupported destruction slot, owner or movie counter.");
            int unit = victimShip * 268, type = D(shipPool, unit), parent = W(shipPool, unit + 12), owner = W(shipPool, unit + 6);
            if (type < 1 || type > 4 || W(shipPool, unit + 16) != 0)
                throw new NotSupportedException("Destruction currently admits only active type1..4 units at zero HP.");
            if (parent < 0 || parent >= 250 || W(shipPool, unit + 4) != victimShip || owner < -1 || owner > 3)
                throw new InvalidDataException("Unsupported destruction identity or owner.");
            int fleet = parent * 492, kind = D(fleetPool, fleet), x = W(shipPool, unit + 8), y = W(shipPool, unit + 10), length = W(shipPool, unit + 14);
            if ((kind != 1 && kind != 9) || W(scoreRegion, 172) != -2)
                throw new NotSupportedException("Destruction requires fleet kind1/9 and the explicit offscreen local-owner context.");
            if (W(fleetPool, fleet + 4) != parent || length < 1 || length > 6 || x < 0 || x >= 10 || y < 0 || y + length > 10 ||
                D(fleetPool, fleet + 16 + fleetIndex * 4) != 0x4FC340 + unit)
                throw new InvalidDataException("Destruction fleet identity, member index or hull bounds differ.");
            for (int n = 0; n < 6; n++) {
                int reference = D(fleetPool, fleet + 16 + n * 4);
                if (reference == 0) continue;
                int member = Reference(shipPool, reference);
                if (n != fleetIndex && member == unit)
                    throw new NotSupportedException("Duplicate victim references require a separate destruction study.");
            }
            byte[] ships = (byte[])shipPool.Clone(), fleets = (byte[])fleetPool.Clone(), scores = (byte[])scoreRegion.Clone(), effects = (byte[])effectPool.Clone();
            short points = tables.Points(type);
            PracticeSegmentDamage.Score(scores, attackerOwner, owner, points);
            if (clearHull) for (int row = 0; row < length; row++) I(fleets, fleet + 64 + (x * 10 + y + row) * 4, 0);
            int effectSlot = -1;
            if (!clearHull) {
                // Source419270 calls420550 before selecting a movie. Types1..4
                // set the counter to4 and consume no CRT random numbers.
                if (W(visualRecords, type * 20) != -1)
                    effects = PracticeMissImpact.SpawnEffect(effects, visualRecords, type, parent, x, y, out effectSlot);
                movieCounter = 4;
            }
            I(ships, unit, 0); I(fleets, fleet + 16 + fleetIndex * 4, 0);
            byte speed = 0;
            for (int n = 0; n < 6; n++) {
                int reference = D(fleets, fleet + 16 + n * 4);
                if (reference == 0) continue;
                int member = Reference(ships, reference);
                if (D(ships, member) == 0) continue;
                short next = (short)W(ships, member + 72);
                if (speed == 0) speed = unchecked((byte)next);
                if (speed > next) speed = unchecked((byte)next);
            }
            BitConverter.GetBytes((short)speed).CopyTo(fleets, fleet + 60);
            // Source only replaces the selection or retires the empty fleet
            // when this was the selected slot. Preserve even stale selections.
            if (fleets[fleet + 468] == fleetIndex) {
                int first;
                for (first = 0; first < 6; first++) {
                    int reference = D(fleets, fleet + 16 + first * 4);
                    if (reference != 0 && D(ships, Reference(ships, reference)) != 0) break;
                }
                if (first < 6) fleets[fleet + 468] = (byte)first;
                else I(fleets, fleet, 0);
            }
            return new PracticeDestructionResult(ships, fleets, scores, effects, points, effectSlot, movieCounter, D(fleets, fleet) == 0);
        }
    }
}
