using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace FleetLab
{
    public sealed class PracticeAircraftHomeCandidate
    {
        public int FleetSlot
        {
            get;
            private set;
        }
        public int UnitIdentity
        {
            get;
            private set;
        }
        public int Distance
        {
            get;
            private set;
        }
        internal PracticeAircraftHomeCandidate(int fleet,int unit,int distance)
        {
            FleetSlot=fleet;
            UnitIdentity=unit;
            Distance=distance;
        }
    }
    public sealed class PracticeAircraftNoHomeResult
    {
        readonly byte[] ships,fleets;
        internal PracticeAircraftNoHomeResult(byte[] s,byte[] f)
        {
            ships=s;
            fleets=f;
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
    // Pinned RIP B9A8...6A569: 428A60 roster candidate list and the empty-list
    // branch of 428BB0. ApplyOffscreen additionally recovers bounded landing-slot
    // selection/relinking at playercount0/network0; no movement or actual UI.
    public sealed class PracticeAircraftHomeSearchResult
    {
        readonly byte[] ships,fleets;
        public bool FoundHome
        {
            get;
            private set;
        }
        public int CandidateCount
        {
            get;
            private set;
        }
        public int ParentUnit
        {
            get;
            private set;
        }
        public int Link
        {
            get;
            private set;
        }
        internal PracticeAircraftHomeSearchResult(byte[] s,byte[] f,int actor,bool found,int candidates)
        {
            ships=s;
            fleets=f;
            FoundHome=found;
            CandidateCount=candidates;
            ParentUnit=BitConverter.ToInt16(f,actor*492+8);
            Link=BitConverter.ToInt16(f,actor*492+10);
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
    public static class PracticeAircraftHomeSearch
    {
        static int W(byte[] b,int at)
        {
            return BitConverter.ToInt16(b,at);
        }
        static int I(byte[] b,int at)
        {
            return BitConverter.ToInt32(b,at);
        }
        static int Ship(uint reference)
        {
            long at=(long)reference-0x4fc340;
            if(at<0||at>=402000||at%268!=0)throw new InvalidDataException("Home search member reference is outside its complete ship pool.");
            return (int)at;
        }
        static int Actor(byte[] ships,byte[] fleets,int slot)
        {
            if(ships==null||ships.Length!=402000||fleets==null||fleets.Length!=123000)throw new InvalidDataException("Aircraft home search requires complete ship and fleet pools.");
            if(slot<0||slot>=250)throw new ArgumentOutOfRangeException("slot");
            int kind=I(fleets,slot*492);
            if(kind<3||kind>5)throw new NotSupportedException("Only active helicopter, plane or AWACS home searches are admitted.");
            return kind;
        }
        public static ReadOnlyCollection<PracticeAircraftHomeCandidate> FindCandidates(byte[] shipPool,byte[] fleetPool,int aircraftFleet)
        {
            int kind=Actor(shipPool,fleetPool,aircraftFleet),actor=aircraftFleet*492,owner=W(fleetPool,actor+6);
            var list=new List<PracticeAircraftHomeCandidate>();
            for(int fleet=0;fleet<250;fleet++)
            {
                int at=fleet*492;
                if(W(fleetPool,at+6)!=owner)continue;
                for(int row=0;row<6;row++)
                {
                    uint reference=BitConverter.ToUInt32(fleetPool,at+16+row*4);
                    if(reference==0)continue;
                    int unit=Ship(reference),type=I(shipPool,unit);
                    if(type!=20&&(kind==5||type!=5&&type!=6&&type!=7))continue;
                    int distance=Math.Max(Math.Abs(W(fleetPool,actor+12)-W(fleetPool,at+12)),Math.Abs(W(fleetPool,actor+14)-W(fleetPool,at+14)));
                    list.Add(new PracticeAircraftHomeCandidate(fleet,W(shipPool,unit+4),unchecked((short)distance)));
                    break;
                }
            }
            // The source compares signed 16-bit distances and swaps each smaller
            // later entry immediately. Equal-distance ordering is not stable.
            for(int a=0;a<list.Count;a++)for(int b=a+1;b<list.Count;b++)if(list[b].Distance<list[a].Distance)
            {
                var swap=list[a];
                list[a]=list[b];
                list[b]=swap;
            }
            return list.AsReadOnly();
        }
        public static PracticeAircraftNoHomeResult ApplyWithoutCandidates(byte[] shipPool,byte[] fleetPool,int aircraftFleet)
        {
            Actor(shipPool,fleetPool,aircraftFleet);
            int at=aircraftFleet*492,parent=W(fleetPool,at+8),link=W(fleetPool,at+10);
            if(parent<0||parent>=1500||link<0||link>3)throw new InvalidDataException("Aircraft previous home attachment is outside its pool.");
            if(FindCandidates(shipPool,fleetPool,aircraftFleet).Count!=0)throw new NotSupportedException("Candidate landing-slot selection and relinking require a separate home-search study; pools preserved.");
            var s=(byte[])shipPool.Clone();
            var f=(byte[])fleetPool.Clone();
            BitConverter.GetBytes(0).CopyTo(s,parent*268+56+link*4);
            BitConverter.GetBytes(2).CopyTo(f,at+472);
            BitConverter.GetBytes(2).CopyTo(f,at+476);
            f[at+486]=0;
            return new PracticeAircraftNoHomeResult(s,f);
        }
        // Whole 428BB0 with network0 and real playercount0. This admits the
        // original 40A240 early return, not actual owner UI or network work.
        public static PracticeAircraftHomeSearchResult ApplyOffscreen(byte[] shipPool,byte[] fleetPool,int aircraftFleet)
        {
            int kind=Actor(shipPool,fleetPool,aircraftFleet),at=aircraftFleet*492,parent=W(fleetPool,at+8),oldLink=W(fleetPool,at+10);
            if(parent<0||parent>=1500||oldLink<0||oldLink>3)throw new InvalidDataException("Aircraft previous home attachment is outside its pool.");
            var candidates=FindCandidates(shipPool,fleetPool,aircraftFleet);
            var s=(byte[])shipPool.Clone();
            var f=(byte[])fleetPool.Clone();
            BitConverter.GetBytes(0).CopyTo(s,parent*268+56+oldLink*4);
            foreach(var candidate in candidates)
            {
                int identity=candidate.UnitIdentity;
                if(identity<0||identity>=1500)throw new NotSupportedException("Landing candidate identity is outside the recorded ship pool; pools preserved.");
                int unit=identity*268;
                for(int link=0;link<4;link++)
                {
                    if(I(s,unit+56+link*4)!=0)continue;
                    bool eligible=false;
                    if(I(s,unit)==20)
                    {
                        int landingFleet=W(s,unit+12),cell=W(s,unit+8)*10+W(s,unit+10)+link;
                        if(landingFleet<0||landingFleet>=250||cell<0||cell>=100)throw new NotSupportedException("Airfield landing cell is outside its admitted fleet grid; pools preserved.");
                        eligible=I(f,landingFleet*492+64+cell*4)==2;
                    }
                    else
                    {
                        int length=W(s,unit+14);
                        if(length==0)continue;
                        if(length<0)eligible=true;
                        if(length>6)throw new NotSupportedException("Landing mount search exceeds six admitted unit rows; pools preserved.");
                        // Source jle skips the scan, then tests length!=row(0): a negative
                        // row count admits this free slot without reading equipment.
                        for(int row=0;row<length&&!eligible;row++)for(int mount=0;mount<4;mount++)
                        {
                            int equipment=I(s,unit+100+(row*4+mount)*4);
                            if(kind==3&&link==0&&equipment==35||kind==4&&link>0&&equipment==35+link)
                            {
                                eligible=true;
                                break;
                            }
                        }
                    }
                    if(!eligible)continue;
                    BitConverter.GetBytes((short)identity).CopyTo(f,at+8);
                    BitConverter.GetBytes((short)link).CopyTo(f,at+10);
                    BitConverter.GetBytes(0x4b47a0+aircraftFleet*492).CopyTo(s,unit+56+link*4);
                    BitConverter.GetBytes(2).CopyTo(f,at+476);
                    f[at+486]=0;
                    return new PracticeAircraftHomeSearchResult(s,f,aircraftFleet,true,candidates.Count);
                }
            }
            BitConverter.GetBytes(2).CopyTo(f,at+472);
            BitConverter.GetBytes(2).CopyTo(f,at+476);
            f[at+486]=0;
            return new PracticeAircraftHomeSearchResult(s,f,aircraftFleet,false,candidates.Count);
        }
    }
}
