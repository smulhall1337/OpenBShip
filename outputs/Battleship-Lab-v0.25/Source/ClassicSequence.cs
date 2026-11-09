using System;
using System.Drawing;
namespace FleetLab {
    public enum ShotStage { Firing,Travel,Feedback }
    public sealed class PendingShot {
        public readonly Point Cell;public readonly bool Opponent;public readonly FiringPresentation Firing;public readonly int TravelId;
        public ShotStage Stage {get;internal set;}public Shot Result {get;internal set;}public int FeedbackId {get;internal set;}
        public string StageLabel {get{return Stage!=ShotStage.Feedback?Stage.ToString():Result.Sunk?"Sinking":Result.Hit?"Hit":"Miss";}}
        public PendingShot(Point cell,bool opponent,FiringPresentation firing,int travel){Cell=cell;Opponent=opponent;Firing=firing;TravelId=travel;}
        public string MovieName {get{return Stage==ShotStage.Firing?Firing.MovieName:ClassicSequence.MovieName(Stage==ShotStage.Travel?TravelId:FeedbackId);}}
    }
    public static class ClassicSequence {
        // Bs.exe 0x4235B9..0x4236C2 and mirrored 0x42373B..0x423836.
        public static int Travel(int weapon,int roll){if(roll<0||roll>32767)throw new ArgumentOutOfRangeException("roll");if(weapon==25||weapon==26)return 24;if(weapon>=27&&weapon<=30)return 12;if(weapon>=14&&weapon<=18)return 129+roll%3;return 21;}
        public static string MovieName(int id){switch(id){case 12:return "MIS_FLY2";case 13:return "MIS_FLY3";case 18:return "CAR_UMH";case 20:return "CAR_USH";case 21:return "CRS_FLY1";case 24:return "TORPRUN";case 43:return "BATT_UMH";case 44:return "BATT_UTH";case 45:return "BATT_USH";case 46:return "BATT_DMH";case 47:return "BATTSINK";case 53:return "SUB_USH";case 54:return "FRG_UMH";case 55:return "FRG_USH";case 65:return "TORPMISA";case 66:return "TORPMISB";case 67:return "TORPMISC";case 68:return "SHELMISA";case 69:return "SHELMISB";case 71:return "CRUISDIE";case 72:return "BATT_DTH";case 74:return "FRG_UTH";case 78:return "BATT_DSH";case 79:return "CAR_UTH";case 80:return "SHELMISC";case 81:return "SUB_UTH";case 84:return "FRIGSINK";case 85:return "SUB_UMH";case 103:return "SUB_DIE";case 106:return "CARRSINK";case 113:return "CAR_DSH";case 114:return "CAR_DTH";case 118:return "CAR_DMH";case 119:return "FRG_DTH";case 120:return "FRG_DMH";case 122:return "FRG_DSH";case 129:return "S_FLY1";case 130:return "S_FLY2";case 131:return "S_FLY3";case 136:return "CL_MISS";case 137:return "CL_HIT";default:throw new NotSupportedException("Unsupported Classic sequence movie: "+id);}}
    }
}

