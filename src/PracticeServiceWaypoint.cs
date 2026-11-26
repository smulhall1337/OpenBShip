using System;
using System.IO;

namespace FleetLab
{
    public enum PracticeServiceWaypointOutcome { Updated, NoFirstWaypoint, NavigationActive, ModeTwo }

    public sealed class PracticeServiceWaypointResult
    {
        readonly byte[] fleets;
        public PracticeAiSharedBacking Backing { get; private set; }
        public PracticeServiceWaypointOutcome Outcome { get; private set; }
        public bool SelectorInvoked { get { return Outcome == PracticeServiceWaypointOutcome.Updated; } }
        public int ReplacedWaypoint { get; private set; }
        public bool ClearedTargetReference { get; private set; }
        // Null means the caller returned before invoking a destination selector.
        public PracticeServiceDestinationResult Destination { get; private set; }

        internal PracticeServiceWaypointResult(byte[] f, PracticeAiSharedBacking b,
            PracticeServiceWaypointOutcome outcome, int index, bool cleared,
            PracticeServiceDestinationResult destination)
        {
            fleets = (byte[])f.Clone(); Backing = b; Outcome = outcome;
            ReplacedWaypoint = index; ClearedTargetReference = cleared; Destination = destination;
        }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
    }

    // RIP B9A8...6A569: whole41C690 for task11,including its41D9B0 selector
    // and41E540 destination leaf. Native775 compares792 controlled contexts.
    // This updates copied intent;no owner admission,movement,pathfinding or
    // world clock runs. Older complete-dispatcher rejection guards stay intact.
    public static class PracticeServiceWaypoint
    {
        public static PracticeServiceWaypointResult Evaluate(byte[] ships, byte[] fleets,
            PracticeAiSharedBacking backing, int taskSlot, int activeOwner)
        {
            if (ships == null || ships.Length != 402000 || fleets == null || fleets.Length != 123000)
                throw new InvalidDataException("Service waypoint requires complete ship and fleet pools.");
            if (backing == null) throw new ArgumentNullException("backing");
            if (activeOwner != 1)
                throw new NotSupportedException("Service waypoint currently admits compared active-owner1 contexts only.");
            var task = backing.CopyTaskRecord(taskSlot); int at = taskSlot * 492;
            if (BitConverter.ToUInt32(task, 204) != 0x4b47a0u + (uint)at ||
                BitConverter.ToInt16(fleets, at + 4) != taskSlot)
                throw new InvalidDataException("Service waypoint task and fleet identities must agree.");

            // Source order matters. These returns do not read task kind,308 or
            // yard roster references. Unknown/unreachable data remains opaque.
            if (BitConverter.ToInt16(fleets, at + 40) == -1)
                return Skipped(fleets, backing, PracticeServiceWaypointOutcome.NoFirstWaypoint);
            if (BitConverter.ToInt32(task, 304) != 0)
                return Skipped(fleets, backing, PracticeServiceWaypointOutcome.NavigationActive);
            if (BitConverter.ToInt32(fleets, at + 476) == 2)
                return Skipped(fleets, backing, PracticeServiceWaypointOutcome.ModeTwo);
            if (BitConverter.ToInt32(task, 0) != 11)
                throw new NotSupportedException("Only the service task11 destination selector is reconstructed here.");

            int target = BitConverter.ToInt32(task, 308); bool cleared = false;
            var working = backing;
            if (target >= 0)
            {
                if (target >= 250) throw new InvalidDataException("Service waypoint reached a target outside the fleet pool.");
                if (BitConverter.ToInt32(fleets, target * 492) == 0)
                {
                    BitConverter.GetBytes(-1).CopyTo(task, 308);
                    working = backing.WithTaskRecord(taskSlot, task); cleared = true;
                }
            }
            var destination = PracticeServiceDestination.Evaluate(ships, fleets, working, taskSlot, activeOwner);
            var after = destination.CopyFleetPool(); task = destination.Backing.CopyTaskRecord(taskSlot);
            BitConverter.GetBytes(2).CopyTo(task, 200); // Task11 selector's post-call goal.
            int count = 0;
            while (count < 5 && BitConverter.ToInt16(after, at + 40 + count * 2) != -1) count++;
            if (count == 0) throw new InvalidDataException("Destination unexpectedly removed the caller's first waypoint.");
            int index = count - 1;
            BitConverter.GetBytes(unchecked((short)BitConverter.ToInt32(task, 8))).CopyTo(after, at + 40 + index * 2);
            BitConverter.GetBytes(unchecked((short)BitConverter.ToInt32(task, 12))).CopyTo(after, at + 50 + index * 2);
            return new PracticeServiceWaypointResult(after, destination.Backing.WithTaskRecord(taskSlot, task),
                PracticeServiceWaypointOutcome.Updated, index, cleared, destination);
        }

        static PracticeServiceWaypointResult Skipped(byte[] fleets, PracticeAiSharedBacking backing,
            PracticeServiceWaypointOutcome outcome)
        { return new PracticeServiceWaypointResult(fleets, backing, outcome, -1, false, null); }
    }
}
