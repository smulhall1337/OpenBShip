using System;using System.IO;
namespace FleetLab
{
    public sealed class PracticeReinforcementClockUpdate
    {
        public PracticeReinforcementClockState Before{get;private set;}
        public PracticeReinforcementClockState State{get;private set;}
        public OriginalFrameClock BodyClock{get;private set;}
        public uint SampleMilliseconds{get;private set;}
        public PracticeReinforcementPhaseUpdate BodyUpdate{get;private set;}
        public PracticeGeneratedReinforcements PreparedWorld{get;private set;}
        public bool ZeroInterval{get{return BodyClock.CurrentMilliseconds==BodyClock.PreviousMilliseconds;}}
        internal PracticeReinforcementClockUpdate(PracticeReinforcementClockState before,PracticeReinforcementClockState state,OriginalFrameClock body,uint sample,PracticeReinforcementPhaseUpdate update,PracticeGeneratedReinforcements prepared){Before=before;State=state;BodyClock=body;SampleMilliseconds=sample;BodyUpdate=update;PreparedWorld=prepared;}
    }

    // Whole4109F0 header ->owned body ->actual phase/pause/41D640/consumer/
    // visibility. Original1143R2/independent1144R2/fresh1146. Its supplied time
    // sequence intentionally repeats one sample; this is not a live scheduler.
    public sealed class PracticeReinforcementClockState
    {
        readonly PracticeReinforcementPhaseState initial;
        public PracticeReinforcementPhaseState Phase{get;private set;}
        public OriginalFrameClock Clock{get;private set;}
        public int Frames{get{return Phase.Frames;}}
        public bool CanAdvance{get{return Phase.CanAdvance;}}
        public static PracticeReinforcementClockState Create(PracticeReinforcementTables tables,string programs,byte[] map,uint seed,uint phase,int pauseProfile)
        {return CreateFromPhase(PracticeReinforcementPhaseState.Create(tables,programs,map,seed,phase,pauseProfile));}
        internal static PracticeReinforcementClockState CreateFixture(PracticeReinforcementTables tables,string programs,byte[] map,uint seed,uint phase,int pauseProfile,byte[] raw)
        {return CreateFromPhase(PracticeReinforcementPhaseState.CreateFixture(tables,programs,map,seed,phase,pauseProfile,raw));}
        static PracticeReinforcementClockState CreateFromPhase(PracticeReinforcementPhaseState phase)
        {
            if(phase.PauseProfile!=0&&phase.PauseProfile!=2)throw new ArgumentOutOfRangeException("pauseProfile","Clock comparison admits None or the compared negative-word pause only.");return new PracticeReinforcementClockState(phase,phase,new OriginalFrameClock(11,77,phase.World.Clock,13));
        }
        PracticeReinforcementClockState(PracticeReinforcementPhaseState initial,PracticeReinforcementPhaseState phase,OriginalFrameClock clock){this.initial=initial;Phase=phase;Clock=clock;}
        public PracticeReinforcementClockState ResetCopy(){return new PracticeReinforcementClockState(initial,initial,new OriginalFrameClock(11,77,initial.World.Clock,13));}
        public PracticeReinforcementClockUpdate AdvanceFrame()
        {
            if(!CanAdvance)throw new InvalidOperationException("The modern96-frame clock comparison limit has been reached.");
            int frame=Frames;uint sample=unchecked((uint)(frame<64?(Phase.InitialPhase==31?Phase.FirstDeadline+1+frame*40:Phase.FirstDeadline-1240+frame*40):Phase.LastDeadline+1+(frame-64)*40));
            if(frame==66)sample=Clock.CurrentMilliseconds;
            var prepared=Phase.World.PrepareDriverFrame(Clock.GameMilliseconds);var body=Clock.BeginFrame(sample,0);var update=Phase.AdvanceWithClock(body.GameMilliseconds);var after=body.CompleteFrame();var phase=update.State.CompleteClock(after.GameMilliseconds);var state=new PracticeReinforcementClockState(initial,phase,after);return new PracticeReinforcementClockUpdate(this,state,body,sample,update,prepared);
        }
    }
}
