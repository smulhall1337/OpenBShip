using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FleetLab
{
    public sealed class PracticePrephasePatrolResult
    {
        readonly byte[] ships, fleets, orders, candidates;
        readonly ReadOnlyCollection<int> activated, visited, stops;
        public PracticeAiSharedBacking Backing { get; private set; }
        public int Visits { get { return 250; } }
        public int PolicyVisits { get; private set; }
        public int IdleReturns { get; private set; }
        public int RouteUiRequests { get; private set; }
        public int SelectedWaypoint { get; private set; }
        public int CompletedGoals { get; private set; }
        public int ActiveWaiting { get; private set; }
        public ReadOnlyCollection<int> ActivatedAircraft { get { return activated; } }
        public ReadOnlyCollection<int> VisitedAircraft { get { return visited; } }
        public ReadOnlyCollection<int> StateOneStops { get { return stops; } }
        internal PracticePrephasePatrolResult(byte[] s, byte[] f, byte[] o, byte[] c, PracticeAiSharedBacking backing,
            int policies, int idles, int ui, int waypoint, int completed, int waiting,
            List<int> activated, List<int> visited, List<int> stops)
        {
            ships = (byte[])s.Clone(); fleets = (byte[])f.Clone(); orders = (byte[])o.Clone(); candidates = (byte[])c.Clone();
            Backing = backing; PolicyVisits = policies; IdleReturns = idles; RouteUiRequests = ui; SelectedWaypoint = waypoint; CompletedGoals = completed; ActiveWaiting = waiting;
            this.activated = new List<int>(activated).AsReadOnly(); this.visited = new List<int>(visited).AsReadOnly(); this.stops = new List<int>(stops).AsReadOnly();
        }
        public byte[] CopyShipPool() { return (byte[])ships.Clone(); }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
        public byte[] CopyPrimaryOrders() { return (byte[])orders.Clone(); }
        public byte[] CopyCandidateRegion() { return (byte[])candidates.Clone(); }
    }

    // Whole41FCC0 restricted flag1 + idle + navigation composition. Original695
    // compares1152 complete states. Collision255 and empty visibility are explicit
    // supplied fixtures; this is not the outer owner/phase controller or pathfinder.
    public static class PracticePrephasePatrol
    {
        static void AircraftOnly(byte[] s, byte[] f, int slot)
        {
            int at = slot * 492, kind = BitConverter.ToInt32(f, at), count = 0;
            if (kind != 3 && kind != 4) throw new NotSupportedException("Only surface parents or populated active aircraft are compared on the flag1 path.");
            for (int n = 0; n < 6; n++)
            {
                uint p = BitConverter.ToUInt32(f, at + 16 + n * 4); if (p == 0) continue; long offset = (long)p - 0x4fc340;
                if (offset < 0 || offset >= 402000 || offset % 268 != 0) throw new InvalidDataException("Flag1 aircraft roster reference.");
                int type = BitConverter.ToInt32(s, (int)offset);
                if (type < 8 || type > 10) throw new NotSupportedException("Flagged aircraft must contain only the compared aircraft types8..10."); count++;
            }
            if (count == 0) throw new NotSupportedException("The compared active attachments have real populated aircraft rosters.");
        }
        public static PracticePrephasePatrolResult EvaluateNoTargets(PracticeAttachmentPatrolTables tables,
            byte[] shipPool, byte[] fleetPool, byte[] primaryOrders, PracticeAiSharedBacking backing,
            byte[] candidateRegion, int rawDelta, int network, int selectedWaypoint)
        {
            if (tables == null || backing == null) throw new ArgumentNullException("tables/backing");
            if (shipPool == null || shipPool.Length != 402000 || fleetPool == null || fleetPool.Length != 123000 || primaryOrders == null || primaryOrders.Length != 78000 || candidateRegion == null || candidateRegion.Length != 1184)
                throw new InvalidDataException("Whole patrol requires complete ship/fleet/order/shared-candidate pools.");
            if (network != 0 || selectedWaypoint < -1 || selectedWaypoint > 4) throw new NotSupportedException("Only offline selected-waypoint values -1 through4 are compared.");
            for (int g = 0; g < 250; g++)
            {
                int at = g * 492, flag = fleetPool[at + 486];
                if (flag == 2 || flag == 3 || fleetPool[at + 482] != 255 || fleetPool[at + 467] != 0)
                    throw new NotSupportedException("Flag2/3 policy, pathfinding and visible-target contexts are outside the whole patrol subset.");
                if (BitConverter.ToInt32(backing.CopyTaskRecord(g), 204) != 0x4b47a0 + at) throw new InvalidDataException("Whole patrol requires normalized one-to-one task/fleet links.");
                if (flag == 1 && BitConverter.ToInt32(fleetPool, at) != 1) AircraftOnly(shipPool, fleetPool, g);
            }
            var s = (byte[])shipPool.Clone(); var f = (byte[])fleetPool.Clone(); var o = (byte[])primaryOrders.Clone(); var c = (byte[])candidateRegion.Clone(); var tasks = backing;
            int policies = 0, idles = 0, ui = 0, waypoint = selectedWaypoint, completed = 0, waiting = 0;
            var activated = new List<int>(); var visited = new List<int>(); var stops = new List<int>();
            for (int g = 0; g < 250; g++)
            {
                int at = g * 492;
                if (f[at + 486] == 1)
                {
                    policies++;
                    if (BitConverter.ToInt32(f, at) == 1)
                    {
                        var p = PracticeAttachmentPatrol.EvaluateNoTargets(tables, s, f, o, tasks, c, g, rawDelta, waypoint);
                        s = p.CopyShipPool(); f = p.CopyFleetPool(); o = p.CopyPrimaryOrders(); tasks = p.Backing; c = p.CopyCandidateRegion();
                        ui += p.RouteUiRequests; waypoint = p.SelectedWaypoint; activated.AddRange(p.ActivatedAircraft); visited.AddRange(p.VisitedAircraft);
                        if (p.StoppedAtStateOne >= 0) stops.Add(p.StoppedAtStateOne);
                    }
                    //41F690 scans the newly activated aircraft later in this same
                    // pass. Their8..10 rosters contain no eligible carrier5..7.
                    else AircraftOnly(s, f, g);
                }
                int state = BitConverter.ToInt32(f, at + 472);
                if (BitConverter.ToInt16(f, at + 40) == -1 && (state == 5 || state == 6))
                {
                    var idle = PracticeIdleReturn.Apply(f, tasks, g, 0, waypoint);
                    f = idle.CopyFleetPool(); tasks = idle.Backing; ui += idle.RouteUiRequests; waypoint = idle.SelectedWaypoint; idles++;
                }
                var navigation = PracticeNavigationGate.Evaluate(f, tasks, g); tasks = navigation.Backing;
                if (navigation.Outcome == PracticeNavigationGateOutcome.CompletedGoal) completed++;
                if (navigation.Outcome == PracticeNavigationGateOutcome.ActiveWaiting) waiting++;
            }
            return new PracticePrephasePatrolResult(s, f, o, c, tasks, policies, idles, ui, waypoint, completed, waiting, activated, visited, stops);
        }
    }
}
