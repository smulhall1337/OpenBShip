using System;
namespace FleetLab {
    // Exact supplied Bs.exe: hit 0x419CE0, miss 0x4177A0, sink 0x419270.
    // Bounded to Classic fleet types and reachable weapons; camera routing remains modern.
    public static class ClassicImpact {
        static void Weapon(int weapon){if(!((weapon>=14&&weapon<=18)||weapon==20||weapon==24||weapon==25||weapon==26||weapon==29||weapon==30))throw new NotSupportedException("Unsupported Classic impact weapon: "+weapon);}
        static bool Guns(int weapon){return weapon>=14&&weapon<=18;}
        static bool Torpedo(int weapon){return weapon==25||weapon==26;}
        public static int Hit(int type,int length,int remaining,int weapon){
            Weapon(weapon);if(length<1||length>10||remaining<0||remaining>=length)throw new ArgumentOutOfRangeException("remaining");
            if(remaining==0)return 0;bool first=length-remaining==1;
            switch(type){
                case 1:return Guns(weapon)?(first?45:78):Torpedo(weapon)?(first?44:72):(first?43:46);
                case 7:return Guns(weapon)?(first?20:113):Torpedo(weapon)?(first?79:114):(first?18:118);
                case 2:case 4:return Guns(weapon)?(first?55:122):Torpedo(weapon)?(first?74:119):(first?54:120);
                case 12:return Guns(weapon)?53:Torpedo(weapon)?81:85;
                default:throw new NotSupportedException("Unsupported Classic impact ship: "+type);
            }
        }
        public static int Sink(int type){switch(type){case 1:return 47;case 7:return 106;case 2:case 4:return 84;case 12:return 103;default:throw new NotSupportedException("Unsupported Classic sink ship: "+type);}}
        public static int Miss(int weapon,int roll){
            Weapon(weapon);if(roll<0||roll>32767)throw new ArgumentOutOfRangeException("roll");
            if(Guns(weapon))return new[]{68,69,80}[roll%3];if(Torpedo(weapon))return 65+roll%3;
            return weapon==29||weapon==30?13:71; // Classic initializer fleet kind is 1.
        }
        // 0x42408D returns before feedback/RNG for repeats on a surviving ship.
        // 0x424147/160 removes a sunk native record: a repeat there takes the
        // miss branch even though the modern board retains its ship metadata.
        public static bool NeedsRandom(int setting,Shot shot,Ship ship,int weapon){Weapon(weapon);return setting==0&&!shot.Hit&&!shot.Sunk&&!(shot.Repeated&&ship!=null&&!ship.Sunk)&&(Guns(weapon)||Torpedo(weapon));}
        public static int Select(int setting,Shot shot,Ship ship,int weapon,int roll){
            if(setting<0||setting>3||shot==null)throw new ArgumentException("Invalid impact input");
            if(shot.Hit&&ship==null)throw new ArgumentException("Missing impacted ship");
            if(shot.Repeated&&ship!=null&&!ship.Sunk)return 0;
            if(shot.Sunk)return Sink(ship.SourceType);
            if(setting>0)return shot.Hit?137:136;
            return shot.Hit?Hit(ship.SourceType,ship.Cells.Count,ship.Cells.Count-ship.Hits,weapon):Miss(weapon,roll);
        }
    }
}

