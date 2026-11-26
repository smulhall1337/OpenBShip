using System;
using System.IO;

namespace FleetLab
{
    public sealed class PracticePrephaseIdleResult
    {
        readonly byte[] fleets;
        public PracticeAiSharedBacking Backing { get; private set; }
        public int Visits { get { return 250; } }
        public int IdleReturns { get; private set; }
        public int RouteUiRequests { get; private set; }
        public int SelectedWaypoint { get; private set; }
        public int CompletedGoals { get; private set; }
        public int ActiveWaiting { get; private set; }

        internal PracticePrephaseIdleResult(byte[] fleets, PracticeAiSharedBacking backing,
                                            int idles, int requests, int waypoint,
                                            int completed, int waiting)
        {
            this.fleets = (byte[])fleets.Clone(); Backing = backing;
            IdleReturns = idles; RouteUiRequests = requests; SelectedWaypoint = waypoint;
            CompletedGoals = completed; ActiveWaiting = waiting;
        }

        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
    }

    // Whole41FCC0 numeric idle+navigation subset. Original658/664 compare
    // single/multiple idle callbacks with the renderer replaced by a counter.
    // Active policy, pathfinding and networking remain unsupported. The older
    // navigation-only API keeps its explicit rejection of idle callbacks.
    public static class PracticePrephaseIdle
    {
        public static PracticePrephaseIdleResult Evaluate(byte[] fleetPool, PracticeAiSharedBacking backing,
                                                          int network, int selectedWaypoint)
        {
            if (fleetPool == null || fleetPool.Length != 123000)
                throw new InvalidDataException("Pre-phase idle requires the complete fleet pool.");
            if (backing == null) throw new ArgumentNullException("backing");
            if (network != 0 || selectedWaypoint < -1 || selectedWaypoint > 4)
                throw new NotSupportedException("Only offline selected-waypoint values -1 through4 are compared.");

            for (int slot = 0; slot < 250; slot++)
            {
                int flag = fleetPool[slot * 492 + 486];
                if (flag == 1 || flag == 2 || flag == 3)
                    throw new NotSupportedException("Active pre-phase policy is outside this idle/navigation subset.");
            }

            var fleets = (byte[])fleetPool.Clone(); var tasks = backing;
            int idles = 0, requests = 0, waypoint = selectedWaypoint, completed = 0, waiting = 0;
            for (int slot = 0; slot < 250; slot++)
            {
                int at = slot * 492, state = BitConverter.ToInt32(fleets, at + 472);
                if (BitConverter.ToInt16(fleets, at + 40) == -1 && (state == 5 || state == 6))
                {
                    var idle = PracticeIdleReturn.Apply(fleets, tasks, slot, network, waypoint);
                    fleets = idle.CopyFleetPool(); tasks = idle.Backing;
                    requests += idle.RouteUiRequests; waypoint = idle.SelectedWaypoint; idles++;
                }
                var navigation = PracticeNavigationGate.Evaluate(fleets, tasks, slot);
                tasks = navigation.Backing;
                if (navigation.Outcome == PracticeNavigationGateOutcome.CompletedGoal) completed++;
                if (navigation.Outcome == PracticeNavigationGateOutcome.ActiveWaiting) waiting++;
            }
            // Everything above uses owned snapshots. A rejected later callback
            // cannot publish an earlier idle return to either input.
            return new PracticePrephaseIdleResult(fleets, tasks, idles, requests, waypoint, completed, waiting);
        }
    }
}
