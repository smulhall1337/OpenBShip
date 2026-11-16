using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace FleetLab
{
    public sealed class PracticeMapYardStudy : Form
    {
        readonly string programs;
        readonly PracticeMapStartup startup;
        readonly ComboBox target=new ComboBox(),profile=new ComboBox();
        readonly Button enter=new Button(),one=new Button(),until=new Button(),load=new Button(),reset=new Button();
        readonly Label summary=new Label(),feedback=new Label();
        readonly TextBox details=new TextBox();
        readonly YardCanvas canvas;
        readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeMapYardState state,initial;
        byte[] ships,fleets;
        internal PracticeMapYardState StudyState { get { return state; } }
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeMapYardStudy(string programs,PracticeMapStartup startup,int preferredTarget)
        {
            this.programs=programs;this.startup=startup;
            Name="mapYardStudy";Text="BSSB202 map yard · diagnostic copy";StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1160,820);MinimumSize=new Size(960,700);Font=new Font("Segoe UI",9);
            BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,350));Controls.Add(layout);
            canvas=new YardCanvas(this){Dock=DockStyle.Fill,Name="mapYardCanvas"};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Name="mapYardScroll"};layout.Controls.Add(scroll,1,0);
            var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control control,int height)
            {
                control.Width=316;control.Height=height;control.Margin=new Padding(0,3,0,3);
                if(control is Button||control is ComboBox){control.BackColor=Color.FromArgb(28,55,76);control.ForeColor=ForeColor;}
                var button=control as Button;
                if(button!=null){button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;button.EnabledChanged+=delegate{if(button.IsDisposed||button.Disposing)return;button.BackColor=button.Enabled?Color.FromArgb(28,55,76):Color.LightGray;button.ForeColor=button.Enabled?ForeColor:Color.Black;};}
                side.Controls.Add(control);
            };
            add(new Label{Text="Independent copy of recovered BSSB202 startup, seed 731. Yard selection 3 is supplied explicitly; hull/ammo changes are test inputs. No travel, discovery, world clock, AI or original display runs."},94);
            add(new Label{Text="Target unit (pending until Load)"},22);
            target.Name="mapYardTarget";target.DropDownStyle=ComboBoxStyle.DropDownList;
            target.Items.AddRange(new object[]{"30 · type 1 · length 5","31 · type 3 · length 3","34 · type 4 · length 2"});target.SelectedIndex=Array.IndexOf(new[]{30,31,34},preferredTarget);if(target.SelectedIndex<0)target.SelectedIndex=0;add(target,30);
            add(new Label{Text="Copied hull / ammunition profile"},22);
            profile.Name="mapYardProfile";profile.DropDownStyle=ComboBoxStyle.DropDownList;
            profile.Items.AddRange(new object[]{"Intact · original ammunition","One state 8 · HP length−1 · empty ammo","Mixed states · HP 0 · empty ammo"});profile.SelectedIndex=1;add(profile,30);
            load.Name="mapYardLoad";load.Text="Load selected test copy";add(load,34);
            reset.Name="mapYardReset";reset.Text="Reset applied test copy";add(reset,34);
            summary.Name="mapYardSummary";add(summary,142);
            feedback.Name="mapYardFeedback";add(feedback,92);
            enter.Name="mapYardEnter";enter.Text="Enter copied yard (once)";add(enter,34);
            one.Name="mapYardOne";one.Text="One recovered worker pass";add(one,34);
            until.Name="mapYardUntil";until.Text="Up to 20 passes / stop on return";add(until,34);
            details.Name="mapYardDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,260);
            var close=new Button{Name="mapYardClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            target.SelectedIndexChanged+=delegate{Pending();};profile.SelectedIndexChanged+=delegate{Pending();};
            load.Click+=delegate{LoadCopy(new[]{30,31,34}[target.SelectedIndex],profile.SelectedIndex);};
            reset.Click+=delegate{if(initial!=null){state=initial;Feedback("Applied copy reset; pending selectors are unchanged.");Sync();}};
            enter.Click+=delegate{Change(delegate{return state.Enter();},"Copied target admitted. Apply worker passes manually.");};
            one.Click+=delegate{Change(delegate{return state.WorkerStep();},"One source worker pass applied.");};
            until.Click+=delegate{Change(delegate{var next=state;for(int n=0;n<20&&!next.Returned;n++)next=next.WorkerStep();return next;},"Bounded worker batch finished.");};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{held.Clear();};
            Activated+=delegate{foreach(var key in new[]{Keys.Enter,Keys.Space,Keys.Escape})if((GetAsyncKeyState((int)key)&0x8000)!=0)held.Add(key);};
            LoadCopy(new[]{30,31,34}[target.SelectedIndex],profile.SelectedIndex);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);if(e.KeyCode!=Keys.Enter&&e.KeyCode!=Keys.Space&&e.KeyCode!=Keys.Escape)return;
            var button=ActiveControl as Button;if(e.KeyCode!=Keys.Escape&&button==null)return;
            e.Handled=e.SuppressKeyPress=true;if(!held.Add(e.KeyCode))return;
            if(e.KeyCode==Keys.Escape)Close();else if(button.Enabled)button.PerformClick();
        }
        void Pending(){if(state!=null)Feedback("Selection is pending. Load applies it; Reset restores the applied copy.");}
        void Feedback(string text){feedback.ForeColor=ForeColor;feedback.Text=text;}
        void LoadCopy(int unit,int variant)
        {
            try{var next=PracticeMapYardState.Create(programs,startup,unit,variant);state=initial=next;Feedback("Supplied command: source selected-yard byte = 3. Local owner = 1. Enter transfers the target into the first fitting recovered bay.");Sync();}
            catch(InvalidDataException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(ArgumentException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(UnauthorizedAccessException e){Reject(e.Message);}
        }
        void Change(Func<PracticeMapYardState> action,string message)
        {
            try{var next=action();state=next;Feedback(next.Returned?"Returned to original roster after "+next.WorkerCalls+" worker pass(es). This copy may be inspected or reset.":message);Sync();}
            catch(InvalidDataException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidOperationException e){Reject(e.Message);}
        }
        void Reject(string reason){feedback.ForeColor=Color.Orange;feedback.Text="Rejected; existing copy preserved. "+reason;Sync();}
        void Sync()
        {
            if(IsDisposed||Disposing)return;
            reset.Enabled=state!=null;enter.Enabled=state!=null&&!state.Entered;one.Enabled=until.Enabled=state!=null&&state.Entered&&!state.Returned;
            if(state==null){summary.Text="No admitted map yard copy loaded.";return;}
            ships=state.CopyShipPool();fleets=state.CopyFleetPool();int parent=BitConverter.ToInt16(ships,state.TargetUnit*268+12);
            summary.Text="Applied unit "+state.TargetUnit+" · profile "+state.Profile+" · seed 731\r\n"+(state.Returned?"Returned":state.Entered?"In copied yard":"Ready for admission")+" · worker passes "+state.WorkerCalls+"\r\nSource "+state.OriginFleet+" → bay fleet "+state.YardFleet+" · parent now "+parent+"\r\nHP "+state.TargetHP+" / "+state.TargetLength+" · ammo "+state.TargetAmmo+" (Δ "+(state.TargetAmmo-initial.TargetAmmo)+")\r\nDepot unit "+state.DepotUnit+" · ammo "+state.DepotAmmo+" (Δ "+(state.DepotAmmo-initial.DepotAmmo)+")";
            var text=new StringBuilder();text.AppendLine("Source selection: fleet "+state.SelectedFleet+" / unit "+state.SelectedUnit);
            text.AppendLine("Display requests (counted): text "+state.TextRequests+", map "+state.MapRequests+", refresh "+state.RefreshRequests);
            text.AppendLine("Grid: rows = local X; columns = local Y. Gold = target; orange = bay marker. 2 occupied, 3 base background, 5..8 supplied damage states.");
            text.AppendLine("Marker "+state.MarkerUnit+" / saved return fields:");int mk=state.MarkerUnit*268;
            text.AppendLine("fleet "+BitConverter.ToInt16(ships,mk+196)+", member "+BitConverter.ToInt16(ships,mk+198)+", X "+BitConverter.ToInt16(ships,mk+200)+", Y "+BitConverter.ToInt16(ships,mk+202));
            text.AppendLine("The original worker supplies one weapon type per pass. A pass without ammunition transfer returns the unit and sets its HP to length. This does not prove every damaged cell was repaired first.");
            text.AppendLine("Parent startup snapshots, active battle and settings remain independent. No original code executes in this study; the general world support guard remains.");details.Text=text.ToString();canvas.Invalidate();
        }
        sealed class YardCanvas : Control
        {
            readonly PracticeMapYardStudy owner;
            internal YardCanvas(PracticeMapYardStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);if(owner.state==null||Width<100||Height<250)return;
                var state=owner.state;var g=e.Graphics;float size=Math.Min(Width-70,(Height-164)/2f),left=(Width-size)/2;
                DrawGrid(g,state.OriginFleet,left,42,size,"Original fleet "+state.OriginFleet);
                DrawGrid(g,state.YardFleet,left,size+106,size,"Working bay fleet "+state.YardFleet);
                g.DrawString("Copied map, manual worker calls\r\nGold target · orange marker · row X / column Y",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-48,Math.Max(1,Width-24),46));
            }
            void DrawGrid(Graphics g,int fleet,float left,float top,float size,string title)
            {
                float cell=size/10;g.DrawString(title,owner.Font,Brushes.White,left,top-30);
                for(int x=0;x<10;x++)for(int y=0;y<10;y++)
                {
                    int value=BitConverter.ToInt32(owner.fleets,fleet*492+64+(x*10+y)*4);
                    Color color=value==2?Color.SteelBlue:value>=5&&value<=8?Color.FromArgb(145,72,55):value==3?Color.FromArgb(40,65,63):Color.FromArgb(25,45,61);
                    using(var brush=new SolidBrush(color))g.FillRectangle(brush,left+y*cell,top+x*cell,cell-1,cell-1);
                    if(value!=0&&value!=3)g.DrawString(value.ToString(),owner.Font,Brushes.White,left+y*cell+2,top+x*cell+1);
                }
                for(int n=0;n<10;n++){g.DrawString(n.ToString(),owner.Font,Brushes.LightSteelBlue,left+n*cell,top-17);g.DrawString(n.ToString(),owner.Font,Brushes.LightSteelBlue,left-22,top+n*cell);}
                Outline(g,owner.state.MarkerUnit,fleet,left,top,cell,Color.Coral);Outline(g,owner.state.TargetUnit,fleet,left,top,cell,Color.Gold);
            }
            void Outline(Graphics g,int unit,int fleet,float left,float top,float cell,Color color)
            {
                int at=unit*268;if(BitConverter.ToInt16(owner.ships,at+12)!=fleet)return;
                int x=BitConverter.ToInt16(owner.ships,at+8),y=BitConverter.ToInt16(owner.ships,at+10),length=BitConverter.ToInt16(owner.ships,at+14);
                if(x<0||x>=10||y<0||length<1||y+length>10)return;
                using(var pen=new Pen(color,3))g.DrawRectangle(pen,left+y*cell+1,top+x*cell+1,length*cell-3,cell-3);
            }
        }
    }
}
