using System;
using System.IO;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeProducedFlightChecks
    {
        static readonly string[] Original={
            "2,0,22345,231,423,235,423,F1D76A06852A54A30A473669CBCCADEA6E21846D9928722A56CD2D331A185168,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A",
            "2,1,22385,231,423,235,423,F1D76A06852A54A30A473669CBCCADEA6E21846D9928722A56CD2D331A185168,7E9AE19E305EA03AA4E39E7CD82FE43A7D28D0523C850B203ADB5554D2BEA588",
            "2,32,23625,240,423,241,423,9352D6A44AA304ECCEE74261E8FD10ED5BAF3ABBA2C770D6EB71B0F329DFF5B2,7E9AE19E305EA03AA4E39E7CD82FE43A7D28D0523C850B203ADB5554D2BEA588",
            "2,128,27465,288,401,289,397,965CE91BE3E69AB3B7D3FBB650BFF186724ABC2E956F9DA6CD90E36DE51DA8E0,7E9AE19E305EA03AA4E39E7CD82FE43A7D28D0523C850B203ADB5554D2BEA588",
            "2,320,35145,384,305,385,301,7F24EE7E78A02B7363334202FC05057BC1E187032BD8709AA76464A13D5AFB5C,7E9AE19E305EA03AA4E39E7CD82FE43A7D28D0523C850B203ADB5554D2BEA588",
            "2,321,35185,385,304,385,301,C5C104BA04295B42AF435250339E348F748CC086F8E73335F69B65B3BAAD086F,7E9AE19E305EA03AA4E39E7CD82FE43A7D28D0523C850B203ADB5554D2BEA588",
            "2,322,35210,385,304,385,301,C5C104BA04295B42AF435250339E348F748CC086F8E73335F69B65B3BAAD086F,7E9AE19E305EA03AA4E39E7CD82FE43A7D28D0523C850B203ADB5554D2BEA588",
            "6,0,22345,231,423,235,423,F1D76A06852A54A30A473669CBCCADEA6E21846D9928722A56CD2D331A185168,B97C732B6E8517E7DA1E5D9AFCB6F6972C59DAF03D81C1F38F928115E716CE2C",
            "6,32,23625,240,423,241,423,9352D6A44AA304ECCEE74261E8FD10ED5BAF3ABBA2C770D6EB71B0F329DFF5B2,03D86BE50CA3583763016C44941898B51C0514256C5B75018F429B7F014BC316",
            "6,128,27465,288,401,289,397,965CE91BE3E69AB3B7D3FBB650BFF186724ABC2E956F9DA6CD90E36DE51DA8E0,03D86BE50CA3583763016C44941898B51C0514256C5B75018F429B7F014BC316",
            "6,321,35185,385,304,385,301,C5C104BA04295B42AF435250339E348F748CC086F8E73335F69B65B3BAAD086F,03D86BE50CA3583763016C44941898B51C0514256C5B75018F429B7F014BC316",
            "6,322,35210,385,304,385,301,C5C104BA04295B42AF435250339E348F748CC086F8E73335F69B65B3BAAD086F,03D86BE50CA3583763016C44941898B51C0514256C5B75018F429B7F014BC316",
        };
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static bool Reject(Action a){try{a();return false;}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}catch(NotSupportedException){return true;}}
        static void Compare(PracticeProducedFlight c,string[] r){var p=c.Projectiles;Need(r.Length==9&&c.Updates==int.Parse(r[1])&&c.Clock==uint.Parse(r[2])&&p[0].X==int.Parse(r[3])&&p[0].Y==int.Parse(r[4])&&p[1].X==int.Parse(r[5])&&p[1].Y==int.Parse(r[6])&&Hash(c.CopyTimedPool())==r[7]&&Hash(c.CopyFleetPool())==r[8],"Original617 complete flight state "+r[0]+"/"+r[1]);}
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var requests=new PracticeFleetMemberRequests[2];for(int p=0;p<2;p++){var joined=PracticeVisibleCandidates.Create(programs,map,PracticeOwnerVisibilityStudyState.Create(programs,map,p,1,0,1,0,0,0).Execute());var prefix=PracticeFleetMemberPreparation.CreateFromVisibility(programs,map,joined,8,2,false,0).Advance().Advance().Advance();requests[p]=PracticeFleetMemberRequests.CreateFromPreparation(prefix,0,0).Execute();}
            Func<int,PracticeProducedLaunch> launch=delegate(int p){return PracticeProducedLaunch.Create(programs,requests[p],1,0).Execute();};Func<int,PracticeProducedFlight> create=delegate(int p){return PracticeProducedFlight.Create(launch(p));};
            check("Original-map weapon22 flight matches twelve original617 complete golden states",delegate
            {
                foreach(string line in Original){var r=line.Split(',');var c=create((int.Parse(r[0])-2)/4);int steps=int.Parse(r[1]);for(int n=0;n<steps;n++)c=c.CanStep?c.Step():c.ToBoundary();Compare(c,r);}Need(Original.Length==12,"617 golden count");
            });
            check("Weapon22 flight preserves original rounding and stops before second-record deadline",delegate
            {
                var c=create(0);var first=c.Step();Need(first.Projectiles[0].X==231&&first.Projectiles[0].Y==423&&first.Projectiles[1].X==235&&first.Projectiles[1].Y==423,"First update should retain rounded coordinates");var batch=c.UpTo(32);Need(batch.Clock==23625&&batch.Projectiles[0].X==240&&batch.Projectiles[1].X==241&&batch.Projectiles[0].Y==423&&batch.Projectiles[1].Y==423,"Original32update positions");var stop=batch.ToBoundary();Need(stop.Clock==35210&&stop.FirstDeadline==35211&&stop.Projectiles[0].Deadline==35278&&stop.Projectiles[1].Deadline==35211&&stop.Projectiles[0].X==385&&stop.Projectiles[0].Y==304&&stop.Projectiles[1].X==385&&stop.Projectiles[1].Y==301&&stop.Updates==322&&stop.LastStoppedAtRecord==1&&stop.AtBoundary&&!stop.CanStep&&stop.UsedBoundaryJump,"Original last future visit");
            });
            check("Future-only weapon22 snapshots hold ammo requests RNG and parent launch",delegate
            {
                var before=create(0);string t=Hash(before.CopyTimedPool()),s=Hash(before.CopyShipPool()),o=Hash(before.CopyOrderPool());var stop=before.ToBoundary();var b=stop.CopyTimedPool();b[0]=99;var f=stop.CopyFleetPool();f[0]^=255;Need(before.Clock==22345&&before.Updates==0&&Hash(before.CopyTimedPool())==t&&Hash(stop.CopyShipPool())==s&&Hash(stop.CopyOrderPool())==o&&stop.Launch.Ammunition(0)==7&&stop.Launch.Ammunition(1)==7&&stop.Launch.RandomState==4052711439u&&stop.Launch.Clock==22345&&stop.CopyTimedPool()[0]!=99&&Hash(stop.CopyFleetPool())!=Hash(f),"Future copy changed owned launch or exposes mutable data");
            });
            check("Weapon22 flight excludes pending capacity other-timer and impact updates",delegate
            {
                Need(Reject(delegate{PracticeProducedFlight.Create(null);})&&Reject(delegate{PracticeProducedFlight.Create(PracticeProducedLaunch.Create(programs,requests[0],1,0));})&&Reject(delegate{PracticeProducedFlight.Create(PracticeProducedLaunch.Create(programs,requests[0],1,1).Execute());})&&Reject(delegate{PracticeProducedFlight.Create(PracticeProducedLaunch.Create(programs,requests[0],1,2).Execute());})&&Reject(delegate{PracticeProducedFlight.Create(PracticeProducedLaunch.Create(programs,requests[0],3,0).Execute());}),"Uncaptured flight fixture admitted");var stop=create(0).ToBoundary();Need(Reject(delegate{stop.Step();})&&Reject(delegate{stop.ToBoundary();})&&Reject(delegate{stop.UpTo(32);})&&Reject(delegate{create(0).UpTo(0);})&&Reject(delegate{create(0).UpTo(129);})&&stop.Clock==35210,"Impact/batch boundary entered");
            });
            evidence("Canonical weapon22 flight matches all646 original617 +40 observations",delegate
            {
                string path=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Produced-Flight-617.csv");Need(Hash(File.ReadAllBytes(path))=="160B3CAADFE3000E8F684D5E31507458CCF19839A428782476A1B69BC2FD49A9","617 selected evidence identity");int history=-1,count=0,histories=0;PracticeProducedFlight c=null;
                foreach(string line in File.ReadLines(path)){if(line.StartsWith("History,"))continue;var r=line.Split(',');int h=int.Parse(r[0]);if(h!=history){Need((histories==0?h==2:h==6)&&int.Parse(r[1])==0,"617 canonical history order");history=h;c=create((h-2)/4);histories++;}else c=uint.Parse(r[2])==c.Clock+40?c.Step():c.ToBoundary();Compare(c,r);count++;}Need(histories==2&&count==646&&c.AtBoundary,"617 complete canonical histories");
            });
        }
    }
}
