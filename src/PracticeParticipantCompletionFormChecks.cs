using System;using System.IO;using System.Drawing;using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeParticipantCompletionFormChecks
    {
        static T C<T>(Form form,string name)where T:Control{return(T)form.Controls.Find(name,true)[0];}
        static void Need(bool value,string message){if(!value)throw new Exception(message);}
        static void Click(Form form,string name){C<Button>(form,name).PerformClick();Application.DoEvents();}
        static void Show(Form form,Form owner){form.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            check("Participant form shows a prepared copy and source completion without changing existing records",delegate{
                using(var form=new PracticeParticipantCompletionStudy(2,1,delegate{}))
                {
                    Show(form,owner);Need(form.StateSnapshot==null&&C<DataGridView>(form,"participantRecords").Rows.Count==4&&C<Button>(form,"participantComplete").Enabled&&!C<Button>(form,"participantRepeat").Enabled,"Prepared four-record view");Click(form,"participantComplete");
                    Need(form.StateSnapshot.FinalCount==2&&form.StateSnapshot.KindAt(1)==2&&form.StateSnapshot.DisplayNameAt(1)=="CPU"&&C<Label>(form,"participantSummary").Text.Contains("current 1 -> 2"),"Source single-record completion feedback");Need(C<TextBox>(form,"participantDetails").ReadOnly&&C<Button>(form,"participantUseCounts").Enabled,"Reviewable detail and explicit apply");
                }
            });
            check("Participant form exposes counted formation admissions and idempotent repeat",delegate{
                using(var form=new PracticeParticipantCompletionStudy(4,1,delegate{}))
                {
                    Show(form,owner);C<ComboBox>(form,"participantMode").SelectedIndex=2;Click(form,"participantPrepare");Click(form,"participantComplete");Need(form.StateSnapshot.NameRequests.Count==3&&form.StateSnapshot.FormationRequests.Count==3&&C<TextBox>(form,"participantDetails").Text.Contains("1 2 3"),"Mode5per-insert requests visible");string before=BitConverter.ToString(form.StateSnapshot.CopyRecords());Click(form,"participantRepeat");Need(form.StateSnapshot.NameRequests.Count==0&&form.StateSnapshot.FormationRequests.Count==0&&before==BitConverter.ToString(form.StateSnapshot.CopyRecords()),"Repeat makes no new record/name/formation request");
                }
            });
            check("Participant form preserves oversubscription and transfers completed counts as a pending parent choice",delegate{
                string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");using(var parent=new PracticeGeneratedReinforcementStudy(programs))
                {
                    Show(parent,owner);var original=parent.StateSnapshot;Need(parent.Controls.Find("generatedParticipants",true).Length==1,"Discoverable participant-study entry");
                    using(var form=new PracticeParticipantCompletionStudy(2,4,parent.ApplyCompletedParticipantCounts))
                    {
                        Show(form,parent);Click(form,"participantComplete");Need(form.StateSnapshot.FinalCount==4&&!C<Button>(form,"participantUseCounts").Enabled&&C<Label>(form,"participantSummary").Text.Contains("Oversubscribed"),"No invented count truncation or invalid apply");
                        C<NumericUpDown>(form,"participantConfigured").Value=3;C<NumericUpDown>(form,"participantCurrent").Value=1;Click(form,"participantPrepare");Click(form,"participantComplete");Click(form,"participantUseCounts");
                        Need(C<ComboBox>(parent,"generatedCounts").SelectedIndex==4&&object.ReferenceEquals(parent.StateSnapshot,original)&&C<Label>(parent,"generatedPending").Text.Contains("pending"),"Use counts updates selection only");
                    }
                    Click(parent,"generatedLoad");Need(parent.StateSnapshot.ProducerCount==3&&parent.StateSnapshot.ConsumerCount==3,"Explicit parent Load applies completed counts");
                }
            });
            check("Participant form reset,pending edits,Enter edges and minimum scrolling preserve a fixed Close",delegate{
                using(var form=new PracticeParticipantCompletionStudy(2,1,null))
                {
                    Show(form,owner);Click(form,"participantComplete");C<NumericUpDown>(form,"participantConfigured").Value=4;Click(form,"participantReset");Need(form.StateSnapshot==null&&C<NumericUpDown>(form,"participantConfigured").Value==4&&C<Label>(form,"participantPending").Text.Contains("pending"),"Applied reset retains pending input");
                    form.StudyKey(Keys.Enter,false);form.StudyKey(Keys.Enter,true);Click(form,"participantComplete");C<NumericUpDown>(form,"participantConfigured").Value=3;form.StudyKey(Keys.Enter,true);Need(form.StateSnapshot.ConfiguredCount==4,"Held Enter suppressed");form.StudyKey(Keys.Enter,false);form.StudyKey(Keys.Enter,true);Need(form.StateSnapshot==null&&C<Label>(form,"participantSummary").Text.Contains("total: 3"),"New Enter press prepares pending copy");form.StudyKey(Keys.Enter,false);
                    form.ClientSize=new Size(940,620);var details=C<TextBox>(form,"participantDetails");details.Focus();C<Panel>(form,"participantScroll").ScrollControlIntoView(details);Application.DoEvents();var close=C<Button>(form,"participantClose");Need(details.Width>=860&&details.ForeColor!=details.BackColor&&form.ClientRectangle.Contains(new Rectangle(form.PointToClient(close.PointToScreen(Point.Empty)),close.Size)),"Readable scrolling detail/fixed Close");Click(form,"participantClose");Need(form.IsDisposed||!form.Visible,"Clean participant-study exit");
                }
            });
        }
        static void Save(Form form,string file){Application.DoEvents();using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(file,System.Drawing.Imaging.ImageFormat.Png);}}
        internal static int SavePreviews(string assets,string output)
        {
            try
            {
                Directory.CreateDirectory(output);using(var form=new PracticeParticipantCompletionStudy(2,1,delegate{}))
                {
                    form.Show();Application.DoEvents();Save(form,Path.Combine(output,"01-prepared.png"));Click(form,"participantComplete");Save(form,Path.Combine(output,"02-completed.png"));C<NumericUpDown>(form,"participantConfigured").Value=4;C<ComboBox>(form,"participantMode").SelectedIndex=2;Click(form,"participantPrepare");Click(form,"participantComplete");Save(form,Path.Combine(output,"03-formation-requests.png"));C<NumericUpDown>(form,"participantConfigured").Value=2;C<NumericUpDown>(form,"participantCurrent").Value=4;Click(form,"participantPrepare");Click(form,"participantComplete");Save(form,Path.Combine(output,"04-oversubscribed.png"));form.ClientSize=new Size(940,620);var details=C<TextBox>(form,"participantDetails");details.Focus();C<Panel>(form,"participantScroll").ScrollControlIntoView(details);Save(form,Path.Combine(output,"05-minimum-details.png"));var configured=C<NumericUpDown>(form,"participantConfigured");configured.Focus();C<Panel>(form,"participantScroll").ScrollControlIntoView(configured);Save(form,Path.Combine(output,"06-minimum-controls.png"));form.Close();
                }
                return 0;
            }
            catch(Exception ex){File.WriteAllText(Path.Combine(output,"Error.txt"),ex.ToString());return 1;}
        }
    }
}
