using System;using System.IO;using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeNavigationPhaseChecks
    {
        // Numeric outputs from original977 only; inserted after capture-hash check.
        static readonly string[] PhaseOriginal={
            "0,-1,0,0,0,1",
            "1,-1,1,0,1,0",
            "2,-1,2,0,-1,0",
            "3,-1,3,0,32767,0",
            "4,-1,4,0,-32768,0",
            "5,-1,5,1,0,2",
            "6,-1,6,1,1,1",
            "7,-1,7,1,-1,1",
            "8,-1,8,1,32767,1",
            "9,-1,9,1,-32768,1",
            "10,-1,10,30,0,31",
            "11,-1,11,30,1,30",
            "12,-1,12,30,-1,30",
            "13,-1,13,30,32767,30",
            "14,-1,14,30,-32768,30",
            "15,-1,15,31,0,32",
            "16,-1,16,31,1,31",
            "17,-1,17,31,-1,31",
            "18,-1,18,31,32767,31",
            "19,-1,19,31,-32768,31",
            "20,-1,20,32,0,33",
            "21,-1,21,32,1,32",
            "22,-1,22,32,-1,32",
            "23,-1,23,32,32767,32",
            "24,-1,24,32,-32768,32",
            "25,-1,25,63,0,64",
            "26,-1,26,63,1,63",
            "27,-1,27,63,-1,63",
            "28,-1,28,63,32767,63",
            "29,-1,29,63,-32768,63",
            "30,-1,30,64,0,65",
            "31,-1,31,64,1,64",
            "32,-1,32,64,-1,64",
            "33,-1,33,64,32767,64",
            "34,-1,34,64,-32768,64",
            "35,-1,35,127,0,128",
            "36,-1,36,127,1,127",
            "37,-1,37,127,-1,127",
            "38,-1,38,127,32767,127",
            "39,-1,39,127,-32768,127",
            "40,-1,40,128,0,129",
            "41,-1,41,128,1,128",
            "42,-1,42,128,-1,128",
            "43,-1,43,128,32767,128",
            "44,-1,44,128,-32768,128",
            "45,-1,45,2147483647,0,2147483648",
            "46,-1,46,2147483647,1,2147483647",
            "47,-1,47,2147483647,-1,2147483647",
            "48,-1,48,2147483647,32767,2147483647",
            "49,-1,49,2147483647,-32768,2147483647",
            "50,-1,50,2147483648,0,2147483649",
            "51,-1,51,2147483648,1,2147483648",
            "52,-1,52,2147483648,-1,2147483648",
            "53,-1,53,2147483648,32767,2147483648",
            "54,-1,54,2147483648,-32768,2147483648",
            "55,-1,55,4294967294,0,4294967295",
            "56,-1,56,4294967294,1,4294967294",
            "57,-1,57,4294967294,-1,4294967294",
            "58,-1,58,4294967294,32767,4294967294",
            "59,-1,59,4294967294,-32768,4294967294",
            "60,-1,60,4294967295,0,0",
            "61,-1,61,4294967295,1,4294967295",
            "62,-1,62,4294967295,-1,4294967295",
            "63,-1,63,4294967295,32767,4294967295",
            "64,-1,64,4294967295,-32768,4294967295",
            "65,0,0,30,-1,30",
            "66,0,1,30,-1,30",
            "67,0,2,30,-1,30",
            "68,0,3,30,0,31",
            "69,0,4,31,0,32",
            "70,0,5,32,0,33",
            "71,0,6,33,0,34",
            "72,0,7,34,0,35",
            "73,0,8,35,-32768,35",
            "74,0,9,35,0,36",
            "75,0,10,36,0,37",
            "76,0,11,37,0,38",
            "77,0,12,38,0,39",
            "78,0,13,39,0,40",
            "79,0,14,40,0,41",
            "80,0,15,41,0,42",
            "81,1,0,4294967295,-1,4294967295",
            "82,1,1,4294967295,-1,4294967295",
            "83,1,2,4294967295,-1,4294967295",
            "84,1,3,4294967295,0,0",
            "85,1,4,0,0,1",
            "86,1,5,1,0,2",
            "87,1,6,2,0,3",
            "88,1,7,3,0,4",
            "89,1,8,4,-32768,4",
            "90,1,9,4,0,5",
            "91,1,10,5,0,6",
            "92,1,11,6,0,7",
            "93,1,12,7,0,8",
            "94,1,13,8,0,9",
            "95,1,14,9,0,10",
            "96,1,15,10,0,11",
        };
        // Original-only983 exporter. Inputs include signed WORD variants, so
        // histories1/2 have different chains despite matching spatial output.
        static readonly string[] Original={
            "981,0,AB219DB9AAAC8DB3B281C0B0B2AE5C5DCF72BDD3E7D90E92E4756F07405EFF39,24FC7B5EE9D1681FB51A179144EE2ED3FCAA451750FFFA0232583C9470BD9658,117,117,120,120,4,1,15025,8,4,17385,17425,17425,40,0,3,1,125,3,125",
            "981,1,599FC9B809E5A91B1E4F9C1F4374BD5D3BE25B62719607A5B69D1CC2049BC981,3FB3C9B11105D60C5CB1E973EA8C0786FEFB615CB8C0BE1152E804AA411E7C03,110,100,110,90,4,1,16225,8,4,17385,17425,17425,40,1,3,1,125,3,125",
            "981,2,599FC9B809E5A91B1E4F9C1F4374BD5D3BE25B62719607A5B69D1CC2049BC981,D802A63EC390ED80631969AF90894F08A0E3B2024DD09462FED6672E4D0E4063,110,100,110,90,4,1,16225,8,4,17385,17425,17425,40,1,3,1,125,3,125",
            "981,3,599FC9B809E5A91B1E4F9C1F4374BD5D3BE25B62719607A5B69D1CC2049BC981,08F0BBB350D83EEB5485D8053055370C1447758B66FE3A572AADDB2C5AD0DF37,110,100,110,90,4,1,16265,8,4,17385,17425,17425,40,0,3,1,125,3,125"
        };
        static void Need(bool v,string why){if(!v)throw new InvalidDataException(why);}
        static byte[] Hash(byte[] b){using(var h=SHA256.Create())return h.ComputeHash(b);}
        static string Hex(byte[] b){return BitConverter.ToString(b).Replace("-","");}
        static string H(byte[] b){return Hex(Hash(b));}
        static void I(byte[] b,ref int at,int v){BitConverter.GetBytes(v).CopyTo(b,at);at+=4;}
        static short Pause(int h,int p){bool held=h==3?p>=32&&p<=34:p>=30&&p<=32;return held?(h==0?(short)1:h==1?(short)-1:short.MinValue):(short)0;}
        static int Sample(int h,int p){return(h==1||h==2)&&p==98?12345+97*40:12345+p*40;}
        static PracticeNavigationPhaseState Initial(PracticeGoalRouteStudyBasis basis,PracticeOwnerVisibilityRules visibility,int h)
        {
            var s=basis.CreateMovementComparison(h+8);var f=s.CopyFleetPool();f[249*492+467]=3;f[249*492+480]=1;
            return new PracticeNavigationPhaseState(new PracticeNavigationMovementState(f,s.Backing,s.Grid,s.CopyTerrainPool(),s.CopySecondaryPool(),s.Schedule,4),new OriginalFrameClock(12265,12305,12345,40),new OriginalPracticePhase(0),visibility);
        }
        static PracticeNavigationPhaseState World(PracticeNavigationPhaseState s,int h,int p,int eventFrame)
        {
            if(p!=eventFrame||h==0)return s;var moved=PracticeGoalRouteStudyBasis.ApplyMovementWorldEvent(s.Movement,h+8,65);
            return new PracticeNavigationPhaseState(moved,s.Clock,s.Phase,s.Visibility);
        }
        static byte[] Digest(PracticeNavigationMovementState s,OriginalFrameClock before,OriginalFrameClock body,OriginalFrameClock after,int sample,
            uint phaseBefore,uint phaseAfter,short pause,bool driver,int? scratch)
        {
            var b=new byte[451744];s.CopyFleetPool().CopyTo(b,0);s.Backing.CopyRegion().CopyTo(b,123000);s.Grid.CopyCells().CopyTo(b,201560);s.CopyTerrainPool().CopyTo(b,205656);int at=451656;
            foreach(var c in new[]{before,body,after})foreach(uint v in new[]{c.PreviousMilliseconds,c.CurrentMilliseconds,c.GameMilliseconds,c.DeltaMilliseconds})I(b,ref at,unchecked((int)v));
            foreach(int v in new[]{sample,(int)phaseAfter,s.Schedule.BatchStart,(int)s.Schedule.CompletedCycles})I(b,ref at,v);
            BitConverter.GetBytes((short)s.SelectedWaypoint).CopyTo(b,at);at+=2;I(b,ref at,driver?scratch??0:0);
            foreach(uint v in new[]{phaseBefore,phaseAfter,phaseAfter})I(b,ref at,unchecked((int)v));BitConverter.GetBytes(pause).CopyTo(b,at);at+=2;I(b,ref at,driver?1:0);Need(at==b.Length,"Phase clock digest extent");return Hash(b);
        }
        static byte[] Chain(byte[] chain,byte[] digest){var b=new byte[64];chain.CopyTo(b,0);digest.CopyTo(b,32);return Hash(b);}
        static string Identity(PracticeNavigationPhaseState s){return H(s.Movement.CopyFleetPool())+H(s.Movement.Backing.CopyRegion())+H(s.Movement.Grid.CopyCells());}
        static bool Reject(Action a){try{a();return false;}catch(ArgumentException){return true;}catch(NotSupportedException){return true;}catch(InvalidDataException){return true;}}
        static byte[] Read(BinaryReader r,int count){var b=r.ReadBytes(count);Need(b.Length==count,"Truncated phase fixture");return b;}
        static uint[] Clock(BinaryReader r){return new[]{r.ReadUInt32(),r.ReadUInt32(),r.ReadUInt32(),r.ReadUInt32()};}
        static void SameClock(OriginalFrameClock c,uint[] v){Need(c.PreviousMilliseconds==v[0]&&c.CurrentMilliseconds==v[1]&&c.GameMilliseconds==v[2]&&c.DeltaMilliseconds==v[3],"Source981 clock fields");}
        static byte[] Patch(BinaryReader r,byte[] basis){int count=r.ReadInt32(),last=-1;Need(count>=0&&count<=basis.Length,"Phase patch count");var b=(byte[])basis.Clone();for(int n=0;n<count;n++){int at=r.ReadInt32();Need(at>last&&at<b.Length,"Phase patch bounds/order");last=at;b[at]=r.ReadByte();}return b;}
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");PracticeGoalRouteStudyBasis basis=null;PracticeOwnerVisibilityRules visibility=null;
            Action prepare=delegate{if(basis==null){basis=PracticeGoalRouteStudyBasis.Create(programs,PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731));visibility=PracticeOwnerVisibilityRules.Load(programs);}};
            check("Original phase producer matches97 original977 signed-WORD boundary and carried outputs",delegate{
                Need(PhaseOriginal.Length==97,"Original977 complete numeric fixture");OriginalPracticePhase carried=null;
                for(int n=0;n<PhaseOriginal.Length;n++)
                {
                    var v=PhaseOriginal[n].Split(',');Need(v.Length==6&&int.Parse(v[0])==n,"Original977 row");int h=int.Parse(v[1]),frame=int.Parse(v[2]);uint before=uint.Parse(v[3]),after=uint.Parse(v[5]);short pause=short.Parse(v[4]);
                    if(h==-1||frame==0)carried=new OriginalPracticePhase(before);Need(carried.Value==before,"Original977 phase carry");var original=carried;carried=carried.Advance(pause);Need(carried.Value==after&&original.Value==before,"Original977 phase result/immutability");
                }
            });
            check("Phase clock navigation matches four original-only983 initial and128-frame chains",delegate{
                prepare();for(int h=0;h<4;h++)
                {
                    var v=Original[h].Split(',');Need(v.Length==23&&int.Parse(v[1])==h,"Original983 metadata");var s=Initial(basis,visibility,h);var original=s;string frozen=Identity(s);
                    Need(Hex(Digest(s.Movement,s.Clock,s.Clock,s.Clock,12345,0,0,0,false,null))==v[2],"Original983 independent initial state");var chain=new byte[32];int pauses=0,calls=0,passes=0,clears=0,zeros=0;
                    for(int p=0;p<128;p++)
                    {
                        s=World(s,h,p,68);var bf=s.Movement.CopyFleetPool();var u=s.AdvanceFrame((uint)Sample(h,p),Pause(h,p),0x13579bdf,0x2468ace0);s=u.State;
                        chain=Chain(chain,Digest(s.Movement,u.Before.Clock,u.BodyClock,s.Clock,Sample(h,p),u.Before.Phase.Value,s.Phase.Value,u.PauseWord,u.SimulationAdvanced,u.Driver==null?null:u.Driver.AlignedCallerScratchAfter));
                        if(u.SimulationAdvanced){calls++;passes+=u.Driver.OwnerVisibilityPasses;}else pauses++;if(s.Clock.CurrentMilliseconds==1)zeros++;if(bf[249*492+467]==3&&s.Movement.CopyFleetPool()[249*492+467]==0)clears++;
                    }
                    Need(Hex(chain)==v[3]&&frozen==Identity(original),"Original983 carried chain/immutability");var f=s.Movement.CopyFleetPool();var t=s.Movement.Backing.CopyTaskRecord(16);
                    int[] final={BitConverter.ToInt16(f,16*492+12),BitConverter.ToInt16(f,16*492+14),BitConverter.ToInt16(f,16*492+40),BitConverter.ToInt16(f,16*492+50),BitConverter.ToInt32(t,200),BitConverter.ToInt32(t,304),BitConverter.ToInt32(t,4),s.Movement.Schedule.BatchStart,(int)s.Movement.Schedule.CompletedCycles};
                    for(int n=0;n<final.Length;n++)Need(final[n]==int.Parse(v[n+4]),"Original983 final fields");SameClock(s.Clock,new[]{uint.Parse(v[13]),uint.Parse(v[14]),uint.Parse(v[15]),uint.Parse(v[16])});
                    Need(zeros==int.Parse(v[17])&&passes==int.Parse(v[18])&&clears==int.Parse(v[19])&&s.Phase.Value==uint.Parse(v[20])&&pauses==int.Parse(v[21])&&calls==int.Parse(v[22]),"Original983 observed counters");
                }
            });
            check("Copied pause gate holds phase and scheduler while source clock advances and detects ignored gate",delegate{
                prepare();foreach(int preset in new[]{17,19})
                {
                    int beforePause=preset==17?30:32;var study=new PracticeNavigationMovementStudyState(basis,preset,visibility).Advance(beforePause);string before=H(study.State.CopyFleetPool())+H(study.State.Backing.CopyRegion());int cursor=study.State.Schedule.BatchStart;
                    var held=study.Advance(1);Need(held.PausedFrames==1&&held.Phase.Value==(uint)beforePause&&held.State.Schedule.BatchStart==cursor&&held.Clock.CurrentMilliseconds>study.Clock.CurrentMilliseconds&&before==H(held.State.CopyFleetPool())+H(held.State.Backing.CopyRegion()),"Source pause holds simulation only");
                    var wrong=study.State.AdvanceNeutralDriverPoll((int)held.Clock.GameMilliseconds,new PracticeAiDriverContext(held.Phase.Value,1,0,0,0x13579bdf,0x2468ace0,4),visibility);Need(wrong.State.Schedule.BatchStart!=cursor,"Ignored pause must change source scheduler");
                    var three=study.Advance(3);Need(three.PausedFrames==3&&three.DriverVisits==beforePause&&three.NeutralPasses==(preset==17?0:1),"No repeated aligned driver while held");
                }
            });
            check("Produced-phase terrain replan precedes source zero reset and wrong epilogue changes task time",delegate{
                prepare();var s=Initial(basis,visibility,1);for(int p=0;p<98;p++)s=World(s,1,p,68).AdvanceFrame((uint)Sample(1,p),Pause(1,p),0x13579bdf,0x2468ace0).State;
                Need(s.Phase.Value==95&&s.Movement.CopyFleetPool()[16*492+482]==254,"Delayed source collision97");var u=s.AdvanceFrame(16225,0,0x13579bdf,0x2468ace0);
                var wrong=s.Movement.AdvanceNeutralDriverPoll(1,new PracticeAiDriverContext(96,1,0,0,0x13579bdf,0x2468ace0,4),visibility);
                Need(u.State.Phase.Value==96&&u.BodyClock.GameMilliseconds==16225&&u.State.Clock.GameMilliseconds==1&&BitConverter.ToInt32(u.State.Movement.Backing.CopyTaskRecord(16),4)==16225&&BitConverter.ToInt32(wrong.State.Backing.CopyTaskRecord(16),4)==1,"Actual task timestamp detects early epilogue");
                var next=u.State.AdvanceFrame(16305,0,0x13579bdf,0x2468ace0);Need(next.BodyClock.DeltaMilliseconds==1&&next.State.Clock.DeltaMilliseconds==16304,"Carried source clock reset");
            });
            check("Phase study grouped stepping windows reset and earlier seventeen presets retain their contracts",delegate{
                prepare();var initial=new PracticeNavigationMovementStudyState(basis,18,visibility);var a=initial.Advance(128);var b=initial.Advance(32).Advance(32).Advance(32).Advance(32);
                Need(a.Polls==128&&!a.CanAdvance&&a.Phase.Value==125&&a.PausedFrames==3&&a.DriverVisits==125&&a.NeutralPasses==3&&H(a.State.CopyFleetPool())==H(b.State.CopyFleetPool())&&H(a.State.Backing.CopyRegion())==H(b.State.Backing.CopyRegion()),"Grouped phase history");
                var reset=a.Reset();Need(reset.Polls==0&&reset.Phase.Value==0&&reset.DriverVisits==0&&reset.PausedFrames==0&&initial.Phase.Value==0&&initial.Clock.CurrentMilliseconds==12305,"Phase reset/immutability");
                var legacy=new PracticeNavigationMovementStudyState(basis,15,visibility).Advance(96);Need(!legacy.UsesSourcePhase&&legacy.Phase==null&&legacy.Clock.GameMilliseconds==1&&legacy.PausedFrames==0&&legacy.NeutralPasses==3,"Earlier clock preset unchanged");
                Need(!new PracticeNavigationMovementStudyState(basis,13).UsesSourcePhase&&new PracticeNavigationMovementStudyState(basis,13).WindowPolls==1,"Earlier repeated callback preset unchanged");
            });
            check("Joined phase profile rejects unexamined phase ranges and caller words transactionally",delegate{
                prepare();var s=Initial(basis,visibility,0);string before=Identity(s);Need(Reject(delegate{new PracticeNavigationPhaseState(s.Movement,s.Clock,new OriginalPracticePhase(129),visibility);}),"Joined phase range");
                Need(Reject(delegate{s.AdvanceFrame(12345,0,0,0x2468ace0);})&&Reject(delegate{s.AdvanceFrame(12345,-1,0x13579bdf,0);}),"Explicit caller words even for held input");
                var edge=new PracticeNavigationPhaseState(s.Movement,s.Clock,new OriginalPracticePhase(128),visibility);Need(Reject(delegate{edge.AdvanceFrame(12345,0,0x13579bdf,0x2468ace0);})&&edge.Phase.Value==128&&before==Identity(s),"Unsupported next phase preserves caller");
                Need(new OriginalPracticePhase(uint.MaxValue).Advance(0).Value==0,"Separate source leaf retains full uint wrap");
            });
            foreach(bool later in new[]{true,false})
            {
                bool delayed=later;evidence(delayed?"Produced phase gate matches all512 original981r2 full snapshots":"Earlier obstacle timing matches all512 original981 no-collision control snapshots",delegate{
                    prepare();VerifyCapture(Path.Combine(Path.GetDirectoryName(assets),"Evidence",delayed?"Phase-Clock-981.bin":"Phase-Earlier-981.bin"),basis,visibility,delayed);
                });
            }
        }
        static void VerifyCapture(string path,PracticeGoalRouteStudyBasis basis,PracticeOwnerVisibilityRules visibility,bool later)
        {
            Need(H(File.ReadAllBytes(path))==(later?"757F91025FFFB7FFAB0304B5544410AD977659D5837971B5BBD15F075A703861":"90EA3A768C12E0C8804BB2EAF1E9CF2F0118E4A920A529C34ECF004F68048C73"),"Source981 full fixture identity");
            using(var r=new BinaryReader(File.OpenRead(path)))
            {
                Need(System.Text.Encoding.ASCII.GetString(Read(r,8))=="PC981001"&&r.ReadInt32()==1&&r.ReadInt32()==4&&r.ReadInt32()==128&&r.ReadInt32()==512,"Source981 packet header");
                Read(r,402000);var baseF=Read(r,123000);var zeroT=Read(r,246000);Read(r,78000);var baseR=Read(r,78560);Read(r,64000);var rows=Read(r,1024000);
                PracticeNavigationPhaseState s=null;byte[] expectedRows=null;int count=0;
                for(;;)
                {
                    int id=r.ReadInt32();if(id==-1)break;uint phaseBefore=r.ReadUInt32(),bodyPhase=r.ReadUInt32(),phaseAfter=r.ReadUInt32();short pause=r.ReadInt16();uint calls=r.ReadUInt32();var before=Clock(r);var body=Clock(r);var after=Clock(r);uint scratch=r.ReadUInt32();
                    int h=r.ReadInt32(),p=r.ReadInt32(),stage=r.ReadInt32(),sample=r.ReadInt32(),phase=r.ReadInt32(),cursor0=r.ReadInt32(),cursor=r.ReadInt32();uint cycle0=r.ReadUInt32(),cycle=r.ReadUInt32();int point0=r.ReadInt32(),point=r.ReadInt32(),worldEvent=r.ReadInt32();
                    var terrain=Patch(r,zeroT);var bf=Patch(r,baseF);var br=Patch(r,baseR);var beforeRows=Patch(r,rows);var af=Patch(r,bf);var ar=Patch(r,br);var afterRows=Patch(r,beforeRows);int eventFrame=later?68:65;
                    Need(id==count&&h==id/128&&p==id%128&&stage==0&&sample==Sample(h,p)&&pause==Pause(h,p)&&bodyPhase==phaseAfter&&phase==(int)phaseAfter&&point0==4&&point==4&&worldEvent==(p==eventFrame?(h==1||h==2?1:h==3?3:0):0),"Source981 metadata");
                    if(p==0){s=Initial(basis,visibility,h);expectedRows=(byte[])rows.Clone();}s=World(s,h,p,eventFrame);SameClock(s.Clock,before);Need(s.Phase.Value==phaseBefore,"Source981 actual phase carry");s.Movement.Grid.CopyCells().CopyTo(expectedRows,16*4096);
                    Need(H(s.Movement.CopyFleetPool())==H(bf)&&H(s.Movement.Backing.CopyRegion())==H(br)&&H(s.Movement.CopyTerrainPool())==H(terrain)&&H(expectedRows)==H(beforeRows)&&s.Movement.Schedule.BatchStart==cursor0&&s.Movement.Schedule.CompletedCycles==cycle0,"Source981 complete independently built/carried input");
                    var u=s.AdvanceFrame((uint)sample,pause,0x13579bdf,0x2468ace0);s=u.State;SameClock(u.BodyClock,body);SameClock(s.Clock,after);Need(s.Phase.Value==phaseAfter&&calls==(u.SimulationAdvanced?1u:0u),"Source981 phase and actual driver gate");s.Movement.Grid.CopyCells().CopyTo(expectedRows,16*4096);
                    Need(H(s.Movement.CopyFleetPool())==H(af)&&H(s.Movement.Backing.CopyRegion())==H(ar)&&H(s.Movement.CopyTerrainPool())==H(terrain)&&H(expectedRows)==H(afterRows)&&s.Movement.Schedule.BatchStart==cursor&&s.Movement.Schedule.CompletedCycles==cycle,"Source981 complete output");
                    if(calls==1&&(phaseAfter&31)==0)Need(u.Driver.AlignedCallerScratchAfter==unchecked((int)scratch),"Source981 aligned scratch");count++;
                }
                Need(count==512&&r.BaseStream.Position==r.BaseStream.Length,"Source981 exact packet extent");
            }
        }
    }
}
