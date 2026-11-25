using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeFleetServicePhaseChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}catch(NotSupportedException){return true;}}
        static int Parent(PracticeFleetServicePhaseClock c,int unit){return BitConverter.ToInt16(c.CopyShipPool(),unit*268+12);}
        static string Data(PracticeFleetServicePhaseClock c){return Hash(c.CopyShipPool())+Hash(c.CopyFleetPool())+Hash(c.CopyTaskRecord());}
        // Six independent original511 final snapshots and scalar context.
        static readonly string[] Original={
            "30-1-ordinary,11,0,0,384,0,12,6,0,12,3E75F51B2A5DEA268396673B6CE1876B283210881809F544B17D0401E093D515,640D7EE6BFC7F887D1FF85F1222CE2C8F7D43B8EAC102EE7EC1C65DDEBD37D89,916186F0B176E25D6EAC74DCE02857840799F2D92F7B8000A3D25FEFC9B7928D",
            "30-1-ordinary,12,0,0,384,0,12,6,0,1,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,FDE14B2BE34F326919E1EC67A4A903D610904EA74313F1A5E79796DB8322FBB6,A91FBBDAFC0011DCFFA86BEE75F27657C91A5F9FF3CC113E2531C71765C184B5",
            "30-2-firstworse,11,31,1,399,16,12,6,12,13,7D17977EE08DCA480CB78069F797BA19BCEB302A8D187CA9C876113CAEDF8553,9CB16DFBDFF13DFF6287E0A74095E981459E21F9D0528FC7ECF61F8246012D4C,D50EC2997F06F09AE88EBF445A107F706D3322B2176DFFACB17F610B25BAB4D7",
            "30-1-blocked,11,12,1,380,16,11,6,11,1,1784F46F3520EE6C0F808540B1AE0FC2F8A140A73E784C4C5244B8C64FA491F8,CE199D97FB5F582F1569C1B6ED040A4BBB6BF54B74A04769FEB229E83DD1B8C4,C3B0086CFE47DE189C32AD47D5736956D256009A8671F4D2E0E62C06C9681EA8",
            "30-1-ordinary,11,4294967294,1,366,16,12,6,0,11,3E75F51B2A5DEA268396673B6CE1876B283210881809F544B17D0401E093D515,640D7EE6BFC7F887D1FF85F1222CE2C8F7D43B8EAC102EE7EC1C65DDEBD37D89,4A192028A9885EE22EA6069DB6CAD62D55E6DE0C8288C04DA0DDA0D1A3604420",
            "30-1-ordinary,11,12,0,396,0,12,6,0,12,3E75F51B2A5DEA268396673B6CE1876B283210881809F544B17D0401E093D515,640D7EE6BFC7F887D1FF85F1222CE2C8F7D43B8EAC102EE7EC1C65DDEBD37D89,916186F0B176E25D6EAC74DCE02857840799F2D92F7B8000A3D25FEFC9B7928D"
        };
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            Func<PracticeMapStartup> map=delegate{return PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);};
            check("Service clock separates pre-admission support return and queue completion",delegate
            {
                var c=PracticeFleetServicePhaseClock.Create(programs,map(),"30-1-ordinary",11,0);string before=Data(c);c=c.AdvanceMany(13,false);Need(c.PhaseCounter==13&&c.WorkerCalls==1&&c.TaskVisits==0&&Parent(c,30)==16&&Data(c)==before,"Original511 first worker has no admitted target");
                c=c.AdvanceMany(19,false);Need(c.PhaseCounter==32&&c.TaskVisits==1&&c.WorkerCalls==1&&Parent(c,30)==3,"Dispatcher admission at32");c=c.AdvanceMany(301,false);Need(c.PhaseCounter==333&&c.TaskKind==12&&c.WorkerCalls==6&&Parent(c,30)==16,"Source return before task completion");
                c=c.AdvanceMany(19,false);Need(c.PhaseCounter==352&&c.TaskKind==0&&c.TaskVisits==11&&c.RouteUiRequests==12,"Task completion at352");c=c.AdvanceMany(32,false);Need(c.TaskVisits==12&&c.WorkerCalls==6&&c.Polls==384,"Queue0 incorrectly stopped scheduled visits");
            });
            check("Service phase clock matches six complete original511 final snapshots",delegate
            {
                var startup=map();foreach(string line in Original)
                {
                    var row=line.Split(',');var c=PracticeFleetServicePhaseClock.Create(programs,startup,row[0],int.Parse(row[1]),uint.Parse(row[2]));int held13=0,held32=0;
                    for(int step=1;step<=384;step++)
                    {
                        bool pause=false;if(row[3]=="1"){if(step<=8)pause=true;else if((c.PhaseCounter&63)==13&&held13<4){held13++;pause=true;}else if((c.PhaseCounter&31)==0&&held32<4){held32++;pause=true;}}c=c.Advance(pause);
                    }
                    Need(c.PhaseCounter==uint.Parse(row[4])&&c.PausedPolls==int.Parse(row[5])&&c.TaskVisits==int.Parse(row[6])&&c.WorkerCalls==int.Parse(row[7])&&c.TaskKind==int.Parse(row[8])&&c.RouteUiRequests==int.Parse(row[9])&&Hash(c.CopyShipPool())==row[10]&&Hash(c.CopyFleetPool())==row[11]&&Hash(c.CopyTaskRecord())==row[12],"Original511 complete endpoint "+line);
                }
            });
            check("Paused service phases hold data without replaying the current phase",delegate
            {
                var c=PracticeFleetServicePhaseClock.Create(programs,map(),"30-1-ordinary",11,0).AdvanceMany(13,false);string before=Data(c);var held=c.AdvanceMany(4,true);Need(held.PhaseCounter==13&&held.WorkerCalls==1&&held.PausedPolls==4&&held.Polls==17&&Data(held)==before&&held.NextWorkerIn==64,"Paused support phase repeated");
                c=held.AdvanceMany(19,false);before=Data(c);held=c.AdvanceMany(4,true);Need(held.PhaseCounter==32&&held.TaskVisits==1&&held.WorkerCalls==1&&Data(held)==before&&held.NextDispatcherIn==32&&held.NextWorkerIn==45,"Paused task phase repeated");
            });
            check("Service phase histories are independent of caller batch partitions",delegate
            {
                var startup=map();foreach(string id in PracticeFleetServicePhaseClock.CaseIds)foreach(uint counter in PracticeFleetServicePhaseClock.InitialCounters)foreach(int kind in new[]{11,12})
                {
                    var first=PracticeFleetServicePhaseClock.Create(programs,startup,id,kind,counter);var batched=first.AdvanceMany(384,false);var singles=first;for(int n=0;n<384;n++)singles=singles.Advance(false);var mixed=first;foreach(int count in new[]{13,19,45,64,128,64,51})mixed=mixed.AdvanceMany(count,false);
                    Need(Data(batched)==Data(singles)&&Data(mixed)==Data(singles)&&batched.PhaseCounter==mixed.PhaseCounter&&mixed.Polls==384&&mixed.TaskVisits==singles.TaskVisits&&mixed.WorkerCalls==singles.WorkerCalls,"Batch partition changed source history "+id+"/"+counter+"/"+kind);
                }
            });
            check("Service clock wraps unsigned counter and keeps predecessors immutable",delegate
            {
                var first=PracticeFleetServicePhaseClock.Create(programs,map(),"30-1-ordinary",11,0xfffffffeu);string initial=Data(first);var c=first.Advance(false);Need(c.PhaseCounter==uint.MaxValue&&c.TaskVisits==0,"Counter before wrap");c=c.Advance(false);Need(c.PhaseCounter==0&&c.TaskVisits==1&&Parent(c,30)==3,"Counter wrap task gate");var copy=c.CopyTaskRecord();copy[260]^=255;var ships=c.CopyShipPool();ships[0]^=255;var units=c.CopyOriginalUnits();units[0]=-1;
                Need(c.CopyTaskRecord()[260]!=copy[260]&&c.CopyShipPool()[0]!=ships[0]&&c.CopyOriginalUnits()[0]!=-1&&first.Polls==0&&Data(first)==initial,"Clock view or predecessor escaped");for(int n=260;n<296;n++)Need(c.CopyTaskRecord()[n]==0x6d,"Opaque context changed");
            });
            check("Service clock factory and captured window reject unsupported requests",delegate
            {
                var startup=map();Need(Reject(delegate{PracticeFleetServicePhaseClock.Create(programs,startup,"31-1-ordinary",11,0);})&&Reject(delegate{PracticeFleetServicePhaseClock.Create(programs,startup,"30-1-ordinary",11,1);})&&Reject(delegate{PracticeFleetServicePhaseClock.Create(programs,startup,"30-1-ordinary",10,0);})&&Reject(delegate{PracticeFleetServicePhaseClock.Create(programs,null,"30-1-ordinary",11,0);}),"Unsupported clock input");
                var first=PracticeFleetServicePhaseClock.Create(programs,startup,"30-1-ordinary",11,0);string before=Data(first);Need(Reject(delegate{first.AdvanceMany(-1,false);})&&Reject(delegate{first.AdvanceMany(385,false);})&&Object.ReferenceEquals(first,first.AdvanceMany(0,true))&&Data(first)==before,"Rejected clock action mutated state");var end=first.AdvanceMany(384,true);Need(end.PhaseCounter==0&&end.PausedPolls==384&&Data(end)==before&&Reject(delegate{end.Advance(false);})&&Reject(delegate{end.Advance(true);}),"Paused window boundary");
            });
            evidence("Service clock matches all18480 original511 phase pool and context observations",delegate
            {
                var startup=map();PracticeFleetServicePhaseClock c=null;var seen=new HashSet<string>();int expected=0,count=0,pauseProfile=0;
                foreach(string line in File.ReadLines(Path.Combine(Path.GetDirectoryName(assets),"Evidence","Service-Phase-511.csv")))
                {
                    if(line.StartsWith("CaseId,"))continue;var r=line.Split(',');Need(r.Length==21,"Original511 columns");int step=int.Parse(r[4]);
                    if(step==0){Need((c==null||expected==385)&&seen.Add(r[0]+"/"+r[1]+"/"+r[2]+"/"+r[3]),"Original511 boundary/duplicate");c=PracticeFleetServicePhaseClock.Create(programs,startup,r[0],int.Parse(r[1]),uint.Parse(r[2]));pauseProfile=int.Parse(r[3]);expected=0;}
                    Need(step==expected&&c.CaseId==r[0]&&c.InitialTaskKind==int.Parse(r[1])&&c.InitialCounter==uint.Parse(r[2]),"Original511 order");if(step>0)c=c.Advance(r[5]=="1");
                    Need(c.PhaseCounter==uint.Parse(r[6])&&c.PausedPolls==int.Parse(r[7])&&c.TaskVisits==int.Parse(r[8])&&c.WorkerCalls==int.Parse(r[9])&&c.TaskKind==int.Parse(r[10])&&c.SelectedFleet==int.Parse(r[11])&&c.SelectedUnit==int.Parse(r[12])&&c.SelectedWaypoint==int.Parse(r[13])&&c.TextRequests==int.Parse(r[14])&&c.MapRequests==int.Parse(r[15])&&c.RefreshRequests==int.Parse(r[16])&&c.RouteUiRequests==int.Parse(r[17])&&Hash(c.CopyShipPool())==r[18]&&Hash(c.CopyFleetPool())==r[19]&&Hash(c.CopyTaskRecord())==r[20],"Original511 full observation "+r[0]+"/"+r[1]+"/"+r[2]+"/"+step);expected++;count++;
                    if(step==384)Need(c.PausedPolls==(pauseProfile==0?0:16),"Original511 pause fixture count");
                }
                Need(seen.Count==48&&count==18480&&expected==385,"Original511 completeness");
            });
        }
    }
}
