using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FleetLab
{
    public sealed class PracticeFleetRequestLaunchStudy:Form
    {
        readonly string programs;readonly PracticeMapStartup startup;
        readonly ComboBox family=new ComboBox(),capacity=new ComboBox();
        readonly Button load=new Button(),reset=new Button(),execute=new Button();
        readonly Label summary=new Label(),feedback=new Label();readonly ListBox projectiles=new ListBox();
        readonly TextBox details=new TextBox();readonly LaunchCanvas canvas;readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeFleetRequestLaunch initial,state;byte[] fleets;
        internal PracticeFleetRequestLaunch StudyState {get{return state;}}
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeFleetRequestLaunchStudy(string programs,PracticeMapStartup startup)
        {
            this.programs=programs;this.startup=startup;Name="fleetRequestLaunchStudy";Text="Member requests -> copied local launch";
            StartPosition=FormStartPosition.CenterParent;Size=new Size(1160,840);MinimumSize=new Size(960,720);
            Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,360));Controls.Add(layout);
            canvas=new LaunchCanvas(this){Name="requestLaunchCanvas",Dock=DockStyle.Fill};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Name="requestLaunchScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);
            var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control c,int height)
            {
                c.Width=326;c.Height=height;c.Margin=new Padding(0,3,0,3);
                if(c is ComboBox||c is Button||c is ListBox){c.BackColor=Color.FromArgb(28,55,76);c.ForeColor=ForeColor;}
                var b=c as Button;if(b!=null){b.UseVisualStyleBackColor=false;b.FlatStyle=FlatStyle.Flat;b.EnabledChanged+=delegate{if(b.IsDisposed||b.Disposing)return;b.BackColor=b.Enabled?Color.FromArgb(28,55,76):Color.LightGray;b.ForeColor=b.Enabled?ForeColor:Color.Black;};}
                side.Controls.Add(c);
            };
            add(new Label{Text="One recovered consumer pass on generated member requests. It allocates numeric projectile records and uses ammunition. No flight, impact, damage, movie or full AI loop runs."},84);
            add(new Label{Text="Copied inputs (pending until Load)"},22);
            family.Name="requestLaunchFamily";family.DropDownStyle=ComboBoxStyle.DropDownList;
            family.Items.AddRange(new object[]{"F2 id 14 · targets at exact range 20","F3 id 19 · original copied positions"});family.SelectedIndex=0;add(family,30);
            capacity.Name="requestLaunchCapacity";capacity.DropDownStyle=ComboBoxStyle.DropDownList;
            capacity.Items.AddRange(new object[]{"Empty projectile pool (80 free)","One free projectile slot (slot 79)","Full projectile pool (no free slots)"});capacity.SelectedIndex=0;add(capacity,30);
            load.Name="requestLaunchLoad";load.Text="Load generated requests + copied fixtures";add(load,36);
            reset.Name="requestLaunchReset";reset.Text="Reset applied copy before launch";add(reset,34);
            execute.Name="requestLaunchExecute";execute.Text="Consume requests / one local launch pass";add(execute,42);
            summary.Name="requestLaunchSummary";add(summary,166);feedback.Name="requestLaunchFeedback";add(feedback,86);
            add(new Label{Text="Newly allocated projectile records"},22);
            projectiles.Name="requestLaunchObjects";projectiles.HorizontalScrollbar=true;add(projectiles,112);
            projectiles.SelectedIndexChanged+=delegate{RefreshDetails();};
            details.Name="requestLaunchDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;
            details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,290);
            var close=new Button{Name="requestLaunchClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            family.SelectedIndexChanged+=delegate{Pending();};capacity.SelectedIndexChanged+=delegate{Pending();};
            load.Click+=delegate{LoadCopy();};reset.Click+=delegate{if(initial!=null){state=initial;Feedback("Applied initial copy restored. Pending selectors are unchanged.");Sync();}};
            execute.Click+=delegate
            {
                if(state==null)return;
                try{state=state.Execute();Feedback("Consumed "+state.ConsumedRequests+" requests; allocated "+state.CreatedObjects+" projectile records. "+(state.CreatedObjects==0?"The full pool prevented allocation; ammunition is unchanged.":"Ammunition changed only for successful allocations."));Sync();}
                catch(InvalidOperationException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}
            };
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};
            Deactivate+=delegate{foreach(Keys k in new[]{Keys.Enter,Keys.Space})if((GetAsyncKeyState((int)k)&0x8000)!=0)held.Add(k);};LoadCopy();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){var button=ActiveControl as Button;if(button!=null){e.Handled=e.SuppressKeyPress=true;if(held.Add(e.KeyCode)&&button.Enabled)button.PerformClick();return;}}base.OnKeyDown(e);
        }
        void Pending(){if(state!=null)Feedback("Inputs pending. Load applies them; Reset restores the applied fixture.");}
        void Feedback(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.LightSteelBlue;feedback.Text=text;}
        void Reject(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.Orange;feedback.Text="Rejected; applied copy preserved. "+text;Sync();}
        void LoadCopy()
        {
            try{var next=PracticeFleetRequestLaunch.Create(programs,startup,family.SelectedIndex,capacity.SelectedIndex);state=initial=next;Feedback("Generated requests loaded. Supplied observer, zero timers and capacity fixture are applied; no consumer pass yet.");Sync();}
            catch(ArgumentException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(UnauthorizedAccessException e){Reject(e.Message);}
        }
        void Sync()
        {
            if(IsDisposed||Disposing)return;execute.Enabled=state!=null&&!state.Executed;reset.Enabled=state!=null;
            if(state==null){summary.Text="No admitted copied launch context loaded.";return;}
            fleets=state.CopyFleetPool();summary.Text="Applied: "+(state.Family==0?"F2 / exact range":"F3 / copied positions")+"\r\nRaw clock "+state.Clock+" · pass "+(state.Executed?"complete":"pending")+
                "\r\nUnit 30: requests "+state.RequestCount(0)+" · ammo "+state.Ammunition(0)+"\r\nUnit 31: requests "+state.RequestCount(1)+" · ammo "+state.Ammunition(1)+
                "\r\nConsumed "+state.ConsumedRequests+" · attempts "+state.AttemptedObjects+"\r\nAllocated "+state.CreatedObjects+" · last target "+state.LastTargetFleet+
                "\r\nAdmission 30 / 31: "+state.Eligibility(0).Denial+" / "+state.Eligibility(1).Denial;
            projectiles.BeginUpdate();projectiles.Items.Clear();foreach(var item in state.Projectiles)projectiles.Items.Add(item);if(projectiles.Items.Count>0)projectiles.SelectedIndex=0;projectiles.EndUpdate();RefreshDetails();
        }
        void RefreshDetails()
        {
            if(state==null||IsDisposed||Disposing)return;var item=projectiles.SelectedItem as PracticeMemberProjectile;
            details.Text=(item==null?"No newly allocated projectile selected.":item+"\r\nStored start "+item.X+","+item.Y+"; target local "+item.OffsetX+","+item.OffsetY+"; deadline "+item.Deadline)+
                "\r\nGold = source fleet 16. Cyan = target 15 / unit 30; orange = target 14 / unit 31. White ring = viewed projectile start. Dashed line = stored destination, not simulated flight. Coincident targets can overlap; labels retain both ids."+
                "\r\nBoth sources begin with 7 rounds. One consumer pass removes one request per source even if the projectile pool is full. Only successful allocations consume ammunition. The occupied kind-99 records are capacity fixtures and are never stepped."+
                "\r\nSupplied observer 249 / owner 1 / radii 255 enables numeric visibility. Source order timers are explicitly zero; local owner -2 and UI/message flags 0 exclude original movie/UI callbacks. F2 target origins are placed at exact range; F3 origins are unchanged."+
                "\r\nThis one pass is compared with original numeric records. No projectile motion, impacts, damage, full AI cadence or campaign progression is established. Reset and selector changes preserve the parent study and current battle.";canvas.Invalidate();
        }
        sealed class LaunchCanvas:Control
        {
            readonly PracticeFleetRequestLaunchStudy owner;
            internal LaunchCanvas(PracticeFleetRequestLaunchStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);if(owner.state==null||Width<200||Height<250)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
                float minX=float.MaxValue,maxX=float.MinValue,minY=float.MaxValue,maxY=float.MinValue;
                foreach(int id in new[]{14,15,16}){float x=BitConverter.ToInt16(owner.fleets,id*492+12),y=BitConverter.ToInt16(owner.fleets,id*492+14);minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
                minX-=20;maxX+=20;minY-=20;maxY+=20;float scale=Math.Min((Width-100)/(maxX-minX),(Height-190)/(maxY-minY));
                Func<float,float,PointF> point=delegate(float x,float y){return new PointF(50+(x-minX)*scale,90+(y-minY)*scale);};
                Func<int,PointF> origin=delegate(int id){return point(BitConverter.ToInt16(owner.fleets,id*492+12),BitConverter.ToInt16(owner.fleets,id*492+14));};
                g.DrawString("Copied world origins · X across / Y down",owner.Font,Brushes.WhiteSmoke,new PointF(14,14));
                g.DrawString("One local launch pass; records remain at their stored start positions.",owner.Font,Brushes.LightSteelBlue,new RectangleF(14,38,Width-28,42));
                foreach(var item in owner.state.Projectiles)
                {
                    var from=point(item.X,item.Y);int target=item.TargetFleet*492;
                    var to=point(BitConverter.ToInt16(owner.fleets,target+12)+item.OffsetX,BitConverter.ToInt16(owner.fleets,target+14)+item.OffsetY);
                    using(var pen=new Pen(item.SourceUnit==30?Color.Cyan:Color.Orange,2)){pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,from,to);}
                    g.FillEllipse(item.SourceUnit==30?Brushes.Cyan:Brushes.Orange,from.X-4,from.Y-4,8,8);
                    var selected=owner.projectiles.SelectedItem as PracticeMemberProjectile;if(selected!=null&&selected.Slot==item.Slot)using(var pen=new Pen(Color.White,2))g.DrawEllipse(pen,from.X-8,from.Y-8,16,16);
                }
                foreach(int id in new[]{16,15,14})
                {
                    PointF p=origin(id);Brush brush=id==16?Brushes.Gold:id==15?Brushes.Cyan:Brushes.Orange;
                    using(var pen=new Pen(id==16?Color.Gold:id==15?Color.Cyan:Color.Orange,2))g.DrawRectangle(pen,p.X-10,p.Y-10,20,20);
                    // Separate labels when exact-range fixtures coincide.
                    float shift=id==14?24:id==15?-42:12;
                    g.DrawString("Fleet "+id+" · "+BitConverter.ToInt16(owner.fleets,id*492+12)+","+BitConverter.ToInt16(owner.fleets,id*492+14),owner.Font,brush,new PointF(Math.Max(4,Math.Min(Width-170,p.X-40)),p.Y+shift));
                }
                g.DrawString("Dashed lines show stored destinations, not moving shots.\r\nAllocation and ammo only; no impact, damage, movie or sound.",owner.Font,Brushes.LightSteelBlue,new RectangleF(14,Height-64,Width-28,60));
            }
        }
    }
}
