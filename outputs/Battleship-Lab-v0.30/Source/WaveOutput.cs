using System;
using System.Runtime.InteropServices;
namespace FleetLab {
    public interface IClipOutput : IDisposable {bool Active {get;}int Samples {get;}void Start(short[] pcm,int offset);void Pause();void Resume();void Stop();}
    // One finite, owned PCM buffer. No original DLL, process or global mixer volume.
    public sealed class WaveOutput : IClipOutput {
        [StructLayout(LayoutKind.Sequential,Pack=2)] struct Format {public ushort Tag,Channels;public uint Rate,BytesPerSecond;public ushort Align,Bits,Extra;}
        [StructLayout(LayoutKind.Sequential)] struct Header {public IntPtr Data;public uint Length,Recorded;public UIntPtr User;public uint Flags,Loops;public IntPtr Next;public UIntPtr Reserved;}
        [StructLayout(LayoutKind.Explicit,Size=12)] struct Position {[FieldOffset(0)]public uint Type;[FieldOffset(4)]public uint Value;}
        [DllImport("winmm.dll")] static extern uint waveOutOpen(out IntPtr device,uint id,ref Format format,IntPtr callback,IntPtr instance,uint flags);
        [DllImport("winmm.dll")] static extern uint waveOutPrepareHeader(IntPtr device,IntPtr header,uint size);
        [DllImport("winmm.dll")] static extern uint waveOutWrite(IntPtr device,IntPtr header,uint size);
        [DllImport("winmm.dll")] static extern uint waveOutPause(IntPtr device);
        [DllImport("winmm.dll")] static extern uint waveOutRestart(IntPtr device);
        [DllImport("winmm.dll")] static extern uint waveOutReset(IntPtr device);
        [DllImport("winmm.dll")] static extern uint waveOutGetPosition(IntPtr device,ref Position position,uint size);
        [DllImport("winmm.dll")] static extern uint waveOutUnprepareHeader(IntPtr device,IntPtr header,uint size);
        [DllImport("winmm.dll")] static extern uint waveOutClose(IntPtr device);
        IntPtr device,header,data;bool prepared;int start,count;readonly uint size=(uint)Marshal.SizeOf(typeof(Header));
        public string Problem="";public bool Active {get{return device!=IntPtr.Zero&&prepared;}}
        static void Check(uint code,string action){if(code!=0)throw new InvalidOperationException("Audio "+action+" failed (WinMM "+code+").");}
        public static int HeaderBytes {get{return Marshal.SizeOf(typeof(Header));}}
        public void Start(short[] pcm,int offset){
            Stop();if(pcm==null||offset<0||offset>=pcm.Length)throw new ArgumentException("Invalid finite audio range");start=offset;count=pcm.Length-offset;
            try{
                var format=new Format{Tag=1,Channels=1,Rate=ClipSound.Rate,BytesPerSecond=ClipSound.Rate*2,Align=2,Bits=16};
                Check(waveOutOpen(out device,uint.MaxValue,ref format,IntPtr.Zero,IntPtr.Zero,0),"open");
                data=Marshal.AllocHGlobal(count*2);Marshal.Copy(pcm,offset,data,count);header=Marshal.AllocHGlobal((int)size);
                Marshal.StructureToPtr(new Header{Data=data,Length=(uint)(count*2)},header,false);
                Check(waveOutPrepareHeader(device,header,size),"prepare");prepared=true;Check(waveOutWrite(device,header,size),"write");Problem="";
            }catch{Dispose();throw;}
        }
        public int Samples {
            get {
                if(!Active)throw new InvalidOperationException("Audio stream is not active");
                var h=(Header)Marshal.PtrToStructure(header,typeof(Header));if((h.Flags&1)!=0)return start+count;
                var position=new Position{Type=2};Check(waveOutGetPosition(device,ref position,12),"position");
                long value=position.Type==2?position.Value:position.Type==4?position.Value/2:position.Type==1?(long)position.Value*ClipSound.Rate/1000:-1;
                if(value<0)throw new InvalidOperationException("Audio device returned an unsupported position unit");return start+(int)Math.Min(count,value);
            }
        }
        public void Pause(){if(Active)Check(waveOutPause(device),"pause");}
        public void Resume(){if(Active)Check(waveOutRestart(device),"resume");}
        public void Stop(){
            if(device!=IntPtr.Zero){Check(waveOutReset(device),"reset");if(prepared){Check(waveOutUnprepareHeader(device,header,size),"unprepare");prepared=false;}Check(waveOutClose(device),"close");device=IntPtr.Zero;}
            if(header!=IntPtr.Zero){Marshal.FreeHGlobal(header);header=IntPtr.Zero;}if(data!=IntPtr.Zero){Marshal.FreeHGlobal(data);data=IntPtr.Zero;}
        }
        public void Dispose(){try{Stop();}catch(Exception e){Problem=e.Message+" Native storage retained until safe release.";}}
        public static int CheckDevice(string report){
            var lines=new System.Collections.Generic.List<string>();
            try{if(IntPtr.Size!=8||HeaderBytes!=48)throw new Exception("Expected x64 WAVEHDR layout");using(var output=new WaveOutput()){
                short[] silence=new short[ClipSound.Rate/2];output.Start(silence,0);System.Threading.Thread.Sleep(140);int before=output.Samples;
                if(before<=0)throw new Exception("Device position did not advance");output.Pause();int paused=output.Samples;System.Threading.Thread.Sleep(90);int after=output.Samples;
                if(after-paused>ClipSound.SamplesPerFrame)throw new Exception("Pause did not hold device position");output.Resume();var watch=System.Diagnostics.Stopwatch.StartNew();while(output.Samples<silence.Length&&watch.ElapsedMilliseconds<1800)System.Threading.Thread.Sleep(10);
                if(output.Samples!=silence.Length)throw new Exception("Finite buffer did not complete");output.Stop();if(output.Active)throw new Exception("Stop retained a prepared stream");output.Start(silence,100);System.Threading.Thread.Sleep(40);if(output.Samples<100)throw new Exception("Seek base sample lost");output.Stop();output.Stop();
                lines.Add("PASS x64 waveOut open/prepare/write, position advance, pause/restart, finite completion, reset/unprepare/close, reopen/seek and idempotent stop.");lines.Add("Observed device samples before pause="+before+", paused="+paused+", after wait="+after+".");
            }lines.Add("The submitted PCM buffer was SILENT. This verifies target API/device behavior, not audible output or physical audio/video synchronization.");System.IO.File.WriteAllLines(report,lines.ToArray());return 0;}
            catch(Exception e){lines.Add("FAIL device check: "+e.Message);lines.Add("The application reports audio failure and continues visual playback silently.");System.IO.File.WriteAllLines(report,lines.ToArray());return 1;}
        }
    }
}
