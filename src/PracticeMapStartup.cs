using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
namespace FleetLab
{
    public sealed class PracticeMapStartupStage
    {
        readonly byte[] ships,fleets;
        readonly uint[] counters;
        public string Name { get; private set; }
        public uint RandomState { get; private set; }
        public int RandomCalls { get; private set; }
        public int NextInactiveFleet { get; private set; }
        public int NonemptyUnits { get; private set; }
        public int NonemptyFleets { get; private set; }
        internal PracticeMapStartupStage(string name,byte[] s,byte[] f,uint[] counts,uint random,int calls)
        {
            Name=name;ships=(byte[])s.Clone();fleets=(byte[])f.Clone();counters=(uint[])counts.Clone();RandomState=random;RandomCalls=calls;NextInactiveFleet=250;
            for(int n=0;n<1500;n++)if(BitConverter.ToInt32(s,n*268)!=0)NonemptyUnits++;
            for(int n=0;n<250;n++)if(BitConverter.ToInt32(f,n*492)!=0)NonemptyFleets++;else if(NextInactiveFleet==250)NextInactiveFleet=n;
        }
        public byte[] CopyShipPool(){return (byte[])ships.Clone();}
        public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
        public uint[] CopyOwnerCounters(){return (uint[])counters.Clone();}
        public override string ToString(){return Name;}
    }
    // Ordinary selector2 primary-pool subset only. Fixed supplied BSSB202 map;
    // root/base/object/group/airfield/stock/formation/late data stages recovered.
    // Secondary terrain, SCN in-place mutation, FLT preset globals, callback/UI,
    // AI, mission logic and natural application timing are intentionally absent.
    public sealed class PracticeMapStartup
    {
        public const string SupportedSourceHash="1355582241BD807AE2943F5A67D32BB23EA58A77A388BE8928AEA41F5E892F4F";
        readonly byte[] source;
        readonly ReadOnlyCollection<PracticeMapStartupStage> stages;
        public uint Seed { get; private set; }
        public ReadOnlyCollection<PracticeMapStartupStage> Stages { get { return stages; } }
        public byte[] CopySource(){return (byte[])source.Clone();}
        PracticeMapStartup(byte[] input,uint seed,List<PracticeMapStartupStage> states){source=input;Seed=seed;stages=states.AsReadOnly();}
        public static PracticeMapStartup CreateBssb202(string programs,byte[] input,uint seed)
        {
            if(input==null||input.Length!=8890||Hash(input)!=SupportedSourceHash)throw new InvalidDataException("Ordinary map startup currently admits the supplied8890-byte BSSB202.SCN identity only.");
            var builder=new Builder(programs,(byte[])input.Clone(),seed);
            return builder.Build(seed);
        }
        static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","");}
        sealed class Builder
        {
            readonly byte[] source,stock;
            readonly Scenario scene;
            readonly PracticeUnitCatalog catalog;
            readonly PracticeAirfieldFactory bases;
            readonly List<PracticeMapStartupStage> states=new List<PracticeMapStartupStage>();
            readonly List<PracticeFleetOrigin> reservations=new List<PracticeFleetOrigin>();
            byte[] ships,fleets;
            uint[] counters;
            NativeRandom random;
            int nextFleet,nextUnit,calls;
            static int I(byte[] b,int at){return BitConverter.ToInt32(b,at);}static int W(byte[] b,int at){return BitConverter.ToInt16(b,at);}
            internal Builder(string programs,byte[] input,uint seed)
            {
                source=input;scene=Scenario.Load(input,"BSSB202.SCN");catalog=PracticeUnitCatalog.Load(programs);bases=PracticeAirfieldFactory.Load(programs);random=new NativeRandom(seed);
                stock=File.ReadAllBytes(Path.Combine(programs,"InstallationStock.seqbin"));
                if(stock.Length!=70||Hash(stock)!="B04CCA1D57AC6303B54D82C282EA1815B890131C720F40829BD49B993409CF25")throw new InvalidDataException("Ordinary startup requires the pinned installation stock table.");
            }
            internal PracticeMapStartup Build(uint seed)
            {
                LoadEntities();Capture("1 · Bases and map objects");
                LoadGroups();LoadAirfieldsAndStock();Capture("2 · Grouped fleets and stock");
                // Selector2's403570 preset/global load leaves these pools and RNG
                // unchanged. No corresponding global/template behavior is claimed.
                for(int fleet=0;fleet<250;fleet++)
                {
                    int kind=I(fleets,fleet*492);if(kind!=1&&kind!=9)continue;
                    var result=PracticeFormationPlacement.Apply(ships,fleets,fleet,random.State);
                    ships=result.CopyShipPool();fleets=result.CopyFleetPool();random=new NativeRandom(result.RandomState);calls+=result.RandomCalls;
                }
                Capture("3 · Practice fleet placement");
                var late=PracticeLateConfiguration.ApplyOffscreen(catalog,ships,fleets,-2);ships=late.CopyShipPool();fleets=late.CopyFleetPool();
                Capture("4 · Late roster and carrier data");
                return new PracticeMapStartup(source,seed,states);
            }
            void Capture(string name){states.Add(new PracticeMapStartupStage(name,ships,fleets,counters,random.State,calls));}
            void Merge(PracticeFleetSnapshot part)
            {
                if(nextFleet>=250||nextUnit+part.Units.Count>1500||part.FleetSlot!=nextFleet||part.FirstShipSlot!=nextUnit)throw new InvalidDataException("Ordinary startup exceeds its admitted sequential allocation profile.");
                Array.Copy(part.CopyShipPool(),nextUnit*268,ships,nextUnit*268,part.Units.Count*268);Array.Copy(part.CopyFleetRecord(),0,fleets,nextFleet*492,492);
                nextUnit+=part.Units.Count;nextFleet++;reservations.Add(new PracticeFleetOrigin(part.Kind,part.WorldOrigin));
            }
            void LoadEntities()
            {
                var built=PracticeMapInstallations.Create(bases,source,"BSSB202.SCN");ships=built.CopyShipPool();fleets=built.CopyFleetPool();counters=built.CopyOwnerCounters();nextFleet=built.NextFleetSlot;nextUnit=built.UnitCount;
                for(int n=0;n<nextFleet;n++)reservations.Add(new PracticeFleetOrigin(I(fleets,n*492),new Point(W(fleets,n*492+12),W(fleets,n*492+14))));
                for(int x=0;x<64;x++)for(int y=0;y<64;y++)
                {
                    var cell=scene.At(x,y);if(cell.Kind!=7)continue;
                    int owner=unchecked((sbyte)cell.Value),length=W(catalog.MapObjectTemplate,14);
                    if(length!=2)throw new InvalidDataException("Ordinary type25 single-unit placement extent changed.");
                    var local=new Point(random.Next()%10,random.Next()%(length-11));
                    Merge(PracticeFleetSnapshot.CreateMapObject(catalog,owner,new Point(x*10,y*10),nextFleet,nextUnit,local));
                }
                calls=random.Calls;
            }
            void LoadGroups()
            {
                counters=new uint[]{1,1,1,1,1};var planner=new PracticeSpawnPlanner(scene,random.State,reservations);
                for(int owner=0;owner<scene.Players;owner++)for(int group=0;group<2;group++)foreach(var bytes in scene.Groups[owner][group])
                {
                    int kind=group==0?1:9;Point origin=planner.NextOrigin(owner);
                    var part=PracticeFleetSnapshot.CreateDeployment(catalog,bytes,owner,origin,counters[owner+1],kind,nextFleet,nextUnit,true);
                    counters[owner+1]=part.NextCounter;Merge(part);planner.Reserve(kind,origin);
                }
                calls+=planner.RandomCalls;random=new NativeRandom(planner.RandomState);
            }
            int Metadata(int x,int y,int offset)
            {
                y+=offset/402;int at=offset%402;
                if(x<0||x>=64||y<0||y>=64)throw new InvalidDataException("Ordinary base metadata crosses its admitted SCN columns.");
                var cell=scene.At(x,y);
                if(at==0)return unchecked((sbyte)cell.Kind);
                if(at==1)return unchecked((sbyte)cell.Value);
                return cell.Payload==null?0:unchecked((sbyte)cell.Payload[at-2]);
            }
            void LoadAirfieldsAndStock()
            {
                for(int fleet=0;fleet<250;fleet++)
                {
                    int at=fleet*492;if(I(fleets,at)!=2)continue;int parent=-1,type24Unit=-1;
                    for(int member=0;member<6;member++)
                    {
                        uint reference=BitConverter.ToUInt32(fleets,at+16+member*4);if(reference==0)continue;int unit=(int)((reference-0x4fc340)/268),type=I(ships,unit*268);
                        if(type==20)parent=unit;if(type==24)type24Unit=unit;
                    }
                    if(parent<0&&type24Unit<0)continue;
                    int x=W(fleets,at+12)/10,y=W(fleets,at+14)/10,tag=Metadata(x,y,0),type24Word,awacs,helicopters,fighters;
                    //403A50's source-derived layouts; the currently pinned map
                    //reaches tag31. Offsets can continue in neighboring402-byte cells.
                    if(tag==3){type24Word=Metadata(x,y,0x6b);awacs=Metadata(x,y,0xf7);helicopters=Metadata(x,y,0xf8);fighters=Metadata(x,y,0xf9);}
                    else if(tag==31){type24Word=Metadata(x,y,0x1e9);awacs=Metadata(x,y,0x295);helicopters=Metadata(x,y,0x296);fighters=Metadata(x,y,0x297);}
                    else if(tag==32){type24Word=Metadata(x,y,0xff);awacs=Metadata(x,y,0x1f1);helicopters=Metadata(x,y,0x1f2);fighters=Metadata(x,y,0x1f3);}
                    else if(tag==33){type24Word=Metadata(x,y,0x225);awacs=Metadata(x-1,y,0x239);helicopters=Metadata(x-1,y,0x23a);fighters=Metadata(x-1,y,0x23b);}
                    else throw new NotSupportedException("Ordinary base metadata has an unadmitted source tag.");
                    //Source writes type24+242 (overlaps ammo index23). Preserve
                    //the field without inferring another gameplay meaning.
                    if(type24Unit>=0)BitConverter.GetBytes(unchecked((short)type24Word)).CopyTo(ships,type24Unit*268+242);
                    if(parent<0)continue;
                    if(awacs<0||helicopters<0||fighters<0||awacs+helicopters+fighters>4)throw new InvalidDataException("Ordinary airfield attachments exceed the copied four-link profile.");
                    int link=0;
                    foreach(var group in new[]{new[]{8,awacs},new[]{6,helicopters},new[]{7,fighters}})for(int n=0;n<group[1];n++)
                    {
                        var result=PracticeAircraftFactory.CreateOffscreen(catalog,ships,fleets,parent,fleet,nextFleet,group[0],link++,-2);ships=result.CopyShipPool();fleets=result.CopyFleetPool();
                        for(int k=0;k<result.AllocatedUnits.Count;k++)if(result.AllocatedUnits[k]!=nextUnit+k)throw new InvalidDataException("Ordinary aircraft allocation left the admitted sequential profile.");
                        nextUnit+=result.AllocatedUnits.Count;nextFleet++;
                    }
                }
                for(int fleet=0;fleet<250;fleet++)for(int member=0;member<6;member++)
                {
                    uint reference=BitConverter.ToUInt32(fleets,fleet*492+16+member*4);if(reference==0)continue;int unit=(int)((reference-0x4fc340)/268);
                    if(I(ships,unit*268)==19)Array.Copy(stock,0,ships,unit*268+196,70);
                }
            }
        }
    }
}
