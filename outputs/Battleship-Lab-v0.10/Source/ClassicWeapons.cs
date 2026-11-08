using System;
namespace FleetLab {
    public sealed class FiringPresentation {
        public readonly string Shooter,WeaponName,MovieName;
        public readonly int WeaponId,MovieId,RandomCalls;
        public FiringPresentation(string shooter,int weapon,string name,int movie,string clip,int calls) {
            Shooter=shooter;WeaponId=weapon;WeaponName=name;MovieId=movie;MovieName=clip;RandomCalls=calls;
        }
        public string Description { get { return Shooter+" / "+WeaponName; } }
    }
    // Bs.exe B9A8...A569. Player: 0x423EDE..0x423F2C, lists 0x44D730/750/760.
    // AI: 0x41C8C0 always uses the generic list. Movie dispatch: 0x416640.
    // These are presentation choices, not additional damage or ammunition rules.
    public static class ClassicWeapons {
        static readonly int[] Generic={14,15,16,26,17,18,20},Submarine={26,24,25},Carrier={29,30};
        // Native text records: 0x44B560 + weaponID*48 (xref 0x40DA45..0x40DA60).
        static readonly string[] Names={"Small Guns","Medium Guns","Large Guns","Rapid Small Guns","Ultra Large Guns","Nuclear Missile","Multi Head Missile","Submarine Nuclear Missile","Cruise Missile","Medium Missile","Submarine Cruise Missile","Torpedos","Large Torpedos","Air Rockets","Air Missiles","Multi Air Rockets","Multi Air Missiles"};
        public static int Weapon(int shipType,bool opponent,int randomResult) {
            if(randomResult<0||randomResult>32767)throw new ArgumentOutOfRangeException("randomResult");
            int[] list=opponent?Generic:shipType==12?Submarine:shipType==7?Carrier:Generic;
            // The source walks a zero-terminated list, wrapping after each zero.
            // Modulo 50 BEFORE wrapping is intentional: generic/sub lists are biased.
            return list[(randomResult%50)%list.Length];
        }
        public static string Name(int weapon) {
            if(weapon<14||weapon>30)throw new NotSupportedException("Unsupported weapon name: "+weapon);
            return Names[weapon-14];
        }
        public static int Movie(int shipType,int weapon,int randomResult) {
            if(randomResult<0||randomResult>32767)throw new ArgumentOutOfRangeException("randomResult");
            bool odd=(randomResult&1)!=0;
            switch(weapon) {
                case 14:case 15:return odd?127:128;
                case 16:case 18:return new[]{1,2,3,123}[randomResult%4];
                case 17:return odd?33:34;
                case 20:return odd?29:30;
                case 24:return odd?27:28;
                case 25:case 26:
                    return shipType==12||shipType==11?(odd?25:26):shipType==1||shipType==3?(odd?124:125):(odd?126:90);
                case 29:case 30:return shipType==10?(odd?37:38):(odd?10:9);
                default:throw new NotSupportedException("Weapon is outside the recovered Classic firing subset: "+weapon);
            }
        }
        public static string Clip(int movie) {
            switch(movie) {
                case 1:return "BATFIRE1";case 2:return "BATFIRE2";case 3:return "BATFIRE3";case 123:return "BATFIRE4";
                case 9:return "F18FIRE2";case 10:return "F18FIRE1";
                case 25:return "SUBTORP1";case 26:return "SUBTORP2";case 27:return "SUBMISS1";case 28:return "SUBMISS2";
                case 29:return "SML_LNC1";case 30:return "SML_LNC2";case 33:return "TWOFIRE1";case 34:return "TWOFIRE2";
                case 37:return "CHOP_FR1";case 38:return "CHOP_FR2";case 90:return "FRG_FTRP";
                case 124:return "BATT_LT1";case 125:return "BATT_LT2";case 126:return "FRG_LT2";
                case 127:return "MEDFIRE1";case 128:return "MEDFIRE2";
                default:throw new NotSupportedException("Movie is outside the recovered Classic firing subset: "+movie);
            }
        }
        public static FiringPresentation Choose(Ship shooter,bool opponent,NativeRandom random) {
            if(shooter==null||shooter.Sunk)throw new ArgumentException("A surviving firing ship is required");
            if(random==null)throw new ArgumentNullException("random");
            int before=random.Calls;
            int weapon=Weapon(shooter.SourceType,opponent,random.Next());
            int movie=Movie(shooter.SourceType,weapon,random.Next());
            return new FiringPresentation(shooter.Name,weapon,Name(weapon),movie,Clip(movie),random.Calls-before);
        }
    }
}
