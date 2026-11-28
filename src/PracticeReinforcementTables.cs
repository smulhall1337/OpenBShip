using System;using System.Collections.Generic;using System.IO;using System.Security.Cryptography;
namespace FleetLab
{
    // Optional, separately pinned readers. The established 15-unit catalog
    // and 40-entry ammunition reader retain their original identities.
    public sealed class PracticeReinforcementTables
    {
        internal readonly PracticeUnitCatalog Catalog;
        internal readonly byte[] Units,Stock,Templates;
        internal PracticeReinforcementTables(PracticeUnitCatalog catalog,byte[] extraUnits,byte[] stock,byte[] templates)
        {
            if(catalog==null)throw new ArgumentNullException("catalog");
            Check(extraUnits,536,"28A7F31F9D8876BE5C944BCE26B0B37FC3A21894D861F1A46284573460867B55","additional unit templates");
            Check(stock,70,"B04CCA1D57AC6303B54D82C282EA1815B890131C720F40829BD49B993409CF25","reinforcement stock");
            Check(templates,304,"D6326BDFECAFB2D999606DDD2C5D177C25062119990B9398CB26FC1B2A933C4B","bank-five templates");
            Catalog=catalog;Stock=(byte[])stock.Clone();Templates=(byte[])templates.Clone();Units=new byte[4556];Array.Copy(catalog.Templates,Units,catalog.Templates.Length);Array.Copy(extraUnits,0,Units,4020,536);
            for(int type=15;type<=16;type++){int at=type*268;if(I(Units,at)!=type||W(Units,at+14)<1||W(Units,at+14)>6)throw new InvalidDataException("Unsupported additional unit shape.");}
            for(int template=0;template<4;template++){if(I(Templates,template*76)!=1)throw new InvalidDataException("Expected carrierless kind-1 template.");int members=0;for(int n=0;n<6;n++){int at=template*76+4+n*12,type=I(Templates,at);if(type==0)break;if(type!=2&&type!=14&&type!=15&&type!=16)throw new InvalidDataException("Unsupported default reinforcement unit.");int x=I(Templates,at+4),y=I(Templates,at+8),length=W(Units,type*268+14);if(x<0||x>9||y<0||y+length>10)throw new InvalidDataException("Reinforcement template exceeds the local grid.");members++;}if(members!=5)throw new InvalidDataException("Expected five default reinforcement members.");}
        }
        static void Check(byte[] bytes,int length,string identity,string label){if(bytes==null||bytes.Length!=length)throw new InvalidDataException("Incomplete "+label+"; run Setup again.");using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","")!=identity)throw new InvalidDataException("Unsupported "+label+" identity.");}
        internal static int I(byte[] b,int at){return BitConverter.ToInt32(b,at);}internal static int W(byte[] b,int at){return BitConverter.ToInt16(b,at);}
        internal static void I(byte[] b,int at,int value){BitConverter.GetBytes(value).CopyTo(b,at);}internal static void W(byte[] b,int at,int value){BitConverter.GetBytes(unchecked((short)value)).CopyTo(b,at);}
        public static PracticeReinforcementTables Load(string programs){foreach(string name in new[]{"ReinforcementUnits.seqbin","ReinforcementBankFive.seqbin"})if(!File.Exists(Path.Combine(programs,name)))throw new InvalidDataException("Generated reinforcement tables are missing. Rerun Setup.ps1 with the supported original installation.");return new PracticeReinforcementTables(PracticeUnitCatalog.Load(programs),File.ReadAllBytes(Path.Combine(programs,"ReinforcementUnits.seqbin")),File.ReadAllBytes(Path.Combine(programs,"InstallationStock.seqbin")),File.ReadAllBytes(Path.Combine(programs,"ReinforcementBankFive.seqbin")));}
        public byte[] CopyAdditionalUnits(){var result=new byte[536];Array.Copy(Units,4020,result,0,result.Length);return result;}
        public byte[] CopyTemplates(){return (byte[])Templates.Clone();}
        // 0042CFE0/42D070/42D120. Type15's equipment40 reaches the next
        // record at offset276; source ammo40 is stockWORD4, zero in this build.
        // The subsequent 35-WORD override is preserved independently of that
        // flat-pool access. It is not an invented in-record inventory slot.
        internal void ConstructUnit(byte[] ships,int slot,int type,int owner,int fleet,int x,int y)
        {
            if(type<15){PracticeAircraftFactory.Construct(Catalog,ships,slot,type,owner,fleet,x,y);return;}
            if(type!=15&&type!=16)throw new NotSupportedException("Unrecovered reinforcement unit type.");
            int at=slot*268;Array.Copy(Units,type*268,ships,at,268);W(ships,at+4,slot);W(ships,at+6,owner);W(ships,at+8,x);W(ships,at+10,y);W(ships,at+12,fleet);int length=W(ships,at+14);
            for(int n=0;n<35;n++)W(ships,at+196+n*2,0);
            for(int n=0;n<length*4;n++){int weapon=I(ships,at+100+n*4);if(weapon<0||weapon>40)throw new InvalidDataException("Unsupported extended equipment.");int destination=at+196+weapon*2;if(destination>ships.Length-2)throw new InvalidDataException("Equipment crosses the complete ship pool.");int amount=weapon==40?W(Stock,8):W(Catalog.Ammunition,weapon*2);W(ships,destination,amount==-1?-1:W(ships,destination)+amount);}
            if(type==15)Array.Copy(Stock,0,ships,at+196,70);
            var equipment=new List<int>();for(int n=0;n<length*4;n++)equipment.Add(I(ships,at+100+n*4));var offense=new HashSet<int>();for(int n=0;n<Catalog.Offense.Length;n+=4){int weapon=I(Catalog.Offense,n);if(weapon==0)break;offense.Add(weapon);}
            foreach(int field in new[]{76,80,84}){int current=I(ships,at+field);bool present=current!=0&&equipment.Contains(current)&&(field==80||W(ships,at+196+current*2)!=0);if(present)continue;foreach(int weapon in equipment){bool eligible=field==76?weapon>=4&&weapon<=8&&W(ships,at+196+weapon*2)!=0:field==80?weapon>=9&&weapon<=13:offense.Contains(weapon)&&W(ships,at+196+weapon*2)!=0;if(eligible){I(ships,at+field,weapon);break;}}}
        }
    }
}
