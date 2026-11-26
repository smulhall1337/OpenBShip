using System;
using System.IO;
namespace FleetLab
{
    internal static class PracticeIdleStudyStateChecks
    {
        static void Need(bool ok, string text) { PracticeIdleReturnChecks.Need(ok, text); }
        static string Hash(byte[] b) { return PracticeIdleReturnChecks.Hash(b); }
        static bool Reject(Action a)
        { try { a(); return false; } catch (ArgumentException) { return true; } catch (InvalidDataException) { return true; } catch (InvalidOperationException) { return true; } catch (NotSupportedException) { return true; } }
        internal static void Run(Action<string, Action> check, string assets)
        {
            string programs = Path.Combine(Path.GetDirectoryName(assets), "Programs"); var source = File.ReadAllBytes(Path.Combine(assets, "Graphics", "BSSB202.SCN"));
            var startup = PracticeMapStartup.CreateBssb202(programs, source, 731);
            check("Observable idle factory matches all24 original664 before and after complete golden states", delegate
            {
                int count = 0;
                foreach (string line in PracticePrephaseIdleChecks.OriginalRows)
                {
                    var r = line.Split(','); var before = PracticeIdleStudyState.Create(programs, startup, int.Parse(r[1]), int.Parse(r[2]), int.Parse(r[3])); var after = before.Execute();
                    Need(int.Parse(r[0]) == count && Hash(before.CopyFleetPool()) == r[6] && Hash(before.Backing.CopyRegion()) == r[8] && Hash(after.CopyFleetPool()) == r[7] && Hash(after.Backing.CopyRegion()) == r[9] && after.SelectedWaypoint == int.Parse(r[4]) && after.RouteUiRequests == int.Parse(r[5]) && after.Visits == 250, "Original664 observable factory " + count); count++;
                }
                Need(count == 24, "664 factory case count");
            });
            check("Idle study executes once resets its applied copy and previews source destinations without movement", delegate
            {
                var before = PracticeIdleStudyState.Create(programs, startup, 0, 1, 4); string f = Hash(before.CopyFleetPool()), b = Hash(before.Backing.CopyRegion()); int x, y;
                Need(before.TryReturnDestination(16, out x, out y) && x == BitConverter.ToInt16(before.CopyFleetPool(), 12) && y == BitConverter.ToInt16(before.CopyFleetPool(), 14), "Referenced destination preview");
                var after = before.Execute(); var reset = after.Reset(); var pool = after.CopyFleetPool(); pool[0] ^= 255;
                Need(after.Executed && after.IdleReturns == 1 && after.RouteUiRequests == 1 && after.SelectedWaypoint == -1 && before.SelectedWaypoint == 4 && !before.Executed && Hash(before.CopyFleetPool()) == f && Hash(before.Backing.CopyRegion()) == b && !reset.Executed && reset.Profile == 1 && reset.HistoryFixture == 0 && reset.InitialSelectedWaypoint == 4 && Hash(reset.CopyFleetPool()) == f && Hash(reset.Backing.CopyRegion()) == b && pool[0] != after.CopyFleetPool()[0], "Applied state/reset/owned snapshots");
                Need(BitConverter.ToInt16(before.CopyFleetPool(), 16 * 492 + 12) == BitConverter.ToInt16(after.CopyFleetPool(), 16 * 492 + 12) && Reject(delegate { after.Execute(); }), "Study moved an origin or admitted a second pass");
                var none = PracticeIdleStudyState.Create(programs, startup, 0, 0, 4); Need(!none.TryReturnDestination(16, out x, out y) && none.Execute().SelectedWaypoint == 4, "No-idle preview/selection");
            });
            check("Idle factory rejects unsupported seed history profile waypoint and view bounds", delegate
            {
                Need(Reject(delegate { PracticeIdleStudyState.Create(programs, null, 0, 1, 4); }) && Reject(delegate { PracticeIdleStudyState.Create(programs, startup, 2, 1, 4); }) && Reject(delegate { PracticeIdleStudyState.Create(programs, startup, 0, 6, 4); }) && Reject(delegate { PracticeIdleStudyState.Create(programs, startup, 0, 1, 0); }), "Uncompared fixture admitted");
                var other = PracticeMapStartup.CreateBssb202(programs, source, 732); Need(Reject(delegate { PracticeIdleStudyState.Create(programs, other, 0, 1, 4); }), "Uncompared seed admitted");
                var state = PracticeIdleStudyState.Create(programs, startup, 0, 1, 4); Need(Reject(delegate { int x, y; state.TryReturnDestination(250, out x, out y); }), "View index outside pool");
            });
        }
    }
}
