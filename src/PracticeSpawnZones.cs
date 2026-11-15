using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab
{
    public sealed class PracticeSpawnZoneResult
    {
        readonly byte[] cells;
        readonly int[] counts=new int[4];
        public int PlayerCount { get; private set; }
        public int AvailableCells { get; private set; }
        public int PrimaryReservations { get; private set; }
        public int SecondaryReservations { get; private set; }
        internal PracticeSpawnZoneResult(byte[] cells,int players,int primary,int secondary)
        {
            this.cells=cells;
            PlayerCount=players;
            PrimaryReservations=primary;
            SecondaryReservations=secondary;
            for(int at=0;at<cells.Length;at+=2)if(cells[at]==4){AvailableCells++;counts[cells[at+1]]++;}
        }
        static int Index(int x,int y)
        {
            if(x<0||x>=64||y<0||y>=64)throw new ArgumentOutOfRangeException("x/y");
            return (x*64+y)*2;
        }
        public bool IsAvailable(int x,int y) { return cells[Index(x,y)]==4; }
        // The owner byte of a blocked cell is opaque, retained source data.
        public byte RawOwnerAt(int x,int y) { return cells[Index(x,y)+1]; }
        public int? OwnerAt(int x,int y) { int at=Index(x,y);return cells[at]==4?(int?)cells[at+1]:null; }
        public byte[] CopyCells() { return (byte[])cells.Clone(); }
        public int[] CopyOwnerCounts() { return (int[])counts.Clone(); }
    }
    // B9A8...6A569: whole42D400..42D5D9. Rebuilds source cell headers only;
    // it does not select random positions or initialize a mission.
    public static class PracticeSpawnZones
    {
        // Original446168 bytes30-47-8F-DF-DB-A5-4C-40, not180/PI.
        public const double DegreesPerRadian=57.2957725;
        static void Reservations(byte[] pool,List<Point> points)
        {
            for(int at=0;at<pool.Length;at+=492)if(BitConverter.ToInt32(pool,at)!=0)points.Add(new Point(BitConverter.ToInt16(pool,at+12),BitConverter.ToInt16(pool,at+14)));
        }
        public static PracticeSpawnZoneResult Rebuild(byte[] sourceCells,byte[] primary,byte[] secondary,int players)
        {
            if(sourceCells==null||sourceCells.Length!=8192)throw new InvalidDataException("Expected4096 two-byte source cell headers in outer-X/inner-Y order.");
            if(primary==null||primary.Length!=123000||secondary==null||secondary.Length!=246000)throw new InvalidDataException("Spawn-zone rebuilding requires complete250/500 reservation pools.");
            if(players<1||players>4)throw new ArgumentOutOfRangeException("players");
            var cells=(byte[])sourceCells.Clone();
            var points=new List<Point>();
            Reservations((byte[])primary.Clone(),points);
            int primaryCount=points.Count;
            Reservations((byte[])secondary.Clone(),points);
            int secondaryCount=points.Count-primaryCount,sector=360/players;
            for(int x=0;x<64;x++)for(int y=0;y<64;y++)
            {
                int at=(x*64+y)*2,worldX=x*10,worldY=y*10;
                cells[at]=0;
                if(x==0||y==0||x==63||y==63)continue;
                bool reserved=false;
                foreach(Point point in points)if(worldX>=point.X-10&&worldX<=point.X+10&&worldY>=point.Y-10&&worldY<=point.Y+10)
                {
                    reserved=true;
                    break;
                }
                if(reserved)continue;
                int angle=(int)(Math.Atan2(worldY-320,worldX-320)*DegreesPerRadian);
                if(angle<0)angle+=360;
                cells[at]=4;
                cells[at+1]=(byte)(angle/sector);
            }
            return new PracticeSpawnZoneResult(cells,players,primaryCount,secondaryCount);
        }
        // Deliberate copied profile: reserve the study's constructed base
        // fleets and the first500 SCN kind2 cells, in original scan order.
        // Only reservation-relevant fields are populated in the private
        // secondary pool. This is not a general terrain/entity constructor.
        public static PracticeSpawnZoneResult ForMapBaseStudy(Scenario scene,byte[] baseFleets)
        {
            if(scene==null||scene.Cells==null||scene.Cells.Length!=4096)throw new InvalidDataException("A complete SCN cell map is required.");
            var cells=new byte[8192];
            var secondary=new byte[246000];
            int count=0;
            for(int x=0;x<64;x++)for(int y=0;y<64;y++)
            {
                var cell=scene.At(x,y);
                if(cell==null)throw new InvalidDataException("Missing SCN cell.");
                int at=(x*64+y)*2;
                cells[at]=cell.Kind;
                cells[at+1]=cell.Value;
                if(cell.Kind!=2||count==500)continue;
                int record=count++*492;
                BitConverter.GetBytes(1).CopyTo(secondary,record);
                BitConverter.GetBytes((short)(x*10)).CopyTo(secondary,record+12);
                BitConverter.GetBytes((short)(y*10)).CopyTo(secondary,record+14);
            }
            return Rebuild(cells,baseFleets,secondary,scene.Players);
        }
    }
}
