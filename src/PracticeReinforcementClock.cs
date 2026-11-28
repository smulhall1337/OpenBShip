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
        readonly OriginalPauseTimers initialPause;
        public OriginalPauseTimers PauseTimers{get;private set;}
        public bool AppliesPauseCompensation{get{return PauseTimers!=null;}}
        public PracticeReinforcementPhaseState Phase{get;private set;}
        public OriginalFrameClock Clock{get;private set;}
        public int Frames{get{return Phase.Frames;}}
        public bool CanAdvance{get{return Phase.CanAdvance;}}
        public static PracticeReinforcementClockState Create(PracticeReinforcementTables tables,string programs,byte[] map,uint seed,uint phase,int pauseProfile,bool compensate=false)
        {return CreateFromPhase(PracticeReinforcementPhaseState.Create(tables,programs,map,seed,phase,pauseProfile),compensate);}
        internal static PracticeReinforcementClockState CreateFixture(PracticeReinforcementTables tables,string programs,byte[] map,uint seed,uint phase,int pauseProfile,byte[] raw,bool compensate=false)
        {return CreateFromPhase(PracticeReinforcementPhaseState.CreateFixture(tables,programs,map,seed,phase,pauseProfile,raw),compensate);}
        static PracticeReinforcementClockState CreateFromPhase(PracticeReinforcementPhaseState phase,bool compensate)
        {
            if(phase.PauseProfile!=0&&phase.PauseProfile!=2)throw new ArgumentOutOfRangeException("pauseProfile","Clock comparison admits None or the compared negative-word pause only.");var pause=compensate?CreatePauseFixture(phase.World):null;return new PracticeReinforcementClockState(phase,phase,new OriginalFrameClock(11,77,phase.World.Clock,13),pause,pause);
        }
        // Original1154 explicit opaque fixture; these selected timer fields do
        // not execute the complete timed-object or primary-order systems.
        static OriginalPauseTimers CreatePauseFixture(PracticeGeneratedReinforcements world)
        {
            var timed=new byte[OriginalPauseTimers.TimedRecordBytes];var orders=new byte[OriginalPauseTimers.ShipTimerBytes];for(int n=0;n<timed.Length;n++)timed[n]=(byte)(0x6d^(n%13));for(int n=0;n<orders.Length;n++)orders[n]=(byte)(0xa7^(n%11));for(int n=0;n<80;n++)BitConverter.GetBytes(n==0?2:n==79?1:0).CopyTo(timed,n*268);for(int n=0;n<1500;n++)BitConverter.GetBytes((short)(n==1||n==1499?0:-1)).CopyTo(orders,n*52+8);BitConverter.GetBytes(0xfffffff0u).CopyTo(timed,24);BitConverter.GetBytes(41u).CopyTo(timed,79*268+24);BitConverter.GetBytes(0xfffffffeu).CopyTo(orders,56);BitConverter.GetBytes(0x80000000u).CopyTo(orders,1499*52+4);var four=new byte[16];uint[] values={0,uint.MaxValue,0x80000000,41};for(int n=0;n<4;n++)BitConverter.GetBytes(values[n]).CopyTo(four,n*4);return new OriginalPauseTimers(timed,orders,four,world.CopyDriverDeadlines(),0xfffffff0);
        }
        PracticeReinforcementClockState(PracticeReinforcementPhaseState initial,PracticeReinforcementPhaseState phase,OriginalFrameClock clock,OriginalPauseTimers initialPause,OriginalPauseTimers pause){this.initial=initial;Phase=phase;Clock=clock;this.initialPause=initialPause;PauseTimers=pause;}
        public PracticeReinforcementClockState ResetCopy(){return new PracticeReinforcementClockState(initial,initial,new OriginalFrameClock(11,77,initial.World.Clock,13),initialPause,initialPause);}
        public PracticeReinforcementClockUpdate AdvanceFrame()
        {
            if(!CanAdvance)throw new InvalidOperationException("The modern96-frame clock comparison limit has been reached.");
            int frame=Frames;uint sample=unchecked((uint)(frame<64?(Phase.InitialPhase==31?Phase.FirstDeadline+1+frame*40:Phase.FirstDeadline-1240+frame*40):Phase.LastDeadline+1+(frame-64)*40));
            if(AppliesPauseCompensation&&Phase.PauseProfile==2&&frame>=32&&frame<64)sample+=120;
            if(frame==66)sample=Clock.CurrentMilliseconds;
            var prepared=Phase.World.PrepareDriverFrame(Clock.GameMilliseconds);var body=Clock.BeginFrame(sample,0);var update=Phase.AdvanceWithClock(body.GameMilliseconds);var phase=update.State;OriginalPauseTimers timers=null;
            if(AppliesPauseCompensation){timers=new OriginalPauseTimers(PauseTimers.CopyTimedRecords(),PauseTimers.CopyShipTimers(),PauseTimers.CopyFourCounters(),phase.World.CopyDriverDeadlines(),PauseTimers.GlobalCounter).AdvancePausedFrame(update.PauseWord,body.DeltaMilliseconds);phase=phase.ApplyPausedDeadlines(timers.CopyTimestamps());}
            var after=body.CompleteFrame();phase=phase.CompleteClock(after.GameMilliseconds);var state=new PracticeReinforcementClockState(initial,phase,after,initialPause,timers);return new PracticeReinforcementClockUpdate(this,state,body,sample,update,prepared);
        }
    }
}
