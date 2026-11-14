using System;
using System.IO;
namespace FleetLab {
    // Supplied fixed-fleet study: secondary419970 before416C40, then inactive
    // primary416090 and finite40E0D0 effects. This is not the AI caller/phase.
    public sealed class PracticeAircraftOrderStudyState {
        readonly PracticeTimedWeaponCatalog weapons;
        readonly PracticeUnitCatalog units;
        readonly PracticeFiringRangeTable ranges;
        readonly PracticeVisibility visibility;
        readonly PracticeFiringRateTable rates;
        readonly byte[] secondary;
        readonly int[] created;
        public PracticeTimedStudyState Combat { get; private set; }
        public int LastQueueReturnCode { get; private set; }
        public int LastQueueSlot { get; private set; }
        public int Polls { get; private set; }
        public int ConsumedOrders { get; private set; }
        public int RejectedOrders { get; private set; }
        public int CreatedObjects { get; private set; }
        public int LastConsumed { get; private set; }
        public int LastRejected { get; private set; }
        public int LastCreated { get { return created.Length; } }
        public int LastAttempts { get; private set; }
        public int LastDepletions { get; private set; }
        public byte[] CopySecondaryOrders() { return (byte[])secondary.Clone(); }
        public int[] CopyLastCreatedSlots() { return (int[])created.Clone(); }
        public int QueuedSources {
            get { int count=0;for(int n=0;n<1500;n++)if(BitConverter.ToInt16(secondary,n*52+8)!=-1)count++;return count; }
        }
        public static PracticeAircraftOrderStudyState ForStudy(PracticeTimedStudyState combat,PracticeTimedWeaponCatalog weapons,
                PracticeUnitCatalog units,PracticeFiringRangeTable ranges,PracticeVisibility visibility,PracticeFiringRateTable rates) {
            if(combat==null||weapons==null||units==null||ranges==null||visibility==null||rates==null)throw new ArgumentNullException("aircraft orders");
            if(!combat.IsAircraftStudy||combat.IncludesFiringOrders)throw new NotSupportedException("Aircraft secondary composition requires the explicit aircraft combat profile without ordinary primary firing.");
            byte[] primary=combat.PauseTimers.CopyShipTimers();
            for(int n=0;n<1500;n++)if(BitConverter.ToInt16(primary,n*52+8)!=-1)throw new NotSupportedException("Active primary orders are outside this aircraft secondary study.");
            var orders=new byte[78000];for(int n=0;n<1500;n++)for(int p=0;p<10;p++)BitConverter.GetBytes((short)-1).CopyTo(orders,n*52+8+p*2);
            return new PracticeAircraftOrderStudyState(combat,weapons,units,ranges,visibility,rates,orders);
        }
        PracticeAircraftOrderStudyState(PracticeTimedStudyState combat,PracticeTimedWeaponCatalog weapons,PracticeUnitCatalog units,
                PracticeFiringRangeTable ranges,PracticeVisibility visibility,PracticeFiringRateTable rates,byte[] orders) {
            Combat=combat;this.weapons=weapons;this.units=units;this.ranges=ranges;this.visibility=visibility;this.rates=rates;secondary=(byte[])orders.Clone();created=new int[0];LastQueueSlot=-1;
        }
        PracticeAircraftOrderStudyState(PracticeAircraftOrderStudyState before,PracticeTimedStudyState combat,byte[] orders,int[] slots) {
            Combat=combat;weapons=before.weapons;units=before.units;ranges=before.ranges;visibility=before.visibility;rates=before.rates;secondary=(byte[])orders.Clone();created=(int[])slots.Clone();
            LastQueueReturnCode=before.LastQueueReturnCode;LastQueueSlot=before.LastQueueSlot;Polls=before.Polls;ConsumedOrders=before.ConsumedOrders;RejectedOrders=before.RejectedOrders;CreatedObjects=before.CreatedObjects;
        }
        public PracticeFiringCheck CheckFiring(int source,int target,int weapon) {
            if(weapon<1||weapon>3)throw new ArgumentOutOfRangeException("weapon");
            return PracticeFiringEligibility.Evaluate(ranges,visibility,Combat.Objects.CopyShipPool(),Combat.Objects.CopyFleetPool(),source,target,weapon,0,-2);
        }
        // Raw419B20 enqueue. Admission happens when a due order is consumed.
        public PracticeAircraftOrderStudyState Queue(int source,int target,int weapon,int x,int y) {
            if(weapon<1||weapon>3)throw new ArgumentOutOfRangeException("weapon");
            var result=PracticeSecondaryFiringOrders.Enqueue(Combat.Objects.CopyShipPool(),secondary,source,weapon,target,x,y);
            var next=new PracticeAircraftOrderStudyState(this,Combat,result.CopyOrderPool(),new int[0]);next.LastQueueReturnCode=result.ReturnCode;next.LastQueueSlot=result.SelectedSlot;return next;
        }
        public PracticeAircraftOrderStudyState Clear(int source) {
            return new PracticeAircraftOrderStudyState(this,Combat,PracticeSecondaryFiringOrders.Clear(secondary,source),new int[0]);
        }
        public PracticeAircraftOrderStudyState AdvanceFrame(bool paused) {
            if(paused)return new PracticeAircraftOrderStudyState(this,Combat.AdvanceFrame(true),secondary,new int[0]);
            uint clock=unchecked(Combat.ClockMilliseconds+PracticeTimedStudyState.StepMilliseconds);
            var update=PracticeSecondaryFiringOrders.AdvanceWithAntiAircraftLaunch(ranges,visibility,rates,weapons,units,Combat.Objects,
                Combat.PauseTimers.CopyShipTimers(),secondary,clock,0,-2);
            var advanced=Combat.WithObjectsAndPrimaryOrders(update.Objects,update.CopyPrimaryOrders()).AdvanceFrame(false);
            var next=new PracticeAircraftOrderStudyState(this,advanced,update.CopyOrderPool(),update.CopyCreatedSlots());
            next.Polls++;next.ConsumedOrders+=update.ConsumedOrders;next.RejectedOrders+=update.RejectedOrders;next.CreatedObjects+=update.CreatedObjects;
            next.LastConsumed=update.ConsumedOrders;next.LastRejected=update.RejectedOrders;next.LastAttempts=update.AttemptedObjects;next.LastDepletions=update.LastRoundCallbacks;return next;
        }
    }
}
