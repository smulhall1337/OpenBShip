using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace FleetLab
{
    public static class PracticeMapInstallationExport
    {
        public static void Save(string assets,string programs,string destination,string name)
        {
            string output=Path.GetFullPath(destination);
            if(Directory.Exists(output)||File.Exists(output))throw new IOException("Choose a new export directory; existing files are preserved.");
            if(string.IsNullOrEmpty(name)||Path.GetFileName(name)!=name||!string.Equals(Path.GetExtension(name),".scn",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Choose one SCN filename from Assets/Graphics.");
            var state=PracticeMapInstallations.Create(PracticeAirfieldFactory.Load(programs),File.ReadAllBytes(Path.Combine(assets,"Graphics",name)),name);
            var ships=state.CopyShipPool();
            var fleets=state.CopyFleetPool();
            var installations=new List<string>{"FirstFleet,CellX,CellY,OriginX,OriginY,Owner,SourceTag,TagArgument"};
            foreach(var item in state.Installations)installations.Add(item.FirstFleet+","+item.SourceCell.X+","+item.SourceCell.Y+","+item.Origin.X+","+item.Origin.Y+","+item.Owner+","+item.SourceTag+","+item.TagArgument);
            var units=new List<string>{"Slot,Type,Fleet,Owner,LocalX,LocalY,Length,HP"};
            for(int slot=0;slot<state.UnitCount;slot++)
            {
                int at=slot*268;
                units.Add(slot+","+BitConverter.ToInt32(ships,at)+","+BitConverter.ToInt16(ships,at+12)+","+BitConverter.ToInt16(ships,at+6)+","+BitConverter.ToInt16(ships,at+8)+","+BitConverter.ToInt16(ships,at+10)+","+BitConverter.ToInt16(ships,at+14)+","+BitConverter.ToInt16(ships,at+16));
            }
            var records=new List<string>{"Slot,Kind,Owner,WorldX,WorldY,Flag,Members"};
            Directory.CreateDirectory(output);
            File.WriteAllBytes(Path.Combine(output,"Ships.bin"),ships);
            File.WriteAllBytes(Path.Combine(output,"Fleets.bin"),fleets);
            File.WriteAllLines(Path.Combine(output,"Installations.csv"),installations.ToArray());
            File.WriteAllLines(Path.Combine(output,"Units.csv"),units.ToArray());
            for(int slot=0;slot<state.NextFleetSlot;slot++)
            {
                int at=slot*492,count=0;
                for(int n=0;n<6;n++)if(BitConverter.ToUInt32(fleets,at+16+n*4)!=0)count++;
                records.Add(slot+","+BitConverter.ToInt32(fleets,at)+","+BitConverter.ToInt16(fleets,at+6)+","+BitConverter.ToInt16(fleets,at+12)+","+BitConverter.ToInt16(fleets,at+14)+","+fleets[at+484]+","+count);
                var grid=new List<string>();
                for(int x=0;x<10;x++)
                {
                    var row=new string[10];
                    for(int y=0;y<10;y++)row[y]=BitConverter.ToInt32(fleets,at+64+(x*10+y)*4).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    grid.Add(string.Join(",",row));
                }
                File.WriteAllLines(Path.Combine(output,"Fleet"+slot+"-Grid.csv"),grid.ToArray());
            }
            File.WriteAllLines(Path.Combine(output,"Fleets.csv"),records.ToArray());
            var inputs=new List<string>{"Source="+state.SourceName,"SourceBytes="+state.SourceBytes,"SourceSHA256="+state.SourceHash,"CandidateCells="+state.CandidateCells,"DuplicateCells="+state.DuplicateCells,"Installations="+state.Installations.Count,"NextFleetSlot="+state.NextFleetSlot,"Units="+state.UnitCount,"RandomCalls=0"};
            uint[] counters=state.CopyOwnerCounters();
            for(int n=0;n<counters.Length;n++)inputs.Add("OwnerCounter["+(n-1)+"]="+counters[n]);
            File.WriteAllLines(Path.Combine(output,"Source-Inputs.txt"),inputs.ToArray());
            File.WriteAllLines(Path.Combine(output,"Readme.txt"),new[]{
                "OpenBShip ordinary SCN base-construction pass — standalone copied data.",
                "Pinned RIP Bs.exe B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569: ordinary402D70 base scan,4030F0 and42C4A0 constructor/callees.",
                "Only tags3/31/32/33 create installations; existing kind2 origins suppress duplicate cells. Scan order is outerX/innerY. TagArgument is preserved opaque input and not interpreted as gameplay.",
                "Fresh initialized pools, first fleet0 and five owner counters starting1. No random draws in this stage. Scenario-specific constructors, other terrain/objects, grouped fleets, postload stock/aircraft, live support/AI/missions and application pacing are excluded.",
                "Ships.bin:1500 records/268bytes; Fleets.bin:250 records/492bytes; little endian. Member references are normalized to4FC340; aircraft links to4B47A0. These are data, never executable code.",
                "Grid rows use localX, columns localY. Source3 is empty kind2 grid and2 is occupied. Raw marker rosters are not worker-ready; do not invoke active support on these exports.",
                "Manifest.csv covers every other file. The supplied SCN, active game, saved settings and original executable are preserved. This export is not a complete mission."
            });
            var manifest=new List<string>{"File,Bytes,SHA256"};
            string[] files=Directory.GetFiles(output);
            Array.Sort(files,StringComparer.OrdinalIgnoreCase);
            foreach(string file in files)using(var hash=SHA256.Create())using(var input=File.OpenRead(file))manifest.Add(Path.GetFileName(file)+","+input.Length+","+BitConverter.ToString(hash.ComputeHash(input)).Replace("-",""));
            File.WriteAllLines(Path.Combine(output,"Manifest.csv"),manifest.ToArray());
        }
    }
}
