using System;using System.IO;using System.Drawing;using System.Collections.Generic;
namespace FleetLab {
 public sealed class QuitProgram {
  public readonly List<TitleButton> Buttons=new List<TitleButton>();
  public static QuitProgram Read(byte[] bytes,IList<IndexedImage> frames){
   uint[] expected={21,0x457b58,2,8,0x44ef2c,32,0,38,2,364,372,0,33,1,162,372,2,33,2,13,11,9,21,0x456a18,0xffffffff,39,13,1,9,26,0x432360,0,0x457b58,18,0x44efc0,13,2,9,26,0x432360,0,0x457b58,21,0x457b58,4,14,1,4};
   if(bytes==null||bytes.Length!=expected.Length*4)throw new InvalidDataException("Unsupported Quit script length");
   for(int i=0;i<expected.Length;i++)if(BitConverter.ToUInt32(bytes,i*4)!=expected[i])throw new InvalidDataException("Unsupported Quit instruction/operand at byte "+i*4);
   if(frames==null||frames.Count!=5||frames[4]!=null)throw new InvalidDataException("Unsupported Quit sprite table");
   for(int i=0;i<4;i++)if(frames[i]==null||frames[i].Width!=109||frames[i].Height!=43)throw new InvalidDataException("Unsupported Quit button geometry");
   var result=new QuitProgram();result.Buttons.Add(new TitleButton(364,372,0,33,1,109,43,0));result.Buttons.Add(new TitleButton(162,372,2,33,2,109,43,0));return result;
  }
  public int Hit(int x,int y){for(int i=0;i<Buttons.Count;i++)if(Buttons[i].Bounds.Contains(x,y))return i;return -1;}
  public static IndexedImage Compose(IndexedImage card,IList<IndexedImage> frames,QuitProgram program,int selected){
   if(card==null||card.Width!=640||card.Height<480||card.Pixels==null||card.Pixels.Length<640*480||program==null||selected<0||selected>=2)throw new InvalidDataException("Unsupported Quit composition");
   var result=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=card.Palette};Array.Copy(card.Pixels,result.Pixels,result.Pixels.Length);
   for(int i=0;i<2;i++){var b=program.Buttons[i];var f=frames[b.Frame+(i==selected?1:0)];for(int y=0;y<f.Height;y++)for(int x=0;x<f.Width;x++){int p=y*f.Width+x;if(f.Visible[p]&&f.Pixels[p]!=0)result.Pixels[(b.Y+y)*640+b.X+x]=f.Pixels[p];}}
   return result;
  }
 }
}
