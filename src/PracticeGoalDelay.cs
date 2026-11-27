using System;
using System.IO;

namespace FleetLab
{
    public enum PracticeGoalDelayOutcome { KindReturns, DelayScheduled, DeadlineHeld, WaypointEmitted }

    public sealed class PracticeGoalDelayResult
    {
        readonly byte[] fleets;
        public PracticeAiSharedBacking Backing { get; private set; }
        public PracticeGoalDelayOutcome Outcome { get; private set; }
        public int SourceClock { get; private set; }
        internal PracticeGoalDelayResult(byte[] f, PracticeAiSharedBacking b, PracticeGoalDelayOutcome outcome, int clock)
        { fleets = (byte[])f.Clone(); Backing = b; Outcome = outcome; SourceClock = clock; }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
    }

    // Supported RIP B9A8...6A569, goal2 41EB00 and goal3 41F110.
    // Compared against original790 in 1056 controlled full-state cases.
    // Clock and current delay weight are explicit source DWORD values. No
    // elapsed-time conversion, clock advancement or complete AI driver runs.
    public static class PracticeGoalDelay
    {
        // Raw source448680 before difficulty initialization. The eight-byte
        // AiPreparationDelays resource starts at448684 and does not contain it.
        public const int SourceBaseDelayWeight = 32000;

        public static PracticeGoalDelayResult Schedule(byte[] fleets, PracticeAiSharedBacking backing,
            int taskSlot, int sourceClock, int delayWeight)
        {
            byte[] task = Validate(fleets, backing, taskSlot, 2);
            int kind = BitConverter.ToInt32(fleets, taskSlot * 492);
            // These source branches return before the navigation field.
            if (kind == 2 || kind == 11)
                return new PracticeGoalDelayResult(fleets, backing, PracticeGoalDelayOutcome.KindReturns, sourceClock);
            if (kind != 3 && kind != 4 && kind != 5 && BitConverter.ToInt32(task, 304) != 0)
                throw new NotSupportedException("Goal2 active navigation enters unreconstructed route-grid work.");
            BitConverter.GetBytes(unchecked(sourceClock + delayWeight)).CopyTo(task, 4);
            BitConverter.GetBytes(3).CopyTo(task, 200);
            return new PracticeGoalDelayResult(fleets, backing.WithTaskRecord(taskSlot, task),
                PracticeGoalDelayOutcome.DelayScheduled, sourceClock);
        }

        public static PracticeGoalDelayResult EmitWhenDue(byte[] fleets, PracticeAiSharedBacking backing,
            int taskSlot, int sourceClock)
        {
            byte[] task = Validate(fleets, backing, taskSlot, 3);
            int at = taskSlot * 492, kind = BitConverter.ToInt32(fleets, at);
            // Signed comparison and strict inequality are deliberate. Equality
            // holds. Source kinds3/4 bypass this test, including equality.
            if (kind != 3 && kind != 4 && BitConverter.ToInt32(task, 4) >= sourceClock)
                return new PracticeGoalDelayResult(fleets, backing, PracticeGoalDelayOutcome.DeadlineHeld, sourceClock);
            var after = (byte[])fleets.Clone();
            BitConverter.GetBytes(unchecked((short)BitConverter.ToInt32(task, 8))).CopyTo(after, at + 40);
            BitConverter.GetBytes(unchecked((short)BitConverter.ToInt32(task, 12))).CopyTo(after, at + 50);
            BitConverter.GetBytes(4).CopyTo(task, 200);
            return new PracticeGoalDelayResult(after, backing.WithTaskRecord(taskSlot, task),
                PracticeGoalDelayOutcome.WaypointEmitted, sourceClock);
        }

        static byte[] Validate(byte[] fleets, PracticeAiSharedBacking backing, int slot, int goal)
        {
            if (fleets == null || fleets.Length != 123000)
                throw new InvalidDataException("Goal delay requires the complete fleet pool.");
            if (backing == null) throw new ArgumentNullException("backing");
            var task = backing.CopyTaskRecord(slot); int at = slot * 492;
            if (BitConverter.ToUInt32(task, 204) != 0x4b47a0u + (uint)at ||
                BitConverter.ToInt16(fleets, at + 4) != slot)
                throw new InvalidDataException("Goal delay requires matching normalized task/fleet identities.");
            int kind = BitConverter.ToInt32(fleets, at);
            if (kind < 0 || (kind > 12 && kind != 25))
                throw new NotSupportedException("Goal delay admits only fleet kinds compared with the supported original.");
            if (BitConverter.ToInt32(task, 200) != goal || BitConverter.ToInt16(fleets, at + 40) != -1)
                throw new NotSupportedException("Goal delay requires the compared goal and an empty first waypoint.");
            return task;
        }
    }
}
