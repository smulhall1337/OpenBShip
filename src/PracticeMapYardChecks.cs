using System;
using System.IO;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeMapYardChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}catch(NotSupportedException){return true;}catch(InvalidOperationException){return true;}}
        // Original425, supported RIP B9A8...6A569; captured before/entered/after
        // whole-pool hashes. Rows are data evidence, not managed expected output.
        // target,profile,returnPass,beforeS,beforeF,enteredS,enteredF,afterS,afterF.
        static readonly string[] Original={
            "30,0,1,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,80D625895D59739925D5F45C4E18421F035E1D9EB658401D2CEA4DD245C62B78,A616EA51DF2CA74975894A0485B6AAAEB150ADC3616CD85A2350E3199E78570A,AD827412D307CE333BB1C8153B42D1F2DC8FF0F8EEF9E7DE6CDAE3B7CC96E94B,26231CBC64AD3875EC2BEF7D80F88ECA9A46030AB08110EB540BA8367A949A77,640D7EE6BFC7F887D1FF85F1222CE2C8F7D43B8EAC102EE7EC1C65DDEBD37D89",
            "30,1,5,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,FDE14B2BE34F326919E1EC67A4A903D610904EA74313F1A5E79796DB8322FBB6,68C7C027D33533F9416B94D381596CFFD4453E2FE0BFDACF819207971A749A13,F71034F2DD5E072280DBDC762F26985CB5C5798AF2C5CB8CE1F403BEF4F25A59,3E75F51B2A5DEA268396673B6CE1876B283210881809F544B17D0401E093D515,640D7EE6BFC7F887D1FF85F1222CE2C8F7D43B8EAC102EE7EC1C65DDEBD37D89",
            "30,2,5,4A1041FA42C6E6A38E0348DF9F75CC22CEFD43BFE060EC5ABEDFEE0CF47CE912,DAD07816FC46B4E223D1639022516283D940AAD3A9CAF652D0EF782909497481,AE3A38BEE80A65492481FE0D39185D137294B7E25C1B2F94670CBA43B57CEC82,581A81476EA845647E9566FDCFE5D4A96A12A02F4FEF0BBF518A4DFA6B32C3F9,3E75F51B2A5DEA268396673B6CE1876B283210881809F544B17D0401E093D515,640D7EE6BFC7F887D1FF85F1222CE2C8F7D43B8EAC102EE7EC1C65DDEBD37D89",
            "31,0,1,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,80D625895D59739925D5F45C4E18421F035E1D9EB658401D2CEA4DD245C62B78,5EDC7E0F33C67EC90AC1CC1EB19F62BF55CC530D05068FBE911D51BBA37F9357,A2E69E5C245933894BAB28903738DF0AC98C2A01A6640CA7CA94793AE3F22159,6B368A460B0DE7CB6872E307EC0A8B33A1B7384EE3DB2C01C89ABC83C49A102E,7CAE55A392040F410DC28D8A314C2CD3FD58791534F959E6F78F8FB68673604E",
            "31,1,4,5286D62FDC78663DCC2ACC16F996CE6AEC9F493EE0977331E00FB3C0E4513840,F25BCAE991B1B5D0ED3A234CA32ED02C3858DFAEB0A619035347F3B78467CA0D,F846003F34128D2BEDA2E35BEC166D99DB1DB99EACBF928081BB7FFBA6F3AC62,82BA27B7F75AEE362A3E2ECFF243AF26D5B295D92EE7CDB898A8FD7258A40727,D4C68D5891E6790D61CD93E4B5537117993BBD26AE17430A5B9D0BDB23E29D32,7CAE55A392040F410DC28D8A314C2CD3FD58791534F959E6F78F8FB68673604E",
            "31,2,4,C99BD1B70BA5356D8444B3D8CFAA03517C855C63DF5188F6D1E402EBB4C825CA,82ACACEB91926D3F341C30DDE0438C80FEBB7E8C065CF8EF23ADA3CC8A71708A,2DD23A32AA6D1668F7F5A697188BF22365A68534412B8A3EF335B2AD4467FE07,EB56E6C4DE4D9BDEE7FCEA190AB7A565B2921770B0177C9878008198003C7F5E,D4C68D5891E6790D61CD93E4B5537117993BBD26AE17430A5B9D0BDB23E29D32,7CAE55A392040F410DC28D8A314C2CD3FD58791534F959E6F78F8FB68673604E",
            "34,0,1,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,5CDB75A1D1EECAB71F7DE3EE1B2F21CE166264A9CC06AEE6100C303D85D1AB17,93D9A37FEEFCF671B3542B7700FEB9A27E4AF189C797ACC82266275AACA19EDF,ECB590835AD8BBEC337B77DA273168B10372EE9C08FDBA42235C7134F2478F84,E2A61EED0A0BFDA72963D78FA6A13D60C8A28C9A6C1209F41BE457A2DE3D601D,ABCD57D944EBB65D4310168565F8EAA80CC2CB42922796F4B6C5CF85248E3833",
            "34,1,3,80751CAE90B9438C87BDB45020413EDB67FBD0FCD6997052D945D9CC94474774,BC7D1C587BF2397D8DA1E940FBCF98CB62F4D212F44C06F27473062B0C7FB6C2,C16C5E1E49C9C225601F1ECB865943208AC9E27EBED856F8F1DCDD36B6550ABB,3F538E771ECA21EB6C99B6D31227FEFB6D15039143AB265B6B64C1A12E736CB6,B656F19DD51D18BCC92AC493FEF8E245430368CFB27FE9FC1DC663F4CB3C8111,ABCD57D944EBB65D4310168565F8EAA80CC2CB42922796F4B6C5CF85248E3833",
            "34,2,3,1C752E5958B7DE164A8301CBE0AFDA99C683ED3CC280B2E131F07C0BD37361B4,D54CC237521259BFFEC597E8D72C597F2A87A2A8B0AE104E54A6D682173FC110,8905DACB6733CD8D6A045BE0EA12A5B77A6D83A48503AF47FB5B3199C94CDABA,44C27C87972246CE6694BB59264D93425F73D94DD8E0BB5229C6EAFF075BDA32,B656F19DD51D18BCC92AC493FEF8E245430368CFB27FE9FC1DC663F4CB3C8111,ABCD57D944EBB65D4310168565F8EAA80CC2CB42922796F4B6C5CF85248E3833",
        };
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            Func<uint,PracticeMapStartup> startup=delegate(uint seed){return PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),seed);};
            check("Copied map yard matches nine original before entry and returned whole-pool goldens",delegate
            {
                var map=startup(731);
                foreach(string line in Original)
                {
                    var row=line.Split(',');int target=int.Parse(row[0]),profile=int.Parse(row[1]);
                    var state=PracticeMapYardState.Create(programs,map,target,profile);
                    Need(Hash(state.CopyShipPool())==row[3]&&Hash(state.CopyFleetPool())==row[4],"Original425 input "+target+"/"+profile);
                    var entered=state.Enter();Need(Hash(entered.CopyShipPool())==row[5]&&Hash(entered.CopyFleetPool())==row[6],"Original425 entered pools");
                    int firstReturn=0;state=entered;
                    for(int n=1;n<=20;n++){state=state.WorkerStep();if(state.Returned&&firstReturn==0)firstReturn=n;}
                    Need(firstReturn==int.Parse(row[2])&&Hash(state.CopyShipPool())==row[7]&&Hash(state.CopyFleetPool())==row[8],"Original425 full return/post-return pools");
                    Need(state.SelectedFleet==state.OriginFleet&&state.SelectedUnit==target&&state.TextRequests==2&&state.MapRequests==1&&state.RefreshRequests==3,"Original425 selection/request context");
                }
            });
            check("Copied map yard conserves depot ammunition and uses the first fitting source bay",delegate
            {
                var map=startup(731);int[] targets={30,31,34},amounts={138,162,70},passes={5,4,3};
                for(int n=0;n<targets.Length;n++)
                {
                    var initial=PracticeMapYardState.Create(programs,map,targets[n],1);var gate=initial.CheckAdmission();
                    Need(gate.Available&&gate.SelectedYard==3&&gate.MarkerFleet==(n==0?3:2)&&initial.DepotUnit==2,"Original425 aligned first-fit/depot");
                    int total=initial.TargetAmmo+initial.DepotAmmo;var state=initial.Enter();
                    for(int call=1;call<=passes[n];call++)
                    {
                        state=state.WorkerStep();Need(state.TargetAmmo+state.DepotAmmo==total,"Copied-map ammunition conservation");
                        Need(state.Returned==(call==passes[n]),"Original425 return boundary");
                    }
                    Need(state.TargetAmmo==amounts[n]&&initial.DepotAmmo-state.DepotAmmo==amounts[n]&&state.TargetHP==state.TargetLength,"Original425 stock totals and source return HP");
                }
            });
            check("Copied map yard retains escort selection and restores the original roster and coordinates",delegate
            {
                var map=startup(731);
                foreach(int target in new[]{30,31,34})
                {
                    var initial=PracticeMapYardState.Create(programs,map,target,2);byte[] before=initial.CopyShipPool(),roster=initial.CopyFleetPool();
                    var state=initial.Enter();Need(state.SelectedUnit!=target&&state.SelectedFleet==initial.OriginFleet,"Entry did not select retained escort");
                    for(int call=0;call<20;call++)state=state.WorkerStep();
                    var after=state.CopyShipPool();Need(BitConverter.ToInt16(after,target*268+8)==BitConverter.ToInt16(before,target*268+8)&&BitConverter.ToInt16(after,target*268+10)==BitConverter.ToInt16(before,target*268+10),"Source coordinates not restored");
                    var restored=state.CopyFleetPool();for(int m=0;m<6;m++)Need(BitConverter.ToUInt32(roster,initial.OriginFleet*492+16+m*4)==BitConverter.ToUInt32(restored,initial.OriginFleet*492+16+m*4),"Original source roster not restored");
                    Need(state.SelectedUnit==target&&state.SelectedFleet==initial.OriginFleet,"Source selection not restored");
                }
            });
            check("Copied map yard preserves predecessor states startup snapshots and returned copies",delegate
            {
                var map=startup(731);string parent=Hash(map.Stages[3].CopyShipPool())+Hash(map.Stages[3].CopyFleetPool());
                var initial=PracticeMapYardState.Create(programs,map,30,1);string before=Hash(initial.CopyShipPool())+Hash(initial.CopyFleetPool());
                var entered=initial.Enter();string enteredHash=Hash(entered.CopyShipPool())+Hash(entered.CopyFleetPool());var next=entered.WorkerStep();
                byte[] copy=next.CopyShipPool();copy[0]^=255;var fleet=next.CopyFleetPool();fleet[0]^=255;
                Need(!initial.Entered&&initial.WorkerCalls==0&&Hash(initial.CopyShipPool())+Hash(initial.CopyFleetPool())==before,"Entry changed predecessor");
                Need(entered.WorkerCalls==0&&Hash(entered.CopyShipPool())+Hash(entered.CopyFleetPool())==enteredHash,"Worker changed predecessor");
                Need(next.CopyShipPool()[0]!=copy[0]&&next.CopyFleetPool()[0]!=fleet[0]&&Hash(map.Stages[3].CopyShipPool())+Hash(map.Stages[3].CopyFleetPool())==parent,"Mutable copy/parent isolation");
            });
            check("Copied map yard rejects unsupported seeds profiles and command order transactionally",delegate
            {
                var map=startup(731);var initial=PracticeMapYardState.Create(programs,map,30,1);string before=Hash(initial.CopyShipPool())+Hash(initial.CopyFleetPool());
                Need(Reject(delegate{PracticeMapYardState.Create(programs,null,30,1);})&&Reject(delegate{PracticeMapYardState.Create(programs,startup(0),30,1);}),"Unadmitted seed/null startup");
                foreach(int target in new[]{-1,0,29,32,1500})Need(Reject(delegate{PracticeMapYardState.Create(programs,map,target,1);}),"Unadmitted target");
                foreach(int profile in new[]{-1,3})Need(Reject(delegate{PracticeMapYardState.Create(programs,map,30,profile);}),"Unadmitted profile");
                Need(Reject(delegate{initial.WorkerStep();})&&!initial.Entered&&Hash(initial.CopyShipPool())+Hash(initial.CopyFleetPool())==before,"Worker before entry changed copy");
                var entered=initial.Enter();string after=Hash(entered.CopyShipPool())+Hash(entered.CopyFleetPool());
                Need(Reject(delegate{entered.Enter();})&&Hash(entered.CopyShipPool())+Hash(entered.CopyFleetPool())==after,"Repeated command changed copy");
            });
            evidence("Copied map yard matches all 198 original425 pool selection and request observations",delegate
            {
                var map=startup(731);PracticeMapYardState state=null;int cases=0,observations=0,expectedStep=0;
                foreach(string line in File.ReadLines(Path.Combine(Path.GetDirectoryName(assets),"Evidence","Map-Yard-425.csv")))
                {
                    if(line.StartsWith("Target,"))continue;var row=line.Split(',');Need(row.Length==15,"Original425 history columns");
                    int target=int.Parse(row[0]),profile=int.Parse(row[1]),step=int.Parse(row[2]);
                    if(step==0){Need(state==null||expectedStep==22,"Incomplete original425 case");state=PracticeMapYardState.Create(programs,map,target,profile);expectedStep=0;cases++;}
                    Need(step==expectedStep&&state.TargetUnit==target&&state.Profile==profile,"Original425 case/step order");
                    if(step==1)state=state.Enter();else if(step>1)state=state.WorkerStep();
                    Need(Hash(state.CopyShipPool())==row[13]&&Hash(state.CopyFleetPool())==row[14]&&state.SelectedFleet==int.Parse(row[8])&&state.SelectedUnit==int.Parse(row[9])&&state.TextRequests==int.Parse(row[10])&&state.MapRequests==int.Parse(row[11])&&state.RefreshRequests==int.Parse(row[12]),"Original425 whole history "+target+"/"+profile+"/"+step);
                    observations++;expectedStep++;
                }
                Need(cases==9&&observations==198&&expectedStep==22,"Original425 history completeness");
            });
        }
    }
}
