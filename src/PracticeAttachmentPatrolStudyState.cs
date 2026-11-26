using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace FleetLab
{
    // Reproducible supplied fixtures on the compared original617 normal boundary.
    // This basis is immutable; it does not run an AI world or advance its clock.
    public sealed class PracticeAttachmentPatrolStudyBasis
    {
        readonly byte[] ships, fleets, orders;
        readonly PracticeAttachmentPatrolTables tables;
        PracticeAttachmentPatrolStudyBasis(byte[] s, byte[] f, byte[] o, PracticeAttachmentPatrolTables t)
        { ships = (byte[])s.Clone(); fleets = (byte[])f.Clone(); orders = (byte[])o.Clone(); tables = t; }
        static string Hash(byte[] b)
        { using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(b)).Replace("-", ""); }
        internal static void I(byte[] b, int at, int n) { BitConverter.GetBytes(n).CopyTo(b, at); }
        internal static void W(byte[] b, int at, int n) { BitConverter.GetBytes(unchecked((short)n)).CopyTo(b, at); }
        public static PracticeAttachmentPatrolStudyBasis Create(string programs, PracticeMapStartup startup)
        {
            if (startup == null) throw new ArgumentNullException("startup");
            if (startup.Seed != 731) throw new NotSupportedException("Patrol fixtures require the compared BSSB202 seed731 copy.");
            var visibility = PracticeOwnerVisibilityStudyState.Create(programs, startup, 0, 1, 0, 1, 0, 0, 0).Execute();
            var joined = PracticeVisibleCandidates.Create(programs, startup, visibility);
            var prepared = PracticeFleetMemberPreparation.CreateFromVisibility(programs, startup, joined, 8, 2, false, 0).Advance().Advance().Advance();
            var requests = PracticeFleetMemberRequests.CreateFromPreparation(prepared, 0, 0).Execute();
            var flight = PracticeProducedFlight.Create(PracticeProducedLaunch.Create(programs, requests, 1, 0).Execute()).ToBoundary();
            var s = flight.CopyShipPool(); var f = flight.CopyFleetPool(); var o = flight.CopyOrderPool();
            if (Hash(s) != "F1C676D0082F18F385B016E60E137D4CDE2F8B2FEE0D522E10D53952026484B6" || Hash(f) != "7E9AE19E305EA03AA4E39E7CD82FE43A7D28D0523C850B203ADB5554D2BEA588" || Hash(o) != "3332BE7B5EB6EBA46055A3E2725F902CA79258BE2B5B46FB6DC4B0420E2504B1")
                throw new NotSupportedException("Copied patrol basis differs from the original681 comparison.");
            return new PracticeAttachmentPatrolStudyBasis(s, f, o, PracticeAttachmentPatrolTables.Load(programs));
        }
        public PracticeAttachmentPatrolStudyState CreateFixture(int parentFleet, bool active, int timer, int rawDelta,
            int phase, bool existingWaypoint, int coordinates, bool extraCarrier, bool stateOne)
        {
            if (parentFleet != 15 && parentFleet != 17 || coordinates < 0 || coordinates > 2 || phase < -1 || phase > 4)
                throw new ArgumentOutOfRangeException("parentFleet", "Choose a compared parent, coordinate profile and phase.");
            if (!active && (phase != 0 || existingWaypoint || stateOne))
                throw new NotSupportedException("Stored fixtures use phase0, no supplied waypoint and no state1 override.");
            var s = (byte[])ships.Clone(); var f = (byte[])fleets.Clone(); var o = (byte[])orders.Clone();
            int pa = parentFleet * 492, unit = parentFleet == 15 ? 27 : 32, owner = parentFleet == 15 ? 0 : 1;
            int[] children = parentFleet == 15 ? new[] { 23, 24, 25, 26 } : new[] { 27, 28 };
            for (int g = 0; g < 250; g++) f[g * 492 + 467] = 0;
            f[pa + 486] = 1; Array.Clear(f, pa + 16, 24);
            if (extraCarrier)
            {
                Array.Copy(s, unit * 268, s, 1499 * 268, 268); W(s, 1499 * 268 + 4, 1499);
                Array.Clear(s, 1499 * 268 + 56, 16); I(f, pa + 16, 0x4fc340 + 1499 * 268);
            }
            I(f, pa + 16 + (extraCarrier ? 4 : 0), 0x4fc340 + unit * 268);
            if (coordinates == 1) { W(f, pa + 12, owner == 0 ? 0 : 630); W(f, pa + 14, owner == 0 ? 630 : 0); }
            if (coordinates == 2) { W(f, pa + 12, 32760); W(f, pa + 14, -32760); }
            var r = new byte[78560];
            for (int g = 0; g < 250; g++) PracticeFleetServiceContext.CreateDiagnostic(g).CopyTo(r, g * 312);
            if (active) foreach (int child in children)
            {
                var transition = PracticeAircraftTransitions.ApplyOffscreen(tables.Aircraft, tables.Units, s, f, o, child, 4, 249, 4, -2);
                s = transition.CopyShipPool(); f = transition.CopyFleetPool(); o = transition.CopyPrimaryOrders();
            }
            foreach (int child in children)
            {
                int ca = child * 492, ta = child * 312;
                if (active)
                {
                    f[ca + 486] = 1; I(f, ca + 472, stateOne ? 1 : 0); W(f, ca + 40, existingWaypoint ? 700 : -1); W(f, ca + 50, existingWaypoint ? -20 : 290);
                    for (int n = 1; n < 5; n++) W(f, ca + 40 + n * 2, 100 + n * 10);
                }
                I(r, ta + 200, phase); I(r, ta + 252, timer);
                for (int n = 0; n < 6; n++) I(r, ta + 212 + n * 4, 1000 + child * 10 + n);
            }
            var c = new byte[1184]; for (int n = 0; n < c.Length; n++) c[n] = 0xa7;
            for (int n = 0; n < 248; n++) I(c, 176 + n * 4, unchecked(0x51a00000 + n));
            return new PracticeAttachmentPatrolStudyState(tables, s, f, o, new PracticeAiSharedBacking(r), c, parentFleet,
                active, timer, rawDelta, phase, existingWaypoint, coordinates, extraCarrier, stateOne, children, null);
        }
    }

    public sealed class PracticeAttachmentPatrolStudyState
    {
        readonly PracticeAttachmentPatrolTables tables;
        readonly byte[] ships, fleets, orders, candidates;
        readonly PracticeAiSharedBacking backing;
        readonly PracticeAttachmentPatrolResult result;
        readonly PracticePrephasePatrolResult wholeResult;
        readonly int[] children;
        public int ParentFleet { get; private set; }
        public bool InitiallyActive { get; private set; }
        public int Timer { get; private set; }
        public int RawDelta { get; private set; }
        public int InitialPhase { get; private set; }
        public bool ExistingWaypoint { get; private set; }
        public int Coordinates { get; private set; }
        public bool ExtraCarrier { get; private set; }
        public bool StateOne { get; private set; }
        public bool WholePass { get; private set; }
        public int IdleProfile { get; private set; }
        public bool Executed { get { return result != null || wholeResult != null; } }
        public int RouteUiRequests { get { return wholeResult != null ? wholeResult.RouteUiRequests : result == null ? 0 : result.RouteUiRequests; } }
        public int SelectedWaypoint { get { return wholeResult != null ? wholeResult.SelectedWaypoint : result == null ? 4 : result.SelectedWaypoint; } }
        public int StoppedAtStateOne { get { return wholeResult != null ? wholeResult.StateOneStops.Count == 0 ? -1 : wholeResult.StateOneStops[0] : result == null ? -1 : result.StoppedAtStateOne; } }
        public int Visits { get { return wholeResult == null ? 0 : wholeResult.Visits; } }
        public int PolicyVisits { get { return wholeResult == null ? 0 : wholeResult.PolicyVisits; } }
        public int IdleReturns { get { return wholeResult == null ? 0 : wholeResult.IdleReturns; } }
        public int CompletedGoals { get { return wholeResult == null ? 0 : wholeResult.CompletedGoals; } }
        public int ActiveWaiting { get { return wholeResult == null ? 0 : wholeResult.ActiveWaiting; } }
        public ReadOnlyCollection<int> Children { get { return Array.AsReadOnly((int[])children.Clone()); } }
        public ReadOnlyCollection<int> VisitedAircraft { get { return wholeResult != null ? wholeResult.VisitedAircraft : result == null ? Array.AsReadOnly(new int[0]) : result.VisitedAircraft; } }
        public ReadOnlyCollection<int> ActivatedAircraft { get { return wholeResult != null ? wholeResult.ActivatedAircraft : result == null ? Array.AsReadOnly(new int[0]) : result.ActivatedAircraft; } }
        public PracticeAiSharedBacking Backing { get { return wholeResult != null ? wholeResult.Backing : result == null ? backing : result.Backing; } }
        public byte[] CopyShipPool() { return wholeResult != null ? wholeResult.CopyShipPool() : result == null ? (byte[])ships.Clone() : result.CopyShipPool(); }
        public byte[] CopyFleetPool() { return wholeResult != null ? wholeResult.CopyFleetPool() : result == null ? (byte[])fleets.Clone() : result.CopyFleetPool(); }
        public byte[] CopyPrimaryOrders() { return wholeResult != null ? wholeResult.CopyPrimaryOrders() : result == null ? (byte[])orders.Clone() : result.CopyPrimaryOrders(); }
        public byte[] CopyCandidateRegion() { return wholeResult != null ? wholeResult.CopyCandidateRegion() : result == null ? (byte[])candidates.Clone() : result.CopyCandidateRegion(); }
        internal PracticeAttachmentPatrolStudyState(PracticeAttachmentPatrolTables t, byte[] s, byte[] f, byte[] o,
            PracticeAiSharedBacking b, byte[] c, int parent, bool active, int timer, int delta, int phase,
            bool waypoint, int coordinates, bool extra, bool one, int[] children, PracticeAttachmentPatrolResult result,
            bool wholePass = false, int idleProfile = 0, PracticePrephasePatrolResult wholeResult = null)
        {
            tables = t; ships = (byte[])s.Clone(); fleets = (byte[])f.Clone(); orders = (byte[])o.Clone(); backing = b; candidates = (byte[])c.Clone();
            ParentFleet = parent; InitiallyActive = active; Timer = timer; RawDelta = delta; InitialPhase = phase;
            ExistingWaypoint = waypoint; Coordinates = coordinates; ExtraCarrier = extra; StateOne = one; this.children = (int[])children.Clone(); this.result = result;
            WholePass = wholePass; IdleProfile = idleProfile; this.wholeResult = wholeResult;
        }
        PracticeAttachmentPatrolStudyState With(PracticeAttachmentPatrolResult next, PracticePrephasePatrolResult whole = null)
        { return new PracticeAttachmentPatrolStudyState(tables, ships, fleets, orders, backing, candidates, ParentFleet, InitiallyActive, Timer, RawDelta, InitialPhase, ExistingWaypoint, Coordinates, ExtraCarrier, StateOne, children, next, WholePass, IdleProfile, whole); }
        public PracticeAttachmentPatrolStudyState ForWholePass(int idleProfile)
        {
            if (Executed || WholePass) throw new InvalidOperationException("Choose whole-pass scope on a fresh leaf fixture before execution.");
            if (idleProfile < 0 || idleProfile > 2) throw new ArgumentOutOfRangeException("idleProfile");
            var f = (byte[])fleets.Clone(); var r = backing.CopyRegion();
            for (int g = 0; g < 250; g++) f[g * 492 + 482] = 255;
            for (int n = 0; n < idleProfile; n++)
            {
                int idle = n == 0 ? 16 : 249, at = idle * 492, ta = idle * 312;
                PracticeAttachmentPatrolStudyBasis.I(f, at + 472, n == 0 ? 5 : 6); PracticeAttachmentPatrolStudyBasis.W(f, at + 488, 0);
                for (int q = 0; q < 5; q++) { PracticeAttachmentPatrolStudyBasis.W(f, at + 40 + q * 2, q == 0 ? -1 : 100 + q * 10); PracticeAttachmentPatrolStudyBasis.W(f, at + 50 + q * 2, 200 + q * 10); }
                PracticeAttachmentPatrolStudyBasis.I(r, ta + 28, 550); PracticeAttachmentPatrolStudyBasis.I(r, ta + 32, 290); PracticeAttachmentPatrolStudyBasis.I(r, ta + 304, 2);
            }
            return new PracticeAttachmentPatrolStudyState(tables, ships, f, orders, new PracticeAiSharedBacking(r), candidates,
                ParentFleet, InitiallyActive, Timer, RawDelta, InitialPhase, ExistingWaypoint, Coordinates, ExtraCarrier, StateOne, children, null, true, idleProfile);
        }
        public PracticeAttachmentPatrolStudyState Execute()
        {
            if (Executed) throw new InvalidOperationException("The copied policy visit is complete. Reset to repeat.");
            if (WholePass) return With(null, PracticePrephasePatrol.EvaluateNoTargets(tables, ships, fleets, orders, backing, candidates, RawDelta, 0, 4));
            return With(PracticeAttachmentPatrol.EvaluateNoTargets(tables, ships, fleets, orders, backing, candidates, ParentFleet, RawDelta, 4));
        }
        public PracticeAttachmentPatrolStudyState Reset() { return With(null); }
    }
}
