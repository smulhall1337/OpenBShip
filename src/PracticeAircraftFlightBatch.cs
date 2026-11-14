using System;
namespace FleetLab
{
    // One resource prefix, followed by the admitted ordinary or returning body.
    // The alternative results identify which original body produced the update.
    public sealed class PracticeAircraftFlightBatchResult
    {
        readonly byte[] primary,auxiliary;
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
        internal PracticeAircraftFlightBatchResult(PracticeAircraftWaypointBatchResult waypoint,byte[] orders)
        {
            WaypointUpdate=waypoint;
            Batch=waypoint.Batch;
            SelectedWaypoint=waypoint.SelectedWaypoint;
            primary=(byte[])orders.Clone();
            auxiliary=waypoint.CopyAuxiliary();
        }
        internal PracticeAircraftFlightBatchResult(PracticeAircraftReturnBatchResult returning)
        {
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
        public static PracticeAircraftFlightBatchResult AdvanceOffscreen(PracticeAircraftDestructionTable tables,byte[] visuals,PracticeUnitCatalog units,PracticeAircraftTables modes,byte[] ships,byte[] fleets,byte[] scores,byte[] effects,byte[] primary,byte[] auxiliary,PracticeFleetScheduler schedule,int actor,int point,int movie,uint random)
        {
            return PracticeAircraftReturnBatch.AdvanceIncludingWaypoints(tables,visuals,units,modes,ships,fleets,scores,effects,primary,auxiliary,schedule,actor,point,movie,random);
        }
    }
}
