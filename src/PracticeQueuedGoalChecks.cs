using System;using System.IO;using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeQueuedGoalChecks
    {
        // Numeric outputs independently read from original744 admitted frames.
        // Columns: input DWORD XY, output WORD XY, output task goal.
        static readonly int[][] Original={new[]{-1,77,-1,0,1},new[]{0,0,0,0,4},new[]{630,630,630,630,4},new[]{631,-20,631,-20,4},new[]{65535,65536,-1,0,4},new[]{65536,65535,0,-1,4},new[]{int.MinValue,int.MaxValue,0,-1,4},new[]{int.MaxValue,int.MinValue,-1,0,4},new[]{-65537,-65536,-1,0,4},new[]{-65536,-65537,0,-1,4}};
        static void Need(bool ok,string t){if(!ok)throw new InvalidDataException(t);}static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static void I(byte[] b,int a,int v){BitConverter.GetBytes(v).CopyTo(b,a);}static void W(byte[] b,int a,int v){BitConverter.GetBytes(unchecked((short)v)).CopyTo(b,a);}
        static bool Reject(Action a){try{a();return false;}catch(ArgumentException){return true;}catch(InvalidDataException){return true;}catch(NotSupportedException){return true;}}
        static byte[][] Fixture(int slot,int x,int y)
        {
            var f=new byte[123000];var r=new byte[78560];for(int n=0;n<f.Length;n++)f[n]=(byte)(n*13+7);for(int n=0;n<r.Length;n++)r[n]=(byte)(n*17+3);
            I(f,slot*492,1);W(f,slot*492+4,slot);W(f,slot*492+6,1);W(f,slot*492+40,-1);W(f,slot*492+50,0);I(r,slot*312+204,0x4b47a0+slot*492);I(r,slot*312+200,4);I(r,slot*312+8,x);I(r,slot*312+12,y);return new[]{f,r};
        }
        static byte[] Read(BinaryReader r,int n){var b=r.ReadBytes(n);Need(b.Length==n,"744 truncated");return b;}static byte[] Patch(BinaryReader r,byte[] basis){var b=(byte[])basis.Clone();int count=r.ReadInt32(),last=-1;Need(count>=0&&count<=b.Length,"744 delta count");for(int n=0;n<count;n++){int at=r.ReadInt32();Need(at>last&&at<b.Length,"744 delta bounds");last=at;b[at]=r.ReadByte();}return b;}
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            check("Goal-four numeric API matches ten original744 signed/sentinel outputs and preserves opaque queue bytes",delegate
            {
                foreach(var row in Original)
                {
                    var d=Fixture(16,row[0],row[1]);var f=(byte[])d[0].Clone();var r=(byte[])d[1].Clone();var result=PracticeQueuedGoal.ApplyGoalFour(d[0],new PracticeAiSharedBacking(d[1]),16);
                    W(f,16*492+40,row[2]);W(f,16*492+50,row[3]);I(r,16*312+200,row[4]);Need(Hash(result.CopyFleetPool())==Hash(f)&&Hash(result.Backing.CopyRegion())==Hash(r)&&result.LinkedFleet==16&&(result.Outcome==PracticeQueuedGoalOutcome.ResetToGoalOne)==(row[4]==1),"744 captured numeric outcome "+row[0]);
                }
            });
            check("Goal-four result owns snapshots and retains the shared movement tail at record249",delegate
            {
                var d=Fixture(249,-1,77);var b=new PracticeAiSharedBacking(d[1]);string fh=Hash(d[0]),rh=Hash(b.CopyRegion());var result=PracticeQueuedGoal.ApplyGoalFour(d[0],b,249);string af=Hash(result.CopyFleetPool()),ar=Hash(result.Backing.CopyRegion());d[0][0]^=255;d[1][0]^=255;var copy=result.CopyFleetPool();copy[0]^=255;copy=result.Backing.CopyRegion();copy[copy.Length-1]^=255;Need(Hash(result.CopyFleetPool())==af&&Hash(result.Backing.CopyRegion())==ar&&Hash(b.CopyRegion())==rh&&af==fh,"Queued goal input/output aliased");var expected=b.CopyMovementAuxiliary();I(expected,249*312+200-224,1);Need(Hash(expected)==Hash(result.Backing.CopyMovementAuxiliary()),"Shared task write/movement tail did not agree");
            });
            check("Goal-four rejects unsupported planners and malformed identity transactionally",delegate
            {
                var d=Fixture(16,100,200);var b=new PracticeAiSharedBacking(d[1]);string fh=Hash(d[0]),rh=Hash(b.CopyRegion());
                foreach(int which in new[]{0,1,2,3,4,5}){var f=(byte[])d[0].Clone();var t=b.CopyTaskRecord(16);if(which==0)I(f,16*492,2);if(which==1)W(f,16*492+6,0);if(which==2)W(f,16*492+40,0);if(which==3)I(t,200,5);if(which==4)I(t,204,0x4b47a1);if(which==5)W(f,16*492+4,17);var bad=b.WithTaskRecord(16,t);Need(Reject(delegate{PracticeQueuedGoal.ApplyGoalFour(f,bad,16);}),"Unsupported queued goal admitted "+which);}
                Need(Reject(delegate{PracticeQueuedGoal.ApplyGoalFour(null,b,16);})&&Reject(delegate{PracticeQueuedGoal.ApplyGoalFour(new byte[1],b,16);})&&Reject(delegate{PracticeQueuedGoal.ApplyGoalFour(d[0],null,16);})&&Reject(delegate{PracticeQueuedGoal.ApplyGoalFour(d[0],b,-1);})&&Reject(delegate{PracticeQueuedGoal.ApplyGoalFour(d[0],b,250);})&&Hash(d[0])==fh&&Hash(b.CopyRegion())==rh,"Malformed queued goal published changes");
            });
            evidence("Goal-four API matches ten admitted original744 branches within40 full captured driver frames",delegate
            {
                string path=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Queued-Goal-744.bin");Need(Hash(File.ReadAllBytes(path))=="268556EE0A208E2E9552FBABED4303D31F894916A9F8C7E6A68D306510E49C8B","744 identity");int count=0,visits=0;
                using(var r=new BinaryReader(File.OpenRead(path)))
                {
                    Need(System.Text.Encoding.ASCII.GetString(Read(r,8))=="OP744001"&&r.ReadInt32()==1&&r.ReadInt32()==40,"744 header");Read(r,402000);var f=Read(r,123000);Read(r,78000);var c=Read(r,1184);Read(r,78000);var region=Read(r,78560);Read(r,64000);Read(r,1024000);
                    for(;;){int id=r.ReadInt32();if(id==-1)break;uint phase=r.ReadUInt32();int raw=r.ReadInt32(),type=r.ReadInt32(),suppressed=r.ReadInt32(),admitted=r.ReadInt32(),x=r.ReadInt32(),y=r.ReadInt32();var before=Patch(r,region);var afterF=Patch(r,f);var afterR=Patch(r,before);Patch(r,c);int ui=r.ReadInt32(),wp=r.ReadInt32();Need(id==count&&type==2&&suppressed==0&&admitted==(((phase&31)==0&&raw!=0)?1:0)&&BitConverter.ToInt32(before,16*312+8)==x&&BitConverter.ToInt32(before,16*312+12)==y&&ui==0&&wp==4,"744 metadata");
                        if(admitted!=0){Array.Clear(before,16*312+212,24);var result=PracticeQueuedGoal.ApplyGoalFour(f,new PracticeAiSharedBacking(before),16);Need(Hash(result.CopyFleetPool())==Hash(afterF)&&Hash(result.Backing.CopyRegion())==Hash(afterR),"744 full branch "+id);visits++;}else Need(Hash(afterF)==Hash(f)&&Hash(afterR)==Hash(before),"744 skipped branch mutated");count++;}
                    Need(count==40&&visits==10&&r.BaseStream.Position==r.BaseStream.Length,"744 extent");
                }
            });
        }
    }
}
