using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab
{
    public sealed class PracticeInstallationOriginResult
    {
        public readonly Point Origin;
        public readonly uint RandomState;
        public readonly int RandomCalls,Attempts;
        internal PracticeInstallationOriginResult(Point origin,NativeRandom random,int attempts)
        {
            Origin=origin;
            RandomState=random.State;
            RandomCalls=random.Calls;
            Attempts=attempts;
        }
    }
    // RIP B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569.
    // 42D5E0..42D7A8: supplied CRT seed, player sector and two reservation pools.
    // World units and signed-coordinate conventions retained. No terrain lookup.
    public static class PracticeInstallationOrigin
    {
        // Source double at446170, bytes1DE1639946DF913F; do not replace with PI/180.
        const double RadiansPerDegree=0.017453292;
        public static Point ProjectCandidate(int angle,int radius)
        {
            if(angle<10||angle>=350||radius<50||radius>=370)throw new ArgumentOutOfRangeException("angle/radius");
            double radians=angle*RadiansPerDegree;
            return new Point(((int)(Math.Cos(radians)*radius+320)/10)*10,((int)(Math.Sin(radians)*radius+320)/10)*10);
        }
        static void AddReservations(byte[] pool,int count,List<Point> result)
        {
            if(pool==null||pool.Length!=count*492)throw new InvalidDataException("Complete installation origin reservation pools required.");
            for(int n=0;n<count;n++)
            {
                int at=n*492;
                if(BitConverter.ToInt32(pool,at)==0)continue;
                result.Add(new Point(BitConverter.ToInt16(pool,at+12),BitConverter.ToInt16(pool,at+14)));
            }
        }
        public static PracticeInstallationOriginResult Find(byte[] primary,byte[] secondary,int players,int owner,uint seed,int attemptLimit=50000)
        {
            if(players<1||players>4||owner< -1||owner>=players||attemptLimit<1||attemptLimit>50000)throw new ArgumentOutOfRangeException("players/owner/attemptLimit");
            var reservations=new List<Point>();
            AddReservations(primary,250,reservations);
            AddReservations(secondary,500,reservations);
            var random=new NativeRandom(seed);
            int sector=owner<0?360:360/players,start=owner<0?0:sector*owner;
            for(int attempt=1;attempt<=attemptLimit;attempt++)
            {
                int angle=random.Next()%360;
                if(angle<start+10||angle>=start+sector-10)continue;
                var point=ProjectCandidate(angle,random.Next()%320+50);
                if(point.X<0||point.Y<0||point.X>=630||point.Y>=630)continue;
                bool blocked=false;
                foreach(var other in reservations)if(point.X>=other.X-10&&point.X<=other.X+10&&point.Y>=other.Y-10&&point.Y<=other.Y+10)
                {
                    blocked=true;
                    break;
                }
                if(!blocked)return new PracticeInstallationOriginResult(point,random,attempt);
            }
            // The source retries forever. Return no partial RNG or caller-pool change.
            throw new InvalidDataException("Installation origin search reached its bounded attempt limit; caller pools and seed were preserved.");
        }
    }
}
