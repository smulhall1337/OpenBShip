using System;
using System.IO;
namespace FleetLab
{
    // Deliberate diagnostic copy. Constructor and recovery/lookup rules are
    // recovered; location, damage and explicit button calls are supplied inputs.
    public sealed class PracticeInstallationStudyState
    {
        readonly byte[] originalShips,originalFleets,ships,fleets;
        public int RecoveryCalls
        {
            get;
            private set;
        }
        public int ChangedCells
        {
            get;
            private set;
        }
        public PracticeInstallationRepairResult LastRecovery
        {
            get;
            private set;
        }
        public const int FirstFleet=32, UnitCount=10;
        public PracticeInstallationStudyState(string programs)
        {
            var result=PracticeAirfieldFactory.Load(programs).CreateFresh(FirstFleet,new System.Drawing.Point(400,370),0,1);
            ships=originalShips=result.CopyShipPool();
            fleets=originalFleets=result.CopyFleetPool();
        }
        PracticeInstallationStudyState(PracticeInstallationStudyState before,byte[] s,byte[] f)
        {
            originalShips=before.originalShips;
            originalFleets=before.originalFleets;
            ships=s;
            fleets=f;
            RecoveryCalls=before.RecoveryCalls;
            ChangedCells=before.ChangedCells;
            LastRecovery=before.LastRecovery;
        }
        static int W(byte[] b,int p)
        {
            return BitConverter.ToInt16(b,p);
        }
        static void W(byte[] b,int p,int v)
        {
            BitConverter.GetBytes(unchecked((short)v)).CopyTo(b,p);
        }
        int Fleet(int unit)
        {
            if(unit<0||unit>=UnitCount)throw new ArgumentOutOfRangeException("unit");
            int fleet=W(ships,unit*268+12);
            if(fleet<FirstFleet||fleet>=FirstFleet+4)throw new InvalidDataException("Study unit is outside the copied installation.");
            return fleet;
        }
        public byte[] CopyShipPool()
        {
            return (byte[])ships.Clone();
        }
        public byte[] CopyFleetPool()
        {
            return (byte[])fleets.Clone();
        }
        public PracticeInstallationStudyState Reset()
        {
            var next=new PracticeInstallationStudyState(this,originalShips,originalFleets);
            next.RecoveryCalls=0;
            next.ChangedCells=0;
            next.LastRecovery=null;
            return next;
        }
        public PracticeInstallationStudyState SupplyDamage(int unit,bool hpBoundary)
        {
            int fleet=Fleet(unit),at=unit*268,length=W(ships,at+14),start=W(ships,at+8)*10+W(ships,at+10);
            if(length<1||length>6||start<0||start>100-length)throw new InvalidDataException("Study damage is outside the selected unit's complete grid cells.");
            var s=(byte[])ships.Clone();
            var f=(byte[])fleets.Clone();
            int[] pattern=
            {
                5,6,7,8,2,5
            };
            for(int n=0;n<length;n++)BitConverter.GetBytes(hpBoundary?(n==0?8:2):pattern[n]).CopyTo(f,fleet*492+64+(start+n)*4);
            W(s,at+16,hpBoundary?length-1:0);
            var next=new PracticeInstallationStudyState(this,s,f);
            next.LastRecovery=null;
            return next;
        }
        public PracticeInstallationStudyState Recover(int unit,int maximumCalls)
        {
            int fleet=Fleet(unit);
            if(maximumCalls<1||maximumCalls>16)throw new ArgumentOutOfRangeException("maximumCalls");
            var s=ships;
            var f=fleets;
            PracticeInstallationRepairResult last=null;
            int calls=0,changes=0;
            for(int n=0;n<maximumCalls;n++)
            {
                last=PracticeInstallationRepair.StepOffscreen(s,f,fleet,unit);
                s=last.CopyShipPool();
                f=last.CopyFleetPool();
                calls++;
                if(!last.Changed)break;
                changes++;
            }
            var next=new PracticeInstallationStudyState(this,s,f);
            next.RecoveryCalls+=calls;
            next.ChangedCells+=changes;
            next.LastRecovery=last;
            return next;
        }
        public int FindType(int anchorFleet,int type)
        {
            return PracticeInstallationLookup.FindFirstInAlignedBlock(ships,fleets,anchorFleet,type);
        }
    }
}
