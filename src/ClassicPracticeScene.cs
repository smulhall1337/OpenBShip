using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing.Imaging;
namespace FleetLab {
    // Static software comparison to432490. Supplied type/page/hover states are
    // not a replacement for the original Practice interpreter or its input.
    public sealed class ClassicPracticeScene {
        readonly IndexedImage card;
        readonly Dictionary<int,List<IndexedImage>> sprites;
        readonly SoloProgram program;
        readonly ClassicMenuFont font;
        internal SoloProgram Program {get{return program;}}
        public ClassicPracticeScene(string assets){
            string graphics=Path.Combine(assets,"Graphics"),programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            card=Assets.Pcx(Path.Combine(graphics,"E2_SOLO.PCX"));
            var reserved=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(graphics,"ISLAND.COL")));
            for(int i=0;i<256;i++)if(i<10||i>245)card.Palette[i]=reserved[i];
            sprites=new Dictionary<int,List<IndexedImage>>{{28,Assets.Sph(Path.Combine(graphics,"EO1_SOLO.SPH"),card.Palette)},{29,Assets.Sph(Path.Combine(graphics,"EO2_SOLO.SPH"),card.Palette)},{38,Assets.Sph(Path.Combine(graphics,"EO4_SOLO.SPH"),card.Palette)}};
            program=SoloProgram.Read(File.ReadAllBytes(Path.Combine(programs,"SoloEntry.seqbin")),File.ReadAllBytes(Path.Combine(programs,"PracticeSetup.seqbin")),File.ReadAllBytes(Path.Combine(programs,"PlayerLimits.seqbin")),sprites,File.ReadAllLines(Path.Combine(graphics,"SCNTEXT.TXT")));
            font=new ClassicMenuFont(Assets.Sph(Path.Combine(graphics,"FEFONT.SPH"),card.Palette));
        }
        public IndexedImage Compose(int type=1,int players=2,int page=0,int hover=-1){
            return ComposeCore(type,players,page,hover,-1);
        }
        // Explicit comparison override for long source descriptions. Normal
        // Practice selection never changes its description pointer table.
        public IndexedImage ComposeDescriptionComparison(int description,int players=2,int page=0,int hover=-1){
            if(description<0||description>20)throw new InvalidDataException("Unsupported original description comparison");
            return ComposeCore(1,players,page,hover,description);
        }
        IndexedImage ComposeCore(int type,int players,int page,int hover,int description){
            if(type!=1&&type!=2&&type!=3&&type!=20||players<2||players>4||page!=0&&page!=12&&page!=24||hover!=-1&&hover!=10&&hover!=11&&hover!=12&&hover!=13)throw new InvalidDataException("Unsupported Practice comparison context");
            var state=new SoloState(program);state.SelectType(type);state.SelectPlayers(players);
            var result=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=(System.Drawing.Color[])card.Palette.Clone()};
            Array.Copy(card.Pixels,result.Pixels,result.Pixels.Length);
            string[] rows=ClassicMenuText.ReadRows(ClassicMenuText.Format(program.Description(description<0?type:description)));
            for(int row=0;row<12;row++)font.Draw(result.Pixels,5,16,250+row*14,rows[page+row]);
            foreach(var command in program.Commands(state,hover)){
                var b=program.Buttons[command.Index];ClassicMenuFont.Blit(result.Pixels,sprites[b.Group][command.Frame],b.X,b.Y);
            }
            if(page==24||rows[page+12].Length==0)Button(result.Pixels,13,2);else if(hover==13)Button(result.Pixels,13,0);
            if(page==0)Button(result.Pixels,12,2);else if(hover==12)Button(result.Pixels,12,0);
            if(rows[12].Length!=0){
                ClassicMenuFont.Blit(result.Pixels,sprites[38][page/12+1],260,222);
                ClassicMenuFont.Blit(result.Pixels,sprites[38][10],275,222);
                ClassicMenuFont.Blit(result.Pixels,sprites[38][rows[24].Length!=0?3:2],290,222);
            }
            return result;
        }
        void Button(byte[] pixels,int index,int delta){var b=program.Buttons[index];ClassicMenuFont.Blit(pixels,sprites[b.Group][b.Frame+delta],b.X,b.Y);}
        public void SavePreview(string output){Save(Compose(),output);}
        public void SaveDescriptionComparison(int description,int page,string output){Save(ComposeDescriptionComparison(description,2,page),output);}
        static void Save(IndexedImage pixels,string output){string path=Path.GetFullPath(output);Directory.CreateDirectory(Path.GetDirectoryName(path));using(var image=pixels.Bitmap())image.Save(path,ImageFormat.Png);}
    }
}
