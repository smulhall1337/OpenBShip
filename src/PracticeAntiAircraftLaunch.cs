using System;using System.IO;
namespace FleetLab {
    // Complete416570→416A00 data path for equipment1..3, supported RIP
    // B9A8...6A569. Raw launch helper: firing admission, owner-dependent media,
    // motion and impact are separate. Ordinary public constructors keep14..30.
    public static class PracticeAntiAircraftLaunch {
        public static PracticeTimedCreationUpdate Apply(PracticeTimedWeaponCatalog weapons,PracticeUnitCatalog units,
                byte[] timedPool,byte[] shipPool,byte[] fleetPool,byte[] primaryOrders,int weapon,int sourceShip,
                int targetFleet,int offsetX,int offsetY,uint milliseconds,int lastTargetFleet) {
            if(weapons==null||units==null)throw new ArgumentNullException("weapons");
            if(primaryOrders==null||primaryOrders.Length!=78000)throw new InvalidDataException("Anti-aircraft launch requires the complete primary-order pool.");
            var state=new PracticeTimedCreationState(weapons,timedPool,shipPool,fleetPool,lastTargetFleet);weapons.AntiAircraftSpeed(weapon);
            if(sourceShip<0||sourceShip>=1500||targetFleet<0||targetFleet>=250)throw new ArgumentOutOfRangeException("sourceShip");
            int type=BitConverter.ToInt32(shipPool,sourceShip*268),kind=BitConverter.ToInt32(fleetPool,targetFleet*492);
            if(type<1||type>7||BitConverter.ToInt16(shipPool,sourceShip*268+16)<1||kind<3||kind>5)throw new NotSupportedException("Anti-aircraft launch admits a live surface unit and an active aircraft target group in this profile.");
            return state.CreateAntiAircraftObject(weapon,sourceShip,targetFleet,offsetX,offsetY,milliseconds,units,primaryOrders);
        }
    }
}
