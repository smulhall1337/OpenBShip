using System;using System.Drawing;using System.Collections.Generic;using System.Collections.ObjectModel;
namespace FleetLab
{
    // Bounded original histories. Earlier profiles supply phase; the appended
    // phase profiles derive it from copied pause words. Samples remain supplied;
    // this is not a natural application clock or a complete mission/world.
    public sealed class PracticeNavigationMovementStudyState
    {
        public const int MaximumPolls=1024;
        static readonly string[] names={"Open route · speed 3 / flag 2 · clock held","Open route · speed 7 / flag 3 · +40 ms",
            "Nonmultiple input · 9,19 to 29,39","Already at destination · 320,320","Blocked destination · primary ring",
            "Long open route · 1024 polls","Gap in primary wall · 1024 polls","Closed primary wall · 1024 polls",
            "Gap in terrain wall · 1024 polls","Terrain inserted before poll 65 · held","Terrain inserted 65 / cleared 193","Fleet obstacle inserted before poll 65",
            "Repeat callback · blocked current point · 1 poll","Repeat callback · future cooldown · 1 poll",
            "Clock + neutral driver · terrain gap · 128 frames","Clock + neutral driver · terrain / zero reset","Clock + neutral driver · fleet obstacle",
            "Phase + clock · pause before decision","Phase + clock · terrain / pause","Phase + clock · hold decision phase"};
        readonly PracticeGoalRouteStudyBasis basis;readonly Point[] trail;
        readonly PracticeOwnerVisibilityRules visibility;
        public static ReadOnlyCollection<string> Presets {get{return Array.AsReadOnly(names);}}
        public int Preset {get;private set;}public int Polls {get;private set;}
        public int ActorVisits {get;private set;}public int PositionChanges {get;private set;}public int QueueConsumptions {get;private set;}
        public int NavigationCompletions {get;private set;}public int DormantPrephases {get;private set;}
        public int ReplanEntries {get;private set;}public int TerrainCollisionVisits {get;private set;}public int FleetCollisionVisits {get;private set;}public int SuppliedWorldEvents {get;private set;}
        public int WaypointFunctionVisits {get;private set;}public int RepeatedWaypointVisits {get;private set;}public int GridFunctionVisits {get;private set;}
        public PracticeNavigationMovementState State {get;private set;}
        public PracticeNavigationMovementUpdate LastUpdate {get;private set;}
        public OriginalFrameClock Clock {get;private set;}
        public PracticeNavigationClockUpdate LastClockUpdate {get;private set;}
        public int NeutralPasses {get;private set;}
        public OriginalPracticePhase Phase {get;private set;}
        public PracticeNavigationPhaseUpdate LastPhaseUpdate {get;private set;}
        public int PausedFrames {get;private set;}
        public int DriverVisits {get;private set;}
        public bool UsesSourcePhase {get{return Preset>=17;}}
        public bool UsesRecoveredClock {get{return Preset>=14;}}
        public bool IsRepeatedCallback {get{return Preset>=12&&Preset<14;}}
        public int SourcePreset {get{return UsesRecoveredClock?((Preset-14)%3==0?8:(Preset-14)%3==1?9:11):Preset;}}
        public int ClockStep {get{return Preset==0?0:40;}}
        public int LastSourceClock {get{return LastPhaseUpdate!=null?unchecked((int)LastPhaseUpdate.BodyClock.GameMilliseconds):LastUpdate==null?12345:LastUpdate.SourceClock;}}
        public int NextSourceClock {get{return SampleAt(Polls);}}
        public int WindowPolls {get{return UsesRecoveredClock?128:IsRepeatedCallback?1:Preset<5?256:MaximumPolls;}}
        public bool CanAdvance {get{return Polls<WindowPolls;}}
        public bool HasGridOutput {get{return Polls>0;}}
        public Point Position {get{var f=State.CopyFleetPool();return new Point(BitConverter.ToInt16(f,16*492+12),BitConverter.ToInt16(f,16*492+14));}}
        public PracticeNavigationMovementStudyState(PracticeGoalRouteStudyBasis basis,int preset)
            :this(basis,preset,null) { }
        public PracticeNavigationMovementStudyState(PracticeGoalRouteStudyBasis basis,int preset,PracticeOwnerVisibilityRules visibilityRules)
        {
            if(basis==null)throw new ArgumentNullException("basis");if(preset<0||preset>=names.Length)throw new ArgumentOutOfRangeException("preset");
            this.basis=basis;Preset=preset;visibility=visibilityRules;State=basis.CreateMovementComparison(SourcePreset);
            if(UsesRecoveredClock)
            {
                if(visibility==null)throw new ArgumentNullException("visibilityRules");
                var f=State.CopyFleetPool();f[249*492+467]=3;f[249*492+480]=1;
                State=new PracticeNavigationMovementState(f,State.Backing,State.Grid,State.CopyTerrainPool(),State.CopySecondaryPool(),State.Schedule,4);
                Clock=new OriginalFrameClock(12265,12305,12345,40);
                if(UsesSourcePhase)Phase=new OriginalPracticePhase(0);
            }
            trail=new[]{Position};
        }
        int SampleAt(int poll){return Preset==15&&poll==95?12345+94*40:Preset==18&&poll==98?12345+97*40:12345+poll*ClockStep;}
        int WorldPoll(int poll){return UsesSourcePhase?(poll==68?65:poll==65?64:poll):poll;}
        public short PauseWordAt(int poll)
        {
            if(poll<0||poll>=WindowPolls)throw new ArgumentOutOfRangeException("poll");
            if(!UsesSourcePhase)return 0;bool held=Preset==19?poll>=32&&poll<=34:poll>=30&&poll<=32;
            return held?(Preset==17?(short)1:Preset==18?(short)-1:short.MinValue):(short)0;
        }
        PracticeNavigationMovementStudyState(PracticeNavigationMovementStudyState original,PracticeNavigationMovementState state,
            PracticeNavigationMovementUpdate last,int polls,int visits,int changes,int consumed,int completed,int dormant,int replans,int terrainHits,int fleetHits,int worldEvents,int waypointCalls,int repeatedCalls,int gridCalls,Point[] points,OriginalFrameClock clock=null,PracticeNavigationClockUpdate clockUpdate=null,int neutralPasses=0,OriginalPracticePhase phase=null,PracticeNavigationPhaseUpdate phaseUpdate=null,int pausedFrames=0,int driverVisits=0)
        {
            basis=original.basis;Preset=original.Preset;State=state;LastUpdate=last;Polls=polls;ActorVisits=visits;PositionChanges=changes;
            QueueConsumptions=consumed;NavigationCompletions=completed;DormantPrephases=dormant;trail=points;
            ReplanEntries=replans;TerrainCollisionVisits=terrainHits;FleetCollisionVisits=fleetHits;SuppliedWorldEvents=worldEvents;
            WaypointFunctionVisits=waypointCalls;RepeatedWaypointVisits=repeatedCalls;GridFunctionVisits=gridCalls;
            visibility=original.visibility;Clock=clock;LastClockUpdate=clockUpdate;NeutralPasses=neutralPasses;
            Phase=phase;LastPhaseUpdate=phaseUpdate;PausedFrames=pausedFrames;DriverVisits=driverVisits;
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
            var clock=Clock;var clockUpdate=LastClockUpdate;int neutralPasses=NeutralPasses;
            var phase=Phase;var phaseUpdate=LastPhaseUpdate;int pausedFrames=PausedFrames,driverVisits=DriverVisits;
            for(int n=0;n<count;n++)
            {
                int poll=Polls+n,worldPoll=WorldPoll(poll);if(PracticeGoalRouteStudyBasis.MovementWorldEvent(SourcePreset,worldPoll)!=0)worldEvents++;
                state=PracticeGoalRouteStudyBasis.ApplyMovementWorldEvent(state,SourcePreset,worldPoll);
                if(UsesSourcePhase)
                {
                    var joined=new PracticeNavigationPhaseState(state,clock,phase,visibility);
                    phaseUpdate=joined.AdvanceFrame((uint)SampleAt(poll),PauseWordAt(poll),0x13579bdf,0x2468ace0);
                    clock=phaseUpdate.State.Clock;phase=phaseUpdate.State.Phase;state=phaseUpdate.State.Movement;
                    if(!phaseUpdate.SimulationAdvanced){pausedFrames++;continue;}
                    last=phaseUpdate.Driver;driverVisits++;neutralPasses+=last.OwnerVisibilityPasses;
                }
                else if(UsesRecoveredClock)
                {
                    var joined=new PracticeNavigationClockState(state,clock,visibility);
                    clockUpdate=joined.AdvanceFrame((uint)SampleAt(poll),new PracticeAiDriverContext((uint)(poll+1),1,0,0,0x13579bdf,0x2468ace0,4));
                    last=clockUpdate.Movement;clock=clockUpdate.State.Clock;neutralPasses+=last.OwnerVisibilityPasses;driverVisits++;
                }
                else last=state.AdvanceOfflinePoll(SampleAt(poll));
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
            return new PracticeNavigationMovementStudyState(this,state,last,Polls+count,visits,changes,consumed,completed,dormant,replans,terrainHits,fleetHits,worldEvents,waypointCalls,repeatedCalls,gridCalls,points.ToArray(),clock,clockUpdate,neutralPasses,phase,phaseUpdate,pausedFrames,driverVisits);
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
        public PracticeNavigationMovementStudyState Reset(){return new PracticeNavigationMovementStudyState(basis,Preset,visibility);}
    }
}
