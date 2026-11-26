using System;
using System.IO;
namespace FleetLab
{
    public sealed class PracticePrephaseNavigationResult
    {
        public PracticeAiSharedBacking Backing {get;private set;}
        public int Visits {get{return 250;}}
        public int CompletedGoals {get;private set;}public int ActiveWaiting {get;private set;}
        internal PracticePrephaseNavigationResult(PracticeAiSharedBacking backing,int completed,int waiting){Backing=backing;CompletedGoals=completed;ActiveWaiting=waiting;}
    }
    // Whole41FCC0 only when ALL policy/idle callbacks are inactive. The outer
    // phase/owner controller is not executed. No kind or owner filter belongs here.
    public static class PracticePrephaseNavigation
    {
        public static PracticePrephaseNavigationResult Evaluate(byte[] fleets,PracticeAiSharedBacking backing)
        {
            if(fleets==null||fleets.Length!=123000)throw new InvalidDataException("Pre-phase navigation requires the complete fleet pool.");
            if(backing==null)throw new ArgumentNullException("backing");
            for(int slot=0;slot<250;slot++)
            {
                int at=slot*492,flag=fleets[at+486],state=BitConverter.ToInt32(fleets,at+472);
                if(flag==1||flag==2||flag==3)throw new NotSupportedException("Pre-phase policy callbacks41F410/41F690 are outside this navigation-only subset.");
                if(BitConverter.ToInt16(fleets,at+40)==-1&&(state==5||state==6))throw new NotSupportedException("Pre-phase idle callback41FC10 is outside this navigation-only subset.");
            }
            var next=backing;int completed=0,waiting=0;
            for(int slot=0;slot<250;slot++)
            {
                var visit=PracticeNavigationGate.Evaluate(fleets,next,slot);next=visit.Backing;
                if(visit.Outcome==PracticeNavigationGateOutcome.CompletedGoal)completed++;
                if(visit.Outcome==PracticeNavigationGateOutcome.ActiveWaiting)waiting++;
            }
            return new PracticePrephaseNavigationResult(next,completed,waiting);
        }
    }
}
