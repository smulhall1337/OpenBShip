using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
namespace FleetLab
{
    // B9A8...6A569: reached41D2B0/41D0D0/41D240 preparation paths.
    // Original527/independent528 establish768 controlled prefixes. State2 is
    // observable but NEVER executed. Caller comparison word0/1 is supplied:
    // the state1 path reads this stack local before a state0 path writes it.
    public sealed class PracticeFleetMemberPreparation
    {
        byte[] ships,fleets,task,metadata,ranges,combat,delays;
        PracticeFleetCandidates scan;
        int firstDeadline;
        public int EquipmentProfile {get;private set;}
        public int EligibilityProfile {get;private set;}
        public int InitialTask {get;private set;}
        public int Difficulty {get;private set;}
        public bool PlayerOverride {get;private set;}
        public int ComparisonWord {get;private set;}
        public int Visits {get;private set;}
        public int Clock {get;private set;}
        public int SelectedWaypoint {get;private set;}
        public int RouteUiRequests {get;private set;}
        public int TaskKind {get{return I(task,0);}}
        public bool HasScanned {get{return scan!=null;}}
        public bool FiringBoundary {get{for(int n=0;n<6;n++)if(I(task,212+n*4)==2)return true;return false;}}
        public bool CanAdvance {get{return Visits<3&&!FiringBoundary;}}
        public int F2Delay {get{return I(delays,0)/(1<<Difficulty);}}
        public int F3Delay {get{return I(delays,4)/(1<<Difficulty);}}
        public int NextClock
        {
            get
            {
                if(!CanAdvance)throw new InvalidOperationException("The captured preparation prefix is complete.");if(Visits==0)return 12345;if(Visits==2)return firstDeadline;
                int due=int.MaxValue;for(int n=0;n<6;n++)if(I(task,212+n*4)==1)due=Math.Min(due,I(task,236+n*4));return due==int.MaxValue?12345:unchecked(due-1);
            }
        }
        public ReadOnlyCollection<int> Candidates {get{return scan==null?Array.AsReadOnly(new int[0]):scan.Candidates;}}
        public int MemberState(int member){Member(member);return I(task,212+member*4);}
        public int MemberDue(int member){Member(member);return I(task,236+member*4);}
        public int MemberTarget(int member){Member(member);return MemberState(member)==0?-1:I(task,260+member*4);}
        public int WeaponField(int member){Member(member);return I(ships,(30+member)*268+84);}
        static void Member(int member){if(member<0||member>1)throw new ArgumentOutOfRangeException("member","The captured source roster has two members.");}
        public byte[] CopyShipPool(){return (byte[])ships.Clone();}
        public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
        public byte[] CopyTaskRecord(){return (byte[])task.Clone();}
        public byte[] CopyScratchBytes(){if(scan==null)throw new InvalidOperationException("No scan has produced scratch data yet.");return scan.CopyScratchBytes();}
        internal byte[] CopyRawList(){if(scan==null)throw new InvalidOperationException("No scan has produced a list yet.");return scan.CopyRawList();}
        static int I(byte[] b,int p){return BitConverter.ToInt32(b,p);}static int W(byte[] b,int p){return BitConverter.ToInt16(b,p);}
        static void I(byte[] b,int p,int v){BitConverter.GetBytes(v).CopyTo(b,p);}static void W(byte[] b,int p,int v){BitConverter.GetBytes((short)v).CopyTo(b,p);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static byte[] Table(string folder,string file,int length,string hash){var b=File.ReadAllBytes(Path.Combine(folder,file));if(b.Length!=length||Hash(b)!=hash)throw new InvalidDataException("Member preparation requires the supported table "+file);return b;}
        public static PracticeFleetMemberPreparation Create(string programs,PracticeMapStartup startup,int equipment,int eligibility,int initialTask,int difficulty,bool playerOverride,int comparisonWord)
        {
            if(startup==null)throw new ArgumentNullException("startup");if(startup.Seed!=731)throw new NotSupportedException("Member preparation admits BSSB202 seed731 only.");
            if(equipment<0||equipment>5)throw new ArgumentOutOfRangeException("equipment");if(eligibility<0||eligibility>3)throw new ArgumentOutOfRangeException("eligibility");if(initialTask!=0&&initialTask!=8)throw new ArgumentOutOfRangeException("initialTask");if(difficulty<0||difficulty>3)throw new ArgumentOutOfRangeException("difficulty");if(comparisonWord!=0&&comparisonWord!=1)throw new ArgumentOutOfRangeException("comparisonWord");
            var c=new PracticeFleetMemberPreparation{ships=startup.Stages[3].CopyShipPool(),fleets=startup.Stages[3].CopyFleetPool(),task=PracticeFleetServiceContext.CreateDiagnostic(16),EquipmentProfile=equipment,EligibilityProfile=eligibility,InitialTask=initialTask,Difficulty=difficulty,PlayerOverride=playerOverride,ComparisonWord=comparisonWord,Clock=12345,SelectedWaypoint=3};
            c.metadata=Table(programs,"EquipmentDispatchKinds.seqbin",41,"02CE3E390AC8866AD1F584102B9D10011B3D62663642AF1B117DE8A8D5A63A74");c.ranges=Table(programs,"AiCandidateRawRanges.seqbin",82,"BAD7B5737AE23238DF5F15E582D54426208E9A226AFF78F48696198B702DB68F");c.combat=Table(programs,"CombatMath.seqbin",1152,"976F96563684FC08774653BADCC75AF765F781605E91B4DF270936DEE7E2C337");c.delays=Table(programs,"AiPreparationDelays.seqbin",8,"545F5A83A9D7ADDC56D100898BBFAE1FC223BEFCD1F091ED724FB225F4BA39FE");
            I(c.task,0,initialTask);
            if(equipment!=0)for(int member=0;member<2;member++)
            {
                int unit=30+member,length=W(c.ships,unit*268+14);if(length<=0||length>6)throw new InvalidDataException("Member preparation supplied length.");
                for(int n=0;n<length*4;n++)I(c.ships,unit*268+100+n*4,0);for(int n=0;n<35;n++)W(c.ships,unit*268+196+n*2,0);
                int weapon=equipment==2&&member==0||equipment==3?14:equipment==4?19:equipment==5?(member==0?14:19):0;if(weapon!=0){I(c.ships,unit*268+100,weapon);W(c.ships,unit*268+196+weapon*2,7);}
            }
            for(int slot=0;slot<250;slot++)if(I(c.fleets,slot*492)!=0)c.fleets[slot*492+467]=(byte)(eligibility==3||eligibility==1&&slot==14||eligibility==2&&(slot==14||slot==15)?1:0);
            return c;
        }
        public PracticeFleetMemberPreparation Advance()
        {
            if(!CanAdvance)throw new InvalidOperationException(FiringBoundary?"Firing state reached;this preparation study stops before executing it.":"Three captured preparation visits are complete;reset this copy.");
            var next=(PracticeFleetMemberPreparation)MemberwiseClone();next.ships=(byte[])ships.Clone();next.fleets=(byte[])fleets.Clone();next.task=(byte[])task.Clone();next.Clock=NextClock;
            if(Visits==1)next.firstDeadline=unchecked(next.Clock+1);next.Visit();next.Visits++;return next;
        }
        void Visit()
        {
            scan=PracticeFleetCandidates.Evaluate(ships,fleets,task,metadata,ranges,16,1,1);ships=scan.CopyShipPool();fleets=scan.CopyFleetPool();task=scan.CopyTaskRecord();RouteUiRequests+=scan.RouteUiRequests;if(scan.RouteUiRequests>0)SelectedWaypoint=scan.SelectedWaypoint;
            if(scan.Candidates.Count==0){Array.Clear(task,212,24);return;}
            int cursor=0,comparison=ComparisonWord;
            for(int member=0;member<6;member++)
            {
                uint reference=BitConverter.ToUInt32(fleets,16*492+16+member*4);if(reference==0)continue;long delta=(long)reference-0x4fc340;if(delta<0||delta>=402000||delta%268!=0)throw new InvalidDataException("Member preparation normalized roster.");int unit=(int)(delta/268),state=I(task,212+member*4);
                if(state==0){comparison=cursor;cursor=Choose(unit,cursor,member);}
                else if(state==1){if(PlayerOverride)I(task,236+member*4,2);if(I(task,236+member*4)<=Clock)I(task,212+member*4,2);}
                else throw new NotSupportedException("Only member states0 and1 may execute in this study.");
                if(comparison!=cursor&&Difficulty<2)break;if(cursor>=scan.Candidates.Count)break;
            }
        }
        int Choose(int unit,int cursor,int member)
        {
            if(W(ships,unit*268+12)!=16||cursor<0||cursor>=scan.Candidates.Count)throw new NotSupportedException("Member preparation parent context or target cursor is outside captured input.");int target=scan.Candidates[cursor];if(target<0||target>=250)throw new InvalidDataException("Member preparation target id.");int targetKind=I(fleets,target*492),length=W(ships,unit*268+14),best=0;if(targetKind<0||targetKind>=13||length>6)throw new NotSupportedException("Member preparation target kind or length.");
            for(int n=0;n<length*4;n++)
            {
                int weapon=I(ships,unit*268+100+n*4);if(weapon<0||weapon>=35)throw new NotSupportedException("Member preparation weapon/ammo field exceeds captured storage.");if(W(ships,unit*268+196+weapon*2)==0)continue;bool f3=I(task,0)==8||I(fleets,16*492)==9&&I(task,0)==4;
                if(metadata[weapon]!=0xf2&&(!f3||metadata[weapon]!=0xf3))continue;if(unchecked((sbyte)combat[weapon*13+targetKind])>unchecked((sbyte)combat[best*13+targetKind]))best=weapon;
            }
            if(best!=0){I(ships,unit*268+84,best);I(task,260+member*4,target);I(task,212+member*4,1);I(task,236+member*4,unchecked(Clock+(metadata[best]==0xf3?F3Delay:F2Delay)));cursor++;}
            if(cursor>=scan.Candidates.Count&&Difficulty>1)cursor=0;return cursor;
        }
    }
}
