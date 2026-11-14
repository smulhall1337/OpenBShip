using System;
using System.IO;
using System.Collections.Generic;

namespace FleetLab {
    public sealed class PracticeSecondaryQueueResult {
        readonly byte[] orders;
        public int ReturnCode { get; private set; }
        public int SelectedSlot { get; private set; }
        internal PracticeSecondaryQueueResult(byte[] pool,int code,int slot) { orders=pool;ReturnCode=code;SelectedSlot=slot; }
        public byte[] CopyOrderPool() { return (byte[])orders.Clone(); }
    }
    public sealed class PracticeSecondaryOrderUpdate {
        readonly byte[] orders;
        public int RejectedOrders { get; private set; }
        public int ConsumedOrders { get; private set; }
        readonly byte[] primary;
        readonly int[] created;
        public PracticeTimedCreationState Objects { get; private set; }
        public int AttemptedObjects { get; private set; }
        public int LastRoundCallbacks { get; private set; }
        public int CreatedObjects { get { return created.Length; } }
        public int[] CopyCreatedSlots() { return (int[])created.Clone(); }
        internal PracticeSecondaryOrderUpdate(byte[] pool,int rejected,int consumed,byte[] primaryOrders,
                PracticeTimedCreationState objects,List<int> slots,int attempts,int lastRounds) {
            orders=pool;RejectedOrders=rejected;ConsumedOrders=consumed;primary=primaryOrders;Objects=objects;
            created=slots.ToArray();AttemptedObjects=attempts;LastRoundCallbacks=lastRounds;
        }
        public byte[] CopyOrderPool() { return (byte[])orders.Clone(); }
        public byte[] CopyPrimaryOrders() { if(primary==null)throw new InvalidOperationException("This update did not include local launch data.");return (byte[])primary.Clone(); }
    }
    // Pinned RIP B9A8...6A569:419B20/419AF0/419970, pool4E4310.
    // Separate from primary496FA0. The observed419790 producer selects
    // equipment1..3 against fleet kinds3/4/5; its AI/target selection and local
    // projectile callbacks are NOT executed here. Network2 skips local launch.
    public static class PracticeSecondaryFiringOrders {
        static void Pool(byte[] orders) { if(orders==null||orders.Length!=78000)throw new InvalidDataException("Secondary orders require the complete 1500x52 pool."); }
        public static byte[] Clear(byte[] orders,int source) { return PracticeFireQueue.Clear(orders,source); }
        public static PracticeSecondaryQueueResult Enqueue(byte[] ships,byte[] orders,int source,int weapon,int target,int x,int y) {
            Pool(orders);
            if(ships==null||ships.Length!=402000)throw new InvalidDataException("Secondary enqueue requires the complete ship pool.");
            if(source<0||source>=1500||target<0||target>=250)throw new ArgumentOutOfRangeException("source");
            if(BitConverter.ToInt16(ships,source*268+4)!=source)throw new InvalidDataException("Secondary source ID differs from its pool slot.");
            if(x<short.MinValue||x>short.MaxValue||y<short.MinValue||y>short.MaxValue)throw new ArgumentOutOfRangeException("x");
            byte[] next=(byte[])orders.Clone();int at=source*52,slot;
            // Unlike4162A0: no clearing on a kind/target change, no timer
            // reset and only the FIRST FIVE X words are searched for space.
            for(slot=0;slot<5;slot++)if(BitConverter.ToInt16(next,at+8+slot*2)==-1)break;
            if(slot==5)return new PracticeSecondaryQueueResult(next,1,-1);
            BitConverter.GetBytes(weapon).CopyTo(next,at);
            BitConverter.GetBytes((short)x).CopyTo(next,at+8+slot*2);
            BitConverter.GetBytes((short)y).CopyTo(next,at+28+slot*2);
            BitConverter.GetBytes(0x4b47a0+target*492).CopyTo(next,at+48);
            return new PracticeSecondaryQueueResult(next,slot==4?12:0,slot);
        }
        public static PracticeSecondaryOrderUpdate AdvanceWithoutLaunch(PracticeFiringRangeTable ranges,PracticeVisibility visibility,
                PracticeFiringRateTable rates,byte[] ships,byte[] fleets,byte[] orders,uint clock,int scale,byte visibilityFlag,short localOwner) {
            return Advance(ranges,visibility,rates,ships,fleets,orders,clock,scale,visibilityFlag,localOwner,null,null,null,null);
        }
        // Bounded local launch profile: ONLY the previously admitted ordinary
        // weapons14..30. The observed producer's equipment1..3 and their impact
        // branches are separate. Depletion clears PRIMARY orders, not this pool.
        public static PracticeSecondaryOrderUpdate AdvanceWithLaunch(PracticeFiringRangeTable ranges,PracticeVisibility visibility,
                PracticeFiringRateTable rates,PracticeTimedWeaponCatalog weapons,PracticeUnitCatalog units,
                PracticeTimedCreationState objects,byte[] primaryOrders,byte[] secondaryOrders,uint clock,byte visibilityFlag,short localOwner) {
            if(weapons==null||units==null||objects==null)throw new ArgumentNullException("launch state");Pool(primaryOrders);
            return Advance(ranges,visibility,rates,objects.CopyShipPool(),objects.CopyFleetPool(),secondaryOrders,clock,1000,
                visibilityFlag,localOwner,weapons,units,objects,primaryOrders);
        }
        static PracticeSecondaryOrderUpdate Advance(PracticeFiringRangeTable ranges,PracticeVisibility visibility,
                PracticeFiringRateTable rates,byte[] ships,byte[] fleets,byte[] orders,uint clock,int scale,byte visibilityFlag,short localOwner,
                PracticeTimedWeaponCatalog weapons,PracticeUnitCatalog units,PracticeTimedCreationState objects,byte[] primaryOrders) {
            if(ranges==null||visibility==null||rates==null)throw new ArgumentNullException("tables");
            Pool(orders);
            if(ships==null||ships.Length!=402000||fleets==null||fleets.Length!=123000)throw new InvalidDataException("Secondary firing requires complete ship/fleet pools.");
            byte[] next=(byte[])orders.Clone();int now=unchecked((int)clock),rejected=0,consumed=0;
            byte[] primary=primaryOrders==null?null:(byte[])primaryOrders.Clone();var created=new List<int>();int attempts=0,lastRounds=0;
            for(int source=0;source<1500;source++) {
                int at=source*52;
                if(BitConverter.ToInt16(next,at+8)==-1||BitConverter.ToInt32(next,at+4)>=now)continue;
                // No primary >34 skip and NO now+15000 clamp. Opaque idle
                // kinds/references remain untouched; due malformed reads fail.
                uint reference=BitConverter.ToUInt32(next,at+48);bool allowed=false;int weapon=BitConverter.ToInt32(next,at);
                if(BitConverter.ToInt32(ships,source*268)!=0&&reference!=0) {
                    long delta=(long)reference-0x4b47a0;
                    if(delta<0||delta>=123000||delta%492!=0)throw new InvalidDataException("Due secondary order has an unsupported target reference.");
                    int target=(int)(delta/492);
                    if(BitConverter.ToInt32(fleets,target*492)!=0)
                        allowed=PracticeFiringEligibility.Evaluate(ranges,visibility,ships,fleets,source,target,weapon,visibilityFlag,localOwner).Allowed;
                }
                if(!allowed){ClearX(next,at);rejected++;continue;}
                if(objects!=null) {
                    if(weapon<14||weapon>30)throw new NotSupportedException("Secondary local launches currently admit ordinary weapons14..30; equipment1..3 is not reconstructed.");
                    if(BitConverter.ToInt16(ships,source*268+6)==localOwner)throw new NotSupportedException("Local-owner media callbacks are outside the secondary data profile.");
                    int target=(int)(((long)reference-0x4b47a0)/492);
                    var launch=PracticeTimedLaunch.Apply(weapons,units,objects.CopyTimedPool(),ships,fleets,primary,
                        weapon,source,target,BitConverter.ToInt16(next,at+8),BitConverter.ToInt16(next,at+28),clock,objects.LastTargetFleet);
                    objects=launch.State;ships=objects.CopyShipPool();fleets=objects.CopyFleetPool();primary=launch.CopyOrderPool();
                    created.AddRange(launch.CopyCreatedSlots());attempts+=launch.AttemptedObjects;lastRounds+=launch.LastRoundCallbacks;
                }
                // Source shifts TEN coordinate pairs, even though enqueue
                // searches five. Tail coordinates and stale last Y matter.
                for(int n=1;n<10;n++){Array.Copy(next,at+8+n*2,next,at+6+n*2,2);Array.Copy(next,at+28+n*2,next,at+26+n*2,2);}
                BitConverter.GetBytes((short)-1).CopyTo(next,at+26);
                // A new empty first X does NOT clear the remaining tail in
                // this consumer. A later enqueue can reactivate those slots.
                consumed++;int unit=source*268,length=BitConverter.ToInt16(ships,unit+14),mounts=0;
                if(length<0||length>6)throw new NotSupportedException("Unsupported secondary firing equipment row count.");
                for(int n=0;n<length*4;n++)if(BitConverter.ToInt32(ships,unit+100+n*4)==weapon)mounts++;
                if(mounts==0){ClearX(next,at);continue;}
                BitConverter.GetBytes(unchecked(now+rates.Interval(weapon,scale,mounts))).CopyTo(next,at+4);
            }
            return new PracticeSecondaryOrderUpdate(next,rejected,consumed,primary,objects,created,attempts,lastRounds);
        }
        static void ClearX(byte[] pool,int at) { for(int n=0;n<10;n++)BitConverter.GetBytes((short)-1).CopyTo(pool,at+8+n*2); }
    }
}
