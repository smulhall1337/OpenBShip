using System;
using System.IO;
using System.Security.Cryptography;

namespace FleetLab {
    public sealed class PracticeMissImpactResult {
        readonly byte[] timed,effects;
        public uint RandomState { get; private set; }
        public int RandomCalls { get; private set; }
        public int EffectSlot { get; private set; }
        public int EffectType { get; private set; }
        internal PracticeMissImpactResult(byte[] records,byte[] visuals,uint state,int calls,int slot,int type) {
            timed=records;effects=visuals;RandomState=state;RandomCalls=calls;EffectSlot=slot;EffectType=type;
        }
        public byte[] CopyTimedPool(){return (byte[])timed.Clone();}
        public byte[] CopyEffectPool(){return (byte[])effects.Clone();}
    }

    // Supported RIP B9A8...6A569:416DD0 non-ship cells0/1/3 ->416E35,
    // 4177A0 RNG path and420550 creator. Local UI/media and network excluded.
    // The extended manual study admits this branch; hit paths stay guarded.
    public static class PracticeMissImpact {
        static int Word(byte[] b,int at){return BitConverter.ToInt16(b,at);}
        static int Dword(byte[] b,int at){return BitConverter.ToInt32(b,at);}
        static void Word(byte[] b,int at,int value){BitConverter.GetBytes(unchecked((short)value)).CopyTo(b,at);}
        static void Dword(byte[] b,int at,int value){BitConverter.GetBytes(value).CopyTo(b,at);}
        public static PracticeMissImpactResult Resolve(byte[] timedPool,byte[] fleetPool,byte[] effectPool,
                                                         byte[] visualRecords,int timedSlot,uint randomState) {
            if(timedPool==null||timedPool.Length!=21440||fleetPool==null||fleetPool.Length!=123000||effectPool==null||effectPool.Length!=800||visualRecords==null||visualRecords.Length!=580)
                throw new InvalidDataException("Miss impact requires complete original pools and visual records.");
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(visualRecords)).Replace("-","");
            if(hash!="CF815C9CD12610B8D775DE811992A490A8DA627998CA202AE1BF43563AFDE5C3")throw new InvalidDataException("Unsupported visual record identity.");
            if(timedSlot<0||timedSlot>=80)throw new ArgumentOutOfRangeException("timedSlot");
            int at=timedSlot*268,weapon=Dword(timedPool,at);
            if(weapon<14||weapon>30||weapon==19||weapon==21)
                throw new NotSupportedException("This projectile uses an unreconstructed expiry branch.");
            int target=Word(timedPool,at+12),x=Word(timedPool,at+18),y=Word(timedPool,at+20);
            if(target<0||target>=250||x<0||x>=10||y<0||y>=10)
                throw new InvalidDataException("Miss impact target is outside the admitted fleet grid.");
            int fleet=target*492,kind=Dword(fleetPool,fleet),cell=Dword(fleetPool,fleet+64+(x*10+y)*4);
            if(kind!=1&&kind!=2&&kind!=9&&kind!=11)throw new NotSupportedException("Unsupported miss-impact fleet kind.");
            if(cell!=0&&cell!=1&&cell!=3)throw new NotSupportedException("Hit, defense and other target-cell expiry effects are not reconstructed.");
            var random=new NativeRandom(randomState);
            // Random choice is consumed before effect allocation, including
            // when the visual pool is full. Selection does not play media here.
            if((weapon>=14&&weapon<=18&&kind!=2)||(weapon==25||weapon==26)&&kind!=9)random.Next();
            byte[] timed=(byte[])timedPool.Clone(),effects=(byte[])effectPool.Clone();
            Dword(timed,at,0);int type=kind==1||kind==9?27:28,slot=-1;
            for(int i=0;i<40;i++)if(Word(effects,i*20)==-1){slot=i;break;}
            if(slot>=0){int entry=slot*20;Word(effects,entry,target);Word(effects,entry+2,x);Word(effects,entry+4,y);Dword(effects,entry+8,Dword(visualRecords,type*20+8));Word(effects,entry+12,0);Dword(effects,entry+16,type);}
            return new PracticeMissImpactResult(timed,effects,random.State,random.Calls,slot,type);
        }
    }
}
