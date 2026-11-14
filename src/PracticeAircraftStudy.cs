using System;
using System.IO;
using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
namespace FleetLab
{
    public sealed class PracticeAircraftStudy : Form
    {
        sealed class Choice
        {
            internal int Id;
            internal string Text;
            public override string ToString()
            {
                return Text;
            }
        }
        readonly PracticeAircraftStudyState initial;
        PracticeAircraftStudyState state;
        readonly ClassicMessages names;
        readonly ComboBox parent=new ComboBox(),kind=new ComboBox(),mode=new ComboBox();
        readonly NumericUpDown link=new NumericUpDown();
        readonly Button create=new Button(),activate=new Button(),dock=new Button(),reset=new Button();
        readonly Button combat=new Button();
        readonly HashSet<Keys> held=new HashSet<Keys>();
        readonly ListBox members=new ListBox();
        readonly TextBox details=new TextBox();
        readonly Label summary=new Label(),feedback=new Label();
        readonly AircraftCanvas canvas;
        internal PracticeAircraftStudyState StudyState
        {
            get
            {
                return state;
            }
        }
        internal int SelectedMember
        {
            get
            {
                return members.SelectedIndex;
            }
        }
        internal RectangleF GridBounds
        {
            get
            {
                return canvas.GridBounds;
            }
        }
        public PracticeAircraftStudy(string assets,string programs,uint seed,ClassicMessages messages)
        {
            names=messages;
            var catalog=PracticeUnitCatalog.Load(programs);
            var deployment=PracticeDeployment.FirstPractice(catalog,File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB201.SCN")),seed);
            state=initial=new PracticeAircraftStudyState(catalog,PracticeAircraftTables.Load(programs),deployment);
            Text="Recovered aircraft data · seed "+seed;
            Name="aircraftStudy";
            StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1060,800);
            MinimumSize=new Size(860,640);
            BackColor=Color.FromArgb(12,23,35);
            ForeColor=Color.WhiteSmoke;
            Font=new Font("Segoe UI",9);
            KeyPreview=true;
            var layout=new TableLayoutPanel
            {
                Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,320));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,72));
            Controls.Add(layout);
            canvas=new AircraftCanvas(this)
            {
                Dock=DockStyle.Fill,TabStop=true,Name="aircraftGrid",AccessibleName="Schematic aircraft roster grid"
            };
            layout.Controls.Add(canvas,0,0);
            var scroll=new Panel
            {
                Dock=DockStyle.Fill,AutoScroll=true
            };
            layout.Controls.Add(scroll,1,0);
            var side=new FlowLayoutPanel
            {
                FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(4)
            };
            scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control c,int height)
            {
                c.Width=282;
                c.Height=height;
                c.Margin=new Padding(0,3,0,3);
                var button=c as Button;
                if(button!=null)
                {
                    button.UseVisualStyleBackColor=false;
                    button.FlatStyle=FlatStyle.Flat;
                    button.FlatAppearance.BorderColor=Color.SteelBlue;
                    Action tint=delegate
                    {
                        button.BackColor=button.Enabled?Color.FromArgb(28,55,76):Color.LightGray;
                        button.ForeColor=button.Enabled?Color.WhiteSmoke:Color.Black;
                    };
                    button.EnabledChanged+=delegate
                    {
                        tint();
                    };
                    tint();
                }
                side.Controls.Add(c);
            };
            add(new Label
            {
                Text="One supplied attachment in a copy of the first Practice map. Parent selection is a test input; the original attachment-discovery caller is still being recovered."
            },72);
            add(new Label
            {
                Text="Parent unit"
            },22);
            parent.Name="aircraftParent";
            parent.AccessibleName="Supplied parent unit";
            parent.DropDownStyle=ComboBoxStyle.DropDownList;
            add(parent,30);
            var ships=initial.CopyShipPool();
            for(int n=0;n<1500;n++)
            {
                int type=BitConverter.ToInt32(ships,n*268);
                if(type<1||type>14)continue;
                parent.Items.Add(new Choice
                {
                    Id=n,Text="Unit "+n+" · "+UnitName(type)+" · player "+(BitConverter.ToInt16(ships,n*268+6)+1)
                });
            }
            if(parent.Items.Count>0)parent.SelectedIndex=0;
            add(new Label
            {
                Text="Stored group"
            },22);
            kind.Name="aircraftKind";
            kind.AccessibleName="Aircraft group type";
            kind.DropDownStyle=ComboBoxStyle.DropDownList;
            kind.Items.AddRange(new object[]
            {
                new Choice
                {
                    Id=6,Text="Helicopter · four units"
                },new Choice
                {
                    Id=7,Text="Plane · four units"
                },new Choice
                {
                    Id=8,Text="AWACS · one unit"
                }
            });
            kind.SelectedIndex=0;
            add(kind,30);
            add(new Label
            {
                Text="Parent link slot (0–3)"
            },22);
            link.Name="aircraftLink";
            link.AccessibleName="Supplied parent link slot";
            link.Minimum=0;
            link.Maximum=3;
            add(link,30);
            create.Text="Create stored group";
            create.Name="aircraftCreate";
            create.AccessibleName=create.Text;
            add(create,34);
            add(new Label
            {
                Text="Recovered mode (raw source value)"
            },22);
            mode.Name="aircraftMode";
            mode.AccessibleName="Aircraft activation mode";
            mode.DropDownStyle=ComboBoxStyle.DropDownList;
            for(int n=1;n<=5;n++)mode.Items.Add("Mode "+n);
            mode.SelectedIndex=0;
            add(mode,30);
            activate.Text="Activate selected mode";
            activate.Name="aircraftActivate";
            activate.AccessibleName=activate.Text;
            add(activate,34);
            dock.Text="Dock group";
            dock.Name="aircraftDock";
            dock.AccessibleName=dock.Text;
            add(dock,34);
            combat.Text="Timed combat study…";
            combat.Name="aircraftCombat";
            combat.AccessibleName=combat.Text;
            combat.AccessibleDescription="Activate a group, then open copied AA projectile travel and impact controls.";
            add(combat,36);
            summary.Name="aircraftSummary";
            summary.AccessibleName="Aircraft group state";
            add(summary,112);
            members.Name="aircraftMembers";
            members.AccessibleName="Aircraft members";
            members.IntegralHeight=false;
            add(members,100);
            details.Name="aircraftDetails";
            details.AccessibleName="Selected aircraft data";
            details.Multiline=true;
            details.ReadOnly=true;
            details.ScrollBars=ScrollBars.Vertical;
            details.BackColor=BackColor;
            details.ForeColor=ForeColor;
            add(details,228);
            reset.Text="Reset study";
            reset.Name="aircraftReset";
            reset.AccessibleName=reset.Text;
            add(reset,34);
            var close=new Button
            {
                Text="Close",Name="aircraftClose",DialogResult=DialogResult.Cancel
            };
            add(close,34);
            CancelButton=close;
            feedback.Name="aircraftFeedback";
            feedback.Dock=DockStyle.Fill;
            feedback.ForeColor=Color.LightSteelBlue;
            feedback.AccessibleName="Aircraft study feedback";
            layout.Controls.Add(feedback,0,1);
            layout.SetColumnSpan(feedback,2);
            create.Click+=delegate
            {
                Act(delegate
                {
                    state=state.Create(((Choice)parent.SelectedItem).Id,((Choice)kind.SelectedItem).Id,(int)link.Value);
                },"Stored group created. Inspect a numbered unit, then activate a mode.");
            };
            activate.Click+=delegate
            {
                Act(delegate
                {
                    state=state.Transition(mode.SelectedIndex+1);
                },"Mode applied. Flight, automatic targeting and missions are not running in this study.");
            };
            dock.Click+=delegate
            {
                Act(delegate
                {
                    state=state.Transition(0);
                },"Group docked. The source retains its task, equipment and ammunition.");
            };
            combat.Click+=delegate
            {
                if(!combat.Enabled)return;
                try
                {
                    using(var study=new PracticeAircraftCombatStudy(programs,state,names))study.ShowDialog(this);
                }
                catch(InvalidDataException e)
                {
                    feedback.Text=e.Message;
                }
                catch(IOException e)
                {
                    feedback.Text="Run Setup again to import current combat tables. "+e.Message;
                }
                catch(UnauthorizedAccessException e)
                {
                    feedback.Text=e.Message;
                }
                held.Clear();
            };
            reset.Click+=delegate
            {
                state=initial;
                RefreshState();
                feedback.Text="Starting pools restored. Your battle, map preview and settings were preserved.";
            };
            members.SelectedIndexChanged+=delegate
            {
                UpdateDetails();
            };
            KeyUp+=delegate(object sender,KeyEventArgs e)
            {
                held.Remove(e.KeyCode);
            };
            Deactivate+=delegate
            {
                held.Clear();
            };
            canvas.KeyDown+=delegate(object sender,KeyEventArgs e)
            {
                if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Up||e.KeyCode==Keys.Right||e.KeyCode==Keys.Down)
                {
                    e.Handled=true;
                    e.SuppressKeyPress=true;
                    if(members.Items.Count>0)
                    {
                        int step=e.KeyCode==Keys.Left||e.KeyCode==Keys.Up?-1:1;
                        members.SelectedIndex=Math.Max(0,Math.Min(members.Items.Count-1,members.SelectedIndex+step));
                    }
                }
            };
            RefreshState();
            feedback.Text="Create a group to compare its recovered records. The schematic shows local roster placement, not live flight.";
        }
        string UnitName(int type)
        {
            return names==null||type<0||type>12?"type "+type:names.ShipName(type);
        }
        static Button FocusedButton(Control c)
        {
            foreach(Control child in c.Controls)
            {
                if(child.Focused)return child as Button;
                var nested=FocusedButton(child);
                if(nested!=null)return nested;
            }
            return null;
        }
        protected override bool ProcessCmdKey(ref Message m,Keys keys)
        {
            var button=FocusedButton(this);
            if(keys==Keys.Enter&&button!=null)
            {
                bool repeat=(m.LParam.ToInt64()&0x40000000L)!=0;
                if(!repeat&&held.Add(Keys.Enter)&&button.Enabled)button.PerformClick();
                return true;
            }
            return base.ProcessCmdKey(ref m,keys);
        }
        void Act(Action action,string message)
        {
            try
            {
                action();
                RefreshState();
                feedback.Text=message;
            }
            catch(InvalidDataException e)
            {
                feedback.Text=e.Message;
            }
            catch(NotSupportedException e)
            {
                feedback.Text=e.Message;
            }
            catch(InvalidOperationException e)
            {
                feedback.Text=e.Message;
            }
        }
        void RefreshState()
        {
            int selected=members.SelectedIndex;
            members.Items.Clear();
            foreach(var unit in state.Members)members.Items.Add("Unit "+unit.Slot+" · "+UnitName(unit.Type));
            if(members.Items.Count>0)members.SelectedIndex=Math.Max(0,Math.Min(selected,members.Items.Count-1));
            parent.Enabled=kind.Enabled=link.Enabled=create.Enabled=!state.HasGroup&&parent.Items.Count>0;
            activate.Enabled=mode.Enabled=state.HasGroup&&state.Stored;
            dock.Enabled=state.HasGroup&&!state.Stored;
            combat.Enabled=state.HasGroup&&!state.Stored;
            if(!state.HasGroup)summary.Text="No aircraft group created.\r\nReset restores the copied starting pools.";
            else
            {
                var f=state.CopyFleetPool();
                int at=state.FleetSlot*492;
                summary.Text="Fleet "+state.FleetSlot+" · "+(state.Stored?"Stored":"Active")+" · kind "+state.Kind+"\r\nParent unit "+state.ParentShip+" · link "+state.Link+"\r\nWorld "+BitConverter.ToInt16(f,at+12)+", "+BitConverter.ToInt16(f,at+14)+" · speed "+BitConverter.ToInt16(f,at+60)+"\r\nTask "+state.Mode+" · visibility "+f[at+464]+"/"+f[at+465]+"/"+f[at+466];
            }
            UpdateDetails();
            canvas.Invalidate();
        }
        void UpdateDetails()
        {
            if(members.SelectedIndex<0||members.SelectedIndex>=state.Members.Count)
            {
                details.Text="Select an aircraft unit.";
                canvas.Invalidate();
                return;
            }
            var unit=state.Members[members.SelectedIndex];
            var b=unit.CopyRecord();
            var text=new StringBuilder();
            text.AppendLine("Unit "+unit.Slot+" · "+UnitName(unit.Type));
            text.AppendLine("Local "+unit.X+", "+unit.Y+" · length "+unit.Length+" · HP "+unit.HitPoints);
            text.AppendLine("Speed "+unit.Speed+" · source field96 "+BitConverter.ToInt16(b,96));
            text.AppendLine("Selections "+unit.SelectedEquipmentA+" / "+unit.SelectedEquipmentB+" / "+unit.SelectedEquipmentC);
            text.AppendLine("Equipment rows:");
            for(int row=0;row<unit.Length;row++)
            {
                text.Append((row+1)+": ");
                for(int n=0;n<4;n++)text.Append(BitConverter.ToInt32(b,100+(row*4+n)*4)).Append(n==3?"":" / ");
                text.AppendLine();
            }
            text.AppendLine("Ammunition (weapon:count):");
            bool any=false;
            for(int w=0;w<35;w++)if(unit.Ammunition(w)!=0)
            {
                text.Append(w).Append(':').Append(unit.Ammunition(w)).Append(' ');
                any=true;
            }
            if(!any)text.Append("none");
            details.Text=text.ToString();
            details.AccessibleDescription=details.Text;
            canvas.Invalidate();
        }
        void Pick(Point p)
        {
            var b=canvas.GridBounds;
            if(!b.Contains(p)||b.Width<10)return;
            float cell=b.Width/10;
            int x=(int)((p.X-b.X)/cell),y=(int)((p.Y-b.Y)/cell);
            var roster=state.Members;
            for(int n=0;n<roster.Count;n++)if(roster[n].X==x&&y>=roster[n].Y&&y<roster[n].Y+roster[n].Length)
            {
                members.SelectedIndex=n;
                return;
            }
        }
        sealed class AircraftCanvas : Control
        {
            readonly PracticeAircraftStudy owner;
            internal AircraftCanvas(PracticeAircraftStudy study)
            {
                owner=study;
                DoubleBuffered=true;
                ResizeRedraw=true;
            }
            internal RectangleF GridBounds
            {
                get
                {
                    float size=Math.Max(0,Math.Min(Width-28,Height-68));
                    return new RectangleF((Width-size)/2,42,size,size);
                }
            }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if(e.Button==MouseButtons.Left)
                {
                    Focus();
                    owner.Pick(e.Location);
                }
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var b=GridBounds;
                if(b.Width<10)return;
                float cell=b.Width/10;
                e.Graphics.DrawString("Aircraft roster · schematic",owner.Font,Brushes.LightSteelBlue,8,8);
                using(var water=new SolidBrush(Color.FromArgb(23,49,67)))e.Graphics.FillRectangle(water,b);
                var roster=owner.state.Members;
                for(int n=0;n<roster.Count;n++)
                {
                    var u=roster[n];
                    var r=new RectangleF(b.X+u.X*cell+3,b.Y+u.Y*cell+3,Math.Max(1,cell-6),Math.Max(1,u.Length*cell-6));
                    using(var brush=new SolidBrush(n==owner.members.SelectedIndex?Color.Gold:owner.state.Stored?Color.SlateGray:Color.LightSkyBlue))e.Graphics.FillRectangle(brush,r);
                    e.Graphics.DrawString((n+1).ToString(),owner.Font,Brushes.Black,r.X+4,r.Y+4);
                }
                using(var pen=new Pen(Color.FromArgb(63,97,115)))for(int n=0;n<=10;n++)
                {
                    e.Graphics.DrawLine(pen,b.X+n*cell,b.Y,b.X+n*cell,b.Bottom);
                    e.Graphics.DrawLine(pen,b.X,b.Y+n*cell,b.Right,b.Y+n*cell);
                }
                e.Graphics.DrawString("X →     Y ↓     Click a unit, or use arrow keys",owner.Font,Brushes.LightSteelBlue,b.X,b.Bottom+8);
            }
        }
    }
}
