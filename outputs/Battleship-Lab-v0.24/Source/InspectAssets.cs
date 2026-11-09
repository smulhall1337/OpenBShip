using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing.Imaging;
namespace FleetLab {
    class InspectAssets {
        static int Main(string[] args) {
            if(args.Length!=2)return 2;
            string dir=args[0],outDir=args[1];
            Directory.CreateDirectory(outDir);
            var lines=new List<string>();
            int ok=0,failed=0;
            foreach(string file in Directory.GetFiles(dir,"*.SCN")) {
                try {
                    var s=Scenario.Load(file);
                    lines.Add(Path.GetFileName(file)+" OK terrain="+s.TerrainBytes+" players="+s.Players);
                    ok++;
                }
                catch(Exception e) {
                    lines.Add(Path.GetFileName(file)+" UNSUPPORTED "+e.Message);
                    failed++;
                }
            }
            lines.Add("Scenario exact parses="+ok+" unsupported="+failed);
            ok=0;
            failed=0;
            foreach(string file in Directory.GetFiles(dir,"*.PCX")) {
                try {
                    var image=Assets.Pcx(file);
                    if(image.TrailingBytes!=0)lines.Add(Path.GetFileName(file)+" preserved PCX trailing bytes="+image.TrailingBytes);
                    ok++;
                    if(Path.GetFileName(file).Equals("WATER.PCX",StringComparison.OrdinalIgnoreCase)||Path.GetFileName(file).Equals("E_CLASS.PCX",StringComparison.OrdinalIgnoreCase))using(var bmp=image.Bitmap())bmp.Save(Path.Combine(outDir,Path.GetFileNameWithoutExtension(file)+".png"),ImageFormat.Png);
                }
                catch(Exception e) {
                    lines.Add(Path.GetFileName(file)+" UNSUPPORTED "+e.Message);
                    failed++;
                }
            }
            lines.Add("PCX decoded="+ok+" unsupported="+failed);
            var palette=Assets.Palette(Path.Combine(dir,"ISLAND.COL"));
            ok=0;
            failed=0;
            foreach(string file in Directory.GetFiles(dir,"*.SPH")) {
                try {
                    var frames=Assets.Sph(file,palette);
                    int trailing=0;
                    foreach(var image in frames)if(image!=null)trailing+=image.TrailingBytes;
                    if(trailing!=0)lines.Add(Path.GetFileName(file)+" preserved SPH trailing bytes="+trailing);
                    ok++;
                    if(Path.GetFileName(file).Equals("BATTSHIP.SPH",StringComparison.OrdinalIgnoreCase)||Path.GetFileName(file).Equals("POINTER.SPH",StringComparison.OrdinalIgnoreCase))for(int i=0; i<frames.Count; i++)if(frames[i]!=null)using(var bmp=frames[i].Bitmap())bmp.Save(Path.Combine(outDir,Path.GetFileNameWithoutExtension(file)+"-"+i+".png"),ImageFormat.Png);
                }
                catch(Exception e) {
                    lines.Add(Path.GetFileName(file)+" UNSUPPORTED "+e.Message);
                    failed++;
                }
            }
            lines.Add("SPH decoded="+ok+" unsupported="+failed);
            File.WriteAllLines(Path.Combine(outDir,"format-report.txt"),lines.ToArray());
            Console.WriteLine(string.Join(Environment.NewLine,lines.ToArray()));
            return 0;
        }
    }
}
