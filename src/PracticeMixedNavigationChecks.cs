using System;using System.IO;using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeMixedNavigationChecks
    {
        // GOLDENS1000: six initial inputs and48 outputs from original996r3
        // only. Raw complete pools and INT16 selection, never model outputs.
        static readonly string[][] goldens = {
            new[]{"0B0531F8C1E588E322719F16075E2AA69D10E454EC69864DCF4C4130A1EE4849","F48849116628A4EE777A32E31DA2949E303AF44A07A6056CCB86148C8B913EE0","ED1164C5482C45AC4EFEF0855AFCA9CA1D42AF4BE076A1D7A5D69AA1FD898B00","BB1D1EE5F61A62BF160E6F3144E17422362944953ED62EA4B6433582D03B6BB4","51180B88E2A671C5E38BFFA061925D3302C488BD0B8F97F8BAEEE0522007CB56","B4B7762E95515565A6364C452E81AD8FD16D928750D2928D62A3DD880DBB8214","187206981AE6702036DFD4C223B0E9A5809A658F87C3C4CC7EA4F23CA2C6A05A","56926A91E146F6AE1242C052F72A0201CCE6CBD9BB5CD55913E4F6895EEBE43A","48033AFA4194337635C94571C61EA54F66C943D52A79EBFC9C760C69605AAC26"},
            new[]{"F38E32226B3DC49AF420A6AEA5F6B39711456D69680CCB761D28165676500EF4","6F83DBB44ECB2C66241D2D48DE54DD8C2A3D9C5ADF4A286FEA5D02A2AC6F0D01","4DAB990143DF5F4851E6FB35128DA61703AB18469F3924729C4B3C491748D1F5","4DAB990143DF5F4851E6FB35128DA61703AB18469F3924729C4B3C491748D1F5","4DAB990143DF5F4851E6FB35128DA61703AB18469F3924729C4B3C491748D1F5","4DAB990143DF5F4851E6FB35128DA61703AB18469F3924729C4B3C491748D1F5","4DAB990143DF5F4851E6FB35128DA61703AB18469F3924729C4B3C491748D1F5","4DAB990143DF5F4851E6FB35128DA61703AB18469F3924729C4B3C491748D1F5","4DAB990143DF5F4851E6FB35128DA61703AB18469F3924729C4B3C491748D1F5"},
            new[]{"2753C6D7505A3D9BCD4A63319AF44C08CF57BD7CB418901D0BD594DCA65C324A","E563E8998ACCF923061717A9A8759489BC58EFD602A2A5CF92EF1BB449D36B21","3E05531CB5F609EF671731DEE989DAD75880A6A05AF8821EFBA844B4B9D6D20E","3E05531CB5F609EF671731DEE989DAD75880A6A05AF8821EFBA844B4B9D6D20E","3E05531CB5F609EF671731DEE989DAD75880A6A05AF8821EFBA844B4B9D6D20E","3E05531CB5F609EF671731DEE989DAD75880A6A05AF8821EFBA844B4B9D6D20E","3E05531CB5F609EF671731DEE989DAD75880A6A05AF8821EFBA844B4B9D6D20E","3E05531CB5F609EF671731DEE989DAD75880A6A05AF8821EFBA844B4B9D6D20E","3E05531CB5F609EF671731DEE989DAD75880A6A05AF8821EFBA844B4B9D6D20E"},
            new[]{"A115CD7B69361FC50E046FE45D218346CD38F93A411A4DE666B9ECE4A30C9C19","592066DE88809C18E0D1323B39C996F12A299BA3FCD84494FCDDD01838A71C51","809DB0621984E8FB2E2476B0373214667E8744C5665F229C45F623732DD9A4E9","A65E3D7BCC2BBA832FFC10E13C229FAD2830DA428ABE95EF98D92C8A08C66987","D27B185B94BE933D10263D543D711E0419A53A256E85FAA8215EA4CADA46E67B","D2ABCF2DB31A16226E4CCD33194914D81DF490C261F49F7738A4A5EE10CCE41F","5327C7B8727E6985E3F95D148C0621049D75A7C3C033F7CF528E8F6DF2B24891","F37A8CD2D367B417198E60A16171D30ED8B2E7625F75DE86F90730557F584911","FFF49FD6D57CC4F2D5D80E80F5A4B761A1D9CF3CF71398CC82B87EA2E99F1A47"},
            new[]{"D26EF0DA8AACA61E631BF2A25306364A58856FFD853A5402EE7BADAD2C2160F7","50F0627C7B42E20CFD8810BE3F854DA9F533E8A50607D9FF0EA0A7870702B17C","37EEE85A04552ECBA13DEF418FE52848F8BEB596C08086137BC25BDC6F5A0E93","37EEE85A04552ECBA13DEF418FE52848F8BEB596C08086137BC25BDC6F5A0E93","37EEE85A04552ECBA13DEF418FE52848F8BEB596C08086137BC25BDC6F5A0E93","37EEE85A04552ECBA13DEF418FE52848F8BEB596C08086137BC25BDC6F5A0E93","37EEE85A04552ECBA13DEF418FE52848F8BEB596C08086137BC25BDC6F5A0E93","37EEE85A04552ECBA13DEF418FE52848F8BEB596C08086137BC25BDC6F5A0E93","37EEE85A04552ECBA13DEF418FE52848F8BEB596C08086137BC25BDC6F5A0E93"},
            new[]{"A093232330193F9B1073193E57B3FDE3BCCDC5F3D342FB0F97B442349D2A4818","3A08E1456F77F879208E7B17D1C475B2796A3732246A104C6848DFD53FBC820C","0ACF1E2A342B1B4A33109BA1F1FE0A6385C0B7CC0D763ECC59A8C66C9EBBB2F1","B5EDCB0D2D1C623AB0AF0D724F93D62BB1ED1315F62DFA289092BFF51522B403","D42047C29D52A96A38381B19EB230A3459801CBF263BDA7BD0E642C73A829014","85C11B355CFC8DA260399D65C3D9ED7E6A24E36A4CCA5C75442DCD77591DD80F","53EC4E5EE9FE3B4407FE8D75E3AC1585CF01E9A6EF48CACE166C03195BA0C7C0","9F10EFAEFA77FA25C02E04FC57B25B3CEEAD1F119094EC0CD35C1F78D5551323","9A77BC268DC4E9DA1EFB415E7B20164B2993E13B317F9D6F70C47A8818EB44EA"},
        };
        static void Need(bool v,string why){if(!v)throw new InvalidDataException(why);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        internal static string Digest(PracticeMixedNavigationState state)
        {
            using(var memory=new MemoryStream())using(var w=new BinaryWriter(memory))
            {
                foreach(var pool in state.SnapshotForComparison())w.Write(pool);w.Write(state.CopyTerrainPool());w.Write(state.CallerWordAfter);w.Write(state.UiRequests);w.Write(unchecked((short)state.SelectedWaypoint));w.Flush();return Hash(memory.ToArray());
            }
        }
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");PracticeMapStartup startup=null;
            Func<int,PracticeMixedNavigationState> create=delegate(int preset){if(startup==null)startup=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);return PracticeMixedNavigationState.Create(programs,startup,preset);};
            check("Mixed policy/navigation matches54 original-only1000 complete-state digests",delegate{
                Need(goldens.Length==6,"Original1000 six profiles");int calls=0;
                for(int h=0;h<6;h++){var s=create(h);Need(goldens[h].Length==9&&Digest(s)==goldens[h][0],"Original996 independently map-built input "+h);for(int n=1;n<=8;n++){var old=s;string frozen=Digest(old);s=s.Advance();Need(Digest(s)==goldens[h][n],"Original996 complete output "+h+"/"+n);Need(Digest(old)==frozen,"Mixed input immutable");calls++;}Need(!s.CanAdvance&&s.Visits==8,"Mixed bounded window");}Need(calls==48,"Exactly48suppliedvisits,notbattlecount");
            });
            check("Mixed navigation initial-kind alias changes aircraft leg despite equal final counter",delegate{
                var initial=create(1);var correct=initial.Advance();var wrong=initial.AdvanceIgnoringInitialAliasForComparison();
                Need(correct.CounterBeforeParent==11&&correct.CounterAfterParent==13&&correct.InitialCollisionAliasWrites==1&&correct.CallerWordAfter==6,"Original initialkind11localalias");
                Need(BitConverter.ToInt32(correct.Backing.CopyTaskRecord(27),200)==2&&BitConverter.ToInt32(wrong.Backing.CopyTaskRecord(27),200)==3&&wrong.CallerWordAfter==6&&Digest(correct)!=Digest(wrong),"Original negative control:final6 masks wrong patrol phase");
                Need(Digest(initial)==goldens[1][0],"Counterfactual uses immutable copy");
            });
            check("Mixed navigation callback order,active replans and selection carry retain source boundaries",delegate{
                int launches=0,aliases=0,carry=0;for(int h=0;h<6;h++){var s=create(h);for(int n=0;n<8;n++){s=s.Advance();launches+=s.Activations;aliases+=s.InitialCollisionAliasWrites;if(n>0&&s.SelectedWaypoint==-1)carry++;Need(s.CounterBeforeParent==(h==1?11:0)&&s.CounterAfterParent==(h==1?13:2),"Original callback-order alias");if(n==0&&(h==3||h==5))Need(s.NavigationDescription=="Replanned"&&s.InitialCollisionAliasWrites==0,"Activecollision path preserves aliased local");if(h==1||h==2||h==4)Need(s.SelectedWaypoint==-1&&s.UiRequests==(n==0?1:0),"Activation resets selection once then carries");else Need(s.SelectedWaypoint==4&&s.UiRequests==0,"Stored aircraft selection unchanged");}Need(s.TotalActivations==(h==1||h==2||h==4?1:0),"One activation per threshold profile");}Need(launches==3&&aliases==8&&carry==21,"Original mixed coverage");
            });
            check("Mixed navigation copies,reset,invalid profile and exhausted-window operations preserve state",delegate{
                var s=create(3);string frozen=Digest(s);foreach(var b in new[]{s.CopyShipPool(),s.CopyFleetPool(),s.CopyPrimaryOrders(),s.CopyCandidateRegion(),s.CopyTerrainPool(),s.Backing.CopyRegion(),s.Grid.CopyCells()})b[0]^=255;Need(Digest(s)==frozen&&Digest(create(3))==frozen,"Copied arrays/reset");bool bad=false;try{create(-1);}catch(ArgumentOutOfRangeException){bad=true;}Need(bad,"Invalid profile rejects");while(s.CanAdvance)s=s.Advance();frozen=Digest(s);bad=false;try{s.Advance();}catch(InvalidOperationException){bad=true;}Need(bad&&Digest(s)==frozen,"Window exhaustion transactional");
            });
            check("Mixed navigation leaves older policy and queued-navigation admission guards intact",delegate{
                var s=create(0);bool policyRejected=false,navRejected=false;try{PracticePrephaseFlag23.EvaluateNoTargets(PracticeAttachmentPatrolTables.Load(programs),s.CopyShipPool(),s.CopyFleetPool(),s.CopyPrimaryOrders(),s.Backing,s.CopyCandidateRegion(),40,0,0,4);}catch(NotSupportedException){policyRejected=true;}
                try{PracticeQueuedNavigation.EvaluateOffline(s.Grid,s.CopyFleetPool(),s.CopyTerrainPool(),s.Backing,249,4,12345,32000,0);}catch(NotSupportedException){navRejected=true;}Need(policyRejected&&navRejected,"Legacy public APIs must still reject mixed active input");
            });
            evidence("Mixed navigation full48 original996r3 records match map-built pools,all250rows and caller state",delegate{Full(Path.Combine(Path.GetDirectoryName(assets),"Evidence","Mixed-Navigation-996.bin"),create);});
        }
        static byte[] Read(BinaryReader r,int n){var b=r.ReadBytes(n);Need(b.Length==n,"Mixed fixture truncated");return b;}
        static byte[] Patch(BinaryReader r,byte[] basis){int count=r.ReadInt32(),last=-1;Need(count>=0&&count<=basis.Length,"Mixed fixture patchcount");var b=(byte[])basis.Clone();for(int n=0;n<count;n++){int at=r.ReadInt32();Need(at>last&&at<b.Length,"Mixed fixture patchbounds");last=at;b[at]=r.ReadByte();}return b;}
        static void Full(string path,Func<int,PracticeMixedNavigationState> create)
        {
            Need(Hash(File.ReadAllBytes(path))=="E6719A23D90D8097AC39B149030B8334B98FF86EE2BF864C58AEF5525B4A5564","Original996 fixture identity");int count=0;PracticeMixedNavigationState s=null;
            using(var r=new BinaryReader(File.OpenRead(path)))
            {
                Need(System.Text.Encoding.ASCII.GetString(Read(r,8))=="MX996001"&&r.ReadInt32()==1&&r.ReadInt32()==6&&r.ReadInt32()==8&&r.ReadInt32()==48,"Original996 header");var basis=new byte[5][];int[] sizes={402000,123000,78000,78560,1184};for(int n=0;n<5;n++)basis[n]=Read(r,sizes[n]);var zeroT=Read(r,246000);var baseR=Read(r,78560);var tables=Read(r,64000);var rows=Read(r,1024000);
                for(int g=0;g<250;g++)for(int row=0;row<64;row++)Need(BitConverter.ToUInt32(tables,(g*64+row)*4)==0x70000000u+(uint)(g*6432+304+row*96),"Original heap table reference");
                for(;;){int id=r.ReadInt32();if(id==-1)break;int h=r.ReadInt32(),frame=r.ReadInt32(),parent=r.ReadInt32(),child=r.ReadInt32();uint scratch=r.ReadUInt32(),ui=r.ReadUInt32();short point=r.ReadInt16();var terrain=Patch(r,zeroT);byte[][] bases={basis[0],basis[1],basis[2],baseR,basis[4],rows};var before=new byte[6][];var after=new byte[6][];for(int n=0;n<6;n++){before[n]=Patch(r,bases[n]);after[n]=Patch(r,before[n]);}Need(id==count&&h==id/8&&frame==id%8,"Original996 order");if(frame==0)s=create(h);Need(s.ParentFleet==parent&&s.AircraftFleet==child&&Hash(s.CopyTerrainPool())==Hash(terrain),"Original996 inputs");var actual=s.SnapshotForComparison();for(int n=0;n<6;n++)Need(Hash(actual[n])==Hash(before[n]),"Original996 full prepared/carried input "+id+"/"+n);s=s.Advance();actual=s.SnapshotForComparison();for(int n=0;n<6;n++)Need(Hash(actual[n])==Hash(after[n]),"Original996 full output "+id+"/"+n);Need(unchecked((uint)s.CallerWordAfter)==scratch&&s.UiRequests==ui&&s.SelectedWaypoint==point,"Original996 caller/UI/INT16selection");count++;}
                Need(count==48&&r.BaseStream.Position==r.BaseStream.Length,"Original996 exact extent");
            }
        }
    }
}
