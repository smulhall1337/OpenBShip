using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab {
    public sealed class ClassicHudCommand {
        public readonly bool Panel;public readonly int Frame;public readonly Point At;
        public ClassicHudCommand(bool panel,int frame,int x,int y){Panel=panel;Frame=frame;At=new Point(x,y);}
    }
    // Source command dispatch 0x40938C..0x40956F, asset slot41 at0x42AC51.
    // UI backing stays opaque per the approved modern presentation; original SPH
    // stored runs/pixels are retained, including black backing rather than keying it.
    public sealed class ClassicHud : IDisposable {
        readonly List<Bitmap> panels=new List<Bitmap>();
        public ClassicHud(string graphics,Color[] palette){
            try{var frames=Assets.Sph(Path.Combine(graphics,"E_CLAS2.SPH"),palette);Size[] sizes={new Size(200,216),new Size(200,216),new Size(197,216),new Size(205,216)};if(frames.Count!=5||frames[4]!=null)throw new InvalidDataException("Unsupported Classic HUD panel table");for(int i=0;i<4;i++){if(frames[i]==null||frames[i].Width!=sizes[i].Width||frames[i].Height!=sizes[i].Height)throw new InvalidDataException("Classic HUD panel dimensions differ");panels.Add(frames[i].Bitmap());}}catch{Dispose();throw;}
        }
        public Bitmap Panel(int frame){if(frame<0||frame>=panels.Count)throw new ArgumentOutOfRangeException("frame");return panels[frame];}
        public static int Phase(Game game){
            if(game==null)throw new ArgumentNullException("game");
            if(game.Phase==FleetLab.Phase.Finished)return -1;
            if(game.Pending==null)return game.Phase==FleetLab.Phase.OpponentTurn?4:0;
            return (game.Pending.Opponent?4:0)+(game.Pending.Stage==ShotStage.Firing?1:game.Pending.Stage==ShotStage.Travel?2:3);
        }
        public static List<ClassicHudCommand> Commands(int phase,bool movie,int ownResult,int enemyResult,int aliveMask){
            if(phase<0||phase>8||ownResult< -1||ownResult>1||enemyResult< -1||enemyResult>1||aliveMask<0||aliveMask>31)throw new ArgumentOutOfRangeException("phase");
            var commands=new List<ClassicHudCommand>();
            if(movie){commands.Add(new ClassicHudCommand(true,0,0,0));commands.Add(new ClassicHudCommand(true,2,200,0));commands.Add(new ClassicHudCommand(true,1,235,264));commands.Add(new ClassicHudCommand(true,3,435,264));}
            bool enemy=phase>3;commands.Add(new ClassicHudCommand(false,enemy?1:0,enemy?382:232,enemy?392:56));
            for(int i=0;i<5;i++){Point at=BoardView.RosterPosition(enemy,i);commands.Add(new ClassicHudCommand(false,((aliveMask&(1<<i))!=0?4:9)+i,at.X,at.Y));}
            if((phase==3||phase==4)&&enemyResult!=-1)commands.Add(new ClassicHudCommand(false,enemyResult+2,512,272));
            if((phase==7||phase==8)&&ownResult!=-1)commands.Add(new ClassicHudCommand(false,ownResult+2,32,176));
            return commands;
        }
        public void Dispose(){foreach(Bitmap panel in panels)panel.Dispose();panels.Clear();}
    }
}
