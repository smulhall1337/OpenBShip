using System;using System.IO;
namespace FleetLab {
 public sealed class StartupProgram {
  static readonly string[] names={"HASBRO","NMSLOGO1","UK_INTRO"};public static string[] Names{get{return (string[])names.Clone();}}
  public static StartupProgram Read(byte[] bytes){uint[] expected={1,6,141,14,1,6,142,14,1,6,138,14,18,0x44ef88,4,0};if(bytes==null||bytes.Length!=64)throw new InvalidDataException("Startup script size");for(int i=0;i<expected.Length;i++)if(BitConverter.ToUInt32(bytes,i*4)!=expected[i])throw new InvalidDataException("Unsupported startup instruction/operand");return new StartupProgram();}
  public static string Directory(string name){if(Array.IndexOf(names,name)<0)throw new InvalidDataException("Unknown startup movie");return "HWEBS";}
  public static string SoundDirectory(string name){Directory(name);return name=="UK_INTRO"?"INTRO":name;}
 }
}
