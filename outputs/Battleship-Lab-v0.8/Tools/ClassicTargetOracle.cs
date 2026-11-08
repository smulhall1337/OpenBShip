using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Collections.Generic;

// Executes ONLY copied numerical selector/RNG code in this new x86 process.
// Never loads/starts Bs.exe, attaches to a game, or writes its original files.
// Hash-locked source spans and explicitly enumerated relocations are required.
class ClassicTargetOracle {
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr size,uint allocation,uint protect);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualProtect(IntPtr p,UIntPtr size,uint protect,out uint old);
    [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr size,uint release);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Selector(IntPtr x,IntPtr y);
    byte[] source; int pe; uint imageBase;
    int Offset(uint va) {
        uint rva=va-imageBase;int table=pe+24+BitConverter.ToUInt16(source,pe+20);
        for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++) {
            int at=table+i*40; uint start=BitConverter.ToUInt32(source,at+12),size=BitConverter.ToUInt32(source,at+16);
            if(rva>=start&&rva-start<size)return checked((int)(BitConverter.ToUInt32(source,at+20)+rva-start));
        }
        throw new InvalidDataException("Address outside source sections");
    }
    byte[] Read(uint va,int count) { var b=new byte[count];Array.Copy(source,Offset(va),b,0,count);return b; }
    static string Hash(byte[] b) {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
    static uint Address(IntPtr p,int offset) {return unchecked((uint)p.ToInt32()+(uint)offset);}
    static void Patch(byte[] b,int offset,uint expected,uint value) {
        if(BitConverter.ToUInt32(b,offset)!=expected)throw new InvalidDataException("Relocation did not match source operand");
        BitConverter.GetBytes(value).CopyTo(b,offset);
    }
    ClassicTargetOracle(string exe) {
        source=File.ReadAllBytes(exe);
        if(Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new InvalidDataException("Executable identity differs");
        pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);
        if(IntPtr.Size!=4)throw new InvalidOperationException("Oracle must run as x86");
    }
    string Evaluate(string name,int[] grid,uint seed,int level) {
        IntPtr code=VirtualAlloc(IntPtr.Zero,new UIntPtr(4096),0x3000,4),data=VirtualAlloc(IntPtr.Zero,new UIntPtr(4096),0x3000,4);
        if(code==IntPtr.Zero||data==IntPtr.Zero)throw new Exception("Oracle allocation failed");
        try {
            const uint start=0x41C940;const int rngOffset=0x400;
            byte[] selector=Read(start,0x348),rng=Read(0x439790,0x2A);
            Patch(selector,3,0x4E42D0,Address(data,0));
            Patch(selector,0x24,0x4B498C,Address(data,64+492));
            Patch(selector,0x13E,0x4FBDF4,Address(data,4));
            Patch(selector,0x14E,0x41CC60,Address(code,0x320));
            Patch(selector,0x266,0x41CC70,Address(code,0x330));
            foreach(uint call in new uint[]{0x41CA94,0x41CB4B,0x41CB5B,0x41CB8D}) {
                int offset=(int)(call-start);
                if(selector[offset]!=0xE8)throw new InvalidDataException("Expected RNG call");
                uint expected=unchecked(0x439790-(call+5));
                uint relative=unchecked(Address(code,rngOffset)-Address(code,offset+5));
                Patch(selector,offset+1,expected,relative);
            }
            for(int i=0;i<10;i++) {
                int at=0x320+i*4;uint target=BitConverter.ToUInt32(selector,at);
                if(target<start||target>=0x41CC5E)throw new InvalidDataException("Invalid original jump-table destination");
                Patch(selector,at,target,Address(code,(int)(target-start)));
            }
            Patch(rng,1,0x454D50,Address(data,8));
            Patch(rng,0x1D,0x454D50,Address(data,8));
            Marshal.Copy(selector,0,code,selector.Length);
            Marshal.Copy(rng,0,IntPtr.Add(code,rngOffset),rng.Length);
            Marshal.WriteInt16(data,0,1);Marshal.WriteInt32(data,4,level);Marshal.WriteInt32(data,8,unchecked((int)seed));
            Marshal.WriteInt16(data,12,-1);Marshal.WriteInt16(data,14,-1);
            Marshal.Copy(grid,0,IntPtr.Add(data,128),100);
            uint old;if(!VirtualProtect(code,new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");
            var select=(Selector)Marshal.GetDelegateForFunctionPointer(code,typeof(Selector));
            select(IntPtr.Add(data,12),IntPtr.Add(data,14));
            string cells=string.Join(";",Array.ConvertAll(grid,delegate(int v){return v.ToString();}));
            return name+","+level+","+seed+","+Marshal.ReadInt16(data,12)+","+Marshal.ReadInt16(data,14)+","+unchecked((uint)Marshal.ReadInt32(data,8))+",\""+cells+"\"";
        }
        finally {VirtualFree(code,UIntPtr.Zero,0x8000);VirtualFree(data,UIntPtr.Zero,0x8000);}
    }
    static int[] Grid(params int[] triples) {var b=new int[100];for(int i=0;i<triples.Length;i+=3)b[triples[i]*10+triples[i+1]]=triples[i+2];return b;}
    // Used only to construct branch-specific INPUTS. Expected outputs still
    // come from executing the original relocated selector and RNG bytes.
    static int InputRoll(ref uint state) {state=unchecked(state*214013u+2531011u);return (int)((state>>16)&32767);}
    static int Main(string[] args) {
        if(args.Length!=2){Console.Error.WriteLine("ClassicTargetOracle <matching Bs.exe> <output CSV>");return 2;}
        try {
            var oracle=new ClassicTargetOracle(args[0]);var rows=new List<string>{"Case,Level,Seed,X,Y,FinalRandomState,CellsXMajor"};
            for(int level=0;level<4;level++)for(uint seed=1;seed<=16;seed++)rows.Add(oracle.Evaluate("empty",Grid(),seed,level));
            for(uint seed=1;seed<=16;seed++)rows.Add(oracle.Evaluate("isolated",Grid(5,5,8),seed,0));
            rows.Add(oracle.Evaluate("chain-negative",Grid(6,4,8,6,5,8),1,0));
            rows.Add(oracle.Evaluate("chain-positive",Grid(6,3,1,6,4,8,6,5,8),1,0));
            rows.Add(oracle.Evaluate("edge-low",Grid(2,0,8,2,1,8),1,0));
            rows.Add(oracle.Evaluate("edge-high",Grid(2,8,8,2,9,8),1,0));
            rows.Add(oracle.Evaluate("scan-order",Grid(2,4,8,2,5,8,1,6,8,1,7,8),1,0));
            rows.Add(oracle.Evaluate("blocked-chain",Grid(6,3,1,6,4,8,6,5,8,6,6,1),1,0));
            for(int level=0;level<4;level++)for(uint seed=1;seed<=4;seed++) {
                int[] sparse=new int[100];for(int i=0;i<100;i++)sparse[i]=1;sparse[25]=0;
                rows.Add(oracle.Evaluate("sparse",sparse,seed,level));
            }
            rows.Add(oracle.Evaluate("mask",Grid(1,1,2,2,2,4),17,2));
            for(int pattern=0;pattern<6;pattern++)for(uint seed=1;seed<10000;seed++) {
                uint state=seed;int x=InputRoll(ref state)%10,y=InputRoll(ref state)%8+1,which=InputRoll(ref state)%6;
                if(which!=pattern||x<1||x>8||y<3||y>6)continue;
                int[] grid=Grid();
                switch(pattern) {
                    case 0:grid[x*10+y-2]=1;break;
                    case 1:grid[x*10+y+2]=1;break;
                    case 2:grid[(x-1)*10+y-1]=1;break;
                    case 3:grid[(x-1)*10+y+1]=1;break;
                    case 4:grid[(x+1)*10+y-1]=1;break;
                    case 5:grid[(x+1)*10+y+1]=1;break;
                }
                rows.Add(oracle.Evaluate("pattern-"+pattern,grid,seed,0));break;
            }
            rows.Add(oracle.Evaluate("isolated-low-edge",Grid(3,0,8),1,0));
            rows.Add(oracle.Evaluate("isolated-high-edge",Grid(3,9,8),1,0));
            rows.Add(oracle.Evaluate("isolated-direction-retry",Grid(3,5,8,3,6,1),1,0));
            rows.Add(oracle.Evaluate("isolated-other-side",Grid(3,5,8,3,4,1),2,0));
            File.WriteAllLines(args[1],rows.ToArray());
            Console.WriteLine("Original x86 numerical selector fixtures: "+(rows.Count-1));return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
