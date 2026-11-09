using System;
using System.IO;
using System.Media;
namespace FleetLab {
    public sealed class Audio : IDisposable {
        SoundPlayer click,fire;
        public bool Enabled=true;
        public string Problem="";
        public Audio(string root) {
            try {
                click=new SoundPlayer(Path.Combine(root,"SFX","KEYCLIK2.WAV"));
                fire=new SoundPlayer(Path.Combine(root,"SFX","CANNON1.WAV"));
                click.Load();
                fire.Load();
            }
            catch(Exception e) {
                Problem="Audio unavailable: "+e.Message;
            }
        }
        public void Play(string kind) {
            if(!Enabled||Problem!="")return;
            try {
                if(kind=="fire")fire.Play();
                else if(kind=="place")click.Play();
            }
            catch(Exception e) {
                Problem="Audio unavailable: "+e.Message;
            }
        }
        public void Dispose() {
            if(click!=null) {
                click.Stop();
                click.Dispose();
            }
            if(fire!=null) {
                fire.Stop();
                fire.Dispose();
            }
        }
    }
}
