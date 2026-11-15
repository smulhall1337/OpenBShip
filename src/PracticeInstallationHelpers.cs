using System;
using System.IO;
namespace FleetLab
{
    public sealed class PracticeInstallationRepairResult
    {
        readonly byte[] ships,fleets;
        public bool Changed
        {
            get;
            private set;
        }
        public int GridCell
        {
            get;
            private set;
        }
        public int BeforeState
        {
            get;
            private set;
        }
        public int AfterState
        {
            get;
            private set;
        }
        public int BeforeHP
        {
            get;
            private set;
        }
        public int AfterHP
        {
            get;
            private set;
        }
        internal PracticeInstallationRepairResult(byte[] s,byte[] f,bool changed,int cell,int before,int after,int hp,int nextHP)
        {
            ships=s;
            fleets=f;
            Changed=changed;
            GridCell=cell;
            BeforeState=before;
            AfterState=after;
            BeforeHP=hp;
            AfterHP=nextHP;
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
    // Pinned RIP B9A8...6A569:429A90..429B84, network0. One cell per call;
    // this is not the active support caller, nor a complete repair operation.
    public static class PracticeInstallationRepair
    {
        static int W(byte[] b,int p)
        {
            return BitConverter.ToInt16(b,p);
        }
        public static PracticeInstallationRepairResult StepOffscreen(byte[] shipPool,byte[] fleetPool,int fleet,int unit)
        {
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000)throw new InvalidDataException("Installation recovery requires complete ship and fleet pools.");
            if(fleet<0||fleet>=250||unit<0||unit>=1500)throw new ArgumentOutOfRangeException("fleet/unit");
            int at=unit*268,length=W(shipPool,at+14),hp=W(shipPool,at+16);
            var ships=(byte[])shipPool.Clone();
            var fleets=(byte[])fleetPool.Clone();
            // Source tests HP equality and nonpositive length before reading
            // local coordinates or the grid. Preserve this early-return order.
            if(hp==length||length<=0)return new PracticeInstallationRepairResult(ships,fleets,false,-1,0,0,hp,hp);
            if(length>6)throw new NotSupportedException("Installation recovery supports the recorded unit lengths through six.");
            int first=W(ships,at+8)*10+W(ships,at+10);
            if(first<0||first>100-length)throw new InvalidDataException("Installation recovery would cross its complete 100-cell grid.");
            for(int n=0;n<length;n++)
            {
                int cell=first+n,p=fleet*492+64+cell*4,before=BitConverter.ToInt32(fleets,p),after;
                if(before==5)after=2;
                else if(before>=6&&before<=8)after=before-1;
                else continue;
                BitConverter.GetBytes(after).CopyTo(fleets,p);
                int nextHP=before==8?unchecked((short)(hp+1)):hp;
                BitConverter.GetBytes((short)nextHP).CopyTo(ships,at+16);
                return new PracticeInstallationRepairResult(ships,fleets,true,cell,before,after,hp,nextHP);
            }
            return new PracticeInstallationRepairResult(ships,fleets,false,-1,0,0,hp,hp);
        }
    }
    // Pinned RIP B9A8...6A569:429B90..429BEC. Source searches by type only;
    // inactive fleets, different owners and referenced type0 units can match.
    public static class PracticeInstallationLookup
    {
        public static int FindFirstInAlignedBlock(byte[] ships,byte[] fleets,int anchorFleet,int type)
        {
            if(ships==null||ships.Length!=402000||fleets==null||fleets.Length!=123000)throw new InvalidDataException("Installation lookup requires complete ship and fleet pools.");
            if(anchorFleet<0||anchorFleet>=248)throw new ArgumentOutOfRangeException("anchorFleet","The complete aligned four-fleet block must remain inside the 250-record pool.");
            int first=anchorFleet&~3;
            for(int fleet=first;fleet<first+4;fleet++)for(int member=0;member<6;member++)
            {
                uint reference=BitConverter.ToUInt32(fleets,fleet*492+16+member*4);
                if(reference==0)continue;
                long offset=(long)reference-0x4fc340;
                if(offset<0||offset>=402000||offset%268!=0)throw new InvalidDataException("Installation lookup encountered a member outside its complete ship pool.");
                if(BitConverter.ToInt32(ships,(int)offset)==type)return (int)offset/268;
            }
            return -1;
        }
    }
}
