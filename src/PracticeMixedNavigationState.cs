using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;

namespace FleetLab
{
    // Bounded whole41D640/41FCC0 comparison from original996r3, independently
    // replayed at997/999. The caller word, phase and clock are supplied inputs.
    // This does not enable the old policy/navigation APIs for arbitrary worlds.
    public sealed class PracticeMixedNavigationState : IPracticeMixedComparison
    {
        static readonly string[] names = {
            "Policy first · stored aircraft / active route",
            "Navigation first · blocked destination / activation",
            "Navigation first · route start / activation",
            "Navigation first · active fleet-collision replan",
            "Policy first · activation / active route",
            "Navigation first · active terrain-collision replan"
        };
        readonly MixedPrephaseWork work;
        readonly PracticeAttachmentPatrolTables tables;
        public static ReadOnlyCollection<string> Presets { get { return Array.AsReadOnly(names); } }
        public const int MaximumVisits = 8;
        public int WindowVisits { get { return MaximumVisits; } }
        public int Preset { get; private set; }
        public int Visits { get; private set; }
        public bool CanAdvance { get { return Visits < MaximumVisits; } }
        public int ParentFleet { get { return Preset==0 || Preset==4 ? 15 : 17; } }
        public int AircraftFleet { get { return ParentFleet==15 ? 23 : 27; } }
        public int SourceClock { get { return 12345 + Math.Max(0,Visits-1)*40; } }
        public int NextSourceClock { get { if(!CanAdvance)throw new InvalidOperationException("Compared visit window complete.");return 12345+Visits*40; } }
        public int CounterBeforeParent { get { return work.ParentCounterBefore; } }
        public int CounterAfterParent { get { return work.ParentCounterAfter; } }
        public int CallerWordAfter { get { return work.Counter; } }
        public int InitialCollisionAliasWrites { get { return work.AliasWrites; } }
        public int UiRequests { get { return work.Ui; } }
        public int SelectedWaypoint { get { return work.Waypoint; } }
        public int Activations { get { return work.Activations; } }
        public int TotalActivations { get; private set; }
        public string NavigationDescription { get { return Visits==0 ? "Prepared source input" : work.Navigation; } }
        public PracticeAiSharedBacking Backing { get { return work.Backing; } }
        public PracticeAiRouteGrid Grid { get { return work.Grid; } }

        PracticeMixedNavigationState(PracticeAttachmentPatrolTables tables,MixedPrephaseWork work,int preset,int visits,int total)
        { this.tables=tables;this.work=work;Preset=preset;Visits=visits;TotalActivations=total; }
        public static PracticeMixedNavigationState Create(string programs,PracticeMapStartup startup,int preset)
        {
            if(preset<0 || preset>=names.Length)throw new ArgumentOutOfRangeException("preset");
            var basis=PracticeAttachmentPatrolStudyBasis.Create(programs,startup);
            var input=basis.CreateFixture(15,false,0,40,0,false,0,false,false).ForWholePass(0);
            var fleets=input.CopyFleetPool();fleets[15*492+486]=2;
            var pools=new[]{input.CopyShipPool(),fleets,input.CopyPrimaryOrders(),input.Backing.CopyRegion(),input.CopyCandidateRegion()};
            return CreateForComparison(PracticeAttachmentPatrolTables.Load(programs),pools,preset);
        }
        internal static PracticeMixedNavigationState CreateForComparison(PracticeAttachmentPatrolTables tables,byte[][] basis,int preset)
        {
            if(tables==null)throw new ArgumentNullException("tables");
            if(preset<0 || preset>=names.Length)throw new ArgumentOutOfRangeException("preset");
            string[] expected={"F1C676D0082F18F385B016E60E137D4CDE2F8B2FEE0D522E10D53952026484B6","1F6B03A758FB8B14108CF298B20E2854475463932EF1C5077C8516716B5F822E","3332BE7B5EB6EBA46055A3E2725F902CA79258BE2B5B46FB6DC4B0420E2504B1","82F5B27A0565944362E1FD8E74FBB78CFFE56D86F49FE8B4496E06406CC8486B","1FD2EEB25D21856EB358CC57632432DF2CEBE81AA1240C5D0B83850902E7FC70"};
            if(basis==null || basis.Length!=5)throw new InvalidDataException("Mixed comparison requires five declared source input pools.");
            using(var hash=SHA256.Create())for(int n=0;n<5;n++)if(basis[n]==null || BitConverter.ToString(hash.ComputeHash(basis[n])).Replace("-","")!=expected[n])throw new InvalidDataException("Mixed navigation source input differs at pool "+n+".");
            return new PracticeMixedNavigationState(tables,MixedPrephaseWork.Initial(basis,preset),preset,0,0);
        }
        public PracticeMixedNavigationState Advance() { return AdvanceCore(false); }
        IPracticeMixedComparison IPracticeMixedComparison.AdvanceForStudy() { return Advance(); }
        internal static MixedPrephaseWork CreateMovingFixture(string programs,PracticeMapStartup startup,int preset)
        {
            if(preset<0 || preset>6)throw new ArgumentOutOfRangeException("preset");
            var basis=PracticeAttachmentPatrolStudyBasis.Create(programs,startup);
            var input=basis.CreateFixture(15,false,0,40,0,false,0,false,false).ForWholePass(0);
            var fleets=input.CopyFleetPool();fleets[15*492+486]=2;
            var pools=new[]{input.CopyShipPool(),fleets,input.CopyPrimaryOrders(),input.Backing.CopyRegion(),input.CopyCandidateRegion()};
            // Keep the same five complete input hashes as the eight-visit API.
            CreateForComparison(PracticeAttachmentPatrolTables.Load(programs),pools,preset==6?0:preset);
            return MixedPrephaseWork.Initial(pools,preset);
        }
        internal PracticeMixedNavigationState AdvanceIgnoringInitialAliasForComparison() { return AdvanceCore(true); }
        PracticeMixedNavigationState AdvanceCore(bool ignoreInitialAlias)
        {
            if(!CanAdvance)throw new InvalidOperationException("Eight compared visits are complete; reset the study.");
            var next=work.Advance(tables,NextSourceClock,ignoreInitialAlias);
            return new PracticeMixedNavigationState(tables,next,Preset,Visits+1,TotalActivations+next.Activations);
        }
        public byte[] CopyShipPool() { return (byte[])work.Ships.Clone(); }
        public byte[] CopyFleetPool() { return (byte[])work.Fleets.Clone(); }
        public byte[] CopyPrimaryOrders() { return (byte[])work.Orders.Clone(); }
        public byte[] CopyCandidateRegion() { return (byte[])work.Candidates.Clone(); }
        public byte[] CopyTerrainPool() { return (byte[])work.Terrain.Clone(); }
        internal byte[][] SnapshotForComparison()
        {
            // Every nonactor row remains the supplied CD diagnostic fill in
            // all48 complete original records. No live native heap is exposed.
            var rows=new byte[1024000];for(int n=0;n<rows.Length;n++)rows[n]=0xcd;
            return work.Pools(rows);
        }
    }
}
