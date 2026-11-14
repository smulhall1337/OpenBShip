using System;
using System.Drawing;
using System.IO;

namespace FleetLab {
    // Pinned RIP B9A8...6A569: the admitted caller fragment runs427670 twice,
    // then416C40,416090 and40E0D0. Native32 histories use one consistent raw
    // context:4E3868=1(scenario selector),4FB3D8=0,network0,localowner-2.
    // AI, attached objects, input, rendering, missions and the application
    // clock are absent. Supplied40ms steps are not complete original frames.
    public sealed class PracticeWorldStudyState {
        readonly byte[] terrain, auxiliary, secondary;
        public PracticeTimedStudyState Combat { get; private set; }
        public PracticeFleetScheduler Schedule { get; private set; }
        public int SelectedFleet { get; private set; }
        public int SelectedWaypoint { get; private set; }
        public int MovedFleetUpdates { get; private set; }
        public int LastMovedFleetUpdates { get; private set; }
        public PracticeMovementOutcome? LastSelectedOutcome { get; private set; }

        public PracticeWorldStudyState(PracticeTimedStudyState combat,byte[] terrainPool,byte[] auxiliaryData,
                byte[] secondaryPool,int selectedFleet,int selectedWaypoint,PracticeFleetScheduler schedule) {
            if(combat==null||schedule==null)throw new ArgumentNullException("study state");
            if(!combat.IncludesFiringOrders)throw new NotSupportedException("Movement with combat requires the firing-data study.");
            // Shared constructors validate complete extents, slot identities,
            // selection and the inactive secondary-pool boundary.
            var movement=new PracticeMovementState(combat.Objects.CopyFleetPool(),terrainPool,auxiliaryData,selectedFleet,selectedWaypoint);
            new PracticeFleetBatchState(movement,schedule,secondaryPool);
            Combat=combat;Schedule=schedule;SelectedFleet=selectedFleet;SelectedWaypoint=selectedWaypoint;
            terrain=movement.CopyTerrainPool();auxiliary=movement.CopyAuxiliary();secondary=(byte[])secondaryPool.Clone();
        }
        public static PracticeWorldStudyState ForStudy(PracticeTimedStudyState combat,PracticeDeployment deployment,int selectedFleet) {
            if(deployment==null||!deployment.IncludesMapObjects||deployment.Terrain==null)
                throw new InvalidDataException("Movement with combat requires the first Practice startup terrain.");
            return new PracticeWorldStudyState(combat,deployment.Terrain.CopyRecords(),new byte[PracticeMovementState.AuxiliaryBytes],
                new byte[125*492],selectedFleet,-1,new PracticeFleetScheduler(0,0));
        }
        PracticeWorldStudyState Copy(PracticeTimedStudyState combat,byte[] aux,int point,PracticeFleetScheduler schedule) {
            var next=new PracticeWorldStudyState(combat,terrain,aux,secondary,SelectedFleet,point,schedule);
            next.MovedFleetUpdates=MovedFleetUpdates;next.LastMovedFleetUpdates=LastMovedFleetUpdates;next.LastSelectedOutcome=LastSelectedOutcome;return next;
        }
        public PracticeWorldStudyState WithCombat(PracticeTimedStudyState combat) { return Copy(combat,auxiliary,SelectedWaypoint,Schedule); }
        public byte[] CopyAuxiliary() { return (byte[])auxiliary.Clone(); }
        public byte[] CopyTerrainPool() { return (byte[])terrain.Clone(); }
        public byte[] CopySecondaryPool() { return (byte[])secondary.Clone(); }
        public PracticeWorldStudyState AdvanceFrame(bool paused) {
            if(paused) {
                var held=Copy(Combat.AdvanceFrame(true),auxiliary,SelectedWaypoint,Schedule);
                held.LastMovedFleetUpdates=0;held.LastSelectedOutcome=null;return held;
            }
            // Recreate from current combat fleets every poll: damage and fleet
            // retirement must never be overwritten by an older movement copy.
            var movement=new PracticeMovementState(Combat.Objects.CopyFleetPool(),terrain,auxiliary,SelectedFleet,SelectedWaypoint);
            var batch=new PracticeFleetBatchState(movement,Schedule,secondary).AdvanceNormalPoll();
            var combat=Combat.WithFleetPool(batch.State.Movement.CopyFleetPool()).AdvanceFrame(false);
            var next=Copy(combat,batch.State.Movement.CopyAuxiliary(),batch.State.Movement.SelectedWaypoint,batch.State.Schedule);
            next.LastMovedFleetUpdates=0;next.LastSelectedOutcome=null;
            foreach(var item in batch.FleetUpdates) {
                if(item.Update.Before!=item.Update.After)next.LastMovedFleetUpdates++;
                if(item.Slot==SelectedFleet)next.LastSelectedOutcome=item.Update.Outcome;
            }
            next.MovedFleetUpdates+=next.LastMovedFleetUpdates;return next;
        }
        PracticeWaypointQueue Queue(Scenario scene) {
            var pool=Combat.Objects.CopyFleetPool();int at=SelectedFleet*492;
            if(BitConverter.ToInt32(pool,at)!=1||BitConverter.ToInt16(pool,at+6)!=0)
                throw new NotSupportedException("Waypoint controls require a live player 1 kind-1 fleet.");
            var record=new byte[492];Array.Copy(pool,at,record,0,492);
            return new PracticeWaypointQueue(record,scene,1,SelectedWaypoint);
        }
        PracticeWorldStudyState ApplyQueue(PracticeWaypointQueue queue) {
            var movement=new PracticeMovementState(Combat.Objects.CopyFleetPool(),terrain,auxiliary,SelectedFleet,SelectedWaypoint)
                .WithFleetRecord(SelectedFleet,queue.CopyRecord(),queue.Selected);
            return Copy(Combat.WithFleetPool(movement.CopyFleetPool()),auxiliary,queue.Selected,Schedule);
        }
        public PracticeWorldStudyState AppendOrSelectWaypoint(Scenario scene,Point world,out PracticeWaypointResult result) {
            var queue=Queue(scene);result=queue.AppendOrSelect(world);return ApplyQueue(queue);
        }
        public PracticeWorldStudyState ClearWaypoints(Scenario scene) { var queue=Queue(scene);queue.Clear();return ApplyQueue(queue); }
        public PracticeWorldStudyState DeselectWaypoint(Scenario scene) { var queue=Queue(scene);queue.Deselect();return ApplyQueue(queue); }
    }
}
