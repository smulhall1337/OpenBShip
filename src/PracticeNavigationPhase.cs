using System;

namespace FleetLab
{
    // Original981:clock header -> actual phase producer -> paused simulation
    // gate -> whole neutral driver/middle/two batches -> clock epilogue.
    // Samples,pause word and caller words remain explicit diagnostic inputs.
    // Full world input/presentation/pause-deadline compensation are separate.
    public sealed class PracticeNavigationPhaseState
    {
        public PracticeNavigationMovementState Movement {get;private set;}
        public OriginalFrameClock Clock {get;private set;}
        public OriginalPracticePhase Phase {get;private set;}
        public PracticeOwnerVisibilityRules Visibility {get;private set;}
        public PracticeNavigationPhaseState(PracticeNavigationMovementState movement,OriginalFrameClock clock,
            OriginalPracticePhase phase,PracticeOwnerVisibilityRules visibility)
        {
            if(movement==null||clock==null||phase==null||visibility==null)throw new ArgumentNullException("movement/clock/phase/visibility");
            if(phase.Value>128)throw new NotSupportedException("The joined navigation comparison is bounded to phases0..128;the separate phase leaf supports full uint wrap.");
            Movement=movement;Clock=clock;Phase=phase;Visibility=visibility;
        }
        public PracticeNavigationPhaseUpdate AdvanceFrame(uint sample,short sourcePauseWord,int callerWord,int incomingFrameWord)
        {
            if(callerWord!=0x13579bdf||incomingFrameWord!=0x2468ace0)
                throw new NotSupportedException("The compared phase/navigation caller explicitly supplies word13579BDF and frame2468ACE0.");
            var body=Clock.BeginFrame(sample,0);var phase=Phase.Advance(sourcePauseWord);PracticeNavigationMovementUpdate driver=null;
            if(sourcePauseWord==0)
                driver=Movement.AdvanceNeutralDriverPoll(unchecked((int)body.GameMilliseconds),
                    new PracticeAiDriverContext(phase.Value,1,0,0,callerWord,incomingFrameWord,4),Visibility);
            var next=new PracticeNavigationPhaseState(driver==null?Movement:driver.State,body.CompleteFrame(),phase,Visibility);
            return new PracticeNavigationPhaseUpdate(this,body,driver,next,sourcePauseWord,sample);
        }
    }
    public sealed class PracticeNavigationPhaseUpdate
    {
        public PracticeNavigationPhaseState Before {get;private set;}
        public OriginalFrameClock BodyClock {get;private set;}
        // Null explicitly means the source gate held the entire admitted slice.
        public PracticeNavigationMovementUpdate Driver {get;private set;}
        public PracticeNavigationPhaseState State {get;private set;}
        public short PauseWord {get;private set;}
        public uint Sample {get;private set;}
        public bool SimulationAdvanced {get{return Driver!=null;}}
        internal PracticeNavigationPhaseUpdate(PracticeNavigationPhaseState before,OriginalFrameClock body,
            PracticeNavigationMovementUpdate driver,PracticeNavigationPhaseState state,short word,uint sample)
        {Before=before;BodyClock=body;Driver=driver;State=state;PauseWord=word;Sample=sample;}
    }
}
