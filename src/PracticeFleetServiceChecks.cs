using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeFleetServiceChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}catch(NotSupportedException){return true;}}
        static int Parent(PracticeFleetService state,int unit){return BitConverter.ToInt16(state.CopyShipPool(),unit*268+12);}
        static int Ammo(PracticeFleetService state,int unit){var s=state.CopyShipPool();int total=0;for(int n=0;n<35;n++)total+=BitConverter.ToInt16(s,unit*268+196+n*2);return total;}
        // Independent original482 snapshots:case,kind,beforeS/F/T,initialTaskS/F/T,
        // after20S/F/T. Factory tags reproduce supplied inputs, not source meanings.
        static readonly string[] Original={
            "30-1-ordinary,11,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,FDE14B2BE34F326919E1EC67A4A903D610904EA74313F1A5E79796DB8322FBB6,3B0C48AEFE3CF0F0FB1C11FC53CA8E0FFFCD2AD1B99B161B15F027B3DD8903D4,68C7C027D33533F9416B94D381596CFFD4453E2FE0BFDACF819207971A749A13,F71034F2DD5E072280DBDC762F26985CB5C5798AF2C5CB8CE1F403BEF4F25A59,FDF11566BB35B4EF82C14842CE09CD388B75493C08B107664CCEB8871D42519A,3E75F51B2A5DEA268396673B6CE1876B283210881809F544B17D0401E093D515,640D7EE6BFC7F887D1FF85F1222CE2C8F7D43B8EAC102EE7EC1C65DDEBD37D89,41D5A2132ED1DEF9DCDB914D0D166188559E00CD87813E74E78571835816BF91",
            "30-3-bothammo,11,5C04A8500A9F609F386FBCC0DAE85994DF1DE3C41D27E25BEFD8EF02431AD6A6,80D625895D59739925D5F45C4E18421F035E1D9EB658401D2CEA4DD245C62B78,3B0C48AEFE3CF0F0FB1C11FC53CA8E0FFFCD2AD1B99B161B15F027B3DD8903D4,CB90D280CDC68C19AF23C896ED219F23684A940970B1F22B9C2AE123AAE95882,B3C922388C75777522841A3EDB762CBD19D828D96C765CB8B10C5142086D7999,FDF11566BB35B4EF82C14842CE09CD388B75493C08B107664CCEB8871D42519A,D2E6DF7BCBE95D48EE54120018747015B9C70E82D1C846B4090C6F31094DDB3A,C01A35539F99542D24BC759E9F2EEF2F871D3C84827BD0FA913F193677BEC4C4,FDF11566BB35B4EF82C14842CE09CD388B75493C08B107664CCEB8871D42519A",
            "30-2-firstworse,11,EB408400F4BC4701325AEBB6EF7896A5AE0BA7B6CD603BD08CE8B85F07FAF9DE,A2E51EAF9F010B8917FCBF42762AE674FE97776CB5676DE0EDC8105B621DFBAC,3B0C48AEFE3CF0F0FB1C11FC53CA8E0FFFCD2AD1B99B161B15F027B3DD8903D4,D0C5B3187F33D3CE2EA43A8B4BDDD048D8DFC03AC1A72D7F45245B06D0277500,7A1270387CB31E491043D8AB9F6FAC2D24A46CF6B64733C9AFD672EDDE030793,FDF11566BB35B4EF82C14842CE09CD388B75493C08B107664CCEB8871D42519A,D2E6DF7BCBE95D48EE54120018747015B9C70E82D1C846B4090C6F31094DDB3A,C01A35539F99542D24BC759E9F2EEF2F871D3C84827BD0FA913F193677BEC4C4,FDF11566BB35B4EF82C14842CE09CD388B75493C08B107664CCEB8871D42519A",
            "30-1-blocked,11,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,CE199D97FB5F582F1569C1B6ED040A4BBB6BF54B74A04769FEB229E83DD1B8C4,3B0C48AEFE3CF0F0FB1C11FC53CA8E0FFFCD2AD1B99B161B15F027B3DD8903D4,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,CE199D97FB5F582F1569C1B6ED040A4BBB6BF54B74A04769FEB229E83DD1B8C4,3B0C48AEFE3CF0F0FB1C11FC53CA8E0FFFCD2AD1B99B161B15F027B3DD8903D4,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,CE199D97FB5F582F1569C1B6ED040A4BBB6BF54B74A04769FEB229E83DD1B8C4,3B0C48AEFE3CF0F0FB1C11FC53CA8E0FFFCD2AD1B99B161B15F027B3DD8903D4",
            "30-3-blocked,12,AC4D917BC3F008557916A3B529D84DC22FB47E220D5C435ADB4CA316C8053E38,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,1CCC43986FCAD24E0F1D189DFDCB29CC2F4C990D6D431DC59A6B3C4FECE46F48,AC4D917BC3F008557916A3B529D84DC22FB47E220D5C435ADB4CA316C8053E38,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,BB0EF50ECEF2E9E39DA9B47DAD63796A19B86FB86AD5DB6618B7C3F0991BFB0D,AC4D917BC3F008557916A3B529D84DC22FB47E220D5C435ADB4CA316C8053E38,8C9975E865D0A29C3514B07652CD22D76A467C670423610382E048FB9D0EEE2D,BB0EF50ECEF2E9E39DA9B47DAD63796A19B86FB86AD5DB6618B7C3F0991BFB0D",
            "30-0-hpzero,12,58EB336CCAC5827908699D87A297EEF3C9371E942647EDC50315C185816F07C2,80D625895D59739925D5F45C4E18421F035E1D9EB658401D2CEA4DD245C62B78,1CCC43986FCAD24E0F1D189DFDCB29CC2F4C990D6D431DC59A6B3C4FECE46F48,58EB336CCAC5827908699D87A297EEF3C9371E942647EDC50315C185816F07C2,80D625895D59739925D5F45C4E18421F035E1D9EB658401D2CEA4DD245C62B78,BB0EF50ECEF2E9E39DA9B47DAD63796A19B86FB86AD5DB6618B7C3F0991BFB0D,58EB336CCAC5827908699D87A297EEF3C9371E942647EDC50315C185816F07C2,80D625895D59739925D5F45C4E18421F035E1D9EB658401D2CEA4DD245C62B78,BB0EF50ECEF2E9E39DA9B47DAD63796A19B86FB86AD5DB6618B7C3F0991BFB0D",
            "30-0-state3,12,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,0F3072A21DCB285D1E5F20D55417AD350ED658500337BA946E9F29A7B4A82694,1CCC43986FCAD24E0F1D189DFDCB29CC2F4C990D6D431DC59A6B3C4FECE46F48,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,0F3072A21DCB285D1E5F20D55417AD350ED658500337BA946E9F29A7B4A82694,1CCC43986FCAD24E0F1D189DFDCB29CC2F4C990D6D431DC59A6B3C4FECE46F48,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,0F3072A21DCB285D1E5F20D55417AD350ED658500337BA946E9F29A7B4A82694,1CCC43986FCAD24E0F1D189DFDCB29CC2F4C990D6D431DC59A6B3C4FECE46F48",
        };
        static void Pools(PracticeFleetService state,string[] row,int start)
        {Need(Hash(state.CopyShipPool())==row[start]&&Hash(state.CopyFleetPool())==row[start+1]&&Hash(state.CopyTaskRecord())==row[start+2],"Original482 complete pool/task snapshot "+row[0]+"/"+start);}
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var input=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));
            Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,input,731);};
            check("Fleet service matches seven complete original482 tagged queue and pool goldens",delegate
            {
                var map=startup();foreach(string line in Original){var row=line.Split(',');var state=PracticeFleetService.Create(programs,map,row[0],int.Parse(row[1]));Pools(state,row,2);state=state.VisitTask();Pools(state,row,5);state=state.Advance(20);Pools(state,row,8);Need(state.TaskVisits==21&&state.WorkerPasses==20,"Bounded service diagnostic counts");}
            });
            check("Service damage command selects last positive grid damage rather than worst HP",delegate
            {
                var state=PracticeFleetService.Create(programs,startup(),"30-2-firstworse",11);var before=state.CopyShipPool();Need(BitConverter.ToInt16(before,30*268+16)==0&&BitConverter.ToInt16(before,31*268+16)==2,"Original482 first-worse fixture");
                state=state.VisitTask();Need(state.TaskKind==12&&Parent(state,30)==16&&Parent(state,31)==2&&state.SelectedUnit==30,"Last damaged member admission");state=state.StepWorkerAndTask();Need(Parent(state,30)==3&&Parent(state,31)==2&&state.SourceKind==0&&state.SourceState==3,"Next task visit admits remaining damaged member");
            });
            check("Service ammo path admits two ships and preserves observed shared-stock shortage",delegate
            {
                var state=PracticeFleetService.Create(programs,startup(),"30-3-bothammo",11).VisitTask();Need(state.SourceKind==0&&Parent(state,30)==3&&Parent(state,31)==2&&state.SelectedFleet==2&&state.SelectedUnit==31,"Original482 simultaneous ammo admission/selection");
                state=state.Advance(20);Need(state.TaskKind==12&&state.SourceState==3&&Parent(state,30)==3&&Parent(state,31)==16&&Ammo(state,30)==130&&Ammo(state,31)==162&&Ammo(state,2)==194,"Held-contact manual driver re-admission/stock outcome");
                Need(Ammo(state,30)+Ammo(state,31)+Ammo(state,2)==486,"Copied shared ammunition conservation");
            });
            check("Service task push and double pop retain tagged payloads and distinct waypoint effects",delegate
            {
                var map=startup();foreach(int kind in new[]{11,12})
                {
                    var initial=PracticeFleetService.Create(programs,map,"30-0-ordinary",kind);var before=initial.CopyTaskRecord();var state=initial.VisitTask();var after=state.CopyTaskRecord();
                    Need(state.QueueEmpty&&BitConverter.ToInt32(after,4)==(kind==11?1011:1021)&&BitConverter.ToInt32(after,16)==1&&BitConverter.ToInt32(after,0x134)==-1&&BitConverter.ToInt32(after,0xc8)==0,"Original482 tagged queue pop");
                    Need(state.SelectedWaypoint==(kind==11?-1:3)&&state.RouteUiRequests==(kind==11?1:0)&&BitConverter.ToUInt32(after,0xcc)==BitConverter.ToUInt32(before,0xcc),"Push route clear versus direct pop");
                    for(int n=0xd0;n<0x134;n++)Need(after[n]==before[n],"Opaque task tail changed");
                }
            });
            check("Fleet service copies preserve startup predecessors and returned views",delegate
            {
                var map=startup();string parent=Hash(map.Stages[3].CopyShipPool())+Hash(map.Stages[3].CopyFleetPool());var initial=PracticeFleetService.Create(programs,map,"30-1-ordinary",11);string before=Hash(initial.CopyShipPool())+Hash(initial.CopyFleetPool())+Hash(initial.CopyTaskRecord());var next=initial.VisitTask().StepWorkerAndTask();
                var ships=next.CopyShipPool();var fleets=next.CopyFleetPool();var task=next.CopyTaskRecord();var units=next.CopyOriginalUnits();ships[0]^=255;fleets[0]^=255;task[0]^=255;units[0]=-1;
                Need(next.CopyShipPool()[0]!=ships[0]&&next.CopyFleetPool()[0]!=fleets[0]&&next.CopyTaskRecord()[0]!=task[0]&&next.CopyOriginalUnits()[0]!=-1,"Writable result view escaped");
                Need(initial.TaskVisits==0&&Hash(initial.CopyShipPool())+Hash(initial.CopyFleetPool())+Hash(initial.CopyTaskRecord())==before&&Hash(map.Stages[3].CopyShipPool())+Hash(map.Stages[3].CopyFleetPool())==parent,"Service changed parent/predecessor");
            });
            check("Fleet service rejects unsupported profiles order and update windows transactionally",delegate
            {
                var map=startup();Need(PracticeFleetService.CaseIds.Count==19&&Reject(delegate{PracticeFleetService.Create(programs,null,"30-1-ordinary",11);})&&Reject(delegate{PracticeFleetService.Create(programs,map,"unknown",11);})&&Reject(delegate{PracticeFleetService.Create(programs,map,"30-1-ordinary",10);})&&Reject(delegate{PracticeFleetService.Create(programs,PracticeMapStartup.CreateBssb202(programs,input,0),"30-1-ordinary",11);}),"Unadmitted factory profile");
                var initial=PracticeFleetService.Create(programs,map,"30-1-ordinary",11);Need(Reject(delegate{initial.StepWorkerAndTask();})&&Object.ReferenceEquals(initial,initial.Advance(0)),"Initial command order");var state=initial.VisitTask();string before=Hash(state.CopyFleetPool());Need(Reject(delegate{state.VisitTask();})&&Reject(delegate{state.Advance(-1);})&&Reject(delegate{state.Advance(21);})&&Hash(state.CopyFleetPool())==before,"Rejected action changed copy");state=state.Advance(20);Need(Reject(delegate{state.StepWorkerAndTask();})&&state.WorkerPasses==20,"Study update bound");
            });
            evidence("Fleet service matches all 836 original482 pool task and selection observations",delegate
            {
                var map=startup();PracticeFleetService state=null;var seen=new HashSet<string>();int count=0,expectedStep=0;
                foreach(string line in File.ReadLines(Path.Combine(Path.GetDirectoryName(assets),"Evidence","Fleet-Service-482.csv")))
                {
                    if(line.StartsWith("CaseId,"))continue;var row=line.Split(',');Need(row.Length==23,"Original482 history columns");int initialKind=int.Parse(row[1]),step=int.Parse(row[2]);
                    if(step==0){Need((state==null||expectedStep==22)&&seen.Add(row[0]+"/"+initialKind),"Incomplete/duplicate original482 history");state=PracticeFleetService.Create(programs,map,row[0],initialKind);expectedStep=0;}
                    Need(step==expectedStep&&state.CaseId==row[0]&&state.InitialTaskKind==initialKind,"Original482 case/step order");if(step==1)state=state.VisitTask();else if(step>1)state=state.StepWorkerAndTask();
                    Need(Hash(state.CopyShipPool())==row[18]&&Hash(state.CopyFleetPool())==row[19]&&Hash(state.CopyTaskRecord())==row[20]&&state.TaskKind==int.Parse(row[4])&&state.SelectedFleet==int.Parse(row[7])&&state.SelectedUnit==int.Parse(row[8])&&state.TextRequests==int.Parse(row[15])&&state.MapRequests==int.Parse(row[16])&&state.RefreshRequests==int.Parse(row[17])&&state.SelectedWaypoint==int.Parse(row[21])&&state.RouteUiRequests==int.Parse(row[22]),"Whole original482 observation "+row[0]+"/"+initialKind+"/"+step);
                    count++;expectedStep++;
                }
                Need(seen.Count==38&&count==836&&expectedStep==22,"Original482 history completeness");
            });
        }
    }
}
