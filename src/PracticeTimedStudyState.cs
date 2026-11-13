using System;

namespace FleetLab {
    // Copied-state composition for an explicit manual study. Fleets stay fixed;
    // supplied40ms deltas are numeric steps, not calibrated original frame pace.
    // The auxiliary pause-timer regions are an inactive fixture, not recovered
    // application initialization. Expiration, combat admission and impacts fail
    // explicitly rather than being replaced with invented damage behavior.
    public sealed class PracticeTimedStudyState {
        readonly PracticeTimedWeaponCatalog catalog;
        public PracticeTimedCreationState Objects { get; private set; }
        public OriginalPauseTimers PauseTimers { get; private set; }
        public uint ClockMilliseconds { get; private set; }
        public int MovementFrames { get; private set; }
        public int PausedFrames { get; private set; }
        public int LastStoppedAtRecord { get; private set; }
        public int LastCreatedSlot { get; private set; }
        public PracticeTimedCreationOutcome? LastCreationOutcome { get; private set; }
        public const uint StepMilliseconds = 40;

        public PracticeTimedStudyState(PracticeTimedWeaponCatalog weapons, byte[] ships, byte[] fleets, uint initialClock) {
            catalog = weapons; Objects = new PracticeTimedCreationState(weapons, new byte[21440], ships, fleets, -1);
            byte[] timerShips = new byte[78000], timestamps = new byte[64];
            for (int i = 0; i < 1500; i++) BitConverter.GetBytes((short)-1).CopyTo(timerShips, i * 52 + 8);
            for (int i = 0; i < timestamps.Length; i++) timestamps[i] = 255;
            PauseTimers = new OriginalPauseTimers(Objects.CopyTimedPool(), timerShips, new byte[16], timestamps, 0);
            ClockMilliseconds = initialClock; LastStoppedAtRecord = LastCreatedSlot = -1;
        }
        PracticeTimedStudyState(PracticeTimedStudyState before, PracticeTimedCreationState objects,
                                OriginalPauseTimers timers, uint clock) {
            catalog = before.catalog; Objects = objects; PauseTimers = timers; ClockMilliseconds = clock;
            MovementFrames = before.MovementFrames; PausedFrames = before.PausedFrames;
            LastCreatedSlot = before.LastCreatedSlot; LastCreationOutcome = before.LastCreationOutcome;
            LastStoppedAtRecord = before.LastStoppedAtRecord;
        }
        OriginalPauseTimers WithTimedPool(byte[] timed) {
            return new OriginalPauseTimers(timed, PauseTimers.CopyShipTimers(), PauseTimers.CopyFourCounters(),
                                           PauseTimers.CopyTimestamps(), PauseTimers.GlobalCounter);
        }
        public PracticeTimedStudyState Create(int weapon, int sourceShip, int targetFleet, int offsetX, int offsetY) {
            PracticeTimedCreationUpdate result = Objects.Create(weapon, sourceShip, targetFleet, offsetX, offsetY, ClockMilliseconds);
            var next = new PracticeTimedStudyState(this, result.State, WithTimedPool(result.State.CopyTimedPool()), ClockMilliseconds);
            next.LastCreationOutcome = result.Outcome; next.LastCreatedSlot = result.CreatedSlot;
            return next;
        }
        public PracticeTimedStudyState AdvanceFrame(bool paused) {
            uint nextClock = unchecked(ClockMilliseconds + StepMilliseconds);
            if (paused) {
                OriginalPauseTimers timers = PauseTimers.AdvancePausedFrame(1, StepMilliseconds);
                var objects = new PracticeTimedCreationState(catalog, timers.CopyTimedRecords(), Objects.CopyShipPool(), Objects.CopyFleetPool(), Objects.LastTargetFleet);
                var next = new PracticeTimedStudyState(this, objects, timers, nextClock);
                next.PausedFrames++; return next;
            }
            var motion = new PracticeTimedMotionState(Objects.CopyTimedPool(), Objects.CopyFleetPool()).Advance(nextClock, StepMilliseconds);
            var advanced = new PracticeTimedCreationState(catalog, motion.State.CopyTimedPool(), Objects.CopyShipPool(), motion.State.CopyFleetPool(), Objects.LastTargetFleet);
            var result = new PracticeTimedStudyState(this, advanced, WithTimedPool(motion.State.CopyTimedPool()), nextClock);
            result.MovementFrames++; result.LastStoppedAtRecord = motion.StoppedAtRecord; return result;
        }
    }
}
