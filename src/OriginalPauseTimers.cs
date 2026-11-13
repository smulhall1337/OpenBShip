using System;
using System.IO;

namespace FleetLab {
    // Supported RIP B9A8...6A569:411313..4113AC, network0 pause tail.
    // These opaque buffers retain source layout. Their complete object logic
    // is not reconstructed by adjusting the timer fields. No live timer runs.
    public sealed class OriginalPauseTimers {
        public const int TimedRecordBytes = 80 * 268;
        public const int ShipTimerBytes = 1500 * 52;
        readonly byte[] timedRecords, shipTimers, fourCounters, timestamps;
        public uint GlobalCounter { get; private set; } // 4FC2D4

        public OriginalPauseTimers(byte[] timed, byte[] ships, byte[] four, byte[] sixteen, uint globalCounter) {
            Need(timed, TimedRecordBytes); Need(ships, ShipTimerBytes);
            Need(four, 16); Need(sixteen, 64);
            timedRecords = (byte[])timed.Clone(); shipTimers = (byte[])ships.Clone();
            fourCounters = (byte[])four.Clone(); timestamps = (byte[])sixteen.Clone();
            GlobalCounter = globalCounter;
        }
        static void Need(byte[] data, int length) {
            if (data == null || data.Length != length)
                throw new InvalidDataException("Pause compensation requires complete original timer buffers.");
        }
        static void Add(byte[] data, int at, uint delta) {
            BitConverter.GetBytes(unchecked(BitConverter.ToUInt32(data, at) + delta)).CopyTo(data, at);
        }
        // The enclosing source world body sees the preceding frame delta.
        // Its paused tail shifts deadlines forward instead of running them.
        // Any nonzero source pause word selects this branch, not only1.
        public OriginalPauseTimers AdvancePausedFrame(short pauseWord, uint priorDeltaMilliseconds) {
            if (pauseWord == 0 || priorDeltaMilliseconds == 0) return this;
            byte[] timed = CopyTimedRecords(), ships = CopyShipTimers();
            byte[] four = CopyFourCounters(), sixteen = CopyTimestamps();
            for (int at = 0; at < timed.Length; at += 268)
                if (BitConverter.ToUInt32(timed, at) != 0) Add(timed, at + 24, priorDeltaMilliseconds);
            for (int at = 0; at < ships.Length; at += 52)
                if (BitConverter.ToInt16(ships, at + 8) != -1) Add(ships, at + 4, priorDeltaMilliseconds);
            for (int at = 0; at < four.Length; at += 4) Add(four, at, priorDeltaMilliseconds);
            for (int at = 0; at < sixteen.Length; at += 4)
                if (BitConverter.ToUInt32(sixteen, at) != uint.MaxValue) Add(sixteen, at, priorDeltaMilliseconds);
            return new OriginalPauseTimers(timed, ships, four, sixteen, unchecked(GlobalCounter + priorDeltaMilliseconds));
        }
        public byte[] CopyTimedRecords() { return (byte[])timedRecords.Clone(); }
        public byte[] CopyShipTimers() { return (byte[])shipTimers.Clone(); }
        public byte[] CopyFourCounters() { return (byte[])fourCounters.Clone(); }
        public byte[] CopyTimestamps() { return (byte[])timestamps.Clone(); }
    }
}
