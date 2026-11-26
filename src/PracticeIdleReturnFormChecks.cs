using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeIdleReturnFormChecks
    {
        static void Need(bool ok, string text) { if (!ok) throw new Exception(text); }
        static T Find<T>(Form form, string name) where T : Control { return (T)form.Controls.Find(name, true)[0]; }
        static void Show(Form form, Form owner) { form.ShowInTaskbar = false; form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000); form.Show(owner); Application.DoEvents(); }
        internal static void Run(Action<string, Action> check, string assets, Form owner)
        {
            string programs = Path.Combine(Path.GetDirectoryName(assets), "Programs"); var source = File.ReadAllBytes(Path.Combine(assets, "Graphics", "BSSB202.SCN")); var startup = PracticeMapStartup.CreateBssb202(programs, source, 731);
            check("Idle return form applies one source pass displays queue changes and resets", delegate
            {
                using (var form = new PracticeIdleReturnStudy(programs, startup))
                {
                    Show(form, owner); Need(form.StudyState.Profile == 1 && !form.StudyState.Executed && Find<ComboBox>(form, "idleReturnRecord").SelectedIndex == 16, "Default return fixture");
                    Find<Button>(form, "idleReturnExecute").PerformClick(); Need(form.StudyState.IdleReturns == 1 && form.StudyState.RouteUiRequests == 1 && form.StudyState.SelectedWaypoint == -1 && !Find<Button>(form, "idleReturnExecute").Enabled && Find<ListBox>(form, "idleReturnQueue").Items.Count == 5 && Find<TextBox>(form, "idleReturnDetails").Text.Contains("(unchanged)"), "One-pass queue/readout");
                    Find<Button>(form, "idleReturnReset").PerformClick(); Need(!form.StudyState.Executed && form.StudyState.SelectedWaypoint == 4 && Find<Button>(form, "idleReturnExecute").Enabled, "Applied idle reset"); form.Close();
                }
            });
            check("Idle form keeps selections pending and supports zero mixed and all250 return counts", delegate
            {
                using (var form = new PracticeIdleReturnStudy(programs, startup))
                {
                    Show(form, owner); Find<ComboBox>(form, "idleReturnProfile").SelectedIndex = 0; Find<Button>(form, "idleReturnReset").PerformClick(); Need(form.StudyState.Profile == 1 && Find<ComboBox>(form, "idleReturnProfile").SelectedIndex == 0, "Pending profile lost on reset");
                    Find<Button>(form, "idleReturnLoad").PerformClick(); Find<Button>(form, "idleReturnExecute").PerformClick(); Need(form.StudyState.IdleReturns == 0 && form.StudyState.SelectedWaypoint == 4, "Zero-idle global selection");
                    Find<ComboBox>(form, "idleReturnProfile").SelectedIndex = 3; Find<ComboBox>(form, "idleReturnHistory").SelectedIndex = 1; Find<Button>(form, "idleReturnLoad").PerformClick(); Find<Button>(form, "idleReturnExecute").PerformClick(); Need(form.StudyState.IdleReturns == 2 && form.StudyState.HistoryFixture == 1 && form.StudyState.SelectedWaypoint == -1, "Mixed/history fixture");
                    Find<ComboBox>(form, "idleReturnProfile").SelectedIndex = 4; Find<Button>(form, "idleReturnLoad").PerformClick(); Find<Button>(form, "idleReturnExecute").PerformClick(); Need(form.StudyState.IdleReturns == 250 && form.StudyState.RouteUiRequests == 250, "All-record diagnostic"); Find<ComboBox>(form, "idleReturnRecord").SelectedIndex = 249; Need(Find<TextBox>(form, "idleReturnDetails").Text.Contains("Viewed record 249"), "Inactive record details"); form.Close();
                }
            });
            check("Idle form suppresses held confirmation scrolls at minimum and rejects an unsupported seed", delegate
            {
                using (var form = new PracticeIdleReturnStudy(programs, startup))
                {
                    Show(form, owner); form.ActiveControl = Find<Button>(form, "idleReturnExecute"); var down = typeof(PracticeIdleReturnStudy).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic); down.Invoke(form, new object[] { new KeyEventArgs(Keys.Enter) }); var after = form.StudyState; down.Invoke(form, new object[] { new KeyEventArgs(Keys.Enter) }); Need(after.Executed && Object.ReferenceEquals(after, form.StudyState), "Held confirmation repeated pass");
                    form.Size = form.MinimumSize; Application.DoEvents(); Need(Find<Panel>(form, "idleReturnScroll").AutoScroll && Find<Control>(form, "idleReturnCanvas").Width > 500, "Minimum idle layout"); form.Close();
                }
                using (var bad = new PracticeIdleReturnStudy(programs, PracticeMapStartup.CreateBssb202(programs, source, 732))) { Show(bad, owner); Need(bad.StudyState == null && !Find<Button>(bad, "idleReturnExecute").Enabled && Find<Label>(bad, "idleReturnFeedback").Text.Contains("seed731"), "Unsupported seed silently admitted"); bad.Close(); }
            });
            check("Recovered maps opens the direct idle study and preserves the active battle", delegate
            {
                var main = owner as MainForm; Need(main != null, "Main owner required"); var tabs = (TabControl)typeof(MainForm).GetField("tabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(main); int old = tabs.SelectedIndex, maps = -1;
                for (int n = 0; n < tabs.TabPages.Count; n++) if (tabs.TabPages[n].Text == "Recovered maps") maps = n; Need(maps >= 0, "Recovered map tab missing"); tabs.SelectedIndex = maps; Application.DoEvents();
                var before = main.State; int events = before.Events.Count; bool saw = false; Exception error = null; int ticks = 0;
                using (var timer = new Timer { Interval = 100 })
                {
                    timer.Tick += delegate
                    {
                        if (++ticks > 30) { timer.Stop(); error = new Exception("Idle study child missing"); return; }
                        foreach (Form child in main.OwnedForms)
                        { var form = child as PracticeIdleReturnStudy; if (form == null) continue; timer.Stop(); try { Find<Button>(form, "idleReturnExecute").PerformClick(); Need(form.StudyState.IdleReturns == 1, "Direct child pass"); saw = true; } catch (Exception e) { error = e; } finally { form.Close(); } return; }
                    };
                    timer.Start(); Find<Button>(main, "openIdleReturnStudy").PerformClick(); timer.Stop();
                }
                tabs.SelectedIndex = old; if (error != null) throw error; Need(saw && Object.ReferenceEquals(before, main.State) && before.Events.Count == events, "Study changed battle or entry unavailable");
            });
        }

        internal static int SavePreviews(string assets, string destination)
        {
            destination = Path.GetFullPath(destination); if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException("Choose a new idle study preview directory."); Directory.CreateDirectory(destination);
            string programs = Path.Combine(Path.GetDirectoryName(assets), "Programs"); var startup = PracticeMapStartup.CreateBssb202(programs, File.ReadAllBytes(Path.Combine(assets, "Graphics", "BSSB202.SCN")), 731);
            using (var form = new PracticeIdleReturnStudy(programs, startup))
            {
                Show(form, null); Action<string> capture = delegate(string name) { form.PerformLayout(); Application.DoEvents(); using (var b = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(b, new Rectangle(Point.Empty, form.Size)); b.Save(Path.Combine(destination, name + ".png"), System.Drawing.Imaging.ImageFormat.Png); } };
                capture("01-Prepared"); Find<Button>(form, "idleReturnExecute").PerformClick(); capture("02-Applied"); Find<Panel>(form, "idleReturnScroll").ScrollControlIntoView(Find<TextBox>(form, "idleReturnDetails")); capture("03-Queue-Details");
                Find<ComboBox>(form, "idleReturnProfile").SelectedIndex = 0; Find<Button>(form, "idleReturnLoad").PerformClick(); Find<Button>(form, "idleReturnExecute").PerformClick(); Find<Panel>(form, "idleReturnScroll").AutoScrollPosition = Point.Empty; capture("04-Zero-Returns");
                Find<ComboBox>(form, "idleReturnProfile").SelectedIndex = 3; Find<Button>(form, "idleReturnLoad").PerformClick(); Find<Button>(form, "idleReturnExecute").PerformClick(); Find<ComboBox>(form, "idleReturnRecord").SelectedIndex = 249; capture("05-Mixed-Inactive");
                form.Size = form.MinimumSize; Find<Panel>(form, "idleReturnScroll").AutoScrollPosition = Point.Empty; capture("06-Minimum-Controls"); Find<Panel>(form, "idleReturnScroll").ScrollControlIntoView(Find<TextBox>(form, "idleReturnDetails")); capture("07-Minimum-Details"); form.Close();
            }
            File.WriteAllText(Path.Combine(destination, "Scope.txt"), "Seven actual-form copied idle-return previews. Queue intent only,origins fixed. Return state/context/history supplied. Original UI requests counted,not rendered;no clock/movement/policy/pathfinding/fullworld or physical acceptance.\r\n"); return 0;
        }
    }
}
