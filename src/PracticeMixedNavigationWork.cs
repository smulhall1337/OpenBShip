using System;using System.IO;
namespace FleetLab
{
// Source996r3 comparison: fixed six profiles, active navigation and flag23 in
// source slot order. Kept separate from the older restricted APIs.
sealed class MixedPrephaseWork
{
    internal byte[] Ships,Fleets,Orders,Candidates,Terrain;
    internal PracticeAiSharedBacking Backing;
    internal PracticeAiRouteGrid Grid;
    internal int Counter,Ui,Waypoint=4,ParentCounterBefore,ParentCounterAfter,Activations,NavVisits,AliasWrites;
    internal string Navigation;
    static int I(byte[] b,int at){return BitConverter.ToInt32(b,at);}
    static int W(byte[] b,int at){return BitConverter.ToInt16(b,at);}
    static void I(byte[] b,int at,int v){BitConverter.GetBytes(v).CopyTo(b,at);}
    static void W(byte[] b,int at,int v){BitConverter.GetBytes(unchecked((short)v)).CopyTo(b,at);}
    internal static MixedPrephaseWork Initial(byte[][] basis,int history)
    {
        int parent=history==0||history==4?15:17,child=parent==15?23:27,at=16*492,ta=16*312;
        var f=(byte[])basis[1].Clone();var r=(byte[])basis[3].Clone();
        for(int g=0;g<250;g++){f[g*492+486]=0;I(r,g*312+208,unchecked((int)(0x70000010u+(uint)(g*6432))));}
        for(int n=78000;n<78016;n++)r[n]=0xa7;
        I(f,at,1);W(f,at+6,1);W(f,at+12,100);W(f,at+14,100);W(f,at+60,7);f[at+485]=3;f[at+482]=254;I(f,at+476,0);
        for(int n=0;n<5;n++){W(f,at+40+n*2,n==0?160:-1);W(f,at+50+n*2,n==0?100:300+n*10);}
        Array.Copy(r,ta,r,ta+20,180);Array.Clear(r,ta,20);I(r,ta,11);I(r,ta+200,2);I(r,ta+304,0);I(r,ta+296,600);I(r,ta+300,610);I(r,ta+8,160);I(r,ta+12,100);I(r,ta+4,12345);r[ta+284]=0;
        var s=new MixedPrephaseWork{Ships=(byte[])basis[0].Clone(),Fleets=f,Orders=(byte[])basis[2].Clone(),Candidates=(byte[])basis[4].Clone(),Backing=new PracticeAiSharedBacking(r),Grid=PracticeAiRouteGrid.Diagnostic(16,0xcd),Terrain=new byte[246000]};
        if(history!=1&&history!=2)s.NavigationStep(12345,false);
        s.Fleets[parent*492+486]=2;r=s.Backing.CopyRegion();I(r,child*312+252,history==1||history==2||history==4?4960:0);s.Backing=new PracticeAiSharedBacking(r);
        if(history==1||history==3){int b=30*492;I(s.Fleets,b,11);W(s.Fleets,b+6,0);W(s.Fleets,b+12,history==1?160:130);W(s.Fleets,b+14,100);I(s.Fleets,b+476,0);s.Fleets[b+485]=0;s.Fleets[at+482]=30;}
        if(history==5){I(s.Terrain,0,1);W(s.Terrain,12,110);W(s.Terrain,14,100);s.Fleets[at+482]=254;}
        s.Counter=s.Ui=s.Activations=s.NavVisits=s.AliasWrites=0;return s;
    }
    MixedPrephaseWork Clone()
    {
        return new MixedPrephaseWork{Ships=(byte[])Ships.Clone(),Fleets=(byte[])Fleets.Clone(),Orders=(byte[])Orders.Clone(),Candidates=(byte[])Candidates.Clone(),Backing=Backing,Grid=Grid,Terrain=(byte[])Terrain.Clone(),Waypoint=Waypoint};
    }
    internal MixedPrephaseWork Advance(PracticeAttachmentPatrolTables tables,int clock,bool ignoreInitialCollisionAlias)
    {
        var s=Clone();s.Counter=0;
        for(int g=0;g<250;g++)
        {
            int at=g*492,flag=s.Fleets[at+486];var task=s.Backing.CopyTaskRecord(g);
            if(s.Fleets[at+467]!=0||I(task,204)!=0x4b47a0+at||I(s.Fleets,at+472)!=0)throw new NotSupportedException("997 no-target/state0/identity subset");
            if(g!=16&&(I(task,304)!=0||s.Fleets[at+482]!=255))throw new NotSupportedException("997 onlyactor16navigation");
            if(flag==2||flag==3)
            {
                if(g!=15&&g!=17)throw new NotSupportedException("997 suppliedparent15/17only");
                s.ParentCounterBefore=s.Counter;var p=PracticeFlag23Patrol.EvaluateNoTargets(tables,s.Ships,s.Fleets,s.Orders,s.Backing,s.Candidates,g,40,s.Counter,s.Waypoint);
                s.Ships=p.CopyShipPool();s.Fleets=p.CopyFleetPool();s.Orders=p.CopyPrimaryOrders();s.Candidates=p.CopyCandidateRegion();s.Backing=p.Backing;s.Counter=p.CounterWordAfter;s.ParentCounterAfter=s.Counter;s.Ui+=p.RouteUiRequests;s.Waypoint=p.SelectedWaypoint;s.Activations+=p.Activations;
            }
            else if(flag==1)
            {
                int count=0;if(I(s.Fleets,at)!=3&&I(s.Fleets,at)!=4)throw new NotSupportedException("997 airflag1only");for(int n=0;n<6;n++){uint p=BitConverter.ToUInt32(s.Fleets,at+16+n*4);if(p==0)continue;long off=(long)p-0x4fc340;if(off<0||off>=402000||off%268!=0||I(s.Ships,(int)off)<8||I(s.Ships,(int)off)>10)throw new NotSupportedException("997 noflag1carrier");count++;}if(count==0)throw new NotSupportedException("997 realairmembers");s.Counter=6;
            }
            if(g==16)s.NavigationStep(clock,ignoreInitialCollisionAlias);else if(PracticeNavigationGate.Evaluate(s.Fleets,s.Backing,g).Outcome!=PracticeNavigationGateOutcome.NoCollisionTarget)throw new NotSupportedException("997 othernavigationfastreturn");
        }
        return s;
    }
    void WaypointStep(int clock,bool collision)
    {
        var result=collision?PracticeGoalWaypoint.EvaluateAfterCollision(Grid,Fleets,Terrain,Backing,clock,32000,0):PracticeGoalWaypoint.EvaluateAfterQueuedNavigation(Grid,Fleets,Terrain,Backing,clock,32000,0);Fleets=result.CopyFleetPool();Backing=result.Backing;Grid=result.Grid;
    }
    void NavigationStep(int clock,bool ignoreInitialCollisionAlias)
    {
        int at=16*492;var task=Backing.CopyTaskRecord(16);int nav=I(task,304),collision=Fleets[at+482],x=W(Fleets,at+12),y=W(Fleets,at+14);NavVisits++;
        if(nav==0)
        {
            if(collision==255){Navigation="No collision target";return;}
            if(W(Fleets,at+40)==-1)throw new NotSupportedException("997 queuedstartrequirespoint");I(task,304,1);Backing=Backing.WithTaskRecord(16,task);
            if(collision!=254)
            {
                if(collision!=30||I(Fleets,30*492)!=11)throw new NotSupportedException("997 initialpositivecollisionkind11only");
                if(!ignoreInitialCollisionAlias){Counter=11;AliasWrites++;}
                if(W(Fleets,at+40)==W(Fleets,30*492+12)&&W(Fleets,at+50)==W(Fleets,30*492+14)){I(task,304,0);Backing=Backing.WithTaskRecord(16,task);Navigation="Blocked destination early return";return;}
                throw new NotSupportedException("997 positivecollisionnotatdestination");
            }
            for(int n=4;n>0;n--){W(Fleets,at+40+n*2,W(Fleets,at+40+(n-1)*2));W(Fleets,at+50+n*2,W(Fleets,at+50+(n-1)*2));}
            I(task,296,W(Fleets,at+40));I(task,300,W(Fleets,at+50));Backing=Backing.WithTaskRecord(16,task);
            var route=PracticeGoalRoute.BuildForQueuedNavigation(Grid,Fleets,Terrain,Backing,clock,32000,0);Backing=route.Backing;Grid=route.Grid;W(Fleets,at+40,x/10*10);W(Fleets,at+50,y/10*10);Fleets[at+482]=255;Navigation="Started";return;
        }
        if(nav!=1)throw new NotSupportedException("997 nav0/1only");
        if(I(task,296)==x&&I(task,300)==y){I(task,304,0);Backing=Backing.WithTaskRecord(16,task);Navigation="Completed goal";return;}
        if(collision!=255)
        {
            if(collision!=254&&collision!=30)throw new NotSupportedException("997 activecollisionFE/30only");var route=PracticeGoalRoute.BuildForQueuedNavigation(Grid,Fleets,Terrain,Backing,clock,32000,0);Backing=route.Backing;Grid=route.Grid;WaypointStep(clock,true);Fleets[at+482]=255;
            if(W(Fleets,at+40)==x&&W(Fleets,at+50)==y)WaypointStep(clock,true);Navigation="Replanned";return;
        }
        if(W(Fleets,at+40)==x&&W(Fleets,at+50)==y){WaypointStep(clock,false);Navigation="Reached waypoint";return;}
        Navigation="Waiting";
    }
    internal byte[][] Pools(byte[] originalRows)
    {
        var rows=(byte[])originalRows.Clone();Grid.CopyCells().CopyTo(rows,16*4096);return new[]{Ships,Fleets,Orders,Backing.CopyRegion(),Candidates,rows};
    }
}

}
