using System;using System.IO;
namespace FleetLab
{
    internal static class PracticeNavigationCollisionFixtureChecks
    {
        static void Need(bool value,string reason){PracticeNavigationCollisionChecks.Need(value,reason);}
        static string Hash(byte[] b){return PracticeNavigationCollisionChecks.Hash(b);}
        static byte[] Read(BinaryReader r,int size){var b=r.ReadBytes(size);Need(b.Length==size,"Long navigation fixture truncated");return b;}
        static byte[] Patch(BinaryReader r,byte[] basis)
        {var b=(byte[])basis.Clone();int count=r.ReadInt32(),last=-1;Need(count>=0&&count<=b.Length,"Long navigation delta count");for(int n=0;n<count;n++){int at=r.ReadInt32();Need(at>last&&at<b.Length,"Long navigation delta index");last=at;b[at]=r.ReadByte();}return b;}
        static void Compare(PracticeNavigationMovementState state,byte[] f,byte[] region,byte[] terrain,byte[] rows,byte[] expectedRows,int cursor,uint cycles,int selection,string label)
        {
            state.Grid.CopyCells().CopyTo(expectedRows,16*4096);
            Need(Hash(state.CopyFleetPool())==Hash(f)&&Hash(state.Backing.CopyRegion())==Hash(region)&&Hash(state.CopyTerrainPool())==Hash(terrain)&&Hash(expectedRows)==Hash(rows)&&state.Schedule.BatchStart==cursor&&state.Schedule.CompletedCycles==cycles&&state.SelectedWaypoint==selection,label);
        }
        internal static void Run(Action<string,Action> evidence,string assets,Func<PracticeGoalRouteStudyBasis> basis)
        {
            foreach(int capture in new[]{875,884})
            {
                int selected=capture;
                evidence("Navigation/movement matches original"+capture+" full long-history snapshots and supplied world inputs",delegate{
                    string path=Path.Combine(Path.GetDirectoryName(assets),"Evidence","Navigation-Movement-"+selected+".bin");
                    string pin=selected==875?"C66529DAEDB82CA2DC3B727B320625CB671C0B251BAE99C8B6B70811987DAD96":"C1495760D67179B73DF20A9ED9864B154ADA29A08CCF04D014131D1A4E2CF15C";
                    Need(Hash(File.ReadAllBytes(path))==pin,"Long fixture identity "+selected);int histories=selected==875?3:4,count=0;
                    PracticeNavigationMovementState state=null;PracticeNavigationMovementUpdate update=null;byte[] expectedRows=null;
                    using(var r=new BinaryReader(File.OpenRead(path)))
                    {
                        Need(System.Text.Encoding.ASCII.GetString(Read(r,8))=="NM"+selected+"001"&&r.ReadInt32()==1&&r.ReadInt32()==histories&&r.ReadInt32()==1024&&r.ReadInt32()==histories*2048,"Long fixture header");
                        Read(r,402000);var baselineF=Read(r,123000);var zeroTerrain=selected==884?Read(r,246000):new byte[246000];Read(r,78000);var baselineR=Read(r,78560);Read(r,64000);var baselineRows=Read(r,1024000);
                        for(;;)
                        {
                            int id=r.ReadInt32();if(id==-1)break;
                            int history=r.ReadInt32(),poll=r.ReadInt32(),stage=r.ReadInt32(),clock=r.ReadInt32(),phase=r.ReadInt32(),cursorBefore=r.ReadInt32(),cursorAfter=r.ReadInt32();
                            uint cyclesBefore=r.ReadUInt32(),cyclesAfter=r.ReadUInt32();int selectionBefore=r.ReadInt32(),selectionAfter=r.ReadInt32();int worldEvent=selected==884?r.ReadInt32():0;
                            var terrain=selected==884?Patch(r,zeroTerrain):zeroTerrain;var bf=Patch(r,baselineF);var br=Patch(r,baselineR);var beforeRows=Patch(r,baselineRows);var af=Patch(r,bf);var ar=Patch(r,br);var afterRows=Patch(r,beforeRows);
                            int preset=selected==875?5+history:8+history;
                            Need(id==count&&history==id/2048&&poll==id/2%1024&&stage==id%2&&clock==12345+40*poll&&phase==poll+1&&selectionBefore==4&&selectionAfter==4&&worldEvent==(stage==0?PracticeGoalRouteStudyBasis.MovementWorldEvent(preset,poll):0),"Long fixture metadata "+id);
                            if(poll==0&&stage==0){state=basis().CreateMovementComparison(preset);expectedRows=(byte[])baselineRows.Clone();}
                            if(stage==0)state=PracticeGoalRouteStudyBasis.ApplyMovementWorldEvent(state,preset,poll);
                            var before=stage==0?state:update.Prephase;
                            Compare(before,bf,br,terrain,beforeRows,expectedRows,cursorBefore,cyclesBefore,selectionBefore,"Full carried long input "+id);
                            if(stage==0)update=state.AdvanceOfflinePoll(clock);
                            var after=stage==0?update.Prephase:update.State;
                            Compare(after,af,ar,terrain,afterRows,expectedRows,cursorAfter,cyclesAfter,selectionAfter,"Full long output "+id);
                            if(stage==1)state=update.State;count++;
                        }
                        Need(count==histories*2048&&r.BaseStream.Position==r.BaseStream.Length,"Long fixture complete extent");
                    }
                });
            }
        }
    }
}
