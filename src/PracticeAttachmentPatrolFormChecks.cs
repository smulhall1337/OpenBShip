using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace FleetLab
{
    internal static class PracticeAttachmentPatrolFormChecks
    {
        static void Need(bool ok, string text) { if (!ok) throw new Exception(text); }
        static T Find<T>(Form f, string name) where T : Control { return (T)f.Controls.Find(name, true)[0]; }
        static void Show(Form f, Form owner) { f.ShowInTaskbar = false; f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-20000, -20000); f.Show(owner); Application.DoEvents(); }
        internal static void Run(Action<string, Action> check, string assets, Form owner)
        {
            string programs = Path.Combine(Path.GetDirectoryName(assets), "Programs"); var source = File.ReadAllBytes(Path.Combine(assets, "Graphics", "BSSB202.SCN")); var startup = PracticeMapStartup.CreateBssb202(programs, source, 731);
            check("Carrier patrol form activates actual attached groups displays queues and resets", delegate
            {
                using (var f = new PracticeAttachmentPatrolStudy(programs, startup))
                {
                    Show(f, owner); Need(f.StudyState.ParentFleet == 15 && !f.StudyState.Executed && Find<ComboBox>(f, "patrolChild").SelectedIndex == 3, "Default copied carrier");
                    Find<Button>(f, "patrolApply").PerformClick(); Need(f.StudyState.ActivatedAircraft.Count == 4 && f.StudyState.VisitedAircraft.Count == 4 && f.StudyState.RouteUiRequests == 4 && f.StudyState.SelectedWaypoint == -1 && !Find<Button>(f, "patrolApply").Enabled && Find<ListBox>(f, "patrolQueue").Items.Count == 5 && Find<TextBox>(f, "patrolDetails").Text.Contains("kind 7 -> 4"), "Activation/queues readout");
                    Find<Button>(f, "patrolReset").PerformClick(); Need(!f.StudyState.Executed && f.StudyState.SelectedWaypoint == 4 && Find<Button>(f, "patrolApply").Enabled, "Applied reset"); f.Close();
                }
            });
            check("Carrier patrol form keeps pending settings and exposes hold active early-return and overflow fixtures", delegate
            {
                using (var f = new PracticeAttachmentPatrolStudy(programs, startup))
                {
                    Show(f, owner); Find<ComboBox>(f, "patrolProfile").SelectedIndex = 1; Find<ComboBox>(f, "patrolParent").SelectedIndex = 1; Find<Button>(f, "patrolReset").PerformClick(); Need(f.StudyState.ParentFleet == 15 && f.StudyState.RawDelta == 1 && Find<ComboBox>(f, "patrolProfile").SelectedIndex == 1, "Pending copy changed on reset");
                    Find<Button>(f, "patrolLoad").PerformClick(); Find<Button>(f, "patrolApply").PerformClick(); Need(f.StudyState.ParentFleet == 17 && f.StudyState.ActivatedAircraft.Count == 0 && f.StudyState.SelectedWaypoint == 4, "Below-threshold hold");
                    for (int p = 2; p < 8; p++)
                    {
                        Find<ComboBox>(f, "patrolProfile").SelectedIndex = p; Find<Button>(f, "patrolLoad").PerformClick(); Find<Button>(f, "patrolApply").PerformClick(); Need(f.StudyState.Executed && f.StudyState.ActivatedAircraft.Count == 0, "Profile activation " + p);
                        Need(f.StudyState.VisitedAircraft.Count == (p == 6 ? 0 : p == 5 ? 1 : 2), "Profile visits " + p);
                    }
                    Find<ComboBox>(f, "patrolChild").SelectedIndex = 0; Need(Find<TextBox>(f, "patrolDetails").Text.Contains("Viewed group 27"), "Group details selection"); f.Close();
                }
            });
            check("Carrier patrol form suppresses held confirmation scrolls at minimum and rejects unsupported seed", delegate
            {
                using (var f = new PracticeAttachmentPatrolStudy(programs, startup))
                {
                    Show(f, owner); f.ActiveControl = Find<Button>(f, "patrolApply"); var down = typeof(PracticeAttachmentPatrolStudy).GetMethod("OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance); down.Invoke(f, new object[] { new KeyEventArgs(Keys.Enter) }); var after = f.StudyState; down.Invoke(f, new object[] { new KeyEventArgs(Keys.Enter) }); Need(after.Executed && Object.ReferenceEquals(after, f.StudyState), "Held button repeated visit");
                    f.Size = f.MinimumSize; Application.DoEvents(); Need(Find<Panel>(f, "patrolScroll").AutoScroll && Find<Control>(f, "patrolCanvas").Width > 480, "Minimum scroll/canvas layout"); f.Close();
                }
                using (var bad = new PracticeAttachmentPatrolStudy(programs, PracticeMapStartup.CreateBssb202(programs, source, 732))) { Show(bad, owner); Need(bad.StudyState == null && !Find<Button>(bad, "patrolApply").Enabled && !Find<Button>(bad, "patrolLoad").Enabled && Find<Label>(bad, "patrolFeedback").Text.Contains("seed731"), "Unsupported seed admitted"); bad.Close(); }
            });
            check("Carrier whole-pass form displays shared policy idle and navigation results and resets applied scope", delegate
            {
                using (var f = new PracticeAttachmentPatrolStudy(programs, startup))
                {
                    Show(f, owner); Find<ComboBox>(f, "patrolScope").SelectedIndex = 3; Find<Button>(f, "patrolLoad").PerformClick(); Find<Button>(f, "patrolApply").PerformClick();
                    Need(f.StudyState.WholePass && f.StudyState.Visits == 250 && f.StudyState.PolicyVisits == 5 && f.StudyState.IdleReturns == 2 && f.StudyState.RouteUiRequests == 6 && f.StudyState.SelectedWaypoint == -1 && Find<ComboBox>(f, "patrolChild").Items.Count == 6 && Find<TextBox>(f, "patrolDetails").Text.Contains("Viewed idle record 249") && Find<ListBox>(f, "patrolQueue").Items[0].ToString().Contains("550"), "Whole joined display");
                    Find<ComboBox>(f, "patrolChild").SelectedIndex = 4; Need(Find<TextBox>(f, "patrolDetails").Text.Contains("Viewed idle record 16") && Find<ListBox>(f, "patrolQueue").Items[0].ToString().Contains("610"), "Idle fleet queue selection");
                    Find<ComboBox>(f, "patrolScope").SelectedIndex = 0; Find<Button>(f, "patrolReset").PerformClick(); Need(f.StudyState.WholePass && f.StudyState.IdleProfile == 2 && !f.StudyState.Executed && Find<ComboBox>(f, "patrolScope").SelectedIndex == 0, "Applied scope changed on reset");
                    Find<Button>(f, "patrolLoad").PerformClick(); Need(!f.StudyState.WholePass && Find<ComboBox>(f, "patrolChild").Items.Count == 4, "Pending leaf scope not applied"); f.Close();
                }
            });
            check("Carrier whole-pass form preserves zero-output selection and handles idle before later carrier policy", delegate
            {
                using (var f = new PracticeAttachmentPatrolStudy(programs, startup))
                {
                    Show(f, owner); Find<ComboBox>(f, "patrolProfile").SelectedIndex = 1; Find<ComboBox>(f, "patrolParent").SelectedIndex = 1; Find<ComboBox>(f, "patrolScope").SelectedIndex = 1; Find<Button>(f, "patrolLoad").PerformClick(); Find<Button>(f, "patrolApply").PerformClick();
                    Need(f.StudyState.Visits == 250 && f.StudyState.PolicyVisits == 1 && f.StudyState.RouteUiRequests == 0 && f.StudyState.SelectedWaypoint == 4, "Zero-output whole pass");
                    Find<ComboBox>(f, "patrolScope").SelectedIndex = 2; Find<Button>(f, "patrolLoad").PerformClick(); Find<Button>(f, "patrolApply").PerformClick(); Need(f.StudyState.IdleReturns == 1 && f.StudyState.RouteUiRequests == 1 && f.StudyState.SelectedWaypoint == -1, "Earlier idle selection state");
                    Find<ComboBox>(f, "patrolProfile").SelectedIndex = 5; Find<ComboBox>(f, "patrolScope").SelectedIndex = 3; Find<Button>(f, "patrolLoad").PerformClick(); Find<Button>(f, "patrolApply").PerformClick(); Need(f.StudyState.PolicyVisits == 3 && f.StudyState.VisitedAircraft.Count == 1 && f.StudyState.StoppedAtStateOne == 28 && f.StudyState.IdleReturns == 2, "Whole pass after early carrier-helper return"); f.Close();
                }
            });
            check("Recovered maps opens carrier patrol directly and preserves the active battle", delegate
            {
                var main = owner as MainForm; Need(main != null, "Main owner"); var tabs = (TabControl)typeof(MainForm).GetField("tabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(main); int old = tabs.SelectedIndex, maps = -1;
                for (int n = 0; n < tabs.TabPages.Count; n++) if (tabs.TabPages[n].Text == "Recovered maps") maps = n; Need(maps >= 0, "Map tab missing"); tabs.SelectedIndex = maps; Application.DoEvents(); var before = main.State; int events = before.Events.Count; bool saw = false; Exception error = null;
                using (var timer = new Timer { Interval = 100 })
                {
                    timer.Tick += delegate { foreach (Form child in main.OwnedForms) { var f = child as PracticeAttachmentPatrolStudy; if (f == null) continue; timer.Stop(); try { Find<Button>(f, "patrolApply").PerformClick(); Need(f.StudyState.ActivatedAircraft.Count == 4, "Direct child activation"); saw = true; } catch (Exception e) { error = e; } finally { f.Close(); } return; } };
                    timer.Start(); Find<Button>(main, "openAttachmentPatrolStudy").PerformClick(); timer.Stop();
                }
                tabs.SelectedIndex = old; if (error != null) throw error; Need(saw && Object.ReferenceEquals(before, main.State) && before.Events.Count == events, "Patrol entry changed battle");
            });
        }
        internal static int SavePreviews(string assets, string destination)
        {
            destination = Path.GetFullPath(destination); if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException("Choose a fresh patrol preview directory."); Directory.CreateDirectory(destination);
            string programs = Path.Combine(Path.GetDirectoryName(assets), "Programs"); var startup = PracticeMapStartup.CreateBssb202(programs, File.ReadAllBytes(Path.Combine(assets, "Graphics", "BSSB202.SCN")), 731);
            using (var f = new PracticeAttachmentPatrolStudy(programs, startup))
            {
                Show(f, null); Action<string> capture = delegate(string name) { f.PerformLayout(); Application.DoEvents(); using (var b = new Bitmap(f.Width, f.Height)) { f.DrawToBitmap(b, new Rectangle(Point.Empty, f.Size)); b.Save(Path.Combine(destination, name + ".png"), System.Drawing.Imaging.ImageFormat.Png); } };
                capture("01-Prepared"); Find<Button>(f, "patrolApply").PerformClick(); capture("02-Activated"); Find<Panel>(f, "patrolScroll").ScrollControlIntoView(Find<TextBox>(f, "patrolDetails")); capture("03-Details");
                Find<ComboBox>(f, "patrolProfile").SelectedIndex = 1; Find<Button>(f, "patrolLoad").PerformClick(); Find<Button>(f, "patrolApply").PerformClick(); Find<Panel>(f, "patrolScroll").AutoScrollPosition = Point.Empty; capture("04-Hold");
                Find<ComboBox>(f, "patrolProfile").SelectedIndex = 5; Find<Button>(f, "patrolLoad").PerformClick(); Find<Button>(f, "patrolApply").PerformClick(); capture("05-Early-Return");
                f.Size = f.MinimumSize; Find<Panel>(f, "patrolScroll").AutoScrollPosition = Point.Empty; capture("06-Minimum-Controls"); Find<Panel>(f, "patrolScroll").ScrollControlIntoView(Find<TextBox>(f, "patrolDetails")); capture("07-Minimum-Details");
                f.Size = new Size(1180, 860); Find<ComboBox>(f, "patrolProfile").SelectedIndex = 0; Find<ComboBox>(f, "patrolScope").SelectedIndex = 3; Find<Button>(f, "patrolLoad").PerformClick(); Find<Button>(f, "patrolApply").PerformClick(); Find<Panel>(f, "patrolScroll").AutoScrollPosition = Point.Empty; capture("08-Whole-Idle249"); Find<ComboBox>(f, "patrolChild").SelectedIndex = 4; capture("09-Whole-Idle16");
                f.Size = f.MinimumSize; Find<Panel>(f, "patrolScroll").ScrollControlIntoView(Find<TextBox>(f, "patrolDetails")); capture("10-Whole-Minimum-Details"); f.Close();
            }
            File.WriteAllText(Path.Combine(destination, "Scope.txt"), "Ten actual-form copied carrier-patrol previews,including whole250-record policy/idle/navigation scope. Numeric visits change activation/queues;origins stayfixed. UI requests counted,not source renderer. Supplied visibility/collision/timer/phase/idle fixtures;no movement/clock/visible-target combat/fullworld or physical acceptance.\r\n"); return 0;
        }
    }
}
