using System;
using System.Collections.ObjectModel;
namespace FleetLab
{
    internal sealed partial class FleetServiceCore
    {
        // Clock events are separate from the v0.95 coupled manual action.
        // In the source phase subset a worker can precede the first task visit.
        internal FleetServiceCore ApplySourcePhase(bool dispatch,bool support)
        {
            if(mode!=PracticeFleetServiceMode.InitializedDispatcher)throw new InvalidOperationException("Source phases require initialized dispatcher state.");
            if(dispatch&&support)throw new InvalidOperationException("Dispatcher and support phases cannot coincide.");
            if(!dispatch&&!support)return this;
            var next=Copy();
            if(dispatch){next.VisitAppliedHandler();next.taskVisits++;}
            if(support){next.Worker();next.workerPasses++;}
            return next;
        }
    }

    // B9A8...6A569: pause410D4A/increment410D55,task gate41D64C,
    // support gate410E57. Native511 executes these original fragments with
    // whole41C750/4297F0. Other world/AI/policy/AA/movement work is excluded.
    // This API exposes read-only service data and admits only captured inputs.
    public sealed class PracticeFleetServicePhaseClock
    {
        readonly FleetServiceCore core;
        readonly uint initialCounter,counter;
        readonly int polls,pausedPolls;
        static readonly ReadOnlyCollection<string> cases=Array.AsReadOnly(new[]{"30-1-ordinary","30-2-firstworse","30-1-blocked"});
        static readonly ReadOnlyCollection<uint> counters=Array.AsReadOnly(new[]{0u,12u,31u,0xfffffffeu});
        public const int MaximumPolls=384;
        public static ReadOnlyCollection<string> CaseIds {get{return cases;}}
        public static ReadOnlyCollection<uint> InitialCounters {get{return counters;}}
        public string CaseId {get{return core.CaseId;}}
        public int InitialTaskKind {get{return core.InitialTaskKind;}}
        public uint InitialCounter {get{return initialCounter;}}
        public uint PhaseCounter {get{return counter;}}
        public int Polls {get{return polls;}}
        public int PausedPolls {get{return pausedPolls;}}
        public int RemainingPolls {get{return MaximumPolls-polls;}}
        public int TaskVisits {get{return core.TaskVisits;}}
        public int WorkerCalls {get{return core.WorkerPasses;}}
        public int TaskKind {get{return core.TaskKind;}}
        public int SourceFleet {get{return core.SourceFleet;}}
        public int SourceKind {get{return core.SourceKind;}}
        public int SourceState {get{return core.SourceState;}}
        public int SelectedFleet {get{return core.SelectionFleet;}}
        public int SelectedUnit {get{return core.SelectionUnit;}}
        public int SelectedWaypoint {get{return core.SelectionWaypoint;}}
        public int TextRequests {get{return core.TextRequests;}}
        public int MapRequests {get{return core.MapRequests;}}
        public int RefreshRequests {get{return core.RefreshRequests;}}
        public int RouteUiRequests {get{return core.RouteUiRequests;}}
        public int NextDispatcherIn {get{return 32-(int)(counter&31);}}
        public int NextWorkerIn {get{int delta=(13-(int)(counter&63)+64)&63;return delta==0?64:delta;}}
        PracticeFleetServicePhaseClock(FleetServiceCore core,uint initialCounter,uint counter,int polls,int pausedPolls)
        {this.core=core;this.initialCounter=initialCounter;this.counter=counter;this.polls=polls;this.pausedPolls=pausedPolls;}
        public static PracticeFleetServicePhaseClock Create(string programs,PracticeMapStartup startup,string caseId,int taskKind,uint initialCounter)
        {
            if(!cases.Contains(caseId))throw new ArgumentException("Choose one of the three captured source-phase cases.","caseId");
            if(!counters.Contains(initialCounter))throw new ArgumentOutOfRangeException("initialCounter","Choose a captured counter:0,12,31 or4294967294.");
            var core=FleetServiceCore.Create(programs,startup,caseId,taskKind,PracticeFleetServiceMode.InitializedDispatcher);
            return new PracticeFleetServicePhaseClock(core,initialCounter,initialCounter,0,0);
        }
        public byte[] CopyShipPool(){return core.CopyShips();}
        public byte[] CopyFleetPool(){return core.CopyFleets();}
        public byte[] CopyTaskRecord(){return core.CopyTask();}
        public int[] CopyOriginalUnits(){return core.CopyOriginalUnits();}
        public PracticeFleetServicePhaseClock Advance(bool paused)
        {
            if(polls>=MaximumPolls)throw new InvalidOperationException("The captured384-poll window is complete;reset this copy.");
            uint phase=paused?counter:unchecked(counter+1);
            var next=core.ApplySourcePhase(!paused&&(phase&31)==0,!paused&&(phase&63)==13);
            return new PracticeFleetServicePhaseClock(next,initialCounter,phase,polls+1,pausedPolls+(paused?1:0));
        }
        public PracticeFleetServicePhaseClock AdvanceMany(int count,bool paused)
        {
            if(count<0||count>RemainingPolls)throw new ArgumentOutOfRangeException("count","Choose a count inside the remaining captured poll window.");
            var clock=this;for(int n=0;n<count;n++)clock=clock.Advance(paused);return clock;
        }
    }
}
