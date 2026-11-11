using System;using System.IO;using System.Drawing.Imaging;using System.Collections.Generic;
namespace FleetLab {
    // Whole-image Bs.exe B9A8...A569: title script ->4300B0/4323E0 ->
    //407800/407890/407950. Numeric navigation is outside this renderer.
    public sealed class ClassicTitleScene {
        internal readonly IndexedImage Card;
        readonly List<IndexedImage> frames;
        public readonly TitleProgram Program;
        public ClassicTitleScene(string assets){
            string graphics=Path.Combine(assets,"Graphics"),programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            Card=Assets.Pcx(Path.Combine(graphics,"E_TITLE.PCX"));
            // Native401D40 changes10..245. Keep the established modern Windows
            // reserved colors; this is not a capture of the original desktop.
            var reserved=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(graphics,"ISLAND.COL")));
            for(int i=0;i<256;i++)if(i<10||i>245)Card.Palette[i]=reserved[i];
            frames=Assets.Sph(Path.Combine(graphics,"EO_TITLE.SPH"),Card.Palette);
            Program=TitleProgram.Read(File.ReadAllBytes(Path.Combine(programs,"MenuEntry.seqbin")),File.ReadAllBytes(Path.Combine(programs,"MenuTitle.seqbin")),frames);
        }
        public IndexedImage Render(int selected){return TitleProgram.Compose(Card,frames,Program,selected);}
        public void SavePreview(string filename){
            string output=Path.GetFullPath(filename);Directory.CreateDirectory(Path.GetDirectoryName(output));
            using(var bitmap=Render(0).Bitmap())bitmap.Save(output,ImageFormat.Png);
        }
    }
}
