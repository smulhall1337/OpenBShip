using System;
using System.IO;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeSpawnZoneChecks
    {
        static void Require(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","");}
        static bool Reject(Action action){try{action();return false;}catch(InvalidDataException){return true;}catch(ArgumentException){return true;}}
        static void Record(byte[] pool,int slot,int kind,int x,int y)
        {
            int at=slot*492;BitConverter.GetBytes(kind).CopyTo(pool,at);BitConverter.GetBytes((short)x).CopyTo(pool,at+12);BitConverter.GetBytes((short)y).CopyTo(pool,at+14);
        }
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            check("Recovered spawn sectors retain source angle constant, axes and border exclusions",delegate
            {
                Require(BitConverter.DoubleToInt64Bits(PracticeSpawnZones.DegreesPerRadian)==0x404CA5DBDF8F4730L,"Original446168 constant bits");
                var zones=PracticeSpawnZones.Rebuild(new byte[8192],new byte[123000],new byte[246000],4);
                Require(zones.AvailableCells==3844&&string.Join(",",Array.ConvertAll(zones.CopyOwnerCounts(),delegate(int n){return n.ToString();}))=="961,961,961,961","Original empty-grid owner totals");
                Require(zones.OwnerAt(32,33)==0&&zones.OwnerAt(31,32)==1&&zones.OwnerAt(32,31)==3&&zones.OwnerAt(33,32)==0&&zones.OwnerAt(32,32)==0,"Source angular axis/truncation convention");
                for(int n=0;n<64;n++)Require(!zones.IsAvailable(0,n)&&!zones.IsAvailable(63,n)&&!zones.IsAvailable(n,0)&&!zones.IsAvailable(n,63),"Source outer border");
                Require((int)(Math.Atan2(10,0)*(180/Math.PI))/90==1&&zones.OwnerAt(32,33)!=1,"Angle negative control failed to distinguish180/PI");
            });
            check("Spawn reservations use inclusive boxes, all nonzero kinds and preserve blocked owners",delegate
            {
                var cells=new byte[8192];for(int n=0;n<cells.Length;n+=2){cells[n]=31;cells[n+1]=165;}
                var primary=new byte[123000];var secondary=new byte[246000];Record(primary,249,-1,320,320);Record(secondary,499,2,320,320);
                var zones=PracticeSpawnZones.Rebuild(cells,primary,secondary,2);
                Require(zones.AvailableCells==3835&&zones.PrimaryReservations==1&&zones.SecondaryReservations==1&&!zones.IsAvailable(31,31)&&!zones.IsAvailable(33,33)&&zones.IsAvailable(30,31)&&zones.IsAvailable(34,33),"Inclusive reservation extents");
                Require(zones.RawOwnerAt(31,31)==165&&zones.OwnerAt(31,31)==null&&zones.RawOwnerAt(0,0)==165,"Blocked header value changed");
                Record(primary,249,0,320,320);Record(secondary,499,0,320,320);
                var inactive=PracticeSpawnZones.Rebuild(cells,primary,secondary,2);
                Require(inactive.AvailableCells==3844&&inactive.PrimaryReservations==0&&inactive.SecondaryReservations==0,"Inactive stale coordinates reserve cells");
            });
            check("Spawn-zone complete exclusion is finite and leaves input data unchanged",delegate
            {
                var cells=new byte[8192];for(int n=0;n<cells.Length;n++)cells[n]=(byte)(n*7+19);
                var p=new byte[123000];var q=new byte[246000];
                for(int x=0;x<21;x++)for(int y=0;y<21;y++){int n=x*21+y;Record(n<250?p:q,n<250?n:n-250,1,10+30*x,10+30*y);}
                string before=Hash(cells)+Hash(p)+Hash(q);var result=PracticeSpawnZones.Rebuild(cells,p,q,3);
                Require(result.AvailableCells==0&&Hash(cells)+Hash(p)+Hash(q)==before,"Exhausted grid changed input or left cells open");
                var after=result.CopyCells();for(int n=0;n<8192;n+=2)Require(after[n]==0&&after[n+1]==cells[n+1],"Exhausted grid lost opaque owner bytes");
            });
            check("Map base spawn-zone profile matches original supplied BSSB202 reservations",delegate
            {
                byte[] input=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));var scene=Scenario.Load(input,"BSSB202.SCN");
                var bases=PracticeMapInstallations.Create(PracticeAirfieldFactory.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs")),input,"BSSB202.SCN");
                var f=bases.CopyFleetPool();string before=Hash(f);var zones=PracticeSpawnZones.ForMapBaseStudy(scene,f);
                Require(zones.PlayerCount==2&&zones.AvailableCells==3605&&zones.PrimaryReservations==4&&zones.SecondaryReservations==53&&zones.CopyOwnerCounts()[0]==1815&&zones.CopyOwnerCounts()[1]==1790,"Original399 study profile");
                Require(Hash(f)==before,"Base reservations changed");var cells=zones.CopyCells();string key=Hash(cells);cells[0]^=255;var owners=zones.CopyOwnerCounts();owners[0]=0;
                Require(Hash(zones.CopyCells())==key&&zones.CopyOwnerCounts()[0]==1815,"Spawn result escaped mutable copy");
            });
            check("Spawn-zone rebuilding rejects incomplete grids, pools and invalid player counts",delegate
            {
                var cells=new byte[8192];var p=new byte[123000];var q=new byte[246000];
                Require(Reject(delegate{PracticeSpawnZones.Rebuild(new byte[8191],p,q,2);})&&Reject(delegate{PracticeSpawnZones.Rebuild(cells,new byte[122999],q,2);})&&Reject(delegate{PracticeSpawnZones.Rebuild(cells,p,new byte[245999],2);})&&Reject(delegate{PracticeSpawnZones.Rebuild(cells,p,q,0);})&&Reject(delegate{PracticeSpawnZones.Rebuild(cells,p,q,5);}),"Invalid source geometry admitted");
            });
            evidence("Spawn-zone geometry matches 48 complete original grid cases",delegate
            {
                string root=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Spawn-Zones-399");int count=0;var seen=new bool[48];
                Require(BitConverter.ToString(File.ReadAllBytes(Path.Combine(root,"DegreeConstant.bin")))=="30-47-8F-DF-DB-A5-4C-40","Source degree constant identity");
                foreach(string line in File.ReadLines(Path.Combine(root,"Cases.csv")))
                {
                    if(line.StartsWith("Case,"))continue;var row=line.Split(',');int id=int.Parse(row[0]);
                    Require(row.Length==11&&id>=0&&id<48&&!seen[id]&&Array.IndexOf(new[]{"Empty","Sparse","Inactive","FullLoaded","StudyBaseTerrain","Exhausted"},row[1])>=0,"Spawn-zone evidence case");seen[id]=true;
                    Require((row[3]=="639"||row[3]=="895")&&row[4]==row[3],"Native precision context");
                    var result=PracticeSpawnZones.Rebuild(File.ReadAllBytes(Path.Combine(root,row[1]+"-InputCells.bin")),File.ReadAllBytes(Path.Combine(root,row[1]+"-Primary.bin")),File.ReadAllBytes(Path.Combine(root,row[1]+"-Secondary.bin")),int.Parse(row[2]));
                    Require(Hash(result.CopyCells())==row[10]&&Hash(File.ReadAllBytes(Path.Combine(root,id.ToString("D3")+"-Cells.bin")))==row[10]&&result.AvailableCells==int.Parse(row[5]),"Original complete grid "+id);
                    var owners=result.CopyOwnerCounts();for(int n=0;n<4;n++)Require(owners[n]==int.Parse(row[6+n]),"Original owner count "+id);count++;
                }
                Require(count==48,"Incomplete original grid evidence");
            });
        }
    }
}
