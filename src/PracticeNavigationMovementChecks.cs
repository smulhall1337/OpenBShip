using System;using System.IO;using System.Security.Cryptography;

namespace FleetLab
{
    internal static class PracticeNavigationMovementChecks
    {
        // Original864 only: initial digest,then a rolling digest of all512
        // recorded post-stage snapshots per history. Export868 never runs the
        // maintained model. Remaining fields are recorded/derived source totals.
        static readonly string[] Original={
            "0,C4C29758EE9EE244C3A55A205471DA06DC2A63282AC906E289CC40D8E0B90066,ACD373D0BE64C78A8BD4A76E389236BA06F3FC6116D6155536B9E921EAA83F82,9,8,0,0,0,116,116,120,120,4,1,48,8",
            "1,7A6B92160030443340671F86F4CA0DAEAA2BC54B4120C865458257B0F7E7B85D,AF51BA9D6635A1C3E5EE65D940B96FFD6A38214B9AAD83EC694357BEA0946144,9,6,2,1,65,130,120,-1,310,4,0,48,8",
            "2,6126DAC52E7E0FA8A3D2DF479533A916DFCED9BA4D21E8E3018BB1379F35E24D,93FB14A6C23023E673D19251014A273D76CC852E937744BBF9FBB55C672E584C,9,9,0,0,0,8,18,10,20,4,1,48,8",
            "3,5B8F9760A4CDF4B506ED4F413722BE31D9C53FD6597A5D8F1E630D6F4BEE5C3B,45932185AFEA69E54DC97B731B2A37D32444C51849C0902AB5152F2010BF6F40,9,0,2,1,254,320,320,-1,310,4,0,48,8",
            "4,2D3D3B01E289310B4434F397B39E6D0E8E195503392BB46F73C8BE8E4F93A3A6,E3969B9E8B027E402AAE7380B2AB1A2CCA73213C56026B84B8768C802A91545A,9,0,0,0,0,100,100,100,100,2,1,48,8"
        };
        sealed class History
        {
            internal string InitialDigest,Chain;
            internal PracticeNavigationMovementState Initial,End,After32;
            internal PracticeNavigationMovementUpdate First,Second;
            internal int Visits,Moves,Consumed,Completed,Dormant;
        }
        static void Need(bool ok,string message){if(!ok)throw new InvalidDataException(message);}
        static byte[] HashBytes(byte[] b){using(var h=SHA256.Create())return h.ComputeHash(b);}
        static string Hex(byte[] b){return BitConverter.ToString(b).Replace("-","");}
        static string Hash(byte[] b){return Hex(HashBytes(b));}
        static void I(byte[] b,int at,int value){BitConverter.GetBytes(value).CopyTo(b,at);}
        static void W(byte[] b,int at,int value){BitConverter.GetBytes(unchecked((short)value)).CopyTo(b,at);}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}catch(InvalidDataException){return true;}catch(NotSupportedException){return true;}catch(InvalidOperationException){return true;}}
        static byte[] Digest(PracticeNavigationMovementState state,int clock,int phase,int stage)
        {
            var b=new byte[123000+78560+4096+22];state.CopyFleetPool().CopyTo(b,0);state.Backing.CopyRegion().CopyTo(b,123000);state.Grid.CopyCells().CopyTo(b,123000+78560);
            int at=123000+78560+4096;
            foreach(int value in new[]{clock,phase,state.Schedule.BatchStart}){BitConverter.GetBytes(value).CopyTo(b,at);at+=4;}
            BitConverter.GetBytes(state.Schedule.CompletedCycles).CopyTo(b,at);at+=4;BitConverter.GetBytes((short)state.SelectedWaypoint).CopyTo(b,at);at+=2;BitConverter.GetBytes(stage).CopyTo(b,at);
            return HashBytes(b);
        }
        static byte[] Chain(byte[] chain,byte[] digest){var b=new byte[64];chain.CopyTo(b,0);digest.CopyTo(b,32);return HashBytes(b);}
        static History Evaluate(PracticeGoalRouteStudyBasis basis,int preset)
        {
            var h=new History();var state=basis.CreateMovementComparison(preset);h.Initial=state;h.InitialDigest=Hex(Digest(state,12345,1,-1));var chain=new byte[32];
            for(int poll=0;poll<256;poll++)
            {
                int clock=12345+(preset==0?0:40*poll);var update=state.AdvanceOfflinePoll(clock);
                chain=Chain(chain,Digest(update.Prephase,clock,poll+1,0));chain=Chain(chain,Digest(update.State,clock,poll+1,1));
                if(update.Navigation==null)h.Dormant++;else if(update.Navigation.Outcome==PracticeQueuedNavigationOutcome.CompletedGoal)h.Completed++;
                foreach(var visit in update.Movement.FleetUpdates){h.Visits++;if(visit.Update.Before!=visit.Update.After)h.Moves++;if(visit.Update.QueueConsumed)h.Consumed++;}
                if(poll==0)h.First=update;if(poll==1)h.Second=update;if(poll==31)h.After32=update.State;state=update.State;
            }
            h.End=state;h.Chain=Hex(chain);return h;
        }
        static PracticeNavigationMovementState Copy(PracticeNavigationMovementState state,byte[] f,PracticeAiSharedBacking backing,byte[] terrain,byte[] secondary,int selection)
        {return new PracticeNavigationMovementState(f,backing,state.Grid,terrain,secondary,state.Schedule,selection);}
        static byte[] Read(BinaryReader r,int count){var b=r.ReadBytes(count);Need(b.Length==count,"864 truncated");return b;}
        static byte[] Patch(BinaryReader r,byte[] basis)
        {
            var b=(byte[])basis.Clone();int count=r.ReadInt32(),last=-1;Need(count>=0&&count<=b.Length,"864 patch count");
            for(int n=0;n<count;n++){int at=r.ReadInt32();Need(at>last&&at<b.Length,"864 patch index");last=at;b[at]=r.ReadByte();}return b;
        }
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var source=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));
            PracticeGoalRouteStudyBasis basis=null;var histories=new History[5];
            Func<PracticeGoalRouteStudyBasis> data=delegate{if(basis==null)basis=PracticeGoalRouteStudyBasis.Create(programs,PracticeMapStartup.CreateBssb202(programs,source,731));return basis;};
            Func<int,History> history=delegate(int preset){if(histories[preset]==null)histories[preset]=Evaluate(data(),preset);return histories[preset];};
            check("Navigation/movement matches all5 original864 256-poll recorded history chains",delegate{
                for(int preset=0;preset<5;preset++)
                {
                    var v=Original[preset].Split(',');var h=history(preset);Need(int.Parse(v[0])==preset&&h.InitialDigest==v[1],"864 exact initial input "+preset);
                    Need(h.Chain==v[2],"864 complete512stage chain "+preset);
                    Need(h.Visits==int.Parse(v[3])&&h.Moves==int.Parse(v[4])&&h.Consumed==int.Parse(v[5])&&h.Completed==int.Parse(v[6])&&h.Dormant==int.Parse(v[7]),"864 history totals "+preset);
                    var f=h.End.CopyFleetPool();var task=h.End.Backing.CopyTaskRecord(16);
                    Need(BitConverter.ToInt16(f,16*492+12)==int.Parse(v[8])&&BitConverter.ToInt16(f,16*492+14)==int.Parse(v[9])&&
                        BitConverter.ToInt16(f,16*492+40)==int.Parse(v[10])&&BitConverter.ToInt16(f,16*492+50)==int.Parse(v[11])&&
                        BitConverter.ToInt32(task,200)==int.Parse(v[12])&&BitConverter.ToInt32(task,304)==int.Parse(v[13])&&
                        h.End.Schedule.BatchStart==int.Parse(v[14])&&h.End.Schedule.CompletedCycles==uint.Parse(v[15]),"864 exact endpoint "+preset);
                }
            });
            check("Navigation/movement preserves scheduler visits arrival locks terminal queues and failed retries",delegate{
                var open=history(0);Need(open.First.Navigation.Outcome==PracticeQueuedNavigationOutcome.Started&&
                    open.First.Movement.FleetUpdates.Count==1&&open.First.Movement.FleetUpdates[0].Update.Outcome==PracticeMovementOutcome.ReachedLockedWaypoint&&
                    !open.First.Movement.FleetUpdates[0].Update.QueueConsumed,"Inserted point lost navigation lock");
                Need(BitConverter.ToInt16(open.Second.Prephase.CopyFleetPool(),16*492+40)==110&&open.Second.Movement.FleetUpdates.Count==0&&
                    BitConverter.ToInt16(open.After32.CopyFleetPool(),16*492+12)==102&&open.After32.Schedule.CompletedCycles==1,"Movement ignored actual scheduler");
                var fast=history(1);Need(fast.Completed==1&&fast.Consumed==2&&fast.Dormant==65&&fast.End.Backing.CopyTaskRecord(249)[284]==0&&
                    BitConverter.ToInt32(fast.End.Backing.CopyTaskRecord(16),200)==4,"Completion/selected-other auxiliary semantics");
                var rounded=history(2);Need(BitConverter.ToInt16(rounded.First.Prephase.CopyFleetPool(),16*492+40)==0&&
                    BitConverter.ToInt16(rounded.First.State.CopyFleetPool(),16*492+12)==7&&BitConverter.ToInt16(rounded.First.State.CopyFleetPool(),16*492+14)==17,"Rounded point caused a teleport");
                var final=history(3);Need(final.Second.Navigation.Outcome==PracticeQueuedNavigationOutcome.CompletedGoal&&final.Moves==0&&final.Consumed==2,"Already-final history differs");
                var ring=history(4);Need(ring.Moves==0&&BitConverter.ToInt32(ring.End.Backing.CopyTaskRecord(16),200)==2&&
                    BitConverter.ToInt32(ring.End.Backing.CopyTaskRecord(16),304)==1,"Blocked destination lost held goal2/navigation");
            });
            check("Navigation/movement owns copies and rejects unsupported world branches without partial publication",delegate{
                var initial=data().CreateMovementComparison(0);var f=initial.CopyFleetPool();var terrain=initial.CopyTerrainPool();var secondary=initial.CopySecondaryPool();
                var state=Copy(initial,f,initial.Backing,terrain,secondary,4);string before=Hex(Digest(state,12345,1,-1));
                f[0]^=255;terrain[0]=1;secondary[0]=1;Need(Hex(Digest(state,12345,1,-1))==before&&state.CopyTerrainPool()[0]==0&&state.CopySecondaryPool()[0]==0,"Aliased constructor arrays");
                var update=state.AdvanceOfflinePoll(12345);string after=Hex(Digest(update.State,12345,1,1));var exported=update.State.CopyFleetPool();exported[0]^=255;
                Need(Hex(Digest(update.State,12345,1,1))==after&&Hex(Digest(state,12345,1,-1))==before,"Aliased result or input");
                foreach(int kind in new[]{9,12}){var bad=state.CopyFleetPool();I(bad,16*492,kind);Need(Reject(delegate{Copy(state,bad,state.Backing,state.CopyTerrainPool(),state.CopySecondaryPool(),4);}),"Unverified actor kind "+kind);}
                var other=state.CopyFleetPool();I(other,20*492,1);Need(Reject(delegate{Copy(state,other,state.Backing,state.CopyTerrainPool(),state.CopySecondaryPool(),4);}),"Unverified second active fleet");
                var activeTerrain=state.CopyTerrainPool();I(activeTerrain,499*492,1);Need(Reject(delegate{Copy(state,state.CopyFleetPool(),state.Backing,activeTerrain,state.CopySecondaryPool(),4);}),"Unverified terrain composition");
                var activeSecondary=state.CopySecondaryPool();I(activeSecondary,124*492,1);Need(Reject(delegate{Copy(state,state.CopyFleetPool(),state.Backing,state.CopyTerrainPool(),activeSecondary,4);}),"Unverified secondary composition");
                Need(Reject(delegate{Copy(state,state.CopyFleetPool(),state.Backing,state.CopyTerrainPool(),state.CopySecondaryPool(),5);}),"Raw counter5 silently clamped");
                var malformed=state.CopyFleetPool();W(malformed,16*492+42,-2);
                var late=Copy(state,malformed,state.Backing,state.CopyTerrainPool(),state.CopySecondaryPool(),4);string lateBefore=Hex(Digest(late,12345,1,-1));
                Need(Reject(delegate{late.AdvanceOfflinePoll(12345);})&&Hex(Digest(late,12345,1,-1))==lateBefore,"Late movement failure published partial navigation");
                Need(Reject(delegate{PracticePrephaseNavigation.Evaluate(state.CopyFleetPool(),state.Backing);})&&
                    Reject(delegate{PracticeNavigationGate.Evaluate(state.CopyFleetPool(),state.Backing,16);}),"Old navigation return guards expanded");
            });
            check("Copied movement study preserves partitioned polls bounds reset and supplied-clock accounting",delegate{
                var initial=new PracticeNavigationMovementStudyState(data(),0);var moved=initial.AdvanceUntilPositionChanges();
                Need(moved.Polls==32&&moved.Position.X==102&&moved.Position.Y==102&&moved.ActorVisits==2&&initial.Polls==0,"Bounded next movement");
                var split=initial.Advance(16).Advance(16);Need(Hex(Digest(split.State,12345,32,1))==Hex(Digest(moved.State,12345,32,1)),"Frame partition changed history");
                var end=moved.Advance(256);Need(end.Polls==256&&!end.CanAdvance&&end.Position.X==116&&end.Position.Y==116&&end.CopyTrail().Length==9,"Recorded window bound/trail");
                var reset=end.Reset();Need(reset.Preset==0&&reset.Polls==0&&reset.CopyTrail().Length==1&&Hex(Digest(reset.State,12345,1,-1))==Hex(Digest(initial.State,12345,1,-1)),"Reset lost applied input");
                var fast=new PracticeNavigationMovementStudyState(data(),1).Advance(32);Need(fast.LastSourceClock==13585&&fast.NextSourceClock==13625,"Last/next supplied clock accounting");
                var done=new PracticeNavigationMovementStudyState(data(),3).AdvanceUntilPositionChanges();Need(done.Polls==2&&done.NavigationCompletions==1&&done.Position.X==320,"Bounded completion stop");
                var blocked=new PracticeNavigationMovementStudyState(data(),4).AdvanceUntilPositionChanges();Need(blocked.Polls==64&&blocked.PositionChanges==0,"Blocked macro exceeded64polls");
                Need(Reject(delegate{end.Advance(1);})&&Reject(delegate{initial.Advance(0);}),"Invalid study advance admitted");
            });
            evidence("Navigation/movement matches2560 original864 full snapshots and actual scheduler carryover",delegate{
                string path=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Navigation-Movement-864.bin");
                Need(Hash(File.ReadAllBytes(path))=="FC82B5BAB8DD83FD6E4C9DA201E34667007F1927F16C0405595A3FF42543720E","864 fixture identity");
                int count=0;PracticeNavigationMovementState state=null;PracticeNavigationMovementUpdate result=null;byte[] expectedRows=null;
                using(var r=new BinaryReader(File.OpenRead(path)))
                {
                    Need(System.Text.Encoding.ASCII.GetString(Read(r,8))=="NM864001"&&r.ReadInt32()==1&&r.ReadInt32()==5&&r.ReadInt32()==256&&r.ReadInt32()==2560,"864 header");
                    Read(r,402000);var f=Read(r,123000);Read(r,78000);var region=Read(r,78560);Read(r,64000);var rows=Read(r,1024000);
                    for(;;)
                    {
                        int id=r.ReadInt32();if(id==-1)break;int h=r.ReadInt32(),poll=r.ReadInt32(),stage=r.ReadInt32(),clock=r.ReadInt32(),phase=r.ReadInt32(),cursorBefore=r.ReadInt32(),cursorAfter=r.ReadInt32();
                        uint cycleBefore=r.ReadUInt32(),cycleAfter=r.ReadUInt32();int selectionBefore=r.ReadInt32(),selectionAfter=r.ReadInt32();
                        var bf=Patch(r,f);var br=Patch(r,region);var beforeRows=Patch(r,rows);var af=Patch(r,bf);var ar=Patch(r,br);var afterRows=Patch(r,beforeRows);
                        Need(id==count&&h==id/512&&poll==id/2%256&&stage==id%2&&phase==poll+1&&clock==12345+(h==0?0:40*poll)&&selectionBefore==4&&selectionAfter==4,"864 metadata");
                        if(poll==0&&stage==0){var cells=new byte[4096];Array.Copy(beforeRows,16*4096,cells,0,4096);expectedRows=(byte[])beforeRows.Clone();state=new PracticeNavigationMovementState(bf,new PracticeAiSharedBacking(br),new PracticeAiRouteGrid(16,cells),new byte[246000],new byte[61500],new PracticeFleetScheduler(16,0),4);}
                        var before=stage==0?state:result.Prephase;
                        Need(Hash(before.CopyFleetPool())==Hash(bf)&&Hash(before.Backing.CopyRegion())==Hash(br)&&Hash(expectedRows)==Hash(beforeRows)&&before.Schedule.BatchStart==cursorBefore&&before.Schedule.CompletedCycles==cycleBefore,"864 exact carried input "+id);
                        if(stage==0)result=state.AdvanceOfflinePoll(clock);var after=stage==0?result.Prephase:result.State;after.Grid.CopyCells().CopyTo(expectedRows,16*4096);
                        Need(Hash(after.CopyFleetPool())==Hash(af)&&Hash(after.Backing.CopyRegion())==Hash(ar)&&Hash(expectedRows)==Hash(afterRows)&&after.Schedule.BatchStart==cursorAfter&&after.Schedule.CompletedCycles==cycleAfter&&after.SelectedWaypoint==selectionAfter,"864 complete output "+id);
                        if(stage==1)state=result.State;count++;
                    }
                    Need(count==2560&&r.BaseStream.Position==r.BaseStream.Length,"864 complete extent");
                }
            });
        }
    }
}
