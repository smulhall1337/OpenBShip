using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace FleetLab {
    // Canonical Classic deployment scene: 430880/431B00/431C70/431DA0.
    // Supplied complete, undamaged own fleet; simulation and input stay separate.
    public sealed class ClassicFleetScene {
        readonly IndexedImage card;
        readonly Dictionary<int,List<IndexedImage>> banks=new Dictionary<int,List<IndexedImage>>();
        readonly Dictionary<string,IndexedImage> scaled=new Dictionary<string,IndexedImage>();
        readonly List<ClassicSceneCommand> commands=new List<ClassicSceneCommand>();
        readonly ReadOnlyCollection<ClassicSceneCommand> view;
        internal FleetProgram Program {get;private set;}
        public IList<ClassicSceneCommand> Commands {get{return view;}}
        public ClassicFleetScene(string assets) {
            string graphics=Path.Combine(assets,"Graphics"),programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            card=FleetProgram.Viewport(Assets.Pcx(Path.Combine(graphics,"EC_FLEET.PCX")));
            var reserved=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(graphics,"ISLAND.COL")));
            for(int i=0;i<256;i++)if(i<10||i>245)card.Palette[i]=reserved[i];
            int[] ids={6,10,7,13,31};string[] files={"ALLSHIPS.SPH","BATTSHIP.SPH","FRIGATE.SPH","NEWSHIPS.SPH","EO_FLEET.SPH"};
            for(int i=0;i<ids.Length;i++)banks.Add(ids[i],Assets.Sph(Path.Combine(graphics,files[i]),card.Palette));
            Program=FleetProgram.Read(File.ReadAllBytes(Path.Combine(programs,"FleetFlow.seqbin")),File.ReadAllBytes(Path.Combine(programs,"FleetButtons.seqbin")),banks[31]);
            view=commands.AsReadOnly();
        }
        static void Validate(Board board) {
            if(board==null||board.Ships.Count!=5)throw new InvalidDataException("Classic deployment requires its complete five-ship roster");
            var occupied=new bool[10,10];
            for(int i=0;i<5;i++) {
                var ship=board.Ships[i];var spec=RuleProfile.Recovered.Fleet[i];
                if(ship==null||ship.SourceType!=spec.SourceType||ship.Cells==null||ship.Cells.Count!=spec.Length||ship.Hits!=0)throw new InvalidDataException("Unsupported Classic deployment ship");
                Point start=ship.Cells[0];
                for(int j=0;j<ship.Cells.Count;j++){Point cell=ship.Cells[j];if(!Board.Inside(cell)||cell.X!=start.X||cell.Y!=start.Y+j||occupied[cell.X,cell.Y])throw new InvalidDataException("Unsupported Classic deployment cells");occupied[cell.X,cell.Y]=true;}
            }
            foreach(bool shot in board.Shots)if(shot)throw new InvalidDataException("Deployment scene does not support fired cells");
        }
        static void Pixel(IndexedImage image,int x,int y){if(x<0||x>=640||y<0||y>=480)throw new InvalidDataException("Fleet grid outside viewport");image.Pixels[y*640+x]=234;}
        static void Line(IndexedImage image,Point start,int direction){for(int i=0;i<(147*10/6+2)/3;i++){int x=start.X+i*3,y=start.Y+i*direction*2;Pixel(image,x,y);Pixel(image,x+1,y+direction);Pixel(image,x+2,y+direction);}}
        void Add(int x,int y,int frame,int bank,int mode,int percent){commands.Add(new ClassicSceneCommand(x,y,frame,bank,mode,percent));}
        void Hulls(Board board,bool miniature){foreach(var ship in board.Ships){Point at=miniature?FleetOverview.Anchor(ship.Cells[0],ship.Cells.Count):FleetGeometry.Anchor(ship.Cells[0],ship.Cells.Count);int type=ship.SourceType,bank=type==7||type==12?6:type==1?10:type==2?7:13;Add(at.X,at.Y,ClassicShips.Frame(type),bank,1,miniature?18:100);}}
        public IndexedImage Compose(Board board,int hover=-1) {
            Validate(board);if(hover!=-1&&hover!=2&&hover!=3)throw new InvalidDataException("Unsupported Classic Fleet hover");
            var result=new IndexedImage{Width=640,Height=480,Pixels=(byte[])card.Pixels.Clone(),Palette=(Color[])card.Palette.Clone()};
            commands.Clear();FleetOverview.Raster(result,false);FleetOverview.Raster(result,true);
            for(int i=0;i<=10;i++){Line(result,FleetGeometry.Project(i,0),1);Line(result,FleetGeometry.Project(0,i),-1);}
            Hulls(board,true);for(int i=0;i<4;i++)Add(17,183+i*60,10+i,31,0,100);Hulls(board,false);
            // Classic suppresses purchase/budget controls with the source overlay.
            Add(417,104,28,31,0,100);
            if(hover>=0){var button=Program.Buttons[hover];Add(button.X,button.Y,button.Frame,31,0,100);}
            foreach(var command in commands){string key=command.Bank+":"+command.Frame+":"+command.Percent;IndexedImage image;if(!scaled.TryGetValue(key,out image)){image=ClassicShips.Scale(banks[command.Bank][command.Frame],command.Percent);scaled.Add(key,image);}int x=command.X,y=command.Y;if(command.Mode==1){x-=image.Width/2;y-=image.Height/2;}ClassicMenuFont.Blit(result.Pixels,image,x,y);}
            return result;
        }
        public IndexedImage Preview(int hover=-1){var game=new Game(0);game.AutoPlace();return Compose(game.Player,hover);}
        public void SavePreview(string path,int hover=-1){string full=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(full));using(var image=Preview(hover).Bitmap())image.Save(full,System.Drawing.Imaging.ImageFormat.Png);}
    }
}
