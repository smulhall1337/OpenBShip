using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab {
    public static class Validation {
        static string Hash(byte[] value){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(value)).Replace("-","");}
        static PracticeTimedStudyState FiringFixture(string assets) {
            string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var units=PracticeUnitCatalog.Load(p);
            var deployment=PracticeDeployment.FirstPractice(units,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN")),143514006u);
            return new PracticeTimedStudyState(PracticeTimedWeaponCatalog.Load(p),units,File.ReadAllBytes(Path.Combine(p,"ClassicVisuals.seqbin")),
                deployment.CopyShipPool(),deployment.CopyFleetPool(),0,731,PracticeDefenseTables.Load(p),PracticeDestructionTables.Load(p),
                PracticeFiringRangeTable.Load(p),PracticeVisibility.Load(p),PracticeFiringRateTable.Load(p));
        }
        // Original292 context023: deployment143514006, RNG731, two supplied
        // eastbound routes. Whole-pool source hashes are independent fixtures.
        static PracticeWorldStudyState WorldFixture(string assets) {
            string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            var deployment=PracticeDeployment.FirstPractice(PracticeUnitCatalog.Load(p),File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN")),143514006u);
            var combat=FiringFixture(assets);byte[] fleets=combat.Objects.CopyFleetPool();
            foreach(int fleet in new[]{7,10}){int at=fleet*492;for(int n=0;n<5;n++)W16(fleets,at+40+n*2,-1);W16(fleets,at+40,BitConverter.ToInt16(fleets,at+12)+30);W16(fleets,at+50,BitConverter.ToInt16(fleets,at+14));}
            Require(Hash(fleets)=="CE287384954A28AA300A73814E2B056EBB36F00E73C8BA78BBCC9897F29DA79E","Original292 supplied routes");
            return PracticeWorldStudyState.ForStudy(combat.WithFleetPool(fleets),deployment,7);
        }
        // Prepared original300 cases1/3/7. These are explicit numeric input
        // pools, not a claim to recreate an original aircraft deployment.
        static byte[][] SecondaryTargetFixture(int mask,int queue) {
            var ships=new byte[402000];for(int n=0;n<1500;n++)W16(ships,n*268+4,n);
            var fleets=new byte[123000];for(int n=0;n<250;n++)W16(fleets,n*492+4,n);
            var orders=new byte[78000];for(int n=0;n<1500;n++){int at=n*52;BitConverter.GetBytes(99).CopyTo(orders,at);BitConverter.GetBytes(123456789+n).CopyTo(orders,at+4);BitConverter.GetBytes(0xdeadbeefu).CopyTo(orders,at+48);for(int p=0;p<10;p++){W16(orders,at+8+p*2,-1);W16(orders,at+28+p*2,-400-p);}}
            BitConverter.GetBytes(1).CopyTo(fleets,0);W16(fleets,12,100);W16(fleets,14,100);
            for(int n=0;n<6;n++){int u=7+n,at=u*268;BitConverter.GetBytes(1+n%4).CopyTo(ships,at);W16(ships,at+6,-2);W16(ships,at+12,249);W16(ships,at+14,6);BitConverter.GetBytes(0x4fc340+u*268).CopyTo(fleets,16+n*4);for(int e=0;e<24;e++)BitConverter.GetBytes(14).CopyTo(ships,at+100+e*4);int equipment=n%6*4+n%4;BitConverter.GetBytes(1+n%3).CopyTo(ships,at+100+equipment*4);if(equipment<23)BitConverter.GetBytes(1+(n+1)%3).CopyTo(ships,at+100+(equipment+1)*4);for(int p=0;p<10;p++)W16(orders,u*52+8+p*2,queue==2&&p<5?50+p:-1);}
            int[] targets={2,10,248},starts={20,30,40};for(int t=0;t<3;t++){int at=targets[t]*492;BitConverter.GetBytes(3).CopyTo(fleets,at);W16(fleets,at+6,1);W16(fleets,at+12,100+(t==0?7:t==1?1:0));W16(fleets,at+14,100);for(int n=0;n<6;n++){int u=starts[t]+n,p=u*268;BitConverter.GetBytes(1).CopyTo(ships,p);W16(ships,p+8,n+1);W16(ships,p+10,n*10+4);W16(ships,p+14,1+n%6);BitConverter.GetBytes(t==0&&mask==2?0:0x4fc340+u*268).CopyTo(fleets,at+16+n*4);}}
            return new[]{ships,fleets,orders};
        }
        // Supplied original301 case0/4050 input: six template8 members and
        // a copied associated unit. This is not natural aircraft allocation.
        static byte[][] AircraftFixture(string assets,int kind) {
            string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var catalog=PracticeUnitCatalog.Load(p);var initial=FiringFixture(assets);var ships=initial.Objects.CopyShipPool();var fleets=initial.Objects.CopyFleetPool();var orders=new byte[78000];
            Array.Copy(catalog.Templates,5*268,ships,7*268,268);W16(ships,7*268+4,7);W16(ships,7*268+6,0);W16(ships,7*268+12,7);
            for(int n=0;n<492;n++)fleets[n]=(byte)(n*7+19);BitConverter.GetBytes(kind).CopyTo(fleets,0);W16(fleets,4,0);W16(fleets,6,0);W16(fleets,8,7);W16(fleets,12,-123);W16(fleets,14,456);W16(fleets,60,111);BitConverter.GetBytes(123).CopyTo(fleets,472);BitConverter.GetBytes(77).CopyTo(fleets,476);fleets[486]=13;
            for(int n=0;n<6;n++){int slot=20+n,at=slot*268;Array.Copy(catalog.Templates,8*268,ships,at,268);W16(ships,at+4,slot);W16(ships,at+6,0);W16(ships,at+8,4);W16(ships,at+10,4);W16(ships,at+12,0);BitConverter.GetBytes(0x4fc340+slot*268).CopyTo(fleets,16+n*4);}
            for(int n=0;n<5;n++){W16(fleets,40+n*2,100+n*10);W16(fleets,50+n*2,-200-n);}for(int source=0;source<1500;source++){int at=source*52;BitConverter.GetBytes(17).CopyTo(orders,at);BitConverter.GetBytes(123456789+source).CopyTo(orders,at+4);BitConverter.GetBytes(0xdeadbeefu).CopyTo(orders,at+48);for(int n=0;n<10;n++){W16(orders,at+8+n*2,1+n);W16(orders,at+28+n*2,-100-n);}}
            return new[]{ships,fleets,orders};
        }
        // Explicit whole-factory302 case0/1323 inputs, not a natural scenario.
        static byte[][] AircraftFactoryFixture(string assets,int prefix) {
            var catalog=PracticeUnitCatalog.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs"));var ships=new byte[402000];var fleets=new byte[123000];for(int n=0;n<1500;n++)W16(ships,n*268+4,n);for(int n=0;n<250;n++)PracticeFleetSnapshot.InitializeRootFleet(fleets,n*492,n);
            for(int n=0;n<prefix;n++){int p=n*268;BitConverter.GetBytes(1).CopyTo(ships,p);W16(ships,p+14,1);W16(ships,p+72,10);W16(ships,p+8,2);W16(ships,p+10,2);}Array.Copy(catalog.Templates,5*268,ships,7*268,268);W16(ships,7*268+4,7);W16(ships,7*268+6,0);W16(ships,7*268+12,7);BitConverter.GetBytes(1).CopyTo(fleets,7*492);W16(fleets,7*492+6,0);W16(fleets,7*492+12,-32768);W16(fleets,7*492+14,32767);return new[]{ships,fleets};
        }
        static byte[][] AntiAircraftLaunchFixture(string assets,int stock,bool full) {
            string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var catalog=PracticeUnitCatalog.Load(p);var original=FiringFixture(assets);var ships=original.Objects.CopyShipPool();var fleets=original.Objects.CopyFleetPool();var orders=new byte[78000];var timed=new byte[21440];Array.Copy(catalog.Templates,268,ships,7*268,268);W16(ships,7*268+4,7);W16(ships,7*268+6,0);W16(ships,7*268+12,7);W16(ships,7*268+198,stock);BitConverter.GetBytes(3).CopyTo(fleets,13*492);W16(fleets,13*492+6,1);W16(fleets,13*492+12,360);W16(fleets,13*492+14,400);
            for(int u=0;u<1500;u++){int at=u*52;BitConverter.GetBytes(17).CopyTo(orders,at);BitConverter.GetBytes(123456789+u).CopyTo(orders,at+4);BitConverter.GetBytes(0xdeadbeefu).CopyTo(orders,at+48);for(int n=0;n<10;n++){W16(orders,at+8+n*2,n+1);W16(orders,at+28+n*2,-100-n);}}if(full)for(int n=0;n<80;n++){int at=n*268;for(int q=0;q<268;q++)timed[at+q]=(byte)(q*13+n*7+9);BitConverter.GetBytes(17).CopyTo(timed,at);BitConverter.GetBytes(0xdeadbeefu).CopyTo(timed,at+28);}return new[]{ships,fleets,orders,timed};
        }
        static byte[][] AircraftDefenseFixture(string assets,int weapon,int kind,int victim,int primary,int secondary,int profile,int stance) {
            var catalog=PracticeUnitCatalog.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs"));var ships=new byte[402000];var fleets=new byte[123000];var timed=new byte[21440];for(int n=0;n<1500;n++)W16(ships,n*268+4,n);for(int n=0;n<250;n++)W16(fleets,n*492+4,n);int p=victim*268,type=kind==3?10:kind==4?9:8;Array.Copy(catalog.Templates,type*268,ships,p,268);W16(ships,p+4,victim);W16(ships,p+6,1);W16(ships,p+12,13);BitConverter.GetBytes(primary).CopyTo(ships,p+76);BitConverter.GetBytes(secondary).CopyTo(ships,p+80);ships[p+88]=7;ships[p+89]=9;for(int n=0;n<24;n++)BitConverter.GetBytes(0).CopyTo(ships,p+100+n*4);if(profile>0)for(int n=0;n<(profile==1?1:3);n++)BitConverter.GetBytes(primary).CopyTo(ships,p+100+n*4);BitConverter.GetBytes(kind).CopyTo(fleets,13*492);fleets[13*492+485]=(byte)(profile==3?255:0);fleets[13*492+486]=(byte)stance;BitConverter.GetBytes(weapon).CopyTo(timed,0);W16(timed,12,13);W16(timed,16,1);return new[]{ships,fleets,timed};
        }
        static byte[][] AircraftDestructionFixture(string assets,int type) {
            string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var catalog=PracticeUnitCatalog.Load(p);var original=FiringFixture(assets);var ships=original.Objects.CopyShipPool();var fleets=original.Objects.CopyFleetPool();var effects=new byte[800];var scores=new byte[176];Array.Copy(catalog.Templates,5*268,ships,7*268,268);W16(ships,7*268+4,7);W16(ships,7*268+12,7);BitConverter.GetBytes(0x4b47a0+13*492).CopyTo(ships,7*268+56);int u=20*268,f=13*492;Array.Copy(catalog.Templates,type*268,ships,u,268);W16(ships,u+4,20);W16(ships,u+6,1);W16(ships,u+8,4);W16(ships,u+10,3);W16(ships,u+12,13);W16(ships,u+16,0);BitConverter.GetBytes(type==10?3:type==9?4:5).CopyTo(fleets,f);W16(fleets,f+6,1);W16(fleets,f+8,7);W16(fleets,f+10,0);for(int n=0;n<6;n++)BitConverter.GetBytes(0).CopyTo(fleets,f+16+n*4);BitConverter.GetBytes(0x4fc340+u).CopyTo(fleets,f+16);fleets[f+468]=0;W16(fleets,f+60,123);for(int n=0;n<100;n++)BitConverter.GetBytes(0).CopyTo(fleets,f+64+n*4);for(int n=0;n<BitConverter.ToInt16(ships,u+14);n++)BitConverter.GetBytes(2).CopyTo(fleets,f+64+(43+n)*4);for(int n=0;n<800;n++)effects[n]=(byte)((n*17+9)%251);for(int n=0;n<40;n++)W16(effects,n*20,-1);for(int n=0;n<176;n++)scores[n]=(byte)(n*19+13);for(int n=0;n<4;n++)BitConverter.GetBytes(n%2==0?int.MaxValue-10:int.MinValue+10).CopyTo(scores,n*44);W16(scores,172,-2);return new[]{ships,fleets,effects,scores};
        }
        static byte[][] AircraftImpactFixture(string assets,int group,int weapon) {
            string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var catalog=PracticeUnitCatalog.Load(p);var deployment=PracticeDeployment.FirstPractice(catalog,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN")),143514006u);var state=new PracticeAircraftStudyState(catalog,PracticeAircraftTables.Load(p),deployment).Create(7,6+group/5,0).Transition(1+group%5);var ships=state.CopyShipPool();var fleets=state.CopyFleetPool();var timed=new byte[21440];var effects=new byte[800];var scores=new byte[176];Array.Copy(ships,14*268,timed,0,268);BitConverter.GetBytes(weapon).CopyTo(timed,0);W16(timed,4,0);W16(timed,6,1);W16(timed,12,13);W16(timed,16,14);W16(timed,18,BitConverter.ToInt16(ships,19*268+8));W16(timed,20,BitConverter.ToInt16(ships,19*268+10));BitConverter.GetBytes(12345).CopyTo(timed,24);BitConverter.GetBytes(0x4b47a0+13*492).CopyTo(timed,28);for(int n=0;n<800;n++)effects[n]=(byte)((n*17+9)%251);for(int n=0;n<40;n++)W16(effects,n*20,-1);for(int n=0;n<176;n++)scores[n]=(byte)(n*19+13);for(int n=0;n<4;n++)BitConverter.GetBytes(n%2==0?int.MaxValue-10:int.MinValue+10).CopyTo(scores,n*44);W16(scores,172,-2);return new[]{timed,ships,fleets,effects,scores};
        }
        // Independent original311 cases0/90/110 supplied-state inputs.
        static byte[][] AircraftSecondaryFixture(string assets,int stock){string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var units=PracticeUnitCatalog.Load(programs);var deployment=PracticeDeployment.FirstPractice(units,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN")),143514006u);var aircraft=new PracticeAircraftStudyState(units,PracticeAircraftTables.Load(programs),deployment).Create(7,6,0).Transition(1);var s=aircraft.CopyShipPool();var f=aircraft.CopyFleetPool();var p=new byte[78000];for(int u=0;u<1500;u++){Put32(p,u*52,17);Put32(p,u*52+4,123456+u);Put32(p,u*52+48,unchecked((int)0xdeadbeef));for(int n=0;n<10;n++){W16(p,u*52+8+n*2,-1);W16(p,u*52+28+n*2,-100-n);}}var q=(byte[])p.Clone();Put32(s,7*268+56,0);Put32(s,14*268+56,0x4b47a0+13*492);W16(f,13*492+6,1);W16(f,13*492+8,14);for(int n=19;n<=22;n++)W16(s,n*268+6,1);for(int n=0;n<24;n++)Put32(s,7*268+100+n*4,0);Put32(s,7*268+100,1);W16(s,7*268+198,stock);W16(f,13*492+12,BitConverter.ToInt16(f,7*492+12)+1);W16(f,13*492+14,BitConverter.ToInt16(f,7*492+14));for(int n=464;n<=466;n++)f[7*492+n]=255;for(int n=0;n<10;n++){W16(p,7*52+8+n*2,10+n);W16(p,7*52+28+n*2,20+n);}Put32(p,7*52+48,0x4b47a0+13*492);Put32(q,7*52,1);Put32(q,7*52+4,999);Put32(q,7*52+48,0x4b47a0+13*492);W16(q,7*52+8,3);W16(q,7*52+28,1);return new[]{s,f,p,q,new byte[21440]};}

        static void Put32(byte[] bytes,int at,int value){BitConverter.GetBytes(value).CopyTo(bytes,at);}
        static PracticeTimedStudyState AircraftTimedFixture(string assets,uint clock) {
            string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var units=PracticeUnitCatalog.Load(p);var deployment=PracticeDeployment.FirstPractice(units,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN")),143514006u);var aircraft=new PracticeAircraftStudyState(units,PracticeAircraftTables.Load(p),deployment).Create(7,6,0).Transition(1);var ships=aircraft.CopyShipPool();W16(ships,14*268+198,2);Require(Hash(ships)=="9B1D69D70FE817F9E4DFE0E8EF54B432269F889A25DF96B333D1F3211B240BAE","Original308 history0 supplied source stock");return PracticeTimedStudyState.ForAircraft(PracticeTimedWeaponCatalog.Load(p),units,File.ReadAllBytes(Path.Combine(p,"ClassicVisuals.seqbin")),ships,aircraft.CopyFleetPool(),clock,731,PracticeDefenseTables.Load(p),PracticeAircraftDestructionTable.Load(p));
        }
        // Independent source289 case0/1080 inputs: timed, ships, fleet,
        // effects, scores. Their original output hashes are pinned below.
        static byte[][] RetiredFixture(string assets,int weapon,int cell) {
            byte[] ships=FiringFixture(assets).Objects.CopyShipPool(),timed=new byte[21440],fleets=new byte[123000],effects=new byte[800],scores=new byte[176];
            Array.Copy(ships,7*268,timed,0,268);BitConverter.GetBytes(weapon).CopyTo(timed,0);W16(timed,4,0);W16(timed,12,0);W16(timed,18,2);W16(timed,20,3);BitConverter.GetBytes(0x4b47a0).CopyTo(timed,28);
            W16(fleets,4,0);W16(fleets,6,1);W16(fleets,12,100);W16(fleets,14,100);BitConverter.GetBytes(cell).CopyTo(fleets,64+23*4);
            for(int n=0;n<800;n++)effects[n]=(byte)((n*17+9)%251);for(int n=0;n<40;n++)W16(effects,n*20,-1);
            for(int n=0;n<176;n++)scores[n]=(byte)(n*31+7);W16(scores,172,-2);BitConverter.GetBytes(0).CopyTo(scores,168);
            return new[]{timed,ships,fleets,effects,scores};
        }
        static void Require(bool condition,string message) {
            if(!condition)throw new Exception(message);
        }
        static void Reject(Action action) {
            bool rejected=false;
            try {
                action();
            }
            catch(InvalidDataException) {
                rejected=true;
            }
            Require(rejected,"Malformed input was accepted");
        }
        static void RejectArgument(Action action) {
            bool rejected=false;
            try { action(); }
            catch(ArgumentOutOfRangeException) { rejected=true; }
            Require(rejected,"Out-of-range argument was accepted");
        }
        static void W16(byte[] b,int p,int value) {
            b[p]=(byte)value;
            b[p+1]=(byte)(value>>8);
        }
        static byte[] PcxFixture() {
            byte[] b=new byte[901];
            b[0]=10;
            b[1]=5;
            b[2]=1;
            b[3]=8;
            W16(b,8,1);
            W16(b,10,1);
            b[65]=1;
            W16(b,66,2);
            for(int i=0; i<4; i++)b[128+i]=(byte)(i+1);
            b[132]=12;
            b[133+3]=255;
            b[133+7]=255;
            return b;
        }
        static byte[] CueWaveFixture(){
            byte[] b=new byte[48];Encoding.ASCII.GetBytes("RIFF").CopyTo(b,0);BitConverter.GetBytes(40).CopyTo(b,4);Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(b,8);BitConverter.GetBytes(16).CopyTo(b,16);W16(b,20,1);W16(b,22,1);BitConverter.GetBytes(11025).CopyTo(b,24);BitConverter.GetBytes(11025).CopyTo(b,28);W16(b,32,1);W16(b,34,8);Encoding.ASCII.GetBytes("data").CopyTo(b,36);BitConverter.GetBytes(3).CopyTo(b,40);b[44]=0;b[45]=128;b[46]=255;return b;
        }
        public static int Run(string assets,string report) {
            var output=new List<string>();
            int passed=0,failed=0,skipped=0;
            Action<string,Action> check=delegate(string name,Action action) {
                try {
                    action();
                    passed++;
                    output.Add("PASS "+name);
                }
                catch(Exception e) {
                    failed++;
                    output.Add("FAIL "+name+": "+e.Message);
                }
            };
            // Private original-code comparison fixtures are optional for a
            // public checkout. Explicitly named fixture groups skip only when
            // the entire directory is unavailable; a present but incomplete
            // or altered directory still fails its normal checks.
            bool hasPrivateEvidence=Directory.Exists(Path.Combine(Path.GetDirectoryName(assets),"Evidence"));
            Action<string,Action> checkEvidence=delegate(string name,Action action) {
                if(!hasPrivateEvidence){skipped++;output.Add("SKIP "+name+": optional private Evidence directory unavailable.");}
                else check(name,action);
            };
            check("Original frame clock preserves pre-wait sample, prior delta and observed zero reset",delegate {
                // Values from source probes234/236 and the two observed zero
                // exits in actual234. Expected values are recorded boundaries.
                var before=new OriginalFrameClock(99,100,731,17);
                var body=before.BeginFrame(100,0);var after=body.CompleteFrame();
                Require(before.PreviousMilliseconds==99&&before.CurrentMilliseconds==100&&before.GameMilliseconds==731&&before.DeltaMilliseconds==17,"Clock input mutated");
                Require(body.PreviousMilliseconds==100&&body.CurrentMilliseconds==100&&body.GameMilliseconds==100&&body.DeltaMilliseconds==17,"Clock callback ordering");
                Require(after.PreviousMilliseconds==100&&after.CurrentMilliseconds==1&&after.GameMilliseconds==1&&after.DeltaMilliseconds==1,"Original zero reset");
                var next=after.BeginFrame(179270175,0).CompleteFrame();
                Require(next.DeltaMilliseconds==179270174,"Clock reset lost following uptime spike");
                var wrapped=new OriginalFrameClock(0,uint.MaxValue,731,17).BeginFrame(0,2);
                Require(wrapped.GameMilliseconds==731&&wrapped.DeltaMilliseconds==17&&wrapped.CompleteFrame().DeltaMilliseconds==1,"Network clock or wrapped subtraction");
                var zeroNetwork=before.BeginFrame(100,2).CompleteFrame();
                Require(zeroNetwork.GameMilliseconds==1,"Network zero branch failed to reset game clock");
                var negativeMode=before.BeginFrame(101,-1);Require(negativeMode.GameMilliseconds==101,"Network comparison was unsigned");
                var wait=new OriginalFrameClock(0,0xfffffff0,0,0).BeginFrame(0xfffffff0,0);
                Require(!wait.WorldWaitSatisfied(23)&&wait.WorldWaitSatisfied(24)&&!wait.WorldWaitSatisfied(0x7ffffff0),"Signed wait wrap/threshold");
                Require(body.WorldWaitSatisfied(140)&&!body.WorldWaitSatisfied(139)&&!body.WorldWaitSatisfied(99),"Wait predicate uses wrong timestamp");
            });
            check("Original paused deadlines preserve active flags, sentinel, wrap and opaque bytes",delegate {
                var timed=new byte[OriginalPauseTimers.TimedRecordBytes];var ships=new byte[OriginalPauseTimers.ShipTimerBytes];
                for(int i=0;i<1500;i++)W16(ships,i*52+8,-1);
                BitConverter.GetBytes(1).CopyTo(timed,0);BitConverter.GetBytes(0xfffffffeu).CopyTo(timed,24);timed[25+268]=17;
                BitConverter.GetBytes(uint.MaxValue).CopyTo(timed,79*268);BitConverter.GetBytes(1).CopyTo(timed,79*268+24);timed[79*268+267]=93;
                W16(ships,8,0);BitConverter.GetBytes(uint.MaxValue).CopyTo(ships,4);W16(ships,1499*52+8,1499);BitConverter.GetBytes(0x80000000u).CopyTo(ships,1499*52+4);
                var four=new byte[16];BitConverter.GetBytes(uint.MaxValue).CopyTo(four,12);var sixteen=new byte[64];BitConverter.GetBytes(uint.MaxValue).CopyTo(sixteen,0);BitConverter.GetBytes(0xfffffffeu).CopyTo(sixteen,60);
                var state=new OriginalPauseTimers(timed,ships,four,sixteen,uint.MaxValue);string before=Hash(timed)+Hash(ships);var next=state.AdvancePausedFrame(-1,40);
                byte[] t=next.CopyTimedRecords(),s=next.CopyShipTimers(),c=next.CopyFourCounters(),times=next.CopyTimestamps();
                Require(BitConverter.ToUInt32(t,24)==38&&BitConverter.ToUInt32(t,79*268+24)==41&&t[25+268]==17&&t[79*268+267]==93,"Paused timed record boundary/opaque bytes");
                Require(BitConverter.ToUInt32(s,4)==39&&BitConverter.ToUInt32(s,1499*52+4)==0x80000028u&&BitConverter.ToUInt32(s,52+4)==0,"Paused active ship timer boundary");
                Require(BitConverter.ToUInt32(c,0)==40&&BitConverter.ToUInt32(c,12)==39&&BitConverter.ToUInt32(times,0)==uint.MaxValue&&BitConverter.ToUInt32(times,4)==40&&BitConverter.ToUInt32(times,60)==38&&next.GlobalCounter==39,"Paused counter/sentinel arithmetic");
                Require(before==Hash(state.CopyTimedRecords())+Hash(state.CopyShipTimers())&&Object.ReferenceEquals(state,state.AdvancePausedFrame(0,40))&&Object.ReferenceEquals(state,state.AdvancePausedFrame(1,0)),"Pause input/no-op preservation");
                timed[0]=0;ships[0]=91;four[0]=91;sixteen[0]=91;t[0]=0;s[0]=17;c[0]=17;times[0]=17;
                Require(state.CopyTimedRecords()[0]==1&&state.CopyShipTimers()[0]==0&&state.CopyFourCounters()[0]==0&&state.CopyTimestamps()[0]==255&&next.CopyTimedRecords()[0]==1&&next.CopyShipTimers()[0]==0&&next.CopyFourCounters()[0]==40&&next.CopyTimestamps()[0]==255,"Pause buffer ownership");
                Reject(delegate{new OriginalPauseTimers(null,ships,four,sixteen,0);});Reject(delegate{new OriginalPauseTimers(new byte[1],ships,four,sixteen,0);});
                Reject(delegate{new OriginalPauseTimers(state.CopyTimedRecords(),new byte[1],four,sixteen,0);});Reject(delegate{new OriginalPauseTimers(state.CopyTimedRecords(),ships,new byte[1],sixteen,0);});Reject(delegate{new OriginalPauseTimers(state.CopyTimedRecords(),ships,four,new byte[1],0);});
            });
            check("Timed motion preserves original signed rounding, grouped marks and whole-pass early return",delegate {
                var timed=new byte[PracticeTimedMotionState.TimedBytes];var fleets=new byte[PracticeTimedMotionState.FleetBytes];
                BitConverter.GetBytes(2).CopyTo(fleets,5*492);W16(fleets,5*492+12,317);W16(fleets,5*492+14,189);
                for(int i=0;i<250;i++)fleets[i*492+483]=17;
                foreach(int slot in new[]{0,17,79}){int at=slot*268;BitConverter.GetBytes(14).CopyTo(timed,at);W16(timed,at+8,300);W16(timed,at+10,200);W16(timed,at+12,5);BitConverter.GetBytes((uint)(slot==17?139:180)).CopyTo(timed,at+24);}
                var initial=new PracticeTimedMotionState(timed,fleets);string before=Hash(timed)+Hash(fleets);var result=initial.Advance(100,40);byte[] t=result.State.CopyTimedPool(),f=result.State.CopyFleetPool();
                Require(result.InterpolatedRecords==1&&result.StoppedAtRecord==17&&BitConverter.ToInt16(t,8)==309&&BitConverter.ToInt16(t,10)==194&&BitConverter.ToInt16(t,79*268+8)==300,"Timed signed half or early return");
                for(int i=0;i<250;i++)Require(f[i*492+483]==(i==4?1:0),"Timed grouped mark/whole-pool reset");
                Require(before==Hash(initial.CopyTimedPool())+Hash(initial.CopyFleetPool()),"Timed input mutated");
                foreach(uint delta in new[]{0u,0x80000000u,uint.MaxValue}){bool rejected=false;try{initial.Advance(100,delta);}catch(NotSupportedException){rejected=true;}Require(rejected&&before==Hash(initial.CopyTimedPool())+Hash(initial.CopyFleetPool()),"Unsupported timed delta changed state");}
                bool expired=false;try{initial.Advance(180,40);}catch(NotSupportedException){expired=true;}Require(expired&&before==Hash(initial.CopyTimedPool())+Hash(initial.CopyFleetPool()),"Timed expiry silently skipped");
                W16(timed,12,250);Reject(delegate{new PracticeTimedMotionState(timed,fleets).Advance(100,40);});
                Reject(delegate{new PracticeTimedMotionState(null,fleets);});Reject(delegate{new PracticeTimedMotionState(new byte[1],fleets);});Reject(delegate{new PracticeTimedMotionState(initial.CopyTimedPool(),new byte[1]);});
                t[0]=0;f[4*492+483]=0;timed[0]=0;fleets[0]=13;Require(result.State.CopyTimedPool()[0]==14&&result.State.CopyFleetPool()[4*492+483]==1&&initial.CopyTimedPool()[0]==14&&initial.CopyFleetPool()[0]==0,"Timed buffer ownership");
                var idle=new PracticeTimedMotionState(new byte[PracticeTimedMotionState.TimedBytes],fleets).Advance(0,0);Require(idle.InterpolatedRecords==0&&idle.StoppedAtRecord==-1,"Empty timed pool should not divide");
            });
            check("Timed creation imports source speed table and preserves ammunition, identity and duration order",delegate {
                string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");byte[] table=File.ReadAllBytes(Path.Combine(programs,"TimedWeaponSpeed.seqbin"));var catalog=new PracticeTimedWeaponCatalog(table);
                Require(catalog.SourceSpeed(14)==20&&catalog.SourceSpeed(17)==40&&catalog.SourceSpeed(26)==22,"Original timed speed words");table[28]=0;Require(catalog.SourceSpeed(14)==20,"Weapon table aliases input");Reject(delegate{new PracticeTimedWeaponCatalog(table);});Reject(delegate{new PracticeTimedWeaponCatalog(new byte[1]);});RejectArgument(delegate{catalog.SourceSpeed(13);});RejectArgument(delegate{catalog.SourceSpeed(31);});
                var ships=new byte[402000];var fleets=new byte[123000];for(int i=0;i<250;i++)W16(fleets,i*492+4,i);W16(ships,8,7);W16(ships,10,9);W16(ships,196+14*2,2);W16(fleets,12,300);W16(fleets,14,200);W16(fleets,492+12,100);W16(fleets,492+14,100);ships[267]=93;
                var before=new PracticeTimedCreationState(catalog,new byte[21440],ships,fleets,-1);string saved=Hash(ships);var update=before.Create(14,0,1,0,0,100);byte[] t=update.State.CopyTimedPool();
                Require(update.Outcome==PracticeTimedCreationOutcome.Created&&update.CreatedSlot==0&&update.State.LastTargetFleet==1&&BitConverter.ToInt16(t,8)==307&&BitConverter.ToInt16(t,10)==209&&BitConverter.ToInt16(t,12)==1&&BitConverter.ToInt16(t,196+14*2)==1&&t[267]==93,"Created record copy/ammo/world identity");
                Require(BitConverter.ToUInt32(t,24)==23400&&BitConverter.ToUInt32(t,28)==0x4B498Cu,"Truncated distance before scale or target token");
                var precision=before.Create(14,0,1,122,-95,100);Require(BitConverter.ToUInt32(precision.State.CopyTimedPool(),24)==22100,"Constructor lost original CRT distance precision");
                Require(saved==Hash(before.CopyShipPool())&&before.LastTargetFleet==-1,"Creation changed input");
                string after=Hash(update.State.CopyShipPool());bool depleted=false;try{update.State.Create(14,0,1,0,0,100);}catch(NotSupportedException){depleted=true;}Require(depleted&&after==Hash(update.State.CopyShipPool()),"Depletion callback silently skipped");
                W16(ships,196+14*2,0);var empty=new PracticeTimedCreationState(catalog,new byte[21440],ships,fleets,7);var noAmmo=empty.Create(14,0,1,0,0,100);Require(noAmmo.Outcome==PracticeTimedCreationOutcome.NoAmmunition&&noAmmo.CreatedSlot==-1&&noAmmo.State.LastTargetFleet==7&&Object.ReferenceEquals(empty,noAmmo.State),"Empty ammo changed target state");
                W16(ships,196+14*2,-1);var infinite=new PracticeTimedCreationState(catalog,new byte[21440],ships,fleets,-1).Create(14,0,1,207,109,uint.MaxValue);Require(BitConverter.ToInt16(infinite.State.CopyShipPool(),196+14*2)==-1&&BitConverter.ToUInt32(infinite.State.CopyTimedPool(),24)==uint.MaxValue,"Infinite ammo/zero-distance clock");
                var full=new byte[21440];for(int i=0;i<80;i++)BitConverter.GetBytes(14).CopyTo(full,i*268);var capacity=new PracticeTimedCreationState(catalog,full,ships,fleets,7);Require(capacity.Create(14,0,1,0,0,100).Outcome==PracticeTimedCreationOutcome.PoolFull,"Timed pool capacity");
                Reject(delegate{new PracticeTimedCreationState(catalog,new byte[1],ships,fleets,-1);});RejectArgument(delegate{before.Create(14,-1,1,0,0,100);});RejectArgument(delegate{before.Create(14,0,250,0,0,100);});RejectArgument(delegate{before.Create(14,0,1,32768,0,100);});
            });
            check("Original timed distance retains CRT register/store precision before truncation",delegate {
                Require(OriginalTimedDistance.Truncated(0,0)==0&&OriginalTimedDistance.Truncated(3,4)==5&&OriginalTimedDistance.Truncated(-3,-4)==5,"Timed distance axes/sign");
                Require(OriginalTimedDistance.Truncated(85,204)==220&&OriginalTimedDistance.Truncated(-85,204)==220&&OriginalTimedDistance.Truncated(27,120)==122&&OriginalTimedDistance.Truncated(165,52)==173&&OriginalTimedDistance.Truncated(1615,2856)==3280,"Original scaled CRT precision boundaries");
                Require(OriginalTimedDistance.Truncated(98303,0)==98303&&OriginalTimedDistance.Truncated(0,-98303)==98303,"Timed coordinate range");
                RejectArgument(delegate{OriginalTimedDistance.Truncated(98304,0);});RejectArgument(delegate{OriginalTimedDistance.Truncated(0,-98304);});
            });
            check("Original fleet batch boundaries, cycle wrap and world caller enumeration",delegate {
                var start=new PracticeFleetScheduler(248,uint.MaxValue);
                var last=start.TakeBatch();int[] slots=last.CopySlots();
                Require(slots.Length==2&&slots[0]==248&&slots[1]==249&&last.Next.BatchStart==0&&last.Next.CompletedCycles==0,"Original final batch/cycle wrap");
                slots[0]=-1;Require(last.CopySlots()[0]==248&&start.BatchStart==248&&start.CompletedCycles==uint.MaxValue,"Scheduler ownership");
                var normal=new PracticeFleetScheduler(244,731).TakeWorldPoll(false);
                Require(normal.BatchCount==2&&normal.CopySlots().Length==6&&normal.Next.BatchStart==0&&normal.Next.CompletedCycles==732,"Original normal two batches");
                var extra=new PracticeFleetScheduler(0,0).TakeWorldPoll(true);
                Require(extra.BatchCount==65&&extra.CopySlots().Length==258&&extra.Next.BatchStart==8&&extra.Next.CompletedCycles==1,"Original additional63batches");
                var cursor=new PracticeFleetScheduler(0,0);int visited=0;
                for(int n=0;n<63;n++){var batch=cursor.TakeBatch();visited+=batch.CopySlots().Length;cursor=batch.Next;}
                Require(visited==250&&cursor.BatchStart==0&&cursor.CompletedCycles==1,"Original63batch cycle");
                foreach(int invalid in new[]{-4,-1,1,2,3,249,250,252})RejectArgument(delegate{new PracticeFleetScheduler(invalid,0);});
            });
            check("Whole admitted fleet polls preserve source order, secondary ownership and atomic failure",delegate {
                var pool=new byte[250*492];
                for(int i=0;i<250;i++)W16(pool,i*492+4,i);
                foreach(int slot in new[]{7,8}){int at=slot*492;BitConverter.GetBytes(1).CopyTo(pool,at);W16(pool,at+12,slot==7?350:362);W16(pool,at+14,390);W16(pool,at+60,3);pool[at+485]=2;for(int i=0;i<5;i++)W16(pool,at+40+i*2,-1);W16(pool,at+40,slot==7?380:332);W16(pool,at+50,390);}
                var movement=new PracticeMovementState(pool,new byte[PracticeMovementState.TerrainBytes],new byte[PracticeMovementState.AuxiliaryBytes],7,-1);
                var secondary=new byte[PracticeFleetBatchState.SecondaryBytes];secondary[secondary.Length-1]=17;
                var initial=new PracticeFleetBatchState(movement,new PracticeFleetScheduler(4,0),secondary);
                secondary[secondary.Length-1]=0;
                var poll=initial.AdvanceNormalPoll();byte[] result=poll.State.Movement.CopyFleetPool();
                Require(poll.BatchCount==2&&poll.State.Schedule.BatchStart==12&&poll.FleetUpdates.Count==2&&
                    poll.State.Movement.Origin(7)==new Point(352,390)&&poll.State.Movement.Origin(8)==new Point(362,390)&&
                    result[8*492+482]==7&&result[4*492+482]==255&&result[482]==0,"Source sequential bodies/preamble");
                Require(initial.Movement.Origin(7)==new Point(350,390)&&initial.Schedule.BatchStart==4&&
                    initial.CopySecondaryPool()[secondary.Length-1]==17&&poll.State.CopySecondaryPool()[secondary.Length-1]==17,"Batch input/secondary ownership");
                var copy=poll.State.CopySecondaryPool();copy[0]=1;Require(poll.State.CopySecondaryPool()[0]==0,"Secondary copy aliases state");
                bool readOnly=false;try{poll.FleetUpdates.Clear();}catch(NotSupportedException){readOnly=true;}Require(readOnly,"Mutable batch update list");
                var unsupported=(byte[])pool.Clone();BitConverter.GetBytes(3).CopyTo(unsupported,8*492);
                var state=new PracticeFleetBatchState(new PracticeMovementState(unsupported,movement.CopyTerrainPool(),movement.CopyAuxiliary(),7,-1),new PracticeFleetScheduler(4,731),new byte[PracticeFleetBatchState.SecondaryBytes]);
                string before=Hash(state.Movement.CopyFleetPool());bool rejected=false;try{state.AdvanceNormalPoll();}catch(NotSupportedException){rejected=true;}
                Require(rejected&&Hash(state.Movement.CopyFleetPool())==before&&state.Schedule.BatchStart==4&&state.Schedule.CompletedCycles==731,"Unsupported second batch published first batch");
                secondary[0]=1;rejected=false;try{new PracticeFleetBatchState(movement,initial.Schedule,secondary);}catch(NotSupportedException){rejected=true;}Require(rejected,"Active secondary accepted");
                Reject(delegate{new PracticeFleetBatchState(movement,initial.Schedule,new byte[1]);});
            });
            check("Fleet movement rejects unreconstructed field476 modes atomically",delegate {
                var pool=new byte[250*492];for(int i=0;i<250;i++)W16(pool,i*492+4,i);
                int at=7*492;BitConverter.GetBytes(1).CopyTo(pool,at);W16(pool,at+12,320);W16(pool,at+14,320);W16(pool,at+40,350);W16(pool,at+50,320);W16(pool,at+60,3);pool[at+485]=2;
                foreach(int task in new[]{3,5}){BitConverter.GetBytes(task).CopyTo(pool,at+476);var state=new PracticeMovementState(pool,new byte[PracticeMovementState.TerrainBytes],new byte[PracticeMovementState.AuxiliaryBytes],7,-1);string before=Hash(state.CopyFleetPool());bool rejected=false;try{state.Step(7);}catch(NotSupportedException){rejected=true;}Require(rejected&&Hash(state.CopyFleetPool())==before,"Unsupported follow task changed input");}
            });
            check("Battle checkpoint preserves manual deployments, repeated shuffles and selected shooter",delegate {
                foreach(bool movies in new[]{false,true}) {
                    var game=new Game(731,RuleProfile.Recovered,OpponentKind.Original,2,movies);
                    game.AutoPlace();game.AutoPlace();game.AutoPlace();
                    bool moved=false;for(int y=2;y<8&&!moved;y++)for(int x=0;x<10&&!moved;x++)moved=game.MovePlacedShip(0,new Point(x,y));
                    Require(moved&&game.Start()&&game.SelectShooter(3),"Checkpoint deployment fixture");
                    byte[] bytes=game.SerializeBattle();Game restored=Game.ParseBattle(bytes);
                    Require(restored.Paused&&restored.Turn==1&&restored.PlayerShooter==3&&restored.MovieTurns==movies&&Hash(bytes)==Hash(restored.SerializeBattle()),"Initial checkpoint state");
                    var restart=restored.RestartBattle();Require(restart!=null&&restart.PlayerShooter==1&&restart.PlacementState==game.PlacementState&&restart.PlacementCalls==game.PlacementCalls,"Loaded restart lost initial stream or selection");
                    for(int i=0;i<5;i++)Require(restart.Player.Ships[i].Cells[0]==game.Player.Ships[i].Cells[0]&&restart.Enemy.Ships[i].Cells[0]==game.Enemy.Ships[i].Cells[0],"Loaded restart lost deployed fleets");
                }
            });
            check("Saved Classic battles reproduce future targeting, movies, sinks and terminal outcomes",delegate {
                foreach(bool movies in new[]{false,true})for(int level=0;level<4;level++) {
                    var game=new Game(1741+level,RuleProfile.Recovered,OpponentKind.Original,level,movies);game.AutoPlace();game.Start();
                    Point first=game.Enemy.Ships[0].Cells[0];Require(game.Fire(first),"Checkpoint first shot");DrainMovies(game);Require(game.AdvanceOpponent(),"Checkpoint first opponent");DrainMovies(game);
                    var restored=Game.ParseBattle(game.SerializeBattle());restored.Paused=false;int turns=0;
                    while(game.Phase!=Phase.Finished) {
                        Point target=new Point(-1,-1);foreach(var ship in game.Enemy.Ships)foreach(Point cell in ship.Cells)if(!game.Enemy.Shots[cell.X,cell.Y]&&target.X<0)target=cell;
                        Require(target.X>=0&&++turns<100,"Checkpoint complete-battle bound");
                        int shooter=game.PlayerShooter;Require(game.SelectShooter(shooter)&&restored.SelectShooter(shooter)&&game.Fire(target)&&restored.Fire(target),"Resumed player action");
                        MatchSavedMovies(game,restored);
                        if(game.Phase!=Phase.Finished){Require(game.AdvanceOpponent()&&restored.AdvanceOpponent(),"Resumed opponent action");MatchSavedMovies(game,restored);}
                        Require(game.Phase==restored.Phase&&game.Turn==restored.Turn&&GameFingerprint(game)==GameFingerprint(restored),"Resumed future history diverged");
                        if(game.CanSaveBattle)Require(Hash(game.SerializeBattle())==Hash(restored.SerializeBattle()),"Resumed proof or history changed");
                    }
                    Require(!game.CanSaveBattle&&!restored.CanSaveBattle,"Finished battle offered checkpoint");
                }
            });
            check("Checkpoint eligibility excludes pending stages, opponent turns and prototype rules",delegate {
                var game=new Game(731,RuleProfile.Recovered,OpponentKind.Original,0,true);Reject(delegate{game.SerializeBattle();});game.AutoPlace();game.Start();
                Require(game.CanSaveBattle,"Ready turn cannot save");game.Paused=true;Require(game.CanSaveBattle,"Paused stable turn cannot save");game.Paused=false;
                Require(game.Fire(new Point(0,0)),"Pending save fixture");while(game.Pending!=null){Require(!game.CanSaveBattle,"Pending movie can save");Reject(delegate{game.SerializeBattle();});game.CompleteMovie(game.Pending,game.Pending.Stage);}
                Require(!game.CanSaveBattle,"Opponent turn can save");Reject(delegate{game.SerializeBattle();});
                foreach(var profile in new[]{RuleProfile.Recovered,RuleProfile.Legacy}){var prototype=new Game(731,profile,OpponentKind.Prototype,0);prototype.AutoPlace();prototype.Start();Require(!prototype.CanSaveBattle,"Prototype can save unsupported random state");Reject(delegate{prototype.SerializeBattle();});}
            });
            check("Checkpoint refuses unrecorded state mutation and ignores rejected firing attempts",delegate {
                var game=new Game(731);game.AutoPlace();game.Start();byte[] before=game.SerializeBattle();Require(!game.Fire(new Point(-1,-1))&&Hash(before)==Hash(game.SerializeBattle()),"Rejected fire altered checkpoint");
                game.Enemy.Shoot(game.Enemy.Ships[0].Cells[0]);Reject(delegate{game.SerializeBattle();});
            });
            check("Battle checkpoint validates checksum, version, sizes and replay semantics",delegate {
                var game=new Game(731);game.AutoPlace();game.Start();byte[] valid=game.SerializeBattle();string text=Encoding.ASCII.GetString(valid);
                var corrupt=(byte[])valid.Clone();corrupt[30]^=1;Reject(delegate{Game.ParseBattle(corrupt);});Reject(delegate{Game.ParseBattle(null);});Reject(delegate{Game.ParseBattle(new byte[8193]);});Reject(delegate{Game.ParseBattle(new byte[10]);});
                Reject(delegate{Game.ParseBattle(Encoding.ASCII.GetBytes(text.TrimEnd('\n')));});Reject(delegate{Game.ParseBattle(Encoding.ASCII.GetBytes(text.Replace("\n","\r\n")));});
                foreach(string altered in new[]{text.Replace("BATTLE 1","BATTLE 2"),text.Replace("difficulty=0","difficulty=4"),text.Replace("movies=0","movies=2"),text.Replace("selected=1","selected=5"),text.Replace("selected=1","selected=+1"),text.Replace("shots=\n","shots=1,0,0;1,0,0\n"),text.Replace("shots=\n","shots=1,-1,0\n"),text.Replace("proof=","proof=0"),text.Replace("enemy=","unexpected=")})Reject(delegate{Game.ParseBattle(ResignBattle(altered));});
                string[] lines=text.Split('\n');lines[5]="player=0,0;0,0;0,0;0,0;0,0";Reject(delegate{Game.ParseBattle(ResignBattle(string.Join("\n",lines)));});
                lines=text.Split('\n');lines[4]="placement=4294967296,0";Reject(delegate{Game.ParseBattle(ResignBattle(string.Join("\n",lines)));});lines[4]="placement=0,1000001";Reject(delegate{Game.ParseBattle(ResignBattle(string.Join("\n",lines)));});
            });
            check("Local checkpoint replaces atomically, retains one backup and preserves failures",delegate {
                string dir=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)),"checkpoint-io-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string path=Path.Combine(dir,"battle.obship");
                var game=new Game(731);game.AutoPlace();game.Start();game.SaveBattle(path);byte[] first=File.ReadAllBytes(path);Require(Game.LoadBattle(path).Paused&&!File.Exists(path+".new"),"Initial checkpoint file");
                Require(game.Fire(new Point(0,0))&&game.AdvanceOpponent(),"Checkpoint file turn");game.SaveBattle(path);byte[] second=File.ReadAllBytes(path);Require(Hash(first)==Hash(File.ReadAllBytes(path+".bak"))&&Hash(first)!=Hash(second),"Checkpoint previous backup");
                Require(game.Fire(new Point(1,0))&&game.AdvanceOpponent(),"Second checkpoint file turn");game.SaveBattle(path);Require(Hash(second)==Hash(File.ReadAllBytes(path+".bak"))&&!File.Exists(path+".new"),"Checkpoint backup replacement");
                string before=GameFingerprint(game);File.WriteAllBytes(path,new byte[8193]);Reject(delegate{Game.LoadBattle(path);});Require(GameFingerprint(game)==before&&Game.LoadBattle(path+".bak").Turn==2,"Corrupt load mutated live game or backup");
                bool writeFailed=false;try{game.SaveBattle(dir);}catch(IOException){writeFailed=true;}Require(writeFailed&&Directory.Exists(dir)&&!File.Exists(dir+".new"),"Failed atomic save damaged path or left temporary file");
            });
            check("Battle checkpoint canonical bytes remain stable across cultures and paused reloads",delegate {
                var game=new Game(int.MinValue,RuleProfile.Recovered,OpponentKind.Original,3,true);game.AutoPlace();game.Start();game.Fire(new Point(0,0));DrainMovies(game);game.AdvanceOpponent();DrainMovies(game);byte[] bytes=game.SerializeBattle();
                var prior=System.Threading.Thread.CurrentThread.CurrentCulture;try{System.Threading.Thread.CurrentThread.CurrentCulture=new System.Globalization.CultureInfo("fr-FR");var restored=Game.ParseBattle(bytes);Require(Hash(bytes)==Hash(restored.SerializeBattle()),"Culture changed checkpoint bytes");}finally{System.Threading.Thread.CurrentThread.CurrentCulture=prior;}
            });
            string visualTable=Path.Combine(Path.GetDirectoryName(assets),"Programs","ClassicVisuals.seqbin");
            check("Original finite visual table fields and unresolved-record preservation",delegate {
                byte[] b=File.ReadAllBytes(visualTable);var catalog=new ClassicVisualCatalog(b);
                Require(catalog.Get(7).SpriteBank==43&&catalog.Get(1).LastCounter==57&&catalog.Get(27).Resource=="RINGS.SPH","Native record mapping");
                byte[] copy=catalog.OriginalRecords;copy[0]^=1;Require(catalog.OriginalRecords[0]==b[0],"Catalog exposed owned bytes");
                var unknown=(byte[])b.Clone();unknown[7*20+4]^=1;Require(new ClassicVisualCatalog(unknown).OriginalRecords[7*20+4]==unknown[7*20+4],"Unknown field discarded");
                Reject(delegate{new ClassicVisualCatalog(new byte[579]);});
                foreach(int at in new[]{7*20,7*20+8,7*20+12}){var bad=(byte[])b.Clone();bad[at]^=1;Reject(delegate{new ClassicVisualCatalog(bad);});}
                Reject(delegate{catalog.Get(28);});
            });
            check("Finite visual first-free capacity, terminal tick and safe large advance",delegate {
                var catalog=new ClassicVisualCatalog(File.ReadAllBytes(visualTable));var timeline=new ClassicVisualTimeline();
                var ring=timeline.Spawn(catalog.Get(27),false,new Point(0,0),0);var ship=timeline.Spawn(catalog.Get(7),true,new Point(2,3),0);
                Require(ring.Slot==0&&ship.Slot==1&&!timeline.Advance(0),"Initial allocator state");
                timeline.Advance(32);Require(timeline.Count==2&&ring.Counter==32,"Empty terminal slot must remain active");
                timeline.Advance(1);Require(timeline.Count==1&&ship.Counter==33,"Strictly-greater retirement");
                Require(timeline.Spawn(catalog.Get(27),false,new Point(1,1),1).Slot==0,"First-free hole not reused");
                while(timeline.Count<40)timeline.Spawn(catalog.Get(1),false,new Point(),0);
                Require(timeline.Spawn(catalog.Get(1),false,new Point(),0)==null,"Capacity exceeded");
                timeline.Advance(long.MaxValue);Require(timeline.Count==0,"Large elapsed count overflowed");
            });
            check("Finite visual counter history is independent of frame partitions",delegate {
                var spec=new ClassicVisualCatalog(File.ReadAllBytes(visualTable)).Get(1);var a=new ClassicVisualTimeline();var b=new ClassicVisualTimeline();
                var x=a.Spawn(spec,false,new Point(),1);var y=b.Spawn(spec,false,new Point(),1);
                a.Advance(30);b.Advance(7);b.Advance(11);b.Advance(12);
                Require(x.Counter==31&&y.Counter==31&&a.Count==b.Count,"Counter partition drift");
                a.Clear();b.Clear();Require(a.Count==0&&b.Count==0,"Reset retained entries");
            });
            check("Native finite visual orientation-zero anchor fixtures",delegate {
                var c=new ClassicVisualCatalog(File.ReadAllBytes(visualTable));
                Require(ClassicVisualGeometry.Anchor(c.Get(7),false,new Point())==new Point(94,325),"Own carrier anchor");
                Require(ClassicVisualGeometry.Anchor(c.Get(7),true,new Point())==new Point(308,182),"Enemy carrier anchor");
                Require(ClassicVisualGeometry.Anchor(c.Get(27),true,new Point())==new Point(242,149),"Miss top-left anchor");
                Require(ClassicVisualGeometry.Anchor(c.Get(27),false,new Point())==new Point(27,292),"Own miss anchor");
            });
            check("Finite visual impact admission covers miss, hit, sink and repeats",delegate {
                var board=new Board();board.Place(new Point(2,2),2,true,"Destroyer",4);int type;Point at;
                Shot miss=board.Shoot(new Point(0,0));Require(ClassicVisualGeometry.Impact(board,miss,out type,out at)&&type==27&&at==miss.Cell,"Water entry");
                Shot hit=board.Shoot(new Point(2,2));Require(!ClassicVisualGeometry.Impact(board,hit,out type,out at),"Non-sinking hit spawned a sink");
                Require(!ClassicVisualGeometry.Impact(board,new Shot{Cell=hit.Cell,Repeated=true},out type,out at),"Repeated live-ship hit spawned a miss");
                Shot sink=board.Shoot(new Point(2,3));Require(ClassicVisualGeometry.Impact(board,sink,out type,out at)&&type==4&&at==new Point(2,2),"Sink source type/start");
                Require(ClassicVisualGeometry.Impact(board,new Shot{Cell=hit.Cell,Repeated=true},out type,out at)&&type==27,"Removed-ship repeat entry");
            });
            check("Original finite sprite banks include empty terminal frames and dispose safely",delegate {
                var palette=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette;var sprites=new ClassicVisualSprites(Path.Combine(assets,"Graphics"),File.ReadAllBytes(visualTable),palette);
                foreach(int type in new[]{7,1,2,12,4,27}){var spec=sprites.Catalog.Get(type);Require(sprites.HasFrame(type,0)&&sprites.HasFrame(type,spec.LastCounter-1)&&!sprites.HasFrame(type,spec.LastCounter),"SPH terminal/frame bounds");}
                Require(sprites.Bounds(7,60,new Point(300,200)).IsEmpty,"Terminal bounds are visible");
                sprites.Dispose();sprites.Dispose();bool rejected=false;try{sprites.HasFrame(7,0);}catch(ObjectDisposedException){rejected=true;}Require(rejected,"Disposed bank remained usable");
            });
            check("Independent PCX RLE/color fixture",delegate {
                var p=Assets.Pcx(PcxFixture());
                Require(p.Width==2&&p.Height==2&&p.Pixels[0]==1&&p.Pixels[3]==4,"Pixel geometry");
                Require(p.Palette[1].R==255&&p.Palette[2].G==255,"Palette channels");
            });
            check("PCX truncation, palette and RLE bounds",delegate {
                var p=PcxFixture();
                p[128]=197;
                p[129]=1;
                Reject(delegate {
                    Assets.Pcx(p);
                });
                p=PcxFixture();
                p[132]=0;
                Reject(delegate {
                    Assets.Pcx(p);
                });
                Reject(delegate {
                    Assets.Pcx(new byte[10]);
                });
            });
            check("Independent SPH skip/run and missing-frame fixture",delegate {
                byte[] b=new byte[25];
                W16(b,0,2);
                BitConverter.GetBytes(10).CopyTo(b,2);
                BitConverter.GetBytes(uint.MaxValue).CopyTo(b,6);
                W16(b,10,65535);
                W16(b,12,2);
                W16(b,14,3);
                W16(b,16,2);
                new byte[] {
                    1,2,5,6,0,1,7
                }
                .CopyTo(b,18);
                var f=Assets.Sph(b,new Color[256]);
                Require(f[1]==null&&f[0].OriginX==-1&&!f[0].Visible[0]&&f[0].Pixels[1]==5&&f[0].Pixels[3]==7,"Sprite geometry");
                b[18]=3;
                Reject(delegate {
                    Assets.Sph(b,new Color[256]);
                });
            });
            check("Independent SCN x-major grid and tail bounds",delegate {
                var b=new List<byte>();
                for(int x=0; x<64; x++)for(int y=0; y<64; y++) {
                    if(x==2&&y==3) {
                        b.Add(2);
                        b.Add(29);
                    }
                    else b.Add(1);
                }
                b.Add(2);
                b.AddRange(new byte[16]);
                var s=Scenario.Load(b.ToArray(),"fixture");
                Require(s.At(2,3).Kind==2&&s.At(2,3).Value==29&&s.At(3,2).Kind==1,"Grid orientation");
                b.RemoveAt(b.Count-1);
                Reject(delegate {
                    Scenario.Load(b.ToArray(),"truncated");
                });
            });
            check("Packaged original resource fixtures",delegate {
                var p=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));
                Require(p.Width==640&&p.Height==512,"Classic background dimensions");
                var f=Assets.Sph(Path.Combine(assets,"Graphics","BATTSHIP.SPH"),Assets.Palette(Path.Combine(assets,"Graphics","ISLAND.COL")));
                Require(f.Count==5&&f[0].Width==134&&f[0].Height==95&&f[4]==null,"Battleship sprite frame table");
                int[] expected= {
                    4342,4519,11052
                };
                string[] names= {
                    "BS201.SCN","BS202.SCN","BS204.SCN"
                };
                for(int i=0; i<3; i++) {
                    var s=Scenario.Load(Path.Combine(assets,"Graphics",names[i]));
                    Require(s.TerrainBytes==expected[i]&&s.Players==2,"Scenario fixture offset "+names[i]);
                }
            });
            check("Practice fleet constructor matches complete original ship and fleet pools",delegate {
                var catalog=PracticeUnitCatalog.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs"));
                string[] maps={"BS201.SCN","BS202.SCN","BS204.SCN"};int[] owners={0,1,0};
                string[] ships={"A27F5D7ADC2E3C12041E90641E689A96CEAA91A02F8F6E5D66B5249C26F8E425","7139E558709F95DE58AC4CFC118E71C91BFC93A06537DB1B21E12EBFA8783B48","DEE9ABC38D2FEAD06761CA892E57ED933A11711BFFFF51A54250086E01F24DF6"};
                string[] fleets={"581BE9B1C99E560EBBF22CEBF19829B52115637A4AD4F0744F08C6C59D007404","17797B0CFBB4743E26B2B84A42817F5E3D12223CC8C688F23E8DE2D07A370DF2","40D9DF2DC4F4893CC96756278B3EDDDDB3E36393AF333F6F7D97EF95065A229D"};
                for(int i=0;i<maps.Length;i++){
                    var scene=Scenario.Load(Path.Combine(assets,"Graphics",maps[i]));
                    var fleet=PracticeFleetSnapshot.Create(catalog,scene.Groups[owners[i]][0][0],owners[i],Point.Empty,0);
                    // Source205 complete buffers, fixed zero origin, local UI excluded.
                    Require(Hash(fleet.CopyShipPool())==ships[i]&&Hash(fleet.CopyFleetPool())==fleets[i],"Original full-pool fixture "+maps[i]);
                    Require(Hash(fleet.CopyConsumedPayload())=="2D55A4878D242C44DE4302E4CEBA7152737B0716B115D97331861C332E249A1D","Original destructive marker fixture");
                }
            });
            check("Grouped deployment matches complete original initializer pools and random histories",delegate {
                var catalog=PracticeUnitCatalog.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs"));
                // Original209:4037B0+403210+403E20, pinned RIP, freshzero pools,
                // source map, supplied seed and localUIowner-1. Nine native cases.
                string[] fixtures={
                    "BS201,0,ECAE40FD7F9102D012E28F1CEB9F2378CDC3E7546242599B80C02644B5805E6E,429AE63C356DD897F0553E9B1CF5D65398CE77655CE2BF5AF78698A7CE98FF59,1149097276,700,4,16",
                    "BS201,731,ECAE40FD7F9102D012E28F1CEB9F2378CDC3E7546242599B80C02644B5805E6E,5AF9D07F5829D54F468D7469EAAC649A250B899158C0092E6F82E0D07E337626,2073215917,386,4,16",
                    "BS201,4294967295,ECAE40FD7F9102D012E28F1CEB9F2378CDC3E7546242599B80C02644B5805E6E,CAB8108946139A297D79546A91A2C309001EB2EEFE68787F3C1F781DD967430F,1191079719,520,4,16",
                    "BS202,0,1563EA37843C263292DBB0E0D98AA4E1911E57744E9B7C3E3CB0E6E2E16B481A,ECBDC3AB95DA838A300670617334E9AE6E93E4CAC26C931B7C1C4071E7119C4B,2202582748,348,4,15",
                    "BS202,731,1563EA37843C263292DBB0E0D98AA4E1911E57744E9B7C3E3CB0E6E2E16B481A,B9E0AA7556C9E2091B32C368D812239670CE98B17A99F64AE600E5542731F95C,54921299,152,4,15",
                    "BS202,4294967295,1563EA37843C263292DBB0E0D98AA4E1911E57744E9B7C3E3CB0E6E2E16B481A,96E323C531A232C9B1666A2B0C2A4C08CEFDCE48C51DCDB47C4F37AAAE0DC1EB,1414743281,130,4,15",
                    "BS204,0,AABF80D8547BC6E230A72A33FFC052652E299EE990D51BF2B9BF117971F78451,C40C45FBD5019D1B63F9862B1836AC955D4F33C8095F34019C37740EC3B008DD,1114490042,578,9,34",
                    "BS204,731,AABF80D8547BC6E230A72A33FFC052652E299EE990D51BF2B9BF117971F78451,BEAA2215C5D0C3665EC0BAA6AE332F409592056E2E76863E2E76BBEFA3A7A125,358922119,796,9,34",
                    "BS204,4294967295,AABF80D8547BC6E230A72A33FFC052652E299EE990D51BF2B9BF117971F78451,4DDCD60E2CF5E8C6E34FB5117913D29CF619DCEF626750AA5A55808EC57A5BDA,786305251,436,9,34",
                };
                foreach(string fixture in fixtures){
                    string[] f=fixture.Split(',');var scene=Scenario.Load(Path.Combine(assets,"Graphics",f[0]+".SCN"));
                    var deployed=new PracticeDeployment(catalog,scene,uint.Parse(f[1]));
                    Require(Hash(deployed.CopyShipPool())==f[2]&&Hash(deployed.CopyFleetPool())==f[3],"Original grouped fullpool "+f[0]+" seed"+f[1]);
                    Require(deployed.RandomState==uint.Parse(f[4])&&deployed.RandomCalls==int.Parse(f[5])&&deployed.Fleets.Count==int.Parse(f[6])&&deployed.UnitCount==int.Parse(f[7]),"Original grouped counters/random "+f[0]);
                    for(int owner=0;owner<scene.Players;owner++)Require(deployed.OwnerCounter(owner)==1+scene.Groups[owner][0].Count+scene.Groups[owner][1].Count,"Original initial owner counter");
                }
            });
            check("Original placement rejects occupied origins and ignores inactive records",delegate {
                var scene=Scenario.Load(Path.Combine(assets,"Graphics","BS201.SCN"));
                var planner=new PracticeSpawnPlanner(scene,0,null);
                Point[] expected={new Point(320,330),new Point(370,390),new Point(320,310)};int[] calls={160,258,374};uint[] states={1909300640u,4144848762u,3384151758u};
                for(int i=0;i<3;i++){Require(planner.NextOrigin(0)==expected[i]&&planner.RandomCalls==calls[i]&&planner.RandomState==states[i],"Original placement draw order");planner.Reserve(2,expected[i]);}
                foreach(int kind in new[]{0,1,-1}){
                    planner=new PracticeSpawnPlanner(scene,0,new[]{new PracticeFleetOrigin(kind,expected[0])});
                    Require(planner.NextOrigin(0)==expected[kind==0?0:1]&&planner.RandomCalls==calls[kind==0?0:1],"Original active/inactive reservation");
                }
            });
            check("Bounded deployment failures preserve random state and source data",delegate {
                var scene=Scenario.Load(Path.Combine(assets,"Graphics","BS201.SCN"));
                var planner=new PracticeSpawnPlanner(scene,0,null);
                Reject(delegate{planner.NextOrigin(0,1);});Require(planner.RandomState==0&&planner.RandomCalls==0,"Failed search consumed random");
                RejectArgument(delegate{planner.NextOrigin(4);});RejectArgument(delegate{planner.NextOrigin(0,0);});
                byte[] source=scene.Groups[0][0][0];string hash=Hash(source);
                var deployed=new PracticeDeployment(PracticeUnitCatalog.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs")),scene,0);
                Require(Hash(source)==hash&&deployed.Fleets[0].Kind==1&&deployed.Fleets[0].Occupancy(0,0)==0,"Source/emptycell kind isolation");
                byte[] selectedPool=deployed.Fleets[1].CopyFleetPool();
                Require(BitConverter.ToInt32(selectedPool,0)==0&&BitConverter.ToInt32(selectedPool,PracticeFleetSnapshot.FleetBytes)==1,"Selected fleet snapshot lost its pool slot");
                byte[] copy=deployed.CopyShipPool();copy[0]^=1;Require(deployed.Fleets[0].Units[0].Type==3,"Deployment pool alias");
                bool readOnly=false;try{((IList<PracticeFleetSnapshot>)deployed.Fleets).Clear();}catch(NotSupportedException){readOnly=true;}Require(readOnly,"Deployment roster mutable");
                for(int i=0;i<4096;i++)scene.Cells[i].Kind=1;
                planner=new PracticeSpawnPlanner(scene,731,null);scene.Cells[0].Kind=4;
                Reject(delegate{planner.NextOrigin(0);});Require(planner.RandomState==731&&planner.RandomCalls==0,"Nozone failure consumed random or map aliased");
            });

            check("Visibility matches original inclusive tiers and kind2 hidden branch",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var rules=PracticeVisibility.Load(p);
                var ships=new byte[402000];var fleets=new byte[123000];BitConverter.GetBytes(1).CopyTo(fleets,0);
                fleets[464]=10;fleets[465]=20;fleets[466]=40;int at=249*492;BitConverter.GetBytes(1).CopyTo(fleets,at);W16(fleets,at+6,1);
                int[] distances={0,9,10,11,19,20,21,39,40,41,-40,-41},levels={3,3,3,2,2,2,1,1,1,0,1,0};
                for(int n=0;n<distances.Length;n++){W16(fleets,at+12,distances[n]);W16(fleets,at+14,-distances[n]);Require(rules.Level(ships,fleets,0,249,0,-2)==levels[n],"Original279 visibility threshold "+n);}
                BitConverter.GetBytes(2).CopyTo(fleets,at);Require(rules.Level(ships,fleets,0,249,0,-2)==1&&rules.Level(ships,fleets,0,249,1,-2)==0&&rules.Level(ships,fleets,0,249,1,1)==1,"Original282 kind2 flag/local owner");
                fleets[at+467]=1;Require(rules.Level(ships,fleets,0,249,1,-2)==1,"Original282 revealed kind2");
            });
            check("First Practice firing eligibility matches original startup observations",delegate {
                var initial=FiringFixture(assets);string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var rules=PracticeVisibility.Load(p);
                byte[] ships=initial.Objects.CopyShipPool(),fleets=initial.Objects.CopyFleetPool();
                int[] levels={3,3,1,0,3,3,3,3,0,1,1,0,0};for(int n=0;n<levels.Length;n++)Require(rules.Level(ships,fleets,7,n,0,-2)==levels[n],"Original279 startup visibility "+n);
                var admitted=initial.CheckFiring(7,10,20);var denied=initial.CheckFiring(7,10,17);
                Require(admitted.Allowed&&admitted.VisibilityLevel==1&&admitted.Distance==150&&admitted.Range==180,"Original283 admitted startup shot");
                Require(denied.Denial==PracticeFiringDenial.OutOfRange&&denied.Distance==150&&denied.Range==20,"Original283 startup range rejection");
            });
            check("Queued firing matches the original enqueue and first complete frame",delegate {
                var initial=FiringFixture(assets);var queued=initial.QueueOrder(20,7,10,4,6);
                Require(Hash(queued.PauseTimers.CopyShipTimers())=="1DB4CB1236397A3B32013642A2A92A86D6AA543AEAEDDB314B6CE125CC518857","Original286 enqueue pool");
                var fired=queued.AdvanceFrame(false);Require(fired.ClockMilliseconds==40&&fired.LastOrdersConsumed==1&&fired.ObjectsFromOrders==3,"Original286 first frame counters");
                Require(Hash(fired.Objects.CopyTimedPool())=="6D10C7DB96947014EDA28E5D106430C5DE90779FC5AAEA938E9DDAFCDDC65AD4"&&
                    Hash(fired.Objects.CopyShipPool())=="B78317AD4085A15A6AB72E7348DB7D6C8B61D56958ED00567AB98A482F1425B5"&&
                    Hash(fired.PauseTimers.CopyShipTimers())=="2D8137560DC450AD714AA869BE70B27BD7DFC09A56281CB7B58D07D7511B76E5","Original286 complete timed/ship/order outputs");
                Require(Hash(initial.Objects.CopyShipPool())=="BB5FC9652DF41EC906A746B555300DD24F13F711B7DECD4002A96CDC83459121"&&initial.FiringPolls==0,"Initial comparison checkpoint changed");
            });
            check("Queued firing pauses deadlines and retains explicit resource boundaries",delegate {
                var initial=FiringFixture(assets);var state=initial.QueueOrder(20,7,10,4,6);for(int n=0;n<25;n++)state=state.AdvanceFrame(true);
                Require(state.ClockMilliseconds==1000&&state.FiringPolls==0&&state.ObjectsFromOrders==0&&BitConverter.ToUInt32(state.PauseTimers.CopyShipTimers(),7*52+4)==1000,"Original286 paused order timer");
                Require(state.ProcessOrders().ConsumedOrders==0&&state.AdvanceFrame(false).ObjectsFromOrders==3,"Original strict timer equality/next update");
                var cleared=state.ClearOrders(7);Require(BitConverter.ToInt16(cleared.PauseTimers.CopyShipTimers(),7*52+8)==-1&&cleared.ClockMilliseconds==1000&&initial.ClockMilliseconds==0,"Clear and reset baseline");
                Reject(delegate{new PracticeVisibility(new byte[624]);});Reject(delegate{new PracticeFiringRangeTable(new byte[70]);});Reject(delegate{new PracticeFiringRateTable(new byte[140]);});
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");RejectArgument(delegate{PracticeFiringRateTable.Load(p).Interval(17,1000,0);});
            });
            check("Moving fleets and queued shots match the first original combined poll",delegate {
                var world=WorldFixture(assets);var combat=world.Combat;for(int n=0;n<10;n++)combat=combat.QueueOrder(20,7,10,3,3);
                var next=world.WithCombat(combat).AdvanceFrame(false);
                Require(next.Combat.ClockMilliseconds==40&&next.Schedule.BatchStart==8&&next.Schedule.CompletedCycles==0&&next.Combat.ObjectsFromOrders==3,"Original292 first poll counters");
                Require(Hash(next.Combat.Objects.CopyFleetPool())=="4584538D956EC712CBCA898B52C6389EFCB4AAED6DB49B567CEF3752B942517A"&&Hash(next.Combat.Objects.CopyTimedPool())=="D4DE083D4BC2E943733A898CA9F62F31917D2A45990994F230C51ECA826ED671","Original292 movement before firing");
                Require(Hash(next.Combat.PauseTimers.CopyShipTimers())=="2960AB9B4469B031472CFAC5CA3B37F9921D5AE98F0096546DD5266694F3C7CC"&&world.Combat.ClockMilliseconds==0&&world.Schedule.BatchStart==0,"Original292 orders/immutable input");
            });
            check("Combined moving-target history matches the original terminal buffers",delegate {
                var world=WorldFixture(assets);var combat=world.Combat;for(int n=0;n<10;n++)combat=combat.QueueOrder(20,7,10,3,3);world=world.WithCombat(combat);
                for(int n=0;n<512;n++)world=world.AdvanceFrame(false);
                Require(world.Combat.ClockMilliseconds==20480&&world.Schedule.BatchStart==64&&world.Schedule.CompletedCycles==16&&world.MovedFleetUpdates>0&&world.Combat.RandomState==731,"Original292 terminal counters");
                Require(Hash(world.Combat.Objects.CopyFleetPool())=="F80BF0C5E58EE231145E5C21E63E567F28431F00B9F040EFA36E0862F926517C"&&Hash(world.Combat.Objects.CopyTimedPool())=="F69A8B74403067F3F92C80041D35DE1CC9BC6DFD13C885DD7F7CADC993D5ED7B"&&Hash(world.Combat.PauseTimers.CopyShipTimers())=="84D40A6E49ED66CF35B4730C19911D52F3D76D9A0FC5B4FF6D944C98FDFB1E80","Original292 terminal complete pools");
            });
            check("Combined study holds movement during pause and rejects unsupported batches atomically",delegate {
                var world=WorldFixture(assets);world=world.WithCombat(world.Combat.QueueOrder(20,7,10,3,3));string fleets=Hash(world.Combat.Objects.CopyFleetPool()),aux=Hash(world.CopyAuxiliary());
                for(int n=0;n<25;n++)world=world.AdvanceFrame(true);
                Require(world.Combat.ClockMilliseconds==1000&&world.Schedule.BatchStart==0&&world.Schedule.CompletedCycles==0&&world.MovedFleetUpdates==0&&Hash(world.Combat.Objects.CopyFleetPool())==fleets&&Hash(world.CopyAuxiliary())==aux&&BitConverter.ToUInt32(world.Combat.PauseTimers.CopyShipTimers(),7*52+4)==1000,"Paused movement/cursor/deadlines");
                byte[] unsupported=world.Combat.Objects.CopyFleetPool();BitConverter.GetBytes(9).CopyTo(unsupported,0);world=world.WithCombat(world.Combat.WithFleetPool(unsupported));
                bool guarded=false;try{world.AdvanceFrame(false);}catch(NotSupportedException){guarded=true;}
                Require(guarded&&world.Combat.ClockMilliseconds==1000&&world.Schedule.BatchStart==0&&Hash(world.Combat.Objects.CopyFleetPool())==Hash(unsupported)&&Hash(world.CopyAuxiliary())==aux,"Unsupported update published partial movement or clock");
            });
            check("Secondary enqueue preserves the original five-slot capacity and shared header",delegate {
                var ships=FiringFixture(assets).Objects.CopyShipPool();var orders=new byte[78000];for(int n=0;n<orders.Length;n++)orders[n]=(byte)(n*31+17);int at=7*52;
                BitConverter.GetBytes(123456789).CopyTo(orders,at+4);for(int n=0;n<10;n++){W16(orders,at+8+n*2,n==4?-1:50+n);W16(orders,at+28+n*2,-500-n);}
                Require(Hash(orders)=="E98070C7367CAD4A649FDE74CD47B845E3C2BFFECF6D715661A8128EA92B35F3","Original295 case860 input");
                var queued=PracticeSecondaryFiringOrders.Enqueue(ships,orders,7,3,10,639,-639);
                Require(queued.ReturnCode==12&&queued.SelectedSlot==4&&Hash(queued.CopyOrderPool())=="C04ADA81513613F1D5400E12E1F8A1B61587BFC354033903327CE9CA9F80A007","Original295 fifth-slot return/full pool");
                var full=PracticeSecondaryFiringOrders.Enqueue(ships,queued.CopyOrderPool(),7,1,249,0,0);Require(full.ReturnCode==1&&full.SelectedSlot==-1&&Hash(full.CopyOrderPool())==Hash(queued.CopyOrderPool()),"Full secondary queue unexpectedly cleared on kind/target change");
            });
            check("Secondary consumption retains the original hole tail and omits the primary timer clamp",delegate {
                var ships=FiringFixture(assets).Objects.CopyShipPool();var fleets=new byte[123000];var orders=new byte[78000];for(int n=0;n<orders.Length;n++)orders[n]=(byte)(n*31+17);for(int unit=0;unit<1500;unit++)for(int n=0;n<10;n++)W16(orders,unit*52+8+n*2,-1);
                BitConverter.GetBytes(1).CopyTo(ships,0);W16(ships,6,-2);W16(ships,12,7);W16(ships,14,6);W16(ships,16,0);W16(ships,198,-1);for(int n=0;n<24;n++)BitConverter.GetBytes(n==0?1:35).CopyTo(ships,100+n*4);
                BitConverter.GetBytes(6).CopyTo(fleets,7*492);W16(fleets,7*492+6,-2);BitConverter.GetBytes(2).CopyTo(fleets,10*492);W16(fleets,10*492+6,1);
                BitConverter.GetBytes(1).CopyTo(orders,0);BitConverter.GetBytes(-1).CopyTo(orders,4);for(int n=0;n<10;n++){W16(orders,8+n*2,n==0?4:n==1?-1:n);W16(orders,28+n*2,100+n);}BitConverter.GetBytes(0x4b47a0+10*492).CopyTo(orders,48);
                Require(Hash(orders)=="2DCFE3302F9A3BF0B3BC71685DA427FFC47919A1568E14CA5FD6D497342A80EC","Original294 case450 input");string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var ranges=PracticeFiringRangeTable.Load(p);var visibility=PracticeVisibility.Load(p);var rates=PracticeFiringRateTable.Load(p);
                var update=PracticeSecondaryFiringOrders.AdvanceWithoutLaunch(ranges,visibility,rates,ships,fleets,orders,0,0,0,-2);
                Require(update.ConsumedOrders==1&&update.RejectedOrders==0&&Hash(update.CopyOrderPool())=="FF6DECBF82CCED54F47037F1865FD7248B1DB82B05786BD4239BCE06AEAEB234","Original294 ten-slot shift with retained hole tail");
                BitConverter.GetBytes(0x7ffffff0).CopyTo(orders,4);var future=PracticeSecondaryFiringOrders.AdvanceWithoutLaunch(ranges,visibility,rates,ships,fleets,orders,0x7ffff000u,0,0,-2);Require(future.ConsumedOrders==0&&Hash(future.CopyOrderPool())==Hash(orders),"Future secondary timer was clamped as though it were primary");
            });
            check("Secondary local launch clears primary requests on depletion without aliasing the queues",delegate {
                var combat=FiringFixture(assets);byte[] ships=combat.Objects.CopyShipPool(),fleets=combat.Objects.CopyFleetPool(),orders=new byte[78000];
                W16(ships,7*268+196+17*2,1);BitConverter.GetBytes(17).CopyTo(ships,7*268+100);BitConverter.GetBytes(1).CopyTo(fleets,10*492);W16(fleets,10*492+12,BitConverter.ToInt16(fleets,7*492+12)+10);W16(fleets,10*492+14,BitConverter.ToInt16(fleets,7*492+14));
                for(int source=0;source<1500;source++)for(int n=0;n<10;n++)W16(orders,source*52+8+n*2,-1);int at=7*52;BitConverter.GetBytes(17).CopyTo(orders,at);BitConverter.GetBytes(999).CopyTo(orders,at+4);BitConverter.GetBytes(0x4b47a0+10*492).CopyTo(orders,at+48);for(int n=0;n<10;n++){W16(orders,at+8+n*2,n<2?4+n:-1);W16(orders,at+28+n*2,5+n%3);}
                Require(Hash(ships)=="D77E7E366342A78D66C9E05764BA2C998CF91E2F13A92AF3EF642172B2BB187E"&&Hash(fleets)=="DD5A36A0CFBBA6B1C8B94527DD424A7BF5C8CF4B35211260F757BBC0249CA7D8"&&Hash(orders)=="FCC799803A188CE9FF98A48AEF95B1F5AD2082ED664B3B7FDEB039FFA83E58C8","Original296 case252 inputs");
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var objects=new PracticeTimedCreationState(PracticeTimedWeaponCatalog.Load(p),new byte[21440],ships,fleets,-1);
                var update=PracticeSecondaryFiringOrders.AdvanceWithLaunch(PracticeFiringRangeTable.Load(p),PracticeVisibility.Load(p),PracticeFiringRateTable.Load(p),PracticeTimedWeaponCatalog.Load(p),PracticeUnitCatalog.Load(p),objects,orders,orders,1000,0,-2);
                Require(update.ConsumedOrders==1&&update.CreatedObjects==1&&update.LastRoundCallbacks==1&&Hash(update.CopyOrderPool())=="A3BECEE1E1120934CD0F3A658843D1E4ED155A72EB1CEC50EC6BC5B6D41C483A"&&Hash(update.CopyPrimaryOrders())=="F2316E58BF84221FBE51280924B149CA08D6FF1E37D5A7D3B900ED6BA6385430","Original296 distinct primary clear and secondary shift/rate");
                Require(Hash(update.Objects.CopyShipPool())=="116B793220A02346CE9E8B4F04694DBABDC98542A1C7A29F9D7CB13024578DF6"&&Hash(update.Objects.CopyTimedPool())=="5619D0BA66AB8E8E35456BF3B92A15817B0F65BE0017C3C640B9DB3A633CB175"&&Hash(orders)=="FCC799803A188CE9FF98A48AEF95B1F5AD2082ED664B3B7FDEB039FFA83E58C8","Original296 full ship/projectile buffers and immutable input");
            });
            check("Movement and both firing queues match the original ordered poll and asymmetric pause",delegate {
                var world=WorldFixture(assets);var combat=world.Combat;for(int n=0;n<10;n++)combat=combat.QueueOrder(20,7,10,3,3);world=world.WithCombat(combat);for(int n=0;n<5;n++)world=world.QueueSecondaryOrder(20,7,10,3,3);
                world=world.AdvanceFrame(false);Require(world.Combat.ClockMilliseconds==40&&world.Schedule.BatchStart==8&&world.SecondaryCreatedObjects==3&&world.Combat.ObjectsFromOrders==3,"Original297 first poll counts");
                Require(Hash(world.Combat.Objects.CopyTimedPool())=="2971870A6D41E1974089AEB974B3AE800ECD9311102B39FF827D0CDFFF3432F9"&&Hash(world.Combat.Objects.CopyFleetPool())=="84F427C70863C14BA657B68203F6647D7E3920446CC21000C05D3F6AA50D58CA"&&Hash(world.CopySecondaryOrders())=="408B2577AF4000972335FCEA3306EBC5DC36887A6CB3A8CD683EA55C035C5D7E","Original297 secondary launch before motion and primary launch after motion");
                string secondary=Hash(world.CopySecondaryOrders()),fleets=Hash(world.Combat.Objects.CopyFleetPool());uint timer=BitConverter.ToUInt32(world.Combat.PauseTimers.CopyShipTimers(),7*52+4);for(int n=0;n<25;n++)world=world.AdvanceFrame(true);
                Require(world.Combat.ClockMilliseconds==1040&&world.Schedule.BatchStart==8&&Hash(world.CopySecondaryOrders())==secondary&&Hash(world.Combat.Objects.CopyFleetPool())==fleets&&BitConverter.ToUInt32(world.Combat.PauseTimers.CopyShipTimers(),7*52+4)==unchecked(timer+1000u),"Original411313 shifts primary but not secondary deadlines");
            });
            check("Secondary target generation matches original cached first-fleet choices and full order pool",delegate {
                var data=SecondaryTargetFixture(0,0);Require(Hash(data[0])=="348544572BA1D396CC2DB8A3C2C2AFF3DFF67317EF3884B300EB1FFCEDD9E6B6"&&Hash(data[1])=="7FF0B10477E5A04AECBCDAE8228502A10B93AE2F28B3035FB2B1ABA44BE75DA9"&&Hash(data[2])=="A1DAA9489051E1036A8D35E094BAF4A51B782B64F9A85DF2DB4B2873375D108C","Original300 case1 complete inputs");
                var result=PracticeSecondaryTargeting.Generate(PracticeFiringRangeTable.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs")),data[0],data[1],data[2],0);
                Require(result.RandomCalls==12&&result.RandomState==3989110284u&&result.AttemptedOrders==6&&result.AcceptedOrders==6&&Hash(result.CopyOrderPool())=="C66406910523DEE6651344E775B2FF30410119D81011EB8786C6AB046224089C","Original300 producer complete output/counters");
                Require(result.Choices[0].SourceShip==7&&result.Choices[0].TargetFleet==2&&result.Choices[0].TargetShip==22&&result.Choices[0].Weapon==1&&result.Choices[0].X==3&&result.Choices[0].Y==24,"Original300 first choice; nearer later fleet must not win");
            });
            check("Secondary target generation preserves no-live-member and full-queue RNG behavior",delegate {
                var ranges=PracticeFiringRangeTable.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs"));var empty=SecondaryTargetFixture(2,0);var absent=PracticeSecondaryTargeting.Generate(ranges,empty[0],empty[1],empty[2],0);
                Require(absent.RandomCalls==6&&absent.RandomState==773150046u&&absent.AttemptedOrders==0&&Hash(absent.CopyOrderPool())=="A1DAA9489051E1036A8D35E094BAF4A51B782B64F9A85DF2DB4B2873375D108C","Original300 case7 keeps empty first target instead of switching fleets");
                var full=SecondaryTargetFixture(0,2);var rejected=PracticeSecondaryTargeting.Generate(ranges,full[0],full[1],full[2],0);Require(rejected.RandomCalls==12&&rejected.RandomState==3989110284u&&rejected.AttemptedOrders==6&&rejected.AcceptedOrders==0&&Hash(rejected.CopyOrderPool())=="4F96887DAD3D0753D6F0172B119C3F48AA04CF2CCC4DF923B6BCA549D24AA592","Original300 case3 still consumes both draws before full-queue rejection");
            });
            check("Secondary generation publishes immutable results and rejects later malformed records atomically",delegate {
                var ranges=PracticeFiringRangeTable.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs"));var data=SecondaryTargetFixture(0,0);var result=PracticeSecondaryTargeting.Generate(ranges,data[0],data[1],data[2],0);var exposed=result.CopyOrderPool();exposed[0]^=255;Require(Hash(result.CopyOrderPool())=="C66406910523DEE6651344E775B2FF30410119D81011EB8786C6AB046224089C","Producer exposed mutable result bytes");
                W16(data[0],8*268+4,7);string ships=Hash(data[0]),fleets=Hash(data[1]),orders=Hash(data[2]);Reject(delegate{PracticeSecondaryTargeting.Generate(ranges,data[0],data[1],data[2],0);});Require(Hash(data[0])==ships&&Hash(data[1])==fleets&&Hash(data[2])==orders,"Later malformed source ID published a partial queue/RNG update");
            });
            check("Aircraft activation matches the original complete pools and mode visibility",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=AircraftFixture(assets,6);Require(Hash(data[0])=="A586361BBF7B8D1D257463ED23077D16D443BDD7535EAF04D1BC3BC4E07E851F"&&Hash(data[1])=="C53B8C469B8FCDF9564A91E430723398F4974354C5DB6FE2E8D335D56A31601F"&&Hash(data[2])=="EEEF99F950AC35104BFC9406955E0932BCD48B912BCE96E3AC547AD37DBD775E","Original301 case0 input pools");
                var result=PracticeAircraftTransitions.ApplyOffscreen(PracticeAircraftTables.Load(p),PracticeUnitCatalog.Load(p),data[0],data[1],data[2],0,1,249,3,-2);
                Require(result.Outcome==PracticeAircraftTransitionOutcome.Activated&&result.ConfiguredMembers==6&&result.SelectedWaypoint== -1&&result.Invalidations==1,"Original301 activation callbacks");Require(Hash(result.CopyShipPool())=="7C16BF900856DFD24F9009DCA9CC340DC6DA32C65E6CFC9D4A161E2601122DDA"&&Hash(result.CopyFleetPool())=="87CABB7A54287A809FA81BE4E19C1A4ECFF54C1CAD139EF457FD0032A3CEE35F"&&Hash(result.CopyPrimaryOrders())==Hash(data[2]),"Original301 case0 complete outputs");
            });
            check("Aircraft docking preserves the source member-index order clearing",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=AircraftFixture(assets,3);Require(Hash(data[1])=="3FFB4A0BAA06E93E75B1C04F13A7DBE980D9EDD9FB3FAB5977E851DB74D79841","Original301 case4050 input fleet");
                var result=PracticeAircraftTransitions.ApplyOffscreen(PracticeAircraftTables.Load(p),PracticeUnitCatalog.Load(p),data[0],data[1],data[2],0,0,249,3,-2);Require(result.Outcome==PracticeAircraftTransitionOutcome.Docked&&result.SelectedWaypoint==3&&result.Invalidations==0,"Original301 docking continuation");Require(Hash(result.CopyShipPool())==Hash(data[0])&&Hash(result.CopyFleetPool())=="9D90921D97EDE3663160D06A427D8509EA95E83AB4B3A68D0DD10964350758C0"&&Hash(result.CopyPrimaryOrders())=="06E1919D77D16163849B386946D6FD7C45554627022D2A011AF4A4EC0227058C","Original301 dock whole pools");var orders=result.CopyPrimaryOrders();Require(BitConverter.ToInt16(orders,8)== -1&&BitConverter.ToInt16(orders,20*52+8)==1,"Source clears member position, not unit slot20");
            });
            check("Aircraft transitions own their results and reject late unsupported members atomically",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var tables=PracticeAircraftTables.Load(p);var catalog=PracticeUnitCatalog.Load(p);var data=AircraftFixture(assets,6);var result=PracticeAircraftTransitions.ApplyOffscreen(tables,catalog,data[0],data[1],data[2],0,1,249,3,-2);var exposed=result.CopyShipPool();exposed[0]^=255;Require(Hash(result.CopyShipPool())=="7C16BF900856DFD24F9009DCA9CC340DC6DA32C65E6CFC9D4A161E2601122DDA","Mutable aircraft result");BitConverter.GetBytes(1).CopyTo(data[0],25*268);string ships=Hash(data[0]),fleets=Hash(data[1]),orders=Hash(data[2]);bool rejected=false;try{PracticeAircraftTransitions.ApplyOffscreen(tables,catalog,data[0],data[1],data[2],0,1,249,3,-2);}catch(NotSupportedException){rejected=true;}Require(rejected&&Hash(data[0])==ships&&Hash(data[1])==fleets&&Hash(data[2])==orders,"Late unsupported member published partial aircraft changes");
            });
            check("Stored helicopter creation matches whole original pools and parent link",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=AircraftFactoryFixture(assets,10);Require(Hash(data[0])=="4491A97AAD928DC2254023067D6D0E47E823E6F79B6FA72C2E881F88E809B675"&&Hash(data[1])=="BF9B19ACA18684D03510C7746276F5384D2EED7CAEF836A94C017F2FDED899D1","Original302 case0 input pools");var result=PracticeAircraftFactory.CreateOffscreen(PracticeUnitCatalog.Load(p),data[0],data[1],7,7,0,6,0,-2);Require(result.AllocatedUnits.Count==4&&result.AllocatedUnits[0]==10&&result.AllocatedUnits[3]==13,"Original302 sequential four-unit allocation");Require(Hash(result.CopyShipPool())=="9725B462401DB65346935FF51BEB03993B2552A875C4308CF104C661610F9C88"&&Hash(result.CopyFleetPool())=="7CCABF9B200418F1689154ABFB4D9CF73A0AB3C018E04D3C7880A09FC2B6DBCE","Original302 complete constructor/finalizer outputs");
            });
            check("Stored AWACS creation admits the final unit slot without reading past the pool",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=AircraftFactoryFixture(assets,1499);Require(Hash(data[0])=="FC41E07A52AA925334C83B4088212D0345C6266B0E4485DF9814E78038B9208F","Original302 case1323 input ships");var result=PracticeAircraftFactory.CreateOffscreen(PracticeUnitCatalog.Load(p),data[0],data[1],7,7,0,8,0,-2);Require(result.AllocatedUnits.Count==1&&result.AllocatedUnits[0]==1499&&Hash(result.CopyShipPool())=="15C70A88612EEBFB394A647625AC82C14C984571BE78CAE8F0DE9724AB38BF43"&&Hash(result.CopyFleetPool())=="5EFD803753B008B9B633D7AEAE42DFDC7322492E148CADC45821B580AAC88475","Original302 final-slot whole output");
            });
            check("Aircraft factory results are immutable and exhausted allocation fails atomically",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var catalog=PracticeUnitCatalog.Load(p);var data=AircraftFactoryFixture(assets,10);var result=PracticeAircraftFactory.CreateOffscreen(catalog,data[0],data[1],7,7,0,6,0,-2);var copy=result.CopyShipPool();copy[0]^=255;Require(Hash(result.CopyShipPool())=="9725B462401DB65346935FF51BEB03993B2552A875C4308CF104C661610F9C88","Factory mutable result");var exhausted=AircraftFactoryFixture(assets,1497);string ships=Hash(exhausted[0]),fleets=Hash(exhausted[1]);Reject(delegate{PracticeAircraftFactory.CreateOffscreen(catalog,exhausted[0],exhausted[1],7,7,0,6,0,-2);});Require(Hash(exhausted[0])==ships&&Hash(exhausted[1])==fleets,"Factory failure published allocated prefix or parent link");
            });
            check("Copied aircraft study follows a complete original create and five-mode dock history",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var catalog=PracticeUnitCatalog.Load(p);var deployment=PracticeDeployment.FirstPractice(catalog,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN")),143514006u);var initial=new PracticeAircraftStudyState(catalog,PracticeAircraftTables.Load(p),deployment);var state=initial.Create(7,6,0);Require(state.FleetSlot==13&&state.Stored&&state.Members.Count==4&&Hash(state.CopyShipPool())=="7500F1D7B9DB41EF98C245523F62ED66757CB4F171CCAF74A40A18E98317EED0"&&Hash(state.CopyFleetPool())=="E63991DBEB80A2FA476E8BCD11DB04C92EB4A9DE973DDF1ECE340A17F6F857F8","Original303 created group");for(int mode=1;mode<=5;mode++){state=state.Transition(mode);Require(!state.Stored&&state.Mode==mode,"Mode activation");state=state.Transition(0);Require(state.Stored&&state.Mode==mode,"Dock retains task");}Require(state.Invalidations==5&&Hash(state.CopyShipPool())=="448CE7A0FED00E8AF07029FBC70FAF5CBE081F1685994E2156FB8A7D5799E97C"&&Hash(state.CopyFleetPool())=="45926E8C5E1E8282DB8B3AE38A430B362476B24CE235006732614B59CB71577D"&&Hash(state.CopyPrimaryOrders())=="6C7254C191D63DC247ADEE64B7C58D3ABDCFEC23D1EB1A230F8B77DA90D2628E","Original303 terminal pools");Require(!initial.HasGroup&&Hash(initial.CopyFleetPool())==Hash(deployment.CopyFleetPool()),"Study changed starting deployment");
            });
            check("Anti-aircraft raw last-round launch matches complete original construction pools",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=AntiAircraftLaunchFixture(assets,1,false);Require(Hash(data[0])=="6F1141A87ED55533DC7E5EEAE6810A2A3DC23B1532B8C2143442C62A5728100A"&&Hash(data[1])=="C1A235AE2688CD82DE154DD02F21D776CC2698CB3F598881FD362F7568D2110F"&&Hash(data[2])=="EEEF99F950AC35104BFC9406955E0932BCD48B912BCE96E3AC547AD37DBD775E"&&Hash(data[3])=="E6044CD3BAE2BC9E13F892FE0B3DCDCDC5FB0038A4397C217722471FA98D1AD7","Original304 case36 inputs");var update=PracticeAntiAircraftLaunch.Apply(PracticeTimedWeaponCatalog.Load(p),PracticeUnitCatalog.Load(p),data[3],data[0],data[1],data[2],1,7,13,3,4,0,2);Require(update.Outcome==PracticeTimedCreationOutcome.Created&&update.CreatedSlot==0&&update.DepletedLastRound&&update.State.LastTargetFleet==13&&Hash(update.State.CopyShipPool())=="569F9709C2470D591C73F86384DE574F123DD5A452D214590B4F317B406C1F8E"&&Hash(update.CopyOrderPool())=="29D21C066D4123B4D9B7F45F8EEECC8C66463B901C25564AEF779D74BC4C14C6"&&Hash(update.State.CopyTimedPool())=="A18FED724F4E17ED0F72AA27C8C307AAF4C0863A5041098D89E655BDD9FB0C24","Original304 case36 complete outputs");
            });
            check("Anti-aircraft raw stock wraps and full capacity preserves every source byte",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var weapons=PracticeTimedWeaponCatalog.Load(p);var units=PracticeUnitCatalog.Load(p);var data=AntiAircraftLaunchFixture(assets,-32768,false);var update=PracticeAntiAircraftLaunch.Apply(weapons,units,data[3],data[0],data[1],data[2],1,7,13,3,4,0,2);Require(Hash(update.State.CopyShipPool())=="BA7B1D9F6586B27621212041F4870397377DCD2F669DFDF932CD251328B47D8C"&&Hash(update.State.CopyTimedPool())=="5B4CCB895578CDC8154B90348648A2920A005E86351E97EA5101EC76B1E2E05F","Original304 case216 signed decrement");data=AntiAircraftLaunchFixture(assets,-32768,true);Require(Hash(data[3])=="097793DA8C12E58FE97947CE3FEDDBC6020682B64030EA10FC3B82F0A9EC959D","Original304 case234 full input");update=PracticeAntiAircraftLaunch.Apply(weapons,units,data[3],data[0],data[1],data[2],1,7,13,3,4,0,2);Require(update.Outcome==PracticeTimedCreationOutcome.PoolFull&&update.State.LastTargetFleet==2&&Hash(update.State.CopyShipPool())==Hash(data[0])&&Hash(update.State.CopyTimedPool())==Hash(data[3])&&Hash(update.CopyOrderPool())==Hash(data[2]),"Original304 full capacity precedes stock consumption");
            });
            check("Anti-aircraft launch owns its data and leaves ordinary weapon admission unchanged",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var weapons=PracticeTimedWeaponCatalog.Load(p);var units=PracticeUnitCatalog.Load(p);var data=AntiAircraftLaunchFixture(assets,1,false);RejectArgument(delegate{weapons.SourceSpeed(1);});RejectArgument(delegate{new PracticeTimedCreationState(weapons,data[3],data[0],data[1],2).CreateWithDepletion(1,7,13,3,4,0,units,data[2]);});var update=PracticeAntiAircraftLaunch.Apply(weapons,units,data[3],data[0],data[1],data[2],1,7,13,3,4,0,2);var copy=update.State.CopyTimedPool();copy[0]^=255;Require(Hash(update.State.CopyTimedPool())=="A18FED724F4E17ED0F72AA27C8C307AAF4C0863A5041098D89E655BDD9FB0C24","Mutable anti-aircraft result");W16(data[0],7*268+4,8);string ships=Hash(data[0]),orders=Hash(data[2]);Reject(delegate{PracticeAntiAircraftLaunch.Apply(weapons,units,data[3],data[0],data[1],data[2],1,7,13,3,4,0,2);});Require(Hash(data[0])==ships&&Hash(data[2])==orders,"Malformed source published depletion");
            });
            check("Aircraft defense matches original no-mount and secondary-selection decisions",delegate {
                var tables=PracticeDefenseTables.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs"));var data=AircraftDefenseFixture(assets,1,3,7,0,0,0,0);Require(Hash(data[0])=="34D610721EE3AA83D70A3A3B6B41D988984FA33BD018440D04F0E16CB3AF4DF0"&&Hash(data[1])=="020A01E51B4FBD8622C0F1BB55209694E52F44869AD96891C1097206068F9F08"&&Hash(data[2])=="509701B84E4E3CECB2129A2301A5188F55E03C195BA6F7D0BD38287B9538C2DD","Original305 case0 inputs");var result=PracticeDefense.ResolveAntiAircraft(tables,data[2],data[0],data[1],0,7,0);Require(result.Outcome==PracticeDefenseOutcome.Miss&&result.RandomState==2531011u&&Hash(result.CopyShipPool())==Hash(data[0]),"Original305 case0 miss");data=AircraftDefenseFixture(assets,1,3,1499,0,9,0,1);result=PracticeDefense.ResolveAntiAircraft(tables,data[2],data[0],data[1],0,1499,731);Require(result.Outcome==PracticeDefenseOutcome.ProceedToDamage&&result.RandomState==158974514u&&Hash(result.CopyShipPool())=="142AF2FFC7ACEBC4E28C3B90F63CBE438307BECEAFCE7CB210BC8DFA97799DE9","Original305 case127 secondary selection with no matching mount");
            });
            check("Aircraft defense preserves repeated primary attempts and terminal random state",delegate {
                var data=AircraftDefenseFixture(assets,2,4,1499,4,10,2,1);Require(Hash(data[0])=="53F4AFA9DE3FEE55F76F6E545031353826F2839CCA8BAA65BDCFEC717446EB48"&&Hash(data[1])=="5EBE262B37F92D554B3488AAC54BA9CFF0477F820BB87797FA7C897A5BD066D1"&&Hash(data[2])=="BD3DC49E28E608DA6A2D0AF05D6052E75FC12504D1F2DB20BE38DC9B59B45A13","Original305 case18307 inputs");var result=PracticeDefense.ResolveAntiAircraft(PracticeDefenseTables.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs")),data[2],data[0],data[1],0,1499,731);Require(result.Outcome==PracticeDefenseOutcome.Miss&&result.PrimaryAttempts==3&&result.RandomCalls==4&&result.RandomState==4052711439u&&Hash(result.CopyShipPool())=="5B522EDB65BE1EA513E54BBD36F13FF37522E315DB1412446A9CD674CDAAF541","Original305 repeated-failure/secondary full output");
            });
            check("Aircraft defense owns results and leaves ordinary defense admission unchanged",delegate {
                var tables=PracticeDefenseTables.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs"));var data=AircraftDefenseFixture(assets,2,4,1499,4,10,2,1);Reject(delegate{PracticeDefense.Resolve(tables,data[2],data[0],data[1],0,1499,731);});var result=PracticeDefense.ResolveAntiAircraft(tables,data[2],data[0],data[1],0,1499,731);var copy=result.CopyShipPool();copy[0]^=255;Require(Hash(result.CopyShipPool())=="5B522EDB65BE1EA513E54BBD36F13FF37522E315DB1412446A9CD674CDAAF541"&&Hash(data[0])=="53F4AFA9DE3FEE55F76F6E545031353826F2839CCA8BAA65BDCFEC717446EB48","Aircraft defense data ownership");W16(data[0],1499*268+14,7);Reject(delegate{PracticeDefense.ResolveAntiAircraft(tables,data[2],data[0],data[1],0,1499,731);});
            });
            check("AWACS destruction matches complete original pools without a death-effect allocation",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=AircraftDestructionFixture(assets,8);Require(Hash(data[0])=="3C62448204773FCEDFFA999C46049F72164732B2E4039647BA7594877975A7F9"&&Hash(data[1])=="4999DAB4A40871D4D7B669F691BDAED000DE5FD39CD1E288D08EDE3957E6F048","Original306 case0 inputs");var update=PracticeAircraftDestruction.Resolve(PracticeAircraftDestructionTable.Load(p),File.ReadAllBytes(Path.Combine(p,"ClassicVisuals.seqbin")),data[0],data[1],data[3],data[2],20,0,false,-1,37,0);Require(update.Points==500&&update.FleetRetired&&update.EffectSlot== -1&&update.MovieCounter==4&&update.RandomState==0&&Hash(update.CopyShipPool())=="190194BA71E8133BCBB4859F9251B443E5A1CAE1033827EBFCFBF03A3CF3B3E3"&&Hash(update.CopyFleetPool())=="E2FD103EF916A155B5F9061CCD68C58107601A903D8DFD09450F300D1C1BEDC5"&&Hash(update.CopyEffectPool())==Hash(data[2])&&Hash(update.CopyScoreRegion())=="F8BBEF7361F34A0B90F4DC2D1531777F15206EA0014FC78A3524FF2355B65E28","Original306 AWACS outputs/retained parent link");
            });
            check("Plane and helicopter retirement match original effects counters and parent unlinking",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var tables=PracticeAircraftDestructionTable.Load(p);var visual=File.ReadAllBytes(Path.Combine(p,"ClassicVisuals.seqbin"));var plane=AircraftDestructionFixture(assets,9);var update=PracticeAircraftDestruction.Resolve(tables,visual,plane[0],plane[1],plane[3],plane[2],20,0,false,-1,37,0);Require(update.Points==100&&update.MovieCounter==1&&update.RandomCalls==1&&update.RandomState==2531011u&&Hash(update.CopyShipPool())=="EFB9AFD5F2E7EE7E0EE7AB1B4454C5EBAF865F9F06B266AC2FE84554E01A690C"&&Hash(update.CopyFleetPool())=="6363F83C917D8C5A0CA9E74110FC58F3EACE8130E7C886A1B83543DA0AF83BB0"&&Hash(update.CopyEffectPool())=="764316D9AC35DB81A0BD3132C0F522C29B4B10D6F9427AF7445C3BAB5789F918"&&Hash(update.CopyScoreRegion())=="10B13C46E8C3185B728DA5818FBAC90BC643101DBF02A1048D8211E9B4C8BB9C","Original306 case1728 plane output");var helicopter=AircraftDestructionFixture(assets,10);update=PracticeAircraftDestruction.Resolve(tables,visual,helicopter[0],helicopter[1],helicopter[3],helicopter[2],20,0,false,-1,37,0);Require(update.RandomCalls==0&&update.MovieCounter==1&&Hash(update.CopyShipPool())=="9AC592D24DA564DCEE05B88C8C8DD234EFF20DEB95D4DE9F117C1C80F41DCD5F"&&Hash(update.CopyEffectPool())=="42EE3D16A74A64E976E6CF9545834A53BE965C839AF48C81BDD4EE862DD801BA","Original306 case3456 helicopter output");
            });
            check("Aircraft destruction owns outputs and late invalid parent links fail atomically",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var table=PracticeAircraftDestructionTable.Load(p);var visual=File.ReadAllBytes(Path.Combine(p,"ClassicVisuals.seqbin"));var data=AircraftDestructionFixture(assets,9);var update=PracticeAircraftDestruction.Resolve(table,visual,data[0],data[1],data[3],data[2],20,0,false,-1,37,0);var copy=update.CopyShipPool();copy[0]^=255;Require(Hash(update.CopyShipPool())=="EFB9AFD5F2E7EE7E0EE7AB1B4454C5EBAF865F9F06B266AC2FE84554E01A690C","Mutable aircraft destruction result");W16(data[1],13*492+10,4);string ships=Hash(data[0]),fleets=Hash(data[1]),effects=Hash(data[2]),scores=Hash(data[3]);Reject(delegate{PracticeAircraftDestruction.Resolve(table,visual,data[0],data[1],data[3],data[2],20,0,false,-1,37,0);});Require(Hash(data[0])==ships&&Hash(data[1])==fleets&&Hash(data[2])==effects&&Hash(data[3])==scores,"Late parent failure published destruction prefix");
            });
            check("Helicopter impact processes every live hull row and matches whole original outputs",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=AircraftImpactFixture(assets,0,1);Require(Hash(data[0])=="422A9A412504942D9A0A0F1EF07A069E1E060AEDB09F661DD3F87E5AC35CF654"&&Hash(data[1])=="33BADC384351FB72F1C1582B79C619FBD1E1AE63881F42D9593E0333FD15E070"&&Hash(data[2])=="AFA7736DDCA4C5C33085334F6E620E1EFAD7DE545AB1EEF39A9CE34CE8F40CD1","Original307 case2 inputs");var result=PracticeAircraftImpact.Resolve(PracticeDefenseTables.Load(p),PracticeUnitCatalog.Load(p),PracticeAircraftDestructionTable.Load(p),File.ReadAllBytes(Path.Combine(p,"ClassicVisuals.seqbin")),data[0],data[1],data[2],data[3],data[4],0,731,37);Require(result.Outcome==PracticeImpactOutcome.Damage&&result.VictimDestroyed&&result.RandomState==158974514u&&result.MovieCounter==1&&Hash(result.CopyShipPool())=="C334147B62A04D2B52A36111FCE129595589153697B4614A0901F7A51B4EB618"&&Hash(result.CopyFleetPool())=="2F5E0D8CB1814FAAD0F35B55491E31FD20D2B14C095B813DE8F4FCD71DA21FDC"&&Hash(result.CopyTimedPool())=="06CDABADA13A62131AA004DD30A7F677662F805A3478C34D1AC24951D84402CC"&&Hash(result.CopyEffectPool())=="2C4DFAC05C5470BE76A6CB39F2A4F4E735403906A743EC5E118E147306327287"&&Hash(result.CopyScoreRegion())=="CF3ED18A5EF8ED41CD4748625A641633070EE03707E3BAB9331EA0E35CA4B89B","Original307 complete helicopter callback");
            });
            check("Plane death RNG and AWACS single-row impact retain their different original paths",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var defenses=PracticeDefenseTables.Load(p);var units=PracticeUnitCatalog.Load(p);var table=PracticeAircraftDestructionTable.Load(p);var visual=File.ReadAllBytes(Path.Combine(p,"ClassicVisuals.seqbin"));var data=AircraftImpactFixture(assets,5,1);var result=PracticeAircraftImpact.Resolve(defenses,units,table,visual,data[0],data[1],data[2],data[3],data[4],0,731,37);Require(result.VictimDestroyed&&result.RandomState==2179244077u&&result.MovieCounter==1&&Hash(result.CopyShipPool())=="12035ADA9A67C7B5D1BAF98743D245600081B510D6D10AE2902CE34C624E5DA0"&&Hash(result.CopyFleetPool())=="DED985D4570BB24F90E0D3CBC28B027F75DEC096D5C743122A9A2D398B5494C5"&&Hash(result.CopyTimedPool())=="E46917BF56EBA525147EFE25AE01812E906CEF35C8C18DA403CA0BCD9526D912"&&Hash(result.CopyEffectPool())=="6D1741FEC1AA5ACC5FF60427617C7367AA7F27988D4F316702455DC9DC623E09","Original307 case4952 plane callback");data=AircraftImpactFixture(assets,10,3);result=PracticeAircraftImpact.Resolve(defenses,units,table,visual,data[0],data[1],data[2],data[3],data[4],0,731,37);Require(!result.VictimDestroyed&&result.RandomState==158974514u&&result.MovieCounter==37&&BitConverter.ToInt16(result.CopyShipPool(),19*268+16)==3&&Hash(result.CopyShipPool())=="6B7FE71FA9B9F892163509A50F01C87104E1B7A9D08A4CBFDDF2CA7976A9F28F"&&Hash(result.CopyFleetPool())=="8BE5E4E0ECFB6BF6DE9E3296A752109B2A4F187C726FA3177368954636A3B980"&&Hash(result.CopyScoreRegion())=="1C044CB4BEE5D043883FD8D0719E95E581F7840F660DE3522AF4EFEEBBE581A0","Original307 case10562 AWACS single row");
            });
            check("Aircraft impact preserves unmatched shots and rejects unsupported copied hull state atomically",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var defenses=PracticeDefenseTables.Load(p);var units=PracticeUnitCatalog.Load(p);var table=PracticeAircraftDestructionTable.Load(p);var visual=File.ReadAllBytes(Path.Combine(p,"ClassicVisuals.seqbin"));var data=AircraftImpactFixture(assets,0,1);for(int n=0;n<6;n++)BitConverter.GetBytes(0).CopyTo(data[2],13*492+16+n*4);var unmatched=PracticeAircraftImpact.Resolve(defenses,units,table,visual,data[0],data[1],data[2],data[3],data[4],0,731,37);Require(unmatched.Outcome==PracticeImpactOutcome.NoVictim&&!unmatched.Retired&&unmatched.RandomState==731&&Hash(unmatched.CopyTimedPool())==Hash(data[0]),"Original307 unmatched shot retained");data=AircraftImpactFixture(assets,0,1);W16(data[1],19*268+16,2);string ships=Hash(data[1]),fleets=Hash(data[2]),effects=Hash(data[3]),scores=Hash(data[4]);bool rejected=false;try{PracticeAircraftImpact.Resolve(defenses,units,table,visual,data[0],data[1],data[2],data[3],data[4],0,731,37);}catch(NotSupportedException){rejected=true;}Require(rejected&&Hash(data[1])==ships&&Hash(data[2])==fleets&&Hash(data[3])==effects&&Hash(data[4])==scores,"Incoherent aircraft hull published damage prefix");
            });
            check("Timed aircraft shot matches original rounded travel impact and finite effect history",delegate {
                var initial=AircraftTimedFixture(assets,0);var state=initial.LaunchAntiAircraft(1,14,13,3,1);Require(state.IsAircraftStudy&&Hash(state.Objects.CopyTimedPool())=="37EB7BB7BB2246D37E9772E04E67F16D8062C932F2E899E2D63D52A433D5D4E3","Original308 history0 launch");for(int n=0;n<25;n++)state=state.AdvanceFrame(false);Require(state.ClockMilliseconds==1000&&Hash(state.Objects.CopyTimedPool())=="37EB7BB7BB2246D37E9772E04E67F16D8062C932F2E899E2D63D52A433D5D4E3"&&Hash(state.Objects.CopyFleetPool())=="67238ED16839AE3FBA2D82066321AA76550150F999ECB40C46B1BF82B6769F7E","Original308 rounded early travel/fleet mark");for(int n=25;n<818;n++)state=state.AdvanceFrame(false);Require(state.ClockMilliseconds==32720&&state.ExpiredObjects==1&&state.DestroyedUnits==1&&state.ActiveEffects==0&&state.RandomState==158974514u&&state.MovieCounter==1&&Hash(state.Objects.CopyTimedPool())=="5AF139EC24CE7201E8EC97761DB24DBF05F61FE73FD80035E38F937316E243EE"&&Hash(state.Objects.CopyShipPool())=="59AEDF7B2DC62E3C8178644BCD22121FA315A762525CD7FA00A9035EB4177B6D"&&Hash(state.Objects.CopyFleetPool())=="2F5E0D8CB1814FAAD0F35B55491E31FD20D2B14C095B813DE8F4FCD71DA21FDC"&&Hash(state.CopyEffectPool())=="78FDB6B52495CF9355E3830B40608266F9FFB288A4824EE642119B5ECAFA7CD5"&&Hash(state.CopyScoreRegion())=="C026F27563AF204AECC197B89B20D7D5EFA5103859ED66051C59FC09D8EB2CBD","Original308 history0 terminal pools");Require(initial.ClockMilliseconds==0&&initial.MovementFrames==0&&BitConverter.ToInt16(initial.Objects.CopyShipPool(),14*268+198)==2,"Aircraft timed study changed starting copy");
            });
            check("Aircraft timed pause shifts deadlines while holding flight effects and RNG",delegate {
                var state=AircraftTimedFixture(assets,0).LaunchAntiAircraft(1,14,13,3,1);for(int n=0;n<10;n++)state=state.AdvanceFrame(false);for(int n=0;n<25;n++)state=state.AdvanceFrame(true);Require(state.IsAircraftStudy&&state.ClockMilliseconds==1400&&state.PausedFrames==25&&state.MovementFrames==10&&state.PauseTimers.GlobalCounter==1000&&state.RandomState==731&&state.ActiveEffects==0&&Hash(state.Objects.CopyTimedPool())=="7DE5FF4179FB43F5039B7DF7EE985819F6C81AC7B9FC15A41C4F9BC235D51A9F"&&Hash(state.Objects.CopyShipPool())=="4EA81B97115B3DF9D01B436134C210A5DB6F4697BD8623BF6D923D46B4744F4D"&&Hash(state.Objects.CopyFleetPool())=="67238ED16839AE3FBA2D82066321AA76550150F999ECB40C46B1BF82B6769F7E","Original308 history2 final paused row");
            });
            check("Aircraft timed profile rejects ordinary launches and later unsupported callbacks atomically",delegate {
                var initial=AircraftTimedFixture(assets,0);bool rejected=false;try{initial.Launch(17,14,13,3,1);}catch(NotSupportedException){rejected=true;}Require(rejected,"Aircraft study accepted an ordinary launch");var prepared=initial.LaunchAntiAircraft(1,14,13,3,1);var timed=prepared.Objects.CopyTimedPool();Array.Copy(timed,0,timed,268,268);BitConverter.GetBytes(0u).CopyTo(timed,24);BitConverter.GetBytes(0u).CopyTo(timed,268+24);W16(timed,268+12,10);string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var pass=PracticeTimedImpactState.ForAircraft(PracticeUnitCatalog.Load(p),PracticeDefenseTables.Load(p),File.ReadAllBytes(Path.Combine(p,"ClassicVisuals.seqbin")),timed,prepared.Objects.CopyShipPool(),prepared.Objects.CopyFleetPool(),prepared.CopyEffectPool(),prepared.CopyScoreRegion(),731,PracticeAircraftDestructionTable.Load(p));string ships=Hash(pass.CopyShipPool()),fleets=Hash(pass.CopyFleetPool()),effects=Hash(pass.CopyEffectPool());rejected=false;try{pass.Advance(40,40);}catch(NotSupportedException){rejected=true;}Require(rejected&&Hash(pass.CopyShipPool())==ships&&Hash(pass.CopyFleetPool())==fleets&&Hash(pass.CopyEffectPool())==effects&&Hash(pass.CopyTimedPool())==Hash(timed),"Later unsupported aircraft callback published its earlier damage");
            });
            check("Copied AA battery setup matches original reevaluation and preserves pending combat atomically",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var weapons=PracticeTimedWeaponCatalog.Load(p);var units=PracticeUnitCatalog.Load(p);var initial=AircraftTimedFixture(assets,0);string original=Hash(initial.Objects.CopyShipPool());var prepared=PracticeAircraftTestBattery.Configure(initial,weapons,units,14,1,2);Require(prepared.IsAircraftStudy&&prepared.ClockMilliseconds==0&&prepared.RandomState==731&&Hash(prepared.Objects.CopyShipPool())=="911FE3881E8EC541AE48BB09C8760D6E84EF5394C7E87FB264981FB435EF64AB"&&Hash(initial.Objects.CopyShipPool())==original,"Original309 history0 copied first mount/stock/reevaluation fixture");var active=prepared.LaunchAntiAircraft(1,14,13,3,1);string ships=Hash(active.Objects.CopyShipPool()),timed=Hash(active.Objects.CopyTimedPool());bool rejected=false;try{PracticeAircraftTestBattery.Configure(active,weapons,units,14,2,2);}catch(InvalidOperationException){rejected=true;}Require(rejected&&Hash(active.Objects.CopyShipPool())==ships&&Hash(active.Objects.CopyTimedPool())==timed&&active.ClockMilliseconds==0,"Rearm changed an active copied shot");
            });
            check("AA secondary consumer matches original complete queue launch and signed stock",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=AircraftSecondaryFixture(assets,-2);Require(Hash(data[0])=="9780C45160C53F366011CE4BD3BA982191954527E2D90FD2E2A4353FA8EC9E32"&&Hash(data[1])=="5A0887426E07086CA63EFF8D29C8EA14B2ADF31767C25830A9EA7993C755026A"&&Hash(data[2])=="D1CA3085B94E30B8C982B38B938A69BB54C9A77790A643FB64AF906D7873911E"&&Hash(data[3])=="E39D422CE314CF4425D26B0AA4529CAE713A3EA6074FD71681D5B709ADE57E4D","Original311 case0 full input pools");var weapons=PracticeTimedWeaponCatalog.Load(p);var result=PracticeSecondaryFiringOrders.AdvanceWithAntiAircraftLaunch(PracticeFiringRangeTable.Load(p),PracticeVisibility.Load(p),PracticeFiringRateTable.Load(p),weapons,PracticeUnitCatalog.Load(p),new PracticeTimedCreationState(weapons,data[4],data[0],data[1],-1),data[2],data[3],1000,0,-2);Require(result.ConsumedOrders==1&&result.CreatedObjects==1&&result.AttemptedObjects==1&&result.LastRoundCallbacks==0&&result.Objects.LastTargetFleet==13&&Hash(result.Objects.CopyShipPool())=="9D93B5D1C8F3293A06BACDB76FF5626246C0EC46790833C506BA01CEAFB2EB9F"&&Hash(result.Objects.CopyFleetPool())==Hash(data[1])&&Hash(result.CopyPrimaryOrders())==Hash(data[2])&&Hash(result.CopyOrderPool())=="721CB9C3CBB86C4EFEDAE2115F76A28B63416EDE03044738624FD61BB4F8FF4A"&&Hash(result.Objects.CopyTimedPool())=="E3E72468D847F7C2E88252BDF58D83C27286FC7D51822D1041D6B97974D00ACD","Original311 case0 complete launch/shift/rate outputs");
            });
            check("AA queued last round clears primary while full capacity still consumes secondary",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=AircraftSecondaryFixture(assets,1);var weapons=PracticeTimedWeaponCatalog.Load(p);var units=PracticeUnitCatalog.Load(p);var ranges=PracticeFiringRangeTable.Load(p);var visibility=PracticeVisibility.Load(p);var rates=PracticeFiringRateTable.Load(p);var result=PracticeSecondaryFiringOrders.AdvanceWithAntiAircraftLaunch(ranges,visibility,rates,weapons,units,new PracticeTimedCreationState(weapons,data[4],data[0],data[1],-1),data[2],data[3],1000,0,-2);Require(Hash(data[0])=="4B9377EA36BD16CFEA46D472448EA3213FEF294646BD14D6DED8CCF4A5018837"&&result.LastRoundCallbacks==1&&Hash(result.Objects.CopyShipPool())=="1EC1259F1F937792F3A4BD3927BE4743C169B0FBBC317FC049070F73A6583B8C"&&Hash(result.CopyPrimaryOrders())=="827F5EADDF0411B13CD706AA5E3EF22E5FE81B6BEF6DE4020585B200446C6ECA"&&Hash(result.CopyOrderPool())=="721CB9C3CBB86C4EFEDAE2115F76A28B63416EDE03044738624FD61BB4F8FF4A"&&Hash(result.Objects.CopyTimedPool())=="9732B910DE3C6C5F5E3F3A529E7DE7D5F216448868185838E4F19EFEA73826BB","Original311 case90 primary/secondary depletion order");for(int n=0;n<80;n++)Put32(data[4],n*268,17);result=PracticeSecondaryFiringOrders.AdvanceWithAntiAircraftLaunch(ranges,visibility,rates,weapons,units,new PracticeTimedCreationState(weapons,data[4],data[0],data[1],-1),data[2],data[3],1000,0,-2);Require(result.ConsumedOrders==1&&result.CreatedObjects==0&&result.AttemptedObjects==1&&result.LastRoundCallbacks==0&&result.Objects.LastTargetFleet==-1&&Hash(result.Objects.CopyShipPool())==Hash(data[0])&&Hash(result.CopyPrimaryOrders())==Hash(data[2])&&Hash(result.Objects.CopyTimedPool())=="F57B1EA6AAC0C9D6C1CD07574BA2EEC72F085ED69633EBE79E1BC5C563B682C1"&&Hash(result.CopyOrderPool())=="721CB9C3CBB86C4EFEDAE2115F76A28B63416EDE03044738624FD61BB4F8FF4A","Original311 case110 full timed pool consumes secondary request without spending stock");
            });
            check("AA secondary opt-in preserves ordinary scope and later-record failure atomicity",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=AircraftSecondaryFixture(assets,-2);var weapons=PracticeTimedWeaponCatalog.Load(p);var units=PracticeUnitCatalog.Load(p);var ranges=PracticeFiringRangeTable.Load(p);var visibility=PracticeVisibility.Load(p);var rates=PracticeFiringRateTable.Load(p);var objects=new PracticeTimedCreationState(weapons,data[4],data[0],data[1],-1);bool rejected=false;try{PracticeSecondaryFiringOrders.AdvanceWithLaunch(ranges,visibility,rates,weapons,units,objects,data[2],data[3],1000,0,-2);}catch(NotSupportedException){rejected=true;}Require(rejected,"Ordinary secondary API silently admitted AA");Put32(data[3],14*52,1);Put32(data[3],14*52+4,999);W16(data[3],14*52+8,3);Put32(data[3],14*52+48,unchecked((int)0xdeadbeef));string ships=Hash(objects.CopyShipPool()),timed=Hash(objects.CopyTimedPool()),primary=Hash(data[2]),secondary=Hash(data[3]);rejected=false;try{PracticeSecondaryFiringOrders.AdvanceWithAntiAircraftLaunch(ranges,visibility,rates,weapons,units,objects,data[2],data[3],1000,0,-2);}catch(InvalidDataException){rejected=true;}Require(rejected&&Hash(objects.CopyShipPool())==ships&&Hash(objects.CopyTimedPool())==timed&&Hash(data[2])==primary&&Hash(data[3])==secondary,"Later unsupported secondary record published an earlier launch or depletion");
            });
            check("Retired fleet stale ship cell preserves the original unmatched projectile",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=RetiredFixture(assets,17,2);
                Require(Hash(data[0])=="D4FB7DF9492E7EECA05309936575DCF441D640804626F2B44BB6819A488F05D8"&&Hash(data[2])=="575680C602FD12D0C483D89C9893578CBDC69A2811E380C064D59D6B33D0F86E","Original289 no-victim inputs");
                var result=PracticeImpact.ResolveWithDestruction(PracticeDefenseTables.Load(p),PracticeUnitCatalog.Load(p),File.ReadAllBytes(Path.Combine(p,"ClassicVisuals.seqbin")),data[0],data[1],data[2],data[3],data[4],0,0,PracticeDestructionTables.Load(p),37);
                Require(result.Outcome==PracticeImpactOutcome.NoVictim&&!result.Retired&&result.RandomState==0&&result.RandomCalls==0&&result.MovieCounter==37,"Original289 retained projectile/counter");
                Require(Hash(result.CopyTimedPool())=="D4FB7DF9492E7EECA05309936575DCF441D640804626F2B44BB6819A488F05D8"&&Hash(result.CopyEffectPool())=="5CB859B90F8E0AF206F329C7D98485866F7FF0C83B538B8AB4166C3756A84B35","Original289 no-victim whole outputs");
            });
            check("Retired fleet water expiry matches original RNG and type28 effect",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=RetiredFixture(assets,14,0);byte[] visual=File.ReadAllBytes(Path.Combine(p,"ClassicVisuals.seqbin"));
                Require(Hash(data[0])=="A40A73FA626618A8A4921D955554830E78154A9471534E07A4BC286F35037B61"&&Hash(data[2])=="B4D74A26B3868DF92E2DA5EA3DD6EBFF19836DCDD850071564F0BCA533D4EA66","Original289 water inputs");
                var result=PracticeImpact.ResolveWithDestruction(PracticeDefenseTables.Load(p),PracticeUnitCatalog.Load(p),visual,data[0],data[1],data[2],data[3],data[4],0,0,PracticeDestructionTables.Load(p),37);
                Require(result.Retired&&result.Outcome==PracticeImpactOutcome.NonshipCell&&result.RandomState==2531011u&&result.RandomCalls==1&&result.EffectSlot==0&&result.EffectType==28&&result.MovieCounter==37,"Original289 retired water result");
                Require(Hash(result.CopyTimedPool())=="5D119DF10B460A422D6D0E364B40E81EC9C1A91E3613BFA751D165FA7801EFE2"&&Hash(result.CopyEffectPool())=="15A1DFF8D00D35372C22D8D14CFF2305626A698AEA2D32EEA365EF02D91F9515","Original289 water whole outputs");
                bool legacyGuard=false;try{PracticeMissImpact.Resolve(data[0],data[2],data[3],visual,0,0);}catch(NotSupportedException){legacyGuard=true;}Require(legacyGuard,"Legacy no-destruction scope changed");
            });
            check("First Practice startup matches complete source pools across five initial random states",delegate {
                var catalog=PracticeUnitCatalog.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs"));
                byte[] source=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN"));
                // Source213 includes root pool defaults, seven kind7 objects,
                // type25 construction/local placement, and grouped creation.
                // Two seeds also match complete actual WinMain212 snapshots.
                string[] fixtures={
                    "0,653E5A4ED50C67C8FDE818DB540476E1D91A58712A959ED2A7AE7D054A799B71,5194430FAF502BB2B467B112BCCF663DBC87F1D417910222D28CE916CAEE482E,2451081716,692",
                    "731,508D5FFB81B0539A2D7807D9E9884D73893D6D7900411FD9C136DCA3310A6C27,E6F61762D9EAAC619A46C7CE7A385B04B2DCBBB5C97530E00370718315A945BA,3548431817,494",
                    "4294967295,E00C1F571D07CAFAD42648ECB50468F42A7320F37D278510CB35AA4E9C0803A3,CBC7EB9912AC0D854A2C650E3F4074CC7ADAC2339FB659AFFE883FA7AC96E809,1191079719,520",
                    "143514006,BB5FC9652DF41EC906A746B555300DD24F13F711B7DECD4002A96CDC83459121,9CF2E0C76FD8683D241732740FB2836599A77F8FF2EA9C1D32F40DE7BA760449,1835941210,548",
                    "144062032,691A61B5268E2E427DC05E57B325039031E7E6C9AE1C2E3AF2E56E3F9B746847,613886C0A73968179A655F49B1EF5FB500252B61FEECFB857C90DB682FB68BC6,2218741530,210",
                };
                foreach(string fixture in fixtures){string[] f=fixture.Split(',');var state=PracticeDeployment.FirstPractice(catalog,source,uint.Parse(f[0]));
                    Require(Hash(state.CopyShipPool())==f[1]&&Hash(state.CopyFleetPool())==f[2],"Original whole startup pools seed"+f[0]);
                    Require(state.RandomState==uint.Parse(f[3])&&state.RandomCalls==int.Parse(f[4])&&state.MapObjectCount==7&&state.Fleets.Count==13&&state.UnitCount==19,"Original startup random/count context");
                }
            });
            check("Kind7 records retain source IDs, local placement, counters and resource identity",delegate {
                var catalog=PracticeUnitCatalog.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs"));
                byte[] source=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN"));string hash=Hash(source);
                var state=PracticeDeployment.FirstPractice(catalog,source,143514006u);
                Require(Hash(source)==hash&&state.IncludesMapObjects&&state.Fleets[0].WorldOrigin==new Point(170,180),"Input map/source scan order");
                var first=state.Fleets[0];Require(first.Kind==11&&first.Units.Count==1&&first.Units[0].Type==25&&first.Units[0].X==8&&first.Units[0].Y==3&&first.Units[0].Length==2&&first.Units[0].HitPoints==2&&first.Units[0].Speed==0,"Actual original type25 fields");
                Require(first.NextCounter==1&&first.CopyConsumedPayload().Length==0&&state.OwnerCounter(0)==2&&state.OwnerCounter(1)==6,"Special object spent owner counter or invented payload");
                byte[] ships=state.CopyShipPool(),fleets=state.CopyFleetPool();Require(BitConverter.ToInt16(ships,1499*268+4)==1499&&BitConverter.ToInt16(fleets,249*492+4)==249&&fleets[249*492+40]==255&&fleets[249*492+482]==255,"Original inactive IDs/sentinels");
                ships[0]^=1;Require(first.Units[0].Type==25,"Startup pool aliases unit records");
            });
            check("First Practice startup rejects unsupported maps and absent or changed special tables",delegate {
                string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var data=new byte[7][];string[] names={"UnitTemplates","UnitAmmunition","UnitOffense","FleetColors","UnitMarkerMap","Type25Template","Kind11Colors"};for(int i=0;i<7;i++)data[i]=File.ReadAllBytes(Path.Combine(programs,names[i]+".seqbin"));
                var catalog=PracticeUnitCatalog.ReadWithMapObjects(data[0],data[1],data[2],data[3],data[4],data[5],data[6]);byte[] source=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN"));
                var old=PracticeUnitCatalog.Read(data[0],data[1],data[2],data[3],data[4]);Require(!old.HasMapObjectTables,"Old five-table catalog changed");
                Reject(delegate{PracticeDeployment.FirstPractice(old,source,0);});Reject(delegate{PracticeDeployment.FirstPractice(catalog,new byte[0],0);});source[0]^=1;Reject(delegate{PracticeDeployment.FirstPractice(catalog,source,0);});
                foreach(int i in new[]{5,6}){data[i][0]^=1;Reject(delegate{PracticeUnitCatalog.ReadWithMapObjects(data[0],data[1],data[2],data[3],data[4],data[5],data[6]);});data[i][0]^=1;}
                Reject(delegate{PracticeUnitCatalog.ReadWithMapObjects(data[0],data[1],data[2],data[3],data[4],data[5],null);});
            });

            check("First Practice primary terrain matches the original 500-record pool",delegate {
                byte[] source=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN"));
                var terrain=PracticeTerrainPool.FirstPractice(source);
                Require(terrain.Count==54&&terrain.Cells[0].Slot==0&&terrain.Cells[0].Origin==new Point(150,190),"Native terrain scan order/count");
                Require(Hash(terrain.CopyRecords())=="1F28D6B86CAFAEED50F099D5BB093019744B352427D035795562E156A98B4DF2","Original full primary terrain pool");
            });
            check("Practice fleet steps match original numeric, collision and arrival fixtures",delegate {
                // Original223 cases; current fields12..15,40..59,62,482,
                // selected waypoint and complete auxiliary-buffer hash.
                string[] fixtures={
                    "Case000-F2-S1-V0,2,1,30,0,0,0,0,False,False,False,False,410140015E019A01A401AE01B8014001FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case004-F2-S1-V4,2,1,30,10,0,0,0,False,False,False,False,410140015E019A01A401AE01B8014A01FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case022-F2-S3-V0,2,3,30,0,0,0,0,False,False,False,False,420140015E019A01A401AE01B8014001FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case033-F2-S4-V0,2,4,30,0,0,0,0,False,False,False,False,420140015E019A01A401AE01B8014001FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case048-F2-S5-V4,2,5,30,10,0,0,0,False,False,False,False,430141015E019A01A401AE01B8014A01FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case060-F2-S6-V5,2,6,-30,10,0,0,0,False,False,False,False,3D01410122019A01A401AE01B8014A01FE01080212021C0203FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case074-F2-S7-V8,2,7,1,1,0,0,0,False,False,False,False,410141019A01A401AE01B801FFFFFE01080212021C021C0202FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case075-F2-S7-V9,2,7,0,0,0,0,0,False,False,False,False,400140019A01A401AE01B801FFFFFE01080212021C021C0203FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case087-F3-S1-V10,3,1,-1,-1,0,0,0,False,False,False,False,3F013F019A01A401AE01B801FFFFFE01080212021C021C0200FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case099-F3-S3-V0,3,3,30,0,0,0,0,False,False,False,False,430140015E019A01A401AE01B8014001FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case114-F3-S4-V4,3,4,30,10,0,0,0,False,False,False,False,440141015E019A01A401AE01B8014A01FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case129-F3-S5-V8,3,5,1,1,0,0,0,False,False,False,False,410141019A01A401AE01B801FFFFFE01080212021C021C0202FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case143-F3-S7-V0,3,7,30,0,0,0,0,False,False,False,False,470140015E019A01A401AE01B8014001FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case154-FleetKind1,2,3,30,0,1,331,320,False,False,False,False,400140015E019A01A401AE01B8014001FE01080212021C020108,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case155-FleetKind2,2,3,30,0,2,331,320,False,False,False,False,400140015E019A01A401AE01B8014001FE01080212021C020108,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case156-FleetKind9,2,3,30,0,9,331,320,False,False,False,False,420140015E019A01A401AE01B8014001FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case157-FleetKind10,2,3,30,0,10,331,320,False,False,False,False,400140015E019A01A401AE01B8014001FE01080212021C020108,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case158-FleetKind11,2,3,30,0,11,331,320,False,False,False,False,400140015E019A01A401AE01B8014001FE01080212021C020108,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case159-FleetEdgeX,2,3,30,0,1,332,320,False,False,False,False,420140015E019A01A401AE01B8014001FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case160-FleetEdgeY,2,3,30,0,1,331,330,False,False,False,False,420140015E019A01A401AE01B8014001FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case161-FleetCorner,2,3,30,0,1,331,329,False,False,False,False,400140015E019A01A401AE01B8014001FE01080212021C020108,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case162-TerrainInside,2,3,30,0,1,331,320,True,False,False,False,400140015E019A01A401AE01B8014001FE01080212021C0201FE,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case163-TerrainEdgeX,2,3,30,0,1,332,320,True,False,False,False,420140015E019A01A401AE01B8014001FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case164-TerrainEdgeY,2,3,30,0,1,331,330,True,False,False,False,420140015E019A01A401AE01B8014001FE01080212021C0201FF,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case165-TerrainCorner,2,3,30,0,1,331,329,True,False,False,False,400140015E019A01A401AE01B8014001FE01080212021C0201FE,-1,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case166-ArrivalLock,2,3,2,0,0,0,0,False,True,False,False,4201400142019A01A401AE01B8014001FE01080212021C0201FF,-1,F4B9734FF8775BFD6C3B21561797C3A46CF307026023A8D33B6F87003C10A7DB",
                    "Case167-ArrivalSelection,2,3,2,0,0,0,0,False,False,False,True,420140019A01A401AE01B801FFFFFE01080212021C021C0201FF,2,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                    "Case168-ArrivalOtherSelection,2,3,2,0,0,0,0,False,False,True,True,420140019A01A401AE01B801FFFFFE01080212021C021C0201FF,3,93B750CDABA989A539174CB4412E859A62F568A8D1EA1C5C8F1A257D5981404C",
                };
                foreach(string fixture in fixtures){
                    string[] f=fixture.Split(',');var pool=new byte[250*492];var terrain=new byte[500*492];var aux=new byte[PracticeMovementState.AuxiliaryBytes];
                    for(int i=0;i<250;i++)BitConverter.GetBytes((short)i).CopyTo(pool,i*492+4);
                    int at=7*492,dx=int.Parse(f[3]),dy=int.Parse(f[4]);BitConverter.GetBytes(1).CopyTo(pool,at);BitConverter.GetBytes((short)320).CopyTo(pool,at+12);BitConverter.GetBytes((short)320).CopyTo(pool,at+14);BitConverter.GetBytes(short.Parse(f[2])).CopyTo(pool,at+60);pool[at+62]=3;pool[at+482]=255;pool[at+485]=byte.Parse(f[1]);
                    for(int i=0;i<5;i++){BitConverter.GetBytes((short)(i==0?320+dx:400+i*10)).CopyTo(pool,at+40+i*2);BitConverter.GetBytes((short)(i==0?320+dy:500+i*10)).CopyTo(pool,at+50+i*2);}
                    int collision=int.Parse(f[5]);if(collision!=0){bool isTerrain=bool.Parse(f[8]);byte[] buffer=isTerrain?terrain:pool;int offset=isTerrain?0:8*492;BitConverter.GetBytes(collision).CopyTo(buffer,offset);BitConverter.GetBytes(short.Parse(f[6])).CopyTo(buffer,offset+12);BitConverter.GetBytes(short.Parse(f[7])).CopyTo(buffer,offset+14);}
                    if(bool.Parse(f[9]))BitConverter.GetBytes(1).CopyTo(aux,0x50+7*312);
                    int selected=bool.Parse(f[10])?8:7;if(selected==8){BitConverter.GetBytes(17).CopyTo(aux,0x50+8*312);aux[0x3c+8*312]=17;}
                    var expected=(byte[])pool.Clone();int position=0;foreach(int field in new[]{12,13,14,15,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,62,482})expected[at+field]=Convert.ToByte(f[12].Substring(position++*2,2),16);
                    var state=new PracticeMovementState(pool,terrain,aux,selected,bool.Parse(f[11])?3:-1);var update=state.Step(7);
                    Require(Hash(update.State.CopyFleetPool())==Hash(expected)&&update.State.SelectedWaypoint==int.Parse(f[13]),"Native movement/fleet selection "+f[0]);
                    Require(Hash(update.State.CopyAuxiliary())==f[14],"Native complete movement auxiliary data "+f[0]);
                    Require(Hash(state.CopyFleetPool())==Hash(pool),"Step mutated its input "+f[0]);
                    var scheduled=state.StepSelectedFleetPoll(new PracticeFleetScheduler(0,0),7);
                    Require(scheduled.FleetUpdate!=null&&Hash(scheduled.State.CopyFleetPool())==Hash(expected)&&
                        Hash(scheduled.State.CopyAuxiliary())==f[14]&&scheduled.State.SelectedWaypoint==int.Parse(f[13]),
                        "Scheduled selected-fleet composition "+f[0]);
                }
            });
            check("Selected-fleet study polls retain original 32/31 cadence and atomic failure",delegate {
                var pool=new byte[250*492];
                for(int i=0;i<250;i++)BitConverter.GetBytes((short)i).CopyTo(pool,i*492+4);
                int at=7*492;BitConverter.GetBytes(1).CopyTo(pool,at);W16(pool,at+12,320);W16(pool,at+14,320);
                W16(pool,at+60,3);pool[at+485]=2;
                for(int i=0;i<5;i++){W16(pool,at+40+i*2,i==0?350:-1);W16(pool,at+50+i*2,320);}
                var state=new PracticeMovementState(pool,new byte[PracticeMovementState.TerrainBytes],new byte[PracticeMovementState.AuxiliaryBytes],7,-1);
                var schedule=new PracticeFleetScheduler(0,0);int updates=0;
                for(int poll=1;poll<=64;poll++){
                    var before=state;var next=state.StepSelectedFleetPoll(schedule,7);state=next.State;schedule=next.NextSchedule;
                    if(next.FleetUpdate!=null){updates++;Require(poll==1||poll==33||poll==64,"Original slot7 cadence");}
                    else Require(object.ReferenceEquals(before,state),"Unvisited fleet changed during study poll");
                }
                Require(updates==3&&state.Origin(7)==new Point(326,320)&&schedule.BatchStart==8&&schedule.CompletedCycles==2,"Source poll/movement composition");
                byte[] unsupported=state.CopyFleetPool();unsupported[at+485]=0;
                var rejected=new PracticeMovementState(unsupported,state.CopyTerrainPool(),state.CopyAuxiliary(),7,-1);
                string hash=Hash(rejected.CopyFleetPool());var phase=new PracticeFleetScheduler(4,731);bool unsupportedRejected=false;
                try{rejected.StepSelectedFleetPoll(phase,7);}catch(NotSupportedException){unsupportedRejected=true;}
                Require(unsupportedRejected&&Hash(rejected.CopyFleetPool())==hash&&phase.BatchStart==4&&phase.CompletedCycles==731,"Unsupported poll changed input");
                RejectArgument(delegate{state.StepSelectedFleetPoll(schedule,250);});
            });
            check("Practice waypoint data matches 33 original append and selection contexts",delegate {
                // Captured from supported Bs.exe41377C..4138BA, checkpoint217.
                // name,kind,selection,type,x,y,terrain,before20B,after20B,selection,notification
                string[] fixtures={
                    "Blocked-K1,1,-1,1,417,367,2,FFFFFFFFFFFFFFFFFFFF00000000000000000000,FFFFFFFFFFFFFFFFFFFF00000000000000000000,-1,0",
                    "Blocked-K11,11,-1,1,417,367,2,FFFFFFFFFFFFFFFFFFFF00000000000000000000,9A01FFFFFFFFFFFFFFFF68010000000000000000,-1,1",
                    "Blocked-K2,2,-1,1,417,367,2,FFFFFFFFFFFFFFFFFFFF00000000000000000000,9A01FFFFFFFFFFFFFFFF68010000000000000000,-1,1",
                    "Blocked-K9,9,-1,1,417,367,2,FFFFFFFFFFFFFFFFFFFF00000000000000000000,FFFFFFFFFFFFFFFFFFFF00000000000000000000,-1,0",
                    "Boundary0,1,-1,1,0,0,1,FFFFFFFFFFFFFFFFFFFF00000000000000000000,0000FFFFFFFFFFFFFFFF00000000000000000000,-1,1",
                    "Boundary10,1,-1,1,10,10,1,FFFFFFFFFFFFFFFFFFFF00000000000000000000,0A00FFFFFFFFFFFFFFFF0A000000000000000000,-1,1",
                    "Boundary629,1,-1,1,629,629,1,FFFFFFFFFFFFFFFFFFFF00000000000000000000,6C02FFFFFFFFFFFFFFFF6C020000000000000000,-1,1",
                    "Boundary630,1,-1,1,630,630,1,FFFFFFFFFFFFFFFFFFFF00000000000000000000,7602FFFFFFFFFFFFFFFF76020000000000000000,-1,1",
                    "Boundary639,1,-1,1,639,639,1,FFFFFFFFFFFFFFFFFFFF00000000000000000000,7602FFFFFFFFFFFFFFFF76020000000000000000,-1,1",
                    "Boundary9,1,-1,1,9,9,1,FFFFFFFFFFFFFFFFFFFF00000000000000000000,0000FFFFFFFFFFFFFFFF00000000000000000000,-1,1",
                    "Duplicate0,1,-1,1,109,207,1,64006E00780082008C00C800D200DC00E600F000,64006E00780082008C00C800D200DC00E600F000,0,1",
                    "Duplicate1,1,-1,1,119,217,1,64006E00780082008C00C800D200DC00E600F000,64006E00780082008C00C800D200DC00E600F000,1,1",
                    "Duplicate2,1,-1,1,129,227,1,64006E00780082008C00C800D200DC00E600F000,64006E00780082008C00C800D200DC00E600F000,2,1",
                    "Duplicate3,1,-1,1,139,237,1,64006E00780082008C00C800D200DC00E600F000,64006E00780082008C00C800D200DC00E600F000,3,1",
                    "Duplicate4,1,-1,1,149,247,1,64006E00780082008C00C800D200DC00E600F000,64006E00780082008C00C800D200DC00E600F000,4,1",
                    "DuplicateAfterHole,1,-1,1,149,249,1,6400FFFF780082008C00C800D200DC00E600F000,6400FFFF780082008C00C800D200DC00E600F000,4,1",
                    "HoleFirst,1,-1,1,417,367,1,6400FFFF780082008C00C800D200DC00E600F000,64009A01780082008C00C8006801DC00E600F000,-1,1",
                    "K1-Count0,1,-1,1,417,367,1,FFFFFFFFFFFFFFFFFFFF00000000000000000000,9A01FFFFFFFFFFFFFFFF68010000000000000000,-1,1",
                    "K1-Count2,1,-1,1,417,367,1,64006E00FFFFFFFFFFFFC800D200000000000000,64006E009A01FFFFFFFFC800D200680100000000,-1,1",
                    "K1-Count5,1,-1,1,417,367,1,64006E00780082008C00C800D200DC00E600F000,64006E00780082008C00C800D200DC00E600F000,-1,0",
                    "K11-Count0,11,-1,1,417,367,1,FFFFFFFFFFFFFFFFFFFF00000000000000000000,9A01FFFFFFFFFFFFFFFF68010000000000000000,-1,1",
                    "K11-Count2,11,-1,1,417,367,1,64006E00FFFFFFFFFFFFC800D200000000000000,64006E009A01FFFFFFFFC800D200680100000000,-1,1",
                    "K11-Count5,11,-1,1,417,367,1,64006E00780082008C00C800D200DC00E600F000,64006E00780082008C00C800D200DC00E600F000,-1,0",
                    "K2-Count0,2,-1,1,417,367,1,FFFFFFFFFFFFFFFFFFFF00000000000000000000,9A01FFFFFFFFFFFFFFFF68010000000000000000,-1,1",
                    "K2-Count2,2,-1,1,417,367,1,64006E00FFFFFFFFFFFFC800D200000000000000,64006E009A01FFFFFFFFC800D200680100000000,-1,1",
                    "K2-Count5,2,-1,1,417,367,1,64006E00780082008C00C800D200DC00E600F000,64006E00780082008C00C800D200DC00E600F000,-1,0",
                    "K9-Count0,9,-1,1,417,367,1,FFFFFFFFFFFFFFFFFFFF00000000000000000000,9A01FFFFFFFFFFFFFFFF68010000000000000000,-1,1",
                    "K9-Count2,9,-1,1,417,367,1,64006E00FFFFFFFFFFFFC800D200000000000000,64006E009A01FFFFFFFFC800D200680100000000,-1,1",
                    "K9-Count5,9,-1,1,417,367,1,64006E00780082008C00C800D200DC00E600F000,64006E00780082008C00C800D200DC00E600F000,-1,0",
                    "Selected0,1,0,1,417,367,1,64006E00FFFFFFFFFFFFC800D200000000000000,64006E00FFFFFFFFFFFFC800D200000000000000,0,0",
                    "Selected2,1,2,1,417,367,1,64006E00FFFFFFFFFFFFC800D200000000000000,64006E00FFFFFFFFFFFFC800D200000000000000,2,0",
                    "Selected4,1,4,1,417,367,1,64006E00FFFFFFFFFFFFC800D200000000000000,64006E00FFFFFFFFFFFFC800D200000000000000,4,0",
                    "Type0-Blocked,1,-1,0,417,367,2,FFFFFFFFFFFFFFFFFFFF00000000000000000000,9A01FFFFFFFFFFFFFFFF68010000000000000000,-1,1",
                };
                foreach(string fixture in fixtures){
                    string[] f=fixture.Split(',');var scene=new Scenario();for(int i=0;i<4096;i++)scene.Cells[i]=new Cell{Kind=1};
                    int x=int.Parse(f[4]),y=int.Parse(f[5]);scene.At(x/10,y/10).Kind=byte.Parse(f[6]);
                    var record=new byte[492];BitConverter.GetBytes(int.Parse(f[1])).CopyTo(record,0);BitConverter.GetBytes((short)380).CopyTo(record,12);BitConverter.GetBytes((short)350).CopyTo(record,14);record[482]=255;
                    for(int i=0;i<20;i++)record[40+i]=Convert.ToByte(f[7].Substring(i*2,2),16);
                    var expected=(byte[])record.Clone();for(int i=0;i<20;i++)expected[40+i]=Convert.ToByte(f[8].Substring(i*2,2),16);
                    var queue=new PracticeWaypointQueue(record,scene,int.Parse(f[3]),int.Parse(f[2]));var result=queue.AppendOrSelect(new Point(x,y));
                    Require(Hash(queue.CopyRecord())==Hash(expected)&&queue.Selected==int.Parse(f[9]),"Native waypoint record/selection "+f[0]);
                    Require(((result==PracticeWaypointResult.Added||result==PracticeWaypointResult.Selected)?1:0)==int.Parse(f[10]),"Native waypoint outcome "+f[0]);
                }
            });
            check("Practice source counter wrap and extra equipment preserve pool bytes",delegate {
                var catalog=PracticeUnitCatalog.Load(Path.Combine(Path.GetDirectoryName(assets),"Programs"));
                var scene=Scenario.Load(Path.Combine(assets,"Graphics","BS201.SCN"));
                var wrapped=PracticeFleetSnapshot.Create(catalog,scene.Groups[0][0][0],3,new Point(34,12),uint.MaxValue);
                Require(wrapped.NextCounter==0&&wrapped.CopyFleetRecord()[484]==255,"Original owner counter wrap/truncation");
                Require(Hash(wrapped.CopyShipPool())=="A3133ED6A205FAAA217AD276A8216680B6E34FF8CF89486D6D972EA833E6F8E5"&&Hash(wrapped.CopyFleetPool())=="326655C690A49CBA614311B11212B32F10FBBD7EE53045EB27114A41FF21079F","Source205 wrapped full-pool fixture");
                byte[] payload=new byte[400];for(int i=0;i<100;i++)payload[i*4]=1;payload[(2*10+3)*4]=27;
                var extra=PracticeFleetSnapshot.Create(catalog,payload,0,Point.Empty,0);
                Require(extra.Units.Count==1&&extra.Units[0].Type==14&&Hash(extra.CopyShipPool())=="E399BC1A9E6D3764A637F83C4A428593DBF13F898E5AFD09CBD509DF37593BDC","Source204 equipment39 full-pool fixture");
            });
            check("Practice unit records reject unsupported inputs and preserve owned data",delegate {
                string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var catalog=PracticeUnitCatalog.Load(programs);
                byte[] payload=Scenario.Load(Path.Combine(assets,"Graphics","BS201.SCN")).Groups[0][0][0];string before=Hash(payload);
                var fleet=PracticeFleetSnapshot.Create(catalog,payload,0,Point.Empty,0);Require(Hash(payload)==before,"Modified source fleet record");
                byte[] copy=fleet.CopyShipPool();copy[0]^=1;Require(fleet.Units[0].Type==3,"Mutable snapshot pool");
                copy=fleet.Units[0].CopyRecord();copy[0]^=1;Require(fleet.Units[0].Type==3,"Mutable unit record");
                bool readonlyUnits=false;try{((IList<PracticeUnitRecord>)fleet.Units).Clear();}catch(NotSupportedException){readonlyUnits=true;}Require(readonlyUnits,"Mutable roster");
                Reject(delegate{PracticeFleetSnapshot.Create(catalog,new byte[399],0,Point.Empty,0);});
                RejectArgument(delegate{PracticeFleetSnapshot.Create(catalog,payload,4,Point.Empty,0);});
                RejectArgument(delegate{PracticeFleetSnapshot.Create(catalog,payload,0,new Point(32768,0),0);});
                copy=(byte[])payload.Clone();copy[0]=255;Reject(delegate{PracticeFleetSnapshot.Create(catalog,copy,0,Point.Empty,0);});
                byte[][] tables={File.ReadAllBytes(Path.Combine(programs,"UnitTemplates.seqbin")),File.ReadAllBytes(Path.Combine(programs,"UnitAmmunition.seqbin")),File.ReadAllBytes(Path.Combine(programs,"UnitOffense.seqbin")),File.ReadAllBytes(Path.Combine(programs,"FleetColors.seqbin")),File.ReadAllBytes(Path.Combine(programs,"UnitMarkerMap.seqbin"))};
                tables[0][0]^=1;Reject(delegate{PracticeUnitCatalog.Read(tables[0],tables[1],tables[2],tables[3],tables[4]);});
            });
            check("Classic grid matches the original whole-world software buffer",delegate {
                var original=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));
                string unchanged=Hash(original.Pixels);var grid=ClassicGrid.Compose(original);
                byte[] viewport=new byte[640*480];Array.Copy(grid.Pixels,viewport,viewport.Length);
                // Whole Bs.exe world draw before queue flush, two original layouts
                // and both resource branches: checkpoint110, supported executable.
                Require(Hash(viewport)=="8F321D89F6587DB5120EFD7E595C17A30C90D6135BBF7C0CF908F5D5B45FD58D","Native grid/border byte identity");
                int differences=0;for(int i=0;i<original.Pixels.Length;i++)if(original.Pixels[i]!=grid.Pixels[i]){differences++;Require(i<viewport.Length,"PCX storage tail changed");}
                Require(differences==807&&Hash(original.Pixels)==unchanged&&!object.ReferenceEquals(original.Pixels,grid.Pixels),"Grid altered source or unexpected pixels");
                Require(Hash(ClassicGrid.Compose(grid).Pixels)==Hash(grid.Pixels),"Repeated grid composition changed pixels");
                Reject(delegate{ClassicGrid.Compose(null);});
                Reject(delegate{ClassicGrid.Compose(new IndexedImage{Width=641,Height=480,Pixels=new byte[641*480]});});
                Reject(delegate{ClassicGrid.Compose(new IndexedImage{Width=640,Height=481,Pixels=new byte[640*481]});});
                Reject(delegate{ClassicGrid.Compose(new IndexedImage{Width=640,Height=480,Pixels=new byte[8]});});
                Reject(delegate{ClassicGrid.Compose(new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Visible=new bool[8]});});
            });
            check("Classic cell raster and visibility match native pixel fixtures",delegate {
                Func<byte[]> background=delegate{var b=new byte[640*480];for(int i=0;i<b.Length;i++)b[i]=(byte)((i*37+i/640*11)&255);return b;};
                int[][] fixtures={new[]{0,0,10,0},new[]{20,9,242,0},new[]{0,-1,243,1},new[]{30,20,255,-1}};
                string[] hashes={"EC02A4133AB591A669A0A3A37B502FE2741FFC9A9F1DF6FA1B885F3969480992","4BAAF207D111FB0D43BC012DF2625AAB69AAD2C798A6A72BDA9949D6BB087E2A","F17842DD1BB0304AF54796DCEBD04B2EB1AB8118F3F241F02C3AAB52FF0D17DA","CA35BBD7AD16D268D7FA37E6D9A7309F4644C03951704B93BF97A9AB1544810E"};
                for(int i=0;i<fixtures.Length;i++){var f=fixtures[i];var pixels=background();ClassicCellRaster.Fill(pixels,f[0],f[1],f[2],f[3]);Require(Hash(pixels)==hashes[i],"Native cell fill/clipping fixture "+i);}
                var map=new int[10,10];map[8,5]=2;Point[] centers={new Point(8,5),new Point(5,3),new Point(3,5),new Point(0,3),new Point(2,3)};
                var visible=background();ClassicCellRaster.Overlay(visible,map,Point.Empty,1,0,1,2,centers);
                Require(Hash(visible)=="205584FCECC5A0BB68FE34453CA038F634BC1CBD8D7CBE1FCCA23612017B5730","Native forward rotation/center visibility");
                var retired=background();ClassicCellRaster.Overlay(retired,map,Point.Empty,1,0,1,2,new[]{centers[1],centers[2],centers[3],centers[4]});Require(Hash(retired)==hashes[3],"Removed center remained visible");
                Reject(delegate{ClassicCellRaster.Fill(new byte[8],0,0,10,0);});Reject(delegate{ClassicCellRaster.Fill(background(),41,0,10,0);});Reject(delegate{ClassicCellRaster.Fill(background(),0,0,256,0);});
                Reject(delegate{ClassicCellRaster.Overlay(background(),new int[9,10],Point.Empty,0,0,1,1,new Point[0]);});Reject(delegate{ClassicCellRaster.Overlay(background(),map,Point.Empty,4,0,1,1,centers);});
            });
            check("Classic software scene preview matches native world pixels and owns snapshots",delegate {
                string graphics=Path.Combine(assets,"Graphics");var preview=ClassicScene.Preview(graphics);
                Require(preview.Width==640&&preview.Height==480&&Hash(preview.Pixels)=="F3D72FF78BBABC3BEF00C950B2AA4F32914392D33EE96179E83991D7F76CC19D","Native complete idle scene");
                var random=new NativeRandom(0);var boards=new[]{new Board(),new Board()};int[] lengths={5,5,3,4,2};
                foreach(var board in boards){var positions=OriginalPlacement.Generate(lengths,random.Next);for(int i=0;i<5;i++){var ship=RuleProfile.Recovered.Fleet[i];Require(board.Place(positions.Positions[i],ship.Length,true,ship.Name,ship.SourceType),"Scene fixture deployment");}}
                var scene=new ClassicScene(graphics);var state=new ClassicSceneState(boards[0],boards[1]){Tick=4};scene.Render(state);var snapshot=scene.Snapshot();var copied=new byte[640*480];scene.CopyPixelsTo(copied);Require(Hash(copied)==Hash(preview.Pixels)&&scene.Commands.Count==11,"Maintained scene copy/queue");
                snapshot.Pixels[0]^=1;snapshot.Palette[0]=Color.Magenta;Require(Hash(scene.Snapshot().Pixels)==Hash(copied)&&scene.Snapshot().Palette[0]!=Color.Magenta,"Snapshot aliases renderer storage");
                state.Tick=1;scene.Render(state);Require(Hash(scene.Snapshot().Pixels)=="3397F607B573EF5E27E179C8153983653892DEB046FABBC2A81D9C64E0854A38"&&scene.Commands.Count==10,"Native selected-hull blink");
                scene.Reset();Require(scene.Commands.Count==0&&Hash(scene.Snapshot().Pixels)=="EB8F3F551582267B1D3B189A69E68AEA549EE1C46462708AE28BFBC5E88B38C7","Software scene reset");
                Reject(delegate{scene.CopyPixelsTo(new byte[8]);});Reject(delegate{scene.Render(null);});state.Selected=5;Reject(delegate{scene.Render(state);});
            });
            check("Classic software result-transition HUD gate matches the original full buffer",delegate {
                string graphics=Path.Combine(assets,"Graphics");var hidden=ClassicScene.Preview(graphics,false);
                Require(Hash(hidden.Pixels)=="92D17A59FB70AC629662165CE866E04A97D950873FE1A223B6FEE3C1C2D5C11E","Native mode4 HUD gate; world hulls remain");
                Require(Hash(ClassicScene.Preview(graphics).Pixels)=="F3D72FF78BBABC3BEF00C950B2AA4F32914392D33EE96179E83991D7F76CC19D","Default HUD scene changed");
            });
            check("Classic title scene matches joined original menu poll and raster buffers",delegate {
                var scene=new ClassicTitleScene(assets);string original=Hash(scene.Card.Pixels);
                string[] expected={"01DD24C497F0AAED57C74FFF6067B1E12AD163C5C1325ECFE22FA858AD07D432","061B5F761D0576D629109F37BEE0F51146EACEA84E9004D160BA033EA703EFD3","0BA3D7FE3E0E8BF4E6DACF6115E1A90BAF4E068B33695D1AAAFA7B2078789E7F","121EB700D930890DF0E635EDC3F085A414FE7CBAE628F21BD13F1B77A103C7DB"};
                for(int selected=0;selected<4;selected++){var image=scene.Render(selected);Require(image.Width==640&&image.Height==480&&Hash(image.Pixels)==expected[selected],"Joined native title pixels "+selected);}
                Require(scene.Card.Height==512&&Hash(scene.Card.Pixels)==original,"Title composition changed original storage rows");
                Reject(delegate{scene.Render(-1);});Reject(delegate{scene.Render(4);});
            });
            string evidence=Path.Combine(Path.GetDirectoryName(assets),"Evidence");
            check("Classic software effects match original first-miss scene and retain controller ownership",delegate {
                var random=new NativeRandom(0);var boards=new[]{new Board(),new Board()};int[] lengths={5,5,3,4,2};
                foreach(var board in boards){var positions=OriginalPlacement.Generate(lengths,random.Next);for(int i=0;i<5;i++){var ship=RuleProfile.Recovered.Fleet[i];Require(board.Place(positions.Positions[i],ship.Length,true,ship.Name,ship.SourceType),"Effect scene deployment");}}
                var scene=new ClassicScene(Path.Combine(assets,"Graphics"));var state=new ClassicSceneState(boards[0],boards[1]);
                for(int frame=0;frame<=9;frame++) {
                    state.Tick=frame+1;state.Selected=frame==0?1:0;state.Phase=frame<4?0:frame<7?1:2;
                    state.MovieActive=frame>=4;state.RestoreBackground=frame<=4;
                    state.Pointer=frame==1?new Point(8,3):frame>=2&&frame<=4?new Point(11,0):new Point(-10,-10);
                    scene.Render(state);
                }
                var catalog=new ClassicVisualCatalog(File.ReadAllBytes(visualTable));var timeline=new ClassicVisualTimeline();
                var entry=timeline.Spawn(catalog.Get(27),true,Point.Empty,1);boards[1].Shoot(Point.Empty);
                state.Effects=timeline;state.Tick=11;state.Phase=3;state.EnemyResult=0;state.Pointer=new Point(-10,-10);scene.Render(state);
                Require(Hash(scene.Snapshot().Pixels)=="53AFAE047CA96F8A56890238F3A9688F405F8B929FAC1B71544E73E1A0E0B0AE"&&scene.Commands.Count==17,"Original first-miss software frame");
                Require(entry.Counter==1&&timeline.Count==1,"Renderer advanced supplied effects");
                timeline.Clear();timeline.Spawn(catalog.Get(7),false,Point.Empty,0);state.ExtendedEffects=false;scene.Render(state);
                foreach(var command in scene.Commands)Require(command.Bank!=43,"Reduced-detail sink was queued");
                state.ExtendedEffects=true;scene.Render(state);bool found=false;foreach(var command in scene.Commands)if(command.Bank==43){found=true;Require(command.Mode==3&&command.Percent==79,"Source sink origin/scale mode");}Require(found,"Extended-detail sink missing");
                timeline.Clear();timeline.Spawn(catalog.Get(27),true,Point.Empty,32);scene.Render(state);found=false;foreach(var command in scene.Commands)if(command.Bank==37&&command.Frame==32)found=true;Require(found,"Original empty terminal slot lost its queue entry");
                string before=Hash(scene.Snapshot().Pixels);timeline.Spawn(catalog.Get(27),false,new Point(-1,0),0);Reject(delegate{scene.Render(state);});Require(Hash(scene.Snapshot().Pixels)==before,"Invalid effect mutated the scene");
            });
            check("Classic software text matches original idle scene and rejects invalid queues before drawing",delegate {
                string graphics=Path.Combine(assets,"Graphics");
                var image=ClassicScene.TextPreview(graphics,File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","MessageShipNames.seqbin")));
                Require(Hash(image.Pixels)=="2D67BE6363337F57BC6F4A7767C44F8E5A33E2688816F9F1D7C2E4F7272C4CD9","Original text-inclusive idle scene");
                var random=new NativeRandom(0);var boards=new[]{new Board(),new Board()};int[] lengths={5,5,3,4,2};
                foreach(var board in boards){var positions=OriginalPlacement.Generate(lengths,random.Next);for(int i=0;i<5;i++){var ship=RuleProfile.Recovered.Fleet[i];Require(board.Place(positions.Positions[i],ship.Length,true,ship.Name,ship.SourceType),"Text scene deployment");}}
                var scene=new ClassicScene(graphics);var state=new ClassicSceneState(boards[0],boards[1]){Tick=1,Text=new[]{new ClassicSceneText(12,11,3,"Select Target Square")}};
                scene.Render(state);Require(Hash(scene.Snapshot().Pixels)==Hash(image.Pixels)&&scene.Commands.Count==10,"Text changed source world command queue");
                string before=Hash(scene.Snapshot().Pixels);state.Text=new ClassicSceneText[]{null};Reject(delegate{scene.Render(state);});Require(Hash(scene.Snapshot().Pixels)==before,"Invalid text changed canvas");
                var excessive=new List<ClassicSceneText>();for(int i=0;i<10;i++)excessive.Add(new ClassicSceneText(0,0,3,new string('A',99)));state.Text=excessive;Reject(delegate{scene.Render(state);});Require(Hash(scene.Snapshot().Pixels)==before,"Oversized source text queue changed canvas");
                Reject(delegate{new ClassicSceneText(0,0,2,"A");});Reject(delegate{new ClassicSceneText(32768,0,3,"A");});Reject(delegate{new ClassicSceneText(0,0,3,"A\nB");});Reject(delegate{new ClassicSceneText(0,0,3,new string('A',100));});
            });
            check("Classic software movie/world/text composition matches original F18 frame",delegate {
                string graphics=Path.Combine(assets,"Graphics");
                var preview=ClassicScene.MoviePreview(graphics,File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","MessageShipNames.seqbin")));
                Require(Hash(preview.Pixels)=="7CC7CEA62985761B8C3D13A91DA5193A06F0CB322161C012C59F4D167CDAC365","Original streamed movie/world/text frame");
                var scene=new ClassicScene(graphics);var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS","F18FIRE1.PAT")));
                var pixels=(byte[])movie.Frames[1].Pixels.Clone();scene.ApplyMovieFrame(pixels,160,120);string before=Hash(scene.Snapshot().Pixels);pixels[0]^=1;Require(Hash(scene.Snapshot().Pixels)==before,"Movie frame aliases caller data");
                Reject(delegate{scene.ApplyMovieFrame(new byte[4],160,120);});Reject(delegate{scene.ApplyMovieFrame(new byte[640*480],640,480);});Require(Hash(scene.Snapshot().Pixels)==before,"Invalid movie input changed canvas");
                scene.Reset();Require(Hash(scene.Snapshot().Pixels)=="EB8F3F551582267B1D3B189A69E68AEA549EE1C46462708AE28BFBC5E88B38C7","Movie reset did not restore PCX");
            });
            check("Classic software high movie uses original two-times expansion",delegate {
                var preview=ClassicScene.MoviePreview(Path.Combine(assets,"Graphics"),File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","MessageShipNames.seqbin")),true);
                Require(Hash(preview.Pixels)=="9DC9EE2B201C47099C9C43715B9925C0BA3D00ED5CADA2D925B950D970FBE454","Original high movie/world/text frame");
            });
            check("Original cursor resources preserve hotspots and all monochrome DIB bytes",delegate{
                foreach(string name in new[]{"TargetCursor","HandCursor"}){
                    byte[] raw=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs",name+".seqbin"));var parsed=ClassicCursorData.Read(raw);byte[] cur=parsed.CurFile();
                    Require(parsed.Hotspot==(name=="TargetCursor"?new Point(15,15):new Point(11,16)),"Original named cursor hotspot");
                    Require(cur.Length==326&&BitConverter.ToUInt16(cur,2)==2&&BitConverter.ToUInt16(cur,4)==1&&cur[6]==32&&cur[7]==32&&BitConverter.ToUInt16(cur,10)==parsed.Hotspot.X&&BitConverter.ToUInt16(cur,12)==parsed.Hotspot.Y&&BitConverter.ToInt32(cur,14)==304&&BitConverter.ToInt32(cur,18)==22,"CUR directory/offset");
                    for(int i=0;i<304;i++)Require(cur[i+22]==raw[i+4],"CUR changed original AND/XOR/palette metadata");
                    string before=Hash(cur);raw[307]^=1;cur[325]^=1;Require(Hash(parsed.CurFile())==before,"Cursor parser aliases input/export buffers");
                }
            });
            check("Cursor reader rejects malformed lengths, hotspots and unsupported DIB variants",delegate{
                byte[] raw=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","TargetCursor.seqbin"));Reject(delegate{ClassicCursorData.Read(null);});Reject(delegate{ClassicCursorData.Read(new byte[307]);});Reject(delegate{ClassicCursorData.Read(new byte[309]);});
                foreach(int at in new[]{0,2,4,8,12,16,18,20,24,36,40}){byte[] bad=(byte[])raw.Clone();bad[at]=at<4?(byte)32:(byte)(bad[at]+1);Reject(delegate{ClassicCursorData.Read(bad);});}
            });
            checkEvidence("Credits palette subsets match native quality/state branches and preserve font colors",delegate{
                byte[] island=File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")),basis=ClassicPalette.Patched(island),pal=new byte[768];for(int i=0;i<768;i++)pal[i]=(byte)((i*13+17)%64);string original=Hash(pal);var rows=File.ReadAllLines(Path.Combine(evidence,"Credits-PaletteSubset-Oracle.csv"));Require(rows.Length==7,"Palette seam cases");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');Require(Hash(ClassicPalette.MovieSubset(pal,basis,int.Parse(f[0]),int.Parse(f[1])))==f[2],"Native palette subset "+i);}Require(Hash(pal)==original,"Movie palette mutated source input");var actual=PatMovie.ClipColors(pal,island,true);var gp=ClassicPalette.Gameplay(island);for(int i=0;i<256;i++)Require(actual[i]==(i>=20&&i<=137?Color.FromArgb(pal[i*3]<<2,pal[i*3+1]<<2,pal[i*3+2]<<2):gp[i]),"Credits display subset");var font=Assets.Sph(Path.Combine(assets,"Graphics","FEFONT.SPH"),gp);var indices=new HashSet<byte>();for(int i=376;i<470;i++)for(int p=0;p<font[i].Pixels.Length;p++)if(font[i].Visible[p]&&font[i].Pixels[p]!=0)indices.Add(font[i].Pixels[p]);Require(indices.SetEquals(new byte[]{10,12,209}),"Native front-end style4 palette indices");foreach(byte value in indices)Require(actual[value]==gp[value],"Movie recolored credits text");
            });
            checkEvidence("Credits immediate start and same-poll EOF replacement match native seam traces",delegate{
                var groups=new Dictionary<string,List<string[]>>();foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Credits-Seam-Oracle.csv"))){var f=row.Split(',');if(!f[0].StartsWith("normal-")&&f[0]!="two-frame")continue;if(!groups.ContainsKey(f[0]))groups[f[0]]=new List<string[]>();groups[f[0]].Add(f);}Require(groups.Count==3,"Native normal seam groups");byte[] island=ClassicPalette.Patched(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")));
                foreach(var group in groups){int[] counts=group.Key=="two-frame"?new[]{2,2}:new[]{3,4};int id=1,loads=1,stops=0,clears=0;var timeline=new ClipTimeline(CreditsPresentation.VisibleFrames(counts[0]),50);timeline.Play();foreach(var f in group.Value){int tick=int.Parse(f[1]);if(tick>0){timeline.Advance(50);if(timeline.Completed){stops++;clears++;loads++;id=id==1?2:1;timeline=new ClipTimeline(CreditsPresentation.VisibleFrames(counts[id-1]),50);timeline.Play();}}Require(int.Parse(f[3])==id&&int.Parse(f[4])==timeline.Frame+1&&int.Parse(f[9])==loads&&int.Parse(f[10])==stops&&int.Parse(f[11])==clears&&int.Parse(f[12])==loads,"Native seam cursor "+group.Key+"/"+tick);var pixels=new byte[640*480];for(int i=0;i<pixels.Length;i++)pixels[i]=(byte)(20+(id*19+timeline.Frame)%118);if(tick>0)pixels[0]=209;Require(Hash(pixels)==f[13],"Native visible frame/clear "+group.Key+"/"+tick);byte[] pal=new byte[768];for(int i=0;i<768;i++)pal[i]=(byte)((i*13+id*17)%64);Require(Hash(ClassicPalette.MovieSubset(pal,island,group.Key=="normal-high"?2:1,41))==f[14],"Native shared palette at transition");}}
            });
            checkEvidence("Credits skip advances one final source frame while paused Back retains its snapshot",delegate{
                var rows=File.ReadAllLines(Path.Combine(evidence,"Credits-Seam-Oracle.csv"));var previous=new Dictionary<string,int>();int cases=0;foreach(string row in rows){var f=row.Split(',');if(f[0]!="space-tick1"&&f[0]!="space-completion"&&f[0]!="mouse-left")continue;int frame=int.Parse(f[4])-1;if(f[6]=="0"&&int.Parse(f[1])>0){Require(frame==CreditsPresentation.SkipFrame(previous[f[0]],3)&&int.Parse(f[5])==0&&int.Parse(f[6])==0&&int.Parse(f[11])==0&&int.Parse(f[12])==1,"Native skip completion/clear rule");cases++;}previous[f[0]]=frame;}Require(cases==3,"Native skip cases");Reject(delegate{CreditsPresentation.VisibleFrames(1);});Reject(delegate{CreditsPresentation.VisibleFrames(1025);});Require(CreditsPresentation.VisibleFrames(2)==1&&CreditsPresentation.VisibleFrames(46)==45,"Credits visible frame bounds");
            });
            checkEvidence("Credits indexed scaling/raster and finite audio prefix retain native units",delegate{
                var colors=new Color[256];for(int i=0;i<256;i++)colors[i]=Color.FromArgb(i,i,i);foreach(int scale in new[]{2,4}){byte[] pixels=File.ReadAllBytes(Path.Combine(evidence,"Scale"+scale+"-Input.idx")),expected=File.ReadAllBytes(Path.Combine(evidence,"Scale"+scale+"-Output.idx"));var source=new IndexedImage{Width=640/scale,Height=480/scale,Pixels=pixels,Palette=colors};Require(Hash(CreditsPresentation.Scale(source).Pixels)==Hash(expected),"Native indexed progressive scaler");}List<SoundCue> cues;List<string> missing;var full=ClipSound.LoadMixWithGaps(assets,"ISL_DSHS",86,50,out cues,out missing);var shortMix=full.Prefix(ClipSound.FrameSample(CreditsPresentation.VisibleFrames(86),50));Require(shortMix.Count==46856&&full.Count==47407,"Native Credits EOF duration");foreach(int level in new[]{0,35,100}){var a=full.Pcm(level);var b=shortMix.Pcm(level);for(int i=0;i<b.Length;i++)Require(a[i]==b[i],"Credits gain/prefix samples");}Reject(delegate{full.Prefix(0);});Reject(delegate{full.Prefix(full.Count+1);});
            });
            checkEvidence("Credits catalog matches all274native resource requests and rejects altered tables",delegate{
                var catalog=CreditsCatalog.Load(assets);Require(catalog.Names.Length==137,"Credits catalog size");var rows=File.ReadAllLines(Path.Combine(evidence,"Credits-Catalog-Requests-Oracle.csv"));Require(rows.Length==275,"Catalog request fixture shape");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');string name=catalog.Name(int.Parse(f[7]));Require(name==f[20]&&catalog.Directory(name,f[9]=="2")== (f[19]=="H"?"HWEBS":"LWEBS")&&f[10]=="0","Native catalog request "+i);}var names=catalog.Names;names[0]="changed";Require(catalog.Name(1)=="BATFIRE1","Catalog name exposure");byte[] source=(byte[])catalog.OriginalBytes.Clone();source[9]^=1;Reject(delegate{CreditsCatalog.Read(source);});Reject(delegate{CreditsCatalog.Read(new byte[1241]);});Reject(delegate{catalog.Directory("../bad",false);});foreach(int id in new[]{0,138}){bool rejected=false;try{catalog.Name(id);}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Catalog ID boundary");}
            });
            checkEvidence("Full Credits catalog matches30358native decoded frame hashes and source framing",delegate{
                var groups=new Dictionary<string,List<string[]>>();foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Credits-Catalog-Frames-Oracle.csv"))){if(row.StartsWith("Directory,"))continue;var f=row.Split(',');string key=f[0]+"/"+f[1];if(!groups.ContainsKey(key))groups[key]=new List<string[]>();groups[key].Add(f);}Require(groups.Count==274,"Catalog decoder fixture groups");int total=0;foreach(var group in groups){var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,group.Key.Replace('/',Path.DirectorySeparatorChar)+".PAT")));Require(movie.Frames.Count==group.Value.Count,"Catalog native frame count");foreach(var f in group.Value){int index=int.Parse(f[2]);var frame=movie.Frames[index];Require(frame.Offset==int.Parse(f[3])&&frame.PayloadBytes==int.Parse(f[4])&&Hash(frame.Pixels)==f[5],"Catalog decoder "+group.Key+" frame"+index);total++;}}Require(total==30358,"Catalog frame sweep count");
                var first=File.ReadAllLines(Path.Combine(evidence,"Credits-Catalog-FirstFrames-Oracle.csv"));Require(first.Length==275,"Catalog independent-first fixtures");for(int i=1;i<first.Length;i++){var f=first[i].Split(',');Require(f[2]=="1"&&f[3]==f[4]&&groups[f[0]+"/"+f[1]][0][5]==f[3],"Native first-frame independence");}
            });
            checkEvidence("Full catalog cue onsets retain native sequence order including failed handles",delegate{
                var starts=new Dictionary<string,List<string[]>>();foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Credits-Catalog-Sequence-Oracle.csv"))){var f=row.Split(',');if(f.Length!=6||f[2]!="Start")continue;string key=f[0]+"/"+f[1];if(!starts.ContainsKey(key))starts[key]=new List<string[]>();starts[key].Add(f);}int cuesTotal=0,failures=0,cases=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Credits-Catalog-Sequence-Inputs.csv"))){if(row.StartsWith("Directory,"))continue;var f=row.Split(',');int frames=int.Parse(f[2]);byte[] seq=File.ReadAllBytes(Path.Combine(assets,"SFX",f[1],f[1]+".SEQ"));Require(Hash(seq)==f[3]&&Hash(File.ReadAllBytes(Path.Combine(assets,f[0],f[1]+".PAT")))==f[4],"Native sequence input identity");var cues=ClipSound.Sequence(Encoding.ASCII.GetString(seq));var expected=starts[f[0]+"/"+f[1]];int count=0;for(int i=0;i<cues.Count;i++){if(cues[i].ActualStart>=frames)continue;var native=expected[count++];Require(int.Parse(native[3])==i&&int.Parse(native[4])==cues[i].ActualStart,"Native catalog cue time "+f[1]+"/"+i);bool present=File.Exists(Path.Combine(assets,"SFX",f[1],cues[i].Name+".WAV"));Require(int.Parse(native[5])==(present?i+1:-1),"Native failed cue handle");if(!present)failures++;}Require(count==expected.Count,"Extra native cue starts");cuesTotal+=cues.Count;cases++;}Require(cases==274&&cuesTotal==2050&&failures==12,"Catalog sequence fixture totals");
            });
            check("Missing original cue files preserve remaining samples and queued onset offsets",delegate{
                List<SoundCue> cues;List<string> missing;var mix=ClipSound.LoadMixWithGaps(assets,"ISL_DSHS",86,50,out cues,out missing);Require(cues.Count==10&&missing.Count==4&&new HashSet<string>(missing).SetEquals(new[]{"SHELIN_A.WAV","FIRE45F.WAV","EXP01.WAV","EXP02.WAV"}),"Missing source files");var pcm=mix.Pcm(100);Require(pcm.Length==47407,"20fps fractional sample endpoint");short[] wave=ClipSound.Pcm(File.ReadAllBytes(Path.Combine(assets,"SFX","ISL_DSHS","SMLEXP3.WAV")));var sum=new int[pcm.Length];foreach(int frame in new[]{32,36,44,66}){int at=(int)((long)frame*11025/20);for(int i=0;i<wave.Length&&at+i<sum.Length;i++)sum[at+i]+=wave[i];}bool nonzero=false;for(int i=0;i<sum.Length;i++){short expected=(short)Math.Max(short.MinValue,Math.Min(short.MaxValue,sum[i]));Require(pcm[i]==expected,"Missing-cue mix shifted or lost surviving audio");nonzero|=pcm[i]!=0;}Require(nonzero&&cues[4].ActualStart==32&&cues[4].Slot>=0&&cues[0].Slot==-1&&cues[1].Slot==-1,"Missing cue queue/voice boundary");
                List<SoundCue> normal,partial;List<string> none;var a=ClipSound.LoadMix(assets,"BATFIRE1",46,50,out normal).Pcm(100);var b=ClipSound.LoadMixWithGaps(assets,"BATFIRE1",46,50,out partial,out none).Pcm(100);Require(none.Count==0&&a.Length==b.Length,"No-gap sample count");for(int i=0;i<a.Length;i++)Require(a[i]==b[i],"No-gap mixer changed samples");
            });
            checkEvidence("Original fleet generator matches native layouts, RNG consumption and rebuilt occupancy",delegate{
                var rows=File.ReadAllLines(Path.Combine(evidence,"Placement-Layouts-Oracle.csv"));Require(rows.Length==298,"Placement fixture case count");foreach(string row in new List<string>(rows).GetRange(1,rows.Length-1)){var f=row.Split(',');uint seed=uint.Parse(f[1]);int zero=int.Parse(f[2]),calls=0;int[] types=Array.ConvertAll(f[3].Split(';'),int.Parse),sizes=Array.ConvertAll(f[4].Split(';'),int.Parse),pointers=Array.ConvertAll(f[5].Split(';'),int.Parse);var initial=new Point[6];for(int i=0;i<6;i++){initial[i]=new Point(123,124);if(types[i]==0||pointers[i]==0)sizes[i]=0;}var rng=new NativeRandom(seed);var result=OriginalPlacement.Generate(sizes,delegate{calls++;return calls<=zero?0:rng.Next();},initial);Require(result.Calls==int.Parse(f[7])&&result.Restarts==int.Parse(f[8])&&rng.State==uint.Parse(f[9]),"Native placement RNG/restart "+f[0]);var xy=f[10].Split(';');var grid=new byte[400];for(int i=0;i<100;i++)BitConverter.GetBytes(int.Parse(f[6])==2?3:0).CopyTo(grid,i*4);for(int i=0;i<6;i++){var point=xy[i].Split(':');Require(result.Positions[i]==new Point(int.Parse(point[0]),int.Parse(point[1])),"Native placement coordinate "+f[0]+"/"+i);if(sizes[i]!=0){Require(Board.Inside(result.Positions[i])&&result.Positions[i].Y+sizes[i]<=10,"Native position bounds");for(int y=0;y<sizes[i];y++)BitConverter.GetBytes(2).CopyTo(grid,(result.Positions[i].X*10+result.Positions[i].Y+y)*4);}}Require(Hash(grid)==f[11],"Native occupancy rebuild "+f[0]);foreach(Point p in initial)Require(p==new Point(123,124),"Placement mutated prior input");}
            });
            checkEvidence("Original placement random-call traces retain tie order and full100failure restart",delegate{
                var expected=new Dictionary<string,List<string>>();foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Placement-Trace-Oracle.csv"))){if(row.StartsWith("Case,"))continue;string key=row.Substring(0,row.IndexOf(','));if(!expected.ContainsKey(key))expected[key]=new List<string>();expected[key].Add(row);}foreach(string name in new[]{"default-1","restart-202"}){var rng=new NativeRandom(1);int calls=0,zeros=name=="restart-202"?202:0;Point[] before=null;var rows=new List<string>();int[] sizes={5,5,3,4,2,0};var initial=new Point[6];for(int i=0;i<6;i++)initial[i]=new Point(123,124);var result=OriginalPlacement.Generate(sizes,delegate{int roll=++calls<=zeros?0:rng.Next();var coordinates=new List<string>();for(int i=0;i<6;i++)if(sizes[i]!=0)coordinates.Add(i+":"+before[i].X+":"+before[i].Y);rows.Add(name+","+calls+","+roll+","+rng.State+","+string.Join(";",coordinates.ToArray()));return roll;},initial,256,delegate(Point[] points){before=points;});Require(rows.Count==expected[name].Count,"Placement trace count");for(int i=0;i<rows.Count;i++)Require(rows[i]==expected[name][i],"Native placement trace "+name+"/"+i);Require(result.Restarts==(zeros==0?0:1),"Forced full restart count");}
            });
            check("Placement rejects invalid data and bounds the original unbounded restart",delegate{
                Reject(delegate{OriginalPlacement.Generate(new[]{5},delegate{return -1;});});Reject(delegate{OriginalPlacement.Generate(new[]{5},delegate{return 32768;});});int calls=0;bool stopped=false;var prior=new[]{new Point(3,4),new Point(5,0)};try{OriginalPlacement.Generate(new[]{5,5},delegate{calls++;return 0;},prior,0);}catch(InvalidOperationException e){stopped=e.Message.Contains("native loop is unbounded");}Require(stopped&&calls==202&&prior[0]==new Point(3,4)&&prior[1]==new Point(5,0),"Bounded restart/transaction boundary");foreach(int[] input in new[]{new int[0],new int[7],new[]{11},new[]{-1}}){bool rejected=false;try{OriginalPlacement.Generate(input,delegate{return 0;});}catch(ArgumentException){rejected=true;}Require(rejected,"Invalid placement shape accepted");}
            });
            check("Recovered player re-roll and enemy start use consecutive native placement draws",delegate{
                foreach(int seed in new[]{0,1,731,-1,int.MinValue}){var game=new Game(seed,RuleProfile.Recovered);var rng=new NativeRandom(unchecked((uint)seed));int[] sizes={5,5,3,4,2};for(int pass=0;pass<3;pass++){var expected=OriginalPlacement.Generate(sizes,rng.Next);Board board;if(pass<2){game.AutoPlace();board=game.Player;}else{Require(game.Start(),"Native layout start rejected");board=game.Enemy;}for(int i=0;i<5;i++)Require(board.Ships[i].Cells[0]==expected.Positions[i]&&board.Ships[i].SourceType==RuleProfile.Recovered.Fleet[i].SourceType,"Game native layout stream/roster");Require(game.PlacementState==rng.State&&game.PlacementCalls==rng.Calls,"Game placement stream state");}}
            });
            checkEvidence("Credits bounded script and native text parser preserve 210 original lines",delegate{
                byte[] bytes=File.ReadAllBytes(Path.Combine(assets,"Graphics","CREDITS.TXT"));var text=new CreditsText(bytes);Require(text.Lines.Length==210&&text.Lines[36]=="NMS Battleship Credits"&&text.Lines[209]=="Nancy Trolley ","Credits text boundaries");var rows=File.ReadAllLines(Path.Combine(evidence,"Credits-Text-Oracle.csv"));Require(rows.Length==211,"Credits text fixture shape");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');byte[] line=Encoding.ASCII.GetBytes(text.Lines[i-1]);Require(line.Length==int.Parse(f[1])&&Hash(line)==f[2],"Native Credits parsed line "+i);}Require(Hash(text.OriginalBytes)==Hash(bytes),"Credits input not preserved");Reject(delegate{new CreditsText(Encoding.ASCII.GetBytes("one\n"));});Reject(delegate{new CreditsText(new byte[]{65,128,10,10});});Reject(delegate{new CreditsText(Encoding.ASCII.GetBytes("one\rtwo\n\n"));});byte[] script=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","Credits.seqbin"));CreditsProgram.Read(script);script[16]=42;Reject(delegate{CreditsProgram.Read(script);});
            });
            checkEvidence("Front-end Credits font metrics and direct indexed raster match native fixtures",delegate{
                var palette=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")));using(var font=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),palette,true)){var metrics=File.ReadAllLines(Path.Combine(evidence,"Credits-Metrics-Oracle.csv"));Require(metrics.Length==565,"FE font fixture shape");for(int i=1;i<metrics.Length;i++){var f=metrics[i].Split(',');Require(font.Metric(int.Parse(f[0]))==new Size(int.Parse(f[1]),int.Parse(f[2])),"Native front font metric "+i);}var roll=new CreditsRoll(new CreditsText(File.ReadAllBytes(Path.Combine(assets,"Graphics","CREDITS.TXT"))));var rows=File.ReadAllLines(Path.Combine(evidence,"Credits-Pixels-Oracle.csv"));Require(rows.Length==15,"Credits pixel cases");foreach(string row in new List<string>(rows).GetRange(1,rows.Length-1)){var f=row.Split(',');byte[] pixels=new byte[640*480];for(int i=0;i<pixels.Length;i++)pixels[i]=(byte)(1+(i*7)%255);foreach(var line in roll.Commands(font,int.Parse(f[0]),int.Parse(f[1])))pixels=font.Raster(pixels,640,480,line.At.X,line.At.Y,line.Text,4);Require(Hash(pixels)==f[2],"Native Credits pixels "+f[0]+"/"+f[1]);}}
            });
            checkEvidence("Credits roll matches a complete native loop including pre-update drawing",delegate{
                var text=new CreditsText(File.ReadAllBytes(Path.Combine(assets,"Graphics","CREDITS.TXT")));var roll=new CreditsRoll(text);var palette=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")));using(var font=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),palette,true)){var rows=File.ReadAllLines(Path.Combine(evidence,"Credits-Roll-Oracle.csv"));Require(rows.Length==1472,"Credits native loop length");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');Require(roll.StateLine==int.Parse(f[1])&&roll.StateOffset==int.Parse(f[2]),"Credits before state "+i);roll.Advance(50);Require(roll.StateLine==int.Parse(f[3])&&roll.StateOffset==int.Parse(f[4]),"Credits after state "+i);var commands=roll.Commands(font);var joined=new List<string>();for(int j=0;j<commands.Count;j++){var line=commands[j];joined.Add(j+","+line.At.X+","+line.At.Y+","+Convert.ToBase64String(Encoding.ASCII.GetBytes(line.Text)));}Require(Hash(Encoding.ASCII.GetBytes(string.Join("\n",joined.ToArray())))==f[6],"Credits draw state "+i);}}
            });
            check("Credits scroll retains elapsed remainder and loops without frame partition drift",delegate{
                var text=new CreditsText(File.ReadAllBytes(Path.Combine(assets,"Graphics","CREDITS.TXT")));var a=new CreditsRoll(text);var b=new CreditsRoll(text);a.Advance(147049);for(int i=0;i<147;i++){b.Advance(17);b.Advance(983);}b.Advance(49);Require(a.Ticks==b.Ticks&&a.StateLine==b.StateLine&&a.DisplayOffset==b.DisplayOffset&&a.Ticks==2940,"Credits partition/wrap");Require(!a.Advance(0)&&a.Advance(1)&&a.StateLine==0&&a.StateOffset==-2,"Credits exact boundary");a.Reset();Require(a.Ticks==0&&a.StateOffset==0,"Credits reset");bool rejected=false;try{a.Advance(-1);}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Negative Credits elapsed accepted");
            });
            Func<List<IndexedImage>> prefFrames=delegate{return Assets.Sph(Path.Combine(assets,"Graphics","EO_PREFS.SPH"),Assets.Pcx(Path.Combine(assets,"Graphics","E_PREFS.PCX")).Palette);};
            Func<PreferencesProgram> prefProgram=delegate{return PreferencesProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","Preferences.seqbin")),prefFrames());};
            checkEvidence("Original Preferences script boundary and native thirteen-button records",delegate{
                var frames=prefFrames();byte[] data=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","Preferences.seqbin"));var program=PreferencesProgram.Read(data,frames);var rows=File.ReadAllLines(Path.Combine(evidence,"Preferences-Buttons-Oracle.csv"));Require(rows.Length==14,"Preferences button fixture shape");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');var b=program.Buttons[int.Parse(f[0])];Require(b.At==new Point(int.Parse(f[1]),int.Parse(f[2]))&&b.Action==int.Parse(f[3])&&b.Frame==int.Parse(f[4])&&b.Group==int.Parse(f[5])&&b.Size==new Size(int.Parse(f[6]),int.Parse(f[7])),"Native preference record "+i);}
                for(int i=0;i<data.Length;i+=4){var bad=(byte[])data.Clone();bad[i]^=0x40;Reject(delegate{PreferencesProgram.Read(bad,frames);});}Reject(delegate{PreferencesProgram.Read(new byte[900],frames);});var fewer=new List<IndexedImage>(frames);fewer.RemoveAt(33);Reject(delegate{PreferencesProgram.Read(data,fewer);});Reject(delegate{PreferencesProgram.Change(new PreferenceValues(),10);});Reject(delegate{PreferencesProgram.Change(new PreferenceValues{Volume=3},9);});
            });
            checkEvidence("Preferences changes and save/back/Escape boundaries match original dispatcher",delegate{
                var rows=File.ReadAllLines(Path.Combine(evidence,"Preferences-Changes-Oracle.csv"));Require(rows.Length==1121,"Preference action case count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');var state=new PreferenceValues{Quality=int.Parse(f[0]),Radar=int.Parse(f[1]),Difficulty=int.Parse(f[2]),Volume=int.Parse(f[3])};int action=int.Parse(f[4]);bool external=action==97||action==99||action==-1;var next=external?state:PreferencesProgram.Change(state,action);Require(next.Quality==int.Parse(f[5])&&next.Radar==int.Parse(f[6])&&next.Difficulty==int.Parse(f[7])&&next.Volume==int.Parse(f[8])&&f[9]==(external?"1":"0")&&f[10]==(action==97?"1":"0"),"Native preference action "+i);string calls=action==9||action==98||(action==55&&state.Volume>0)?"Volume|Preview":"";Require(f[11]==calls,"Native preference volume callback order");}
            });
            checkEvidence("Preferences half-open hits and all263volume bar positions match native input",delegate{
                var program=prefProgram();var rows=File.ReadAllLines(Path.Combine(evidence,"Preferences-Hits-Oracle.csv"));Require(rows.Length==1189,"Preference pointer matrix count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');int x=int.Parse(f[0]),y=int.Parse(f[1]),buttons=int.Parse(f[2]);int index=program.Hit(x,y,buttons==2),action=index<0?-2:program.Buttons[index].Action;int volume=buttons==1&&PreferencesProgram.VolumeBar.Contains(x,y)?PreferencesProgram.BarValue(x):int.Parse(f[4]);Require(action==int.Parse(f[5])&&volume==int.Parse(f[6]),"Native preference hit "+i);}
                rows=File.ReadAllLines(Path.Combine(evidence,"Preferences-Bar-Oracle.csv"));Require(rows.Length==264,"Volume bar extent");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');Require(PreferencesProgram.BarValue(int.Parse(f[0]))==int.Parse(f[1]),"Native bar quantization");}Require(program.Hit(-1,-1,false)==-1&&program.Hit(640,480,true)==-1,"Outside preference surface");
            });
            checkEvidence("Preferences indexed overlays, volume bar and footer match320native composites",delegate{
                var frames=prefFrames();var program=PreferencesProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","Preferences.seqbin")),frames);var card=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=new Color[256]};for(int i=0;i<card.Pixels.Length;i++)card.Pixels[i]=(byte)(1+(i*7)%255);var rows=File.ReadAllLines(Path.Combine(evidence,"Preferences-Composite-Oracle.csv"));Require(rows.Length==321,"Preference composition count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');var state=new PreferenceValues{Quality=int.Parse(f[1]),Radar=int.Parse(f[2]),Difficulty=int.Parse(f[3]),Volume=int.Parse(f[4])};Require(Hash(program.Compose(card,frames,state,int.Parse(f[5])).Pixels)==f[6],"Native preference pixels "+f[0]);}Require(card.Pixels[0]==1&&card.Pixels[142+354*640]==(byte)(1+((142+354*640)*7)%255),"Composition mutated input");Reject(delegate{program.Compose(card,frames,new PreferenceValues{Quality=3},-1);});
            });
            checkEvidence("Cinematic gain curve matches original DirectSound requests and retains headroom",delegate{
                var rows=File.ReadAllLines(Path.Combine(evidence,"Preferences-Volume-Oracle.csv"));Require(rows.Length==102,"Native gain fixture count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');int percent=int.Parse(f[0]);Require(SoundLevel.NativeBufferVolume(percent)==int.Parse(f[1])&&SoundLevel.NativeHundredthsDb(percent)==int.Parse(f[2]),"Native gain transfer");}
                Require(SoundLevel.Gain(0)==0&&SoundLevel.Gain(100)==1&&Math.Abs(SoundLevel.Gain(50)-0.1)<1e-12&&Math.Abs(SoundLevel.Gain(80)-0.3981071705534972)<1e-12,"Software gain/modern Off");var raw=new[]{80000,-80000,32767,-32768,0};var full=SoundLevel.Mix(raw,100);var lower=SoundLevel.Mix(raw,50);Require(full[0]==32767&&full[1]==-32768&&lower[0]==8000&&lower[1]==-8000&&lower[2]==3277&&lower[3]==-3277&&raw[0]==80000,"Gain must precede final saturation without mutating source");foreach(short sample in SoundLevel.Mix(raw,0))Require(sample==0,"Off is not silent");foreach(int bad in new[]{-1,101}){bool rejected=false;try{SoundLevel.Gain(bad);}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Gain bound");}
            });
            check("Original22050/16bit and11025/8bit effect WAV gain preserves metadata and rejects malformed RIFF",delegate{
                foreach(string name in new[]{"KEYCLIK2.WAV","CANNON1.WAV"}){byte[] original=File.ReadAllBytes(Path.Combine(assets,"SFX",name));string hash=Hash(original);Require(Hash(SoundLevel.Wave(original,100))==hash,"Unity effect changed");byte[] muted=SoundLevel.Wave(original,0);int at=12,data=-1,length=0,bits=0;while(at<original.Length){string tag=System.Text.Encoding.ASCII.GetString(original,at,4);int count=BitConverter.ToInt32(original,at+4);if(tag=="fmt ")bits=BitConverter.ToUInt16(original,at+8+14);if(tag=="data"){data=at+8;length=count;}at+=8+count+(count&1);}Require(data>=0&&length>0,"Independent effect chunk fixture");for(int i=0;i<original.Length;i++)Require(muted[i]==(i>=data&&i<data+length?(byte)(bits==8?128:0):original[i]),"Muted effect samples/metadata");Require(Hash(original)==hash,"Gain modified original WAV");var truncated=new byte[original.Length-1];Array.Copy(original,truncated,truncated.Length);Reject(delegate{SoundLevel.Wave(truncated,100);});var bad=(byte[])original.Clone();bad[0]=0;Reject(delegate{SoundLevel.Wave(bad,100);});}
                Reject(delegate{SoundLevel.Wave(new byte[44],100);});
            });
            check("Preferences migration, numeric bounds and unknown bindings persist",delegate{
                var old=DisplaySettings.Parse("sound=false\ncustomConfirm=Enter\n");Require(old.FxVolume==100&&old.Difficulty==0&&old.Radar==0&&!old.Sound,"Legacy preference migration changed output");var prefs=DisplaySettings.Parse("fxvolume=80\ndifficulty=3\nradar=1\ncustomConfirm=Enter\n");var again=DisplaySettings.Parse(prefs.Serialize());Require(again.FxVolume==80&&again.Difficulty==3&&again.Radar==1&&again.Serialize().Contains("customConfirm=Enter"),"Preference round trip/binding retention");foreach(string bad in new[]{"fxvolume=-5","fxvolume=101","fxvolume=3","fxvolume=NaN","difficulty=4","radar=2"}){var value=DisplaySettings.Parse(bad);Require(value.Problem!=""&&value.FxVolume==100&&value.Difficulty==0&&value.Radar==0,"Invalid preference accepted");}Reject(delegate{new DisplaySettings{FxVolume=3}.Serialize();});
            });
            Func<ClassicMessages> loadMessages=delegate{return new ClassicMessages(File.ReadAllBytes(Path.Combine(assets,"Graphics","ENGLISH.TXT")),File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","MessageShipNames.seqbin")));};
            checkEvidence("English text-mode lines and native ship-name records",delegate {
                var messages=loadMessages();var rows=File.ReadAllLines(Path.Combine(evidence,"Message-Language-Oracle.csv"));Require(rows.Length==132,"Language oracle count");for(int i=1;i<rows.Length;i++){var fields=rows[i].Split(',');byte[] line=System.Text.Encoding.ASCII.GetBytes(messages.Line(int.Parse(fields[0])));Require(line.Length==int.Parse(fields[1])&&Hash(line)==fields[2],"Native English line "+i);}
                byte[] english=File.ReadAllBytes(Path.Combine(assets,"Graphics","ENGLISH.TXT")),names=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","MessageShipNames.seqbin"));Require(messages.Line(13).EndsWith(" ",StringComparison.Ordinal),"Trailing space lost");Reject(delegate{new ClassicMessages(new byte[]{65,0,10},names);});Reject(delegate{new ClassicMessages(new byte[]{65,13,66,10},names);});Reject(delegate{new ClassicMessages(english,new byte[337]);});var bad=(byte[])names.Clone();for(int i=0;i<26;i++)bad[i]=65;Reject(delegate{new ClassicMessages(english,bad);});
            });
            checkEvidence("Classic messages match1200native dispatch/wrap cases",delegate {
                var messages=loadMessages();int cases=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Message-Commands-Oracle.csv"))){if(row.StartsWith("Case"))continue;var f=row.Split(',');int local=int.Parse(f[1]),phase=int.Parse(f[2]),type=int.Parse(f[3]),sunk=int.Parse(f[4]),result=int.Parse(f[5]),line=int.Parse(f[6]);var commands=messages.Commands(phase,local,type,sunk,result,local==0?"The Computer":"Human");if(line<0)Require(commands.Count==0,"Unexpected message");else{Require(line<commands.Count,"Missing message line");var command=commands[line];Require(command.At==new Point(int.Parse(f[7]),int.Parse(f[8]))&&f[9]=="3"&&command.Text==System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[10])),"Native message command "+f[0]);}if(line<=0)cases++;}Require(cases==1200,"Native dispatch count");
                Require(messages.Commands(3,0,1,1,0,"The Computer").Count==0&&messages.Commands(7,0,1,1,1,"The Computer").Count==0,"Hit/miss should use native icons");Reject(delegate{messages.Commands(1,0,1,1,0,"bad\nname");});
            });
            checkEvidence("Original style3font metrics/glyph positions and native raster pixels",delegate {
                using(var font=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette)){
                    var rows=File.ReadAllLines(Path.Combine(evidence,"Message-Metrics-Oracle.csv"));Require(rows.Length==565,"Metric count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');Require(font.Metric(int.Parse(f[0]))==new Size(int.Parse(f[1]),int.Parse(f[2]))&&int.Parse(f[3])==i-1,"Native font metric "+i);}
                    foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Message-Glyphs-Oracle.csv"))){if(row.StartsWith("Sample"))continue;var f=row.Split(',');string sample=System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[0]));var glyph=font.Layout(12,11,sample)[int.Parse(f[1])];Require(glyph.At==new Point(int.Parse(f[2]),int.Parse(f[3]))&&glyph.Frame==int.Parse(f[4])&&f[5]=="100"&&f[6]=="0","Native glyph placement");}
                    byte[] background=new byte[640*480];for(int i=0;i<background.Length;i++)background[i]=(byte)(1+(i*7)%255);rows=File.ReadAllLines(Path.Combine(evidence,"Message-Pixels-Oracle.csv"));Require(rows.Length==6,"Font pixel fixture count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');string sample=System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[0]));Require(Hash(font.Raster(background,640,480,12,11,sample))==f[1],"Native font pixels "+i);}Reject(delegate{font.Layout(0,0,"bad\ttext");});
                }
            });
            checkEvidence("Native pause centering and style4measure/glyph/pixel fixtures",delegate {
                var messages=loadMessages();using(var font=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette)){
                    var rows=File.ReadAllLines(Path.Combine(evidence,"Pause-Commands-Oracle.csv"));Require(rows.Length==13,"Pause case shape");foreach(string row in rows){if(row.StartsWith("Width"))continue;var f=row.Split(',');if(f[1]=="0"){Require(f[2]=="-1","Unpaused commands");continue;}var line=font.PauseLines(messages,int.Parse(f[0]),true)[int.Parse(f[2])];Require(line.At==new Point(int.Parse(f[3]),int.Parse(f[4]))&&line.Text==System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[6]))&&f[5]=="4","Native pause layout");}
                    foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Pause-Measure-Oracle.csv"))){if(row.StartsWith("Sample"))continue;var f=row.Split(',');Require(font.Measure(System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[0])),4)==int.Parse(f[1]),"Native style4measure");}
                    foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Pause-Glyphs-Oracle.csv"))){if(row.StartsWith("Sample"))continue;var f=row.Split(',');string sample=System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[0]));var glyph=font.Layout(12,11,sample,4)[int.Parse(f[1])];Require(glyph.At==new Point(int.Parse(f[2]),int.Parse(f[3]))&&glyph.Frame==int.Parse(f[4])&&f[5]=="100"&&f[6]=="0","Native style4glyph");}
                    byte[] background=new byte[640*480];for(int i=0;i<background.Length;i++)background[i]=(byte)(1+(i*7)%255);rows=File.ReadAllLines(Path.Combine(evidence,"Pause-Pixels-Oracle.csv"));Require(rows.Length==6,"Pause pixel sample count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');Require(Hash(font.Raster(background,640,480,12,11,System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[0])),4))==f[1],"Native style4pixels");}
                }
            });
            check("Adapted pause prompts preserve binding and reject unsupported typography",delegate {
                using(var font=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette)){
                    var lines=font.PauseLines(loadMessages(),640,false);Require(lines[0].Text=="Game Is Paused"&&lines[1].Text=="Escape Also Resumes"&&lines[2].Text=="Press P To Resume","Runtime prompt contract");Require(font.Measure(" ",4)==font.Metric(412).Width&&font.Measure("E",4)==font.Metric(412).Width+3,"Style4space must omit padding");Require(font.Measure("",4)==0,"Empty width");Reject(delegate{font.Measure("a\t",4);});Reject(delegate{font.Layout(0,0,"abc",2);});foreach(int width in new[]{0,32768}){bool rejected=false;try{font.PauseLines(loadMessages(),width,false);}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Pause viewport bound");}
                }
            });
            checkEvidence("Original paused-Escape teardown and title-entry chain",delegate {
                var rows=File.ReadAllLines(Path.Combine(evidence,"Navigation-Chain-Oracle.csv"));Require(rows.Length==49,"Navigation fixture shape");for(int i=1;i<rows.Length;i++){
                    var f=rows[i].Split(',');int network=int.Parse(f[0]),flag=int.Parse(f[1]),failure=int.Parse(f[2]),stage=int.Parse(f[3]);Require(network>=0&&network<=2&&flag>=0&&flag<=1&&failure>=0&&failure<=1&&stage>=0&&stage<=3,"Navigation fixture domain");
                    int state=stage==0?4:stage==1?20:stage==2?9:failure==0?0:9,mode=stage<2?14:2;string expected="";
                    if(stage==0){if(network>0)expected="NetworkCommand:9:0:0|NetworkFlush";}
                    else if(stage==1){expected="StopAudio:7";if(network>0)expected+="|NetworkFlush|NetworkCleanup";if(network<=1)expected+="|Callback41B6B0";}
                    else if(stage==2)expected=(flag==1?"Callback402D20|":"")+"BattleResourceRelease|FadeOut";
                    else expected="Bank:0|FrontResources|"+(failure==0?"Script:0044EF88":"ResourceError");
                    Require(int.Parse(f[4])==state&&int.Parse(f[5])==mode&&f[6]=="9"&&f[7]=="2"&&f[8]=="0044EF88"&&int.Parse(f[9])==(stage==3?failure:0)&&f[10]==expected,"Native navigation stage "+i);
                }
            });
            checkEvidence("Local battle entry prefix and factor order match original dispatcher",delegate {
                byte[] bytes=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","BattleEntry.seqbin"));var program=EntryProgram.Read(bytes,-1);
                for(int i=0;i<bytes.Length;i+=4){var bad=(byte[])bytes.Clone();bad[i]^=0x40;Reject(delegate{EntryProgram.Read(bad,-1);});}Reject(delegate{EntryProgram.Read(new byte[172],-1);});Reject(delegate{EntryProgram.Read(bytes,0);});Reject(delegate{EntryProgram.Read(bytes,1);});
                var timeline=new EntryTimeline(program);timeline.Advance(560);var expected=new List<string>();foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Entry-Trace-Oracle.csv"))){if(!row.StartsWith("-1,"))continue;var f=row.Split(',');if(f[2]=="Card"||f[2]=="Fade")expected.Add(f[2]+","+f[3]);}
                Require(expected.Count==15&&timeline.Trace.Count==16&&timeline.Complete,"Entry trace shape");for(int i=0;i<expected.Count;i++)Require(timeline.Trace[i]==expected[i],"Native entry order "+i);Require(timeline.Trace[15]=="Boundary,4","Entry boundary");
                var states=File.ReadAllLines(Path.Combine(evidence,"Entry-State-Oracle.csv"));Require(states.Length==4&&states[1]=="-1,6,0,4,14,1,4,00450EC8","Native local outer dispatch");
                var native=File.ReadAllLines(Path.Combine(evidence,"Entry-Trace-Oracle.csv"));Require(Array.IndexOf(native,"-1,15,Init,Human")>=0&&Array.IndexOf(native,"-1,16,Init,Opponents")>=0&&Array.IndexOf(native,"-1,17,Init,EmptyFleets")>=0,"Local callback order");
            });
            check("Entry clock partitions, final-frame hold, pause and idempotent completion",delegate {
                var program=EntryProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","BattleEntry.seqbin")),-1);var one=new EntryTimeline(program);var split=new EntryTimeline(program);one.Advance(560);for(int i=0;i<80;i++)split.Advance(7);Require(string.Join(";",one.Trace.ToArray())==string.Join(";",split.Trace.ToArray()),"Entry partitions");
                var paused=new EntryTimeline(program);paused.Advance(520);Require(paused.Factor==64&&!paused.Complete,"Full-bright frame hold");paused.Pause();paused.Advance(100000);Require(!paused.Complete,"Pause advanced entry");paused.Resume();paused.Advance(39);Require(!paused.Complete,"Final frame shortened");paused.Advance(1);int count=paused.Trace.Count;paused.Resume();paused.Advance(long.MaxValue);Require(paused.Complete&&paused.Trace.Count==count,"Repeated entry completion");
            });
            check("Fleet exit follows recovered17factor fade before14factor loading entry",delegate {
                var program=EntryProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","BattleEntry.seqbin")),-1);var t=new EntryTimeline(program,true);
                Require(t.LeavingFleet&&t.Factor==64,"Fleet exit initial frame");t.Advance(640);Require(t.LeavingFleet&&t.Factor==0&&!t.Complete,"Fleet black frame hold");t.Advance(39);Require(t.LeavingFleet&&t.Factor==0,"Fleet black frame shortened");t.Advance(1);Require(!t.LeavingFleet&&t.Factor==12&&!t.Complete,"Loading card handoff");t.Advance(520);Require(t.Factor==64&&!t.Complete,"Loading final frame hold");t.Advance(40);Require(t.Complete&&t.Trace.Count==33,"Full transition completion");
                for(int i=0;i<17;i++)Require(t.Trace[i]=="FleetFade,"+(64-i*4),"Recovered exit factor "+i);Require(t.Trace[17]=="Card,e_wait1","Card changed before full exit");for(int i=0;i<14;i++)Require(t.Trace[18+i]=="Fade,"+(12+i*4),"Recovered loading factor "+i);Require(t.Trace[32]=="Boundary,4","Transition boundary");
            });
            check("Two-phase entry preserves pause, partitions and idempotent completion",delegate {
                var program=EntryProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","BattleEntry.seqbin")),-1);var whole=new EntryTimeline(program,true);whole.Advance(EntryTimeline.FullTransitionMilliseconds);var split=new EntryTimeline(program,true);foreach(long dt in new[]{1L,639L,39L,1L,519L,1L,39L,1L})split.Advance(dt);Require(string.Join(";",whole.Trace.ToArray())==string.Join(";",split.Trace.ToArray()),"Full transition partitions");
                var pause=new EntryTimeline(program,true);pause.Advance(640);pause.Pause();pause.Advance(long.MaxValue);Require(pause.LeavingFleet&&pause.Factor==0&&!pause.Complete,"Paused black frame advanced");pause.Resume();pause.Advance(600);int count=pause.Trace.Count;pause.Resume();pause.Advance(long.MaxValue);Require(pause.Complete&&pause.Trace.Count==count,"Transition dispatched twice");bool rejected=false;try{pause.Advance(-1);}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Negative transition time accepted");
            });
            check("Loading exit holds every recovered factor before committing the battle",delegate {
                var program=EntryProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","BattleEntry.seqbin")),-1);var t=new EntryTimeline(program,true,true);t.Advance(1240);Require(t.LeavingCard&&t.Factor==64&&!t.Complete,"Loading exit entry");for(int i=1;i<=16;i++){t.Advance(40);Require(t.Factor==64-i*4&&!t.Complete,"Loading exit factor "+i);}t.Pause();t.Advance(long.MaxValue);Require(t.Factor==0&&!t.Complete,"Paused loading black hold");t.Resume();t.Advance(39);Require(!t.Complete,"Black hold shortened");t.Advance(1);Require(t.Complete&&t.Trace.Count==50,"Preparation boundary");
                var split=new EntryTimeline(program,true,true);for(int i=0;i<1920;i++)split.Advance(1);Require(string.Join(";",t.Trace.ToArray())==string.Join(";",split.Trace.ToArray()),"Preparation frame partitions");
            });
            check("Battle reveal follows14source factors with pause, partitions and final hold",delegate {
                var reveal=new BoardRevealTimeline();Require(reveal.Factor==12,"Battle initial factor");for(int i=1;i<=13;i++){reveal.Advance(40,true);Require(reveal.Factor==12+i*4&&!reveal.Complete,"Battle factor "+i);}reveal.Advance(long.MaxValue,false);Require(!reveal.Complete,"Pause consumed final hold");reveal.Advance(39,true);Require(!reveal.Complete,"Final hold shortened");reveal.Advance(1,true);Require(reveal.Complete,"Final hold did not complete");var split=new BoardRevealTimeline();for(int i=0;i<560;i++)split.Advance(1,true);Require(split.Complete&&split.Factor==64,"Reveal partitions");reveal.Advance(long.MaxValue,true);Require(reveal.Factor==64,"Repeated reveal changed factor");bool rejected=false;try{reveal.Advance(-1,true);}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Negative reveal time accepted");
            });
            check("Fleet PCX storage is cropped to the native480line viewport",delegate {
                var card=Assets.Pcx(Path.Combine(assets,"Graphics","EC_FLEET.PCX"));Require(card.Width==640&&card.Height==512,"Original Fleet storage dimensions");
                var view=FleetProgram.Viewport(card);Require(view.Width==640&&view.Height==480&&view.Pixels.Length==307200&&view.Palette==card.Palette,"Fleet viewport shape/palette");
                for(int i=0;i<view.Pixels.Length;i++)Require(view.Pixels[i]==card.Pixels[i],"Fleet crop changed source pixel "+i);
                Reject(delegate{FleetProgram.Viewport(new IndexedImage{Width=640,Height=479,Pixels=new byte[640*479],Palette=card.Palette});});
                Reject(delegate{FleetProgram.Viewport(new IndexedImage{Width=640,Height=512,Pixels=new byte[640*480],Palette=card.Palette});});
            });
            checkEvidence("Fleet camera/projection and cell picks match native fixtures",delegate {
                var camera=File.ReadAllLines(Path.Combine(evidence,"Fleet-Camera-Oracle.csv"));Require(camera.Length==2&&camera[1]=="5,5,147,98,147","Fleet camera");
                var rows=File.ReadAllLines(Path.Combine(evidence,"Fleet-Projection-Oracle.csv"));Require(rows.Length==122,"Projection count");for(int i=1;i<rows.Length;i++){var c=rows[i].Split(',');Require(FleetGeometry.Project(int.Parse(c[0]),int.Parse(c[1]))==new Point(int.Parse(c[2]),int.Parse(c[3])),"Fleet projection "+i);}
                rows=File.ReadAllLines(Path.Combine(evidence,"Fleet-Pick-Oracle.csv"));Require(rows.Length==101,"Pick count");for(int i=1;i<rows.Length;i++){var c=rows[i].Split(',');Require(FleetGeometry.Pick(new Point(int.Parse(c[2]),int.Parse(c[3])))==new Point(int.Parse(c[4]),int.Parse(c[5])),"Fleet pick "+i);}
            });
            checkEvidence("Fleet centered moves match780native validator cases and commit atomically",delegate {
                var board=new Board();var specs=RuleProfile.Recovered.Fleet;for(int i=0;i<5;i++)Require(board.Place(new Point(i*2,0),specs[i].Length,true,specs[i].Name,specs[i].SourceType),"Fixture layout");var rows=File.ReadAllLines(Path.Combine(evidence,"Fleet-Placement-Oracle.csv"));Require(rows.Length==781,"Placement count");for(int i=1;i<rows.Length;i++){var c=rows[i].Split(',');Require(board.CanMoveClassic(int.Parse(c[0]),new Point(int.Parse(c[2]),int.Parse(c[3])))==(c[4]=="1"),"Native placement "+i);}
                var ship=board.Ships[0];Require(!board.MoveClassic(0,new Point(2,2))&&ship.Cells[0]==Point.Empty&&board.ShipAt(Point.Empty)==ship,"Failed move changed occupancy");Require(board.MoveClassic(0,new Point(1,2))&&board.ShipAt(Point.Empty)==null&&board.ShipAt(new Point(1,0))==ship,"Successful move occupancy");
                var game=new Game(731,RuleProfile.Recovered,OpponentKind.Original,0,false);game.AutoPlace();game.Start();Require(!game.MovePlacedShip(0,new Point(1,2)),"Battle admitted fleet editing");
            });
            checkEvidence("Fleet strict script/button reader and native dimensions",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var flow=File.ReadAllBytes(Path.Combine(p,"FleetFlow.seqbin"));var buttons=File.ReadAllBytes(Path.Combine(p,"FleetButtons.seqbin"));var frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_FLEET.SPH"),Assets.Pcx(Path.Combine(assets,"Graphics","EC_FLEET.PCX")).Palette);var program=FleetProgram.Read(flow,buttons,frames);var rows=File.ReadAllLines(Path.Combine(evidence,"Fleet-Buttons-Oracle.csv"));Require(rows.Length==12,"Fleet buttons count");for(int i=1;i<rows.Length;i++){var c=rows[i].Split(',');var b=program.Buttons[i-1];Require(b.X==int.Parse(c[1])&&b.Y==int.Parse(c[2])&&b.Frame==int.Parse(c[3])&&b.Group==int.Parse(c[4])&&b.Action==int.Parse(c[5])&&b.Bounds.Width==int.Parse(c[6])&&b.Bounds.Height==int.Parse(c[7]),"Fleet button "+i);}
                Reject(delegate{FleetProgram.Read(new byte[180],buttons,frames);});var bad=(byte[])flow.Clone();bad[0]=255;Reject(delegate{FleetProgram.Read(bad,buttons,frames);});bad=(byte[])buttons.Clone();bad[12]=255;Reject(delegate{FleetProgram.Read(flow,bad,frames);});Reject(delegate{FleetProgram.Read(flow,new byte[220],frames);});
                Require(program.Hit(15,431)==2&&program.Hit(140,431)==-1,"Fleet half-open button edge");
                rows=File.ReadAllLines(Path.Combine(evidence,"Fleet-Init-Oracle.csv"));Require(rows.Length==11,"Native initializer count");for(int i=1;i<rows.Length;i++){var c=rows[i].Split(',');var spec=RuleProfile.Recovered.Fleet[(i-1)%5];Require(int.Parse(c[1])==spec.SourceType&&int.Parse(c[2])==(i-1)/5&&int.Parse(c[5])==spec.Length,"Native initial roster");}Require(File.ReadAllLines(Path.Combine(evidence,"Fleet-Autoplace-Calls.csv")).Length==3,"Native randomizes both fleets");
            });
            check("Original menu glyph raster and cursor at clipped viewport positions",delegate {
                // Actual427070/407950 source records, six registered FES styles.
                string[] expected={"66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","7416CDF99D4831B16690DC6EE238FFDC21DB1125D89FC1148FA2559C254F2379","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","8CC728631C7F869853D803017C38BD3037BC8D1D49CBE54271A7C9674591DDF7","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","A310DAF71DBE96AB614FBA3E7A12283A5135544109D742C28BFC7E4F9A1D88AA","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","A5F7B944F2D8C6F851F0AD15E3A076F67C384BE24A09E7C1D4799E3A5A228EB6","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","4F008F27BEB045D1567A62CD42DCC487036F190DAFEB2356EF8D6F428BB995DD","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","7D58BE9273685B312525BF7152BAF2C15FC984A54FF2AC3E9BD699FEB71C4E8F","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","094C54BD8D7A9B972B87BCC34D6F0A6F842FB546F8161BAF39FDBB1EBE8B6BE7","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","17370BA759EEAD6DAA17A4320455251C5601909D661B9F3DB9D37AC1E8FBA55E","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","08D3A0E48677F38D7491DBD8267DC15BA4468FFC580B02BBFA205BA4C59AFC2F","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","8A24D06DE57CCD383ABDEA0AA7C9D95B6F5956FBE25DB2ABE5C31A75660A351E","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","34E0058BC9E0B3A5E20A979ACEFBF1F997264D3D6CA95DB62EEF1C97F6437940","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","8A6F3F1E1E97AED857B93A50E8053B1FB6DB372EF4AAD0A108A931413D248033","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","24CBA63B0EDAEA4AD3C8653441F7E6DAAC713F591449D226FCFFAEDE65804F26","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","5AF2795DFC9CF637E13A9682FC66F67BC762CDEFF55987898BB9E3A6FEE2F838","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","437F90076A80E750E00FBF29DC6684538B2D46DA7B0E4EF481D7D5CF173C6D8D","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","1EFA965168E985BF33D53AF8782A50B86DCF5B029B45AC348D5E070EE069AFD3","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","ACE50BDD5460FF8EDF3EBA10AE25A054BC6DD2EFF2E6E4074AD79DC544A853EA","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","6779A3DDE8D133F4F8CAD82784A94BF3C704DFF397ADCF5550EEA6D27A4EB92E","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","03626F7A9F9103C77E0628308244FA41B70BC35FFA30ABE379F78510A7878229","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","40BCCCBB1E4F15F4530B8EDB0F137AE213981CBAC319FD7E4D4E953637DC34AC","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","199E1E774BE5A99EA916EAA69F2CF0DF7038885A8FEC699E66A89D5D1BCCF603","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","F6A79FC3946BC5900347666ACF24261D9EC6C3FA6DFFC9B236893B3133DC7DAB","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","0261E7790EC862C6FCFD7816C9E97C0F9668D1D664B818FACBF7FF2DC48BDECB","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","40543EB4A268FE6D8F6979591F5C92BE048753B82DB4F8CF2780773EB6C64425","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","B3C5340D21155997F0B55AB1BB7C7E6C528396F0D3766AB8F9D8C8A34D57F84E","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","B82D21FD5EAE806D29372AC5DB2FCA4A90D768B5F4D1C2E3A9ED5BA594D8C3E0","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","7031EDF64BED8E6C4E049511B5EFFC6D94825970EFF8EAD66967164F8860E8C7","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","188F6F4FC1D0C9E0B691DA2E8C0A2301B40F6DBA710C05A07C48EBE88A5471F6","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","D8F61736A203EBE48C11FB69807D48AF2EFE776CBB22FF5861B75223BF217D5D","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","66BF1D614CED835383D7864955BC96E853256D38CAE52EC58A0BAEE45B3FB714","900111A25B328C4AAE35EA2FC6DC85ACAC6C861BA830991C7AB45785841B4FFF"};int[] cursors={16,34,58,0,18,42,-7,11,35,635,653,677,320,338,362,16,31,60,0,15,44,-7,8,37,635,650,679,320,335,364,16,37,61,0,21,45,-7,14,38,635,656,680,320,341,365,16,37,63,0,21,47,-7,14,40,635,656,682,320,341,367,16,34,76,0,18,60,-7,11,53,635,653,695,320,338,380,16,43,80,0,27,64,-7,20,57,635,662,699,320,347,384};var palette=Assets.Pcx(Path.Combine(assets,"Graphics","E2_SOLO.PCX")).Palette;var frames=Assets.Sph(Path.Combine(assets,"Graphics","FEFONT.SPH"),palette);var font=new ClassicMenuFont(frames);int at=0;
                foreach(int style in new[]{0,1,2,3,4,5})foreach(Point p in new[]{new Point(16,250),Point.Empty,new Point(-7,5),new Point(635,470),new Point(320,240)})foreach(string text in new[]{"","   ","Aa0!? ~"}){var pixels=new byte[307200];for(int i=0;i<pixels.Length;i++)pixels[i]=unchecked((byte)(i*17+11));Require(font.Draw(pixels,style,p.X,p.Y,text)==cursors[at]&&Hash(pixels)==expected[at],"Native full glyph surface/cursor "+at);at++;}
                Require(at==90,"Six-style native glyph coverage");Reject(delegate{font.Draw(new byte[1],5,0,0,"a");});Reject(delegate{font.Draw(new byte[307200],6,0,0,"a");});Reject(delegate{font.Draw(new byte[307200],5,40000,0,"a");});Reject(delegate{font.Draw(new byte[307200],5,0,0,"\u00e9");});Reject(delegate{new ClassicMenuFont(new List<IndexedImage>());});
            });
            check("Original Fleet full software scene and hover match native layouts",delegate {
                string[] expected={"FE0C3769B45DD1B2B1E52ACE91D6E771C6869D71A665081B2359CA9E9D97A67B","D625D2C74E8568EA9E22EE1B3D3AC918A4A05588A9859C03D8E30374AA428474","46CCC52E1C8E310A010438B50946BC27CDDB5C07838A1DC246EB0DB66D03B48C","15CDEAC425C03BFD4C5F8903188ED6693425A4C6FA1D708A3C795196B2D5268B","0FF49E63E0C1583EA7F01C8EC1D38F4A407B5A3653BAFDD4ACA9BCF84E925A02","1C88EF0A3794C91B8839A96B3411DA2B5BD94F2307F4E22926E9879FC4BEAF4E"};var renderer=new ClassicFleetScene(assets);int at=0;
                foreach(int seed in new[]{0,2531011}){var game=new Game(seed);game.AutoPlace();string before="";foreach(var ship in game.Player.Ships)foreach(var cell in ship.Cells)before+=cell.ToString();foreach(int hover in new[]{-1,2,3}){var image=renderer.Compose(game.Player,hover);Require(Hash(image.Pixels)==expected[at++],"Native full Fleet image");Require(renderer.Commands.Count==(hover<0?15:16)&&renderer.Commands.IsReadOnly,"Native Fleet command count/read-only contract");string after="";foreach(var ship in game.Player.Ships)foreach(var cell in ship.Cells)after+=cell.ToString();Require(after==before&&game.Phase==Phase.Placement,"Rendering changed the draft");}var first=renderer.Compose(game.Player);first.Pixels[0]^=255;first.Palette[20]=Color.FromArgb(first.Palette[20].R^255,first.Palette[20].G,first.Palette[20].B);Require(Hash(renderer.Compose(game.Player).Pixels)==expected[at-3],"Returned scene aliases the card");}
            });
            check("Fleet software scene rejects unsupported deployment states",delegate {
                var renderer=new ClassicFleetScene(assets);Reject(delegate{renderer.Compose(null);});Reject(delegate{renderer.Compose(new Board());});var game=new Game(0);game.AutoPlace();foreach(int hover in new[]{0,1,4})Reject(delegate{renderer.Compose(game.Player,hover);});game.Player.Shots[0,0]=true;Reject(delegate{renderer.Compose(game.Player);});game.Player.Shots[0,0]=false;game.Player.Ships[0].Hits=1;Reject(delegate{renderer.Compose(game.Player);});game.Player.Ships[0].Hits=0;Point cell=game.Player.Ships[0].Cells[1];game.Player.Ships[0].Cells[1]=new Point(cell.X+1,cell.Y);Reject(delegate{renderer.Compose(game.Player);});game.Player.Ships[0].Cells[1]=cell;int type=game.Player.Ships[0].SourceType;game.Player.Ships[0].SourceType=-1;Reject(delegate{renderer.Compose(game.Player);});game.Player.Ships[0].SourceType=type;Require(renderer.Compose(game.Player).Pixels.Length==307200,"Rejected input damaged reusable renderer");
            });
            check("Original prepared Practice complete software scenes",delegate {
                // Actual4300B0/432490 + original wordwrap/glyph/raster, prepared
                // background/type/page/hover values; no live input/session claim.
                string[] expected={"F61A2B1EA81313C4B2E5B540386CEE07DE42CE2B22DC7374C39EEB858FFE4646","C2EBC2CAEE622AEB0BCCB9EF07DDD2DFA47B676DD069A2A281DB3CB07F036929","E9A0010F24BF7C5BCDE7574EC8D7C89F7D79B90DA7F0B4DF76C031B96F26F5D2","F61A2B1EA81313C4B2E5B540386CEE07DE42CE2B22DC7374C39EEB858FFE4646","F61A2B1EA81313C4B2E5B540386CEE07DE42CE2B22DC7374C39EEB858FFE4646","C5FDA3C93BF270BCD6C79A83466B69476E61AA59352995E298590D6AFE67CE4B","2659BBD473E55A2886B1464043D331126AE1CE3A8561B2178A86BE691064B3D4","AC57FA99A3C5C3717E490A3553FF6F270E29FE6130C5D4EC9E0A1E6A2E29FE30","0AF8DB96973DC38B031A2B695F1C1D811E1B2A8D3D37DB68B841F62EC9FFDA17","C5FDA3C93BF270BCD6C79A83466B69476E61AA59352995E298590D6AFE67CE4B","C5FDA3C93BF270BCD6C79A83466B69476E61AA59352995E298590D6AFE67CE4B","2659BBD473E55A2886B1464043D331126AE1CE3A8561B2178A86BE691064B3D4","AC57FA99A3C5C3717E490A3553FF6F270E29FE6130C5D4EC9E0A1E6A2E29FE30","0AF8DB96973DC38B031A2B695F1C1D811E1B2A8D3D37DB68B841F62EC9FFDA17","C5FDA3C93BF270BCD6C79A83466B69476E61AA59352995E298590D6AFE67CE4B","B6657F43C9873DB9C7857D03ABB706BE1B674BA8E99397BC3AA2330FB42DC53E","633BA876F7CC885BC01F3CB4B97CC69620ADAEEF7FCDED0D2563CB8E6074D0C5","DF5D29EC2A118C398D051058253C9E44700858DCB780DE84E7FF5F66E703C8C1","B6657F43C9873DB9C7857D03ABB706BE1B674BA8E99397BC3AA2330FB42DC53E","B6657F43C9873DB9C7857D03ABB706BE1B674BA8E99397BC3AA2330FB42DC53E","E301D1697BE1DCB33FDD64E4C709C6319005DFAD55584A04D82949D969B58A62","533479ABC368FE50E37E3757E419C1A1EE559A2C0F1BD57A4D6E2B441BE98AD7","3BA814C44BAA6400C501C039AB3433F6F5CCD05CC9DC44CA068CD79B17F675BF","C5D13F461BF2B71415625BD021A5993208D3CFAE01A4665F7334CAD08D50D4B9","E301D1697BE1DCB33FDD64E4C709C6319005DFAD55584A04D82949D969B58A62","E301D1697BE1DCB33FDD64E4C709C6319005DFAD55584A04D82949D969B58A62","533479ABC368FE50E37E3757E419C1A1EE559A2C0F1BD57A4D6E2B441BE98AD7","3BA814C44BAA6400C501C039AB3433F6F5CCD05CC9DC44CA068CD79B17F675BF","C5D13F461BF2B71415625BD021A5993208D3CFAE01A4665F7334CAD08D50D4B9","E301D1697BE1DCB33FDD64E4C709C6319005DFAD55584A04D82949D969B58A62","6FC3CD1DC233FAB66302C8909C4435E3694E76F044CC3C2FF2E093DF26BFD38B","E7B7359646F0B18162A7EDC7FA0447B30CA3473D0404627EC9BCBF6F3A516E1B","FB373B246C5622C5252D0C06532177B64DF15855EDA3F2417B1DF161A1BDA967","6FC3CD1DC233FAB66302C8909C4435E3694E76F044CC3C2FF2E093DF26BFD38B","6FC3CD1DC233FAB66302C8909C4435E3694E76F044CC3C2FF2E093DF26BFD38B","8713E71267C17B7BD26216CD8BDC640D17A6D4D3E4CCE1EDA4BF076A2FA782F1","1913F7B22DDFAEACA81F51CFAD722799971BFF82F0B3BC0F26E339773A28539E","64F66884C7DF7CB3BDCA663724F9EF81EC0D5EF6614CC0D1BDA119A2EB574FBB","F8D9B3B4920DB1C46F57ABEAA1423F4F4CD4173851F2472AC89AB330BF21F07C","8713E71267C17B7BD26216CD8BDC640D17A6D4D3E4CCE1EDA4BF076A2FA782F1","8713E71267C17B7BD26216CD8BDC640D17A6D4D3E4CCE1EDA4BF076A2FA782F1","1913F7B22DDFAEACA81F51CFAD722799971BFF82F0B3BC0F26E339773A28539E","64F66884C7DF7CB3BDCA663724F9EF81EC0D5EF6614CC0D1BDA119A2EB574FBB","F8D9B3B4920DB1C46F57ABEAA1423F4F4CD4173851F2472AC89AB330BF21F07C","8713E71267C17B7BD26216CD8BDC640D17A6D4D3E4CCE1EDA4BF076A2FA782F1","860E1BF85013CDF79F0C4414AB73912146A8FCCDDD2E736F3F6489B74BD54CE5","66CD8807BD76AF5D61EC51DEB5053EDBA610A9AA5E595C7BAC52F7A3E6EE0DE6","04BA5D3C94F8327F5A8016178DFC91D3705BD497BEF987571B6FBA1B4727D72C","860E1BF85013CDF79F0C4414AB73912146A8FCCDDD2E736F3F6489B74BD54CE5","860E1BF85013CDF79F0C4414AB73912146A8FCCDDD2E736F3F6489B74BD54CE5","B30F210E003F78053D737E165E3F955299FB1380C06BC82AB20A03D90AB3C666","2680981870C36638AF2232E2CDD559E831F86BFCD6FD7ED7210606739EE75D09","394D93E1A59A525B5674D3D395DD2642542CD3B1BB9F59E8C54AFE71082F7A32","7B099638F0401E62D0A0FC1424AED4D9C4D37763E474230F63C4EADA90C24552","B30F210E003F78053D737E165E3F955299FB1380C06BC82AB20A03D90AB3C666","B30F210E003F78053D737E165E3F955299FB1380C06BC82AB20A03D90AB3C666","2680981870C36638AF2232E2CDD559E831F86BFCD6FD7ED7210606739EE75D09","394D93E1A59A525B5674D3D395DD2642542CD3B1BB9F59E8C54AFE71082F7A32","7B099638F0401E62D0A0FC1424AED4D9C4D37763E474230F63C4EADA90C24552","B30F210E003F78053D737E165E3F955299FB1380C06BC82AB20A03D90AB3C666","0580A6E9A1E13EBF5E5543A93F6CA835311474592BA9A0025FE12DA0B245C433","859CE90F7BE5CE1976E1D5057D6D3A700BACC3A628F5022AF158AD99F1996D59","5D03853F2F71F08BD70027E6C2CEEEFD4BEC6A78764A10DC55D05602BC848AB0","0580A6E9A1E13EBF5E5543A93F6CA835311474592BA9A0025FE12DA0B245C433","0580A6E9A1E13EBF5E5543A93F6CA835311474592BA9A0025FE12DA0B245C433","E6629D7D7CE53933A93C1AECA8E242DC71A790E309CFE9F3CC4886C862BEA623","DFA63EBC4D965325623BC30D2870F8CBF2FE0BF63158038BEE7BECA04FAB3A4A","91353C34C4ECFA7D476F7A96CD3819EE66E299BFB0F47BA4046557AFAACBFD47","F25A5737E0489B78DF79603027ABE873A49A10DD881285919C1E9BD3351BC7AD","E6629D7D7CE53933A93C1AECA8E242DC71A790E309CFE9F3CC4886C862BEA623","E6629D7D7CE53933A93C1AECA8E242DC71A790E309CFE9F3CC4886C862BEA623","DFA63EBC4D965325623BC30D2870F8CBF2FE0BF63158038BEE7BECA04FAB3A4A","91353C34C4ECFA7D476F7A96CD3819EE66E299BFB0F47BA4046557AFAACBFD47","F25A5737E0489B78DF79603027ABE873A49A10DD881285919C1E9BD3351BC7AD","E6629D7D7CE53933A93C1AECA8E242DC71A790E309CFE9F3CC4886C862BEA623","40A46AAC67D9181073C213F8F026A896C078BB596F2C97DE79FAAE74C604EAB9","A69EB6409FE4DE463B0D211F91974939EA1892E017186A061690FC1A0C0C73AE","FE1A3F208BA9239B9AC268647ECB083E60278BA1F513CE18049840365BF43D5D","40A46AAC67D9181073C213F8F026A896C078BB596F2C97DE79FAAE74C604EAB9","40A46AAC67D9181073C213F8F026A896C078BB596F2C97DE79FAAE74C604EAB9","B15AB1FAD6AF2A41AEC8E0944F32FBDB3D5C3F492E3BB32E8238F74D9D8C76CB","13CF481AC02E461825911E1BB8707F044CC3B14F67D8686BAE51D83B6AA892CC","449977797B70CB22B69F0545D28D40B3439187E10ECCBCE7D16413ABE19EC1DA","BB71B1986AAB56A94249109299DD70EE0724DFD71E761F4AF104C2D316AB2ACB","B15AB1FAD6AF2A41AEC8E0944F32FBDB3D5C3F492E3BB32E8238F74D9D8C76CB","B15AB1FAD6AF2A41AEC8E0944F32FBDB3D5C3F492E3BB32E8238F74D9D8C76CB","13CF481AC02E461825911E1BB8707F044CC3B14F67D8686BAE51D83B6AA892CC","449977797B70CB22B69F0545D28D40B3439187E10ECCBCE7D16413ABE19EC1DA","BB71B1986AAB56A94249109299DD70EE0724DFD71E761F4AF104C2D316AB2ACB","B15AB1FAD6AF2A41AEC8E0944F32FBDB3D5C3F492E3BB32E8238F74D9D8C76CB","D9FBE567E8F1AB9E04C46E00E648DA26F68F3241C8BD4E4961D5B33BD6B77D66","03AC8E7111EC526E19DD0467EBE810A8F00E4F77FF1F2E0E56FEDC059C3495DD","A9F7A4F41F526094C9A087DAD5E6D6ED1BADC4E0D6CD991E6417115E6775C539","D9FBE567E8F1AB9E04C46E00E648DA26F68F3241C8BD4E4961D5B33BD6B77D66","D9FBE567E8F1AB9E04C46E00E648DA26F68F3241C8BD4E4961D5B33BD6B77D66","45B48C68F04A20F2F99CF54E6223184510651C7E2304A0C66E07DB60E4E8EB33","719D0E64CC748B3E55B8371E96C890AB1B8D89E8CA9A629F745DA2037320E099","23D368A3DD20F273612AE0BA3161400112D9B91D0C7062E1582E052B50E6771E","DC56B0876C3219E8C67C823DFBF7C74A9B6510611D75B94FEB2468A458E2D005","45B48C68F04A20F2F99CF54E6223184510651C7E2304A0C66E07DB60E4E8EB33","45B48C68F04A20F2F99CF54E6223184510651C7E2304A0C66E07DB60E4E8EB33","719D0E64CC748B3E55B8371E96C890AB1B8D89E8CA9A629F745DA2037320E099","23D368A3DD20F273612AE0BA3161400112D9B91D0C7062E1582E052B50E6771E","DC56B0876C3219E8C67C823DFBF7C74A9B6510611D75B94FEB2468A458E2D005","45B48C68F04A20F2F99CF54E6223184510651C7E2304A0C66E07DB60E4E8EB33","96E8C963B30A4325D665F3F7BE148EDDC7764BC9D0A3853147FC230BBC5B9FE8","F427B8D0DCCFE938968D0CCD6ACA0311C45B45DAE3FB0A13CAC0DDC2BE073A94","04BA59A8F83FEFEADFCAF11AE377FB69CA1E56CD7CBDAE610A84C42C5F4798BA","96E8C963B30A4325D665F3F7BE148EDDC7764BC9D0A3853147FC230BBC5B9FE8","96E8C963B30A4325D665F3F7BE148EDDC7764BC9D0A3853147FC230BBC5B9FE8","775C3821C6C3CB761B2F004705D9906E496896D286E25F19B06F9172ED06EEC3","EA558488E23DE4A0939749BE6965360BF7F968AA0B62E126F7455DAB5CC74FA8","6BD7C095BDAE171E07EC2ED1DA278D0F6BA8B4C7A99D798CE70814D54A8DA418","4569D4B32E83348C91CB4A549526EA7C5063082F98AD96C8CC3A88E15E3E6527","775C3821C6C3CB761B2F004705D9906E496896D286E25F19B06F9172ED06EEC3","775C3821C6C3CB761B2F004705D9906E496896D286E25F19B06F9172ED06EEC3","EA558488E23DE4A0939749BE6965360BF7F968AA0B62E126F7455DAB5CC74FA8","6BD7C095BDAE171E07EC2ED1DA278D0F6BA8B4C7A99D798CE70814D54A8DA418","4569D4B32E83348C91CB4A549526EA7C5063082F98AD96C8CC3A88E15E3E6527","775C3821C6C3CB761B2F004705D9906E496896D286E25F19B06F9172ED06EEC3","C01EF72606DB016FAACC46B7FB616EB4790076E989BFCF9A6200FA2655343413","EA57F5842C3F2B332DC4D568B0D9EAB0D3EF13B39664CCA4412B2E339DADD1B3","1B875410CDC26D6AD1B197E7B77D9E1C38DAE0193CE4F71117FA2AAC147E3A3E","C01EF72606DB016FAACC46B7FB616EB4790076E989BFCF9A6200FA2655343413","C01EF72606DB016FAACC46B7FB616EB4790076E989BFCF9A6200FA2655343413","28D56992B90C74632EBFD4DCA32E5553A7E8D96D528F0645C78F8779AF72EFC0","F4F23EB833135034C9EAE524ABF76F8CB520D63BCC151062E67E524C2C8298B7","AD29FFF7951D8346F51A90CDD655A054A0F4295A2B061A687F6A9D4028FC6E72","0E7BDE44B6A24B67E73B5043E4FAFCBD09BD578819D1BD184182A0800268952D","28D56992B90C74632EBFD4DCA32E5553A7E8D96D528F0645C78F8779AF72EFC0","28D56992B90C74632EBFD4DCA32E5553A7E8D96D528F0645C78F8779AF72EFC0","F4F23EB833135034C9EAE524ABF76F8CB520D63BCC151062E67E524C2C8298B7","AD29FFF7951D8346F51A90CDD655A054A0F4295A2B061A687F6A9D4028FC6E72","0E7BDE44B6A24B67E73B5043E4FAFCBD09BD578819D1BD184182A0800268952D","28D56992B90C74632EBFD4DCA32E5553A7E8D96D528F0645C78F8779AF72EFC0","B44D431D71948620DDC813F7A6149679094D7A830CB73CACF95822F42434FAB0","96BF9A66D129513CD178353D75879ED9671D4CD2C6A3C1D36B7B268E189279A9","B2ABAAC544E5E052931D26B94724A4834709DEC6C91FF3DD345569E7A6D7A63A","B44D431D71948620DDC813F7A6149679094D7A830CB73CACF95822F42434FAB0","B44D431D71948620DDC813F7A6149679094D7A830CB73CACF95822F42434FAB0","2D2431EF0FE73953D22BF81ED40FDF00253FE828E07F0F9BB43E6811C323ADDA","314C5BA9B0F8E3B99A6AF7B911A39D481AC75CF124D9E77A65EAFED8D2779EE1","1BD86202F17F7E070E7457BC02784731C467D79BEF962524A86D4BDB3BDDFE19","229F8983E00F4AB2385D0FE65B7BE21C2154E33EB1EE043E34ADD12541EF14A5","2D2431EF0FE73953D22BF81ED40FDF00253FE828E07F0F9BB43E6811C323ADDA","2D2431EF0FE73953D22BF81ED40FDF00253FE828E07F0F9BB43E6811C323ADDA","314C5BA9B0F8E3B99A6AF7B911A39D481AC75CF124D9E77A65EAFED8D2779EE1","1BD86202F17F7E070E7457BC02784731C467D79BEF962524A86D4BDB3BDDFE19","229F8983E00F4AB2385D0FE65B7BE21C2154E33EB1EE043E34ADD12541EF14A5","2D2431EF0FE73953D22BF81ED40FDF00253FE828E07F0F9BB43E6811C323ADDA"};var scene=new ClassicPracticeScene(assets);int at=0;
                foreach(int type in new[]{1,2,3,20})foreach(int players in type==20?new[]{2}:new[]{2,3,4})foreach(int page in new[]{0,12,24})foreach(int hover in new[]{-1,10,11,12,13})Require(Hash(scene.Compose(type,players,page,hover).Pixels)==expected[at++],"Native complete Practice scene "+at);
                Require(at==150,"Prepared native Practice coverage");Reject(delegate{scene.Compose(0);});Reject(delegate{scene.Compose(1,5);});Reject(delegate{scene.Compose(1,2,6);});Reject(delegate{scene.Compose(1,2,0,0);});
            });
            check("Original Practice active page counters and long-description arrow states",delegate {
                // Native153: explicit copied-runtime SCNTEXT pointer overrides;
                // normal Practice descriptions remain their original single page.
                string[] cases={"5,2,0,-1,47A18FC8BB364D3AC2E0C7D939A822DE66422D8048CAA40A97186433B4859D96","5,2,12,13,40BFC1EBEE0C7E58EC4A69CEA2138CEEE02165CFB23DF506573DC93EC9856F85","5,2,12,-1,40BFC1EBEE0C7E58EC4A69CEA2138CEEE02165CFB23DF506573DC93EC9856F85","5,2,0,12,47A18FC8BB364D3AC2E0C7D939A822DE66422D8048CAA40A97186433B4859D96","5,4,0,-1,0E3C596B2E0FAD4604FA68BCC1D9823BE06716498EF84855427E36C1C416A190","5,3,0,-1,13AD1213480A560077F7EFF1A0B2DEF5D3DEEF95981F7F88E106A3254487E3BB","5,3,12,13,427D6FE8C27B3E457476C8A68F37E6FF22A9B6DC6C162E27710C0E6821006D63","5,3,12,-1,427D6FE8C27B3E457476C8A68F37E6FF22A9B6DC6C162E27710C0E6821006D63","5,3,0,12,13AD1213480A560077F7EFF1A0B2DEF5D3DEEF95981F7F88E106A3254487E3BB","5,4,12,13,A6EE88AF6ABD695D241D575CC6DBB4A9C70D88FE5678AE03499C297240E5C5E5","5,4,12,-1,A6EE88AF6ABD695D241D575CC6DBB4A9C70D88FE5678AE03499C297240E5C5E5","5,4,0,12,0E3C596B2E0FAD4604FA68BCC1D9823BE06716498EF84855427E36C1C416A190","14,2,0,-1,2C002E218BD6423654506A4B6B7C411E1037A2F3FF465E68EB961C0EABC5ADAE","14,2,12,13,B5AECF1B822DCF58CF105E49A5416E1E03E248EB26283C9BA9CBE7712E1D10BA","14,2,12,-1,5D22CAA7AFCA0BF0B62898963A33A79DCA4A86430CDEEF9EE4DAADBAFE3AF0BA","14,2,24,13,5B65F52A1DADD65E36FE5A3996947578FEFF26DAF5B292A315FABF2F3ED17661","14,2,24,-1,5B65F52A1DADD65E36FE5A3996947578FEFF26DAF5B292A315FABF2F3ED17661","14,2,12,12,E1D1CAA2519F55AF1D8EBA3DC0B22F52BAB7E0027CA41C1EE2A7B755F368FC9A","14,2,0,12,2C002E218BD6423654506A4B6B7C411E1037A2F3FF465E68EB961C0EABC5ADAE","14,4,0,-1,9EC2CE010E5476AF91F7945D0F661B49706744310C17D1FB5B0F8B23BC586441","14,3,0,-1,F88788C205D464BEDDAFF38DAF72BA40CE2C1F776CD1AA2F042A03BCD6D9F050","14,3,12,13,3ABE5D037A883C9E7252D90FBB6411285E7CE2BBD7C0D00ADBFC1ADF2E208E4D","14,3,12,-1,C47F3322220472F8EE378629FD5015524E3C8794D0C70FD2205A3B68D2A69DC6","14,3,24,13,7D27F408C816C0C3406826A46ED15467B0CCE031BC5360F567E739966EE8AF55","14,3,24,-1,7D27F408C816C0C3406826A46ED15467B0CCE031BC5360F567E739966EE8AF55","14,3,12,12,7C0AB649F6C4305CB9F95865B98B34D2F9ED2E485F44F047A2782560459F9D8C","14,3,0,12,F88788C205D464BEDDAFF38DAF72BA40CE2C1F776CD1AA2F042A03BCD6D9F050","14,4,12,13,30D68D4DE9B4C080F75FF4C85FB278A0CCEC0331E61968A7A228686A95A1620A","14,4,12,-1,2E3D29B2BE03DD2E777FFDFDDFBA89AD8E9A8738F950DEA8FF9B1A1C12AB2E53","14,4,24,13,CF745A601F1CBDFE87BFE7C5967F5A2B912EEE15AE3464385298C690211E960A","14,4,24,-1,CF745A601F1CBDFE87BFE7C5967F5A2B912EEE15AE3464385298C690211E960A","14,4,12,12,1692A93DA7293B3596B0C7FDBB7B7FEEECD726F890E6C8CDC4D80F06473D0898","14,4,0,12,9EC2CE010E5476AF91F7945D0F661B49706744310C17D1FB5B0F8B23BC586441"};var scene=new ClassicPracticeScene(assets);
                foreach(string row in cases){var v=row.Split(',');Require(Hash(scene.ComposeDescriptionComparison(int.Parse(v[0]),int.Parse(v[1]),int.Parse(v[2]),int.Parse(v[3])).Pixels)==v[4],"Native active Practice page/arrow scene");}
                Require(cases.Length==33,"Native long-description coverage");Reject(delegate{scene.ComposeDescriptionComparison(-1);});Reject(delegate{scene.ComposeDescriptionComparison(21);});Reject(delegate{scene.ComposeDescriptionComparison(14,5);});Reject(delegate{scene.ComposeDescriptionComparison(14,2,36);});Reject(delegate{scene.ComposeDescriptionComparison(14,2,0,0);});
            });
            check("Original Practice description row table and input boundaries",delegate {
                // Hash-pinned Bs.exe4201F0: all21original descriptions, CD-filled
                // tails. Complete1584-byte source outputs, not rendered glyphs.
                string[] expected={"4F27A107544007B2267682B36CBCC7357BB52F651D79DB401F6DF0F608CABF10","EE555611EEC41D5738324D932CE6BAC7D3D60DFCB90663C56271069B1486445E","9D074E86B71B834992C114594846552E18C342DB6C12E3F530F5211724DAA40F","13FFA7BDE13653087361C4B188EAA279A42D1B2BBE6D528AAD3466A57C324C03","28EDABE95C649FCD0A099C4A46E5CDEECF66221461FC1DEEDA43404450E49FAB","B9208146F9F85B99FF54C967282EC48819B4E87BC6207749C0B0AD265A6B84AC","6BD1BA01ED7C857295ABD6AE37F2EF2C088C1090C293FA5B1456F1781B56A2BF","9E0B9C9C14A7B07EBF03B1A0E7D82A84A556DF752D3F84793652B403984547BD","93B1A951887072A7163C898A97C97EAFE7624AD29292EA087D0E836D0C5E4468","A3523BA5B1B44F6FE9504C91EF097C158A50611B620668D9733DB5CFA8FDAA87","BDD99E04393104AF361BF72E62BB1BF1F4A32567FD9898DE67BD48354E3C56C5","089C699C3C82089D43A7B20F91A7BC198405040C29CDB2919D344A3ABF1A102C","B0E543405E6AD8E1BA738CFC747988701BDC27E7B19BB3140B06D905F17EB20C","A9D2124F53AE61FD83F8A3E7B184D6B5851839E1B346F1B6FBF43810137930A6","BB15F067E84B277EF55CF8BFC75FE357D2540F53A825690E708C4EAB759E752A","7535F8EADCED2A729AC7ED1B4D87141D1AF5A494F0CBA7C93F9393F68C3F5A35","9566080EB4E2F30912CF13A37C4CBF30AD88E2FDF42590D2FD31B6475BA27F27","FD2D14C57039C5B2F553175F5BD5C6547FC2999D88E68F75341FA4CC4D75BE3B","B253CDC7D0EAC8367D7B7FD1FFEC517C6A582ED828A17701A1C3F23C7F8BF9EE","6DC2944622B5F6F0F3FEA0590DFF4B1D21F9D6D0EEE9A2CB6FDF43BF8093002F","2685C846895A7F7541393561C45C627228D18E7464B36249255852C21280D0C6"};
                string[] text=File.ReadAllLines(Path.Combine(assets,"Graphics","SCNTEXT.TXT"));Require(text.Length==21,"Original description count");
                var previous=new byte[ClassicMenuText.TableBytes];for(int i=0;i<previous.Length;i++)previous[i]=0xcd;
                for(int i=0;i<21;i++){byte[] formatted=ClassicMenuText.Format(text[i],previous);Require(Hash(formatted)==expected[i],"Native description table "+i);Require(ClassicMenuText.ReadRows(formatted).Length==48,"Complete native row views");}
                foreach(byte b in previous)Require(b==0xcd,"Formatter changed caller data");
                string[] boundaryHashes={"9F61050C747829752C78B61B734BC59403A88A013714F02A42F84C58FF6950FF","88C17D6C0E94F3773B550C33A4ED141B9E009ADE58EF8AEC293DCA9B20385767","9C039FC22257459095BFCD3E674D19A826D1BE9177ED6E99998B80E38F8F0224"};string ring48="",ring49="";for(int i=0;i<49;i++){ring49+=new string('a',31)+" ";if(i<48)ring48+=new string('a',31)+" ";}string[] boundaries={new string('b',33),ring48,ring49};
                for(int i=0;i<boundaries.Length;i++)Require(Hash(ClassicMenuText.Format(boundaries[i],previous))==boundaryHashes[i],"Native row boundary "+i);
                Reject(delegate{ClassicMenuText.Format(null);});Reject(delegate{ClassicMenuText.Format("ok",new byte[1583]);});Reject(delegate{ClassicMenuText.Format(new string('a',34));});Reject(delegate{ClassicMenuText.Format("a\0b");});Reject(delegate{ClassicMenuText.Format("a\tb");});Reject(delegate{ClassicMenuText.Format("caf\u00e9");});Reject(delegate{ClassicMenuText.Format(new string(' ',8193));});Reject(delegate{ClassicMenuText.ReadRows(new byte[1583]);});var malformed=new byte[1584];for(int i=0;i<malformed.Length;i++)malformed[i]=65;Reject(delegate{ClassicMenuText.ReadRows(malformed);});malformed=new byte[1584];malformed[0]=1;Reject(delegate{ClassicMenuText.ReadRows(malformed);});
            });
            Func<SoloProgram> loadSolo=delegate {string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var palette=Assets.Pcx(Path.Combine(assets,"Graphics","E2_SOLO.PCX")).Palette;var resources=new Dictionary<int,List<IndexedImage>>{{28,Assets.Sph(Path.Combine(assets,"Graphics","EO1_SOLO.SPH"),palette)},{29,Assets.Sph(Path.Combine(assets,"Graphics","EO2_SOLO.SPH"),palette)},{38,Assets.Sph(Path.Combine(assets,"Graphics","EO4_SOLO.SPH"),palette)}};return SoloProgram.Read(File.ReadAllBytes(Path.Combine(p,"SoloEntry.seqbin")),File.ReadAllBytes(Path.Combine(p,"PracticeSetup.seqbin")),File.ReadAllBytes(Path.Combine(p,"PlayerLimits.seqbin")),resources,File.ReadAllLines(Path.Combine(assets,"Graphics","SCNTEXT.TXT")));};
            checkEvidence("Solo exact prefixes, text bounds and corrected native title label routes",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var palette=Assets.Pcx(Path.Combine(assets,"Graphics","E2_SOLO.PCX")).Palette;var resources=new Dictionary<int,List<IndexedImage>>{{28,Assets.Sph(Path.Combine(assets,"Graphics","EO1_SOLO.SPH"),palette)},{29,Assets.Sph(Path.Combine(assets,"Graphics","EO2_SOLO.SPH"),palette)},{38,Assets.Sph(Path.Combine(assets,"Graphics","EO4_SOLO.SPH"),palette)}};byte[][] bytes={File.ReadAllBytes(Path.Combine(p,"SoloEntry.seqbin")),File.ReadAllBytes(Path.Combine(p,"PracticeSetup.seqbin")),File.ReadAllBytes(Path.Combine(p,"PlayerLimits.seqbin"))};var text=File.ReadAllLines(Path.Combine(assets,"Graphics","SCNTEXT.TXT"));
                for(int k=0;k<3;k++){byte[] good=bytes[k];for(int at=0;at<good.Length;at+=4){byte[] bad=(byte[])good.Clone();bad[at]^=0x40;bytes[k]=bad;Reject(delegate{SoloProgram.Read(bytes[0],bytes[1],bytes[2],resources,text);});}bytes[k]=new byte[good.Length-1];Reject(delegate{SoloProgram.Read(bytes[0],bytes[1],bytes[2],resources,text);});bytes[k]=good;}Reject(delegate{SoloProgram.Read(bytes[0],bytes[1],bytes[2],resources,new string[20]);});
                string[] routes=File.ReadAllLines(Path.Combine(evidence,"Title-Routes-Oracle.csv"));Require(routes.Length==5&&routes[1]=="1,0044F09C,0044F0D4,0044F3F8"&&routes[2]=="2,0044F078,0044F090,0044F208"&&routes[3].EndsWith(",00450EC8")&&routes[4].EndsWith(",0044F148"),"Native label routing; Solo and Multiplayer must not be swapped");Require(text.Length==21&&loadSolo().Description(20).Contains("turn-based"),"Original Classic description");
            });
            checkEvidence("Practice player clamp, draw commands and indexed pixels match12native states",delegate {
                var program=loadSolo();var card=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=new Color[256]};for(int i=0;i<card.Pixels.Length;i++)card.Pixels[i]=(byte)(1+(i*7)%255);var states=new Dictionary<int,SoloState>();int count=0;
                foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Solo-State-Raster-Oracle.csv"))){if(row.StartsWith("Case"))continue;string[] p=row.Split(',');var state=new SoloState(program);state.SelectPlayers(int.Parse(p[2]));state.SelectType(int.Parse(p[1]));Require(state.Players==int.Parse(p[3])&&state.Playable==(state.Type==20),"Native player clamp/playability");states.Add(int.Parse(p[0]),state);using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(program.Compose(card,state).Pixels)).Replace("-","")==p[4],"Native Practice raster");count++;}Require(count==12,"Grouped setup-state coverage");
                count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Solo-Draw-Oracle.csv"))){if(row.StartsWith("Case"))continue;string[] p=row.Split(',');var commands=program.Commands(states[int.Parse(p[0])],-1);var command=commands[int.Parse(p[4])];var button=program.Buttons[command.Index];Require(command.Index==int.Parse(p[5])&&button.X==int.Parse(p[6])&&button.Y==int.Parse(p[7])&&command.Frame==int.Parse(p[8])&&button.Group==int.Parse(p[9]),"Native Practice sprite command");count++;}Require(count==42,"Draw command coverage");
            });
            checkEvidence("Practice button geometry and half-open hits match native edge sweep",delegate {
                var program=loadSolo();int count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Solo-Buttons-Oracle.csv"))){if(row.StartsWith("Index"))continue;string[] p=row.Split(',');var b=program.Buttons[int.Parse(p[0])];Require(b.X==int.Parse(p[1])&&b.Y==int.Parse(p[2])&&b.Action==int.Parse(p[3])&&b.Frame==int.Parse(p[4])&&b.Group==int.Parse(p[5])&&b.Bounds.Width==int.Parse(p[6])&&b.Bounds.Height==int.Parse(p[7]),"Setup button geometry");count++;}Require(count==14,"Setup button coverage");count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Solo-Hit-Oracle.csv"))){if(row.StartsWith("X"))continue;string[] p=row.Split(',');int hit=program.Hit(int.Parse(p[0]),int.Parse(p[1]));Require((hit<0?-2:program.Buttons[hit].Action)==int.Parse(p[2]),"Native setup hit");count++;}Require(count==3360,"Grouped setup boundary sweep");
            });
            checkEvidence("Result EOF/skip/init/failure ordering matches12guarded native cases",delegate {
                var cases=new Dictionary<string,List<string[]>>();foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Ending-Seam-Oracle.csv"))){if(row.StartsWith("Case"))continue;var c=row.Split(',');if(!cases.ContainsKey(c[0]))cases.Add(c[0],new List<string[]>());cases[c[0]].Add(c);}Require(cases.Count==12,"Result native case coverage");foreach(string movie in new[]{"VICTORY","LOSE"}){int n=movie=="VICTORY"?3:4;var rows=cases[movie+"-normal"];for(int t=0;t<n;t++){var c=rows[t];Require(int.Parse(c[5])==t+1&&int.Parse(c[6])==(t==n-1?0:1)&&int.Parse(c[7])==(t==n-1?1:0),"Native raw frame/EOF stop");var pixels=new byte[640*480];for(int i=0;i<pixels.Length;i++)pixels[i]=(byte)(20+(movie=="LOSE"?40:0)+t);Require(Hash(pixels)==c[10],"Native ending final-frame canvas");string events=Encoding.ASCII.GetString(Convert.FromBase64String(c[11]));Require(events.Contains("Decode:"+t)&&(t<n-1||events.EndsWith("|Stop")),"EOF decoder/stop ordering");}var skip=cases[movie+"-space-tick1"][1];Require(skip[3]=="0"&&skip[5]=="2"&&skip[7]=="1"&&Encoding.ASCII.GetString(Convert.FromBase64String(skip[11]))=="Cue:1|Decode:1|Stop","Native skip polls once before stop");var eof=cases[movie+"-space-eof"][n-1];Require(eof[7]=="2"&&eof[5]==n.ToString(),"EOF/Space override double stop");Require(cases[movie+"-space-init"][0][2]=="9"&&cases[movie+"-single-frame"][0][2]=="6","Native early init return boundaries");foreach(var failedCase in cases[movie+"-load-fails"])Require(failedCase[3]=="6"&&failedCase[5]=="0"&&failedCase[6]=="0"&&failedCase[7]=="0","Native failed-admission wait boundary");}
            });
            checkEvidence("Result full palettes match six actual native front-end outputs",delegate {
                byte[] island=File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL"));int count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Result-Palette-Oracle.csv"))){if(row.StartsWith("Clip"))continue;string[] c=row.Split(',');int state=int.Parse(c[2]);byte[] pal=File.ReadAllBytes(Path.Combine(assets,"HWEBS",c[0]+".PAL")),native=File.ReadAllBytes(Path.Combine(evidence,"Result-Palette-"+c[0]+"-"+state+".bin"));Require(Hash(native)==c[3]&&Hash(ClassicPalette.MovieSubset(pal,ClassicPalette.Patched(island),2,state))==c[3],"Native result palette context");var colors=PatMovie.ClipColors(pal,island,state==41);for(int i=10;i<=245;i++)Require(colors[i].R==native[i*3]&&colors[i].G==native[i*3+1]&&colors[i].B==native[i*3+2],"Native result display palette");count++;}Require(count==6,"Result palette case count");
            });
            checkEvidence("Startup exact script, native movie ordering, fades and skip return",delegate {
                byte[] script=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","Startup.seqbin"));StartupProgram.Read(script);for(int i=0;i<script.Length;i+=4){var bad=(byte[])script.Clone();bad[i]^=0x40;Reject(delegate{StartupProgram.Read(bad);});}Reject(delegate{StartupProgram.Read(new byte[63]);});Reject(delegate{StartupProgram.Directory("../INTRO");});Require(StartupProgram.SoundDirectory("UK_INTRO")=="INTRO"&&StartupProgram.Directory("HASBRO")=="HWEBS","Startup source alias/location");var rows=File.ReadAllLines(Path.Combine(evidence,"Startup-Program-Oracle.csv"));Require(rows.Length==117,"Two native startup traces");foreach(int finish in new[]{6,9}){int i=1+(finish==6?0:58);Require(rows[i]=="Startup,"+finish+",0,Clear,0","Startup initial clear");for(int movie=0;movie<3;movie++){int id=movie==0?141:movie==1?142:138;Require(rows[i+1+movie*19]=="Startup,"+finish+","+(1+movie*19)+",Movie,"+id,"Native startup order");for(int f=0;f<17;f++)Require(rows[i+2+movie*19+f]=="Startup,"+finish+","+(2+movie*19+f)+",Fade,"+(64-f*4),"Native startup fade");}Require(rows[i+57]=="Startup,"+finish+",57,Boundary,18","Startup title entry boundary");}
            });
            checkEvidence("Startup engine ownership, names and nine source palette outputs",delegate {
                var engine=File.ReadAllBytes(Path.Combine(evidence,"Startup-EngineDispatch.bin"));int selector=engine[0xa5];Require(BitConverter.ToUInt32(engine,0x80+selector*4)==0x40758f&&engine[0x1f]==0xe8&&0x407594+BitConverter.ToInt32(engine,0x20)==0x42e9d0,"Original front-end engine mode2 dispatch");var names=File.ReadAllBytes(Path.Combine(evidence,"Startup-MovieNames.bin"));int[] ids={141,142,138};for(int i=0;i<ids.Length;i++)Require(Encoding.ASCII.GetString(names,(ids[i]-138)*9,9).TrimEnd('\0')==StartupProgram.Names[i],"Original startup ID/name mapping");var clone=StartupProgram.Names;clone[0]="../bad";Require(StartupProgram.Names[0]=="HASBRO","Startup name table escaped ownership");byte[] island=File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL"));int count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Startup-Palette-Oracle.csv"))){if(row.StartsWith("Clip"))continue;string[] c=row.Split(',');int state=int.Parse(c[2]);byte[] pal=File.ReadAllBytes(Path.Combine(assets,"HWEBS",c[0]+".PAL")),native=File.ReadAllBytes(Path.Combine(evidence,"Startup-Palette-"+c[0]+"-"+state+".bin"));Require(Hash(native)==c[3]&&Hash(ClassicPalette.MovieSubset(pal,ClassicPalette.Patched(island),2,state))==c[3],"Native actual-asset palette");var colors=PatMovie.ClipColors(pal,island,state==41);for(int i=10;i<=245;i++)Require(colors[i].R==native[i*3]&&colors[i].G==native[i*3+1]&&colors[i].B==native[i*3+2],"Front-end/Credits palette display");count++;}Require(count==9,"Native palette context coverage");
            });
            checkEvidence("Startup1318decoded frames and three independent initial surfaces match source",delegate {
                var movies=new Dictionary<string,PatMovie>();foreach(string name in StartupProgram.Names){var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"HWEBS",name+".PAT")));Require(movie.DecodedWidth==320&&movie.DecodedHeight==240,"Startup native geometry");movies.Add(name,movie);}int count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Startup-Frames-Oracle.csv"))){if(row.StartsWith("Directory"))continue;string[] c=row.Split(',');var frame=movies[c[1]].Frames[int.Parse(c[2])];Require(frame.Offset==int.Parse(c[3])&&frame.PayloadBytes==int.Parse(c[4])&&Hash(frame.Pixels)==c[5],"Startup native consumed span/indexed frame");count++;}Require(count==1318&&movies["HASBRO"].Frames.Count==251&&movies["NMSLOGO1"].Frames.Count==109&&movies["UK_INTRO"].Frames.Count==958,"Startup full frame counts");foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Startup-FirstFrames-Oracle.csv"))){if(row.StartsWith("Directory"))continue;string[] c=row.Split(',');Require(c[2]=="1"&&c[3]==c[4]&&Hash(movies[c[1]].Frames[0].Pixels)==c[3],"Startup first-frame independence");}
            });
            checkEvidence("Startup52cue scheduling, source aliases, missing silent logo and converted PCM",delegate {
                var onsets=new Dictionary<string,List<SoundCue>>();foreach(string name in StartupProgram.Names){List<SoundCue> cues;int frames=name=="HASBRO"?251:name=="NMSLOGO1"?109:958;ClipMix mix=StartupSound.LoadMix(assets,name,frames,out cues);Require(mix.Count==ClipSound.FrameSample(frames,50),"Startup mixer duration");Require(name=="NMSLOGO1"?cues.Count==0:cues.Count>0,"Source logo/no-sequence policy");onsets.Add(name,cues);}int count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Startup-Sequence-Oracle.csv"))){if(row.StartsWith("Directory"))continue;string[] c=row.Split(',');if(c[2]=="Start"){Require(onsets[c[1]][int.Parse(c[3])].ActualStart==int.Parse(c[4])&&int.Parse(c[5])>0,"Startup native one-cue-per-frame order/missing audio");count++;}}Require(count==52,"Startup original cue coverage");Require(StartupSound.Pcm(File.ReadAllBytes(Path.Combine(assets,"SFX","HASBRO","JINGCD.WAV"))).Length==132300,"Original12second44.1kHz stereo jingle conversion");Require(ClipSound.Sequence(StartupSound.SequenceText(File.ReadAllBytes(Path.Combine(assets,"SFX","HASBRO","HASBRO.SEQ")))).Count==1,"DOS EOF source text");Reject(delegate{StartupSound.SequenceText(Encoding.ASCII.GetBytes("*END*\n\x1aX"));});
            });
            check("Startup PCM conversion uses independent channel/rate fixtures and rejects malformed waves",delegate {
                byte[] a=CueWaveFixture();var same=StartupSound.Pcm(a);var original=ClipSound.Pcm(a);Require(same.Length==original.Length,"Native-rate PCM sample count");for(int i=0;i<same.Length;i++)Require(same[i]==original[i],"Native-rate PCM changed");BitConverter.GetBytes(22050).CopyTo(a,24);BitConverter.GetBytes(22050).CopyTo(a,28);var half=StartupSound.Pcm(a);Require(half.Length==2&&half[0]==-16384&&half[1]==32512,"Unsigned22050 partial pair averaging");byte[] b=new byte[68];Array.Copy(CueWaveFixture(),b,44);BitConverter.GetBytes(60).CopyTo(b,4);W16(b,22,2);BitConverter.GetBytes(44100).CopyTo(b,24);BitConverter.GetBytes(176400).CopyTo(b,28);W16(b,32,4);W16(b,34,16);BitConverter.GetBytes(24).CopyTo(b,40);int[] values={1000,2000,3000,4000,5000,6000,7000,8000,-32768,-32768,0,0};for(int i=0;i<values.Length;i++)W16(b,44+2*i,values[i]);var quarter=StartupSound.Pcm(b);Require(quarter.Length==2&&quarter[0]==4500&&quarter[1]==-16384,"Stereo44100 frame/channel downmix and final partial bucket");foreach(int at in new[]{4,20,22,24,28,32,34,40}){var bad=(byte[])b.Clone();bad[at]=99;Reject(delegate{StartupSound.Pcm(bad);});}Reject(delegate{StartupSound.Pcm(new byte[43]);});
            });
            check("Quit exact script and sprite bounds reject unsupported variants",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs","MenuQuit.seqbin");byte[] bytes=File.ReadAllBytes(p);var frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_QUIT.SPH"),new Color[256]);var program=QuitProgram.Read(bytes,frames);Require(program.Buttons.Count==2,"Quit buttons");
                for(int i=0;i<bytes.Length;i+=4){byte[] bad=(byte[])bytes.Clone();bad[i]^=0x40;Reject(delegate{QuitProgram.Read(bad,frames);});}Reject(delegate{QuitProgram.Read(new byte[191],frames);});Reject(delegate{QuitProgram.Read(new byte[193],frames);});Reject(delegate{QuitProgram.Read(bytes,null);});var f=frames[1];frames[1]=null;Reject(delegate{QuitProgram.Read(bytes,frames);});frames[1]=f;int width=f.Width;f.Width=110;Reject(delegate{QuitProgram.Read(bytes,frames);});f.Width=width;
            });
            checkEvidence("Quit buttons and half-open hit bounds match guarded source",delegate {
                var frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_QUIT.SPH"),new Color[256]);var program=QuitProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","MenuQuit.seqbin")),frames);int count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Quit-Buttons-Oracle.csv"))){if(row.StartsWith("Index"))continue;string[] c=row.Split(',');var b=program.Buttons[int.Parse(c[0])];Require(b.X==int.Parse(c[1])&&b.Y==int.Parse(c[2])&&b.Action==int.Parse(c[3])&&b.Frame==int.Parse(c[4])&&b.Group==int.Parse(c[5])&&b.Bounds.Width==int.Parse(c[6])&&b.Bounds.Height==int.Parse(c[7]),"Quit native dimensions");count++;}Require(count==2,"Quit geometry coverage");count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Quit-Hit-Oracle.csv"))){if(row.StartsWith("X"))continue;string[] c=row.Split(',');int hit=program.Hit(int.Parse(c[0]),int.Parse(c[1]));Require((hit<0?-2:program.Buttons[hit].Action)==int.Parse(c[2]),"Quit source hit/edge");count++;}Require(count==450,"Quit grouped boundary coverage");
            });
            checkEvidence("Quit two indexed raster states and native Back/Escape/exit route",delegate {
                var card=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=new Color[256]};for(int i=0;i<card.Pixels.Length;i++)card.Pixels[i]=(byte)(1+(i*7)%255);var frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_QUIT.SPH"),card.Palette);var program=QuitProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","MenuQuit.seqbin")),frames);int count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Quit-Composite-Oracle.csv"))){if(row.StartsWith("Selected"))continue;string[] c=row.Split(',');Require(Hash(QuitProgram.Compose(card,frames,program,int.Parse(c[0])).Pixels)==c[1],"Quit source indexed composite");count++;}Require(count==2,"Quit raster coverage");var rows=File.ReadAllLines(Path.Combine(evidence,"Quit-Flow-Oracle.csv"));Require(Array.Exists(rows,r=>r.StartsWith("back,2,0,0044F068,39,2,2,0,4,0,3,"))&&Array.Exists(rows,r=>r.StartsWith("escape,2,0,0044F068,39,2,2,0,4,0,3,")),"Native title return");Require(Array.Exists(rows,r=>r.StartsWith("quit,20,6,0044F208,0,2,4,-1,2,0,2,0")),"Native terminal6");for(int t=1;t<=17;t++){int tick=t,factor=68-4*t;Require(Array.Exists(rows,r=>r.StartsWith("quit,"+tick+",0,0044F200,0,8,4,-1,2,0,2,"+factor)),"Native palette fade progression");}
            });
            checkEvidence("Exact title prefixes and native menu entry states",delegate {
                var frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_TITLE.SPH"),Assets.Pcx(Path.Combine(assets,"Graphics","E_TITLE.PCX")).Palette);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");byte[] entry=File.ReadAllBytes(Path.Combine(programs,"MenuEntry.seqbin")),title=File.ReadAllBytes(Path.Combine(programs,"MenuTitle.seqbin"));TitleProgram.Read(entry,title,frames);
                for(int at=0;at<entry.Length;at+=4){byte[] bad=(byte[])entry.Clone();bad[at]^=0x40;Reject(delegate{TitleProgram.Read(bad,title,frames);});}for(int at=0;at<title.Length;at+=4){byte[] bad=(byte[])title.Clone();bad[at]^=0x40;Reject(delegate{TitleProgram.Read(entry,bad,frames);});}Reject(delegate{TitleProgram.Read(new byte[55],title,frames);});Reject(delegate{TitleProgram.Read(entry,new byte[167],frames);});
                string[] states=File.ReadAllLines(Path.Combine(evidence,"Menu-State-Oracle.csv"));Require(states.Length==4&&states[1]=="VICTORY,-1,1,2,0,0,0,0,0,0,39,4"&&states[2]=="LOSE,-1,1,2,0,0,0,0,0,0,39,4"&&states[3]=="DirectNoFade,123,123,123,123,123,123,123,0,0,39,4","Native reset/boundary cases");
                string[] trace=File.ReadAllLines(Path.Combine(evidence,"Menu-Entry-Oracle.csv"));Require(trace.Length==32,"Native entry trace coverage");for(int k=0;k<2;k++){string name=k==0?"VICTORY":"LOSE";Require(trace[1+k*15]==name+",0,Card,e_title","Title loader");for(int i=0;i<14;i++)Require(trace[2+k*15+i]==name+","+(i+1)+",Fade,"+(12+4*i),"Title fade order");}Require(trace[31]=="DirectNoFade,0,Card,e_title","Direct no-fade branch");
            });
            checkEvidence("Title button geometry, split draw and half-open hits match original code",delegate {
                string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_TITLE.SPH"),Assets.Pcx(Path.Combine(assets,"Graphics","E_TITLE.PCX")).Palette);var menu=TitleProgram.Read(File.ReadAllBytes(Path.Combine(programs,"MenuEntry.seqbin")),File.ReadAllBytes(Path.Combine(programs,"MenuTitle.seqbin")),frames);int count=0;
                foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Menu-Buttons-Oracle.csv"))){if(row.StartsWith("Index"))continue;string[] p=row.Split(',');var b=menu.Buttons[int.Parse(p[0])];Require(b.X==int.Parse(p[1])&&b.Y==int.Parse(p[2])&&b.Action==int.Parse(p[3])&&b.Frame==int.Parse(p[4])&&b.Group==int.Parse(p[5])&&b.Bounds.Width==int.Parse(p[8])&&b.Bounds.Height==int.Parse(p[7]),"Native button geometry");count++;}Require(count==4,"Button coverage");
                count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Menu-Hit-Oracle.csv"))){if(row.StartsWith("X"))continue;string[] p=row.Split(',');int hit=menu.Hit(int.Parse(p[0]),int.Parse(p[1])),action=hit<0?-2:menu.Buttons[hit].Action;Require(action==int.Parse(p[2]),"Native half-open overlap/edge priority");count++;}Require(count==1104,"Grouped native boundary sweep");
                count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Menu-Draw-Oracle.csv"))){if(row.StartsWith("Selected"))continue;string[] p=row.Split(',');var b=menu.Buttons[int.Parse(p[0])];int half=int.Parse(p[1]);Require(int.Parse(p[2])==b.X+half*200&&int.Parse(p[3])==b.Y&&int.Parse(p[4])==b.Frame+half*4&&p[5]=="0"&&p[6]=="100"&&p[7]=="0","Native split sprite command");count++;}Require(count==8,"Menu draw coverage");
            });
            checkEvidence("Title composition matches all four native unscaled zero-key raster outputs",delegate {
                var card=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=new Color[256]};for(int i=0;i<card.Pixels.Length;i++)card.Pixels[i]=(byte)(1+(i*7)%255);var frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_TITLE.SPH"),card.Palette);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var menu=TitleProgram.Read(File.ReadAllBytes(Path.Combine(programs,"MenuEntry.seqbin")),File.ReadAllBytes(Path.Combine(programs,"MenuTitle.seqbin")),frames);int count=0;
                foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Menu-Composite-Oracle.csv"))){if(row.StartsWith("Selected"))continue;string[] p=row.Split(',');var actual=TitleProgram.Compose(card,frames,menu,int.Parse(p[0]));using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(actual.Pixels)).Replace("-","")==p[1],"Native unscaled indexed composition");count++;}Require(count==4,"Composed button coverage");
            });
            check("Bounded ending programs reject unsupported instructions/references",delegate {
                foreach(string name in MovieCatalog.Endings){byte[] bytes=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs",name+".seqbin"));var program=EndingProgram.Read(bytes);Require(program.Movie==name&&program.HoldMilliseconds==5000&&program.MenuPointer==0x44ef88,"Ending program decode");foreach(int at in new[]{0,4,8,12,16,20,24,28,32,36,40,44,48,52,56,60}){byte[] corrupt=(byte[])bytes.Clone();corrupt[at]^=0x40;Reject(delegate{EndingProgram.Read(corrupt);});}Reject(delegate{EndingProgram.Read(new byte[63]);});}
            });
            checkEvidence("Managed ending prefix matches four native completion/skip traces",delegate {
                var all=new Dictionary<string,List<string>>();foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Ending-Program-Oracle.csv"))){if(row.StartsWith("Outcome"))continue;string[] p=row.Split(',');string key=p[0]+p[1];if(!all.ContainsKey(key))all[key]=new List<string>();Require(int.Parse(p[2])==all[key].Count,"Native prefix order");all[key].Add(p[3]+","+p[4]);}
                foreach(string name in MovieCatalog.Endings)foreach(int status in new[]{6,9}){var timeline=new EndingTimeline(EndingProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs",name+".seqbin"))));timeline.Start();timeline.Advance(6240);Require(timeline.Phase==EndingPhase.Movie,"Ending must block on movie");timeline.Advance(60000);Require(timeline.Phase==EndingPhase.Movie,"Ending crossed pending movie");Require(timeline.CompleteMovie(),"Ending movie completion");timeline.Advance(680);Require(timeline.Phase==EndingPhase.Complete&&!timeline.Playing&&!timeline.CompleteMovie(),"Ending handoff/completion");var native=all[name+status];Require(native.Count==54&&timeline.Trace.Count==native.Count,"Ending trace coverage");for(int i=0;i<native.Count;i++)Require(native[i]==timeline.Trace[i],"Ending native step "+name+"/"+i);}Require(all.Count==4,"Native ending case count");
            });
            check("Ending wait/transition clock partitions and pause without progression",delegate {
                var program=EndingProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","VICTORY.seqbin")));var a=new EndingTimeline(program);var b=new EndingTimeline(program);a.Start();b.Start();for(int i=0;i<624;i++)a.Advance(10);b.Advance(6240);Require(a.Phase==b.Phase&&string.Join(";",a.Trace.ToArray())==string.Join(";",b.Trace.ToArray()),"Ending partition history");a.Pause();a.Advance(10000);Require(!a.CompleteMovie()&&a.Phase==EndingPhase.Movie,"Paused ending advanced");a.Resume();Require(a.CompleteMovie(),"Ending resume gate");a.Advance(679);Require(a.Phase==EndingPhase.MovieFade&&a.Factor==0,"Last fade factor must hold");a.Advance(1);Require(a.Phase==EndingPhase.Complete,"Fade final duration");a.Reset();Require(a.Phase==EndingPhase.Idle&&!a.Playing&&a.Trace.Count==0,"Ending reset");
            });
            check("Palette fades retain protected20entries and use native scale arithmetic",delegate {
                var colors=new Color[256];byte[] rgb=new byte[768];for(int i=0;i<256;i++){colors[i]=Color.FromArgb(i,255-i,i/2);rgb[i*3]=(byte)i;rgb[i*3+1]=(byte)(255-i);rgb[i*3+2]=(byte)(i/2);}foreach(int factor in new[]{0,12,16,60,64}){Color[] faded=ClassicPalette.FadeDisplay(colors,factor);byte[] nativeArithmetic=ClassicPalette.Fade(rgb,factor);for(int i=0;i<256;i++){if(i<10||i>245)Require(faded[i]==colors[i],"Protected palette index faded");else Require(faded[i].R==nativeArithmetic[3*i]&&faded[i].G==nativeArithmetic[3*i+1]&&faded[i].B==nativeArithmetic[3*i+2],"Display fade arithmetic");}}
            });
            checkEvidence("Ending catalog and20fps route against original script/name evidence",delegate {
                int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Ending-Catalog.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');Require(MovieCatalog.IsEnding(p[0])&&MovieCatalog.FrameMilliseconds(p[0])==int.Parse(p[4])&&MovieCatalog.Directory(p[0],false)=="HWEBS"&&MovieCatalog.PaletteDirectory(p[0])=="HWEBS","Ending context metadata");byte[] script=File.ReadAllBytes(Path.Combine(evidence,p[0]=="VICTORY"?"Ending-VictoryScript.bin":"Ending-LossScript.bin"));Require(BitConverter.ToInt32(script,32)==6&&BitConverter.ToInt32(script,36)==int.Parse(p[1]),"Native ending operand");Require(!File.Exists(Path.Combine(assets,"LWEBS",p[0]+".PAT")),"Unexpected synthetic low ending");rows++;}byte[] clock=File.ReadAllBytes(Path.Combine(evidence,"Ending-MovieWait.bin"));Require(clock[11]==0x83&&clock[12]==0xf8&&clock[13]==50&&MovieCatalog.Names.Length==63&&rows==2,"Original50ms limiter/catalog");
            });
            checkEvidence("Both ending movies against1341original decoder frames",delegate {
                var fixtures=new Dictionary<string,List<string[]>>();foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Ending-Frames-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');if(!fixtures.ContainsKey(p[0]))fixtures.Add(p[0],new List<string[]>());fixtures[p[0]].Add(p);}int total=0;
                foreach(string name in MovieCatalog.Endings){var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"HWEBS",name+".PAT")));Require(movie.DecodedWidth==320&&movie.DecodedHeight==240&&movie.Frames.Count==(name=="VICTORY"?661:680),"Native long ending count");for(int i=0;i<movie.Frames.Count;i++){var frame=movie.Frames[i];string[] row=fixtures[name][i];Require(int.Parse(row[1])==i&&frame.Offset==int.Parse(row[2])&&frame.PayloadBytes==int.Parse(row[3]),"Ending native frame bounds");using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(frame.Pixels)).Replace("-","")==row[4],"Ending native pixels "+name+"/"+i);total++;}}Require(total==1341,"Ending decoded coverage");
            });
            checkEvidence("Ending60cue native polling/cleanup and26original PCM paths",delegate {
                int total=0,rows=0;foreach(string name in MovieCatalog.Endings){int count=name=="VICTORY"?661:680;List<SoundCue> cues;short[] mixed=ClipSound.Load(assets,name,count,50,out cues);Require(mixed.Length==(name=="VICTORY"?364376:374850),"Ending finite sample count");total+=cues.Count;var starts=new List<int>();foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Ending-Sequence-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');if(p[0]!=name)continue;if(p[1]=="Start"){int cue=int.Parse(p[2]);Require(cues[cue].ActualStart==int.Parse(p[3]),"Ending native onset");starts.Add(cue);}else Require(int.Parse(p[3])==count,"Ending native close cleanup");rows++;}Require(starts.Count==cues.Count,"Ending requested cue total");}
                int files=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Ending-Wave-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');Require(ClipSound.Pcm(File.ReadAllBytes(Path.Combine(assets,"SFX",p[0],p[1]))).Length==int.Parse(p[7]),"Ending PCM samples");files++;}Require(total==60&&rows==120&&files==26,"Ending sound fixture coverage");
            });
            check("20fps partitions, fractional sample placement and complete last-frame hold",delegate {
                var a=new ClipTimeline(661,50);var b=new ClipTimeline(661,50);a.Play();b.Play();for(int i=0;i<100;i++)a.Advance(10);for(int i=0;i<20;i++)b.Advance(50);Require(a.Frame==20&&b.Frame==20&&a.PositionMilliseconds==1000,"20fps time partitions");a.Synchronize(33049);Require(a.Frame==660&&!a.Completed,"20fps last-frame hold");a.Synchronize(33050);Require(a.Completed&&a.DurationMilliseconds==33050,"20fps finite end");int[] expected={0,551,1102,1653,2205};for(int i=0;i<5;i++)Require(ClipSound.FrameSample(i,50)==expected[i],"Fractional sample floor");Require(ClipSound.FrameSample(661,50)==364376,"No cumulative sample rounding");var cues=ClipSound.Sequence("A,1,2\nB,3,4\n*END*");short[] mixed=ClipSound.Mix(5,50,cues,delegate(string name){return new short[]{1234,-1234};});Require(mixed.Length==2756&&mixed[550]==0&&mixed[551]==1234&&mixed[552]==-1234&&mixed[1653]==1234,"20fps cue placement");Reject(delegate{ClipSound.Sequence("A,1024,1024\n*END*");});
            });
            check("Long ending bitmap cache keeps one current and one retiring frame",delegate {
                var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"HWEBS","LOSE.PAT")));var colors=PatMovie.ClipColors(File.ReadAllBytes(Path.Combine(assets,"HWEBS","LOSE.PAL")),File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")),false);Bitmap previous=null;
                using(var cache=new ClipFrames(movie,colors)){for(int i=0;i<movie.Frames.Count;i++){Bitmap image=cache.Frame(i);Require(cache.CachedBitmaps==(i==0?1:2)&&object.ReferenceEquals(image,cache.Frame(i)),"Unbounded/repeated bitmap realization");Require(image.GetPixel(319,239).ToArgb()==colors[movie.Frames[i].Pixels[76799]].ToArgb(),"Cached ending image pixel");if(previous!=null)Require(previous.Width==320,"Borrowed image retired early");cache.ReleaseRetired();Require(cache.CachedBitmaps==1,"Retired bitmap retained");if(previous!=null){bool disposed=false;try{int width=previous.Width;}catch(ArgumentException){disposed=true;}Require(disposed,"Previous bitmap lifetime");}previous=image;}}bool finalDisposed=false;try{int width=previous.Width;}catch(ArgumentException){finalDisposed=true;}Require(finalDisposed,"Cache dispose retained final image");
            });
            checkEvidence("Classic HUD dispatch against 486 native cases and 4032 ordered commands",delegate {
                int current=-1,order=0,cases=0,commands=0;List<ClassicHudCommand> plan=null;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Hud-Oracle.csv"))){if(line.StartsWith("Case"))continue;string[] p=line.Split(',');int id=int.Parse(p[0]);if(id!=current){if(plan!=null)Require(order==plan.Count,"HUD missing native draw command");current=id;order=0;cases++;plan=ClassicHud.Commands(int.Parse(p[1]),int.Parse(p[2])!=0,int.Parse(p[3]),int.Parse(p[4]),int.Parse(p[5]));}Require(order==int.Parse(p[6])&&order<plan.Count,"HUD command index");ClassicHudCommand command=plan[order];Require(command.Panel==(p[7]=="Panels")&&command.Frame==int.Parse(p[8])&&command.At==new Point(int.Parse(p[9]),int.Parse(p[10]))&&int.Parse(p[11])==100&&int.Parse(p[12])==0&&int.Parse(p[13])==0,"Native HUD command "+id+"/"+order);order++;commands++;}Require(order==plan.Count&&cases==486&&commands==4032,"HUD fixture coverage");
            });
            check("Original cinematic HUD shape and deliberately opaque stored backing",delegate {
                var source=Assets.Sph(Path.Combine(assets,"Graphics","E_CLAS2.SPH"),Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette);int stored=0,black=0;using(var hud=new ClassicHud(Path.Combine(assets,"Graphics"),source[0].Palette)){for(int i=0;i<4;i++){var f=source[i];Bitmap bitmap=hud.Panel(i);for(int y=0;y<f.Height;y++)for(int x=0;x<f.Width;x++){int at=y*f.Width+x;Color color=bitmap.GetPixel(x,y);Require(color.A==(f.Visible[at]?255:0),"HUD stored-run opacity");if(f.Visible[at]){Require(color.ToArgb()==f.Palette[f.Pixels[at]].ToArgb(),"HUD source color");stored++;if(f.Pixels[at]==0)black++;}}}}Require(stored==109467&&black>0,"HUD mask/backing fixture");
            });
            checkEvidence("Progressive nearest-neighbor output matches original 640x480 scalers",delegate {
                var colors=new Color[256];for(int i=0;i<256;i++)colors[i]=Color.FromArgb(i,i,i);
                foreach(int factor in new[]{2,4}){int width=640/factor,height=480/factor;byte[] input=File.ReadAllBytes(Path.Combine(evidence,"Scale"+factor+"-Input.idx")),expected=File.ReadAllBytes(Path.Combine(evidence,"Scale"+factor+"-Output.idx"));Require(input.Length==width*height&&expected.Length==640*480,"Native scaler dimensions");using(var image=new IndexedImage{Width=width,Height=height,Pixels=input,Palette=colors}.Bitmap())using(var result=new Bitmap(640,480)){using(Graphics g=Graphics.FromImage(result)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;g.DrawImage(image,new Rectangle(0,0,640,480));}var data=result.LockBits(new Rectangle(0,0,640,480),System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);try{byte[] row=new byte[640*4];for(int y=0;y<480;y++){System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);for(int x=0;x<640;x++)Require(row[x*4]==expected[y*640+x]&&row[x*4+1]==row[x*4]&&row[x*4+2]==row[x*4],"Original scaler pixel "+factor+"/"+x+","+y);}}finally{result.UnlockBits(data);}}}
                Require(ClassicGeometry.ViewWidth==640&&ClassicGeometry.ViewHeight==480,"Viewport differs from source scaler");
            });
            check("Classic HUD eight-phase mapping follows pending shot and turn handoff",delegate {
                var game=new Game(950,RuleProfile.Recovered,OpponentKind.Original,1,true);game.AutoPlace();game.Start();Require(ClassicHud.Phase(game)==0,"Waiting player phase");game.Fire(game.Enemy.Ships[0].Cells[0]);Require(ClassicHud.Phase(game)==1,"Player firing phase");var pending=game.Pending;game.CompleteMovie(pending,ShotStage.Firing);Require(ClassicHud.Phase(game)==2,"Player travel phase");game.CompleteMovie(pending,ShotStage.Travel);Require(ClassicHud.Phase(game)==3,"Player feedback phase");game.CompleteMovie(pending,ShotStage.Feedback);Require(ClassicHud.Phase(game)==4,"Opponent wait phase");game.AdvanceOpponent();Require(ClassicHud.Phase(game)==5,"Opponent firing phase");pending=game.Pending;game.CompleteMovie(pending,ShotStage.Firing);Require(ClassicHud.Phase(game)==6,"Opponent travel phase");game.CompleteMovie(pending,ShotStage.Travel);Require(ClassicHud.Phase(game)==7,"Opponent feedback phase");game.CompleteMovie(pending,ShotStage.Feedback);Require(ClassicHud.Phase(game)==0,"Player return phase");
            });
            checkEvidence("All 61 high-detail clips against 5641 original 320x240 decoder frames",delegate {
                var fixtures=new Dictionary<string,List<string[]>>();foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-High-Frames-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');if(!fixtures.ContainsKey(p[0]))fixtures.Add(p[0],new List<string[]>());fixtures[p[0]].Add(p);}
                int rows=0;foreach(string name in FiringClips.Names){var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"HWEBS",name+".PAT")));Require(movie.DecodedWidth==320&&movie.DecodedHeight==240&&movie.DisplayScale==2&&movie.Variant==2&&movie.Frames.Count==fixtures[name].Count,"High detail dimensions/count "+name);for(int i=0;i<movie.Frames.Count;i++){var frame=movie.Frames[i];string[] p=fixtures[name][i];Require(int.Parse(p[1])==i&&frame.Offset==int.Parse(p[2])&&frame.PayloadBytes==int.Parse(p[3])&&frame.Pixels.Length==76800,"High native frame span "+name);using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(frame.Pixels)).Replace("-","")==p[4],"High native pixels "+name+"/"+i);rows++;}}
                Require(fixtures.Count==61&&rows==5641,"High decoder fixture coverage");
            });
            checkEvidence("High detail preserves source palettes and documented per-clip durations",delegate {
                var lowCounts=new Dictionary<string,int>();foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Impact-Clip-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');lowCounts.Add(p[0],int.Parse(p[2]));}
                int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"HWEBS-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');Require(int.Parse(p[1])==lowCounts[p[0]]+(p[0]=="BATT_DSH"?1:p[0]=="FRG_DSH"?-1:0)&&p[4]=="28-1E-02-02"&&p[5]=="True"&&p[6]=="True","High framing/timing/palette source "+p[0]);using(var sha=System.Security.Cryptography.SHA256.Create()){Require(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(assets,"HWEBS",p[0]+".PAT")))).Replace("-","")==p[7],"High source identity");Require(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(assets,"LWEBS",p[0]+".PAL")))).Replace("-","")==p[8],"Shared low/high palette identity");}rows++;}Require(rows==61,"High inventory coverage");
            });
            check("Independent high block stride and malformed/mixed variant rejection",delegate {
                byte[] b=new byte[2412];BitConverter.GetBytes(2404).CopyTo(b,0);b[4]=40;b[5]=30;b[6]=2;b[7]=2;for(int i=0;i<1200;i++){b[8+i*2]=0;b[9+i*2]=(byte)((i%40)+(i/40)*3);}BitConverter.GetBytes(uint.MaxValue).CopyTo(b,2408);var movie=PatMovie.Load(b);byte[] pixels=movie.Frames[0].Pixels;Require(pixels[0]==0&&pixels[319]==39&&pixels[239*320]==87&&pixels[239*320+319]==126&&pixels[8]==1&&pixels[8*320]==3&&movie.Frames[0].Opcodes[0]==1200,"High block rows/stride");
                b[8]=1;Reject(delegate{PatMovie.Load(b);});b[8]=0;b[7]=4;Reject(delegate{PatMovie.Load(b);});b[7]=2;byte[] mixed=new byte[2420];Array.Copy(b,mixed,2408);BitConverter.GetBytes(4).CopyTo(mixed,2408);mixed[2412]=20;mixed[2413]=15;mixed[2414]=4;mixed[2415]=4;BitConverter.GetBytes(uint.MaxValue).CopyTo(mixed,2416);Reject(delegate{PatMovie.Load(mixed);});
            });
            check("High-detail preference preserves legacy settings and unknown keys",delegate {
                Require(!DisplaySettings.Parse("movies=true\nsound=false\n").HighMovies,"Legacy detail changed");var prefs=DisplaySettings.Parse("highmovies=true\nmovies=true\nsound=false\ncustom=kept\n");var again=DisplaySettings.Parse(prefs.Serialize());Require(again.HighMovies&&again.MovieTurns&&!again.Sound&&again.Serialize().Contains("custom=kept"),"High preference round trip");Require(!DisplaySettings.Parse("highmovies=invalid\n").HighMovies,"Invalid high setting accepted");
            });
            check("Backdrop mask keeps grid lines opaque, surfaces at 70% and separate HUD holes open",delegate {
                var source=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));byte[] before=(byte[])source.Pixels.Clone();
                using(var foreground=ClassicBackdrop.Foreground(source)){
                    foreach(Point p in new[]{new Point(223,305),new Point(437,162),new Point(28,305),new Point(437,32)})Require(foreground.GetPixel(p.X,p.Y).ToArgb()==source.Palette[source.Pixels[p.Y*640+p.X]].ToArgb(),"Protected board/HUD pixel "+p);
                    foreach(Point p in new[]{new Point(300,80),new Point(10,240),new Point(630,240),new Point(10,20),new Point(100,100),new Point(500,350),new Point(400,450)})Require(foreground.GetPixel(p.X,p.Y).A==0,"Background was not opened "+p);
                    foreach(Point p in new[]{new Point(145,318),new Point(359,175)}){Color color=foreground.GetPixel(p.X,p.Y),original=source.Palette[source.Pixels[p.Y*640+p.X]];Require(color.A==179&&color.R==original.R&&color.G==original.G&&color.B==original.B,"Surface opacity/RGB differs "+p);}
                }
                for(int i=0;i<before.Length;i++)Require(before[i]==source.Pixels[i],"Original indexed data modified");Require(source.Visible==null,"Original visibility data modified");Reject(delegate{ClassicBackdrop.Foreground(new IndexedImage{Width=1,Height=1,Pixels=new byte[1]});});
            });
            check("Backdrop cover keeps movie aspect ratio and centered bounded cropping",delegate {
                foreach(RectangleF area in new[]{new RectangleF(0,0,640,512),new RectangleF(0,0,1920,1080),new RectangleF(12,9,747,430)}){RectangleF fit=ClassicBackdrop.Cover(new Size(160,120),area);Require(fit.Left<=area.Left+.01f&&fit.Top<=area.Top+.01f&&fit.Right>=area.Right-.01f&&fit.Bottom>=area.Bottom-.01f,"Backdrop leaves gaps");Require(Math.Abs(fit.Width/fit.Height-4f/3)<.00001f&&Math.Abs(fit.Left+fit.Width/2-area.Left-area.Width/2)<.01f&&Math.Abs(fit.Top+fit.Height/2-area.Top-area.Height/2)<.01f,"Backdrop stretched/offcenter");}
                bool rejected=false;try{ClassicBackdrop.Cover(Size.Empty,new RectangleF(0,0,1,1));}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Invalid movie dimensions accepted");
            });
            check("Independent signed 16-bit cue conversion and alignment rejection",delegate {
                byte[] b=new byte[50];Array.Copy(CueWaveFixture(),b,44);BitConverter.GetBytes(42).CopyTo(b,4);BitConverter.GetBytes(22050).CopyTo(b,28);W16(b,32,2);W16(b,34,16);BitConverter.GetBytes(6).CopyTo(b,40);W16(b,44,32768);W16(b,46,12345);W16(b,48,32767);short[] decoded=ClipSound.Pcm(b);Require(decoded.Length==3&&decoded[0]==short.MinValue&&decoded[1]==12345&&decoded[2]==short.MaxValue,"Signed PCM values/byte order");b[40]=5;Reject(delegate{ClipSound.Pcm(b);});b[40]=6;b[32]=1;Reject(delegate{ClipSound.Pcm(b);});
            });
            checkEvidence("Impact selectors against 252 original hit/miss/sink cases",delegate {
                int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Impact-Oracle.csv"))){if(line.StartsWith("Kind"))continue;string[] p=line.Split(',');int type=int.Parse(p[1]),length=int.Parse(p[2]),remaining=int.Parse(p[3]),weapon=int.Parse(p[4]),roll=int.Parse(p[5]);int actual=p[0]=="Hit"?ClassicImpact.Hit(type,length,remaining,weapon):p[0]=="Sink"?ClassicImpact.Sink(type):ClassicImpact.Miss(weapon,roll);int draws=p[0]=="Miss"&&(weapon>=14&&weapon<=18||weapon==25||weapon==26)?1:0;Require(actual==int.Parse(p[6])&&draws==int.Parse(p[7])&&int.Parse(p[8])==1,"Native selector "+line);rows++;}Require(rows==252,"Impact fixture total");
            });
            checkEvidence("Native movie names identify every impact and corrected travel clip",delegate {
                var names=new Dictionary<int,string>();foreach(string file in new[]{"Movie-Names.csv","Classic-LateMovieNames.csv"})foreach(string line in File.ReadAllLines(Path.Combine(evidence,file))){if(line.StartsWith("\"MovieID\""))continue;string[] p=line.Replace("\"","").Split(',');names.Add(int.Parse(p[0]),p[1]);}
                var reachable=new HashSet<string>();foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Impact-Oracle.csv"))){if(line.StartsWith("Kind"))continue;int id=int.Parse(line.Split(',')[6]);if(id==0)continue;Require(ClassicSequence.MovieName(id)==names[id],"Native impact movie name "+id);reachable.Add(names[id]);}
                Require(ClassicSequence.MovieName(12)==names[12]&&names[12]=="MIS_FLY2"&&ClassicSequence.MovieName(13)=="MIS_FLY3","Travel/miss movie IDs were conflated");Require(reachable.Count==33&&new HashSet<string>(FiringClips.Names).Count==61,"Impact/catalog names");
            });
            checkEvidence("61 original combat clips: 5641 native frames, 404 cues, 232 PCM paths",delegate {
                var movies=new Dictionary<string,PatMovie>();var cues=new Dictionary<string,List<SoundCue>>();int frames=0,totalCues=0;
                foreach(string name in FiringClips.Names){var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAT")));movies.Add(name,movie);frames+=movie.Frames.Count;List<SoundCue> sequence;ClipSound.Load(assets,name,movie.Frames.Count,out sequence);cues.Add(name,sequence);totalCues+=sequence.Count;}
                Require(movies.Count==61&&frames==5641&&totalCues==404,"Impact library totals");int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Impact-Frames-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');var f=movies[p[0]].Frames[int.Parse(p[1])];Require(f.Offset==int.Parse(p[2])&&f.PayloadBytes==int.Parse(p[3]),"Impact native frame span");using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(f.Pixels)).Replace("-","")==p[4],"Impact native pixels "+p[0]+"/"+p[1]);rows++;}Require(rows==5641,"Impact frame coverage");
                int starts=0,stops=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Impact-Sequence-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');if(p[1]=="Start"){Require(cues[p[0]][int.Parse(p[2])].ActualStart==int.Parse(p[3]),"Impact cue onset");starts++;}else {Require(int.Parse(p[3])==movies[p[0]].Frames.Count,"Impact cue cleanup");stops++;}}Require(starts==404&&stops==404,"Impact cue coverage");
                int files=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Impact-Wave-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');Require(ClipSound.Pcm(File.ReadAllBytes(Path.Combine(assets,"SFX",p[0],p[1]))).Length==int.Parse(p[7]),"Impact PCM samples");files++;}Require(files==232,"Impact PCM coverage");
            });
            check("Default-setting impact and terminal sink wait, pause and once-only completion",delegate {
                var g=new Game(940,RuleProfile.Recovered,OpponentKind.Original,0,true);g.AutoPlace();g.Start();Point target=g.Enemy.Ships[0].Cells[0];var ship=g.Enemy.Ships[0];Require(g.Fire(target),"Default reservation");var pending=g.Pending;g.CompleteMovie(pending,ShotStage.Firing);g.CompleteMovie(pending,ShotStage.Travel);Require(pending.Stage==ShotStage.Feedback&&pending.FeedbackId!=0&&pending.FeedbackId!=137&&ship.Hits==1&&g.Phase==Phase.PlayerTurn,"Default impact movie omitted");g.Paused=true;Require(!g.CompleteMovie(pending,ShotStage.Feedback),"Paused impact completed");g.Paused=false;Require(g.CompleteMovie(pending,ShotStage.Feedback)&&g.Phase==Phase.OpponentTurn&&!g.CompleteMovie(pending,ShotStage.Feedback),"Impact handoff/stale guard");
                for(int level=0;level<4;level++){g=new Game(941+level,RuleProfile.Recovered,OpponentKind.Original,level,true);g.AutoPlace();g.Start();g.Enemy=new Board();g.Enemy.Place(new Point(0,0),1,true,"Last",1);g.Fire(new Point(0,0));pending=g.Pending;g.CompleteMovie(pending,ShotStage.Firing);g.CompleteMovie(pending,ShotStage.Travel);Require(pending.Result.Sunk&&pending.MovieName=="BATTSINK"&&g.Phase==Phase.PlayerTurn&&g.Enemy.AllSunk,"Sink ended game before playback");g.CompleteMovie(pending,ShotStage.Feedback);Require(g.Phase==Phase.Finished&&g.Pending==null,"Sink movie did not finish victory");}
            });
            checkEvidence("Original solo phase and unavailable-movie transitions (528 controlled cases)",delegate {
                int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Phase-Oracle.csv"))){if(line.StartsWith("Stage"))continue;string[] p=line.Split(',');int stage=int.Parse(p[0]),weapon=int.Parse(p[1]),roll=int.Parse(p[2]),unavailable=int.Parse(p[3]);bool travel=stage==1||stage==5;int next=stage==7?0:stage+1,impacts=stage==2||stage==6?1:0;if(travel&&unavailable==1){next++;impacts++;}Require(int.Parse(p[4])==next&&int.Parse(p[5])==(travel?ClassicSequence.Travel(weapon,roll):0)&&int.Parse(p[6])==impacts&&int.Parse(p[7])==(travel&&weapon>=14&&weapon<=18?1:0),"Native phase/availability output");rows++;}Require(rows==528,"Phase fixture count");
            });
            checkEvidence("Complete 28-clip shot library: 2179 native frames, 117 cues and 84 PCM files",delegate {
                var movies=new Dictionary<string,PatMovie>();var cues=new Dictionary<string,List<SoundCue>>();int frames=0,totalCues=0;
                foreach(string name in FiringClips.SequenceNames){var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAT")));movies.Add(name,movie);frames+=movie.Frames.Count;List<SoundCue> sequence;ClipSound.Load(assets,name,movie.Frames.Count,out sequence);cues.Add(name,sequence);totalCues+=sequence.Count;}
                Require(movies.Count==28&&frames==2179&&totalCues==117,"Shot library totals");int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Shot-Frames-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');var f=movies[p[0]].Frames[int.Parse(p[1])];using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(f.Pixels)).Replace("-","")==p[4],"Shot frame "+p[0]+"/"+p[1]);rows++;}Require(rows==2179,"Shot decoder coverage");
                int starts=0,stops=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Combat-Sequence-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');if(p[1]=="Start"){Require(cues[p[0]][int.Parse(p[2])].ActualStart==int.Parse(p[3]),"Shot cue onset");starts++;}else{Require(int.Parse(p[3])==movies[p[0]].Frames.Count,"Shot cue cleanup");stops++;}}Require(starts==117&&stops==117,"Shot cue fixture coverage");
                int files=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Combat-Wave-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');Require(ClipSound.Pcm(File.ReadAllBytes(Path.Combine(assets,"SFX",p[0],p[1]))).Length==int.Parse(p[7]),"Shot PCM count");files++;}Require(files==84,"Shot PCM paths");
            });
            check("Movie shot reserves without damage, impacts once after travel and completes feedback",delegate {
                var g=new Game(920,RuleProfile.Recovered,OpponentKind.Original,1,true);g.AutoPlace();g.Start();Point target=g.Enemy.Ships[0].Cells[0];var ship=g.Enemy.Ships[0];int events=g.Events.Count;
                Require(g.Fire(target)&&g.Pending!=null&&ship.Hits==0&&!g.Enemy.Shots[target.X,target.Y],"Reservation applied premature damage");var pending=g.Pending;
                Require(!g.Fire(new Point(9,9))&&!g.AdvanceOpponent()&&!g.SelectShooter(0)&&g.Events.Count==events,"Busy input gate");
                Require(g.CompleteMovie(pending,ShotStage.Firing)&&pending.Stage==ShotStage.Travel&&ship.Hits==0,"Firing completion");Require(!g.CompleteMovie(pending,ShotStage.Firing),"Stale firing callback advanced travel");
                g.TogglePause();Require(!g.CompleteMovie(pending,ShotStage.Travel)&&ship.Hits==0,"Paused movie applied impact");g.TogglePause();
                Require(g.CompleteMovie(pending,ShotStage.Travel)&&ship.Hits==1&&pending.Stage==ShotStage.Feedback&&pending.MovieName=="CL_HIT"&&g.Phase==Phase.PlayerTurn,"Travel impact/feedback order");Require(!g.CompleteMovie(pending,ShotStage.Travel)&&ship.Hits==1,"Duplicate impact");
                Require(g.CompleteMovie(pending,ShotStage.Feedback)&&g.Pending==null&&g.Phase==Phase.OpponentTurn&&g.Events.Count==events+1,"Feedback turn handoff");Require(!g.CompleteMovie(pending,ShotStage.Feedback),"Old shot callback reused");
            });
            check("Repeated sunk-cell feedback follows native cleanup and completes without new damage",delegate {
                bool covered=false;
                for(int seed=0;seed<512&&!covered;seed++) {
                    var g=new Game(seed,RuleProfile.Recovered,OpponentKind.Original,0,true);g.AutoPlace();g.Start();g.Player=new Board();
                    g.Player.Place(new Point(6,2),5,true,"Sunk battleship",1);g.Player.Place(new Point(8,0),2,true,"Survivor",4);
                    Ship sunk=g.Player.Ships[0];
                    for(int x=0;x<10;x++)for(int y=0;y<10;y++){var p=new Point(x,y);if(g.Player.ShipAt(p)!=g.Player.Ships[1])g.Player.Shoot(p);}
                    g.Phase=Phase.OpponentTurn;
                    if(!g.AdvanceOpponent()||g.Pending==null||g.Player.ShipAt(g.Pending.Cell)!=sunk)continue;
                    int weapon=g.Pending.Firing.WeaponId;if(!((weapon>=14&&weapon<=18)||weapon==25||weapon==26))continue;
                    var pending=g.Pending;int hits=sunk.Hits;
                    Require(g.CompleteMovie(pending,ShotStage.Firing),"Repeated shot firing completion");
                    var expectedRandom=new NativeRandom(unchecked((uint)seed)^0x42AC1u);
                    int expected=ClassicImpact.Miss(weapon,expectedRandom.Next());
                    Require(g.CompleteMovie(pending,ShotStage.Travel)&&g.LastEnemyShot.Repeated&&!g.LastEnemyShot.Hit&&sunk.Hits==hits,"Repeated sunk cell applied new damage");
                    Require(pending.Stage==ShotStage.Feedback&&pending.FeedbackId==expected&&g.Phase==Phase.OpponentTurn,"Native repeated-sunk miss feedback missing");
                    Require(!g.CompleteMovie(pending,ShotStage.Travel)&&sunk.Hits==hits,"Duplicate repeated impact advanced");
                    g.TogglePause();Require(!g.CompleteMovie(pending,ShotStage.Feedback),"Paused repeated feedback completed");g.TogglePause();
                    Require(g.CompleteMovie(pending,ShotStage.Feedback)&&g.Pending==null&&g.Phase==Phase.PlayerTurn&&g.Turn==2,"Repeated feedback turn handoff");
                    covered=true;
                }
                Require(covered,"No reachable exhausted-search repeated-sunk fixture found");
            });
            check("Feedback subset, default opt-in and movie preference round trip",delegate {
                var board=new Board();board.Place(new Point(0,0),5,true,"Battleship",1);Ship ship=board.Ships[0];ship.Hits=1;Require(ClassicImpact.Select(0,new Shot{Hit=true},ship,14,0)==45&&ClassicImpact.Select(1,new Shot{Hit=true},ship,14,0)==137&&ClassicImpact.Select(2,new Shot(),null,14,0)==136,"Hit/miss level policy");Require(ClassicImpact.Select(3,new Shot{Hit=true,Sunk=true},ship,14,0)==47&&ClassicImpact.Select(3,new Shot{Hit=true,Repeated=true},ship,14,0)==0,"Sink/repeated occupied gate");
                var legacy=new Game(921,RuleProfile.Legacy,OpponentKind.Prototype,0,true);Require(!legacy.MovieTurns,"Prototype acquired movies");Require(!new Game(921).MovieTurns&&!DisplaySettings.Parse("sound=false\n").MovieTurns,"Existing default changed");
                var config=DisplaySettings.Parse("movies=true\nsound=false\ncustom=kept\n");Require(config.MovieTurns&&!config.Sound&&DisplaySettings.Parse(config.Serialize()).MovieTurns&&config.Serialize().Contains("custom=kept"),"Movie preference compatibility");
            });
            check("Debug turn-video preference defaults off and persists without changing other settings",delegate {
                Require(!DisplaySettings.Parse("movies=true\nsound=false\n").SkipTurnVideos,"Existing settings skipped videos");
                var config=DisplaySettings.Parse("debugskipturnvideos=true\nmovies=true\nhighmovies=true\nsound=false\ncustom=kept\n");
                var again=DisplaySettings.Parse(config.Serialize());Require(again.SkipTurnVideos&&again.MovieTurns&&again.HighMovies&&!again.Sound&&again.Serialize().Contains("custom=kept"),"Debug preference round trip");
                string path=Path.Combine(Path.GetTempPath(),"OpenBShip-debug-pref-"+Guid.NewGuid().ToString("N")+".txt");
                try{config.Save(path);Require(DisplaySettings.Load(path).SkipTurnVideos,"Debug preference file round trip");config.SkipTurnVideos=false;config.Save(path);Require(!DisplaySettings.Load(path).SkipTurnVideos,"Debug preference disable not persisted");}finally{if(File.Exists(path))File.Delete(path);}
                var invalid=DisplaySettings.Parse("debugskipturnvideos=invalid\n");Require(!invalid.SkipTurnVideos&&invalid.Problem.Length>0,"Invalid debug preference accepted");
            });
            check("Cinematic and instant histories preserve targets, weapons, damage and turns",delegate {
                for(int level=0;level<4;level++){var instant=new Game(925+level,RuleProfile.Recovered,OpponentKind.Original,level);var movie=new Game(925+level,RuleProfile.Recovered,OpponentKind.Original,level,true);instant.AutoPlace();movie.AutoPlace();instant.Start();movie.Start();
                    for(int turn=0;turn<15;turn++){Point target=new Point(turn/10,turn%10);Require(instant.Fire(target)&&movie.Fire(target),"Parallel player action");DrainMovies(movie);Require(instant.LastPlayerShot.Description==movie.LastPlayerShot.Description&&instant.LastPlayerFire.WeaponId==movie.LastPlayerFire.WeaponId&&instant.LastPlayerFire.MovieId==movie.LastPlayerFire.MovieId,"Player stream/damage changed");Require(instant.AdvanceOpponent()&&movie.AdvanceOpponent(),"Parallel opponent action");DrainMovies(movie);Require(instant.LastEnemyShot.Description==movie.LastEnemyShot.Description&&instant.LastEnemyFire.WeaponId==movie.LastEnemyFire.WeaponId&&instant.Phase==movie.Phase&&instant.Turn==movie.Turn,"Opponent history changed");}
                }
            });
            check("Cinematic terminal outcomes and stale cross-game callbacks",delegate {
                var g=new Game(930,RuleProfile.Recovered,OpponentKind.Original,3,true);g.AutoPlace();g.Start();g.Enemy=new Board();g.Enemy.Place(new Point(0,0),1,true,"Last",1);Require(g.Fire(new Point(0,0)),"Terminal reservation");var stale=g.Pending;Require(g.Phase!=Phase.Finished,"Victory before impact");DrainMovies(g);Require(g.Phase==Phase.Finished&&g.Enemy.AllSunk&&g.Pending==null&&!g.AdvanceOpponent(),"Terminal impact");
                var next=new Game(931,RuleProfile.Recovered,OpponentKind.Original,1,true);next.AutoPlace();next.Start();next.Fire(new Point(0,0));Require(!next.CompleteMovie(stale,ShotStage.Firing)&&next.Pending.Stage==ShotStage.Firing,"Cross-game callback accepted");
                g=new Game(932,RuleProfile.Recovered,OpponentKind.Prototype,1,true);g.AutoPlace();g.Start();g.Player=new Board();g.Player.Place(new Point(0,1),1,true,"Last",1);for(int x=0;x<10;x++)for(int y=0;y<10;y++)g.Player.Shots[x,y]=!(x==0&&y==1);g.Phase=Phase.OpponentTurn;Require(g.AdvanceOpponent()&&g.Player.Ships[0].Hits==0,"Opponent reservation");DrainMovies(g);Require(g.Phase==Phase.Finished&&g.Player.AllSunk,"Cinematic defeat");
            });
            checkEvidence("All 20 reachable firing clips and 1460 frames against original decoder",delegate {
                var reachable=new HashSet<string>();foreach(int type in new[]{7,1,2,12,4})foreach(bool ai in new[]{false,true})for(int roll=0;roll<50;roll++)for(int variant=0;variant<4;variant++)reachable.Add(ClassicWeapons.Clip(ClassicWeapons.Movie(type,ClassicWeapons.Weapon(type,ai,roll),variant)));
                Require(reachable.SetEquals(FiringClips.FiringNames)&&reachable.Count==20,"Reachable catalog differs");
                var movies=new Dictionary<string,PatMovie>();foreach(string name in FiringClips.FiringNames)movies.Add(name,PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAT"))));
                int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Firing-Frames-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');var f=movies[p[0]].Frames[int.Parse(p[1])];Require(f.Offset==int.Parse(p[2])&&f.PayloadBytes==int.Parse(p[3]),"Native firing span");using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(f.Pixels)).Replace("-","")==p[4],"Native firing pixels "+p[0]+"/"+p[1]);rows++;}
                int total=0;foreach(var movie in movies.Values)total+=movie.Frames.Count;Require(rows==1460&&rows==total,"Native firing frame total");
            });
            checkEvidence("Original SEQ one-cue-per-frame timing and close cleanup fixtures",delegate {
                var cues=new Dictionary<string,List<SoundCue>>();var counts=new Dictionary<string,int>();int total=0;
                foreach(string name in FiringClips.FiringNames){var list=ClipSound.Sequence(File.ReadAllText(Path.Combine(assets,"SFX",name,name+".SEQ")));cues.Add(name,list);counts.Add(name,PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAT"))).Frames.Count);total+=list.Count;}
                int starts=0,stops=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Sequence-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');int index=int.Parse(p[2]),frame=int.Parse(p[3]);Require(int.Parse(p[4])==index+1,"Dummy handle identity");if(p[1]=="Start"){Require(cues[p[0]][index].ActualStart==frame,"Original cue onset");starts++;}else {Require(p[1]=="Stop"&&frame==counts[p[0]],"Valid handle stopped during frame polling");stops++;}}
                Require(total==104&&starts==total&&stops==total,"Sequence fixture coverage");
                var tied=ClipSound.Sequence("ONE,0,1\nTWO,0,1\nTHREE,0,1\n*END*\n");Require(tied[0].ActualStart==0&&tied[1].ActualStart==1&&tied[2].ActualStart==2&&tied[2].End==1,"Tie spacing/end preservation");
            });
            check("Independent cue PCM sample conversion and malformed RIFF/SEQ bounds",delegate {
                byte[] b=CueWaveFixture();short[] samples=ClipSound.Pcm(b);Require(samples.Length==3&&samples[0]==-32768&&samples[1]==0&&samples[2]==32512,"Unsigned PCM conversion");
                foreach(int at in new[]{4,20,22,24,32,34,40}){b=CueWaveFixture();b[at]=99;Reject(delegate{ClipSound.Pcm(b);});}Reject(delegate{ClipSound.Pcm(new byte[1]);});
                foreach(string text in new[]{"A,0,1", "../A,0,1\n*END*", "A,1,0,2\n*END*", "A,-1,0\n*END*", "A,5,2\n*END*", "A,0,1\n*END*\nB,0,1", "*WAIT*"})Reject(delegate{ClipSound.Sequence(text);});
                var longList=new StringBuilder();for(int i=0;i<64;i++)longList.AppendLine("A,0,1");longList.AppendLine("*END*");Reject(delegate{ClipSound.Sequence(longList.ToString());});
            });
            checkEvidence("Finite overlapping cue mixing, preserved end fields, saturation and eight-voice pool",delegate {
                byte[] request=File.ReadAllBytes(Path.Combine(evidence,"Sound-CueRequest.bin"));Require(request[0x82]==0x6a&&request[0x83]==8&&request[0x84]==0x6a&&request[0x85]==0&&request[0xbb]==0x6a&&request[0xbc]==0,"Native pool range/finite Play argument");
                var cues=ClipSound.Sequence("A,0,1\nB,0,1\nA,0,1\n*END*");short[] a=new short[3*441],b=new short[2*441];for(int i=0;i<a.Length;i++)a[i]=1000;for(int i=0;i<b.Length;i++)b[i]=2000;
                var mixed=ClipSound.Mix(5,cues,delegate(string name){return name=="A"?a:b;});Require(mixed.Length==5*441&&mixed[0]==1000&&mixed[441]==3000&&mixed[882]==4000&&mixed[1323]==1000&&mixed[2204]==1000,"Finite sample overlap/end semantics");
                var records=new StringBuilder();for(int i=0;i<9;i++)records.AppendLine("S"+i+",0,0");records.AppendLine("NEW,12,0");records.AppendLine("*END*");var pool=ClipSound.Sequence(records.ToString());short[] longWave=new short[10*441];for(int i=0;i<longWave.Length;i++)longWave[i]=30000;
                var saturated=ClipSound.Mix(15,pool,delegate(string name){return longWave;});Require(pool[8].Slot==-1&&pool[9].Slot==0&&saturated[441]==32767,"Pool drop/reuse or saturation");
            });
            checkEvidence("All 71 original cue files decode and 20 finite soundtracks compose",delegate {
                int files=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Firing-Wave-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');var samples=ClipSound.Pcm(File.ReadAllBytes(Path.Combine(assets,"SFX",p[0],p[1])));Require(samples.Length==int.Parse(p[7]),"Original sample count "+p[0]+"/"+p[1]);files++;}Require(files==71,"Cue asset count");
                int drops=0;foreach(string name in FiringClips.FiringNames){int frames=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAT"))).Frames.Count;List<SoundCue> cues;short[] pcm=ClipSound.Load(assets,name,frames,out cues);Require(pcm.Length==frames*441,"Soundtrack finite duration");bool nonzero=false;foreach(short value in pcm)if(value!=0){nonzero=true;break;}Require(nonzero,"Empty soundtrack");foreach(var cue in cues)if(cue.Slot<0)drops++;}Require(drops==1,"Expected one eight-voice overflow in supplied library");
            });
            check("Device-clock timeline synchronization is monotonic and respects pause/completion",delegate {
                var timeline=new ClipTimeline(46);timeline.Play();timeline.Synchronize(480);Require(timeline.Frame==12,"Device-frame conversion");timeline.Synchronize(400);Require(timeline.Frame==12,"Device clock regressed video");timeline.Pause();timeline.Synchronize(800);Require(timeline.Frame==12,"Paused sync advanced");timeline.Play();timeline.Synchronize(1840);Require(timeline.Completed&&!timeline.Playing&&timeline.Frame==45,"Device finite completion");
            });
            checkEvidence("Original PAT movie against 46 original-decoder frame hashes and lengths",delegate {
                byte[] input=File.ReadAllBytes(Path.Combine(assets,"LWEBS","BATFIRE1.PAT"));
                var movie=PatMovie.Load(input);string[] rows=File.ReadAllLines(Path.Combine(evidence,"BATFIRE1-Frames-Oracle.csv"));
                Require(movie.Frames.Count==46&&rows.Length==47,"Original frame count");
                int[] counts=new int[13];
                for(int i=0;i<46;i++){
                    PatFrame f=movie.Frames[i];string[] p=rows[i+1].Split(',');
                    Require(int.Parse(p[0])==i&&int.Parse(p[1])==f.Offset&&int.Parse(p[2])==f.PayloadBytes&&int.Parse(p[3])==f.PayloadBytes+4,"Original frame spans");
                    using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(f.Pixels)).Replace("-","")==p[4],"Original pixels at frame "+i);
                    for(int op=0;op<13;op++)counts[op]+=f.Opcodes[op];
                }
                int[] expected={1626,11,682,958,0,6190,150,836,387,540,1129,1197,94};
                for(int i=0;i<13;i++)Require(counts[i]==expected[i],"Movie opcode distribution");
                foreach(int i in new[]{0,20,45})Require(Convert.ToBase64String(movie.Frames[i].Pixels)==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(evidence,"BATFIRE1-"+i.ToString("D2")+".idx"))),"Independent pixel fixture");
            });
            checkEvidence("All 13 PAT block operations and 256 quartet patterns against original code",delegate {
                var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(evidence,"PAT-AllOps-Input.pat")));
                Require(movie.Frames.Count==2&&Convert.ToBase64String(movie.Frames[1].Pixels)==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(evidence,"PAT-AllOps-Oracle.idx"))),"All-opcode pixel fixture");
                foreach(int count in movie.Frames[1].Opcodes)Require(count>0,"Unexercised opcode");
                byte[] fixture=File.ReadAllBytes(Path.Combine(evidence,"PAT-Patterns-Input.bin"));byte[] complete=new byte[fixture.Length+4];fixture.CopyTo(complete,0);BitConverter.GetBytes(uint.MaxValue).CopyTo(complete,fixture.Length);
                var patterns=PatMovie.Load(complete);int total=0;
                foreach(string line in File.ReadAllLines(Path.Combine(evidence,"PAT-Patterns-Oracle.csv"))){if(line.StartsWith("Code"))continue;string[] p=line.Split(',');int code=int.Parse(p[0]),at=(code/20*8)*160+(code%20)*8;for(int x=0;x<4;x++)Require(patterns.Frames[0].Pixels[at+x]==int.Parse(p[x+1]),"Quartet order/literal escape "+code);total++;}
                Require(total==256,"Pattern fixture size");
            });
            check("PAT length, truncation, terminal, opcode, variant and frame-count bounds",delegate {
                byte[] good=File.ReadAllBytes(Path.Combine(assets,"LWEBS","BATFIRE1.PAT"));
                Reject(delegate{PatMovie.Load(new byte[0]);});
                var b=(byte[])good.Clone();BitConverter.GetBytes(uint.MaxValue-1).CopyTo(b,0);Reject(delegate{PatMovie.Load(b);});
                foreach(int at in new[]{4,5,6,7,8}){b=(byte[])good.Clone();b[at]=at==8?(byte)52:(byte)0;Reject(delegate{PatMovie.Load(b);});}
                b=new byte[good.Length-4];Array.Copy(good,b,b.Length);Reject(delegate{PatMovie.Load(b);});
                b=(byte[])good.Clone();BitConverter.GetBytes(0).CopyTo(b,b.Length-4);Reject(delegate{PatMovie.Load(b);});
                b=new byte[good.Length+1];good.CopyTo(b,0);Reject(delegate{PatMovie.Load(b);});
                var many=new List<byte>();byte[] empty=new byte[308];BitConverter.GetBytes(304).CopyTo(empty,0);new byte[]{20,15,4,4}.CopyTo(empty,4);for(int i=8;i<308;i++)empty[i]=44;
                for(int i=0;i<1024;i++)many.AddRange(empty);many.AddRange(BitConverter.GetBytes(uint.MaxValue));Require(PatMovie.Load(many.ToArray()).Frames.Count==1024,"Frame-count upper boundary");many.InsertRange(0,empty);Reject(delegate{PatMovie.Load(many.ToArray());});
            });
            check("Both original movie palette branches preserve untouched entries",delegate {
                byte[] pal=File.ReadAllBytes(Path.Combine(assets,"LWEBS","BATFIRE1.PAL")),island=File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL"));
                var baseline=ClassicPalette.Gameplay(island);var original=(byte[])pal.Clone();
                foreach(bool shared in new[]{false,true}){
                    Color[] colors=PatMovie.ClipColors(pal,island,shared);int first=shared?20:10,last=shared?137:245;
                    for(int i=0;i<256;i++){
                        Color expected=i<first||i>last?baseline[i]:Color.FromArgb(pal[i*3]*4,pal[i*3+1]*4,pal[i*3+2]*4);
                        Require(colors[i].ToArgb()==expected.ToArgb(),"Clip palette boundary/RGB conversion "+i);
                    }
                }
                Require(Convert.ToBase64String(pal)==Convert.ToBase64String(original),"Source palette mutated");
                Reject(delegate{PatMovie.ClipColors(new byte[1],island,false);});pal[60]=64;Reject(delegate{PatMovie.ClipColors(pal,island,false);});
            });
            check("Clip timeline frame holds, completion, partitions, pause/step/reset and bounded catch-up",delegate {
                var a=new ClipTimeline(46);a.Play();Require(!a.Advance(39)&&a.Frame==0,"First frame hold");Require(a.Advance(1)&&a.Frame==1,"40 ms transition");
                var b=new ClipTimeline(46);b.Play();for(int i=0;i<46;i++){b.Advance(16);b.Advance(24);}Require(b.Frame==45&&b.Completed&&!b.Playing,"Finite completion/final-frame hold");
                a=new ClipTimeline(46);a.Play();for(int i=0;i<46;i++)a.Advance(40);Require(a.Frame==b.Frame&&a.Completed==b.Completed,"Frame partitions diverged");
                a.Play();Require(a.Frame==0&&!a.Completed,"Replay reset");a.Advance(10000);Require(a.Frame==5,"Catch-up bound");a.Pause();a.Advance(200);Require(a.Frame==5,"Pause advanced");a.Step(-1);Require(a.Frame==4&&!a.Playing,"Step");a.End();Require(a.Frame==45&&a.Completed,"Skip end");a.Reset();Require(a.Frame==0&&!a.Playing&&!a.Completed,"Reset");
                var single=new ClipTimeline(1);single.Play();single.Advance(40);Require(single.Completed&&single.Frame==0,"Single-frame completion");
            });
            checkEvidence("Palette patches and 65 fade outputs against original x86 code",delegate {
                string directory=Path.Combine(evidence,"Palette");
                byte[] input=File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL"));
                byte[] expected=File.ReadAllBytes(Path.Combine(directory,"Patched-Island.rgb"));
                byte[] patched=ClassicPalette.Patched(input);
                Require(Convert.ToBase64String(patched)==Convert.ToBase64String(expected),"Native palette patch bytes");
                for(int factor=0;factor<=64;factor++) {
                    byte[] faded=ClassicPalette.Fade(patched,factor);
                    byte[] native=File.ReadAllBytes(Path.Combine(directory,"Fade-"+factor.ToString("D2")+".rgb"));
                    Require(Convert.ToBase64String(faded)==Convert.ToBase64String(native),"Native fade arithmetic at "+factor);
                }
                Require(input[30]!=patched[30],"Source palette mutated or fixture patch absent");
                Reject(delegate{ClassicPalette.Patched(new byte[767]);});
                Reject(delegate{ClassicPalette.Fade(input,65);});
            });
            checkEvidence("Recovered gameplay RGB entries and deterministic protected endpoints",delegate {
                byte[] raw=File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL"));
                byte[] expected=File.ReadAllBytes(Path.Combine(evidence,"Palette","Patched-Island.rgb"));
                var colors=ClassicPalette.Gameplay(raw);
                for(int i=10;i<=245;i++)Require(colors[i]==Color.FromArgb(expected[i*3],expected[i*3+1],expected[i*3+2]),"Mutable native RGB entry "+i);
                Require(colors[0].ToArgb()==Color.Black.ToArgb()&&colors[255].ToArgb()==Color.White.ToArgb(),"Protected black/white");
                Require(colors[1]==Color.FromArgb(128,0,0)&&colors[249]==Color.FromArgb(255,0,0),"Captured deterministic endpoints");
                byte[] endpoints=File.ReadAllBytes(Path.Combine(evidence,"Palette","Observed-Reserved-Endpoints.rgb"));
                Require(endpoints.Length==60,"Endpoint observation fixture");int offset=0;
                for(int i=0;i<256;i++)if(i<10||i>=246) {
                    Require(colors[i]==Color.FromArgb(endpoints[offset],endpoints[offset+1],endpoints[offset+2]),"Deterministic endpoint "+i);offset+=3;
                }
                Require(colors[10].ToArgb()==Color.White.ToArgb(),"Native patched white");
            });
            checkEvidence("Classic damage resource, phase and bottom-aligned anchors",delegate {
                var pal=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette;
                var frames=Assets.Sph(Path.Combine(assets,"Graphics","SMOKE.SPH"),pal);
                Require(frames.Count==34&&frames[33]==null&&frames[0].Width==16&&frames[0].Height==47,"Original damage frame fixture");
                for(int i=0;i<33;i++)Require(frames[i]!=null&&frames[i].UnresolvedTail.Length==0,"Damage frame boundary "+i);
                Require(ClassicDamage.Frame(0,0)==0&&ClassicDamage.Frame(17,2)==0&&ClassicDamage.Frame(32,1)==7&&ClassicDamage.Frame(33,0)==0,"Native frame phase/modulus");
                Require(ClassicDamage.Anchor(new Point(2,4))==new Point(164,331),"Native damage anchor");
                Require(ClassicDamage.Bounds(new Point(164,331),new Size(12,37))==new Rectangle(158,294,12,37),"Native bottom-centered draw");
                byte[] draw=File.ReadAllBytes(Path.Combine(evidence,"Classic-DamageDraw.bin"));
                Require(BitConverter.ToInt32(draw,0x40DC90-0x40DB39+4)==33&&draw[0x40DCC1-0x40DB39+4]==8,"Native modulo and segment increment");
                byte[] binding=File.ReadAllBytes(Path.Combine(evidence,"Classic-DamageResource.bin"));
                Require(binding[0]==0x6a&&binding[1]==15,"Native SMOKE sprite bank");
            });
            check("Damage rendering responds to known hits and disappears on sinking",delegate {
                var palette=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette;
                using(var effects=new ClassicDamage(Path.Combine(assets,"Graphics"),palette))using(var bitmap=new Bitmap(640,512))using(var graphics=Graphics.FromImage(bitmap)) {
                    var board=new Board();board.Place(new Point(2,4),2,true,"Fixture",4);Ship ship=board.Ships[0];
                    effects.Draw(graphics,board,ship,0);
                    Require(AlphaCount(bitmap,new Rectangle(150,285,30,50))==0,"Effect before any hit");
                    board.Shoot(new Point(2,4));effects.Draw(graphics,board,ship,0);
                    Require(AlphaCount(bitmap,new Rectangle(158,294,12,37))>0,"Known hit effect absent");
                    board.Shoot(new Point(2,5));graphics.Clear(Color.Transparent);effects.Draw(graphics,board,ship,0);
                    Require(AlphaCount(bitmap,new Rectangle(150,285,60,65))==0,"Sunk ship still burns");
                }
            });
            check("Effect clock partitions, pause/still, reset and bounded catch-up",delegate {
                var a=new EffectClock();var b=new EffectClock();
                for(int i=0;i<10;i++)a.Advance(40,true);
                foreach(int ms in new[]{17,23,80,11,29,120,120})b.Advance(ms,true);
                Require(a.Tick==10&&b.Tick==10,"Frame partition dependence");
                a.Advance(9999,false);Require(a.Tick==10,"Paused/still animation advanced");
                a.Reset();Require(a.Tick==0,"Reset phase");
                a.Advance(1000,true);Require(a.Tick==5,"Catch-up was not capped at five steps");
                a.Reset();a.Advance(39,true);a.Advance(1000,false);a.Advance(1,true);Require(a.Tick==1,"Pause lost partial frame");
            });
            check("Legacy sound settings and atomic display preference round-trip",delegate {
                var old=DisplaySettings.Parse("sound=false");
                Require(!old.Sound&&!old.RecoveredColors&&old.Motion,"Legacy mute/default display");
                var settings=DisplaySettings.Parse("sound=false\r\ncustomConfirm=Enter\r\ncolors=recovered\r\nmotion=false\r\n");
                string path=Path.GetTempFileName();
                try {
                    settings.Save(path);var loaded=DisplaySettings.Load(path);
                    Require(!loaded.Sound&&loaded.RecoveredColors&&!loaded.Motion&&loaded.Serialize().Contains("customConfirm=Enter"),"Preference round trip or unknown key lost");
                    loaded.Motion=true;loaded.Save(path);Require(DisplaySettings.Load(path).Motion,"Atomic replacement");
                }finally{File.Delete(path);if(File.Exists(path+".new"))File.Delete(path+".new");}
                var malformed=DisplaySettings.Parse("sound=false\ncolors=unknown\nmotion=maybe\ninvalid line\n");
                Require(!malformed.Sound&&!malformed.RecoveredColors&&malformed.Motion&&malformed.Problem!="","Malformed preferences changed accepted mute/defaults");
            });
            checkEvidence("Managed targeting against 113 executions of original x86 selector/RNG",delegate {
                string[] rows=File.ReadAllLines(Path.Combine(evidence,"Original-Target-Cases.csv"));
                Require(rows.Length==114,"Original oracle fixture count");
                for(int i=1;i<rows.Length;i++) {
                    string[] f=rows[i].Replace("\"","").Split(',');
                    var raw=new int[10,10];string[] cells=f[6].Split(';');
                    Require(cells.Length==100,"Oracle grid dimensions");
                    for(int x=0;x<10;x++)for(int y=0;y<10;y++)raw[x,y]=int.Parse(cells[x*10+y]);
                    var random=new NativeRandom(uint.Parse(f[2]));
                    TargetDecision decision=OriginalOpponent.Choose(raw,int.Parse(f[1]),random);
                    string identity=f[0]+" level="+f[1]+" seed="+f[2];
                    Require(!decision.Stalled&&decision.Cell==new Point(int.Parse(f[3]),int.Parse(f[4])),"Target mismatch "+identity);
                    Require(random.State==uint.Parse(f[5]),"Random consumption mismatch "+identity);
                }
            });
            checkEvidence("Original targeting budgets, masking and non-mutating observations",delegate {
                int[] expected={10,50,100,200};
                byte[] code=File.ReadAllBytes(Path.Combine(evidence,"Original-TargetSelector.bin"));
                int[] entries={0x41CB11,0x41CB1B,0x41CB25,0x41CB2F};
                for(int i=0;i<4;i++)Require(OriginalOpponent.Budget(i)==expected[i]&&BitConverter.ToInt32(code,entries[i]-0x41C940+4)==expected[i],"Native search budget");
                var raw=new int[10,10];raw[1,1]=2;raw[2,2]=4;raw[3,3]=1;raw[4,4]=8;
                var masked=OriginalOpponent.Mask(raw);
                Require(masked[1,1]==0&&masked[2,2]==0&&masked[3,3]==1&&masked[4,4]==8,"Native observation codes");
                Require(raw[1,1]==2&&raw[2,2]==4,"Source state mutated");
                Reject(delegate {OriginalOpponent.Mask(new int[2,2]);});
                bool rejected=false;try{OriginalOpponent.Budget(4);}catch(ArgumentOutOfRangeException){rejected=true;}
                Require(rejected,"Unsupported original setting accepted");
            });
            check("Opponent observation hides unshot ships and preserves visible sunk hits",delegate {
                var a=new Board();var b=new Board();
                a.Place(new Point(2,2),2,true,"A");b.Place(new Point(7,7),2,true,"B");
                a.Shoot(new Point(0,0));b.Shoot(new Point(0,0));
                int[,] seenA=a.ObservedShots(),seenB=b.ObservedShots();
                for(int x=0;x<10;x++)for(int y=0;y<10;y++)Require(seenA[x,y]==seenB[x,y],"Hidden layout leaked into observation");
                var r1=new NativeRandom(91);var r2=new NativeRandom(91);
                Require(OriginalOpponent.Choose(seenA,2,r1).Cell==OriginalOpponent.Choose(seenB,2,r2).Cell&&r1.State==r2.State,"Hidden layout affected targeting");
                a.Shoot(new Point(2,2));a.Shoot(new Point(2,3));seenA=a.ObservedShots();
                Require(seenA[2,2]==8&&seenA[2,3]==8&&seenA[0,0]==1,"Visible hit/miss state");
            });
            check("Original exhausted search can repeat without damage and still ends its turn",delegate {
                bool covered=false;
                for(int seed=0;seed<64&&!covered;seed++) {
                    var g=new Game(seed);g.AutoPlace();g.Start();
                    for(int x=0;x<10;x++)for(int y=0;y<10;y++)if(g.Player.ShipAt(new Point(x,y))==null)g.Player.Shoot(new Point(x,y));
                    var probe=new NativeRandom(unchecked((uint)(seed^0x17b5)));
                    var decision=OriginalOpponent.Choose(g.Player.ObservedShots(),0,probe);
                    if(!g.Player.Shots[decision.Cell.X,decision.Cell.Y])continue;
                    g.Phase=Phase.OpponentTurn;
                    Require(g.AdvanceOpponent()&&g.LastEnemyShot.Repeated&&!g.LastEnemyShot.Hit&&g.Phase==Phase.PlayerTurn&&g.Turn==2,"Repeat turn handoff");
                    foreach(var ship in g.Player.Ships)Require(ship.Hits==0,"Repeat dealt damage");
                    Require(g.Message.Contains("repeat / no new damage"),"Repeat was hidden from the player");
                    covered=true;
                }
                Require(covered,"No native repeat case exercised");
            });
            check("Unsupported isolated-hit trap fails visibly instead of hanging",delegate {
                var raw=new int[10,10];raw[5,5]=8;raw[5,4]=1;raw[5,6]=1;
                var random=new NativeRandom(1);var decision=OriginalOpponent.Choose(raw,0,random);
                Require(decision.Stalled&&random.Calls==0,"Native trap was silently replaced");
                var g=new Game(90);g.AutoPlace();g.Start();g.Player=new Board();
                g.Player.Place(new Point(5,5),1,true,"Incompatible fixture");g.Player.Place(new Point(9,9),1,true,"Survivor");
                g.Player.Shoot(new Point(5,5));g.Player.Shoot(new Point(5,4));g.Player.Shoot(new Point(5,6));g.Phase=Phase.OpponentTurn;
                Require(!g.AdvanceOpponent()&&g.Paused&&g.Message.Contains("F2")&&g.LastEnemyShot==null,"Stall did not pause/report clearly");
            });
            checkEvidence("Native camera constants and fleet origins from original code bytes",delegate {
                byte[] camera=File.ReadAllBytes(Path.Combine(evidence,"Classic-Camera.bin"));
                byte[] origins=File.ReadAllBytes(Path.Combine(evidence,"Classic-BoardOrigins.bin"));
                Require(camera[0]==0xc7&&camera[30]==0xb8,"Original camera instruction fixture");
                Require(ClassicGeometry.CameraX==BitConverter.ToInt32(camera,6)&&ClassicGeometry.CameraY==BitConverter.ToInt32(camera,16),"Camera origin constants");
                Require(ClassicGeometry.ScaleY==BitConverter.ToInt32(camera,26)&&ClassicGeometry.ScaleX==BitConverter.ToInt32(camera,31),"Projection scale constants");
                Require(origins[0x36]==0xbf&&BitConverter.ToInt32(origins,0x37)==11,"Opponent board X origin");
                byte[] entry=File.ReadAllBytes(Path.Combine(evidence,"Classic-ViewDispatch.bin"));
                byte[] track=File.ReadAllBytes(Path.Combine(evidence,"Classic-CameraDispatch.bin"));
                Require(BitConverter.ToUInt32(entry,7*4)==0x408A30&&BitConverter.ToUInt32(track,7*4)==0x408D11,"Classic mode 7 dispatch");
            });
            check("Independent native board vertices, picking and boundary truncation",delegate {
                Point[] own={new Point(28,305),new Point(223,175),new Point(417,305),new Point(223,435)};
                Point[] other={new Point(242,162),new Point(437,32),new Point(632,162),new Point(437,292)};
                Point[] grid={new Point(0,0),new Point(10,0),new Point(10,10),new Point(0,10)};
                for(int i=0;i<4;i++) {
                    Require(ClassicGeometry.Project(false,grid[i].X,grid[i].Y)==own[i],"Native own vertex "+i);
                    Require(ClassicGeometry.Project(true,grid[i].X,grid[i].Y)==other[i],"Native enemy vertex "+i);
                }
                Require(ClassicGeometry.Pick(false,new PointF(164,331))==new Point(2,4),"Original X/Y axis orientation");
                Require(ClassicGeometry.Pick(false,new PointF(27,305))==new Point(0,0),"Native truncation toward zero near origin");
                Require(ClassicGeometry.Pick(false,new PointF(5,5))==null&&ClassicGeometry.Pick(true,new PointF(164,331))==null,"Outside/wrong board");
                Require(ClassicGeometry.Pick(false,new PointF(float.NaN,0))==null&&ClassicGeometry.Pick(false,new PointF(float.PositiveInfinity,0))==null,"Invalid pixel coordinates");
            });
            check("Original odd/even ship anchor and percent-centering cases",delegate {
                Require(ClassicGeometry.ShipPercent==79,"Native scale truncation");
                Require(ClassicGeometry.ShipAnchor(false,new Point(0,0),5)==new Point(85,331),"Carrier anchor");
                Require(ClassicGeometry.ShipAnchor(true,new Point(0,0),5)==new Point(299,188),"Opponent origin");
                Require(ClassicGeometry.ShipAnchor(false,new Point(4,3),5)==new Point(221,318),"Battleship anchor");
                Require(ClassicGeometry.ShipAnchor(false,new Point(2,1),4)==new Point(134,312),"Even submarine anchor");
                Require(ClassicGeometry.ShipAnchor(false,new Point(9,8),2)==new Point(387,299),"Even destroyer anchor");
                Require(ClassicGeometry.ShipAnchor(false,new Point(1,7),3)==new Point(221,396),"Odd frigate anchor");
                Require(ClassicShips.Bounds(new Point(85,331),new Size(106,75))==new Rectangle(32,294,106,75),"Native centered scaled bounds");
            });
            check("Independent scaled SPH scanline, transparency and 16.16 fixture",delegate {
                var fixture=new IndexedImage {Width=8,Height=4,Pixels=new byte[32],Visible=new bool[32],Palette=new Color[256]};
                byte[] row={0,22,23,24,25};
                for(int i=0;i<5;i++){fixture.Pixels[3+i]=row[i];fixture.Visible[3+i]=true;}
                for(int i=0;i<8;i++){fixture.Pixels[8+i]=(byte)(i+1);fixture.Visible[8+i]=true;}
                for(int i=0;i<3;i++){fixture.Pixels[17+i]=(byte)(11+i);fixture.Visible[17+i]=true;}
                var scaled=ClassicShips.Scale(fixture,50);
                Require(scaled.Width==4&&scaled.Height==2&&!scaled.Visible[1]&&scaled.Visible[2]&&scaled.Pixels[2]==23,"Scaled literal start/zero transparency");
                Require(scaled.Visible[4]&&scaled.Pixels[4]==11&&!scaled.Visible[5]&&!scaled.Visible[6],"Scaled source row/run length");
                scaled=ClassicShips.Scale(fixture,79);
                Require(scaled.Width==6&&scaled.Height==3&&scaled.Pixels[9]==4&&scaled.Pixels[10]==6,"16.16 step truncation versus exact-ratio sampling");
                fixture.Visible[1]=true;
                Reject(delegate {ClassicShips.Scale(fixture,50);});
                Reject(delegate {ClassicShips.Scale(fixture,0);});
                Reject(delegate {ClassicShips.Scale(fixture,1);});
            });
            checkEvidence("Native ship dispatch fixtures and five packaged scaled ship frames",delegate {
                var background=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));
                var lines=File.ReadAllLines(Path.Combine(evidence,"Native-Ship-Dispatch.csv"));
                Require(lines.Length==6,"Original dispatch fixture count");
                using(var ships=new ClassicShips(Path.Combine(assets,"Graphics"),background.Palette)) {
                    for(int i=1;i<lines.Length;i++) {
                        // Extractor emits simple quoted fields, without embedded CSV delimiters.
                        string[] fields=lines[i].Replace("\"","").Split(',');
                        int type=int.Parse(fields[0]),frame=int.Parse(fields[5]);
                        string resource=fields[4];
                        Require(ClassicShips.Resource(type)==resource&&ClassicShips.Frame(type)==frame,"Resource/frame dispatch "+type);
                        var image=ships.Image(type);
                        Require(image.Width==int.Parse(fields[9])&&image.Height==int.Parse(fields[10]),"Native scaled dimensions "+type);
                        byte[] handler=File.ReadAllBytes(Path.Combine(evidence,"Ship-Handler-"+type+".bin"));
                        Require(handler[4]==0x8b&&handler[5]==0x15&&BitConverter.ToUInt32(handler,6)==Convert.ToUInt32(fields[2],16),"Original bank pointer fixture");
                        bool visible=false,transparent=false;
                        for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A==0)transparent=true;else visible=true;
                        Require(visible&&transparent,"Ship transparency "+type);
                    }
                }
            });
            check("Ship visibility hides opponent layout and removes sunk art",delegate {
                var b=new Board();
                b.Place(new Point(2,2),2,true,"Fixture",4);
                var ship=b.Ships[0];
                Require(BoardView.RevealsShip(false,Phase.PlayerTurn,ship)&&!BoardView.RevealsShip(true,Phase.PlayerTurn,ship),"Hidden opponent ship leak");
                b.Shoot(new Point(2,2));
                Require(BoardView.RevealsShip(false,Phase.PlayerTurn,ship),"Partial hit hid live ship");
                Require(BoardView.RevealsShip(true,Phase.Finished,ship),"Post-battle surviving opponent reveal");
                b.Shoot(new Point(2,3));
                Require(!BoardView.RevealsShip(false,Phase.PlayerTurn,ship)&&!BoardView.RevealsShip(true,Phase.Finished,ship),"Sunk sprite retained");
            });
            checkEvidence("Recovered fleet against original initializer/template/name bytes",delegate {
                byte[] types=File.ReadAllBytes(Path.Combine(evidence,"Classic-TypeIds.bin"));
                byte[] templates=File.ReadAllBytes(Path.Combine(evidence,"Classic-Templates.bin"));
                byte[] names=File.ReadAllBytes(Path.Combine(evidence,"Classic-Names.bin"));
                byte[] init=File.ReadAllBytes(Path.Combine(evidence,"Classic-Initializer.bin"));
                int[] pushes={0x42393B,0x42399F,0x4239C2,0x4239E5,0x423A09};
                int[] calls={0x423988,0x4239AB,0x4239CE,0x4239F1,0x423A15};
                Require(types.Length==5&&templates.Length==1340&&names.Length==130,"Fixture sizes");
                int total=0;
                for(int i=0;i<5;i++) {
                    ShipSpec spec=RuleProfile.Recovered.Fleet[i];
                    string original=Encoding.ASCII.GetString(names,i*26,26).Split('\0')[0];
                    int at=pushes[i]-0x4238D0,call=calls[i]-0x4238D0;
                    Require(init[at]==0x6a&&init[at+1]==types[i],"Initializer type argument");
                    Require(init[call]==0xe8&&calls[i]+5+BitConverter.ToInt32(init,call+1)==0x42CEF0,"CreateShip target");
                    Require(spec.SourceType==types[i]&&spec.Name==original,"Ship identity "+i);
                    Require(spec.Length==BitConverter.ToUInt16(templates,i*268+14)&&spec.Length==BitConverter.ToUInt16(templates,i*268+16),"Length and initial hits "+i);
                    total+=spec.Length;
                }
                Require(total==19,"Recovered occupied cell count");
            });
            checkEvidence("Recovered weapon names, weighted lists and dispatch against native data",delegate {
                byte[] lists=File.ReadAllBytes(Path.Combine(evidence,"Classic-WeaponLists.bin"));
                byte[] names=File.ReadAllBytes(Path.Combine(evidence,"Weapon-Names.bin"));
                byte[] movieNames=File.ReadAllBytes(Path.Combine(evidence,"Movie-Names.bin"));
                foreach(int type in new[]{7,1,2,12,4})foreach(bool ai in new[]{false,true}) {
                    int start=ai?0:type==12?32:type==7?48:0,length=0;
                    while(BitConverter.ToInt32(lists,start+length*4)!=0)length++;
                    Require(length>0&&length<=7,"Original list sentinel");
                    for(int input=0;input<=32767;input++)
                        Require(ClassicWeapons.Weapon(type,ai,input)==BitConverter.ToInt32(lists,start+((input%50)%length)*4),"List choice/bias");
                }
                foreach(int weapon in new[]{14,15,16,17,18,20,24,25,26,29,30}) {
                    Require(ClassicWeapons.Name(weapon)==Encoding.ASCII.GetString(names,(weapon-14)*48,48).Split('\0')[0],"Original weapon name");
                    foreach(int type in new[]{7,1,2,12,4})for(int input=0;input<4;input++) {
                        int id=ClassicWeapons.Movie(type,weapon,input);
                        Require(ClassicWeapons.Clip(id)==Encoding.ASCII.GetString(movieNames,id*9,9).Split('\0')[0],"Original movie lookup");
                    }
                }
                bool rejected=false;try{ClassicWeapons.Movie(1,19,0);}catch(NotSupportedException){rejected=true;}
                Require(rejected,"Unsupported dispatch was silently accepted");
            });
            checkEvidence("Original-code weapon selection and movie dispatch fixtures (two numerical grids)",delegate {
                int rows=0;
                foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Weapons-Oracle.csv"))) {
                    if(line.StartsWith("ShipType"))continue;string[] p=line.Split(',');
                    Require(p.Length==5&&ClassicWeapons.Weapon(int.Parse(p[0]),p[1]=="1",int.Parse(p[2]))==int.Parse(p[3])&&p[4]=="1","Native list output/roll count");rows++;
                }
                Require(rows==500,"Selection fixture size");rows=0;
                foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Firing-Movies-Oracle.csv"))) {
                    if(line.StartsWith("ShipType"))continue;string[] p=line.Split(',');
                    Require(p.Length==6&&ClassicWeapons.Movie(int.Parse(p[0]),int.Parse(p[1]),int.Parse(p[2]))==int.Parse(p[3])&&p[4]=="1"&&p[5]=="1","Native movie output/roll/movie count");rows++;
                }
                Require(rows==220,"Movie fixture size");
            });
            check("Firing ship selection gates and initial slot",delegate {
                var g=new Game(910);
                Require(!g.SelectShooter(0)&&!g.SelectShooter(new Point(0,0)),"Selection before battle");
                g.AutoPlace();g.Start();
                Require(g.PlayerShooter==1&&g.EnemyShooter==1,"Native initial firing slot");
                Require(g.SelectShooter(g.Player.Ships[3].Cells[2])&&g.PlayerShooter==3,"Own ship segment selection");
                Require(g.Turn==1&&g.Events.Count==2&&g.LastPlayerShot==null,"Selection consumed a turn");
                Require(!g.SelectShooter(-1)&&!g.SelectShooter(5)&&!g.SelectShooter(new Point(-1,-1)),"Invalid selection");
                g.TogglePause();Require(!g.SelectShooter(0)&&g.PlayerShooter==3,"Paused selection");g.TogglePause();
                g.Fire(new Point(0,0));Require(!g.SelectShooter(0)&&g.PlayerShooter==3,"Opponent-phase selection");
                g.Phase=Phase.Finished;Require(!g.SelectShooter(0),"Terminal selection");
                var legacy=new Game(911,RuleProfile.Legacy);legacy.AutoPlace();legacy.Start();
                Require(legacy.PlayerShooter==-1&&!legacy.SelectShooter(0),"Legacy selection changed");
            });
            check("Selected ship sinking switches to the first surviving roster slot",delegate {
                var g=new Game(912,RuleProfile.Recovered,OpponentKind.Prototype,0);g.AutoPlace();g.Start();
                g.Player=new Board();g.Player.Place(new Point(9,9),1,true,"Survivor",7);g.Player.Place(new Point(0,1),1,true,"Selected",1);
                for(int x=0;x<10;x++)for(int y=0;y<10;y++)g.Player.Shots[x,y]=!(x==0&&y==1);
                g.Phase=Phase.OpponentTurn;
                Require(g.AdvanceOpponent()&&g.LastEnemyShot.Sunk&&g.PlayerShooter==0,"Actual opponent sink did not replace selected ship");
                Require(!g.SelectShooter(1)&&g.SelectShooter(0),"Sunk ship selected");
                var active=g.Enemy.Ships[1];
                foreach(Point cell in active.Cells) {g.Phase=Phase.PlayerTurn;Require(g.Fire(cell),"Sink scenario player shot");}
                Require(active.Sunk&&g.EnemyShooter==0,"Enemy selected ship fallback");
                var survivor=g.Enemy.Ships[2];
                foreach(Point cell in survivor.Cells) {g.Phase=Phase.PlayerTurn;Require(g.Fire(cell),"Other sink scenario");}
                Require(g.EnemyShooter==0,"Unselected ship sinking changed selection");
            });
            check("Weapon presentation consumes two rolls only for valid actions",delegate {
                var random=new NativeRandom(731);var expected=new NativeRandom(731);
                var ship=new Ship {Name="Fixture",SourceType=12};ship.Cells.Add(new Point(0,0));
                int weapon=ClassicWeapons.Weapon(12,false,expected.Next()),movie=ClassicWeapons.Movie(12,weapon,expected.Next());
                var presentation=ClassicWeapons.Choose(ship,false,random);
                Require(presentation.WeaponId==weapon&&presentation.MovieId==movie&&presentation.RandomCalls==2&&random.State==expected.State,"Presentation RNG consumption");
                var g=new Game(913);g.AutoPlace();g.Start();g.SelectShooter(0);
                g.TogglePause();Require(!g.Fire(new Point(0,0))&&g.LastPlayerFire==null,"Paused presentation");g.TogglePause();
                Require(!g.Fire(new Point(-1,0))&&g.LastPlayerFire==null,"Invalid presentation");
                g.Fire(new Point(0,0));var previous=g.LastPlayerFire;
                Require(previous.Shooter=="Super Carrier"&&(previous.WeaponId==29||previous.WeaponId==30),"Selected carrier did not supply its list");
                g.AdvanceOpponent();Require(!g.Fire(new Point(0,0))&&g.LastPlayerFire==previous,"Repeat changed firing presentation");
            });
            check("Different ship/weapon choices retain the accepted target and damage history",delegate {
                var a=new Game(914);var b=new Game(914);a.AutoPlace();b.AutoPlace();a.Start();b.Start();
                int different=0;
                for(int turn=0;turn<15;turn++) {
                    for(int i=0;i<5;i++)if(!a.Player.Ships[i].Sunk){a.SelectShooter(i);break;}
                    for(int i=4;i>=0;i--)if(!b.Player.Ships[i].Sunk){b.SelectShooter(i);break;}
                    var target=new Point(turn/10,turn%10);Require(a.Fire(target)&&b.Fire(target),"Parallel shot");
                    Require(a.LastPlayerShot.Description==b.LastPlayerShot.Description,"Weapon altered cell damage");
                    if(a.LastPlayerFire.WeaponId!=b.LastPlayerFire.WeaponId)different++;
                    Require(a.AdvanceOpponent()&&b.AdvanceOpponent(),"Parallel opponent turn");
                    Require(a.LastEnemyShot.Description==b.LastEnemyShot.Description&&a.LastOpponentDecision.RandomCalls==b.LastOpponentDecision.RandomCalls,"Presentation changed target history");
                }
                Require(different>0,"Comparison did not exercise different weapons");
            });
            check("Recovered manual fixed-axis placement, touching and edge cases",delegate {
                var g=new Game(801);
                Require(!g.Start()&&!g.Rotate(),"Incomplete start or rotation accepted");
                g.Vertical=false; // Even stale modern orientation cannot override the recovered rule.
                Require(!g.Place(new Point(9,9))&&!g.Place(new Point(0,0))&&g.Player.Ships.Count==0,"Centered ship crossed edge");
                Require(g.Place(new Point(9,7)),"Boundary placement");
                Require(g.Player.Ships[0].Cells[4]==new Point(9,9),"Fixed Y axis");
                Require(!g.Place(new Point(9,7)),"Overlap accepted");
                Require(g.Place(new Point(8,7)),"Adjacent parallel ship rejected");
                Require(g.Place(new Point(9,1))&&g.Place(new Point(8,2))&&g.Place(new Point(7,9)),"Remaining fleet");
                Require(g.NextLength==0&&g.Start(),"Completed deployment");
            });
            check("v0.1 preset retains rotating 17-cell fleet",delegate {
                var g=new Game(802,RuleProfile.Legacy);
                int total=0;
                foreach(var spec in g.Rules.Fleet)total+=spec.Length;
                Require(total==17&&!g.PlacementVertical,"Legacy fleet/default axis");
                Require(g.Place(new Point(0,0))&&g.Player.Ships[0].Cells[4]==new Point(4,0),"Horizontal placement");
                Require(g.Rotate()&&g.PlacementVertical&&g.Place(new Point(0,1)),"Rotation/vertical placement");
                g.ClearPlacement();
                Require(g.Player.Ships.Count==0,"Clear placement");
                g.AutoPlace();
                Require(g.Start()&&!g.Rotate(),"Battle rotation gate");
            });
            check("Fleet generation and roster order across both presets (128 seeds each)",delegate {
                foreach(var profile in new[]{RuleProfile.Recovered,RuleProfile.Legacy})for(int seed=0;seed<128;seed++) {
                    var g=new Game(seed,profile);
                    g.AutoPlace();
                    Require(g.Start(),"Generated deployment could not start");
                    foreach(var b in new[]{g.Player,g.Enemy}) {
                        Require(b.Ships.Count==5,"Fleet count");
                        int occupied=0;
                        for(int i=0;i<5;i++) {
                            Ship s=b.Ships[i]; ShipSpec spec=profile.Fleet[i];
                            Require(s.Name==spec.Name&&s.SourceType==spec.SourceType&&s.Cells.Count==spec.Length&&s.Hits==0,"Roster identity/order");
                            for(int cell=0;cell<s.Cells.Count;cell++) {
                                Point p=s.Cells[cell];
                                Require(Board.Inside(p)&&b.ShipAt(p)==s&&!b.Shots[p.X,p.Y],"Cell indexing");
                                if(profile.FixedY)Require(p==new Point(s.Cells[0].X,s.Cells[0].Y+cell),"Recovered axis");
                            }
                        }
                        for(int x=0;x<10;x++)for(int y=0;y<10;y++)if(b.ShipAt(new Point(x,y))!=null)occupied++;
                        Require(occupied==(profile.FixedY?19:17),"Overlapping or missing cells");
                    }
                }
            });
            checkEvidence("Original EO_CLASS icons and native roster coordinate fixture",delegate {
                var pal=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette;
                var icons=Assets.Sph(Path.Combine(assets,"Graphics","EO_CLASS.SPH"),pal);
                Require(icons.Count==15&&icons[14]==null,"Original frame count");
                byte[] xy=File.ReadAllBytes(Path.Combine(evidence,"Classic-RosterXY.bin"));
                Require(xy.Length==64,"Coordinate table size");
                foreach(bool enemy in new[]{false,true})for(int i=0;i<5;i++) {
                    int offset=enemy?32:0;
                    Point p=BoardView.RosterPosition(enemy,i);
                    Require(p==new Point(BitConverter.ToInt16(xy,offset+i*2),BitConverter.ToInt16(xy,offset+16+i*2)),"Native roster coordinate");
                    var f=icons[(enemy?9:4)+i];
                    Require(f.Width==88&&f.Height==30&&f.OriginX==1&&f.OriginY==1,"Status frame geometry");
                }
            });
            check("Hit/miss/sink handoff, repeated-shot gate and destroyed roster state",delegate {
                var g=new Game(803);
                g.AutoPlace(); g.Start();
                g.Enemy=new Board();
                g.Enemy.Place(new Point(0,0),2,true,"Fixture");
                g.Enemy.Place(new Point(9,0),1,true,"Survivor");
                Require(BoardView.RosterFrame(g.Enemy,0)==4,"Live icon");
                Require(g.Fire(new Point(0,0))&&g.LastPlayerShot.Hit&&g.Phase==Phase.OpponentTurn,"Hit retains turn");
                Require(g.AdvanceOpponent(),"Opponent return");
                int events=g.Events.Count;
                Require(!g.Fire(new Point(0,0))&&g.Phase==Phase.PlayerTurn&&g.Enemy.Ships[0].Hits==1&&g.Events.Count==events,"Repeat consumed action");
                Require(g.Fire(new Point(9,9))&&!g.LastPlayerShot.Hit&&g.Phase==Phase.OpponentTurn&&g.AdvanceOpponent(),"Miss handoff");
                Require(g.Fire(new Point(0,1))&&g.LastPlayerShot.Sunk&&g.Phase==Phase.OpponentTurn,"Nonfinal sink handoff");
                Require(BoardView.RosterFrame(g.Enemy,0)==9&&BoardView.RosterFrame(g.Enemy,1)==5,"Destroyed/live icon selection");
            });
            check("Placement overlap, bounds and adjacency",delegate {
                var b=new Board();
                Require(b.Place(new Point(0,0),5,false,"A"),"First ship");
                Require(!b.Place(new Point(0,0),3,true,"B"),"Overlap");
                Require(!b.Place(new Point(9,9),2,false,"B"),"Edge");
                Require(b.Place(new Point(0,1),4,false,"B"),"Adjacency");
            });
            check("Hit, miss, repeat rejection and sink",delegate {
                var b=new Board();
                b.Place(new Point(1,1),2,true,"Fixture");
                Require(!b.Shoot(new Point(9,9)).Hit,"Miss");
                Require(b.Shoot(new Point(1,1)).Hit&&!b.AllSunk,"Hit");
                Require(b.Shoot(new Point(1,1))==null&&b.Ships[0].Hits==1,"Repeat");
                Require(b.Shoot(new Point(1,2)).Sunk&&b.AllSunk,"Sink");
            });
            check("Turn handoff and pause gates",delegate {
                var g=new Game(123);
                g.AutoPlace();
                Require(g.Start(),"Start");
                g.TogglePause();
                Require(!g.Fire(new Point(0,0)),"Paused firing");
                g.TogglePause();
                Require(g.Fire(new Point(0,0))&&!g.Fire(new Point(1,0)),"One player action");
                g.TogglePause();
                Require(!g.AdvanceOpponent(),"Paused opponent");
                g.TogglePause();
                Require(g.AdvanceOpponent()&&g.Phase==Phase.PlayerTurn&&g.Turn==2,"Turn return");
            });
            check("Victory, defeat and terminal input gates",delegate {
                var g=new Game(124);
                g.AutoPlace();
                g.Start();
                g.Enemy=new Board();
                g.Enemy.Place(new Point(0,0),1,false,"Fixture");
                Require(g.Fire(new Point(0,0))&&g.Phase==Phase.Finished&&!g.AdvanceOpponent()&&!g.Fire(new Point(1,0)),"Victory gate");
                g=new Game(125);
                g.AutoPlace();
                g.Start();
                g.Player=new Board();
                g.Player.Place(new Point(0,1),2,true,"Fixture");
                g.Player.Shoot(new Point(0,2));
                for(int x=0; x<10; x++)for(int y=0; y<10; y++)g.Player.Shots[x,y]=!(x==0&&y==1);
                g.Phase=Phase.OpponentTurn;
                Require(g.AdvanceOpponent()&&g.Phase==Phase.Finished&&!g.Fire(new Point(0,0)),"Defeat gate");
            });
            check("Complete deterministic battles and reset in both presets",delegate {
                foreach(var profile in new[]{RuleProfile.Recovered,RuleProfile.Legacy}) {
                var g=new Game(126,profile);
                g.AutoPlace();
                g.Start();
                int shots=0;
                while(g.Phase!=Phase.Finished&&shots<100) {
                    bool fired=false;
                    for(int y=0; y<10&&!fired; y++)for(int x=0; x<10&&!fired; x++)if(!g.Enemy.Shots[x,y]) {
                        Require(g.Fire(new Point(x,y)),"Player action");
                        shots++;
                        fired=true;
                    }
                    if(g.Phase==Phase.OpponentTurn)Require(g.AdvanceOpponent(),"Opponent action");
                }
                Require(g.Phase==Phase.Finished&&shots<=100,"Battle did not terminate");
                g=new Game(126,profile);
                Require(g.Phase==Phase.Placement&&g.Player.Ships.Count==0&&!g.Paused&&g.Turn==1,"Reset state");
                }
            });
            check("Complete battles across four original settings and prototype pursuit",delegate {
                for(int mode=0;mode<5;mode++)for(int seed=810;seed<813;seed++) {
                    var g=new Game(seed,RuleProfile.Recovered,mode==4?OpponentKind.Prototype:OpponentKind.Original,mode==4?0:mode);
                    g.AutoPlace();Require(g.Start(),"Start");int shots=0;
                    while(g.Phase!=Phase.Finished&&shots<100) {
                        bool fired=false;
                        for(int y=0;y<10&&!fired;y++)for(int x=0;x<10&&!fired;x++)if(!g.Enemy.Shots[x,y]) {
                            Require(g.Fire(new Point(x,y)),"Player shot");shots++;fired=true;
                        }
                        if(g.Phase==Phase.OpponentTurn)Require(g.AdvanceOpponent()&&!g.Paused,"Opponent stalled in valid fixed-axis battle");
                    }
                    Require(g.Phase==Phase.Finished,"Battle failed to terminate");
                }
            });
            check("Native and v0.1 board picking across 400 cell centers",delegate {
                foreach(bool legacy in new[]{false,true})foreach(bool enemy in new[] {
                    false,true
                })for(int y=0; y<10; y++)for(int x=0; x<10; x++) {
                    Point? hit=BoardView.HitCell(enemy,BoardView.Project(enemy,(x+.5f)/10,(y+.5f)/10,legacy),legacy);
                    Require(hit.HasValue&&hit.Value==new Point(x,y),"Picking geometry");
                }
            });
            check("Board anchors against independent original border pixels",delegate {
                var image=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));
                foreach(bool enemy in new[] {
                    false,true
                })foreach(Point uv in new[] {
                    new Point(0,0),new Point(1,0),new Point(1,1),new Point(0,1)
                }) {
                    PointF anchor=BoardView.Project(enemy,uv.X,uv.Y);
                    bool found=false;
                    for(int y=(int)anchor.Y-3; y<=(int)anchor.Y+3; y++)for(int x=(int)anchor.X-3; x<=(int)anchor.X+3; x++) {
                        if(x<0||y<0||x>=image.Width||y>=image.Height)continue;
                        Color c=image.Palette[image.Pixels[y*image.Width+x]];
                        if(enemy?(c.R>200&&c.G<55&&c.B<55):(c.B>220&&c.R<30&&c.G>80&&c.G<200))found=true;
                    }
                    Require(found,"Anchor does not meet the original board border");
                }
            });
            check("Battle restart restores manual deployment and all isolated histories",delegate {
                foreach(var profile in new[]{RuleProfile.Recovered,RuleProfile.Legacy})foreach(bool cinematic in new[]{false,true}) {
                    var g=new Game(731,profile,profile.FixedY?OpponentKind.Original:OpponentKind.Prototype,0,cinematic);
                    Require(!g.CanRestartBattle&&g.RestartBattle()==null,"Deployment was restartable");
                    g.AutoPlace();g.AutoPlace();g.AutoPlace();
                    if(profile.FixedY){Point before=g.Player.Ships[0].Cells[0];bool moved=false;for(int x=0;x<10&&!moved;x++)for(int y=0;y<10&&!moved;y++)if(g.MovePlacedShip(0,new Point(x,y))&&g.Player.Ships[0].Cells[0]!=before)moved=true;Require(moved,"Manual deployment fixture did not move");}
                    Require(g.Start()&&g.CanRestartBattle,"Start snapshot missing");string start=RestartBoardKey(g);int calls=g.PlacementCalls;uint state=g.PlacementState;
                    string history=RestartHistory(g,16);g.Paused=true;string old=RestartBoardKey(g);Game replay=g.RestartBattle();
                    Require(replay!=g&&replay.Seed==731&&replay.Rules==profile&&replay.Opponent==g.Opponent&&replay.OpponentLevel==0&&replay.MovieTurns==g.MovieTurns,"Restart configuration");
                    Require(replay.Phase==Phase.PlayerTurn&&!replay.Paused&&replay.Turn==1&&replay.Pending==null&&replay.LastPlayerShot==null&&replay.LastEnemyShot==null,"Restart retained battle progress");
                    Require(RestartBoardKey(replay)==start&&RestartBoardKey(g)==old&&replay.PlacementCalls==calls&&replay.PlacementState==state,"Initial fleet/RNG snapshot changed");
                    Require(!object.ReferenceEquals(g.Player,replay.Player)&&!object.ReferenceEquals(g.Enemy.Ships[0].Cells,replay.Enemy.Ships[0].Cells),"Restart shares mutable board data");
                    Require(RestartHistory(replay,16)==history,"Restart random/presentation/target history differs");
                    var again=replay.RestartBattle();Require(RestartBoardKey(again)==start&&RestartHistory(again,16)==history,"Second attempt mutated its starting snapshot");
                }
            });
            check("Battle restart detaches old pending shots and preserves finished snapshots",delegate {
                var g=new Game(-2147483648,RuleProfile.Recovered,OpponentKind.Prototype,0,true);g.AutoPlace();g.Start();string start=RestartBoardKey(g);g.SelectShooter(3);Require(g.Fire(g.Enemy.Ships[0].Cells[0]),"Pending fixture");var old=g.Pending;g.Paused=true;
                var replay=g.RestartBattle();Require(RestartBoardKey(replay)==start&&replay.PlayerShooter==1&&replay.EnemyShooter==1&&!replay.CompleteMovie(old,old.Stage)&&g.Pending==old&&g.Paused,"Old shot crossed restart boundary");
                g.Paused=false;DrainMovies(g);Require(RestartBoardKey(replay)==start,"Old game completion changed restarted game");
                RestartHistory(replay,100);Require(replay.Phase==Phase.Finished,"Finished restart fixture did not end");var finished=replay.RestartBattle();Require(finished.CanRestartBattle&&finished.Phase==Phase.PlayerTurn&&RestartBoardKey(finished)==start,"Finished outcome lost original deployment");
            });
            foreach(string name in new[] {
                "KEYCLIK2.WAV","CANNON1.WAV"
            })check("Original PCM fixture "+name,delegate {
                output.Add("  "+WaveInfo(Path.Combine(assets,"SFX",name)));
            });
            output.Add("Focused checks passed="+passed+" failed="+failed+(skipped>0?" skipped="+skipped:""));
            output.Add("These checks do not establish original gameplay fidelity, audible playback or physical keyboard/focus behavior.");
            File.WriteAllLines(report,output.ToArray());
            return failed==0?0:1;
        }
        static string RestartBoardKey(Game game) {
            var text=new StringBuilder();foreach(Board b in new[]{game.Player,game.Enemy}){foreach(Ship ship in b.Ships){text.Append(ship.Name).Append(':').Append(ship.SourceType).Append(':').Append(ship.Hits).Append(':');foreach(Point p in ship.Cells)text.Append(p.X).Append('/').Append(p.Y).Append(';');text.Append('|');}for(int x=0;x<10;x++)for(int y=0;y<10;y++)text.Append(b.Shots[x,y]?'1':'0');text.AppendLine();}return text.ToString();
        }
        static string RestartHistory(Game game,int maximumTurns) {
            var history=new StringBuilder();for(int turn=0;turn<maximumTurns&&game.Phase!=Phase.Finished;turn++){
                if(game.Rules.FixedY){int slot=turn%game.Player.Ships.Count;for(int i=0;i<game.Player.Ships.Count&&game.Player.Ships[slot].Sunk;i++)slot=(slot+1)%game.Player.Ships.Count;Require(game.SelectShooter(slot),"Replay shooter choice");}
                Point cell=new Point(-1,-1);for(int y=0;y<10&&cell.X<0;y++)for(int x=0;x<10&&cell.X<0;x++)if(!game.Enemy.Shots[x,y])cell=new Point(x,y);
                Require(game.Fire(cell),"Replay player action");RestartMovies(game,history);
                if(game.Phase==Phase.OpponentTurn){Require(game.AdvanceOpponent(),"Replay opponent action");RestartMovies(game,history);}
                history.Append(game.Phase).Append('/').Append(game.Turn).Append('/').Append(game.PlayerShooter).Append('/').Append(game.EnemyShooter).AppendLine();
                if(game.LastEnemyShot!=null)history.Append(game.LastEnemyShot.Description).AppendLine();history.Append(RestartBoardKey(game));
            }return history.ToString();
        }
        static void RestartMovies(Game game,StringBuilder text) {
            int count=0;while(game.Pending!=null){var p=game.Pending;text.Append(p.Opponent?'E':'P').Append(':').Append(p.Stage).Append(':').Append(p.MovieName).Append(':').Append(p.Firing.WeaponId).AppendLine();Require(game.CompleteMovie(p,p.Stage),"Replay movie completion");if(++count>3)throw new Exception("Replay movie sequence did not end");}
            if(game.LastPlayerFire!=null)text.Append(game.LastPlayerFire.MovieId).AppendLine();if(game.LastEnemyFire!=null)text.Append(game.LastEnemyFire.MovieId).AppendLine();
        }
        static string WaveInfo(string path) {
            byte[] b=File.ReadAllBytes(path);
            Require(b.Length>=12&&Encoding.ASCII.GetString(b,0,4)=="RIFF"&&Encoding.ASCII.GetString(b,8,4)=="WAVE","RIFF/WAVE signature");
            int p=12,rate=0,channels=0,bits=0,block=0,data=-1;
            while(p<=b.Length-8) {
                string id=Encoding.ASCII.GetString(b,p,4);
                uint length=BitConverter.ToUInt32(b,p+4);
                p+=8;
                Require(length<=b.Length-p,"WAV chunk truncated");
                if(id=="fmt ") {
                    Require(length>=16&&BitConverter.ToUInt16(b,p)==1,"Only PCM is expected");
                    channels=BitConverter.ToUInt16(b,p+2);
                    rate=BitConverter.ToInt32(b,p+4);
                    block=BitConverter.ToUInt16(b,p+12);
                    bits=BitConverter.ToUInt16(b,p+14);
                }
                if(id=="data")data=(int)length;
                p+=(int)length+(int)(length%2);
            }
            Require(rate>0&&channels>0&&block==channels*bits/8&&data>=0&&data%block==0,"Invalid PCM units");
            return channels+" channel, "+rate+" Hz, "+bits+" bit; "+(data/(double)(block*rate)).ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+" seconds; playback not auditioned";
        }
        static byte[] ResignBattle(string text) {
            int at=text.LastIndexOf("sha256=",StringComparison.Ordinal);string body=text.Substring(0,at);
            return Encoding.ASCII.GetBytes(body+"sha256="+Hash(Encoding.ASCII.GetBytes(body))+"\n");
        }
        static void MatchSavedMovies(Game first,Game second) {
            int count=0;while(first.Pending!=null||second.Pending!=null) {
                Require(first.Pending!=null&&second.Pending!=null&&++count<=3,"Resumed pending stage count");
                Require(first.Pending.Stage==second.Pending.Stage&&first.Pending.MovieName==second.Pending.MovieName&&first.Pending.Cell==second.Pending.Cell&&first.Pending.Opponent==second.Pending.Opponent,"Resumed movie choice changed");
                Require(first.CompleteMovie(first.Pending,first.Pending.Stage)&&second.CompleteMovie(second.Pending,second.Pending.Stage),"Resumed movie completion");
            }
        }
        static string GameFingerprint(Game game) {
            var text=new StringBuilder();foreach(var board in new[]{game.Player,game.Enemy}){foreach(var ship in board.Ships)text.Append(ship.Hits).Append(':').Append(ship.Sunk).Append(';');for(int y=0;y<10;y++)for(int x=0;x<10;x++)text.Append(board.Shots[x,y]?'1':'0');}
            text.Append(game.Phase).Append(game.Turn).Append(game.PlayerShooter).Append(game.EnemyShooter).Append(string.Join("|",game.Events.ToArray()));return text.ToString();
        }
        static void DrainMovies(Game game){int count=0;while(game.Pending!=null){var pending=game.Pending;Require(game.CompleteMovie(pending,pending.Stage),"Movie completion rejected");if(++count>3)throw new Exception("Shot presentation did not terminate");}}
        static int AlphaCount(Bitmap image,Rectangle area) {
            int count=0;for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++)if(image.GetPixel(x,y).A!=0)count++;
            return count;
        }
    }
}
