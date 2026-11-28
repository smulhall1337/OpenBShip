using System;using System.Collections.Generic;using System.Collections.ObjectModel;using System.Drawing;using System.IO;
namespace FleetLab
{
    public sealed class PracticeReinforcementCreation
    {
        public int Fleet{get;private set;}public int Group{get;private set;}public int Slot{get;private set;}public int Template{get;private set;}public Point World{get;private set;}
        public ReadOnlyCollection<int> Units{get;private set;}
        internal PracticeReinforcementCreation(int fleet,int group,int slot,int template,int x,int y,List<int> units){Fleet=fleet;Group=group;Slot=slot;Template=template;World=new Point(x,y);Units=new List<int>(units).AsReadOnly();}
    }
    // B9A8...6A569: actual reset fragment +405C20 bank5 ->406940 ->406060
    // ->405E10/extended15/16. Original1092R2/independent1093R2/fresh1095.
    // This owns copied comparison pools; it is not a live-world/AI initializer.
    public sealed class PracticeGeneratedReinforcements
    {
        public const int MaximumVisits=128;
        readonly PracticeReinforcementTables tables;readonly byte[] ships,fleets,runtime,beforeTable,terrain;readonly int[] deadlines,payloads;readonly uint[] ownerCounters;
        readonly PracticeReinforcementMapBasis mapBasis;readonly List<int> allCreated;
        readonly ReadOnlyCollection<PracticeReinforcementCreation> created;
        public int ProducerCount{get;private set;}public int ConsumerCount{get;private set;}public uint InitialSeed{get;private set;}public uint InitialClock{get;private set;}public uint InitialCounter{get;private set;}public bool ReserveFirstFleet{get;private set;}
        public uint Clock{get;private set;}public uint InputRandomState{get;private set;}public uint RandomState{get;private set;}public uint NextFleetIndex{get;private set;}public uint RefreshRequests{get;private set;}
        public int Visits{get;private set;}public int CreatedFleetCount{get;private set;}public int CreatedUnitCount{get;private set;}public int StudyRandomDraws{get;private set;}
        public ReadOnlyCollection<PracticeReinforcementCreation> LastCreated{get{return created;}}
        public bool UsesOccupiedMap{get{return mapBasis!=null;}}
        public int MapProfile{get{return mapBasis==null?-1:mapBasis.Profile;}}
        public string BasisDescription{get{return mapBasis==null?"Empty comparison fixture":PracticeReinforcementMapBasis.ProfileNames[mapBasis.Profile];}}
        public uint InitialRandomState{get{return mapBasis==null?InitialSeed:mapBasis.RandomState;}}
        public int ExistingFleetCount{get{return mapBasis==null?0:PracticeReinforcementMapBasis.FleetCount;}}
        public int ExistingUnitCount{get{return mapBasis==null?0:PracticeReinforcementMapBasis.UnitCount;}}
        public bool CanVisit{get{return Visits<MaximumVisits;}}
        public int PendingEvents{get{int count=0;for(int n=0;n<ConsumerCount*4;n++)if(deadlines[n]!=-1)count++;return count;}}
        public int? NextDeadline{get{int next=int.MaxValue;for(int n=0;n<ConsumerCount*4;n++)if(deadlines[n]!=-1&&deadlines[n]<next)next=deadlines[n];return next==int.MaxValue?(int?)null:next;}}
        public static PracticeGeneratedReinforcements Create(PracticeReinforcementTables tables,int producerCount,int consumerCount,uint seed,uint initialClock,bool reserveFirstFleet,uint initialCounter)
        {
            var before=new byte[128];for(int n=0;n<before.Length;n++)before[n]=(byte)(0x5b^n);return CreateFixture(tables,producerCount,consumerCount,seed,initialClock,reserveFirstFleet,initialCounter,before);
        }
        public static PracticeGeneratedReinforcements CreateMapped(PracticeReinforcementTables tables,string programs,byte[] map,int producerCount,int consumerCount,uint startupSeed,int profile)
        {
            var before=new byte[128];for(int n=0;n<before.Length;n++)before[n]=(byte)(0x5b^n);return CreateMappedFixture(tables,programs,map,producerCount,consumerCount,startupSeed,profile,before);
        }
        internal static PracticeGeneratedReinforcements CreateDriverIdleFixture(PracticeReinforcementTables tables,string programs,byte[] map,uint startupSeed,byte[] before)
        {
            var state=CreateMappedFixture(tables,programs,map,2,2,startupSeed,0,before);
            // Original1132R2: a deliberately supplied idle context, not source
            // AI initialization. 41F920 always reads the fleet link at +204.
            for(int fleet=0;fleet<250;fleet++){I(state.runtime,fleet*312+204,0x4b47a0+fleet*492);I(state.runtime,fleet*312+304,0);}
            state.fleets[249*492+480]=1;Array.Clear(state.terrain,0,state.terrain.Length);return state;
        }
        internal PracticeGeneratedReinforcements PrepareDriverFrame(uint clock)
        {
            var next=new PracticeGeneratedReinforcements(this,new List<PracticeReinforcementCreation>());next.Clock=clock;return next;
        }
        internal PracticeGeneratedReinforcements PollSourceDriver(){return Visit(Clock);}
        internal PracticeGeneratedReinforcements ApplyNeutralDriverVisibility(PracticeOwnerVisibilityRules rules)
        {
            if(rules==null)throw new ArgumentNullException("rules");var next=new PracticeGeneratedReinforcements(this,new List<PracticeReinforcementCreation>(created));
            next.InputRandomState=InputRandomState;next.RefreshRequests=RefreshRequests;Array.Copy(rules.Apply(fleets,-1,0,-2,0,249).CopyFleetPool(),next.fleets,fleets.Length);return next;
        }
        internal static PracticeGeneratedReinforcements CreateMappedFixture(PracticeReinforcementTables tables,string programs,byte[] map,int producerCount,int consumerCount,uint startupSeed,int profile,byte[] before)
        {
            Validate(tables,producerCount,consumerCount,0,before);var basis=PracticeReinforcementMapBasis.Create(programs,map,startupSeed,profile);return new PracticeGeneratedReinforcements(tables,producerCount,consumerCount,startupSeed,0,false,0,before,basis);
        }
        internal static PracticeGeneratedReinforcements CreateFixture(PracticeReinforcementTables tables,int producerCount,int consumerCount,uint seed,uint initialClock,bool reserveFirstFleet,uint initialCounter,byte[] beforeTable)
        {
            Validate(tables,producerCount,consumerCount,initialClock,beforeTable);return new PracticeGeneratedReinforcements(tables,producerCount,consumerCount,seed,initialClock,reserveFirstFleet,initialCounter,beforeTable,null);
        }
        static void Validate(PracticeReinforcementTables tables,int producerCount,int consumerCount,uint initialClock,byte[] beforeTable)
        {
            if(tables==null)throw new ArgumentNullException("tables");if(producerCount<1||producerCount>4||consumerCount<1||consumerCount>producerCount)throw new ArgumentOutOfRangeException("producerCount/consumerCount");if(initialClock!=0&&initialClock!=1000)throw new NotSupportedException("This study admits supplied nonwrapping clock bases 0 and 1000 only.");if(beforeTable==null||beforeTable.Length!=128)throw new InvalidDataException("Expected sixteen deadline/payload pairs.");
        }
        PracticeGeneratedReinforcements(PracticeReinforcementTables tables,int pc,int cc,uint seed,uint clockBase,bool reserve,uint counter,byte[] before,PracticeReinforcementMapBasis basis)
        {
            this.tables=tables;ProducerCount=pc;ConsumerCount=cc;InitialSeed=seed;InitialClock=clockBase;InitialCounter=counter;ReserveFirstFleet=reserve;Clock=clockBase;InputRandomState=seed;RandomState=seed;RefreshRequests=1;beforeTable=(byte[])before.Clone();ships=new byte[402000];fleets=new byte[123000];runtime=new byte[78000];deadlines=new int[16];payloads=new int[16];ownerCounters=new uint[4];created=new List<PracticeReinforcementCreation>().AsReadOnly();
            for(int n=0;n<1500;n++)W(ships,n*268+4,n);for(int n=0;n<250;n++)W(fleets,n*492+4,n);fleets[490]=reserve?(byte)1:(byte)0;I(fleets,249*492,20);W(fleets,249*492+14,250);for(int n=0;n<runtime.Length;n++)runtime[n]=0xa7;for(int n=0;n<4;n++)ownerCounters[n]=counter;mapBasis=basis;allCreated=new List<int>();terrain=basis==null?new byte[246000]:(byte[])basis.Terrain.Clone();if(basis!=null){Array.Copy(basis.Ships,ships,ships.Length);Array.Copy(basis.Fleets,fleets,fleets.Length);Array.Copy(basis.Counters,ownerCounters,4);NextFleetIndex=basis.NextFleet;InputRandomState=basis.RandomState;RandomState=basis.RandomState;}
            // Literal 402D99 reset semantics: deadline words only. Untouched
            // payload bytes remain supplied data, including outside consumers.
            for(int n=0;n<16;n++){deadlines[n]=-1;payloads[n]=I(before,64+n*4);}
            var random=new NativeRandom(InitialRandomState);int[] first={1,3,7,11},range={2,3,3,3};for(int group=0;group<pc;group++)for(int slot=0;slot<4;slot++){int at=group*4+slot;uint seconds=(uint)(first[slot]*60)+(uint)random.Next()%(uint)(range[slot]*60);deadlines[at]=unchecked((int)(clockBase+seconds*1000u));payloads[at]=slot;}RandomState=random.State;StudyRandomDraws=random.Calls;
        }
        PracticeGeneratedReinforcements(PracticeGeneratedReinforcements source,List<PracticeReinforcementCreation> events)
        {
            mapBasis=source.mapBasis;terrain=source.terrain;allCreated=new List<int>(source.allCreated);tables=source.tables;ships=(byte[])source.ships.Clone();fleets=(byte[])source.fleets.Clone();runtime=(byte[])source.runtime.Clone();beforeTable=source.beforeTable;deadlines=(int[])source.deadlines.Clone();payloads=(int[])source.payloads.Clone();ownerCounters=(uint[])source.ownerCounters.Clone();created=events.AsReadOnly();ProducerCount=source.ProducerCount;ConsumerCount=source.ConsumerCount;InitialSeed=source.InitialSeed;InitialClock=source.InitialClock;InitialCounter=source.InitialCounter;ReserveFirstFleet=source.ReserveFirstFleet;Clock=source.Clock;RandomState=source.RandomState;InputRandomState=source.RandomState;NextFleetIndex=source.NextFleetIndex;CreatedFleetCount=source.CreatedFleetCount;CreatedUnitCount=source.CreatedUnitCount;StudyRandomDraws=source.StudyRandomDraws;Visits=source.Visits;
        }
        public PracticeGeneratedReinforcements ResetCopy(){return new PracticeGeneratedReinforcements(tables,ProducerCount,ConsumerCount,InitialSeed,InitialClock,ReserveFirstFleet,InitialCounter,beforeTable,mapBasis);}
        public PracticeGeneratedReinforcements AtNextDeadline(){return Visit(RequireNext());}
        public PracticeGeneratedReinforcements JustAfterNextDeadline(){return Visit(checked(RequireNext()+1u));}
        public PracticeGeneratedReinforcements RepeatClock(){return Visit(Clock);}
        public PracticeGeneratedReinforcements JumpPastRemaining(){RequireNext();int latest=-1;for(int n=0;n<ConsumerCount*4;n++)if(deadlines[n]>latest)latest=deadlines[n];return Visit(checked((uint)latest+1u));}
        public PracticeGeneratedReinforcements FinishInDeadlineOrder(){var result=this;int boundaries=0;while(result.NextDeadline.HasValue){if(boundaries++>=16)throw new InvalidDataException("Generated history exceeded sixteen source slots.");result=result.JustAfterNextDeadline();}return result;}
        uint RequireNext(){if(!NextDeadline.HasValue)throw new InvalidOperationException("No deadlines remain within the supplied consumer count.");return checked((uint)NextDeadline.Value);}
        PracticeGeneratedReinforcements Visit(uint clock)
        {
            if(!CanVisit)throw new InvalidOperationException("The modern 128-visit study limit has been reached; reset or load a new copy.");var events=new List<PracticeReinforcementCreation>();var next=new PracticeGeneratedReinforcements(this,events);next.Clock=clock;next.Visits++;
            for(int at=0;at<ConsumerCount*4;at++)
            {
                int deadline=next.deadlines[at];if(deadline==-1||unchecked((int)clock)<=deadline)continue;
                var point=PracticeScenarioPositions.Select(5,at/4,next.RandomState,next.fleets,next.terrain,128);next.RandomState=point.RandomState;next.StudyRandomDraws+=point.RandomCalls;var made=next.Construct(next.payloads[at],at/4,at%4,point.X,point.Y);events.Add(made);next.deadlines[at]=-1;next.RefreshRequests++;
            }
            return next;
        }
        PracticeReinforcementCreation Construct(int template,int group,int slot,int x,int y)
        {
            if(template<0||template>3)throw new NotSupportedException("Only generated bank-five payloads 0..3 are admitted.");int fleet=-1;for(int n=0;n<250;n++)if(I(fleets,n*492)==0&&fleets[n*492+490]==0){fleet=n;break;}if(fleet<0)throw new InvalidDataException("No admitted primary fleet slot.");int at=fleet*492,context=fleet*312;
            W(fleets,at+8,0);I(fleets,at,0);I(fleets,at+472,0);I(fleets,at+476,0);foreach(int offset in new[]{62,468,483,467,480,481,485,486})fleets[at+offset]=0;fleets[at+482]=255;Array.Clear(fleets,at+16,24);I(fleets,at+40,-1);I(fleets,at+44,-1);W(fleets,at+48,-1);
            I(runtime,context+200,0);for(int n=0;n<10;n++)I(runtime,context+n*20,0);for(int n=0;n<6;n++){I(runtime,context+212+n*4,0);I(runtime,context+236+n*4,0);}
            I(fleets,at,1);W(fleets,at+12,x);W(fleets,at+14,y);W(fleets,at+6,group);W(fleets,at+60,2);fleets[at+63]=0;fleets[at+484]=unchecked((byte)ownerCounters[group]);fleets[at+485]=2;ownerCounters[group]=unchecked(ownerCounters[group]+1);
            var units=new List<int>();for(int member=0;member<6;member++){int p=template*76+4+member*12,type=I(tables.Templates,p);if(type==0)break;int unit=AllocateUnit();tables.ConstructUnit(ships,unit,type,group,fleet,I(tables.Templates,p+4),I(tables.Templates,p+8));I(fleets,at+16+member*4,0x4fc340+unit*268);units.Add(unit);}
            byte speed=0;foreach(int unit in units){int current=W(ships,unit*268+72);if(speed==0)speed=unchecked((byte)current);if(speed>current)speed=unchecked((byte)current);}W(fleets,at+60,speed);Array.Copy(tables.Catalog.Colors,3,fleets,at+464,3);for(int n=0;n<100;n++)I(fleets,at+64+n*4,0);foreach(int unit in units){int cell=W(ships,unit*268+8)*10+W(ships,unit*268+10),length=W(ships,unit*268+14);if(cell<0||cell+length>100)throw new InvalidDataException("Generated unit exceeded the source grid.");for(int n=0;n<length;n++)I(fleets,at+64+(cell+n)*4,2);}fleets[at+468]=0;NextFleetIndex=(uint)(fleet+1);CreatedFleetCount++;CreatedUnitCount+=units.Count;allCreated.Add(fleet);return new PracticeReinforcementCreation(fleet,group,slot,template,x,y,units);
        }
        int AllocateUnit()
        {
            // 42CEF0, composed with occupied pools in original1103. A bay's
            // inactive record is not free, nor is a slot before kind22/23.
            var reserved=new HashSet<int>();
            for(int fleet=0;fleet<250;fleet++)
            {
                int at=fleet*492,kind=I(fleets,at);if(kind!=2&&kind!=10)continue;
                for(int member=0;member<6;member++)
                {
                    uint reference=BitConverter.ToUInt32(fleets,at+16+member*4);if(reference==0)continue;
                    int offset=PracticeReinforcementMapBasis.UnitOffset(reference);if(I(ships,offset)==0)reserved.Add(W(ships,offset+4));
                }
            }
            for(int unit=0;unit<1500;unit++)
            {
                int at=unit*268;if(I(ships,at)!=0||reserved.Contains(unit))continue;
                if(unit<1499&&(I(ships,at+268)==22||I(ships,at+268)==23))continue;
                return unit;
            }
            throw new InvalidDataException("No admitted ship slot; no partial copied state was published.");
        }
        public byte[] CopyTerrainPool(){return (byte[])terrain.Clone();}
        public byte[] CopyExistingFleetPool(){return mapBasis==null?new byte[123000]:(byte[])mapBasis.Fleets.Clone();}
        public int DeadlineAt(int group,int slot){return deadlines[Index(group,slot)];}public int PayloadAt(int group,int slot){return payloads[Index(group,slot)];}
        static int Index(int group,int slot){if(group<0||group>3||slot<0||slot>3)throw new ArgumentOutOfRangeException("group/slot");return group*4+slot;}
        public uint OwnerCounter(int group){if(group<0||group>3)throw new ArgumentOutOfRangeException("group");return ownerCounters[group];}
        public ReadOnlyCollection<int> FleetIds(){return allCreated.AsReadOnly();}
        public ReadOnlyCollection<int> UnitIds(int fleet){if(fleet<0||fleet>=250||!allCreated.Contains(fleet)||I(fleets,fleet*492)!=1)throw new ArgumentOutOfRangeException("fleet");var ids=new List<int>();for(int n=0;n<6;n++){uint reference=BitConverter.ToUInt32(fleets,fleet*492+16+n*4);if(reference==0)continue;long offset=(long)reference-0x4fc340;if(offset<0||offset>=ships.Length||offset%268!=0)throw new InvalidDataException("Generated roster reference.");ids.Add((int)(offset/268));}return ids.AsReadOnly();}
        public byte[] CopyShipPool(){return (byte[])ships.Clone();}public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}public byte[] CopyRuntimePool(){return (byte[])runtime.Clone();}
        internal void WriteOriginalSnapshot(BinaryWriter writer){writer.Write(Clock);writer.Write(InputRandomState);writer.Write(RefreshRequests);writer.Write(NextFleetIndex);writer.Write(RandomState);writer.Write(InitialClock);foreach(uint value in ownerCounters)writer.Write(value);foreach(int value in deadlines)writer.Write(value);foreach(int value in payloads)writer.Write(value);writer.Write(ships);writer.Write(fleets);writer.Write(runtime);}
        static int I(byte[] b,int at){return PracticeReinforcementTables.I(b,at);}static int W(byte[] b,int at){return PracticeReinforcementTables.W(b,at);}static void I(byte[] b,int at,int value){PracticeReinforcementTables.I(b,at,value);}static void W(byte[] b,int at,int value){PracticeReinforcementTables.W(b,at,value);}
    }
}
