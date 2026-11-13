using System;
using System.Collections.Generic;

namespace FleetLab {
    public enum PracticeDamagePolicy { EquipmentPriority, KnownEquipmentRow, FullSegment }

    // Supported RIP B9A8...6A569:417990 dispatcher,417A60 priority damage,
    // 417C70 known-equipment removal and417E20 segment callback. Nonlethal
    // types1..4/row IDs0..34 only; media/selected-unit UI excluded as before.
    // Original416DD0 handles special projectiles19/21 before this dispatcher.
    public static class PracticeDamage {
        public static PracticeDamagePolicy PolicyForWeapon(int weapon) {
            if (weapon < 14 || weapon > 30) throw new ArgumentOutOfRangeException("weapon");
            if (weapon == 14 || weapon == 17 || weapon == 27) return PracticeDamagePolicy.EquipmentPriority;
            if (weapon == 16 || weapon == 18 || weapon == 19 || weapon == 21 || weapon == 22 || weapon == 24)
                return PracticeDamagePolicy.FullSegment;
            return PracticeDamagePolicy.KnownEquipmentRow;
        }
        static PracticeSegmentDamageResult KnownRow(PracticeDefenseTables tables, PracticeUnitCatalog units,
                byte[] ships, byte[] fleets, byte[] scores, int victim, int x, int y, int weapon, int attacker) {
            // The original B list contains every1..34. For admitted rows0..34,
            // its final data is identical to the complete segment callback:
            // score/clear equipment, update hull/cell, then reevaluate. Its
            // additional path for remaining attached objects stays unsupported.
            var priorities = new HashSet<int>(tables.CopyDamagePriority(false));
            for (int equipment = 1; equipment <= 34; equipment++)
                if (!priorities.Contains(equipment)) throw new NotSupportedException("Incomplete known-equipment damage priority.");
            return PracticeSegmentDamage.Apply(units, ships, fleets, scores, victim, x, y, weapon, attacker);
        }
        public static PracticeSegmentDamageResult Apply(PracticeDefenseTables tables, PracticeUnitCatalog units,
                byte[] shipPool, byte[] fleetPool, byte[] scoreRegion, int victimShip, int x, int y, int weapon, int attackerOwner) {
            if (tables == null) throw new ArgumentNullException("tables");
            var target = PracticeSegmentDamage.CheckTarget(units, shipPool, fleetPool, scoreRegion, victimShip, x, y, weapon, attackerOwner);
            PracticeDamagePolicy policy = PolicyForWeapon(weapon);
            if (policy == PracticeDamagePolicy.FullSegment)
                return PracticeSegmentDamage.Apply(units, shipPool, fleetPool, scoreRegion, victimShip, x, y, weapon, attackerOwner);
            if (policy == PracticeDamagePolicy.KnownEquipmentRow)
                return KnownRow(tables, units, shipPool, fleetPool, scoreRegion, victimShip, x, y, weapon, attackerOwner);
            int selected = 0, column = -1, unit = target.Unit, row = target.Row;
            foreach (int priority in tables.CopyDamagePriority(true)) {
                for (int c = 0; c < 4; c++) if (BitConverter.ToInt32(shipPool, unit + 100 + row * 16 + c * 4) == priority) { selected = priority; column = c; break; }
                if (column >= 0) break;
            }
            if (column < 0) return KnownRow(tables, units, shipPool, fleetPool, scoreRegion, victimShip, x, y, weapon, attackerOwner);
            byte[] ships = (byte[])shipPool.Clone(), fleets = (byte[])fleetPool.Clone(), scores = (byte[])scoreRegion.Clone();
            int points = 0;
            if (selected >= 14 && selected <= 34) { PracticeSegmentDamage.Score(scores, attackerOwner, target.Owner, 50); points += 50; }
            BitConverter.GetBytes(0).CopyTo(ships, unit + 100 + row * 16 + column * 4);
            int ammoAt = unit + 196 + selected * 2, ammo = BitConverter.ToInt16(ships, ammoAt);
            if (ammo > 0) {
                int copies = 0, length = BitConverter.ToInt16(ships, unit + 14);
                for (int at = unit + 100; at < unit + 100 + length * 16; at += 4)
                    if (BitConverter.ToInt32(ships, at) == selected) copies++;
                short remainingAmmo = unchecked((short)(ammo + ammo / (-1 - copies)));
                BitConverter.GetBytes((short)Math.Max(0, (int)remainingAmmo)).CopyTo(ships, ammoAt);
            }
            int remaining = 0;
            for (int c = 0; c < 4; c++) if (BitConverter.ToInt32(ships, unit + 100 + row * 16 + c * 4) != 0) remaining++;
            bool lost = target.Cell != 8 && remaining == 0;
            if (lost) BitConverter.GetBytes((short)(target.HP - 1)).CopyTo(ships, unit + 16);
            short hullPoints = unchecked((short)((short)(8 - target.Cell - remaining) * 25));
            PracticeSegmentDamage.Score(scores, attackerOwner, target.Owner, hullPoints); points += hullPoints;
            BitConverter.GetBytes(8 - remaining).CopyTo(fleets, target.CellAt);
            if (remaining == 0) {
                var segment = PracticeSegmentDamage.Apply(units, ships, fleets, scores, victimShip, x, y, weapon, attackerOwner);
                return new PracticeSegmentDamageResult(segment.CopyShipPool(), segment.CopyFleetPool(), segment.CopyScoreRegion(), 1 + segment.RemovedEquipment, lost || segment.LostHullSegment, points + segment.Points);
            }
            PracticeUnitDepletion.Reevaluate(units, ships, unit);
            return new PracticeSegmentDamageResult(ships, fleets, scores, 1, lost, points);
        }
    }
}
