using System;
using System.IO;
using System.Security.Cryptography;

namespace FleetLab {
    public sealed class PracticeTimedWeaponCatalog {
        readonly byte[] speeds;
        public PracticeTimedWeaponCatalog(byte[] sourceSpeeds) {
            if (sourceSpeeds == null || sourceSpeeds.Length != 70)
                throw new InvalidDataException("Run Setup to import the original timed-weapon table.");
            string hash; using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(sourceSpeeds)).Replace("-", "");
            if (hash != "1B29AD0AE8EC1CFEDE88C80A4C436C4BC0E9A2DF83ED2310780A4648DF709038")
                throw new InvalidDataException("Unsupported timed-weapon table identity.");
            speeds = (byte[])sourceSpeeds.Clone();
        }
        public static PracticeTimedWeaponCatalog Load(string programs) {
            return new PracticeTimedWeaponCatalog(File.ReadAllBytes(Path.Combine(programs, "TimedWeaponSpeed.seqbin")));
        }
        public int SourceSpeed(int weapon) {
            if (weapon < 14 || weapon > 30) throw new ArgumentOutOfRangeException("weapon");
            return BitConverter.ToInt16(speeds, weapon * 2);
        }
    }

    public enum PracticeTimedCreationOutcome { Created, NoAmmunition, PoolFull }
    public sealed class PracticeTimedCreationUpdate {
        public PracticeTimedCreationState State { get; private set; }
        public PracticeTimedCreationOutcome Outcome { get; private set; }
        public int CreatedSlot { get; private set; }
        internal PracticeTimedCreationUpdate(PracticeTimedCreationState state, PracticeTimedCreationOutcome outcome, int slot) {
            State = state; Outcome = outcome; CreatedSlot = slot;
        }
    }

    // Supported RIP B9A8...6A569:416A00..416BA3, weapon speed words44D658.
    // Numeric/data construction only, network0 and media owner-mismatch branch.
    // Normal firing admission, media and ammo1 depletion callbacks are separate.
    public sealed class PracticeTimedCreationState {
        readonly byte[] timed, ships, fleets;
        readonly PracticeTimedWeaponCatalog catalog;
        public int LastTargetFleet { get; private set; } // normalized4FC268 identity
        public PracticeTimedCreationState(PracticeTimedWeaponCatalog weapons, byte[] timedPool,
                byte[] shipPool, byte[] fleetPool, int lastTargetFleet) {
            if (weapons == null) throw new ArgumentNullException("weapons");
            if (timedPool == null || timedPool.Length != 21440 || shipPool == null || shipPool.Length != 402000 ||
                fleetPool == null || fleetPool.Length != 123000)
                throw new InvalidDataException("Timed creation requires complete original timed, ship and fleet pools.");
            if (lastTargetFleet < -1 || lastTargetFleet >= 250) throw new ArgumentOutOfRangeException("lastTargetFleet");
            catalog = weapons; timed = (byte[])timedPool.Clone(); ships = (byte[])shipPool.Clone();
            fleets = (byte[])fleetPool.Clone(); LastTargetFleet = lastTargetFleet;
        }
        static int Short(byte[] b, int at) { return BitConverter.ToInt16(b, at); }
        static void Short(byte[] b, int at, int value) { BitConverter.GetBytes(unchecked((short)value)).CopyTo(b, at); }
        static void Int(byte[] b, int at, int value) { BitConverter.GetBytes(value).CopyTo(b, at); }
        public PracticeTimedCreationUpdate Create(int weapon, int sourceShip, int targetFleet,
                                                  int offsetX, int offsetY, uint gameMilliseconds) {
            int speed = catalog.SourceSpeed(weapon);
            if (sourceShip < 0 || sourceShip >= 1500) throw new ArgumentOutOfRangeException("sourceShip");
            if (targetFleet < 0 || targetFleet >= 250) throw new ArgumentOutOfRangeException("targetFleet");
            if (offsetX < short.MinValue || offsetX > short.MaxValue) throw new ArgumentOutOfRangeException("offsetX");
            if (offsetY < short.MinValue || offsetY > short.MaxValue) throw new ArgumentOutOfRangeException("offsetY");
            int slot = 0; while (slot < 80 && BitConverter.ToInt32(timed, slot * 268) != 0) slot++;
            if (slot == 80) return new PracticeTimedCreationUpdate(this, PracticeTimedCreationOutcome.PoolFull, -1);
            int source = sourceShip * 268, ammoAt = source + 196 + weapon * 2, ammo = Short(ships, ammoAt);
            if (ammo == 0) return new PracticeTimedCreationUpdate(this, PracticeTimedCreationOutcome.NoAmmunition, -1);
            if (ammo == 1 || ammo < -1)
                throw new NotSupportedException("Timed-weapon depletion callbacks and negative ammo variants are not reconstructed.");
            int parent = Short(ships, source + 12);
            if (parent < 0 || parent >= 250 || Short(ships, source + 4) != sourceShip || Short(fleets, targetFleet * 492 + 4) != targetFleet)
                throw new InvalidDataException("Timed creation contains an invalid source or target identity.");
            byte[] nextShips = CopyShipPool(), nextTimed = CopyTimedPool();
            if (ammo != -1) Short(nextShips, ammoAt, ammo - 1);
            int at = slot * 268; Array.Copy(nextShips, source, nextTimed, at, 268);
            int x = unchecked((short)(Short(ships, source + 8) + Short(fleets, parent * 492 + 12)));
            int y = unchecked((short)(Short(ships, source + 10) + Short(fleets, parent * 492 + 14)));
            int dx = x - Short(fleets, targetFleet * 492 + 12) - offsetX;
            int dy = y - Short(fleets, targetFleet * 492 + 14) - offsetY;
            int distance = OriginalTimedDistance.Truncated(dx, dy);
            // The source truncates distance BEFORE scaling by1000/dividing.
            // That scale is the supported startup default4FB3D0, not a new
            // physical speed calibration. Odd speed words use integer /2.
            int duration = distance * 1000 / (speed / 2);
            Int(nextTimed, at, weapon); Short(nextTimed, at + 4, slot);
            Short(nextTimed, at + 8, x); Short(nextTimed, at + 10, y);
            Short(nextTimed, at + 12, targetFleet); Short(nextTimed, at + 16, sourceShip);
            Short(nextTimed, at + 18, offsetX); Short(nextTimed, at + 20, offsetY);
            Int(nextTimed, at + 24, unchecked((int)(gameMilliseconds + (uint)duration)));
            // Evidence uses the original VA as an identity token only. It is
            // never dereferenced as a managed-process pointer.
            Int(nextTimed, at + 28, 0x4B47A0 + targetFleet * 492);
            return new PracticeTimedCreationUpdate(new PracticeTimedCreationState(catalog, nextTimed, nextShips, fleets, targetFleet), PracticeTimedCreationOutcome.Created, slot);
        }
        public byte[] CopyTimedPool() { return (byte[])timed.Clone(); }
        public byte[] CopyShipPool() { return (byte[])ships.Clone(); }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
    }
}
