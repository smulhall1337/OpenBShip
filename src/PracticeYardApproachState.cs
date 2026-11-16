using System;
using System.Drawing;
namespace FleetLab
{
    // Deliberate composition of recovered movement/collision and admission.
    // Nearby placement/waypoint are supplied diagnostics. Only one fleet moves;
    // no world poll, AI, command UI or complete original approach is claimed.
    public sealed class PracticeYardApproachState
    {
        readonly PracticeMapYardState prepared;
        readonly PracticeMovementState movement;
        public int TargetUnit { get { return prepared.TargetUnit; } }
        public int Profile { get { return prepared.Profile; } }
        public int Fleet { get { return prepared.OriginFleet; } }
        public int Visits { get; private set; }
        public Point Position { get { return movement.Origin(Fleet); } }
        public int Collision { get { return unchecked((sbyte)movement.CopyFleetRecord(Fleet)[482]); } }
        public PracticeMovementOutcome? LastOutcome { get; private set; }
        public int TerrainCount { get { return 53; } }
        public const int MaximumVisits=20;
        public static readonly Point SuppliedStart=new Point(622,600),SuppliedWaypoint=new Point(610,600);
        PracticeYardApproachState(PracticeMapYardState prepared,PracticeMovementState movement,int visits,PracticeMovementOutcome? outcome)
        {this.prepared=prepared;this.movement=movement;Visits=visits;LastOutcome=outcome;}
        static void W(byte[] b,int p,int value){BitConverter.GetBytes((short)value).CopyTo(b,p);}
        public static PracticeYardApproachState Create(string programs,PracticeMapStartup startup,int target,int profile)
        {
            var prepared=PracticeMapYardState.Create(programs,startup,target,profile);
            var fleets=prepared.CopyFleetPool();int at=prepared.OriginFleet*492;
            // Reuse the supplied target damage/ammo preparation, then remove
            // its explicit yard command before any approach state is published.
            fleets[at+482]=255;
            W(fleets,at+12,SuppliedStart.X);W(fleets,at+14,SuppliedStart.Y);
            W(fleets,at+40,SuppliedWaypoint.X);W(fleets,at+50,SuppliedWaypoint.Y);
            var movement=new PracticeMovementState(fleets,startup.CopyTerrainPool(),new byte[PracticeMovementState.AuxiliaryBytes],prepared.OriginFleet,0);
            return new PracticeYardApproachState(prepared,movement,0,null);
        }
        public byte[] CopyShipPool(){return prepared.CopyShipPool();}
        public byte[] CopyFleetPool(){return movement.CopyFleetPool();}
        public byte[] CopyTerrainPool(){return movement.CopyTerrainPool();}
        public PracticeYardAdmissionResult CheckAdmission(){return PracticeYardAdmission.Check(prepared.CopyShipPool(),movement.CopyFleetPool(),TargetUnit,PracticeMapYardState.LocalOwner);}
        public PracticeYardApproachState Step()
        {
            if(CheckAdmission().Available)throw new InvalidOperationException("An admitted yard is ready; open its copy or reset before more movement.");
            if(Visits>=MaximumVisits)throw new InvalidOperationException("The bounded approach reached twenty visits; reset this copy.");
            var update=movement.Step(Fleet);
            return new PracticeYardApproachState(prepared,update.State,Visits+1,update.Outcome);
        }
        public PracticeYardApproachState UntilAdmission()
        {
            var state=this;
            while(state.Visits<MaximumVisits&&!state.CheckAdmission().Available)state=state.Step();
            return state;
        }
        public PracticeMapYardState CreateYardCopy(){return prepared.WithApproach(movement);}
    }
}
