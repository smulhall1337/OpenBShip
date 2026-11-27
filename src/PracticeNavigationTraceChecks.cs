using System;using System.IO;using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeNavigationTraceChecks
    {
        // Export904 reads only pinned original900 bytes. The CALLsite vectors
        // distinguish idempotent callbacks that final-state hashes cannot.
        static readonly string[] Original={
            "0,FE9AA9214401EE06209077DB8BC41BE6903971C6094AD34EC0EF51F82538EB2E,9A00FB3AFAA53B369BE719FA204EC0E76DCF70A507A0032D90B04A68381CE564,3A5A4517A7AF5ED86202E67DEC97BCB0BD1EF158A7BBC47508AE4119CAA6E07A,1,1,0,1,107,107,110,110,4,1,0,12345",
            "1,9763D21A022AC72872D6D53D05F67CA84CA58F68BAE7506A949302E37D3F04BA,C2856C490B9158B0EFE372B65E6EF11F917B963A8B6EE6A9799662A80C04D719,E4D62BAF3711869597ECF3DC5E35381CEE8E3CE8796D8D04D1FDC60BDF5725D6,1,1,1,2,100,100,100,100,4,1,1,14345",
            "2,A4F39814DAC00AE1AA93345F3D5DF48E82845B64C20145F279595D4311914643,7C6E58C1AA886E11BB34D0806DBB33928F371EE8AF54D13A8735C0B4858BD853,EFB8A91A0FA8D116EFEFBB12A82A13B66E94D6961BE6496B19ECF76AC3E2173E,1,1,1,2,100,100,100,100,4,1,1,12346",
            "3,2186FDD0FC4AA02BBC4D8EFBE06193E5DE5BA5CB27C91D6B727F947B5E6CA58C,33A5D5A24B57BF96A037DC202AC0678F605DD292D45056A70EC33D5E414DB579,821965D0042A732C9E00512D31E81C462FF3DA158EA99458FC1FD20800A957F2,1,1,0,1,110,93,110,90,4,1,0,12345",
            "4,8D0845E0A66F7C9DBDF636F92883D88B0AD1D0CD09417A216B44BA1EE7605688,D0D0F82AE6DC5DFAF34A3C604288C7B24B5A86F95454A2FF8A3501AE5A10BE10,4E8DF75F99BBCC703B1E9CF8B570BB961F2BE336B8E0F3679814824D2D6C81CC,0,0,0,0,100,100,-1,310,4,0,0,12345",
            "5,21316090D30157D1AD12AFD7C70DF802608F593ED1D6DB87CB97CD7059298CC3,9A00FB3AFAA53B369BE719FA204EC0E76DCF70A507A0032D90B04A68381CE564,3A5A4517A7AF5ED86202E67DEC97BCB0BD1EF158A7BBC47508AE4119CAA6E07A,0,0,1,1,107,107,110,110,4,1,0,12345",
            "6,26694EE69844B0F3EA8A3ABDDDA6F5B444310D64F66B84C4E2AB941AFF3C72BE,DA92AC055F11FFCEDEB2699A0D2828F9B46A61179B6D253DD6ABC04C0E361613,0F3E7A4003E9531AFAEA4F2670715F9BCAF16E547FF87C79DE9E82AC133BFD01,1,1,0,1,100,100,-1,310,4,1,1,14345",
        };
        static void Need(bool v,string why){if(!v)throw new InvalidDataException(why);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static void I(byte[] b,int at,int value){BitConverter.GetBytes(value).CopyTo(b,at);}
        static bool Reject(Action a){try{a();return false;}catch(ArgumentException){return true;}catch(InvalidDataException){return true;}catch(NotSupportedException){return true;}catch(InvalidOperationException){return true;}}
        static string Digest(PracticeNavigationMovementState state)
        {
            var b=new byte[451666];state.CopyFleetPool().CopyTo(b,0);state.Backing.CopyRegion().CopyTo(b,123000);state.Grid.CopyCells().CopyTo(b,201560);state.CopyTerrainPool().CopyTo(b,205656);
            I(b,451656,state.Schedule.BatchStart);BitConverter.GetBytes(state.Schedule.CompletedCycles).CopyTo(b,451660);BitConverter.GetBytes((short)state.SelectedWaypoint).CopyTo(b,451664);return Hash(b);
        }
        static int[] Vector(PracticeQueuedNavigationResult n)
        {return new[]{n.CollisionRoute==null?0:1,n.CollisionRoute!=null&&n.Waypoint!=null?1:0,(n.Outcome==PracticeQueuedNavigationOutcome.ReachedWaypoint?1:0)+(n.SecondWaypoint==null?0:1),(n.Waypoint!=null&&n.Waypoint.Route!=null?1:0)+(n.SecondWaypoint!=null&&n.SecondWaypoint.Route!=null?1:0)};}
        static byte[] Read(string root,string name,int length){var b=File.ReadAllBytes(Path.Combine(root,name));Need(b.Length==length,"Trace fixture extent "+name);return b;}
        internal static void Run(Action<string,Action> check,Action<string,Action> evidence,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");PracticeGoalRouteStudyBasis basis=null;var updates=new PracticeNavigationMovementUpdate[7];
            Func<PracticeGoalRouteStudyBasis> data=delegate{if(basis==null)basis=PracticeGoalRouteStudyBasis.Create(programs,PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731));return basis;};
            Func<int,PracticeNavigationMovementUpdate> update=delegate(int p){if(updates[p]==null)updates[p]=data().CreateCollisionEntryComparison(p).AdvanceOfflinePoll(12345);return updates[p];};
            check("Coupled navigation matches original900 paired input navigation movement and measured callsite vectors",delegate{
                Need(Original.Length==7,"Seven original trace contexts");
                for(int p=0;p<7;p++)
                {
                    var v=Original[p].Split(',');var initial=data().CreateCollisionEntryComparison(p);var u=update(p);Need(v.Length==16&&int.Parse(v[0])==p&&Digest(initial)==v[1]&&Digest(u.Prephase)==v[2]&&Digest(u.State)==v[3],"Original900 exact coupled digests "+p);
                    var vector=Vector(u.Navigation);for(int n=0;n<4;n++)Need(vector[n]==int.Parse(v[4+n]),"Measured CALLsite vector "+p+"/"+n);
                    Need(u.Navigation.WaypointFunctionVisits==vector[1]+vector[2]&&u.Navigation.GridFunctionVisits==vector[0]+vector[3],"Function-entry totals "+p);
                    var f=u.State.CopyFleetPool();var task=u.State.Backing.CopyTaskRecord(16);int actor=16*492;
                    Need(BitConverter.ToInt16(f,actor+12)==int.Parse(v[8])&&BitConverter.ToInt16(f,actor+14)==int.Parse(v[9])&&BitConverter.ToInt16(f,actor+40)==int.Parse(v[10])&&BitConverter.ToInt16(f,actor+50)==int.Parse(v[11])&&BitConverter.ToInt32(task,200)==int.Parse(v[12])&&BitConverter.ToInt32(task,304)==int.Parse(v[13])&&task[284]==int.Parse(v[14])&&BitConverter.ToInt32(task,4)==int.Parse(v[15]),"Original coupled endpoint "+p);
                }
            });
            check("Repeated collision callback runs once under held deadlines and final completion precedes collision work",delegate{
                foreach(int p in new[]{1,2})
                {
                    var u=update(p);Need(u.Navigation.SecondWaypoint!=null&&u.Navigation.Waypoint.Outcome==PracticeGoalWaypointOutcome.RetryHeld&&u.Navigation.SecondWaypoint.Outcome==PracticeGoalWaypointOutcome.RetryHeld&&u.Navigation.WaypointFunctionVisits==2&&u.Navigation.GridFunctionVisits==3,"Counted repeated/held callback "+p);
                    var task=u.State.Backing.CopyTaskRecord(16);Need(BitConverter.ToInt32(task,200)==4&&BitConverter.ToInt32(task,304)==1&&task[284]==1&&BitConverter.ToInt32(task,4)==(p==1?14345:12346)&&u.Movement.FleetUpdates[0].Update.Outcome==PracticeMovementOutcome.ReachedLockedWaypoint,"Held goal/deadline/movement "+p);
                    var copy=u.Navigation.SecondWaypoint.CopyFleetPool();copy[0]^=255;Need(Digest(u.State)==Original[p].Split(',')[3],"Second result aliases final state");
                }
                var final=update(4);Need(final.Navigation.Outcome==PracticeQueuedNavigationOutcome.CompletedGoal&&final.Navigation.WaypointFunctionVisits==0&&final.Prephase.CopyFleetPool()[16*492+482]==254&&final.State.CopyFleetPool()[16*492+482]==255&&final.Movement.FleetUpdates[0].Update.QueueConsumed,"Final target/collision/queue order");
                Need(Digest(update(0).Prephase)==Digest(update(5).Prephase)&&Digest(update(0).State)==Digest(update(5).State)&&update(0).Navigation.GridFunctionVisits==2&&update(5).Navigation.GridFunctionVisits==1,"Identical outputs concealed different call counts");
            });
            check("Single-poll callback studies retain narrow collision admission reset and bounded viewing",delegate{
                foreach(int preset in new[]{12,13})
                {
                    var initial=new PracticeNavigationMovementStudyState(data(),preset);Need(initial.WindowPolls==1&&initial.Polls==0&&initial.RepeatedWaypointVisits==0,"Single-poll preparation");var end=initial.AdvanceUntilCollisionOrReplan();
                    Need(end.Polls==1&&!end.CanAdvance&&end.ReplanEntries==1&&end.WaypointFunctionVisits==2&&end.RepeatedWaypointVisits==1&&end.GridFunctionVisits==3&&end.TerrainCollisionVisits==0&&end.SuppliedWorldEvents==0&&end.Position.X==100&&end.CopyTrail().Length==1,"Supplied FE is not a new movement collision");
                    var reset=end.Reset();Need(reset.Preset==preset&&reset.Polls==0&&reset.RepeatedWaypointVisits==0&&Reject(delegate{end.Advance(1);}),"Single-poll reset/bound");
                }
                var state=data().CreateCollisionEntryComparison(1);string before=Digest(state);
                Need(Reject(delegate{PracticeQueuedNavigation.EvaluateOffline(state.Grid,state.CopyFleetPool(),state.CopyTerrainPool(),state.Backing,249,4,12345,32000,0);}),"Public839FFguard broadened");
                var task=state.Backing.CopyTaskRecord(16);I(task,200,2);var otherGoal=new PracticeNavigationMovementState(state.CopyFleetPool(),state.Backing.WithTaskRecord(16,task),state.Grid,state.CopyTerrainPool(),state.CopySecondaryPool(),state.Schedule,4);
                Need(Reject(delegate{otherGoal.AdvanceOfflinePoll(12345);})&&Digest(state)==before,"Uncompared goal2/currentFE or partial input publication");
            });
            evidence("Coupled callback entry matches all28 original900 full navigation movement states and seven measured traces",delegate{
                string root=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Navigation-Trace-900"),csv=Path.Combine(root,"Cases.csv");Need(Hash(File.ReadAllBytes(csv))=="37826B66499DDCE22B87194EADF51C54C5D18845585CB6DFB4E62E5BC7D3C57C","Original900 metadata pin");var lines=File.ReadAllLines(csv);Need(lines.Length==15,"Trace record count");var initialized=Read(root,"Initialized-Rows.bin",1024000);
                for(int id=0;id<14;id++)
                {
                    var v=lines[id+1].Split(',');int p=id/2,mode=id%2;Need(v.Length==23&&v[0]==id.ToString()&&v[1]==p.ToString()&&v[2]==mode.ToString()&&v[3]=="12345"&&v[4]=="1","Trace record order");string folder=Path.Combine(root,id.ToString("D2")+"-"+p+"-"+mode);
                    var bf=Read(folder,"Before-Fleets.bin",123000);var br=Read(folder,"Before-Region.bin",78560);var rows=Read(folder,"Before-Rows.bin",1024000);var terrain=Read(folder,"Terrain.bin",246000);var nf=Read(folder,"After-Fleets.bin",123000);var nr=Read(folder,"After-Region.bin",78560);var nrows=Read(folder,"After-Rows.bin",1024000);var mf=Read(folder,"Movement-Fleets.bin",123000);var mr=Read(folder,"Movement-Region.bin",78560);var mrows=Read(folder,"Movement-Rows.bin",1024000);
                    Need(Hash(bf)+Hash(br)+Hash(rows)+Hash(terrain)==v[18]&&Hash(nf)+Hash(nr)+Hash(nrows)+Hash(terrain)==v[19]&&Hash(mf)+Hash(mr)+Hash(mrows)+Hash(terrain)==v[20]&&Hash(rows)==Hash(initialized),"Pinned trace full snapshots "+id);
                    var initial=data().CreateCollisionEntryComparison(p);Need(Hash(initial.CopyFleetPool())==Hash(bf)&&Hash(initial.Backing.CopyRegion())==Hash(br)&&Hash(initial.CopyTerrainPool())==Hash(terrain),"Independently built trace input "+id);var u=update(p);var expectedRows=(byte[])rows.Clone();u.Prephase.Grid.CopyCells().CopyTo(expectedRows,16*4096);
                    Need(Hash(u.Prephase.CopyFleetPool())==Hash(nf)&&Hash(u.Prephase.Backing.CopyRegion())==Hash(nr)&&Hash(expectedRows)==Hash(nrows)&&Hash(u.State.CopyFleetPool())==Hash(mf)&&Hash(u.State.Backing.CopyRegion())==Hash(mr)&&Hash(expectedRows)==Hash(mrows)&&u.State.Schedule.BatchStart==int.Parse(v[21])&&u.State.Schedule.CompletedCycles==uint.Parse(v[22])&&u.State.SelectedWaypoint==4,"Full trace navigation/movement/alias output "+id);
                    var vector=Vector(u.Navigation);for(int n=0;n<4;n++)Need(int.Parse(v[8+n])==(mode==0?0:vector[n]),"Original measured site "+id+"/"+n);
                }
            });
        }
    }
}
