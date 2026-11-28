using System;using System.Collections.ObjectModel;using System.IO;
namespace FleetLab
{
    // Original1026/maintained1027/fresh1029. Samples and pause words are fixed
    // diagnostic inputs. Header clock completion follows ALL admitted bodies;
    // no original wait/render/input or pause-deadline compensation runs here.
    public sealed class PracticeMixedClockState : IPracticeMixedMovingComparison
    {
        public const int MaximumVisits=96;
        static readonly string[] names={"Clock + pause · stored aircraft / route","Clock + pause · blocked destination / activation","Clock + pause · route start / activation","Hold phase 32 · fleet-collision replan","Hold phase 32 · activation / route","Hold phase 32 · terrain-collision replan","Short route + zero sample · aircraft activation"};
        readonly MixedPrephaseWork work;readonly byte[] scores,effects,visuals;
        readonly PracticeAttachmentPatrolTables policy;readonly PracticeOwnerVisibilityRules visibility;
        readonly PracticeAircraftDestructionTable destruction;readonly PracticeUnitCatalog units;readonly PracticeAircraftTables modes;
        public static ReadOnlyCollection<string> Presets{get{return Array.AsReadOnly(names);}}
        public int Preset{get;private set;}public int Visits{get;private set;}public int WindowVisits{get{return MaximumVisits;}}public bool CanAdvance{get{return Visits<MaximumVisits;}}
        public int ParentFleet{get{return Preset==0||Preset==4?15:17;}}public int AircraftFleet{get{return ParentFleet==15?23:27;}}
        public OriginalFrameClock Clock{get;private set;}public OriginalFrameClock BeforeClock{get;private set;}public OriginalFrameClock BodyClock{get;private set;}
        public OriginalPracticePhase Phase{get;private set;}public uint BeforePhase{get;private set;}
        public short PauseWord{get;private set;}public uint SampleMilliseconds{get;private set;}public bool SimulationAdvanced{get;private set;}
        public int HeldFrames{get;private set;}public int DriverVisits{get;private set;}public int NeutralPasses{get;private set;}public int ContactClears{get;private set;}
        public uint DriverScratchAfter{get;private set;}public int SourceClock{get{return unchecked((int)BodyClock.GameMilliseconds);}}
        public int CounterBeforeParent{get{return work.ParentCounterBefore;}}public int CounterAfterParent{get{return work.ParentCounterAfter;}}
        public int PrephaseCallerWordAfter{get{return work.Counter;}}public int CallerWordAfter{get{return unchecked((int)DriverScratchAfter);}}
        public int InitialCollisionAliasWrites{get{return work.AliasWrites;}}public int UiRequests{get{return work.Ui;}}public int SelectedWaypoint{get{return work.Waypoint;}}
        public int Activations{get{return work.Activations;}}public int TotalActivations{get;private set;}
        public string NavigationDescription{get{return Visits==0?"Prepared clocked source input":!SimulationAdvanced?"Source slice held by pause":work.Navigation;}}
        public PracticeAiSharedBacking Backing{get{return work.Backing;}}public PracticeAiRouteGrid Grid{get{return work.Grid;}}public PracticeFleetScheduler Schedule{get;private set;}
        public int MovieCounter{get;private set;}public uint RandomState{get;private set;}public int TotalShipMoves{get;private set;}public int TotalAircraftMoves{get;private set;}public int SchedulerWraps{get;private set;}
        public int SelectedFleetFlag{get{return work.Backing.CopyTaskRecord(249)[284];}}
        public short PauseWordAt(int frame){if(frame<0||frame>=MaximumVisits)throw new ArgumentOutOfRangeException("frame");bool held=Preset>=3?frame>=32&&frame<=34:frame>=30&&frame<=32;return held?(Preset==0?(short)1:Preset==1?(short)-1:short.MinValue):(short)0;}
        public uint SampleAt(int frame){if(frame<0||frame>=MaximumVisits)throw new ArgumentOutOfRangeException("frame");return(uint)(12345+(Preset==6&&frame==66?65:frame)*40);}
        PracticeMixedClockState(string programs,PracticeMapStartup startup,int preset)
        {
            if(preset<0||preset>=names.Length)throw new ArgumentOutOfRangeException("preset");
            policy=PracticeAttachmentPatrolTables.Load(programs);visibility=PracticeOwnerVisibilityRules.Load(programs);destruction=PracticeAircraftDestructionTable.Load(programs);units=PracticeUnitCatalog.Load(programs);modes=PracticeAircraftTables.Load(programs);visuals=File.ReadAllBytes(Path.Combine(programs,"ClassicVisuals.seqbin"));
            work=PracticeMixedNavigationState.CreateMovingFixture(programs,startup,preset);work.Fleets[249*492+480]=1;
            Preset=preset;scores=new byte[176];BitConverter.GetBytes((short)-2).CopyTo(scores,172);effects=new byte[800];Schedule=new PracticeFleetScheduler(16,0);MovieCounter=37;RandomState=731;
            Clock=new OriginalFrameClock(12265,12305,12345,40);BeforeClock=BodyClock=Clock;Phase=new OriginalPracticePhase(0);SampleMilliseconds=12345;
        }
        PracticeMixedClockState(PracticeMixedClockState old,MixedPrephaseWork next,byte[] score,byte[] effect,PracticeFleetScheduler schedule,OriginalFrameClock body,OriginalPracticePhase phase,uint sample,short pause,uint scratch,bool advanced,int neutral,int shipMoves,int airMoves,int wraps,int movie,uint random)
        {
            policy=old.policy;visibility=old.visibility;destruction=old.destruction;units=old.units;modes=old.modes;visuals=old.visuals;
            work=next;scores=score;effects=effect;Schedule=schedule;Preset=old.Preset;Visits=old.Visits+1;
            BeforeClock=old.Clock;BodyClock=body;Clock=body.CompleteFrame();BeforePhase=old.Phase.Value;Phase=phase;SampleMilliseconds=sample;PauseWord=pause;SimulationAdvanced=advanced;DriverScratchAfter=scratch;
            HeldFrames=old.HeldFrames+(pause!=0?1:0);DriverVisits=old.DriverVisits+(advanced?1:0);NeutralPasses=old.NeutralPasses+neutral;ContactClears=old.ContactClears+(old.work.Fleets[249*492+480]==1&&next.Fleets[249*492+480]==0?1:0);
            TotalActivations=old.TotalActivations+next.Activations;TotalShipMoves=old.TotalShipMoves+shipMoves;TotalAircraftMoves=old.TotalAircraftMoves+airMoves;SchedulerWraps=old.SchedulerWraps+wraps;MovieCounter=movie;RandomState=random;
        }
        public static PracticeMixedClockState Create(string programs,PracticeMapStartup startup,int preset){return new PracticeMixedClockState(programs,startup,preset);}
        public PracticeMixedClockState Advance(){return AdvanceCore(false,false,false);}
        IPracticeMixedComparison IPracticeMixedComparison.AdvanceForStudy(){return Advance();}
        internal PracticeMixedClockState AdvanceIgnoringPauseForComparison(){return AdvanceCore(true,false,false);}
        internal PracticeMixedClockState AdvanceUsingEarlyClockForComparison(){return AdvanceCore(false,true,false);}
        internal PracticeMixedClockState AdvanceSkippingNeutralForComparison(){return AdvanceCore(false,false,true);}
        PracticeMixedClockState AdvanceCore(bool ignorePause,bool earlyClock,bool skipNeutral)
        {
            if(!CanAdvance)throw new InvalidOperationException("Ninety-six compared clock frames are complete; reset the copy.");
            uint sample=SampleAt(Visits);short pause=PauseWordAt(Visits);var body=Clock.BeginFrame(sample,0);var phase=Phase.Advance(pause);
            var next=CopyWork();var score=(byte[])scores.Clone();var effect=(byte[])effects.Clone();var schedule=Schedule;int shipMoves=0,airMoves=0,wraps=0,neutral=0,movie=MovieCounter;uint random=RandomState,scratch=0;bool advanced=pause==0||ignorePause;
            if(advanced)
            {
                var supplied=earlyClock?body.CompleteFrame():body;
                next=work.AdvanceWithDelta(policy,unchecked((int)supplied.GameMilliseconds),unchecked((int)supplied.DeltaMilliseconds),false);scratch=unchecked((uint)next.Counter);
                if((phase.Value&31)==0){neutral=1;if(!skipNeutral){next.Fleets=visibility.Apply(next.Fleets,-1,0,-2,0,249).CopyFleetPool();scratch=0x2468ace0;}}
                for(int call=0;call<2;call++)
                {
                    var result=PracticeMixedFleetBatch.AdvanceOffscreen(destruction,visuals,units,modes,next.Ships,next.Fleets,score,effect,next.Orders,next.Backing.CopyMovementAuxiliary(),next.Terrain,new byte[61500],schedule,next.Waypoint,movie,random);
                    foreach(var update in result.SurfaceUpdates)if(update.Update.Before!=update.Update.After)shipMoves++;var f=result.CopyFleetPool();foreach(var update in result.AircraftUpdates){int at=update.Slot*492;if(BitConverter.ToInt16(next.Fleets,at+12)!=BitConverter.ToInt16(f,at+12)||BitConverter.ToInt16(next.Fleets,at+14)!=BitConverter.ToInt16(f,at+14))airMoves++;}
                    if(result.NextSchedule.CompletedCycles!=schedule.CompletedCycles)wraps++;schedule=result.NextSchedule;next.Ships=result.CopyShipPool();next.Fleets=f;next.Orders=result.CopyPrimaryOrders();next.Backing=next.Backing.WithMovementAuxiliary(result.CopyAuxiliary());next.Waypoint=result.SelectedWaypoint;next.Ui+=result.Invalidations;score=result.CopyScoreRegion();effect=result.CopyEffectPool();movie=result.MovieCounter;random=result.RandomState;
                }
            }
            // Header epilogue is deliberately last, including on paused frames.
            return new PracticeMixedClockState(this,next,score,effect,schedule,body,phase,sample,pause,scratch,advanced,neutral,shipMoves,airMoves,wraps,movie,random);
        }
        MixedPrephaseWork CopyWork(){return new MixedPrephaseWork{Ships=CopyShipPool(),Fleets=CopyFleetPool(),Orders=CopyPrimaryOrders(),Candidates=CopyCandidateRegion(),Backing=Backing,Grid=Grid,Terrain=CopyTerrainPool(),Waypoint=SelectedWaypoint};}
        public byte[] CopyShipPool(){return(byte[])work.Ships.Clone();}public byte[] CopyFleetPool(){return(byte[])work.Fleets.Clone();}public byte[] CopyPrimaryOrders(){return(byte[])work.Orders.Clone();}public byte[] CopyCandidateRegion(){return(byte[])work.Candidates.Clone();}public byte[] CopyTerrainPool(){return(byte[])work.Terrain.Clone();}public byte[] CopyScoreRegion(){return(byte[])scores.Clone();}public byte[] CopyEffectPool(){return(byte[])effects.Clone();}
        internal byte[][] SnapshotForComparison(){var rows=new byte[1024000];for(int n=0;n<rows.Length;n++)rows[n]=0xcd;var p=work.Pools(rows);return new[]{p[0],p[1],p[2],p[3],p[4],p[5],CopyScoreRegion(),CopyEffectPool()};}
    }
}
