using System;
using System.IO;

namespace FleetLab
{
    public enum PracticeQueuedGoalOutcome { ResetToGoalOne, QueuedFirstWaypoint }

    public sealed class PracticeQueuedGoalResult
    {
        readonly byte[] fleets;
        public PracticeAiSharedBacking Backing { get; private set; }
        public PracticeQueuedGoalOutcome Outcome { get; private set; }
        public int LinkedFleet { get; private set; }
        internal PracticeQueuedGoalResult(byte[] fleets, PracticeAiSharedBacking backing,
            PracticeQueuedGoalOutcome outcome, int fleet)
        { this.fleets = (byte[])fleets.Clone(); Backing = backing; Outcome = outcome; LinkedFleet = fleet; }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
    }

    // RIP B9A8...6A569,41D798..41D7BD, after an admitted owner/member visit.
    // Original744 compares this branch inside40 whole-driver contexts. This
    // helper does not admit an owner, dispatch tasks, move fleets or find routes.
    public static class PracticeQueuedGoal
    {
        public static PracticeQueuedGoalResult ApplyGoalFour(byte[] fleets, PracticeAiSharedBacking backing, int taskSlot)
        {
            if (fleets == null || fleets.Length != 123000)
                throw new InvalidDataException("Queued goal requires the complete fleet pool.");
            if (backing == null) throw new ArgumentNullException("backing");
            var task = backing.CopyTaskRecord(taskSlot); int at = taskSlot * 492;
            if (BitConverter.ToUInt32(task, 204) != 0x4b47a0u + (uint)at ||
                BitConverter.ToInt16(fleets, at + 4) != taskSlot)
                throw new InvalidDataException("Queued goal task and fleet identities must agree.");
            if (BitConverter.ToInt32(fleets, at) != 1 || BitConverter.ToInt16(fleets, at + 6) != 1 ||
                BitConverter.ToInt16(fleets, at + 40) != -1 || BitConverter.ToInt32(task, 200) != 4)
                throw new NotSupportedException("The compared goal-four branch requires an owner1 surface fleet with no first waypoint. Other goals/planners are outside this API.");
            var after = (byte[])fleets.Clone(); int x = BitConverter.ToInt32(task, 8);
            if (x == -1)
            {
                BitConverter.GetBytes(1).CopyTo(task, 200);
                return new PracticeQueuedGoalResult(after, backing.WithTaskRecord(taskSlot, task),
                    PracticeQueuedGoalOutcome.ResetToGoalOne, taskSlot);
            }
            // Test the DWORD sentinel before truncating. No map clamp occurs;
            // 65535 writes WORD-1 but does not take the reset branch.
            BitConverter.GetBytes(unchecked((short)x)).CopyTo(after, at + 40);
            BitConverter.GetBytes(unchecked((short)BitConverter.ToInt32(task, 12))).CopyTo(after, at + 50);
            return new PracticeQueuedGoalResult(after, backing, PracticeQueuedGoalOutcome.QueuedFirstWaypoint, taskSlot);
        }
    }
}
