using System;using System.IO;using System.Drawing;using System.Reflection;using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeNavigationMovementFormChecks
    {
        static void Need(bool ok,string message){if(!ok)throw new InvalidDataException(message);}
        static T Find<T>(Form form,string name)where T:Control{return(T)form.Controls.Find(name,true)[0];}
        static void Show(Form form,Form owner){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show(owner);Application.DoEvents();}
        static void Load(Form form,int preset){Find<ComboBox>(form,"navigationMoveProfile").SelectedIndex=preset;Find<Button>(form,"navigationMoveLoad").PerformClick();}
        static void Advance(Form form,int polls){while(polls>=32){Find<Button>(form,"navigationMoveMany").PerformClick();polls-=32;}while(polls-->0)Find<Button>(form,"navigationMoveOne").PerformClick();}
        static void Key(Form form,bool down,Keys key){typeof(Control).GetMethod(down?"OnKeyDown":"OnKeyUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new object[]{new KeyEventArgs(key)});}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var raw=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));
            Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,raw,731);};
            check("Navigation/movement form shows locked start real scheduler motion and numeric cost cells",delegate{
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    Show(f,owner);Need(f.StudyState.Polls==0&&Find<Button>(f,"navigationMoveOne").Enabled,"Prepared controls");
                    Find<Button>(f,"navigationMoveOne").PerformClick();Need(f.StudyState.Polls==1&&f.StudyState.Position.X==100&&f.StudyState.ActorVisits==1&&f.StudyState.LastUpdate.Movement.FleetUpdates[0].Update.Outcome==PracticeMovementOutcome.ReachedLockedWaypoint,"Initial navigation lock");
                    Find<Button>(f,"navigationMoveReset").PerformClick();Find<Button>(f,"navigationMoveNext").PerformClick();Need(f.StudyState.Polls==32&&f.StudyState.Position.X==102&&f.StudyState.Position.Y==102&&Find<Label>(f,"navigationMoveFeedback").Text.Contains("Pose changed after 32"),"Bounded physical position update");
                    f.SelectCell(13,12);Need(Find<Label>(f,"navigationMoveCell").Text.Contains("Byte 1 (0x01)")&&Find<TextBox>(f,"navigationMoveDetails").ReadOnly,"Read-only cost cell");
                    var old=f.StudyState;Find<ComboBox>(f,"navigationMoveProfile").SelectedIndex=1;Need(Object.ReferenceEquals(old,f.StudyState),"Pending profile applied early");
                    Find<Button>(f,"navigationMoveReset").PerformClick();Need(f.StudyState.Preset==0,"Reset applied pending profile");Find<Button>(f,"navigationMoveLoad").PerformClick();Find<Button>(f,"navigationMoveAll").PerformClick();
                    Need(f.StudyState.Preset==1&&f.StudyState.Polls==256&&f.StudyState.Position.X==130&&f.StudyState.Position.Y==120&&f.StudyState.QueueConsumptions==2&&!Find<Button>(f,"navigationMoveOne").Enabled&&!Find<Button>(f,"navigationMoveWatch").Enabled,"Recorded window endpoint");f.Close();
                }
            });
            check("Navigation/movement form exposes nonmultiple travel final completion and held blocked goal",delegate{
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    Show(f,owner);Load(f,2);Find<Button>(f,"navigationMoveNext").PerformClick();Need(f.StudyState.Polls==1&&f.StudyState.Position.X==7&&f.StudyState.Position.Y==17,"Nonmultiple input teleported");
                    Load(f,3);Find<Button>(f,"navigationMoveNext").PerformClick();Need(f.StudyState.Polls==2&&f.StudyState.NavigationCompletions==1&&f.StudyState.Position.X==320&&Find<Label>(f,"navigationMoveFeedback").Text.Contains("pose held"),"Final completion macro");
                    Load(f,4);Find<Button>(f,"navigationMoveNext").PerformClick();Need(f.StudyState.Polls==64&&f.StudyState.PositionChanges==0&&BitConverter.ToInt32(f.StudyState.State.Backing.CopyTaskRecord(16),200)==2&&Find<Label>(f,"navigationMoveFeedback").Text.Contains("No pose change"),"Blocked state feedback");f.Close();
                }
                using(var f=new PracticeNavigationMovementStudy(programs,PracticeMapStartup.CreateBssb202(programs,raw,732)))
                {Show(f,owner);Need(f.StudyState==null&&!Find<Button>(f,"navigationMoveLoad").Enabled&&!Find<Button>(f,"navigationMoveOne").Enabled,"Unsupported copy enabled");Find<Button>(f,"navigationMoveClose").PerformClick();Need(f.IsDisposed,"Unsupported copy not closable");}
            });
            check("Navigation/movement viewing stops on focus and pending changes with held-edge and minimum-size guards",delegate{
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    Show(f,owner);Find<Button>(f,"navigationMoveWatch").PerformClick();Need(f.Watching,"Watch did not start");f.StepWatchForCheck();Need(f.StudyState.Polls==1,"Watch tick did not supply one poll");
                    Find<ComboBox>(f,"navigationMoveProfile").SelectedIndex=1;Need(!f.Watching&&f.StudyState.Preset==0,"Pending profile did not stop viewing");
                    Find<Button>(f,"navigationMoveWatch").PerformClick();typeof(Form).GetMethod("OnDeactivate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,new object[]{EventArgs.Empty});Need(!f.Watching,"Focus loss did not stop");
                    f.ActiveControl=Find<Button>(f,"navigationMoveReset");Key(f,false,Keys.Enter);Key(f,true,Keys.Enter);var reset=f.StudyState;Key(f,true,Keys.Enter);Need(Object.ReferenceEquals(reset,f.StudyState),"Held reset repeated");Key(f,false,Keys.Enter);Key(f,true,Keys.Enter);Need(!Object.ReferenceEquals(reset,f.StudyState),"Released reset lost");Key(f,false,Keys.Enter);
                    Find<Button>(f,"navigationMoveWatch").PerformClick();for(int n=0;n<32;n++)f.StepWatchForCheck();Need(!f.Watching&&f.StudyState.Polls==32,"Watch exceeded bounded32ticks");
                    f.Size=f.MinimumSize;Application.DoEvents();var scroll=Find<Panel>(f,"navigationMoveScroll");var close=Find<Button>(f,"navigationMoveClose");scroll.ScrollControlIntoView(close);Application.DoEvents();var box=scroll.RectangleToClient(close.RectangleToScreen(close.ClientRectangle));Need(scroll.AutoScroll&&Find<Control>(f,"navigationMoveCanvas").Width>480&&box.Bottom<=scroll.ClientSize.Height+2,"Minimum-size controls clipped");
                    Find<Button>(f,"navigationMoveWatch").PerformClick();f.Close();Need(f.IsDisposed,"Close while watching failed");
                }
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    // Dispose without Show: there is no Deactivate event to hide a timer leak.
                    var flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(PracticeNavigationMovementStudy);
                    var timer=(Timer)type.GetField("watch",flags).GetValue(f);type.GetField("watchRemaining",flags).SetValue(f,32);timer.Start();Need(f.Watching,"Disposal setup did not start watch");int polls=f.StudyState.Polls;
                    f.Dispose();Need(f.IsDisposed&&!f.Watching&&!timer.Enabled,"Direct disposal retained viewing timer");f.StepWatchForCheck();Need(f.StudyState.Polls==polls,"Late tick changed disposed study");f.Dispose();
                }
            });
            check("Navigation/movement form shows terrain and fleet collision before the next source replan",delegate{
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    Show(f,owner);Load(f,9);Need(Find<ComboBox>(f,"navigationMoveProfile").Items.Count==14&&Find<Button>(f,"navigationMoveAll").Text.Contains("1024"),"Long presets/window action absent");
                    Advance(f,65);Need(f.StudyState.SuppliedWorldEvents==0,"Early supplied world event");Find<Button>(f,"navigationMoveOne").PerformClick();Need(f.StudyState.SuppliedWorldEvents==1&&f.StudyState.ReplanEntries==0,"Insertion skipped collision stage");
                    Find<Button>(f,"navigationMoveCollision").PerformClick();Need(f.StudyState.Polls==95&&f.StudyState.TerrainCollisionVisits==1&&f.StudyState.ReplanEntries==0&&Find<Label>(f,"navigationMoveSummary").Text.Contains("collision FE"),"Terrain collision not observable");
                    Find<Button>(f,"navigationMoveCollision").PerformClick();Need(f.StudyState.Polls==96&&f.StudyState.ReplanEntries==1&&Find<Label>(f,"navigationMoveSummary").Text.Contains("Last navigation: Replanned")&&Find<TextBox>(f,"navigationMoveDetails").Text.Contains("before poll 65"),"Next-prephase replan or supplied-input explanation absent");
                    Find<Button>(f,"navigationMoveAll").PerformClick();Need(f.StudyState.Polls==1024&&f.StudyState.Position.X==160&&!Find<Button>(f,"navigationMoveOne").Enabled,"Long end window");
                    Load(f,11);Advance(f,95);Need(f.StudyState.FleetCollisionVisits==1&&Find<Label>(f,"navigationMoveSummary").Text.Contains("collision 1E"),"Raw source slot30 not shown");Find<Button>(f,"navigationMoveOne").PerformClick();Need(f.StudyState.ReplanEntries==1,"Fleet replan absent");f.Close();
                }
            });
            check("Navigation/movement long presets retain blocked wall reset removal and bounded viewing semantics",delegate{
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    Show(f,owner);Load(f,7);Find<Button>(f,"navigationMoveAll").PerformClick();Need(f.StudyState.PositionChanges==0&&f.StudyState.Polls==1024&&f.StudyState.NavigationCompletions==0,"Closed wall moved or completed");
                    Load(f,10);Advance(f,193);Need(f.StudyState.SuppliedWorldEvents==1&&f.StudyState.State.CopyTerrainPool()[0]==1&&Find<Label>(f,"navigationMoveCell").Text.Contains("terrain 1"),"Removal happened early");Find<Button>(f,"navigationMoveOne").PerformClick();Need(f.StudyState.SuppliedWorldEvents==2&&f.StudyState.State.CopyTerrainPool()[0]==0&&Find<Label>(f,"navigationMoveCell").Text.Contains("terrain 0"),"Removal not reflected");
                    Find<Button>(f,"navigationMoveReset").PerformClick();Need(f.StudyState.Preset==10&&f.StudyState.Polls==0&&f.StudyState.ReplanEntries==0&&f.StudyState.SuppliedWorldEvents==0,"Long reset retained event/collision state");Advance(f,64);Find<Button>(f,"navigationMoveWatch").PerformClick();f.StepWatchForCheck();f.StepWatchForCheck();Need(f.StudyState.Polls==66&&f.StudyState.SuppliedWorldEvents==1,"Viewing duplicated or omitted boundary input");
                    Find<ComboBox>(f,"navigationMoveProfile").SelectedIndex=6;Need(!f.Watching&&f.StudyState.Preset==10,"Pending long preset advanced/loaded early");f.Size=f.MinimumSize;Application.DoEvents();var scroll=Find<Panel>(f,"navigationMoveScroll");var close=Find<Button>(f,"navigationMoveClose");scroll.ScrollControlIntoView(close);Application.DoEvents();Need(scroll.RectangleToClient(close.RectangleToScreen(close.ClientRectangle)).Bottom<=scroll.ClientSize.Height+2,"Long details/Close clipped");f.Close();
                }
            });
            check("Navigation/movement single-poll views expose counted repeated callbacks and bounded watch reset",delegate{
                using(var f=new PracticeNavigationMovementStudy(programs,startup()))
                {
                    Show(f,owner);Load(f,12);Need(f.StudyState.WindowPolls==1&&Find<Button>(f,"navigationMoveAll").Text.Contains("single")&&Find<Label>(f,"navigationMoveFeedback").Text.Contains("collision FE is supplied"),"Supplied current-FE scope absent");
                    Find<Button>(f,"navigationMoveCollision").PerformClick();Need(f.StudyState.Polls==1&&f.StudyState.Position.X==100&&f.StudyState.WaypointFunctionVisits==2&&f.StudyState.RepeatedWaypointVisits==1&&f.StudyState.GridFunctionVisits==3&&f.StudyState.TerrainCollisionVisits==0&&!Find<Button>(f,"navigationMoveOne").Enabled&&Find<Label>(f,"navigationMoveSummary").Text.Contains("Waypoint calls 2; repeated 1; grid calls 3"),"Counted repeated callback not observable");
                    Find<Button>(f,"navigationMoveReset").PerformClick();Need(f.StudyState.Polls==0&&f.StudyState.RepeatedWaypointVisits==0&&f.StudyState.State.CopyFleetPool()[16*492+482]==254,"Reset lost supplied FE input");
                    Load(f,13);Find<Button>(f,"navigationMoveWatch").PerformClick();f.StepWatchForCheck();Need(!f.Watching&&f.StudyState.Polls==1&&f.StudyState.RepeatedWaypointVisits==1&&BitConverter.ToInt32(f.StudyState.State.Backing.CopyTaskRecord(16),4)==12346,"Single-poll watch exceeded window or lost held deadline");f.Close();
                }
            });
            check("Recovered maps opens copied navigation/movement without altering the active battle",delegate{
                var main=owner as MainForm;Need(main!=null,"Main form required");var tabs=(TabControl)typeof(MainForm).GetField("tabs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(main);int previous=tabs.SelectedIndex,maps=-1;
                for(int n=0;n<tabs.TabPages.Count;n++)if(tabs.TabPages[n].Text=="Recovered maps")maps=n;Need(maps>=0,"Maps tab missing");tabs.SelectedIndex=maps;Application.DoEvents();var battle=main.State;int events=battle.Events.Count;bool saw=false;Exception error=null;
                using(var timer=new Timer{Interval=100})
                {
                    timer.Tick+=delegate{foreach(Form child in main.OwnedForms){var f=child as PracticeNavigationMovementStudy;if(f==null)continue;timer.Stop();try{Find<Button>(f,"navigationMoveNext").PerformClick();Need(f.StudyState.Position.X==102&&f.StudyState.Polls==32,"Direct moving study failed");saw=true;}catch(Exception e){error=e;}finally{f.Close();}return;}};
                    timer.Start();Find<Button>(main,"openNavigationMovementStudy").PerformClick();timer.Stop();
                }
                tabs.SelectedIndex=previous;if(error!=null)throw error;Need(saw&&Object.ReferenceEquals(battle,main.State)&&battle.Events.Count==events,"Study changed active battle");
            });
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a fresh movement preview directory.");Directory.CreateDirectory(destination);
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var startup=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);
            using(var f=new PracticeNavigationMovementStudy(programs,startup))
            {
                Show(f,null);Action<string> capture=delegate(string name){f.PerformLayout();Application.DoEvents();using(var image=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(image,new Rectangle(Point.Empty,f.Size));image.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
                capture("01-Prepared");Find<Button>(f,"navigationMoveOne").PerformClick();capture("02-Locked-Start");Find<Button>(f,"navigationMoveReset").PerformClick();Find<Button>(f,"navigationMoveNext").PerformClick();capture("03-First-Motion");Find<Button>(f,"navigationMoveAll").PerformClick();capture("04-Default-256");
                Load(f,1);Find<Button>(f,"navigationMoveAll").PerformClick();capture("05-Fast-Complete");Load(f,2);Find<Button>(f,"navigationMoveNext").PerformClick();capture("06-Nonmultiple");Load(f,3);Find<Button>(f,"navigationMoveNext").PerformClick();capture("07-Already-Final");Load(f,4);Find<Button>(f,"navigationMoveNext").PerformClick();capture("08-Blocked-64");
                Load(f,1);Find<Button>(f,"navigationMoveNext").PerformClick();f.Size=f.MinimumSize;capture("09-Minimum");Find<Panel>(f,"navigationMoveScroll").ScrollControlIntoView(Find<TextBox>(f,"navigationMoveDetails"));capture("10-Minimum-Details");
                f.Size=new Size(1180,850);Find<Panel>(f,"navigationMoveScroll").AutoScrollPosition=Point.Empty;
                Load(f,6);Advance(f,256);capture("11-Primary-Gap-256");Find<Button>(f,"navigationMoveAll").PerformClick();capture("12-Primary-Gap-Complete");
                Load(f,7);Find<Button>(f,"navigationMoveAll").PerformClick();capture("13-Closed-Wall");Load(f,8);Advance(f,256);capture("14-Terrain-Gap-256");
                Load(f,9);Advance(f,95);capture("15-Terrain-Collision-95");Find<Button>(f,"navigationMoveOne").PerformClick();capture("16-Terrain-Replan-96");Find<Button>(f,"navigationMoveAll").PerformClick();capture("17-Terrain-Complete");
                Load(f,10);Advance(f,194);capture("18-Terrain-Removed-194");Load(f,11);Advance(f,95);capture("19-Fleet-Collision-95");Find<Button>(f,"navigationMoveOne").PerformClick();f.Size=f.MinimumSize;capture("20-Fleet-Replan-Minimum");
                f.Size=new Size(1180,850);Find<Panel>(f,"navigationMoveScroll").AutoScrollPosition=Point.Empty;Load(f,12);capture("21-Repeated-Blocked-Prepared");Find<Button>(f,"navigationMoveOne").PerformClick();capture("22-Repeated-Blocked-After");
                Load(f,13);Find<Button>(f,"navigationMoveAll").PerformClick();capture("23-Repeated-Future-After");f.Size=f.MinimumSize;Find<Panel>(f,"navigationMoveScroll").ScrollControlIntoView(Find<TextBox>(f,"navigationMoveDetails"));capture("24-Repeated-Minimum-Details");f.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Twentyfour actual-form navigation/movement previews. Original864/875/884/900profiles,sourcecosts/two-batchscheduler/arrivallocks/collisionreplan/countedrepeat/sharedbacking;suppliedclock/phase/testobstacles/collisioninputs/selectedother. Goldtrail/continuousmarkers aremoderninspectionaids. No fullownercontroller/naturalpacing/originalrenderer/audio/network/battle/settings changes. PhysicalDPI/input remainpending.\r\n");return 0;
        }
    }
}
