using System;using System.IO;using FleetLab;
namespace FleetLab
{
    // Pinned RIP41F410 reads an uninitialized caller-stack word. This numeric
    // API REQUIRES that word explicitly. It does not infer a natural value or
    // execute the outer dispatcher, target combat, pathfinding or original UI.
    public sealed class PracticeFlag23PatrolResult
    {
        readonly byte[] ships,fleets,orders,candidates;
        public PracticeAiSharedBacking Backing {get;private set;}
        public int SuppliedCounterWord {get;private set;}
        public int CounterWordAfter {get;private set;}
        public int RosterIndexAfter {get;private set;}
        public int RouteUiRequests {get;private set;}
        public int SelectedWaypoint {get;private set;}
        public int Activations {get;private set;}
        public int MemberVisits {get;private set;}
        public int ProcessedAircraft {get;private set;}
        public bool StoppedAtStateOne {get;private set;}
        internal PracticeFlag23PatrolResult(Flag23PatrolWork work,int supplied)
        {
            ships=(byte[])work.Ships.Clone();fleets=(byte[])work.Fleets.Clone();orders=(byte[])work.Orders.Clone();candidates=(byte[])work.Candidates.Clone();Backing=work.Backing;
            SuppliedCounterWord=supplied;CounterWordAfter=work.Counter;RosterIndexAfter=work.Roster;RouteUiRequests=work.Ui;SelectedWaypoint=work.Waypoint;Activations=work.Activations;MemberVisits=work.Members;ProcessedAircraft=work.Processed;StoppedAtStateOne=work.StateOne;
        }
        public byte[] CopyShipPool(){return(byte[])ships.Clone();}
        public byte[] CopyFleetPool(){return(byte[])fleets.Clone();}
        public byte[] CopyPrimaryOrders(){return(byte[])orders.Clone();}
        public byte[] CopyCandidateRegion(){return(byte[])candidates.Clone();}
    }
sealed class Flag23PatrolWork
{
    internal byte[] Ships,Fleets,Orders,Candidates;internal PracticeAiSharedBacking Backing;
    internal int Ui,Waypoint,Counter,Roster,Activations,Members,Processed=-1;internal bool StateOne;
}
public static class PracticeFlag23Patrol
{
    static int I(byte[] b,int at){return BitConverter.ToInt32(b,at);}static int W(byte[] b,int at){return BitConverter.ToInt16(b,at);}
    static void I(byte[] b,int at,int n){BitConverter.GetBytes(n).CopyTo(b,at);}static void W(byte[] b,int at,int n){BitConverter.GetBytes(unchecked((short)n)).CopyTo(b,at);}
    static int Ship(uint p){long at=(long)p-0x4fc340;if(at<0||at>=402000||at%268!=0)throw new InvalidDataException("Flag2/3 ship reference");return(int)(at/268);}
    static int Fleet(uint p){long at=(long)p-0x4b47a0;if(at<0||at>=123000||at%492!=0)throw new InvalidDataException("Flag2/3 fleet reference");return(int)(at/492);}
    static int Clamp(int n){return n>=630?630:n>0?n:0;}
    static void Members(PracticeAttachmentPatrolTables t,byte[] s,byte[] f,int child)
    {
        int kind=I(f,child*492),count=0;
        for(int m=0;m<6;m++)
        {
            uint p=BitConverter.ToUInt32(f,child*492+16+m*4);if(p==0)continue;int u=Ship(p)*268,type=I(s,u),length=W(s,u+14);if(type<8||type>10||length<1||length>3)throw new NotSupportedException("Flag2/3 real aircraft templates8..10 required");bool eq=false;
            for(int n=0;n<length*4;n++){int weapon=kind==6?t.Aircraft.Equipment(6,4,n):I(s,u+100+n*4);if(weapon<0||weapon>=41)throw new InvalidDataException("Flag2/3 weapon id");if(weapon!=0&&(t.Metadata[weapon]==0xf2||t.Metadata[weapon]==0xf3))eq=true;}
            if(!eq)throw new NotSupportedException("Flag2/3 no-equipment fallback excluded");count++;
        }
        if(count==0)throw new NotSupportedException("Flag2/3 populated aircraft required");
    }
    public static PracticeFlag23PatrolResult EvaluateNoTargets(PracticeAttachmentPatrolTables t,byte[] ships,byte[] fleets,byte[] orders,PracticeAiSharedBacking backing,byte[] candidate,int parent,int delta,int suppliedLocal,int waypoint)
    {
        if(t==null||backing==null)throw new ArgumentNullException("tables/backing");if(ships==null||ships.Length!=402000||fleets==null||fleets.Length!=123000||orders==null||orders.Length!=78000||candidate==null||candidate.Length!=1184)throw new InvalidDataException("Flag2/3 complete pools");if(parent<0||parent>=250||waypoint< -1||waypoint>4)throw new ArgumentOutOfRangeException("parent");int pa=parent*492,owner=W(fleets,pa+6),flag=fleets[pa+486];
        if(I(fleets,pa)!=1||(flag!=2&&flag!=3)||owner<0||owner>1)throw new NotSupportedException("Flag2/3 compared flag2/3 surface parent");if(I(backing.CopyTaskRecord(parent),204)!=0x4b47a0+pa)throw new InvalidDataException("Flag2/3 parent context");for(int g=0;g<250;g++)if(fleets[g*492+467]!=0)throw new NotSupportedException("Flag2/3 visible targets excluded");
        var r=new Flag23PatrolWork{Ships=(byte[])ships.Clone(),Fleets=(byte[])fleets.Clone(),Orders=(byte[])orders.Clone(),Candidates=(byte[])candidate.Clone(),Backing=backing,Counter=suppliedLocal,Waypoint=waypoint};
        for(int member=0;member<6;member++)
        {
            uint p=BitConverter.ToUInt32(r.Fleets,pa+16+member*4);if(p==0)continue;int unit=Ship(p),up=unit*268,type=I(r.Ships,up);if(type!=5&&type!=6&&type!=7)continue;r.Counter=unchecked(r.Counter+2);
            for(int link=0;link<4;link++)
            {
                uint reference=BitConverter.ToUInt32(r.Ships,up+56+link*4);if(reference==0)continue;int child=Fleet(reference),at=child*492,kind=I(r.Fleets,at);if(kind!=6&&kind!=3)continue;
                if(W(r.Fleets,at+4)!=child||W(r.Fleets,at+8)!=unit||W(r.Ships,up+12)!=parent||W(r.Fleets,at+10)!=link||W(r.Fleets,at+6)<0||W(r.Fleets,at+6)>1)throw new NotSupportedException("Flag2/3 copied attachment identity");var task=r.Backing.CopyTaskRecord(child);if(I(task,204)!=0x4b47a0+at)throw new InvalidDataException("Flag2/3 aircraft task identity");Members(t,r.Ships,r.Fleets,child);
                if(kind==6)
                {
                    int timer=unchecked(I(task,252)+delta);I(task,252,timer);r.Backing=r.Backing.WithTaskRecord(child,task);
                    if(timer>=5000)
                    {
                        var transition=PracticeAircraftTransitions.ApplyOffscreen(t.Aircraft,t.Units,r.Ships,r.Fleets,r.Orders,child,4,249,r.Waypoint,-2);r.Ships=transition.CopyShipPool();r.Fleets=transition.CopyFleetPool();r.Orders=transition.CopyPrimaryOrders();r.Waypoint=transition.SelectedWaypoint;r.Ui+=transition.Invalidations;r.Fleets[at+486]=1;task=r.Backing.CopyTaskRecord(child);I(task,200,unchecked(r.Counter+link)%4);r.Backing=r.Backing.WithTaskRecord(child,task);r.Activations++;
                    }
                }
                if(I(r.Fleets,at)!=3)continue;task=r.Backing.CopyTaskRecord(child);var scan=PracticeFleetCandidates.Evaluate(r.Ships,r.Fleets,task,t.Metadata,t.Ranges,child,1,owner);if(!scan.EquipmentFound||scan.Candidates.Count!=0)throw new NotSupportedException("Flag2/3 member no-target path");scan.CopyScratchBytes().CopyTo(r.Candidates,0);I(r.Candidates,176,-1);Array.Clear(task,212,24);r.Members++;r.Processed=child;
                for(int n=1;n<5;n++)W(r.Fleets,at+40+n*2,-1);
                if(I(r.Fleets,at+472)==1){I(task,200,0);I(task,252,0);r.Backing=r.Backing.WithTaskRecord(child,task);r.StateOne=true;r.Roster=member;return new PracticeFlag23PatrolResult(r,suppliedLocal);}
                int phase=I(task,200),x=W(r.Fleets,at+40),y=W(r.Fleets,at+50),px=W(r.Fleets,pa+12),py=W(r.Fleets,pa+14);
                if(phase<0||phase>3){x=unchecked((short)(px-20));y=py;I(task,200,0);}
                else if(x==-1){I(task,200,(phase+1)%4);x=phase==1?unchecked((short)(px-20)):phase==3?unchecked((short)(px+20)):px;y=phase==0?unchecked((short)(py-20)):phase==2?unchecked((short)(py+20)):py;}
                W(r.Fleets,at+40,Clamp(x));W(r.Fleets,at+50,Clamp(y));r.Backing=r.Backing.WithTaskRecord(child,task);r.Roster=7;return new PracticeFlag23PatrolResult(r,suppliedLocal);
            }
        }
        r.Roster=6;return new PracticeFlag23PatrolResult(r,suppliedLocal);
    }
}

}

