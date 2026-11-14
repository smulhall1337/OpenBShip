using System;using System.IO;using System.Security.Cryptography;
namespace FleetLab {
    public sealed class PracticeAircraftTables {
        readonly byte[][] loadouts;
        readonly byte[] kind6Profiles,combatProfiles,visibility;
        static byte[] Pinned(byte[] b,int length,string hash) {
            if(b==null||b.Length!=length)throw new InvalidDataException("Incomplete aircraft transition table. Run Setup again.");
            using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(b)).Replace("-","")!=hash)throw new InvalidDataException("Unsupported aircraft transition table identity.");return (byte[])b.Clone();
        }
        public PracticeAircraftTables(byte[] kind6,byte[] kind7,byte[] kind8,byte[] profile6,byte[] profileCombat,byte[] visibilityModes) {
            loadouts=new[]{Pinned(kind6,288,"A5B7C0FB6CF4FAD98D750876DB50707F610B84891B3D05331AEC3DE14D6C41E6"),Pinned(kind7,288,"15B71FD9FC80E585A78E0F40647DAB8EBC2ACE7BE65F03B2DF78D81EB953E994"),Pinned(kind8,288,"2A0D545E1924A1455DE99109F86BC3AB38600847C85875007B472D03155D8CE7")};
            visibility=Pinned(visibilityModes,18,"8C1648290812C8F2D5A80F6B2D04B0DABD8E60CA8D9BAA5A888877E735086E2E");
            kind6Profiles=Pinned(profile6,24,"AA61D32811E162B5172ECA08E53DE6F0F24177171106D520BDBE60130ECCBD39");combatProfiles=Pinned(profileCombat,24,"C0B70376C6411AE2E08D1834C34ED94C1291EE531E27F547D35E315CAE957A27");
        }
        public static PracticeAircraftTables Load(string programs) { return new PracticeAircraftTables(File.ReadAllBytes(Path.Combine(programs,"AircraftKind6Loadouts.seqbin")),File.ReadAllBytes(Path.Combine(programs,"AircraftKind7Loadouts.seqbin")),File.ReadAllBytes(Path.Combine(programs,"AircraftKind8Loadouts.seqbin")),File.ReadAllBytes(Path.Combine(programs,"AircraftKind6Profiles.seqbin")),File.ReadAllBytes(Path.Combine(programs,"AircraftCombatProfiles.seqbin")),File.ReadAllBytes(Path.Combine(programs,"AircraftVisibilityModes.seqbin"))); }
        static void Index(int kind,int mode) { if(kind<6||kind>8||mode<0||mode>5)throw new ArgumentOutOfRangeException("mode"); }
        public int Equipment(int kind,int mode,int slot) { Index(kind,mode);if(slot<0||slot>=12)throw new ArgumentOutOfRangeException("slot");return BitConverter.ToInt32(loadouts[kind-6],mode*48+slot*4); }
        public byte Visibility(int mode,int tier) { if(mode<0||mode>5||tier<0||tier>2)throw new ArgumentOutOfRangeException("mode");return visibility[mode*3+tier]; }
        public int Profile96(int kind,int mode) { Index(kind,mode);return BitConverter.ToInt16(kind==6?kind6Profiles:combatProfiles,mode*4); }
        public int UnitSpeed(int kind,int mode) { Index(kind,mode);return BitConverter.ToInt16(kind==6?kind6Profiles:combatProfiles,mode*4+2); }
    }
    public enum PracticeAircraftTransitionOutcome { Inactive, Docked, Activated }
    public sealed class PracticeAircraftTransitionResult {
        readonly byte[] ships,fleets,orders;
        public int SelectedWaypoint { get; private set; }
        public int Invalidations { get; private set; }
        public int ConfiguredMembers { get; private set; }
        public PracticeAircraftTransitionOutcome Outcome { get; private set; }
        internal PracticeAircraftTransitionResult(byte[] s,byte[] f,byte[] o,int point,int invalidations,int configured,PracticeAircraftTransitionOutcome outcome) { ships=s;fleets=f;orders=o;SelectedWaypoint=point;Invalidations=invalidations;ConfiguredMembers=configured;Outcome=outcome; }
        public byte[] CopyShipPool() { return (byte[])ships.Clone(); }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
        public byte[] CopyPrimaryOrders() { return (byte[])orders.Clone(); }
    }
    // Pinned RIP B9A8...6A569:428580,428960,42D070,42D120,4289B0.
    // This configures existing records; it does not allocate aircraft, move
    // them, run missions, or play owner-dependent media. Network0, gameMode0,
    // selected-unit0 and a different selected fleet define this data profile.
    public static class PracticeAircraftTransitions {
        static int Word(byte[] b,int p) { if(p<0||p>b.Length-2)throw new InvalidDataException("Aircraft field crosses its admitted pool.");return BitConverter.ToInt16(b,p); }
        static void Word(byte[] b,int p,int n) { if(p<0||p>b.Length-2)throw new InvalidDataException("Aircraft field crosses its admitted pool.");BitConverter.GetBytes(unchecked((short)n)).CopyTo(b,p); }
        static void Dword(byte[] b,int p,int n) { BitConverter.GetBytes(n).CopyTo(b,p); }
        static int Ship(uint reference) { long at=(long)reference-0x4fc340;if(at<0||at>=402000||at%268!=0)throw new InvalidDataException("Unsupported aircraft member reference.");return (int)at; }
        static void Ammunition(PracticeUnitCatalog catalog,byte[] ships,int at) {
            int length=Word(ships,at+14);if(length<1||length>6)throw new NotSupportedException("Aircraft equipment requires one to six admitted rows.");
            for(int weapon=0;weapon<35;weapon++)Word(ships,at+196+weapon*2,0);
            for(int e=0;e<length*4;e++){int weapon=BitConverter.ToInt32(ships,at+100+e*4);if(weapon<0||weapon>39)throw new InvalidDataException("Unsupported aircraft equipment ID.");int amount=Word(catalog.Ammunition,weapon*2),destination=at+196+weapon*2;Word(ships,destination,amount==-1?-1:Word(ships,destination)+amount);}
        }
        public static PracticeAircraftTransitionResult ApplyOffscreen(PracticeAircraftTables tables,PracticeUnitCatalog catalog,
                byte[] shipPool,byte[] fleetPool,byte[] primaryOrders,int actor,int mode,int selectedFleet,int selectedWaypoint,short localOwner) {
            if(tables==null||catalog==null)throw new ArgumentNullException("aircraft tables");
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000||primaryOrders==null||primaryOrders.Length!=78000)throw new InvalidDataException("Aircraft transitions require complete ship, fleet and primary-order pools.");
            if(actor<0||actor>=250||selectedFleet<0||selectedFleet>=250||mode<0||mode>5||selectedWaypoint< -1||selectedWaypoint>4)throw new ArgumentOutOfRangeException("actor");
            byte[] ships=(byte[])shipPool.Clone(),fleets=(byte[])fleetPool.Clone(),orders=(byte[])primaryOrders.Clone();int at=actor*492,kind=BitConverter.ToInt32(fleets,at);
            if(kind==0)return new PracticeAircraftTransitionResult(ships,fleets,orders,selectedWaypoint,0,0,PracticeAircraftTransitionOutcome.Inactive);
            if(Word(fleets,at+6)==localOwner)throw new NotSupportedException("Owner-dependent aircraft audio/media callbacks are outside this data profile.");
            if(mode==0) {
                if(kind<3||kind>8)throw new NotSupportedException("Docking admits aircraft fleet kinds3..8 only.");
                if(Word(fleets,at+4)==Word(fleets,selectedFleet*492+4))throw new NotSupportedException("Docking the selected fleet has an unreconstructed UI selection-transfer continuation.");
                for(int member=0;member<6;member++){uint reference=BitConverter.ToUInt32(fleets,at+16+member*4);if(reference==0)continue;int unit=Ship(reference);if(BitConverter.ToInt32(ships,unit)==0)continue;
                    // Original pushes the MEMBER INDEX, not the unit ID, to
                    //416250. Preserve its observed clear of primary rows0..5.
                    for(int n=0;n<10;n++)Word(orders,member*52+8+n*2,-1);
                }
                fleets[at+486]=0;if(kind>=3&&kind<=5)Dword(fleets,at,kind+3);Dword(fleets,at+472,0);
                return new PracticeAircraftTransitionResult(ships,fleets,orders,selectedWaypoint,0,0,PracticeAircraftTransitionOutcome.Docked);
            }
            if(kind<6||kind>8)throw new NotSupportedException("Positive aircraft modes require stored fleet kinds6..8; other source kinds leave table locals unresolved.");
            int parent=Word(fleets,at+8);if(parent<0||parent>=1500)throw new InvalidDataException("Aircraft associated unit is outside the ship pool.");int parentFleet=Word(ships,parent*268+12);if(parentFleet<0||parentFleet>=250)throw new InvalidDataException("Aircraft associated unit has an invalid parent fleet.");
            Dword(fleets,at,kind-3);Dword(fleets,at+472,0);Dword(fleets,at+476,mode);Word(fleets,at+12,Word(fleets,parentFleet*492+12));Word(fleets,at+14,Word(fleets,parentFleet*492+14));
            for(int n=0;n<5;n++)Word(fleets,at+40+n*2,-1);for(int tier=0;tier<3;tier++)fleets[at+464+tier]=tables.Visibility(mode,tier);int configured=0;
            for(int member=0;member<6;member++){uint reference=BitConverter.ToUInt32(fleets,at+16+member*4);if(reference==0)continue;int unit=Ship(reference),type=BitConverter.ToInt32(ships,unit);if(type==0)continue;
                if(type<8||type>10)throw new NotSupportedException("The admitted aircraft members use source templates8..10.");
                Word(ships,unit+72,tables.UnitSpeed(kind,mode));Word(ships,unit+96,tables.Profile96(kind,mode));for(int e=0;e<12;e++)Dword(ships,unit+100+e*4,tables.Equipment(kind,mode,e));
                Ammunition(catalog,ships,unit);if(mode==5)Dword(ships,unit+84,31);PracticeUnitDepletion.Reevaluate(catalog,ships,unit);configured++;
            }
            byte speed=0;for(int member=0;member<6;member++){uint reference=BitConverter.ToUInt32(fleets,at+16+member*4);if(reference==0)continue;int unit=Ship(reference);if(BitConverter.ToInt32(ships,unit)==0)continue;int current=Word(ships,unit+72);if(speed==0)speed=unchecked((byte)current);if(speed>current)speed=unchecked((byte)current);}Word(fleets,at+60,speed);
            return new PracticeAircraftTransitionResult(ships,fleets,orders,-1,1,configured,PracticeAircraftTransitionOutcome.Activated);
        }
    }
}
