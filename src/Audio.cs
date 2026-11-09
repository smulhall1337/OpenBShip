using System;
using System.IO;
using System.Media;
namespace FleetLab {
    // Short application effects, kept separate from finite cinematic voices.
    public sealed class Audio : IDisposable {
        SoundPlayer[] players=new SoundPlayer[2];
        MemoryStream[] streams=new MemoryStream[2];
        byte[][] originals;
        int level=100;bool enabled=true,disposed;
        public string Problem="";
        public bool Enabled {get{return enabled;}set{if(disposed)return;enabled=value;if(!value)Stop();}}
        public int Level {get{return level;}}
        public Audio(string root){
            try{originals=new[]{File.ReadAllBytes(Path.Combine(root,"SFX","KEYCLIK2.WAV")),File.ReadAllBytes(Path.Combine(root,"SFX","CANNON1.WAV"))};SetLevel(100);}
            catch(Exception e){Problem="Audio unavailable: "+e.Message;}
        }
        void Stop(){foreach(var player in players)if(player!=null)try{player.Stop();}catch(Exception e){Problem="Audio stop unavailable: "+e.Message;}}
        static void Release(SoundPlayer[] effects,MemoryStream[] data){foreach(var p in effects)if(p!=null)p.Dispose();foreach(var s in data)if(s!=null)s.Dispose();}
        public bool SetLevel(int percent){
            if(disposed)return false;SoundLevel.Validate(percent);if(originals==null)return false;
            if(percent==level&&players[0]!=null&&players[1]!=null)return true;
            var next=new SoundPlayer[2];var nextStreams=new MemoryStream[2];
            try{
                for(int i=0;i<2;i++){nextStreams[i]=new MemoryStream(SoundLevel.Wave(originals[i],percent),false);next[i]=new SoundPlayer(nextStreams[i]);next[i].Load();}
            }catch(Exception e){Release(next,nextStreams);Problem="Audio unavailable: "+e.Message;return false;}
            Stop();var previous=players;var oldStreams=streams;players=next;streams=nextStreams;level=percent;Problem="";Release(previous,oldStreams);return true;
        }
        public void Play(string kind){
            if(disposed||!Enabled||Problem!=""||level==0)return;
            try{if(kind=="fire")players[1].Play();else if(kind=="place")players[0].Play();}
            catch(Exception e){Problem="Audio unavailable: "+e.Message;}
        }
        public void Dispose(){if(disposed)return;disposed=true;Stop();Release(players,streams);players=new SoundPlayer[2];streams=new MemoryStream[2];originals=null;}
    }
}