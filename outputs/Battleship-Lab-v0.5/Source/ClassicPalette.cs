using System;
using System.IO;
using System.Drawing;
namespace FleetLab {
    public static class ClassicPalette {
        // Bs.exe 0x42A7D6..0x42A840: patches after loading ISLAND.COL.
        public static byte[] Patched(byte[] island) {
            if(island==null||island.Length!=768)throw new InvalidDataException("Classic palette requires 768 RGB bytes");
            var result=(byte[])island.Clone();
            int[] offsets={51,52,53,54,55,56,57,58,59,39,30,31,32,40,41};
            byte[] values={166,178,198,206,215,227,138,150,174,13,255,255,255,114,255};
            for(int i=0;i<offsets.Length;i++)result[offsets[i]]=values[i];
            return result;
        }
        // Native fade arithmetic (0x422C70) only. Its original presentation
        // cadence has not been established; automatic fades are not enabled.
        public static byte[] Fade(byte[] rgb,int factor) {
            if(rgb==null||rgb.Length!=768||factor<0||factor>64)throw new InvalidDataException("Invalid palette fade input");
            var result=new byte[768];
            for(int i=0;i<768;i++)result[i]=(byte)(rgb[i]*factor/64);
            return result;
        }
        public static Color[] Gameplay(byte[] island) {
            byte[] rgb=Patched(island);
            var result=new Color[256];
            for(int i=0;i<256;i++)result[i]=Color.FromArgb(rgb[i*3],rgb[i*3+1],rgb[i*3+2]);
            // Native 0x401D40 updates only 10..245. Reserve endpoints using the
            // deterministic Windows color values observed by the local probe.
            // These are a documented modern boundary, not a 1997 palette capture.
            int[] indices={0,1,2,3,4,5,6,7,8,9,246,247,248,249,250,251,252,253,254,255};
            int[] colors={0x000000,0x800000,0x008000,0x808000,0x000080,0x800080,0x008080,0xC0C0C0,0xC0DCC0,0xA6CAF0,
                0xFFFBF0,0xA0A0A4,0x808080,0xFF0000,0x00FF00,0xFFFF00,0x0000FF,0xFF00FF,0x00FFFF,0xFFFFFF};
            for(int i=0;i<indices.Length;i++)result[indices[i]]=Color.FromArgb((colors[i]>>16)&255,(colors[i]>>8)&255,colors[i]&255);
            return result;
        }
    }
}
