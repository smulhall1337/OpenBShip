using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
namespace FleetLab
{
    // Pinned RIP B9A8...6A569:41BC50,41C590 selective task11/12 path,
    // 41B710/41B7A0 queues and429060/4297F0 yard routines. Copied BSSB202
    // seed731 profiles only. Display endpoints counted; original code never runs.
    // Native task pool:5AEE20,312-byte stride. The old movement auxiliary view
    // starts at5AEF00 (+224):its+50/+3C alias task+130/+11C. This isolated study
    // owns one record and deliberately does not compose movement or full AI.
    internal sealed class FleetServiceCore
    {
    byte[] ships,fleets,ammunition;
    int selectedFleet,selectedUnit,textRequests,mapRequests,refreshRequests;
    static int I(byte[] b,int p){return BitConverter.ToInt32(b,p);}
    static int W(byte[] b,int p){return BitConverter.ToInt16(b,p);}
    static void I(byte[] b,int p,int v){BitConverter.GetBytes(v).CopyTo(b,p);}
    static void W(byte[] b,int p,int v){BitConverter.GetBytes(unchecked((short)v)).CopyTo(b,p);}
    static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
    int Ref(int fleet,int member)
    {
        uint reference=BitConverter.ToUInt32(fleets,fleet*492+16+member*4);if(reference==0)return -1;
        long delta=(long)reference-0x4fc340;if(delta<0||delta>=402000||delta%268!=0)throw new InvalidDataException("Fleet service normalized reference");return (int)(delta/268);
    }
    void MaintainSelection()
    {
        if(I(fleets,selectedFleet*492)==0)throw new NotSupportedException("Fleet service inactive selected-fleet fallback not reached by admitted captures");
        if(selectedUnit>=0&&I(ships,selectedUnit*268)!=0&&W(ships,selectedUnit*268+12)==W(fleets,selectedFleet*492+4))return;
        for(int member=0;member<6;member++){int unit=Ref(selectedFleet,member);if(unit<0||I(ships,unit*268)==0)continue;selectedUnit=unit;fleets[selectedFleet*492+468]=(byte)member;return;}
        selectedUnit=Ref(selectedFleet,0);
    }
    void Refresh(){MaintainSelection();refreshRequests++;}
    void BlockStatus(int yard)
    {
        int first=yard&~3;bool live=false;
        for(int fleet=first;fleet<first+4;fleet++)for(int member=0;member<6;member++)
        {int unit=Ref(fleet,member);if(unit<0)continue;int kind=I(ships,unit*268);if(kind!=0&&kind!=22&&kind!=19&&kind!=23&&kind!=20)live=true;}
        int current=I(fleets,first*492+472);if(live&&current==4||!live&&current!=4){for(int fleet=first;fleet<first+4;fleet++)I(fleets,fleet*492+472,live?0:4);textRequests++;}
    }
    void Enter(int target)
    {
        var gate=PracticeYardAdmission.Check(ships,fleets,target,1);if(!gate.Available)throw new InvalidDataException("Fleet service entry gate rejected");
        int at=target*268,origin=W(ships,at+12),yard=gate.MarkerFleet,marker=gate.MarkerUnit,mk=marker*268,previous=gate.MarkerMember-1,sourceMember=-1;
        for(int member=0;member<6;member++)if(Ref(origin,member)==target){sourceMember=member;break;}
        if(previous<0||sourceMember<0||Ref(yard,previous)<0||I(ships,Ref(yard,previous)*268)!=0)throw new NotSupportedException("Fleet service unadmitted paired bay");
        int length=W(ships,at+14),x=W(ships,at+8),y=W(ships,at+10),mx=W(ships,mk+8),my=W(ships,mk+10);
        fleets[origin*492+490]=1;textRequests++;W(ships,mk+196,origin);W(ships,mk+198,sourceMember);W(ships,mk+200,x);W(ships,mk+202,y);
        for(int n=0;n<length;n++)
        {
            W(ships,mk+204+n*2,I(fleets,yard*492+64+(mx*10+my+n)*4));
            I(fleets,yard*492+64+(mx*10+my+n)*4,I(fleets,origin*492+64+(x*10+y+n)*4));I(fleets,origin*492+64+(x*10+y+n)*4,0);
        }
        I(fleets,yard*492+16+previous*4,0x4fc340+at);W(ships,at+12,yard);W(ships,at+8,mx);W(ships,at+10,my);I(fleets,origin*492+472,3);I(fleets,origin*492+16+sourceMember*4,0);
        int active=0;for(;active<6;active++){int unit=Ref(origin,active);if(unit>=0&&I(ships,unit*268)!=0)break;}
        if(active<6){fleets[origin*492+468]=(byte)active;if(selectedFleet==origin){selectedUnit=Ref(origin,active);Refresh();}}
        else{I(fleets,origin*492,0);selectedFleet=yard;Refresh();}
        MaintainSelection();mapRequests++;BlockStatus(yard);
    }
    int Command(int fleet)
    {
        int last=-1;
        for(int member=0;member<6;member++)
        {
            int unit=Ref(fleet,member);if(unit<0||I(ships,unit*268)==0)continue;int at=unit*268,length=W(ships,at+14),damage=0;
            if(length>0){int start=W(ships,at+8)*10+W(ships,at+10);if(start<0||start>100-length)throw new NotSupportedException("Fleet service reached invalid source grid");for(int n=0;n<length;n++){int cell=I(fleets,fleet*492+64+(start+n)*4);if(cell!=2)damage=unchecked(damage+cell-4);}}
            if(damage>0)last=member;
        }
        if(last>=0){int unit=Ref(fleet,last);if(PracticeYardAdmission.Check(ships,fleets,unit,1).Available)Enter(unit);return 0;}
        for(int member=0;member<6;member++)
        {
            int unit=Ref(fleet,member);if(unit<0||I(ships,unit*268)==0)continue;int at=unit*268,length=W(ships,at+14);
            for(int weapon=0;weapon<35;weapon++)
            {
                int maximum=W(ammunition,weapon*2);if(maximum<=0)continue;int occurrences=0;
                for(int n=0;n<length*4;n++)if(I(ships,at+100+n*4)==weapon)occurrences++;
                if(occurrences>0&&unchecked(maximum*occurrences)>W(ships,at+196+weapon*2)&&PracticeYardAdmission.Check(ships,fleets,unit,1).Available)Enter(unit);
            }
        }
        return I(fleets,fleet*492+472)==3?0:1;
    }
    void Release(int target,int marker,int yard,int member)
    {
        int at=target*268,mk=marker*268,origin=W(ships,mk+196),slot=W(ships,mk+198),x=W(ships,mk+200),y=W(ships,mk+202),length=W(ships,at+14),mx=W(ships,mk+8),my=W(ships,mk+10);
        if(I(fleets,origin*492)==0)I(fleets,origin*492,I(ships,at)==11||I(ships,at)==12?9:1);
        I(fleets,origin*492+16+slot*4,0x4fc340+at);W(ships,at+8,x);W(ships,at+10,y);fleets[origin*492+468]=(byte)slot;if(selectedFleet==origin)selectedUnit=target;W(ships,at+16,length);W(ships,at+12,origin);
        for(int n=0;n<length;n++){I(fleets,origin*492+64+(x*10+y+n)*4,2);I(fleets,yard*492+64+(mx*10+my+n)*4,W(ships,mk+204+n*2));}
        I(fleets,yard*492+16+member*4,0x4fc340+(W(ships,mk+4)-1)*268);
        bool other=false;int first=yard&~3;
        for(int fleet=first;fleet<first+4;fleet++)for(int m=1;m<6;m++){int candidate=Ref(fleet,m),previous=Ref(fleet,m-1);if(candidate>=0&&previous>=0&&(I(ships,candidate*268)==22||I(ships,candidate*268)==23)&&I(ships,previous*268)!=0&&W(ships,candidate*268+196)==origin)other=true;}
        if(!other){I(fleets,origin*492+472,0);if(selectedFleet==W(ships,mk+4))Refresh();}
        int active=0;for(;active<6;active++){int unit=Ref(yard,active);if(unit>=0&&I(ships,unit*268)!=0)break;}
        if(active<6){fleets[yard*492+468]=(byte)active;if(selectedFleet==yard)selectedUnit=Ref(yard,active);}
        Refresh();BlockStatus(yard);textRequests++;
    }
    void Worker()
    {
        for(int yard=0;yard<250;yard++)if(I(fleets,yard*492)==2)for(int member=0;member<6;member++)
        {
            int marker=Ref(yard,member);if(marker<0)continue;int type=I(ships,marker*268);if(type!=22&&type!=23)continue;if(member==0)throw new NotSupportedException("Fleet service marker-first worker");
            int unit=Ref(yard,member-1);if(unit<0||I(ships,unit*268)==0)continue;
            var repaired=PracticeInstallationRepair.StepOffscreen(ships,fleets,yard,unit);ships=repaired.CopyShipPool();fleets=repaired.CopyFleetPool();
            int depot=PracticeInstallationLookup.FindFirstInAlignedBlock(ships,fleets,yard,19),at=unit*268,length=W(ships,at+14);bool supplied=false;
            if(depot>=0)for(int row=0;row<length&&!supplied;row++)for(int column=0;column<4;column++)
            {
                int weapon=I(ships,at+100+(row*4+column)*4);if(weapon==0)continue;if(weapon<0||weapon>39)throw new NotSupportedException("Fleet service unsupported equipment");int occurrences=0;
                for(int n=0;n<length*4;n++)if(I(ships,at+100+n*4)==weapon)occurrences++;
                int maximum=W(ammunition,weapon*2)*occurrences,current=W(ships,at+196+weapon*2),available=W(ships,depot*268+196+weapon*2);if(maximum<=current||available<=0)continue;
                int sum=unchecked((short)(current+available));W(ships,at+196+weapon*2,sum);W(ships,depot*268+196+weapon*2,0);if(sum>maximum){W(ships,depot*268+196+weapon*2,sum-maximum);W(ships,at+196+weapon*2,maximum);}supplied=true;if(selectedFleet==yard)Refresh();break;
            }
            if(!supplied){Release(unit,marker,yard,member-1);if(selectedFleet==W(ships,at+12))Refresh();if(selectedFleet==yard)Refresh();}
        }
    }
    byte[] task;
    int selectedWaypoint,routeUiRequests;
    int Actor()
    {
        long delta=(long)BitConverter.ToUInt32(task,0xcc)-0x4b47a0;
        if(delta<0||delta>=123000||delta%492!=0)throw new InvalidDataException("Fleet service normalized task actor");return (int)(delta/492);
    }
    void PushTask(int kind)
    {
        int actor=Actor();
        if(I(task,0)==kind)
        {
            if(kind==10)W(fleets,actor*492+40,-1);I(task,0xc8,1);return;
        }
        Array.Copy(task,0,task,20,180);Array.Clear(task,0,20);I(task,0,kind);
        selectedWaypoint=-1;for(int n=0;n<5;n++)W(fleets,actor*492+40+n*2,-1);routeUiRequests++;I(task,0xc8,1);
    }
    void PopTask()
    {
        int actor=Actor();Array.Copy(task,20,task,0,180);Array.Clear(task,180,20);
        W(fleets,actor*492+40,-1);I(task,0x134,-1);I(task,0xc8,0);I(task,0x10,1);
    }
    void TaskVisit()
    {
        int kind=I(task,0),actor=Actor();
        if(kind!=0&&kind!=11&&kind!=12)throw new NotSupportedException("Fleet service only task kinds0/11/12 admitted");
        if(kind==11)
        {
            int collision=unchecked((sbyte)fleets[actor*492+482]);
            if(collision>=0&&I(fleets,collision*492)==2&&W(fleets,collision*492+6)==W(fleets,actor*492+6))PushTask(12);
        }
        if(I(task,0)==12&&Command(actor)!=0){PopTask();PopTask();}
        if(I(task,0)!=0&&I(task,0)!=11&&I(task,0)!=12)throw new NotSupportedException("Fleet service following task dependency outside profile");
    }

        string caseId;
        int initialTaskKind,taskVisits,workerPasses;
        int[] originalUnits;
        internal string CaseId { get { return caseId; } }
        internal int InitialTaskKind { get { return initialTaskKind; } }
        internal int TaskKind { get { return I(task,0); } }
        internal int SourceFleet { get { return Actor(); } }
        internal int SourceKind { get { return I(fleets,Actor()*492); } }
        internal int SourceState { get { return I(fleets,Actor()*492+472); } }
        internal int SelectionFleet { get { return selectedFleet; } }
        internal int SelectionUnit { get { return selectedUnit; } }
        internal int SelectionWaypoint { get { return selectedWaypoint; } }
        internal int TaskVisits { get { return taskVisits; } }
        internal int WorkerPasses { get { return workerPasses; } }
        internal int TextRequests { get { return textRequests; } }
        internal int MapRequests { get { return mapRequests; } }
        internal int RefreshRequests { get { return refreshRequests; } }
        internal int RouteUiRequests { get { return routeUiRequests; } }
        internal byte[] CopyShips(){return (byte[])ships.Clone();}
        internal byte[] CopyFleets(){return (byte[])fleets.Clone();}
        internal byte[] CopyTask(){return (byte[])task.Clone();}
        internal int[] CopyOriginalUnits(){return (int[])originalUnits.Clone();}
        internal static FleetServiceCore Create(string programs,PracticeMapStartup startup,string id,int initialKind)
        {
            if(startup==null)throw new ArgumentNullException("startup");
            if(startup.Seed!=731)throw new NotSupportedException("Fleet service currently admits the supplied BSSB202 seed731 startup only.");
            if(!PracticeFleetService.CaseIds.Contains(id)||(initialKind!=11&&initialKind!=12))throw new ArgumentException("Choose an admitted service case and initial task11 or12.");
            string[] parts=id.Split('-');int target=int.Parse(parts[0]),profile=int.Parse(parts[1]);string variant=parts[2];
            var state=new FleetServiceCore{ships=startup.Stages[3].CopyShipPool(),fleets=startup.Stages[3].CopyFleetPool(),caseId=id,initialTaskKind=initialKind};
            state.ammunition=File.ReadAllBytes(Path.Combine(programs,"UnitAmmunition.seqbin"));
            if(state.ammunition.Length!=80||Hash(state.ammunition)!="70F5B7233E5FE38F5418FEBC502B4C1FD25020D8CACE907E4EB1BA1F7D5A6322")throw new InvalidDataException("Fleet service requires the pinned ammunition table.");
            int source=W(state.ships,target*268+12);var originals=new List<int>();for(int n=0;n<6;n++){int unit=state.Ref(source,n);if(unit>=0&&I(state.ships,unit*268)!=0)originals.Add(unit);}state.originalUnits=originals.ToArray();
            state.fleets[source*492+482]=variant=="blocked"?(byte)255:(byte)3;state.PrepareTarget(target,profile);
            if(variant=="bothdamaged"||variant=="firstworse")state.PrepareTarget(31,1);
            if(variant=="bothammo")state.PrepareTarget(31,3);
            if(variant=="hpzero")W(state.ships,target*268+16,0);
            if(variant=="state3")I(state.fleets,source*492+472,3);
            state.selectedFleet=source;state.selectedUnit=target;state.selectedWaypoint=3;state.task=new byte[312];
            // Tagged queue data is a deliberate diagnostic fixture. Other task
            // kinds remain zero; opaque tail tags test preservation, not meaning.
            for(int record=0;record<10;record++)for(int field=1;field<5;field++)I(state.task,record*20+field*4,1000+record*10+field);
            I(state.task,0,initialKind);I(state.task,0xc8,0x11223344);I(state.task,0xcc,0x4b47a0+source*492);
            for(int n=0xd0;n<312;n++)state.task[n]=(byte)(n*7+3);
            return state;
        }
        void PrepareTarget(int target,int profile)
        {
            if(profile==0)return;
            int at=target*268,parent=W(ships,at+12),length=W(ships,at+14),x=W(ships,at+8),y=W(ships,at+10);
            if(profile<=2)
            {
                W(ships,at+16,profile==1?length-1:0);
                for(int n=0;n<length;n++)I(fleets,parent*492+64+(x*10+y+n)*4,profile==1?(n==1?8:2):new[]{5,6,7,8,8,5}[n]);
            }
            for(int n=0;n<35;n++)W(ships,at+196+n*2,0);
        }
        FleetServiceCore Copy()
        {
            return new FleetServiceCore
            {
                ships=(byte[])ships.Clone(),fleets=(byte[])fleets.Clone(),task=(byte[])task.Clone(),ammunition=ammunition,
                selectedFleet=selectedFleet,selectedUnit=selectedUnit,selectedWaypoint=selectedWaypoint,
                textRequests=textRequests,mapRequests=mapRequests,refreshRequests=refreshRequests,routeUiRequests=routeUiRequests,
                caseId=caseId,initialTaskKind=initialTaskKind,taskVisits=taskVisits,workerPasses=workerPasses,originalUnits=(int[])originalUnits.Clone()
            };
        }
        internal FleetServiceCore VisitInitialTask()
        {
            if(taskVisits!=0)throw new InvalidOperationException("The initial task was already visited; use worker + task updates or reset.");
            var next=Copy();next.TaskVisit();next.taskVisits++;return next;
        }
        internal FleetServiceCore Step()
        {
            if(taskVisits==0)throw new InvalidOperationException("Visit the initial task before stepping its worker.");
            if(workerPasses>=20)throw new InvalidOperationException("This copied service history is bounded to twenty worker/task updates; reset to continue.");
            var next=Copy();next.Worker();next.workerPasses++;next.TaskVisit();next.taskVisits++;return next;
        }
    }
    // Immutable public study profile. Only the recorded map/cases, tagged task
    // kinds11/12 with zero following kinds, and twenty-update prefixes are
    // admitted. Pools retain normalized source references for comparison.
    public sealed class PracticeFleetService
    {
        readonly FleetServiceCore core;
        static readonly ReadOnlyCollection<string> ids=Array.AsReadOnly(new[]{
            "30-0-ordinary","30-1-ordinary","30-2-ordinary","30-3-ordinary",
            "31-0-ordinary","31-1-ordinary","31-2-ordinary","31-3-ordinary",
            "34-0-ordinary","34-1-ordinary","34-2-ordinary","34-3-ordinary",
            "30-1-blocked","30-3-blocked","30-1-bothdamaged","30-3-bothammo","30-0-hpzero","30-0-state3","30-2-firstworse"});
        public static ReadOnlyCollection<string> CaseIds { get { return ids; } }
        public string CaseId { get { return core.CaseId; } }
        public int InitialTaskKind { get { return core.InitialTaskKind; } }
        public int TaskKind { get { return core.TaskKind; } }
        public bool QueueEmpty { get { return TaskKind==0; } }
        public int SourceFleet { get { return core.SourceFleet; } }
        public int SourceKind { get { return core.SourceKind; } }
        public int SourceState { get { return core.SourceState; } }
        public int SelectedFleet { get { return core.SelectionFleet; } }
        public int SelectedUnit { get { return core.SelectionUnit; } }
        public int SelectedWaypoint { get { return core.SelectionWaypoint; } }
        public int TaskVisits { get { return core.TaskVisits; } }
        public int WorkerPasses { get { return core.WorkerPasses; } }
        public int TextRequests { get { return core.TextRequests; } }
        public int MapRequests { get { return core.MapRequests; } }
        public int RefreshRequests { get { return core.RefreshRequests; } }
        public int RouteUiRequests { get { return core.RouteUiRequests; } }
        PracticeFleetService(FleetServiceCore core){this.core=core;}
        public static PracticeFleetService Create(string programs,PracticeMapStartup startup,string caseId,int initialTaskKind)
        {return new PracticeFleetService(FleetServiceCore.Create(programs,startup,caseId,initialTaskKind));}
        public byte[] CopyShipPool(){return core.CopyShips();}
        public byte[] CopyFleetPool(){return core.CopyFleets();}
        public byte[] CopyTaskRecord(){return core.CopyTask();}
        public int[] CopyOriginalUnits(){return core.CopyOriginalUnits();}
        public PracticeFleetService VisitTask(){return new PracticeFleetService(core.VisitInitialTask());}
        public PracticeFleetService StepWorkerAndTask(){return new PracticeFleetService(core.Step());}
        public PracticeFleetService Advance(int count)
        {
            if(count<0||count>20-WorkerPasses)throw new ArgumentOutOfRangeException("count","Choose a count inside the twenty-update study window.");
            if(count>0&&TaskVisits==0)throw new InvalidOperationException("Visit the initial task first.");
            var state=this;for(int n=0;n<count;n++)state=state.StepWorkerAndTask();return state;
        }
        public static string DescribeCase(string id)
        {
            if(!ids.Contains(id))throw new ArgumentException("Unknown service case.");
            var parts=id.Split('-');string variant=parts[2];
            if(variant=="bothdamaged")return "Fleet 16 · two damaged ships";
            if(variant=="bothammo")return "Fleet 16 · two empty-ammo ships";
            if(variant=="firstworse")return "Fleet 16 · worse damage first";
            if(variant=="hpzero")return "Unit 30 · HP 0, intact grid";
            if(variant=="state3")return "Fleet 16 · state 3 without bay work";
            string profile=new[]{"intact","one state 8 / empty ammo","mixed damage / empty ammo","intact / empty ammo"}[int.Parse(parts[1])];
            return "Unit "+parts[0]+" · "+profile+(variant=="blocked"?" / no contact":"");
        }
    }
}
