using System;using System.IO;using System.Collections.Generic;using System.Collections.ObjectModel;
namespace FleetLab
{
    public sealed class PracticeReinforcementPhaseUpdate
    {
        public PracticeReinforcementPhaseState Before{get;private set;}
        public PracticeGeneratedReinforcements PreparedWorld{get;private set;}
        public PracticeReinforcementPhaseState State{get;private set;}
        public uint GameClock{get;private set;}public short PauseWord{get;private set;}
        public bool DriverCalled{get;private set;}public bool ConsumerPolled{get;private set;}
        public uint CallerScratch{get;private set;}
        readonly ReadOnlyCollection<byte> contacts;
        public ReadOnlyCollection<byte> RefreshContactBytes{get{return contacts;}}
        internal PracticeReinforcementPhaseUpdate(PracticeReinforcementPhaseState before,PracticeGeneratedReinforcements prepared,PracticeReinforcementPhaseState state,uint clock,short pause,bool driver,bool poll,uint scratch,List<byte> contacts)
        {Before=before;PreparedWorld=prepared;State=state;GameClock=clock;PauseWord=pause;DriverCalled=driver;ConsumerPolled=poll;CallerScratch=scratch;this.contacts=contacts.AsReadOnly();}
    }

    // Actual phase/pause fragments ->whole41D640, original1132R2,
    // independent1133/fresh1135. Clock values are supplied by a finite trace.
    // Zero columns/terrain, no neutral fleets/policies, raw owner control0,
    // mapped idle R204/R304 and diagnostic F249contact are explicit fixtures.
    public sealed class PracticeReinforcementPhaseState
    {
        public const int MaximumFrames=96;
        public const uint IncomingFrameWord=0x2468ace0,SkippedCallerSentinel=0x13579bdf;
        readonly PracticeGeneratedReinforcements initial;
        readonly PracticeOwnerVisibilityRules visibility;
        public PracticeGeneratedReinforcements World{get;private set;}
        public uint InitialPhase{get;private set;}public uint PhaseCounter{get;private set;}
        public int PauseProfile{get;private set;}public int Frames{get;private set;}
        public int DriverCalls{get;private set;}public int ConsumerPolls{get;private set;}
        public int FirstDeadline{get;private set;}public int LastDeadline{get;private set;}
        public bool CanAdvance{get{return Frames<MaximumFrames;}}
        public byte ContactByte249{get{return World.CopyFleetPool()[249*492+480];}}

        public static PracticeReinforcementPhaseState Create(PracticeReinforcementTables tables,string programs,byte[] map,uint seed,uint phase,int pauseProfile)
        {
            var before=new byte[128];for(int n=0;n<before.Length;n++)before[n]=(byte)(0x5b^n);return CreateFixture(tables,programs,map,seed,phase,pauseProfile,before);
        }
        internal static PracticeReinforcementPhaseState CreateFixture(PracticeReinforcementTables tables,string programs,byte[] map,uint seed,uint phase,int pauseProfile,byte[] before)
        {
            if(phase!=0&&phase!=31&&phase!=uint.MaxValue)throw new ArgumentOutOfRangeException("phase","The compared initial phases are0,31andMAX.");
            if(pauseProfile<0||pauseProfile>2)throw new ArgumentOutOfRangeException("pauseProfile");
            var world=PracticeGeneratedReinforcements.CreateDriverIdleFixture(tables,programs,map,seed,before);var rules=PracticeOwnerVisibilityRules.Load(programs);return new PracticeReinforcementPhaseState(world,rules,phase,pauseProfile);
        }
        PracticeReinforcementPhaseState(PracticeGeneratedReinforcements world,PracticeOwnerVisibilityRules visibility,uint phase,int pauseProfile)
        {
            ValidateIdle(world);initial=world;World=world;this.visibility=visibility;InitialPhase=PhaseCounter=phase;PauseProfile=pauseProfile;FirstDeadline=int.MaxValue;LastDeadline=-1;
            for(int group=0;group<2;group++)for(int slot=0;slot<4;slot++){int deadline=world.DeadlineAt(group,slot);if(deadline<0||deadline>=2000000)throw new InvalidDataException("Driver trace deadline outside its compared nonwrapping range.");FirstDeadline=Math.Min(FirstDeadline,deadline);LastDeadline=Math.Max(LastDeadline,deadline);}
        }
        PracticeReinforcementPhaseState(PracticeReinforcementPhaseState source,PracticeGeneratedReinforcements world,uint phase,bool driver,bool poll)
        {
            initial=source.initial;visibility=source.visibility;World=world;InitialPhase=source.InitialPhase;PhaseCounter=phase;PauseProfile=source.PauseProfile;Frames=source.Frames+1;DriverCalls=source.DriverCalls+(driver?1:0);ConsumerPolls=source.ConsumerPolls+(poll?1:0);FirstDeadline=source.FirstDeadline;LastDeadline=source.LastDeadline;
        }
        public PracticeReinforcementPhaseState ResetCopy(){return new PracticeReinforcementPhaseState(initial,visibility,InitialPhase,PauseProfile);}
        public PracticeReinforcementPhaseUpdate AdvanceFrame()
        {
            if(!CanAdvance)throw new InvalidOperationException("The modern96-frame comparison limit has been reached;reset or load a new copy.");ValidateIdle(World);
            short pause=(short)(PauseProfile==1&&Frames>=30&&Frames<=32?1:PauseProfile==2&&Frames>=32&&Frames<=34?-32768:0);
            uint clock=(uint)(Frames==0?FirstDeadline:Frames<32?FirstDeadline+1:LastDeadline+1);
            var prepared=World.PrepareDriverFrame(clock);var world=prepared;bool driver=pause==0;uint phase=driver?unchecked(PhaseCounter+1u):PhaseCounter;bool poll=driver&&(phase&31)==0;uint scratch=driver?(poll?IncomingFrameWord:0u):SkippedCallerSentinel;var contacts=new List<byte>();
            // The unconditional arrival/prephase is a fast return ONLY under
            // the verified idle reference/collision/route conditions below.
            if(poll)
            {
                byte contact=world.CopyFleetPool()[249*492+480];world=world.PollSourceDriver();for(int n=0;n<world.RefreshRequests;n++)contacts.Add(contact);
                // The zero-cell mode5 planner makes no assignments. Visibility
                // follows consumption; refresh observes the earlier contact.
                world=world.ApplyNeutralDriverVisibility(visibility);
            }
            ValidateIdle(world);var next=new PracticeReinforcementPhaseState(this,world,phase,driver,poll);return new PracticeReinforcementPhaseUpdate(this,prepared,next,clock,pause,driver,poll,scratch,contacts);
        }
        static void ValidateIdle(PracticeGeneratedReinforcements world)
        {
            if(world==null||!world.UsesOccupiedMap||world.MapProfile!=0||world.ProducerCount!=2||world.ConsumerCount!=2||world.InitialClock!=0)throw new InvalidDataException("Phase driver requires its pinned occupied2/2/base0fixture.");
            byte[] fleets=world.CopyFleetPool(),runtime=world.CopyRuntimePool();
            for(int fleet=0;fleet<250;fleet++)
            {
                int at=fleet*492,task=fleet*312;
                if(fleets[at+486]!=0||fleets[at+482]!=255||BitConverter.ToInt32(runtime,task+204)!=0x4b47a0+at||BitConverter.ToInt32(runtime,task+304)!=0||BitConverter.ToInt32(fleets,at)!=0&&BitConverter.ToInt16(fleets,at+6)==-1)
                    throw new NotSupportedException("This driver admits idle fleet references,collision255,policy0andno active neutral owner only;liveAI/movement is not implemented by this profile.");
            }
        }
    }
}
