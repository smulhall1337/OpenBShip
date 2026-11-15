using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
namespace FleetLab
{
    public sealed class PracticeInstallationInitializationResult
    {
        readonly byte[] ships,fleets;
        readonly ReadOnlyCollection<int> aircraft;
        public int StockReferences
        {
            get;
            private set;
        }
        public ReadOnlyCollection<int> CreatedAircraftFleets
        {
            get
            {
                return aircraft;
            }
        }
        internal PracticeInstallationInitializationResult(byte[] s,byte[] f,int stock,List<int> created)
        {
            ships=s;
            fleets=f;
            StockReferences=stock;
            aircraft=created.AsReadOnly();
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
    // Pinned RIP B9A8...6A569:42BF10..42C00D, localowner-2/network0.
    // This post-construction pass is separate from active support/AI updates.
    public sealed class PracticeInstallationInitialization
    {
        readonly PracticeUnitCatalog units;
        readonly byte[] stock;
        public PracticeInstallationInitialization(PracticeUnitCatalog units,byte[] sourceStock)
        {
            if(units==null)throw new ArgumentNullException("units");
            if(sourceStock==null||sourceStock.Length!=70)throw new InvalidDataException("Installation stock requires 35 complete little-endian words.");
            using(var h=SHA256.Create())if(BitConverter.ToString(h.ComputeHash(sourceStock)).Replace("-","")!="B04CCA1D57AC6303B54D82C282EA1815B890131C720F40829BD49B993409CF25")throw new InvalidDataException("Unsupported installation stock table.");
            this.units=units;
            stock=(byte[])sourceStock.Clone();
        }
        public static PracticeInstallationInitialization Load(string programs)
        {
            return new PracticeInstallationInitialization(PracticeUnitCatalog.Load(programs),File.ReadAllBytes(Path.Combine(programs,"InstallationStock.seqbin")));
        }
        public PracticeInstallationInitializationResult ApplyOffscreen(byte[] shipPool,byte[] fleetPool,int playerCount)
        {
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000)throw new InvalidDataException("Installation initialization requires complete ship and fleet pools.");
            if(playerCount<0||playerCount>4)throw new ArgumentOutOfRangeException("playerCount");
            var ships=(byte[])shipPool.Clone();
            var fleets=(byte[])fleetPool.Clone();
            int stockReferences=0,nextFree=0;
            var created=new List<int>();
            for(int fleet=0;fleet<250;fleet++)for(int member=0;member<6;member++)
            {
                uint reference=BitConverter.ToUInt32(fleets,fleet*492+16+member*4);
                if(reference==0)continue;
                long offset=(long)reference-0x4fc340;
                if(offset<0||offset>=402000||offset%268!=0)throw new InvalidDataException("Installation initialization encountered an invalid member reference.");
                int at=(int)offset,type=BitConverter.ToInt32(ships,at);
                if(type==19)
                {
                    Array.Copy(stock,0,ships,at+196,70);
                    stockReferences++;
                }
                else if(type==20&&(fleet&7)==0&&fleet<playerCount*8)
                {
                    while(nextFree<250&&BitConverter.ToInt32(fleets,nextFree*492)!=0)nextFree++;
                    if(nextFree==250)throw new InvalidDataException("Installation initialization has no free aircraft fleet; no partial result was published.");
                    int destination=nextFree++,parent=BitConverter.ToInt16(ships,at+4);
                    var aircraft=PracticeAircraftFactory.CreateOffscreen(units,ships,fleets,parent,fleet,destination,8,0,-2);
                    ships=aircraft.CopyShipPool();
                    fleets=aircraft.CopyFleetPool();
                    created.Add(destination);
                }
            }
            return new PracticeInstallationInitializationResult(ships,fleets,stockReferences,created);
        }
    }
}
