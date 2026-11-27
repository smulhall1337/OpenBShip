using System;using System.IO;using System.Collections.Generic;using System.Collections.ObjectModel;using System.Security.Cryptography;
namespace FleetLab
{
    public sealed class PracticeGoalRouteStudyBasis
    {
        readonly byte[] fleets,region;
        static readonly string[] names={"Open water · one cost search","Corner-to-corner · gapped barrier","Encircled destination · failed search / retry","Future cooldown · grid clears first","Destination yard quartet · source loop order","Nearby friendly · rounded corners and waypoint","Flagged enemy · nearby rounded corners","Opposite corners · source scan bounds","At destination · waypoint callback pops task"};
        static readonly int[][] presets={new[]{0,0,0},new[]{2,7,0},new[]{0,8,0},new[]{0,8,1},new[]{1,3,0},new[]{0,4,0},new[]{0,5,0},new[]{3,0,0},new[]{4,0,0}};
        internal static readonly int[][] Points={new[]{100,100,130,120},new[]{130,120,100,100},new[]{0,0,630,630},new[]{630,0,0,630},new[]{320,320,320,320},new[]{9,19,29,39}};
        public static ReadOnlyCollection<string> Presets { get { return Array.AsReadOnly(names); } }
        static void I(byte[] b,int at,int value){BitConverter.GetBytes(value).CopyTo(b,at);}static void W(byte[] b,int at,int value){BitConverter.GetBytes(unchecked((short)value)).CopyTo(b,at);}
        static void Fleet(byte[] f,int slot,int kind,int owner,int x,int y){I(f,slot*492,kind);W(f,slot*492+6,owner);W(f,slot*492+12,x);W(f,slot*492+14,y);}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        PracticeGoalRouteStudyBasis(byte[] f,byte[] r){fleets=(byte[])f.Clone();region=(byte[])r.Clone();}
        public static PracticeGoalRouteStudyBasis Create(string programs,PracticeMapStartup startup)
        {
            if(startup==null)throw new ArgumentNullException("startup");if(startup.Seed!=731)throw new NotSupportedException("Route grid study requires the compared BSSB202 seed731 copy.");
            var service=PracticeFleetService.Create(programs,startup,"30-1-blocked",11,PracticeFleetServiceMode.InitializedDispatcher);var f=service.CopyFleetPool();
            if(Hash(f)!="CE199D97FB5F582F1569C1B6ED040A4BBB6BF54B74A04769FEB229E83DD1B8C4")throw new InvalidDataException("Route basis differs from original496 input.");
            for(int g=0;g<250;g++)I(f,g*492,0);var r=new byte[78560];for(int g=0;g<250;g++)PracticeFleetServiceContext.CreateDiagnostic(g).CopyTo(r,g*312);for(int n=78000;n<78016;n++)r[n]=0xa7;
            return new PracticeGoalRouteStudyBasis(f,r);
        }
        public PracticeGoalRouteStudyState CreateFixture(int preset)
        {
            if(preset<0||preset>=presets.Length)throw new ArgumentOutOfRangeException("preset");var spec=presets[preset];var near=new byte[9];for(int n=0;n<9;n++)near[n]=0x6d;
            return CreateComparison(1,spec[0],spec[1],spec[2],spec[1]%2==0?0:2,12345,spec[2]==1?12346:12345,(byte)(spec[2]==0?0:1),near,preset);
        }
        // Independent recorded-input builder shared by public captured-output
        // checks and schematic study presets. No expected output is computed.
        internal PracticeGoalRouteStudyState CreateComparison(int kind,int point,int profile,int retry,int policy,int clock,int deadline,byte flag,byte[] near,int preset)
        {
            if(point<0||point>=Points.Length||profile<0||profile>=10||retry<0||retry>4||near==null||near.Length!=9)throw new ArgumentException("Invalid route fixture fields.");
            var f=(byte[])fleets.Clone();int[] xy=Points[point];Fleet(f,16,kind,1,xy[0],xy[1]);f[16*492+467]=0;for(int n=0;n<5;n++){W(f,16*492+40+n*2,-1);W(f,16*492+50+n*2,200+n*10);}var secondary=new byte[246000];
            if(profile==1)Fleet(f,20,11,0,110,100);
            if(profile==2)for(int n=0;n<4;n++)Fleet(f,4+n,n==0?2:0,0,100+(n%2)*10,100+(n/2)*10);
            if(profile==3){int tx=xy[2]/10*10,ty=xy[3]/10*10;for(int n=0;n<4;n++)Fleet(f,4+n,n==0?2:0,0,Math.Min(630,tx+(n%2)*10),Math.Min(630,ty+(n/2)*10));Fleet(f,8,11,0,110,110);}
            if(profile==4){Fleet(f,20,1,1,109,109);f[20*492+467]=0;W(f,20*492+40,120);W(f,20*492+50,100);}
            if(profile==5){Fleet(f,20,9,0,109,109);f[20*492+467]=1;}
            if(profile==6){Fleet(f,20,9,0,109,109);f[20*492+467]=0;}
            if(profile==7)for(int n=0;n<64;n++)if(n!=32)Fleet(f,30+n,11,0,300,n*10);
            if(profile==8){int tx=xy[2]/10,ty=xy[3]/10,n=30;foreach(var d in new[]{new[]{-1,0},new[]{1,0},new[]{0,-1},new[]{0,1}}){int x=tx+d[0],y=ty+d[1];if(x>=0&&x<64&&y>=0&&y<64)Fleet(f,n++,11,0,x*10,y*10);}}
            if(profile==9)Fleet(secondary,0,1,0,100,100);if(retry==4)Fleet(secondary,0,1,0,xy[0],xy[1]);
            var r=(byte[])region.Clone();int at=16*312;Array.Copy(r,at,r,at+20,180);Array.Clear(r,at,20);I(r,at,11);I(r,at+200,2);I(r,at+304,1);I(r,at+296,xy[2]);I(r,at+300,xy[3]);I(r,at+4,deadline);r[at+284]=flag;near.CopyTo(r,at+285);
            return new PracticeGoalRouteStudyState(f,secondary,new PracticeAiSharedBacking(r),PracticeAiRouteGrid.Diagnostic(16,0xcd),policy,clock,preset,null,0,null,point,profile);
        }
    }
    public sealed class PracticeGoalRouteStudyState
    {
        readonly byte[] fleets,initialSecondary,currentSecondary;readonly PracticeAiSharedBacking initialBacking;readonly PracticeAiRouteGrid initialGrid;readonly int initialClock;
        public int Preset {get;private set;}public int SourceClock {get;private set;}public int OwnerPolicy {get;private set;}public int Visits {get;private set;}
        public PracticeGoalRouteResult Result {get;private set;}
        public PracticeGoalWaypointResult WaypointResult {get;private set;}
        public PracticeAiSharedBacking WaypointInput {get;private set;}
        public PracticeActiveNavigationResult NavigationResult {get;private set;}
        public PracticeActiveNavigationInput NavigationInput {get;private set;}
        public int NavigationMode {get;private set;}
        internal int ComparisonPoint {get;private set;}internal int ComparisonProfile {get;private set;}
        public PracticeAiSharedBacking Backing {get{return NavigationResult!=null?NavigationResult.Backing:WaypointResult!=null?WaypointResult.Backing:Result==null?initialBacking:Result.Backing;}}
        public PracticeAiRouteGrid Grid {get{return NavigationResult!=null?NavigationResult.Grid:WaypointResult!=null?WaypointResult.Grid:Result==null?initialGrid:Result.Grid;}}
        public bool HasGridOutput {get{return NavigationResult!=null?NavigationResult.GridFunctionVisits>0:WaypointResult!=null?WaypointResult.Route!=null:Result!=null;}}
        public bool CanApplyNavigation {get{return Visits==0&&(ComparisonProfile==0||ComparisonProfile==3||ComparisonProfile==8)&&(BitConverter.ToInt32(fleets,16*492)==1||BitConverter.ToInt32(fleets,16*492)==9);}}
        public bool CanRetry {get{return WaypointResult==null&&Visits==1&&Result!=null&&Result.Outcome==PracticeGoalRouteOutcome.RouteUnavailable;}}
        public int OriginX {get{return BitConverter.ToInt16(fleets,16*492+12);}}public int OriginY {get{return BitConverter.ToInt16(fleets,16*492+14);}}
        public int TargetX {get{return BitConverter.ToInt32(Backing.CopyTaskRecord(16),296);}}public int TargetY {get{return BitConverter.ToInt32(Backing.CopyTaskRecord(16),300);}}
        internal PracticeGoalRouteStudyState(byte[] f,byte[] secondary,PracticeAiSharedBacking backing,PracticeAiRouteGrid grid,int policy,int clock,int preset,PracticeGoalRouteResult result,int visits,PracticeGoalRouteStudyState original,int point=-1,int profile=-1)
        {
            fleets=(byte[])f.Clone();currentSecondary=(byte[])secondary.Clone();initialSecondary=original==null?(byte[])secondary.Clone():original.initialSecondary;initialBacking=original==null?backing:original.initialBacking;initialGrid=original==null?grid:original.initialGrid;initialClock=original==null?clock:original.initialClock;
            OwnerPolicy=policy;SourceClock=clock;Preset=preset;Result=result;Visits=visits;
            ComparisonPoint=original==null?point:original.ComparisonPoint;ComparisonProfile=original==null?profile:original.ComparisonProfile;NavigationMode=-1;
        }
        public byte[] CopyFleetPool(){return NavigationResult!=null?NavigationResult.CopyFleetPool():WaypointResult==null?(byte[])fleets.Clone():WaypointResult.CopyFleetPool();}public byte[] CopySecondaryFleetPool(){return (byte[])currentSecondary.Clone();}
        public PracticeGoalRouteStudyState Build()
        {
            if(Visits!=0)throw new InvalidOperationException("One copied route visit is complete;reset or use the explicit failed-search retry.");
            var result=PracticeGoalRoute.Build(Grid,fleets,currentSecondary,Backing,SourceClock,32000,OwnerPolicy);
            return new PracticeGoalRouteStudyState(fleets,currentSecondary,initialBacking,initialGrid,OwnerPolicy,SourceClock,Preset,result,1,this);
        }
        public PracticeGoalRouteStudyState RetryAtDeadline(bool addBlocker)
        {
            if(!CanRetry)throw new InvalidOperationException("Only one retry after the first failed search is admitted by this study.");
            int clock=BitConverter.ToInt32(Backing.CopyTaskRecord(16),4);var secondary=(byte[])currentSecondary.Clone();
            if(addBlocker){BitConverter.GetBytes(1).CopyTo(secondary,0);BitConverter.GetBytes((short)OriginX).CopyTo(secondary,12);BitConverter.GetBytes((short)OriginY).CopyTo(secondary,14);}
            var result=PracticeGoalRoute.Build(Grid,fleets,secondary,Backing,clock,32000,OwnerPolicy);
            return new PracticeGoalRouteStudyState(fleets,secondary,initialBacking,initialGrid,OwnerPolicy,clock,Preset,result,2,this);
        }
        public PracticeGoalRouteStudyState QueueWaypoint()
        {
            if(Visits!=0)throw new InvalidOperationException("Reset before applying a fresh waypoint callback input.");
            var task=initialBacking.CopyTaskRecord(16);BitConverter.GetBytes(4).CopyTo(task,200);BitConverter.GetBytes(TargetX).CopyTo(task,8);BitConverter.GetBytes(TargetY).CopyTo(task,12);
            var input=initialBacking.WithTaskRecord(16,task);
            var waypoint=PracticeGoalWaypoint.Evaluate(initialGrid,fleets,currentSecondary,input,SourceClock,32000,OwnerPolicy);
            var next=new PracticeGoalRouteStudyState(fleets,currentSecondary,initialBacking,initialGrid,OwnerPolicy,SourceClock,Preset,null,1,this);
            next.WaypointInput=input;next.WaypointResult=waypoint;return next;
        }
        internal PracticeActiveNavigationInput CreateNavigationInput(int mode)
        {
            if(!CanApplyNavigation||mode<0||mode>2)throw new NotSupportedException("Choose one supported fresh navigation fixture and wait/replan/arrival mode.");
            var f=(byte[])fleets.Clone();
            for(int slot=0;slot<250;slot++){int at=slot*492;BitConverter.GetBytes(0).CopyTo(f,at+472);f[at+486]=0;f[at+482]=255;BitConverter.GetBytes((short)-1).CopyTo(f,at+40);}
            if(mode==2){BitConverter.GetBytes((short)OriginX).CopyTo(f,16*492+40);BitConverter.GetBytes((short)OriginY).CopyTo(f,16*492+50);}
            f[16*492+482]=(byte)(mode==1?254:255);
            var task=initialBacking.CopyTaskRecord(16);BitConverter.GetBytes(4).CopyTo(task,200);BitConverter.GetBytes(TargetX).CopyTo(task,8);BitConverter.GetBytes(TargetY).CopyTo(task,12);
            return new PracticeActiveNavigationInput(f,currentSecondary,initialBacking.WithTaskRecord(16,task),initialGrid);
        }
        public PracticeGoalRouteStudyState PollNavigation(int mode)
        {
            var input=CreateNavigationInput(mode);
            var result=PracticeActiveNavigation.EvaluateOffline(input.Grid,input.CopyFleetPool(),input.CopySecondaryFleetPool(),input.Backing,SourceClock,32000,OwnerPolicy);
            var next=new PracticeGoalRouteStudyState(fleets,currentSecondary,initialBacking,initialGrid,OwnerPolicy,SourceClock,Preset,null,1,this);
            next.NavigationInput=input;next.NavigationResult=result;next.WaypointResult=result.Waypoint;next.NavigationMode=mode;return next;
        }
        public PracticeGoalRouteStudyState Reset(){return new PracticeGoalRouteStudyState(fleets,initialSecondary,initialBacking,initialGrid,OwnerPolicy,initialClock,Preset,null,0,null,ComparisonPoint,ComparisonProfile);}
    }
}
