namespace FleetLab {
using System;
using System.IO;
using System.Collections.Generic;

using FleetLab;
internal sealed class FleetMemberCellRequests
{
    byte[] ships,fleets,orders;int weapon,randomCalls,sourceUnit=30,targetFleet=14;uint random;bool originalEquipment;
    static int I(byte[] b,int at){return BitConverter.ToInt32(b,at);}static int W(byte[] b,int at){return BitConverter.ToInt16(b,at);}static void I(byte[] b,int at,int v){BitConverter.GetBytes(v).CopyTo(b,at);}static void W(byte[] b,int at,int v){BitConverter.GetBytes((short)v).CopyTo(b,at);}
    static void Need(bool ok,string text){if(!ok)throw new InvalidDataException(text);}
    int Random10(){if(++randomCalls>100000)throw new InvalidDataException("Cell selector unsupported random-search budget");random=unchecked(random*214013u+2531011u);return (int)((random>>16)&32767)%10;}
    int Cell(int x,int y){return I(fleets,targetFleet*492+64+(x*10+y)*4);}
    int Fire(int x,int y)
    {
        if(W(ships,sourceUnit*268+196+weapon*2)==0)return 1;
        var result=PracticeFireQueue.Enqueue(ships,fleets,orders,sourceUnit,weapon,targetFleet,x,y);orders=result.CopyOrderPool();return weapon==19||originalEquipment&&weapon==22?1:result.ReturnCode;
    }
    bool Neighbors(int[] grid,int x,int y,bool includeThree)
    {
        int at=x*10+y;
        if(y>0&&(grid[at-1]==0||includeThree&&grid[at-1]==3)){if(Fire(x,y-1)!=0)return true;grid[at-1]=8;}
        if(y<9&&(grid[at+1]==0||includeThree&&grid[at+1]==3)){if(Fire(x,y+1)!=0)return true;grid[at+1]=8;}
        return false;
    }
    void PartialScan(bool midpoints)
    {
        var grid=new int[100];
        for(int x=0;x<10;x++)for(int y=0;y<10;y++)
        {
            int value=Cell(x,y),at=x*10+y;
            if(!midpoints)grid[at]=value==0||value==2||value==4?0:value;
            else if(value!=2)grid[at]=value;
            else
            {
                grid[at]=0;
                for(int member=0;member<6;member++)
                {
                    uint p=BitConverter.ToUInt32(fleets,targetFleet*492+16+member*4);if(p==0)continue;long offset=(long)p-0x4fc340;Need(offset>=0&&offset<402000&&offset%268==0,"Cell selector target roster");int unit=(int)(offset/268);
                    if(I(ships,unit*268)==0||W(ships,unit*268+8)!=x)continue;
                    // Source resets on each matching-row unit: later roster
                    // entries can overwrite an earlier midpoint match.
                    grid[at]=0;if(W(ships,unit*268+10)+W(ships,unit*268+14)/2==y)grid[at]=2;
                }
            }
        }
        // Original selectors traverse columns first; X selects the source row.
        for(int y=0;y<10;y++)for(int x=0;x<10;x++)
        {
            int at=x*10+y,value=grid[at];
            if(value==2||value==4||value==5||value==6||value==7)
            {if(Fire(x,y)!=0)return;grid[at]=8;if(Neighbors(grid,x,y,!midpoints))return;}
            else if(value==8&&Neighbors(grid,x,y,!midpoints))return;
        }
        if(midpoints)return;
        Need(Array.IndexOf(grid,0)>=0,"Cell selector no-zero random-search input is unsupported");
        for(;;){int x=Random10(),y=Random10();if(grid[x*10+y]!=0)continue;if(Fire(x,y)!=0)return;}
    }
    void FullScan()
    {
        for(int y=0;y<10;y++)for(int x=0;x<10;x++)if(Cell(x,y)==2||Cell(x,y)==4)if(Fire(x,y)!=0)return;
        // Fallback visits rows first and retains duplicates from the first pass.
        for(int x=0;x<10;x++)for(int y=0;y<10;y++){int value=Cell(x,y);if(value==2||value==4||value==5||value==6||value==7)if(Fire(x,y)!=0)return;}
    }
    void Run(int visibility){Need(I(fleets,targetFleet*492)==1,"Cell selector compound target branch is not admitted");if(visibility==1)PartialScan(false);else if(visibility==2)PartialScan(true);else if(visibility==3)FullScan();else throw new InvalidDataException("Cell selector visibility");}
    internal static byte[] Apply(byte[] ships,byte[] fleets,byte[] orders,int source,int target,uint seed,out uint randomAfter)
    {return Apply(ships,fleets,orders,source,target,seed,out randomAfter,false);}
    internal static byte[] Apply(byte[] ships,byte[] fleets,byte[] orders,int source,int target,uint seed,out uint randomAfter,bool originalEquipment)
    {
        Need((source==30||source==31)&&(target==14||target==15),"Cell selector admitted cell source/target");
        var state=new FleetMemberCellRequests{ships=ships,fleets=fleets,orders=orders,sourceUnit=source,targetFleet=target,weapon=I(ships,source*268+84),random=seed,originalEquipment=originalEquipment};
        // Original601 separately admits the actual17/18 F2 and22 F3 fields.
        // The earlier supplied14/19 fixtures keep their existing boundary.
        Need(originalEquipment?(state.weapon==17||state.weapon==18||state.weapon==22):(state.weapon==14||state.weapon==19),"Cell selector selected weapon fixture");state.Run(fleets[target*492+467]);randomAfter=state.random;return state.orders;
    }
}

}

