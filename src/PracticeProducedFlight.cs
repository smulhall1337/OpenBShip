using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FleetLab
{
    // Original617:8 future-only416C40 histories/28070 observations on the two
    // empty-pool609 natural-map weapon22 endpoints. This UI composition uses supplied priorDelta40.
    // No entry at/after the first deadline is admitted, including all expired records.
    public sealed class PracticeProducedFlight
    {
        readonly PracticeProducedLaunch launch;
        readonly PracticeTimedMotionState motion;
        public PracticeProducedLaunch Launch {get{return launch;}}
        public uint Clock {get;private set;}
        public uint FirstDeadline {get;private set;}
        public uint Remaining {get{return FirstDeadline-Clock;}}
        public int Updates {get;private set;}
        public int InterpolatedRecords {get;private set;}
        public int LastStoppedAtRecord {get;private set;}
        public bool UsedBoundaryJump {get;private set;}
        public bool AtBoundary {get{return Clock==FirstDeadline-1;}}
        public bool CanStep {get{return Clock+40u<FirstDeadline;}}
        public byte[] CopyTimedPool(){return motion.CopyTimedPool();}
        public byte[] CopyFleetPool(){return motion.CopyFleetPool();}
        public byte[] CopyShipPool(){return launch.CopyShipPool();}
        public byte[] CopyOrderPool(){return launch.CopyOrderPool();}
        public ReadOnlyCollection<PracticeMemberProjectile> Projectiles
        {
            get{var result=new List<PracticeMemberProjectile>();var timed=motion.CopyTimedPool();foreach(int slot in launch.CopyCreatedSlots())result.Add(new PracticeMemberProjectile(timed,slot));return result.AsReadOnly();}
        }
        PracticeProducedFlight(PracticeProducedLaunch launch,PracticeTimedMotionState motion,uint clock,uint firstDeadline,
            int updates,int interpolated,int stopped,bool boundaryJump)
        {
            this.launch=launch;this.motion=motion;Clock=clock;FirstDeadline=firstDeadline;Updates=updates;
            InterpolatedRecords=interpolated;LastStoppedAtRecord=stopped;UsedBoundaryJump=boundaryJump;
        }
        public static PracticeProducedFlight Create(PracticeProducedLaunch launch)
        {
            if(launch==null)throw new ArgumentNullException("launch");
            if(!launch.Executed||launch.Capacity!=0||launch.CreatedObjects!=2||launch.Requests.InitialTask!=8||launch.TimerMode!=1)
                throw new NotSupportedException("Pre-impact flight requires the completed empty-pool task8 launch with supplied timer0 and two weapon22 projectiles. Capacity-only records cannot be stepped.");
            uint first=uint.MaxValue;foreach(var item in launch.Projectiles)first=Math.Min(first,item.Deadline);
            if(first<=launch.Clock)throw new NotSupportedException("The copied first deadline must be in the future.");
            return new PracticeProducedFlight(launch,new PracticeTimedMotionState(launch.CopyTimedPool(),launch.CopyFleetPool()),launch.Clock,first,0,0,-1,false);
        }
        PracticeProducedFlight Visit(uint clock,bool boundaryJump)
        {
            if(clock<=Clock||clock>=FirstDeadline)throw new NotSupportedException("Stop before the first impact. Motion at or beyond its deadline is outside this study.");
            var update=motion.Advance(clock,40);
            return new PracticeProducedFlight(launch,update.State,clock,FirstDeadline,Updates+1,
                InterpolatedRecords+update.InterpolatedRecords,update.StoppedAtRecord,UsedBoundaryJump||boundaryJump);
        }
        public PracticeProducedFlight Step()
        {
            if(!CanStep)throw new NotSupportedException("Another 40-unit step would reach the first impact. Use the pre-impact boundary control or reset.");
            return Visit(Clock+40,false);
        }
        public PracticeProducedFlight UpTo(int maximum)
        {
            if(maximum<1||maximum>128)throw new ArgumentOutOfRangeException("maximum");
            if(!CanStep)throw new NotSupportedException("No full 40-unit step remains before impact.");
            var next=this;for(int n=0;n<maximum&&next.CanStep;n++)next=next.Step();return next;
        }
        public PracticeProducedFlight ToBoundary()
        {
            if(AtBoundary)throw new InvalidOperationException("Already at the last pre-impact clock. Reset to repeat.");
            var next=this;int count=0;while(next.CanStep){if(++count>512)throw new NotSupportedException("Copied flight exceeded its bounded comparison history.");next=next.Step();}
            // Preserve priorDelta40 on this explicitly labeled diagnostic jump,
            // even when its clock increment is shorter than40.
            if(!next.AtBoundary)next=next.Visit(next.FirstDeadline-1,true);return next;
        }
    }
}
