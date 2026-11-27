using System;
using System.IO;

namespace FleetLab
{
    public sealed class PracticeWaypointNeighbor
    {
        public int CellX { get; private set; }
        public int CellY { get; private set; }
        public int Cost { get; private set; }
        // -1 means no candidate beat the initial FD byte; source emits its
        // rounded current cell in that case. This does not establish a route.
        public int DirectionIndex { get; private set; }
        PracticeWaypointNeighbor(int x, int y, int cost, int direction)
        { CellX = x; CellY = y; Cost = cost; DirectionIndex = direction; }
        public static PracticeWaypointNeighbor Select(PracticeAiRouteGrid grid, int worldX, int worldY)
        {
            if (grid == null) throw new ArgumentNullException("grid");
            if (worldX < 0 || worldX > 630 || worldY < 0 || worldY > 630)
                throw new NotSupportedException("Waypoint selection requires compared source coordinates 0..630.");
            int x = worldX / 10, y = worldY / 10, bestX = x, bestY = y, best = 253, chosen = -1;
            // Strict unsigned lower comparison retains the earlier direction
            // on ties: NW,W,SW,N,S,NE,E,SE. A diagonal cannot cut an FF corner.
            int[,] directions = { { -1, -1 }, { -1, 0 }, { -1, 1 }, { 0, -1 }, { 0, 1 }, { 1, -1 }, { 1, 0 }, { 1, 1 } };
            for (int n = 0; n < 8; n++)
            {
                int dx = directions[n, 0], dy = directions[n, 1], nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= 64 || ny < 0 || ny >= 64) continue;
                if (dx != 0 && dy != 0 && (grid.Cell(nx, y) == 255 || grid.Cell(x, ny) == 255)) continue;
                int value = grid.Cell(nx, ny); if (value >= best) continue;
                best = value; bestX = nx; bestY = ny; chosen = n;
            }
            return new PracticeWaypointNeighbor(bestX, bestY, best, chosen);
        }
    }

    public enum PracticeGoalWaypointOutcome { TaskPopped, CachedWaypoint, RetryHeld, NeighborWaypoint }
    public sealed class PracticeGoalWaypointResult
    {
        readonly byte[] fleets;
        public PracticeAiSharedBacking Backing { get; private set; }
        public PracticeAiRouteGrid Grid { get; private set; }
        public PracticeGoalWaypointOutcome Outcome { get; private set; }
        // Null means source returned before invoking the corresponding stage.
        public PracticeGoalRouteResult Route { get; private set; }
        public PracticeWaypointNeighbor Neighbor { get; private set; }
        internal PracticeGoalWaypointResult(byte[] f, PracticeAiSharedBacking backing, PracticeAiRouteGrid grid,
            PracticeGoalWaypointOutcome outcome, PracticeGoalRouteResult route, PracticeWaypointNeighbor neighbor)
        { fleets = (byte[])f.Clone(); Backing = backing; Grid = grid; Outcome = outcome; Route = route; Neighbor = neighbor; }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
    }

    // Supported RIP B9A8...6A569 whole41F170 with41B7A0/41EB00.
    // Original813 compares396 complete states. This emits one copied point or
    // completes one queue record; fleet world coordinates never move here.
    public static class PracticeGoalWaypoint
    {
        static void Put(byte[] b, int at, int value) { BitConverter.GetBytes(value).CopyTo(b, at); }
        static void Word(byte[] b, int at, int value) { BitConverter.GetBytes(unchecked((short)value)).CopyTo(b, at); }
        public static PracticeGoalWaypointResult Evaluate(PracticeAiRouteGrid grid, byte[] fleets,
            byte[] secondaryFleets, PracticeAiSharedBacking backing, int sourceClock, int delayWeight, int ownerPolicy)
        { return EvaluateCore(grid, fleets, secondaryFleets, backing, sourceClock, delayWeight, ownerPolicy, false); }

        // Original829 compares the41F920 caller with an existing point exactly
        // at the fleet's world position. Keep the public fresh-input guard.
        internal static PracticeGoalWaypointResult EvaluateForNavigation(PracticeAiRouteGrid grid, byte[] fleets,
            byte[] secondaryFleets, PracticeAiSharedBacking backing, int sourceClock, int delayWeight, int ownerPolicy)
        { return EvaluateCore(grid, fleets, secondaryFleets, backing, sourceClock, delayWeight, ownerPolicy, true); }

        // Original839 compares reached points after a queued navigation start.
        internal static PracticeGoalWaypointResult EvaluateAfterQueuedNavigation(PracticeAiRouteGrid grid, byte[] fleets,
            byte[] secondaryFleets, PracticeAiSharedBacking backing, int sourceClock, int delayWeight, int ownerPolicy)
        {
            if(grid==null||backing==null)throw new ArgumentNullException("grid");
            int goal=BitConverter.ToInt32(backing.CopyTaskRecord(grid.TaskSlot),200);
            if(goal!=2&&goal!=4)throw new NotSupportedException("Only captured queued-navigation goals2/4 are admitted.");
            return EvaluateCore(grid,fleets,secondaryFleets,backing,sourceClock,delayWeight,ownerPolicy,true,goal);
        }
        // Original884 compares41FBAD with a still-unreached point after a real
        // terrainFE or stationary kind11/slot30 collision. Other callers retain
        // their fresh/reached-point admission guards.
        internal static PracticeGoalWaypointResult EvaluateAfterCollision(PracticeAiRouteGrid grid, byte[] fleets,
            byte[] secondaryFleets, PracticeAiSharedBacking backing, int sourceClock, int delayWeight, int ownerPolicy)
        {
            if(grid==null||backing==null)throw new ArgumentNullException("grid");
            int goal=BitConverter.ToInt32(backing.CopyTaskRecord(grid.TaskSlot),200);
            if(goal!=2&&goal!=4)throw new NotSupportedException("Collision navigation requires the compared goals2/4.");
            return EvaluateCore(grid,fleets,secondaryFleets,backing,sourceClock,delayWeight,ownerPolicy,true,goal,true);
        }
        static PracticeGoalWaypointResult EvaluateCore(PracticeAiRouteGrid grid, byte[] fleets,
            byte[] secondaryFleets, PracticeAiSharedBacking backing, int sourceClock, int delayWeight, int ownerPolicy, bool navigationArrival, int expectedGoal = 4, bool collisionReplan = false)
        {
            if (grid == null || backing == null) throw new ArgumentNullException("grid");
            if (fleets == null || fleets.Length != 123000 || secondaryFleets == null || secondaryFleets.Length != 246000)
                throw new InvalidDataException("Waypoint callback requires complete primary and secondary fleet pools.");
            int slot = grid.TaskSlot, at = slot * 492; var task = backing.CopyTaskRecord(slot);
            if (BitConverter.ToUInt32(task, 204) != 0x4b47a0u + (uint)at || BitConverter.ToInt16(fleets, at + 4) != slot)
                throw new InvalidDataException("Waypoint callback task and fleet identities must agree.");
            int kind = BitConverter.ToInt32(fleets, at);
            if ((kind != 1 && kind != 9 && kind != 3 && kind != 4 && kind != 5) ||
                BitConverter.ToInt16(fleets, at + 6) != 1 || BitConverter.ToInt32(task, 0) != 11 ||
                BitConverter.ToInt32(task, 200) != expectedGoal || (!navigationArrival && BitConverter.ToInt16(fleets, at + 40) != -1))
                throw new NotSupportedException("Waypoint callback admits compared owner1 task11/goal4 and fleet kinds1/3/4/5/9 with no first waypoint.");
            if (navigationArrival && !collisionReplan && BitConverter.ToInt16(fleets, at + 40) != -1 &&
                (BitConverter.ToInt16(fleets, at + 40) != BitConverter.ToInt16(fleets, at + 12) ||
                 BitConverter.ToInt16(fleets, at + 50) != BitConverter.ToInt16(fleets, at + 14)))
                throw new NotSupportedException("Only an empty or exactly reached first point is compared by the navigation caller.");
            var after = (byte[])fleets.Clone(); int x = BitConverter.ToInt16(fleets, at + 12), y = BitConverter.ToInt16(fleets, at + 14);
            int cachedX = BitConverter.ToInt32(task, 8), cachedY = BitConverter.ToInt32(task, 12);
            // This exact WORD-to-DWORD equality precedes kind selection and
            // does not inspect route table, navigation or cached neighborhood.
            if (x == cachedX && y == cachedY)
            {
                Array.Copy(task, 20, task, 0, 180); Array.Clear(task, 180, 20);
                Put(task, 200, 0); Put(task, 308, -1); Put(task, 16, 1); Word(after, at + 40, -1);
                return new PracticeGoalWaypointResult(after, backing.WithTaskRecord(slot, task), grid,
                    PracticeGoalWaypointOutcome.TaskPopped, null, null);
            }
            if (kind >= 3 && kind <= 5)
            {
                Word(after, at + 40, cachedX); Word(after, at + 50, cachedY);
                return new PracticeGoalWaypointResult(after, backing, grid, PracticeGoalWaypointOutcome.CachedWaypoint, null, null);
            }
            var route = expectedGoal == 2
                ? PracticeGoalRoute.BuildForQueuedNavigation(grid, fleets, secondaryFleets, backing, sourceClock, delayWeight, ownerPolicy)
                : PracticeGoalRoute.BuildForWaypoint(grid, fleets, secondaryFleets, backing, sourceClock, delayWeight, ownerPolicy, navigationArrival);
            task = route.Backing.CopyTaskRecord(slot);
            if (task[284] != 0 && BitConverter.ToInt32(task, 4) > sourceClock)
                return new PracticeGoalWaypointResult(after, route.Backing, route.Grid, PracticeGoalWaypointOutcome.RetryHeld, route, null);
            var neighbor = PracticeWaypointNeighbor.Select(route.Grid, x, y);
            Word(after, at + 40, neighbor.CellX * 10); Word(after, at + 50, neighbor.CellY * 10);
            return new PracticeGoalWaypointResult(after, route.Backing, route.Grid,
                PracticeGoalWaypointOutcome.NeighborWaypoint, route, neighbor);
        }
    }
}
