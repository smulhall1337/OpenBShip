using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Iced.Intel;

// Read-only PE32 analysis. Addresses are always tied to the input image base.
class PeDisasm {
    sealed class Section { public uint Rva,Raw,Size,VirtualSize; public string Name; }
    sealed class TextOutput : FormatterOutput {
        public readonly StringBuilder Text=new StringBuilder();
        public override void Write(string text,FormatterTextKind kind){Text.Append(text);}
    }
    byte[] bytes;uint imageBase;List<Section> sections=new List<Section>();
    Dictionary<uint,string> strings=new Dictionary<uint,string>(),imports=new Dictionary<uint,string>();
    List<Instruction> instructions=new List<Instruction>();
    ushort U16(int p){return BitConverter.ToUInt16(bytes,p);}
    uint U32(int p){return BitConverter.ToUInt32(bytes,p);}
    string Z(int p){int end=p;while(end<bytes.Length&&end-p<16384&&bytes[end]!=0)end++;return Encoding.ASCII.GetString(bytes,p,end-p);}
    int Offset(uint rva){foreach(var s in sections)if(rva>=s.Rva&&rva-s.Rva<s.Size)return checked((int)(s.Raw+rva-s.Rva));throw new InvalidDataException("RVA outside raw sections");}
    PeDisasm(string path){
        bytes=File.ReadAllBytes(path);if(bytes.Length<64)throw new InvalidDataException("Truncated DOS header");int pe=checked((int)U32(60));
        if(pe<0||pe>bytes.Length-248||U32(pe)!=0x4550||U16(pe+24)!=0x10b)throw new InvalidDataException("Expected PE32");
        int opt=pe+24;imageBase=U32(opt+28);int count=U16(pe+6),table=opt+U16(pe+20);
        if(count>128||table>bytes.Length-count*40)throw new InvalidDataException("Section table bounds");
        for(int i=0;i<count;i++){int p=table+i*40;var s=new Section{Name=Encoding.ASCII.GetString(bytes,p,8).Trim('\0'),VirtualSize=U32(p+8),Rva=U32(p+12),Size=U32(p+16),Raw=U32(p+20)};if((ulong)s.Raw+s.Size>(ulong)bytes.Length)throw new InvalidDataException("Raw section bounds");sections.Add(s);}
        foreach(var s in sections){if(s.Name==".rdata"||s.Name==".data"){int start=(int)s.Raw,end=(int)(s.Raw+s.Size);for(int p=start;p<end;){int at=p;while(p<end&&bytes[p]>=32&&bytes[p]<=126)p++;if(p-at>=4)strings[imageBase+s.Rva+(uint)(at-start)]=Encoding.ASCII.GetString(bytes,at,p-at);p++;}}}
        uint irva=U32(opt+104);if(irva!=0){int d=Offset(irva);while(U32(d+12)!=0){string dll=Z(Offset(U32(d+12)));uint lookup=U32(d);if(lookup==0)lookup=U32(d+16);uint iat=U32(d+16);int t=Offset(lookup);for(uint n=0;U32(t)!=0;n++,t+=4){uint v=U32(t);string name=(v&0x80000000)!=0?"ordinal:"+(v&65535):Z(Offset(v)+2);imports[imageBase+iat+n*4]=dll+"!"+name;}d+=20;}}
        foreach(var s in sections)if(s.Name==".text"){
            byte[] code=new byte[Math.Min(s.Size,s.VirtualSize)];Array.Copy(bytes,s.Raw,code,0,code.Length);
            var decoder=Iced.Intel.Decoder.Create(32,new ByteArrayCodeReader(code));decoder.IP=imageBase+s.Rva;ulong end=decoder.IP+(uint)code.Length;
            while(decoder.IP<end){Instruction instruction;decoder.Decode(out instruction);if(instruction.Length==0)break;instructions.Add(instruction);}
        }
    }
    bool References(Instruction i,uint target){
        if(i.MemoryBase==Register.None&&i.MemoryIndex==Register.None&&i.MemoryDisplacement64==target)return true;
        for(int op=0;op<i.OpCount;op++){var kind=i.GetOpKind(op);if(kind==OpKind.Immediate32&&i.Immediate32==target)return true;if(kind==OpKind.NearBranch32&&i.NearBranch32==target)return true;}return false;
    }
    string Line(Instruction instruction){
        var formatter=new IntelFormatter();var output=new TextOutput();
        // Framework's older compiler cannot bind the library's C# 'in' parameter.
        var method=typeof(IntelFormatter).GetMethod("Format",new[]{typeof(Instruction).MakeByRefType(),typeof(FormatterOutput)});
        method.Invoke(formatter,new object[]{instruction,output});var notes=new List<string>();
        for(int op=0;op<instruction.OpCount;op++)if(instruction.GetOpKind(op)==OpKind.Immediate32){string s;if(strings.TryGetValue(instruction.Immediate32,out s))notes.Add('"'+s+'"');}
        string imported;if(instruction.MemoryBase==Register.None&&imports.TryGetValue((uint)instruction.MemoryDisplacement64,out imported))notes.Add(imported);
        return instruction.IP.ToString("X8")+"  "+output.Text+(notes.Count==0?"":" ; "+string.Join(" | ",notes.ToArray()));
    }
    static uint Hex(string s){return Convert.ToUInt32(s.StartsWith("0x")?s.Substring(2):s,16);}
    void Range(uint start,uint end){
        if(end<=start||end-start>0x100000)throw new InvalidDataException("Invalid disassembly range");
        int raw=Offset(start-imageBase),length=checked((int)(end-start));if(raw>bytes.Length-length)throw new InvalidDataException("Range exceeds file");
        byte[] code=new byte[length];Array.Copy(bytes,raw,code,0,length);var decoder=Iced.Intel.Decoder.Create(32,new ByteArrayCodeReader(code));decoder.IP=start;
        while(decoder.IP<end){Instruction i;decoder.Decode(out i);if(i.Length==0)break;Console.WriteLine(Line(i));}
    }
    static int Main(string[] args){try{
        if(args.Length<2){Console.Error.WriteLine("PeDisasm <PE32> dump <file> | range <start> <end> | xref <VA> | strings");return 2;}
        var p=new PeDisasm(args[0]);if(args[1]=="strings"){foreach(var item in p.strings)Console.WriteLine(item.Key.ToString("X8")+" "+item.Value);return 0;}
        if(args[1]=="dump"){using(var w=new StreamWriter(args[2]))foreach(var i in p.instructions)w.WriteLine(p.Line(i));return 0;}
        if(args[1]=="xref"){uint target=Hex(args[2]);foreach(var i in p.instructions)if(p.References(i,target))Console.WriteLine(p.Line(i));return 0;}
        if(args[1]=="range"){p.Range(Hex(args[2]),Hex(args[3]));return 0;}
        return 2;
    }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
