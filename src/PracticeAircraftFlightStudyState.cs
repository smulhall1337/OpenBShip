using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace FleetLab
{
    // Copied manual waypoint study. Queue edits are supplied diagnostic data;
    // each visit repeats the actor's four-slot source batch, with no wall clock.
    public sealed class PracticeAircraftFlightStudyState
    {
        readonly PracticeUnitCatalog units;
        readonly PracticeAircraftTables modes;
        readonly PracticeAircraftDestructionTable destruction;
        readonly byte[] visuals;
        readonly byte[] ships,fleets,scores,effects,auxiliary,primary;
        public bool SuppliedTestAirfield
        {
            get;
            private set;
        }
        public int AvailableHomeResults
        {
            get;
            private set;
        }
        public PracticeAircraftHomeSearchSummary LastHomeSearch
        {
            get;
            private set;
        }
        public bool CanPrepareTestAirfield
        {
            get
            {
                return !SuppliedTestAirfield&&Kind==5&&ProfileReady&&BitConverter.ToInt32(ships,100*268)==0&&BitConverter.ToInt32(fleets,30*492)==0;
            }
        }
        public int FleetSlot
        {
            get;
            private set;
        }
        public int Visits
        {
            get;
            private set;
        }
        public int SelectedWaypoint
        {
            get;
            private set;
        }
        public int MovieCounter
        {
            get;
            private set;
        }
        public uint RandomState
        {
            get;
            private set;
        }
        public PracticeAircraftWaypointBatchResult LastUpdate
        {
            get;
            private set;
        }
        public int Kind
        {
            get
            {
                return BitConverter.ToInt32(fleets,FleetSlot*492);
            }
        }
        public int Task
        {
            get
            {
                return BitConverter.ToInt32(fleets,FleetSlot*492+476);
            }
        }
        public int ReturnState
        {
            get
            {
                return BitConverter.ToInt32(fleets,FleetSlot*492+472);
            }
        }
        public int Speed
        {
            get
            {
                return BitConverter.ToInt16(fleets,FleetSlot*492+60);
            }
        }
        public int Facing
        {
            get
            {
                return fleets[FleetSlot*492+62];
            }
        }
        public Point Origin
        {
            get
            {
                return new Point(BitConverter.ToInt16(fleets,FleetSlot*492+12),BitConverter.ToInt16(fleets,FleetSlot*492+14));
            }
        }
        public PracticeAircraftReturnBatchResult LastReturn
        {
            get;
            private set;
        }
        public bool Stored
        {
            get
            {
                return Kind>=6&&Kind<=8;
            }
        }
        public bool ReturnProfileReady
        {
            get
            {
                return Active&&(ReturnState==0||ReturnState==1)&&(Task==1||Task==2||Task==4)&&fleets[FleetSlot*492+485]==0;
            }
        }
        public int EmptyHomeResults
        {
            get;
            private set;
        }
        public bool LastEmptyHomeResult
        {
            get;
            private set;
        }
        public bool ExtendedReturnProfileReady
        {
            get
            {
                return ReturnProfileReady||(Active&&Kind==5&&ReturnState==2&&Task==2&&Route.Count==0&&fleets[FleetSlot*492+485]==0);
            }
        }
        public bool Active
        {
            get
            {
                return Kind>=3&&Kind<=5;
            }
        }
        public bool ProfileReady
        {
            get
            {
                return Active&&ReturnState==0&&(Task==1||Task==2||Task==4)&&fleets[FleetSlot*492+485]==0;
            }
        }
        public ReadOnlyCollection<Point> Route
        {
            get
            {
                var points=new List<Point>();
                for(int n=0;n<5;n++)
                {
                    int x=BitConverter.ToInt16(fleets,FleetSlot*492+40+n*2);
                    if(x==-1)break;
                    points.Add(new Point(x,BitConverter.ToInt16(fleets,FleetSlot*492+50+n*2)));
                }
                return points.AsReadOnly();
            }
        }
        public ReadOnlyCollection<PracticeUnitRecord> Members
        {
            get
            {
                var list=new List<PracticeUnitRecord>();
                for(int n=0;n<6;n++)
                {
                    uint reference=BitConverter.ToUInt32(fleets,FleetSlot*492+16+n*4);
                    if(reference==0)continue;
                    long at=(long)reference-0x4fc340;
                    if(at<0||at>=402000||at%268!=0)throw new InvalidDataException("Aircraft flight member reference crosses its pool.");
                    if(BitConverter.ToInt32(ships,(int)at)!=0)list.Add(new PracticeUnitRecord(ships,(int)at/268));
                }
                return list.AsReadOnly();
            }
        }
        public PracticeAircraftFlightStudyState(string programs,PracticeAircraftStudyState parent)
        {
            if(parent==null||!parent.HasGroup||parent.Kind<3||parent.Kind>5)throw new InvalidOperationException("Activate an aircraft group before opening its waypoint study.");
            units=PracticeUnitCatalog.Load(programs);
            modes=PracticeAircraftTables.Load(programs);
            destruction=PracticeAircraftDestructionTable.Load(programs);
            visuals=File.ReadAllBytes(Path.Combine(programs,"ClassicVisuals.seqbin"));
            ships=parent.CopyShipPool();
            fleets=parent.CopyFleetPool();
            primary=parent.CopyPrimaryOrders();
            FleetSlot=parent.FleetSlot;
            SelectedWaypoint=parent.SelectedWaypoint;
            scores=new byte[176];
            BitConverter.GetBytes((short)-2).CopyTo(scores,172);
            effects=new byte[800];
            for(int n=0;n<40;n++)BitConverter.GetBytes((short)-1).CopyTo(effects,n*20);
            auxiliary=new byte[78336];
            RandomState=731;
            MovieCounter=0;
        }
        PracticeAircraftFlightStudyState(PracticeAircraftFlightStudyState before,byte[] s,byte[] f,byte[] score,byte[] effect,byte[] aux,byte[] orders,int point,int visits,int movie,uint random,PracticeAircraftWaypointBatchResult last,PracticeAircraftReturnBatchResult returning=null,int emptyHomeResults=-1,bool lastEmptyHome=false,bool suppliedTestAirfield=false,int availableHomeResults=-1,PracticeAircraftHomeSearchSummary homeSearch=null)
        {
            units=before.units;
            modes=before.modes;
            destruction=before.destruction;
            visuals=before.visuals;
            ships=s;
            fleets=f;
            scores=score;
            effects=effect;
            auxiliary=aux;
            primary=orders;
            FleetSlot=before.FleetSlot;
            SelectedWaypoint=point;
            Visits=visits;
            MovieCounter=movie;
            RandomState=random;
            LastUpdate=last;
            LastReturn=returning;
            EmptyHomeResults=emptyHomeResults<0?before.EmptyHomeResults:emptyHomeResults;
            LastEmptyHomeResult=lastEmptyHome;
            SuppliedTestAirfield=before.SuppliedTestAirfield||suppliedTestAirfield;
            AvailableHomeResults=availableHomeResults<0?before.AvailableHomeResults:availableHomeResults;
            LastHomeSearch=homeSearch??before.LastHomeSearch;
        }
        // Synthetic diagnostic fixture, matching the supplied native comparison.
        // This does not recover map placement or the original airfield factory.
        public PracticeAircraftFlightStudyState PrepareTestAirfield()
        {
            if(!CanPrepareTestAirfield)throw new InvalidOperationException("An active, outbound AWACS and unused unit100 / fleet30 are required for the test airfield.");
            if(PracticeAircraftHomeSearch.FindCandidates(ships,fleets,FleetSlot).Count!=0)throw new InvalidOperationException("The test airfield requires a copy with no existing landing candidates.");
            int parent=BitConverter.ToInt16(fleets,FleetSlot*492+8);
            if(parent<0||parent>=1500)throw new InvalidDataException("Associated parent crosses its pool.");
            int home=BitConverter.ToInt16(ships,parent*268+12);
            if(home<0||home>=250)throw new InvalidDataException("Associated home crosses its pool.");
            int x=BitConverter.ToInt16(fleets,home*492+12)+60,y=BitConverter.ToInt16(fleets,home*492+14)-20;
            if(x<0||x>=640||y<0||y>=640)throw new InvalidOperationException("The supplied airfield would be outside the study world.");
            var s=(byte[])ships.Clone();
            var f=(byte[])fleets.Clone();
            int af=30*492,au=100*268,owner=BitConverter.ToInt16(f,FleetSlot*492+6);
            Action<byte[],int,int> word=delegate(byte[] b,int at,int value)
            {
                BitConverter.GetBytes((short)value).CopyTo(b,at);
            };
            Action<byte[],int,int> integer=delegate(byte[] b,int at,int value)
            {
                BitConverter.GetBytes(value).CopyTo(b,at);
            };
            integer(f,af,11);
            word(f,af+4,30);
            word(f,af+6,owner);
            word(f,af+12,x);
            word(f,af+14,y);
            for(int n=0;n<6;n++)integer(f,af+16+n*4,0);
            integer(f,af+16,0x4fc340+au);
            for(int n=0;n<100;n++)integer(f,af+64+n*4,0);
            integer(s,au,20);
            word(s,au+4,100);
            word(s,au+6,owner);
            word(s,au+8,2);
            word(s,au+10,3);
            word(s,au+12,30);
            word(s,au+14,3);
            word(s,au+16,3);
            for(int n=0;n<4;n++)
            {
                integer(s,au+56+n*4,0);
                integer(f,af+64+(23+n)*4,2);
            }
            return new PracticeAircraftFlightStudyState(this,s,f,scores,effects,auxiliary,primary,SelectedWaypoint,Visits,MovieCounter,RandomState,null,null,-1,false,true);
        }
        public PracticeAircraftFlightStudyState QueueSuppliedPoint(int x,int y)
        {
            if(!Active)throw new InvalidOperationException("Reset the flight study before editing a retired group.");
            if(x<0||x>=640||y<0||y>=640)throw new ArgumentOutOfRangeException("point","Test waypoint coordinates must be0..639.");
            int n=0;
            while(n<5&&BitConverter.ToInt16(fleets,FleetSlot*492+40+n*2)!=-1)n++;
            if(n==5)throw new InvalidOperationException("The copied route already contains five waypoints.");
            var f=(byte[])fleets.Clone();
            BitConverter.GetBytes((short)x).CopyTo(f,FleetSlot*492+40+n*2);
            BitConverter.GetBytes((short)y).CopyTo(f,FleetSlot*492+50+n*2);
            return new PracticeAircraftFlightStudyState(this,ships,f,scores,effects,auxiliary,primary,SelectedWaypoint,Visits,MovieCounter,RandomState,null);
        }
        public PracticeAircraftFlightStudyState ClearSuppliedRoute()
        {
            var f=(byte[])fleets.Clone();
            for(int n=0;n<5;n++)BitConverter.GetBytes((short)-1).CopyTo(f,FleetSlot*492+40+n*2);
            return new PracticeAircraftFlightStudyState(this,ships,f,scores,effects,auxiliary,primary,SelectedWaypoint,Visits,MovieCounter,RandomState,null);
        }
        public PracticeAircraftFlightStudyState ApplyModeTwo()
        {
            if(!Active&&!Stored)throw new InvalidOperationException("Reset the flight study before applying a mode to a retired group.");
            PracticeAircraftTransitionResult active;
            if(Stored)active=PracticeAircraftTransitions.ApplyOffscreen(modes,units,ships,fleets,primary,FleetSlot,2,249,SelectedWaypoint,-2);
            else
            {
                var stored=PracticeAircraftTransitions.ApplyOffscreen(modes,units,ships,fleets,primary,FleetSlot,0,249,SelectedWaypoint,-2);
                active=PracticeAircraftTransitions.ApplyOffscreen(modes,units,stored.CopyShipPool(),stored.CopyFleetPool(),stored.CopyPrimaryOrders(),FleetSlot,2,249,stored.SelectedWaypoint,-2);
            }
            return new PracticeAircraftFlightStudyState(this,active.CopyShipPool(),active.CopyFleetPool(),scores,effects,auxiliary,active.CopyPrimaryOrders(),active.SelectedWaypoint,Visits,MovieCounter,RandomState,null);
        }
        public PracticeAircraftFlightStudyState VisitOnce()
        {
            var result=PracticeAircraftWaypointBatch.AdvanceOffscreen(destruction,visuals,ships,fleets,scores,effects,auxiliary,new PracticeFleetScheduler(FleetSlot/4*4,0),FleetSlot,SelectedWaypoint,MovieCounter,RandomState);
            var b=result.Batch;
            return new PracticeAircraftFlightStudyState(this,b.CopyShipPool(),b.CopyFleetPool(),b.CopyScoreRegion(),b.CopyEffectPool(),result.CopyAuxiliary(),primary,result.SelectedWaypoint,checked(Visits+1),b.MovieCounter,b.RandomState,result);
        }
        public PracticeAircraftFlightStudyState VisitIncludingReturn()
        {
            var result=PracticeAircraftFlightBatch.AdvanceOffscreen(destruction,visuals,units,modes,ships,fleets,scores,effects,primary,auxiliary,new PracticeFleetScheduler(FleetSlot/4*4,0),FleetSlot,SelectedWaypoint,MovieCounter,RandomState);
            var b=result.Batch;
            return new PracticeAircraftFlightStudyState(this,b.CopyShipPool(),b.CopyFleetPool(),b.CopyScoreRegion(),b.CopyEffectPool(),result.CopyAuxiliary(),result.CopyPrimaryOrders(),result.SelectedWaypoint,checked(Visits+1),b.MovieCounter,b.RandomState,result.WaypointUpdate,result.ReturnUpdate);
        }
        public PracticeAircraftFlightStudyState VisitIncludingEmptyHomeFallback()
        {
            var result=PracticeAircraftFlightBatch.AdvanceWithEmptyHomeFallback(destruction,visuals,units,modes,ships,fleets,scores,effects,primary,auxiliary,new PracticeFleetScheduler(FleetSlot/4*4,0),FleetSlot,SelectedWaypoint,MovieCounter,RandomState);
            var b=result.Batch;
            return new PracticeAircraftFlightStudyState(this,b.CopyShipPool(),b.CopyFleetPool(),b.CopyScoreRegion(),b.CopyEffectPool(),result.CopyAuxiliary(),result.CopyPrimaryOrders(),result.SelectedWaypoint,checked(Visits+1),b.MovieCounter,b.RandomState,result.WaypointUpdate,result.ReturnUpdate,checked(EmptyHomeResults+result.NoHomeGroups),result.NoHomeGroups!=0);
        }
        public PracticeAircraftFlightStudyState VisitIncludingHomeSearch()
        {
            var result=PracticeAircraftFlightBatch.AdvanceWithHomeSearch(destruction,visuals,units,modes,ships,fleets,scores,effects,primary,auxiliary,new PracticeFleetScheduler(FleetSlot/4*4,0),FleetSlot,SelectedWaypoint,MovieCounter,RandomState);
            var b=result.Batch;
            return new PracticeAircraftFlightStudyState(this,b.CopyShipPool(),b.CopyFleetPool(),b.CopyScoreRegion(),b.CopyEffectPool(),result.CopyAuxiliary(),result.CopyPrimaryOrders(),result.SelectedWaypoint,checked(Visits+1),b.MovieCounter,b.RandomState,result.WaypointUpdate,result.ReturnUpdate,checked(EmptyHomeResults+result.NoHomeGroups),result.NoHomeGroups!=0,false,checked(AvailableHomeResults+(result.HomeSearch!=null&&result.HomeSearch.FoundHome?1:0)),result.HomeSearch);
        }
        public byte[] CopyShipPool()
        {
            return (byte[])ships.Clone();
        }
        public byte[] CopyFleetPool()
        {
            return (byte[])fleets.Clone();
        }
        public byte[] CopyScoreRegion()
        {
            return (byte[])scores.Clone();
        }
        public byte[] CopyEffectPool()
        {
            return (byte[])effects.Clone();
        }
        public byte[] CopyAuxiliary()
        {
            return (byte[])auxiliary.Clone();
        }
        public byte[] CopyPrimaryOrders()
        {
            return (byte[])primary.Clone();
        }
    }
}
