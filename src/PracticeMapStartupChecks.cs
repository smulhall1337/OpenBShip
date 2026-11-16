using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
namespace FleetLab
{
    internal static class PracticeMapStartupChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static void I(byte[] b,int at,int value){BitConverter.GetBytes(value).CopyTo(b,at);}static void W(byte[] b,int at,int value){BitConverter.GetBytes(unchecked((short)value)).CopyTo(b,at);}
        static bool Reject(Action action){try{action();return false;}catch(InvalidDataException){return true;}catch(ArgumentException){return true;}}
        // Synthetic pool definitions from the whole-original451 comparisons.
        internal static byte[][] FormationFixture(int pattern,int fleet,int kind)
        {
            var s=new byte[402000];var f=new byte[123000];for(int n=0;n<s.Length;n++)s[n]=(byte)(n*13+7);for(int n=0;n<f.Length;n++)f[n]=(byte)(n*17+31);int at=fleet*492;I(f,at,kind);for(int m=0;m<6;m++)I(f,at+16+m*4,0);
            int[] lengths=pattern==0?new int[0]:pattern==1?new[]{5}:pattern==2?new[]{3,3,5,3,3}:pattern==3?new[]{4,4,4,4,4,4}:pattern==4?new[]{10,10,10,10,10}:pattern==5?new[]{1,2,3,4,5,6}:new[]{4,2};
            for(int m=0;m<lengths.Length;m++){int unit=pattern>=6?(m==0?1499:13):20+m,u=unit*268;I(s,u,pattern==7?-7:1);W(s,u+4,unit);W(s,u+8,-123);W(s,u+10,456);W(s,u+14,lengths[m]);I(f,at+16+m*4,0x4fc340+u);}
            if(pattern>=6){I(f,at+16,0);I(f,at+20,0x4fc340+1499*268);I(f,at+24,0x4fc340+1498*268);I(s,1498*268,0);W(s,1498*268+8,-32768);W(s,1498*268+10,32767);I(f,at+28,0x4fc340+1499*268);I(f,at+32,0x4fc340+13*268);}
            return new[]{s,f};
        }
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs"),map=Path.Combine(assets,"Graphics","BSSB202.SCN");
            check("Practice formation matches six whole-original empty dense restart and alias goldens",delegate
            {
                // Fields:pattern/fleet/kind/seed/finalRNG/calls/restarts/four pool hashes.
                string[] rows={
                    "0,0,2,731,731,0,0,6DE1826C710F92FA79960FEB5A5F7DEFB276DFC362C8EEEBFB62D63E8F75C941,2A7220A044D8CD03B46460314CECB8F82D3B758D7AEC6282BAB2A1830D30E46C,6DE1826C710F92FA79960FEB5A5F7DEFB276DFC362C8EEEBFB62D63E8F75C941,B1EA9263FFA9123AF787789275BAB8BF1341ECC384CA9FC3ED2B224600E48F72",
                    "3,0,0,4294967295,3315033559,504,2,93345DBCA7CB5140BAF337827C88CDCB3050A2D600BEE6D7D1187274D7853525,31180B14DABBB9749EC309264C621035E2590B687569B8B2FE7A1EB19D0F2F47,AF75E69644F3580DE7F17F78D423046A3FBF68F1B130394F2CDE07CD95C1280A,97FA4822FC4363B66F95461BADD0DDA6C38A07432C3D326DF935D5DEAE389B06",
                    "3,249,2,0,274340270,86,0,93345DBCA7CB5140BAF337827C88CDCB3050A2D600BEE6D7D1187274D7853525,D670D9834D4D0C61075D4C1FC5061E60270E39A069EFBF0E5F5749BF9621FADA,9CB3FC81DBA4736F6991FBCC3EBED6E746283CCE9649C81360C10952B991CE4D,28764306AC27326C78707480ECF4296B6A5AD72C3363B773234CCF54BA0BB216",
                    "4,0,1,731,1593918265,30,0,4ECF2762C21C85D74670237B6B251A6274432B7F3F7F1B95F77102265042AC74,BD635D971449C393E5A495F9452904DAF7F6F4166CDECCEBCFEEA72AF28B7987,52E83438B205899177344E5F6FE4456F84945B0CFA74EA0EA46A93B32ACB45DC,E137E9963270AB831A18049E0A3551F50194678A35CE31CF4649104D02F66D41",
                    "6,249,9,1,3884216597,4,0,FB93B4A07BDC674DA9E1BC78B9C2974F71FF5E31EC5D4CF1D76677649D7A117A,A95D328D7843B890C742A8EBB0AEA1DF2E7F070704422119A0667F61A89ED3F1,F3A65DC2B5222CDC31FC3E9322EDCFBCC19D226C5CC926B8B5FD4EA627EEEBDA,284386F474AE9751ECA3CDB3A01D929608A24741455A2822B586285661B7A519",
                    "7,0,0,4294967295,730189939,4,0,7AC8D54305BBD6548F3A2C33B87FBE0E6C6ED056CD23CBE4C4D45E9060D15B7F,240464B0926C72EBF319CA0D6A235B5989A2E01380879FD4FD57CD10A5EEF403,424B7FC673F7F327CD0A2E8012697F00F61E0EECB82E9D8F5CE09B62577BCECB,845113C5D52E90E5BDD84828AA90793440EBF3FDF8FCF96C260311F6DC15A73A"};
                foreach(string line in rows)
                {
                    var row=line.Split(',');var p=FormationFixture(int.Parse(row[0]),int.Parse(row[1]),int.Parse(row[2]));Need(Hash(p[0])==row[7]&&Hash(p[1])==row[8],"Original451 fixture bytes");
                    var result=PracticeFormationPlacement.Apply(p[0],p[1],int.Parse(row[1]),uint.Parse(row[3]));
                    Need(Hash(result.CopyShipPool())==row[9]&&Hash(result.CopyFleetPool())==row[10]&&result.RandomState==uint.Parse(row[4])&&result.RandomCalls==int.Parse(row[5])&&result.Restarts==int.Parse(row[6]),"Whole original451 output/managed diagnostics");
                    Need(Hash(p[0])==row[7]&&Hash(p[1])==row[8],"Formation changed caller pools");var copy=result.CopyFleetPool();copy[0]^=255;Need(Hash(result.CopyFleetPool())==row[10],"Formation result leaked mutable pool");
                }
            });
            check("Practice formation keeps aliased slots shared and inactive coordinates untouched",delegate
            {
                var p=FormationFixture(6,249,9);var result=PracticeFormationPlacement.Apply(p[0],p[1],249,1);var s=result.CopyShipPool();
                Need(result.PlacedUnits==2&&result.RandomCalls==4&&BitConverter.ToInt16(s,1498*268+8)==short.MinValue&&BitConverter.ToInt16(s,1498*268+10)==short.MaxValue,"Alias/dead slot semantics");
            });
            check("Practice formation rejects invalid and impossible copies without publishing partial changes",delegate
            {
                for(int variant=0;variant<7;variant++)
                {
                    var p=FormationFixture(1,0,1);
                    if(variant<3)W(p[0],20*268+14,new[]{0,11,12}[variant]);
                    if(variant==3)I(p[1],16,0x4fc341);
                    if(variant==4)I(p[1],16,0x4fc340+402000);
                    if(variant==5){p=FormationFixture(4,0,1);I(p[0],25*268,1);W(p[0],25*268+14,10);I(p[1],36,0x4fc340+25*268);}
                    string before=Hash(p[0])+Hash(p[1]);int limit=variant==6?-1:0;
                    Need(Reject(delegate{PracticeFormationPlacement.Apply(p[0],p[1],0,731,limit);})&&Hash(p[0])+Hash(p[1])==before,"Invalid/impossible placement variant "+variant);
                }
                Need(Reject(delegate{PracticeFormationPlacement.Apply(new byte[0],new byte[123000],0,0);})&&Reject(delegate{PracticeFormationPlacement.Apply(new byte[402000],new byte[123000],250,0);}),"Formation extent bounds");
            });
            check("BSSB202 startup matches original default primary pools at all four stages",delegate
            {
                var state=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(map),731);
                string[] sh={"724B2BF109A39CF8409C3967EB8E71E3F8423DF3EB16733A8E1E71791C08F67D","BB221364050948C194DB7C84D180AF01CFE3E13BD551237A77597E764F4CC0E5","8C590E22A49600CC517641C3BA19A9E649E3AAF6F4917E219FDEB1C2B5C90324","84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49"};
                string[] fh={"E4657672F3419A061B92CDADD6860C786AF388C0F5AE6D20930425EAE4B4E482","EF51298ACA6E79F6E79F77F429E9545DECEC6172A4967135D600A022D95FB41F","FB3B8E4DDD9B5B97EA4A0B73A6CDF08E082A7C1C2C9B5CF1FA7F0D0408CA8114","8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D"};
                uint[] random={2672695135,1526221131,249622383,249622383};int[] calls={20,432,484,484},fleets={14,23,23,29},units={20,42,42,63};Need(state.Stages.Count==4&&state.Seed==731,"Setup stages/seed");
                for(int n=0;n<4;n++){var stage=state.Stages[n];Need(Hash(stage.CopyShipPool())==sh[n]&&Hash(stage.CopyFleetPool())==fh[n]&&stage.RandomState==random[n]&&stage.RandomCalls==calls[n]&&stage.NonemptyFleets==fleets[n]&&stage.NextInactiveFleet==fleets[n]&&stage.NonemptyUnits==units[n],"Whole original453 default stage "+n);}
            });
            check("BSSB202 startup retains unsigned seed wrap and recorded final state",delegate
            {
                var state=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(map),uint.MaxValue);var final=state.Stages[3];
                Need(final.RandomState==3482650137&&final.RandomCalls==586&&Hash(final.CopyShipPool())=="1CB1E912C9FD8105A9D984D6AC2BE4A06A271E6AFF5852D173B8E50549B129DF"&&Hash(final.CopyFleetPool())=="DDCA8B7AA72DA79161A4851F8042ABD6DB8FCE0C69895C50BAE3FBC78D72B429","Original453 unsigned seed final primary pools");
            });
            check("Startup source stages counters and returned pools stay independently copied",delegate
            {
                var input=File.ReadAllBytes(map);var state=PracticeMapStartup.CreateBssb202(programs,input,731);Need(Hash(input)==PracticeMapStartup.SupportedSourceHash,"Caller SCN changed");input[0]^=255;
                var first=state.Stages[0];var second=state.Stages[1];var counters=first.CopyOwnerCounters();Need(counters[2]==2&&second.CopyOwnerCounters()[2]==6,"Stage-specific reset owner counters");counters[2]=99;
                var s=first.CopyShipPool();s[0]^=255;var source=state.CopySource();source[0]^=255;
                Need(first.CopyOwnerCounters()[2]==2&&Hash(state.CopySource())==PracticeMapStartup.SupportedSourceHash&&BitConverter.ToInt32(first.CopyShipPool(),4*268)==22&&BitConverter.ToInt32(state.Stages[3].CopyShipPool(),4*268)==0,"Cross-stage/source mutable escape");
            });
            check("Ordinary startup rejects different truncated and modified map identities",delegate
            {
                var valid=File.ReadAllBytes(map);var modified=(byte[])valid.Clone();modified[modified.Length-1]^=1;string before=Hash(modified);
                Need(Reject(delegate{PracticeMapStartup.CreateBssb202(programs,new byte[0],731);})&&Reject(delegate{PracticeMapStartup.CreateBssb202(programs,new byte[valid.Length-1],731);})&&Reject(delegate{PracticeMapStartup.CreateBssb202(programs,modified,731);})&&Hash(modified)==before,"Unadmitted SCN published setup");
            });
            evidence("Practice formation matches 400 complete original pool and random-state cases",delegate
            {
                int cases=0,resets=0;foreach(string line in File.ReadLines(Path.Combine(Path.GetDirectoryName(assets),"Evidence","Placement-451.csv")))
                {
                    if(line.StartsWith("Case,"))continue;var row=line.Split(',');Need(row.Length==12&&int.Parse(row[0])==cases,"Placement451 evidence order");var p=FormationFixture(int.Parse(row[1]),int.Parse(row[2]),int.Parse(row[3]));Need(Hash(p[0])==row[8]&&Hash(p[1])==row[9],"Original451 input fixture "+cases);
                    var result=PracticeFormationPlacement.Apply(p[0],p[1],int.Parse(row[2]),uint.Parse(row[4]));Need(Hash(result.CopyShipPool())==row[10]&&Hash(result.CopyFleetPool())==row[11]&&result.RandomState==uint.Parse(row[5])&&result.RandomCalls==int.Parse(row[6])&&result.Restarts==int.Parse(row[7]),"Original451 full output "+cases);if(result.Restarts>0)resets++;cases++;
                }
                Need(cases==400&&resets==40,"Placement evidence completeness");
            });
            evidence("Ordinary startup matches twenty whole-original seeded primary-pool stages",delegate
            {
                var seen=new HashSet<string>();var cache=new Dictionary<uint,PracticeMapStartup>();int count=0;
                foreach(string line in File.ReadLines(Path.Combine(Path.GetDirectoryName(assets),"Evidence","Map-Startup-453.csv")))
                {
                    if(line.StartsWith("Seed,"))continue;var row=line.Split(',');Need(row.Length==12,"Map startup evidence row");uint seed=uint.Parse(row[0]);int n=int.Parse(row[1]);Need(n>=0&&n<4&&seen.Add(seed+":"+n),"Duplicate/unadmitted startup stage");
                    if(!cache.ContainsKey(seed))cache.Add(seed,PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(map),seed));var stage=cache[seed].Stages[n];
                    Need(Hash(stage.CopyShipPool())==row[3]&&Hash(stage.CopyFleetPool())==row[4]&&stage.RandomState==uint.Parse(row[2])&&stage.NextInactiveFleet==int.Parse(row[11]),"Original453 primary pools/random "+seed+"/"+n);var counters=stage.CopyOwnerCounters();for(int i=0;i<5;i++)Need(counters[i]==uint.Parse(row[5+i]),"Original453 owner counter "+i);count++;
                }
                Need(count==20&&cache.Count==5,"Incomplete seeded startup evidence");
            });
        }
    }
}
