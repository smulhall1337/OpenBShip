using System;
using System.IO;

namespace FleetLab {
    public sealed class PracticeDestructiveDamageResult {
        public PracticeSegmentDamageResult Damage { get; private set; }
        readonly byte[] effects;
        public bool Destroyed { get; private set; }
        public bool FleetRetired { get; private set; }
        public int EffectSlot { get; private set; }
        public int MovieCounter { get; private set; }
        internal PracticeDestructiveDamageResult(PracticeSegmentDamageResult damage, PracticeDamageContinuation continuation) {
            Damage = damage; effects = (byte[])continuation.Effects.Clone(); Destroyed = continuation.Destroyed;
            FleetRetired = continuation.FleetRetired; EffectSlot = continuation.EffectSlot; MovieCounter = continuation.MovieCounter;
        }
        public byte[] CopyEffectPool() { return (byte[])effects.Clone(); }
    }

    // Private per-call continuation: no partial result is published on failure.
    internal sealed class PracticeDamageContinuation {
        readonly PracticeDestructionTables tables;
        readonly byte[] visuals;
        readonly int index;
        internal byte[] Effects;
        internal int MovieCounter, EffectSlot = -1;
        internal bool Destroyed, FleetRetired;
        internal PracticeDamageContinuation(PracticeDestructionTables sourceTables, byte[] visualRecords, byte[] effectPool, int fleetIndex, int movieCounter) {
            tables = sourceTables; visuals = (byte[])visualRecords.Clone(); Effects = (byte[])effectPool.Clone(); index = fleetIndex; MovieCounter = movieCounter;
        }
        internal PracticeSegmentDamageResult Destroy(byte[] ships, byte[] fleets, byte[] scores, int victim, int attacker, int removed, bool lost, int points) {
            var result = PracticeUnitDestruction.Resolve(tables, visuals, ships, fleets, scores, Effects, victim, index, false, attacker, MovieCounter);
            Effects = result.CopyEffectPool(); MovieCounter = result.MovieCounter; EffectSlot = result.EffectSlot;
            Destroyed = true; FleetRetired = result.FleetRetired;
            return new PracticeSegmentDamageResult(result.CopyShipPool(), result.CopyFleetPool(), result.CopyScoreRegion(), removed, lost, points + result.Points);
        }
    }

    // Optional complete417990 data continuation through418BD0 for types1..4,
    // kinds1/9, mode0/network0/playercount0/localowner-2/another selected fleet.
    // Normal firing, defense, live media, missions and attached objects excluded.
    // The original nonlethal API and playable v0.60 guard remain unchanged.
    public static class PracticeDamageWithDestruction {
        public static PracticeDestructiveDamageResult Apply(PracticeDefenseTables tables, PracticeUnitCatalog units,
                PracticeDestructionTables destruction, byte[] visuals, byte[] ships, byte[] fleets, byte[] scores, byte[] effects,
                int victim, int x, int y, int fleetIndex, int weapon, int attacker, int movieCounter) {
            if (destruction == null) throw new ArgumentNullException("destruction");
            PracticeSegmentDamage.CheckTarget(units, ships, fleets, scores, victim, x, y, weapon, attacker, true);
            new PracticeVisualPool(effects, visuals);
            if (fleetIndex < 0 || fleetIndex >= 6 || movieCounter < short.MinValue || movieCounter > short.MaxValue)
                throw new ArgumentOutOfRangeException("fleetIndex");
            int at = victim * 268, parent = BitConverter.ToInt16(ships, at + 12), fleet = parent * 492, kind = BitConverter.ToInt32(fleets, fleet);
            if (BitConverter.ToInt16(ships, at + 16) < 1 || BitConverter.ToInt16(scores, 172) != -2 || (kind != 1 && kind != 9))
                throw new NotSupportedException("Destructive damage requires a live type1..4 unit, fleet1/9 and the explicit offscreen local-owner context.");
            if (BitConverter.ToInt32(fleets, fleet + 16 + fleetIndex * 4) != 0x4FC340 + at)
                throw new InvalidDataException("Destructive damage member index differs from its original reference.");
            var continuation = new PracticeDamageContinuation(destruction, visuals, effects, fleetIndex, movieCounter);
            var result = PracticeDamage.ApplyCore(tables, units, ships, fleets, scores, victim, x, y, weapon, attacker, continuation);
            return new PracticeDestructiveDamageResult(result, continuation);
        }
    }
}
