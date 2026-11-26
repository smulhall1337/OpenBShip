using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FleetLab
{
    public sealed class PracticeProducedFlightStudy:Form
    {
        readonly Button step=new Button(),batch=new Button(),boundary=new Button(),reset=new Button();
        readonly Label summary=new Label(),feedback=new Label();readonly ListBox projectiles=new ListBox();readonly TextBox details=new TextBox();
        readonly FlightCanvas canvas;readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeProducedFlight state,initial;
        internal PracticeProducedFlight StudyState {get{return state;}}
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeProducedFlightStudy(PracticeProducedLaunch launch)
        {
            Name="producedFlightStudy";Text="Original-map weapon22 flight · before first impact";StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1160,840);MinimumSize=new Size(960,720);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,360));Controls.Add(layout);
            canvas=new FlightCanvas(this){Name="producedFlightCanvas",Dock=DockStyle.Fill};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Name="producedFlightScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);
            var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control c,int height)
            {
                c.Width=326;c.Height=height;c.Margin=new Padding(0,3,0,3);if(c is Button||c is ListBox){c.BackColor=Color.FromArgb(28,55,76);c.ForeColor=ForeColor;}
                var b=c as Button;if(b!=null){b.UseVisualStyleBackColor=false;b.FlatStyle=FlatStyle.Flat;b.EnabledChanged+=delegate{if(b.IsDisposed||b.Disposing)return;b.BackColor=b.Enabled?Color.FromArgb(28,55,76):Color.LightGray;b.ForeColor=b.Enabled?ForeColor:Color.Black;};}side.Controls.Add(c);
            };
            add(new Label{Text="Copied future-only flight after the empty-pool launch. Controls supply source clock updates. Stop before the first impact; no damage, further requests, movies or audio run."},80);
            step.Name="producedFlightStep";step.Text="1 numeric update (+40)";add(step,38);
            batch.Name="producedFlightBatch";batch.Text="Up to 32 updates / stop before impact";add(batch,38);
            boundary.Name="producedFlightBoundary";boundary.Text="Advance to first deadline - 1";add(boundary,38);
            reset.Name="producedFlightReset";reset.Text="Reset flight to copied launch";add(reset,34);
            summary.Name="producedFlightSummary";add(summary,150);feedback.Name="producedFlightFeedback";add(feedback,86);
            add(new Label{Text="Live numeric projectile records"},22);projectiles.Name="producedFlightObjects";projectiles.HorizontalScrollbar=true;add(projectiles,94);projectiles.SelectedIndexChanged+=delegate{RefreshDetails();};
            details.Name="producedFlightDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,270);
            var close=new Button{Name="producedFlightClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            step.Click+=delegate{Advance(0);};batch.Click+=delegate{Advance(1);};boundary.Click+=delegate{Advance(2);};
            reset.Click+=delegate{if(initial!=null){state=initial;Feedback("Copied launch positions restored; parent launch and active battle are unchanged.");Sync();}};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{foreach(Keys k in new[]{Keys.Enter,Keys.Space})if((GetAsyncKeyState((int)k)&0x8000)!=0)held.Add(k);};
            try{state=initial=PracticeProducedFlight.Create(launch);Feedback("Flight copy ready. Clock advances only when a numeric update control is used.");Sync();}
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
            var items=state.Projectiles;summary.Text="Applied: "+"F3 id 22"+" · two copied projectiles\r\nRaw clock "+state.Clock+" · first deadline "+state.FirstDeadline+"\r\nRemaining "+state.Remaining+" · updates "+state.Updates+
                "\r\nInterpolations "+state.InterpolatedRecords+" · stopped slot "+state.LastStoppedAtRecord+"\r\nUnit 30: "+items[0].X+","+items[0].Y+"\r\nUnit 31: "+items[1].X+","+items[1].Y+"\r\nAmmo and request rows hold at their launch values.";
            int selected=projectiles.SelectedIndex;projectiles.BeginUpdate();projectiles.Items.Clear();foreach(var item in items)projectiles.Items.Add(item);projectiles.SelectedIndex=selected>=0&&selected<items.Count?selected:0;projectiles.EndUpdate();RefreshDetails();
        }
        void RefreshDetails()
        {
            if(state==null||IsDisposed||Disposing)return;var item=projectiles.SelectedItem as PracticeMemberProjectile;
            details.Text=(item==null?"No viewed projectile.":item+"\r\nCurrent "+item.X+","+item.Y+"; target local "+item.OffsetX+","+item.OffsetY+"; deadline "+item.Deadline)+
                "\r\nCyan = unit 30; orange = unit 31; white ring = viewed position. Faint dots mark copied launch positions. Dashed lines show remaining destination vectors."+
                "\r\nEach normal update advances the supplied clock by 40 with prior delta 40. Integer rounding may leave a coordinate unchanged. A zero remaining-update quotient stops the entire original pass; later slots keep their positions."+
                "\r\nThe boundary control uses normal +40 visits, then an explicit shorter clock jump to first deadline - 1, retaining prior delta 40. It never visits impact. Weapon22 impact is deliberately outside this future-only study."+
                "\r\nAmmunition, requests, RNG and ships are frozen after the copied launch. Fleets stay fixed; only their recovered transient flight marks change. No new firing, impacts, damage, effects, source wall-clock pacing or full AI lifecycle is implied. Reset preserves the parent launch and current battle.";canvas.Invalidate();
        }
        sealed class FlightCanvas:Control
        {
            readonly PracticeProducedFlightStudy owner;internal FlightCanvas(PracticeProducedFlightStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(owner.state==null)return;var selected=owner.projectiles.SelectedItem as PracticeMemberProjectile;PracticeMemberFlightDiagram.Draw(e.Graphics,owner.Font,ClientRectangle,owner.state.CopyFleetPool(),owner.state.Launch.Projectiles,owner.state.Projectiles,selected==null?-1:selected.Slot,true);}
        }
    }

}
