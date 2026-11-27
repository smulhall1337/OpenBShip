using System;using System.Drawing;using System.Collections.Generic;using System.Collections.ObjectModel;
namespace FleetLab
{
    // Bounded viewing of original864/875/884 histories. Poll clocks/phase
    // are supplied inputs; this is not a natural application clock or mission.
    public sealed class PracticeNavigationMovementStudyState
    {
        public const int MaximumPolls=1024;
        static readonly string[] names={"Open route · speed 3 / flag 2 · clock held","Open route · speed 7 / flag 3 · +40 ms",
            "Nonmultiple input · 9,19 to 29,39","Already at destination · 320,320","Blocked destination · primary ring",
            "Long open route · 1024 polls","Gap in primary wall · 1024 polls","Closed primary wall · 1024 polls",
            "Gap in terrain wall · 1024 polls","Terrain inserted before poll 65 · held","Terrain inserted 65 / cleared 193","Fleet obstacle inserted before poll 65",
            "Repeat callback · blocked current point · 1 poll","Repeat callback · future cooldown · 1 poll"};
        readonly PracticeGoalRouteStudyBasis basis;readonly Point[] trail;
        public static ReadOnlyCollection<string> Presets {get{return Array.AsReadOnly(names);}}
        public int Preset {get;private set;}public int Polls {get;private set;}
        public int ActorVisits {get;private set;}public int PositionChanges {get;private set;}public int QueueConsumptions {get;private set;}
        public int NavigationCompletions {get;private set;}public int DormantPrephases {get;private set;}
        public int ReplanEntries {get;private set;}public int TerrainCollisionVisits {get;private set;}public int FleetCollisionVisits {get;private set;}public int SuppliedWorldEvents {get;private set;}
        public int WaypointFunctionVisits {get;private set;}public int RepeatedWaypointVisits {get;private set;}public int GridFunctionVisits {get;private set;}
        public PracticeNavigationMovementState State {get;private set;}
        public PracticeNavigationMovementUpdate LastUpdate {get;private set;}
        public int ClockStep {get{return Preset==0?0:40;}}
        public int LastSourceClock {get{return LastUpdate==null?12345:LastUpdate.SourceClock;}}
        public int NextSourceClock {get{return 12345+Polls*ClockStep;}}
        public int WindowPolls {get{return Preset>=12?1:Preset<5?256:MaximumPolls;}}
        public bool CanAdvance {get{return Polls<WindowPolls;}}
        public bool HasGridOutput {get{return Polls>0;}}
        public Point Position {get{var f=State.CopyFleetPool();return new Point(BitConverter.ToInt16(f,16*492+12),BitConverter.ToInt16(f,16*492+14));}}
        public PracticeNavigationMovementStudyState(PracticeGoalRouteStudyBasis basis,int preset)
        {
            if(basis==null)throw new ArgumentNullException("basis");if(preset<0||preset>=names.Length)throw new ArgumentOutOfRangeException("preset");
            this.basis=basis;Preset=preset;State=basis.CreateMovementComparison(preset);trail=new[]{Position};
        }
        PracticeNavigationMovementStudyState(PracticeNavigationMovementStudyState original,PracticeNavigationMovementState state,
            PracticeNavigationMovementUpdate last,int polls,int visits,int changes,int consumed,int completed,int dormant,int replans,int terrainHits,int fleetHits,int worldEvents,int waypointCalls,int repeatedCalls,int gridCalls,Point[] points)
        {
            basis=original.basis;Preset=original.Preset;State=state;LastUpdate=last;Polls=polls;ActorVisits=visits;PositionChanges=changes;
            QueueConsumptions=consumed;NavigationCompletions=completed;DormantPrephases=dormant;trail=points;
            ReplanEntries=replans;TerrainCollisionVisits=terrainHits;FleetCollisionVisits=fleetHits;SuppliedWorldEvents=worldEvents;
            WaypointFunctionVisits=waypointCalls;RepeatedWaypointVisits=repeatedCalls;GridFunctionVisits=gridCalls;
        }
        public Point[] CopyTrail(){return(Point[])trail.Clone();}
        public PracticeNavigationMovementStudyState Advance(int requestedPolls)
        {
            if(requestedPolls<1||requestedPolls>MaximumPolls)throw new ArgumentOutOfRangeException("requestedPolls");
            if(!CanAdvance)throw new InvalidOperationException("The recorded viewing window is complete;reset before another comparison.");
            int count=Math.Min(requestedPolls,WindowPolls-Polls),visits=ActorVisits,changes=PositionChanges,consumed=QueueConsumptions,completed=NavigationCompletions,dormant=DormantPrephases;
            int replans=ReplanEntries,terrainHits=TerrainCollisionVisits,fleetHits=FleetCollisionVisits,worldEvents=SuppliedWorldEvents;
            int waypointCalls=WaypointFunctionVisits,repeatedCalls=RepeatedWaypointVisits,gridCalls=GridFunctionVisits;
            var state=State;var last=LastUpdate;var points=new List<Point>(trail);
            for(int n=0;n<count;n++)
            {
                int poll=Polls+n;if(PracticeGoalRouteStudyBasis.MovementWorldEvent(Preset,poll)!=0)worldEvents++;
                state=PracticeGoalRouteStudyBasis.ApplyMovementWorldEvent(state,Preset,poll);
                last=state.AdvanceOfflinePoll(12345+poll*ClockStep);
                if(last.Navigation==null)dormant++;else if(last.Navigation.Outcome==PracticeQueuedNavigationOutcome.CompletedGoal)completed++;
                if(last.Navigation!=null&&last.Navigation.Outcome==PracticeQueuedNavigationOutcome.Replanned)replans++;
                if(last.Navigation!=null){waypointCalls+=last.Navigation.WaypointFunctionVisits;repeatedCalls+=last.Navigation.SecondWaypoint==null?0:1;gridCalls+=last.Navigation.GridFunctionVisits;}
                foreach(var visit in last.Movement.FleetUpdates)
                {
                    visits++;if(visit.Update.Before!=visit.Update.After){changes++;points.Add(visit.Update.After);}
                    if(visit.Update.QueueConsumed)consumed++;
                    if(visit.Update.Outcome==PracticeMovementOutcome.BlockedByTerrain)terrainHits++;
                    if(visit.Update.Outcome==PracticeMovementOutcome.BlockedByFleet)fleetHits++;
                }
                state=last.State;
            }
            return new PracticeNavigationMovementStudyState(this,state,last,Polls+count,visits,changes,consumed,completed,dormant,replans,terrainHits,fleetHits,worldEvents,waypointCalls,repeatedCalls,gridCalls,points.ToArray());
        }
        public PracticeNavigationMovementStudyState AdvanceUntilPositionChanges()
        {
            if(!CanAdvance)throw new InvalidOperationException("Reset before another bounded comparison.");
            var next=this;var before=Position;int completed=NavigationCompletions;
            for(int n=0;n<64&&next.CanAdvance;n++)
            {
                next=next.Advance(1);if(next.Position!=before||next.NavigationCompletions>completed)break;
            }
            return next;
        }
        public PracticeNavigationMovementStudyState AdvanceUntilCollisionOrReplan()
        {
            if(!CanAdvance)throw new InvalidOperationException("Reset before another bounded comparison.");
            var next=this;int hits=TerrainCollisionVisits+FleetCollisionVisits,replans=ReplanEntries;
            for(int n=0;n<256&&next.CanAdvance;n++)
            {
                next=next.Advance(1);if(next.TerrainCollisionVisits+next.FleetCollisionVisits>hits||next.ReplanEntries>replans)break;
            }
            return next;
        }
        public PracticeNavigationMovementStudyState Reset(){return new PracticeNavigationMovementStudyState(basis,Preset);}
    }
}
