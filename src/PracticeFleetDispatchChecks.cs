using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeFleetDispatchChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(value)).Replace("-","");}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}catch(NotSupportedException){return true;}}
        static int Parent(PracticeFleetService state,int unit){return BitConverter.ToInt16(state.CopyShipPool(),unit*268+12);}
        // Native496 complete before/first-visit/after20 S/F/T hashes. These
        // are independent captured source outputs, not generated model values.
        static readonly string[] Original={
"30-1-ordinary,11,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,FDE14B2BE34F326919E1EC67A4A903D610904EA74313F1A5E79796DB8322FBB6,C3B0086CFE47DE189C32AD47D5736956D256009A8671F4D2E0E62C06C9681EA8,68C7C027D33533F9416B94D381596CFFD4453E2FE0BFDACF819207971A749A13,F71034F2DD5E072280DBDC762F26985CB5C5798AF2C5CB8CE1F403BEF4F25A59,47B164996BB51F92DDE14F43536C14BC650D5616782D35008CDB4BC051E7E255,3E75F51B2A5DEA268396673B6CE1876B283210881809F544B17D0401E093D515,640D7EE6BFC7F887D1FF85F1222CE2C8F7D43B8EAC102EE7EC1C65DDEBD37D89,A91FBBDAFC0011DCFFA86BEE75F27657C91A5F9FF3CC113E2531C71765C184B5",
"30-1-ordinary,12,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,FDE14B2BE34F326919E1EC67A4A903D610904EA74313F1A5E79796DB8322FBB6,354334B6AD79C4B072FDC72713C9A13DB9B642D2049C49CFF848FEE482E802B6,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,FDE14B2BE34F326919E1EC67A4A903D610904EA74313F1A5E79796DB8322FBB6,B8C7F18E0D5AF5C39C7B3654CAA3880ED1029A00FBCBB41AE016BCAE907E14D9,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,FDE14B2BE34F326919E1EC67A4A903D610904EA74313F1A5E79796DB8322FBB6,A91FBBDAFC0011DCFFA86BEE75F27657C91A5F9FF3CC113E2531C71765C184B5",
"30-2-firstworse,11,EB408400F4BC4701325AEBB6EF7896A5AE0BA7B6CD603BD08CE8B85F07FAF9DE,A2E51EAF9F010B8917FCBF42762AE674FE97776CB5676DE0EDC8105B621DFBAC,C3B0086CFE47DE189C32AD47D5736956D256009A8671F4D2E0E62C06C9681EA8,D0C5B3187F33D3CE2EA43A8B4BDDD048D8DFC03AC1A72D7F45245B06D0277500,7A1270387CB31E491043D8AB9F6FAC2D24A46CF6B64733C9AFD672EDDE030793,47B164996BB51F92DDE14F43536C14BC650D5616782D35008CDB4BC051E7E255,D2E6DF7BCBE95D48EE54120018747015B9C70E82D1C846B4090C6F31094DDB3A,C01A35539F99542D24BC759E9F2EEF2F871D3C84827BD0FA913F193677BEC4C4,D50EC2997F06F09AE88EBF445A107F706D3322B2176DFFACB17F610B25BAB4D7",
"30-3-bothammo,11,5C04A8500A9F609F386FBCC0DAE85994DF1DE3C41D27E25BEFD8EF02431AD6A6,80D625895D59739925D5F45C4E18421F035E1D9EB658401D2CEA4DD245C62B78,C3B0086CFE47DE189C32AD47D5736956D256009A8671F4D2E0E62C06C9681EA8,CB90D280CDC68C19AF23C896ED219F23684A940970B1F22B9C2AE123AAE95882,B3C922388C75777522841A3EDB762CBD19D828D96C765CB8B10C5142086D7999,47B164996BB51F92DDE14F43536C14BC650D5616782D35008CDB4BC051E7E255,D2E6DF7BCBE95D48EE54120018747015B9C70E82D1C846B4090C6F31094DDB3A,C01A35539F99542D24BC759E9F2EEF2F871D3C84827BD0FA913F193677BEC4C4,D50EC2997F06F09AE88EBF445A107F706D3322B2176DFFACB17F610B25BAB4D7",
"30-1-blocked,11,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,CE199D97FB5F582F1569C1B6ED040A4BBB6BF54B74A04769FEB229E83DD1B8C4,C3B0086CFE47DE189C32AD47D5736956D256009A8671F4D2E0E62C06C9681EA8,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,CE199D97FB5F582F1569C1B6ED040A4BBB6BF54B74A04769FEB229E83DD1B8C4,C3B0086CFE47DE189C32AD47D5736956D256009A8671F4D2E0E62C06C9681EA8,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,CE199D97FB5F582F1569C1B6ED040A4BBB6BF54B74A04769FEB229E83DD1B8C4,C3B0086CFE47DE189C32AD47D5736956D256009A8671F4D2E0E62C06C9681EA8",
"30-0-ordinary,11,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,80D625895D59739925D5F45C4E18421F035E1D9EB658401D2CEA4DD245C62B78,C3B0086CFE47DE189C32AD47D5736956D256009A8671F4D2E0E62C06C9681EA8,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,80D625895D59739925D5F45C4E18421F035E1D9EB658401D2CEA4DD245C62B78,1C4DFFB1CB2DF1077CB4941F3660F90E04760E1FC4A6B2B04AD6C4ECA6E50D75,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,80D625895D59739925D5F45C4E18421F035E1D9EB658401D2CEA4DD245C62B78,A91FBBDAFC0011DCFFA86BEE75F27657C91A5F9FF3CC113E2531C71765C184B5",
"30-0-state3,12,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,0F3072A21DCB285D1E5F20D55417AD350ED658500337BA946E9F29A7B4A82694,354334B6AD79C4B072FDC72713C9A13DB9B642D2049C49CFF848FEE482E802B6,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,0F3072A21DCB285D1E5F20D55417AD350ED658500337BA946E9F29A7B4A82694,B8C7F18E0D5AF5C39C7B3654CAA3880ED1029A00FBCBB41AE016BCAE907E14D9,84847AABB23C550EA7C58654748B2ECCBA896D725CC27D53D98D643E8B725E49,0F3072A21DCB285D1E5F20D55417AD350ED658500337BA946E9F29A7B4A82694,A91FBBDAFC0011DCFFA86BEE75F27657C91A5F9FF3CC113E2531C71765C184B5",

        };
        static void Pools(PracticeFleetService state,string[] row,int first)
        {Need(Hash(state.CopyShipPool())==row[first]&&Hash(state.CopyFleetPool())==row[first+1]&&Hash(state.CopyTaskRecord())==row[first+2],"Native496 pools/context "+row[0]+"/"+row[1]+"/"+first);}
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);};
            check("Initialized service context matches original492 selected records",delegate
            {
                Need(Hash(PracticeFleetServiceContext.CreateDiagnostic(16))=="D2D96DFF62CD64FAFFD469E8CD27A780F61C32A557A3B4F652AD82ACA63FAB6D"&&Hash(PracticeFleetServiceContext.CreateDiagnostic(18))=="333A87964EDC41EFF1CB35D049D80A5B1237EEFCC7033921B9C815DA2458B837","Original492 initialized contexts");
                Need(Reject(delegate{PracticeFleetServiceContext.CreateDiagnostic(-1);})&&Reject(delegate{PracticeFleetServiceContext.CreateDiagnostic(250);}),"Initializer boundaries");
            });
            check("Initialized dispatcher matches seven original496 complete histories",delegate
            {
                var map=startup();foreach(string line in Original){var row=line.Split(',');var state=PracticeFleetService.Create(programs,map,row[0],int.Parse(row[1]),PracticeFleetServiceMode.InitializedDispatcher);Pools(state,row,2);state=state.VisitTask();Pools(state,row,5);state=state.Advance(20);Pools(state,row,8);Need(state.TaskVisits==21&&state.WorkerPasses==20,"Recorded dispatcher prefix length");}
            });
            check("Friendly contact dispatcher pops lone task12 before service",delegate
            {
                var map=startup();var raw=PracticeFleetService.Create(programs,map,"30-1-ordinary",12,PracticeFleetServiceMode.InitializedSelective);var full=PracticeFleetService.Create(programs,map,"30-1-ordinary",12,PracticeFleetServiceMode.InitializedDispatcher);
                raw=raw.VisitTask();full=full.VisitTask();Need(Parent(raw,30)==3&&raw.TaskKind==12&&Parent(full,30)==16&&full.QueueEmpty,"Original496 lone task12 dispatcher contrast");
                var service=PracticeFleetService.Create(programs,map,"30-1-ordinary",11,PracticeFleetServiceMode.InitializedDispatcher).VisitTask();service=service.Advance(5);
                var selective=PracticeFleetService.Create(programs,map,"30-1-ordinary",11,PracticeFleetServiceMode.InitializedSelective).VisitTask().Advance(5);
                Need(service.QueueEmpty&&service.RouteUiRequests==7&&selective.RouteUiRequests==2&&Hash(service.CopyShipPool())==Hash(selective.CopyShipPool())&&Hash(service.CopyFleetPool())==Hash(selective.CopyFleetPool()),"Native496 same service pools and distinct route requests");
            });
            check("Service modes retain immutable predecessors and diagnostic context views",delegate
            {
                var map=startup();string parent=Hash(map.Stages[3].CopyFleetPool());
                foreach(PracticeFleetServiceMode mode in Enum.GetValues(typeof(PracticeFleetServiceMode)))
                {
                    var initial=PracticeFleetService.Create(programs,map,"30-2-firstworse",11,mode);string before=Hash(initial.CopyTaskRecord())+Hash(initial.CopyShipPool())+Hash(initial.CopyFleetPool());var next=initial.VisitTask().StepWorkerAndTask();var task=next.CopyTaskRecord();task[260]^=255;var ships=next.CopyShipPool();ships[0]^=255;
                    Need(next.Mode==mode&&next.CopyTaskRecord()[260]!=task[260]&&next.CopyShipPool()[0]!=ships[0]&&initial.TaskVisits==0&&before==Hash(initial.CopyTaskRecord())+Hash(initial.CopyShipPool())+Hash(initial.CopyFleetPool())&&parent==Hash(map.Stages[3].CopyFleetPool()),"Mode escaped or predecessor changed");
                }
            });
            check("Service dispatcher mode bounds and command ordering reject transactionally",delegate
            {
                var map=startup();Need(Reject(delegate{PracticeFleetService.Create(programs,map,"30-1-ordinary",11,(PracticeFleetServiceMode)(-1));})&&Reject(delegate{PracticeFleetService.Create(programs,map,"30-1-ordinary",11,(PracticeFleetServiceMode)3);}),"Invalid dispatcher mode admitted");
                var initial=PracticeFleetService.Create(programs,map,"30-1-ordinary",11,PracticeFleetServiceMode.InitializedDispatcher);string before=Hash(initial.CopyTaskRecord());Need(Reject(delegate{initial.StepWorkerAndTask();})&&Reject(delegate{initial.Advance(21);})&&before==Hash(initial.CopyTaskRecord()),"Initial dispatcher rejection changed data");var next=initial.VisitTask().Advance(20);Need(Reject(delegate{next.StepWorkerAndTask();})&&next.WorkerPasses==20,"Dispatcher prefix bound");
            });
            evidence("Initialized service matches all836 original492 observations",delegate{Compare(assets,startup(),PracticeFleetServiceMode.InitializedSelective,"Fleet-Initialized-492.csv");});
            evidence("Service dispatcher matches all836 original496 observations",delegate{Compare(assets,startup(),PracticeFleetServiceMode.InitializedDispatcher,"Fleet-Dispatcher-496.csv");});
        }
        static void Compare(string assets,PracticeMapStartup map,PracticeFleetServiceMode mode,string name)
        {
            string root=Path.GetDirectoryName(assets),programs=Path.Combine(root,"Programs");PracticeFleetService state=null;int expected=0,count=0;var seen=new HashSet<string>();
            foreach(string line in File.ReadLines(Path.Combine(root,"Evidence",name)))
            {
                if(line.StartsWith("CaseId,"))continue;var row=line.Split(',');Need(row.Length==23,"Native dispatcher history columns");int kind=int.Parse(row[1]),step=int.Parse(row[2]);
                if(step==0){Need((state==null||expected==22)&&seen.Add(row[0]+"/"+kind),"Native history boundary/duplicate");state=PracticeFleetService.Create(programs,map,row[0],kind,mode);expected=0;}
                Need(step==expected&&state.CaseId==row[0]&&state.InitialTaskKind==kind,"Native case/order");if(step==1)state=state.VisitTask();else if(step>1)state=state.StepWorkerAndTask();
                Need(Hash(state.CopyShipPool())==row[18]&&Hash(state.CopyFleetPool())==row[19]&&Hash(state.CopyTaskRecord())==row[20]&&state.TaskKind==int.Parse(row[4])&&state.SourceKind==int.Parse(row[5])&&state.SourceState==int.Parse(row[6])&&state.SelectedFleet==int.Parse(row[7])&&state.SelectedUnit==int.Parse(row[8])&&state.TextRequests==int.Parse(row[15])&&state.MapRequests==int.Parse(row[16])&&state.RefreshRequests==int.Parse(row[17])&&state.SelectedWaypoint==int.Parse(row[21])&&state.RouteUiRequests==int.Parse(row[22]),"Original full observation "+mode+" "+row[0]+"/"+kind+"/"+step);expected++;count++;
            }
            Need(seen.Count==38&&count==836&&expected==22,"Native dispatcher histories incomplete");
        }
    }
}
