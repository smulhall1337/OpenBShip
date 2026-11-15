using System;
using System.IO;
namespace FleetLab
{
    public enum PracticeAircraftClockPreset
    {
        Flight, MovingBattery, EmptyResource, ReturnBeforeImpact, ClockWrap
    }
    // Deliberate diagnostic inputs, separate from recovered update rules.
    // Fixed first-Practice seed143514006 and one supplied parent7 attachment.
    public sealed class PracticeAircraftClockScenario
    {
        readonly PracticeUnitCatalog units;
        readonly PracticeAircraftTables modes;
        readonly PracticeAircraftDestructionTable destruction;
        readonly PracticeTimedWeaponCatalog weapons;
        readonly PracticeDefenseTables defenses;
        readonly PracticeFiringRangeTable ranges;
        readonly PracticeFiringRateTable rates;
        readonly PracticeVisibility visibility;
        readonly byte[] scene,visuals,terrain;
        public const int AircraftFleet=13, BatteryUnit=14, SelectedSurface=7;
        public PracticeAircraftClockScenario(string assets,string programs)
        {
            units=PracticeUnitCatalog.Load(programs);
            modes=PracticeAircraftTables.Load(programs);
            destruction=PracticeAircraftDestructionTable.Load(programs);
            weapons=PracticeTimedWeaponCatalog.Load(programs);
            defenses=PracticeDefenseTables.Load(programs);
            ranges=PracticeFiringRangeTable.Load(programs);
            rates=PracticeFiringRateTable.Load(programs);
            visibility=PracticeVisibility.Load(programs);
            visuals=File.ReadAllBytes(Path.Combine(programs,"ClassicVisuals.seqbin"));
            scene=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN"));
            terrain=PracticeTerrainPool.FirstPractice(scene).CopyRecords();
        }
        static void W(byte[] b,int p,int v)
        {
            BitConverter.GetBytes((short)v).CopyTo(b,p);
        }
        static void I(byte[] b,int p,int v)
        {
            BitConverter.GetBytes(v).CopyTo(b,p);
        }
        public PracticeAircraftWorldStudyState Create(int storedKind,int weapon,PracticeAircraftClockPreset preset,uint randomSeed)
        {
            if(storedKind<6||storedKind>8||weapon<1||weapon>3||!Enum.IsDefined(typeof(PracticeAircraftClockPreset),preset))throw new ArgumentOutOfRangeException("preset");
            var deployment=PracticeDeployment.FirstPractice(units,scene,143514006u);
            var attachment=new PracticeAircraftStudyState(units,modes,deployment).Create(7,storedKind,0).Transition(2);
            var ships=attachment.CopyShipPool();
            var fleets=attachment.CopyFleetPool();
            int at=AircraftFleet*492,u=BatteryUnit*268,home=BitConverter.ToInt16(ships,u+12),x=BitConverter.ToInt16(fleets,at+12),y=BitConverter.ToInt16(fleets,at+14);
            W(fleets,249*492+4,249);
            W(fleets,home*492+12,x-1);
            W(fleets,home*492+14,y);
            for(int n=464;n<=466;n++)fleets[home*492+n]=255;
            int resource=preset==PracticeAircraftClockPreset.EmptyResource?0:preset==PracticeAircraftClockPreset.ReturnBeforeImpact?20:2000;
            for(int n=0;n<6;n++)
            {
                uint reference=BitConverter.ToUInt32(fleets,at+16+n*4);
                if(reference!=0)W(ships,(int)(reference-0x4fc340)+96,resource);
            }
            if(preset!=PracticeAircraftClockPreset.EmptyResource&&preset!=PracticeAircraftClockPreset.ReturnBeforeImpact)
            {
                W(fleets,at+40,x+40);
                W(fleets,at+50,y+30);
            }
            if(preset==PracticeAircraftClockPreset.MovingBattery)
            {
                W(fleets,home*492+40,x+29);
                W(fleets,home*492+50,y);
            }
            I(ships,u+100,weapon);
            W(ships,u+196+weapon*2,weapon==3?1:2);
            PracticeUnitDepletion.Reevaluate(units,ships,u);
            var aux=new byte[78336];
            I(aux,7*312+0x50,29);
            aux[7*312+0x3c]=88;
            I(aux,249*312+0x50,17);
            aux[249*312+0x3c]=119;
            var combat=PracticeTimedStudyState.ForAircraft(weapons,units,visuals,ships,fleets,preset==PracticeAircraftClockPreset.ClockWrap?0xfffffff0u:0,randomSeed,defenses,destruction);
            var orders=PracticeAircraftOrderStudyState.ForStudy(combat,weapons,units,ranges,visibility,rates);
            return new PracticeAircraftWorldStudyState(orders,destruction,units,modes,visuals,terrain,aux,new byte[61500],SelectedSurface,4,new PracticeFleetScheduler(0,0));
        }
    }
}
