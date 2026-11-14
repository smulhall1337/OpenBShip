using System;using System.IO;using System.Security.Cryptography;
namespace FleetLab {
    public sealed class PracticeAircraftDestructionTable {
        readonly byte[] points;
        public PracticeAircraftDestructionTable(byte[] data) {
            if(data==null||data.Length!=22)throw new InvalidDataException("Aircraft destruction requires its complete22-byte score table. Run Setup again.");
            using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(data)).Replace("-","")!="8BF91CF84FE38D66C6BA3DD71FCC29D56A55F3B609D41768F2D80C90B2C90A0F")throw new InvalidDataException("Unsupported aircraft destruction score table.");points=(byte[])data.Clone();
        }
        public static PracticeAircraftDestructionTable Load(string programs){return new PracticeAircraftDestructionTable(File.ReadAllBytes(Path.Combine(programs,"AircraftDestructionPoints.seqbin")));}
        public short Points(int type){if(type<8||type>10)throw new ArgumentOutOfRangeException("type");return BitConverter.ToInt16(points,type*2);}
    }
    public sealed class PracticeAircraftDestructionResult {
        readonly byte[] ships,fleets,scores,effects;
        public int Points {get;private set;}public int EffectSlot {get;private set;}public int MovieCounter {get;private set;}public uint RandomState {get;private set;}public int RandomCalls {get;private set;}public bool FleetRetired {get;private set;}
        internal PracticeAircraftDestructionResult(byte[] s,byte[] f,byte[] score,byte[] e,int points,int effect,int movie,NativeRandom random,bool retired){ships=s;fleets=f;scores=score;effects=e;Points=points;EffectSlot=effect;MovieCounter=movie;RandomState=random.State;RandomCalls=random.Calls;FleetRetired=retired;}
        public byte[] CopyShipPool(){return (byte[])ships.Clone();}public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}public byte[] CopyScoreRegion(){return (byte[])scores.Clone();}public byte[] CopyEffectPool(){return (byte[])effects.Clone();}
    }
    // Complete418BD0/419270 data profile for aircraft8..10, pinned RIP
    // B9A8...6A569. HP0,scenario0/network0/mode0/playercount0/localowner-2,
    // another selected fleet. Actual media, selection UI and missions excluded.
    public static class PracticeAircraftDestruction {
        static int W(byte[] b,int p){return BitConverter.ToInt16(b,p);}static int I(byte[] b,int p){return BitConverter.ToInt32(b,p);}static void I(byte[] b,int p,int v){BitConverter.GetBytes(v).CopyTo(b,p);}
        static int Reference(uint value){long at=(long)value-0x4fc340;if(at<0||at>=402000||at%268!=0)throw new InvalidDataException("Invalid aircraft destruction roster reference.");return (int)at;}
        public static PracticeAircraftDestructionResult Resolve(PracticeAircraftDestructionTable tables,byte[] visuals,
                byte[] shipPool,byte[] fleetPool,byte[] scoreRegion,byte[] effectPool,int victim,int index,bool clearHull,int attacker,int movieCounter,uint randomState){
            if(tables==null)throw new ArgumentNullException("tables");if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000||scoreRegion==null||scoreRegion.Length!=176||effectPool==null||effectPool.Length!=800)throw new InvalidDataException("Aircraft destruction requires complete original pools and scores.");new PracticeVisualPool(effectPool,visuals);
            if(victim<0||victim>=1500||index<0||index>=6||attacker< -1||attacker>3||movieCounter<short.MinValue||movieCounter>short.MaxValue)throw new ArgumentOutOfRangeException("victim");
            int unit=victim*268,type=I(shipPool,unit),parent=W(shipPool,unit+12),owner=W(shipPool,unit+6),x=W(shipPool,unit+8),y=W(shipPool,unit+10),length=W(shipPool,unit+14);if(type<8||type>10||W(shipPool,unit+16)!=0)throw new NotSupportedException("Aircraft destruction requires a type8..10 unit at zero HP.");if(parent<0||parent>=250||owner< -1||owner>3||W(shipPool,unit+4)!=victim||length<1||length>6||x<0||x>=10||y<0||y+length>10)throw new InvalidDataException("Unsupported aircraft destruction identity, hull or owner.");
            int fleet=parent*492,kind=I(fleetPool,fleet),expected=type==8?5:type==9?4:3;if(kind!=expected||W(scoreRegion,172)!=-2)throw new NotSupportedException("Aircraft destruction requires the matching active aircraft kind and offscreen owner context.");if(W(fleetPool,fleet+4)!=parent||I(fleetPool,fleet+16+index*4)!=0x4fc340+unit)throw new InvalidDataException("Aircraft destruction fleet/member identity differs.");
            for(int n=0;n<6;n++){uint reference=BitConverter.ToUInt32(fleetPool,fleet+16+n*4);if(reference==0)continue;int p=Reference(reference);if(n!=index&&p==unit)throw new NotSupportedException("Duplicate aircraft victim references require a separate study.");if(I(shipPool,p)!=0&&W(shipPool,p+4)!=p/268)throw new InvalidDataException("Aircraft roster member identity differs.");}
            byte[] ships=(byte[])shipPool.Clone(),fleets=(byte[])fleetPool.Clone(),scores=(byte[])scoreRegion.Clone(),effects=(byte[])effectPool.Clone();var random=new NativeRandom(randomState);short points=tables.Points(type);PracticeSegmentDamage.Score(scores,attacker,owner,points);int effect=-1;
            if(clearHull)for(int n=0;n<length;n++)I(fleets,fleet+64+(x*10+y+n)*4,0);
            else{if(W(visuals,type*20)!=-1)effects=PracticeMissImpact.SpawnEffect(effects,visuals,type,parent,x,y,out effect);if(type==9)random.Next();movieCounter=type==8?4:1;}
            I(ships,unit,0);I(fleets,fleet+16+index*4,0);byte speed=0;for(int n=0;n<6;n++){uint reference=BitConverter.ToUInt32(fleets,fleet+16+n*4);if(reference==0)continue;int p=Reference(reference);if(I(ships,p)==0)continue;int current=W(ships,p+72);if(speed==0)speed=unchecked((byte)current);if(speed>current)speed=unchecked((byte)current);}BitConverter.GetBytes((short)speed).CopyTo(fleets,fleet+60);
            if(fleets[fleet+468]==index){int first;for(first=0;first<6;first++){uint reference=BitConverter.ToUInt32(fleets,fleet+16+first*4);if(reference!=0&&I(ships,Reference(reference))!=0)break;}if(first<6)fleets[fleet+468]=(byte)first;else{if(kind==3||kind==4){int associated=W(fleets,fleet+8),link=W(fleets,fleet+10);if(associated<0||associated>=1500||link<0||link>3)throw new InvalidDataException("Aircraft parent link is outside the admitted record.");I(ships,associated*268+56+link*4,0);}I(fleets,fleet,0);}}
            return new PracticeAircraftDestructionResult(ships,fleets,scores,effects,points,effect,movieCounter,random,I(fleets,fleet)==0);
        }
    }
}
