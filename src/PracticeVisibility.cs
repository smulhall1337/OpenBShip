using System;
using System.IO;
using System.Security.Cryptography;

namespace FleetLab {
    // Supported RIP B9A8...6A569, table at44CC88 and predicate418900.
    // This returns the original numeric level, not a rendering or fog policy.
    public sealed class PracticeVisibility {
        readonly byte[] allowedKinds;
        public PracticeVisibility(byte[] sourceAllowedKinds) {
            if(sourceAllowedKinds==null||sourceAllowedKinds.Length!=624)
                throw new InvalidDataException("Expected the original 624-byte observer kind table.");
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(sourceAllowedKinds)).Replace("-","");
            if(hash!="969B9D61134295EB5BFC35087A1E269FB505A198B94AE9776F43FB09A2494920")
                throw new InvalidDataException("Unsupported visibility kind table identity.");
            allowedKinds=(byte[])sourceAllowedKinds.Clone();
        }
        public static PracticeVisibility Load(string programs) {
            return new PracticeVisibility(File.ReadAllBytes(Path.Combine(programs,"VisibilityKinds.seqbin")));
        }

        public int Level(byte[] shipPool,byte[] fleetPool,int sourceShip,int targetFleet,
                         byte visibilityFlag,short localOwner) {
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000)
                throw new InvalidDataException("Visibility requires complete ship and fleet pools.");
            if(sourceShip<0||sourceShip>=1500)throw new ArgumentOutOfRangeException("sourceShip");
            if(targetFleet<0||targetFleet>=250)throw new ArgumentOutOfRangeException("targetFleet");
            int target=targetFleet*492,kind=BitConverter.ToInt32(fleetPool,target);
            if(kind==0)return 0;
            if(kind==2) {
                // This branch does not scan observers or use their ranges.
                return visibilityFlag==1&&fleetPool[target+467]==0&&
                    BitConverter.ToInt16(fleetPool,target+6)!=localOwner?0:1;
            }
            if(kind<0||kind>12)throw new NotSupportedException("Unsupported target kind for the visibility study.");
            int owner=BitConverter.ToInt16(shipPool,sourceShip*268+6);
            int x=BitConverter.ToInt16(fleetPool,target+12),y=BitConverter.ToInt16(fleetPool,target+14),level=0;
            for(int observer=0;observer<250;observer++) {
                int at=observer*492,observerKind=BitConverter.ToInt32(fleetPool,at);
                if(observerKind==0||observerKind==6||observerKind==7||observerKind==8||
                    BitConverter.ToInt16(fleetPool,at+6)!=owner)continue;
                if(observerKind<0||observerKind>=12)
                    throw new NotSupportedException("Observer kind is outside the recovered visibility table.");
                bool allowed=false;
                for(int n=0;n<13;n++) {
                    int entry=BitConverter.ToInt32(allowedKinds,observerKind*52+n*4);
                    if(entry==0)break;
                    if(entry==kind){allowed=true;break;}
                }
                if(!allowed)continue;
                int distance=Math.Max(Math.Abs(BitConverter.ToInt16(fleetPool,at+12)-x),
                                      Math.Abs(BitConverter.ToInt16(fleetPool,at+14)-y));
                // The three bytes need not be monotonic. Source tests each
                // independently: far466 ->1, middle465 ->2, near464 ->3.
                for(int candidate=1;candidate<=3;candidate++)
                    if(distance<=fleetPool[at+467-candidate]&&candidate>level)level=candidate;
            }
            return level;
        }
    }
}
