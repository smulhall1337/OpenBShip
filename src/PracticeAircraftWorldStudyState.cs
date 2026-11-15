using System;
using System.IO;
namespace FleetLab
{
    // Copied caller fragment: two427670 batches, secondary419970,
    // timed416C40, inactiveprimary416090 and40E0D0. Supplied40ms steps
    // do not establish application pacing or the AI caller's cadence.
    public sealed class PracticeAircraftWorldStudyState
    {
        readonly PracticeAircraftDestructionTable destruction;
        readonly PracticeUnitCatalog units;
        readonly PracticeAircraftTables modes;
        readonly byte[] visuals,terrain,auxiliary,secondaryObjects;
        public PracticeAircraftOrderStudyState Orders
        {
            get;
            private set;
        }
        public PracticeTimedStudyState Combat
        {
            get
            {
                return Orders.Combat;
            }
        }
        public PracticeFleetScheduler Schedule
        {
            get;
            private set;
        }
        public int SelectedFleet
        {
            get;
            private set;
        }
        public int SelectedWaypoint
        {
            get;
            private set;
        }
        public int AircraftVisits
        {
            get;
            private set;
        }
        public int SurfaceVisits
        {
            get;
            private set;
        }
        public int LastAircraftVisits
        {
            get;
            private set;
        }
        public int LastSurfaceVisits
        {
            get;
            private set;
        }
        public int Invalidations
        {
            get;
            private set;
        }
        public PracticeAircraftWorldStudyState(PracticeAircraftOrderStudyState orders,PracticeAircraftDestructionTable destruction, PracticeUnitCatalog units,PracticeAircraftTables modes,byte[] visuals,byte[] terrain,byte[] auxiliary, byte[] secondaryObjects,int selectedFleet,int selectedWaypoint,PracticeFleetScheduler schedule)
        {
            if(orders==null||destruction==null||units==null||modes==null||schedule==null)throw new ArgumentNullException("aircraft world tables/state");
            if(!orders.Combat.IsAircraftStudy||orders.Combat.IncludesFiringOrders)throw new NotSupportedException("The shared aircraft clock requires the aircraft secondary-order profile.");
            var fleets=orders.Combat.Objects.CopyFleetPool();
            if(selectedFleet<0||selectedFleet>=250)throw new ArgumentOutOfRangeException("selectedFleet");
            int at=selectedFleet*492;
            if(BitConverter.ToInt32(fleets,at)!=1||BitConverter.ToInt32(fleets,at+472)!=0||BitConverter.ToInt32(fleets,at+476)!=0||(fleets[at+485]!=2&&fleets[at+485]!=3))throw new NotSupportedException("The shared aircraft clock requires an admitted selected surface fleet.");
            new PracticeMovementState(fleets,terrain,auxiliary,selectedFleet,selectedWaypoint);
            if(secondaryObjects==null||secondaryObjects.Length!=61500)throw new InvalidDataException("The complete secondary-object pool is required.");
            for(int slot=0;slot<125;slot++)if(BitConverter.ToInt32(secondaryObjects,slot*492)!=0)throw new NotSupportedException("Active secondary objects remain outside the aircraft world study.");
            new PracticeVisualPool(orders.Combat.CopyEffectPool(),visuals);
            Orders=orders.WithCombatForStudy(orders.Combat.WithStoredAircraftImpacts());
            this.destruction=destruction;
            this.units=units;
            this.modes=modes;
            this.visuals=(byte[])visuals.Clone();
            this.terrain=(byte[])terrain.Clone();
            this.auxiliary=(byte[])auxiliary.Clone();
            this.secondaryObjects=(byte[])secondaryObjects.Clone();
            SelectedFleet=selectedFleet;
            SelectedWaypoint=selectedWaypoint;
            Schedule=schedule;
        }
        PracticeAircraftWorldStudyState Copy(PracticeAircraftOrderStudyState orders,byte[] aux,int point,PracticeFleetScheduler schedule)
        {
            var next=new PracticeAircraftWorldStudyState(orders,destruction,units,modes,visuals,terrain,aux,secondaryObjects,SelectedFleet,point,schedule);
            next.AircraftVisits=AircraftVisits;
            next.SurfaceVisits=SurfaceVisits;
            next.Invalidations=Invalidations;
            return next;
        }
        public byte[] CopyAuxiliary()
        {
            return (byte[])auxiliary.Clone();
        }
        public byte[] CopyTerrainPool()
        {
            return (byte[])terrain.Clone();
        }
        public byte[] CopySecondaryObjects()
        {
            return (byte[])secondaryObjects.Clone();
        }
        public PracticeAircraftWorldStudyState Queue(int source,int target,int weapon,int x,int y)
        {
            return Copy(Orders.Queue(source,target,weapon,x,y),auxiliary,SelectedWaypoint,Schedule);
        }
        public PracticeAircraftWorldStudyState Clear(int source)
        {
            return Copy(Orders.Clear(source),auxiliary,SelectedWaypoint,Schedule);
        }
        public PracticeAircraftWorldStudyState ProduceRequests()
        {
            return Copy(Orders.ProduceRequests(),auxiliary,SelectedWaypoint,Schedule);
        }
        internal PracticeAircraftWorldStudyState WithCombatForStudy(PracticeTimedStudyState combat)
        {
            return Copy(Orders.WithCombatForStudy(combat),auxiliary,SelectedWaypoint,Schedule);
        }
        public PracticeAircraftWorldStudyState AdvanceFrame(bool paused)
        {
            return AdvanceCore(paused,false);
        }
        internal PracticeAircraftWorldStudyState AdvanceWithRequestProducer(bool paused,bool produceRequests)
        {
            return AdvanceCore(paused,produceRequests);
        }
        static void RequireInactiveSupport(byte[] fleets)
        {
            for(int slot=0;slot<250;slot++)if(BitConverter.ToInt32(fleets,slot*492)==2)
                throw new NotSupportedException("The recovered request phase currently requires no kind2 fleets: its preceding support helper has active supply branches outside this study.");
        }
        internal void CheckRequestPhaseProfile()
        {
            RequireInactiveSupport(Combat.Objects.CopyFleetPool());
        }
        PracticeAircraftWorldStudyState AdvanceCore(bool paused,bool produceRequests)
        {
            if(paused)return Copy(Orders.AdvanceFrame(true),auxiliary,SelectedWaypoint,Schedule);
            var combat=Combat;
            var aux=auxiliary;
            var schedule=Schedule;
            int point=SelectedWaypoint,air=0,surface=0,invalidations=0;
            for(int call=0;call<2;call++)
            {
                var result=PracticeMixedFleetBatch.AdvanceWithSelectedSurface(destruction,visuals,units,modes,combat.Objects.CopyShipPool(),combat.Objects.CopyFleetPool(),combat.CopyScoreRegion(),combat.CopyEffectPool(),combat.PauseTimers.CopyShipTimers(),aux,terrain,secondaryObjects,schedule,SelectedFleet,point,combat.MovieCounter,combat.RandomState);
                combat=combat.WithMixedFleetBatch(result);
                aux=result.CopyAuxiliary();
                point=result.SelectedWaypoint;
                schedule=result.NextSchedule;
                air+=result.AircraftUpdates.Count;
                surface+=result.SurfaceUpdates.Count;
                invalidations+=result.Invalidations;
            }
            // Publish only after movement, launch, impacts and effects all succeed.
            var orders=Orders.WithCombatForStudy(combat);
            if(produceRequests)
            {
                // Original410E62 calls4297F0 first. It is a whole verified
                // no-op in this no-kind2 profile; its active supply path is
                // deliberately guarded rather than silently omitted.
                RequireInactiveSupport(combat.Objects.CopyFleetPool());
                orders=orders.ProduceRequests();
            }
            var next=Copy(orders.AdvanceFrame(false),aux,point,schedule);
            next.LastAircraftVisits=air;
            next.LastSurfaceVisits=surface;
            next.AircraftVisits+=air;
            next.SurfaceVisits+=surface;
            next.Invalidations+=invalidations;
            return next;
        }
    }
}
