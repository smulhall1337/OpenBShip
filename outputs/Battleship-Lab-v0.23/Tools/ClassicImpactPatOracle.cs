using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Hash-locked 61-clip whitelist. Executes copied decoder
// arithmetic in its own x86 allocation. No game, graphics, sound or registry calls.
class ClassicImpactPatOracle {
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
 {"CL_HIT","9BB5899624486E19E3E23AAC50B328D4CF37A1D83A55F684444FFC773C92C258"},
 {"CAR_UMH","D70DE2A9EEE1F8863215C5D0C75AF339DB51202AD2E0A2851DD7D8E187AB4BD3"},
 {"CAR_USH","439FDEF724BEC64801ABED197B96330E2BA36843E2A9A70C689F636312E56D89"},
 {"BATT_UMH","28223AFD8C3B762ABB0F0E03CC5B7E520AE18049F3E8F02A2F8EB0263F6D69A1"},
 {"BATT_UTH","48783EF658EA944CE15A7189BCF44CD39FF9461E178919B44E690E197F26DF77"},
 {"BATT_USH","F26719FE014B1F49ADC078B080C7F1A7C422BBEF94AF108532399A16EC8752AB"},
 {"BATT_DMH","A848A8D920833E955F5ACF2AA61FACD3F0C09C340AA42B83698F333795502132"},
 {"BATTSINK","9C3AC938B257634C2BFA1B92551771B656F9CA0E525F1489CA3326CB8904F3EE"},
 {"SUB_USH","DC0FF20A605542CDA1AC868AE037447CE87D2DE513FBDF7BF530FFE8EA2BB963"},
 {"FRG_UMH","3253A65EC3295BD1AD25162BF734DB5D2D6DE69E6CCC162BEF03CDD489C2FE6C"},
 {"FRG_USH","3F70E2235B33A3FBAF6E5F74DFEBD9ACEB2F75AE68C52C506D00D2E57E89210D"},
 {"TORPMISA","4E1BE7AF68827002C16CEAE70837E10E205583C5A961686B0187924F007F55EF"},
 {"TORPMISB","008433BB9A9CE3DB00E82503B55EE5302C54275401ED7E40BCF15128531CDE89"},
 {"TORPMISC","7400D6716EA47DDF7AEC26FF426C3C8C6E259BA31EFDB42DB0F5EF93B9372542"},
 {"SHELMISA","D7AAFF2A2C36BA873EDC057F9BD10E98BC9F182C10D0D0953F3D7A2791D26B30"},
 {"SHELMISB","33B5C4502BBC3CEFB11A63A62AB242BD6B1661AE16160EB7EE282540220ABF77"},
 {"CRUISDIE","F20D5C1751615EF31E8F1419D197846518BE18D085230D7D8DD883C2E2A6072F"},
 {"BATT_DTH","6F0F6F7F334D2AAECC9A565E012B60B2E1FA82861073DF14167FAE2C0B2E2EA9"},
 {"FRG_UTH","6456501B419699E93B8B8BB412C8A942EA4BBEB7CC8659C43AC46719BED19F68"},
 {"BATT_DSH","0EEFF2C8ED0C7759E91C06A9A2F497CF0D05720BC1648406A1D67C4F209A1598"},
 {"CAR_UTH","4BED70DCE8A923053161FC29C99EB7847707840E06CB65DBB0084082246FC907"},
 {"SHELMISC","E7CA200B9C1081AF0C731A5B845FAFF27A070E3C6C800296DBF4E8B1BB0EBA48"},
 {"SUB_UTH","D5F1357BF925C4C2335273ABD18D176E57F08E993FD0CEF99AF17E31CDD81F63"},
 {"FRIGSINK","3DC61B132F51F30F3244B77230C799E4176F962A4169FB80DEF10BC17E438E7D"},
 {"SUB_UMH","DCD4B95376FE29A1A360CE67FE1BBBB86D7FF2712903EDE33EC49127C1A2BCA0"},
 {"SUB_DIE","4FA736620F337BD2D5307BBE002ED954D75F3DCC60ADAD59C1CFE26185F627A5"},
 {"CARRSINK","E9CD5EE990CC13951DFB432F38D90B5F6CECC72006A50EEA27BEBE8243C200B0"},
 {"CAR_DSH","40E762DED687418A05653C4B8C85C397E022258FE41CC79845FB0E6620C7B98F"},
 {"CAR_DTH","F20B13CF046284EAE075171B40B39A3324F45DF35324E93A76F1495B85A290A7"},
 {"CAR_DMH","1DA3A4D8253B41D3F1DFA63772BD23B1EBED5B70DF6A10813FD31DBC1F1A8EB6"},
 {"FRG_DTH","781EE1AA2AB5181682587D4B63292FB517D02F82ECF5E1764A5D2B107DF66FF9"},
 {"FRG_DMH","F11A380B8100558E9CFFAF8A1A2655617D0BF5DBC2955A31B786C31FB1BE3FD9"},
 {"FRG_DSH","8967335CB222C97E1927C0F16B703BFDC965A03A63F127CBEB79C7AA7A6A299F"},
 {"MIS_FLY2","1296AB04AA7870E983BD17D2C6DDE51655420C45E8C5FE8D8239D4CDC3D2D96C"}
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
   Directory.CreateDirectory(destination);File.WriteAllLines(Path.Combine(destination,"Classic-Impact-Frames-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured "+total+" original-decoder frame hashes for "+clips+" trusted firing clips, with exact consumed lengths and independent first frames.");
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 } static int Main(string[] args){if(args.Length!=3){Console.Error.WriteLine("ClassicImpactPatOracle <matching Bs.exe> <LWEBS directory> <destination>");return 2;}try{new ClassicImpactPatOracle().Run(args[0],args[1],args[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
