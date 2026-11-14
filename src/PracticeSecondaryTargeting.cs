using System;
using System.IO;
using System.Collections.Generic;

namespace FleetLab {
    public sealed class PracticeSecondaryTargetChoice {
        public int SourceFleet { get; private set; }
        public int SourceShip { get; private set; }
        public int TargetFleet { get; private set; }
        public int TargetShip { get; private set; }
        public int Weapon { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int QueueReturnCode { get; private set; }
        internal PracticeSecondaryTargetChoice(int sf,int source,int target,int victim,int weapon,int x,int y,int code) {
            SourceFleet=sf;SourceShip=source;TargetFleet=target;TargetShip=victim;Weapon=weapon;X=x;Y=y;QueueReturnCode=code;
        }
    }
    public sealed class PracticeSecondaryTargetingResult {
        readonly byte[] orders;
        public uint RandomState { get; private set; }
        public int RandomCalls { get; private set; }
        public int AttemptedOrders { get { return Choices.Count; } }
        public int AcceptedOrders { get; private set; }
        public IList<PracticeSecondaryTargetChoice> Choices { get; private set; }
        internal PracticeSecondaryTargetingResult(byte[] pool,uint random,int calls,List<PracticeSecondaryTargetChoice> choices) {
            orders=pool;RandomState=random;RandomCalls=calls;Choices=choices.AsReadOnly();foreach(var item in choices)if(item.QueueReturnCode!=1)AcceptedOrders++;
        }
        public byte[] CopyOrderPool() { return (byte[])orders.Clone(); }
    }
    // RIP B9A8...6A569:419790 ->439790/419B20. This only generates secondary
    // requests, with no launch, visibility/ammo admission or mission execution.
    // All source/target references use normalized identities, never CLR pointers.
    public static class PracticeSecondaryTargeting {
        static int Word(byte[] b,int p){return BitConverter.ToInt16(b,p);}
        static int Ship(uint reference) {
            long offset=(long)reference-0x4fc340;
            if(offset<0||offset>=402000||offset%268!=0)throw new InvalidDataException("Secondary target generation encountered an unsupported unit reference.");
            return (int)(offset/268);
        }
        static int Target(byte[] fleets,int source,int range) {
            int at=source*492,owner=Word(fleets,at+6),x=Word(fleets,at+12),y=Word(fleets,at+14);
            for(int target=0;target<250;target++) {
                int p=target*492,kind=BitConverter.ToInt32(fleets,p);
                if(Word(fleets,p+6)==owner||(kind!=3&&kind!=4&&kind!=5))continue;
                int distance=Math.Max(Math.Abs(Word(fleets,p+12)-x),Math.Abs(Word(fleets,p+14)-y));
                if(distance<=range)return target;
            }
            return -1;
        }
        public static PracticeSecondaryTargetingResult Generate(PracticeFiringRangeTable ranges,byte[] ships,byte[] fleets,byte[] secondaryOrders,uint seed) {
            if(ranges==null)throw new ArgumentNullException("ranges");
            if(ships==null||ships.Length!=402000||fleets==null||fleets.Length!=123000||secondaryOrders==null||secondaryOrders.Length!=78000)
                throw new InvalidDataException("Secondary target generation requires complete ship, fleet and secondary-order pools.");
            byte[] orders=(byte[])secondaryOrders.Clone();var random=new NativeRandom(seed);var choices=new List<PracticeSecondaryTargetChoice>();int range=ranges.SourceRange(1);
            for(int sourceFleet=0;sourceFleet<250;sourceFleet++) {
                int fleet=sourceFleet*492,kind=BitConverter.ToInt32(fleets,fleet);if(kind!=1&&kind!=2)continue;
                // One first-in-pool target is cached across all six source
                // members. Failure to find a live target member does not select
                // another fleet. Duplicate source references are not deduplicated.
                int targetFleet=-1;
                for(int member=0;member<6;member++) {
                    uint pointer=BitConverter.ToUInt32(fleets,fleet+16+member*4);if(pointer==0)continue;
                    int source=Ship(pointer),at=source*268;if(BitConverter.ToInt32(ships,at)==0)continue;
                    int length=Word(ships,at+14);if(length<=0)continue;
                    if(length>6)throw new NotSupportedException("Secondary source equipment rows exceed the admitted six-row span.");
                    int weapon=0;for(int e=0;e<length*4;e++){int id=BitConverter.ToInt32(ships,at+100+e*4);if(id>=1&&id<=3){weapon=id;break;}}
                    if(weapon==0)continue;
                    if(targetFleet<0)targetFleet=Target(fleets,sourceFleet,range);if(targetFleet<0)continue;
                    int first=random.Next()%6,targetShip=-1;
                    for(int attempt=0;attempt<6;attempt++) {
                        int index=(first+attempt)%6;uint reference=BitConverter.ToUInt32(fleets,targetFleet*492+16+index*4);if(reference==0)continue;
                        int candidate=Ship(reference);if(BitConverter.ToInt32(ships,candidate*268)!=0){targetShip=candidate;break;}
                    }
                    if(targetShip<0)continue;
                    int victim=targetShip*268,targetLength=Word(ships,victim+14);
                    if(targetLength<1||targetLength>6)throw new NotSupportedException("Selected secondary target has an unsupported local extent.");
                    int x=Word(ships,victim+8),y=unchecked((short)(Word(ships,victim+10)+random.Next()%targetLength));
                    var queued=PracticeSecondaryFiringOrders.Enqueue(ships,orders,source,weapon,targetFleet,x,y);orders=queued.CopyOrderPool();
                    choices.Add(new PracticeSecondaryTargetChoice(sourceFleet,source,targetFleet,targetShip,weapon,x,y,queued.ReturnCode));
                }
            }
            return new PracticeSecondaryTargetingResult(orders,random.State,random.Calls,choices);
        }
    }
}
