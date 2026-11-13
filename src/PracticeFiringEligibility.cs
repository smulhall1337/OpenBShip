using System;
using System.IO;
using System.Security.Cryptography;

namespace FleetLab {
    public sealed class PracticeFiringRangeTable {
        readonly byte[] ranges;
        public PracticeFiringRangeTable(byte[] sourceRanges) {
            if(sourceRanges==null||sourceRanges.Length!=70)throw new InvalidDataException("Expected the original 70-byte firing range table.");
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(sourceRanges)).Replace("-","");
            if(hash!="489BD18D81D2133EADC28D6F3CA6827A421367E64C69B8159422F5C5BC00BBFA")throw new InvalidDataException("Unsupported firing range table identity.");
            ranges=(byte[])sourceRanges.Clone();
        }
        public int SourceRange(int weapon) {
            if(weapon<0||weapon>=35)throw new ArgumentOutOfRangeException("weapon");
            return BitConverter.ToInt16(ranges,weapon*2);
        }
    }

    // Supported RIP B9A8...6A569: complete4187C0 in the admitted always-visible
    // kind2 target branch of418900, visibilityflag4B46E8=0. General visibility,
    // other weapons/target kinds, active order execution and live media remain
    // separate. This predicate is not wired into the manual study.
    public static class PracticeFiringEligibility {
        public static bool CanFireAtKind2(PracticeFiringRangeTable ranges,byte[] shipPool,byte[] fleetPool,
                int sourceShip,int targetFleet,int weapon) {
            if(ranges==null)throw new ArgumentNullException("ranges");
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000)
                throw new InvalidDataException("Firing eligibility requires complete ship and fleet pools.");
            if(sourceShip<0||sourceShip>=1500||targetFleet<0||targetFleet>=250)throw new ArgumentOutOfRangeException("sourceShip");
            if(weapon!=17&&weapon!=20)throw new NotSupportedException("This eligibility adapter currently admits weapons17/20 only.");
            int unit=sourceShip*268,type=BitConverter.ToInt32(shipPool,unit),fleet=targetFleet*492,kind=BitConverter.ToInt32(fleetPool,fleet);
            if(type==0||kind==0)return false;
            if(type<1||type>4||kind!=2)throw new NotSupportedException("This eligibility adapter requires startup unit1..4 and the always-visible kind2 target context.");
            if(BitConverter.ToInt16(shipPool,unit+4)!=sourceShip)throw new InvalidDataException("Eligibility source identity differs from its original slot.");
            if(BitConverter.ToInt16(shipPool,unit+196+weapon*2)==0)return false;
            // Source tests nonzero stock, including negative sentinels, and
            // equipment presence. HP itself is not read by this predicate.
            int length=BitConverter.ToInt16(shipPool,unit+14);
            if(length<0||length>6)throw new NotSupportedException("Unsupported eligibility equipment row count.");
            bool equipped=false;for(int at=unit+100;at<unit+100+length*16;at+=4)if(BitConverter.ToInt32(shipPool,at)==weapon){equipped=true;break;}
            if(!equipped)return false;
            int parent=BitConverter.ToInt16(shipPool,unit+12);if(parent<0||parent>=250)throw new InvalidDataException("Eligibility source parent is outside the fleet pool.");
            int dx=Math.Abs(BitConverter.ToInt16(fleetPool,parent*492+12)-BitConverter.ToInt16(fleetPool,fleet+12));
            int dy=Math.Abs(BitConverter.ToInt16(fleetPool,parent*492+14)-BitConverter.ToInt16(fleetPool,fleet+14));
            // Original range is an inclusive Chebyshev distance between fleet
            // origins; it does not use local hull or requested target offsets.
            return Math.Max(dx,dy)<=ranges.SourceRange(weapon);
        }
    }
}
