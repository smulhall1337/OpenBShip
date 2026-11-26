using System;
using System.IO;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeOwnerVisibilityChecks
    {
        // Complete pool hashes captured from the pinned original576 routine.
        static readonly string[] Original={
            "0,0,-1,0,-2,0,0,0,0,0,0,16,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,0,0,0,0,0",
            "1,1,-1,0,-2,0,0,0,0,0,0,16,B8FAACB28BF3863FFB58116FF3604332CE02570CB605DF5EA8DEEDEBA076FB84,1A79979AF13D66586D8AABA4703EA6B209D9FC81B3CC0F027049B160B9FEA401,0,0,0,11,0",
            "2,0,0,0,-2,0,0,0,0,0,0,16,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,CE23E36DC03C412A6A4EBD8C92F93A0BD34B7A41FF79B028E97F1EAE31A7A211,0,0,0,0,0",
            "3,1,0,0,-2,0,0,0,0,0,0,16,B8FAACB28BF3863FFB58116FF3604332CE02570CB605DF5EA8DEEDEBA076FB84,307F0170D19BF2BFD77C9BA30CF985D3721053272C715CBDEA152881FF07B2FD,0,0,0,11,3",
            "4,0,1,0,-2,0,0,0,0,0,0,16,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,E6E8705E559EDE4B439F04F143B108D35ECFEE0AC022579BB965035EDA14191A,0,1,0,0,0",
            "5,1,1,0,-2,0,0,0,0,0,0,16,B8FAACB28BF3863FFB58116FF3604332CE02570CB605DF5EA8DEEDEBA076FB84,B97C732B6E8517E7DA1E5D9AFCB6F6972C59DAF03D81C1F38F928115E716CE2C,0,1,0,11,2",
            "6,0,2,0,-2,0,0,0,0,0,0,16,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,0,0,0,0,0",
            "7,1,2,0,-2,0,0,0,0,0,0,16,B8FAACB28BF3863FFB58116FF3604332CE02570CB605DF5EA8DEEDEBA076FB84,85F79F34036D2385A6599619F76323967BFE5AC6F364AE13240261BDFF436512,0,0,0,11,0",
            "8,0,3,0,-2,0,0,0,0,0,0,16,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,0,0,0,0,0",
            "9,1,3,0,-2,0,0,0,0,0,0,16,B8FAACB28BF3863FFB58116FF3604332CE02570CB605DF5EA8DEEDEBA076FB84,5B5419AFEB06732D1A22F2068399A99311CF8106214E78D7B459015A48A403B0,0,0,0,11,0",
            "2063,2,0,0,-2,0,1,1,11,0,0,0,D0C47A94F129C65A92AE4924179DF952D293C3B5B30EAA92298E99C3207D99DE,8BDAC908423CA8E859271042B1907A02AEF91D40738906AD6159EDD65721B888,0,0,0,0,0",
            "2068,2,0,0,-2,0,1,1,30,0,0,0,CB6D48F2BA9639857DC78B5C8AF7DACAD8DC2AAC3597391C7A1D22354E47714A,6398BF8DF229C8188DCA9F535C3D353791977083048DA72C51AD5E82B8F562AD,0,0,0,0,0",
            "2073,2,0,0,-2,0,1,1,31,0,0,0,CDC7FA40331D8B0C4B5C406CEBA666D3CBD8B0DA5267FD0E670E26ABDE58C05D,6B97B7CAE6A1F2C440E40FED0B4A96C8AE67FF6AB340FBB65F379BDA25390FEA,0,0,0,0,0",
            "9161,2,0,2,0,0,1,3,20,0,1,249,8A4EAD23D7428D4523AEF6B2CA1F0B534ACAF6C4FF0DEECD1E8B2082530A920C,8A4EAD23D7428D4523AEF6B2CA1F0B534ACAF6C4FF0DEECD1E8B2082530A920C,2,3,1,0,3",
            "9164,2,0,2,0,0,1,3,21,0,1,249,EE8FC49692C184F7180355061D6AA8BA9F55CA32504CBEEF8BB4DEC82D8470A3,EE8FC49692C184F7180355061D6AA8BA9F55CA32504CBEEF8BB4DEC82D8470A3,2,3,1,0,3",
            "9167,2,0,2,0,0,1,3,500,0,1,249,B685E8ACB47A3A630313BB29294FD2F132667328E6F5CDF50A38EA8D19B89511,B685E8ACB47A3A630313BB29294FD2F132667328E6F5CDF50A38EA8D19B89511,2,3,1,0,3",
            "9170,2,0,2,0,0,1,3,501,0,1,249,37A3253E324CB9680D424C00B2FB9113C1FCF38D04F920D7E1824524383FC3EF,37A3253E324CB9680D424C00B2FB9113C1FCF38D04F920D7E1824524383FC3EF,2,3,1,0,3",
            "9191,2,0,2,0,14,1,3,20,0,1,249,8A4EAD23D7428D4523AEF6B2CA1F0B534ACAF6C4FF0DEECD1E8B2082530A920C,84687CB19237399F0617382DD142D34B4E5E6D322A1331C1C33EBA7DF43583C0,0,0,0,0,0",
            "9194,2,0,2,0,14,1,3,21,0,1,249,EE8FC49692C184F7180355061D6AA8BA9F55CA32504CBEEF8BB4DEC82D8470A3,A17C9EDDA961F25B2DDFB7521641E386091EA19002907F23463D6DF2356D778E,0,0,0,0,0",
            "9197,2,0,2,0,14,1,3,500,0,1,249,B685E8ACB47A3A630313BB29294FD2F132667328E6F5CDF50A38EA8D19B89511,7A84379B138DB3E1B3B0FB2F91210C742A83EAB0E95E2AE2B421D1DB7306252A,0,0,0,0,0",
            "9200,2,0,2,0,14,1,3,501,0,1,249,37A3253E324CB9680D424C00B2FB9113C1FCF38D04F920D7E1824524383FC3EF,92B3780140DE9D37B611BE13B7F8E8229946C154D9096BCDB8525520C9E7C089,0,0,0,0,0",
            "9221,2,0,2,0,19,1,3,20,0,1,249,8A4EAD23D7428D4523AEF6B2CA1F0B534ACAF6C4FF0DEECD1E8B2082530A920C,645CC66F0846BFB324A15AAC4843F0E99FF0741B0CB9F0D76153AB46FA97A530,0,0,0,0,0",
            "9224,2,0,2,0,19,1,3,21,0,1,249,EE8FC49692C184F7180355061D6AA8BA9F55CA32504CBEEF8BB4DEC82D8470A3,699ECFEE908E94956F9CDD369B3A38A2464A5567DE2CD77C1D3451D4A61A1040,0,0,0,0,0",
            "9227,2,0,2,0,19,1,3,500,0,1,249,B685E8ACB47A3A630313BB29294FD2F132667328E6F5CDF50A38EA8D19B89511,F318AD8F9394414BA0F069D0D49A78D8BB87310580982DD995492D864B1EE1AA,0,0,0,0,0",
            "9230,2,0,2,0,19,1,3,501,0,1,249,37A3253E324CB9680D424C00B2FB9113C1FCF38D04F920D7E1824524383FC3EF,92B3780140DE9D37B611BE13B7F8E8229946C154D9096BCDB8525520C9E7C089,0,0,0,0,0",
        };
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static bool Reject(Action a){try{a();return false;}catch(ArgumentException){return true;}catch(InvalidDataException){return true;}catch(NotSupportedException){return true;}catch(InvalidOperationException){return true;}}
        static byte[] Read(BinaryReader r,int n){var b=r.ReadBytes(n);if(b.Length!=n)throw new EndOfStreamException();return b;}
        static byte[] Patch(BinaryReader r,byte[] basis){var b=(byte[])basis.Clone();int count=r.ReadInt32(),last=-1;Need(count>=0&&count<=b.Length,"576 delta length");for(int n=0;n<count;n++){int at=r.ReadInt32();Need(at>last&&at<b.Length,"576 delta offset/order");last=at;b[at]=r.ReadByte();}return b;}
        static PracticeOwnerVisibilityStudyState Fixture(PracticeOwnerVisibilityRules rules,PracticeMapStartup map,int[] p)
        {
            int preset=p[1]<2?(p[3]==0?p[1]:2):p[10]==0?3:4;
            return PracticeOwnerVisibilityStudyState.CreateWithRules(rules,map,preset,p[2],p[5],p[6],p[7],p[8],p[9]);
        }
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);var rules=PracticeOwnerVisibilityRules.Load(programs);
            Func<int,int,PracticeOwnerVisibilityStudyState> real=delegate(int preset,int owner){return PracticeOwnerVisibilityStudyState.CreateWithRules(rules,map,preset,owner,0,1,0,0,0);};
            check("Visibility producer matches original copied-map sensor and local-range golden pools",delegate
            {
                foreach(string line in Original){var r=line.Split(',');var p=new int[12];for(int n=0;n<12;n++)p[n]=int.Parse(r[n]);var c=Fixture(rules,map,p);Need(Hash(c.CopyFleetPool())==r[12]&&Hash(c.Execute().CopyFleetPool())==r[13],"Original576 complete pool case "+p[0]);}Need(Original.Length==25,"576 public golden count");
            });
            check("Copied owner1 natural visibility leaves fleet14 zero and produces fleet15 level1",delegate
            {
                var before=real(0,1);var after=before.Execute();Need(!before.Executed&&after.Executed&&!after.Skipped&&after.Fleet(14).Level==0&&after.Fleet(15).Level==1&&Hash(map.Stages[3].CopyFleetPool())==Hash(before.CopyInputFleetPool()),"Natural copied-map visibility");
            });
            check("Visibility local mode2 no selection returns before clearing every original byte",delegate
            {
                var c=real(2,1);var d=c.Execute();Need(d.Skipped&&d.ChangedCount==0&&Hash(c.CopyFleetPool())==Hash(d.CopyFleetPool())&&d.Fleet(14).Level==2&&d.Fleet(15).Level==3,"Early no-op must retain fixture tags");
                var result=rules.Apply(c.CopyFleetPool(),1,2,1,0,-999);Need(result.Skipped,"Ignored selected pointer dereferenced");
            });
            check("Visibility copied states preserve predecessors and all opaque record bytes",delegate
            {
                var c=real(1,1);string hash=Hash(c.CopyFleetPool());var d=c.Execute();var b=d.CopyFleetPool();b[0]^=255;var a=d.CopyInputFleetPool();a[0]^=255;Need(Hash(c.CopyFleetPool())==hash&&Hash(d.CopyInputFleetPool())==hash&&Hash(d.CopyFleetPool())!=Hash(b),"Mutable visibility snapshot");
                var old=c.CopyFleetPool();var next=d.CopyFleetPool();for(int n=0;n<old.Length;n++)if(old[n]!=next[n])Need(n%492==467||n%492==480||n%492==481,"Opaque fleet data mutation");
            });
            check("Visibility reset boundary and copied factory reject unsupported inputs atomically",delegate
            {
                var c=real(0,1);var done=c.Execute();Need(Reject(delegate{done.Execute();})&&Reject(delegate{PracticeOwnerVisibilityStudyState.CreateWithRules(rules,map,5,1,0,1,0,0,0);})&&Reject(delegate{PracticeOwnerVisibilityStudyState.CreateWithRules(rules,map,4,-1,14,1,3,20,0);})&&Reject(delegate{PracticeOwnerVisibilityStudyState.CreateWithRules(rules,map,3,1,0,1,0,9,0);})&&Reject(delegate{rules.Apply(new byte[10],1,0,0,0,0);})&&Reject(delegate{rules.Apply(c.CopyFleetPool(),4,0,0,0,0);})&&Reject(delegate{rules.Apply(c.CopyFleetPool(),1,1,0,0,0);})&&Reject(delegate{rules.Apply(c.CopyFleetPool(),1,2,1,35,16);}),"Invalid visibility inputs admitted");
                var bad=c.CopyFleetPool();BitConverter.GetBytes(99).CopyTo(bad,16*492);string hash=Hash(bad);Need(Reject(delegate{rules.Apply(bad,1,0,0,0,0);})&&Hash(bad)==hash,"Unsupported observer mutated input");
            });
            evidence("Visibility producer matches all9638 original cases and9610 copied factory inputs",delegate
            {
                string path=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Owner-Visibility-576.bin");Need(Hash(File.ReadAllBytes(path))=="65222B64A558D2A12547431107240331F2E51949ACD164A7BE0D8D4507212617","576 binary identity");
                int count=0,factories=0,skipped=0;using(var r=new BinaryReader(File.OpenRead(path)))
                {
                    Need(System.Text.Encoding.ASCII.GetString(Read(r,8))=="OVP57601"&&r.ReadInt32()==1&&r.ReadInt32()==9638,"576 header");var realPool=Read(r,123000);var synthetic=Read(r,123000);Need(Hash(realPool)==Hash(map.Stages[3].CopyFleetPool()),"576 original starting pool");
                    for(;;){int id=r.ReadInt32();if(id==-1)break;Need(id==count&&id<9638,"576 case order");var p=new int[12];p[0]=id;for(int n=1;n<12;n++)p[n]=r.ReadInt32();Need(p[1]>=0&&p[1]<=2,"576 scene");var before=Patch(r,p[1]<2?realPool:synthetic);var expected=Patch(r,before);var result=rules.Apply(before,p[2],p[3],checked((short)p[4]),p[5],p[11]);Need(Hash(result.CopyFleetPool())==Hash(expected)&&Hash(result.CopyInputFleetPool())==Hash(before),"Original576 producer case "+id);if(result.Skipped)skipped++;
                        if(p[1]==2||p[3]==0||(p[1]==1&&p[2]==p[4])){var c=Fixture(rules,map,p);Need(Hash(c.CopyFleetPool())==Hash(before)&&Hash(c.Execute().CopyFleetPool())==Hash(expected),"Original576 copied factory case "+id);factories++;}count++;}
                    Need(count==9638&&factories==9610&&skipped==130&&r.BaseStream.Position==r.BaseStream.Length,"576 complete matrix/EOF");
                }
            });
        }
    }
}
