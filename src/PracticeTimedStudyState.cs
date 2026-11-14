using System;

namespace FleetLab {
    internal sealed class PracticeSecondaryStudyUpdate {
        internal PracticeTimedStudyState Combat { get; private set; }
        internal PracticeSecondaryOrderUpdate Firing { get; private set; }
        internal PracticeSecondaryStudyUpdate(PracticeTimedStudyState combat,PracticeSecondaryOrderUpdate firing) { Combat=combat;Firing=firing; }
    }
    // Copied-state composition for an explicit manual study. Fleets stay fixed
    // unless the separate PracticeWorldStudyState supplies a movement result;
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
        readonly PracticeAircraftDestructionTable aircraftDestruction;
        readonly PracticeFiringRangeTable firingRanges;
        readonly PracticeVisibility visibility;
        readonly PracticeFiringRateTable firingRates;
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
        public bool IncludesDestruction { get { return destructionTables != null||aircraftDestruction!=null; } }
        public bool IsAircraftStudy {get{return aircraftDestruction!=null;}}
        public int DestroyedUnits { get; private set; }
        public bool LastVictimDestroyed { get; private set; }
        public int MovieCounter { get; private set; }
        public bool IncludesFiringOrders { get { return firingRanges!=null; } }
        public int LastQueueReturnCode { get; private set; }
        public int LastQueueSlot { get; private set; }
        public int FiringPolls { get; private set; }
        public int ConsumedOrders { get; private set; }
        public int RejectedOrders { get; private set; }
        public int ObjectsFromOrders { get; private set; }
        public int LastOrdersConsumed { get; private set; }
        public int LastOrdersRejected { get; private set; }
        public int LastOrdersCreated { get; private set; }
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
        // Explicit offscreen mode0/scenario-selector0 destruction composition. This
        // models data and selection counters, without live media or missions.
        public PracticeTimedStudyState(PracticeTimedWeaponCatalog weapons, PracticeUnitCatalog unitCatalog,
                byte[] visualRecords, byte[] ships, byte[] fleets, uint initialClock, uint randomState,
                PracticeDefenseTables sourceTables, PracticeDestructionTables destruction)
                : this(weapons,unitCatalog,visualRecords,ships,fleets,initialClock,randomState,sourceTables) {
            if(destruction==null)throw new ArgumentNullException("destruction");
            destructionTables=destruction;
        }
        private PracticeTimedStudyState(PracticeTimedWeaponCatalog weapons,PracticeUnitCatalog units,byte[] visuals,
                byte[] ships,byte[] fleets,uint clock,uint random,PracticeDefenseTables defenses,PracticeAircraftDestructionTable aircraft,bool aircraftProfile)
                :this(weapons,units,visuals,ships,fleets,clock,random,defenses) {
            if(aircraft==null)throw new ArgumentNullException("aircraft");aircraftDestruction=aircraft;
        }
        public static PracticeTimedStudyState ForAircraft(PracticeTimedWeaponCatalog weapons,PracticeUnitCatalog units,byte[] visuals,
                byte[] ships,byte[] fleets,uint clock,uint random,PracticeDefenseTables defenses,PracticeAircraftDestructionTable aircraft) {
            return new PracticeTimedStudyState(weapons,units,visuals,ships,fleets,clock,random,defenses,aircraft,true);
        }
        // Optional local firing-data composition. All ten X sentinels are
        // explicitly cleared in this fresh study fixture; the old inactive-
        // order constructors and their comparison inputs remain unchanged.
        public PracticeTimedStudyState(PracticeTimedWeaponCatalog weapons,PracticeUnitCatalog unitCatalog,
                byte[] visualRecords,byte[] ships,byte[] fleets,uint initialClock,uint randomState,
                PracticeDefenseTables sourceTables,PracticeDestructionTables destruction,
                PracticeFiringRangeTable ranges,PracticeVisibility visibilityRules,PracticeFiringRateTable rates)
                : this(weapons,unitCatalog,visualRecords,ships,fleets,initialClock,randomState,sourceTables,destruction) {
            if(ranges==null||visibilityRules==null||rates==null)throw new ArgumentNullException("firing tables");
            firingRanges=ranges;visibility=visibilityRules;firingRates=rates;
            byte[] orders=PauseTimers.CopyShipTimers();
            for(int source=0;source<1500;source++)for(int n=0;n<10;n++)BitConverter.GetBytes((short)-1).CopyTo(orders,source*52+8+n*2);
            PauseTimers=WithTimedPool(Objects.CopyTimedPool(),orders);LastQueueReturnCode=LastQueueSlot=-1;
        }
        PracticeTimedStudyState(PracticeTimedStudyState before, PracticeTimedCreationState objects,
                                OriginalPauseTimers timers, uint clock) {
            catalog = before.catalog; Objects = objects; PauseTimers = timers; ClockMilliseconds = clock;
            units = before.units; visuals = before.visuals; effects = before.effects;
            impactTables=before.impactTables;scoreRegion=before.scoreRegion;ExpiryCallbacks=before.ExpiryCallbacks;
            destructionTables=before.destructionTables;DestroyedUnits=before.DestroyedUnits;LastVictimDestroyed=before.LastVictimDestroyed;MovieCounter=before.MovieCounter;
            aircraftDestruction=before.aircraftDestruction;
            firingRanges=before.firingRanges;visibility=before.visibility;firingRates=before.firingRates;
            LastQueueReturnCode=before.LastQueueReturnCode;LastQueueSlot=before.LastQueueSlot;FiringPolls=before.FiringPolls;
            ConsumedOrders=before.ConsumedOrders;RejectedOrders=before.RejectedOrders;ObjectsFromOrders=before.ObjectsFromOrders;
            LastOrdersConsumed=before.LastOrdersConsumed;LastOrdersRejected=before.LastOrdersRejected;LastOrdersCreated=before.LastOrdersCreated;
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
        // Explicit request polls share the original RNG stream with impacts.
        internal PracticeTimedStudyState WithRandomState(uint state,int calls) {
            if(calls<0)throw new ArgumentOutOfRangeException("calls");
            var next=new PracticeTimedStudyState(this,Objects,PauseTimers,ClockMilliseconds);
            next.RandomState=state;next.RandomCalls+=calls;return next;
        }
        // Preserve combat, timer, score and RNG history while replacing the
        // owned fleet snapshot after an admitted movement batch. No update is
        // published until the caller's remaining combat pass also succeeds.
        internal PracticeTimedStudyState WithFleetPool(byte[] fleetPool) {
            var objects=new PracticeTimedCreationState(catalog,Objects.CopyTimedPool(),Objects.CopyShipPool(),fleetPool,Objects.LastTargetFleet);
            return new PracticeTimedStudyState(this,objects,WithTimedPool(objects.CopyTimedPool()),ClockMilliseconds);
        }
        internal PracticeTimedStudyState WithObjectsAndPrimaryOrders(PracticeTimedCreationState objects,byte[] primaryOrders) {
            if(objects==null)throw new ArgumentNullException("objects");
            return new PracticeTimedStudyState(this,objects,WithTimedPool(objects.CopyTimedPool(),primaryOrders),ClockMilliseconds);
        }
        internal PracticeSecondaryStudyUpdate ProcessSecondaryOrders(byte[] orders,uint clock) {
            if(!IncludesFiringOrders)throw new InvalidOperationException("Secondary firing requires the firing-data study.");
            var update=PracticeSecondaryFiringOrders.AdvanceWithLaunch(firingRanges,visibility,firingRates,catalog,units,Objects,
                PauseTimers.CopyShipTimers(),orders,clock,0,-2);
            return new PracticeSecondaryStudyUpdate(WithObjectsAndPrimaryOrders(update.Objects,update.CopyPrimaryOrders()),update);
        }
        public PracticeFiringCheck CheckFiring(int sourceShip,int targetFleet,int weapon) {
            if(!IncludesFiringOrders)throw new InvalidOperationException("This study does not include firing rules.");
            return PracticeFiringEligibility.Evaluate(firingRanges,visibility,Objects.CopyShipPool(),Objects.CopyFleetPool(),sourceShip,targetFleet,weapon,0,-2);
        }
        public PracticeTimedStudyState QueueOrder(int weapon,int sourceShip,int targetFleet,int offsetX,int offsetY) {
            if(!IncludesFiringOrders)throw new InvalidOperationException("This study does not include firing orders.");
            var queued=PracticeFireQueue.Enqueue(Objects.CopyShipPool(),Objects.CopyFleetPool(),PauseTimers.CopyShipTimers(),sourceShip,weapon,targetFleet,offsetX,offsetY);
            var next=new PracticeTimedStudyState(this,Objects,WithTimedPool(Objects.CopyTimedPool(),queued.CopyOrderPool()),ClockMilliseconds);
            next.LastQueueReturnCode=queued.ReturnCode;next.LastQueueSlot=queued.SelectedSlot;return next;
        }
        public PracticeTimedStudyState ClearOrders(int sourceShip) {
            if(!IncludesFiringOrders)throw new InvalidOperationException("This study does not include firing orders.");
            byte[] cleared=PracticeFireQueue.Clear(PauseTimers.CopyShipTimers(),sourceShip);
            var next=new PracticeTimedStudyState(this,Objects,WithTimedPool(Objects.CopyTimedPool(),cleared),ClockMilliseconds);
            next.LastQueueReturnCode=next.LastQueueSlot=-1;return next;
        }
        public PracticeTimedStudyState ProcessOrders() {
            if(!IncludesFiringOrders)throw new InvalidOperationException("This study does not include firing orders.");
            var update=PracticeFiringOrders.AdvanceWithLaunch(firingRanges,visibility,firingRates,catalog,units,Objects,PauseTimers.CopyShipTimers(),ClockMilliseconds,0,-2);
            var next=new PracticeTimedStudyState(this,update.Objects,WithTimedPool(update.Objects.CopyTimedPool(),update.CopyOrderPool()),ClockMilliseconds);
            next.FiringPolls++;next.ConsumedOrders+=update.ConsumedOrders;next.RejectedOrders+=update.RejectedOrders;next.ObjectsFromOrders+=update.CreatedObjects;
            next.LastOrdersConsumed=update.ConsumedOrders;next.LastOrdersRejected=update.RejectedOrders;next.LastOrdersCreated=update.CreatedObjects;return next;
        }
        public PracticeTimedStudyState Create(int weapon, int sourceShip, int targetFleet, int offsetX, int offsetY) {
            if(IsAircraftStudy)throw new NotSupportedException("Use LaunchAntiAircraft in this explicit aircraft profile.");
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
            if(IsAircraftStudy)throw new NotSupportedException("Use LaunchAntiAircraft in this explicit aircraft profile.");
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
        public PracticeTimedStudyState LaunchAntiAircraft(int weapon,int sourceShip,int targetFleet,int x,int y) {
            if(!IsAircraftStudy)throw new InvalidOperationException("Anti-aircraft launch requires the aircraft study profile.");
            var result=PracticeAntiAircraftLaunch.Apply(catalog,units,Objects.CopyTimedPool(),Objects.CopyShipPool(),Objects.CopyFleetPool(),PauseTimers.CopyShipTimers(),weapon,sourceShip,targetFleet,x,y,ClockMilliseconds,Objects.LastTargetFleet);
            var next=new PracticeTimedStudyState(this,result.State,WithTimedPool(result.State.CopyTimedPool(),result.CopyOrderPool()),ClockMilliseconds);
            next.LastCreationOutcome=result.Outcome;next.LastCreatedSlot=result.CreatedSlot;next.lastLaunchSlots=result.CreatedSlot<0?new int[0]:new[]{result.CreatedSlot};next.LastLaunchAttempts=1;next.LastLaunchDepletions=result.DepletedLastRound?1:0;return next;
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
                var impact=IsAircraftStudy?PracticeTimedImpactState.ForAircraft(units,impactTables,visuals,Objects.CopyTimedPool(),Objects.CopyShipPool(),Objects.CopyFleetPool(),effects.CopyEffectPool(),scoreRegion,RandomState,aircraftDestruction,MovieCounter):new PracticeTimedImpactState(units,impactTables,visuals,Objects.CopyTimedPool(),Objects.CopyShipPool(),Objects.CopyFleetPool(),effects.CopyEffectPool(),scoreRegion,RandomState,destructionTables,MovieCounter);
                var update=impact.Advance(nextClock,StepMilliseconds);
                var nextEffects=new PracticeVisualPool(update.State.CopyEffectPool(),visuals).Advance();
                var updatedObjects=new PracticeTimedCreationState(catalog,update.State.CopyTimedPool(),update.State.CopyShipPool(),update.State.CopyFleetPool(),Objects.LastTargetFleet);
                var next=new PracticeTimedStudyState(this,updatedObjects,WithTimedPool(update.State.CopyTimedPool()),nextClock);
                next.effects=nextEffects;next.scoreRegion=update.State.CopyScoreRegion();next.RandomState=update.State.RandomState;
                next.ExpiredObjects+=update.RetiredRecords;next.ExpiryCallbacks+=update.ExpiryCallbacks;next.DroppedEffects+=update.DroppedEffects;next.RandomCalls+=update.RandomCalls;
                next.DestroyedUnits+=update.DestroyedUnits;next.MovieCounter=update.State.MovieCounter;
                if(update.LastImpactOutcome.HasValue){next.LastImpactOutcome=update.LastImpactOutcome;next.LastVictimShip=update.LastVictimShip;next.LastImpactPoints=update.LastPoints;next.LastScatterAttempts=update.LastScatterAttempts;next.LastVictimDestroyed=update.LastVictimDestroyed;}
                next.MovementFrames++;next.LastStoppedAtRecord=update.StoppedAtRecord;return next.IncludesFiringOrders?next.ProcessOrders():next;
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
                next.MovementFrames++; next.LastStoppedAtRecord = update.StoppedAtRecord; return next.IncludesFiringOrders?next.ProcessOrders():next;
            }
            var motion = new PracticeTimedMotionState(Objects.CopyTimedPool(), Objects.CopyFleetPool()).Advance(nextClock, StepMilliseconds);
            var advanced = new PracticeTimedCreationState(catalog, motion.State.CopyTimedPool(), Objects.CopyShipPool(), motion.State.CopyFleetPool(), Objects.LastTargetFleet);
            var result = new PracticeTimedStudyState(this, advanced, WithTimedPool(motion.State.CopyTimedPool()), nextClock);
            result.MovementFrames++; result.LastStoppedAtRecord = motion.StoppedAtRecord; return result.IncludesFiringOrders?result.ProcessOrders():result;
        }
    }
}
