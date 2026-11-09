using System;
using System.IO;
using System.Windows.Forms;
namespace FleetLab {
    public static class Program {
        [STAThread] public static int Main(string[] args) {
            string root=AppDomain.CurrentDomain.BaseDirectory,assets=Path.Combine(root,"Assets");
            try {
                if(args.Length==2&&args[0]=="--audio-check")return WaveOutput.CheckDevice(Path.GetFullPath(args[1]));
                if(args.Length==2&&args[0]=="--self-check")return Validation.Run(assets,Path.GetFullPath(args[1]));
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using(var form=new MainForm(assets)) {
                    if(args.Length==2&&args[0]=="--ui-check")return form.CheckUi(Path.GetFullPath(args[1]));
                    if(args.Length==2&&(args[0]=="--render-credits-preview"||args[0]=="--render-credits-small-preview"||args[0]=="--render-preferences-preview"||args[0]=="--render-preferences-small-preview"||args[0]=="--render-navigation-title-preview"||args[0]=="--render-navigation-preview"||args[0]=="--render-navigation-small-preview"||args[0]=="--render-pause-preview"||args[0]=="--render-pause-small-preview"||args[0]=="--render-message-preview"||args[0]=="--render-message-small-preview"||args[0]=="--render-entry-preview"||args[0]=="--render-entry-small-preview"||args[0]=="--render-fleet-config-preview"||args[0]=="--render-fleet-config-small-preview"||args[0]=="--render-solo-preview"||args[0]=="--render-solo-small-preview"||args[0]=="--render-title-preview"||args[0]=="--render-title-small-preview"||args[0]=="--render-victory-preview"||args[0]=="--render-defeat-preview"||args[0]=="--render-ending-small-preview"||args[0]=="--render-result-card-preview"||args[0]=="--render-result-movie-preview"||args[0]=="--render-preview"||args[0]=="--render-battle-preview"||args[0]=="--render-map-preview"||args[0]=="--render-legacy-preview"||args[0]=="--render-small-preview"||args[0]=="--render-fleet-preview"||args[0]=="--render-damage-preview"||args[0]=="--render-recovered-colors-preview"||args[0]=="--render-firing-preview"||args[0]=="--render-clip-preview"||args[0]=="--render-clip-small-preview"||args[0]=="--render-library-preview"||args[0]=="--render-shot-preview"||args[0]=="--render-shot-small-preview"||args[0]=="--render-impact-preview"||args[0]=="--render-impact-small-preview"||args[0]=="--render-miss-preview"||args[0]=="--render-sink-preview"||args[0]=="--render-backdrop-preview"||args[0]=="--render-backdrop-small-preview"||args[0]=="--render-high-preview"||args[0]=="--render-high-small-preview")) {
                        form.SavePreview(Path.GetFullPath(args[1]),args[0]=="--render-credits-preview"?"credits":args[0]=="--render-credits-small-preview"?"credits-small":args[0]=="--render-preferences-preview"?"preferences":args[0]=="--render-preferences-small-preview"?"preferences-small":args[0]=="--render-navigation-title-preview"?"navigation-title":args[0]=="--render-navigation-preview"?"navigation":args[0]=="--render-navigation-small-preview"?"navigation-small":args[0]=="--render-pause-preview"?"pause":args[0]=="--render-pause-small-preview"?"pause-small":args[0]=="--render-message-preview"?"message":args[0]=="--render-message-small-preview"?"message-small":args[0]=="--render-entry-preview"?"entry":args[0]=="--render-entry-small-preview"?"entry-small":args[0]=="--render-fleet-config-preview"?"fleet-config":args[0]=="--render-fleet-config-small-preview"?"fleet-config-small":args[0]=="--render-solo-preview"?"solo":args[0]=="--render-solo-small-preview"?"solo-small":args[0]=="--render-title-preview"?"title":args[0]=="--render-title-small-preview"?"title-small":args[0]=="--render-victory-preview"?"victory":args[0]=="--render-defeat-preview"?"lose":args[0]=="--render-ending-small-preview"?"ending-small":args[0]=="--render-result-card-preview"?"result-card":args[0]=="--render-result-movie-preview"?"result-movie":args[0]=="--render-high-preview"?"high":args[0]=="--render-high-small-preview"?"high-small":args[0]=="--render-backdrop-preview"?"backdrop":args[0]=="--render-backdrop-small-preview"?"backdrop-small":args[0]=="--render-impact-preview"?"impact":args[0]=="--render-impact-small-preview"?"impact-small":args[0]=="--render-miss-preview"?"miss":args[0]=="--render-sink-preview"?"sink":args[0]=="--render-shot-preview"?"shot":args[0]=="--render-shot-small-preview"?"shot-small":args[0]=="--render-library-preview"?"library":args[0]=="--render-clip-preview"?"clip":args[0]=="--render-clip-small-preview"?"clip-small":args[0]=="--render-firing-preview"?"firing":args[0]=="--render-map-preview"?"map":args[0]=="--render-battle-preview"?"battle":args[0]=="--render-legacy-preview"?"legacy":args[0]=="--render-small-preview"?"small":args[0]=="--render-fleet-preview"?"fleet":args[0]=="--render-damage-preview"?"damage":args[0]=="--render-recovered-colors-preview"?"recovered":"placement");
                        return 0;
                    }
                    Application.Run(form);
                }
                return 0;
            }
            catch(Exception e) {
                try {
                    string dir=Path.Combine(root,"UserData");
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir,"last-error.txt"),e.ToString());
                }
                catch {
                }
                if(args.Length==0)MessageBox.Show("Fleet Lab could not start: "+e.Message,"Battleship Fleet Lab");
                return 1;
            }
        }
    }
}





