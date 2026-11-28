using System;using System.IO;using System.Drawing;using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeGeneratedReinforcementFormChecks
    {
        static T Find<T>(Form f,string name)where T:Control{var found=f.Controls.Find(name,true);if(found.Length!=1)throw new Exception("Missing "+name);return (T)found[0];}
        static void Need(bool b,string m){if(!b)throw new Exception(m);}
        static void Show(Form f,Form owner){f.Show(owner);Application.DoEvents();}
        static void Click(Form f,string name){Find<Button>(f,name).PerformClick();Application.DoEvents();}
        static void Load(Form f,int counts,uint seed,bool reserve,bool maxCounter){Find<ComboBox>(f,"generatedCounts").SelectedIndex=counts;Find<NumericUpDown>(f,"generatedSeed").Value=seed;Find<CheckBox>(f,"generatedReserve").Checked=reserve;Find<ComboBox>(f,"generatedCounter").SelectedIndex=maxCounter?1:0;Click(f,"generatedLoad");}
        internal static void Run(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            check("Generated reinforcement form opens the produced sixteen-slot table without creating live fleets",delegate{
                using(var f=new PracticeGeneratedReinforcementStudy(programs)){Show(f,owner);Need(f.StateSnapshot.ProducerCount==1&&f.StateSnapshot.ConsumerCount==1&&f.StateSnapshot.CreatedFleetCount==0&&f.StateSnapshot.RefreshRequests==1,"Original producer-only initial view");Need(Find<DataGridView>(f,"generatedSchedule").Rows.Count==16&&Find<TextBox>(f,"generatedDetails").ReadOnly&&owner.Controls.Find("openGeneratedReinforcementStudy",true).Length==1,"Direct entry/read-only table/details");Need(Find<ComboBox>(f,"generatedCounts").ForeColor!=Find<ComboBox>(f,"generatedCounts").BackColor&&Find<Button>(f,"generatedAfter").ForeColor!=Find<Button>(f,"generatedAfter").BackColor,"Readable controls");}
            });
            check("Generated form equality,default five-unit stock creation,tied deadlines and repeat match source feedback",delegate{
                using(var f=new PracticeGeneratedReinforcementStudy(programs)){Show(f,owner);Click(f,"generatedEquality");Need(f.StateSnapshot.CreatedFleetCount==0&&Find<Label>(f,"generatedSummary").Text.Contains("Last refresh requests: 0"),"Exact deadline hold");Click(f,"generatedAfter");Need(f.StateSnapshot.CreatedFleetCount==1&&f.StateSnapshot.CreatedUnitCount==5&&Find<TextBox>(f,"generatedDetails").Text.Contains("Type 15 stock[0..34]"),"Default source stock-bearing units");uint random=f.StateSnapshot.RandomState;Click(f,"generatedRepeat");Need(f.StateSnapshot.CreatedFleetCount==1&&f.StateSnapshot.RandomState==random&&f.StateSnapshot.LastCreated.Count==0,"No repeated construction");Load(f,3,9,false,false);Click(f,"generatedAfter");Need(f.StateSnapshot.LastCreated.Count==2&&f.StateSnapshot.CreatedUnitCount==10,"Recorded earliest tied deadlines");}
            });
            check("Generated form pending/reset,outside-consumer schedules,source burst order and counter wrap remain explicit",delegate{
                using(var f=new PracticeGeneratedReinforcementStudy(programs)){Show(f,owner);Find<ComboBox>(f,"generatedCounts").SelectedIndex=1;Find<NumericUpDown>(f,"generatedSeed").Value=42;Click(f,"generatedReset");Need(f.StateSnapshot.ProducerCount==1&&f.StateSnapshot.InitialSeed==731&&Find<NumericUpDown>(f,"generatedSeed").Value==42&&Find<Label>(f,"generatedPending").Text.Contains("pending"),"Applied reset retains pending edits");Load(f,1,731,true,true);Click(f,"generatedFinish");Need(f.StateSnapshot.CreatedFleetCount==4&&f.StateSnapshot.FleetIds()[0]==1&&f.StateSnapshot.OwnerCounter(0)==3&&Find<DataGridView>(f,"generatedSchedule").Rows[4].Cells[4].Value.ToString()=="Outside consumer","Source count split/reservation/wrap");Need(!Find<Button>(f,"generatedAfter").Enabled&&Find<Button>(f,"generatedRepeat").Enabled,"Complete source slice remains repeatable only");Load(f,2,731,false,false);Click(f,"generatedJump");Need(f.StateSnapshot.LastCreated.Count==8&&f.StateSnapshot.LastCreated[4].Group==1&&f.StateSnapshot.LastCreated[4].Slot==0,"Original catch-up group/slot order");}
            });
            check("Generated form Enter edges,minimum details scroll and Close preserve independent state",delegate{
                using(var f=new PracticeGeneratedReinforcementStudy(programs)){Show(f,owner);f.StudyKey(Keys.Enter,false);Find<NumericUpDown>(f,"generatedSeed").Value=9;f.StudyKey(Keys.Enter,true);var applied=f.StateSnapshot;Find<NumericUpDown>(f,"generatedSeed").Value=42;f.StudyKey(Keys.Enter,true);Need(object.ReferenceEquals(applied,f.StateSnapshot),"Held Enter suppressed");f.StudyKey(Keys.Enter,false);f.StudyKey(Keys.Enter,true);Need(f.StateSnapshot.InitialSeed==42,"New Enter press applies");f.StudyKey(Keys.Enter,false);Click(f,"generatedAfter");f.ClientSize=new Size(940,620);var details=Find<TextBox>(f,"generatedDetails");details.Focus();Find<Panel>(f,"generatedScroll").ScrollControlIntoView(details);Application.DoEvents();Need(details.Width>=860&&details.Text.Contains("Local grid"),"Scrollable source roster/grid");var close=Find<Button>(f,"generatedClose");Need(f.ClientRectangle.Contains(new Rectangle(f.PointToClient(close.PointToScreen(Point.Empty)),close.Size)),"Visible persistent Close");Click(f,"generatedClose");Need(f.IsDisposed||!f.Visible,"Clean copied-study close");}
            });
        }
        internal static void RunMapped(Action<string,Action> check,string assets,Form owner)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
            check("Occupied reinforcement form separates existing map fleets from new allocations",delegate{
                using(var f=new PracticeGeneratedReinforcementStudy(programs))
                {
                    Show(f,owner);Find<ComboBox>(f,"generatedWorld").SelectedIndex=1;Click(f,"generatedLoad");
                    Need(f.StateSnapshot.UsesOccupiedMap&&f.StateSnapshot.ExistingFleetCount==29&&Find<ComboBox>(f,"generatedFleet").Items.Count==0,"Existing fleet markers are not generated roster choices");
                    Need(!Find<CheckBox>(f,"generatedReserve").Enabled&&!Find<ComboBox>(f,"generatedCounter").Enabled&&Find<Label>(f,"generatedSummary").Text.Contains("existing 29 fleets / 63 units"),"Copied map counters/reservations explicit");
                    Click(f,"generatedEquality");Need(f.StateSnapshot.CreatedFleetCount==0,"Mapped equality holds");Click(f,"generatedAfter");
                    Need(f.StateSnapshot.LastCreated[0].Fleet==29&&f.StateSnapshot.LastCreated[0].Units[0]==66&&Find<ComboBox>(f,"generatedFleet").SelectedItem.ToString()=="29"&&Find<TextBox>(f,"generatedDetails").Text.Contains("no natural transition"),"Recovered allocation and scope visible");
                }
            });
            check("Occupied reinforcement form rejects unsupported pending seeds without replacing the applied world",delegate{
                using(var f=new PracticeGeneratedReinforcementStudy(programs))
                {
                    Show(f,owner);Find<ComboBox>(f,"generatedWorld").SelectedIndex=1;Click(f,"generatedLoad");var applied=f.StateSnapshot;
                    Find<NumericUpDown>(f,"generatedSeed").Value=42;Click(f,"generatedLoad");
                    Need(object.ReferenceEquals(applied,f.StateSnapshot)&&Find<Label>(f,"generatedPending").Text.Contains("0 and 731"),"Unsupported source seed is an inline atomic error");
                    Find<NumericUpDown>(f,"generatedSeed").Value=0;Click(f,"generatedLoad");Need(f.StateSnapshot.InitialSeed==0&&f.StateSnapshot.InitialRandomState==1349664646,"Supported alternative map basis");
                    Find<ComboBox>(f,"generatedWorld").SelectedIndex=0;Click(f,"generatedLoad");Need(!f.StateSnapshot.UsesOccupiedMap&&Find<CheckBox>(f,"generatedReserve").Enabled&&Find<ComboBox>(f,"generatedCounter").Enabled,"Empty-fixture controls restored");
                }
            });
            check("Occupied reinforcement form exposes isolated allocation profiles and pending-safe reset at minimum size",delegate{
                using(var f=new PracticeGeneratedReinforcementStudy(programs))
                {
                    Show(f,owner);
                    foreach(int world in new[]{2,3,4})
                    {
                        Find<ComboBox>(f,"generatedWorld").SelectedIndex=world;Click(f,"generatedLoad");Click(f,"generatedAfter");
                        Need(f.StateSnapshot.MapProfile==world-1&&f.StateSnapshot.LastCreated[0].Fleet==(world==4?30:29)&&f.StateSnapshot.LastCreated[0].Units[0]==66,"Named copied reservation profile");
                    }
                    Find<ComboBox>(f,"generatedWorld").SelectedIndex=0;Find<NumericUpDown>(f,"generatedSeed").Value=42;Click(f,"generatedReset");
                    Need(f.StateSnapshot.MapProfile==3&&f.StateSnapshot.CreatedFleetCount==0&&Find<ComboBox>(f,"generatedWorld").SelectedIndex==0&&Find<Label>(f,"generatedPending").Text.Contains("pending"),"Applied map reset keeps pending world/seed edits");
                    f.ClientSize=new Size(940,620);var worldChoice=Find<ComboBox>(f,"generatedWorld");worldChoice.Focus();Find<Panel>(f,"generatedScroll").ScrollControlIntoView(worldChoice);Application.DoEvents();
                    Need(worldChoice.Width>=450&&worldChoice.ForeColor!=worldChoice.BackColor,"Readable scrollable world choices");
                    var close=Find<Button>(f,"generatedClose");Need(f.ClientRectangle.Contains(new Rectangle(f.PointToClient(close.PointToScreen(Point.Empty)),close.Size)),"Fixed Close survives copied-map controls");Click(f,"generatedClose");Need(f.IsDisposed||!f.Visible,"Clean map-study exit");
                }
            });
        }
        internal static int SaveMappedPreviews(string assets,string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");
                using(var f=new PracticeGeneratedReinforcementStudy(programs))
                {
                    f.Show();Application.DoEvents();Find<ComboBox>(f,"generatedWorld").SelectedIndex=1;Click(f,"generatedLoad");Save(f,Path.Combine(directory,"01-occupied-map.png"));Click(f,"generatedAfter");Save(f,Path.Combine(directory,"02-first-map-fleet.png"));
                    Find<ComboBox>(f,"generatedWorld").SelectedIndex=2;Click(f,"generatedLoad");Click(f,"generatedAfter");Save(f,Path.Combine(directory,"03-bay-only.png"));
                    Find<ComboBox>(f,"generatedWorld").SelectedIndex=3;Click(f,"generatedLoad");Click(f,"generatedAfter");Save(f,Path.Combine(directory,"04-adjacent-only.png"));
                    Find<ComboBox>(f,"generatedWorld").SelectedIndex=4;Find<ComboBox>(f,"generatedCounts").SelectedIndex=1;Click(f,"generatedLoad");Click(f,"generatedFinish");Save(f,Path.Combine(directory,"05-reserved-map-fleet.png"));
                    f.ClientSize=new Size(940,620);var details=Find<TextBox>(f,"generatedDetails");details.Focus();Find<Panel>(f,"generatedScroll").ScrollControlIntoView(details);Save(f,Path.Combine(directory,"06-minimum-details.png"));
                    var world=Find<ComboBox>(f,"generatedWorld");world.Focus();Find<Panel>(f,"generatedScroll").ScrollControlIntoView(world);Save(f,Path.Combine(directory,"07-minimum-world-choice.png"));f.Close();
                }
                return 0;
            }
            catch(Exception ex){File.WriteAllText(Path.Combine(directory,"Error.txt"),ex.ToString());return 1;}
        }
        static void Save(Form f,string file){Application.DoEvents();using(var bitmap=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bitmap,new Rectangle(Point.Empty,f.Size));bitmap.Save(file,System.Drawing.Imaging.ImageFormat.Png);}}
        internal static int SavePreviews(string assets,string directory)
        {
            try{Directory.CreateDirectory(directory);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");using(var f=new PracticeGeneratedReinforcementStudy(programs)){f.Show();Application.DoEvents();Save(f,Path.Combine(directory,"01-generated-table.png"));Click(f,"generatedEquality");Save(f,Path.Combine(directory,"02-equality-held.png"));Click(f,"generatedAfter");Save(f,Path.Combine(directory,"03-default-fleet.png"));Load(f,3,9,false,false);Click(f,"generatedAfter");Save(f,Path.Combine(directory,"04-tied-deadlines.png"));Load(f,1,731,true,true);Click(f,"generatedFinish");Save(f,Path.Combine(directory,"05-outside-consumer.png"));Load(f,2,731,false,false);Click(f,"generatedJump");Save(f,Path.Combine(directory,"06-catch-up.png"));f.ClientSize=new Size(940,620);Find<DataGridView>(f,"generatedSchedule").Focus();Find<Panel>(f,"generatedScroll").ScrollControlIntoView(Find<DataGridView>(f,"generatedSchedule"));Save(f,Path.Combine(directory,"07-minimum-table.png"));var details=Find<TextBox>(f,"generatedDetails");details.Focus();Find<Panel>(f,"generatedScroll").ScrollControlIntoView(details);Save(f,Path.Combine(directory,"08-minimum-stock.png"));f.Close();}return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(directory,"Error.txt"),ex.ToString());return 1;}
        }
    }
}
