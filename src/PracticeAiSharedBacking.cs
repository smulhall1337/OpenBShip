using System;
using System.IO;
namespace FleetLab
{
    // Pinned RIP: task base5AEE20, movement base5AEF00=task+224.
    // Views overlap; the last560 bytes extend past the250 task records.
    public sealed class PracticeAiSharedBacking
    {
        public const int TaskBytes=78000,MovementOffset=224,MovementBytes=78336,RegionBytes=78560;
        readonly byte[] region;
        public PracticeAiSharedBacking(byte[] bytes)
        {if(bytes==null||bytes.Length!=RegionBytes)throw new InvalidDataException("AI shared region must preserve all78560 bytes, including the movement tail.");region=(byte[])bytes.Clone();}
        public static PracticeAiSharedBacking FromViews(byte[] tasks,byte[] movement)
        {
            if(tasks==null||tasks.Length!=TaskBytes||movement==null||movement.Length!=MovementBytes)throw new InvalidDataException("Complete task and movement views are required.");
            for(int n=MovementOffset;n<TaskBytes;n++)if(tasks[n]!=movement[n-MovementOffset])throw new InvalidDataException("Task and movement views conflict in their shared bytes.");
            var bytes=new byte[RegionBytes];tasks.CopyTo(bytes,0);Array.Copy(movement,TaskBytes-MovementOffset,bytes,TaskBytes,RegionBytes-TaskBytes);return new PracticeAiSharedBacking(bytes);
        }
        static void Slot(int slot){if(slot<0||slot>=250)throw new ArgumentOutOfRangeException("slot");}
        byte[] Slice(int at,int count){var result=new byte[count];Array.Copy(region,at,result,0,count);return result;}
        public byte[] CopyRegion(){return(byte[])region.Clone();}
        public byte[] CopyTaskPool(){return Slice(0,TaskBytes);}
        public byte[] CopyMovementAuxiliary(){return Slice(MovementOffset,MovementBytes);}
        public byte[] CopyTaskRecord(int slot){Slot(slot);return Slice(slot*312,312);}
        public PracticeAiSharedBacking WithTaskRecord(int slot,byte[] task)
        {Slot(slot);if(task==null||task.Length!=312)throw new InvalidDataException("A complete312-byte task record is required.");var bytes=CopyRegion();task.CopyTo(bytes,slot*312);return new PracticeAiSharedBacking(bytes);}
        public PracticeAiSharedBacking WithMovementAuxiliary(byte[] movement)
        {if(movement==null||movement.Length!=MovementBytes)throw new InvalidDataException("A complete movement view is required.");var bytes=CopyRegion();movement.CopyTo(bytes,MovementOffset);return new PracticeAiSharedBacking(bytes);}
    }
}
