using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeFleetServiceFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form f,string name)where T:Control{return(T)f.Controls.Find(name,true)[0];}
        static void Show(Form f,Form owner){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var input=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));
            Func<uint,PracticeMapStartup> startup=delegate(uint seed){return PracticeMapStartup.CreateBssb202(programs,input,seed);};
            check("Fleet service form visits the source task and completes its default supplied queue",delegate
            {
                using(var form=new PracticeFleetServiceStudy(programs,startup(731)))
                {
                    Show(form,owner);Need(form.StudyState.CaseId=="30-1-ordinary"&&form.StudyState.TaskKind==11&&Find<ComboBox>(form,"fleetServiceCase").Items.Count==19&&!Find<Button>(form,"fleetServiceOne").Enabled,"Default service state");
                    Find<Button>(form,"fleetServiceVisit").PerformClick();Need(form.StudyState.TaskKind==12&&form.StudyState.SelectedUnit==31&&!Find<Button>(form,"fleetServiceVisit").Enabled&&Find<Button>(form,"fleetServiceOne").Enabled,"Initial task command admission");
                    Find<Button>(form,"fleetServiceOne").PerformClick();Find<Button>(form,"fleetServiceMany").PerformClick();Need(form.StudyState.QueueEmpty&&form.StudyState.WorkerPasses==5&&!Find<Button>(form,"fleetServiceOne").Enabled&&!Find<Button>(form,"fleetServiceMany").Enabled,"Default service completion prefix");
                    Need(Find<Label>(form,"fleetServiceSummary").Text.Contains("ammo 138")&&Find<TextBox>(form,"fleetServiceDetails").Text.Contains("Queue 0: kind 0"),"Service feedback");form.Size=form.MinimumSize;Application.DoEvents();Need(Find<Control>(form,"fleetServiceCanvas").Width>500&&Find<Panel>(form,"fleetServiceScroll").AutoScroll,"Service minimum layout");form.Close();
                }
            });
            check("Fleet service pending profiles reset and inspection preserve native selection",delegate
            {
                using(var form=new PracticeFleetServiceStudy(programs,startup(731)))
                {
                    Show(form,owner);var initial=form.StudyState;Find<Button>(form,"fleetServiceVisit").PerformClick();Find<ComboBox>(form,"fleetServiceCase").SelectedIndex=PracticeFleetService.CaseIds.IndexOf("30-2-firstworse");Find<ComboBox>(form,"fleetServiceKind").SelectedIndex=1;
                    Need(form.StudyState.CaseId=="30-1-ordinary","Pending service case applied early");Find<Button>(form,"fleetServiceReset").PerformClick();Need(Object.ReferenceEquals(initial,form.StudyState)&&Find<ComboBox>(form,"fleetServiceKind").SelectedIndex==1,"Reset consumed pending service choices");
                    Find<Button>(form,"fleetServiceLoad").PerformClick();Find<Button>(form,"fleetServiceVisit").PerformClick();Need(form.StudyState.CaseId=="30-2-firstworse"&&form.StudyState.InitialTaskKind==12&&form.StudyState.SelectedUnit==30&&Find<ComboBox>(form,"fleetServiceUnit").SelectedItem.ToString()=="31"&&Find<ComboBox>(form,"fleetServiceBay").SelectedIndex==0,"Last-damaged admission inspection");
                    var state=form.StudyState;Find<ComboBox>(form,"fleetServiceUnit").SelectedIndex=0;Need(Object.ReferenceEquals(state,form.StudyState)&&form.StudyState.SelectedUnit==30,"Viewing changed source selection");form.Close();
                }
            });
            check("Startup opens an independent fleet service modal and gates unsupported sources",delegate
            {
                using(var parent=new PracticeMapStartupStudy(programs,input))
                {
                    Show(parent,owner);var initial=parent.StudyState;var button=Find<Button>(parent,"mapStartupService");Need(button.Enabled,"Service button disabled for default final stage");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=0;Need(!button.Enabled,"Early stage service entry");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=3;
                    bool saw=false;Exception failure=null;int ticks=0;using(var timer=new Timer{Interval=100})
                    {
                        timer.Tick+=delegate{if(++ticks>30){timer.Stop();failure=new Exception("Service modal missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var service=child as PracticeFleetServiceStudy;if(service==null)continue;timer.Stop();try{Find<Button>(service,"fleetServiceVisit").PerformClick();Find<Button>(service,"fleetServiceMany").PerformClick();Need(service.StudyState.QueueEmpty,"Modal service prefix");saw=true;}catch(Exception e){failure=e;}finally{service.Close();}return;}};
                        timer.Start();button.PerformClick();timer.Stop();
                    }
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(initial,parent.StudyState)&&parent.AppliedStage.NonemptyFleets==29,"Service modified startup parent");parent.Close();
                }
                using(var bad=new PracticeFleetServiceStudy(programs,startup(0))){Show(bad,owner);Need(bad.StudyState==null&&!Find<Button>(bad,"fleetServiceVisit").Enabled&&Find<Label>(bad,"fleetServiceFeedback").Text.Contains("seed731"),"Unsupported service seed failed clearly");bad.Close();}
            });
            check("Fleet service worker uses released confirmation edges",delegate
            {
                using(var form=new PracticeFleetServiceStudy(programs,startup(731)))
                {
                    Show(form,owner);Find<Button>(form,"fleetServiceVisit").PerformClick();form.ActiveControl=Find<Button>(form,"fleetServiceOne");var flags=BindingFlags.Instance|BindingFlags.NonPublic;var down=typeof(PracticeFleetServiceStudy).GetMethod("OnKeyDown",flags);var up=typeof(Form).GetMethod("OnKeyUp",flags);
                    down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.StudyState.WorkerPasses==1,"Held service update repeated");up.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.StudyState.WorkerPasses==2,"Released service update");form.Close();
                }
            });
            check("Initialized service dispatcher completes and gold follows the returned unit",delegate
            {
                using(var form=new PracticeFleetServiceStudy(programs,startup(731),PracticeFleetServiceMode.InitializedDispatcher))
                {
                    Show(form,owner);Need(form.StudyState.Mode==PracticeFleetServiceMode.InitializedDispatcher&&form.StudyState.RouteUiRequests==1,"Initialized form mode");
                    Find<Button>(form,"fleetServiceVisit").PerformClick();Need(GoldInHalf(form,false)>20&&GoldInHalf(form,true)==0,"Admitted ship gold not in bay");
                    Find<Button>(form,"fleetServiceOne").PerformClick();Find<Button>(form,"fleetServiceMany").PerformClick();Need(form.StudyState.QueueEmpty&&form.StudyState.WorkerPasses==5&&form.StudyState.RouteUiRequests==7,"Initialized dispatch completion");
                    Need(GoldInHalf(form,true)>20&&GoldInHalf(form,false)==0&&Find<TextBox>(form,"fleetServiceDetails").Text.Contains("Gold: viewed unit 30 · current parent 16"),"Returned gold or parent readout stale");form.Close();
                }
            });
            check("Initialized service mode and case stay pending through applied reset",delegate
            {
                using(var form=new PracticeFleetServiceStudy(programs,startup(731),PracticeFleetServiceMode.InitializedDispatcher))
                {
                    Show(form,owner);var applied=form.StudyState;Find<Button>(form,"fleetServiceVisit").PerformClick();Find<ComboBox>(form,"fleetServiceMode").SelectedIndex=0;Find<ComboBox>(form,"fleetServiceKind").SelectedIndex=1;
                    Find<Button>(form,"fleetServiceReset").PerformClick();Need(Object.ReferenceEquals(form.StudyState,applied)&&form.StudyState.Mode==PracticeFleetServiceMode.InitializedDispatcher&&Find<ComboBox>(form,"fleetServiceMode").SelectedIndex==0,"Reset consumed pending mode");
                    Find<Button>(form,"fleetServiceLoad").PerformClick();Find<Button>(form,"fleetServiceVisit").PerformClick();Need(form.StudyState.Mode==PracticeFleetServiceMode.TaggedSelective&&form.StudyState.TaskKind==12,"Legacy baseline changed");
                    Find<ComboBox>(form,"fleetServiceMode").SelectedIndex=2;Find<Button>(form,"fleetServiceLoad").PerformClick();Find<Button>(form,"fleetServiceVisit").PerformClick();Need(form.StudyState.QueueEmpty&&form.StudyState.WorkerPasses==0,"Raw12 dispatcher contrast missing");form.Close();
                }
            });
            check("Service viewed unit follows the appropriate bay without changing simulation",delegate
            {
                using(var form=new PracticeFleetServiceStudy(programs,startup(731),PracticeFleetServiceMode.InitializedDispatcher))
                {
                    Show(form,owner);Find<ComboBox>(form,"fleetServiceCase").SelectedIndex=PracticeFleetService.CaseIds.IndexOf("30-2-firstworse");Find<Button>(form,"fleetServiceLoad").PerformClick();Find<Button>(form,"fleetServiceVisit").PerformClick();Find<Button>(form,"fleetServiceOne").PerformClick();
                    var state=form.StudyState;var view=Find<ComboBox>(form,"fleetServiceUnit");view.SelectedItem=31;Need(Find<ComboBox>(form,"fleetServiceBay").SelectedIndex==0,"Unit31 bay2 not followed");view.SelectedItem=30;Need(Find<ComboBox>(form,"fleetServiceBay").SelectedIndex==1&&Object.ReferenceEquals(state,form.StudyState)&&GoldInHalf(form,false)>20,"Unit30 bay3 selection affected source");form.Size=form.MinimumSize;Application.DoEvents();Need(Find<Panel>(form,"fleetServiceScroll").AutoScroll&&Find<Control>(form,"fleetServiceCanvas").Width>500,"Initialized minimum layout");form.Close();
                }
            });
            check("Startup initialized service entry opens an independent gated copy",delegate
            {
                using(var parent=new PracticeMapStartupStudy(programs,input))
                {
                    Show(parent,owner);var baseline=parent.StudyState;var button=Find<Button>(parent,"mapStartupDispatch");Need(button.Enabled,"Initialized entry disabled");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=0;Need(!button.Enabled,"Early-stage initialized entry admitted");Find<ComboBox>(parent,"mapStartupStage").SelectedIndex=3;
                    bool saw=false;Exception failure=null;int visits=0;using(var timer=new Timer{Interval=100})
                    {
                        timer.Tick+=delegate{if(++visits>30){timer.Stop();failure=new Exception("Initialized modal missing");parent.Close();return;}foreach(Form child in parent.OwnedForms){var study=child as PracticeFleetServiceStudy;if(study==null)continue;timer.Stop();try{Need(study.StudyState.Mode==PracticeFleetServiceMode.InitializedDispatcher,"Wrong child mode");Find<Button>(study,"fleetServiceVisit").PerformClick();saw=true;}catch(Exception e){failure=e;}finally{study.Close();}return;}};timer.Start();button.PerformClick();timer.Stop();
                    }
                    if(failure!=null)throw failure;Need(saw&&Object.ReferenceEquals(baseline,parent.StudyState),"Initialized child changed parent");parent.Close();
                }
            });
        }
        static int GoldInHalf(Form form,bool upper)
        {
            var canvas=Find<Control>(form,"fleetServiceCanvas");canvas.Refresh();using(var bitmap=new Bitmap(canvas.Width,canvas.Height))
            {
                canvas.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));int count=0;
                for(int y=upper?0:bitmap.Height/2;y<(upper?bitmap.Height/2:bitmap.Height);y++)for(int x=0;x<bitmap.Width;x++)if(bitmap.GetPixel(x,y).ToArgb()==Color.Gold.ToArgb())count++;
                return count;
            }
        }
        internal static int SavePreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new fleet service preview directory.");string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);Directory.CreateDirectory(destination);
            using(var form=new PracticeFleetServiceStudy(programs,map))
            {
                Show(form,null);Action<string> capture=delegate(string name){form.PerformLayout();Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
                capture("01-Service-Ready");Find<Button>(form,"fleetServiceVisit").PerformClick();capture("02-Service-Admitted");Find<Button>(form,"fleetServiceMany").PerformClick();capture("03-Service-Task0");
                Find<ComboBox>(form,"fleetServiceCase").SelectedIndex=PracticeFleetService.CaseIds.IndexOf("30-2-firstworse");Find<Button>(form,"fleetServiceLoad").PerformClick();Find<Button>(form,"fleetServiceVisit").PerformClick();capture("04-Less-Damaged-Last");Find<Button>(form,"fleetServiceOne").PerformClick();capture("05-Two-Bays");
                form.Size=form.MinimumSize;Find<Panel>(form,"fleetServiceScroll").AutoScrollPosition=Point.Empty;capture("06-Minimum");Find<Panel>(form,"fleetServiceScroll").ScrollControlIntoView(Find<TextBox>(form,"fleetServiceDetails"));capture("07-Minimum-Details");form.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Seven actual-form DrawToBitmap previews of bounded fleet service task11/12. Supplied contact,tagged queue,manual cadence;default and worse-first multi-bay cases. Not full AI/world timing. No settings saved;physical DPI/input/audio separate.\r\n");return 0;
        }
        internal static int SaveInitializedPreviews(string assets,string destination)
        {
            destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Choose a new initialized service preview directory.");string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var map=PracticeMapStartup.CreateBssb202(programs,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN")),731);Directory.CreateDirectory(destination);
            using(var form=new PracticeFleetServiceStudy(programs,map,PracticeFleetServiceMode.InitializedDispatcher))
            {
                Show(form,null);Action<string> capture=delegate(string name){form.PerformLayout();Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(Path.Combine(destination,name+".png"),System.Drawing.Imaging.ImageFormat.Png);}};
                capture("01-Initialized-Ready");Find<Button>(form,"fleetServiceVisit").PerformClick();capture("02-Initialized-Admitted");Find<Button>(form,"fleetServiceMany").PerformClick();capture("03-Initialized-Returned");
                Find<Panel>(form,"fleetServiceScroll").ScrollControlIntoView(Find<TextBox>(form,"fleetServiceDetails"));capture("04-Returned-Details");
                Find<ComboBox>(form,"fleetServiceKind").SelectedIndex=1;Find<Button>(form,"fleetServiceLoad").PerformClick();Find<Button>(form,"fleetServiceVisit").PerformClick();capture("05-Raw12-Popped");
                Find<ComboBox>(form,"fleetServiceKind").SelectedIndex=0;Find<ComboBox>(form,"fleetServiceCase").SelectedIndex=PracticeFleetService.CaseIds.IndexOf("30-2-firstworse");Find<Button>(form,"fleetServiceLoad").PerformClick();Find<Button>(form,"fleetServiceVisit").PerformClick();Find<Button>(form,"fleetServiceOne").PerformClick();capture("06-Initialized-Two-Bays");
                form.Size=form.MinimumSize;Find<Panel>(form,"fleetServiceScroll").AutoScrollPosition=Point.Empty;capture("07-Minimum");Find<Panel>(form,"fleetServiceScroll").ScrollControlIntoView(Find<TextBox>(form,"fleetServiceDetails"));capture("08-Minimum-Details");form.Close();
            }
            File.WriteAllText(Path.Combine(destination,"Scope.txt"),"Eight actual-form previews of source-initialized service contexts and reached dispatcher paths. Supplied contact/active owner1/manual cadence/6D untouched fill/normalized table identity. Other dispatcher branches reject;full AI driver,world timing,movement and original heap absent. Gold follows viewed ship;orange bay markers persist after departure. No settings saved;physical scaling/input/audio separate.\r\n");return 0;
        }
    }
}
