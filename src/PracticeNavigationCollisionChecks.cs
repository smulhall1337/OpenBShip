using System;using System.IO;using System.Security.Cryptography;using System.Collections.Generic;
namespace FleetLab
{
    internal static class PracticeNavigationCollisionChecks
    {
        // Export886 derives these only from original875/884 bytes. Each chain
        // covers2048stages,fullF/shared/actorgrid/terrain and22metadata bytes.
        static readonly string[] Original={
            "875,0,0325E598A7D5930C09DDAFF40B3FD1E45D9127AC93CD869A3A93E12984CDC646,64F7E5FF56E3BB21C768C342E28179C84DC768A06BC54D53A225BD6E13EC99CC,33,12,2,1,644,0,0,160,100,-1,310,4,0,144,32",
            "875,1,D71C4BB62AB77C8E1F29904C96A2031F4661F1797BAD286748563B1AA9009CD6,6240FD9C1D06EE83BE12420E1FBFDE95059637069B0FD5A380AF81506D6E8B84,33,12,2,1,644,0,0,160,100,-1,310,4,0,144,32",
            "875,2,E5A21DCEEAED4675B4520A8E98D7F2FC8B6FB5718A50E597531BD938A4560DB8,F2E514F9A4A180148C7B229AC11F9620A6C6E5099F376A2099FDFCF6B33D4842,33,0,0,0,0,0,0,100,100,100,100,2,1,144,32",
            "884,0,215FFF8C16BB39034F4BF49DA8ED91C326F6F565846D0CF4661908E2AE032A96,46E892CE6114FAE5F52FB834A4D6A8159C12B8E7695A313BC8A83C4F8706C810,33,12,2,1,644,0,0,160,100,-1,310,4,0,144,32",
            "884,1,0325E598A7D5930C09DDAFF40B3FD1E45D9127AC93CD869A3A93E12984CDC646,D94135D8F117162422EEE32228DD305A60D7F188DB8B7D725CA94F0F12879F8E,33,14,2,1,550,1,1,160,100,-1,310,4,0,144,32",
            "884,2,0325E598A7D5930C09DDAFF40B3FD1E45D9127AC93CD869A3A93E12984CDC646,3B31B47A50BBF61C1A05F7ECD348F3E41EDCBA70B75E591A7B4AF484C043668F,33,14,2,1,550,1,1,160,100,-1,310,4,0,144,32",
            "884,3,0325E598A7D5930C09DDAFF40B3FD1E45D9127AC93CD869A3A93E12984CDC646,D059BE1FB3242A43D03F3ADB13877A9F9238C8BEE322A29D6714B1D52FC58177,33,14,2,1,550,1,1,160,100,-1,310,4,0,144,32",
        };
        internal static void Need(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
        internal static byte[] HashBytes(byte[] b){using(var h=SHA256.Create())return h.ComputeHash(b);}
        internal static string Hash(byte[] b){return Hex(HashBytes(b));}
        static string Hex(byte[] b){return BitConverter.ToString(b).Replace("-","");}
        static void I(byte[] b,int at,int value){BitConverter.GetBytes(value).CopyTo(b,at);}
        static void W(byte[] b,int at,int value){BitConverter.GetBytes((short)value).CopyTo(b,at);}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}catch(InvalidDataException){return true;}catch(NotSupportedException){return true;}catch(InvalidOperationException){return true;}}
        internal static byte[] Digest(PracticeNavigationMovementState state,int clock,int phase,int stage)
        {
            var b=new byte[451678];state.CopyFleetPool().CopyTo(b,0);state.Backing.CopyRegion().CopyTo(b,123000);state.Grid.CopyCells().CopyTo(b,201560);state.CopyTerrainPool().CopyTo(b,205656);
            int at=451656;foreach(int value in new[]{clock,phase,state.Schedule.BatchStart}){I(b,at,value);at+=4;}BitConverter.GetBytes(state.Schedule.CompletedCycles).CopyTo(b,at);at+=4;W(b,at,state.SelectedWaypoint);I(b,at+2,stage);return HashBytes(b);
        }
        sealed class History
        {
            internal string Initial,Chain;internal PracticeNavigationMovementState End;
            internal readonly Dictionary<int,PracticeNavigationMovementState> States=new Dictionary<int,PracticeNavigationMovementState>();
            internal PracticeNavigationMovementUpdate Replan;
            internal int Visits,Moves,Consumed,Completed,Dormant,Replans,Collisions;
        }
        static History Evaluate(PracticeGoalRouteStudyBasis basis,int preset)
        {
            var h=new History();var state=basis.CreateMovementComparison(preset);h.Initial=Hex(Digest(state,12345,1,-1));var chain=new byte[32];
            for(int poll=0;poll<1024;poll++)
            {
                state=PracticeGoalRouteStudyBasis.ApplyMovementWorldEvent(state,preset,poll);int clock=12345+40*poll;var update=state.AdvanceOfflinePoll(clock);
                foreach(var stage in new[]{update.Prephase,update.State})
                {var joined=new byte[64];chain.CopyTo(joined,0);Digest(stage,clock,poll+1,Object.ReferenceEquals(stage,update.Prephase)?0:1).CopyTo(joined,32);chain=HashBytes(joined);}
                if(update.Navigation==null)h.Dormant++;else if(update.Navigation.Outcome==PracticeQueuedNavigationOutcome.CompletedGoal)h.Completed++;
                if(update.Navigation!=null&&update.Navigation.Outcome==PracticeQueuedNavigationOutcome.Replanned){h.Replans++;h.Replan=update;}
                foreach(var visit in update.Movement.FleetUpdates){h.Visits++;if(visit.Update.Before!=visit.Update.After)h.Moves++;if(visit.Update.QueueConsumed)h.Consumed++;if(visit.Update.Outcome==PracticeMovementOutcome.BlockedByTerrain||visit.Update.Outcome==PracticeMovementOutcome.BlockedByFleet)h.Collisions++;}
                state=update.State;if(poll==64||poll==65||poll==94||poll==95||poll==192||poll==193)h.States.Add(poll+1,state);
            }
            h.End=state;h.Chain=Hex(chain);return h;
        }
        static PracticeNavigationMovementState Copy(PracticeNavigationMovementState state,byte[] f,byte[] terrain)
        {return new PracticeNavigationMovementState(f,state.Backing,state.Grid,terrain,state.CopySecondaryPool(),state.Schedule,state.SelectedWaypoint);}
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");PracticeGoalRouteStudyBasis basis=null;var histories=new History[7];
            Func<PracticeGoalRouteStudyBasis> data=delegate{if(basis==null)basis=PracticeGoalRouteStudyBasis.Create(programs,PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731));return basis;};
            Func<int,History> history=delegate(int preset){int at=preset-5;if(histories[at]==null)histories[at]=Evaluate(data(),preset);return histories[at];};
            check("Navigation/movement matches seven original875/884 long world-history chains",delegate{
                Need(Original.Length==7,"Seven independent original histories");
                for(int preset=5;preset<12;preset++)
                {
                    var v=Original[preset-5].Split(',');var h=history(preset);Need(h.Initial==v[2]&&h.Chain==v[3],"Exact initial/full2048stage chain preset"+preset);
                    Need(h.Visits==int.Parse(v[4])&&h.Moves==int.Parse(v[5])&&h.Consumed==int.Parse(v[6])&&h.Completed==int.Parse(v[7])&&h.Dormant==int.Parse(v[8])&&h.Replans==int.Parse(v[9])&&h.Collisions==int.Parse(v[10]),"Recorded totals preset"+preset);
                    var f=h.End.CopyFleetPool();var r=h.End.Backing.CopyTaskRecord(16);int actor=16*492;
                    Need(BitConverter.ToInt16(f,actor+12)==int.Parse(v[11])&&BitConverter.ToInt16(f,actor+14)==int.Parse(v[12])&&BitConverter.ToInt16(f,actor+40)==int.Parse(v[13])&&BitConverter.ToInt16(f,actor+50)==int.Parse(v[14])&&BitConverter.ToInt32(r,200)==int.Parse(v[15])&&BitConverter.ToInt32(r,304)==int.Parse(v[16])&&h.End.Schedule.BatchStart==int.Parse(v[17])&&h.End.Schedule.CompletedCycles==uint.Parse(v[18]),"Recorded endpoint preset"+preset);
                }
            });
            check("Collision histories distinguish supplied obstacle arrival collision and next-prephase replan",delegate{
                foreach(int preset in new[]{9,10,11})
                {
                    var h=history(preset);var before=h.States[65];var supplied=h.States[66];var hit=h.States[95];var replanned=h.States[96];int actor=16*492;
                    Need(before.CopyTerrainPool()[0]==0&&before.CopyFleetPool()[30*492]==0,"World changed before supplied event");
                    Need(supplied.CopyFleetPool()[actor+482]==255&&BitConverter.ToInt16(supplied.CopyFleetPool(),actor+40)==120,"Injection was treated as collision or reached waypoint");
                    Need(hit.CopyFleetPool()[actor+482]==(preset==11?30:254)&&BitConverter.ToInt16(hit.CopyFleetPool(),actor+12)==110&&BitConverter.ToInt16(hit.CopyFleetPool(),actor+40)==120,"Actual unreached collision absent");
                    Need(h.Replan.Navigation.CollisionRoute!=null&&h.Replan.Navigation.StartRoute==null&&h.Replan.Navigation.Waypoint!=null&&replanned.CopyFleetPool()[actor+482]==255&&BitConverter.ToInt16(replanned.CopyFleetPool(),actor+40)==110&&BitConverter.ToInt16(replanned.CopyFleetPool(),actor+50)==90,"Source collision route/callback/clear order");
                }
                Need(history(10).States[193].CopyTerrainPool()[0]==1&&history(10).States[194].CopyTerrainPool()[0]==0&&history(9).End.CopyTerrainPool()[0]==1,"Removal boundary or held terrain changed");
                Need(history(7).Moves==0&&BitConverter.ToInt32(history(7).End.Backing.CopyTaskRecord(16),200)==2&&history(6).Completed==1&&history(8).Completed==1,"Closed/gapped barrier distinction");
            });
            check("Long navigation study preserves supplied-event boundaries split histories and reset limits",delegate{
                var initial=new PracticeNavigationMovementStudyState(data(),10);Need(initial.WindowPolls==1024&&new PracticeNavigationMovementStudyState(data(),0).WindowPolls==256,"Old/new window extents");
                var at65=initial.Advance(65);Need(at65.SuppliedWorldEvents==0&&at65.State.CopyTerrainPool()[0]==0,"Early event");var at66=at65.Advance(1);Need(at66.SuppliedWorldEvents==1&&at66.State.CopyTerrainPool()[0]==1,"Missing insertion");
                var hit=at66.Advance(29);Need(hit.Polls==95&&hit.TerrainCollisionVisits==1&&hit.ReplanEntries==0,"Collision boundary");var after=hit.Advance(1);Need(after.ReplanEntries==1,"Next-prephase replan");
                var at193=after.Advance(97);Need(at193.Polls==193&&at193.State.CopyTerrainPool()[0]==1,"Early removal");var at194=at193.Advance(1);Need(at194.SuppliedWorldEvents==2&&at194.State.CopyTerrainPool()[0]==0,"Missing removal");
                var end=at194.Advance(1024);Need(end.Polls==1024&&!end.CanAdvance&&end.Position.X==160&&end.Position.Y==100&&Hash(Digest(end.State,53265,1024,1))==Hash(Digest(history(10).End,53265,1024,1)),"Partitioned history differs");
                Need(end.ReplanEntries==1&&end.TerrainCollisionVisits==1&&end.FleetCollisionVisits==0&&end.NavigationCompletions==1&&end.CopyTrail().Length==15,"Long counters/trail");var points=end.CopyTrail();points[0]=System.Drawing.Point.Empty;Need(end.CopyTrail()[0].X==100,"Aliased trail");
                var reset=end.Reset();Need(reset.Preset==10&&reset.Polls==0&&reset.SuppliedWorldEvents==0&&reset.State.CopyTerrainPool()[0]==0&&Reject(delegate{end.Advance(1);})&&Reject(delegate{reset.Advance(1025);}),"Reset/end bound");
                var viewed=initial.AdvanceUntilCollisionOrReplan();Need(viewed.Polls==95&&viewed.TerrainCollisionVisits==1&&viewed.ReplanEntries==0,"Collision viewing skipped source event");viewed=viewed.AdvanceUntilCollisionOrReplan();Need(viewed.Polls==96&&viewed.ReplanEntries==1,"Collision viewing missed next-prephase replan");
            });
            check("Collision composition retains public caller guards and rejects unexamined world variants immutably",delegate{
                var state=history(9).States[95];int actor=16*492;string before=Hash(Digest(state,16145,96,1));
                Need(Reject(delegate{PracticeQueuedNavigation.EvaluateOffline(state.Grid,state.CopyFleetPool(),state.CopyTerrainPool(),state.Backing,249,4,16145,32000,0);}),"Old public FF guard expanded");
                foreach(int collision in new[]{31,128,253}){var f=state.CopyFleetPool();f[actor+482]=(byte)collision;var bad=Copy(state,f,state.CopyTerrainPool());Need(Reject(delegate{bad.AdvanceOfflinePoll(16145);}),"Unexamined signed/index collision "+collision);}
                foreach(bool empty in new[]{true,false}){var f=state.CopyFleetPool();W(f,actor+40,empty?-1:110);W(f,actor+50,100);var bad=Copy(state,f,state.CopyTerrainPool());Need(Reject(delegate{bad.AdvanceOfflinePoll(16145);}),"Unexamined collision callback entry");}
                var badTerrain=state.CopyTerrainPool();I(badTerrain,499*492,2);Need(Reject(delegate{Copy(state,state.CopyFleetPool(),badTerrain);}),"Unknown terrain kind");I(badTerrain,499*492,1);W(badTerrain,499*492+12,640);Need(Reject(delegate{Copy(state,state.CopyFleetPool(),badTerrain);}),"Out-of-grid terrain");
                var task=state.Backing.CopyTaskRecord(16);I(task,208,0);var late=new PracticeNavigationMovementState(state.CopyFleetPool(),state.Backing.WithTaskRecord(16,task),state.Grid,state.CopyTerrainPool(),state.CopySecondaryPool(),state.Schedule,4);
                Need(Reject(delegate{late.AdvanceOfflinePoll(16145);})&&Hash(Digest(state,16145,96,1))==before,"Failed replan published partial input changes");
            });
            PracticeNavigationCollisionFixtureChecks.Run(evidence,assets,data);
        }
    }
}
