using System;
using System.IO;
using System.Drawing;
using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeYardApproachChecks
    {
        static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}catch(InvalidDataException){return true;}catch(NotSupportedException){return true;}catch(InvalidOperationException){return true;}}
        // Only supplied approach pose/queue/heading differ from original425's
        // yard inputs. Restore exactly those fields for the composition check;
        // every other byte must still match the independent original history.
        static byte[] WithoutSuppliedMovement(PracticeMapYardState state,byte[] baseline)
        {
            var fleets=state.CopyFleetPool();int at=state.OriginFleet*492;
            Array.Copy(baseline,at+12,fleets,at+12,4);Array.Copy(baseline,at+40,fleets,at+40,20);fleets[at+62]=baseline[at+62];
            return fleets;
        }
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");byte[] input=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));
            Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,input,731);};
            check("BSSB202 terrain matches the complete recorded original453 pool",delegate
            {
                var terrain=PracticeTerrainPool.Bssb202(input);var records=terrain.CopyRecords();
                Need(terrain.Count==53&&records.Length==246000&&Hash(records)=="607BCDF7530CF324EF32EA2B3A86A50A4AC41D86548A95CD26707A80828894FC","Original453 terrain pool identity/count");
                for(int n=0;n<500;n++)Need(BitConverter.ToInt16(records,n*492+4)==n,"Original terrain root IDs");
                for(int n=53;n<500;n++)Need(BitConverter.ToInt32(records,n*492)==0,"Inactive terrain tail");
            });
            check("BSSB202 terrain source and startup copies are immutable and identity guarded",delegate
            {
                var original=(byte[])input.Clone();var pool=PracticeTerrainPool.Bssb202(original);original[0]^=255;var records=pool.CopyRecords();records[0]^=255;
                Need(pool.CopyRecords()[0]!=records[0]&&Hash(input)==PracticeMapStartup.SupportedSourceHash,"Terrain input/result alias");
                var map=startup();var copy=map.CopyTerrainPool();copy[0]^=255;Need(map.TerrainCount==53&&map.CopyTerrainPool()[0]!=copy[0]&&Hash(map.CopyTerrainPool())==Hash(pool.CopyRecords()),"Startup terrain isolation");
                Need(Reject(delegate{PracticeTerrainPool.Bssb202(null);})&&Reject(delegate{PracticeTerrainPool.Bssb202(new byte[0]);})&&Reject(delegate{PracticeTerrainPool.Bssb202(original);}),"Unadmitted terrain input");
            });
            check("Nearby approach derives yard3 from the strict movement collision boundary",delegate
            {
                var map=startup();var initial=PracticeYardApproachState.Create(programs,map,30,1);
                Need(initial.Collision==-1&&!initial.CheckAdmission().Available&&initial.Position==new Point(622,600),"Approach injected an admitted yard");
                var first=initial.Step();Need(first.Visits==1&&first.Position==new Point(620,600)&&first.Collision==-1&&!first.CheckAdmission().Available&&first.LastOutcome==PracticeMovementOutcome.Moved,"Touching boxes should not overlap");
                var second=first.Step();Need(second.Visits==2&&second.Position==first.Position&&second.Collision==3&&second.LastOutcome==PracticeMovementOutcome.BlockedByFleet&&second.CheckAdmission().Available,"Proposed overlap should retain origin and admit yard3");
                var faster=PracticeYardApproachState.Create(programs,map,34,1).Step();Need(faster.Visits==1&&faster.Position==new Point(622,600)&&faster.Collision==3&&faster.CheckAdmission().Available,"Recorded speed5 changes first contact visit");
            });
            check("Nine nearby approach compositions preserve the original425 yard data outside supplied movement fields",delegate
            {
                var map=startup();var baseline=map.Stages[3].CopyFleetPool();
                foreach(string line in PracticeMapYardChecks.CopyOriginalGoldens())
                {
                    var row=line.Split(',');int target=int.Parse(row[0]),profile=int.Parse(row[1]);
                    var approach=PracticeYardApproachState.Create(programs,map,target,profile).UntilAdmission();var state=approach.CreateYardCopy();
                    Need(approach.Visits==(target==34?1:2)&&Hash(state.CopyShipPool())==row[3]&&Hash(WithoutSuppliedMovement(state,baseline))==row[4],"Composed original425 input outside supplied pose/queue/heading");
                    state=state.Enter();Need(Hash(state.CopyShipPool())==row[5]&&Hash(WithoutSuppliedMovement(state,baseline))==row[6],"Composed original425 entered data");
                    for(int n=0;n<20;n++)state=state.WorkerStep();
                    Need(state.Returned&&Hash(state.CopyShipPool())==row[7]&&Hash(WithoutSuppliedMovement(state,baseline))==row[8]&&state.SelectedFleet==state.OriginFleet&&state.SelectedUnit==target&&state.TextRequests==2&&state.MapRequests==1&&state.RefreshRequests==3,"Composed original425 return and request context");
                }
                // This proves the bounded composition, not a fresh whole-original
                // execution of movement plus command plus support orchestration.
            });
            check("Nearby approach rejects early transfer and repeated movement without changing predecessors",delegate
            {
                var map=startup();string parent=Hash(map.Stages[3].CopyFleetPool());var initial=PracticeYardApproachState.Create(programs,map,30,2);string before=Hash(initial.CopyFleetPool());
                Need(Reject(delegate{initial.CreateYardCopy();}),"Unadmitted movement allowed transfer");var contact=initial.UntilAdmission();string after=Hash(contact.CopyFleetPool());
                Need(initial.Visits==0&&initial.Collision==-1&&Hash(initial.CopyFleetPool())==before&&Hash(map.Stages[3].CopyFleetPool())==parent,"Approach changed predecessor/parent");
                Need(Reject(delegate{contact.Step();})&&Hash(contact.CopyFleetPool())==after&&Object.ReferenceEquals(contact,contact.UntilAdmission()),"Admitted approach repeated movement");
                var f=contact.CopyFleetPool();f[0]^=255;var t=contact.CopyTerrainPool();t[0]^=255;Need(contact.CopyFleetPool()[0]!=f[0]&&contact.CopyTerrainPool()[0]!=t[0],"Approach leaked writable pools");
            });
            check("Nearby approach manual and bounded visits produce the same copied transfer input",delegate
            {
                var map=startup();
                foreach(int target in new[]{30,31,34})
                {
                    var initial=PracticeYardApproachState.Create(programs,map,target,1);var manual=initial;
                    while(!manual.CheckAdmission().Available)manual=manual.Step();var batch=initial.UntilAdmission();
                    Need(manual.Visits==batch.Visits&&Hash(manual.CopyFleetPool())==Hash(batch.CopyFleetPool())&&Hash(manual.CreateYardCopy().CopyShipPool())==Hash(batch.CreateYardCopy().CopyShipPool()),"Manual/bounded approach differs");
                }
                Need(Reject(delegate{PracticeYardApproachState.Create(programs,null,30,1);})&&Reject(delegate{PracticeYardApproachState.Create(programs,PracticeMapStartup.CreateBssb202(programs,input,0),30,1);}),"Approach admitted unsupported startup");
            });
            evidence("BSSB202 terrain matches twenty complete original453 stage observations",delegate
            {
                string expected=Hash(startup().CopyTerrainPool());int count=0;
                foreach(string line in File.ReadLines(Path.Combine(Path.GetDirectoryName(assets),"Evidence","Map-Terrain-453.csv")))
                {
                    if(line.StartsWith("Seed,"))continue;var row=line.Split(',');Need(row.Length==4&&int.Parse(row[2])==53&&row[3]==expected,"Original453 stage terrain");count++;
                }
                Need(count==20,"Original453 terrain observation count");
            });
        }
    }
}
