using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace FleetLab {
    // Hash-pinned original region44D1D8..44D657, retained as a region rather
    // than inventing a single table layout for its overlapping lookup bases.
    // Setup imports this region for the optional manual impact study.
    public sealed class PracticeDefenseTables {
        readonly byte[] data;
        public PracticeDefenseTables(byte[] sourceRegion) {
            if (sourceRegion == null || sourceRegion.Length != 1152)
                throw new InvalidDataException("Expected the complete original defense-table region.");
            string hash; using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(sourceRegion)).Replace("-", "");
            if (hash != "976F96563684FC08774653BADCC75AF765F781605E91B4DF270936DEE7E2C337")
                throw new InvalidDataException("Unsupported original defense-table identity.");
            data = (byte[])sourceRegion.Clone();
        }
        internal int BaseChance(int weapon, int targetKind) { return unchecked((sbyte)data[weapon * 13 + targetKind]); }
        public static PracticeDefenseTables Load(string programs) {
            return new PracticeDefenseTables(File.ReadAllBytes(Path.Combine(programs,"CombatMath.seqbin")));
        }
        internal int PrimaryChance(int selection, int weapon) { return unchecked((sbyte)data[0x255 + selection * 35 + weapon]); }
        internal int SecondaryChance(int selection, int weapon) { return unchecked((sbyte)data[0x8D + selection * 35 + weapon]); }
        internal int[] CopyDamagePriority(bool primary) {
            int first = primary ? 0x390 : 0x3F0, end = primary ? 0x3F0 : 0x480;
            var result = new List<int>();
            for (int at = first; at < end; at += 4) {
                int value = BitConverter.ToInt32(data, at);
                if (value == 0) return result.ToArray();
                result.Add(value);
            }
            throw new InvalidDataException("Original damage priority list has no bounded terminator.");
        }
    }

    public enum PracticeDefenseOutcome { PrimaryStopped = 0, Miss = 1, ProceedToDamage = 2 }
    public sealed class PracticeDefenseDecision {
        readonly byte[] ships;
        public PracticeDefenseOutcome Outcome { get; private set; }
        public uint RandomState { get; private set; }
        public int RandomCalls { get; private set; }
        public int PrimaryAttempts { get; private set; }
        public int Threshold { get; private set; }
        public int Roll { get; private set; }
        internal PracticeDefenseDecision(byte[] pool, PracticeDefenseOutcome outcome, NativeRandom random, int attempts, int threshold, int roll) {
            ships = pool; Outcome = outcome; RandomState = random.State; RandomCalls = random.Calls;
            PrimaryAttempts = attempts; Threshold = threshold; Roll = roll;
        }
        public byte[] CopyShipPool() { return (byte[])ships.Clone(); }
    }

    // Original418030, RIP B9A8...6A569. Pure decision and unit bytes88/89 only;
    // no damage, ammunition consumption, media or normal firing admission.
    public static class PracticeDefense {
        public static PracticeDefenseDecision Resolve(PracticeDefenseTables tables, byte[] timedPool,
                byte[] shipPool, byte[] fleetPool, int timedSlot, int victimShip, uint randomState) {
            if (tables == null) throw new ArgumentNullException("tables");
            if (timedPool == null || timedPool.Length != 21440 || shipPool == null || shipPool.Length != 402000 || fleetPool == null || fleetPool.Length != 123000)
                throw new InvalidDataException("Defense decisions require complete original timed, ship and fleet pools.");
            if (timedSlot < 0 || timedSlot >= 80) throw new ArgumentOutOfRangeException("timedSlot");
            if (victimShip < 0 || victimShip >= 1500) throw new ArgumentOutOfRangeException("victimShip");
            int timed = timedSlot * 268, unit = victimShip * 268;
            int weapon = BitConverter.ToInt32(timedPool, timed), target = BitConverter.ToInt16(timedPool, timed + 12);
            int parent = BitConverter.ToInt16(shipPool, unit + 12), length = BitConverter.ToInt16(shipPool, unit + 14);
            if (weapon < 14 || weapon > 30 || target < 0 || target >= 250 || parent < 0 || parent >= 250 || length < 0 || length > 6 || BitConverter.ToInt16(shipPool, unit + 4) != victimShip)
                throw new InvalidDataException("Unsupported defense identity, weapon or equipment-row extent.");
            int kind = BitConverter.ToInt32(fleetPool, target * 492), primary = BitConverter.ToInt32(shipPool, unit + 76), secondary = BitConverter.ToInt32(shipPool, unit + 80);
            if ((kind != 1 && kind != 2 && kind != 9 && kind != 11) || (primary != 0 && (primary < 4 || primary > 8)) || (secondary != 0 && (secondary < 9 || secondary > 13)))
                throw new NotSupportedException("Unsupported defense fleet kind or equipment selection.");
            byte[] ships = (byte[])shipPool.Clone(); var random = new NativeRandom(randomState); int attempts = 0;
            if (primary != 0) for (int entry = 0; entry < length * 4; entry++) {
                if (BitConverter.ToInt32(ships, unit + 100 + entry * 4) != primary) continue;
                ships[unit + 88] = 50; attempts++;
                int roll = random.Next() % 101;
                int threshold = 5 * fleetPool[parent * 492 + 485] + tables.PrimaryChance(primary, weapon);
                if (threshold >= roll) return new PracticeDefenseDecision(ships, PracticeDefenseOutcome.PrimaryStopped, random, attempts, threshold, roll);
            }
            int chance = tables.BaseChance(weapon, kind), hitRoll = random.Next() % 101;
            // Unlike the primary selection, this secondary stage checks neither
            // equipment membership nor ammunition inside418030.
            if (secondary != 0) { ships[unit + 89] = 50; chance -= tables.SecondaryChance(secondary, weapon); }
            int stance = fleetPool[parent * 492 + 486];
            if (stance == 2 && weapon >= 19 && weapon <= 24) chance -= 20;
            if (stance == 3 && (weapon == 25 || weapon == 26)) chance -= 20;
            if (stance == 4) chance += -30 * chance / 100;
            if (stance == 5) chance += 15 * chance / -100;
            return new PracticeDefenseDecision(ships, hitRoll <= chance ? PracticeDefenseOutcome.ProceedToDamage : PracticeDefenseOutcome.Miss, random, attempts, chance, hitRoll);
        }
    }
}
