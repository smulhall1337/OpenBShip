using System;
using System.IO;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeFleetRequestMissChecks
    {
        // Independent complete original568 pool observations, not this model's outputs.
        static readonly string[] Original={
            "0,0,14845,2,0,868999715,0,2EB2732234FE054111E855F02324474BF9AC309E6EDF95D917881F20949DF69F,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,EA2901078E939A9183DE674616D2B406C1BAFC4BF4BF09EA11A0DD837AD791AD,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,FFA07F12194283897F9ECEBF7EA813DDA403A080854605C8ABE61BBC3082A086,19EEF2915FD3738C6BFFABBF8FD881196D45F9071C53C55211BE67A48C8F6BB8,374708FFF7719DD5979EC875D56CD2286F6D3CF7EC317A3B25632AAB28EC37BB,8667E718294E9E0DF1D30600BA3EEB201F764AAD2DAD72748643E4A285E1D1F7",
            "0,1,14885,2,0,868999715,0,2EB2732234FE054111E855F02324474BF9AC309E6EDF95D917881F20949DF69F,DAE2B19AFA2AA44AA9FB10F7A4952251421E3D5B13A7167758935B6198A00944,EA2901078E939A9183DE674616D2B406C1BAFC4BF4BF09EA11A0DD837AD791AD,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,FFA07F12194283897F9ECEBF7EA813DDA403A080854605C8ABE61BBC3082A086,19EEF2915FD3738C6BFFABBF8FD881196D45F9071C53C55211BE67A48C8F6BB8,374708FFF7719DD5979EC875D56CD2286F6D3CF7EC317A3B25632AAB28EC37BB,8667E718294E9E0DF1D30600BA3EEB201F764AAD2DAD72748643E4A285E1D1F7",
            "0,38,16365,1,1,859653210,0,5465FDE8C9793EB5A6819A259F36ACA6252122534385AE81F9F48901A698A531,E9E463FDC576D74007C1ABDBB9193AC1C1F72B12960A3849467B5CB9B69F7735,EA2901078E939A9183DE674616D2B406C1BAFC4BF4BF09EA11A0DD837AD791AD,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,22AFDC5B15A0523D50FB011F0BF3AACF6A975F82E643CBD495426FF476F670EC,19EEF2915FD3738C6BFFABBF8FD881196D45F9071C53C55211BE67A48C8F6BB8,374708FFF7719DD5979EC875D56CD2286F6D3CF7EC317A3B25632AAB28EC37BB,8667E718294E9E0DF1D30600BA3EEB201F764AAD2DAD72748643E4A285E1D1F7",
            "0,60,17245,0,2,2040838581,0,32BA1A15C28F3EF654884AED1459C7425969AB01F410FC339F26E52664AC45CE,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,EA2901078E939A9183DE674616D2B406C1BAFC4BF4BF09EA11A0DD837AD791AD,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,F8251BB28045B4E4B5B8F33CF77B9E0EF2972B7E7F6EB82705CCDE59B7C3A14C,19EEF2915FD3738C6BFFABBF8FD881196D45F9071C53C55211BE67A48C8F6BB8,374708FFF7719DD5979EC875D56CD2286F6D3CF7EC317A3B25632AAB28EC37BB,8667E718294E9E0DF1D30600BA3EEB201F764AAD2DAD72748643E4A285E1D1F7",
            "0,92,18525,0,0,2040838581,0,32BA1A15C28F3EF654884AED1459C7425969AB01F410FC339F26E52664AC45CE,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,EA2901078E939A9183DE674616D2B406C1BAFC4BF4BF09EA11A0DD837AD791AD,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,0E41886FB08CA89CAD684D01CF1F3E10703202A055C51F1878A1E616F80B634C,19EEF2915FD3738C6BFFABBF8FD881196D45F9071C53C55211BE67A48C8F6BB8,374708FFF7719DD5979EC875D56CD2286F6D3CF7EC317A3B25632AAB28EC37BB,8667E718294E9E0DF1D30600BA3EEB201F764AAD2DAD72748643E4A285E1D1F7",
            "1,1,16345,1,1,859653210,0,601CB0E6F5A2AD7B281B2F5984CA64AF01C5257AE8784B94BA8A4CCDFFFF1A74,E9E463FDC576D74007C1ABDBB9193AC1C1F72B12960A3849467B5CB9B69F7735,EA2901078E939A9183DE674616D2B406C1BAFC4BF4BF09EA11A0DD837AD791AD,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,22AFDC5B15A0523D50FB011F0BF3AACF6A975F82E643CBD495426FF476F670EC,19EEF2915FD3738C6BFFABBF8FD881196D45F9071C53C55211BE67A48C8F6BB8,374708FFF7719DD5979EC875D56CD2286F6D3CF7EC317A3B25632AAB28EC37BB,8667E718294E9E0DF1D30600BA3EEB201F764AAD2DAD72748643E4A285E1D1F7",
            "1,56,18545,0,0,2040838581,0,DF4153E26F0C7939798145DE413C696F32C2333E0C5BBFF2CEE29436AB2B9187,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,EA2901078E939A9183DE674616D2B406C1BAFC4BF4BF09EA11A0DD837AD791AD,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,0E41886FB08CA89CAD684D01CF1F3E10703202A055C51F1878A1E616F80B634C,19EEF2915FD3738C6BFFABBF8FD881196D45F9071C53C55211BE67A48C8F6BB8,374708FFF7719DD5979EC875D56CD2286F6D3CF7EC317A3B25632AAB28EC37BB,8667E718294E9E0DF1D30600BA3EEB201F764AAD2DAD72748643E4A285E1D1F7",
            "2,1,16385,1,1,859653210,0,F2628A230B6BCC2A4B525621B68B7DE15D2F06E0193FBB9C8C51EE583ABC602C,DAE2B19AFA2AA44AA9FB10F7A4952251421E3D5B13A7167758935B6198A00944,EA2901078E939A9183DE674616D2B406C1BAFC4BF4BF09EA11A0DD837AD791AD,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,22AFDC5B15A0523D50FB011F0BF3AACF6A975F82E643CBD495426FF476F670EC,19EEF2915FD3738C6BFFABBF8FD881196D45F9071C53C55211BE67A48C8F6BB8,374708FFF7719DD5979EC875D56CD2286F6D3CF7EC317A3B25632AAB28EC37BB,8667E718294E9E0DF1D30600BA3EEB201F764AAD2DAD72748643E4A285E1D1F7",
            "2,55,18545,0,0,2040838581,0,C8AB84B47D1CCBE76145424E9CDCBB21891F2BBEFD530E26E2CD884541A87C25,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,EA2901078E939A9183DE674616D2B406C1BAFC4BF4BF09EA11A0DD837AD791AD,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,0E41886FB08CA89CAD684D01CF1F3E10703202A055C51F1878A1E616F80B634C,19EEF2915FD3738C6BFFABBF8FD881196D45F9071C53C55211BE67A48C8F6BB8,374708FFF7719DD5979EC875D56CD2286F6D3CF7EC317A3B25632AAB28EC37BB,8667E718294E9E0DF1D30600BA3EEB201F764AAD2DAD72748643E4A285E1D1F7",
            "3,1,17245,1,1,859653210,0,27816BF76D71118FDFCEAC01039A086082A089D73B3430DFE73EBA9522E94E9C,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,EA2901078E939A9183DE674616D2B406C1BAFC4BF4BF09EA11A0DD837AD791AD,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,017D6564E1D1B50D40E92F8E62436110185214823A653EB48CBFC2FF936474E1,19EEF2915FD3738C6BFFABBF8FD881196D45F9071C53C55211BE67A48C8F6BB8,374708FFF7719DD5979EC875D56CD2286F6D3CF7EC317A3B25632AAB28EC37BB,8667E718294E9E0DF1D30600BA3EEB201F764AAD2DAD72748643E4A285E1D1F7",
            "3,34,18565,0,0,2040838581,0,AFC4733E57FC937487076560D5A2F63A179828862B807F33B0DE70616CEAA6CC,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,EA2901078E939A9183DE674616D2B406C1BAFC4BF4BF09EA11A0DD837AD791AD,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,C19BD43904EA49C31376367D16C856A36743CD880A70D7039A71AB4D7B264E09,19EEF2915FD3738C6BFFABBF8FD881196D45F9071C53C55211BE67A48C8F6BB8,374708FFF7719DD5979EC875D56CD2286F6D3CF7EC317A3B25632AAB28EC37BB,8667E718294E9E0DF1D30600BA3EEB201F764AAD2DAD72748643E4A285E1D1F7",
        };
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}catch(NotSupportedException){return true;}}
        static void Compare(PracticeFleetRequestMisses c,string[] r)
        {
            var times=new byte[64];for(int n=0;n<times.Length;n++)times[n]=255;
            Need(r.Length==15&&c.Mode==int.Parse(r[0])&&c.Updates==int.Parse(r[1])&&c.Clock==uint.Parse(r[2])&&c.Projectiles.Count==int.Parse(r[3])&&c.Effects.Count==int.Parse(r[4])&&c.RandomState==uint.Parse(r[5])&&c.MovieCounter==int.Parse(r[6])&&Hash(c.CopyTimedPool())==r[7]&&Hash(c.CopyFleetPool())==r[8]&&Hash(c.CopyShipPool())==r[9]&&Hash(c.CopyOrderPool())==r[10]&&Hash(c.CopyEffectPool())==r[11]&&Hash(c.CopyScoreRegion())==r[12]&&Hash(new byte[16])==r[13]&&Hash(times)==r[14],"Original568 complete pools/scalars "+r[0]+"/"+r[1]);
        }
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var startup=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            Func<PracticeFleetRequestLaunch> launch=delegate{return PracticeFleetRequestLaunch.Create(programs,startup,0,0).Execute();};
            Func<int,PracticeFleetRequestMisses> create=delegate(int mode){return PracticeFleetRequestMisses.Create(programs,launch(),mode);};
            check("F2 miss composition matches eleven original568 complete pool observations",delegate
            {
                foreach(string line in Original){var r=line.Split(',');var c=create(int.Parse(r[0]));int visits=int.Parse(r[1]);for(int n=0;n<visits;n++)c=c.Step();Compare(c,r);}Need(Original.Length==11,"568 public observation count");
            });
            check("Default F2 shots resolve as water misses and effects retire at original transitions",delegate
            {
                var c=create(0);uint[] clocks={16365,17245,17645,18525};int[] projectiles={1,0,0,0},effects={1,2,1,0};
                for(int n=0;n<4;n++)
                {
                    c=c.NextTransition();Need(c.Clock==clocks[n]&&c.Projectiles.Count==projectiles[n]&&c.Effects.Count==effects[n],"Original water transition "+n);
                    if(n==0){var e=c.Effects[0];Need(c.ResolvedMisses==1&&c.RandomCalls==1&&e.TargetFleet==14&&e.X==0&&e.Y==8&&e.Counter==1&&e.Type==27,"Same-update effect aging / first water target");}
                }
                Need(c.Complete&&c.Updates==92&&c.ResolvedMisses==2&&c.RandomCalls==2&&c.RandomState==2040838581u&&c.MovieCounter==0,"Complete water history");
            });
            check("Diagnostic deadline visits preserve callback order and retired coordinate writes",delegate
            {
                var exact=create(1).Step();var late=create(2).Step();var second=create(3).Step();Need(exact.Clock==16345&&late.Clock==16385&&second.Clock==17245&&exact.Projectiles.Count==1&&late.Projectiles.Count==1&&second.Projectiles.Count==1&&second.Projectiles[0].Slot==1,"Diagnostic first visits");
                var e=exact.Finish().CopyTimedPool();var l=late.Finish().CopyTimedPool();var s=second.Finish().CopyTimedPool();Need(BitConverter.ToInt16(e,276)==235&&BitConverter.ToInt16(e,278)==423&&BitConverter.ToInt16(l,276)==220&&BitConverter.ToInt16(l,278)==418&&BitConverter.ToInt16(s,8)==231&&BitConverter.ToInt16(s,10)==423&&BitConverter.ToInt16(s,276)==234,"Retired coordinate bytes replaced with guessed destinations");
                Need(exact.Finish().Clock==18545&&late.Finish().Clock==18545&&second.Finish().Clock==18565,"Diagnostic effect lifetimes");
            });
            check("Miss-history snapshots preserve copied launch and do not expose mutable data",delegate
            {
                var first=create(0);string t=Hash(first.CopyTimedPool()),f=Hash(first.CopyFleetPool()),s=Hash(first.CopyShipPool());var next=first.NextTransition();
                var timed=next.CopyTimedPool();timed[0]=99;var fleet=next.CopyFleetPool();fleet[0]^=255;var ships=next.CopyShipPool();ships[0]^=255;var effects=next.CopyEffectPool();effects[0]^=255;var scores=next.CopyScoreRegion();scores[0]=99;var orders=next.CopyOrderPool();orders[0]^=255;
                Need(first.Updates==0&&first.Projectiles.Count==2&&first.Effects.Count==0&&Hash(first.CopyTimedPool())==t&&Hash(first.CopyFleetPool())==f&&Hash(first.CopyShipPool())==s&&next.CopyTimedPool()[0]!=99&&next.CopyFleetPool()[0]!=fleet[0]&&next.CopyShipPool()[0]!=ships[0]&&next.CopyEffectPool()[0]!=effects[0]&&next.CopyScoreRegion()[0]!=99&&next.CopyOrderPool()[0]!=orders[0],"Mutable history/predecessor");
            });
            check("Misses hold all remaining requests ammunition ships and scores after the original launch",delegate
            {
                var first=create(0);var done=first.Finish();Need(Hash(first.CopyShipPool())==Hash(done.CopyShipPool())&&Hash(first.CopyOrderPool())==Hash(done.CopyOrderPool())&&Hash(first.CopyScoreRegion())==Hash(done.CopyScoreRegion())&&done.Launch.Ammunition(0)==6&&done.Launch.Ammunition(1)==6&&done.Launch.RequestCount(0)==9&&done.Launch.RequestCount(1)==9&&done.RandomCalls==2&&done.ResolvedMisses==2,"Unreached firing or damage occurred");
            });
            check("Water-miss composition rejects F3 capacity fixtures invalid modes and completed updates",delegate
            {
                Need(Reject(delegate{PracticeFleetRequestMisses.Create(programs,null,0);})&&Reject(delegate{PracticeFleetRequestMisses.Create(programs,launch(),-1);})&&Reject(delegate{PracticeFleetRequestMisses.Create(programs,launch(),4);})&&Reject(delegate{PracticeFleetRequestMisses.Create(programs,PracticeFleetRequestLaunch.Create(programs,startup,1,0).Execute(),0);})&&Reject(delegate{PracticeFleetRequestMisses.Create(programs,PracticeFleetRequestLaunch.Create(programs,startup,0,1).Execute(),0);})&&Reject(delegate{PracticeFleetRequestMisses.Create(programs,PracticeFleetRequestLaunch.Create(programs,startup,0,0),0);}),"Uncaptured miss input admitted");
                var c=create(0).Finish();string t=Hash(c.CopyTimedPool());Need(Reject(delegate{c.Step();})&&Reject(delegate{c.UpTo(32);})&&Reject(delegate{c.NextTransition();})&&Reject(delegate{c.Finish();})&&Reject(delegate{create(0).UpTo(0);})&&Reject(delegate{create(0).UpTo(129);})&&Hash(c.CopyTimedPool())==t,"Completed/invalid update changed data");
            });
            evidence("Canonical copied water misses match all241 original568 observations",delegate
            {
                string file=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Fleet-Member-Misses-568.csv");Need(Hash(File.ReadAllBytes(file))=="662883A67C5ADF4ECA3D7E422CFAF6BD98BC363F8A6B14DD8B935F1CB306E602","568 evidence identity");int mode=-1,count=0;PracticeFleetRequestMisses c=null;
                foreach(string line in File.ReadLines(file))
                {
                    if(line.StartsWith("Mode,"))continue;var r=line.Split(',');int selected=int.Parse(r[0]);if(selected!=mode){Need(selected==mode+1&&int.Parse(r[1])==0,"568 case order");mode=selected;c=create(mode);}else c=c.Step();Compare(c,r);count++;
                }
                Need(mode==3&&count==241&&c.Complete,"568 complete history matrix");
            });
        }
    }
}
