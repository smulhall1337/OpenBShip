using System;
using System.IO;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeProducedLaunchChecks
    {
        static readonly string[] Original={
            "0,0,0,0,0,0,14845,868999715,1,1,10,10,30,150,29845,29845,,-1,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A,40CD4B77CCB494E8CBA7A6CC71C861D298011FBB6F14AECFA20E81DDAC5CA738,E6044CD3BAE2BC9E13F892FE0B3DCDCDC5FB0038A4397C217722471FA98D1AD7",
            "3,0,0,0,1,0,14845,868999715,1,1,0,0,30,150,0,0,,-1,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A,24798F8ABEAC7CEFB32F63A90213125AEA005DC41E9C912B31DC054203435049,E6044CD3BAE2BC9E13F892FE0B3DCDCDC5FB0038A4397C217722471FA98D1AD7",
            "6,0,0,0,2,0,14845,868999715,1,1,10,10,30,150,14845,14845,,-1,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A,F0D72AA1E69E90CC89E8415891C9C30FBD4213D4BBAA601D20003C6EB51B3DA4,E6044CD3BAE2BC9E13F892FE0B3DCDCDC5FB0038A4397C217722471FA98D1AD7",
            "12,0,0,0,4,0,14845,868999715,1,1,10,10,30,150,29845,29845,,-1,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A,40CD4B77CCB494E8CBA7A6CC71C861D298011FBB6F14AECFA20E81DDAC5CA738,E6044CD3BAE2BC9E13F892FE0B3DCDCDC5FB0038A4397C217722471FA98D1AD7",
            "18,12,0,8,1,0,22345,4052711439,0,0,0,0,7,7,25345,25345,0/1,15,F1C676D0082F18F385B016E60E137D4CDE2F8B2FEE0D522E10D53952026484B6,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A,3332BE7B5EB6EBA46055A3E2725F902CA79258BE2B5B46FB6DC4B0420E2504B1,F1D76A06852A54A30A473669CBCCADEA6E21846D9928722A56CD2D331A185168",
            "19,12,0,8,1,1,22345,4052711439,0,0,0,0,7,8,25345,25345,79,15,E002A1DBF74B1CB5358EA11B1AB19DC22A3465FA1392A2A00F7F33B63AE5CDB7,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A,3332BE7B5EB6EBA46055A3E2725F902CA79258BE2B5B46FB6DC4B0420E2504B1,E3C77F2D976590370BCE11A37BF9FAC34DBC33D83C3DA2BBDD0AB58045CC42A0",
            "20,12,0,8,1,2,22345,4052711439,0,0,0,0,8,8,25345,25345,,-1,B8E6DD12AECA847606BE4A78B3E0F9425E4BB19284DE47280CF76CAA08F1536B,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A,3332BE7B5EB6EBA46055A3E2725F902CA79258BE2B5B46FB6DC4B0420E2504B1,2F824AD68268C60D7866C718153F8C8DE05B9D2B5D1B2EC4746DE1AE064FA738",
            "21,12,0,8,2,0,22345,4052711439,0,0,1,1,8,8,22345,22345,,-1,B8E6DD12AECA847606BE4A78B3E0F9425E4BB19284DE47280CF76CAA08F1536B,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A,BE596B7EDEC8079A44A409257F682CD19583B9704AD3429ECA2D5F3625C14706,E6044CD3BAE2BC9E13F892FE0B3DCDCDC5FB0038A4397C217722471FA98D1AD7",
            "24,12,0,8,3,0,22345,4052711439,0,0,0,0,7,7,25345,25345,0/1,15,F1C676D0082F18F385B016E60E137D4CDE2F8B2FEE0D522E10D53952026484B6,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A,3332BE7B5EB6EBA46055A3E2725F902CA79258BE2B5B46FB6DC4B0420E2504B1,F1D76A06852A54A30A473669CBCCADEA6E21846D9928722A56CD2D331A185168",
            "48,36,1,8,1,0,22345,4052711439,0,0,0,0,7,7,25345,25345,0/1,15,F1C676D0082F18F385B016E60E137D4CDE2F8B2FEE0D522E10D53952026484B6,B97C732B6E8517E7DA1E5D9AFCB6F6972C59DAF03D81C1F38F928115E716CE2C,3332BE7B5EB6EBA46055A3E2725F902CA79258BE2B5B46FB6DC4B0420E2504B1,F1D76A06852A54A30A473669CBCCADEA6E21846D9928722A56CD2D331A185168",
        };
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static bool Reject(Action a){try{a();return false;}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}catch(NotSupportedException){return true;}}
        static string Slots(int[] slots){return string.Join("/",Array.ConvertAll(slots,delegate(int n){return n.ToString();}));}
        static byte[] Read(BinaryReader r,int n){var b=r.ReadBytes(n);if(b.Length!=n)throw new EndOfStreamException();return b;}
        static byte[] Patch(BinaryReader r,byte[] basis){var b=(byte[])basis.Clone();int count=r.ReadInt32(),last=-1;Need(count>=0&&count<=b.Length,"609 delta count");for(int n=0;n<count;n++){int at=r.ReadInt32();Need(at>last&&at<b.Length,"609 delta order/bounds");last=at;b[at]=r.ReadByte();}return b;}
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var requests=new PracticeFleetMemberRequests[2,2];for(int p=0;p<2;p++){var joined=PracticeVisibleCandidates.Create(programs,map,PracticeOwnerVisibilityStudyState.Create(programs,map,p,1,0,1,0,0,0).Execute());for(int t=0;t<2;t++){var prefix=PracticeFleetMemberPreparation.CreateFromVisibility(programs,map,joined,t*8,2,false,0).Advance().Advance().Advance();requests[p,t]=PracticeFleetMemberRequests.CreateFromPreparation(prefix,0,0).Execute();}}
            Func<int,int,int,int,PracticeProducedLaunch> create=delegate(int p,int task,int timer,int capacity){return PracticeProducedLaunch.Create(programs,requests[p,task/8],timer,capacity);};
            check("Natural-distance consumer matches ten original609 complete-pool golden outputs",delegate
            {
                foreach(string line in Original){var r=line.Split(',');var c=create(int.Parse(r[2]),int.Parse(r[3]),int.Parse(r[4]),int.Parse(r[5])).Execute();Need(c.Clock==uint.Parse(r[6])&&c.RandomState==uint.Parse(r[7])&&(c.Eligibility(0).Allowed?0:1)==int.Parse(r[8])&&(c.Eligibility(1).Allowed?0:1)==int.Parse(r[9])&&c.RequestCount(0)==int.Parse(r[10])&&c.RequestCount(1)==int.Parse(r[11])&&c.Ammunition(0)==int.Parse(r[12])&&c.Ammunition(1)==int.Parse(r[13])&&c.Timer(0)==int.Parse(r[14])&&c.Timer(1)==int.Parse(r[15])&&Slots(c.CopyCreatedSlots())==r[16]&&c.LastTargetFleet==int.Parse(r[17])&&Hash(c.CopyShipPool())==r[18]&&Hash(c.CopyFleetPool())==r[19]&&Hash(c.CopyOrderPool())==r[20]&&Hash(c.CopyTimedPool())==r[21],"Original609 whole consumer case "+r[0]);}Need(Original.Length==10,"609 golden count");
            });
            check("Actual map separates F2 range rejection from due F3 allocation and strict timer holds",delegate
            {
                var f2=create(0,0,1,0).Execute();Need(f2.RejectedRows==2&&f2.ConsumedRequests==0&&f2.CreatedObjects==0&&f2.RequestCount(0)==0&&f2.RequestCount(1)==0&&f2.Ammunition(0)==30&&f2.Ammunition(1)==150&&f2.Eligibility(0).Denial==PracticeFiringDenial.OutOfRange&&f2.Eligibility(0).Distance==150&&f2.Eligibility(0).Range==60&&f2.Eligibility(1).Range==20,"Candidate mistaken for firing admission");var f3=create(0,8,1,0).Execute();Need(f3.ConsumedRequests==2&&f3.CreatedObjects==2&&Slots(f3.CopyCreatedSlots())=="0/1"&&f3.Ammunition(0)==7&&f3.Ammunition(1)==7&&f3.Timer(0)==25345&&f3.RequestCount(0)==0&&f3.LastTargetFleet==15,"Natural geometry F3 allocation");
                foreach(int mode in new[]{0,2,4}){var hold=create(0,8,mode,0).Execute();Need(hold.CreatedObjects==0&&hold.ConsumedRequests==0&&hold.RequestCount(0)==1&&hold.Ammunition(0)==8&&hold.Timer(0)==(mode==2?22345:37345),"Strict timer/ceiling behavior");}
            });
            check("F3 capacity consumes requests independently of ammunition and preserves original map observers",delegate
            {
                var one=create(0,8,1,1).Execute();var full=create(0,8,1,2).Execute();Need(one.CreatedObjects==1&&Slots(one.CopyCreatedSlots())=="79"&&one.Ammunition(0)==7&&one.Ammunition(1)==8&&full.CreatedObjects==0&&full.Ammunition(0)==8&&full.Ammunition(1)==8&&one.ConsumedRequests==2&&full.ConsumedRequests==2&&full.RequestCount(0)==0&&full.RequestCount(1)==0&&full.Timer(0)==25345,"Capacity changed request consumption");Need(Hash(full.CopyFleetPool())==Hash(requests[0,1].CopyFleetPool())&&Hash(one.CopyFleetPool())==Hash(requests[0,1].CopyFleetPool())&&requests[0,1].QueuedCells.Count==2,"Observer/position/parent requests changed");
            });
            check("Natural-distance consumer copies inputs and rejects uncaptured request contexts or repeats",delegate
            {
                var c=create(0,8,1,0);string s=Hash(c.CopyShipPool()),o=Hash(c.CopyOrderPool());var done=c.Execute();var b=done.CopyTimedPool();b[0]=99;Need(Hash(c.CopyShipPool())==s&&Hash(c.CopyOrderPool())==o&&!c.Executed&&done.CopyTimedPool()[0]!=99,"Mutable consumer copy");Need(Reject(delegate{PracticeProducedLaunch.Create(programs,null,0,0);})&&Reject(delegate{create(0,8,5,0);})&&Reject(delegate{create(0,8,0,3);})&&Reject(delegate{done.Execute();})&&Reject(delegate{PracticeProducedLaunch.Create(programs,PracticeFleetMemberRequests.Create(programs,map,3,0,2,0,0,1,0).Execute(),0,0);}),"Uncaptured consumer admitted");
            });
            evidence("Natural-distance consumer matches all60 original609 inputs admissions and full outputs",delegate
            {
                string path=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Produced-Launch-609.bin");Need(Hash(File.ReadAllBytes(path))=="9A6BC6F94C965C02C96082208F48A62234CE71DEF5E90A7C019E20330D326AED","609 evidence identity");int count=0;
                using(var r=new BinaryReader(File.OpenRead(path)))
                {
                    Need(System.Text.Encoding.ASCII.GetString(Read(r,8))=="VL609001"&&r.ReadInt32()==1&&r.ReadInt32()==60,"609 header");var bs=Read(r,402000);var bf=Read(r,123000);var bo=Read(r,78000);Need(Hash(bs)==Hash(map.Stages[3].CopyShipPool())&&Hash(bf)==Hash(map.Stages[3].CopyFleetPool()),"609 source bases");
                    for(;;){int id=r.ReadInt32();if(id==-1)break;Need(id==count&&id<60,"609 case order");int prefix=r.ReadInt32(),p=r.ReadInt32(),task=r.ReadInt32(),timer=r.ReadInt32(),capacity=r.ReadInt32(),clock=r.ReadInt32();uint random=r.ReadUInt32();int a=r.ReadInt32(),b=r.ReadInt32();Need(p>=0&&p<2&&(task==0||task==8)&&prefix==p*24+(task==8?12:0),"609 request identity");var beforeS=Patch(r,bs);var beforeF=Patch(r,bf);var beforeO=Patch(r,bo);var beforeT=Patch(r,new byte[21440]);var afterS=Patch(r,beforeS);var afterF=Patch(r,beforeF);var afterO=Patch(r,beforeO);var afterT=Patch(r,beforeT);int last=r.ReadInt32();var c=create(p,task,timer,capacity);Need(c.Clock==clock&&c.RandomState==random&&Hash(c.CopyShipPool())==Hash(beforeS)&&Hash(c.CopyFleetPool())==Hash(beforeF)&&Hash(c.CopyOrderPool())==Hash(beforeO)&&Hash(c.CopyTimedPool())==Hash(beforeT)&&(c.Eligibility(0).Allowed?0:1)==a&&(c.Eligibility(1).Allowed?0:1)==b,"609 original input/admission "+id);c=c.Execute();Need(c.LastTargetFleet==last&&Hash(c.CopyShipPool())==Hash(afterS)&&Hash(c.CopyFleetPool())==Hash(afterF)&&Hash(c.CopyOrderPool())==Hash(afterO)&&Hash(c.CopyTimedPool())==Hash(afterT),"609 complete output "+id);count++;}
                    Need(count==60&&r.BaseStream.Position==r.BaseStream.Length,"609 full matrix/EOF");
                }
            });
        }
    }
}
