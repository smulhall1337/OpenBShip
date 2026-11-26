using System;using System.IO;using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeServiceDestinationChecks
    {
        // Original759 numeric XY outputs;yard IDs follow the uniquely placed
        // fixture yards. ID is a diagnostic result,not a source persistent field.
        static readonly int[][] Original={new[]{-1,305419896,1985229328},new[]{2,100,100},new[]{3,130,80},new[]{2,100,0},new[]{3,50,0},new[]{3,100,0},new[]{3,130,80},new[]{3,130,80},new[]{2,100,100},new[]{3,32766,-32768}};
        static void Need(bool ok,string t){if(!ok)throw new InvalidDataException(t);}static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static void I(byte[] b,int a,int v){BitConverter.GetBytes(v).CopyTo(b,a);}static void W(byte[] b,int a,int v){BitConverter.GetBytes(unchecked((short)v)).CopyTo(b,a);}
        static bool Reject(Action a){try{a();return false;}catch(ArgumentException){return true;}catch(InvalidDataException){return true;}catch(NotSupportedException){return true;}}
        static byte[][] Fixture(int kind,int profile,int owner)
        {
            var s=new byte[402000];var f=new byte[123000];var r=new byte[78560];for(int n=0;n<r.Length;n++)r[n]=(byte)(n*17+3);I(s,5*268,22);I(s,7*268,22);I(s,9*268,23);int a=16*492;I(f,a,kind);W(f,a+4,16);W(f,a+6,owner);I(f,a+472,0x11223344);f[a+485]=83;W(f,a+12,profile>=3?0:610);W(f,a+14,profile>=3?0:590);
            foreach(int yard in new[]{2,3}){W(f,yard*492+6,1);W(f,yard*492+12,yard==2?100:130);W(f,yard*492+14,yard==2?100:80);}Action<int,int> marker=delegate(int yard,int unit){I(f,yard*492,2);I(f,yard*492+16,0x4fc340+unit*268);};if(profile==1)marker(2,5);if(profile==2)marker(3,9);if(profile>=3){marker(2,5);marker(3,9);}
            if(profile==3){W(f,2*492+12,100);W(f,2*492+14,0);W(f,3*492+12,0);W(f,3*492+14,100);}if(profile==4){W(f,2*492+12,200);W(f,2*492+14,0);W(f,3*492+12,50);W(f,3*492+14,0);}if(profile==5){W(f,2*492+6,0);W(f,2*492+12,10);W(f,2*492+14,0);W(f,3*492+12,100);W(f,3*492+14,0);}if(profile==6)Array.Clear(f,2*492+16,24);if(profile==7)I(f,2*492,1);if(profile==8)I(f,2*492+20,0x4fc340+7*268);if(profile==9){W(f,a+12,-32768);W(f,a+14,-32768);W(f,2*492+12,32767);W(f,2*492+14,32767);W(f,3*492+12,32766);W(f,3*492+14,-32768);}
            I(r,16*312+204,0x4b47a0+a);I(r,16*312+200,4);I(r,16*312+8,0x12345678);I(r,16*312+12,0x76543210);return new[]{s,f,r};
        }
        static byte[] Read(BinaryReader r,int n){var b=r.ReadBytes(n);Need(b.Length==n,"759 truncated");return b;}static byte[] Patch(BinaryReader r,byte[] basis){var b=(byte[])basis.Clone();int n=r.ReadInt32(),last=-1;Need(n>=0&&n<=b.Length,"759 delta count");for(int x=0;x<n;x++){int a=r.ReadInt32();Need(a>last&&a<b.Length,"759 delta bounds");last=a;b[a]=r.ReadByte();}return b;}
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            check("Service destination matches original759 yard eligibility tie and signed-distance outputs",delegate
            {
                foreach(int kind in new[]{1,9})foreach(int owner in new[]{0,1})for(int p=0;p<10;p++){var d=Fixture(kind,p,owner);var b=new PracticeAiSharedBacking(d[2]);var result=PracticeServiceDestination.Evaluate(d[0],d[1],b,16,1);var f=(byte[])d[1].Clone();f[16*492+485]=1;var r=(byte[])d[2].Clone();I(r,16*312+8,Original[p][1]);I(r,16*312+12,Original[p][2]);Need(result.SelectedYard==Original[p][0]&&Hash(result.CopyFleetPool())==Hash(f)&&Hash(result.Backing.CopyRegion())==Hash(r)&&(result.Outcome==PracticeServiceDestinationOutcome.DestinationChosen)==(p!=0),"759 numeric destination profile "+p);}
            });
            check("Service destination early-kind outcomes and result snapshots preserve opaque state",delegate
            {
                foreach(int kind in new[]{0,2,3,4,5,6,7,8,10,11,12,25}){var d=Fixture(kind,4,1);var b=new PracticeAiSharedBacking(d[2]);string rh=Hash(b.CopyRegion());var result=PracticeServiceDestination.Evaluate(d[0],d[1],b,16,1);var f=(byte[])d[1].Clone();if(kind!=2&&kind!=11)I(f,16*492+472,1);Need(result.SelectedYard==-1&&Hash(result.CopyFleetPool())==Hash(f)&&Hash(result.Backing.CopyRegion())==rh,"759 kind outcome "+kind);string fh=Hash(result.CopyFleetPool());d[1][0]^=255;d[2][0]^=255;var copy=result.CopyFleetPool();copy[0]^=255;copy=result.Backing.CopyRegion();copy[copy.Length-1]^=255;Need(Hash(result.CopyFleetPool())==fh&&Hash(result.Backing.CopyRegion())==rh,"Service destination result aliased inputs/accessors");}
            });
            check("Service destination rejects late invalid yard references atomically and ignores unreached ones",delegate
            {
                var d=Fixture(1,1,1);var b=new PracticeAiSharedBacking(d[2]);string fh=Hash(d[1]),rh=Hash(b.CopyRegion()),sh=Hash(d[0]);var bad=(byte[])d[1].Clone();I(bad,249*492,2);W(bad,249*492+6,1);I(bad,249*492+16,0x4fc341);Need(Reject(delegate{PracticeServiceDestination.Evaluate(d[0],bad,b,16,1);})&&Hash(d[1])==fh&&Hash(b.CopyRegion())==rh&&Hash(d[0])==sh,"Late marker error published earlier choice");W(bad,249*492+6,0);Need(PracticeServiceDestination.Evaluate(d[0],bad,b,16,1).SelectedYard==2,"Wrong-owner pointer was dereferenced");
                var task=b.CopyTaskRecord(16);I(task,204,0x4b47a1);var invalid=b.WithTaskRecord(16,task);var unknown=(byte[])d[1].Clone();I(unknown,16*492,13);Need(Reject(delegate{PracticeServiceDestination.Evaluate(d[0],d[1],invalid,16,1);})&&Reject(delegate{PracticeServiceDestination.Evaluate(d[0],unknown,b,16,1);})&&Reject(delegate{PracticeServiceDestination.Evaluate(d[0],d[1],b,16,0);})&&Reject(delegate{PracticeServiceDestination.Evaluate(new byte[1],d[1],b,16,1);})&&Reject(delegate{PracticeServiceDestination.Evaluate(d[0],new byte[1],b,16,1);})&&Reject(delegate{PracticeServiceDestination.Evaluate(d[0],d[1],null,16,1);})&&Reject(delegate{PracticeServiceDestination.Evaluate(d[0],d[1],b,250,1);}),"Unsupported destination input admitted");
            });
            evidence("Service destination matches280 original759 complete fleet and shared-backing outputs",delegate
            {
                string path=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Yard-Destination-759.bin");Need(Hash(File.ReadAllBytes(path))=="A7FD995545F5730A618BBA1A60492A641636F0AC9C3289069B76EE530ADCFC08","759 capture identity");int count=0;
                using(var r=new BinaryReader(File.OpenRead(path))){Need(System.Text.Encoding.ASCII.GetString(Read(r,8))=="NY759001"&&r.ReadInt32()==1&&r.ReadInt32()==280,"759 header");var s=Read(r,402000);var f=Read(r,123000);Read(r,78000);var region=Read(r,78560);Read(r,64000);Read(r,1024000);for(;;){int id=r.ReadInt32();if(id==-1)break;int kind=r.ReadInt32(),profile=r.ReadInt32(),owner=r.ReadInt32();var beforeF=Patch(r,f);var beforeR=Patch(r,region);var afterF=Patch(r,beforeF);var afterR=Patch(r,beforeR);Need(id==count&&profile>=0&&profile<10&&BitConverter.ToInt32(beforeF,16*492)==kind&&BitConverter.ToInt16(beforeF,16*492+6)==owner,"759 metadata");var result=PracticeServiceDestination.Evaluate(s,beforeF,new PracticeAiSharedBacking(beforeR),16,1);Need(Hash(result.CopyFleetPool())==Hash(afterF)&&Hash(result.Backing.CopyRegion())==Hash(afterR),"759 complete destination "+id);count++;}Need(count==280&&r.BaseStream.Position==r.BaseStream.Length,"759 extent");}
            });
        }
    }
}
