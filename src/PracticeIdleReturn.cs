using System;
using System.IO;

namespace FleetLab
{
    public sealed class PracticeIdleReturnResult
    {
        readonly byte[] fleets;
        public PracticeAiSharedBacking Backing { get; private set; }
        public int LinkedFleet { get; private set; }
        public int DestinationX { get; private set; }
        public int DestinationY { get; private set; }
        public int RouteUiRequests { get { return 1; } }
        public int SelectedWaypoint { get { return -1; } }

        internal PracticeIdleReturnResult(byte[] fleets, PracticeAiSharedBacking backing,
                                          int fleet, int x, int y)
        {
            this.fleets = (byte[])fleets.Clone();
            Backing = backing; LinkedFleet = fleet; DestinationX = x; DestinationY = y;
        }

        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
    }

    // Pinned RIP41FC10 +428960 numeric rules. Original654 compares48 complete
    // contexts with40A900 explicitly counted instead of rendered. This API
    // returns that request as data; it does not execute source UI or networking.
    public static class PracticeIdleReturn
    {
        static void Integer(byte[] data, int at, int value)
        { BitConverter.GetBytes(value).CopyTo(data, at); }

        static void Word(byte[] data, int at, int value)
        { BitConverter.GetBytes(unchecked((short)value)).CopyTo(data, at); }

        public static PracticeIdleReturnResult Apply(byte[] fleetPool, PracticeAiSharedBacking backing,
                                                     int taskSlot, int network, int selectedWaypoint)
        {
            if (fleetPool == null || fleetPool.Length != 123000)
                throw new InvalidDataException("Idle return requires the complete fleet pool.");
            if (backing == null) throw new ArgumentNullException("backing");
            if (network != 0 || selectedWaypoint < -1 || selectedWaypoint > 4)
                throw new NotSupportedException("Only the offline selected-waypoint range -1 through4 is compared.");

            var task = backing.CopyTaskRecord(taskSlot);
            long delta = (long)BitConverter.ToUInt32(task, 204) - 0x4b47a0;
            if (delta < 0 || delta >= 123000 || delta % 492 != 0)
                throw new InvalidDataException("Idle return requires a normalized linked fleet reference.");
            int at = (int)delta, state = BitConverter.ToInt32(fleetPool, at + 472);
            if (state != 5 && state != 6)
                throw new NotSupportedException("Only dispatched idle states5 and6 are compared.");

            int x, y;
            if (state == 5)
            {
                int home = BitConverter.ToInt16(fleetPool, at + 488);
                if (home < 0 || home >= 250)
                    throw new InvalidDataException("The idle home reference is outside the fleet pool.");
                Integer(task, 28, BitConverter.ToInt16(fleetPool, at + 12));
                Integer(task, 32, BitConverter.ToInt16(fleetPool, at + 14));
                x = BitConverter.ToInt16(fleetPool, home * 492 + 12);
                y = BitConverter.ToInt16(fleetPool, home * 492 + 14);
            }
            else
            {
                x = BitConverter.ToInt32(task, 28);
                y = BitConverter.ToInt32(task, 32);
            }

            Integer(task, 8, x); Integer(task, 12, y);
            var fleets = (byte[])fleetPool.Clone();
            for (int n = 0; n < 5; n++) Word(fleets, at + 40 + n * 2, -1);
            Integer(task, 304, 0);
            // Preserve full DWORD destinations in task data. Only the fleet
            // waypoints truncate to signed WORDs; the remaining Ys stay opaque.
            Word(fleets, at + 40, x); Word(fleets, at + 50, y);
            return new PracticeIdleReturnResult(fleets, backing.WithTaskRecord(taskSlot, task),
                                                at / 492, x, y);
        }
    }
}
