using System;
namespace FleetLab
{
    // B9A8...6A569:410D55 increments the independent32bit phase counter
    // only when unpaused;410E57 gates4297F0/419790 at low6bits==13.
    // This carries that gate through supplied40ms study polls, without
    // reconstructing the application clock, other AI passes or missions.
    public sealed class PracticeAircraftRequestClock
    {
        public PracticeAircraftWorldStudyState World
        {
            get;
            private set;
        }
        public uint PhaseCounter
        {
            get;
            private set;
        }
        public int ProducerPasses
        {
            get;
            private set;
        }
        public PracticeAircraftRequestClock(PracticeAircraftWorldStudyState world,uint phaseCounter)
        {
            if(world==null)throw new ArgumentNullException("world");
            world.CheckRequestPhaseProfile();
            World=world;
            PhaseCounter=phaseCounter;
        }
        PracticeAircraftRequestClock Copy(PracticeAircraftWorldStudyState world,uint phase,int passes)
        {
            var next=new PracticeAircraftRequestClock(world,phase);
            next.ProducerPasses=passes;
            return next;
        }
        public PracticeAircraftRequestClock Queue(int source,int target,int weapon,int x,int y)
        {
            return Copy(World.Queue(source,target,weapon,x,y),PhaseCounter,ProducerPasses);
        }
        public PracticeAircraftRequestClock Clear(int source)
        {
            return Copy(World.Clear(source),PhaseCounter,ProducerPasses);
        }
        public PracticeAircraftRequestClock AdvanceFrame(bool paused)
        {
            uint phase=paused?PhaseCounter:unchecked(PhaseCounter+1);
            bool produce=!paused&&(phase&63)==13;
            var next=World.AdvanceWithRequestProducer(paused,produce);
            return Copy(next,phase,ProducerPasses+(produce?1:0));
        }
    }
}
