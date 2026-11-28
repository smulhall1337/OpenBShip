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
        readonly PracticeReinforcementTables tables;readonly byte[] ships,fleets,runtime,beforeTable;readonly int[] deadlines,payloads;readonly uint[] ownerCounters;
        readonly ReadOnlyCollection<PracticeReinforcementCreation> created;
        public int ProducerCount{get;private set;}public int ConsumerCount{get;private set;}public uint InitialSeed{get;private set;}public uint InitialClock{get;private set;}public uint InitialCounter{get;private set;}public bool ReserveFirstFleet{get;private set;}
        public uint Clock{get;private set;}public uint InputRandomState{get;private set;}public uint RandomState{get;private set;}public uint NextFleetIndex{get;private set;}public uint RefreshRequests{get;private set;}
        public int Visits{get;private set;}public int CreatedFleetCount{get;private set;}public int CreatedUnitCount{get;private set;}public int StudyRandomDraws{get;private set;}
        public ReadOnlyCollection<PracticeReinforcementCreation> LastCreated{get{return created;}}
        public bool CanVisit{get{return Visits<MaximumVisits;}}
        public int PendingEvents{get{int count=0;for(int n=0;n<ConsumerCount*4;n++)if(deadlines[n]!=-1)count++;return count;}}
        public int? NextDeadline{get{int next=int.MaxValue;for(int n=0;n<ConsumerCount*4;n++)if(deadlines[n]!=-1&&deadlines[n]<next)next=deadlines[n];return next==int.MaxValue?(int?)null:next;}}
        public static PracticeGeneratedReinforcements Create(PracticeReinforcementTables tables,int producerCount,int consumerCount,uint seed,uint initialClock,bool reserveFirstFleet,uint initialCounter)
        {
            var before=new byte[128];for(int n=0;n<before.Length;n++)before[n]=(byte)(0x5b^n);return CreateFixture(tables,producerCount,consumerCount,seed,initialClock,reserveFirstFleet,initialCounter,before);
        }
        internal static PracticeGeneratedReinforcements CreateFixture(PracticeReinforcementTables tables,int producerCount,int consumerCount,uint seed,uint initialClock,bool reserveFirstFleet,uint initialCounter,byte[] beforeTable)
        {
            if(tables==null)throw new ArgumentNullException("tables");if(producerCount<1||producerCount>4||consumerCount<1||consumerCount>producerCount)throw new ArgumentOutOfRangeException("producerCount/consumerCount");if(initialClock!=0&&initialClock!=1000)throw new NotSupportedException("This study admits supplied nonwrapping clock bases 0 and 1000 only.");if(beforeTable==null||beforeTable.Length!=128)throw new InvalidDataException("Expected sixteen deadline/payload pairs.");return new PracticeGeneratedReinforcements(tables,producerCount,consumerCount,seed,initialClock,reserveFirstFleet,initialCounter,beforeTable);
        }
        PracticeGeneratedReinforcements(PracticeReinforcementTables tables,int pc,int cc,uint seed,uint clockBase,bool reserve,uint counter,byte[] before)
        {
            this.tables=tables;ProducerCount=pc;ConsumerCount=cc;InitialSeed=seed;InitialClock=clockBase;InitialCounter=counter;ReserveFirstFleet=reserve;Clock=clockBase;InputRandomState=seed;RandomState=seed;RefreshRequests=1;beforeTable=(byte[])before.Clone();ships=new byte[402000];fleets=new byte[123000];runtime=new byte[78000];deadlines=new int[16];payloads=new int[16];ownerCounters=new uint[4];created=new List<PracticeReinforcementCreation>().AsReadOnly();
            for(int n=0;n<1500;n++)W(ships,n*268+4,n);for(int n=0;n<250;n++)W(fleets,n*492+4,n);fleets[490]=reserve?(byte)1:(byte)0;I(fleets,249*492,20);W(fleets,249*492+14,250);for(int n=0;n<runtime.Length;n++)runtime[n]=0xa7;for(int n=0;n<4;n++)ownerCounters[n]=counter;
            // Literal 402D99 reset semantics: deadline words only. Untouched
            // payload bytes remain supplied data, including outside consumers.
            for(int n=0;n<16;n++){deadlines[n]=-1;payloads[n]=I(before,64+n*4);}
            var random=new NativeRandom(seed);int[] first={1,3,7,11},range={2,3,3,3};for(int group=0;group<pc;group++)for(int slot=0;slot<4;slot++){int at=group*4+slot;uint seconds=(uint)(first[slot]*60)+(uint)random.Next()%(uint)(range[slot]*60);deadlines[at]=unchecked((int)(clockBase+seconds*1000u));payloads[at]=slot;}RandomState=random.State;StudyRandomDraws=random.Calls;
        }
        PracticeGeneratedReinforcements(PracticeGeneratedReinforcements source,List<PracticeReinforcementCreation> events)
        {
            tables=source.tables;ships=(byte[])source.ships.Clone();fleets=(byte[])source.fleets.Clone();runtime=(byte[])source.runtime.Clone();beforeTable=source.beforeTable;deadlines=(int[])source.deadlines.Clone();payloads=(int[])source.payloads.Clone();ownerCounters=(uint[])source.ownerCounters.Clone();created=events.AsReadOnly();ProducerCount=source.ProducerCount;ConsumerCount=source.ConsumerCount;InitialSeed=source.InitialSeed;InitialClock=source.InitialClock;InitialCounter=source.InitialCounter;ReserveFirstFleet=source.ReserveFirstFleet;Clock=source.Clock;RandomState=source.RandomState;InputRandomState=source.RandomState;NextFleetIndex=source.NextFleetIndex;CreatedFleetCount=source.CreatedFleetCount;CreatedUnitCount=source.CreatedUnitCount;StudyRandomDraws=source.StudyRandomDraws;Visits=source.Visits;
        }
        public PracticeGeneratedReinforcements ResetCopy(){return CreateFixture(tables,ProducerCount,ConsumerCount,InitialSeed,InitialClock,ReserveFirstFleet,InitialCounter,beforeTable);}
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
                var point=PracticeScenarioPositions.Select(5,at/4,next.RandomState,next.fleets,new byte[246000],128);next.RandomState=point.RandomState;next.StudyRandomDraws+=point.RandomCalls;var made=next.Construct(next.payloads[at],at/4,at%4,point.X,point.Y);events.Add(made);next.deadlines[at]=-1;next.RefreshRequests++;
            }
            return next;
        }
        PracticeReinforcementCreation Construct(int template,int group,int slot,int x,int y)
        {
            if(template<0||template>3)throw new NotSupportedException("Only generated bank-five payloads 0..3 are admitted.");int fleet=-1;for(int n=0;n<250;n++)if(I(fleets,n*492)==0&&fleets[n*492+490]==0){fleet=n;break;}if(fleet<0)throw new InvalidDataException("No admitted primary fleet slot.");int at=fleet*492,context=fleet*312;
            W(fleets,at+8,0);I(fleets,at,0);I(fleets,at+472,0);I(fleets,at+476,0);foreach(int offset in new[]{62,468,483,467,480,481,485,486})fleets[at+offset]=0;fleets[at+482]=255;Array.Clear(fleets,at+16,24);I(fleets,at+40,-1);I(fleets,at+44,-1);W(fleets,at+48,-1);
            I(runtime,context+200,0);for(int n=0;n<10;n++)I(runtime,context+n*20,0);for(int n=0;n<6;n++){I(runtime,context+212+n*4,0);I(runtime,context+236+n*4,0);}
            I(fleets,at,1);W(fleets,at+12,x);W(fleets,at+14,y);W(fleets,at+6,group);W(fleets,at+60,2);fleets[at+63]=0;fleets[at+484]=unchecked((byte)ownerCounters[group]);fleets[at+485]=2;ownerCounters[group]=unchecked(ownerCounters[group]+1);
            var units=new List<int>();for(int member=0;member<6;member++){int p=template*76+4+member*12,type=I(tables.Templates,p);if(type==0)break;int unit=-1;for(int n=0;n<1500;n++)if(I(ships,n*268)==0){unit=n;break;}if(unit<0)throw new InvalidDataException("No admitted ship slot.");tables.ConstructUnit(ships,unit,type,group,fleet,I(tables.Templates,p+4),I(tables.Templates,p+8));I(fleets,at+16+member*4,0x4fc340+unit*268);units.Add(unit);}
            byte speed=0;foreach(int unit in units){int current=W(ships,unit*268+72);if(speed==0)speed=unchecked((byte)current);if(speed>current)speed=unchecked((byte)current);}W(fleets,at+60,speed);Array.Copy(tables.Catalog.Colors,3,fleets,at+464,3);for(int n=0;n<100;n++)I(fleets,at+64+n*4,0);foreach(int unit in units){int cell=W(ships,unit*268+8)*10+W(ships,unit*268+10),length=W(ships,unit*268+14);if(cell<0||cell+length>100)throw new InvalidDataException("Generated unit exceeded the source grid.");for(int n=0;n<length;n++)I(fleets,at+64+(cell+n)*4,2);}fleets[at+468]=0;NextFleetIndex=(uint)(fleet+1);CreatedFleetCount++;CreatedUnitCount+=units.Count;return new PracticeReinforcementCreation(fleet,group,slot,template,x,y,units);
        }
        public int DeadlineAt(int group,int slot){return deadlines[Index(group,slot)];}public int PayloadAt(int group,int slot){return payloads[Index(group,slot)];}
        static int Index(int group,int slot){if(group<0||group>3||slot<0||slot>3)throw new ArgumentOutOfRangeException("group/slot");return group*4+slot;}
        public uint OwnerCounter(int group){if(group<0||group>3)throw new ArgumentOutOfRangeException("group");return ownerCounters[group];}
        public ReadOnlyCollection<int> FleetIds(){var ids=new List<int>();for(int n=0;n<249;n++)if(I(fleets,n*492)==1)ids.Add(n);return ids.AsReadOnly();}
        public ReadOnlyCollection<int> UnitIds(int fleet){if(fleet<0||fleet>=249||I(fleets,fleet*492)!=1)throw new ArgumentOutOfRangeException("fleet");var ids=new List<int>();for(int n=0;n<6;n++){uint reference=BitConverter.ToUInt32(fleets,fleet*492+16+n*4);if(reference==0)continue;long offset=(long)reference-0x4fc340;if(offset<0||offset>=ships.Length||offset%268!=0)throw new InvalidDataException("Generated roster reference.");ids.Add((int)(offset/268));}return ids.AsReadOnly();}
        public byte[] CopyShipPool(){return (byte[])ships.Clone();}public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}public byte[] CopyRuntimePool(){return (byte[])runtime.Clone();}
        internal void WriteOriginalSnapshot(BinaryWriter writer){writer.Write(Clock);writer.Write(InputRandomState);writer.Write(RefreshRequests);writer.Write(NextFleetIndex);writer.Write(RandomState);writer.Write(InitialClock);foreach(uint value in ownerCounters)writer.Write(value);foreach(int value in deadlines)writer.Write(value);foreach(int value in payloads)writer.Write(value);writer.Write(ships);writer.Write(fleets);writer.Write(runtime);}
        static int I(byte[] b,int at){return PracticeReinforcementTables.I(b,at);}static int W(byte[] b,int at){return PracticeReinforcementTables.W(b,at);}static void I(byte[] b,int at,int value){PracticeReinforcementTables.I(b,at,value);}static void W(byte[] b,int at,int value){PracticeReinforcementTables.W(b,at,value);}
    }
}
