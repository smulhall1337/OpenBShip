using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
namespace FleetLab {
    public sealed class DisplaySettings {
        public bool Sound=true,RecoveredColors,Motion=true;
        public string Problem="";
        readonly SortedDictionary<string,string> extra=new SortedDictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public static DisplaySettings Parse(string text) {
            var settings=new DisplaySettings();
            using(var reader=new StringReader(text)) {
                string line;
                while((line=reader.ReadLine())!=null) {
                    line=line.Trim();if(line.Length==0)continue;
                    int at=line.IndexOf('=');
                    if(at<1) { settings.Problem="Ignored an invalid settings line.";continue; }
                    string key=line.Substring(0,at).Trim(),value=line.Substring(at+1).Trim();
                    if(key.Equals("colors",StringComparison.OrdinalIgnoreCase)) {
                        if(value=="recovered"||value=="accepted")settings.RecoveredColors=value=="recovered";
                        else settings.Problem="Unknown color preference; using the current default.";
                    }
                    else if(key.Equals("sound",StringComparison.OrdinalIgnoreCase)||key.Equals("motion",StringComparison.OrdinalIgnoreCase)) {
                        bool enabled;
                        if(!bool.TryParse(value,out enabled)) {settings.Problem="Invalid boolean setting; using the current default.";continue;}
                        if(key.Equals("sound",StringComparison.OrdinalIgnoreCase))settings.Sound=enabled;else settings.Motion=enabled;
                    }
                    else settings.extra[key]=value;
                }
            }
            return settings;
        }
        public static DisplaySettings Load(string path) {
            try{return File.Exists(path)?Parse(File.ReadAllText(path)):new DisplaySettings();}
            catch(Exception e){return new DisplaySettings {Problem="Settings unavailable: "+e.Message};}
        }
        public string Serialize() {
            var text=new StringBuilder();
            text.AppendLine("sound="+(Sound?"true":"false"));
            text.AppendLine("colors="+(RecoveredColors?"recovered":"accepted"));
            text.AppendLine("motion="+(Motion?"true":"false"));
            foreach(var pair in extra)text.AppendLine(pair.Key+"="+pair.Value);
            return text.ToString();
        }
        public void Save(string path) {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary=path+".new";
            File.WriteAllText(temporary,Serialize());
            if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
        }
    }
}
