using System;
using System.IO;
namespace FleetLab
{
    public sealed class PracticeConfigurationSelectionResult
    {
        public readonly int Fleet, Unit;
        public readonly bool FoundOwnedFleet;
        internal PracticeConfigurationSelectionResult(int fleet,int unit,bool found){Fleet=fleet;Unit=unit;FoundOwnedFleet=found;}
    }
    // Pinned RIP B9A8...6A569:whole42C380. Only the two selection values;
    // no roster-index write or following4085D0/414380 application work.
    public static class PracticeConfigurationSelection
    {
        static int Unit(byte[] fleets,int at)
        {
            uint reference=BitConverter.ToUInt32(fleets,at);
            if(reference==0)return -1;
            long offset=(long)reference-0x4fc340;
            if(offset<0||offset>=402000||offset%268!=0)throw new InvalidDataException("Selection reached an invalid normalized unit reference.");
            return (int)(offset/268);
        }
        public static PracticeConfigurationSelectionResult Select(byte[] ships,byte[] fleets,short owner,int priorFleet,int priorUnit)
        {
            if(ships==null||ships.Length!=402000||fleets==null||fleets.Length!=123000)throw new InvalidDataException("Configuration selection requires complete unit and fleet pools.");
            if(priorFleet< -1||priorFleet>=250||priorUnit< -1||priorUnit>=1500)throw new ArgumentOutOfRangeException("prior selection");
            int selected=-1;
            for(int n=0;n<250;n++)if(BitConverter.ToInt32(fleets,n*492)==1&&BitConverter.ToInt16(fleets,n*492+6)==owner){selected=n;break;}
            if(selected<0)for(int n=0;n<250;n++)if(BitConverter.ToInt16(fleets,n*492+6)==owner){selected=n;break;}
            if(selected<0)return new PracticeConfigurationSelectionResult(priorFleet,priorUnit,false);
            int first=Unit(fleets,selected*492+16);
            for(int member=0;member<6;member++)
            {
                int unit=Unit(fleets,selected*492+16+member*4);
                if(unit>=0&&BitConverter.ToInt32(ships,unit*268)!=0)return new PracticeConfigurationSelectionResult(selected,unit,true);
            }
            return new PracticeConfigurationSelectionResult(selected,first,true);
        }
    }
}
