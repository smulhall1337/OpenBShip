using System;
using System.IO;

namespace FleetLab {
    public sealed class PracticeTimedMotionUpdate {
        public PracticeTimedMotionState State { get; private set; }
        public int InterpolatedRecords { get; private set; }
        public int StoppedAtRecord { get; private set; } // -1 means full pass
        internal PracticeTimedMotionUpdate(PracticeTimedMotionState state, int count, int stopped) {
            State = state; InterpolatedRecords = count; StoppedAtRecord = stopped;
        }
    }

    // Supported RIP B9A8...6A569:416C40..416DCC. This admits only future
    // records, positive signed frame deltas and network0. The original expiry
    // callback416DD0, creation416A00 and weapon/impact semantics are separate.
    // Pools retain their opaque bytes and source units (world coordinates/ms).
    public sealed class PracticeTimedMotionState {
        public const int TimedBytes = 80 * 268, FleetBytes = 250 * 492;
        readonly byte[] timed, fleets;
        public PracticeTimedMotionState(byte[] timedPool, byte[] fleetPool) {
            if (timedPool == null || timedPool.Length != TimedBytes ||
                fleetPool == null || fleetPool.Length != FleetBytes)
                throw new InvalidDataException("Timed motion requires complete original record and fleet pools.");
            timed = (byte[])timedPool.Clone(); fleets = (byte[])fleetPool.Clone();
        }
        static int Short(byte[] data, int at) { return BitConverter.ToInt16(data, at); }
        static void Short(byte[] data, int at, int value) {
            BitConverter.GetBytes(unchecked((short)value)).CopyTo(data, at);
        }
        static int Component(int distance, int remainingUpdates) {
            // Original x87CW027F: integer operands, single-precision quotient
            // store, signed half adjustment, truncation, then 16bit position add.
            float value = (float)((double)distance / remainingUpdates);
            if (value > 0) value = (float)(value + .5);
            if (value < 0) value = (float)(value - .5);
            return (int)value;
        }
        public PracticeTimedMotionUpdate Advance(uint gameMilliseconds, uint priorDeltaMilliseconds) {
            // Conservative admission checks every active record before any
            // output is published, including records after a possible early
            // return. Unsupported expiry never silently skips its effects.
            for (int at = 0; at < timed.Length; at += 268) {
                if (BitConverter.ToInt32(timed, at) == 0) continue;
                int target = Short(timed, at + 12);
                if (target < 0 || target >= 250) throw new InvalidDataException("Timed record target is outside the fleet pool.");
                if (priorDeltaMilliseconds == 0 || priorDeltaMilliseconds > int.MaxValue)
                    throw new NotSupportedException("Timed motion requires a positive signed source delta.");
                if (unchecked((int)(BitConverter.ToUInt32(timed, at + 24) - gameMilliseconds)) <= 0)
                    throw new NotSupportedException("Timed record expiry and its effects are not reconstructed.");
            }
            byte[] nextTimed = CopyTimedPool(), nextFleets = CopyFleetPool();
            for (int at = 483; at < nextFleets.Length; at += 492) nextFleets[at] = 0;
            int count = 0, stopped = -1;
            for (int slot = 0; slot < 80; slot++) {
                int at = slot * 268;
                if (BitConverter.ToInt32(nextTimed, at) == 0) continue;
                int remaining = unchecked((int)(BitConverter.ToUInt32(nextTimed, at + 24) - gameMilliseconds));
                int updates = remaining / (int)priorDeltaMilliseconds;
                // Source jumps to the function epilogue here. Later records
                // stay unchanged; earlier updates and cleared marks are kept.
                if (updates == 0) { stopped = slot; break; }
                int target = Short(nextTimed, at + 12), fleet = target * 492;
                int marked = BitConverter.ToInt32(nextFleets, fleet) == 2 ? target & 0xFFC : target;
                nextFleets[marked * 492 + 483] = 1;
                int x = Short(nextTimed, at + 8), y = Short(nextTimed, at + 10);
                int dx = Short(nextFleets, fleet + 12) - x + Short(nextTimed, at + 18);
                int dy = Short(nextFleets, fleet + 14) - y + Short(nextTimed, at + 20);
                Short(nextTimed, at + 8, x + Component(dx, updates));
                Short(nextTimed, at + 10, y + Component(dy, updates)); count++;
            }
            return new PracticeTimedMotionUpdate(new PracticeTimedMotionState(nextTimed, nextFleets), count, stopped);
        }
        public byte[] CopyTimedPool() { return (byte[])timed.Clone(); }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
    }
}
