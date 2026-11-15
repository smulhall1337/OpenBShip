using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace FleetLab
{
    // Manual, independent copy of one prepared paired bay. No source world clock.
    public sealed class PracticeYardStudy : Form
    {
        readonly string programs;
        PracticeYardStudyState state,initial;
        readonly ComboBox ship=new ComboBox(),marker=new ComboBox(),hull=new ComboBox(),ammo=new ComboBox(),supplies=new ComboBox();
        readonly CheckBox escort=new CheckBox();
        readonly Button admit=new Button(),one=new Button(),many=new Button(),reset=new Button(),load=new Button();
        readonly Label summary=new Label(),feedback=new Label(),profile=new Label(),admission=new Label();
        readonly TextBox details=new TextBox();
        readonly YardCanvas canvas;
        readonly HashSet<Keys> held=new HashSet<Keys>();
        static readonly int[] Types=
        {
            2,4,6,1,11,12
        };
        bool pending;
        internal PracticeYardStudyState StudyState
        {
            get
            {
                return state;
            }
        }
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
        void SuppressHeld()
        {
            foreach(var k in new[]
            {
                Keys.Enter,Keys.Space,Keys.Escape
            })if((GetAsyncKeyState((int)k)&0x8000)!=0)held.Add(k);
        }
        public PracticeYardStudy(string programs)
        {
            this.programs=programs;
            state=initial=PracticeYardStudyState.Create(programs,2,22,1,1,1,false);
            Text="Paired yard admission, supply and return · diagnostic copy";
            Name="yardStudy";
            StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1100,800);
            MinimumSize=new Size(900,680);
            BackColor=Color.FromArgb(12,23,35);
            ForeColor=Color.WhiteSmoke;
            Font=new Font("Segoe UI",9);
            KeyPreview=true;
            var layout=new TableLayoutPanel
            {
                Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,330));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,86));
            Controls.Add(layout);
            canvas=new YardCanvas(this)
            {
                Dock=DockStyle.Fill,Name="yardCanvas",AccessibleName="Schematic source fleet and paired yard grids",TabStop=true
            };
            layout.Controls.Add(canvas,0,0);
            var scroll=new Panel
            {
                Dock=DockStyle.Fill,AutoScroll=true,Name="yardScroll"
            };
            layout.Controls.Add(scroll,1,0);
            var side=new FlowLayoutPanel
            {
                FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(4)
            };
            scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control c,int height)
            {
                c.Width=292;
                c.Height=height;
                c.Margin=new Padding(0,3,0,3);
                var b=c as Button;
                if(b!=null)
                {
                    b.UseVisualStyleBackColor=false;
                    b.FlatStyle=FlatStyle.Flat;
                    b.FlatAppearance.BorderColor=Color.SteelBlue;
                    b.BackColor=Color.FromArgb(28,55,76);
                    b.ForeColor=Color.WhiteSmoke;
                    b.EnabledChanged+=delegate
                    {
                        if(b.IsDisposed||b.Disposing)return;
                        b.BackColor=b.Enabled?Color.FromArgb(28,55,76):Color.LightGray;
                        b.ForeColor=b.Enabled?Color.WhiteSmoke:Color.Black;
                    };
                }
                side.Controls.Add(c);
            };
            Action<Button,string,string> button=delegate(Button b,string name,string text)
            {
                b.Name=name;
                b.Text=text;
                b.AccessibleName=text;
                add(b,36);
            };
            summary.Name="yardSummary";
            summary.AccessibleName="Applied yard phase and ship state";
            add(summary,104);
            button(admit,"yardAdmit","Admit prepared ship");
            button(one,"yardOne","One recovered worker pass");
            button(many,"yardMany","Up to 20 passes / stop on return");
            button(reset,"yardReset","Reset applied profile");
            add(new Label
            {
                Text="Manual calls only. Repair and rearming use recovered rules; release can restore full HP before all intermediate cell states finish. One weapon transfer per pass. No world time advances."
            },102);
            Action<ComboBox,string,string,object[],int> choice=delegate(ComboBox c,string name,string title,object[] values,int selected)
            {
                add(new Label
                {
                    Text=title
                },22);
                c.Name=name;
                c.AccessibleName=title;
                c.DropDownStyle=ComboBoxStyle.DropDownList;
                c.Items.AddRange(values);
                c.SelectedIndex=selected;
                add(c,30);
            };
            choice(ship,"yardShip","Supplied source ship",new object[]
            {
                "Type 2 · length 3","Type 4 · length 2","Type 6 · length 4","Type 1 · length 5","Type 11 · length 3 / fleet kind 9","Type 12 · length 4 / fleet kind 9"
            },0);
            choice(marker,"yardMarker","Paired marker capacity",new object[]
            {
                "Type 22 · capacity 4","Type 23 · capacity 5"
            },0);
            choice(hull,"yardHull","Supplied hull / cell state",new object[]
            {
                "Full HP / intact cells","One state 8 / HP length−1","Mixed cells / HP 0"
            },1);
            choice(ammo,"yardAmmo","Supplied ship ammunition",new object[]
            {
                "Original constructor quantities","Empty"
            },1);
            choice(supplies,"yardSupplies","Supplied depot inventory",new object[]
            {
                "Empty","Recovered startup stock table"
            },1);
            escort.Name="yardEscort";
            escort.Text="Keep a second ship in source fleet";
            escort.AccessibleName=escort.Text;
            add(escort,28);
            button(load,"yardLoad","Load selected profile");
            profile.Name="yardProfile";
            profile.AccessibleName="Pending yard profile";
            add(profile,54);
            admission.Name="yardAdmission";
            admission.AccessibleName="Read-only original admission gate";
            add(admission,104);
            details.Name="yardDetails";
            details.AccessibleName="Ammunition, inventory and source context";
            details.Multiline=true;
            details.ReadOnly=true;
            details.ScrollBars=ScrollBars.Vertical;
            details.BackColor=Color.FromArgb(20,35,48);
            details.ForeColor=Color.WhiteSmoke;
            add(details,250);
            add(new Label
            {
                Text="One prepared paired bay: blank unit 100 before marker 101, depot 102, local owner 0. This is separate from map placement, discovery, missions and the guarded general support caller. Three source display endpoints are counted, not rendered."
            },130);
            var close=new Button();
            button(close,"yardClose","Close study");
            close.Click+=delegate
            {
                Close();
            };
            feedback.Name="yardFeedback";
            feedback.AccessibleName="Yard action feedback";
            feedback.Dock=DockStyle.Fill;
            feedback.Padding=new Padding(4,8,4,0);
            layout.Controls.Add(feedback,0,1);
            layout.SetColumnSpan(feedback,2);
            EventHandler changed=delegate
            {
                pending=true;
                RefreshView();
                feedback.Text="Profile selection is pending. Load applies it to a fresh copy; Reset restores the applied profile.";
            };
            foreach(var c in new[]
            {
                ship,marker,hull,ammo,supplies
            })c.SelectedIndexChanged+=changed;
            escort.CheckedChanged+=changed;
            load.Click+=delegate
            {
                try
                {
                    var next=PracticeYardStudyState.Create(this.programs,Types[ship.SelectedIndex],marker.SelectedIndex==0?22:23,hull.SelectedIndex,ammo.SelectedIndex,supplies.SelectedIndex,escort.Checked);
                    state=initial=next;
                    pending=false;
                    RefreshView();
                    feedback.Text="Fresh selected profile loaded. Admit the ship, then apply worker passes.";
                }
                catch(ArgumentException e)
                {
                    feedback.Text=e.Message;
                }
                catch(InvalidOperationException e)
                {
                    feedback.Text=e.Message;
                }
                catch(IOException e)
                {
                    feedback.Text=e.Message;
                }
                catch(NotSupportedException e)
                {
                    feedback.Text=e.Message;
                }
            };
            reset.Click+=delegate
            {
                state=initial;
                RefreshView();
                feedback.Text="Applied profile restored before admission. Pending selectors have not been applied.";
            };
            admit.Click+=delegate
            {
                state=state.Enter();
                RefreshView();
                feedback.Text="Ship admitted to yard fleet 34. Its source mapping and yard cells are saved for return.";
            };
            one.Click+=delegate
            {
                Advance(1);
            };
            many.Click+=delegate
            {
                Advance(20);
            };
            KeyUp+=delegate(object s,KeyEventArgs e)
            {
                held.Remove(e.KeyCode);
            };
            Deactivate+=delegate
            {
                held.Clear();
            };
            Activated+=delegate
            {
                SuppressHeld();
            };
            RefreshView();
            feedback.Text="Default profile: type 2, one damaged cell, empty ammunition and recovered depot stock. Admit → one pass → up to 20 passes.";
        }
        void Advance(int maximum)
        {
            int count=0;
            while(count<maximum&&!state.Returned)
            {
                state=state.WorkerStep();
                count++;
            }
            RefreshView();
            feedback.Text=count+" worker pass(es) applied. "+(state.Returned?"Ship returned to source fleet 7; further passes are disabled. Reset permits another comparison.":"Ship remains in the paired bay. Another pass may transfer ammunition or release it.");
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(e.KeyCode!=Keys.Enter&&e.KeyCode!=Keys.Space&&e.KeyCode!=Keys.Escape)return;
            var b=ActiveControl as Button;
            if(e.KeyCode!=Keys.Escape&&b==null)return;
            e.Handled=true;
            e.SuppressKeyPress=true;
            if(!held.Add(e.KeyCode))return;
            if(e.KeyCode==Keys.Escape)Close();
            else if(b.Enabled)b.PerformClick();
        }
        static int I(byte[] b,int p)
        {
            return BitConverter.ToInt32(b,p);
        }
        static int W(byte[] b,int p)
        {
            return BitConverter.ToInt16(b,p);
        }
        void RefreshView()
        {
            if(IsDisposed||Disposing)return;
            var s=state.CopyShipPool();
            var f=state.CopyFleetPool();
            string phase=!state.Entered?"Prepared":state.Returned?"Returned to source":"Inside paired bay";
            summary.Text=phase+" / worker passes "+state.WorkerCalls+"\r\nUnit 0 · type "+I(s,0)+" · HP "+W(s,16)+" / "+W(s,14)+"\r\nParent fleet "+W(s,12)+" · source fleet kind "+I(f,7*492)+"\r\nLocal "+W(s,8)+", "+W(s,10);
            admit.Enabled=!state.Entered;
            one.Enabled=many.Enabled=state.Entered&&!state.Returned;
            profile.Text=pending?"Selection pending until Load. Reset uses the applied profile.":"Selectors match the applied profile.";
            var gate=state.CheckAdmission();
            admission.Text="Read-only source gate "+gate.SourceReturn+" ("+(gate.Available?"available":"unavailable")+").\r\n"+gate.Reason+"\r\nAvailability alone does not establish safe entry.";
            var text=new StringBuilder();
            text.AppendLine("Ammo ID: ship / depot (nonzero values)");
            for(int n=0;n<35;n++)
            {
                int a=W(s,196+n*2),stock=W(s,102*268+196+n*2);
                if(a!=0||stock!=0)text.AppendLine(n+": "+a+" / "+stock);
            }
            text.AppendLine();
            text.AppendLine("Source selected fleet: "+state.SelectedFleet+" / unit: "+state.SelectedUnit);
            text.AppendLine("Display requests since prepared:");
            text.AppendLine("text "+state.TextRequests+" / map "+state.MapRequests+" / refresh "+state.RefreshRequests);
            text.AppendLine("These counters are not actual original UI playback.");
            text.AppendLine("Source reserved flag: "+f[7*492+490]+" / source state: "+I(f,7*492+472));
            text.AppendLine("Yard block state: "+I(f,32*492+472));
            details.Text=text.ToString();
            canvas.Invalidate();
        }
        sealed class YardCanvas:Control
        {
            readonly PracticeYardStudy owner;
            public YardCanvas(PracticeYardStudy owner)
            {
                this.owner=owner;
                DoubleBuffered=true;
                ResizeRedraw=true;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g=e.Graphics;
                var s=owner.state.CopyShipPool();
                var f=owner.state.CopyFleetPool();
                g.Clear(owner.BackColor);
                using(var white=new SolidBrush(Color.WhiteSmoke))using(var muted=new SolidBrush(Color.LightSteelBlue))using(var gold=new Pen(Color.Gold,3))using(var line=new Pen(Color.SteelBlue))
                {
                    g.DrawString("SUPPLIED SOURCE FLEET / PAIRED YARD",Font,white,new RectangleF(8,8,Width-16,38));
                    int cell=Math.Max(12,Math.Min(32,(Width-62)/20)),size=cell*10,top=88;
                    for(int board=0;board<2;board++)
                    {
                        int left=14+board*(size+30),fleet=board==0?7:34;
                        g.DrawString(board==0?"Source fleet 7":"Yard fleet 34",Font,white,left,50);
                        g.DrawString("kind "+I(f,fleet*492)+" · X↓ Y→",Font,muted,left,69);
                        for(int x=0;x<10;x++)for(int y=0;y<10;y++)
                        {
                            int value=I(f,fleet*492+64+(x*10+y)*4);
                            var rect=new Rectangle(left+y*cell,top+x*cell,cell,cell);
                            Color color=value==0?Color.FromArgb(17,36,52):value==2?Color.FromArgb(34,94,110):value==3?Color.FromArgb(44,48,58):Color.FromArgb(122,61,48);
                            using(var fill=new SolidBrush(color))g.FillRectangle(fill,rect);
                            g.DrawRectangle(line,rect);
                            if(cell>=20&&value!=0)g.DrawString(value.ToString(),Font,white,rect.Left+3,rect.Top+2);
                        }
                        if(W(s,12)==fleet)
                        {
                            int x=W(s,8),y=W(s,10),length=W(s,14);
                            g.DrawRectangle(gold,left+y*cell+2,top+x*cell+2,length*cell-4,cell-4);
                        }
                    }
                    int below=top+size+18;
                    string phase=!owner.state.Entered?"Prepared at source":owner.state.Returned?"Returned to source":"Admitted to paired bay";
                    var gate=owner.state.CheckAdmission();
                    g.DrawString(phase+" · HP "+W(s,16)+" / "+W(s,14)+" · worker passes "+owner.state.WorkerCalls+"\nRead-only gate "+gate.SourceReturn+" ("+(gate.Available?"available":"unavailable")+")",Font,white,new RectangleF(10,below,Width-20,46));
                    g.DrawString("Gold: target ship. Numbers: original cell states.\nTeal: state 2; red: supplied damage; gray: yard background.\nCoordinates retain the source X-row / Y-column convention.",Font,muted,new RectangleF(10,below+50,Width-20,82));
                    g.DrawString("Schematic diagnostic grids; placement is supplied.\nThis window runs explicit admission and support calls, with no world clock, discovery or mission execution.",Font,muted,new RectangleF(10,below+140,Width-20,100));
                }
            }
        }
    }
}
