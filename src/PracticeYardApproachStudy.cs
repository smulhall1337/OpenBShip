using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace FleetLab
{
    public sealed class PracticeYardApproachStudy : Form
    {
        readonly string programs;
        readonly PracticeMapStartup startup;
        readonly ComboBox target=new ComboBox(),profile=new ComboBox();
        readonly Button load=new Button(),reset=new Button(),one=new Button(),until=new Button(),open=new Button();
        readonly Label summary=new Label(),feedback=new Label();
        readonly ApproachCanvas canvas;
        readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeYardApproachState state,initial;
        byte[] fleets,terrain;
        internal PracticeYardApproachState StudyState { get { return state; } }
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeYardApproachStudy(string programs,PracticeMapStartup startup,int preferredTarget)
        {
            this.programs=programs;this.startup=startup;
            Name="yardApproachStudy";Text="BSSB202 nearby yard approach · composed study";StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1140,800);MinimumSize=new Size(960,700);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,350));Controls.Add(layout);
            canvas=new ApproachCanvas(this){Name="yardApproachCanvas",Dock=DockStyle.Fill};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Name="yardApproachScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);
            var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control control,int height)
            {
                control.Width=316;control.Height=height;control.Margin=new Padding(0,3,0,3);
                if(control is Button||control is ComboBox){control.BackColor=Color.FromArgb(28,55,76);control.ForeColor=ForeColor;}
                var button=control as Button;if(button!=null){button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;button.EnabledChanged+=delegate{if(button.IsDisposed||button.Disposing)return;button.BackColor=button.Enabled?Color.FromArgb(28,55,76):Color.LightGray;button.ForeColor=button.Enabled?ForeColor:Color.Black;};}
                side.Controls.Add(control);
            };
            add(new Label{Text="Supplied nearby start 622,600 and waypoint 610,600. Only the chosen surface fleet moves. Recovered movement produces the collision byte used by yard admission; it starts at −1. BSSB202's 53 terrain records are included."},104);
            add(new Label{Text="Target / profile (pending until Load)"},22);
            target.Name="yardApproachTarget";target.DropDownStyle=ComboBoxStyle.DropDownList;target.Items.AddRange(new object[]{"30 · length 5 · fleet 16","31 · length 3 · fleet 16","34 · length 2 · fleet 18"});target.SelectedIndex=Array.IndexOf(new[]{30,31,34},preferredTarget);if(target.SelectedIndex<0)target.SelectedIndex=0;add(target,30);
            profile.Name="yardApproachProfile";profile.DropDownStyle=ComboBoxStyle.DropDownList;profile.Items.AddRange(new object[]{"Intact · original ammunition","One state 8 · HP length−1 · empty ammo","Mixed states · HP 0 · empty ammo"});profile.SelectedIndex=1;add(profile,30);
            load.Name="yardApproachLoad";load.Text="Load nearby test copy";add(load,34);
            reset.Name="yardApproachReset";reset.Text="Reset applied approach";add(reset,34);
            summary.Name="yardApproachSummary";add(summary,140);
            feedback.Name="yardApproachFeedback";add(feedback,96);
            one.Name="yardApproachOne";one.Text="One recovered movement visit";add(one,34);
            until.Name="yardApproachUntil";until.Text="Up to 20 visits / stop when admitted";add(until,34);
            open.Name="yardApproachOpen";open.Text="Open admitted yard copy…";add(open,36);
            add(new Label{Text="This is a deliberate composition of recovered routines, not a replay of the full original approach. Other fleets stay fixed. No AI, world clock, travel from the original spawn or automatic transfer runs. The yard opens another copy."},110);
            var close=new Button{Name="yardApproachClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            target.SelectedIndexChanged+=delegate{Pending();};profile.SelectedIndexChanged+=delegate{Pending();};
            load.Click+=delegate{LoadCopy(new[]{30,31,34}[target.SelectedIndex],profile.SelectedIndex);};
            reset.Click+=delegate{if(initial!=null){state=initial;Feedback("Applied nearby start restored; pending selectors are unchanged.");Sync();}};
            one.Click+=delegate{Change(delegate{return state.Step();});};until.Click+=delegate{Change(delegate{return state.UntilAdmission();});};
            open.Click+=delegate{OpenYard();};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{held.Clear();};
            Activated+=delegate{foreach(var key in new[]{Keys.Enter,Keys.Space,Keys.Escape})if((GetAsyncKeyState((int)key)&0x8000)!=0)held.Add(key);};
            LoadCopy(new[]{30,31,34}[target.SelectedIndex],profile.SelectedIndex);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);if(e.KeyCode!=Keys.Enter&&e.KeyCode!=Keys.Space&&e.KeyCode!=Keys.Escape)return;var button=ActiveControl as Button;if(e.KeyCode!=Keys.Escape&&button==null)return;
            e.Handled=e.SuppressKeyPress=true;if(!held.Add(e.KeyCode))return;if(e.KeyCode==Keys.Escape)Close();else if(button.Enabled)button.PerformClick();
        }
        void Pending(){if(state!=null)Feedback("Target/profile selection is pending. Load applies it; Reset keeps the applied copy.");}
        void Feedback(string text){feedback.ForeColor=ForeColor;feedback.Text=text;}
        void LoadCopy(int unit,int variant)
        {
            try{var next=PracticeYardApproachState.Create(programs,startup,unit,variant);state=initial=next;Feedback("Start supplied near the recovered installation. No yard is selected. Apply movement visits to reach a collision and test admission.");Sync();}
            catch(InvalidDataException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(ArgumentException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(UnauthorizedAccessException e){Reject(e.Message);}
        }
        void Change(Func<PracticeYardApproachState> action)
        {
            try{state=action();Feedback(state.CheckAdmission().Available?"Movement produced an admitted yard. Open its copy, then choose Enter copied yard to apply the transfer command.":"Movement visit applied. The admission gate is still closed.");Sync();}
            catch(InvalidOperationException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}
        }
        void OpenYard()
        {
            if(state==null||!state.CheckAdmission().Available)return;
            try
            {
                using(var yard=new PracticeMapYardStudy(programs,startup,state.TargetUnit,state.CreateYardCopy()))yard.ShowDialog(this);
                Feedback("Yard copy closed. Approach position, visit count and parent startup snapshots are unchanged.");Sync();
            }
            catch(InvalidOperationException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}
        }
        void Reject(string reason){feedback.ForeColor=Color.Orange;feedback.Text="Rejected; existing approach preserved. "+reason;Sync();}
        void Sync()
        {
            if(IsDisposed||Disposing)return;reset.Enabled=state!=null;
            bool admitted=state!=null&&state.CheckAdmission().Available;one.Enabled=until.Enabled=state!=null&&!admitted&&state.Visits<PracticeYardApproachState.MaximumVisits;open.Enabled=admitted;
            if(state==null){summary.Text="No supported nearby approach loaded.";return;}
            fleets=state.CopyFleetPool();terrain=state.CopyTerrainPool();var gate=state.CheckAdmission();
            summary.Text="Applied unit "+state.TargetUnit+" · profile "+state.Profile+" · fleet "+state.Fleet+"\r\nMovement visits: "+state.Visits+" / 20\r\nWorld: "+state.Position.X+", "+state.Position.Y+"\r\nCollision / yard byte: "+state.Collision+"\r\nOutcome: "+(state.LastOutcome.HasValue?state.LastOutcome.Value.ToString():"Not visited")+"\r\nAdmission: "+(gate.Available?"available · bay fleet "+gate.MarkerFleet:"unavailable");canvas.Invalidate();
        }
        sealed class ApproachCanvas : Control
        {
            readonly PracticeYardApproachStudy owner;
            internal ApproachCanvas(PracticeYardApproachStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);if(owner.state==null||Width<100||Height<250)return;var g=e.Graphics;
                float size=Math.Min(Width-74,Height-164),left=(Width-size)/2,top=55,scale=size/60;
                using(var background=new SolidBrush(Color.FromArgb(20,44,64)))g.FillRectangle(background,left,top,size,size);
                for(int n=0;n<=6;n++)
                {
                    using(var pen=new Pen(Color.FromArgb(47,74,91))){g.DrawLine(pen,left+n*10*scale,top,left+n*10*scale,top+size);g.DrawLine(pen,left,top+n*10*scale,left+size,top+n*10*scale);}
                    g.DrawString((580+n*10).ToString(),owner.Font,Brushes.LightSteelBlue,left+n*10*scale-10,top-22);
                    g.DrawString((570+n*10).ToString(),owner.Font,Brushes.LightSteelBlue,left-33,top+n*10*scale-5);
                }
                for(int n=0;n<500;n++)if(BitConverter.ToInt32(owner.terrain,n*492)==1)
                {
                    int x=BitConverter.ToInt16(owner.terrain,n*492+12),y=BitConverter.ToInt16(owner.terrain,n*492+14);
                    if(x>=580&&x<640&&y>=570&&y<630)g.FillRectangle(Brushes.DarkOliveGreen,left+(x-580)*scale,top+(y-570)*scale,10*scale,10*scale);
                }
                for(int n=0;n<4;n++)
                {
                    int at=n*492,x=BitConverter.ToInt16(owner.fleets,at+12),y=BitConverter.ToInt16(owner.fleets,at+14);
                    var rectangle=new RectangleF(left+(x-580)*scale,top+(y-570)*scale,10*scale,10*scale);
                    using(var pen=new Pen(Color.Coral,2))g.DrawRectangle(pen,rectangle.X,rectangle.Y,rectangle.Width,rectangle.Height);
                    g.DrawString("Base "+n,owner.Font,Brushes.Coral,rectangle.X+3,rectangle.Y+3);
                }
                Point position=owner.state.Position;float px=left+(position.X-580)*scale,py=top+(position.Y-570)*scale;
                float wx=left+30*scale,wy=top+30*scale;
                using(var pen=new Pen(Color.LightGreen,2)){g.DrawLine(pen,wx-6,wy,wx+6,wy);g.DrawLine(pen,wx,wy-6,wx,wy+6);g.DrawLine(pen,px,py,wx,wy);}
                using(var pen=new Pen(Color.Gold,3))g.DrawRectangle(pen,px,py,10*scale,10*scale);
                g.DrawString("Fleet "+owner.state.Fleet,owner.Font,Brushes.Gold,px+3,py+20);
                g.DrawString("Nearby world view · X across / Y down\r\nGold = selected surface fleet; coral = recovered base fleets.\r\nGreen cross = supplied waypoint. Rectangles are 10 world units.\r\n53 recovered terrain records loaded; only this surface fleet advances.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-88,Math.Max(1,Width-24),84));
            }
        }
    }
}
