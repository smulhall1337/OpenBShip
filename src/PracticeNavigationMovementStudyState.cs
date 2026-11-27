using System;using System.Drawing;using System.Collections.Generic;using System.Collections.ObjectModel;
namespace FleetLab
{
    // Bounded viewing of the five original864 histories. Poll clocks/phase
    // are supplied inputs; this is not a natural application clock or mission.
    public sealed class PracticeNavigationMovementStudyState
    {
        public const int MaximumPolls=256;
        static readonly string[] names={"Open route · speed 3 / flag 2 · clock held","Open route · speed 7 / flag 3 · +40 ms",
            "Nonmultiple input · 9,19 to 29,39","Already at destination · 320,320","Blocked destination · primary ring"};
        readonly PracticeGoalRouteStudyBasis basis;readonly Point[] trail;
        public static ReadOnlyCollection<string> Presets {get{return Array.AsReadOnly(names);}}
        public int Preset {get;private set;}public int Polls {get;private set;}
        public int ActorVisits {get;private set;}public int PositionChanges {get;private set;}public int QueueConsumptions {get;private set;}
        public int NavigationCompletions {get;private set;}public int DormantPrephases {get;private set;}
        public PracticeNavigationMovementState State {get;private set;}
        public PracticeNavigationMovementUpdate LastUpdate {get;private set;}
        public int ClockStep {get{return Preset==0?0:40;}}
        public int LastSourceClock {get{return LastUpdate==null?12345:LastUpdate.SourceClock;}}
        public int NextSourceClock {get{return 12345+Polls*ClockStep;}}
        public bool CanAdvance {get{return Polls<MaximumPolls;}}
        public bool HasGridOutput {get{return Polls>0;}}
        public Point Position {get{var f=State.CopyFleetPool();return new Point(BitConverter.ToInt16(f,16*492+12),BitConverter.ToInt16(f,16*492+14));}}
        public PracticeNavigationMovementStudyState(PracticeGoalRouteStudyBasis basis,int preset)
        {
            if(basis==null)throw new ArgumentNullException("basis");if(preset<0||preset>=names.Length)throw new ArgumentOutOfRangeException("preset");
            this.basis=basis;Preset=preset;State=basis.CreateMovementComparison(preset);trail=new[]{Position};
        }
        PracticeNavigationMovementStudyState(PracticeNavigationMovementStudyState original,PracticeNavigationMovementState state,
            PracticeNavigationMovementUpdate last,int polls,int visits,int changes,int consumed,int completed,int dormant,Point[] points)
        {
            basis=original.basis;Preset=original.Preset;State=state;LastUpdate=last;Polls=polls;ActorVisits=visits;PositionChanges=changes;
            QueueConsumptions=consumed;NavigationCompletions=completed;DormantPrephases=dormant;trail=points;
        }
        public Point[] CopyTrail(){return(Point[])trail.Clone();}
        public PracticeNavigationMovementStudyState Advance(int requestedPolls)
        {
            if(requestedPolls<1||requestedPolls>MaximumPolls)throw new ArgumentOutOfRangeException("requestedPolls");
            if(!CanAdvance)throw new InvalidOperationException("The recorded 256-poll viewing window is complete;reset before another comparison.");
            int count=Math.Min(requestedPolls,MaximumPolls-Polls),visits=ActorVisits,changes=PositionChanges,consumed=QueueConsumptions,completed=NavigationCompletions,dormant=DormantPrephases;
            var state=State;var last=LastUpdate;var points=new List<Point>(trail);
            for(int n=0;n<count;n++)
            {
                last=state.AdvanceOfflinePoll(12345+(Polls+n)*ClockStep);
                if(last.Navigation==null)dormant++;else if(last.Navigation.Outcome==PracticeQueuedNavigationOutcome.CompletedGoal)completed++;
                foreach(var visit in last.Movement.FleetUpdates)
                {
                    visits++;if(visit.Update.Before!=visit.Update.After){changes++;points.Add(visit.Update.After);}
                    if(visit.Update.QueueConsumed)consumed++;
                }
                state=last.State;
            }
            return new PracticeNavigationMovementStudyState(this,state,last,Polls+count,visits,changes,consumed,completed,dormant,points.ToArray());
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
        public PracticeNavigationMovementStudyState Reset(){return new PracticeNavigationMovementStudyState(basis,Preset);}
    }
}
