using System;
using System.Drawing;
using System.IO;

namespace FleetLab {
    // Pinned RIP B9A8...6A569: the admitted caller fragment runs427670 twice,
    // then419970,416C40,416090 and40E0D0. Native histories use consistent raw
    // context:4E3868=1(scenario selector),4FB3D8=0,network0,localowner-2.
    // AI, attached objects, input, rendering, missions and the application
    // clock are absent. Supplied40ms steps are not complete original frames.
    public sealed class PracticeWorldStudyState {
        readonly byte[] terrain, auxiliary, secondary, secondaryOrders;
        public PracticeTimedStudyState Combat { get; private set; }
        public PracticeFleetScheduler Schedule { get; private set; }
        public int SelectedFleet { get; private set; }
        public int SelectedWaypoint { get; private set; }
        public int MovedFleetUpdates { get; private set; }
        public int LastMovedFleetUpdates { get; private set; }
        public PracticeMovementOutcome? LastSelectedOutcome { get; private set; }
        public int SecondaryConsumedOrders { get; private set; }
        public int SecondaryRejectedOrders { get; private set; }
        public int SecondaryCreatedObjects { get; private set; }
        public int LastSecondaryReturnCode { get; private set; }
        public int LastSecondarySlot { get; private set; }
        public int PendingSecondaryUnits { get { int count=0;for(int at=8;at<secondaryOrders.Length;at+=52)if(BitConverter.ToInt16(secondaryOrders,at)!=-1)count++;return count; } }

        public PracticeWorldStudyState(PracticeTimedStudyState combat,byte[] terrainPool,byte[] auxiliaryData,
                byte[] secondaryPool,int selectedFleet,int selectedWaypoint,PracticeFleetScheduler schedule,byte[] secondaryOrderPool=null) {
            if(combat==null||schedule==null)throw new ArgumentNullException("study state");
            if(!combat.IncludesFiringOrders)throw new NotSupportedException("Movement with combat requires the firing-data study.");
            // Shared constructors validate complete extents, slot identities,
            // selection and the inactive secondary-pool boundary.
            var movement=new PracticeMovementState(combat.Objects.CopyFleetPool(),terrainPool,auxiliaryData,selectedFleet,selectedWaypoint);
            new PracticeFleetBatchState(movement,schedule,secondaryPool);
            Combat=combat;Schedule=schedule;SelectedFleet=selectedFleet;SelectedWaypoint=selectedWaypoint;
            terrain=movement.CopyTerrainPool();auxiliary=movement.CopyAuxiliary();secondary=(byte[])secondaryPool.Clone();
            if(secondaryOrderPool!=null&&secondaryOrderPool.Length!=78000)throw new InvalidDataException("World study requires the complete secondary order pool.");
            secondaryOrders=secondaryOrderPool==null?new byte[78000]:(byte[])secondaryOrderPool.Clone();
            if(secondaryOrderPool==null)for(int source=0;source<1500;source++)for(int n=0;n<10;n++)BitConverter.GetBytes((short)-1).CopyTo(secondaryOrders,source*52+8+n*2);
            LastSecondaryReturnCode=LastSecondarySlot=-1;
        }
        public static PracticeWorldStudyState ForStudy(PracticeTimedStudyState combat,PracticeDeployment deployment,int selectedFleet) {
            if(deployment==null||!deployment.IncludesMapObjects||deployment.Terrain==null)
                throw new InvalidDataException("Movement with combat requires the first Practice startup terrain.");
            return new PracticeWorldStudyState(combat,deployment.Terrain.CopyRecords(),new byte[PracticeMovementState.AuxiliaryBytes],
                new byte[125*492],selectedFleet,-1,new PracticeFleetScheduler(0,0));
        }
        PracticeWorldStudyState Copy(PracticeTimedStudyState combat,byte[] aux,int point,PracticeFleetScheduler schedule,byte[] orders=null) {
            var next=new PracticeWorldStudyState(combat,terrain,aux,secondary,SelectedFleet,point,schedule,orders??secondaryOrders);
            next.MovedFleetUpdates=MovedFleetUpdates;next.LastMovedFleetUpdates=LastMovedFleetUpdates;next.LastSelectedOutcome=LastSelectedOutcome;
            next.SecondaryConsumedOrders=SecondaryConsumedOrders;next.SecondaryRejectedOrders=SecondaryRejectedOrders;next.SecondaryCreatedObjects=SecondaryCreatedObjects;
            next.LastSecondaryReturnCode=LastSecondaryReturnCode;next.LastSecondarySlot=LastSecondarySlot;return next;
        }
        public PracticeWorldStudyState WithCombat(PracticeTimedStudyState combat) { return Copy(combat,auxiliary,SelectedWaypoint,Schedule); }
        public byte[] CopyAuxiliary() { return (byte[])auxiliary.Clone(); }
        public byte[] CopyTerrainPool() { return (byte[])terrain.Clone(); }
        public byte[] CopySecondaryPool() { return (byte[])secondary.Clone(); }
        public byte[] CopySecondaryOrders() { return (byte[])secondaryOrders.Clone(); }
        public PracticeWorldStudyState QueueSecondaryOrder(int weapon,int source,int target,int x,int y) {
            var queued=PracticeSecondaryFiringOrders.Enqueue(Combat.Objects.CopyShipPool(),secondaryOrders,source,weapon,target,x,y);
            var next=Copy(Combat,auxiliary,SelectedWaypoint,Schedule,queued.CopyOrderPool());next.LastSecondaryReturnCode=queued.ReturnCode;next.LastSecondarySlot=queued.SelectedSlot;return next;
        }
        public PracticeWorldStudyState ClearSecondaryOrders(int source) {
            return Copy(Combat,auxiliary,SelectedWaypoint,Schedule,PracticeSecondaryFiringOrders.Clear(secondaryOrders,source));
        }
        public PracticeWorldStudyState ProcessSecondaryOrders() {
            var update=Combat.ProcessSecondaryOrders(secondaryOrders,Combat.ClockMilliseconds);
            return FromSecondary(update,auxiliary,SelectedWaypoint,Schedule);
        }
        PracticeWorldStudyState FromSecondary(PracticeSecondaryStudyUpdate update,byte[] aux,int point,PracticeFleetScheduler schedule) {
            var next=Copy(update.Combat,aux,point,schedule,update.Firing.CopyOrderPool());
            next.SecondaryConsumedOrders+=update.Firing.ConsumedOrders;next.SecondaryRejectedOrders+=update.Firing.RejectedOrders;next.SecondaryCreatedObjects+=update.Firing.CreatedObjects;return next;
        }
        public PracticeWorldStudyState AdvanceFrame(bool paused) { return AdvanceFrame(paused,true); }
        public PracticeWorldStudyState AdvanceFrame(bool paused,bool runMovement) {
            if(paused) {
                var held=Copy(Combat.AdvanceFrame(true),auxiliary,SelectedWaypoint,Schedule);
                // Original411313 compensates PRIMARY deadlines only. The
                // secondary pool4E4310 stays byte-for-byte unchanged here.
                held.LastMovedFleetUpdates=0;held.LastSelectedOutcome=null;return held;
            }
            // Recreate from current combat fleets every poll: damage and fleet
            // retirement must never be overwritten by an older movement copy.
            var movement=new PracticeMovementState(Combat.Objects.CopyFleetPool(),terrain,auxiliary,SelectedFleet,SelectedWaypoint);
            var batch=runMovement?new PracticeFleetBatchState(movement,Schedule,secondary).AdvanceNormalPoll():null;
            var moved=batch==null?movement:batch.State.Movement;var schedule=batch==null?Schedule:batch.State.Schedule;
            var combat=Combat.WithFleetPool(moved.CopyFleetPool());
            var secondaryUpdate=combat.ProcessSecondaryOrders(secondaryOrders,unchecked(combat.ClockMilliseconds+PracticeTimedStudyState.StepMilliseconds));
            var next=FromSecondary(secondaryUpdate,moved.CopyAuxiliary(),moved.SelectedWaypoint,schedule);
            next.Combat=secondaryUpdate.Combat.AdvanceFrame(false);
            next.LastMovedFleetUpdates=0;next.LastSelectedOutcome=null;
            if(batch!=null)foreach(var item in batch.FleetUpdates) {
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
