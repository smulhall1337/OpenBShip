using System;
using System.IO;
using System.Security.Cryptography;

namespace FleetLab
{
    internal static class PracticePrephaseIdleChecks
    {
        internal static System.Collections.Generic.IEnumerable<string> OriginalRows
        { get { foreach (string row in Original) yield return row; } }
        // Independent whole-caller original664 pool hashes; supplied fixtures.
        static readonly string[] Original = { 
            "0,0,0,-1,-1,0,7E9AE19E305EA03AA4E39E7CD82FE43A7D28D0523C850B203ADB5554D2BEA588,7E9AE19E305EA03AA4E39E7CD82FE43A7D28D0523C850B203ADB5554D2BEA588,2690D8E814AB23CB8FBBDEFED9AAE4D141336017724D90C71C78D1A85C4B6C84,2690D8E814AB23CB8FBBDEFED9AAE4D141336017724D90C71C78D1A85C4B6C84",
            "1,0,0,4,4,0,7E9AE19E305EA03AA4E39E7CD82FE43A7D28D0523C850B203ADB5554D2BEA588,7E9AE19E305EA03AA4E39E7CD82FE43A7D28D0523C850B203ADB5554D2BEA588,2690D8E814AB23CB8FBBDEFED9AAE4D141336017724D90C71C78D1A85C4B6C84,2690D8E814AB23CB8FBBDEFED9AAE4D141336017724D90C71C78D1A85C4B6C84",
            "2,0,1,-1,-1,1,87B7DB6019D1B5F9289CC92A15BA26ED92E3337E082676DE73353C01F3D126CC,54330EA51FEE724777DD7D0C0B37E9484B7D1139007E77B99AA83097C3ACD70E,D75B38CFB96AFEC274FDBA8D6B3472D746C14E785991E6557178E197DBC3EBD4,66F1E8AFA5F80E551CF76270DC8504EC3F87BEF2AB454CBEFAE8C56E233DEEFE",
            "3,0,1,4,-1,1,87B7DB6019D1B5F9289CC92A15BA26ED92E3337E082676DE73353C01F3D126CC,54330EA51FEE724777DD7D0C0B37E9484B7D1139007E77B99AA83097C3ACD70E,D75B38CFB96AFEC274FDBA8D6B3472D746C14E785991E6557178E197DBC3EBD4,66F1E8AFA5F80E551CF76270DC8504EC3F87BEF2AB454CBEFAE8C56E233DEEFE",
            "4,0,2,-1,-1,1,CD32CF2D6A43E0F36A90D3EBD427B4327CBFB4B8A865F7704D33D0A1643DA79C,502141EAA01700E7AED9C38ECFDA20B64F10A95CDA74E0D2F777AB7211972C29,29D9E0BAF301F056F37280488E87721D48BA793B87B3AA9D22AB33A8B58C394A,5442A7029CB64106F45B4BEBEA1792775889C8B380BBE2D7CF17FD3F1E87F0F0",
            "5,0,2,4,-1,1,CD32CF2D6A43E0F36A90D3EBD427B4327CBFB4B8A865F7704D33D0A1643DA79C,502141EAA01700E7AED9C38ECFDA20B64F10A95CDA74E0D2F777AB7211972C29,29D9E0BAF301F056F37280488E87721D48BA793B87B3AA9D22AB33A8B58C394A,5442A7029CB64106F45B4BEBEA1792775889C8B380BBE2D7CF17FD3F1E87F0F0",
            "6,0,3,-1,-1,2,64610BF2749C7FE8998ED75CE0B2005323466F283460BE85FBD45465AE654A36,BE3697ABB16A70970A173E1B9CA4AC42DECF12BD7B54C0562C28AAD4906155BC,AB70A06E9FE50C9BDA7352F64145A3EC3C59683B1F19DA35115D50F3038DD44C,EC0000D4EBCCAEB4E07C3B856AE958702A08678A7763535604EA3D98AA39A008",
            "7,0,3,4,-1,2,64610BF2749C7FE8998ED75CE0B2005323466F283460BE85FBD45465AE654A36,BE3697ABB16A70970A173E1B9CA4AC42DECF12BD7B54C0562C28AAD4906155BC,AB70A06E9FE50C9BDA7352F64145A3EC3C59683B1F19DA35115D50F3038DD44C,EC0000D4EBCCAEB4E07C3B856AE958702A08678A7763535604EA3D98AA39A008",
            "8,0,4,-1,-1,250,D8591D32B6B9E48F6C14A19187D47D2D79A09F3614CA749080FE0ED7F1E6D524,CA74EBD1C5D208C9033AD2E75D14B27B9FE5D435A35DEE1E88C5A10AADC37142,95699931F6412E552E4CE0C170F65222071113AB4772F6F91C11CE271300E3A4,5B801EEC90D5067F8EC271C8A028330B44B2FBB3E72007C4D74DC160731E61E6",
            "9,0,4,4,-1,250,D8591D32B6B9E48F6C14A19187D47D2D79A09F3614CA749080FE0ED7F1E6D524,CA74EBD1C5D208C9033AD2E75D14B27B9FE5D435A35DEE1E88C5A10AADC37142,95699931F6412E552E4CE0C170F65222071113AB4772F6F91C11CE271300E3A4,5B801EEC90D5067F8EC271C8A028330B44B2FBB3E72007C4D74DC160731E61E6",
            "10,0,5,-1,-1,2,0900EF126E553AC7042903151BD043748EDC0371637BD5DE22FE5904FCAC120A,32481FEF89F39C29443AB678AC76AC3551D5F59BA9C5CCEACC92096C6F491293,AB70A06E9FE50C9BDA7352F64145A3EC3C59683B1F19DA35115D50F3038DD44C,FD75B6BD946C1974EEA2F5380C746481D956ECFC6F411146F2A3F5A3D8A368C0",
            "11,0,5,4,-1,2,0900EF126E553AC7042903151BD043748EDC0371637BD5DE22FE5904FCAC120A,32481FEF89F39C29443AB678AC76AC3551D5F59BA9C5CCEACC92096C6F491293,AB70A06E9FE50C9BDA7352F64145A3EC3C59683B1F19DA35115D50F3038DD44C,FD75B6BD946C1974EEA2F5380C746481D956ECFC6F411146F2A3F5A3D8A368C0",
            "12,1,0,-1,-1,0,03D86BE50CA3583763016C44941898B51C0514256C5B75018F429B7F014BC316,03D86BE50CA3583763016C44941898B51C0514256C5B75018F429B7F014BC316,2690D8E814AB23CB8FBBDEFED9AAE4D141336017724D90C71C78D1A85C4B6C84,2690D8E814AB23CB8FBBDEFED9AAE4D141336017724D90C71C78D1A85C4B6C84",
            "13,1,0,4,4,0,03D86BE50CA3583763016C44941898B51C0514256C5B75018F429B7F014BC316,03D86BE50CA3583763016C44941898B51C0514256C5B75018F429B7F014BC316,2690D8E814AB23CB8FBBDEFED9AAE4D141336017724D90C71C78D1A85C4B6C84,2690D8E814AB23CB8FBBDEFED9AAE4D141336017724D90C71C78D1A85C4B6C84",
            "14,1,1,-1,-1,1,64A9658DA12ECB9D3FCFB31D91E70F7AD0CBA226A1F3F0ECDDC355AE9959B11C,37E0864A807E1458A8D34F063F3317E62F0C3E22C9B59AC9FDEEC4D60CDECBD6,D75B38CFB96AFEC274FDBA8D6B3472D746C14E785991E6557178E197DBC3EBD4,66F1E8AFA5F80E551CF76270DC8504EC3F87BEF2AB454CBEFAE8C56E233DEEFE",
            "15,1,1,4,-1,1,64A9658DA12ECB9D3FCFB31D91E70F7AD0CBA226A1F3F0ECDDC355AE9959B11C,37E0864A807E1458A8D34F063F3317E62F0C3E22C9B59AC9FDEEC4D60CDECBD6,D75B38CFB96AFEC274FDBA8D6B3472D746C14E785991E6557178E197DBC3EBD4,66F1E8AFA5F80E551CF76270DC8504EC3F87BEF2AB454CBEFAE8C56E233DEEFE",
            "16,1,2,-1,-1,1,7933FE7914C2303FA4589F9D6093061E8D4BE61739AA86746180BEF111976A89,6AE1B6051470EC6BD0D17E0E0FAB6AA28789FE86AFE2EB6547BFA27EDC95593D,29D9E0BAF301F056F37280488E87721D48BA793B87B3AA9D22AB33A8B58C394A,5442A7029CB64106F45B4BEBEA1792775889C8B380BBE2D7CF17FD3F1E87F0F0",
            "17,1,2,4,-1,1,7933FE7914C2303FA4589F9D6093061E8D4BE61739AA86746180BEF111976A89,6AE1B6051470EC6BD0D17E0E0FAB6AA28789FE86AFE2EB6547BFA27EDC95593D,29D9E0BAF301F056F37280488E87721D48BA793B87B3AA9D22AB33A8B58C394A,5442A7029CB64106F45B4BEBEA1792775889C8B380BBE2D7CF17FD3F1E87F0F0",
            "18,1,3,-1,-1,2,D2F03F902DC3DC019B7B28F029001DAAEB834675573C619F15EEDBD2914FF781,77F9929EFA311907477E27F602D6C4C435C5A69F0FEC9B44BF741B879FA73CD7,AB70A06E9FE50C9BDA7352F64145A3EC3C59683B1F19DA35115D50F3038DD44C,EC0000D4EBCCAEB4E07C3B856AE958702A08678A7763535604EA3D98AA39A008",
            "19,1,3,4,-1,2,D2F03F902DC3DC019B7B28F029001DAAEB834675573C619F15EEDBD2914FF781,77F9929EFA311907477E27F602D6C4C435C5A69F0FEC9B44BF741B879FA73CD7,AB70A06E9FE50C9BDA7352F64145A3EC3C59683B1F19DA35115D50F3038DD44C,EC0000D4EBCCAEB4E07C3B856AE958702A08678A7763535604EA3D98AA39A008",
            "20,1,4,-1,-1,250,E0B9377269BDED54842DE0633BB7F7A366BC854FAACE70326B1CEC2308CCABAB,EC9B75DE02ACB02EB5F9D2F5FF522703E4276EF0C5BFDED4774C63D6E4E69E95,95699931F6412E552E4CE0C170F65222071113AB4772F6F91C11CE271300E3A4,5B801EEC90D5067F8EC271C8A028330B44B2FBB3E72007C4D74DC160731E61E6",
            "21,1,4,4,-1,250,E0B9377269BDED54842DE0633BB7F7A366BC854FAACE70326B1CEC2308CCABAB,EC9B75DE02ACB02EB5F9D2F5FF522703E4276EF0C5BFDED4774C63D6E4E69E95,95699931F6412E552E4CE0C170F65222071113AB4772F6F91C11CE271300E3A4,5B801EEC90D5067F8EC271C8A028330B44B2FBB3E72007C4D74DC160731E61E6",
            "22,1,5,-1,-1,2,C08880B883D401F448C05C3CE7CBD8934636A04A823D5BF3EF37C20B3B9324A7,1D3177EB30DC09EA874C554AAB53384A138FC1978D270648CC720750F1F025D9,AB70A06E9FE50C9BDA7352F64145A3EC3C59683B1F19DA35115D50F3038DD44C,FD75B6BD946C1974EEA2F5380C746481D956ECFC6F411146F2A3F5A3D8A368C0",
            "23,1,5,4,-1,2,C08880B883D401F448C05C3CE7CBD8934636A04A823D5BF3EF37C20B3B9324A7,1D3177EB30DC09EA874C554AAB53384A138FC1978D270648CC720750F1F025D9,AB70A06E9FE50C9BDA7352F64145A3EC3C59683B1F19DA35115D50F3038DD44C,FD75B6BD946C1974EEA2F5380C746481D956ECFC6F411146F2A3F5A3D8A368C0",
         };
        static void Need(bool ok, string text) { PracticeIdleReturnChecks.Need(ok, text); }
        static string Hash(byte[] b) { return PracticeIdleReturnChecks.Hash(b); }
        static bool Reject(Action a) { return PracticeIdleReturnChecks.Reject(a); }
        static void I(byte[] b, int at, int n) { PracticeIdleReturnChecks.Integer(b, at, n); }
        static void W(byte[] b, int at, int n) { PracticeIdleReturnChecks.Word(b, at, n); }
        internal static void Fixture(byte[] basis, int profile, out byte[] fleets, out PracticeAiSharedBacking backing)
        {
            fleets = (byte[])basis.Clone(); var r = new byte[78560];
            for (int g = 0; g < 250; g++) PracticeFleetServiceContext.CreateDiagnostic(g).CopyTo(r, g * 312);
            for (int g = 0; g < 250; g++)
            {
                bool on = profile == 1 && g == 16 || profile == 2 && g == 249 || profile == 3 && (g == 16 || g == 249) || profile == 4 || profile == 5 && (g == 16 || g == 249);
                if (!on) continue; int at = g * 492, ta = g * 312;
                int state = profile == 1 || profile == 3 && g == 16 || profile == 5 ? 5 : 6;
                I(fleets, at + 472, state); W(fleets, at + 488, profile == 5 ? (g == 16 ? 249 : 16) : 0);
                for (int n = 0; n < 5; n++) { W(fleets, at + 40 + n * 2, n == 0 ? -1 : 100 + n * 10); W(fleets, at + 50 + n * 2, 200 + n * 10); }
                fleets[at + 482] = 255; I(r, ta + 304, 2); I(r, ta + 28, profile == 4 ? 550 + g : 550); I(r, ta + 32, profile == 4 ? 290 - g : 290);
            }
            backing = new PracticeAiSharedBacking(r);
        }

        internal static void Run(Action<string, Action> check, Action<string, Action> evidence, string assets)
        {
            var bases = PracticeIdleReturnChecks.Bases(assets);
            check("Idle plus navigation matches24 original664 whole-caller complete golden states", delegate
            {
                int count = 0, requests = 0;
                foreach (string line in Original)
                {
                    var row = line.Split(','); int id = int.Parse(row[0]), preset = int.Parse(row[1]), profile = int.Parse(row[2]), selected = int.Parse(row[3]); byte[] f; PracticeAiSharedBacking b;
                    Fixture(bases[preset], profile, out f, out b); var result = PracticePrephaseIdle.Evaluate(f, b, 0, selected); int expected = new[] { 0, 1, 1, 2, 250, 2 }[profile];
                    Need(id == count && row.Length == 10 && result.Visits == 250 && result.IdleReturns == expected && result.RouteUiRequests == expected && result.SelectedWaypoint == int.Parse(row[4]) && result.RouteUiRequests == int.Parse(row[5]) && Hash(f) == row[6] && Hash(result.CopyFleetPool()) == row[7] && Hash(b.CopyRegion()) == row[8] && Hash(result.Backing.CopyRegion()) == row[9], "Original664 complete state " + id);
                    requests += result.RouteUiRequests; count++;
                }
                Need(count == 24 && requests == 1024, "664 context and request totals");
            });
            check("Pre-phase zero-idle selection holds while cross-home and inactive returns request refreshes", delegate
            {
                byte[] f; PracticeAiSharedBacking b; Fixture(bases[0], 0, out f, out b); var none = PracticePrephaseIdle.Evaluate(f, b, 0, 4);
                Need(none.IdleReturns == 0 && none.SelectedWaypoint == 4 && none.RouteUiRequests == 0 && Hash(none.CopyFleetPool()) == Hash(f), "Zero-idle selection or fleet changed");
                Fixture(bases[0], 5, out f, out b); var both = PracticePrephaseIdle.Evaluate(f, b, 0, 4); var after = both.CopyFleetPool();
                Need(both.IdleReturns == 2 && both.RouteUiRequests == 2 && both.SelectedWaypoint == -1 && BitConverter.ToInt32(f, 249 * 492) == 0 && BitConverter.ToInt16(after, 16 * 492 + 40) == BitConverter.ToInt16(f, 249 * 492 + 12) && BitConverter.ToInt16(after, 249 * 492 + 40) == BitConverter.ToInt16(f, 16 * 492 + 12), "Cross-home/inactive dispatcher return");
                Fixture(bases[0], 4, out f, out b); var all = PracticePrephaseIdle.Evaluate(f, b, 0, -1); Need(all.IdleReturns == 250 && all.RouteUiRequests == 250 && all.Visits == 250, "Inactive slots skipped");
            });
            check("Pre-phase idle preserves parent snapshots and rejects late invalid home atomically", delegate
            {
                byte[] f; PracticeAiSharedBacking b; Fixture(bases[0], 3, out f, out b); string fh = Hash(f), bh = Hash(b.CopyRegion());
                var result = PracticePrephaseIdle.Evaluate(f, b, 0, 4); var copy = result.CopyFleetPool(); copy[0] ^= 255;
                Need(Hash(f) == fh && Hash(b.CopyRegion()) == bh && copy[0] != result.CopyFleetPool()[0] && result.Backing.CopyRegion()[78559] == b.CopyRegion()[78559], "Mutable whole idle result");
                I(f, 249 * 492 + 472, 5); W(f, 249 * 492 + 488, -1); string bad = Hash(f);
                Need(Reject(delegate { PracticePrephaseIdle.Evaluate(f, b, 0, 4); }) && Hash(f) == bad && Hash(b.CopyRegion()) == bh && BitConverter.ToInt32(b.CopyTaskRecord(16), 304) == 2, "Late home error published earlier idle writes");
            });
            check("Idle composition keeps navigation-only contract and rejects policy network and malformed inputs", delegate
            {
                byte[] f; PracticeAiSharedBacking b; Fixture(bases[0], 3, out f, out b);
                Need(Reject(delegate { PracticePrephaseNavigation.Evaluate(f, b); }), "Existing navigation-only idle guard lost");
                foreach (int flag in new[] { 1, 2, 3 }) { var bad = (byte[])f.Clone(); bad[249 * 492 + 486] = (byte)flag; Need(Reject(delegate { PracticePrephaseIdle.Evaluate(bad, b, 0, 4); }), "Active policy admitted " + flag); }
                Need(Reject(delegate { PracticePrephaseIdle.Evaluate(f, b, 1, 4); }) && Reject(delegate { PracticePrephaseIdle.Evaluate(f, b, 0, 5); }) && Reject(delegate { PracticePrephaseIdle.Evaluate(new byte[1], b, 0, 4); }) && Reject(delegate { PracticePrephaseIdle.Evaluate(f, null, 0, 4); }), "Unsupported whole idle context admitted");
            });
            evidence("Canonical idle composition matches all48 original658 single-idle caller full snapshots", delegate
            {
                string path = Path.Combine(Path.GetDirectoryName(assets), "Evidence", "Prephase-Single-Idle-658.bin");
                Need(Hash(File.ReadAllBytes(path)) == "A0E9989272F16FE8D4866B43072561CB275F4B97D33B7DE2E9F014A57F1C0786", "658 identity"); int count = 0;
                using (var r = new BinaryReader(File.OpenRead(path)))
                {
                    Need(System.Text.Encoding.ASCII.GetString(PracticeIdleReturnChecks.Read(r, 8)) == "WI658001" && r.ReadInt32() == 1 && r.ReadInt32() == 48, "658 header"); var bf = PracticeIdleReturnChecks.Read(r, 123000); var br = PracticeIdleReturnChecks.Read(r, 78560);
                    for (;;)
                    {
                        int id = r.ReadInt32(); if (id == -1) break; int preset = r.ReadInt32(), actor = r.ReadInt32(), slot = r.ReadInt32(), state = r.ReadInt32(), profile = r.ReadInt32(), selected = r.ReadInt32(), wp = r.ReadInt32(), requests = r.ReadInt32();
                        Need(id == count && preset >= 0 && preset < 2 && slot == actor && (actor == 16 || actor == 249) && (state == 5 || state == 6) && profile >= 0 && profile < 3, "658 metadata");
                        var f = PracticeIdleReturnChecks.Patch(r, bf); var b = PracticeIdleReturnChecks.Patch(r, br); var af = PracticeIdleReturnChecks.Patch(r, f); var ab = PracticeIdleReturnChecks.Patch(r, b); var result = PracticePrephaseIdle.Evaluate(f, new PracticeAiSharedBacking(b), 0, selected);
                        Need(Hash(result.CopyFleetPool()) == Hash(af) && Hash(result.Backing.CopyRegion()) == Hash(ab) && result.SelectedWaypoint == wp && result.RouteUiRequests == requests, "658 full snapshots " + id); count++;
                    }
                    Need(count == 48 && r.BaseStream.Position == r.BaseStream.Length, "658 count/EOF");
                }
            });
            evidence("Canonical idle composition matches all24 original664 multi-idle full snapshots", delegate
            {
                string path = Path.Combine(Path.GetDirectoryName(assets), "Evidence", "Prephase-Multi-Idle-664.bin");
                Need(Hash(File.ReadAllBytes(path)) == "6A9BD8BB15103BF53B67E130B6400FB6A279F3BCB86AFB9DF3F5E8411E609E6F", "664 identity"); int count = 0;
                using (var r = new BinaryReader(File.OpenRead(path)))
                {
                    Need(System.Text.Encoding.ASCII.GetString(PracticeIdleReturnChecks.Read(r, 8)) == "MI664001" && r.ReadInt32() == 1 && r.ReadInt32() == 24, "664 header"); var bf = PracticeIdleReturnChecks.Read(r, 123000); var br = PracticeIdleReturnChecks.Read(r, 78560);
                    for (;;)
                    {
                        int id = r.ReadInt32(); if (id == -1) break; int preset = r.ReadInt32(), profile = r.ReadInt32(), selected = r.ReadInt32(), wp = r.ReadInt32(), requests = r.ReadInt32();
                        Need(id == count && preset >= 0 && preset < 2 && profile >= 0 && profile < 6, "664 metadata");
                        var f = PracticeIdleReturnChecks.Patch(r, bf); var b = PracticeIdleReturnChecks.Patch(r, br); var af = PracticeIdleReturnChecks.Patch(r, f); var ab = PracticeIdleReturnChecks.Patch(r, b); var result = PracticePrephaseIdle.Evaluate(f, new PracticeAiSharedBacking(b), 0, selected);
                        Need(Hash(result.CopyFleetPool()) == Hash(af) && Hash(result.Backing.CopyRegion()) == Hash(ab) && result.SelectedWaypoint == wp && result.RouteUiRequests == requests, "664 full snapshots " + id); count++;
                    }
                    Need(count == 24 && r.BaseStream.Position == r.BaseStream.Length, "664 count/EOF");
                }
            });
        }
    }
}

