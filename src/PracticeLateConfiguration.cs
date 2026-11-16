using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace FleetLab
{
    public sealed class PracticeCarrierAllocation
    {
        public readonly int ParentUnit, ParentFleet, Equipment, DestinationFleet, Kind, Link;
        internal PracticeCarrierAllocation(int parent,int fleet,int equipment,int destination,int kind,int link)
        {
            ParentUnit=parent;ParentFleet=fleet;Equipment=equipment;DestinationFleet=destination;Kind=kind;Link=link;
        }
    }
    public sealed class PracticeLateConfigurationResult
    {
        readonly byte[] ships,fleets;
        readonly ReadOnlyCollection<PracticeCarrierAllocation> allocations;
        readonly ReadOnlyCollection<int> cleared;
        public int NextInactiveFleet { get; private set; }
        public int NumberedMapObjects { get; private set; }
        public ReadOnlyCollection<PracticeCarrierAllocation> CarrierGroups { get { return allocations; } }
        public ReadOnlyCollection<int> ClearedMarkerUnits { get { return cleared; } }
        internal PracticeLateConfigurationResult(byte[] s,byte[] f,int next,int numbered,List<PracticeCarrierAllocation> groups,List<int> markers)
        {
            ships=s;fleets=f;NextInactiveFleet=next;NumberedMapObjects=numbered;allocations=groups.AsReadOnly();cleared=markers.AsReadOnly();
        }
        public byte[] CopyShipPool(){return (byte[])ships.Clone();}
        public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
    }
    // RIP SHA B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569.
    // 42C100..42C354 data pass, before the scenario callback405C20.
    // Complete copied pools; offscreen aircraft factories only. No inventory
    // startup, callback/button layout, selection, rendering, AI or mission flow.
    public static class PracticeLateConfiguration
    {
        static int I(byte[] b,int p){return BitConverter.ToInt32(b,p);}
        static int W(byte[] b,int p){return BitConverter.ToInt16(b,p);}
        static int Unit(uint reference)
        {
            long at=(long)reference-0x4fc340;
            if(at<0||at>=402000||at%268!=0)throw new InvalidDataException("Late configuration reached an invalid normalized unit reference.");
            return (int)(at/268);
        }
        static int Next(byte[] fleets,int start)
        {
            for(int slot=start;slot<250;slot++)if(I(fleets,slot*492)==0)return slot;
            throw new InvalidDataException("Late configuration has no admitted next inactive fleet; no copied result was published.");
        }
        public static PracticeLateConfigurationResult ApplyOffscreen(PracticeUnitCatalog catalog,byte[] shipPool,byte[] fleetPool,short localOwner)
        {
            if(catalog==null)throw new ArgumentNullException("catalog");
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000)throw new InvalidDataException("Late configuration requires complete unit and fleet pools.");
            var ships=(byte[])shipPool.Clone();var fleets=(byte[])fleetPool.Clone();
            int next=Next(fleets,0),numbered=0;var counters=new int[]{1,1,1,1};
            var groups=new List<PracticeCarrierAllocation>();var markers=new List<int>();
            for(int fleet=0;fleet<250;fleet++)
            {
                int at=fleet*492;
                if(I(fleets,at)==11)
                {
                    int owner=W(fleets,at+6);
                    if(owner<0||owner>3)throw new InvalidDataException("Late map-object numbering requires an owner in0..3.");
                    fleets[at+484]=(byte)counters[owner]++;numbered++;
                }
                for(int member=0;member<6;member++)
                {
                    uint reference=BitConverter.ToUInt32(fleets,at+16+member*4);
                    if(reference==0)continue;
                    int unit=Unit(reference),u=unit*268,type=I(ships,u);
                    if(type<5||type>7)continue;
                    int length=W(ships,u+14);
                    if(length<0||length>6)throw new InvalidDataException("Late carrier equipment is outside the admitted six-row extent.");
                    for(int row=0;row<length;row++)for(int column=0;column<4;column++)
                    {
                        int equipment=I(ships,u+100+(row*4+column)*4);
                        if(equipment<35||equipment>38)continue;
                        if(W(ships,u+4)!=unit)throw new InvalidDataException("Late carrier unit ID does not match its normalized reference.");
                        int kind=equipment==35?6:7,link=equipment-35,destination=next;
                        var result=PracticeAircraftFactory.CreateOffscreen(catalog,ships,fleets,unit,fleet,destination,kind,link,localOwner);
                        ships=result.CopyShipPool();fleets=result.CopyFleetPool();
                        groups.Add(new PracticeCarrierAllocation(unit,fleet,equipment,destination,kind,link));
                        // The source searches again even after its last creation.
                        next=Next(fleets,next);
                    }
                }
            }
            for(int fleet=0;fleet<250;fleet++)for(int member=0;member<6;member+=2)
            {
                uint reference=BitConverter.ToUInt32(fleets,fleet*492+16+member*4);
                if(reference==0)continue;
                int unit=Unit(reference),at=unit*268,type=I(ships,at);
                if(type==22||type==23){BitConverter.GetBytes(0).CopyTo(ships,at);markers.Add(unit);}
            }
            return new PracticeLateConfigurationResult(ships,fleets,next,numbered,groups,markers);
        }
    }
}
