using System;using System.IO;using FleetLab;
namespace FleetLab
{
    // Pinned41FCC0 caller-specific subset with an EXPLICIT initial aliased
    // stack word. One flag2/3 surface parent, flag1 aircraft8..10 only, and
    // activity0/collision255 navigation. No natural initial value is inferred.
    public sealed class PracticePrephaseFlag23Result
    {
        readonly byte[] ships,fleets,orders,candidates;
        public PracticeAiSharedBacking Backing {get;private set;}
        public int Visits {get{return 250;}}
        public int SuppliedCallerWord {get;private set;}
        public int CallerWordAfter {get;private set;}
        public int PolicyVisits {get;private set;}
        public int Flag1RosterWrites {get;private set;}
        public int IdleReturns {get;private set;}
        public int RouteUiRequests {get;private set;}
        public int SelectedWaypoint {get;private set;}
        public int Activations {get;private set;}
        public int MemberVisits {get;private set;}
        internal PracticePrephaseFlag23Result(PrephaseFlag23Work work,int supplied)
        {
            ships=(byte[])work.Ships.Clone();fleets=(byte[])work.Fleets.Clone();orders=(byte[])work.Orders.Clone();candidates=(byte[])work.Candidates.Clone();Backing=work.Backing;
            SuppliedCallerWord=supplied;CallerWordAfter=work.Counter;PolicyVisits=work.Policies;Flag1RosterWrites=work.Flag1RosterWrites;IdleReturns=work.Idles;RouteUiRequests=work.Ui;SelectedWaypoint=work.Waypoint;Activations=work.Activations;MemberVisits=work.Members;
        }
        public byte[] CopyShipPool(){return(byte[])ships.Clone();}
        public byte[] CopyFleetPool(){return(byte[])fleets.Clone();}
        public byte[] CopyPrimaryOrders(){return(byte[])orders.Clone();}
        public byte[] CopyCandidateRegion(){return(byte[])candidates.Clone();}
    }
sealed class PrephaseFlag23Work
{
    internal byte[] Ships,Fleets,Orders,Candidates;internal PracticeAiSharedBacking Backing;
    internal int Counter,Ui,Waypoint,Idles,Policies,Flag1RosterWrites,Activations,Members;
}
public static class PracticePrephaseFlag23
{
    static int I(byte[] b,int at){return BitConverter.ToInt32(b,at);}
    static void AirOnly(byte[] s,byte[] f,int slot)
    {
        int at=slot*492,kind=I(f,at),count=0;if((kind!=3&&kind!=4)||(I(f,at+472)!=0&&I(f,at+472)!=1))throw new NotSupportedException("Explicit whole policy: compared flag1 aircraft kinds/states only");
        for(int n=0;n<6;n++){uint p=BitConverter.ToUInt32(f,at+16+n*4);if(p==0)continue;long offset=(long)p-0x4fc340;if(offset<0||offset>=402000||offset%268!=0)throw new InvalidDataException("Explicit whole policy: roster reference");int type=I(s,(int)offset);if(type<8||type>10)throw new NotSupportedException("Explicit whole policy: no eligible carrier in flag1 air callbacks");count++;}if(count==0)throw new NotSupportedException("Explicit whole policy: real air members required");
    }
    public static PracticePrephaseFlag23Result EvaluateNoTargets(PracticeAttachmentPatrolTables t,byte[] s,byte[] f,byte[] o,PracticeAiSharedBacking backing,byte[] c,int delta,int suppliedWord,int network,int selectedWaypoint)
    {
        if(t==null||backing==null)throw new ArgumentNullException("tables/backing");if(s==null||s.Length!=402000||f==null||f.Length!=123000||o==null||o.Length!=78000||c==null||c.Length!=1184)throw new InvalidDataException("Explicit whole policy: complete pools required");if(network!=0||selectedWaypoint< -1||selectedWaypoint>4)throw new ArgumentOutOfRangeException("selectedWaypoint");int parents=0;
        for(int g=0;g<250;g++)
        {
            int at=g*492,flag=f[at+486],state=I(f,at+472);var task=backing.CopyTaskRecord(g);bool idle=BitConverter.ToInt16(f,at+40)==-1&&(state==5||state==6);
            if(f[at+467]!=0||f[at+482]!=255||I(task,204)!=0x4b47a0+at)throw new NotSupportedException("Explicit whole policy: no-target/collision255/identity subset");
            if(I(task,304)!=0&&!idle)throw new NotSupportedException("Explicit whole policy: active navigation local-alias writes not compared");
            if(flag==2||flag==3){if(I(f,at)!=1)throw new NotSupportedException("Explicit whole policy: only surface flag23 parent");parents++;}else if(flag==1)AirOnly(s,f,g);
        }
        if(parents!=1)throw new NotSupportedException("Explicit whole policy: exactly one supplied flag23 surface parent is compared");
        var r=new PrephaseFlag23Work{Ships=(byte[])s.Clone(),Fleets=(byte[])f.Clone(),Orders=(byte[])o.Clone(),Candidates=(byte[])c.Clone(),Backing=backing,Counter=suppliedWord,Waypoint=selectedWaypoint};
        for(int g=0;g<250;g++)
        {
            int at=g*492,flag=r.Fleets[at+486];
            if(flag==2||flag==3)
            {
                r.Policies++;var p=PracticeFlag23Patrol.EvaluateNoTargets(t,r.Ships,r.Fleets,r.Orders,r.Backing,r.Candidates,g,delta,r.Counter,r.Waypoint);r.Ships=p.CopyShipPool();r.Fleets=p.CopyFleetPool();r.Orders=p.CopyPrimaryOrders();r.Backing=p.Backing;r.Candidates=p.CopyCandidateRegion();r.Counter=p.CounterWordAfter;r.Ui+=p.RouteUiRequests;r.Waypoint=p.SelectedWaypoint;r.Activations+=p.Activations;r.Members+=p.MemberVisits;
            }
            else if(flag==1){AirOnly(r.Ships,r.Fleets,g);r.Counter=6;r.Flag1RosterWrites++;r.Policies++;}
            int state=I(r.Fleets,at+472);
            if(BitConverter.ToInt16(r.Fleets,at+40)==-1&&(state==5||state==6))
            {
                var idle=PracticeIdleReturn.Apply(r.Fleets,r.Backing,g,0,r.Waypoint);r.Fleets=idle.CopyFleetPool();r.Backing=idle.Backing;r.Ui+=idle.RouteUiRequests;r.Waypoint=idle.SelectedWaypoint;r.Idles++;
                //41FCC0 keepsEBX=-1;41FC10's first push aliases this child's local.
                r.Counter=-1;
            }
            if(I(r.Backing.CopyTaskRecord(g),304)!=0)throw new NotSupportedException("Explicit whole policy: navigation must remain on the activity0 fast return");
            var nav=PracticeNavigationGate.Evaluate(r.Fleets,r.Backing,g);if(nav.Outcome!=PracticeNavigationGateOutcome.NoCollisionTarget)throw new NotSupportedException("Explicit whole policy: navigation path not compared");r.Backing=nav.Backing;
            //41F920 subESP4 leaves its local untouched on this fast return.
        }
        return new PracticePrephaseFlag23Result(r,suppliedWord);
    }
}


}

