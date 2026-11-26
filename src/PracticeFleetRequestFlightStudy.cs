using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FleetLab
{
    public sealed class PracticeFleetRequestFlightStudy:Form
    {
        readonly Button step=new Button(),batch=new Button(),boundary=new Button(),reset=new Button();
        readonly Label summary=new Label(),feedback=new Label();readonly ListBox projectiles=new ListBox();readonly TextBox details=new TextBox();
        readonly FlightCanvas canvas;readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeFleetRequestFlight state,initial;
        internal PracticeFleetRequestFlight StudyState {get{return state;}}
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeFleetRequestFlightStudy(PracticeFleetRequestLaunch launch)
        {
            Name="fleetRequestFlightStudy";Text="Copied member flight · stop before first impact";StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1160,840);MinimumSize=new Size(960,720);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,360));Controls.Add(layout);
            canvas=new FlightCanvas(this){Name="memberFlightCanvas",Dock=DockStyle.Fill};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Name="memberFlightScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);
            var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control c,int height)
            {
                c.Width=326;c.Height=height;c.Margin=new Padding(0,3,0,3);if(c is Button||c is ListBox){c.BackColor=Color.FromArgb(28,55,76);c.ForeColor=ForeColor;}
                var b=c as Button;if(b!=null){b.UseVisualStyleBackColor=false;b.FlatStyle=FlatStyle.Flat;b.EnabledChanged+=delegate{if(b.IsDisposed||b.Disposing)return;b.BackColor=b.Enabled?Color.FromArgb(28,55,76):Color.LightGray;b.ForeColor=b.Enabled?ForeColor:Color.Black;};}side.Controls.Add(c);
            };
            add(new Label{Text="Copied future-only flight after the empty-pool launch. Controls supply source clock updates. Stop before the first impact; no damage, further requests, movies or audio run."},80);
            step.Name="memberFlightStep";step.Text="1 numeric update (+40)";add(step,38);
            batch.Name="memberFlightBatch";batch.Text="Up to 32 updates / stop before impact";add(batch,38);
            boundary.Name="memberFlightBoundary";boundary.Text="Advance to first deadline - 1";add(boundary,38);
            reset.Name="memberFlightReset";reset.Text="Reset flight to copied launch";add(reset,34);
            summary.Name="memberFlightSummary";add(summary,150);feedback.Name="memberFlightFeedback";add(feedback,86);
            add(new Label{Text="Live numeric projectile records"},22);projectiles.Name="memberFlightObjects";projectiles.HorizontalScrollbar=true;add(projectiles,94);projectiles.SelectedIndexChanged+=delegate{RefreshDetails();};
            details.Name="memberFlightDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,270);
            var close=new Button{Name="memberFlightClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            step.Click+=delegate{Advance(0);};batch.Click+=delegate{Advance(1);};boundary.Click+=delegate{Advance(2);};
            reset.Click+=delegate{if(initial!=null){state=initial;Feedback("Copied launch positions restored; parent launch and active battle are unchanged.");Sync();}};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{foreach(Keys k in new[]{Keys.Enter,Keys.Space})if((GetAsyncKeyState((int)k)&0x8000)!=0)held.Add(k);};
            try{state=initial=PracticeFleetRequestFlight.Create(launch);Feedback("Flight copy ready. Clock advances only when a numeric update control is used.");Sync();}
            catch(ArgumentException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){var b=ActiveControl as Button;if(b!=null){e.Handled=e.SuppressKeyPress=true;if(held.Add(e.KeyCode)&&b.Enabled)b.PerformClick();return;}}base.OnKeyDown(e);
        }
        void Feedback(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.LightSteelBlue;feedback.Text=text;}
        void Reject(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.Orange;feedback.Text="Rejected; applied copy preserved. "+text;Sync();}
        void Advance(int action)
        {
            if(state==null)return;try
            {
                state=action==0?state.Step():action==1?state.UpTo(32):state.ToBoundary();
                Feedback(state.AtBoundary?"Stopped one source clock unit before the first impact. No expiry or damage was run.":!state.CanStep?"No full +40 step remains before impact. The boundary control can visit deadline - 1.":"Numeric flight updated. Original rounding can leave a coordinate unchanged on a step.");Sync();
            }
            catch(InvalidOperationException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}
        }
        void Sync()
        {
            if(IsDisposed||Disposing)return;step.Enabled=batch.Enabled=state!=null&&state.CanStep;boundary.Enabled=state!=null&&!state.AtBoundary;reset.Enabled=state!=null;
            if(state==null){summary.Text="No admitted empty-pool launch loaded.";return;}
            var items=state.Projectiles;summary.Text="Applied: "+(state.Launch.Family==0?"F2 id 14":"F3 id 19")+" · two copied projectiles\r\nRaw clock "+state.Clock+" · first deadline "+state.FirstDeadline+"\r\nRemaining "+state.Remaining+" · updates "+state.Updates+
                "\r\nInterpolations "+state.InterpolatedRecords+" · stopped slot "+state.LastStoppedAtRecord+"\r\nUnit 30: "+items[0].X+","+items[0].Y+"\r\nUnit 31: "+items[1].X+","+items[1].Y+"\r\nAmmo and request rows hold at their launch values.";
            int selected=projectiles.SelectedIndex;projectiles.BeginUpdate();projectiles.Items.Clear();foreach(var item in items)projectiles.Items.Add(item);projectiles.SelectedIndex=selected>=0&&selected<items.Count?selected:0;projectiles.EndUpdate();RefreshDetails();
        }
        void RefreshDetails()
        {
            if(state==null||IsDisposed||Disposing)return;var item=projectiles.SelectedItem as PracticeMemberProjectile;
            details.Text=(item==null?"No viewed projectile.":item+"\r\nCurrent "+item.X+","+item.Y+"; target local "+item.OffsetX+","+item.OffsetY+"; deadline "+item.Deadline)+
                "\r\nCyan = unit 30; orange = unit 31; white ring = viewed position. Faint dots mark copied launch positions. Dashed lines show remaining destination vectors."+
                "\r\nEach normal update advances the supplied clock by 40 with prior delta 40. Integer rounding may leave a coordinate unchanged. A zero remaining-update quotient stops the entire original pass; later slots keep their positions."+
                "\r\nThe boundary control uses normal +40 visits, then an explicit shorter clock jump to first deadline - 1, retaining prior delta 40. It never visits impact. F3 weapon 19's impact branch remains unsupported."+
                "\r\nAmmunition, requests, RNG and ships are frozen after the copied launch. Fleets stay fixed; only their recovered transient flight marks change. No new firing, impacts, damage, effects, source wall-clock pacing or full AI lifecycle is implied. Reset preserves the parent launch and current battle.";canvas.Invalidate();
        }
        sealed class FlightCanvas:Control
        {
            readonly PracticeFleetRequestFlightStudy owner;internal FlightCanvas(PracticeFleetRequestFlightStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(owner.state==null)return;var selected=owner.projectiles.SelectedItem as PracticeMemberProjectile;PracticeMemberFlightDiagram.Draw(e.Graphics,owner.Font,ClientRectangle,owner.state.CopyFleetPool(),owner.state.Launch.Projectiles,owner.state.Projectiles,selected==null?-1:selected.Slot,true);}
        }
    }

    internal static class PracticeMemberFlightDiagram
    {
        internal static Func<float,float,PointF> Draw(Graphics g,Font font,Rectangle bounds,byte[] fleets,IList<PracticeMemberProjectile> initial,IList<PracticeMemberProjectile> current,int selected,bool flight,string heading=null,string footer=null)
        {
            int width=bounds.Width,height=bounds.Height;if(width<200||height<250)return null;g.SmoothingMode=SmoothingMode.AntiAlias;
            float minX=float.MaxValue,maxX=float.MinValue,minY=float.MaxValue,maxY=float.MinValue;
            Action<float,float> include=delegate(float x,float y){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);};
            foreach(int id in new[]{14,15,16})include(BitConverter.ToInt16(fleets,id*492+12),BitConverter.ToInt16(fleets,id*492+14));
            foreach(var item in initial){include(item.X,item.Y);int at=item.TargetFleet*492;include(BitConverter.ToInt16(fleets,at+12)+item.OffsetX,BitConverter.ToInt16(fleets,at+14)+item.OffsetY);}
            minX-=20;maxX+=20;minY-=20;maxY+=20;float scale=Math.Min((width-100)/(maxX-minX),(height-190)/(maxY-minY));
            Func<float,float,PointF> point=delegate(float x,float y){return new PointF(50+(x-minX)*scale,90+(y-minY)*scale);};
            g.DrawString("Copied world origins · X across / Y down",font,Brushes.WhiteSmoke,new PointF(14,14));
            g.DrawString(heading??(flight?"Numeric pre-impact flight · faint dots mark launch positions.":"One local launch pass; records remain at their stored start positions."),font,Brushes.LightSteelBlue,new RectangleF(14,38,width-28,42));
            if(flight)foreach(var item in initial){var p=point(item.X,item.Y);using(var brush=new SolidBrush(Color.FromArgb(85,item.SourceUnit==30?Color.Cyan:Color.Orange)))g.FillEllipse(brush,p.X-4,p.Y-4,8,8);}
            foreach(var item in current)
            {
                var from=point(item.X,item.Y);int at=item.TargetFleet*492;var to=point(BitConverter.ToInt16(fleets,at+12)+item.OffsetX,BitConverter.ToInt16(fleets,at+14)+item.OffsetY);
                using(var pen=new Pen(item.SourceUnit==30?Color.Cyan:Color.Orange,2)){pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,from,to);g.DrawLine(pen,to.X-3,to.Y,to.X+3,to.Y);g.DrawLine(pen,to.X,to.Y-3,to.X,to.Y+3);}
                g.FillEllipse(item.SourceUnit==30?Brushes.Cyan:Brushes.Orange,from.X-4,from.Y-4,8,8);
                if(selected==item.Slot)using(var pen=new Pen(Color.White,2))g.DrawEllipse(pen,from.X-8,from.Y-8,16,16);
            }
            foreach(int id in new[]{16,15,14})
            {
                var p=point(BitConverter.ToInt16(fleets,id*492+12),BitConverter.ToInt16(fleets,id*492+14));int radius=id==14?13:10;
                using(var pen=new Pen(id==16?Color.Gold:id==15?Color.Cyan:Color.Orange,2))g.DrawRectangle(pen,p.X-radius,p.Y-radius,radius*2,radius*2);
                float shift=id==14?24:id==15?-42:12;
                g.DrawString("Fleet "+id+" · "+BitConverter.ToInt16(fleets,id*492+12)+","+BitConverter.ToInt16(fleets,id*492+14),font,id==16?Brushes.Gold:id==15?Brushes.Cyan:Brushes.Orange,new PointF(Math.Max(4,Math.Min(width-170,p.X-40)),p.Y+shift));
            }
            g.DrawString(footer??(flight?"Stops before the first impact; no damage or further firing.\r\nDashed lines show stored destinations; clock advances only by controls.":"Dashed lines show stored destinations, not moving shots.\r\nAllocation and ammo only; no impact, damage, movie or sound."),font,Brushes.LightSteelBlue,new RectangleF(14,height-64,width-28,60));
            return point;
        }
    }
}
