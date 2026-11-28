using System;
using System.Collections.ObjectModel;
using System.IO;
namespace FleetLab
{
    // Shared presentation contract; the two public models keep separate limits
    // and return types. This interface does not admit arbitrary world inputs.
    internal interface IPracticeMixedComparison
    {
        int Preset{get;} int Visits{get;} int WindowVisits{get;} bool CanAdvance{get;}
        int ParentFleet{get;} int AircraftFleet{get;} int SourceClock{get;}
        int CounterBeforeParent{get;} int CounterAfterParent{get;} int CallerWordAfter{get;}
        int InitialCollisionAliasWrites{get;} int UiRequests{get;} int SelectedWaypoint{get;}
        int Activations{get;} int TotalActivations{get;} string NavigationDescription{get;}
        PracticeAiSharedBacking Backing{get;} PracticeAiRouteGrid Grid{get;}
        byte[] CopyFleetPool();byte[] CopyTerrainPool();IPracticeMixedComparison AdvanceForStudy();
    }

    // Original1012R2/maintained1013/fresh1015. Whole mixed prephase first,
    // then inactive415DF0 and exactly two427670 batches. Phase1/caller0,
    // clock12345+40*visit/delta40 and diagnostic zero columns are supplied.
    // This is not a natural application loop or an unrestricted Practice world.
    public sealed class PracticeMixedMovementState : IPracticeMixedComparison
    {
        public const int MaximumVisits=96;
        static readonly string[] names={
            "Policy first · stored aircraft / active route",
            "Navigation first · blocked destination / activation",
            "Navigation first · route start / activation",
            "Navigation first · active fleet-collision replan",
            "Policy first · activation / active route",
            "Navigation first · active terrain-collision replan",
            "Short route · selected-fleet arrival writeback"
        };
        readonly MixedPrephaseWork work;
        readonly byte[] scores,effects;
        readonly PracticeAttachmentPatrolTables policy;
        readonly PracticeAircraftDestructionTable destruction;
        readonly PracticeUnitCatalog units;readonly PracticeAircraftTables modes;readonly byte[] visuals;
        public static ReadOnlyCollection<string> Presets{get{return Array.AsReadOnly(names);}}
        public int Preset{get;private set;}public int Visits{get;private set;}
        public int WindowVisits{get{return MaximumVisits;}}public bool CanAdvance{get{return Visits<MaximumVisits;}}
        public int ParentFleet{get{return Preset==0||Preset==4?15:17;}}public int AircraftFleet{get{return ParentFleet==15?23:27;}}
        public int SourceClock{get{return 12345+Math.Max(0,Visits-1)*40;}}
        public int NextSourceClock{get{if(!CanAdvance)throw new InvalidOperationException("Compared moving window complete.");return 12345+Visits*40;}}
        public int CounterBeforeParent{get{return work.ParentCounterBefore;}}public int CounterAfterParent{get{return work.ParentCounterAfter;}}
        public int CallerWordAfter{get{return work.Counter;}}public int InitialCollisionAliasWrites{get{return work.AliasWrites;}}
        public int UiRequests{get{return work.Ui;}}public int SelectedWaypoint{get{return work.Waypoint;}}
        public int Activations{get{return work.Activations;}}public int TotalActivations{get;private set;}
        public string NavigationDescription{get{return Visits==0?"Prepared moving source input":work.Navigation;}}
        public PracticeAiSharedBacking Backing{get{return work.Backing;}}public PracticeAiRouteGrid Grid{get{return work.Grid;}}
        public PracticeFleetScheduler Schedule{get;private set;}
        public int MovieCounter{get;private set;}public uint RandomState{get;private set;}
        public int TotalShipMoves{get;private set;}public int TotalAircraftMoves{get;private set;}public int SchedulerWraps{get;private set;}
        public bool MovementChangedSharedBacking{get;private set;}
        public int SelectedFleetFlag{get{return work.Backing.CopyTaskRecord(249)[284];}}

        PracticeMixedMovementState(string programs,PracticeMapStartup startup,int preset)
        {
            if(preset<0||preset>=names.Length)throw new ArgumentOutOfRangeException("preset");
            policy=PracticeAttachmentPatrolTables.Load(programs);destruction=PracticeAircraftDestructionTable.Load(programs);units=PracticeUnitCatalog.Load(programs);modes=PracticeAircraftTables.Load(programs);visuals=File.ReadAllBytes(System.IO.Path.Combine(programs,"ClassicVisuals.seqbin"));
            work=PracticeMixedNavigationState.CreateMovingFixture(programs,startup,preset);Preset=preset;Schedule=new PracticeFleetScheduler(16,0);scores=new byte[176];BitConverter.GetBytes((short)-2).CopyTo(scores,172);effects=new byte[800];MovieCounter=37;RandomState=731;
        }
        PracticeMixedMovementState(PracticeMixedMovementState old,MixedPrephaseWork next,byte[] score,byte[] effect,PracticeFleetScheduler schedule,int movie,uint random,int shipMoves,int airMoves,int wraps,bool sharedChanged)
        {
            policy=old.policy;destruction=old.destruction;units=old.units;modes=old.modes;visuals=old.visuals;
            work=next;scores=score;effects=effect;Schedule=schedule;MovieCounter=movie;RandomState=random;Preset=old.Preset;Visits=old.Visits+1;
            TotalActivations=old.TotalActivations+next.Activations;TotalShipMoves=old.TotalShipMoves+shipMoves;TotalAircraftMoves=old.TotalAircraftMoves+airMoves;SchedulerWraps=old.SchedulerWraps+wraps;MovementChangedSharedBacking=sharedChanged;
        }
        public static PracticeMixedMovementState Create(string programs,PracticeMapStartup startup,int preset){return new PracticeMixedMovementState(programs,startup,preset);}
        public PracticeMixedMovementState Advance(){return AdvanceCore(false);}
        IPracticeMixedComparison IPracticeMixedComparison.AdvanceForStudy(){return Advance();}
        internal PracticeMixedMovementState AdvanceWithReversedOrderForComparison(){return AdvanceCore(true);}
        PracticeMixedMovementState AdvanceCore(bool reversed)
        {
            if(!CanAdvance)throw new InvalidOperationException("Ninety-six compared polls are complete; reset the copy.");
            var next=reversed?CopyWork():work.Advance(policy,NextSourceClock,false);var score=(byte[])scores.Clone();var effect=(byte[])effects.Clone();var schedule=Schedule;int movie=MovieCounter,shipMoves=0,airMoves=0,wraps=0;uint random=RandomState;
            var beforeShared=next.Backing.CopyRegion();
            for(int call=0;call<2;call++)
            {
                var result=PracticeMixedFleetBatch.AdvanceOffscreen(destruction,visuals,units,modes,next.Ships,next.Fleets,score,effect,next.Orders,next.Backing.CopyMovementAuxiliary(),next.Terrain,new byte[61500],schedule,next.Waypoint,movie,random);
                foreach(var update in result.SurfaceUpdates)if(update.Update.Before!=update.Update.After)shipMoves++;
                var after=result.CopyFleetPool();foreach(var update in result.AircraftUpdates){int at=update.Slot*492;if(BitConverter.ToInt16(next.Fleets,at+12)!=BitConverter.ToInt16(after,at+12)||BitConverter.ToInt16(next.Fleets,at+14)!=BitConverter.ToInt16(after,at+14))airMoves++;}
                if(result.NextSchedule.CompletedCycles!=schedule.CompletedCycles)wraps++;schedule=result.NextSchedule;
                next.Ships=result.CopyShipPool();next.Fleets=after;next.Orders=result.CopyPrimaryOrders();
                // Movement/task views overlap by224 bytes. Keep EVERY changed
                // byte, including clears to selected249 when actor16 arrives.
                next.Backing=next.Backing.WithMovementAuxiliary(result.CopyAuxiliary());
                next.Waypoint=result.SelectedWaypoint;next.Ui+=result.Invalidations;score=result.CopyScoreRegion();effect=result.CopyEffectPool();movie=result.MovieCounter;random=result.RandomState;
            }
            var afterShared=next.Backing.CopyRegion();bool changed=false;for(int n=0;n<beforeShared.Length;n++)if(beforeShared[n]!=afterShared[n]){changed=true;break;}
            if(reversed){int ui=next.Ui;next=next.Advance(policy,NextSourceClock,false);next.Ui+=ui;}
            return new PracticeMixedMovementState(this,next,score,effect,schedule,movie,random,shipMoves,airMoves,wraps,changed);
        }
        MixedPrephaseWork CopyWork(){return new MixedPrephaseWork{Ships=CopyShipPool(),Fleets=CopyFleetPool(),Orders=CopyPrimaryOrders(),Candidates=CopyCandidateRegion(),Backing=Backing,Grid=Grid,Terrain=CopyTerrainPool(),Waypoint=SelectedWaypoint};}
        public byte[] CopyShipPool(){return(byte[])work.Ships.Clone();}public byte[] CopyFleetPool(){return(byte[])work.Fleets.Clone();}
        public byte[] CopyPrimaryOrders(){return(byte[])work.Orders.Clone();}public byte[] CopyCandidateRegion(){return(byte[])work.Candidates.Clone();}
        public byte[] CopyTerrainPool(){return(byte[])work.Terrain.Clone();}public byte[] CopyScoreRegion(){return(byte[])scores.Clone();}public byte[] CopyEffectPool(){return(byte[])effects.Clone();}
        internal byte[][] SnapshotForComparison(){var rows=new byte[1024000];for(int n=0;n<rows.Length;n++)rows[n]=0xcd;var p=work.Pools(rows);return new[]{p[0],p[1],p[2],p[3],p[4],p[5],CopyScoreRegion(),CopyEffectPool()};}
    }
}
