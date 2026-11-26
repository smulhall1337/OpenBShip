using System;
using System.IO;
namespace FleetLab
{
    public enum PracticeServiceDestinationOutcome { Unchanged, SetStateOne, NoAvailableYard, DestinationChosen }
    public sealed class PracticeServiceDestinationResult
    {
        readonly byte[] fleets;
        public PracticeAiSharedBacking Backing { get; private set; }
        public int SelectedYard { get; private set; }
        public PracticeServiceDestinationOutcome Outcome { get; private set; }
        internal PracticeServiceDestinationResult(byte[] f, PracticeAiSharedBacking b, int yard, PracticeServiceDestinationOutcome outcome)
        { fleets=(byte[])f.Clone(); Backing=b; SelectedYard=yard; Outcome=outcome; }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
    }
    // RIP B9A8...6A569,41E540/41BB30. Native759 compares280 supplied leaf
    // contexts. This chooses cached destination data; it does not admit ships,
    // change the task goal,find routes,move fleets or run a world clock.
    public static class PracticeServiceDestination
    {
        static readonly int[] kinds={0,1,2,3,4,5,6,7,8,9,10,11,12,25};
        public static PracticeServiceDestinationResult Evaluate(byte[] ships,byte[] fleets,
            PracticeAiSharedBacking backing,int taskSlot,int activeOwner)
        {
            if(ships==null||ships.Length!=402000||fleets==null||fleets.Length!=123000)
                throw new InvalidDataException("Service destination requires complete ship and fleet pools.");
            if(backing==null)throw new ArgumentNullException("backing");
            if(activeOwner!=1)throw new NotSupportedException("Service destination currently admits compared active-owner1 contexts only.");
            var task=backing.CopyTaskRecord(taskSlot);int at=taskSlot*492,kind=BitConverter.ToInt32(fleets,at);
            if(BitConverter.ToUInt32(task,204)!=0x4b47a0u+(uint)at||BitConverter.ToInt16(fleets,at+4)!=taskSlot)
                throw new InvalidDataException("Service destination task and fleet identities must agree.");
            if(Array.IndexOf(kinds,kind)<0)throw new NotSupportedException("Actor kind is outside the compared destination profiles.");
            var after=(byte[])fleets.Clone();
            if(kind==2||kind==11)return new PracticeServiceDestinationResult(after,backing,-1,PracticeServiceDestinationOutcome.Unchanged);
            if(kind!=1&&kind!=9){BitConverter.GetBytes(1).CopyTo(after,at+472);return new PracticeServiceDestinationResult(after,backing,-1,PracticeServiceDestinationOutcome.SetStateOne);}
            int closest=-1,chosen=-1;
            for(int yard=0;yard<250;yard++)
            {
                int a=yard*492;if(BitConverter.ToInt32(fleets,a)!=2||BitConverter.ToInt16(fleets,a+6)!=activeOwner)continue;
                for(int member=0;member<6;member++)
                {
                    uint p=BitConverter.ToUInt32(fleets,a+16+member*4);if(p==0)continue;long offset=(long)p-0x4fc340;
                    if(offset<0||offset>=402000||offset%268!=0)throw new InvalidDataException("Service destination reached an invalid marker reference.");
                    int marker=BitConverter.ToInt32(ships,(int)offset);if(marker!=22&&marker!=23)continue;
                    int distance=Math.Max(Math.Abs(BitConverter.ToInt16(fleets,at+12)-BitConverter.ToInt16(fleets,a+12)),Math.Abs(BitConverter.ToInt16(fleets,at+14)-BitConverter.ToInt16(fleets,a+14)));
                    if(closest!=-1&&distance>=closest)continue;closest=distance;chosen=yard;
                    BitConverter.GetBytes((int)BitConverter.ToInt16(fleets,a+12)).CopyTo(task,8);
                    BitConverter.GetBytes((int)BitConverter.ToInt16(fleets,a+14)).CopyTo(task,12);
                }
            }
            after[at+485]=1; // Broader meaning remains unresolved;preserve raw byte.
            return new PracticeServiceDestinationResult(after,chosen<0?backing:backing.WithTaskRecord(taskSlot,task),chosen,
                chosen<0?PracticeServiceDestinationOutcome.NoAvailableYard:PracticeServiceDestinationOutcome.DestinationChosen);
        }
    }
}
