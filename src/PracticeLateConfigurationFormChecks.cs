using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
namespace FleetLab
{
    internal static class PracticeLateConfigurationFormChecks
    {
        static void Need(bool condition,string text){if(!condition)throw new Exception(text);}
        static T Find<T>(Form form,string name) where T:Control {return (T)form.Controls.Find(name,true)[0];}
        static void Show(Form form,Form owner){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            Func<string,PracticeMapInstallations> source=delegate(string name){return PracticeMapInstallations.Create(PracticeAirfieldFactory.Load(programs),File.ReadAllBytes(Path.Combine(assets,"Graphics",name)),name);};
            check("Late copied-base form prepares once and resets its own raw roster",delegate
            {
                var original=source("BSSB202.SCN");
                using(var form=new PracticeLateConfigurationStudy(programs,original))
                {
                    Show(form,owner);var prepare=Find<Button>(form,"lateConfigurationPrepare");
                    Need(!form.Prepared&&prepare.Enabled&&BitConverter.ToInt32(form.CopyShipPool(),4*268)==22,"Raw late form profile");
                    prepare.PerformClick();var applied=form.LateState;
                    Need(form.Prepared&&!prepare.Enabled&&applied.ClearedMarkerUnits.Count==3&&BitConverter.ToInt32(form.CopyShipPool(),4*268)==0&&BitConverter.ToInt32(form.CopyShipPool(),5*268)==22,"Prepared even/odd roster");
                    prepare.PerformClick();Need(Object.ReferenceEquals(applied,form.LateState),"Disabled preparation repeated");
                    Find<ComboBox>(form,"lateConfigurationFleet").SelectedIndex=2;
                    Need(Find<ListBox>(form,"lateConfigurationMembers").Items[0].ToString().Contains("unit 4 · type 0"),"Empty referenced record was hidden");
                    form.Size=form.MinimumSize;Application.DoEvents();Need(Find<Control>(form,"lateConfigurationGrid").Width>400,"Late grid minimum size");
                    Find<Button>(form,"lateConfigurationReset").PerformClick();
                    Need(!form.Prepared&&prepare.Enabled&&BitConverter.ToInt32(form.CopyShipPool(),4*268)==22&&BitConverter.ToInt32(original.CopyShipPool(),4*268)==22,"Reset/parent copy isolation");form.Close();
                }
            });
            check("Late form keeps owner selection explicit and retains the source inactive fallback",delegate
            {
                using(var form=new PracticeLateConfigurationStudy(programs,source("BSSB202.SCN")))
                {
                    Show(form,owner);var input=Find<ComboBox>(form,"lateConfigurationOwner");var select=Find<Button>(form,"lateConfigurationSelect");
                    Need(form.SourceSelection==null,"Selection ran during construction");select.PerformClick();var first=form.SourceSelection;
                    Need(first.FoundOwnedFleet&&first.Fleet==0&&first.Unit==0,"Owner1 base fallback");
                    input.SelectedItem=3;Need(Object.ReferenceEquals(first,form.SourceSelection),"Pending owner applied selection");select.PerformClick();Need(!form.SourceSelection.FoundOwnedFleet&&form.SourceSelection.Fleet==0&&form.SourceSelection.Unit==0,"Absent owner did not retain prior");
                    input.SelectedItem=0;select.PerformClick();Need(form.SourceSelection.FoundOwnedFleet&&form.SourceSelection.Fleet==4&&form.SourceSelection.Unit==-1,"Inactive owner0 fallback");
                    Find<Button>(form,"lateConfigurationReset").PerformClick();Need(form.SourceSelection==null&&input.SelectedItem.Equals(0),"Reset selection/input policy");form.Close();
                }
            });
            check("Late form accepts empty and multiple-base sources without adding carrier or inventory data",delegate
            {
                foreach(string name in new[]{"BSSB201.SCN","BSSB205.SCN"})using(var form=new PracticeLateConfigurationStudy(programs,source(name)))
                {
                    Show(form,owner);Find<Button>(form,"lateConfigurationPrepare").PerformClick();
                    Need(form.Prepared&&form.LateState.ClearedMarkerUnits.Count==(name=="BSSB201.SCN"?0:42)&&form.LateState.CarrierGroups.Count==0&&form.LateState.NextInactiveFleet==(name=="BSSB201.SCN"?0:56),"Empty/multiple prepared source "+name);form.Close();
                }
            });
            check("Late form requires released confirmation edges across prepare and reset",delegate
            {
                using(var form=new PracticeLateConfigurationStudy(programs,source("BSSB202.SCN")))
                {
                    Show(form,owner);var flags=BindingFlags.Instance|BindingFlags.NonPublic;var down=typeof(PracticeLateConfigurationStudy).GetMethod("OnKeyDown",flags);var up=typeof(Form).GetMethod("OnKeyUp",flags);
                    form.ActiveControl=Find<Button>(form,"lateConfigurationPrepare");down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.Prepared,"Initial prepare edge");
                    form.ActiveControl=Find<Button>(form,"lateConfigurationReset");down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.Prepared,"Held confirm reset copy");
                    up.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(!form.Prepared,"Released reset edge");
                    form.ActiveControl=Find<Button>(form,"lateConfigurationPrepare");down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(!form.Prepared,"Held reset confirmation prepared again");
                    up.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(form.Prepared,"Released re-prepare edge");form.Close();
                }
            });
        }
    }
}
