using System;
using System.IO;
using System.Collections.Generic;

namespace FleetLab {
    public sealed class PracticeFleetBodyUpdate {
        public int Slot { get; private set; }
        public PracticeMovementUpdate Update { get; private set; }
        internal PracticeFleetBodyUpdate(int slot, PracticeMovementUpdate update) { Slot = slot; Update = update; }
    }
    public sealed class PracticeFleetBatchAdvance {
        public PracticeFleetBatchState State { get; private set; }
        public IList<PracticeFleetBodyUpdate> FleetUpdates { get; private set; }
        public int BatchCount { get; private set; }
        internal PracticeFleetBatchAdvance(PracticeFleetBatchState state, List<PracticeFleetBodyUpdate> updates, int count) {
            State = state; FleetUpdates = updates.AsReadOnly(); BatchCount = count;
        }
    }

    // B9A8...6A569: bounded427670 whole-batch reconstruction. Network0/type1,
    // kind1/state0/task0/flags2or3; stationary kind11 and source early skips.
    // Secondary records must all be inactive. AI/input/render/elapsed time
    // are outside this immutable state. Unsupported batches publish no changes.
    public sealed class PracticeFleetBatchState {
        public const int SecondaryBytes = 125 * PracticeMovementState.FleetBytes;
        readonly byte[] secondary;
        public PracticeMovementState Movement { get; private set; }
        public PracticeFleetScheduler Schedule { get; private set; }
        public PracticeFleetBatchState(PracticeMovementState movement, PracticeFleetScheduler schedule, byte[] secondaryPool) {
            if (movement == null) throw new ArgumentNullException("movement");
            if (schedule == null) throw new ArgumentNullException("schedule");
            if (secondaryPool == null || secondaryPool.Length != SecondaryBytes)
                throw new InvalidDataException("Batch stepping requires the complete secondary pool.");
            for (int slot = 0; slot < 125; slot++)
                if (BitConverter.ToInt32(secondaryPool, slot * PracticeMovementState.FleetBytes) != 0)
                    throw new NotSupportedException("Active secondary objects are outside the recovered batch subset.");
            Movement = movement; Schedule = schedule; secondary = (byte[])secondaryPool.Clone();
        }
        PracticeFleetBatchState(PracticeMovementState movement, PracticeFleetScheduler schedule, byte[] ownedSecondary, bool share) {
            Movement = movement; Schedule = schedule; secondary = ownedSecondary; // Never mutated; public copies are independent.
        }
        public byte[] CopySecondaryPool() { return (byte[])secondary.Clone(); }
        public PracticeFleetBatchAdvance AdvanceBatch() {
            PracticeFleetBatch plan = Schedule.TakeBatch();
            PracticeMovementState current = Movement;
            var updates = new List<PracticeFleetBodyUpdate>();
            foreach (int slot in plan.CopySlots()) {
                byte[] record = current.CopyFleetRecord(slot); int kind = BitConverter.ToInt32(record, 0);
                if (kind == 1) {
                    PracticeMovementUpdate update = current.Step(slot);
                    current = update.State; updates.Add(new PracticeFleetBodyUpdate(slot, update));
                } else if (kind == 0 || kind == 2 || kind == 6 || kind == 7 || kind == 8) {
                    current = current.ResetCollision(slot);
                } else if (kind == 11 && BitConverter.ToInt32(record, 472) == 0 &&
                           BitConverter.ToInt32(record, 476) == 0 && record[485] == 0 &&
                           BitConverter.ToInt16(record, 40) == -1) {
                    current = current.ResetCollision(slot);
                } else {
                    throw new NotSupportedException("Fleet " + (slot + 1) + " is outside the recovered batch subset; batch preserved.");
                }
            }
            return new PracticeFleetBatchAdvance(new PracticeFleetBatchState(current, plan.Next, secondary, true), updates, 1);
        }
        public PracticeFleetBatchAdvance AdvanceNormalPoll() {
            PracticeFleetBatchAdvance first = AdvanceBatch(), second = first.State.AdvanceBatch();
            var updates = new List<PracticeFleetBodyUpdate>();
            foreach (var update in first.FleetUpdates) updates.Add(update);
            foreach (var update in second.FleetUpdates) updates.Add(update);
            return new PracticeFleetBatchAdvance(second.State, updates, 2);
        }
    }
}
