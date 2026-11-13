using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FleetLab {
    public struct PracticeFleetOrigin {
        public readonly int Kind;
        public readonly Point Origin;
        public PracticeFleetOrigin(int kind, Point origin) { Kind = kind; Origin = origin; }
    }

    // Bs.exe B9A8...6A569, 403E20. The source retries forever; this adapter
    // rejects an exhausted zone and bounds attempts without committing failed
    // random draws. Supplied seeds do not reproduce earlier mission RNG use.
    public sealed class PracticeSpawnPlanner {
        readonly byte[] kinds = new byte[4096], owners = new byte[4096];
        readonly List<PracticeFleetOrigin> fleets = new List<PracticeFleetOrigin>();
        NativeRandom random;
        public uint RandomState { get { return random.State; } }
        public int RandomCalls { get { return random.Calls; } }

        public PracticeSpawnPlanner(Scenario scene, uint seed,
                                    IEnumerable<PracticeFleetOrigin> initialFleets) {
            if (scene == null) throw new ArgumentNullException("scene");
            if (scene.Cells == null || scene.Cells.Length != 4096)
                throw new InvalidDataException("A deployment map must contain 64 by 64 cells.");
            for (int i = 0; i < 4096; i++) {
                if (scene.Cells[i] == null) throw new InvalidDataException("Missing deployment map cell.");
                kinds[i] = scene.Cells[i].Kind;
                owners[i] = scene.Cells[i].Value;
            }
            random = new NativeRandom(seed);
            if (initialFleets != null)
                foreach (var fleet in initialFleets) Reserve(fleet.Kind, fleet.Origin);
        }

        public void Reserve(int kind, Point origin) {
            if (fleets.Count == PracticeFleetSnapshot.FleetSlots)
                throw new InvalidDataException("Deployment exceeds 250 fleet records.");
            if (origin.X < short.MinValue || origin.X > short.MaxValue ||
                origin.Y < short.MinValue || origin.Y > short.MaxValue)
                throw new ArgumentOutOfRangeException("origin");
            fleets.Add(new PracticeFleetOrigin(kind, origin));
        }

        bool Occupied(Point point) {
            foreach (var fleet in fleets)
                if (fleet.Kind != 0 && fleet.Origin == point) return true;
            return false;
        }

        bool Eligible(int x, int y, int owner) {
            int at = y * 64 + x;
            return kinds[at] == 4 && unchecked((sbyte)owners[at]) == owner &&
                   !Occupied(new Point(x * 10, y * 10));
        }

        public Point NextOrigin(int owner) { return NextOrigin(owner, 50000); }

        public Point NextOrigin(int owner, int attemptLimit) {
            if (owner < 0 || owner > 3) throw new ArgumentOutOfRangeException("owner");
            if (attemptLimit < 1 || attemptLimit > 50000)
                throw new ArgumentOutOfRangeException("attemptLimit");
            bool available = false;
            for (int x = 0; x < 64 && !available; x++)
                for (int y = 0; y < 64 && !available; y++) available = Eligible(x, y, owner);
            if (!available)
                throw new InvalidDataException("No free owner-tagged kind-4 deployment cells for player " + (owner + 1) + ".");
            var candidate = random.Copy();
            for (int attempt = 0; attempt < attemptLimit; attempt++) {
                int x = (candidate.Next() % 640) / 10;
                int y = (candidate.Next() % 640) / 10;
                if (!Eligible(x, y, owner)) continue;
                random = candidate;
                // The source routine does not reserve its result. Its caller
                // installs the fleet; keep that step explicit here too.
                return new Point(x * 10, y * 10);
            }
            throw new InvalidDataException("Deployment random-search limit reached; seed and reservations were preserved.");
        }
    }

    // Bounded, fresh-pool subset of 4037B0: grouped records only, kinds 1/9.
    // Embedded terrain entities, earlier startup, local-player UI and mission
    // simulation are not executed. Input Scenario data is never consumed.
    public sealed class PracticeDeployment {
        readonly byte[] ships = new byte[PracticeFleetSnapshot.ShipSlots * PracticeUnitCatalog.RecordBytes];
        readonly byte[] fleetPool = new byte[PracticeFleetSnapshot.FleetSlots * PracticeFleetSnapshot.FleetBytes];
        readonly uint[] counters = { 1, 1, 1, 1, 1 };
        readonly ReadOnlyCollection<PracticeFleetSnapshot> fleets;
        public ReadOnlyCollection<PracticeFleetSnapshot> Fleets { get { return fleets; } }
        public uint Seed { get; private set; }
        public uint RandomState { get; private set; }
        public int RandomCalls { get; private set; }
        public int UnitCount { get; private set; }

        public PracticeDeployment(PracticeUnitCatalog catalog, Scenario scene, uint seed) {
            if (catalog == null) throw new ArgumentNullException("catalog");
            if (scene == null) throw new ArgumentNullException("scene");
            if (scene.Players < 1 || scene.Players > 4 || scene.Groups == null ||
                scene.Groups.Length != scene.Players)
                throw new InvalidDataException("Unsupported deployment player groups.");
            Seed = seed;
            var planner = new PracticeSpawnPlanner(scene, seed, null);
            var records = new List<PracticeFleetSnapshot>();
            for (int owner = 0; owner < scene.Players; owner++) {
                if (scene.Groups[owner] == null || scene.Groups[owner].Length != 2)
                    throw new InvalidDataException("A player must contain two deployment groups.");
                for (int group = 0; group < 2; group++) {
                    var inputs = scene.Groups[owner][group];
                    if (inputs == null || inputs.Count > 5)
                        throw new InvalidDataException("Unsupported deployment group size (maximum five records).");
                    foreach (byte[] input in inputs) {
                        int kind = group == 0 ? 1 : 9;
                        Point origin = planner.NextOrigin(owner);
                        var fleet = PracticeFleetSnapshot.CreateDeployment(catalog, input, owner,
                            origin, counters[owner + 1], kind, records.Count, UnitCount);
                        byte[] pool = fleet.CopyShipPool();
                        Array.Copy(pool, UnitCount * PracticeUnitCatalog.RecordBytes, ships,
                                   UnitCount * PracticeUnitCatalog.RecordBytes,
                                   fleet.Units.Count * PracticeUnitCatalog.RecordBytes);
                        Array.Copy(fleet.CopyFleetRecord(), 0, fleetPool,
                                   records.Count * PracticeFleetSnapshot.FleetBytes,
                                   PracticeFleetSnapshot.FleetBytes);
                        UnitCount += fleet.Units.Count;
                        counters[owner + 1] = fleet.NextCounter;
                        records.Add(fleet);
                        planner.Reserve(kind, origin);
                    }
                }
            }
            fleets = records.AsReadOnly();
            RandomState = planner.RandomState;
            RandomCalls = planner.RandomCalls;
        }

        // Five original counters at 5C3360; index0 belongs to owner -1.
        public uint OwnerCounter(int owner) {
            if (owner < -1 || owner > 3) throw new ArgumentOutOfRangeException("owner");
            return counters[owner + 1];
        }
        public byte[] CopyShipPool() { return (byte[])ships.Clone(); }
        public byte[] CopyFleetPool() { return (byte[])fleetPool.Clone(); }
    }
}
