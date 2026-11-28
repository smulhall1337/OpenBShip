using System;using System.IO;using System.Drawing;using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeReinforcementPhaseFormChecks
    {
        static T C<T>(Form form,string name)where T:Control{return(T)form.Controls.Find(name,true)[0];}
        static void Need(bool value,string message){if(!value)throw new Exception(message);}
        static void Click(Form form,string name){C<Button>(form,name).PerformClick();Application.DoEvents();}
        static void Show(Form form,Form owner){form.Show(owner);Application.DoEvents();}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            check("Phase-wave form starts an explicit completed2/2 idle copy and supplied equality frame",delegate{
                using(var parent=new PracticeGeneratedReinforcementStudy(programs))using(var form=new PracticeReinforcementPhaseStudy(programs))
                {
                    Need(parent.Controls.Find("generatedPhaseWaves",true).Length==1,"Discoverable phase-study entry");Show(form,owner);Need(form.StateSnapshot.Frames==0&&form.StateSnapshot.World.ConsumerCount==2&&C<DataGridView>(form,"phaseWaveTrace").Rows.Count==0,"Prepared source world");Click(form,"phaseWaveStep");Need(form.StateSnapshot.Frames==1&&form.StateSnapshot.PhaseCounter==1&&form.StateSnapshot.ConsumerPolls==0&&form.StateSnapshot.World.CreatedFleetCount==0,"Explicit equality input is not an offphase poll");Need(C<TextBox>(form,"phaseWaveDetails").ReadOnly&&C<ComboBox>(form,"phaseWavePause").ForeColor!=C<ComboBox>(form,"phaseWavePause").BackColor,"Readable inspection controls");
                }
            });
            check("Phase-wave form shows source frame32/64 bursts and independently inspects past rows",delegate{
                using(var form=new PracticeReinforcementPhaseStudy(programs))
                {
                    Show(form,owner);Click(form,"phaseWave32");Need(form.StateSnapshot.Frames==32&&form.StateSnapshot.World.CreatedFleetCount==1&&form.StateSnapshot.ConsumerPolls==1,"Source first wave at displayed32");Click(form,"phaseWave32");Need(form.StateSnapshot.Frames==64&&form.StateSnapshot.World.CreatedFleetCount==8&&C<DataGridView>(form,"phaseWaveTrace").Rows.Count==64,"All eight by displayed64");C<DataGridView>(form,"phaseWaveTrace").CurrentCell=C<DataGridView>(form,"phaseWaveTrace").Rows[0].Cells[0];Application.DoEvents();Need(form.ShownSnapshot.Frames==1&&form.StateSnapshot.Frames==64&&C<TextBox>(form,"phaseWaveDetails").Text.Contains("Viewing frame 1"),"Historical inspection does not rewind current state");
                }
            });
            check("Phase-wave form displays negative pause holds,delayed frame67 and MAX wrap equality",delegate{
                using(var form=new PracticeReinforcementPhaseStudy(programs))
                {
                    Show(form,owner);C<ComboBox>(form,"phaseWavePause").SelectedIndex=2;Click(form,"phaseWaveLoad");Click(form,"phaseWave32");Click(form,"phaseWaveStep");Need(form.StateSnapshot.Frames==33&&form.StateSnapshot.PhaseCounter==32&&form.StateSnapshot.World.CreatedFleetCount==1,"Held phase32 with due clock");Click(form,"phaseWaveFinish");Need(form.StateSnapshot.Frames==96&&form.StateSnapshot.World.CreatedFleetCount==8&&!C<Button>(form,"phaseWaveStep").Enabled,"Finite source trace completed");C<DataGridView>(form,"phaseWaveTrace").CurrentCell=C<DataGridView>(form,"phaseWaveTrace").Rows[66].Cells[0];Application.DoEvents();Need(form.ShownSnapshot.Frames==67&&form.ShownSnapshot.World.LastCreated.Count==7,"Delayed remaining burst visible in source row67");C<ComboBox>(form,"phaseWaveInitial").SelectedIndex=2;C<ComboBox>(form,"phaseWavePause").SelectedIndex=0;Click(form,"phaseWaveLoad");Click(form,"phaseWaveStep");Need(form.StateSnapshot.PhaseCounter==0&&form.StateSnapshot.ConsumerPolls==1&&form.StateSnapshot.World.CreatedFleetCount==0,"MAX wraps to aligned equality without spawning");
                }
            });
            check("Phase-wave form preserves pending reset,Enter edges and fixed Close at minimum size",delegate{
                using(var form=new PracticeReinforcementPhaseStudy(programs))
                {
                    Show(form,owner);Click(form,"phaseWave32");C<ComboBox>(form,"phaseWaveSeed").SelectedIndex=1;Click(form,"phaseWaveReset");Need(form.StateSnapshot.Frames==0&&form.StateSnapshot.World.InitialSeed==731&&(uint)C<ComboBox>(form,"phaseWaveSeed").SelectedItem==0&&C<Label>(form,"phaseWavePending").Text.Contains("pending"),"Applied reset retains pending seed");form.StudyKey(Keys.Enter,false);form.StudyKey(Keys.Enter,true);C<ComboBox>(form,"phaseWaveSeed").SelectedIndex=0;form.StudyKey(Keys.Enter,true);Need(form.StateSnapshot.World.InitialSeed==0,"Held Enter cannot reload");form.StudyKey(Keys.Enter,false);form.StudyKey(Keys.Enter,true);form.StudyKey(Keys.Enter,false);Need(form.StateSnapshot.World.InitialSeed==731,"New Enter press loads pending seed");form.ClientSize=new Size(940,620);var details=C<TextBox>(form,"phaseWaveDetails");details.Focus();C<Panel>(form,"phaseWaveScroll").ScrollControlIntoView(details);Application.DoEvents();var close=C<Button>(form,"phaseWaveClose");Need(details.Width>=860&&form.ClientRectangle.Contains(new Rectangle(form.PointToClient(close.PointToScreen(Point.Empty)),close.Size)),"Scrollable details/fixed Close");Click(form,"phaseWaveClose");Need(form.IsDisposed||!form.Visible,"No selection callback after disposal");
                }
            });
        }
        static void Save(Form form,string path){Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
        internal static int SavePreviews(string assets,string output)
        {
            try
            {
                Directory.CreateDirectory(output);using(var form=new PracticeReinforcementPhaseStudy(Path.Combine(Path.GetDirectoryName(assets),"Programs")))
                {
                    form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();Application.DoEvents();Save(form,Path.Combine(output,"01-initial.png"));Click(form,"phaseWave32");Save(form,Path.Combine(output,"02-source-frame32.png"));Click(form,"phaseWave32");Save(form,Path.Combine(output,"03-source-frame64.png"));C<ComboBox>(form,"phaseWavePause").SelectedIndex=2;Click(form,"phaseWaveLoad");Click(form,"phaseWave32");Click(form,"phaseWaveStep");Save(form,Path.Combine(output,"04-held-phase32.png"));C<ComboBox>(form,"phaseWaveInitial").SelectedIndex=2;C<ComboBox>(form,"phaseWavePause").SelectedIndex=0;Click(form,"phaseWaveLoad");Click(form,"phaseWaveStep");Save(form,Path.Combine(output,"05-max-wrap-equality.png"));Click(form,"phaseWave32");form.ClientSize=new Size(940,620);var details=C<TextBox>(form,"phaseWaveDetails");details.Focus();C<Panel>(form,"phaseWaveScroll").ScrollControlIntoView(details);Save(form,Path.Combine(output,"06-minimum-details.png"));var grid=C<DataGridView>(form,"phaseWaveTrace");grid.Focus();C<Panel>(form,"phaseWaveScroll").ScrollControlIntoView(grid);Save(form,Path.Combine(output,"07-minimum-trace.png"));form.Close();
                }
                return 0;
            }
            catch(Exception ex){File.WriteAllText(Path.Combine(output,"Error.txt"),ex.ToString());return 1;}
        }
    }
}
