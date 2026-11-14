using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace FleetLab
{
    // One explicitly supplied attachment in copied first-Practice data. The
    // recovered caller that chooses attachments is not executed by this study.
    public sealed class PracticeAircraftStudyState
    {
        readonly PracticeUnitCatalog catalog;
        readonly PracticeAircraftTables tables;
        readonly byte[] ships,fleets,orders;
        public int FleetSlot
        {
            get;
            private set;
        }
        public int ParentShip
        {
            get;
            private set;
        }
        public int Link
        {
            get;
            private set;
        }
        public int SelectedWaypoint
        {
            get;
            private set;
        }
        public int Invalidations
        {
            get;
            private set;
        }
        public int HomeSearches
        {
            get;
            private set;
        }
        public bool? LastHomeFound
        {
            get;
            private set;
        }
        public ReadOnlyCollection<PracticeAircraftHomeCandidate> HomeCandidates
        {
            get
            {
                return Kind>=3&&Kind<=5?PracticeAircraftHomeSearch.FindCandidates(ships,fleets,FleetSlot):new List<PracticeAircraftHomeCandidate>().AsReadOnly();
            }
        }
        public bool HasGroup
        {
            get
            {
                return FleetSlot>=0;
            }
        }
        public int Kind
        {
            get
            {
                return HasGroup?BitConverter.ToInt32(fleets,FleetSlot*492):0;
            }
        }
        public int Mode
        {
            get
            {
                return HasGroup?BitConverter.ToInt32(fleets,FleetSlot*492+476):0;
            }
        }
        public bool Stored
        {
            get
            {
                return Kind>=6&&Kind<=8;
            }
        }
        public ReadOnlyCollection<PracticeUnitRecord> Members
        {
            get
            {
                var result=new List<PracticeUnitRecord>();
                if(HasGroup)for(int n=0;n<6;n++)
                {
                    uint r=BitConverter.ToUInt32(fleets,FleetSlot*492+16+n*4);
                    if(r==0)continue;
                    long at=(long)r-0x4fc340;
                    if(at<0||at>=402000||at%268!=0)throw new InvalidDataException("Invalid aircraft member reference.");
                    if(BitConverter.ToInt32(ships,(int)at)!=0)result.Add(new PracticeUnitRecord(ships,(int)at/268));
                }
                return result.AsReadOnly();
            }
        }
        public PracticeAircraftStudyState(PracticeUnitCatalog unitTables,PracticeAircraftTables aircraftTables,PracticeDeployment deployment)
        {
            if(unitTables==null||aircraftTables==null||deployment==null)throw new ArgumentNullException("deployment");
            catalog=unitTables;
            tables=aircraftTables;
            ships=deployment.CopyShipPool();
            fleets=deployment.CopyFleetPool();
            orders=new byte[78000];
            for(int unit=0;unit<1500;unit++)for(int n=0;n<10;n++)BitConverter.GetBytes((short)-1).CopyTo(orders,unit*52+8+n*2);
            FleetSlot=ParentShip=-1;
            SelectedWaypoint=-1;
        }
        PracticeAircraftStudyState(PracticeAircraftStudyState before,byte[] s,byte[] f,byte[] o,int slot,int parent,int link,int point,int refresh)
        {
            catalog=before.catalog;
            tables=before.tables;
            ships=s;
            fleets=f;
            orders=o;
            FleetSlot=slot;
            ParentShip=parent;
            Link=link;
            SelectedWaypoint=point;
            Invalidations=refresh;
            HomeSearches=before.HomeSearches;
            LastHomeFound=before.LastHomeFound;
        }
        public PracticeAircraftStudyState Create(int parentShip,int kind,int link)
        {
            if(HasGroup)throw new InvalidOperationException("Reset the study before creating another aircraft group.");
            if(parentShip<0||parentShip>=1500)throw new ArgumentOutOfRangeException("parentShip");
            int parentFleet=BitConverter.ToInt16(ships,parentShip*268+12),destination=-1;
            for(int f=0;f<250;f++)if(BitConverter.ToInt32(fleets,f*492)==0)
            {
                destination=f;
                break;
            }
            if(destination<0)throw new InvalidDataException("No inactive fleet slot remains in the copied study.");
            var result=PracticeAircraftFactory.CreateOffscreen(catalog,ships,fleets,parentShip,parentFleet,destination,kind,link,-2);
            return new PracticeAircraftStudyState(this,result.CopyShipPool(),result.CopyFleetPool(),(byte[])orders.Clone(),destination,parentShip,link,SelectedWaypoint,Invalidations);
        }
        public PracticeAircraftStudyState Transition(int mode)
        {
            if(!HasGroup)throw new InvalidOperationException("Create a stored aircraft group first.");
            var result=PracticeAircraftTransitions.ApplyOffscreen(tables,catalog,ships,fleets,orders,FleetSlot,mode,FleetSlot==249?248:249,SelectedWaypoint,-2);
            return new PracticeAircraftStudyState(this,result.CopyShipPool(),result.CopyFleetPool(),result.CopyPrimaryOrders(),FleetSlot,ParentShip,Link,result.SelectedWaypoint,checked(Invalidations+result.Invalidations));
        }
        public PracticeAircraftStudyState SearchHome()
        {
            if(!HasGroup||Kind<3||Kind>5)throw new InvalidOperationException("Activate an aircraft group before searching for a home.");
            var result=PracticeAircraftHomeSearch.ApplyOffscreen(ships,fleets,FleetSlot);
            var next=new PracticeAircraftStudyState(this,result.CopyShipPool(),result.CopyFleetPool(),(byte[])orders.Clone(),FleetSlot,result.ParentUnit,result.Link,SelectedWaypoint,Invalidations);
            next.HomeSearches=checked(HomeSearches+1);
            next.LastHomeFound=result.FoundHome;
            return next;
        }
        public byte[] CopyShipPool()
        {
            return (byte[])ships.Clone();
        }
        public byte[] CopyFleetPool()
        {
            return (byte[])fleets.Clone();
        }
        public byte[] CopyPrimaryOrders()
        {
            return (byte[])orders.Clone();
        }
    }
}
