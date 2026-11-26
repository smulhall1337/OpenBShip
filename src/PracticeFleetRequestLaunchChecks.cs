using System;
using System.IO;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeFleetRequestLaunchChecks
    {
        // Full original551 snapshot hashes; generated from retained capture, not this port.
        static readonly string[] Original={
            "0,14,0,14845,14,FC2669B5A880ECAB2784794441555CDBA2C1227954FC3875D4F6FCF391ED196D,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,6D6ECE370A36E4FB45D012262A9AE1CFBAB70B20050BCBDE63A2CFDBF817C883,E6044CD3BAE2BC9E13F892FE0B3DCDCDC5FB0038A4397C217722471FA98D1AD7,EA2901078E939A9183DE674616D2B406C1BAFC4BF4BF09EA11A0DD837AD791AD,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,2EB2732234FE054111E855F02324474BF9AC309E6EDF95D917881F20949DF69F",
            "1,14,1,14845,15,FC2669B5A880ECAB2784794441555CDBA2C1227954FC3875D4F6FCF391ED196D,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,6D6ECE370A36E4FB45D012262A9AE1CFBAB70B20050BCBDE63A2CFDBF817C883,77708D26BB9C0950FDD931E7EEE31D43821DD979A6690848FE5E3BE939259C55,B87D09A3E529A46FABB92A8BACBD42DB20F58F0DD30FBCCDE6C6A6D5E851B141,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,FAC9DB143A8A766715E576BE14B22283D151A59EDDB6EAB59150061579687DD9",
            "2,14,2,14845,-1,FC2669B5A880ECAB2784794441555CDBA2C1227954FC3875D4F6FCF391ED196D,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,6D6ECE370A36E4FB45D012262A9AE1CFBAB70B20050BCBDE63A2CFDBF817C883,2F824AD68268C60D7866C718153F8C8DE05B9D2B5D1B2EC4746DE1AE064FA738,FC2669B5A880ECAB2784794441555CDBA2C1227954FC3875D4F6FCF391ED196D,D0ABF2C19E6E28368274DC53DB1AC756ECBC8E68030A36859EE30D55E7C1DB95,1C29741C9F1812F17ACA26D61A522C8385A1B79A3632EBEC5559C4436438B83A,2F824AD68268C60D7866C718153F8C8DE05B9D2B5D1B2EC4746DE1AE064FA738",
            "3,19,0,22345,14,1D8003379B1415CC67E71B2F68438632DC10492B5D0DD5496E8A1B6D42A2706A,22F4F127CF53C5EE15F746681A7F9E2B178B283F04F389C5E583630238E8415F,9064E6D8354077014A795CB9CA77DC909609544069757E4D3C06E860D869BEF7,E6044CD3BAE2BC9E13F892FE0B3DCDCDC5FB0038A4397C217722471FA98D1AD7,9F5F7B7F9FB1323E68E1B297726F47742996FF9E750DFB0E3104BB43955D0333,22F4F127CF53C5EE15F746681A7F9E2B178B283F04F389C5E583630238E8415F,61846EA7DE6127A0D9523D7EDB37594D88E3EFCE6320C1C2515A83E23EC4F8EC,0E89E5BEAC63A5870BB5BCBED4C4D26FC3B14DE6B4EA769624ADD34276E3F42D",
            "4,19,1,22345,15,1D8003379B1415CC67E71B2F68438632DC10492B5D0DD5496E8A1B6D42A2706A,22F4F127CF53C5EE15F746681A7F9E2B178B283F04F389C5E583630238E8415F,9064E6D8354077014A795CB9CA77DC909609544069757E4D3C06E860D869BEF7,77708D26BB9C0950FDD931E7EEE31D43821DD979A6690848FE5E3BE939259C55,125F9CAD5F066E16D72F411CC8A8977F47079F4CE0B3ECA008B548D00CB145F7,22F4F127CF53C5EE15F746681A7F9E2B178B283F04F389C5E583630238E8415F,61846EA7DE6127A0D9523D7EDB37594D88E3EFCE6320C1C2515A83E23EC4F8EC,4112C33673D3D43447461A255C17C914A1CA301F4AF60795F58634A7FB612454",
            "5,19,2,22345,-1,1D8003379B1415CC67E71B2F68438632DC10492B5D0DD5496E8A1B6D42A2706A,22F4F127CF53C5EE15F746681A7F9E2B178B283F04F389C5E583630238E8415F,9064E6D8354077014A795CB9CA77DC909609544069757E4D3C06E860D869BEF7,2F824AD68268C60D7866C718153F8C8DE05B9D2B5D1B2EC4746DE1AE064FA738,1D8003379B1415CC67E71B2F68438632DC10492B5D0DD5496E8A1B6D42A2706A,22F4F127CF53C5EE15F746681A7F9E2B178B283F04F389C5E583630238E8415F,61846EA7DE6127A0D9523D7EDB37594D88E3EFCE6320C1C2515A83E23EC4F8EC,2F824AD68268C60D7866C718153F8C8DE05B9D2B5D1B2EC4746DE1AE064FA738",
        };
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}catch(NotSupportedException){return true;}}
        static void Pools(PracticeFleetRequestLaunch c,string[] row,int offset)
        {
            Need(Hash(c.CopyShipPool())==row[offset]&&Hash(c.CopyFleetPool())==row[offset+1]&&Hash(c.CopyOrderPool())==row[offset+2]&&Hash(c.CopyTimedPool())==row[offset+3],"Original551 complete pools case "+row[0]+" / "+offset);
        }
        internal static void Run(Action<string,Action> check,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var input=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));
            Func<uint,PracticeMapStartup> map=delegate(uint seed){return PracticeMapStartup.CreateBssb202(programs,input,seed);};
            check("Generated member launch matches all six original551 complete before and after pools",delegate
            {
                var startup=map(731);int cases=0;
                foreach(string line in Original)
                {
                    var r=line.Split(',');int family=int.Parse(r[1])==14?0:1,capacity=int.Parse(r[2]);
                    var first=PracticeFleetRequestLaunch.Create(programs,startup,family,capacity);Pools(first,r,5);
                    Need(first.Clock==uint.Parse(r[3])&&!first.Executed&&first.CreatedObjects==0&&first.LastTargetFleet==-1,"551 initial scalars");
                    var c=first.Execute();Pools(c,r,9);Need(c.Executed&&c.Clock==first.Clock&&c.LastTargetFleet==int.Parse(r[4])&&c.ConsumedRequests==2&&c.AttemptedObjects==2&&c.CreatedObjects==(capacity==0?2:capacity==1?1:0)&&c.RequestRandomState==first.RequestRandomState,"551 final scalars");cases++;
                }
                Need(cases==6,"551 complete fixture count");
            });
            check("Full and nearly full projectile pools consume requests without inventing ammunition use",delegate
            {
                var startup=map(731);for(int family=0;family<2;family++)for(int capacity=0;capacity<3;capacity++)
                {
                    var first=PracticeFleetRequestLaunch.Create(programs,startup,family,capacity);var c=first.Execute();int initial=family==0?10:1;
                    Need(first.RequestCount(0)==initial&&first.RequestCount(1)==initial&&c.RequestCount(0)==initial-1&&c.RequestCount(1)==initial-1,"One removed request per source");
                    Need(c.Ammunition(0)==(capacity==2?7:6)&&c.Ammunition(1)==(capacity==0?6:7),"Allocation-specific ammo");
                    if(capacity==1)Need(c.CopyCreatedSlots().Length==1&&c.CopyCreatedSlots()[0]==79&&c.Projectiles[0].SourceUnit==30,"First source receives last slot");
                }
            });
            check("Copied launch exposes projectile destinations and retains capacity-only records",delegate
            {
                var startup=map(731);var c=PracticeFleetRequestLaunch.Create(programs,startup,0,0).Execute();Need(c.Projectiles.Count==2,"Two projectile records");
                var seen=c.Projectiles;Need(seen[0].Slot==0&&seen[0].Weapon==14&&seen[0].SourceUnit==30&&seen[0].TargetFleet==15&&seen[1].Slot==1&&seen[1].SourceUnit==31&&seen[1].TargetFleet==14&&seen[0].Deadline>c.Clock&&seen[1].Deadline>c.Clock,"Stored projectile identities/deadlines");
                var filled=PracticeFleetRequestLaunch.Create(programs,startup,1,1);var after=filled.Execute();var before=filled.CopyTimedPool();var timed=after.CopyTimedPool();for(int n=0;n<79*268;n++)Need(before[n]==timed[n],"Capacity fixture was stepped or overwritten");
            });
            check("Launch snapshots preserve predecessors and do not leak mutable pools or slot arrays",delegate
            {
                var c=PracticeFleetRequestLaunch.Create(programs,map(731),0,0);string ships=Hash(c.CopyShipPool()),orders=Hash(c.CopyOrderPool());var next=c.Execute();
                var s=next.CopyShipPool();s[0]^=255;var o=next.CopyOrderPool();o[0]^=255;var t=next.CopyTimedPool();t[0]=99;var f=next.CopyFleetPool();f[0]^=255;var slots=next.CopyCreatedSlots();slots[0]=79;
                Need(Hash(c.CopyShipPool())==ships&&Hash(c.CopyOrderPool())==orders&&!c.Executed&&c.Ammunition(0)==7&&c.RequestCount(0)==10&&next.CopyShipPool()[0]!=s[0]&&next.CopyOrderPool()[0]!=o[0]&&next.CopyTimedPool()[0]!=99&&next.CopyFleetPool()[0]!=f[0]&&next.CopyCreatedSlots()[0]==0,"Mutable snapshot alias");
            });
            check("Member launch rejects uncaptured inputs and repeated execution",delegate
            {
                var startup=map(731);Need(Reject(delegate{PracticeFleetRequestLaunch.Create(programs,startup,-1,0);})&&Reject(delegate{PracticeFleetRequestLaunch.Create(programs,startup,2,0);})&&Reject(delegate{PracticeFleetRequestLaunch.Create(programs,startup,0,-1);})&&Reject(delegate{PracticeFleetRequestLaunch.Create(programs,startup,0,3);})&&Reject(delegate{PracticeFleetRequestLaunch.Create(programs,map(0),0,0);}),"Uncaptured input admitted");
                var c=PracticeFleetRequestLaunch.Create(programs,startup,0,0).Execute();Need(Reject(delegate{c.Execute();})&&Reject(delegate{c.RequestCount(2);})&&Reject(delegate{c.Ammunition(-1);})&&Reject(delegate{c.Eligibility(2);}),"Invalid repeat/member accepted");
            });
            check("Copied numeric launch leaves original request-only and preparation endpoints intact",delegate
            {
                var startup=map(731);string s=Hash(startup.Stages[3].CopyShipPool()),f=Hash(startup.Stages[3].CopyFleetPool());
                var request=PracticeFleetMemberRequests.Create(programs,startup,3,0,2,0,0,1,0);var ready=request.Execute();var launched=PracticeFleetRequestLaunch.Create(programs,startup,0,0).Execute();
                Need(request.QueuedCells.Count==0&&ready.QueuedCells.Count==20&&Hash(request.CopyShipPool())==Hash(ready.CopyShipPool())&&launched.Ammunition(0)==6&&Hash(startup.Stages[3].CopyShipPool())==s&&Hash(startup.Stages[3].CopyFleetPool())==f,"Parent or request-only state changed");
            });
        }
    }
}
