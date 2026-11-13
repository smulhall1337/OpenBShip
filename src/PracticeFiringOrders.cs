using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace FleetLab {
    public sealed class PracticeFiringRateTable {
        readonly byte[] rates;
        public PracticeFiringRateTable(byte[] sourceRates) {
            if(sourceRates==null||sourceRates.Length!=140)throw new InvalidDataException("Expected the original 140-byte firing rate table.");
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(sourceRates)).Replace("-","");
            if(hash!="6FAF98880ED597C57F44ABCCD95E5F75AC6B2D15ABEA78EADDB98BEEDF238C1B")throw new InvalidDataException("Unsupported firing rate table identity.");
            rates=(byte[])sourceRates.Clone();
        }
        public int Interval(int weapon,int scale,int mounts) {
            if(weapon<0||weapon>=35)throw new ArgumentOutOfRangeException("weapon");
            if(mounts<1||mounts>24)throw new ArgumentOutOfRangeException("mounts");
            // Preserve the source's signed32 multiply wrap and two separate
            // truncating divisions; this is not a calibrated real-time rate.
            return unchecked(BitConverter.ToInt32(rates,weapon*4)*scale)/10/mounts;
        }
    }

    public sealed class PracticeFiringOrderUpdate {
        readonly byte[] orders;
        public int ClampedTimers { get; private set; }
        public int RejectedOrders { get; private set; }
        public int ConsumedOrders { get; private set; }
        public PracticeTimedCreationState Objects { get; private set; }
        public int AttemptedObjects { get; private set; }
        public int LastRoundCallbacks { get; private set; }
        readonly int[] slots;
        public int CreatedObjects { get { return slots.Length; } }
        public int[] CopyCreatedSlots() { return (int[])slots.Clone(); }
        internal PracticeFiringOrderUpdate(byte[] result,int clamped,int rejected,int consumed,
                PracticeTimedCreationState objects,List<int> created,int attempts,int lastRounds) {
            orders=result;ClampedTimers=clamped;RejectedOrders=rejected;ConsumedOrders=consumed;
            Objects=objects;slots=created.ToArray();AttemptedObjects=attempts;LastRoundCallbacks=lastRounds;
        }
        public byte[] CopyOrderPool() { return (byte[])orders.Clone(); }
    }

    // Supported RIP B9A8...6A569:416090, network2 scheduling and opt-in local
    // network0 launch data. Neither API runs network transport or missions.
    public static class PracticeFiringOrders {
        static void Clear(byte[] orders,int at) {
            for(int n=0;n<10;n++)BitConverter.GetBytes((short)-1).CopyTo(orders,at+8+n*2);
        }
        public static PracticeFiringOrderUpdate AdvanceWithoutLaunch(PracticeFiringRangeTable ranges,
                PracticeVisibility visibility,PracticeFiringRateTable rates,byte[] shipPool,byte[] fleetPool,
                byte[] orderPool,uint gameMilliseconds,int rateScale,byte visibilityFlag,short localOwner) {
            return Advance(ranges,visibility,rates,shipPool,fleetPool,orderPool,gameMilliseconds,rateScale,
                visibilityFlag,localOwner,null,null,null);
        }

        // Opt-in local network0 data profile: startup scale1000, message/UI0,
        // source/local-owner mismatch excludes live movies and OS callbacks.
        public static PracticeFiringOrderUpdate AdvanceWithLaunch(PracticeFiringRangeTable ranges,
                PracticeVisibility visibility,PracticeFiringRateTable rates,PracticeTimedWeaponCatalog weapons,
                PracticeUnitCatalog units,PracticeTimedCreationState objects,byte[] orderPool,
                uint gameMilliseconds,byte visibilityFlag,short localOwner) {
            if(weapons==null||units==null||objects==null)throw new ArgumentNullException("launch state");
            return Advance(ranges,visibility,rates,objects.CopyShipPool(),objects.CopyFleetPool(),orderPool,
                gameMilliseconds,1000,visibilityFlag,localOwner,weapons,units,objects);
        }

        static PracticeFiringOrderUpdate Advance(PracticeFiringRangeTable ranges,
                PracticeVisibility visibility,PracticeFiringRateTable rates,byte[] shipPool,byte[] fleetPool,
                byte[] orderPool,uint gameMilliseconds,int rateScale,byte visibilityFlag,short localOwner,
                PracticeTimedWeaponCatalog weapons,PracticeUnitCatalog units,PracticeTimedCreationState objects) {
            if(ranges==null||visibility==null||rates==null)throw new ArgumentNullException("tables");
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000||orderPool==null||orderPool.Length!=78000)
                throw new InvalidDataException("Firing order update requires complete ship, fleet and order pools.");
            byte[] result=(byte[])orderPool.Clone();int clamped=0,rejected=0,consumed=0;
            var created=new List<int>();int attempts=0,lastRounds=0;
            int now=unchecked((int)gameMilliseconds),ceiling=unchecked((int)(gameMilliseconds+15000u));
            for(int source=0;source<1500;source++) {
                int at=source*52;
                if(BitConverter.ToInt16(result,at+8)==-1)continue;
                int weapon=BitConverter.ToInt32(result,at);
                if(weapon>34)continue;
                if(weapon<0||weapon==31)throw new NotSupportedException("Unsupported active firing order kind.");
                int timer=BitConverter.ToInt32(result,at+4);
                if(timer>=ceiling){timer=ceiling;BitConverter.GetBytes(timer).CopyTo(result,at+4);clamped++;}
                if(timer>=now)continue;
                uint pointer=BitConverter.ToUInt32(result,at+48);
                // Original checks inactive source and null target before
                // dereferencing the target. Opaque references on idle orders
                // are preserved; active invalid references fail atomically.
                bool allowed=false;int target=-1;
                if(BitConverter.ToInt32(shipPool,source*268)!=0&&pointer!=0) {
                    long delta=(long)pointer-0x4b47a0;
                    if(delta<0||delta>=123000||delta%492!=0)
                        throw new InvalidDataException("Active firing order has an unsupported target reference.");
                    target=(int)(delta/492);
                    allowed=PracticeFiringEligibility.Evaluate(ranges,visibility,shipPool,fleetPool,source,target,weapon,visibilityFlag,localOwner).Allowed;
                }
                if(!allowed){Clear(result,at);rejected++;continue;}
                if(objects!=null) {
                    if(BitConverter.ToInt16(shipPool,source*268+6)==localOwner)
                        throw new NotSupportedException("Local-owner movie callbacks are outside the firing data profile.");
                    var launch=PracticeTimedLaunch.Apply(weapons,units,objects.CopyTimedPool(),shipPool,fleetPool,result,
                        weapon,source,target,BitConverter.ToInt16(result,at+8),BitConverter.ToInt16(result,at+28),gameMilliseconds,objects.LastTargetFleet);
                    objects=launch.State;result=launch.CopyOrderPool();shipPool=objects.CopyShipPool();fleetPool=objects.CopyFleetPool();
                    created.AddRange(launch.CopyCreatedSlots());attempts+=launch.AttemptedObjects;lastRounds+=launch.LastRoundCallbacks;
                }
                // Shift AFTER launch's possible last-round queue clear and
                // equipment reevaluation. Preserve the stale last Y word.
                for(int n=1;n<10;n++) {
                    Array.Copy(result,at+8+n*2,result,at+6+n*2,2);
                    Array.Copy(result,at+28+n*2,result,at+26+n*2,2);
                }
                BitConverter.GetBytes((short)-1).CopyTo(result,at+26);
                if(BitConverter.ToInt16(result,at+8)==-1)Clear(result,at);
                consumed++;
                int unit=source*268,length=BitConverter.ToInt16(shipPool,unit+14),mounts=0;
                if(length<0||length>6)throw new NotSupportedException("Unsupported firing rate equipment row count.");
                for(int equipment=unit+100;equipment<unit+100+length*16;equipment+=4)
                    if(BitConverter.ToInt32(shipPool,equipment)==weapon)mounts++;
                if(mounts==0){Clear(result,at);continue;}
                BitConverter.GetBytes(unchecked(now+rates.Interval(weapon,rateScale,mounts))).CopyTo(result,at+4);
            }
            return new PracticeFiringOrderUpdate(result,clamped,rejected,consumed,objects,created,attempts,lastRounds);
        }
    }
}
