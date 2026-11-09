using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Hash-locked 61-clip whitelist. Executes copied decoder
// arithmetic in its own x86 allocation. No game, graphics, sound or registry calls.
class ClassicHighPatOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protect);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protect,out uint old);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Decode(IntPtr input,IntPtr output);
 byte[] source;int pe;uint imageBase;IntPtr memory;
 const int Input=0x180000,Output=0x1c0000,Consumed=0x1ff000,Pixels=76800;
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
  if(frame.Length>65536||frame[4]!=40||frame[5]!=30||frame[6]!=2||frame[7]!=2)throw new InvalidDataException("Oracle only accepts bounded 320x240 scale-two fixtures");
  Write(Input,frame);Marshal.WriteInt32(memory,Output-4,unchecked((int)0x5a3cc35a));Marshal.WriteInt32(memory,Output+Pixels,unchecked((int)0x5a3cc35a));
  decode(IntPtr.Add(memory,Input),IntPtr.Add(memory,Output));
  int consumed=unchecked((int)((uint)Marshal.ReadInt32(memory,Consumed)-Address(Input)));
  if(consumed!=frame.Length||Marshal.ReadInt32(memory,Output-4)!=unchecked((int)0x5a3cc35a)||Marshal.ReadInt32(memory,Output+Pixels)!=unchecked((int)0x5a3cc35a))throw new InvalidDataException("Original decoder length/canary differs: "+consumed+"/"+frame.Length);
  byte[] result=new byte[Pixels];Marshal.Copy(IntPtr.Add(memory,Output),result,0,result.Length);return result;
 }
 static readonly Dictionary<string,string> Trusted=new Dictionary<string,string>{
 {"BATFIRE1","81866E8056616B7A9C3636E493700495EDD9156257DFBFB7FAF1A819F25B5680"},
 {"BATFIRE2","D5368A202D5724252E9231FE1812F887E2D497CB54BE38549083379B732AEF7A"},
 {"BATFIRE3","32368BE69D8CFA71A81C568F7673BB2A7F216651FE58545E20F4ADB015F8ACE1"},
 {"BATFIRE4","DB918F25A5861C81E072C7C74F877A3F5464C912C25D6E6EC82A920CCFD517CE"},
 {"F18FIRE1","8C26E29130BE28CB1C16830FC9404F73A451BD0DD7578DBB5F8A8A656DEE7BC2"},
 {"F18FIRE2","8021A9D3198CD50A6E227F9E33917678ACAFAA9F54D36508ED9F985BA01277A6"},
 {"SUBTORP1","B4E30E58C0705A56A912B2A4997FF1048C443506D901FB878A9D3BEABEE3CD5A"},
 {"SUBTORP2","9F1470DD8B8167067D35E058CF931D7078DCEE40A5E96FD8F41389ABE49DAE43"},
 {"SUBMISS1","CD668CCC302372442E5741548C4CCB95C43E7FC237DE44942C762CFCAFCACD35"},
 {"SUBMISS2","AF157F9E16D223B98112F05DD6343D9E247FFCE4242498E7938FDE909A34CD3D"},
 {"SML_LNC1","63A4DA544FFFEA6021DBAC8984CF0905C70248AB22EE61DA028B26F900C003E7"},
 {"SML_LNC2","E5EE9353F471341DE7B47F923DA178BB5E9009ECCDAF5172B5D94BE659738542"},
 {"TWOFIRE1","C36FBA0C440567A4362A3490C19741F299B7574681CFF8E7CFCA5E93D631D91A"},
 {"TWOFIRE2","065A6BE4B6E6D8F4EA1B8089BA9C5BA08D698A8637A5EB92F718C0576B264EAE"},
 {"FRG_FTRP","36771377DCE79CAB7E8E33C29DF7B08ABCF7ACFD25D271219094349BC8D6ECA6"},
 {"BATT_LT1","07B45915D56886D8AE8568EDBF6535CBAEFFF8A2DE56FFAE3478814BC96DD59C"},
 {"BATT_LT2","2B0674B56B2205D0379C2DE2C28828356F12DFD5619FE1ACCA898556E77AF0FB"},
 {"FRG_LT2","16A069511CCE1001AABF3CA07F3D9E68CC55AC6A225179B71462D57638780241"},
 {"MEDFIRE1","13F72E563E7155EFFF5AEAD278E9AE857D5F8A5E5DD1DD7E366CC6E9C430CBA9"},
 {"MEDFIRE2","C853EEA865DC98E205AF73AAE4354101404DA6EE47E25989A398FC313D6B510D"},
 {"S_FLY1","FB968AF00D4419012AB1C0DA95A1E66A879717F2D6C354C4934C033E64F67874"},
 {"S_FLY2","8FE26BD4BF92DDF94F24B8F0FF3DD6FFC79ED1AD216717AE15616B52481FDA66"},
 {"S_FLY3","1A3BD482671D4E01C7E4AD3E38A65F091D6E308A95DD960009DEE9C634BF7171"},
 {"TORPRUN","DA9A8ADE9D0FCE2AE221F85984F0492AEB66569E3A6B9B818B7CDBFD7153FAB4"},
 {"MIS_FLY3","B1C8B501FF5AC446CDCE51E8A4DEEACD0114ED704A2F58D2B0422FD49CD00D7F"},
 {"CRS_FLY1","40D12D2209C67AA24F4770428C1E87532859334A97E35EA509C61DDB30061AF9"},
 {"CL_MISS","83860E2DDCD2FC3E798A24E070C6FC0B9C6E7B200922600C8A06904265F42A34"},
 {"CL_HIT","00893FA8255A0F75418210FE04A76CD07D7A259EC08C2634EFEA52C2231ACB2A"},
 {"CAR_UMH","5DA47CAA4261CCC44FE8A01D5DCF78EF9689379FCDB3A6E41BD8DEA1A4B9D6FD"},
 {"CAR_USH","038A45C08B1860E28CF5069ED6A858386DF9C11DFAB4F6D6F9373B47D1B063DA"},
 {"BATT_UMH","31923E48924B8361A737372E9C6D33732CE9A151A5F5CB6CC176336067F37FC7"},
 {"BATT_UTH","29CDE9ED54C1A7846012A0C29814E3C4B0DEE4E12F215FD495C1F81453B8367D"},
 {"BATT_USH","C5086C31843706338B55EE487D7EA6DED4D61E50CD64A5D79BE4F00702EA6656"},
 {"BATT_DMH","027F765629FC9F82C1CBE472A5DD013007A38E5AF03258AE9C1A1BB89AB0D0EC"},
 {"BATTSINK","0DF2138DDC408FF378F32064604590A107228A852B5FC5FAF90D3F170A8054E1"},
 {"SUB_USH","BEDE0CAE4357672E79EF3526A8065B181E5688C72930B6162BFC88936C532D8A"},
 {"FRG_UMH","F1F188A0CCD335FC4CD5F021F14B9497A1739545533EDB552286A37DAF3837CD"},
 {"FRG_USH","73F5E0938715197717F227747CF457B49E415F579E986847F1D610AAF739AA27"},
 {"TORPMISA","801CE6BDE8428207E9C84A2EF836F2E6232BF3F85986131A9B29B02A95951B90"},
 {"TORPMISB","CC62847298EFA83CDC66804D638F9B773F5372A7CA191098845ECF01044B018E"},
 {"TORPMISC","EEE103952588C09C194E6CA0AD37629264CECA302FE34BDC76583E7B5458DBA1"},
 {"SHELMISA","6B983EF31A8DBA53E2DA1696D29FCA674AFC30ED22BB26FF2E07C98C88770CA8"},
 {"SHELMISB","0BE112C1FD0095ABD7EAEA55519AE63C43043A7DF17CE0E34A1436CF25A7EDB9"},
 {"CRUISDIE","A0D386ECB64071189DBB7A5AE4F6B5A93B39CAE5EDB6DDAE11EB160BCBC22FDF"},
 {"BATT_DTH","E100CBADF371C588E37DA50BFA700FAAB1DF9FCB43CC38025E91AC40D3357D22"},
 {"FRG_UTH","3FE149891226376E704230D984C4A7C3DB476C95E74ED803255A127FFCE3B4E7"},
 {"BATT_DSH","CB69E3CAF695161B652B743D5ACBDA0382E24C61FC891985F972C87CD4E8AB6C"},
 {"CAR_UTH","8B7253A9B82938EE4CE152AAF615D1D18B5AF19CF0DCCE4503906D6E0FCBC2FA"},
 {"SHELMISC","F529D0E39FB67824DF5CF05A362E6580B6692ACDAA99B906EF9EDEB76DABFD8B"},
 {"SUB_UTH","9FC2932AE86E6D63B5E687113FAADB1523F87A8BD7DA220F63882DA8D4F3ACF5"},
 {"FRIGSINK","FB9F64814EC71A1D5E5C3AE4AF10FD2B503E1EAD269704C9C9C60C176B8224E9"},
 {"SUB_UMH","AE2C6921C7F7C147C9E692510D42A5AAB0935F47A44333A7899693108A9CEC24"},
 {"SUB_DIE","682537AD3BF6ADA79F2579972015D0BEB67AD86D39E4931092ADB593AFF4CF57"},
 {"CARRSINK","DC8C19B52CB585AFE691400789603BDC1690BEC180E79A29AE013F1F8EB6B504"},
 {"CAR_DSH","725D379813A6455FDC8F72B36B75ABAC5E848CEFD538EA6CEF0F8B5FAC1F1D6D"},
 {"CAR_DTH","E024A116FEC3902E54E84638BD1D987FA52A88DD606253E27FBAA863FDFD888B"},
 {"CAR_DMH","7A233F8D07FEDA282E65C6805B36629966952B3085133D5514A4658533356FCD"},
 {"FRG_DTH","1F1FFE48DC445FD702BEA886D80470519D27F01B1DDEBA4D8CBB558367CC9AA6"},
 {"FRG_DMH","2CAE9AB7B449139022B63465E9643A7BA6D900A316880AA2F8EBA8FEBFDA939E"},
 {"FRG_DSH","C41540E3ECAF484AC54EBF8951B6C1E11575B314CCC4BBFC19333AAC692AEEE6"},
 {"MIS_FLY2","E54F3ED3FFC59653AC0897E4E88A168F9BC6A1BA01554ADE6BA04446EB1C6E9A"}
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
   Directory.CreateDirectory(destination);File.WriteAllLines(Path.Combine(destination,"Classic-High-Frames-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured "+total+" original-decoder frame hashes for "+clips+" trusted firing clips, with exact consumed lengths and independent first frames.");
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 } static int Main(string[] args){if(args.Length!=3){Console.Error.WriteLine("ClassicHighPatOracle <matching Bs.exe> <HWEBS directory> <destination>");return 2;}try{new ClassicHighPatOracle().Run(args[0],args[1],args[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}

