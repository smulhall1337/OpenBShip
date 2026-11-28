using System;
using System.IO;
using System.Security.Cryptography;

namespace FleetLab
{
    // Original1103 / independent1104 / fresh1107: bank5 deliberately applied
    // to the complete copied BSSB202 primary pools. This is not natural map
    // selection, a generic save reader, or a live AI/runtime initializer.
    internal sealed class PracticeReinforcementMapBasis
    {
        internal readonly byte[] Ships, Fleets, Terrain;
        internal readonly uint[] Counters;
        internal readonly uint StartupSeed, RandomState, NextFleet;
        internal readonly int Profile, StartupRandomCalls;
        internal const int FleetCount = 29, UnitCount = 63;

        internal static readonly string[] ProfileNames = {
            "Original occupied map copy",
            "Bay reservation only (copied markers changed)",
            "Adjacent 22/23 rule only (copied bay links cleared)",
            "Reserve first free map fleet 29 (copy)"
        };

        PracticeReinforcementMapBasis(PracticeMapStartup startup, int profile)
        {
            var final = startup.Stages[3];
            StartupSeed = startup.Seed;
            RandomState = final.RandomState;
            StartupRandomCalls = final.RandomCalls;
            // The late aircraft pass adds six fleets without advancing the
            // source's cached next index. Allocation itself searches afresh.
            NextFleet = (uint)startup.Stages[2].NextInactiveFleet;
            Ships = final.CopyShipPool();
            Fleets = final.CopyFleetPool();
            Terrain = startup.CopyTerrainPool();
            // Startup includes the neutral owner at index0. Reinforcement
            // owner0 starts at source5C3364, one word after that prefix.
            var startupCounters = final.CopyOwnerCounters();
            if (startupCounters.Length != 5 || startupCounters[0] != 1)
                throw new InvalidDataException("Unexpected neutral-owner counter prefix in recovered map startup.");
            Counters = new uint[4];
            Array.Copy(startupCounters, 1, Counters, 0, 4);
            Profile = profile;

            int map = StartupSeed == 731 ? 0 : 1;
            string[] shipHashes = {
                "84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49",
                "DB098304790C12CD6C96A461EA324804301AE73258753A566DFEF1396657FAB6"
            };
            string[] fleetHashes = {
                "8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D",
                "2EC191C698C4070D3D51C096A4DAE08F5376C86152C30C5F68088D30BEA1A747"
            };
            if (Hash(Ships) != shipHashes[map] || Hash(Fleets) != fleetHashes[map] ||
                Hash(Terrain) != "607BCDF7530CF324EF32EA2B3A86A50A4AC41D86548A95CD26707A80828894FC" ||
                NextFleet != 23 || final.NonemptyFleets != FleetCount || final.NonemptyUnits != UnitCount ||
                RandomState != (map == 0 ? 249622383u : 1349664646u) ||
                Counters.Length != 4 || Counters[0] != 3 || Counters[1] != 6 || Counters[2] != 1 || Counters[3] != 1)
                throw new InvalidDataException("Recovered map basis differs from the original occupied-map comparison.");

            // These are named diagnostic mutations of owned arrays, never
            // changes to the SCN, templates, or caller's startup object.
            if (profile == 1)
                for (int unit = 0; unit < 1500; unit++)
                {
                    int at = unit * 268, kind = Read(Ships, at);
                    if (kind == 22 || kind == 23) Write(Ships, at, 24);
                }
            if (profile == 2)
                for (int fleet = 0; fleet < 250; fleet++)
                {
                    int at = fleet * 492, kind = Read(Fleets, at);
                    if (kind != 2 && kind != 10) continue;
                    for (int member = 0; member < 6; member++)
                    {
                        int link = at + 16 + member * 4;
                        uint reference = BitConverter.ToUInt32(Fleets, link);
                        if (reference == 0) continue;
                        int offset = UnitOffset(reference);
                        if (Read(Ships, offset) == 0) Write(Fleets, link, 0);
                    }
                }
            if (profile == 3) Fleets[29 * 492 + 490] = 1;
        }

        internal static PracticeReinforcementMapBasis Create(string programs, byte[] map, uint seed, int profile)
        {
            if (seed != 0 && seed != 731)
                throw new ArgumentOutOfRangeException("seed", "Occupied-map comparison admits startup seeds 0 and 731 only.");
            if (profile < 0 || profile >= ProfileNames.Length) throw new ArgumentOutOfRangeException("profile");
            return new PracticeReinforcementMapBasis(PracticeMapStartup.CreateBssb202(programs, map, seed), profile);
        }

        internal static int UnitOffset(uint reference)
        {
            long offset = (long)reference - 0x4fc340;
            if (offset < 0 || offset >= 402000 || offset % 268 != 0)
                throw new InvalidDataException("Invalid copied bay unit reference.");
            return (int)offset;
        }

        static int Read(byte[] bytes, int at) { return BitConverter.ToInt32(bytes, at); }
        static void Write(byte[] bytes, int at, int value) { BitConverter.GetBytes(value).CopyTo(bytes, at); }
        static string Hash(byte[] bytes)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "");
        }
    }
}
