using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
namespace FleetLab
{
    // Supported RIP B9A8...6A569: whole41BFB0/41BDA0 and41BB30.
    // Native519 + independent520 establish raw behavior in880 supplied cases.
    // Its role in the full AI policy is unresolved. This does not issue attacks.
    public sealed class PracticeFleetCandidates
    {
        static readonly string[] names={"Surface fleet 16","Base fleet 0","Stored aircraft fleet 23","Kind 3 / parent category 0","Kind 3 / parent category 1","Kind 3 / parent category 2","Kind 3 / parent category 3","Kind 4 / parent category 1","Kind 4 / parent category 2","Kind 4 / parent category 3","Surface / scenario 8 / owner -1"};
        static readonly int[][] configurations={new[]{16,1,-1,1},new[]{0,2,-1,1},new[]{23,6,-1,1},new[]{23,3,0,1},new[]{23,3,1,1},new[]{23,3,2,1},new[]{23,3,3,1},new[]{23,4,1,1},new[]{23,4,2,1},new[]{23,4,3,1},new[]{16,1,-1,8}};
        static readonly int[] distances={-1,0,1,128,32767};
        byte[] ships,fleets,task,scratch,rawList;
        ReadOnlyCollection<int> candidates;
        public static ReadOnlyCollection<string> CaseNames {get{return Array.AsReadOnly(names);}}
        public static ReadOnlyCollection<int> Distances {get{return Array.AsReadOnly(distances);}}
        public int CaseIndex {get;private set;}
        public int EquipmentProfile {get;private set;}
        public bool SuppliedEligibility {get;private set;}
        public int DistanceProfile {get;private set;}
        public int ActiveOwner {get;private set;}
        public int Actor {get;private set;}
        public int ActorKind {get{return I(fleets,Actor*492);}}
        public int Scenario {get;private set;}
        public int ParentCategory {get;private set;}
        public int TaskKind {get{return I(task,0);}}
        public int SelectedWaypoint {get;private set;}
        public int RouteUiRequests {get;private set;}
        public bool EquipmentFound {get;private set;}
        public ReadOnlyCollection<int> Candidates {get{return candidates;}}
        public byte[] CopyShipPool(){return (byte[])ships.Clone();}
        public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
        public byte[] CopyTaskRecord(){return (byte[])task.Clone();}
        public byte[] CopyScratchBytes(){return (byte[])scratch.Clone();}
        internal byte[] CopyRawList(){return (byte[])rawList.Clone();}
        static int I(byte[] b,int p){return BitConverter.ToInt32(b,p);}static int W(byte[] b,int p){return BitConverter.ToInt16(b,p);}
        static void I(byte[] b,int p,int value){BitConverter.GetBytes(value).CopyTo(b,p);}static void W(byte[] b,int p,int value){BitConverter.GetBytes((short)value).CopyTo(b,p);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static byte[] Table(string folder,string name,int length,string hash)
        {var bytes=File.ReadAllBytes(Path.Combine(folder,name));if(bytes.Length!=length||Hash(bytes)!=hash)throw new InvalidDataException("Candidate scan requires the supported source table: "+name);return bytes;}
        static int Unit(byte[] f,int actor,int member)
        {
            uint reference=BitConverter.ToUInt32(f,actor*492+16+member*4);if(reference==0)return -1;long delta=(long)reference-0x4fc340;
            if(delta<0||delta>=402000||delta%268!=0)throw new InvalidDataException("Candidate scan normalized roster reference.");return (int)(delta/268);
        }
        public static PracticeFleetCandidates Create(string programs,PracticeMapStartup startup,int caseIndex,int equipmentProfile,bool suppliedEligibility,int distance,int activeOwner)
        {
            if(startup==null)throw new ArgumentNullException("startup");if(startup.Seed!=731)throw new NotSupportedException("Candidate scan admits BSSB202 seed731 only.");
            if(caseIndex<0||caseIndex>=configurations.Length)throw new ArgumentOutOfRangeException("caseIndex");if(equipmentProfile<0||equipmentProfile>3)throw new ArgumentOutOfRangeException("equipmentProfile");
            if(Array.IndexOf(distances,distance)<0)throw new ArgumentOutOfRangeException("distance");if(activeOwner!=0&&activeOwner!=1)throw new ArgumentOutOfRangeException("activeOwner");
            var metadata=Table(programs,"EquipmentDispatchKinds.seqbin",41,"02CE3E390AC8866AD1F584102B9D10011B3D62663642AF1B117DE8A8D5A63A74");
            var ranges=Table(programs,"AiCandidateRawRanges.seqbin",82,"BAD7B5737AE23238DF5F15E582D54426208E9A226AFF78F48696198B702DB68F");
            var s=startup.Stages[3].CopyShipPool();var f=startup.Stages[3].CopyFleetPool();var config=configurations[caseIndex];int actor=config[0],weapon=-1;
            for(int n=1;n<41;n++)if(metadata[n]==0xf2||metadata[n]==0xf3){weapon=n;break;}if(weapon<0)throw new InvalidDataException("No supported equipment dispatch entry.");
            I(f,actor*492,config[1]);if(config[2]>=0){W(f,actor*492+8,30);f[16*492+486]=(byte)config[2];}if(config[3]==8)W(f,actor*492+6,-1);
            if(equipmentProfile!=0)for(int member=0;member<6;member++)
            {
                int unit=Unit(f,actor,member);if(unit<0)continue;int length=W(s,unit*268+14);if(length<0||length>6)throw new InvalidDataException("Candidate fixture length.");
                for(int e=0;e<length*4;e++)I(s,unit*268+100+e*4,0);if(length>0&&member==equipmentProfile-2)I(s,unit*268+100,weapon);
            }
            if(suppliedEligibility)for(int slot=0;slot<250;slot++)if(I(f,slot*492)!=0)f[slot*492+467]=1;
            if(distance>=0){W(f,actor*492+12,0);W(f,actor*492+14,0);for(int slot=0;slot<250;slot++)if(slot!=actor&&I(f,slot*492)!=0){W(f,slot*492+12,distance);W(f,slot*492+14,0);}}
            var result=Evaluate(s,f,PracticeFleetServiceContext.CreateDiagnostic(actor),metadata,ranges,actor,config[3],activeOwner);
            result.CaseIndex=caseIndex;result.EquipmentProfile=equipmentProfile;result.SuppliedEligibility=suppliedEligibility;result.DistanceProfile=distance;result.ParentCategory=config[2];return result;
        }
        // Internal evidence entry: copies all inputs and validates every reached
        // pointer/count before use. Public factory only supplies captured presets.
        internal static PracticeFleetCandidates Evaluate(byte[] s,byte[] f,byte[] t,byte[] metadata,byte[] ranges,int actor,int scenario,int activeOwner)
        {
            if(s==null||s.Length!=402000||f==null||f.Length!=123000||t==null||t.Length!=312||metadata==null||metadata.Length!=41||ranges==null||ranges.Length!=82)throw new ArgumentException("Candidate scan input sizes.");
            if(actor<0||actor>=250||W(f,actor*492+4)!=actor||I(t,204)!=0x4b47a0+actor*492)throw new InvalidDataException("Candidate scan actor identity.");
            var state=new PracticeFleetCandidates{ships=(byte[])s.Clone(),fleets=(byte[])f.Clone(),task=(byte[])t.Clone(),scratch=new byte[164],rawList=new byte[1008],Actor=actor,Scenario=scenario,ActiveOwner=activeOwner,SelectedWaypoint=3};
            // Comparison-only tail tags/canary match native519's list adapter.
            // Public API exposes only the recovered terminated integer list.
            for(int n=0;n<248;n++)I(state.rawList,n*4,unchecked(0x51a00000+n));for(int n=992;n<1008;n++)state.rawList[n]=0xa7;I(state.rawList,0,-1);
            for(int n=0;n<41;n++)I(state.scratch,n*4,-1);
            for(int member=0;member<6;member++)
            {
                int unit=Unit(state.fleets,actor,member);if(unit<0)continue;int length=W(state.ships,unit*268+14);if(length<=0)continue;if(length>6)throw new NotSupportedException("Candidate scan length exceeds captured equipment storage.");
                for(int e=0;e<length*4;e++)
                {
                    int weapon=I(state.ships,unit*268+100+e*4);if(weapon==0)continue;if(weapon<0||weapon>=41)throw new NotSupportedException("Candidate scan equipment id exceeds captured metadata.");
                    if(metadata[weapon]==0xf2||metadata[weapon]==0xf3){state.EquipmentFound=true;I(state.scratch,weapon*4,member);}
                }
            }
            var found=new List<int>();
            if(!state.EquipmentFound)
            {
                int kind=I(state.fleets,actor*492);if(kind<5||kind>8)state.PushServiceTask();
            }
            else for(int slot=249;slot>=0;slot--)
            {
                if(scenario==8&&W(state.fleets,slot*492+6)==0&&W(state.fleets,actor*492+6)==-1)continue;
                // Preserve the source's order: kind check precedes alignment.
                if(I(state.fleets,slot*492)==0)continue;int actorKind=I(state.fleets,actor*492);if(actorKind==2)slot&=~3;
                int at=slot*492;if(W(state.fleets,at+6)==activeOwner||state.fleets[at+467]==0)continue;
                if(actorKind==3||actorKind==4)
                {
                    int unit=W(state.fleets,actor*492+8);if(unit<0||unit>=1500)throw new InvalidDataException("Candidate scan parent unit.");int parent=W(state.ships,unit*268+12);if(parent<0||parent>=250)throw new InvalidDataException("Candidate scan parent fleet.");
                    int category=state.fleets[parent*492+486],kind=I(state.fleets,at);if(category==1&&kind!=3&&kind!=4&&kind!=5||category==3&&kind!=9||category==2)continue;
                }
                int distance=Math.Max(Math.Abs(W(state.fleets,actor*492+12)-W(state.fleets,at+12)),Math.Abs(W(state.fleets,actor*492+14)-W(state.fleets,at+14)));
                for(int e=0;e<41;e++)if(I(state.scratch,e*4)!=0&&W(ranges,e*2)<=distance)
                {
                    if(found.Count>=247)throw new NotSupportedException("Candidate list exceeds the captured bounded adapter window.");int id=W(state.fleets,at+4);found.Add(id);I(state.rawList,(found.Count-1)*4,id);break;
                }
            }
            I(state.rawList,found.Count*4,-1);state.candidates=found.AsReadOnly();return state;
        }
        void PushServiceTask()
        {
            if(I(task,0)!=11)
            {
                Array.Copy(task,0,task,20,180);Array.Clear(task,0,20);I(task,0,11);for(int n=0;n<5;n++)W(fleets,Actor*492+40+n*2,-1);SelectedWaypoint=-1;RouteUiRequests++;
            }
            I(task,200,1);
        }
    }
}
