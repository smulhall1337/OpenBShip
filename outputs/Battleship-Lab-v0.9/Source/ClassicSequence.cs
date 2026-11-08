using System;
using System.Drawing;
namespace FleetLab {
    public enum ShotStage { Firing,Travel,Feedback }
    public sealed class PendingShot {
        public readonly Point Cell;public readonly bool Opponent;public readonly FiringPresentation Firing;public readonly int TravelId;
        public ShotStage Stage {get;internal set;}public Shot Result {get;internal set;}public int FeedbackId {get;internal set;}
        public PendingShot(Point cell,bool opponent,FiringPresentation firing,int travel){Cell=cell;Opponent=opponent;Firing=firing;TravelId=travel;}
        public string MovieName {get{return Stage==ShotStage.Firing?Firing.MovieName:ClassicSequence.MovieName(Stage==ShotStage.Travel?TravelId:FeedbackId);}}
    }
    public static class ClassicSequence {
        // Bs.exe 0x4235B9..0x4236C2 and mirrored 0x42373B..0x423836.
        public static int Travel(int weapon,int roll){if(roll<0||roll>32767)throw new ArgumentOutOfRangeException("roll");if(weapon==25||weapon==26)return 24;if(weapon>=27&&weapon<=30)return 12;if(weapon>=14&&weapon<=18)return 129+roll%3;return 21;}
        public static string MovieName(int id){switch(id){case 129:return "S_FLY1";case 130:return "S_FLY2";case 131:return "S_FLY3";case 24:return "TORPRUN";case 12:return "MIS_FLY3";case 21:return "CRS_FLY1";case 136:return "CL_MISS";case 137:return "CL_HIT";default:throw new NotSupportedException("Unsupported Classic sequence movie: "+id);}}
        // 0x4240B7 returns on sinking; 0x42408D rejects repeated occupied hits.
        // Valid misses/hits at source setting >0 request CL_MISS / CL_HIT.
        public static int Feedback(int setting,Shot shot,bool repeatedOccupied){if(setting<0||setting>3||shot==null)throw new ArgumentException("Invalid impact feedback input");return setting==0||shot.Sunk||repeatedOccupied?0:shot.Hit?137:136;}
    }
}
