using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeMapStartupFormChecks
    {
        static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
        static T Find<T>(Form form,string name)where T:Control{return(T)form.Controls.Find(name,true)[0];}
        static void Show(Form form,Form owner){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show(owner);Application.DoEvents();}
        static void Fleet(Form form,int slot){var choices=Find<ComboBox>(form,"mapStartupFleet");for(int n=0;n<choices.Items.Count;n++)if(choices.Items[n].ToString().StartsWith(slot+" ·")){choices.SelectedIndex=n;return;}throw new Exception("Missing fleet "+slot);}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");byte[] source=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));
            check("Ordinary startup form exposes four copied stages without advancing the world",delegate
            {
                using(var form=new PracticeMapStartupStudy(programs,source))
                {
                    Show(form,owner);var original=form.StudyState;var stages=Find<ComboBox>(form,"mapStartupStage");
                    Need(original!=null&&original.Seed==731&&stages.Items.Count==4&&stages.SelectedIndex==3&&form.SelectedFleet==16&&form.SelectedUnit==30&&form.AppliedStage.NonemptyFleets==29&&form.AppliedStage.NonemptyUnits==63,"Default late startup stage");
                    stages.SelectedIndex=0;Need(form.AppliedStage.NonemptyFleets==14&&form.AppliedStage.NonemptyUnits==20&&form.SelectedFleet==0,"Initial stage copy");
                    stages.SelectedIndex=2;Need(form.AppliedStage.RandomState==249622383&&form.AppliedStage.RandomCalls==484&&Object.ReferenceEquals(original,form.StudyState),"Viewing stage changed setup");
                    form.Size=form.MinimumSize;Application.DoEvents();Need(Find<Control>(form,"mapStartupWorld").Width>450,"Startup minimum world layout");form.Close();
                }
            });
            check("Startup seed changes stay pending and reset uses the applied seed",delegate
            {
                using(var form=new PracticeMapStartupStudy(programs,source))
                {
                    Show(form,owner);var initial=form.StudyState;var seed=Find<NumericUpDown>(form,"mapStartupSeed");seed.Value=1;
                    Need(Object.ReferenceEquals(initial,form.StudyState),"Pending seed rebuilt setup");Find<Button>(form,"mapStartupLoad").PerformClick();Need(form.StudyState.Seed==1&&!Object.ReferenceEquals(initial,form.StudyState),"Explicit seed load");
                    seed.Value=0;Find<Button>(form,"mapStartupReset").PerformClick();Need(form.StudyState.Seed==1&&seed.Value==0,"Reset consumed pending seed");
                    seed.Value=uint.MaxValue;Find<Button>(form,"mapStartupLoad").PerformClick();Need(form.StudyState.Seed==uint.MaxValue&&form.AppliedStage.RandomState==3482650137&&form.AppliedStage.RandomCalls==586,"Unsigned numeric seed");form.Close();
                }
            });
            check("Startup roster views retain raw and cleared marker fields across stages",delegate
            {
                using(var form=new PracticeMapStartupStudy(programs,source))
                {
                    Show(form,owner);Fleet(form,2);var members=Find<ListBox>(form,"mapStartupMembers");Need(form.SelectedUnit==4&&members.Items[0].ToString().Contains("type 0")&&members.Items[1].ToString().Contains("type 22"),"Late even/odd marker view");
                    Find<ComboBox>(form,"mapStartupStage").SelectedIndex=0;Need(form.SelectedFleet==2&&members.Items[0].ToString().Contains("type 22"),"Raw snapshot did not restore marker display");
                    Find<TabControl>(form,"mapStartupViews").SelectedIndex=1;members.SelectedIndex=1;Need(Find<TextBox>(form,"mapStartupDetails").Text.Contains("Unit 5 / type 22"),"Selected marker details");form.Close();
                }
            });
            check("Startup form rejects unadmitted source and isolates its source copy",delegate
            {
                using(var bad=new PracticeMapStartupStudy(programs,new byte[0]))
                {
                    Show(bad,owner);Need(bad.StudyState==null&&!Find<Button>(bad,"mapStartupReset").Enabled&&Find<Label>(bad,"mapStartupFeedback").Text.Contains("Startup rejected"),"Unadmitted source failed clearly");bad.Close();
                }
                var input=(byte[])source.Clone();using(var good=new PracticeMapStartupStudy(programs,input))
                {
                    Show(good,owner);input[0]^=255;Find<Button>(good,"mapStartupReset").PerformClick();Need(good.StudyState!=null&&good.AppliedStage.NonemptyFleets==29,"Caller source mutated held copy");good.Close();
                }
            });
            check("Startup load requires released confirmation edges",delegate
            {
                using(var form=new PracticeMapStartupStudy(programs,source))
                {
                    Show(form,owner);form.ActiveControl=Find<Button>(form,"mapStartupLoad");var flags=BindingFlags.Instance|BindingFlags.NonPublic;var down=typeof(PracticeMapStartupStudy).GetMethod("OnKeyDown",flags);var up=typeof(Form).GetMethod("OnKeyUp",flags);
                    down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});var first=form.StudyState;down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(Object.ReferenceEquals(first,form.StudyState),"Held load confirmation repeated");
                    up.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});down.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});Need(!Object.ReferenceEquals(first,form.StudyState)&&form.AppliedStage.RandomCalls==484,"Released load edge or deterministic reset");form.Close();
                }
            });
        }
    }
}
