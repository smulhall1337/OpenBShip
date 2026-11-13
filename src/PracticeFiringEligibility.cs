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

    public enum PracticeFiringDenial { None, InactiveSource, InactiveTarget, HiddenTarget, NoAmmunition, NoMount, OutOfRange }
    public sealed class PracticeFiringCheck {
        public PracticeFiringDenial Denial { get; private set; }
        public bool Allowed { get { return Denial==PracticeFiringDenial.None; } }
        public int VisibilityLevel { get; private set; }
        public int Distance { get; private set; }
        public int Range { get; private set; }
        internal PracticeFiringCheck(PracticeFiringDenial denial,int visibility=0,int distance=-1,int range=-1) {
            Denial=denial;VisibilityLevel=visibility;Distance=distance;Range=range;
        }
    }

    // Supported RIP B9A8...6A569:4187C0. Evaluate composes the recovered numeric
    // visibility predicate; weapon31's original uninitialized stack dependency
    // remains unsupported. The older kind2-only comparison API is retained.
    public static class PracticeFiringEligibility {
        public static PracticeFiringCheck Evaluate(PracticeFiringRangeTable ranges,PracticeVisibility visibility,
                byte[] shipPool,byte[] fleetPool,int sourceShip,int targetFleet,int weapon,
                byte visibilityFlag,short localOwner) {
            if(ranges==null)throw new ArgumentNullException("ranges");
            if(visibility==null)throw new ArgumentNullException("visibility");
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000)
                throw new InvalidDataException("Firing eligibility requires complete ship and fleet pools.");
            if(sourceShip<0||sourceShip>=1500)throw new ArgumentOutOfRangeException("sourceShip");
            if(targetFleet<0||targetFleet>=250)throw new ArgumentOutOfRangeException("targetFleet");
            if(weapon<0||weapon>=35||weapon==31)
                throw new NotSupportedException("Eligibility admits source weapon IDs0..34 except unresolved weapon31.");
            int unit=sourceShip*268,target=targetFleet*492;
            if(BitConverter.ToInt32(shipPool,unit)==0)return new PracticeFiringCheck(PracticeFiringDenial.InactiveSource);
            if(BitConverter.ToInt32(fleetPool,target)==0)return new PracticeFiringCheck(PracticeFiringDenial.InactiveTarget);
            int level=visibility.Level(shipPool,fleetPool,sourceShip,targetFleet,visibilityFlag,localOwner);
            if(level==0)return new PracticeFiringCheck(PracticeFiringDenial.HiddenTarget);
            if(BitConverter.ToInt16(shipPool,unit+196+weapon*2)==0)return new PracticeFiringCheck(PracticeFiringDenial.NoAmmunition,level);
            int length=BitConverter.ToInt16(shipPool,unit+14);
            if(length<0||length>6)throw new NotSupportedException("Unsupported eligibility equipment row count.");
            bool equipped=false;
            for(int at=unit+100;at<unit+100+length*16;at+=4)
                if(BitConverter.ToInt32(shipPool,at)==weapon){equipped=true;break;}
            if(!equipped)return new PracticeFiringCheck(PracticeFiringDenial.NoMount,level);
            int parent=BitConverter.ToInt16(shipPool,unit+12);
            if(parent<0||parent>=250)throw new InvalidDataException("Eligibility source parent is outside the fleet pool.");
            int distance=Math.Max(Math.Abs(BitConverter.ToInt16(fleetPool,parent*492+12)-BitConverter.ToInt16(fleetPool,target+12)),
                                  Math.Abs(BitConverter.ToInt16(fleetPool,parent*492+14)-BitConverter.ToInt16(fleetPool,target+14)));
            int range=ranges.SourceRange(weapon);
            return new PracticeFiringCheck(distance<=range?PracticeFiringDenial.None:PracticeFiringDenial.OutOfRange,level,distance,range);
        }

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
