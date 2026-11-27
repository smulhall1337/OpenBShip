using System;
using System.IO;

namespace FleetLab
{
    public enum PracticeActiveNavigationOutcome { CompletedGoal, Waiting, Replanned, ReachedWaypoint }
    public sealed class PracticeActiveNavigationInput
    {
        readonly byte[] fleets,secondary;
        public PracticeAiSharedBacking Backing {get;private set;}
        public PracticeAiRouteGrid Grid {get;private set;}
        internal PracticeActiveNavigationInput(byte[] f,byte[] s,PracticeAiSharedBacking b,PracticeAiRouteGrid g)
        {fleets=(byte[])f.Clone();secondary=(byte[])s.Clone();Backing=b;Grid=g;}
        public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
        public byte[] CopySecondaryFleetPool(){return (byte[])secondary.Clone();}
    }
    public sealed class PracticeActiveNavigationResult
    {
        readonly byte[] fleets;
        public PracticeAiSharedBacking Backing {get;private set;}
        public PracticeAiRouteGrid Grid {get;private set;}
        public PracticeActiveNavigationOutcome Outcome {get;private set;}
        public PracticeGoalRouteResult InitialReplan {get;private set;}
        public PracticeGoalWaypointResult Waypoint {get;private set;}
        public int NavigationVisits {get{return 250;}}
        public int GridFunctionVisits {get{return (InitialReplan==null?0:1)+(Waypoint!=null&&Waypoint.Route!=null?1:0);}}
        public int WaypointFunctionVisits {get{return Waypoint==null?0:1;}}
        internal PracticeActiveNavigationResult(byte[] f,PracticeAiSharedBacking b,PracticeAiRouteGrid g,
            PracticeActiveNavigationOutcome outcome,PracticeGoalRouteResult initial,PracticeGoalWaypointResult waypoint)
        {fleets=(byte[])f.Clone();Backing=b;Grid=g;Outcome=outcome;InitialReplan=initial;Waypoint=waypoint;}
        public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
    }

    // Supported RIP B9A8...6A569 whole41FCC0 with41F920 and its route leaves.
    // Original829 compares108 complete states. This explicit offline profile
    // has only actor16 active; all policy/idle callbacks and network mode are
    // inactive. No41D640 owner controller, movement or clock increment runs.
    public static class PracticeActiveNavigation
    {
        public static PracticeActiveNavigationResult EvaluateOffline(PracticeAiRouteGrid previous,byte[] fleets,
            byte[] secondaryFleets,PracticeAiSharedBacking backing,int sourceClock,int delayWeight,int ownerPolicy)
        {
            if(previous==null||backing==null)throw new ArgumentNullException("previous");
            if(previous.TaskSlot!=16)throw new NotSupportedException("The compared active prephase profile uses actor16.");
            if(fleets==null||fleets.Length!=123000||secondaryFleets==null||secondaryFleets.Length!=246000)
                throw new InvalidDataException("Active prephase requires complete primary and secondary fleet pools.");
            if(ownerPolicy!=0&&ownerPolicy!=2)throw new NotSupportedException("Only compared owner policy0/2 is admitted.");
            for(int slot=0;slot<250;slot++)
            {
                int at=slot*492;var task=backing.CopyTaskRecord(slot);
                if(BitConverter.ToUInt32(task,204)!=0x4b47a0u+(uint)at)
                    throw new InvalidDataException("Active prephase requires the compared task-to-fleet slot identities.");
                if(fleets[at+486]!=0||BitConverter.ToInt32(fleets,at+472)!=0)
                    throw new NotSupportedException("Active prephase currently requires dormant policy/idle states in all fleets.");
                if(slot!=16&&(BitConverter.ToInt32(task,304)!=0||fleets[at+482]!=255))
                    throw new NotSupportedException("Only one active navigation task is compared; other slots require collisionFF/nav0.");
            }
            int actor=16*492,kind=BitConverter.ToInt32(fleets,actor);var active=backing.CopyTaskRecord(16);
            if((kind!=1&&kind!=9)||BitConverter.ToInt16(fleets,actor+4)!=16||BitConverter.ToInt16(fleets,actor+6)!=1||
                BitConverter.ToInt32(active,0)!=11||BitConverter.ToInt32(active,200)!=4||BitConverter.ToInt32(active,304)!=1||
                (fleets[actor+482]!=254&&fleets[actor+482]!=255))
                throw new NotSupportedException("Active prephase requires the compared owner1 task11/goal4 navigation profile.");
            var after=(byte[])fleets.Clone();var next=backing;var grid=previous;
            PracticeGoalRouteResult initial=null;PracticeGoalWaypointResult waypoint=null;
            PracticeActiveNavigationOutcome outcome=PracticeActiveNavigationOutcome.Waiting;
            for(int slot=0;slot<250;slot++)
            {
                if(slot!=16){next=PracticeNavigationGate.Evaluate(after,next,slot).Backing;continue;}
                int x=BitConverter.ToInt16(after,actor+12),y=BitConverter.ToInt16(after,actor+14);
                var task=next.CopyTaskRecord(16);
                if(BitConverter.ToInt32(task,296)==x&&BitConverter.ToInt32(task,300)==y)
                {
                    next=PracticeNavigationGate.Evaluate(after,next,16).Backing;outcome=PracticeActiveNavigationOutcome.CompletedGoal;continue;
                }
                int firstX=BitConverter.ToInt16(after,actor+40),firstY=BitConverter.ToInt16(after,actor+50);
                if(firstX!=-1&&(firstX!=x||firstY!=y))
                    throw new NotSupportedException("Only an empty or exactly reached first waypoint is compared in this active profile.");
                if(after[actor+482]==254)
                {
                    if(firstX!=-1)throw new NotSupportedException("The compared collisionFE replan begins with an empty first point.");
                    initial=PracticeGoalRoute.BuildForWaypoint(grid,after,secondaryFleets,next,sourceClock,delayWeight,ownerPolicy,false);
                    waypoint=PracticeGoalWaypoint.EvaluateForNavigation(initial.Grid,after,secondaryFleets,initial.Backing,sourceClock,delayWeight,ownerPolicy);
                    after=waypoint.CopyFleetPool();next=waypoint.Backing;grid=waypoint.Grid;after[actor+482]=255;outcome=PracticeActiveNavigationOutcome.Replanned;
                    if(BitConverter.ToInt16(after,actor+40)==x&&BitConverter.ToInt16(after,actor+50)==y)
                        throw new NotSupportedException("A second waypoint callback after the replan is outside the compared profile.");
                }
                else if(firstX==x&&firstY==y)
                {
                    waypoint=PracticeGoalWaypoint.EvaluateForNavigation(grid,after,secondaryFleets,next,sourceClock,delayWeight,ownerPolicy);
                    after=waypoint.CopyFleetPool();next=waypoint.Backing;grid=waypoint.Grid;outcome=PracticeActiveNavigationOutcome.ReachedWaypoint;
                }
            }
            return new PracticeActiveNavigationResult(after,next,grid,outcome,initial,waypoint);
        }
    }
}
