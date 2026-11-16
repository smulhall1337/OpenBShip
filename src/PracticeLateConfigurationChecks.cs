using System;
using System.IO;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeLateConfigurationChecks
    {
        static void Need(bool condition,string message){if(!condition)throw new Exception(message);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static void I(byte[] b,int at,int value){BitConverter.GetBytes(value).CopyTo(b,at);}
        static void W(byte[] b,int at,int value){BitConverter.GetBytes(unchecked((short)value)).CopyTo(b,at);}
        static bool Reject(Action action){try{action();return false;}catch(InvalidDataException){return true;}catch(NotSupportedException){return true;}catch(ArgumentException){return true;}}
        static byte[][] Roots()
        {
            var s=new byte[402000];var f=new byte[123000];
            for(int n=0;n<1500;n++)W(s,n*268+4,n);
            for(int n=0;n<250;n++)PracticeFleetSnapshot.InitializeRootFleet(f,n*492,n);
            return new[]{s,f};
        }
        static byte[][] Carrier(PracticeUnitCatalog catalog)
        {
            var p=Roots();I(p[1],7*492,1);W(p[1],7*492+6,1);
            PracticeAircraftFactory.Construct(catalog,p[0],1490,5,1,7,1,1);
            I(p[1],7*492+16,0x4fc340+1490*268);
            for(int n=0;n<24;n++)I(p[0],1490*268+100+n*4,0);
            I(p[0],1490*268+100,35);
            return p;
        }
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            check("Late configuration matches original BSSB202 base marker preparation",delegate
            {
                var original=PracticeMapInstallations.Create(PracticeAirfieldFactory.Load(programs),File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),"BSSB202.SCN");
                byte[] s=original.CopyShipPool(),f=original.CopyFleetPool();string before=Hash(s)+Hash(f);
                var result=PracticeLateConfiguration.ApplyOffscreen(PracticeUnitCatalog.Load(programs),s,f,-2);
                Need(Hash(result.CopyShipPool())=="2682F92EA6F251F0369C4B87B65CED6FDF1D0C6168046D1F99FB6521F8E140C2"&&Hash(result.CopyFleetPool())=="1E67D9D7089349F1DBEEF48AAE0A39136FF51AD5A2015F150F4251741A588462","Original412 whole prepared pools");
                Need(result.ClearedMarkerUnits.Count==3&&result.ClearedMarkerUnits[0]==4&&result.ClearedMarkerUnits[1]==6&&result.ClearedMarkerUnits[2]==8&&result.CarrierGroups.Count==0&&result.NextInactiveFleet==4,"Source marker slots and next fleet");
                Need(Hash(s)+Hash(f)==before,"Original input changed");var copy=result.CopyShipPool();copy[0]^=255;Need(Hash(result.CopyShipPool())=="2682F92EA6F251F0369C4B87B65CED6FDF1D0C6168046D1F99FB6521F8E140C2","Mutable late result escaped");
                var again=PracticeLateConfiguration.ApplyOffscreen(PracticeUnitCatalog.Load(programs),result.CopyShipPool(),result.CopyFleetPool(),-2);
                Need(again.ClearedMarkerUnits.Count==0&&Hash(again.CopyShipPool())==Hash(result.CopyShipPool())&&Hash(again.CopyFleetPool())==Hash(result.CopyFleetPool()),"No-carrier marker preparation repeat");
            });
            check("Late marker cleanup includes inactive fleets and final unit while retaining odd payloads",delegate
            {
                var p=Roots();for(int m=0;m<6;m++){int unit=1494+m;I(p[0],unit*268,22+(m&1));p[0][unit*268+267]=(byte)(17+m);I(p[1],249*492+16+m*4,0x4fc340+unit*268);}
                var result=PracticeLateConfiguration.ApplyOffscreen(PracticeUnitCatalog.Load(programs),p[0],p[1],-2);var s=result.CopyShipPool();
                Need(result.ClearedMarkerUnits.Count==3&&result.NextInactiveFleet==0,"Inactive fleet was filtered");
                for(int m=0;m<6;m++){int unit=1494+m;Need(BitConverter.ToInt32(s,unit*268)==(m%2==0?0:23)&&s[unit*268+267]==17+m,"Even/odd/final-slot payload");}
                Need(Hash(result.CopyFleetPool())==Hash(p[1]),"Cleanup changed grid/references");
            });
            check("Late carrier raw repeat allocates additional groups and overwrites only its parent link",delegate
            {
                var catalog=PracticeUnitCatalog.Load(programs);var p=Carrier(catalog);string before=Hash(p[0])+Hash(p[1]);
                var first=PracticeLateConfiguration.ApplyOffscreen(catalog,p[0],p[1],-2);
                Need(first.CarrierGroups.Count==1&&first.CarrierGroups[0].ParentUnit==1490&&first.CarrierGroups[0].ParentFleet==7&&first.CarrierGroups[0].Equipment==35&&first.CarrierGroups[0].DestinationFleet==0&&first.CarrierGroups[0].Kind==6&&first.CarrierGroups[0].Link==0&&first.NextInactiveFleet==1,"First carrier allocation order");
                var second=PracticeLateConfiguration.ApplyOffscreen(catalog,first.CopyShipPool(),first.CopyFleetPool(),-2);
                Need(second.CarrierGroups.Count==1&&second.CarrierGroups[0].DestinationFleet==1&&second.NextInactiveFleet==2&&BitConverter.ToInt32(second.CopyFleetPool(),0)==6&&BitConverter.ToInt32(second.CopyFleetPool(),492)==6&&BitConverter.ToInt32(second.CopyShipPool(),1490*268+56)==0x4b47a0+492,"Whole finalizer was made idempotent");
                Need(Hash(p[0])+Hash(p[1])==before,"Carrier inputs changed");
            });
            check("Late kind11 numbering keeps independent owner counters and scan order",delegate
            {
                var p=Roots();foreach(int n in new[]{0,13,249})I(p[1],n*492,11);W(p[1],13*492+6,3);
                var result=PracticeLateConfiguration.ApplyOffscreen(PracticeUnitCatalog.Load(programs),p[0],p[1],-2);var f=result.CopyFleetPool();
                Need(result.NumberedMapObjects==3&&f[484]==1&&f[13*492+484]==1&&f[249*492+484]==2,"Source per-owner ordinal");
            });
            check("Late configuration rejects unsafe capacity references owners and local UI transactionally",delegate
            {
                var catalog=PracticeUnitCatalog.Load(programs);
                for(int variant=0;variant<8;variant++)
                {
                    var p=variant==0||variant>=4?Carrier(catalog):Roots();
                    if(variant==0){for(int n=0;n<250;n++)I(p[1],n*492,1);}
                    if(variant==1)I(p[1],16,0x4fc341);
                    if(variant==2){I(p[1],0,11);W(p[1],6,4);}
                    if(variant==3){I(p[1],249*492+36,0x4fc340+402000);}
                    if(variant==4)W(p[0],1490*268+14,7);
                    if(variant==5)W(p[0],1490*268+4,1491);
                    if(variant==7){for(int n=0;n<249;n++)I(p[1],n*492,1);}
                    string before=Hash(p[0])+Hash(p[1]);short owner=(short)(variant==6?1:-2);
                    Need(Reject(delegate{PracticeLateConfiguration.ApplyOffscreen(catalog,p[0],p[1],owner);})&&Hash(p[0])+Hash(p[1])==before,"Unsafe variant published partial result "+variant);
                }
                Need(Reject(delegate{PracticeLateConfiguration.ApplyOffscreen(catalog,new byte[401999],new byte[123000],-2);}),"Short pool admitted");
            });
            check("Configuration fallback matches owned kind preference empty and absent owners",delegate
            {
                var p=Roots();I(p[1],0,2);W(p[1],6,1);I(p[1],249*492,1);W(p[1],249*492+6,1);I(p[1],249*492+36,0x4fc340+1499*268);I(p[0],1499*268,-7);string before=Hash(p[0])+Hash(p[1]);
                var selected=PracticeConfigurationSelection.Select(p[0],p[1],1,248,1498);Need(selected.FoundOwnedFleet&&selected.Fleet==249&&selected.Unit==1499,"Kind1 final roster preference");
                I(p[0],1499*268,0);selected=PracticeConfigurationSelection.Select(p[0],p[1],1,248,1498);Need(selected.Fleet==249&&selected.Unit==-1,"All dead/null member0 fallback");
                selected=PracticeConfigurationSelection.Select(p[0],p[1],0,248,1498);Need(selected.FoundOwnedFleet&&selected.Fleet==1&&selected.Unit==-1,"Inactive owned fallback");
                selected=PracticeConfigurationSelection.Select(p[0],p[1],3,248,1498);Need(!selected.FoundOwnedFleet&&selected.Fleet==248&&selected.Unit==1498,"Absent owner prior globals");
                selected=PracticeConfigurationSelection.Select(p[0],p[1],3,-1,-1);Need(selected.Fleet==-1&&selected.Unit==-1,"Null prior globals");
                I(p[0],1499*268,-7);Need(Hash(p[0])+Hash(p[1])==before,"Selection changed input bytes");
            });
            check("Configuration selection rejects reached bad references and invalid priors",delegate
            {
                var p=Roots();I(p[1],0,1);I(p[1],16,0x4fc341);string before=Hash(p[0])+Hash(p[1]);
                Need(Reject(delegate{PracticeConfigurationSelection.Select(p[0],p[1],0,0,0);})&&Hash(p[0])+Hash(p[1])==before,"Bad selected reference changed pools");
                Need(Reject(delegate{PracticeConfigurationSelection.Select(p[0],p[1],0,250,0);})&&Reject(delegate{PracticeConfigurationSelection.Select(new byte[0],p[1],0,0,0);}),"Bad selection extent/prior admitted");
            });
            evidence("Late configuration matches complete original carrier setup and repeated setup pools",delegate
            {
                string root=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Late-Configuration");var catalog=PracticeUnitCatalog.Load(programs);
                foreach(string name in new[]{"First","Second"})
                {
                    string folder=Path.Combine(root,name);byte[] s=File.ReadAllBytes(Path.Combine(folder,"Before-Ships.bin")),f=File.ReadAllBytes(Path.Combine(folder,"Before-Fleets.bin"));string before=Hash(s)+Hash(f);
                    var result=PracticeLateConfiguration.ApplyOffscreen(catalog,s,f,-2);
                    Need(Hash(result.CopyShipPool())==Hash(File.ReadAllBytes(Path.Combine(folder,"After-Ships.bin")))&&Hash(result.CopyFleetPool())==Hash(File.ReadAllBytes(Path.Combine(folder,"After-Fleets.bin"))),"Whole original late pools "+name);
                    Need(result.CarrierGroups.Count==6&&result.ClearedMarkerUnits.Count==(name=="First"?3:0)&&result.NextInactiveFleet==(name=="First"?29:35)&&Hash(s)+Hash(f)==before,"Source late counts/order/transaction "+name);
                }
            });
            evidence("Configuration selection matches five complete original map contexts",delegate
            {
                string root=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Late-Configuration","Second");byte[] s=File.ReadAllBytes(Path.Combine(root,"After-Ships.bin")),f=File.ReadAllBytes(Path.Combine(root,"After-Fleets.bin"));
                short[] owners={-2,-1,0,1,3};int[] fleets={249,249,14,16,249},units={1499,1499,20,30,1499};
                for(int n=0;n<owners.Length;n++){var result=PracticeConfigurationSelection.Select(s,f,owners[n],249,1499);Need(result.Fleet==fleets[n]&&result.Unit==units[n],"Whole original419 owner "+owners[n]);}
            });
            evidence("Late marker preparation matches 120 complete original map-base pool outputs",delegate
            {
                var factory=PracticeAirfieldFactory.Load(programs);var catalog=PracticeUnitCatalog.Load(programs);int maps=0,markers=0;
                foreach(string line in File.ReadLines(Path.Combine(Path.GetDirectoryName(assets),"Evidence","Late-Base-436.csv")))
                {
                    if(line.StartsWith("File,"))continue;var row=line.Split(',');Need(row.Length==6&&Path.GetFileName(row[0])==row[0],"Late base evidence row");
                    var bases=PracticeMapInstallations.Create(factory,File.ReadAllBytes(Path.Combine(assets,"Graphics",row[0])),row[0]);
                    Need(Hash(bases.CopyShipPool())==row[1]&&Hash(bases.CopyFleetPool())==row[2],"Original391 input "+row[0]);
                    var result=PracticeLateConfiguration.ApplyOffscreen(catalog,bases.CopyShipPool(),bases.CopyFleetPool(),-2);
                    Need(Hash(result.CopyShipPool())==row[3]&&Hash(result.CopyFleetPool())==row[4]&&result.ClearedMarkerUnits.Count==int.Parse(row[5])&&result.CarrierGroups.Count==0,"Original436 whole pools "+row[0]);
                    maps++;markers+=result.ClearedMarkerUnits.Count;
                }
                Need(maps==120&&markers==1482,"Incomplete original436 evidence");
            });
        }
    }
}
