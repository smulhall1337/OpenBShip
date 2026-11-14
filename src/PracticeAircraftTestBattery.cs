using System;
using System.IO;
namespace FleetLab
{
    // Deliberate diagnostic input override. The original attachment/equipment
    // caller is not claimed here: replace one copied mount and its AA stock,
    // then use the separately recovered42D120 selection reevaluation.
    public static class PracticeAircraftTestBattery
    {
        public static PracticeTimedStudyState Configure(PracticeTimedStudyState before,PracticeTimedWeaponCatalog weapons, PracticeUnitCatalog units,int source,int weapon,int stock)
        {
            if(before==null||weapons==null||units==null)throw new ArgumentNullException("battery study");
            if(!before.IsAircraftStudy)throw new InvalidOperationException("A copied AA battery requires the aircraft timed profile.");
            if(source<0||source>=1500||weapon<1||weapon>3||stock<1||stock>1000)throw new ArgumentOutOfRangeException("stock");
            byte[] timed=before.Objects.CopyTimedPool(),ships=before.Objects.CopyShipPool();
            for(int n=0;n<80;n++)if(BitConverter.ToInt32(timed,n*268)!=0)throw new InvalidOperationException("Resolve active projectiles before changing the copied battery.");
            int at=source*268,type=BitConverter.ToInt32(ships,at);
            if(type<1||type>4||BitConverter.ToInt16(ships,at+16)<1||BitConverter.ToInt16(ships,at+4)!=source)throw new NotSupportedException("The copied test battery requires a live normalized surface unit1..4.");
            BitConverter.GetBytes(weapon).CopyTo(ships,at+100);
            BitConverter.GetBytes((short)stock).CopyTo(ships,at+196+weapon*2);
            PracticeUnitDepletion.Reevaluate(units,ships,at);
            var objects=new PracticeTimedCreationState(weapons,timed,ships,before.Objects.CopyFleetPool(),before.Objects.LastTargetFleet);
            return before.WithObjectsAndPrimaryOrders(objects,before.PauseTimers.CopyShipTimers());
        }
    }
}
