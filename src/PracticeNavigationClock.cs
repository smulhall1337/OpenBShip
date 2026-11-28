using System;

namespace FleetLab
{
    // Pinned RIP B9A8...6A569, original964/maintained965. This comparison
    // composes an explicit sample/phase with the neutral navigation profile.
    // It does not produce a natural clock, phase, pause state or full world.
    public sealed class PracticeNavigationClockState
    {
        public PracticeNavigationMovementState Movement { get; private set; }
        public OriginalFrameClock Clock { get; private set; }
        public PracticeOwnerVisibilityRules Visibility { get; private set; }
        public PracticeNavigationClockState(PracticeNavigationMovementState movement,
            OriginalFrameClock clock,PracticeOwnerVisibilityRules visibility)
        {
            if(movement==null||clock==null||visibility==null)throw new ArgumentNullException("movement/clock/visibility");
            Movement=movement;Clock=clock;Visibility=visibility;
        }
        public PracticeNavigationClockUpdate AdvanceFrame(uint sample,PracticeAiDriverContext context)
        {
            var body=Clock.BeginFrame(sample,0);
            var movement=Movement.AdvanceNeutralDriverPoll(unchecked((int)body.GameMilliseconds),context,Visibility);
            // Source replanning sees the sampled game time. Only after BOTH
            // movement batches does the header compute delta and its zero reset.
            var next=new PracticeNavigationClockState(movement.State,body.CompleteFrame(),Visibility);
            return new PracticeNavigationClockUpdate(this,body,movement,next,sample);
        }
    }
    public sealed class PracticeNavigationClockUpdate
    {
        public PracticeNavigationClockState Before { get; private set; }
        public OriginalFrameClock BodyClock { get; private set; }
        public PracticeNavigationMovementUpdate Movement { get; private set; }
        public PracticeNavigationClockState State { get; private set; }
        public uint Sample { get; private set; }
        internal PracticeNavigationClockUpdate(PracticeNavigationClockState before,OriginalFrameClock body,
            PracticeNavigationMovementUpdate movement,PracticeNavigationClockState state,uint sample)
        {Before=before;BodyClock=body;Movement=movement;State=state;Sample=sample;}
    }
}
