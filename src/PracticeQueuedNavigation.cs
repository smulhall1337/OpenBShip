using System;
using System.IO;

namespace FleetLab
{
    public enum PracticeQueuedNavigationOutcome { Started, CompletedGoal, Waiting, ReachedWaypoint }
    public sealed class PracticeQueuedNavigationResult
    {
        readonly byte[] fleets;
        public PracticeAiSharedBacking Backing { get; private set; }
        public PracticeAiRouteGrid Grid { get; private set; }
        public int SelectedFleet { get; private set; }
        public int SelectedWaypoint { get; private set; }
        public PracticeQueuedNavigationOutcome Outcome { get; private set; }
        public PracticeGoalRouteResult StartRoute { get; private set; }
        public PracticeGoalWaypointResult Waypoint { get; private set; }
        public int NavigationVisits { get { return 250; } }
        internal PracticeQueuedNavigationResult(byte[] f, PracticeAiSharedBacking b, PracticeAiRouteGrid g,
            int selectedFleet, int selectedPoint, PracticeQueuedNavigationOutcome outcome,
            PracticeGoalRouteResult route, PracticeGoalWaypointResult waypoint)
        { fleets=(byte[])f.Clone(); Backing=b; Grid=g; SelectedFleet=selectedFleet; SelectedWaypoint=selectedPoint; Outcome=outcome; StartRoute=route; Waypoint=waypoint; }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
    }

    // Supported RIP B9A8...6A569. Original839 compares
    // 216 two-visit histories through41FCC0/41F920. Explicit offline profile:
    // actor16 owner1 kind1/9 task11; all policy/idle callbacks dormant; no
    // other navigation, network callback, movement or clock increment.
    // This is a start and its compared follow-up, not an indefinite world loop.
    public static class PracticeQueuedNavigation
    {
        static void Put(byte[] b,int at,int value) { BitConverter.GetBytes(value).CopyTo(b,at); }
        static void Word(byte[] b,int at,int value) { BitConverter.GetBytes(unchecked((short)value)).CopyTo(b,at); }
        public static PracticeQueuedNavigationResult EvaluateOffline(PracticeAiRouteGrid grid, byte[] fleets,
            byte[] secondary, PracticeAiSharedBacking backing, int selectedFleet, int selectedPoint,
            int sourceClock, int delayWeight, int ownerPolicy)
        {
            if(grid==null||backing==null) throw new ArgumentNullException("grid");
            if(grid.TaskSlot!=16) throw new NotSupportedException("The queued-navigation comparison uses actor16.");
            if(fleets==null||fleets.Length!=123000||secondary==null||secondary.Length!=246000)
                throw new InvalidDataException("Queued navigation requires complete primary and secondary fleet pools.");
            if((ownerPolicy!=0&&ownerPolicy!=2)||delayWeight!=32000)
                throw new NotSupportedException("The start histories use owner policy0/2 and delay weight32000.");
            if(selectedFleet!=16&&selectedFleet!=249)
                throw new NotSupportedException("The compared selected fleet is actor16 or dormant249.");
            if(BitConverter.ToInt16(fleets,selectedFleet*492+4)!=selectedFleet)
                throw new InvalidDataException("Selected fleet identity does not agree with its slot.");
            for(int slot=0;slot<250;slot++)
            {
                int at=slot*492; var record=backing.CopyTaskRecord(slot);
                if(BitConverter.ToUInt32(record,204)!=0x4b47a0u+(uint)at)
                    throw new InvalidDataException("Every navigation context must reference its compared fleet slot.");
                if(fleets[at+486]!=0||BitConverter.ToInt32(fleets,at+472)!=0||
                    (slot!=16&&(BitConverter.ToInt32(record,304)!=0||fleets[at+482]!=255)))
                    throw new NotSupportedException("Queued navigation requires dormant policy/idle and only actor16 navigation.");
            }
            int actor=16*492,kind=BitConverter.ToInt32(fleets,actor); var task=backing.CopyTaskRecord(16);
            int goal=BitConverter.ToInt32(task,200),nav=BitConverter.ToInt32(task,304);
            if((kind!=1&&kind!=9)||BitConverter.ToInt16(fleets,actor+4)!=16||BitConverter.ToInt16(fleets,actor+6)!=1||
                BitConverter.ToInt32(task,0)!=11||BitConverter.ToUInt32(task,208)!=grid.NormalizedTableReference)
                throw new NotSupportedException("Queued navigation requires the compared owner1 task11 kind1/9 identity.");
            int x=BitConverter.ToInt16(fleets,actor+12),y=BitConverter.ToInt16(fleets,actor+14);
            if(x<0||x>630||y<0||y>630) throw new NotSupportedException("Compared world coordinates lie in0..630.");
            var after=(byte[])fleets.Clone();
            if(nav==0)
            {
                if(goal!=2||fleets[actor+482]!=254||BitConverter.ToInt16(fleets,actor+40)==-1||
                    (selectedFleet==249&&selectedPoint!=4)||(selectedFleet==16&&selectedPoint!=4&&selectedPoint!=-1))
                    throw new NotSupportedException("The initial start requires queued goal2/collisionFE and a compared selection context.");
                Put(task,304,1);
                for(int n=4;n>0;n--)
                {
                    Word(after,actor+40+n*2,BitConverter.ToInt16(after,actor+40+(n-1)*2));
                    Word(after,actor+50+n*2,BitConverter.ToInt16(after,actor+50+(n-1)*2));
                }
                int nextSelection=selectedFleet==16&&selectedPoint!=-1?unchecked((short)(selectedPoint+1)):selectedPoint;
                Put(task,296,BitConverter.ToInt16(after,actor+40)); Put(task,300,BitConverter.ToInt16(after,actor+50));
                var route=PracticeGoalRoute.BuildForQueuedNavigation(grid,after,secondary,backing.WithTaskRecord(16,task),sourceClock,delayWeight,ownerPolicy);
                Word(after,actor+40,x/10*10); Word(after,actor+50,y/10*10); after[actor+482]=255;
                return new PracticeQueuedNavigationResult(after,route.Backing,route.Grid,selectedFleet,nextSelection,
                    PracticeQueuedNavigationOutcome.Started,route,null);
            }
            if(nav!=1||(goal!=2&&goal!=4)||fleets[actor+482]!=255||
                (selectedFleet==249&&selectedPoint!=4)||(selectedFleet==16&&selectedPoint!=5&&selectedPoint!=-1))
                throw new NotSupportedException("The captured follow-up requires nav1, goal2/4, collisionFF and retained selection.");
            if(BitConverter.ToInt32(task,296)==x&&BitConverter.ToInt32(task,300)==y)
            {
                Put(task,304,0);
                return new PracticeQueuedNavigationResult(after,backing.WithTaskRecord(16,task),grid,selectedFleet,selectedPoint,
                    PracticeQueuedNavigationOutcome.CompletedGoal,null,null);
            }
            if(BitConverter.ToInt16(after,actor+40)==x&&BitConverter.ToInt16(after,actor+50)==y)
            {
                var next=PracticeGoalWaypoint.EvaluateAfterQueuedNavigation(grid,after,secondary,backing,sourceClock,delayWeight,ownerPolicy);
                return new PracticeQueuedNavigationResult(next.CopyFleetPool(),next.Backing,next.Grid,selectedFleet,selectedPoint,
                    PracticeQueuedNavigationOutcome.ReachedWaypoint,null,next);
            }
            return new PracticeQueuedNavigationResult(after,backing,grid,selectedFleet,selectedPoint,
                PracticeQueuedNavigationOutcome.Waiting,null,null);
        }
    }
}
