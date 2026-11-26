using System;
using System.IO;
using System.Security.Cryptography;

namespace FleetLab
{
    // Explicit study fixtures on the exact617 fleet baseline used by664.
    // The prefix supplies the compared map/history; it is not a running world.
    public sealed class PracticeIdleStudyState
    {
        readonly byte[] initialFleets;
        readonly PracticeAiSharedBacking initialBacking;
        readonly PracticePrephaseIdleResult result;
        public int HistoryFixture { get; private set; }
        public int Profile { get; private set; }
        public int InitialSelectedWaypoint { get; private set; }
        public bool Executed { get { return result != null; } }
        public int RouteUiRequests { get { return result == null ? 0 : result.RouteUiRequests; } }
        public int IdleReturns { get { return result == null ? 0 : result.IdleReturns; } }
        public int Visits { get { return result == null ? 0 : result.Visits; } }
        public int SelectedWaypoint { get { return result == null ? InitialSelectedWaypoint : result.SelectedWaypoint; } }
        public PracticeAiSharedBacking Backing { get { return result == null ? initialBacking : result.Backing; } }
        public byte[] CopyFleetPool() { return result == null ? (byte[])initialFleets.Clone() : result.CopyFleetPool(); }
        public byte[] CopyInitialFleetPool() { return (byte[])initialFleets.Clone(); }
        public PracticeAiSharedBacking InitialBacking { get { return initialBacking; } }

        PracticeIdleStudyState(byte[] fleets, PracticeAiSharedBacking backing, int history, int profile,
                                int selectedWaypoint, PracticePrephaseIdleResult result)
        {
            initialFleets = (byte[])fleets.Clone(); initialBacking = backing;
            HistoryFixture = history; Profile = profile; InitialSelectedWaypoint = selectedWaypoint; this.result = result;
        }

        static string Hash(byte[] bytes)
        { using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-", ""); }
        static void I(byte[] b, int at, int n) { BitConverter.GetBytes(n).CopyTo(b, at); }
        static void W(byte[] b, int at, int n) { BitConverter.GetBytes(unchecked((short)n)).CopyTo(b, at); }

        public static PracticeIdleStudyState Create(string programs, PracticeMapStartup startup,
                                                     int history, int profile, int selectedWaypoint)
        {
            if (startup == null) throw new ArgumentNullException("startup");
            if (startup.Seed != 731) throw new NotSupportedException("The idle study requires the compared BSSB202 seed731 copy.");
            if (history < 0 || history > 1 || profile < 0 || profile > 5 || selectedWaypoint != -1 && selectedWaypoint != 4)
                throw new ArgumentOutOfRangeException("profile", "Choose a compared history, return fixture and selected-waypoint value.");
            var visibility = PracticeOwnerVisibilityStudyState.Create(programs, startup, history, 1, 0, 1, 0, 0, 0).Execute();
            var joined = PracticeVisibleCandidates.Create(programs, startup, visibility);
            var prepared = PracticeFleetMemberPreparation.CreateFromVisibility(programs, startup, joined, 8, 2, false, 0).Advance().Advance().Advance();
            var requests = PracticeFleetMemberRequests.CreateFromPreparation(prepared, 0, 0).Execute();
            var flight = PracticeProducedFlight.Create(PracticeProducedLaunch.Create(programs, requests, 1, 0).Execute()).ToBoundary();
            var fleets = flight.CopyFleetPool();
            string expected = history == 0 ? "7E9AE19E305EA03AA4E39E7CD82FE43A7D28D0523C850B203ADB5554D2BEA588" : "03D86BE50CA3583763016C44941898B51C0514256C5B75018F429B7F014BC316";
            if (Hash(fleets) != expected) throw new NotSupportedException("The copied fleet baseline differs from the original664 comparison.");
            var region = new byte[PracticeAiSharedBacking.RegionBytes];
            for (int g = 0; g < 250; g++) PracticeFleetServiceContext.CreateDiagnostic(g).CopyTo(region, g * 312);
            for (int g = 0; g < 250; g++)
            {
                bool on = profile == 1 && g == 16 || profile == 2 && g == 249 || profile == 3 && (g == 16 || g == 249) || profile == 4 || profile == 5 && (g == 16 || g == 249);
                if (!on) continue; int at = g * 492, ta = g * 312;
                I(fleets, at + 472, profile == 1 || profile == 3 && g == 16 || profile == 5 ? 5 : 6);
                W(fleets, at + 488, profile == 5 ? (g == 16 ? 249 : 16) : 0);
                for (int n = 0; n < 5; n++) { W(fleets, at + 40 + n * 2, n == 0 ? -1 : 100 + n * 10); W(fleets, at + 50 + n * 2, 200 + n * 10); }
                fleets[at + 482] = 255; I(region, ta + 304, 2);
                I(region, ta + 28, profile == 4 ? 550 + g : 550); I(region, ta + 32, profile == 4 ? 290 - g : 290);
            }
            return new PracticeIdleStudyState(fleets, new PracticeAiSharedBacking(region), history, profile, selectedWaypoint, null);
        }

        public PracticeIdleStudyState Execute()
        {
            if (Executed) throw new InvalidOperationException("This copied pass is complete. Reset or load another fixture.");
            var next = PracticePrephaseIdle.Evaluate(initialFleets, initialBacking, 0, InitialSelectedWaypoint);
            return new PracticeIdleStudyState(initialFleets, initialBacking, HistoryFixture, Profile, InitialSelectedWaypoint, next);
        }
        public PracticeIdleStudyState Reset()
        { return new PracticeIdleStudyState(initialFleets, initialBacking, HistoryFixture, Profile, InitialSelectedWaypoint, null); }

        public bool TryReturnDestination(int slot, out int x, out int y)
        {
            if (slot < 0 || slot >= 250) throw new ArgumentOutOfRangeException("slot");
            int at = slot * 492, state = BitConverter.ToInt32(initialFleets, at + 472); x = y = 0;
            if (BitConverter.ToInt16(initialFleets, at + 40) != -1 || state != 5 && state != 6) return false;
            if (state == 5)
            {
                int home = BitConverter.ToInt16(initialFleets, at + 488);
                x = BitConverter.ToInt16(initialFleets, home * 492 + 12); y = BitConverter.ToInt16(initialFleets, home * 492 + 14);
            }
            else { var task = initialBacking.CopyTaskRecord(slot); x = BitConverter.ToInt32(task, 28); y = BitConverter.ToInt32(task, 32); }
            return true;
        }
    }
}
