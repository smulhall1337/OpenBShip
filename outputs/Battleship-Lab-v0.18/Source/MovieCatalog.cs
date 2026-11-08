using System;
using System.Collections.Generic;
namespace FleetLab {
    public static class MovieCatalog {
        public static readonly string[] Endings={"VICTORY","LOSE"};
        public static readonly string[] Names=All();
        static string[] All(){var names=new List<string>(FiringClips.Names);names.AddRange(Endings);return names.ToArray();}
        public static bool IsEnding(string name){return name=="VICTORY"||name=="LOSE";}
        public static int FrameMilliseconds(string name){Validate(name);return IsEnding(name)?50:40;}
        public static string Directory(string name,bool high){Validate(name);return high||IsEnding(name)?"HWEBS":"LWEBS";}
        public static string PaletteDirectory(string name){Validate(name);return IsEnding(name)?"HWEBS":"LWEBS";}
        static void Validate(string name){if(Array.IndexOf(Names,name)<0)throw new ArgumentException("Unknown movie resource", "name");}
    }
}
