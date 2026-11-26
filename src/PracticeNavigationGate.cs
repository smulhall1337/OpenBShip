using System;
using System.IO;
namespace FleetLab
{
    public enum PracticeNavigationGateOutcome { NoCollisionTarget,Unowned,NoWaypoint,CompletedGoal,ActiveWaiting }
    public sealed class PracticeNavigationGateResult
    {
        public PracticeAiSharedBacking Backing {get;private set;}
        public PracticeNavigationGateOutcome Outcome {get;private set;}
        public int LinkedFleet {get;private set;}
        internal PracticeNavigationGateResult(PracticeAiSharedBacking backing,PracticeNavigationGateOutcome outcome,int fleet){Backing=backing;Outcome=outcome;LinkedFleet=fleet;}
    }
    // Restricted whole41F920 returns verified by original633. This is not a
    // pathfinder or the41FCC0 dispatcher. Reject before any unsupported call.
    public static class PracticeNavigationGate
    {
        public static PracticeNavigationGateResult Evaluate(byte[] fleets,PracticeAiSharedBacking backing,int taskSlot)
        {
            if(fleets==null||fleets.Length!=123000)throw new InvalidDataException("Navigation requires the complete fleet pool.");
            if(backing==null)throw new ArgumentNullException("backing");var task=backing.CopyTaskRecord(taskSlot);
            long delta=(long)BitConverter.ToUInt32(task,204)-0x4b47a0;
            if(delta<0||delta>=123000||delta%492!=0)throw new InvalidDataException("Navigation task fleet reference is outside the normalized fleet pool.");
            int fleet=(int)(delta/492),at=fleet*492,x=BitConverter.ToInt16(fleets,at+12),y=BitConverter.ToInt16(fleets,at+14),collision=fleets[at+482];
            PracticeNavigationGateOutcome result;
            if(BitConverter.ToInt32(task,304)==0)
            {
                if(collision==255)result=PracticeNavigationGateOutcome.NoCollisionTarget;
                else if(BitConverter.ToInt16(fleets,at+6)==-1)result=PracticeNavigationGateOutcome.Unowned;
                else if(BitConverter.ToInt16(fleets,at+40)==-1)result=PracticeNavigationGateOutcome.NoWaypoint;
                else throw new NotSupportedException("Navigation route construction/pathfinding is outside the compared return subset.");
            }
            else if(BitConverter.ToInt32(task,296)==x&&BitConverter.ToInt32(task,300)==y)
            {
                BitConverter.GetBytes(0).CopyTo(task,304);return new PracticeNavigationGateResult(backing.WithTaskRecord(taskSlot,task),PracticeNavigationGateOutcome.CompletedGoal,fleet);
            }
            else
            {
                if(collision!=255)throw new NotSupportedException("Navigation retarget/pathfinding is outside the compared return subset.");
                if(BitConverter.ToInt16(fleets,at+40)==x&&BitConverter.ToInt16(fleets,at+50)==y)throw new NotSupportedException("Navigation waypoint-pop/network callbacks are outside the compared return subset.");
                result=PracticeNavigationGateOutcome.ActiveWaiting;
            }
            return new PracticeNavigationGateResult(backing,result,fleet);
        }
    }
}
