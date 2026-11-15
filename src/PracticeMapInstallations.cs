using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
namespace FleetLab
{
    public sealed class PracticeMapInstallation
    {
        public readonly Point SourceCell, Origin;
        public readonly int FirstFleet, Owner, SourceTag, TagArgument;
        internal PracticeMapInstallation(Point cell,int first,int owner,int tag)
        {
            SourceCell=cell;
            Origin=new Point(cell.X*10,cell.Y*10);
            FirstFleet=first;
            Owner=owner;
            SourceTag=tag;
            TagArgument=tag==3?0:tag-30;
        }
        public override string ToString()
        {
            return "Fleets "+FirstFleet+"–"+(FirstFleet+3)+" · owner "+Owner+" · "+Origin.X+", "+Origin.Y;
        }
    }
    // Pinned RIP B9A8...6A569: ordinary402D70 base-tag scan and4030F0.
    // ONLY this construction stage: other terrain/objects, grouped fleets,
    // postload, support and scenario-specific constructors are not executed.
    public sealed class PracticeMapInstallations
    {
        readonly byte[] ships,fleets;
        readonly uint[] ownerCounters={1,1,1,1,1};
        readonly ReadOnlyCollection<PracticeMapInstallation> installations;
        public ReadOnlyCollection<PracticeMapInstallation> Installations { get { return installations; } }
        public string SourceName { get; private set; }
        public string SourceHash { get; private set; }
        public int SourceBytes { get; private set; }
        public int CandidateCells { get; private set; }
        public int DuplicateCells { get; private set; }
        public int NextFleetSlot { get { return installations.Count*4; } }
        public int UnitCount { get { return installations.Count*10; } }
        public byte[] CopyShipPool() { return (byte[])ships.Clone(); }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
        // Index0 is source owner-1, indices1..4 are owners0..3 (5C3360).
        public uint[] CopyOwnerCounters() { return (uint[])ownerCounters.Clone(); }
        public static PracticeMapInstallations Create(PracticeAirfieldFactory factory,byte[] source,string name)
        {
            if(factory==null)throw new ArgumentNullException("factory");
            if(source==null)throw new ArgumentNullException("source");
            return new PracticeMapInstallations(factory,(byte[])source.Clone(),name);
        }
        PracticeMapInstallations(PracticeAirfieldFactory factory,byte[] source,string name)
        {
            var scene=Scenario.Load(source,name??"SCN copy");
            SourceName=name??"SCN copy";
            SourceBytes=source.Length;
            using(var hash=SHA256.Create())SourceHash=BitConverter.ToString(hash.ComputeHash(source)).Replace("-","");
            var s=new byte[402000];
            var f=new byte[123000];
            for(int n=0;n<1500;n++)BitConverter.GetBytes((short)n).CopyTo(s,n*268+4);
            for(int n=0;n<250;n++)PracticeFleetSnapshot.InitializeRootFleet(f,n*492,n);
            var created=new List<PracticeMapInstallation>();
            for(int x=0;x<64;x++)for(int y=0;y<64;y++)
            {
                Cell cell=scene.At(x,y);
                int tag=cell.Kind;
                if(tag!=3&&(tag<31||tag>33))continue;
                CandidateCells++;
                bool duplicate=false;
                for(int slot=0;slot<250;slot++)
                {
                    int at=slot*492;
                    if(BitConverter.ToInt32(f,at)==2&&BitConverter.ToInt16(f,at+12)==x*10&&BitConverter.ToInt16(f,at+14)==y*10)
                    {
                        duplicate=true;
                        break;
                    }
                }
                if(duplicate)
                {
                    DuplicateCells++;
                    continue;
                }
                int owner=unchecked((sbyte)cell.Value),first=created.Count*4;
                if(owner< -1||owner>3)throw new InvalidDataException("Base owner is outside the five recovered counter entries.");
                if(first>246)throw new InvalidDataException("Map base construction exceeds the complete fleet pool; no result was published.");
                var result=factory.Create(s,f,first,new Point(x*10,y*10),(short)owner,(byte)(first/4+1));
                s=result.CopyShipPool();
                f=result.CopyFleetPool();
                ownerCounters[owner+1]++;
                created.Add(new PracticeMapInstallation(new Point(x,y),first,owner,tag));
            }
            ships=s;
            fleets=f;
            installations=created.AsReadOnly();
        }
    }
}
