using System;
using System.IO;
using System.Collections.Generic;

namespace FleetLab {
    public sealed class PracticeUnitDepletionResult {
        readonly byte[] ships, orders;
        internal PracticeUnitDepletionResult(byte[] updatedShips, byte[] updatedOrders) { ships=updatedShips;orders=updatedOrders; }
        public byte[] CopyShipPool() { return (byte[])ships.Clone(); }
        public byte[] CopyOrderPool() { return (byte[])orders.Clone(); }
    }

    // Data-only last-round path in supported RIP B9A8...6A569:416A49..416A6B,
    // 416250 and42D120..42D329. Selected-unit UI refresh and media are excluded.
    // CreateWithDepletion composes this before copying the projectile record.
    public static class PracticeUnitDepletion {
        static int Word(byte[] bytes,int at) {
            if(at<0||at>bytes.Length-2)throw new InvalidDataException("Equipment ammunition crosses the admitted ship pool.");
            return BitConverter.ToInt16(bytes,at);
        }
        static int Dword(byte[] bytes,int at) { return BitConverter.ToInt32(bytes,at); }
        static void Dword(byte[] bytes,int at,int value) { BitConverter.GetBytes(value).CopyTo(bytes,at); }
        internal static void Reevaluate(PracticeUnitCatalog catalog,byte[] pool,int at) {
            int length=Word(pool,at+14);
            if(length<0||length>6)throw new InvalidDataException("Unsupported unit equipment row count.");
            var equipment=new List<int>();
            for(int i=0;i<length*4;i++){int weapon=Dword(pool,at+100+i*4);if(weapon<0||weapon>39)throw new InvalidDataException("Unsupported equipment ID.");equipment.Add(weapon);}
            var offense=new HashSet<int>();
            for(int i=0;i<catalog.Offense.Length;i+=4){int weapon=Dword(catalog.Offense,i);if(weapon==0)break;offense.Add(weapon);}
            foreach(int field in new[]{76,80,84}){
                int current=Dword(pool,at+field);
                // The source first retains any present current selection. Its
                // category is considered only when choosing a replacement.
                bool present=current!=0&&equipment.Contains(current)&&(field==80||Word(pool,at+196+current*2)!=0);
                if(present)continue;
                foreach(int weapon in equipment){
                    bool eligible=field==76?weapon>=4&&weapon<=8&&Word(pool,at+196+weapon*2)!=0:
                                  field==80?weapon>=9&&weapon<=13:
                                  offense.Contains(weapon)&&Word(pool,at+196+weapon*2)!=0;
                    if(eligible){Dword(pool,at+field,weapon);break;}
                }
                // No replacement leaves the old field untouched, even when
                // that field names the now-exhausted weapon. Do not clear it.
            }
        }
        public static PracticeUnitDepletionResult ConsumeLastRound(PracticeUnitCatalog catalog,byte[] shipPool,
                                                                    byte[] orderPool,int sourceShip,int weapon) {
            if(catalog==null)throw new ArgumentNullException("catalog");
            if(shipPool==null||shipPool.Length!=402000||orderPool==null||orderPool.Length!=78000)
                throw new InvalidDataException("Last-round data effects require complete ship and order pools.");
            if(sourceShip<0||sourceShip>=1500)throw new ArgumentOutOfRangeException("sourceShip");
            if(weapon<14||weapon>30)throw new ArgumentOutOfRangeException("weapon");
            int at=sourceShip*268;
            if(Word(shipPool,at+4)!=sourceShip||Word(shipPool,at+196+weapon*2)!=1)
                throw new InvalidDataException("Last-round data effects require the matching unit and exactly one round.");
            byte[] ships=(byte[])shipPool.Clone(),orders=(byte[])orderPool.Clone();
            BitConverter.GetBytes((short)0).CopyTo(ships,at+196+weapon*2);
            for(int field=8;field<=24;field+=4)Dword(orders,sourceShip*52+field,-1);
            Reevaluate(catalog,ships,at);
            return new PracticeUnitDepletionResult(ships,orders);
        }
    }
}
