using System;
using System.IO;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeFleetMemberPreparationChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}catch(NotSupportedException){return true;}catch(InvalidOperationException){return true;}}
        // Independent original527 complete endpoint rows.
        static readonly string[] Original={
            "106,3,0,3,0,2,1,0,12346,0,3,0,0,0,0,0,1835887981,1835887981,18,17,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,D0D7F1E7028510CADA7BB87C8ECDC6D2598CC56B5830113D0ACF542B4F532FB0,D2D96DFF62CD64FAFFD469E8CD27A780F61C32A557A3B4F652AD82ACA63FAB6D,B6FBF913D2A5198FE2D41E20069C2E3E341238BC68D2F09E521D8ACADAA64116,468969B773B8EFE95A45A539C23DD5C3D5A6A5710C370B2CE73A7FF5EAB5F9D7,01F4385EDA0211FE0D1D7EA8901E7CAE865C1F377AF63B4F5DF9FD435CC96FC1",
            "244,3,1,3,8,1,0,0,12346,11,-1,1,0,0,0,0,1835887981,1835887981,18,17,E2059DA98E7605BFF15C0CE4F37AF7E2F6E659B4A448687E60261D219E2A8100,D0D7F1E7028510CADA7BB87C8ECDC6D2598CC56B5830113D0ACF542B4F532FB0,0507049FA0A39ECB0219E2C23FFA847703DE8C16B981BD18F312829B17F9F122,E6CB9C9C356D2BA83CE89493AE74C91AD065FAC76EEC4D3409211C0B8450F091,8C16B0D52AE31E3A0C1337F2CA3A94770390BB18D83EADBB2973841DD102B474,E8305AAC5F710FFD083AA1C7AADA124DEA9E71F4EE8AB6A7CB4EEA9643F09746",
            "416,3,3,1,0,0,0,0,22345,0,3,0,2,1,22345,32344,14,14,14,14,FC2669B5A880ECAB2784794441555CDBA2C1227954FC3875D4F6FCF391ED196D,D2933D5EACD0600FB5D5D7A31137AB6CA5A478F5197B7EF4CD5FF68E830436C2,77C255B794C7B93BB7BC569033953899A83CC84048679065913EE8C2D6552D53,229121EA006CD2FB52E267BD8A6A92C02DEAEA186634A1526C8B21341E452E5A,A1A397DF0B880B023D10583CCABCCDB28688679C376CA068F57261C73F57E99D,453BEA39966E355B18751ECC4F11CA151D23601940E17593A9F0179D24BDD5C3",
            "417,3,3,1,0,0,0,1,22345,0,3,0,2,0,22345,0,14,1835887981,14,17,813EF1E8A832019175C497AFF30090C4E0E149D5FAD46EA49C81C50993F8A5F5,D2933D5EACD0600FB5D5D7A31137AB6CA5A478F5197B7EF4CD5FF68E830436C2,D2F2BE3BBF6BBC199652FBE52A1A32B47B827FDE0410CB152335B6EC5ED89804,229121EA006CD2FB52E267BD8A6A92C02DEAEA186634A1526C8B21341E452E5A,A1A397DF0B880B023D10583CCABCCDB28688679C376CA068F57261C73F57E99D,47E6A7A105B0257E59CB543BAEF4E2A15A6D7B9C94DD1192B33516FCA29653FC",
            "448,3,3,2,0,0,0,0,22345,0,3,0,2,1,22345,32344,15,15,14,14,FC2669B5A880ECAB2784794441555CDBA2C1227954FC3875D4F6FCF391ED196D,7F2A6778FB0E863D39418ED92EF745E167223EA35125520ADACB3EAA818FDEEA,6E759A9CB56FC4A4E3D0770A645A49DE921AB167E5409562C8D6E8FFAB04FDBD,229121EA006CD2FB52E267BD8A6A92C02DEAEA186634A1526C8B21341E452E5A,1CBFD0995F90AACD0691FF43E6158E8114FE6969E9F940E524C807B78E6745DB,E90E1F5EB79FBBA7BABC772EC77C83A0B9FADF9F47889AB049946AD03F710B96",
            "456,3,3,2,0,2,0,0,14845,0,3,0,2,2,14845,14845,15,14,14,14,FC2669B5A880ECAB2784794441555CDBA2C1227954FC3875D4F6FCF391ED196D,7F2A6778FB0E863D39418ED92EF745E167223EA35125520ADACB3EAA818FDEEA,1CA5D62C29E39B81416DFB8523FE7A59C978B654FFAB0C9169577AF43219678E,229121EA006CD2FB52E267BD8A6A92C02DEAEA186634A1526C8B21341E452E5A,1CBFD0995F90AACD0691FF43E6158E8114FE6969E9F940E524C807B78E6745DB,ED4EEED7E0107518E8884C70242249CB6EC45E2072BE1DA722359AC12462E0FF",
            "458,2,3,2,0,2,1,0,14844,0,3,0,2,2,2,2,15,14,14,14,FC2669B5A880ECAB2784794441555CDBA2C1227954FC3875D4F6FCF391ED196D,7F2A6778FB0E863D39418ED92EF745E167223EA35125520ADACB3EAA818FDEEA,C572EE72406AE7C166E55F42F94418209E2BA727525B099BD569EC0A8487F9F5,229121EA006CD2FB52E267BD8A6A92C02DEAEA186634A1526C8B21341E452E5A,1CBFD0995F90AACD0691FF43E6158E8114FE6969E9F940E524C807B78E6745DB,A23365F635DD23E5F5848CA1B3D0182E01B129AA83DB2DBEA88F5B36339B3F29",
            "584,3,4,2,0,2,0,0,12346,0,3,0,0,0,0,0,1835887981,1835887981,18,17,1BB1F1A432EDCC87B2C30D7CE87C5370E4AD2081873F28FC467E72C7EA97FB2D,7F2A6778FB0E863D39418ED92EF745E167223EA35125520ADACB3EAA818FDEEA,D2D96DFF62CD64FAFFD469E8CD27A780F61C32A557A3B4F652AD82ACA63FAB6D,76879DF936C89B25E7AE0C7B9485019FB88C88312B721E3C26D888D2C3F08405,1CBFD0995F90AACD0691FF43E6158E8114FE6969E9F940E524C807B78E6745DB,01F4385EDA0211FE0D1D7EA8901E7CAE865C1F377AF63B4F5DF9FD435CC96FC1",
            "600,3,4,2,8,2,0,0,22345,8,3,0,2,2,22345,22345,15,14,19,19,1D8003379B1415CC67E71B2F68438632DC10492B5D0DD5496E8A1B6D42A2706A,7F2A6778FB0E863D39418ED92EF745E167223EA35125520ADACB3EAA818FDEEA,0C0FB1D78F95E6338DBCE4F866FC6DA3B17B14741078F791402AF48F1261D250,76879DF936C89B25E7AE0C7B9485019FB88C88312B721E3C26D888D2C3F08405,1CBFD0995F90AACD0691FF43E6158E8114FE6969E9F940E524C807B78E6745DB,30C800A81B43470AD73D77863FC14304F7711E5F7FC97C3A55A9A8C36DF23FC2",
        };
        static PracticeFleetMemberPreparation FromRow(string programs,PracticeMapStartup map,string[] row)
        {return PracticeFleetMemberPreparation.Create(programs,map,int.Parse(row[2]),int.Parse(row[3]),int.Parse(row[4]),int.Parse(row[5]),row[6]=="1",int.Parse(row[7]));}
        static void Compare(PracticeFleetMemberPreparation c,string[] r,bool raw)
        {
            Need(r.Length==26&&c.Visits==int.Parse(r[1])&&c.Clock==int.Parse(r[8])&&c.TaskKind==int.Parse(r[9])&&c.SelectedWaypoint==int.Parse(r[10])&&c.RouteUiRequests==int.Parse(r[11])&&c.MemberState(0)==int.Parse(r[12])&&c.MemberState(1)==int.Parse(r[13])&&c.MemberDue(0)==int.Parse(r[14])&&c.MemberDue(1)==int.Parse(r[15])&&c.WeaponField(0)==int.Parse(r[18])&&c.WeaponField(1)==int.Parse(r[19]),"Original527 scalars "+r[0]+"/"+r[1]);
            var t=c.CopyTaskRecord();Need(BitConverter.ToInt32(t,260)==int.Parse(r[16])&&BitConverter.ToInt32(t,264)==int.Parse(r[17])&&Hash(c.CopyShipPool())==r[20]&&Hash(c.CopyFleetPool())==r[21]&&Hash(t)==r[22],"Original527 complete pools/context "+r[0]+"/"+r[1]);if(raw)Need(Hash(c.CopyScratchBytes())==r[23]&&Hash(c.CopyRawList())==r[24],"Original527 scratch/list "+r[0]+"/"+r[1]);
        }
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string root=Path.GetDirectoryName(assets),programs=Path.Combine(root,"Programs");Func<uint,PracticeMapStartup> map=delegate(uint seed){return PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),seed);};
            check("Member preparation matches nine complete original527 endpoints",delegate
            {
                var startup=map(731);foreach(string line in Original){var r=line.Split(',');var c=FromRow(programs,startup,r);for(int n=0;n<int.Parse(r[1]);n++)c=c.Advance();Compare(c,r,true);Need(!c.CanAdvance,"Captured endpoint still advances");}Need(Original.Length==9,"Original527 fixture count");
            });
            check("Member preparation difficulty controls delay and candidate allocation",delegate
            {
                var startup=map(731);for(int level=0;level<4;level++)
                {
                    var c=PracticeFleetMemberPreparation.Create(programs,startup,3,2,0,level,false,0).Advance();Need(c.F2Delay==10000/(1<<level)&&c.F3Delay==40000/(1<<level)&&c.MemberState(0)==1&&c.MemberDue(0)==12345+c.F2Delay&&c.MemberTarget(0)==15,"Difficulty parameter scaling");
                    Need(c.MemberState(1)==(level<2?0:1)&&(level<2||c.MemberTarget(1)==14),"Difficulty member allocation");
                }
            });
            check("Supplied caller comparison word exposes waiting-loop dependency",delegate
            {
                var startup=map(731);var zero=PracticeFleetMemberPreparation.Create(programs,startup,3,1,0,0,false,0).Advance().Advance();var one=PracticeFleetMemberPreparation.Create(programs,startup,3,1,0,0,false,1).Advance().Advance();
                Need(zero.Clock==22344&&one.Clock==22344&&zero.MemberState(0)==1&&one.MemberState(0)==1&&zero.MemberState(1)==1&&one.MemberState(1)==0&&zero.MemberDue(1)==32344,"Supplied stack comparison distinction");
            });
            check("Member preparation F3 task gate override and firing boundary remain separate",delegate
            {
                var startup=map(731);var excluded=PracticeFleetMemberPreparation.Create(programs,startup,4,2,0,2,false,0).Advance();var allowed=PracticeFleetMemberPreparation.Create(programs,startup,4,2,8,2,false,0).Advance();Need(excluded.MemberState(0)==0&&allowed.MemberState(0)==1&&allowed.WeaponField(0)==19&&allowed.MemberDue(0)==22345,"F3 task/priority admission");
                var c=PracticeFleetMemberPreparation.Create(programs,startup,3,2,0,2,false,0).Advance();c=c.Advance();Need(c.Clock==14844&&c.MemberState(0)==1,"Deadline-1 fired early");c=c.Advance();Need(c.Clock==14845&&c.MemberState(0)==2&&c.MemberState(1)==2&&!c.CanAdvance&&Reject(delegate{c.Advance();}),"Deadline boundary guard");
                var instant=PracticeFleetMemberPreparation.Create(programs,startup,3,2,0,2,true,0).Advance().Advance();Need(instant.Visits==2&&instant.FiringBoundary&&instant.MemberDue(0)==2&&!instant.CanAdvance,"Player record override");
            });
            check("Member preparation preserves source opaque fields and rejects unsupported inputs",delegate
            {
                var startup=map(731);string before=Hash(startup.Stages[3].CopyShipPool())+Hash(startup.Stages[3].CopyFleetPool());var initial=PracticeFleetMemberPreparation.Create(programs,startup,3,2,0,2,false,0);Need(!initial.HasScanned&&initial.MemberTarget(0)==-1&&Reject(delegate{initial.CopyScratchBytes();}),"Unproduced scratch or opaque target presented as recovered");var c=initial.Advance();var t=c.CopyTaskRecord();t[0]^=255;Need(c.TaskKind==0&&initial.Visits==0&&initial.MemberState(0)==0&&before==Hash(startup.Stages[3].CopyShipPool())+Hash(startup.Stages[3].CopyFleetPool()),"Mutable predecessor/source");
                var empty=PracticeFleetMemberPreparation.Create(programs,startup,1,3,8,1,false,0).Advance().Advance().Advance();Need(empty.TaskKind==11&&empty.RouteUiRequests==1&&!empty.FiringBoundary&&!empty.CanAdvance,"No-equipment prefix/task side effect");
                Need(Reject(delegate{PracticeFleetMemberPreparation.Create(programs,startup,6,0,0,0,false,0);})&&Reject(delegate{PracticeFleetMemberPreparation.Create(programs,startup,0,4,0,0,false,0);})&&Reject(delegate{PracticeFleetMemberPreparation.Create(programs,startup,0,0,1,0,false,0);})&&Reject(delegate{PracticeFleetMemberPreparation.Create(programs,startup,0,0,0,4,false,0);})&&Reject(delegate{PracticeFleetMemberPreparation.Create(programs,startup,0,0,0,0,false,2);})&&Reject(delegate{PracticeFleetMemberPreparation.Create(programs,map(0),0,0,0,0,false,0);}),"Unsupported preparation input admitted");
            });
            evidence("Member preparation matches all2928 original527 observations",delegate
            {
                var startup=map(731);var baseS=startup.Stages[3].CopyShipPool();var baseF=startup.Stages[3].CopyFleetPool();var all=new byte[250*312];for(int n=0;n<250;n++)PracticeFleetServiceContext.CreateDiagnostic(n).CopyTo(all,n*312);
                var lines=File.ReadAllLines(Path.Combine(root,"Evidence","Fleet-Member-Prepare-527.csv"));int histories=0,observations=0;
                using(var r=new BinaryReader(File.OpenRead(Path.Combine(root,"Evidence","Fleet-Member-Prepare-527.bin"))))
                {
                    Need(r.ReadInt32()==0x3532374d&&r.ReadInt32()==2,"527 evidence header");
                    for(;;)
                    {
                        int id=r.ReadInt32();if(id==-1)break;Need(id==histories&&id<768,"527 history order");var input=new int[6];for(int n=0;n<6;n++)input[n]=r.ReadInt32();var beforeS=Delta(r,baseS,268);var beforeF=Delta(r,baseF,492);var beforeT=Read(r,312);var c=PracticeFleetMemberPreparation.Create(programs,startup,input[0],input[1],input[2],input[3],input[4]==1,input[5]);Need(Hash(c.CopyShipPool())==Hash(beforeS)&&Hash(c.CopyFleetPool())==Hash(beforeF)&&Hash(c.CopyTaskRecord())==Hash(beforeT),"527 input factory "+id);int expected=0;
                        for(;;)
                        {
                            int step=r.ReadInt32();if(step==-1)break;Need(step==expected++&&step<=3,"527 step order");int clock=r.ReadInt32();var afterS=Delta(r,beforeS,268);var afterF=Delta(r,beforeF,492);var afterT=Read(r,312);var scratch=Read(r,164);var list=Read(r,1008);int waypoint=r.ReadInt32(),route=r.ReadInt32();if(step>0)c=c.Advance();var row=lines[observations+1].Split(',');Need(int.Parse(row[0])==id,"527 CSV order");Compare(c,row,step>0);
                            Need(c.Clock==clock&&c.SelectedWaypoint==waypoint&&c.RouteUiRequests==route&&Hash(c.CopyShipPool())==Hash(afterS)&&Hash(c.CopyFleetPool())==Hash(afterF)&&Hash(c.CopyTaskRecord())==Hash(afterT),"527 binary full state "+id+"/"+step);if(step>0)Need(Hash(c.CopyScratchBytes())==Hash(scratch)&&Hash(c.CopyRawList())==Hash(list),"527 binary raw state");c.CopyTaskRecord().CopyTo(all,16*312);Need(Hash(all)==row[25],"527 all-context hash");observations++;
                        }
                        Need(expected>=3&&expected<=4&&!c.CanAdvance,"527 prefix boundary");histories++;
                    }
                    Need(histories==768&&observations==2928&&lines.Length==2929&&r.BaseStream.Position==r.BaseStream.Length,"527 complete matrix/trailing");
                }
            });
        }
        static byte[] Read(BinaryReader r,int length){var bytes=r.ReadBytes(length);if(bytes.Length!=length)throw new EndOfStreamException("527 truncated evidence");return bytes;}
        static byte[] Delta(BinaryReader r,byte[] basis,int stride){var bytes=(byte[])basis.Clone();int count=r.ReadUInt16(),last=-1;if(count>bytes.Length/stride)throw new InvalidDataException("527 delta count");for(int n=0;n<count;n++){int slot=r.ReadUInt16();if(slot<=last||slot>=bytes.Length/stride)throw new InvalidDataException("527 delta slot");last=slot;Read(r,stride).CopyTo(bytes,slot*stride);}return bytes;}
    }
}

