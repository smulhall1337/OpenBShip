using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab
{
    public sealed class PracticeFormationPlacementResult
    {
        readonly byte[] ships,fleets;
        public uint RandomState { get; private set; }
        public int RandomCalls { get; private set; }
        public int Restarts { get; private set; }
        public int PlacedUnits { get; private set; }
        internal PracticeFormationPlacementResult(byte[] s,byte[] f,uint random,int calls,int restarts,int units)
        {ships=s;fleets=f;RandomState=random;RandomCalls=calls;Restarts=restarts;PlacedUnits=units;}
        public byte[] CopyShipPool(){return (byte[])ships.Clone();}
        public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
    }
    // Pinned RIP B9A8...6A569:41B9A0 and42CE70. Practice uses a one-cell
    // separation on adjacent rows/columns, unlike the Classic41B820 routine.
    // Source retries indefinitely; this copy rejects invalid extents and bounds
    // restarts without publishing changed pools or random state on failure.
    public static class PracticeFormationPlacement
    {
        static int I(byte[] b,int at){return BitConverter.ToInt32(b,at);}
        static int W(byte[] b,int at){return BitConverter.ToInt16(b,at);}
        static void I(byte[] b,int at,int value){BitConverter.GetBytes(value).CopyTo(b,at);}
        static void W(byte[] b,int at,int value){BitConverter.GetBytes(unchecked((short)value)).CopyTo(b,at);}
        static int Unit(uint reference)
        {
            long delta=(long)reference-0x4fc340;
            if(delta<0||delta>=402000||delta%268!=0)throw new InvalidDataException("Practice formation reached an invalid normalized unit reference.");
            return (int)(delta/268);
        }
        public static PracticeFormationPlacementResult Apply(byte[] shipPool,byte[] fleetPool,int fleet,uint seed,int maximumRestarts=256)
        {
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000)throw new InvalidDataException("Practice formation requires complete unit and fleet pools.");
            if(fleet<0||fleet>=250||maximumRestarts<0||maximumRestarts>100000)throw new ArgumentOutOfRangeException("fleet/restart limit");
            var ships=(byte[])shipPool.Clone();var fleets=(byte[])fleetPool.Clone();
            var units=new List<int>();var lengths=new List<int>();
            for(int member=0;member<6;member++)
            {
                uint reference=BitConverter.ToUInt32(fleets,fleet*492+16+member*4);
                if(reference==0)continue;
                int unit=Unit(reference);
                if(I(ships,unit*268)==0||units.Contains(unit))continue;
                int length=W(ships,unit*268+14);
                if(length<1||length>10)throw new InvalidDataException("Practice formation admits live lengths1..10; zero/invalid native loops are not executed.");
                units.Add(unit);lengths.Add(length);
            }
            var random=new NativeRandom(seed);var positions=new Point[units.Count];
            Action clear=delegate{for(int n=0;n<positions.Length;n++)positions[n]=new Point(-1,-1);};
            clear();int restarts=0;
            while(true)
            {
                int selected=-1,longest=0;
                for(int n=0;n<units.Count;n++)if(positions[n].X==-1&&lengths[n]>longest){selected=n;longest=lengths[n];}
                if(selected<0)break;
                bool placed=false;
                for(int attempt=0;attempt<100;attempt++)
                {
                    int x=random.Next()%10,y=random.Next()%(longest-11);bool collision=false;
                    for(int n=0;n<units.Count;n++)
                    {
                        Point other=positions[n];
                        if(other.X!=-1&&x>=other.X-1&&x<=other.X+1&&y>=other.Y-longest&&y<=other.Y+lengths[n]){collision=true;break;}
                    }
                    if(!collision){positions[selected]=new Point(x,y);placed=true;break;}
                }
                if(!placed)
                {
                    clear();restarts++;
                    if(restarts>maximumRestarts)throw new InvalidDataException("Practice formation exceeded the copied restart limit; caller pools and seed were preserved.");
                }
            }
            for(int n=0;n<units.Count;n++){W(ships,units[n]*268+8,positions[n].X);W(ships,units[n]*268+10,positions[n].Y);}
            for(int n=0;n<100;n++)I(fleets,fleet*492+64+n*4,I(fleets,fleet*492)==2?3:0);
            for(int n=0;n<units.Count;n++)for(int cell=0;cell<lengths[n];cell++)I(fleets,fleet*492+64+(positions[n].X*10+positions[n].Y+cell)*4,2);
            return new PracticeFormationPlacementResult(ships,fleets,random.State,random.Calls,restarts,units.Count);
        }
    }
}
