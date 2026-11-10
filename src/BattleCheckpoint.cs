using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace FleetLab {
    // Modern local checkpoint, not the original game's save format. The small
    // action history is replayed through Game so resumed turns keep the same
    // targeting, weapon/movie choices and five isolated random streams.
    public sealed partial class Game {
        sealed class BattleTurn {
            public readonly int Shooter;
            public readonly Point Target;
            public BattleTurn(int shooter,Point target){Shooter=shooter;Target=target;}
        }
        readonly List<BattleTurn> battleTurns=new List<BattleTurn>();
        const int SaveLimit=8192;
        const string SaveHeader="OPENBSHIP-BATTLE 1";
        static readonly CultureInfo SaveCulture=CultureInfo.InvariantCulture;
        public bool CanSaveBattle {
            get {return Rules==RuleProfile.Recovered&&Opponent==OpponentKind.Original&&
                CanRestartBattle&&Phase==Phase.PlayerTurn&&Pending==null&&battleTurns.Count<=99;}
        }
        static string SaveHash(byte[] bytes) {
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");
        }
        static string Number(int value){return value.ToString(SaveCulture);}
        static string StreamState(NativeRandom random){return random.State.ToString(SaveCulture)+","+Number(random.Calls);}
        static string Deployment(Board board) {
            var result=new List<string>();
            if(board==null||board.Ships.Count!=5)throw new InvalidDataException("Expected a complete Classic fleet");
            for(int i=0;i<5;i++) {
                Ship ship=board.Ships[i];ShipSpec spec=RuleProfile.Recovered.Fleet[i];
                if(ship.SourceType!=spec.SourceType||ship.Cells.Count!=spec.Length)throw new InvalidDataException("Unsupported Classic ship roster");
                Point first=ship.Cells[0];
                for(int j=0;j<spec.Length;j++)if(ship.Cells[j]!=new Point(first.X,first.Y+j))throw new InvalidDataException("Unsupported Classic ship axis");
                result.Add(Number(first.X)+","+Number(first.Y));
            }
            return string.Join(";",result.ToArray());
        }
        static string BoardState(Board board) {
            var text=new StringBuilder(Deployment(board));text.Append('|');
            for(int y=0;y<10;y++)for(int x=0;x<10;x++)text.Append(board.Shots[x,y]?'1':'0');
            foreach(var ship in board.Ships)text.Append('|').Append(Number(ship.Hits)).Append(':').Append(ship.Name);
            return text.ToString();
        }
        static string ShotState(Shot shot) {
            return shot==null?"-":Number(shot.Cell.X)+","+Number(shot.Cell.Y)+","+shot.Hit+","+shot.Sunk+","+shot.Repeated+","+shot.ShipName;
        }
        static string FireState(FiringPresentation fire) {
            return fire==null?"-":fire.Shooter+","+Number(fire.WeaponId)+","+fire.WeaponName+","+Number(fire.MovieId)+","+fire.MovieName+","+Number(fire.RandomCalls);
        }
        string BattleProof() {
            var text=new StringBuilder();
            foreach(string value in new[]{BoardState(Player),BoardState(Enemy),Number(Turn),Number(PlayerShooter),Number(EnemyShooter),
                StreamState(classicPlacementRandom),StreamState(originalRandom),StreamState(presentationRandom),StreamState(travelRandom),StreamState(impactRandom),
                ShotState(LastPlayerShot),ShotState(LastEnemyShot),FireState(LastPlayerFire),FireState(LastEnemyFire),string.Join("\n",Events.ToArray())})
                text.Append(Number(value.Length)).Append(':').Append(value).Append('\n');
            if(LastOpponentDecision!=null)text.Append(Number(LastOpponentDecision.Cell.X)).Append(',').Append(Number(LastOpponentDecision.Cell.Y)).Append(':').Append(LastOpponentDecision.Reason)
                .Append(':').Append(Number(LastOpponentDecision.Attempts)).Append(':').Append(Number(LastOpponentDecision.RandomCalls)).Append(':').Append(LastOpponentDecision.Stalled);
            return SaveHash(Encoding.UTF8.GetBytes(text.ToString()));
        }
        public byte[] SerializeBattle() {
            if(!CanSaveBattle)throw new InvalidDataException("Save is available between turns in Classic with an original opponent, after all shot movies finish.");
            var turns=new List<string>();
            foreach(var turn in battleTurns)turns.Add(Number(turn.Shooter)+","+Number(turn.Target.X)+","+Number(turn.Target.Y));
            string body=SaveHeader+"\nseed="+Number(Seed)+"\ndifficulty="+Number(OpponentLevel)+"\nmovies="+(MovieTurns?"1":"0")+
                "\nplacement="+StreamState(startingPlacement)+"\nplayer="+Deployment(startingPlayer)+"\nenemy="+Deployment(startingEnemy)+
                "\nshots="+string.Join(";",turns.ToArray())+"\nselected="+Number(PlayerShooter)+"\nproof="+BattleProof()+"\n";
            byte[] bytes=Encoding.ASCII.GetBytes(body+"sha256="+SaveHash(Encoding.ASCII.GetBytes(body))+"\n");
            if(bytes.Length>SaveLimit)throw new InvalidDataException("Battle checkpoint exceeds its supported size");
            // Refuse unsupported mutations instead of writing a file that cannot
            // reproduce this exact stable state on its next load.
            ParseBattle(bytes);return bytes;
        }
        static string Value(string line,string key) {
            string prefix=key+"=";
            if(!line.StartsWith(prefix,StringComparison.Ordinal))throw new InvalidDataException("Missing or reordered battle field: "+key);
            return line.Substring(prefix.Length);
        }
        static int Integer(string value,int min,int max) {
            int result;
            if(!int.TryParse(value,NumberStyles.AllowLeadingSign,SaveCulture,out result)||result<min||result>max||value!=Number(result))throw new InvalidDataException("Invalid battle integer");
            return result;
        }
        static Board ReadDeployment(string value) {
            string[] records=value.Split(';');if(records.Length!=5)throw new InvalidDataException("Expected five saved ships");
            var board=new Board();
            for(int i=0;i<5;i++) {
                string[] pair=records[i].Split(',');if(pair.Length!=2)throw new InvalidDataException("Invalid saved ship coordinate");
                var spec=RuleProfile.Recovered.Fleet[i];
                if(!board.Place(new Point(Integer(pair[0],0,9),Integer(pair[1],0,9)),spec.Length,true,spec.Name,spec.SourceType))throw new InvalidDataException("Saved ships overlap or cross the board edge");
            }
            return board;
        }
        static NativeRandom ReadStream(string value) {
            string[] pair=value.Split(',');uint state;
            if(pair.Length!=2||!uint.TryParse(pair[0],NumberStyles.None,SaveCulture,out state)||pair[0]!=state.ToString(SaveCulture))throw new InvalidDataException("Invalid saved placement stream");
            return NativeRandom.Restore(state,Integer(pair[1],0,1000000));
        }
        static void FinishSavedMovie(Game game) {
            int stages=0;
            while(game.Pending!=null) {
                if(++stages>3||!game.CompleteMovie(game.Pending,game.Pending.Stage))throw new InvalidDataException("Saved shot cannot complete");
            }
        }
        Game(int seed,int level,bool movies,NativeRandom placement,Board player,Board enemy)
            :this(seed,RuleProfile.Recovered,OpponentKind.Original,level,movies) {
            classicPlacementRandom=placement;Player=player;Enemy=enemy;EnterBattle();
        }
        public static Game ParseBattle(byte[] bytes) {
            if(bytes==null||bytes.Length<200||bytes.Length>SaveLimit)throw new InvalidDataException("Unsupported battle checkpoint size");
            foreach(byte b in bytes)if(b>127||b==13||b==0)throw new InvalidDataException("Battle checkpoint must use canonical ASCII lines");
            string text=Encoding.ASCII.GetString(bytes);string[] lines=text.Split('\n');
            if(lines.Length!=12||lines[0]!=SaveHeader||lines[11]!="")throw new InvalidDataException("Unsupported battle checkpoint version or field count");
            int checksumAt=text.LastIndexOf("sha256=",StringComparison.Ordinal);
            if(checksumAt<0||Value(lines[10],"sha256")!=SaveHash(Encoding.ASCII.GetBytes(text.Substring(0,checksumAt))))throw new InvalidDataException("Battle checkpoint checksum failed");
            int seed=Integer(Value(lines[1],"seed"),int.MinValue,int.MaxValue),level=Integer(Value(lines[2],"difficulty"),0,3);
            bool movies=Integer(Value(lines[3],"movies"),0,1)==1;
            var placement=ReadStream(Value(lines[4],"placement"));
            Board player=ReadDeployment(Value(lines[5],"player")),enemy=ReadDeployment(Value(lines[6],"enemy"));
            var game=new Game(seed,level,movies,placement,player,enemy);
            string history=Value(lines[7],"shots");string[] turns=history==""?new string[0]:history.Split(';');
            if(turns.Length>99)throw new InvalidDataException("Too many saved Classic turns");
            foreach(string turn in turns) {
                string[] fields=turn.Split(',');if(fields.Length!=3)throw new InvalidDataException("Invalid saved shot record");
                int shooter=Integer(fields[0],0,4);var target=new Point(Integer(fields[1],0,9),Integer(fields[2],0,9));
                if(!game.SelectShooter(shooter)||!game.Fire(target))throw new InvalidDataException("Saved shot is invalid at this turn");
                FinishSavedMovie(game);
                if(game.Phase!=Phase.OpponentTurn||!game.AdvanceOpponent())throw new InvalidDataException("Saved opponent turn cannot proceed");
                FinishSavedMovie(game);
                if(game.Phase!=Phase.PlayerTurn)throw new InvalidDataException("Saved battle already ended");
            }
            if(!game.SelectShooter(Integer(Value(lines[8],"selected"),0,4)))throw new InvalidDataException("Saved firing ship is unavailable");
            if(Value(lines[9],"proof")!=game.BattleProof())throw new InvalidDataException("Saved battle does not reproduce its recorded state with this build");
            game.Paused=true;game.Message="Saved battle loaded at turn "+Number(game.Turn)+". Press P or Resume to continue.";return game;
        }
        public void SaveBattle(string path) {
            byte[] bytes=SerializeBattle();string full=Path.GetFullPath(path),temporary=full+".new";
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            try {
                File.WriteAllBytes(temporary,bytes);
                if(File.Exists(full))File.Replace(temporary,full,full+".bak");else File.Move(temporary,full);
            }
            finally {try{if(File.Exists(temporary))File.Delete(temporary);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }
        public static Game LoadBattle(string path) {
            using(var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) {
                if(input.Length>SaveLimit)throw new InvalidDataException("Battle checkpoint exceeds its supported size");
                var bytes=new byte[(int)input.Length];int read=0;
                while(read<bytes.Length){int got=input.Read(bytes,read,bytes.Length-read);if(got==0)throw new InvalidDataException("Battle checkpoint was truncated");read+=got;}
                return ParseBattle(bytes);
            }
        }
    }
}
