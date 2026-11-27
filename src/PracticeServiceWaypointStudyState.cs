using System;using System.IO;using System.Collections.ObjectModel;using System.Security.Cryptography;
namespace FleetLab
{
    public sealed class PracticeServiceWaypointStudyBasis
    {
        static readonly string[] names={"Nearest eligible yard","Equal distance · earlier yard","No yard · keep cached destination","Queue gap · preserve later entries","No first waypoint · skip","Navigation active · skip unread fields","Mode 2 · skip","Active target reference · retain","Signed coordinate limit (diagnostic)"};
        static readonly int[][] presets={new[]{1,4,3,0,0,11},new[]{1,3,2,0,-2,11},new[]{1,0,3,0,-1,11},new[]{1,3,6,0,0,11},new[]{1,4,3,1,250,11},new[]{1,4,3,2,int.MaxValue,99},new[]{1,4,3,3,250,11},new[]{1,4,5,0,249,11},new[]{1,9,1,0,-1,11}};
        readonly byte[] ships,fleets,region;
        public static ReadOnlyCollection<string> Presets {get{return Array.AsReadOnly(names);}}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        internal static void I(byte[] b,int at,int v){BitConverter.GetBytes(v).CopyTo(b,at);}internal static void W(byte[] b,int at,int v){BitConverter.GetBytes(unchecked((short)v)).CopyTo(b,at);}
        PracticeServiceWaypointStudyBasis(byte[] s,byte[] f,byte[] r){ships=(byte[])s.Clone();fleets=(byte[])f.Clone();region=(byte[])r.Clone();}
        public static PracticeServiceWaypointStudyBasis Create(string programs,PracticeMapStartup startup)
        {
            if(startup==null)throw new ArgumentNullException("startup");if(startup.Seed!=731)throw new NotSupportedException("Service waypoint study requires the compared BSSB202 seed731 copy.");
            var service=PracticeFleetService.Create(programs,startup,"30-1-blocked",11,PracticeFleetServiceMode.InitializedDispatcher);var s=service.CopyShipPool();var f=service.CopyFleetPool();
            if(Hash(s)!="1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8"||Hash(f)!="CE199D97FB5F582F1569C1B6ED040A4BBB6BF54B74A04769FEB229E83DD1B8C4")throw new InvalidDataException("Copied service basis differs from original496 comparison.");
            for(int u=0;u<1500;u++){int at=u*268,k=BitConverter.ToInt32(s,at);if(k==5||k==6||k==7)Array.Clear(s,at+56,16);}for(int g=0;g<250;g++)I(f,g*492,0);var r=new byte[78560];for(int g=0;g<250;g++)PracticeFleetServiceContext.CreateDiagnostic(g).CopyTo(r,g*312);for(int n=78000;n<78016;n++)r[n]=0xa7;return new PracticeServiceWaypointStudyBasis(s,f,r);
        }
        // Raw recorded input factory shared by original-output checks. Public
        // study presets supply the smaller goal/cache values documented below.
        internal PracticeServiceWaypointStudyState CreateComparison(int[] spec)
        {
            if(spec==null||spec.Length!=6)throw new ArgumentException("Six supplied comparison fields required.");var f=FixtureFleet(fleets,spec);var r=(byte[])region.Clone();int at=16*312;Array.Copy(r,at,r,at+20,180);Array.Clear(r,at,20);I(r,at,spec[5]);I(r,at+200,0x11223344);I(r,at+8,0x12345678);I(r,at+12,0x76543210);I(r,at+304,spec[3]==2?1:0);I(r,at+308,spec[4]);return new PracticeServiceWaypointStudyState(ships,f,new PracticeAiSharedBacking(r),-1,null);
        }
        public PracticeServiceWaypointStudyState CreateFixture(int preset)
        {
            if(preset<0||preset>=presets.Length)throw new ArgumentOutOfRangeException("preset");var state=CreateComparison(presets[preset]);var task=state.BeforeBacking.CopyTaskRecord(16);I(task,200,4);I(task,8,550);I(task,12,290);return new PracticeServiceWaypointStudyState(ships,state.CopyBeforeFleetPool(),state.BeforeBacking.WithTaskRecord(16,task),preset,null);
        }
        // FIXTURE783
        internal static byte[] FixtureFleet(byte[] basis,int[] spec)
    {
        int kind=spec[0],profile=spec[1],queue=spec[2],gate=spec[3],target=spec[4];var f=(byte[])basis.Clone();int actor=16*492;I(f,actor,kind);W(f,actor+6,1);I(f,actor+472,0x11223344);I(f,actor+476,gate==3?2:0);f[actor+485]=83;W(f,actor+12,profile>=3?0:610);W(f,actor+14,profile>=3?0:590);
        for(int n=0;n<5;n++){W(f,actor+40+n*2,queue==6?(n%2==0?100+n*10:-1):(n<queue?100+n*10:-1));W(f,actor+50+n*2,200+n*10);}if(gate==1)W(f,actor+40,-1);
        foreach(int yard in new[]{2,3}){int at=yard*492;Array.Clear(f,at+16,24);W(f,at+6,1);W(f,at+12,yard==2?100:130);W(f,at+14,yard==2?100:80);}
        Action<int,int> marker=delegate(int yard,int unit){I(f,yard*492,2);I(f,yard*492+16,0x4fc340+unit*268);};if(profile==1)marker(2,5);if(profile==2)marker(3,9);if(profile>=3){marker(2,5);marker(3,9);}
        if(profile==3){W(f,2*492+12,100);W(f,2*492+14,0);W(f,3*492+12,0);W(f,3*492+14,100);}if(profile==4){W(f,2*492+12,200);W(f,2*492+14,0);W(f,3*492+12,50);W(f,3*492+14,0);}if(profile==5){W(f,2*492+6,0);W(f,2*492+12,10);W(f,2*492+14,0);W(f,3*492+12,100);W(f,3*492+14,0);}if(profile==6)Array.Clear(f,2*492+16,24);if(profile==7)I(f,2*492,1);if(profile==8)I(f,2*492+20,0x4fc340+7*268);if(profile==9){W(f,actor+12,-32768);W(f,actor+14,-32768);W(f,2*492+12,32767);W(f,2*492+14,32767);W(f,3*492+12,32766);W(f,3*492+14,-32768);}
        if(target==249)I(f,249*492,1);return f;
    }
    }
    public sealed class PracticeServiceWaypointStudyState
    {
        readonly byte[] ships,before;
        public PracticeAiSharedBacking BeforeBacking {get;private set;}
        public PracticeServiceWaypointResult Result {get;private set;}
        public bool Executed {get{return Result!=null;}}
        public int Preset {get;private set;}
        public PracticeAiSharedBacking Backing {get{return Executed?Result.Backing:BeforeBacking;}}
        public int OriginX {get{return BitConverter.ToInt16(before,16*492+12);}}public int OriginY {get{return BitConverter.ToInt16(before,16*492+14);}}
        internal PracticeServiceWaypointStudyState(byte[] s,byte[] f,PracticeAiSharedBacking b,int preset,PracticeServiceWaypointResult result){ships=(byte[])s.Clone();before=(byte[])f.Clone();BeforeBacking=b;Preset=preset;Result=result;}
        public byte[] CopyShipPool(){return (byte[])ships.Clone();}public byte[] CopyBeforeFleetPool(){return (byte[])before.Clone();}public byte[] CopyFleetPool(){return Executed?Result.CopyFleetPool():(byte[])before.Clone();}
        public PracticeServiceWaypointStudyState Execute(){if(Executed)throw new InvalidOperationException("One copied callback is complete;reset before repeating.");return new PracticeServiceWaypointStudyState(ships,before,BeforeBacking,Preset,PracticeServiceWaypoint.Evaluate(ships,before,BeforeBacking,16,1));}
        public PracticeServiceWaypointStudyState Reset(){return new PracticeServiceWaypointStudyState(ships,before,BeforeBacking,Preset,null);}
    }
}
