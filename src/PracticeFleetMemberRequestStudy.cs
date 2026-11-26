using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace FleetLab
{
    public sealed class PracticeFleetMemberRequestStudy:Form
    {
        readonly string programs;readonly PracticeMapStartup startup;readonly PracticeFleetMemberPreparation prepared;
        readonly ComboBox equipment=new ComboBox(),task=new ComboBox(),difficulty=new ComboBox(),comparison=new ComboBox(),globalWeapon=new ComboBox(),visibility=new ComboBox(),queue=new ComboBox();
        readonly Button load=new Button(),reset=new Button(),execute=new Button();readonly Label summary=new Label(),feedback=new Label();readonly ListBox cells=new ListBox();readonly TextBox details=new TextBox();readonly RequestCanvas canvas;readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeFleetMemberRequests state,initial;byte[] ships,fleets;
        internal PracticeFleetMemberRequests StudyState {get{return state;}}
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeFleetMemberRequestStudy(string programs,PracticeMapStartup startup):this(programs,startup,null){}
        public PracticeFleetMemberRequestStudy(string programs,PracticeMapStartup startup,PracticeFleetMemberPreparation prepared)
        {
            this.programs=programs;this.startup=startup;this.prepared=prepared;Name="fleetMemberRequestStudy";Text=prepared==null?"Fleet member requests · one copied visit":"Produced visibility → original-equipment requests";StartPosition=FormStartPosition.CenterParent;Size=new Size(1160,840);MinimumSize=new Size(960,720);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,360));Controls.Add(layout);canvas=new RequestCanvas(this){Name="memberRequestCanvas",Dock=DockStyle.Fill};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Name="memberRequestScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control c,int height){c.Width=326;c.Height=height;c.Margin=new Padding(0,3,0,3);if(c is ComboBox||c is Button||c is ListBox){c.BackColor=Color.FromArgb(28,55,76);c.ForeColor=ForeColor;}var b=c as Button;if(b!=null){b.UseVisualStyleBackColor=false;b.FlatStyle=FlatStyle.Flat;b.EnabledChanged+=delegate{if(b.IsDisposed||b.Disposing)return;b.BackColor=b.Enabled?Color.FromArgb(28,55,76):Color.LightGray;b.ForeColor=b.Enabled?ForeColor:Color.Black;};}side.Controls.Add(c);};
            add(new Label{Text="One whole copied member visit after preparation. Colored crosses are queued requests, not launched shots or hits. Target grids expose copied data for inspection. No ammo use, projectiles or damage runs."},84);add(new Label{Text="Inputs (pending until Load)"},22);
            Action<ComboBox,string,object[],int> choices=delegate(ComboBox box,string name,object[] values,int selected){box.Name=name;box.DropDownStyle=ComboBoxStyle.DropDownList;box.Items.AddRange(values);box.SelectedIndex=selected;add(box,30);box.SelectedIndexChanged+=delegate{if(state!=null)feedback.Text="Inputs pending. Load applies them; Reset restores the applied queue fixture.";};};
            choices(equipment,"memberRequestEquipment",prepared==null?new object[]{"F2 id 14 · both units","F3 id 19 · both units","F2 unit 30 / F3 unit 31"}:new object[]{"Original equipment from preparation"},0);choices(task,"memberRequestTask",new object[]{"Supplied initial task 0","Supplied initial task 8"},prepared!=null&&prepared.InitialTask==8?1:0);choices(difficulty,"memberRequestDifficulty",new object[]{"Difficulty 0","Difficulty 2"},1);choices(comparison,"memberRequestComparison",new object[]{"Supplied caller comparison word 0","Supplied caller comparison word 1"},0);choices(globalWeapon,"memberRequestGlobalWeapon",prepared==null?new object[]{"Supplied global weapon field 0","Supplied global weapon field 14","Supplied global weapon field 19"}:new object[]{"Supplied global weapon field 0","Supplied global weapon field 17","Supplied global weapon field 18","Supplied global weapon field 22"},0);choices(visibility,"memberRequestVisibility",prepared==null?new object[]{"Supplied raw visibility byte 1","Supplied raw visibility byte 2","Supplied raw visibility byte 3"}:new object[]{"Produced visibility 1 (held)"},0);choices(queue,"memberRequestQueue",new object[]{"Empty copied request rows","9 occupied test slots per row","10 occupied test slots per row"},0);
            if(prepared!=null)equipment.Enabled=task.Enabled=difficulty.Enabled=comparison.Enabled=visibility.Enabled=false;
            load.Name="memberRequestLoad";load.Text="Load selected prepared copy";add(load,34);reset.Name="memberRequestReset";reset.Text="Reset applied copy before requests";add(reset,34);execute.Name="memberRequestExecute";execute.Text="Queue one member visit";add(execute,40);summary.Name="memberRequestSummary";add(summary,150);feedback.Name="memberRequestFeedback";add(feedback,86);
            add(new Label{Text="All occupied source request slots"},22);cells.Name="memberRequestCells";cells.HorizontalScrollbar=true;add(cells,170);cells.SelectedIndexChanged+=delegate{RefreshDetails();};details.Name="memberRequestDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,280);var close=new Button{Name="memberRequestClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            load.Click+=delegate{LoadCopy();};reset.Click+=delegate{if(initial!=null){state=initial;Feedback("Applied prepared copy restored; pending selectors are unchanged.");Sync();}};execute.Click+=delegate{if(state==null)return;try{state=state.Execute();Feedback("One member request visit complete. Request storage only; ammunition and target data are unchanged.");Sync();}catch(InvalidOperationException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{foreach(Keys k in new[]{Keys.Enter,Keys.Space})if((GetAsyncKeyState((int)k)&0x8000)!=0)held.Add(k);};LoadCopy();
        }
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){var b=ActiveControl as Button;if(b!=null){e.Handled=e.SuppressKeyPress=true;if(held.Add(e.KeyCode)&&b.Enabled)b.PerformClick();return;}}base.OnKeyDown(e);}
        void Feedback(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.LightSteelBlue;feedback.Text=text;}
        void Reject(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.Orange;feedback.Text="Rejected; applied copy preserved. "+text;Sync();}
        void LoadCopy(){try{var c=prepared==null?PracticeFleetMemberRequests.Create(programs,startup,equipment.SelectedIndex+3,task.SelectedIndex==0?0:8,difficulty.SelectedIndex==0?0:2,comparison.SelectedIndex,globalWeapon.SelectedIndex==0?0:globalWeapon.SelectedIndex==1?14:19,visibility.SelectedIndex+1,queue.SelectedIndex):PracticeFleetMemberRequests.CreateFromPreparation(prepared,new[]{0,17,18,22}[globalWeapon.SelectedIndex],queue.SelectedIndex);state=initial=c;Feedback(prepared==null?"Verified preparation prefix loaded with supplied queue/global inputs. No request visit has run yet.":"Completed preparation and produced flags held. Global weapon and tagged queue/timer fixtures are supplied; no visit has run.");Sync();}catch(ArgumentException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(UnauthorizedAccessException e){Reject(e.Message);}}
        void Sync()
        {
            if(IsDisposed||Disposing)return;reset.Enabled=state!=null;execute.Enabled=state!=null&&!state.Executed;if(state==null){summary.Text="No admitted member request copy loaded.";return;}ships=state.CopyShipPool();fleets=state.CopyFleetPool();summary.Text=(state.UsesProducedVisibility?"Original equipment / produced flags":"Applied: equipment "+state.EquipmentProfile)+" · task "+state.InitialTask+"\r\nDifficulty "+state.Difficulty+" · comparison "+state.ComparisonWord+"\r\nGlobal weapon "+state.GlobalWeapon+" · raw visibility "+state.Visibility+"\r\nRaw clock "+state.Clock+" · visit "+(state.Executed?"complete":"pending")+"\r\nPrepared states "+state.PreparedState(0)+" / "+state.PreparedState(1)+"\r\nCurrent states "+state.MemberState(0)+" / "+state.MemberState(1)+"\r\nOccupied slots "+state.QueuedCells.Count+" · RNG "+state.RandomState;
            cells.BeginUpdate();cells.Items.Clear();foreach(var cell in state.QueuedCells)cells.Items.Add(cell);if(cells.Items.Count>0)cells.SelectedIndex=0;cells.EndUpdate();RefreshDetails();
        }
        void RefreshDetails()
        {
            if(state==null||IsDisposed||Disposing)return;details.Text=(cells.SelectedItem==null?"No occupied request slot selected.":cells.SelectedItem.ToString())+"\r\nCyan = unit 30; orange = unit 31. White ring = viewed request. Marks can overlap; the list retains every slot. Test prefilled slots use Y tags 900+ and are not drawn on a 0..9 grid.\r\nRaw visibility 1 selects partial/neighbor/random search; 2 uses roster midpoints; 3 uses full-grid priority and fallback passes. This entry retains its displayed visibility inputs; the separate joined path uses produced flags.\r\nThe separate global weapon field affects the member state after requests. It is not silently replaced with the chosen unit weapon. Caller comparison, global weapon and queue fixtures remain diagnostic inputs.\r\nF2 may fill several slots; F3 stops after one enqueue attempt. Request storage does not check firing range, consume ammo or launch a projectile. Selected-unit UI is absent in this captured continuation.\r\nOne request visit only. Actual order execution, launch, impact, damage and full AI policy/world timing remain outside this study.";canvas.Invalidate();
        }
        sealed class RequestCanvas:Control
        {
            readonly PracticeFleetMemberRequestStudy owner;internal RequestCanvas(PracticeFleetMemberRequestStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);if(owner.state==null||Width<200||Height<300)return;int boards=owner.state.UsesProducedVisibility?1:2;float size=Math.Min(Width-70,(Height-164)/(float)boards),left=(Width-size)/2;
                for(int board=0;board<boards;board++)
                {
                    int fleet=owner.state.UsesProducedVisibility?15:14+board;float top=42+board*(size+64);PracticeFleetServiceGrid.Draw(e.Graphics,owner.Font,owner.ships,owner.fleets,-1,fleet,left,top,size,"Copied target fleet "+fleet+" · colored marks = requests");float cellSize=size/10;
                    foreach(var request in owner.state.QueuedCells)
                    {
                        if(request.TargetFleet!=fleet||request.X<0||request.X>9||request.Y<0||request.Y>9)continue;float x=left+(request.Y+.5f)*cellSize,y=top+(request.X+.5f)*cellSize,r=Math.Max(3,cellSize*.22f);using(var pen=new Pen(request.SourceUnit==30?Color.Cyan:Color.Orange,2)){e.Graphics.DrawLine(pen,x-r,y-r,x+r,y+r);e.Graphics.DrawLine(pen,x-r,y+r,x+r,y-r);}
                        var viewed=owner.cells.SelectedItem as PracticeFleetQueuedCell;if(viewed!=null&&viewed.SourceUnit==request.SourceUnit&&viewed.Slot==request.Slot)using(var pen=new Pen(Color.White,2))e.Graphics.DrawEllipse(pen,x-r-3,y-r-3,2*r+6,2*r+6);
                    }
                }
                e.Graphics.DrawString("Cyan = unit 30 · orange = unit 31 · white = viewed slot\r\nQueued requests only; no shots, ammo use or damage.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-48,Width-24,46));
            }
        }
    }
}
