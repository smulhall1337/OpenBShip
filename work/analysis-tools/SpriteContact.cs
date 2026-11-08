using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using FleetLab;
class SpriteContact {
 static int Main(string[] args){Directory.CreateDirectory(args[1]);var pal=Assets.Pcx(Path.Combine(args[0],"E_CLASS.PCX")).Palette;foreach(string name in new[]{"E_CLAS2.SPH","EO_CLASS.SPH","ALLSHIPS.SPH","SHIPPICS.SPH"}){var frames=Assets.Sph(Path.Combine(args[0],name),pal);int cellW=220,cellH=155,rows=(frames.Count+3)/4;using(var sheet=new Bitmap(cellW*4,cellH*rows))using(var g=Graphics.FromImage(sheet))using(var font=new Font("Segoe UI",9)){g.Clear(Color.FromArgb(25,40,55));for(int i=0;i<frames.Count;i++){int x=(i%4)*cellW,y=(i/4)*cellH;var frame=frames[i];g.DrawString(i+": "+(frame==null?"absent":frame.Width+"x"+frame.Height),font,Brushes.White,x+6,y+3);if(frame!=null)using(var bmp=frame.Bitmap()){float scale=Math.Min(1f,Math.Min((cellW-12)/(float)frame.Width,(cellH-28)/(float)frame.Height));g.DrawImage(bmp,x+6,y+24,frame.Width*scale,frame.Height*scale);}}sheet.Save(Path.Combine(args[1],name+".png"),ImageFormat.Png);}Console.WriteLine(name+": "+frames.Count+" slots");}return 0;}
}
