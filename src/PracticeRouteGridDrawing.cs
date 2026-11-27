using System;using System.Drawing;
namespace FleetLab
{
    internal sealed class PracticeRouteGridView
    {
        internal PracticeAiRouteGrid Grid;internal bool HasOutput,ContinuousPosition,OriginOnTop;
        internal Point Origin,Target;internal Point? Waypoint;internal Point[] Trail;
        internal int SelectedX,SelectedY;internal string Header,Subheader,Footer;
    }
    internal static class PracticeRouteGridDrawing
    {
        static PointF PlotPoint(RectangleF plot,float step,Point p,bool continuous)
        {
            float x=continuous?p.X/10f:p.X/10,y=continuous?p.Y/10f:p.Y/10;
            return new PointF(plot.Left+(x+0.5f)*step,plot.Top+(y+0.5f)*step);
        }
        internal static RectangleF Paint(Graphics graphics,Size size,Font font,PracticeRouteGridView view)
        {
            float side=Math.Max(64,Math.Min(size.Width-70,size.Height-180));var plot=new RectangleF(42,86,side,side);float step=side/64;
            graphics.DrawString(view.Header,font,Brushes.WhiteSmoke,12,12);graphics.DrawString(view.Subheader,font,Brushes.LightSteelBlue,12,36);
            var bytes=view.Grid.CopyCells();
            using(var brush=new SolidBrush(Color.Black))for(int x=0;x<64;x++)for(int y=0;y<64;y++)
            {
                int value=bytes[x*64+y];brush.Color=!view.HasOutput?Color.FromArgb(43,47,57):value==255?Color.FromArgb(155,66,55):value==254?Color.FromArgb(26,43,58):Color.FromArgb(35,Math.Max(65,185-value),Math.Max(110,210-value/2));
                graphics.FillRectangle(brush,plot.Left+x*step,plot.Top+y*step,step+0.3f,step+0.3f);
            }
            using(var pen=new Pen(Color.FromArgb(68,85,99)))for(int n=0;n<=64;n+=8)
            {
                float offset=n*step;graphics.DrawLine(pen,plot.Left+offset,plot.Top,plot.Left+offset,plot.Bottom);graphics.DrawLine(pen,plot.Left,plot.Top+offset,plot.Right,plot.Top+offset);
                if(n<64){graphics.DrawString(n.ToString(),font,Brushes.LightSteelBlue,plot.Left+offset,plot.Top-23);graphics.DrawString(n.ToString(),font,Brushes.LightSteelBlue,plot.Left-28,plot.Top+offset);}
            }
            using(var pen=new Pen(Color.White,1.5f))graphics.DrawRectangle(pen,plot.Left+view.SelectedX*step,plot.Top+view.SelectedY*step,step,step);
            if(view.Trail!=null&&view.Trail.Length>1)
            {
                var points=new PointF[view.Trail.Length];for(int n=0;n<points.Length;n++)points[n]=PlotPoint(plot,step,view.Trail[n],view.ContinuousPosition);
                using(var pen=new Pen(Color.FromArgb(170,Color.Gold),1.8f))graphics.DrawLines(pen,points);
            }
            var origin=PlotPoint(plot,step,view.Origin,view.ContinuousPosition);var target=PlotPoint(plot,step,view.Target,view.ContinuousPosition);
            using(var pen=new Pen(Color.LightGreen,2.4f))graphics.DrawRectangle(pen,target.X-6,target.Y-6,12,12);
            if(!view.OriginOnTop)using(var pen=new Pen(Color.Gold,2.4f))graphics.DrawEllipse(pen,origin.X-4,origin.Y-4,8,8);
            if(view.Waypoint.HasValue&&view.Waypoint.Value.X>=0&&view.Waypoint.Value.X<=630&&view.Waypoint.Value.Y>=0&&view.Waypoint.Value.Y<=630)
            {
                var point=PlotPoint(plot,step,view.Waypoint.Value,view.ContinuousPosition);
                using(var pen=new Pen(Color.Cyan,2))
                {
                    graphics.DrawLine(pen,origin.X,origin.Y,point.X,point.Y);graphics.DrawLine(pen,point.X-5,point.Y-5,point.X+5,point.Y+5);graphics.DrawLine(pen,point.X-5,point.Y+5,point.X+5,point.Y-5);
                }
            }
            if(view.OriginOnTop)using(var pen=new Pen(Color.Gold,2.4f))graphics.DrawEllipse(pen,origin.X-4,origin.Y-4,8,8);
            graphics.DrawString(view.Footer,font,Brushes.LightSteelBlue,new RectangleF(12,plot.Bottom+20,Math.Max(1,size.Width-24),68));
            return plot;
        }
    }
}
