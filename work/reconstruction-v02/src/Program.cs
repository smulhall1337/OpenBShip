using System;
using System.IO;
using System.Windows.Forms;
namespace FleetLab {
    public static class Program {
        [STAThread] public static int Main(string[] args) {
            string root=AppDomain.CurrentDomain.BaseDirectory,assets=Path.Combine(root,"Assets");
            try {
                if(args.Length==2&&args[0]=="--self-check")return Validation.Run(assets,Path.GetFullPath(args[1]));
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using(var form=new MainForm(assets)) {
                    if(args.Length==2&&args[0]=="--ui-check")return form.CheckUi(Path.GetFullPath(args[1]));
                    if(args.Length==2&&(args[0]=="--render-preview"||args[0]=="--render-battle-preview"||args[0]=="--render-map-preview"||args[0]=="--render-legacy-preview"||args[0]=="--render-small-preview")) {
                        form.SavePreview(Path.GetFullPath(args[1]),args[0]=="--render-map-preview"?"map":args[0]=="--render-battle-preview"?"battle":args[0]=="--render-legacy-preview"?"legacy":args[0]=="--render-small-preview"?"small":"placement");
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
