using System;
using System.IO;
using System.Drawing;
namespace FleetLab
{
    public enum PracticeAircraftReturnOutcome
    {
        Moved,ReachedLockedHome,Docked,FleetRetired
    }
    public sealed class PracticeAircraftReturnBatchResult
    {
        readonly byte[] primary,auxiliary;
        public PracticeAircraftResourceBatchResult Batch
        {
            get;
            private set;
        }
        public PracticeAircraftReturnOutcome Outcome
        {
            get;
            private set;
        }
        public Point Before
        {
            get;
            private set;
        }
        public Point After
        {
            get;
            private set;
        }
        public Point Home
        {
            get;
            private set;
        }
        public bool StartedReturn
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
        internal PracticeAircraftReturnBatchResult(PracticeAircraftResourceBatchResult prefix,byte[] ships,byte[] fleets,byte[] orders,byte[] aux,PracticeAircraftReturnOutcome outcome,Point before,Point after,Point home,bool started,int point,int invalidations)
        {
            Batch=new PracticeAircraftResourceBatchResult(ships,fleets,prefix.CopyScoreRegion(),prefix.CopyEffectPool(),prefix.NextSchedule,prefix.AircraftMembers,prefix.RetiredUnits,prefix.MovieCounter,prefix.RandomState);
            primary=orders;
            auxiliary=aux;
            Outcome=outcome;
            Before=before;
            After=after;
            Home=home;
            StartedReturn=started;
            SelectedWaypoint=point;
            Invalidations=invalidations;
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
    // Pinned B9A8...6A569 whole427670 resource prefix,427C51 home refresh,
    //427CC7 movement and428202->428550 docking. Explicit selected-other249,
    //offscreen owner-2/network0/playercount0. No home search,AI or caller timing.
    public static class PracticeAircraftReturnBatch
    {
        static int W(byte[] b,int at)
        {
            return BitConverter.ToInt16(b,at);
        }
        static int I(byte[] b,int at)
        {
            return BitConverter.ToInt32(b,at);
        }
        static void W(byte[] b,int at,int value)
        {
            BitConverter.GetBytes((short)value).CopyTo(b,at);
        }
        static void I(byte[] b,int at,int value)
        {
            BitConverter.GetBytes(value).CopyTo(b,at);
        }
        public static PracticeAircraftReturnBatchResult AdvanceOffscreen(PracticeAircraftDestructionTable tables,byte[] visuals,PracticeUnitCatalog units,PracticeAircraftTables modes,byte[] ships,byte[] fleets,byte[] scores,byte[] effects,byte[] primary,byte[] auxiliary,PracticeFleetScheduler schedule,int actor,int point,int movie,uint random)
        {
            return AdvanceCore(tables,visuals,units,modes,ships,fleets,scores,effects,primary,auxiliary,schedule,actor,point,movie,random,false).ReturnUpdate;
        }
        internal static PracticeAircraftFlightBatchResult AdvanceIncludingWaypoints(PracticeAircraftDestructionTable tables,byte[] visuals,PracticeUnitCatalog units,PracticeAircraftTables modes,byte[] ships,byte[] fleets,byte[] scores,byte[] effects,byte[] primary,byte[] auxiliary,PracticeFleetScheduler schedule,int actor,int point,int movie,uint random,bool allowEmptyAwacs=false,bool searchLandingCandidates=false,bool singleActor=false)
        {
            return AdvanceCore(tables,visuals,units,modes,ships,fleets,scores,effects,primary,auxiliary,schedule,actor,point,movie,random,true,allowEmptyAwacs,searchLandingCandidates,singleActor);
        }
        static PracticeAircraftFlightBatchResult AdvanceCore(PracticeAircraftDestructionTable tables,byte[] visuals,PracticeUnitCatalog units,PracticeAircraftTables modes,byte[] ships,byte[] fleets,byte[] scores,byte[] effects,byte[] primary,byte[] auxiliary,PracticeFleetScheduler schedule,int actor,int point,int movie,uint random,bool allowOrdinary,bool allowEmptyAwacs=false,bool searchLandingCandidates=false,bool singleActor=false)
        {
            if(schedule==null)throw new ArgumentNullException("schedule");
            if(ships==null||ships.Length!=402000||fleets==null||fleets.Length!=123000||auxiliary==null||auxiliary.Length!=78336)throw new InvalidDataException("Returning aircraft require complete ship/fleet/auxiliary pools.");
            if(actor<0||actor>=249)throw new ArgumentOutOfRangeException("actor");
            if(point< -1||point>4)throw new ArgumentOutOfRangeException("point");
            if(W(fleets,249*492+4)!=249)throw new NotSupportedException("Return profile requires selected-other identity249; batch preserved.");
            bool visited=false;
            foreach(int slot in schedule.TakeBatch().CopySlots())
            {
                if(slot==actor)visited=true;
                else if(!singleActor&&!PracticeAircraftResourceBatch.IsRecordedEarlySkip(fleets,slot*492))throw new NotSupportedException("Other visited fleets must take recorded early skips; batch preserved.");
            }
            if(!visited)throw new NotSupportedException("The source batch does not visit the requested returning aircraft; batch preserved.");
            int at=actor*492,kind=I(fleets,at),state=I(fleets,at+472),task=I(fleets,at+476),speed=W(fleets,at+60);
            if(kind<3||kind>5||W(fleets,at+4)!=actor||(state!=0&&state!=1&&!(allowEmptyAwacs&&kind==5&&state==2&&task==2))||(task!=1&&task!=2&&task!=4)||fleets[at+485]!=0||speed<0||speed>13)throw new NotSupportedException("Aircraft flight requires the admitted kind3..5, return/task/flag and source speed0..13 profile; batch preserved.");
            if(state==2&&W(fleets,at+40)!=-1)throw new NotSupportedException("The no-home idle continuation requires an empty route; batch preserved.");
            var before=new Point(W(fleets,at+12),W(fleets,at+14));
            int parent=W(fleets,at+8);
            if(parent<0||parent>=1500)throw new InvalidDataException("Return association crosses the unit pool.");
            int homeFleet=W(ships,parent*268+12);
            if(homeFleet<0||homeFleet>=250)throw new InvalidDataException("Return association crosses the fleet pool.");
            var home=new Point(W(fleets,homeFleet*492+12),W(fleets,homeFleet*492+14));
            if(before.X<0||before.X>=640||before.Y<0||before.Y>=640||home.X<0||home.X>=640||home.Y<0||home.Y>=640)throw new NotSupportedException("Return origin/home must be in world0..639; batch preserved.");
            var prepared=PracticeAircraftResourceBatch.ApplyBeforeReturn(tables,visuals,units,modes,ships,fleets,scores,effects,primary,schedule,movie,random,actor,point,allowEmptyAwacs,searchLandingCandidates,singleActor);
            var prefix=prepared.Batch;
            var s=prefix.CopyShipPool();
            var f=prefix.CopyFleetPool();
            var p=prepared.CopyPrimaryOrders();
            var aux=(byte[])auxiliary.Clone();
            int selected=prepared.SelectedWaypoint;
            if(I(f,at)==0)return new PracticeAircraftFlightBatchResult(new PracticeAircraftReturnBatchResult(prefix,s,f,p,aux,PracticeAircraftReturnOutcome.FleetRetired,before,before,home,false,selected,prepared.Invalidations),prepared.SearchResult);
            if(allowOrdinary&&(I(f,at+472)==0||(allowEmptyAwacs&&kind==5&&I(f,at+472)==2)))return new PracticeAircraftFlightBatchResult(PracticeAircraftWaypointBatch.CompleteAfterResources(prefix,auxiliary,actor,selected,before),p,prepared.NoHomeGroups,prepared.Invalidations,prepared.SearchResult);
            if(I(f,at+472)!=1)throw new NotSupportedException("This visit remains ordinary flight; use the waypoint profile instead. Batch preserved.");
            parent=W(f,at+8);
            if(parent<0||parent>=1500)throw new InvalidDataException("Selected home association crosses its unit pool.");
            homeFleet=W(s,parent*268+12);
            if(homeFleet<0||homeFleet>=250)throw new InvalidDataException("Selected home association crosses its fleet pool.");
            home=new Point(W(f,homeFleet*492+12),W(f,homeFleet*492+14));
            if(home.X<0||home.X>=640||home.Y<0||home.Y>=640)throw new NotSupportedException("Selected home is outside world0..639; batch preserved.");
            bool started=state==0;
            W(f,at+40,home.X);
            W(f,at+50,home.Y);
            var after=PracticeAircraftWaypointBatch.MoveToward(f,actor,before,home.X,home.Y);
            var outcome=PracticeAircraftReturnOutcome.Moved;
            if(after==home)
            {
                if(I(aux,actor*312+0x50)==1)outcome=PracticeAircraftReturnOutcome.ReachedLockedHome;
                else
                {
                    I(aux,249*312+0x50,0);
                    aux[249*312+0x3c]=0;
                    var dock=PracticeAircraftTransitions.ApplyOffscreen(modes,units,s,f,p,actor,0,249,selected,-2);
                    s=dock.CopyShipPool();
                    f=dock.CopyFleetPool();
                    p=dock.CopyPrimaryOrders();
                    selected=dock.SelectedWaypoint;
                    outcome=PracticeAircraftReturnOutcome.Docked;
                }
            }
            return new PracticeAircraftFlightBatchResult(new PracticeAircraftReturnBatchResult(prefix,s,f,p,aux,outcome,before,after,home,started,selected,prepared.Invalidations),prepared.SearchResult);
        }
    }
}
