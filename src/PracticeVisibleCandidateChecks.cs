using System;
using System.IO;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeVisibleCandidateChecks
    {
        // Original584 complete-stage observations, captured independently.
        static readonly string[] Original={
            "0,0,0,0,14,1,9,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,CE23E36DC03C412A6A4EBD8C92F93A0BD34B7A41FF79B028E97F1EAE31A7A211,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,CE23E36DC03C412A6A4EBD8C92F93A0BD34B7A41FF79B028E97F1EAE31A7A211,56836F7416CE692C7223E3473821752B5E9CD40D7B3D26A4DD9EB398E9315626,875BCDF0941EA401A566795E781FD73FF662943F383485941F425B19102E2083",
            "3,0,1,0,14,4,9/3/2/0,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,B8FAACB28BF3863FFB58116FF3604332CE02570CB605DF5EA8DEEDEBA076FB84,307F0170D19BF2BFD77C9BA30CF985D3721053272C715CBDEA152881FF07B2FD,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,307F0170D19BF2BFD77C9BA30CF985D3721053272C715CBDEA152881FF07B2FD,56836F7416CE692C7223E3473821752B5E9CD40D7B3D26A4DD9EB398E9315626,4BD48F5FCB298F48FAC6521EAFA2CB5CEE0901EFE450C241A52DA21A3E812773",
            "15,1,0,0,16,1,15,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A,B6FBF913D2A5198FE2D41E20069C2E3E341238BC68D2F09E521D8ACADAA64116,45E13390C56E489A964DE2277F6FF826FC59D9495D8D948112C8B120F07A55A6",
            "18,1,1,0,16,1,15,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,B8FAACB28BF3863FFB58116FF3604332CE02570CB605DF5EA8DEEDEBA076FB84,B97C732B6E8517E7DA1E5D9AFCB6F6972C59DAF03D81C1F38F928115E716CE2C,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,B97C732B6E8517E7DA1E5D9AFCB6F6972C59DAF03D81C1F38F928115E716CE2C,B6FBF913D2A5198FE2D41E20069C2E3E341238BC68D2F09E521D8ACADAA64116,45E13390C56E489A964DE2277F6FF826FC59D9495D8D948112C8B120F07A55A6",
        };
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static string Ids(PracticeFleetCandidates c){var s=new string[c.Candidates.Count];for(int n=0;n<s.Length;n++)s[n]=c.Candidates[n].ToString();return string.Join("/",s);}
        static bool Reject(Action a){try{a();return false;}catch(ArgumentException){return true;}catch(NotSupportedException){return true;}catch(InvalidDataException){return true;}}
        static byte[] Read(BinaryReader r,int n){var b=r.ReadBytes(n);if(b.Length!=n)throw new EndOfStreamException();return b;}
        static byte[] Patch(BinaryReader r,byte[] basis){var b=(byte[])basis.Clone();int count=r.ReadInt32(),last=-1;Need(count>=0&&count<=b.Length,"584 delta count");for(int n=0;n<count;n++){int at=r.ReadInt32();Need(at>last&&at<b.Length,"584 delta order/bounds");last=at;b[at]=r.ReadByte();}return b;}
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var bytes=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));var map=PracticeMapStartup.CreateBssb202(programs,bytes,731);var rules=PracticeOwnerVisibilityRules.Load(programs);
            Func<int,int,PracticeOwnerVisibilityStudyState> visibility=delegate(int preset,int owner){return PracticeOwnerVisibilityStudyState.CreateWithRules(rules,map,preset,owner,0,1,0,0,0).Execute();};
            check("Produced visibility candidate factories match four original584 complete endpoints",delegate
            {
                foreach(string line in Original){var r=line.Split(',');var c=PracticeVisibleCandidates.Create(programs,map,visibility(int.Parse(r[2]),int.Parse(r[1])));var s=c.Scan;Need(s.Actor==int.Parse(r[4])&&s.Candidates.Count==int.Parse(r[5])&&Ids(s)==r[6]&&Hash(s.CopyShipPool())==r[10]&&Hash(s.CopyFleetPool())==r[11]&&Hash(s.CopyScratchBytes())==r[12]&&Hash(s.CopyRawList())==r[13]&&s.RouteUiRequests==0,"Original584 factory case "+r[0]);}Need(Original.Length==4,"584 golden count");
            });
            check("Natural copied visibility feeds candidates without replacing flags or moving fleets",delegate
            {
                var v=visibility(0,1);var c=PracticeVisibleCandidates.Create(programs,map,v);Need(Ids(c.Scan)=="15"&&c.Scan.Actor==16&&c.Scan.EquipmentFound&&Hash(c.Scan.CopyFleetPool())==Hash(v.CopyFleetPool())&&Hash(c.Scan.CopyShipPool())==Hash(map.Stages[3].CopyShipPool())&&v.Fleet(14).Level==0&&v.Fleet(15).Level==1&&c.Scan.TaskKind==0&&c.Scan.RouteUiRequests==0,"Natural producer replaced by supplied visibility");
                var other=PracticeVisibleCandidates.Create(programs,map,visibility(0,0));Need(other.Scan.Actor==14&&Ids(other.Scan)=="9","Owner0 candidate path");
            });
            check("Packed history can retain extra candidates and joined copies stay immutable",delegate
            {
                var v=visibility(1,0);string f=Hash(v.CopyFleetPool());var c=PracticeVisibleCandidates.Create(programs,map,v);Need(Ids(c.Scan)=="9/3/2/0","Cached owner0 candidate order");var fleets=c.Scan.CopyFleetPool();fleets[0]^=255;var ships=c.Scan.CopyShipPool();ships[0]^=255;Need(Hash(v.CopyFleetPool())==f&&Hash(c.Scan.CopyFleetPool())==f&&Hash(c.Scan.CopyShipPool())!=Hash(ships),"Candidate child mutable inputs");
            });
            check("Joined candidate boundary rejects pending local synthetic neutral and unverified seed states",delegate
            {
                Need(Reject(delegate{PracticeVisibleCandidates.Create(programs,map,null);})&&Reject(delegate{PracticeVisibleCandidates.Create(programs,map,PracticeOwnerVisibilityStudyState.CreateWithRules(rules,map,0,1,0,1,0,0,0));})&&Reject(delegate{PracticeVisibleCandidates.Create(programs,map,visibility(0,-1));})&&Reject(delegate{PracticeVisibleCandidates.Create(programs,map,visibility(0,2));})&&Reject(delegate{PracticeVisibleCandidates.Create(programs,map,visibility(2,1));})&&Reject(delegate{PracticeVisibleCandidates.Create(programs,map,visibility(3,1));})&&Reject(delegate{PracticeVisibleCandidates.Create(programs,PracticeMapStartup.CreateBssb202(programs,bytes,732),visibility(0,1));}),"Uncaptured candidate join admitted");
            });
            evidence("Visibility-to-candidate join matches all30 original584 stage scratch and list observations",delegate
            {
                string path=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Visible-Candidates-584.bin");Need(Hash(File.ReadAllBytes(path))=="BFF5FB79DB274B2EC4D9BE8F2400D27EC108CF04BE602B24BEE4D045900748C8","584 evidence identity");var metadata=File.ReadAllBytes(Path.Combine(programs,"EquipmentDispatchKinds.seqbin"));var ranges=File.ReadAllBytes(Path.Combine(programs,"AiCandidateRawRanges.seqbin"));int count=0;
                using(var r=new BinaryReader(File.OpenRead(path)))
                {
                    Need(System.Text.Encoding.ASCII.GetString(Read(r,8))=="VC584001"&&r.ReadInt32()==1&&r.ReadInt32()==30,"584 header");var bs=Read(r,402000);var bf=Read(r,123000);Need(Hash(bs)==Hash(map.Stages[3].CopyShipPool())&&Hash(bf)==Hash(map.Stages[3].CopyFleetPool()),"584 original bases");
                    for(;;){int id=r.ReadInt32();if(id==-1)break;Need(id==count&&id<30,"584 case order");int owner=r.ReadInt32(),fixture=r.ReadInt32(),equipment=r.ReadInt32(),actor=r.ReadInt32();Need((owner==0||owner==1)&&fixture>=0&&fixture<=4&&(equipment==0||equipment==14||equipment==19)&&actor==(owner==0?14:16),"584 scalars");var s=Patch(r,bs);var f=Patch(r,bf);var seen=Patch(r,f);var after=Patch(r,seen);var scratch=Read(r,164);var list=Read(r,1008);var v=rules.Apply(f,owner,0,-2,0,actor);Need(Hash(v.CopyFleetPool())==Hash(seen),"584 producer stage "+id);var task=PracticeFleetServiceContext.CreateDiagnostic(actor);var c=PracticeFleetCandidates.Evaluate(s,v.CopyFleetPool(),task,metadata,ranges,actor,1,owner);Need(c.EquipmentFound&&Hash(c.CopyShipPool())==Hash(s)&&Hash(c.CopyFleetPool())==Hash(after)&&Hash(c.CopyScratchBytes())==Hash(scratch)&&Hash(c.CopyRawList())==Hash(list)&&Hash(c.CopyTaskRecord())==Hash(task)&&c.RouteUiRequests==0,"584 candidate stage "+id);count++;}
                    Need(count==30&&r.BaseStream.Position==r.BaseStream.Length,"584 complete matrix/EOF");
                }
            });
        }
    }
}
