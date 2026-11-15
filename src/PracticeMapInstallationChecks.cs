using System;
using System.IO;
using System.Drawing;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeMapInstallationChecks
    {
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","");}
        static bool Reject(Action action){try{action();return false;}catch(InvalidDataException){return true;}}
        static byte[] Fixture(int count,byte owner)
        {
            using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
            {
                for(int x=0;x<64;x++)for(int y=0;y<64;y++)
                {
                    bool special=x%3==0&&y%3==0&&x/3<4&&y/3<16&&(x/3*16+y/3)<count;
                    writer.Write((byte)(special?3:1));
                    if(special){writer.Write(owner);writer.Write(new byte[400]);}
                }
                writer.Write((byte)1);writer.Write(0);writer.Write(0);
                return stream.ToArray();
            }
        }
        internal static void Run(Action<string,Action> check,Action<string,Action> checkEvidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            check("Map base pass matches original BSSB202 whole pools and owner counters",delegate
            {
                var state=PracticeMapInstallations.Create(PracticeAirfieldFactory.Load(programs),File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),"BSSB202.SCN");
                Require(state.SourceHash=="1355582241BD807AE2943F5A67D32BB23EA58A77A388BE8928AEA41F5E892F4F"&&state.SourceBytes==8890,"Original map identity");
                Require(state.CandidateCells==4&&state.DuplicateCells==3&&state.Installations.Count==1&&state.NextFleetSlot==4&&state.UnitCount==10,"Source scan/dedup/counters");
                var item=state.Installations[0];
                Require(item.SourceCell==new Point(60,59)&&item.Origin==new Point(600,590)&&item.Owner==1&&item.SourceTag==31&&item.TagArgument==1&&item.FirstFleet==0,"Source input metadata");
                Require(Hash(state.CopyShipPool())=="AFE0FF75918DE5F0680A792CA8491E0250EC23CE46680486F341A1EE825ED314"&&Hash(state.CopyFleetPool())=="1E67D9D7089349F1DBEEF48AAE0A39136FF51AD5A2015F150F4251741A588462","Whole-original391 pools");
                Require(string.Join(",",Array.ConvertAll(state.CopyOwnerCounters(),delegate(uint n){return n.ToString();}))=="1,1,2,1,1","Owner-1 counter offset");
            });
            check("First Practice map has no base construction and retains original roots",delegate
            {
                var state=PracticeMapInstallations.Create(PracticeAirfieldFactory.Load(programs),File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN")),"BSSB201.SCN");
                Require(state.Installations.Count==0&&state.CandidateCells==0&&state.NextFleetSlot==0&&state.UnitCount==0,"Empty ordinary base pass");
                Require(Hash(state.CopyShipPool())=="FD2BF00D70C38D92B6FC0C28FA580694B499E663905FE6BFC7A2B1E240B5C733"&&Hash(state.CopyFleetPool())=="DED6C152E3080E29AD6C6DD52716F7336C57F7536BFFB1C4CEA3E27B7F4C7C22","Whole-original root bytes");
            });
            check("Map base pass preserves neutral and multiple owned installations",delegate
            {
                var state=PracticeMapInstallations.Create(PracticeAirfieldFactory.Load(programs),File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB205.SCN")),"BSSB205.SCN");
                Require(state.Installations.Count==14&&state.CandidateCells==56&&state.DuplicateCells==42&&state.NextFleetSlot==56&&state.UnitCount==140,"Multiple base scan counts");
                Require(Hash(state.CopyShipPool())=="C75D3D379A679074B6DFF51AC120DCFE15CE60648C477957F0311293D5FE22E0"&&Hash(state.CopyFleetPool())=="187EBEFAC75B3347DC23C90DADE7563C6A0FD566C6560A38EBE5E22745D875EE","Multiple-base whole-original pools");
                Require(string.Join(",",Array.ConvertAll(state.CopyOwnerCounters(),delegate(uint n){return n.ToString();}))=="6,5,6,1,1","Signed neutral owner counter");
            });
            check("Map installation copies isolate input, pools and returned counters",delegate
            {
                byte[] source=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));string before=Hash(source);
                var state=PracticeMapInstallations.Create(PracticeAirfieldFactory.Load(programs),source,"BSSB202.SCN");
                Require(Hash(source)==before,"Input SCN changed");
                string ships=Hash(state.CopyShipPool()),fleets=Hash(state.CopyFleetPool());
                source[0]^=255;var s=state.CopyShipPool();s[0]^=255;var f=state.CopyFleetPool();f[0]^=255;var counters=state.CopyOwnerCounters();counters[0]=99;
                Require(Hash(state.CopyShipPool())==ships&&Hash(state.CopyFleetPool())==fleets&&state.SourceHash==before&&state.CopyOwnerCounters()[0]==1,"Mutable output escaped copy");
            });
            check("Map base pass rejects owner and fleet-capacity overflow without changing source",delegate
            {
                var factory=PracticeAirfieldFactory.Load(programs);
                foreach(byte[] bytes in new[]{Fixture(1,4),Fixture(1,254),Fixture(63,0)})
                {
                    string before=Hash(bytes);
                    Require(Reject(delegate{PracticeMapInstallations.Create(factory,bytes,"bounded fixture");})&&Hash(bytes)==before,"Unsafe base fixture was admitted or mutated");
                }
                var maximum=PracticeMapInstallations.Create(factory,Fixture(62,0),"maximum admitted fixture");
                Require(maximum.NextFleetSlot==248&&maximum.UnitCount==620&&maximum.Installations.Count==62,"Last complete four-fleet block");
            });
            check("Map base pass rejects truncated and unresolved SCN variants",delegate
            {
                var factory=PracticeAirfieldFactory.Load(programs);
                byte[] complete=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));
                foreach(int size in new[]{0,4095,complete.Length-1}){var bytes=new byte[size];Array.Copy(complete,bytes,size);Require(Reject(delegate{PracticeMapInstallations.Create(factory,bytes,"truncated");}),"Truncated SCN accepted");}
                Require(Reject(delegate{PracticeMapInstallations.Create(factory,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB418.SCN")),"BSSB418.SCN");}),"Unresolved SCN was silently accepted");
            });
            checkEvidence("Map base pass matches 120 original source-file scans",delegate
            {
                var factory=PracticeAirfieldFactory.Load(programs);int count=0,bases=0,cells=0;
                foreach(string line in File.ReadLines(Path.Combine(Path.GetDirectoryName(assets),"Evidence","Map-Installations-391.csv")))
                {
                    if(line.StartsWith("File,"))continue;
                    var row=line.Split(',');
                    Require(row.Length==11&&Path.GetFileName(row[0])==row[0],"Map base evidence row");
                    var state=PracticeMapInstallations.Create(factory,File.ReadAllBytes(Path.Combine(assets,"Graphics",row[0])),row[0]);
                    Require(state.SourceHash==row[1]&&Hash(state.CopyShipPool())==row[2]&&Hash(state.CopyFleetPool())==row[3]&&state.NextFleetSlot==int.Parse(row[4])&&state.CandidateCells==int.Parse(row[10]),"Original base scan "+row[0]);
                    var counters=state.CopyOwnerCounters();for(int n=0;n<5;n++)Require(counters[n]==uint.Parse(row[5+n]),"Original owner counters "+row[0]);
                    count++;bases+=state.Installations.Count;cells+=state.CandidateCells;
                }
                Require(count==120&&bases==494&&cells==1976,"Original map evidence is incomplete");
            });
        }
    }
}
