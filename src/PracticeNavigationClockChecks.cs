using System;
using System.IO;
using System.Security.Cryptography;

namespace FleetLab
{
    internal static class PracticeNavigationClockChecks
    {
        // Original964 only, exported by data-only966. Histories1/2 are identical
        // within this window; the later removal lies outside these128 frames.
        static readonly string[] Original = {
            "964,0,BCCDE816CF220CE40F47EAB66CD8216746011E3A4CD5EFBDB8E72CA2DF387275,4B1D2D5DC37C8437DD8044FAFADF308E338842C84F01F73CAB4D5957EDA69D23,120,120,130,120,4,1,17425,32,4,17385,17425,17425,40,0,4,1",
            "964,1,0AC7F6B75F91A0CDF106F8478E55B0B437BC34CDEE57E92B3FAD48947B41FEC9,DDBAA9EAFBB6D7176BC70AD1C67E435AEF8C2E5C967C0D6CD9B8F86DCF2C709F,110,93,110,90,4,1,16105,32,4,17385,17425,17425,40,1,4,1",
            "964,2,0AC7F6B75F91A0CDF106F8478E55B0B437BC34CDEE57E92B3FAD48947B41FEC9,DDBAA9EAFBB6D7176BC70AD1C67E435AEF8C2E5C967C0D6CD9B8F86DCF2C709F,110,93,110,90,4,1,16105,32,4,17385,17425,17425,40,1,4,1",
            "964,3,0AC7F6B75F91A0CDF106F8478E55B0B437BC34CDEE57E92B3FAD48947B41FEC9,A57E27813F7707BD28A8F209AE66ED909EFA50DD70A776A6F91288449BE29FCD,110,93,110,90,4,1,16145,32,4,17385,17425,17425,40,0,4,1"
        };
        static void Need(bool value,string why){if(!value)throw new InvalidDataException(why);}
        static byte[] Hash(byte[] b){using(var h=SHA256.Create())return h.ComputeHash(b);}
        static string Hex(byte[] b){return BitConverter.ToString(b).Replace("-","");}
        static string H(byte[] b){return Hex(Hash(b));}
        static void I(byte[] b,ref int at,int v){BitConverter.GetBytes(v).CopyTo(b,at);at+=4;}
        static PracticeAiDriverContext Context(int phase){return new PracticeAiDriverContext((uint)phase,1,0,0,0x13579bdf,0x2468ace0,4);}
        static int Sample(int history,int poll){return (history==1||history==2)&&poll==95?12345+94*40:12345+poll*40;}
        static PracticeNavigationClockState Initial(PracticeGoalRouteStudyBasis basis,PracticeOwnerVisibilityRules visibility,int history)
        {
            var s=basis.CreateMovementComparison(history+8);var f=s.CopyFleetPool();f[249*492+467]=3;f[249*492+480]=1;
            return new PracticeNavigationClockState(new PracticeNavigationMovementState(f,s.Backing,s.Grid,s.CopyTerrainPool(),s.CopySecondaryPool(),s.Schedule,4),new OriginalFrameClock(12265,12305,12345,40),visibility);
        }
        static PracticeNavigationClockState World(PracticeNavigationClockState s,int history,int poll)
        {
            var moved=PracticeGoalRouteStudyBasis.ApplyMovementWorldEvent(s.Movement,history+8,poll);
            return new PracticeNavigationClockState(moved,s.Clock,s.Visibility);
        }
        // Same documented byte layout as original-only966, independently filled
        // from maintained state. Unaligned stack scratch is explicitly excluded.
        static byte[] Digest(PracticeNavigationMovementState s,OriginalFrameClock before,OriginalFrameClock body,
            OriginalFrameClock after,int sample,int phase,int? alignedScratch)
        {
            var b=new byte[451726];s.CopyFleetPool().CopyTo(b,0);s.Backing.CopyRegion().CopyTo(b,123000);s.Grid.CopyCells().CopyTo(b,201560);s.CopyTerrainPool().CopyTo(b,205656);int at=451656;
            foreach(var c in new[]{before,body,after})foreach(uint v in new[]{c.PreviousMilliseconds,c.CurrentMilliseconds,c.GameMilliseconds,c.DeltaMilliseconds})I(b,ref at,unchecked((int)v));
            foreach(int v in new[]{sample,phase,s.Schedule.BatchStart,unchecked((int)s.Schedule.CompletedCycles)})I(b,ref at,v);
            BitConverter.GetBytes((short)s.SelectedWaypoint).CopyTo(b,at);at+=2;I(b,ref at,alignedScratch??0);Need(at==b.Length,"Clock/navigation digest extent");return Hash(b);
        }
        static byte[] Chain(byte[] chain,byte[] digest){var b=new byte[64];chain.CopyTo(b,0);digest.CopyTo(b,32);return Hash(b);}
        static void SameClock(OriginalFrameClock c,uint[] v){Need(c.PreviousMilliseconds==v[0]&&c.CurrentMilliseconds==v[1]&&c.GameMilliseconds==v[2]&&c.DeltaMilliseconds==v[3],"Original964 complete clock fields");}
        static byte[] Read(BinaryReader r,int count){var b=r.ReadBytes(count);Need(b.Length==count,"Truncated clock/navigation fixture");return b;}
        static uint[] ReadClock(BinaryReader r){return new[]{r.ReadUInt32(),r.ReadUInt32(),r.ReadUInt32(),r.ReadUInt32()};}
        static byte[] Patch(BinaryReader r,byte[] basis)
        {
            int count=r.ReadInt32(),last=-1;Need(count>=0&&count<=basis.Length,"Clock/navigation patch count");var b=(byte[])basis.Clone();
            for(int n=0;n<count;n++){int at=r.ReadInt32();Need(at>last&&at<b.Length,"Clock/navigation patch bounds/order");last=at;b[at]=r.ReadByte();}return b;
        }
        static bool Reject(Action a){try{a();return false;}catch(ArgumentException){return true;}catch(NotSupportedException){return true;}catch(InvalidDataException){return true;}}
        static string Identity(PracticeNavigationClockState s){return H(s.Movement.CopyFleetPool())+H(s.Movement.Backing.CopyRegion())+H(s.Movement.Grid.CopyCells());}
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");PracticeGoalRouteStudyBasis basis=null;PracticeOwnerVisibilityRules visibility=null;
            Action prepare=delegate{if(basis==null){basis=PracticeGoalRouteStudyBasis.Create(programs,PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731));visibility=PracticeOwnerVisibilityRules.Load(programs);}};
            check("Clock navigation matches four original-only966 initial and128-frame chains from map-built inputs",delegate{
                prepare();
                for(int history=0;history<4;history++)
                {
                    var v=Original[history].Split(',');Need(v.Length==20&&int.Parse(v[1])==history,"Original966 metadata");var s=Initial(basis,visibility,history);var initial=s;string frozen=Identity(initial);
                    Need(Hex(Digest(s.Movement,s.Clock,s.Clock,s.Clock,12345,1,null))==v[2],"Original966 independent initial state");var chain=new byte[32];int zeros=0,passes=0,clears=0;
                    for(int poll=0;poll<128;poll++)
                    {
                        s=World(s,history,poll);var bf=s.Movement.CopyFleetPool();var u=s.AdvanceFrame((uint)Sample(history,poll),Context(poll+1));s=u.State;
                        chain=Chain(chain,Digest(s.Movement,u.Before.Clock,u.BodyClock,s.Clock,Sample(history,poll),poll+1,u.Movement.AlignedCallerScratchAfter));
                        if(s.Clock.CurrentMilliseconds==1)zeros++;passes+=u.Movement.OwnerVisibilityPasses;if(bf[249*492+467]==3&&s.Movement.CopyFleetPool()[249*492+467]==0)clears++;
                    }
                    Need(Hex(chain)==v[3]&&frozen==Identity(initial),"Original966 carried chain/caller immutability");var f=s.Movement.CopyFleetPool();var task=s.Movement.Backing.CopyTaskRecord(16);
                    int[] final={BitConverter.ToInt16(f,16*492+12),BitConverter.ToInt16(f,16*492+14),BitConverter.ToInt16(f,16*492+40),BitConverter.ToInt16(f,16*492+50),BitConverter.ToInt32(task,200),BitConverter.ToInt32(task,304),BitConverter.ToInt32(task,4),s.Movement.Schedule.BatchStart,(int)s.Movement.Schedule.CompletedCycles};
                    for(int n=0;n<final.Length;n++)Need(final[n]==int.Parse(v[n+4]),"Original966 final field");SameClock(s.Clock,new[]{uint.Parse(v[13]),uint.Parse(v[14]),uint.Parse(v[15]),uint.Parse(v[16])});
                    Need(zeros==int.Parse(v[17])&&passes==int.Parse(v[18])&&clears==int.Parse(v[19]),"Original966 measured boundaries");
                }
            });
            check("Clock navigation zero-reset epilogue order is observed through the actual replan timestamp",delegate{
                prepare();var s=Initial(basis,visibility,1);for(int p=0;p<95;p++)s=World(s,1,p).AdvanceFrame((uint)Sample(1,p),Context(p+1)).State;
                Need(s.Movement.CopyFleetPool()[16*492+482]==254,"Original964 collision94 boundary");string before=Identity(s);var u=s.AdvanceFrame(16105,Context(96));
                var wrong=s.Movement.AdvanceNeutralDriverPoll(1,Context(96),visibility);
                Need(u.BodyClock.GameMilliseconds==16105&&u.State.Clock.GameMilliseconds==1&&u.State.Clock.CurrentMilliseconds==1&&u.State.Clock.DeltaMilliseconds==1&&
                    BitConverter.ToInt32(u.State.Movement.Backing.CopyTaskRecord(16),4)==16105&&BitConverter.ToInt32(wrong.State.Backing.CopyTaskRecord(16),4)==1&&before==Identity(s),"Early epilogue must change source task time");
                var next=u.State.AdvanceFrame(16185,Context(97));Need(next.BodyClock.DeltaMilliseconds==1&&next.State.Clock.DeltaMilliseconds==16184,"Carried source zero-reset state");
            });
            check("Clock navigation study preserves bounded windows reset and single versus grouped frame history",delegate{
                prepare();var initial=new PracticeNavigationMovementStudyState(basis,15,visibility);string before=H(initial.State.CopyFleetPool());var a=initial.Advance(96);var b=initial.Advance(32).Advance(32).Advance(32);
                Need(a.UsesRecoveredClock&&!a.IsRepeatedCallback&&a.WindowPolls==128&&a.Polls==96&&a.TerrainCollisionVisits==1&&a.ReplanEntries==1&&a.NeutralPasses==3&&a.Clock.GameMilliseconds==1&&
                    H(a.State.CopyFleetPool())==H(b.State.CopyFleetPool())&&H(a.State.Backing.CopyRegion())==H(b.State.Backing.CopyRegion())&&before==H(initial.State.CopyFleetPool()),"Grouped carried comparison");
                var end=a.Advance(1024);var reset=end.Reset();Need(!end.CanAdvance&&end.Polls==128&&reset.Polls==0&&reset.Clock.CurrentMilliseconds==12305&&reset.NeutralPasses==0&&reset.Preset==15,"Clock window/reset");
                Need(new PracticeNavigationMovementStudyState(basis,14,visibility).Advance(128).NeutralPasses==4&&new PracticeNavigationMovementStudyState(basis,16,visibility).Advance(95).FleetCollisionVisits==1,"Three compared UI profiles");
            });
            check("Neutral clock navigation leaves legacy prephase API and earlier study presets intact",delegate{
                prepare();var s=Initial(basis,visibility,0);var old=s.Movement.AdvanceOfflinePoll(12345);var joined=s.Movement.AdvanceNeutralDriverPoll(12345,Context(32),visibility);
                Need(old.DriverContext==null&&old.OwnerVisibilityPasses==0&&old.AlignedCallerScratchAfter==null&&old.State.CopyFleetPool()[249*492+467]==3&&joined.OwnerVisibilityPasses==1&&joined.AlignedCallerScratchAfter==0x2468ace0&&joined.State.CopyFleetPool()[249*492+467]==0,"Explicit opt-in preserves old behavior");
                var legacy=new PracticeNavigationMovementStudyState(basis,13);Need(!legacy.UsesRecoveredClock&&legacy.IsRepeatedCallback&&legacy.Clock==null&&legacy.WindowPolls==1&&!legacy.Advance(1).CanAdvance,"Prior callback preset preserved");
            });
            check("Clock neutral driver rejects unexamined callers phases visibility and unowned dispatch transactionally",delegate{
                prepare();var s=Initial(basis,visibility,0);string before=Identity(s);
                Need(Reject(delegate{s.AdvanceFrame(12345,Context(0));})&&Reject(delegate{s.AdvanceFrame(12345,Context(129));})&&
                    Reject(delegate{s.AdvanceFrame(12345,new PracticeAiDriverContext(1,1,0,0,0,0x2468ace0));})&&Reject(delegate{s.AdvanceFrame(12345,new PracticeAiDriverContext(1,1,0,0,0x13579bdf,0x2468ace0,-1));}),"Unsupported driver context");
                var f=s.Movement.CopyFleetPool();f[10*492+467]=1;var bad=new PracticeNavigationMovementState(f,s.Movement.Backing,s.Movement.Grid,s.Movement.CopyTerrainPool(),s.Movement.CopySecondaryPool(),s.Movement.Schedule,4);
                Need(Reject(delegate{bad.AdvanceNeutralDriverPoll(12345,Context(1),visibility);}),"Unsupported visibility mixture");
                f=s.Movement.CopyFleetPool();BitConverter.GetBytes(11).CopyTo(f,30*492);BitConverter.GetBytes((short)-1).CopyTo(f,30*492+6);f[30*492+485]=0;BitConverter.GetBytes(0).CopyTo(f,30*492+476);BitConverter.GetBytes((short)-1).CopyTo(f,30*492+40);
                bad=new PracticeNavigationMovementState(f,s.Movement.Backing,s.Movement.Grid,s.Movement.CopyTerrainPool(),s.Movement.CopySecondaryPool(),s.Movement.Schedule,4);
                Need(Reject(delegate{bad.AdvanceNeutralDriverPoll(12345,Context(1),visibility);})&&before==Identity(s)&&s.Clock.CurrentMilliseconds==12305,"Unowned dispatcher/caller immutability");
            });
            evidence("Clock navigation matches all512 original964 complete snapshots and all250 heap rows",delegate{
                prepare();string path=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Clock-Navigation-964.bin");Need(H(File.ReadAllBytes(path))=="EE3214BB3E0C656E88B4C1894D888708081716333899F3B896704198E528EA0D","Original964 private identity");
                using(var r=new BinaryReader(File.OpenRead(path)))
                {
                    Need(System.Text.Encoding.ASCII.GetString(Read(r,8))=="NM964001"&&r.ReadInt32()==1&&r.ReadInt32()==4&&r.ReadInt32()==128&&r.ReadInt32()==512,"Original964 header");
                    Read(r,402000);var basisF=Read(r,123000);var zeroTerrain=Read(r,246000);Read(r,78000);var basisR=Read(r,78560);Read(r,64000);var rows=Read(r,1024000);
                    PracticeNavigationClockState s=null;byte[] expectedRows=null;int count=0;
                    for(;;)
                    {
                        int id=r.ReadInt32();if(id==-1)break;var before=ReadClock(r);var body=ReadClock(r);var after=ReadClock(r);uint scratch=r.ReadUInt32();
                        int history=r.ReadInt32(),poll=r.ReadInt32(),stage=r.ReadInt32(),sample=r.ReadInt32(),phase=r.ReadInt32(),cursor0=r.ReadInt32(),cursor=r.ReadInt32();uint cycle0=r.ReadUInt32(),cycle=r.ReadUInt32();int point0=r.ReadInt32(),point=r.ReadInt32(),worldEvent=r.ReadInt32();
                        var terrain=Patch(r,zeroTerrain);var bf=Patch(r,basisF);var br=Patch(r,basisR);var beforeRows=Patch(r,rows);var af=Patch(r,bf);var ar=Patch(r,br);var afterRows=Patch(r,beforeRows);
                        Need(id==count&&history==id/128&&poll==id%128&&stage==0&&sample==Sample(history,poll)&&phase==poll+1&&point0==4&&point==4&&worldEvent==(poll==65?(history==1||history==2?1:history==3?3:0):0),"Original964 metadata");
                        if(poll==0){s=Initial(basis,visibility,history);expectedRows=(byte[])rows.Clone();}
                        s=World(s,history,poll);s.Movement.Grid.CopyCells().CopyTo(expectedRows,16*4096);SameClock(s.Clock,before);
                        Need(H(s.Movement.CopyFleetPool())==H(bf)&&H(s.Movement.Backing.CopyRegion())==H(br)&&H(s.Movement.CopyTerrainPool())==H(terrain)&&H(expectedRows)==H(beforeRows)&&s.Movement.Schedule.BatchStart==cursor0&&s.Movement.Schedule.CompletedCycles==cycle0,"Original964 complete carried input");
                        var u=s.AdvanceFrame((uint)sample,Context(phase));s=u.State;SameClock(u.BodyClock,body);SameClock(s.Clock,after);s.Movement.Grid.CopyCells().CopyTo(expectedRows,16*4096);
                        Need(H(s.Movement.CopyFleetPool())==H(af)&&H(s.Movement.Backing.CopyRegion())==H(ar)&&H(s.Movement.CopyTerrainPool())==H(terrain)&&H(expectedRows)==H(afterRows)&&s.Movement.Schedule.BatchStart==cursor&&s.Movement.Schedule.CompletedCycles==cycle,"Original964 complete output");
                        if((phase&31)==0)Need(u.Movement.AlignedCallerScratchAfter==unchecked((int)scratch),"Original964 aligned scratch");count++;
                    }
                    Need(count==512&&r.BaseStream.Position==r.BaseStream.Length,"Original964 exact packet extent");
                }
            });
        }
    }
}
