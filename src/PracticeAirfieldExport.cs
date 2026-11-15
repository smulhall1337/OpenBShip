using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace FleetLab
{
    public static class PracticeAirfieldExport
    {
        static int I(byte[] b,int p)
        {
            return BitConverter.ToInt32(b,p);
        }
        static int W(byte[] b,int p)
        {
            return BitConverter.ToInt16(b,p);
        }
        public static void Save(string programs,string destination)
        {
            string output=Path.GetFullPath(destination);
            if(Directory.Exists(output)||File.Exists(output))throw new IOException("Choose a new export directory; existing files are preserved.");
            var result=PracticeAirfieldFactory.Load(programs).CreateFresh(30,new Point(400,370),0,1);
            var ships=result.CopyShipPool();
            var fleets=result.CopyFleetPool();
            Directory.CreateDirectory(output);
            File.WriteAllBytes(Path.Combine(output,"Ships.bin"),ships);
            File.WriteAllBytes(Path.Combine(output,"Fleets.bin"),fleets);
            var units=new List<string>
            {
                "Slot,Type,Fleet,Owner,LocalX,LocalY,Length,HP,AA1,AA2,AA3"
            };
            foreach(int slot in result.AllocatedUnits)
            {
                int p=slot*268;
                units.Add(slot+","+I(ships,p)+","+W(ships,p+12)+","+W(ships,p+6)+","+W(ships,p+8)+","+W(ships,p+10)+","+W(ships,p+14)+","+W(ships,p+16)+","+W(ships,p+198)+","+W(ships,p+200)+","+W(ships,p+202));
            }
            File.WriteAllLines(Path.Combine(output,"Units.csv"),units.ToArray());
            var records=new List<string>
            {
                "Slot,Kind,Owner,WorldX,WorldY,Flag,Members"
            };
            for(int slot=30;slot<34;slot++)
            {
                int p=slot*492,count=0;
                for(int n=0;n<6;n++)if(I(fleets,p+16+n*4)!=0)count++;
                records.Add(slot+","+I(fleets,p)+","+W(fleets,p+6)+","+W(fleets,p+12)+","+W(fleets,p+14)+","+fleets[p+484]+","+count);
                var grid=new List<string>();
                for(int x=0;x<10;x++)
                {
                    var row=new string[10];
                    for(int y=0;y<10;y++)row[y]=I(fleets,p+64+(x*10+y)*4).ToString();
                    grid.Add(string.Join(",",row));
                }
                File.WriteAllLines(Path.Combine(output,"Fleet"+slot+"-Grid.csv"),grid.ToArray());
            }
            File.WriteAllLines(Path.Combine(output,"Fleets.csv"),records.ToArray());
            File.WriteAllLines(Path.Combine(output,"Readme.txt"),new[]
            {
                "OpenBShip recovered compound airfield constructor — standalone diagnostic copy.", "Pinned RIP Bs.exe SHA256 B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569; source42C4A0..42C80D and unit/grid helpers.", "Supplied fresh pools, first fleet30, origin400,370, owner0, flag1; four kind2 fleets and ten original-template units. Creation/placement is supplied, not a loaded mission.", "Ships.bin:1500records/268bytes; Fleets.bin:250records/492bytes; little endian. Six member references use original normalized base4FC340, four aircraft links base4B47A0. These files contain data, never executable code.", "Grid CSV: first index(localX) selects row; localY selects column; source3 is empty kind2 grid and source2 is occupied. Original coordinate convention retained.", "This constructor is not installed into the shared flight study. Kind2 support/supply continuation, map caller, mission placement, AI and application timing remain separate. The shared study's synthetic home is preserved.", "This export creates a new directory and does not change the active game, saved settings or source media."
            });
            var manifest=new List<string>
            {
                "File,Bytes,SHA256"
            };
            foreach(string file in Directory.GetFiles(output))using(var hash=SHA256.Create())using(var input=File.OpenRead(file))manifest.Add(Path.GetFileName(file)+","+input.Length+","+BitConverter.ToString(hash.ComputeHash(input)).Replace("-",""));
            File.WriteAllLines(Path.Combine(output,"Manifest.csv"),manifest.ToArray());
        }
    }
}
