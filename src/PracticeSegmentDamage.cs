using System;
using System.IO;

namespace FleetLab {
    public sealed class PracticeSegmentDamageResult {
        readonly byte[] ships, fleets, scores;
        public int RemovedEquipment { get; private set; }
        public bool LostHullSegment { get; private set; }
        // Sum of score-call arguments; neutral/self owners can net zero.
        public int Points { get; private set; }
        internal PracticeSegmentDamageResult(byte[] shipPool, byte[] fleetPool, byte[] scoreRegion, int removed, bool lost, int points) {
            ships = shipPool; fleets = fleetPool; scores = scoreRegion;
            RemovedEquipment = removed; LostHullSegment = lost; Points = points;
        }
        public byte[] CopyShipPool() { return (byte[])ships.Clone(); }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
        public byte[] CopyScoreRegion() { return (byte[])scores.Clone(); }
    }

    // RIP B9A8...6A569: complete417E20 nonlethal data path,41A220 score and
    // 42D120 reevaluation. Local-owner mismatch/mode0 exclude actual media and
    // selected-unit refresh. Destruction/attached-object callbacks are guarded.
    // The manual study uses this bounded path; destruction stays guarded.
    public static class PracticeSegmentDamage {
        internal static void Score(byte[] region, int attacker, int victim, short points) {
            if (attacker >= 0) { int at = attacker * 44; BitConverter.GetBytes(unchecked(BitConverter.ToInt32(region, at) + points)).CopyTo(region, at); }
            if (victim >= 0) { int at = victim * 44; BitConverter.GetBytes(unchecked(BitConverter.ToInt32(region, at) - points)).CopyTo(region, at); }
        }
        internal struct Target {
            internal int Unit, Row, HP, CellAt, Cell, Owner;
        }
        internal static Target CheckTarget(PracticeUnitCatalog catalog, byte[] shipPool,
                byte[] fleetPool, byte[] scoreRegion, int victimShip, int x, int y, int weapon, int attackerOwner) {
            if (catalog == null) throw new ArgumentNullException("catalog");
            if (shipPool == null || shipPool.Length != 402000 || fleetPool == null || fleetPool.Length != 123000 || scoreRegion == null || scoreRegion.Length != 176)
                throw new InvalidDataException("Segment damage requires complete ship/fleet pools and the original 176-byte score region.");
            if (victimShip < 0 || victimShip >= 1500) throw new ArgumentOutOfRangeException("victimShip");
            int unit = victimShip * 268, type = BitConverter.ToInt32(shipPool, unit), owner = BitConverter.ToInt16(shipPool, unit + 6);
            int parent = BitConverter.ToInt16(shipPool, unit + 12), length = BitConverter.ToInt16(shipPool, unit + 14), hp = BitConverter.ToInt16(shipPool, unit + 16);
            int row = y - BitConverter.ToInt16(shipPool, unit + 10);
            if (type < 1 || type > 4) throw new NotSupportedException("This unit type has unreconstructed segment callbacks.");
            if (parent < 0 || parent >= 250 || length < 1 || length > 6 || row < 0 || row >= length || x < 0 || x >= 10 || y < 0 || y >= 10 || x != BitConverter.ToInt16(shipPool, unit + 8) ||
                BitConverter.ToInt16(shipPool, unit + 4) != victimShip || BitConverter.ToInt16(fleetPool, parent * 492 + 4) != parent || weapon < 14 || weapon > 30 || attackerOwner < -1 || attackerOwner > 3 || owner < -1 || owner > 3)
                throw new InvalidDataException("Segment damage contains an unsupported identity, hull coordinate, owner or weapon.");
            int cellAt = parent * 492 + 64 + (x * 10 + y) * 4, cell = BitConverter.ToInt32(fleetPool, cellAt);
            if (cell != 2 && cell != 4 && cell != 5 && cell != 6 && cell != 7 && cell != 8)
                throw new NotSupportedException("This target cell is outside the admitted ship-segment states.");
            if (hp < 1 || cell != 8 && hp < 2) throw new NotSupportedException("Lethal segment damage requires unreconstructed destruction callbacks.");
            for (int column = 0; column < 4; column++) {
                int equipment = BitConverter.ToInt32(shipPool, unit + 100 + row * 16 + column * 4);
                if (equipment < 0 || equipment > 34) throw new NotSupportedException("This segment has unreconstructed attached-object equipment.");
            }
            return new Target { Unit = unit, Row = row, HP = hp, CellAt = cellAt, Cell = cell, Owner = owner };
        }
        public static PracticeSegmentDamageResult Apply(PracticeUnitCatalog catalog, byte[] shipPool,
                byte[] fleetPool, byte[] scoreRegion, int victimShip, int x, int y, int weapon, int attackerOwner) {
            Target target = CheckTarget(catalog, shipPool, fleetPool, scoreRegion, victimShip, x, y, weapon, attackerOwner);
            int unit = target.Unit, row = target.Row, hp = target.HP, cellAt = target.CellAt, cell = target.Cell, owner = target.Owner;
            byte[] ships = (byte[])shipPool.Clone(), fleets = (byte[])fleetPool.Clone(), scores = (byte[])scoreRegion.Clone();
            int removed = 0, points = 0;
            for (int column = 0; column < 4; column++) {
                int at = unit + 100 + row * 16 + column * 4, equipment = BitConverter.ToInt32(ships, at);
                if (equipment >= 14 && equipment <= 34) { Score(scores, attackerOwner, owner, 50); points += 50; }
                if (equipment != 0) removed++;
                BitConverter.GetBytes(0).CopyTo(ships, at);
            }
            bool lost = cell != 8;
            if (lost) BitConverter.GetBytes((short)(hp - 1)).CopyTo(ships, unit + 16);
            // Source AX subtraction/multiplication wraps at16 bits before
            //41A220 sign-extends the point value into the32-bit score fields.
            short hullPoints = unchecked((short)((short)(8 - cell) * 25));
            Score(scores, attackerOwner, owner, hullPoints); points += hullPoints;
            BitConverter.GetBytes(8).CopyTo(fleets, cellAt);
            PracticeUnitDepletion.Reevaluate(catalog, ships, unit);
            return new PracticeSegmentDamageResult(ships, fleets, scores, removed, lost, points);
        }
    }
}
