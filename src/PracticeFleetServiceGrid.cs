using System;
using System.Drawing;
namespace FleetLab
{
    internal static class PracticeFleetServiceGrid
    {
        internal static void Draw(Graphics g,Font font,byte[] ships,byte[] fleets,int viewedUnit,int fleet,float left,float top,float size,string title)
        {
            g.DrawString(title,font,Brushes.White,left,top-30);float cell=size/10;
            for(int x=0;x<10;x++)for(int y=0;y<10;y++)
            {
                int value=BitConverter.ToInt32(fleets,fleet*492+64+(x*10+y)*4);
                using(var brush=new SolidBrush(value==2?Color.SteelBlue:value>=5&&value<=8?Color.FromArgb(145,72,55):value==3?Color.FromArgb(40,65,63):Color.FromArgb(25,45,61)))g.FillRectangle(brush,left+y*cell,top+x*cell,cell-1,cell-1);
                if(value!=0&&value!=3)g.DrawString(value.ToString(),font,Brushes.White,left+y*cell+2,top+x*cell+1);
            }
            for(int n=0;n<10;n++){g.DrawString(n.ToString(),font,Brushes.LightSteelBlue,left+n*cell,top-17);g.DrawString(n.ToString(),font,Brushes.LightSteelBlue,left-22,top+n*cell);}
            for(int member=0;member<6;member++)
            {
                uint reference=BitConverter.ToUInt32(fleets,fleet*492+16+member*4);long delta=(long)reference-0x4fc340;
                if(reference==0||delta<0||delta>=ships.Length||delta%268!=0)continue;int marker=(int)delta,type=BitConverter.ToInt32(ships,marker);
                if(type!=22&&type!=23)continue;int mx=BitConverter.ToInt16(ships,marker+8),my=BitConverter.ToInt16(ships,marker+10),ml=BitConverter.ToInt16(ships,marker+14);
                if(mx>=0&&mx<10&&my>=0&&ml>0&&my+ml<=10)using(var pen=new Pen(Color.Orange,1)){pen.DashStyle=System.Drawing.Drawing2D.DashStyle.Dash;g.DrawRectangle(pen,left+my*cell+4,top+mx*cell+4,ml*cell-9,cell-9);}
            }
            if(viewedUnit<0||BitConverter.ToInt16(ships,viewedUnit*268+12)!=fleet)return;int at=viewedUnit*268,x0=BitConverter.ToInt16(ships,at+8),y0=BitConverter.ToInt16(ships,at+10),length=BitConverter.ToInt16(ships,at+14);
            if(x0>=0&&x0<10&&y0>=0&&length>0&&y0+length<=10)using(var pen=new Pen(Color.Gold,3))g.DrawRectangle(pen,left+y0*cell+1,top+x0*cell+1,length*cell-3,cell-3);
        }
    }
}
