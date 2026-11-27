using System;
using System.IO;

namespace FleetLab
{
    // One copied task's 64x64 X-major byte grid. The normalized reference is
    // an identity in the compared diagnostic allocator, never a live pointer.
    public sealed class PracticeAiRouteGrid
    {
        readonly byte[] cells;
        public const int Side = 64;
        public int TaskSlot { get; private set; }
        public uint NormalizedTableReference { get { return 0x70000010u + (uint)(TaskSlot * 6432); } }
        public PracticeAiRouteGrid(int taskSlot, byte[] sourceCells)
        {
            if (taskSlot < 0 || taskSlot >= 250) throw new ArgumentOutOfRangeException("taskSlot");
            if (sourceCells == null || sourceCells.Length != 4096)
                throw new InvalidDataException("A route grid requires exactly 4096 X-major bytes.");
            TaskSlot = taskSlot; cells = (byte[])sourceCells.Clone();
        }
        public byte[] CopyCells() { return (byte[])cells.Clone(); }
        public byte Cell(int x, int y)
        {
            if (x < 0 || x >= Side || y < 0 || y >= Side) throw new ArgumentOutOfRangeException("x");
            return cells[x * Side + y];
        }
        public static PracticeAiRouteGrid Diagnostic(int slot, byte fill)
        { var cells = new byte[4096]; for (int n = 0; n < cells.Length; n++) cells[n] = fill; return new PracticeAiRouteGrid(slot, cells); }
    }

    public enum PracticeGoalRouteOutcome { CooldownHeld, NeighborhoodHeld, RouteFound, RouteUnavailable }
    public sealed class PracticeGoalRouteResult
    {
        public PracticeAiRouteGrid Grid { get; private set; }
        public PracticeAiSharedBacking Backing { get; private set; }
        public PracticeGoalRouteOutcome Outcome { get; private set; }
        internal PracticeGoalRouteResult(PracticeAiRouteGrid grid, PracticeAiSharedBacking backing, PracticeGoalRouteOutcome outcome)
        { Grid = grid; Backing = backing; Outcome = outcome; }
    }

    // Supported RIP B9A8...6A569:41EB00 with41BB30/41E990/41EA30.
    // Native805 compares396 controlled visits, including actual failed-search
    // retries. This leaf builds costs and copied task intent; it does not run
    // goal4 route following, enqueue waypoints, move fleets or advance time.
    public static class PracticeGoalRoute
    {
        static void Put(byte[] b, int at, int value) { BitConverter.GetBytes(value).CopyTo(b, at); }
        static int Coordinate(int value)
        {
            if (value < 0 || value > 630) throw new NotSupportedException("Route study coordinates must lie in the compared 0..630 range.");
            return value / 10;
        }
        static int X(byte[] f, int at) { return Coordinate(BitConverter.ToInt16(f, at + 12)); }
        static int Y(byte[] f, int at) { return Coordinate(BitConverter.ToInt16(f, at + 14)); }
        static void Set(byte[] grid, int x, int y, byte value)
        {
            if (x < 0 || x >= 64 || y < 0 || y >= 64) throw new InvalidDataException("Reached route coordinate is outside its grid.");
            grid[x * 64 + y] = value;
        }
        static void BlockCorners(byte[] grid, byte[] fleets, int at)
        {
            int x = BitConverter.ToInt16(fleets, at + 12), y = BitConverter.ToInt16(fleets, at + 14);
            int cx = Coordinate(x), cy = Coordinate(y), right = (x + 9) / 10, bottom = (y + 9) / 10;
            Set(grid, cx, cy, 255); Set(grid, right, cy, 255); Set(grid, cx, bottom, 255); Set(grid, right, bottom, 255);
        }
        static int Distance(byte[] f, int a, int b)
        { return Math.Max(Math.Abs(BitConverter.ToInt16(f, a + 12) - BitConverter.ToInt16(f, b + 12)), Math.Abs(BitConverter.ToInt16(f, a + 14) - BitConverter.ToInt16(f, b + 14))); }
        static byte[] Neighborhood(byte[] grid, int x, int y)
        {
            var result = new byte[9]; int n = 0;
            for (int i = x - 1; i <= x + 1; i++) for (int j = y - 1; j <= y + 1; j++)
                result[n++] = i < 0 || i >= 64 || j < 0 || j >= 64 ? (byte)255 : grid[i * 64 + j];
            return result;
        }

        public static PracticeGoalRouteResult Build(PracticeAiRouteGrid previous, byte[] fleets,
            byte[] secondaryFleets, PracticeAiSharedBacking backing, int sourceClock, int delayWeight, int ownerPolicy)
        {
            if (previous == null || backing == null) throw new ArgumentNullException("previous");
            if (fleets == null || fleets.Length != 123000 || secondaryFleets == null || secondaryFleets.Length != 246000)
                throw new InvalidDataException("Route building requires complete primary and secondary fleet pools.");
            int slot = previous.TaskSlot, actor = slot * 492;
            var task = backing.CopyTaskRecord(slot); int kind = BitConverter.ToInt32(fleets, actor);
            if (BitConverter.ToUInt32(task, 204) != 0x4b47a0u + (uint)actor ||
                BitConverter.ToInt16(fleets, actor + 4) != slot ||
                BitConverter.ToUInt32(task, 208) != previous.NormalizedTableReference)
                throw new InvalidDataException("Route grid, task and fleet identities must agree.");
            if ((kind != 1 && kind != 9) || BitConverter.ToInt16(fleets, actor + 6) != 1 ||
                (ownerPolicy != 0 && ownerPolicy != 2) || BitConverter.ToInt32(task, 0) != 11 ||
                BitConverter.ToInt32(task, 200) != 2 || BitConverter.ToInt32(task, 304) != 1 ||
                BitConverter.ToInt16(fleets, actor + 40) != -1 || task[284] > 1)
                throw new NotSupportedException("Route building admits compared task11/goal2, owner1, kind1/9 navigation contexts only.");
            var grid = new byte[4096]; for (int n = 0; n < grid.Length; n++) grid[n] = 254;
            // Clearing precedes the signed cooldown test. Unreached coordinates
            // and blocker records stay opaque on this return.
            if (task[284] != 0 && BitConverter.ToInt32(task, 4) > sourceClock)
                return Result(previous, grid, backing, PracticeGoalRouteOutcome.CooldownHeld);
            int targetX = Coordinate(BitConverter.ToInt32(task, 296)), targetY = Coordinate(BitConverter.ToInt32(task, 300));
            int actorX = X(fleets, actor), actorY = Y(fleets, actor);
            for (int index = 0; index < 250; index++)
            {
                int at = index * 492, fleetKind = BitConverter.ToInt32(fleets, at);
                if (fleetKind == 2)
                {
                    if (X(fleets, at) == targetX && Y(fleets, at) == targetY)
                    {
                        index &= ~3;
                        if (index > 246) throw new InvalidDataException("Target yard quartet extends outside the supplied fleet pool.");
                        for (int n = 0; n < 4; n++) { at = (index + n) * 492; Set(grid, X(fleets, at), Y(fleets, at), 254); }
                        index += 4; // Source then increments once more at the loop tail.
                    }
                    else Set(grid, X(fleets, at), Y(fleets, at), 255);
                }
                fleetKind = BitConverter.ToInt32(fleets, at);
                if (fleetKind == 11) Set(grid, X(fleets, at), Y(fleets, at), 255);
                if (fleetKind != 1 && fleetKind != 9) continue;
                if (fleets[at + 467] != 0 && Distance(fleets, actor, at) < 20) BlockCorners(grid, fleets, at);
                if (BitConverter.ToInt16(fleets, at + 6) != 1 || at == actor || Distance(fleets, actor, at) >= 20) continue;
                BlockCorners(grid, fleets, at);
                int waypointX = BitConverter.ToInt16(fleets, at + 40);
                if (waypointX != -1) Set(grid, Coordinate(waypointX), Coordinate(BitConverter.ToInt16(fleets, at + 50)), 255);
            }
            for (int n = 0; n < 500; n++)
            { int at = n * 492; if (BitConverter.ToInt32(secondaryFleets, at) == 1) Set(grid, X(secondaryFleets, at), Y(secondaryFleets, at), 255); }
            if (task[284] != 0)
            {
                var near = Neighborhood(grid, actorX, actorY); bool same = true;
                for (int n = 0; n < 9; n++) if (near[n] != task[285 + n]) same = false;
                if (same) return Result(previous, grid, backing, PracticeGoalRouteOutcome.NeighborhoodHeld);
            }
            Set(grid, targetX, targetY, 1);
            // Preserve source scan bounds, order and partial output on success.
            // A generic breadth-first queue need not leave the same grid bytes.
            for (int cost = 1; cost < 128; cost++) for (int x = targetX - cost; x < targetX + cost; x++)
            {
                if (x < 0 || x >= 64) continue;
                for (int y = targetY - cost; y < targetY + cost; y++)
                {
                    if (y < 0 || y >= 64 || grid[x * 64 + y] != cost) continue;
                    if (x == actorX && y == actorY)
                    {
                        task[284] = 0; Put(task, 200, 4);
                        Put(task, 4, ownerPolicy == 2 ? unchecked(sourceClock + delayWeight) : sourceClock);
                        return Result(previous, grid, backing.WithTaskRecord(slot, task), PracticeGoalRouteOutcome.RouteFound);
                    }
                    if (x > 0 && grid[(x - 1) * 64 + y] == 254) grid[(x - 1) * 64 + y] = (byte)(cost + 1);
                    if (y > 0 && grid[x * 64 + y - 1] == 254) grid[x * 64 + y - 1] = (byte)(cost + 1);
                    if (y < 63 && grid[x * 64 + y + 1] == 254) grid[x * 64 + y + 1] = (byte)(cost + 1);
                    if (x < 63 && grid[(x + 1) * 64 + y] == 254) grid[(x + 1) * 64 + y] = (byte)(cost + 1);
                }
            }
            task[284] = 1; Put(task, 4, unchecked(sourceClock + 2000));
            Neighborhood(grid, actorX, actorY).CopyTo(task, 285);
            return Result(previous, grid, backing.WithTaskRecord(slot, task), PracticeGoalRouteOutcome.RouteUnavailable);
        }
        static PracticeGoalRouteResult Result(PracticeAiRouteGrid previous, byte[] grid, PracticeAiSharedBacking backing, PracticeGoalRouteOutcome outcome)
        { return new PracticeGoalRouteResult(new PracticeAiRouteGrid(previous.TaskSlot, grid), backing, outcome); }
    }
}
