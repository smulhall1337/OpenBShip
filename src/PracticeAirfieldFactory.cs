using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
namespace FleetLab
{
    // Bounded compound constructor. Creation and copied pools only; no map caller, support pass or missions.
    // B9A8...6A569:42C4A0 compound constructor plus allocator/unit/grid helpers.
    public sealed class PracticeAirfieldFactoryResult
    {
        readonly byte[] ships,fleets;
        readonly ReadOnlyCollection<int> allocated;
        internal PracticeAirfieldFactoryResult(byte[] s,byte[] f,List<int> units)
        {
            ships=s;
            fleets=f;
            allocated=units.AsReadOnly();
        }
        public ReadOnlyCollection<int> AllocatedUnits
        {
            get
            {
                return allocated;
            }
        }
        public byte[] CopyShipPool()
        {
            return (byte[])ships.Clone();
        }
        public byte[] CopyFleetPool()
        {
            return (byte[])fleets.Clone();
        }
    }
    public sealed class PracticeAirfieldFactory
    {
        readonly PracticeUnitCatalog units;
        readonly byte[] templates;
        static int I(byte[] b,int p)
        {
            return BitConverter.ToInt32(b,p);
        }
        static int W(byte[] b,int p)
        {
            return BitConverter.ToInt16(b,p);
        }
        static void I(byte[] b,int p,int v)
        {
            BitConverter.GetBytes(v).CopyTo(b,p);
        }
        static void W(byte[] b,int p,int v)
        {
            BitConverter.GetBytes(unchecked((short)v)).CopyTo(b,p);
        }
        public PracticeAirfieldFactory(PracticeUnitCatalog units,byte[] type19to24)
        {
            if(units==null)throw new ArgumentNullException("units");
            if(type19to24==null||type19to24.Length!=1608)throw new InvalidDataException("Expected six complete type19..24 templates.");
            using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(type19to24)).Replace("-","")!="519105739C811D974FF7ADA4022305B3186D06440484B7CFC18984311C93C428")throw new InvalidDataException("Unsupported compound installation templates.");
            this.units=units;
            templates=(byte[])type19to24.Clone();
        }
        public static PracticeAirfieldFactory Load(string programs)
        {
            return new PracticeAirfieldFactory(PracticeUnitCatalog.Load(programs),File.ReadAllBytes(Path.Combine(programs,"InstallationTemplates.seqbin")));
        }
        public PracticeAirfieldFactoryResult CreateFresh(int firstFleet,Point origin,short owner,byte flag)
        {
            var ships=new byte[402000];
            var fleets=new byte[123000];
            for(int n=0;n<1500;n++)W(ships,n*268+4,n);
            for(int n=0;n<250;n++)PracticeFleetSnapshot.InitializeRootFleet(fleets,n*492,n);
            return Create(ships,fleets,firstFleet,origin,owner,flag);
        }
        static int Reference(uint r)
        {
            long at=(long)r-0x4fc340;
            if(at<0||at>=402000||at%268!=0)throw new InvalidDataException("Installation member reference outside complete ship pool.");
            return (int)at;
        }
        static int Allocate(byte[] ships,byte[] fleets)
        {
            var reserved=new HashSet<int>();
            for(int n=0;n<250;n++)
            {
                int p=n*492,kind=I(fleets,p);
                if(kind!=2&&kind!=10)continue;
                for(int q=0;q<6;q++)
                {
                    uint r=BitConverter.ToUInt32(fleets,p+16+q*4);
                    if(r==0)continue;
                    int at=Reference(r);
                    if(I(ships,at)==0)reserved.Add(W(ships,at+4));
                }
            }
            for(int n=0;n<1500;n++)
            {
                int at=n*268;
                if(I(ships,at)!=0||n<1499&&(I(ships,at+268)==22||I(ships,at+268)==23)||reserved.Contains(n))continue;
                return n;
            }
            throw new InvalidDataException("Installation requires ten admitted free units; copy not published.");
        }
        void Construct(byte[] ships,int slot,int type,int owner,int fleet,int x,int y)
        {
            int at=slot*268;
            Array.Copy(templates,(type-19)*268,ships,at,268);
            W(ships,at+4,slot);
            W(ships,at+6,owner);
            W(ships,at+8,x);
            W(ships,at+10,y);
            W(ships,at+12,fleet);
            int length=W(ships,at+14);
            if(length<1||length>6||x<0||y<0||x*10+y+length>100)throw new InvalidDataException("Installation unit shape is outside its grid.");
            for(int n=0;n<35;n++)W(ships,at+196+n*2,0);
            for(int n=0;n<length*4;n++)
            {
                int weapon=I(ships,at+100+n*4);
                if(weapon<0||weapon>39)throw new InvalidDataException("Unsupported installation equipment.");
                int dest=at+196+weapon*2;
                if(dest>ships.Length-2)throw new InvalidDataException("Installation ammunition crosses complete ship pool.");
                int amount=W(units.Ammunition,weapon*2);
                W(ships,dest,amount==-1?-1:W(ships,dest)+amount);
            }
            PracticeUnitDepletion.Reevaluate(units,ships,at);
        }
        static void Grid(byte[] ships,byte[] fleets,int fleet)
        {
            int at=fleet*492;
            for(int n=0;n<100;n++)I(fleets,at+64+n*4,3);
            for(int n=0;n<6;n++)
            {
                uint r=BitConverter.ToUInt32(fleets,at+16+n*4);
                if(r==0)continue;
                int p=Reference(r);
                if(I(ships,p)==0)continue;
                int start=W(ships,p+8)*10+W(ships,p+10),length=W(ships,p+14);
                if(length<0||length>6||start<0||start+length>100)throw new InvalidDataException("Installation occupancy crosses grid.");
                for(int k=0;k<length;k++)I(fleets,at+64+(start+k)*4,2);
            }
        }
        public PracticeAirfieldFactoryResult Create(byte[] shipPool,byte[] fleetPool,int firstFleet,Point origin,short owner,byte flag)
        {
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000)throw new InvalidDataException("Complete pools required.");
            if(firstFleet<0||firstFleet>246||origin.X<short.MinValue||origin.X>short.MaxValue||origin.Y<short.MinValue||origin.Y>short.MaxValue)throw new ArgumentOutOfRangeException("firstFleet/origin");
            for(int n=0;n<4;n++)
            {
                int p=(firstFleet+n)*492;
                if(I(fleetPool,p)!=0||W(fleetPool,p+4)!=firstFleet+n)throw new InvalidDataException("Four initialized inactive consecutive fleets required.");
                for(int q=0;q<6;q++)if(I(fleetPool,p+16+q*4)!=0)throw new InvalidDataException("Installation destination has stale member references.");
            }
            var ships=(byte[])shipPool.Clone();
            var fleets=(byte[])fleetPool.Clone();
            var allocated=new List<int>();
            int[][][] positions=
            {
                new[]
                {
                    new[]
                    {
                        20,6,1
                    },new[]
                    {
                        24,2,6
                    }
                },new[]
                {
                    new[]
                    {
                        19,5,5
                    },new[]
                    {
                        21,8,2
                    }
                },new[]
                {
                    new[]
                    {
                        22,1,4
                    },new[]
                    {
                        22,1,4
                    },new[]
                    {
                        22,4,4
                    },new[]
                    {
                        22,4,4
                    }
                },new[]
                {
                    new[]
                    {
                        23,5,2
                    },new[]
                    {
                        23,5,2
                    }
                }
            };
            for(int n=0;n<4;n++)
            {
                int slot=firstFleet+n,at=slot*492;
                I(fleets,at,2);
                W(fleets,at+12,origin.X+(n==0||n==3?10:0));
                W(fleets,at+14,origin.Y+(n>=2?10:0));
                W(fleets,at+6,owner);
                if(n<2)W(fleets,at+488,owner);
                if(n==0)W(fleets,at+10,0);
                fleets[at+464]=fleets[at+465]=fleets[at+466]=30;
                fleets[at+63]=0;
                fleets[at+484]=flag;
                for(int member=0;member<positions[n].Length;member++)
                {
                    var p=positions[n][member];
                    int unit=Allocate(ships,fleets);
                    Construct(ships,unit,p[0],owner,slot,p[1],p[2]);
                    I(fleets,at+16+member*4,0x4fc340+unit*268);
                    allocated.Add(unit);
                }
                Grid(ships,fleets,slot);
            }
            return new PracticeAirfieldFactoryResult(ships,fleets,allocated);
        }
    }
}
