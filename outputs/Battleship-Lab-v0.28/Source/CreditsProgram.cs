using System;using System.IO;using System.Text;using System.Collections.Generic;
namespace FleetLab {
 public sealed class CreditsText {
  public readonly byte[] OriginalBytes;public readonly string[] Lines;
  public CreditsText(byte[] bytes){if(bytes==null||bytes.Length<2||bytes.Length>65536)throw new InvalidDataException("Credits text size");foreach(byte b in bytes)if(b!=10&&b!=13&&(b<32||b>126))throw new InvalidDataException("Credits ASCII encoding");string value=Encoding.ASCII.GetString(bytes).Replace("\r\n","\n");if(value.IndexOf('\r')>=0||!value.EndsWith("\n\n",StringComparison.Ordinal))throw new InvalidDataException("Credits terminal blank line");string[] split=value.Split('\n');Lines=new string[split.Length-2];Array.Copy(split,Lines,Lines.Length);if(Lines.Length<1)throw new InvalidDataException("Empty credits");foreach(string line in Lines)if(line.Length>1024)throw new InvalidDataException("Credits line size");OriginalBytes=(byte[])bytes.Clone();}
 }
 // Exact bounded Preferences action10; opcode1 is clear, not a fade operand.
 public sealed class CreditsProgram {
  public readonly byte[] OriginalScript;CreditsProgram(byte[] b){OriginalScript=(byte[])b.Clone();}
  public static CreditsProgram Read(byte[] bytes){int[] expected={13,10,14,1,41,14,1,18,0x44ef88};if(bytes==null||bytes.Length!=36)throw new InvalidDataException("Credits script size");for(int i=0;i<expected.Length;i++)if(BitConverter.ToInt32(bytes,i*4)!=expected[i])throw new InvalidDataException("Unsupported Credits dependency");return new CreditsProgram(bytes);}
 }
 public sealed class CreditsRoll {
  public readonly CreditsText Text;public long Ticks {get;private set;}long remainder;
  public CreditsRoll(CreditsText text){if(text==null)throw new ArgumentNullException("text");Text=text;}
  public int StateLine{get{return (int)(Ticks%(Text.Lines.Length*7L))/7;}}public int StateOffset{get{return -(int)(Ticks%7)*2;}}
  public int DisplayLine{get{return (int)((Ticks==0?0:Ticks-1)%(Text.Lines.Length*7L))/7;}}public int DisplayOffset{get{return -(int)((Ticks==0?0:Ticks-1)%7)*2;}}
  public bool Advance(long milliseconds){if(milliseconds<0)throw new ArgumentOutOfRangeException("milliseconds");long full=milliseconds/50;remainder+=milliseconds%50;full+=remainder/50;remainder%=50;Ticks=checked(Ticks+full);return full>0;}
  public void Reset(){Ticks=remainder=0;}
  public List<MessageLine> Commands(ClassicSpriteFont font){return Commands(font,DisplayLine,DisplayOffset);}
  public List<MessageLine> Commands(ClassicSpriteFont font,int first,int offset){if(first<0||first>=Text.Lines.Length||offset>0||offset< -12||offset%2!=0)throw new ArgumentOutOfRangeException("first");var lines=new List<MessageLine>();for(int row=0;row<35&&first+row<Text.Lines.Length;row++){string value=Text.Lines[first+row];lines.Add(new MessageLine(320-font.Measure(value,4)/2,row*14+offset,value));}return lines;}
 }
}
