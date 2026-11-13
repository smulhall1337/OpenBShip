using System;
using System.IO;
using System.Security.Cryptography;

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
        internal PracticeFiringOrderUpdate(byte[] result,int clamped,int rejected,int consumed) {
            orders=result;ClampedTimers=clamped;RejectedOrders=rejected;ConsumedOrders=consumed;
        }
        public byte[] CopyOrderPool() { return (byte[])orders.Clone(); }
    }

    // Supported RIP B9A8...6A569:416090, admitted network2 branch. This runs
    // eligibility and consumes orders while source skips416570 object launch.
    // It is a copied-data comparison API, not network transport or gameplay.
    public static class PracticeFiringOrders {
        static void Clear(byte[] orders,int at) {
            for(int n=0;n<10;n++)BitConverter.GetBytes((short)-1).CopyTo(orders,at+8+n*2);
        }
        public static PracticeFiringOrderUpdate AdvanceWithoutLaunch(PracticeFiringRangeTable ranges,
                PracticeVisibility visibility,PracticeFiringRateTable rates,byte[] shipPool,byte[] fleetPool,
                byte[] orderPool,uint gameMilliseconds,int rateScale,byte visibilityFlag,short localOwner) {
            if(ranges==null||visibility==null||rates==null)throw new ArgumentNullException("tables");
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000||orderPool==null||orderPool.Length!=78000)
                throw new InvalidDataException("Firing order update requires complete ship, fleet and order pools.");
            byte[] result=(byte[])orderPool.Clone();int clamped=0,rejected=0,consumed=0;
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
                bool allowed=false;
                if(BitConverter.ToInt32(shipPool,source*268)!=0&&pointer!=0) {
                    long delta=(long)pointer-0x4b47a0;
                    if(delta<0||delta>=123000||delta%492!=0)
                        throw new InvalidDataException("Active firing order has an unsupported target reference.");
                    allowed=PracticeFiringEligibility.Evaluate(ranges,visibility,shipPool,fleetPool,source,(int)(delta/492),weapon,visibilityFlag,localOwner).Allowed;
                }
                if(!allowed){Clear(result,at);rejected++;continue;}
                // network2 deliberately skips launch but still consumes this
                // one request. Empty holes can clear remaining X sentinels.
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
            return new PracticeFiringOrderUpdate(result,clamped,rejected,consumed);
        }
    }
}
