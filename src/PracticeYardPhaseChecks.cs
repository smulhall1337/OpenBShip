using System;
using System.IO;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeYardPhaseChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}}
        internal static void Run(Action<string,Action> check,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);};
            check("Yard phase clock requires an entered copy and rejects unbounded actions transactionally",delegate
            {
                var ready=PracticeMapYardState.Create(programs,startup(),30,1);Need(Reject(delegate{new PracticeYardPhaseClock(null,12);})&&Reject(delegate{new PracticeYardPhaseClock(ready,12);}),"Unentered/null clock admission");
                var entered=ready.Enter();var clock=new PracticeYardPhaseClock(entered,12);string before=Hash(entered.CopyShipPool())+Hash(entered.CopyFleetPool());
                Need(Reject(delegate{clock.AdvanceMany(-1,false);})&&Reject(delegate{clock.AdvanceMany(4097,false);})&&Object.ReferenceEquals(clock,clock.AdvanceMany(0,false)),"Clock action bound/zero identity");
                Need(clock.PhaseCounter==12&&clock.Polls==0&&Hash(clock.Yard.CopyShipPool())+Hash(clock.Yard.CopyFleetPool())==before&&!ready.Entered,"Rejected clock changed source copy");
            });
            check("Yard phase gate increments before testing phase13 and preserves uint wrap",delegate
            {
                var entered=PracticeMapYardState.Create(programs,startup(),30,1).Enter();
                var before=new PracticeYardPhaseClock(entered,12).Advance(false);Need(before.PhaseCounter==13&&before.WorkerPasses==1&&before.Yard.WorkerCalls==1,"Counter12 boundary");
                var after=new PracticeYardPhaseClock(entered,13).Advance(false);Need(after.PhaseCounter==14&&after.WorkerPasses==0,"Counter13 was tested before increment");after=after.AdvanceMany(63,false);Need(after.PhaseCounter==77&&after.WorkerPasses==1,"64-poll gate period");
                var zero=new PracticeYardPhaseClock(entered,0).AdvanceMany(12,false);Need(zero.PhaseCounter==12&&zero.WorkerPasses==0,"Initial12 polls admitted too early");Need(zero.Advance(false).WorkerPasses==1,"Initial0 missed poll13");
                var wrap=new PracticeYardPhaseClock(entered,uint.MaxValue).Advance(false);Need(wrap.PhaseCounter==0&&wrap.WorkerPasses==0,"Unsigned phase rollover");wrap=wrap.AdvanceMany(13,false);Need(wrap.PhaseCounter==13&&wrap.WorkerPasses==1&&wrap.Polls==14,"Wrapped phase13 gate");
            });
            check("Paused yard polls hold counter and worker even when already at phase13",delegate
            {
                var entered=PracticeMapYardState.Create(programs,startup(),30,1).Enter();var initial=new PracticeYardPhaseClock(entered,12);
                var clock=initial.AdvanceMany(64,true);Need(clock.PhaseCounter==12&&clock.WorkerPasses==0&&Object.ReferenceEquals(entered,clock.Yard)&&clock.Polls==64&&clock.PausedPolls==64,"Paused pre-gate changed yard");
                clock=clock.Advance(false);var first=clock.Yard;clock=clock.AdvanceMany(64,true);Need(clock.PhaseCounter==13&&clock.WorkerPasses==1&&Object.ReferenceEquals(first,clock.Yard)&&clock.Polls==129&&clock.PausedPolls==128,"Paused phase13 repeated worker");
                clock=clock.AdvanceMany(64,false);Need(clock.PhaseCounter==77&&clock.WorkerPasses==2&&clock.Yard.WorkerCalls==2&&initial.Polls==0&&entered.WorkerCalls==0,"Resume/predecessor isolation");
            });
            check("Default clocked yard returns at unpaused poll257 and retains post-return data",delegate
            {
                var entered=PracticeMapYardState.Create(programs,startup(),30,1).Enter();var clock=new PracticeYardPhaseClock(entered,12).AdvanceMany(256,false);
                Need(clock.PhaseCounter==268&&clock.WorkerPasses==4&&!clock.Yard.Returned,"Return before phase269");clock=clock.Advance(false);
                Need(clock.Polls==257&&clock.PhaseCounter==269&&clock.WorkerPasses==5&&clock.Yard.Returned&&clock.Yard.TargetAmmo==138&&clock.Yard.DepotAmmo==348,"Return phase269 boundary");
                string before=Hash(clock.Yard.CopyShipPool())+Hash(clock.Yard.CopyFleetPool());clock=clock.AdvanceMany(64,false);
                Need(clock.WorkerPasses==6&&clock.Yard.WorkerCalls==6&&Hash(clock.Yard.CopyShipPool())+Hash(clock.Yard.CopyFleetPool())==before&&clock.Yard.TextRequests==2&&clock.Yard.RefreshRequests==3,"Post-return source pass changed data");
                var partial=entered.WorkerStep();var resumed=new PracticeYardPhaseClock(partial,12).Advance(false);Need(resumed.WorkerPasses==1&&resumed.Yard.WorkerCalls==2&&partial.WorkerCalls==1,"Clock did not preserve received manual history");
            });
            check("Nine phase-gated yard compositions retain recorded original425 twenty-pass endpoints",delegate
            {
                var map=startup();
                foreach(string line in PracticeMapYardChecks.CopyOriginalGoldens())
                {
                    var row=line.Split(',');var entered=PracticeMapYardState.Create(programs,map,int.Parse(row[0]),int.Parse(row[1])).Enter();
                    var clock=new PracticeYardPhaseClock(entered,12).AdvanceMany(1280,false);
                    Need(clock.PhaseCounter==1292&&clock.Polls==1280&&clock.WorkerPasses==20&&clock.Yard.WorkerCalls==20&&clock.Yard.Returned,"Clock composition phase/pass count");
                    Need(Hash(clock.Yard.CopyShipPool())==row[7]&&Hash(clock.Yard.CopyFleetPool())==row[8]&&clock.Yard.SelectedUnit==entered.TargetUnit&&clock.Yard.SelectedFleet==entered.OriginFleet&&clock.Yard.TextRequests==2&&clock.Yard.MapRequests==1&&clock.Yard.RefreshRequests==3,"Original425 endpoint outside phase model");
                }
                // These are compositions using prior yard endpoints and the
                // recovered gate, not nine new whole-original world replays.
            });
        }
    }
}
