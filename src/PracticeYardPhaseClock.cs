using System;
namespace FleetLab
{
    // B9A8...6A569:410D55 unpaused uint increment;410E57 low6bits==13
    // calls4297F0 before419790. Compose only the recovered yard worker with
    // that gate. Supplied polls/counter, no full world clock, AI or AA updates.
    public sealed class PracticeYardPhaseClock
    {
        public PracticeMapYardState Yard { get; private set; }
        public uint InitialCounter { get; private set; }
        public uint PhaseCounter { get; private set; }
        public long Polls { get; private set; }
        public long PausedPolls { get; private set; }
        public long WorkerPasses { get; private set; }
        public PracticeYardPhaseClock(PracticeMapYardState enteredYard,uint initialCounter)
        {
            if(enteredYard==null)throw new ArgumentNullException("enteredYard");
            if(!enteredYard.Entered)throw new InvalidOperationException("Enter a copied yard before starting its phase-clock study.");
            Yard=enteredYard;InitialCounter=PhaseCounter=initialCounter;
        }
        public PracticeYardPhaseClock Advance(bool paused)
        {
            uint phase=paused?PhaseCounter:unchecked(PhaseCounter+1);
            bool work=!paused&&(phase&63)==13;
            var yard=work?Yard.WorkerStep():Yard;
            return new PracticeYardPhaseClock(yard,InitialCounter)
            {PhaseCounter=phase,Polls=Polls+1,PausedPolls=PausedPolls+(paused?1:0),WorkerPasses=WorkerPasses+(work?1:0)};
        }
        public PracticeYardPhaseClock AdvanceMany(int count,bool paused)
        {
            if(count<0||count>4096)throw new ArgumentOutOfRangeException("count","A copied clock action admits 0 through4096 polls.");
            var clock=this;for(int n=0;n<count;n++)clock=clock.Advance(paused);return clock;
        }
    }
}
