using System;
using System.IO;
using System.Collections.Generic;

namespace FleetLab {
    public sealed class PracticeTimedLaunchResult {
        readonly byte[] orders;
        readonly int[] createdSlots;
        public PracticeTimedCreationState State { get; private set; }
        public int AttemptedObjects { get; private set; }
        public int LastRoundCallbacks { get; private set; }
        public int CreatedCount { get { return createdSlots.Length; } }
        internal PracticeTimedLaunchResult(PracticeTimedCreationState state, byte[] orderPool, List<int> slots, int attempts, int lastRounds) {
            State = state; orders = (byte[])orderPool.Clone(); createdSlots = slots.ToArray();
            AttemptedObjects = attempts; LastRoundCallbacks = lastRounds;
        }
        public byte[] CopyOrderPool() { return (byte[])orders.Clone(); }
        public int[] CopyCreatedSlots() { return (int[])createdSlots.Clone(); }
    }

    // Supported RIP B9A8...6A569: complete416570 data launch helper and416A00.
    // Weapon20 adjusts signed16 stock before three ordered constructor calls.
    // This is not firing admission. Messageflag0/network0/localowner mismatch/
    // selectedUI0 exclude text, actual media and network callbacks. The manual
    // study continues to create one explicit test object through its old API.
    public static class PracticeTimedLaunch {
        public static PracticeTimedLaunchResult Apply(PracticeTimedWeaponCatalog weapons, PracticeUnitCatalog units,
                byte[] timedPool, byte[] shipPool, byte[] fleetPool, byte[] orderPool,
                int weapon, int sourceShip, int targetFleet, int offsetX, int offsetY, uint gameMilliseconds, int lastTargetFleet) {
            if (weapons == null) throw new ArgumentNullException("weapons");
            if (units == null) throw new ArgumentNullException("units");
            if (orderPool == null || orderPool.Length != 78000)
                throw new InvalidDataException("Timed launch requires the complete original order pool.");
            var state = new PracticeTimedCreationState(weapons,timedPool,shipPool,fleetPool,lastTargetFleet);
            weapons.SourceSpeed(weapon);
            if (sourceShip < 0 || sourceShip >= 1500 || targetFleet < 0 || targetFleet >= 250)
                throw new ArgumentOutOfRangeException("sourceShip");
            if (offsetX < short.MinValue || offsetX > short.MaxValue || offsetY < short.MinValue || offsetY > short.MaxValue)
                throw new NotSupportedException("Timed launch offsets must fit the admitted signed16 coordinate span.");
            int source = sourceShip * 268, parent = BitConverter.ToInt16(shipPool,source+12), type = BitConverter.ToInt32(shipPool,source);
            if (type < 1 || type > 4 || BitConverter.ToInt16(shipPool,source+16) < 1)
                throw new NotSupportedException("Timed launch currently admits active startup unit types1..4.");
            if (parent < 0 || parent >= 250 || BitConverter.ToInt16(shipPool,source+4) != sourceShip || BitConverter.ToInt16(fleetPool,targetFleet*492+4) != targetFleet)
                throw new InvalidDataException("Timed launch source or target identity differs from its pool slot.");
            byte[] orders = (byte[])orderPool.Clone(); var slots = new List<int>();
            int attempts = 1, ammoAt = source + 196 + weapon * 2;
            short ammo = BitConverter.ToInt16(shipPool,ammoAt);
            if (weapon == 20) {
                if (ammo == 0) return new PracticeTimedLaunchResult(state,orders,slots,0,0);
                if (offsetY == short.MinValue || offsetY == short.MaxValue)
                    throw new NotSupportedException("The three-object launch crosses the admitted signed16 offset span.");
                byte[] ships = state.CopyShipPool();
                BitConverter.GetBytes(unchecked((short)(ammo+2))).CopyTo(ships,ammoAt);
                // The original stock adjustment precedes the pool-capacity
                // checks. Preserve it even if all three constructor calls fail.
                state = new PracticeTimedCreationState(weapons,state.CopyTimedPool(),ships,state.CopyFleetPool(),lastTargetFleet);
                attempts = 3;
            }
            int lastRounds = 0;
            for (int attempt = 0; attempt < attempts; attempt++) {
                int y = attempts == 3 ? offsetY + attempt - 1 : offsetY;
                var result = state.CreateLaunchObject(weapon,sourceShip,targetFleet,offsetX,y,gameMilliseconds,units,orders);
                state = result.State; orders = result.CopyOrderPool();
                if (result.Outcome == PracticeTimedCreationOutcome.Created) slots.Add(result.CreatedSlot);
                if (result.DepletedLastRound) lastRounds++;
            }
            return new PracticeTimedLaunchResult(state,orders,slots,attempts,lastRounds);
        }
    }
}
