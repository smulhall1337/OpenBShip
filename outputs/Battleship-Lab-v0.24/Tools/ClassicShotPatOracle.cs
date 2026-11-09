using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Hash-locked twenty-clip whitelist. Executes copied decoder
// arithmetic in its own x86 allocation. No game, graphics, sound or registry calls.
class ClassicShotPatOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protect);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protect,out uint old);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Decode(IntPtr input,IntPtr output);
 byte[] source;int pe;uint imageBase;IntPtr memory;
 const int Input=0x180000,Output=0x1c0000,Consumed=0x1ff000,Pixels=19200;
 static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
 int Offset(uint va){uint rva=va-imageBase;int table=pe+24+BitConverter.ToUInt16(source,pe+20);for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++){int at=table+i*40;uint start=BitConverter.ToUInt32(source,at+12),size=BitConverter.ToUInt32(source,at+16);if(rva>=start&&rva-start<size)return checked((int)(BitConverter.ToUInt32(source,at+20)+rva-start));}throw new InvalidDataException("VA outside raw sections");}
 byte[] Read(uint va,int count){byte[] b=new byte[count];Array.Copy(source,Offset(va),b,0,count);return b;}
 uint Address(int offset){return unchecked((uint)memory.ToInt32()+(uint)offset);}
 void Write(int offset,byte[] b){Marshal.Copy(b,0,IntPtr.Add(memory,offset),b.Length);}
 int Copy(uint va,int size){
  byte[] b=Read(va,size);int count=0;
  var globals=new HashSet<uint>(new uint[]{0x4517f0,0x4517f4,0x4517f8,0x4517fc,0x451804,0x451808,0x45185d,0x451865,0x451867,0x451869,0x45186d,0x451875,0x4540e1});
  for(int i=0;i<=b.Length-4;i++){uint value=BitConverter.ToUInt32(b,i);if(!globals.Contains(value))continue;BitConverter.GetBytes(Address((int)(value-0x400000))).CopyTo(b,i);i+=3;count++;}
  Write((int)(va-0x400000),b);return count;
 }
 void Table(uint va,int count,uint first,uint last){byte[] b=Read(va,count*4);for(int i=0;i<count;i++){uint target=BitConverter.ToUInt32(b,i*4);if(target<first||target>last)throw new InvalidDataException("Source dispatch pointer out of range");BitConverter.GetBytes(Address((int)(target-0x400000))).CopyTo(b,4*i);}Write((int)(va-0x400000),b);}
 byte[] Frame(Decode decode,byte[] frame){
  if(frame.Length>65536||frame[4]!=20||frame[5]!=15||frame[6]!=4||frame[7]!=4)throw new InvalidDataException("Oracle only accepts bounded 160x120 scale-four fixtures");
  Write(Input,frame);Marshal.WriteInt32(memory,Output-4,unchecked((int)0x5a3cc35a));Marshal.WriteInt32(memory,Output+Pixels,unchecked((int)0x5a3cc35a));
  decode(IntPtr.Add(memory,Input),IntPtr.Add(memory,Output));
  int consumed=unchecked((int)((uint)Marshal.ReadInt32(memory,Consumed)-Address(Input)));
  if(consumed!=frame.Length||Marshal.ReadInt32(memory,Output-4)!=unchecked((int)0x5a3cc35a)||Marshal.ReadInt32(memory,Output+Pixels)!=unchecked((int)0x5a3cc35a))throw new InvalidDataException("Original decoder length/canary differs: "+consumed+"/"+frame.Length);
  byte[] result=new byte[Pixels];Marshal.Copy(IntPtr.Add(memory,Output),result,0,result.Length);return result;
 }
 static readonly Dictionary<string,string> Trusted=new Dictionary<string,string>{
   {"BATFIRE1","F1D9162231109BC56D5E483C47FFE1B6EA53589BB9178594D905542FEB1EAC1F"},
   {"BATFIRE2","BEE55EFB57FCAF6643DB9E9167529EDBDB8CE2F36F8F0E7300EBE4EAEDE0BBD4"},
   {"BATFIRE3","EF6D183560B3E9D9CA1BE86EB3FB4B36A7EA35493F9779B9FE072BFF3B9C8195"},
   {"BATFIRE4","EBCD3B2B6955A06B1A10EBB5D58DE3BDC94EB5531106BB29DB34977550F57209"},
   {"F18FIRE1","745944644E9AD1875BD0ED2E9D037464F52108C8A111EF18D18A37F5905F5EFC"},
   {"F18FIRE2","41E08D80276B4D03A0894ED3B5C8ADFD20E7F07E3BDC5E2640F0D5667C0A8961"},
   {"SUBTORP1","139811B66098070723F07E54CB2CF3A5F98FC80197F7FF06BF999FF36963E195"},
   {"SUBTORP2","85C755F64A1BC3533591DD07AE370423721221B003AE479E28C4F4180B3EB527"},
   {"SUBMISS1","8647E21E31F5DB8E5FBE853337444EDF61B93186D7E840FFC50158F97A6FC7EA"},
   {"SUBMISS2","946C70BFBB8329FD73B1D0D98869F21A272B629A508316E43FA61AFAC3E184E2"},
   {"SML_LNC1","76E82CC03E88EFED06F0917968F794E4016E2A2E96E2CDA6F13EB7917513C47E"},
   {"SML_LNC2","8431F968EA026A03883A7C7CF549F10B1D5001A51874665B140B2BF6E249DB93"},
   {"TWOFIRE1","0DBC3970DE6566503EFB5958872F91A1589ECE76713B961AA78B81208230AAFC"},
   {"TWOFIRE2","059004FD10DC6C94E869BB82730B9CC9AF348C1DF443CD134DBBA09A22400C00"},
   {"FRG_FTRP","5A4B61537CFA72F0591EF50647E36DDE5924524EBD69F91E09085BB932E2B456"},
   {"BATT_LT1","EC1AE84FF65FE321EB0B5A46A26FD35272B2023D6BE08A805D5274458EC42905"},
   {"BATT_LT2","D2557E3798D945CCB5A623BABECD833C37C4E1092A9E052CE272FDA7031CD639"},
   {"FRG_LT2","4E3EB63A85D86C3DA7876400E7B3983378BABD94F4B4A8C27BE99DFC2D3A8242"},
   {"MEDFIRE1","FDEC0755F6A37050F5B6F170F79A8B63EE3D7EC4A6AFC82B062318A6A5C8514F"},
   {"MEDFIRE2","E263F67264F2E728CB7ECE8D06608537AA053B679EA08239E4CA031A16670A7D"},
   {"S_FLY1","8F0C0257531737BDF1B1BAEFCA00DCEBC6CEEFE3CD445EDBA01782F7A01C7819"},
   {"S_FLY2","3D6B560413C1D4220BEE166474606D7F3536E42E72CC1C7BA4981215E8CEFC8E"},
   {"S_FLY3","BA9B9587B71B041BD935DBFC4C6A6DBC923054FEF9A82176641CC932B364639C"},
   {"TORPRUN","CEFBB32D995889F80F558D3F1AF9787FF615D9203A1CE15E264648DFFADAD650"},
   {"MIS_FLY3","549BED3ACB6EFCEE7B49839A1A92C97FAE8D574ADB19F6BDBB4F71E4C21AEE69"},
   {"CRS_FLY1","BBA82659976C3BCCF81272FD599A216159C13FA55B4069194C1F2AEE97FD170F"},
   {"CL_MISS","D295D1E667A73ECDACC49C30A20F8E382D390597323ED4C9E426471E44A05BAA"},
   {"CL_HIT","9BB5899624486E19E3E23AAC50B328D4CF37A1D83A55F684444FFC773C92C258"}
 };
 void Run(string exe,string directory,string destination){
  source=File.ReadAllBytes(exe);
  if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new InvalidDataException("Expected matching Bs.exe and x86 oracle");
  pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);
  memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try {
   if(Copy(0x433920,0x10)!=0||Copy(0x433aa5,0xd8)!=22||Copy(0x433d41,0xf4f)!=34||Copy(0x437ebd,0xde)!=4)throw new InvalidDataException("Decoder relocations differ");
   Table(0x451875,13,0x433d41,0x437f03);Table(0x4540e1,256,0x434078,0x434c8e);
   if(BitConverter.ToString(Read(0x433b1d,7))!="61-A1-5D-18-45-00-C3")throw new InvalidDataException("Decoder exit differs");
   var jump=new List<byte>();jump.Add(0xe9);jump.AddRange(BitConverter.GetBytes(unchecked(0x433900-0x433b22)));Write(0x33b1d,jump.ToArray());
   var exit=new List<byte>(new byte[]{0x89,0x35});exit.AddRange(BitConverter.GetBytes(Address(Consumed)));exit.AddRange(new byte[]{0x61,0xc3});Write(0x33900,exit.ToArray());
   foreach(int page in new[]{0x33000,0x34000,0x37000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");}
   var decode=(Decode)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x33920),typeof(Decode));
   var rows=new List<string>{"Clip,Frame,Offset,PayloadBytes,SHA256"};int clips=0,total=0;
   foreach(var item in Trusted){
    byte[] movie=File.ReadAllBytes(Path.Combine(directory,item.Key+".PAT"));if(Hash(movie)!=item.Value)throw new InvalidDataException("Untrusted PAT: "+item.Key);
    Write(Output,new byte[Pixels]);int offset=0,index=0;
    while(offset<=movie.Length-4){uint n=BitConverter.ToUInt32(movie,offset);if(n==uint.MaxValue)break;if(n<4||n>65532||n>movie.Length-offset-4||index>=512)throw new InvalidDataException("Trusted framing differs");byte[] frame=new byte[n+4];Array.Copy(movie,offset,frame,0,frame.Length);byte[] result=Frame(decode,frame);
     if(index==0){byte[] dirty=new byte[Pixels];for(int i=0;i<dirty.Length;i++)dirty[i]=0xa7;Write(Output,dirty);if(Hash(Frame(decode,frame))!=Hash(result))throw new InvalidDataException("First frame depends on previous surface: "+item.Key);}
     rows.Add(item.Key+","+index+","+offset+","+n+","+Hash(result));index++;offset+=frame.Length;
    }
    if(index==0||offset!=movie.Length-4)throw new InvalidDataException("Trusted terminal differs");clips++;total+=index;
   }
   Directory.CreateDirectory(destination);File.WriteAllLines(Path.Combine(destination,"Classic-Shot-Frames-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured "+total+" original-decoder frame hashes for "+clips+" trusted firing clips, with exact consumed lengths and independent first frames.");
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 } static int Main(string[] args){if(args.Length!=3){Console.Error.WriteLine("ClassicShotPatOracle <matching Bs.exe> <LWEBS directory> <destination>");return 2;}try{new ClassicShotPatOracle().Run(args[0],args[1],args[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}