using System;
namespace FleetLab
{
    // One resource prefix, followed by the admitted ordinary or returning body.
    // The alternative results identify which original body produced the update.
    public sealed class PracticeAircraftHomeSearchSummary
    {
        public bool FoundHome
        {
            get;
            private set;
        }
        public int CandidateCount
        {
            get;
            private set;
        }
        public int ParentUnit
        {
            get;
            private set;
        }
        public int Link
        {
            get;
            private set;
        }
        internal PracticeAircraftHomeSearchSummary(PracticeAircraftHomeSearchResult result)
        {
            FoundHome=result.FoundHome;
            CandidateCount=result.CandidateCount;
            ParentUnit=result.ParentUnit;
            Link=result.Link;
        }
    }
    public sealed class PracticeAircraftFlightBatchResult
    {
        readonly byte[] primary,auxiliary;
        public PracticeAircraftHomeSearchSummary HomeSearch
        {
            get;
            private set;
        }
        public int NoHomeGroups
        {
            get;
            private set;
        }
        public int Invalidations
        {
            get;
            private set;
        }
        public PracticeAircraftResourceBatchResult Batch
        {
            get;
            private set;
        }
        public PracticeAircraftWaypointBatchResult WaypointUpdate
        {
            get;
            private set;
        }
        public PracticeAircraftReturnBatchResult ReturnUpdate
        {
            get;
            private set;
        }
        public int SelectedWaypoint
        {
            get;
            private set;
        }
        internal PracticeAircraftFlightBatchResult(PracticeAircraftWaypointBatchResult waypoint,byte[] orders,int noHomeGroups=0,int invalidations=0,PracticeAircraftHomeSearchResult homeSearch=null)
        {
            HomeSearch=homeSearch==null?null:new PracticeAircraftHomeSearchSummary(homeSearch);
            NoHomeGroups=noHomeGroups;
            Invalidations=invalidations;
            WaypointUpdate=waypoint;
            Batch=waypoint.Batch;
            SelectedWaypoint=waypoint.SelectedWaypoint;
            primary=(byte[])orders.Clone();
            auxiliary=waypoint.CopyAuxiliary();
        }
        internal PracticeAircraftFlightBatchResult(PracticeAircraftReturnBatchResult returning,PracticeAircraftHomeSearchResult homeSearch=null)
        {
            HomeSearch=homeSearch==null?null:new PracticeAircraftHomeSearchSummary(homeSearch);
            Invalidations=returning.Invalidations;
            ReturnUpdate=returning;
            Batch=returning.Batch;
            SelectedWaypoint=returning.SelectedWaypoint;
            primary=returning.CopyPrimaryOrders();
            auxiliary=returning.CopyAuxiliary();
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
    public static class PracticeAircraftFlightBatch
    {
        // Explicit opt-in: AWACS home fallback requires the full candidate search
        // to be empty. Candidate-bearing searches remain guarded atomically.
        public static PracticeAircraftFlightBatchResult AdvanceWithEmptyHomeFallback(PracticeAircraftDestructionTable tables,byte[] visuals,PracticeUnitCatalog units,PracticeAircraftTables modes,byte[] ships,byte[] fleets,byte[] scores,byte[] effects,byte[] primary,byte[] auxiliary,PracticeFleetScheduler schedule,int actor,int point,int movie,uint random)
        {
            return PracticeAircraftReturnBatch.AdvanceIncludingWaypoints(tables,visuals,units,modes,ships,fleets,scores,effects,primary,auxiliary,schedule,actor,point,movie,random,true);
        }
        public static PracticeAircraftFlightBatchResult AdvanceWithHomeSearch(PracticeAircraftDestructionTable tables,byte[] visuals,PracticeUnitCatalog units,PracticeAircraftTables modes,byte[] ships,byte[] fleets,byte[] scores,byte[] effects,byte[] primary,byte[] auxiliary,PracticeFleetScheduler schedule,int actor,int point,int movie,uint random)
        {
            return PracticeAircraftReturnBatch.AdvanceIncludingWaypoints(tables,visuals,units,modes,ships,fleets,scores,effects,primary,auxiliary,schedule,actor,point,movie,random,true,true);
        }
        public static PracticeAircraftFlightBatchResult AdvanceOffscreen(PracticeAircraftDestructionTable tables,byte[] visuals,PracticeUnitCatalog units,PracticeAircraftTables modes,byte[] ships,byte[] fleets,byte[] scores,byte[] effects,byte[] primary,byte[] auxiliary,PracticeFleetScheduler schedule,int actor,int point,int movie,uint random)
        {
            return PracticeAircraftReturnBatch.AdvanceIncludingWaypoints(tables,visuals,units,modes,ships,fleets,scores,effects,primary,auxiliary,schedule,actor,point,movie,random);
        }
    }
}
