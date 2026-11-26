using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FleetLab
{
    public sealed class PracticeOwnerFleetVisibility
    {
        public int Fleet {get;private set;}public int Kind {get;private set;}public int Owner {get;private set;}
        public int X {get;private set;}public int Y {get;private set;}
        public byte Level {get;private set;}public byte ContactByte {get;private set;}public byte HistoryByte {get;private set;}
        internal PracticeOwnerFleetVisibility(byte[] f,int slot)
        {
            int at=slot*492;Fleet=slot;Kind=BitConverter.ToInt32(f,at);Owner=BitConverter.ToInt16(f,at+6);X=BitConverter.ToInt16(f,at+12);Y=BitConverter.ToInt16(f,at+14);
            Level=f[at+467];ContactByte=f[at+480];HistoryByte=f[at+481];
        }
        public override string ToString(){return "Fleet "+Fleet+" · kind "+Kind+" · owner "+Owner+" · level "+Level;}
    }
    public sealed class PracticeOwnerVisibilityResult
    {
        readonly byte[] before,fleets;
        public int Owner {get;private set;}public int Mode {get;private set;}
        public bool Skipped {get;private set;}
        internal PracticeOwnerVisibilityResult(byte[] before,byte[] after,int owner,int mode,bool skipped){this.before=before;fleets=after;Owner=owner;Mode=mode;Skipped=skipped;}
        public byte[] CopyInputFleetPool(){return (byte[])before.Clone();}
        public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
        public PracticeOwnerFleetVisibility Fleet(int slot){if(slot<0||slot>=250)throw new ArgumentOutOfRangeException("slot");return new PracticeOwnerFleetVisibility(fleets,slot);}
        public PracticeOwnerFleetVisibility Before(int slot){if(slot<0||slot>=250)throw new ArgumentOutOfRangeException("slot");return new PracticeOwnerFleetVisibility(before,slot);}
        public ReadOnlyCollection<PracticeOwnerFleetVisibility> ActiveFleets
        {
            get{var list=new List<PracticeOwnerFleetVisibility>();for(int slot=0;slot<250;slot++)if(BitConverter.ToInt32(fleets,slot*492)!=0)list.Add(new PracticeOwnerFleetVisibility(fleets,slot));return list.AsReadOnly();}
        }
        public ReadOnlyCollection<int> ChangedFleets
        {
            get{var list=new List<int>();for(int slot=0;slot<250;slot++){int at=slot*492;if(before[at+467]!=fleets[at+467]||before[at+480]!=fleets[at+480]||before[at+481]!=fleets[at+481])list.Add(slot);}return list.AsReadOnly();}
        }
    }

    // RIP B9A8...6A569 whole4282C0. Source576 captures the per-owner producer,
    // distinct from the per-shot numeric418900 predicate. Only fleet bytes
    //467(current level),480(observer far-contact flag),481(packed history) write.
    public sealed class PracticeOwnerVisibilityRules
    {
        readonly byte[] kinds;
        readonly PracticeFiringRangeTable ranges;
        readonly PracticeDefenseTables combat;
        public PracticeOwnerVisibilityRules(byte[] sourceKinds,PracticeFiringRangeTable ranges,PracticeDefenseTables combat)
        {
            new PracticeVisibility(sourceKinds); // validates the pinned624-byte prefix
            if(ranges==null||combat==null)throw new ArgumentNullException("visibility tables");
            kinds=(byte[])sourceKinds.Clone();this.ranges=ranges;this.combat=combat;
        }
        public static PracticeOwnerVisibilityRules Load(string programs)
        {
            return new PracticeOwnerVisibilityRules(File.ReadAllBytes(Path.Combine(programs,"VisibilityKinds.seqbin")),PracticeFiringRangeTable.Load(programs),PracticeDefenseTables.Load(programs));
        }
        bool Admits(int observerKind,int targetKind)
        {
            // Original576 separately verifies all52 bytes at44CEF8 are zero
            // (full676-byte hash20D351...CE6B4). Kind12's later special branch
            // is unreachable with this executable's empty row. Reuse the
            // existing624-byte resource; do not invent another sensor rule.
            if(observerKind==12)return false;
            for(int n=0;n<13;n++){int entry=BitConverter.ToInt32(kinds,observerKind*52+n*4);if(entry==0)return false;if(entry==targetKind)return true;}return false;
        }
        static int Distance(byte[] f,int a,int b)
        {
            return Math.Max(Math.Abs(BitConverter.ToInt16(f,a+12)-BitConverter.ToInt16(f,b+12)),Math.Abs(BitConverter.ToInt16(f,a+14)-BitConverter.ToInt16(f,b+14)));
        }
        public PracticeOwnerVisibilityResult Apply(byte[] fleetPool,int owner,int mode,short localOwner,int selectedWeapon,int selectedFleet)
        {
            if(fleetPool==null||fleetPool.Length!=123000)throw new InvalidDataException("Owner visibility requires all250 original492-byte fleet records.");
            if(owner< -1||owner>3)throw new ArgumentOutOfRangeException("owner");if(mode!=0&&mode!=2)throw new NotSupportedException("The owner-visibility adapter admits source modes0 and2.");
            var before=(byte[])fleetPool.Clone();bool filter=mode==2&&owner==localOwner;
            // Source returns before clearing any flag when this local context
            // has no selected weapon. Ignored fields are not dereferenced.
            if(filter&&selectedWeapon==0)return new PracticeOwnerVisibilityResult(before,before,owner,mode,true);
            int range=0;
            if(filter){if(selectedWeapon<0||selectedWeapon>=35)throw new ArgumentOutOfRangeException("selectedWeapon");if(selectedFleet<0||selectedFleet>=250)throw new ArgumentOutOfRangeException("selectedFleet");range=ranges.SourceRange(selectedWeapon);}
            var f=(byte[])before.Clone();for(int slot=0;slot<250;slot++){f[slot*492+467]=0;f[slot*492+480]=0;}
            for(int observer=0;observer<250;observer++)
            {
                int at=observer*492,kind=BitConverter.ToInt32(f,at);
                if(kind==0||kind==6||kind==7||kind==8||BitConverter.ToInt16(f,at+6)!=owner)continue;
                if(kind<1||kind>12)throw new NotSupportedException("Owned observer kind is outside the recovered13-row visibility span.");
                for(int target=0;target<250;target++)
                {
                    int other=target*492;if(BitConverter.ToInt16(f,other+6)==owner)continue;int targetKind=BitConverter.ToInt32(f,other);
                    if(!Admits(kind,targetKind))continue;
                    if(filter&&(combat.BaseChance(selectedWeapon,targetKind)==0||Distance(f,selectedFleet*492,other)>range))continue;
                    int distance=Distance(f,at,other);
                    if(distance<=f[at+466]){f[at+480]=1;if(f[other+467]<1)f[other+467]=1;}
                    if(distance<=f[at+465]&&f[other+467]<2)f[other+467]=2;
                    if(distance<=f[at+464]&&f[other+467]<3)f[other+467]=3;
                }
            }
            if(owner>=0)
            {
                int shift=owner*2;byte mask=unchecked((byte)((((0xfffd<<shift)&0xffff)+0xff)&255));
                for(int slot=0;slot<250;slot++)
                {
                    int at=slot*492,kind=BitConverter.ToInt32(f,at);if(kind!=2&&kind!=10&&kind!=25)continue;
                    // Preserve the original UNMASKED shifted-byte comparison.
                    // Higher owner fields can block a lower field's upgrade.
                    int packed=f[at+481];if((packed>>shift)<f[at+467])f[at+481]=unchecked((byte)((f[at+467]<<shift)|(packed&mask)));
                    f[at+467]=(byte)((f[at+481]>>shift)&3);
                }
            }
            return new PracticeOwnerVisibilityResult(before,f,owner,mode,false);
        }
    }
}
