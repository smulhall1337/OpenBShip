using System;

namespace FleetLab {
    // Copied-state composition for an explicit manual study. Fleets stay fixed;
    // supplied40ms deltas are numeric steps, not calibrated original frame pace.
    // The auxiliary pause-timer regions are an inactive fixture, not recovered
    // application initialization. The optional extended constructor admits
    // verified last-round and shared nonship expiry data. Unsupported combat
    // branches fail explicitly rather than inventing damage behavior.
    public sealed class PracticeTimedStudyState {
        readonly PracticeTimedWeaponCatalog catalog;
        readonly PracticeUnitCatalog units;
        readonly byte[] visuals;
        readonly PracticeDefenseTables impactTables;
        readonly PracticeDestructionTables destructionTables;
        byte[] scoreRegion;
        PracticeVisualPool effects;
        public PracticeTimedCreationState Objects { get; private set; }
        public OriginalPauseTimers PauseTimers { get; private set; }
        public uint ClockMilliseconds { get; private set; }
        public int MovementFrames { get; private set; }
        public int PausedFrames { get; private set; }
        public int LastStoppedAtRecord { get; private set; }
        public int LastCreatedSlot { get; private set; }
        public PracticeTimedCreationOutcome? LastCreationOutcome { get; private set; }
        public bool IncludesDataExpiry { get { return visuals != null; } }
        public uint RandomState { get; private set; }
        public int ExpiredObjects { get; private set; }
        public int DroppedEffects { get; private set; }
        public int RandomCalls { get; private set; }
        public int ExpiryCallbacks { get; private set; }
        public PracticeImpactOutcome? LastImpactOutcome { get; private set; }
        public int LastVictimShip { get; private set; }
        public int LastImpactPoints { get; private set; }
        public int LastScatterAttempts { get; private set; }
        public bool IncludesShipImpacts { get { return impactTables != null; } }
        public bool IncludesDestruction { get { return destructionTables != null; } }
        public int DestroyedUnits { get; private set; }
        public bool LastVictimDestroyed { get; private set; }
        public int MovieCounter { get; private set; }
        int[] lastLaunchSlots = new int[0];
        public int LastLaunchCount { get { return lastLaunchSlots.Length; } }
        public int LastLaunchAttempts { get; private set; }
        public int LastLaunchDepletions { get; private set; }
        public int[] CopyLastLaunchSlots() { return (int[])lastLaunchSlots.Clone(); }
        public byte[] CopyScoreRegion() {
            if(scoreRegion==null)throw new InvalidOperationException("This study does not include ship-impact scores.");
            return (byte[])scoreRegion.Clone();
        }
        public int ActiveEffects { get { return effects == null ? 0 : effects.Count; } }
        public byte[] CopyEffectPool() {
            if (effects == null) throw new InvalidOperationException("This study does not include visual records.");
            return effects.CopyEffectPool();
        }
        public const uint StepMilliseconds = 40;

        public PracticeTimedStudyState(PracticeTimedWeaponCatalog weapons, byte[] ships, byte[] fleets, uint initialClock) {
            catalog = weapons; Objects = new PracticeTimedCreationState(weapons, new byte[21440], ships, fleets, -1);
            byte[] timerShips = new byte[78000], timestamps = new byte[64];
            for (int i = 0; i < 1500; i++) BitConverter.GetBytes((short)-1).CopyTo(timerShips, i * 52 + 8);
            for (int i = 0; i < timestamps.Length; i++) timestamps[i] = 255;
            PauseTimers = new OriginalPauseTimers(Objects.CopyTimedPool(), timerShips, new byte[16], timestamps, 0);
            ClockMilliseconds = initialClock; LastStoppedAtRecord = LastCreatedSlot = LastVictimShip = -1;
        }
        // Opt-in composition of verified data branches. The old constructor
        // remains the bounded future-only comparison baseline. Supplying a
        // random seed and inactive auxiliary pools does not reconstruct the
        // original application seed selection, normal firing or mission loop.
        public PracticeTimedStudyState(PracticeTimedWeaponCatalog weapons, PracticeUnitCatalog unitCatalog,
                byte[] visualRecords, byte[] ships, byte[] fleets, uint initialClock, uint randomState)
                : this(weapons, ships, fleets, initialClock) {
            if (unitCatalog == null) throw new ArgumentNullException("unitCatalog");
            effects = PracticeVisualPool.Empty(visualRecords);
            units = unitCatalog; visuals = (byte[])visualRecords.Clone(); RandomState = randomState;
        }
        public PracticeTimedStudyState(PracticeTimedWeaponCatalog weapons, PracticeUnitCatalog unitCatalog,
                byte[] visualRecords, byte[] ships, byte[] fleets, uint initialClock, uint randomState,
                PracticeDefenseTables sourceTables) : this(weapons,unitCatalog,visualRecords,ships,fleets,initialClock,randomState) {
            if(sourceTables==null)throw new ArgumentNullException("sourceTables");
            impactTables=sourceTables;scoreRegion=new byte[176];BitConverter.GetBytes((short)-2).CopyTo(scoreRegion,172);
        }
        // Explicit offscreen mode0/playercount0 destruction composition. This
        // models data and selection counters, without live media or missions.
        public PracticeTimedStudyState(PracticeTimedWeaponCatalog weapons, PracticeUnitCatalog unitCatalog,
                byte[] visualRecords, byte[] ships, byte[] fleets, uint initialClock, uint randomState,
                PracticeDefenseTables sourceTables, PracticeDestructionTables destruction)
                : this(weapons,unitCatalog,visualRecords,ships,fleets,initialClock,randomState,sourceTables) {
            if(destruction==null)throw new ArgumentNullException("destruction");
            destructionTables=destruction;
        }
        PracticeTimedStudyState(PracticeTimedStudyState before, PracticeTimedCreationState objects,
                                OriginalPauseTimers timers, uint clock) {
            catalog = before.catalog; Objects = objects; PauseTimers = timers; ClockMilliseconds = clock;
            units = before.units; visuals = before.visuals; effects = before.effects;
            impactTables=before.impactTables;scoreRegion=before.scoreRegion;ExpiryCallbacks=before.ExpiryCallbacks;
            destructionTables=before.destructionTables;DestroyedUnits=before.DestroyedUnits;LastVictimDestroyed=before.LastVictimDestroyed;MovieCounter=before.MovieCounter;
            lastLaunchSlots=before.lastLaunchSlots;LastLaunchAttempts=before.LastLaunchAttempts;LastLaunchDepletions=before.LastLaunchDepletions;
            LastImpactOutcome=before.LastImpactOutcome;LastVictimShip=before.LastVictimShip;LastImpactPoints=before.LastImpactPoints;LastScatterAttempts=before.LastScatterAttempts;
            RandomState = before.RandomState; ExpiredObjects = before.ExpiredObjects;
            DroppedEffects = before.DroppedEffects; RandomCalls = before.RandomCalls;
            MovementFrames = before.MovementFrames; PausedFrames = before.PausedFrames;
            LastCreatedSlot = before.LastCreatedSlot; LastCreationOutcome = before.LastCreationOutcome;
            LastStoppedAtRecord = before.LastStoppedAtRecord;
        }
        OriginalPauseTimers WithTimedPool(byte[] timed, byte[] orderPool = null) {
            return new OriginalPauseTimers(timed, orderPool ?? PauseTimers.CopyShipTimers(), PauseTimers.CopyFourCounters(),
                                           PauseTimers.CopyTimestamps(), PauseTimers.GlobalCounter);
        }
        public PracticeTimedStudyState Create(int weapon, int sourceShip, int targetFleet, int offsetX, int offsetY) {
            PracticeTimedCreationUpdate result = IncludesDataExpiry ?
                Objects.CreateWithDepletion(weapon, sourceShip, targetFleet, offsetX, offsetY, ClockMilliseconds, units, PauseTimers.CopyShipTimers()) :
                Objects.Create(weapon, sourceShip, targetFleet, offsetX, offsetY, ClockMilliseconds);
            var next = new PracticeTimedStudyState(this, result.State,
                WithTimedPool(result.State.CopyTimedPool(), IncludesDataExpiry ? result.CopyOrderPool() : null), ClockMilliseconds);
            next.LastCreationOutcome = result.Outcome; next.LastCreatedSlot = result.CreatedSlot;
            next.lastLaunchSlots=new int[0];next.LastLaunchAttempts=next.LastLaunchDepletions=0;
            return next;
        }
        // Explicit source launch pattern; existing Create remains one object.
        // Creation and any last-round work complete before publishing the state.
        public PracticeTimedStudyState Launch(int weapon, int sourceShip, int targetFleet, int offsetX, int offsetY) {
            if(!IncludesDataExpiry)throw new NotSupportedException("Source launch patterns require the extended data study.");
            var result=PracticeTimedLaunch.Apply(catalog,units,Objects.CopyTimedPool(),Objects.CopyShipPool(),Objects.CopyFleetPool(),
                PauseTimers.CopyShipTimers(),weapon,sourceShip,targetFleet,offsetX,offsetY,ClockMilliseconds,Objects.LastTargetFleet);
            var next=new PracticeTimedStudyState(this,result.State,WithTimedPool(result.State.CopyTimedPool(),result.CopyOrderPool()),ClockMilliseconds);
            next.lastLaunchSlots=result.CopyCreatedSlots();next.LastLaunchAttempts=result.AttemptedObjects;next.LastLaunchDepletions=result.LastRoundCallbacks;
            next.LastCreatedSlot=next.LastLaunchCount==0?-1:next.lastLaunchSlots[next.LastLaunchCount-1];
            bool full=true;byte[] timed=result.State.CopyTimedPool();for(int at=0;at<timed.Length;at+=268)if(BitConverter.ToInt32(timed,at)==0){full=false;break;}
            next.LastCreationOutcome=next.LastLaunchCount>0?PracticeTimedCreationOutcome.Created:
                result.AttemptedObjects>0&&full?PracticeTimedCreationOutcome.PoolFull:PracticeTimedCreationOutcome.NoAmmunition;
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
            if(IncludesShipImpacts) {
                var update=new PracticeTimedImpactState(units,impactTables,visuals,Objects.CopyTimedPool(),Objects.CopyShipPool(),Objects.CopyFleetPool(),effects.CopyEffectPool(),scoreRegion,RandomState,destructionTables,MovieCounter).Advance(nextClock,StepMilliseconds);
                var nextEffects=new PracticeVisualPool(update.State.CopyEffectPool(),visuals).Advance();
                var updatedObjects=new PracticeTimedCreationState(catalog,update.State.CopyTimedPool(),update.State.CopyShipPool(),update.State.CopyFleetPool(),Objects.LastTargetFleet);
                var next=new PracticeTimedStudyState(this,updatedObjects,WithTimedPool(update.State.CopyTimedPool()),nextClock);
                next.effects=nextEffects;next.scoreRegion=update.State.CopyScoreRegion();next.RandomState=update.State.RandomState;
                next.ExpiredObjects+=update.RetiredRecords;next.ExpiryCallbacks+=update.ExpiryCallbacks;next.DroppedEffects+=update.DroppedEffects;next.RandomCalls+=update.RandomCalls;
                next.DestroyedUnits+=update.DestroyedUnits;next.MovieCounter=update.State.MovieCounter;
                if(update.LastImpactOutcome.HasValue){next.LastImpactOutcome=update.LastImpactOutcome;next.LastVictimShip=update.LastVictimShip;next.LastImpactPoints=update.LastPoints;next.LastScatterAttempts=update.LastScatterAttempts;next.LastVictimDestroyed=update.LastVictimDestroyed;}
                next.MovementFrames++;next.LastStoppedAtRecord=update.StoppedAtRecord;return next;
            }
            if (IncludesDataExpiry) {
                var update = new PracticeTimedExpiryState(Objects.CopyTimedPool(), Objects.CopyFleetPool(), effects.CopyEffectPool(), visuals, RandomState)
                    .Advance(nextClock, StepMilliseconds);
                // In the admitted inactive-order fixture, source416090 between
                // 416C40 and40E0D0 changes nothing. Advance new effects in the
                // same supplied update in which the expiry created them.
                var nextEffects = new PracticeVisualPool(update.State.CopyEffectPool(), visuals).Advance();
                var updatedObjects = new PracticeTimedCreationState(catalog, update.State.CopyTimedPool(), Objects.CopyShipPool(), update.State.CopyFleetPool(), Objects.LastTargetFleet);
                var next = new PracticeTimedStudyState(this, updatedObjects, WithTimedPool(update.State.CopyTimedPool()), nextClock);
                next.effects = nextEffects; next.RandomState = update.State.RandomState;
                next.ExpiredObjects += update.ExpiredRecords; next.DroppedEffects += update.DroppedEffects; next.RandomCalls += update.RandomCalls;
                next.ExpiryCallbacks+=update.ExpiredRecords;
                next.MovementFrames++; next.LastStoppedAtRecord = update.StoppedAtRecord; return next;
            }
            var motion = new PracticeTimedMotionState(Objects.CopyTimedPool(), Objects.CopyFleetPool()).Advance(nextClock, StepMilliseconds);
            var advanced = new PracticeTimedCreationState(catalog, motion.State.CopyTimedPool(), Objects.CopyShipPool(), motion.State.CopyFleetPool(), Objects.LastTargetFleet);
            var result = new PracticeTimedStudyState(this, advanced, WithTimedPool(motion.State.CopyTimedPool()), nextClock);
            result.MovementFrames++; result.LastStoppedAtRecord = motion.StoppedAtRecord; return result;
        }
    }
}
