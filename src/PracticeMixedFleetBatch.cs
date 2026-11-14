using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace FleetLab
{
    public sealed class PracticeMixedAircraftUpdate
    {
        public int Slot
        {
            get;
            private set;
        }
        public PracticeAircraftFlightBatchResult Flight
        {
            get;
            private set;
        }
        internal PracticeMixedAircraftUpdate(int slot,PracticeAircraftFlightBatchResult flight)
        {
            Slot=slot;
            Flight=flight;
        }
    }
    public sealed class PracticeMixedFleetBatchResult
    {
        readonly byte[] ships,fleets,scores,effects,primary,auxiliary;
        public PracticeFleetScheduler NextSchedule
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
        public int Invalidations
        {
            get;
            private set;
        }
        public ReadOnlyCollection<PracticeFleetBodyUpdate> SurfaceUpdates
        {
            get;
            private set;
        }
        public ReadOnlyCollection<PracticeMixedAircraftUpdate> AircraftUpdates
        {
            get;
            private set;
        }
        internal PracticeMixedFleetBatchResult(byte[] s,byte[] f,byte[] score,byte[] effect,byte[] p,byte[] aux,PracticeFleetScheduler next,int point,int movie,uint random,int invalidations,List<PracticeFleetBodyUpdate> surface,List<PracticeMixedAircraftUpdate> aircraft)
        {
            ships=s;
            fleets=f;
            scores=score;
            effects=effect;
            primary=p;
            auxiliary=aux;
            NextSchedule=next;
            SelectedWaypoint=point;
            MovieCounter=movie;
            RandomState=random;
            Invalidations=invalidations;
            SurfaceUpdates=surface.AsReadOnly();
            AircraftUpdates=aircraft.AsReadOnly();
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
        public byte[] CopyPrimaryOrders()
        {
            return (byte[])primary.Clone();
        }
        public byte[] CopyAuxiliary()
        {
            return (byte[])auxiliary.Clone();
        }
    }
    // Pinned RIP B9A8...6A569: one ordered four-slot 427670 batch. Compose
    // the admitted surface and aircraft bodies without a second resource pass.
    // Offscreen owner-2, selected-other249, network/playercount0, scenario1;
    // inactive secondary objects and the existing body guards remain required.
    // These supplied batches do not establish application timing, AI or missions.
    public static class PracticeMixedFleetBatch
    {
        public static PracticeMixedFleetBatchResult AdvanceOffscreen(PracticeAircraftDestructionTable tables,byte[] visuals,PracticeUnitCatalog units,PracticeAircraftTables modes,byte[] shipPool,byte[] fleetPool,byte[] scoreRegion,byte[] effectPool,byte[] primaryOrders,byte[] auxiliaryData,byte[] terrainPool,byte[] secondaryPool,PracticeFleetScheduler schedule,int selectedWaypoint,int movieCounter,uint randomState)
        {
            if(tables==null||units==null||modes==null||schedule==null)throw new ArgumentNullException("mixed fleet batch tables/schedule");
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000||scoreRegion==null||scoreRegion.Length!=176||effectPool==null||effectPool.Length!=800||primaryOrders==null||primaryOrders.Length!=78000)throw new InvalidDataException("Mixed batches require complete ship, fleet, score, effect and primary pools.");
            if(secondaryPool==null||secondaryPool.Length!=PracticeFleetBatchState.SecondaryBytes)throw new InvalidDataException("Mixed batches require the complete secondary pool.");
            for(int slot=0;slot<125;slot++)if(BitConverter.ToInt32(secondaryPool,slot*492)!=0)throw new NotSupportedException("Active secondary objects remain outside this mixed batch; caller pools preserved.");
            if(BitConverter.ToInt16(scoreRegion,172)!=-2||movieCounter<short.MinValue||movieCounter>short.MaxValue)throw new NotSupportedException("Mixed batches require the recorded offscreen owner context and signed16 movie counter.");
            new PracticeMovementState(fleetPool,terrainPool,auxiliaryData,249,selectedWaypoint);
            new PracticeVisualPool(effectPool,visuals);
            var s=(byte[])shipPool.Clone();
            var f=(byte[])fleetPool.Clone();
            var score=(byte[])scoreRegion.Clone();
            var effect=(byte[])effectPool.Clone();
            var p=(byte[])primaryOrders.Clone();
            var aux=(byte[])auxiliaryData.Clone();
            var plan=schedule.TakeBatch();
            var surface=new List<PracticeFleetBodyUpdate>();
            var aircraft=new List<PracticeMixedAircraftUpdate>();
            int point=selectedWaypoint,movie=movieCounter,invalidations=0;
            uint random=randomState;
            foreach(int slot in plan.CopySlots())
            {
                int at=slot*492,kind=BitConverter.ToInt32(f,at);
                if(kind==1)
                {
                    var moved=new PracticeMovementState(f,terrainPool,aux,249,point).Step(slot);
                    f=moved.State.CopyFleetPool();
                    aux=moved.State.CopyAuxiliary();
                    point=moved.State.SelectedWaypoint;
                    surface.Add(new PracticeFleetBodyUpdate(slot,moved));
                }
                else if(kind>=3&&kind<=5)
                {
                    var flight=PracticeAircraftReturnBatch.AdvanceIncludingWaypoints(tables,visuals,units,modes,s,f,score,effect,p,aux,schedule,slot,point,movie,random,true,true,true);
                    s=flight.Batch.CopyShipPool();
                    f=flight.Batch.CopyFleetPool();
                    score=flight.Batch.CopyScoreRegion();
                    effect=flight.Batch.CopyEffectPool();
                    p=flight.CopyPrimaryOrders();
                    aux=flight.CopyAuxiliary();
                    point=flight.SelectedWaypoint;
                    movie=flight.Batch.MovieCounter;
                    random=flight.Batch.RandomState;
                    invalidations+=flight.Invalidations;
                    aircraft.Add(new PracticeMixedAircraftUpdate(slot,flight));
                }
                else if(PracticeAircraftResourceBatch.IsRecordedEarlySkip(f,at))
                {
                    f[at+482]=255;
                }
                else throw new NotSupportedException("Fleet "+slot+" is outside the admitted mixed batch bodies; caller pools preserved.");
            }
            return new PracticeMixedFleetBatchResult(s,f,score,effect,p,aux,plan.Next,point,movie,random,invalidations,surface,aircraft);
        }
    }
}
