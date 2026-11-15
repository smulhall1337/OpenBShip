using System;
using System.IO;
namespace FleetLab
{
    public sealed class PracticeYardAdmissionResult
    {
        public readonly bool Available;
        public readonly string Reason;
        public readonly int SelectedYard,MarkerFleet,MarkerMember,MarkerUnit;
        public int SourceReturn
        {
            get
            {
                return Available?0:1;
            }
        }
        internal PracticeYardAdmissionResult(bool available,string reason,int selected=-1,int fleet=-1,int member=-1,int unit=-1)
        {
            Available=available;
            Reason=reason;
            SelectedYard=selected;
            MarkerFleet=fleet;
            MarkerMember=member;
            MarkerUnit=unit;
        }
    }
    // RIP B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569,
    // whole read-only gate 428EE0..429006. Result 0 means available.
    // This gate is weaker than the preconditions needed by entry/worker routines.
    public static class PracticeYardAdmission
    {
        static int I(byte[] b,int p)
        {
            return BitConverter.ToInt32(b,p);
        }
        static int W(byte[] b,int p)
        {
            return BitConverter.ToInt16(b,p);
        }
        static int Reference(byte[] fleets,int at)
        {
            uint value=BitConverter.ToUInt32(fleets,at);
            if(value==0)return -1;
            long delta=(long)value-0x4fc340;
            if(delta<0||delta>=402000||delta%268!=0)throw new InvalidDataException("Admission reached a reference outside its normalized ship pool.");
            return (int)delta/268;
        }
        public static PracticeYardAdmissionResult Check(byte[] ships,byte[] fleets,int targetUnit,short localOwner)
        {
            if(ships==null||ships.Length!=402000||fleets==null||fleets.Length!=123000)throw new InvalidDataException("Admission requires complete 268-byte unit and 492-byte fleet pools.");
            if(targetUnit< -1||targetUnit>=1500)throw new ArgumentOutOfRangeException("targetUnit");
            if(targetUnit==-1)return new PracticeYardAdmissionResult(false,"Null target.");
            int at=targetUnit*268,parent=W(ships,at+12);
            if(parent<0||parent>=250)throw new InvalidDataException("Admission target parent is outside its fleet pool.");
            int selected=unchecked((sbyte)fleets[parent*492+482]);
            if(selected<0)return new PracticeYardAdmissionResult(false,"Source fleet has no nonnegative signed-byte yard selection.");
            int kind=I(fleets,parent*492);
            if(kind!=1&&kind!=9)return new PracticeYardAdmissionResult(false,"Source fleet kind must be 1 or 9.",selected);
            if(W(fleets,selected*492+6)!=localOwner)return new PracticeYardAdmissionResult(false,"Selected yard owner differs from the supplied local owner.",selected);
            if(I(fleets,selected*492)!=2)return new PracticeYardAdmissionResult(false,"Selected yard fleet kind must be 2.",selected);
            int first=selected&~3;
            for(int f=first;f<first+4;f++)for(int member=0;member<6;member++)
            {
                int marker=Reference(fleets,f*492+16+member*4);
                if(marker<0)continue;
                int type=I(ships,marker*268);
                if(type!=22&&type!=23)continue;
                // The original reads the preceding 32-bit field even for member 0
                // (fleet+12, normally packed coordinates). Guard reached bad refs;
                // do not turn this weak source gate into permission to run entry.
                int previous=Reference(fleets,f*492+12+member*4);
                if(previous<0||I(ships,previous*268)!=0)continue;
                if(W(ships,at+14)<=W(ships,marker*268+14))return new PracticeYardAdmissionResult(true,"An empty preceding record and a fitting type-22/23 marker are available.",selected,f,member,marker);
            }
            return new PracticeYardAdmissionResult(false,"No fitting marker with an empty preceding record in the aligned four-fleet block.",selected);
        }
    }
}
