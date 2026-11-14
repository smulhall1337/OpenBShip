using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;
namespace FleetLab
{
    public sealed class PracticeAircraftFlightStudy:Form
    {
        readonly PracticeAircraftFlightStudyState initial;
        PracticeAircraftFlightStudyState state;
        readonly NumericUpDown targetX=new NumericUpDown(),targetY=new NumericUpDown();
        readonly Button modeTwo=new Button(),queue=new Button(),clear=new Button(),one=new Button(),ten=new Button(),reset=new Button();
        readonly CheckBox includeReturn=new CheckBox();
        readonly Button prepareHome=new Button();
        readonly Label summary=new Label(),feedback=new Label();
        readonly TextBox details=new TextBox();
        readonly FlightCanvas canvas;
        readonly HashSet<Keys> held=new HashSet<Keys>();
        readonly List<string> history=new List<string>();
        internal PracticeAircraftFlightStudyState StudyState
        {
            get
            {
                return state;
            }
        }
        public PracticeAircraftFlightStudy(string programs,PracticeAircraftStudyState parent)
        {
            state=initial=new PracticeAircraftFlightStudyState(programs,parent);
            Text="Aircraft flight and return · copied study";
            Name="aircraftFlightStudy";
            StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1080,800);
            MinimumSize=new Size(860,640);
            Font=new Font("Segoe UI",9);
            BackColor=Color.FromArgb(12,23,35);
            ForeColor=Color.WhiteSmoke;
            KeyPreview=true;
            var layout=new TableLayoutPanel
            {
                Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,320));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,76));
            Controls.Add(layout);
            canvas=new FlightCanvas(this)
            {
                Dock=DockStyle.Fill,TabStop=true,Name="aircraftFlightWorld",AccessibleName="Schematic copied world and aircraft route"
            };
            layout.Controls.Add(canvas,0,0);
            var scroll=new Panel
            {
                Dock=DockStyle.Fill,AutoScroll=true,Name="aircraftFlightScroll"
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
                var b=c as Button;
                if(b!=null)
                {
                    b.UseVisualStyleBackColor=false;
                    b.FlatStyle=FlatStyle.Flat;
                    b.FlatAppearance.BorderColor=Color.SteelBlue;
                    Action tint=delegate
                    {
                        b.BackColor=b.Enabled?Color.FromArgb(28,55,76):Color.LightGray;
                        b.ForeColor=b.Enabled?Color.WhiteSmoke:Color.Black;
                    };
                    b.EnabledChanged+=delegate
                    {
                        tint();
                    };
                    tint();
                }
                side.Controls.Add(c);
            };
            add(new Label
            {
                Text="Manual visits in a copied world. Test waypoints are supplied data. Enable return / idle visits to follow resource-triggered home travel and docking. Other fleets remain fixed."
            },78);
            modeTwo.Text="Apply mode 2 in this copy";
            modeTwo.Name="flightModeTwo";
            modeTwo.AccessibleName=modeTwo.Text;
            modeTwo.AccessibleDescription="Uses source dock and activation helpers. Repositions at the associated parent, restores mode2 resource and clears the route.";
            add(modeTwo,36);
            add(new Label
            {
                Text="Mode 1 helicopters have zero source resource. Mode 2 provides a usable source preset. No seconds or fuel calibration is implied."
            },60);
            prepareHome.Text="Prepare test airfield (copy)";
            prepareHome.Name="flightPrepareHome";
            prepareHome.AccessibleName=prepareHome.Text;
            prepareHome.AccessibleDescription="Supplies a test airfield near the associated home for an active AWACS. Synthetic placement in this flight copy only. Reset removes it.";
            add(prepareHome,36);
            add(new Label
            {
                Text="Optional AWACS test: supplies an airfield near home. Enable return visits to inspect automatic selection and docking. Reset removes it."
            },66);
            prepareHome.Click+=delegate
            {
                Act(delegate
                {
                    state=state.PrepareTestAirfield();Log("Supplied test airfield unit100 / fleet30 in this flight copy only.");
                },"Test airfield prepared in this copy. Enable return / idle visits, queue a distant point and advance visits.");
            };
            add(new Label
            {
                Text="Test waypoint · world coordinates"
            },24);
            targetX.Name="flightTargetX";
            targetX.AccessibleName="Test waypoint world X";
            targetY.Name="flightTargetY";
            targetY.AccessibleName="Test waypoint world Y";
            foreach(var input in new[]
            {
                targetX,targetY
            })
            {
                input.Minimum=0;
                input.Maximum=639;
                input.Increment=1;
                input.Width=112;
            }
            var coords=new FlowLayoutPanel
            {
                FlowDirection=FlowDirection.LeftToRight,WrapContents=false
            };
            coords.Controls.Add(new Label
            {
                Text="X",AutoSize=true,Margin=new Padding(0,6,3,0)
            });
            coords.Controls.Add(targetX);
            coords.Controls.Add(new Label
            {
                Text="Y",AutoSize=true,Margin=new Padding(5,6,3,0)
            });
            coords.Controls.Add(targetY);
            add(coords,32);
            queue.Text="Queue test point";
            queue.Name="flightQueue";
            queue.AccessibleName=queue.Text;
            add(queue,34);
            clear.Text="Clear supplied route";
            clear.Name="flightClear";
            clear.AccessibleName=clear.Text;
            add(clear,34);
            one.Text="1 aircraft batch visit";
            one.Name="flightOne";
            one.AccessibleName=one.Text;
            add(one,36);
            ten.Text="Up to 10 visits";
            ten.Name="flightTen";
            ten.AccessibleName=ten.Text;
            ten.AccessibleDescription="Stops when the route ends, a locked point is reached, the group retires, or the next visit is unsupported.";
            add(ten,36);
            includeReturn.Text="Include recovered return / idle visits";
            includeReturn.Name="flightIncludeReturn";
            includeReturn.AccessibleName=includeReturn.Text;
            includeReturn.AccessibleDescription="Opt in to resource-triggered home travel and docking during explicit visits. Visits continue with no supplied route. AWACS searches available landing slots and relinks its home, or reports no available home.";
            add(includeReturn,46);
            add(new Label
            {
                Text="Visits continue after a route ends. Low resources can send groups home; AWACS may report no available home. No live clock or AI."
            },66);
            includeReturn.CheckedChanged+=delegate
            {
                held.Clear();
                RefreshState();
                feedback.Text=includeReturn.Checked?"Return / idle visits enabled in this copy. Each click still advances only explicit source batches.":"Waypoint-only visits restored. Resource-return boundaries stop before publishing.";
            };
            summary.Name="flightSummary";
            summary.AccessibleName="Aircraft position, resource and route state";
            add(summary,160);
            details.Name="flightDetails";
            details.AccessibleName="Copied units and visit history";
            details.Multiline=true;
            details.ReadOnly=true;
            details.ScrollBars=ScrollBars.Vertical;
            details.BackColor=BackColor;
            details.ForeColor=ForeColor;
            add(details,230);
            reset.Text="Reset flight study";
            reset.Name="flightReset";
            reset.AccessibleName=reset.Text;
            add(reset,34);
            var close=new Button
            {
                Text="Close",Name="flightClose",DialogResult=DialogResult.Cancel
            };
            add(close,34);
            CancelButton=close;
            feedback.Name="flightFeedback";
            feedback.AccessibleName="Flight study feedback";
            feedback.Dock=DockStyle.Fill;
            feedback.ForeColor=Color.LightSteelBlue;
            layout.Controls.Add(feedback,0,1);
            layout.SetColumnSpan(feedback,2);
            modeTwo.Click+=delegate
            {
                Act(delegate
                {
                    state=state.ApplyModeTwo();DefaultTarget();Log("Mode2 applied in copy; source parent origin restored and route cleared.");
                },"Source mode 2 applied. Queue a test point, then visit its aircraft batch.");
            };
            queue.Click+=delegate
            {
                Act(delegate
                {
                    state=state.QueueSuppliedPoint((int)targetX.Value,(int)targetY.Value);Log("Supplied point "+targetX.Value+", "+targetY.Value+" queued.");
                },"Copied test point queued. This does not establish the original waypoint input rules.");
            };
            clear.Click+=delegate
            {
                Act(delegate
                {
                    state=state.ClearSuppliedRoute();Log("Supplied route cleared; position and resource retained.");
                },"Route cleared. Position, resource and explicit visit count are preserved.");
            };
            one.Click+=delegate
            {
                Advance(1);
            };
            ten.Click+=delegate
            {
                Advance(10);
            };
            reset.Click+=delegate
            {
                state=initial;
                includeReturn.Checked=false;
                history.Clear();
                DefaultTarget();
                RefreshState();
                feedback.Text="Starting flight copy restored. Parent aircraft, active battle, map preview and settings were preserved.";
            };
            targetX.ValueChanged+=delegate
            {
                canvas.Invalidate();
            };
            targetY.ValueChanged+=delegate
            {
                canvas.Invalidate();
            };
            KeyUp+=delegate(object sender,KeyEventArgs e)
            {
                held.Remove(e.KeyCode);
            };
            Deactivate+=delegate
            {
                held.Clear();
            };
            DefaultTarget();
            RefreshState();
            feedback.Text="For a first flight, apply mode 2 in this copy, queue the nearby test point, then use 1 aircraft batch visit or Up to 10 visits.";
        }
        void DefaultTarget()
        {
            targetX.Value=Math.Min(639,state.Origin.X+40);
            targetY.Value=Math.Min(639,state.Origin.Y+30);
        }
        void Log(string message)
        {
            history.Add(message);
            if(history.Count>120)history.RemoveAt(0);
        }
        void Act(Action action,string success)
        {
            try
            {
                action();
                RefreshState();
                feedback.Text=success;
            }
            catch(NotSupportedException e)
            {
                feedback.Text="Study limit: "+e.Message;
                RefreshState();
            }
            catch(InvalidDataException e)
            {
                feedback.Text=e.Message;
                RefreshState();
            }
            catch(ArgumentException e)
            {
                feedback.Text=e.Message;
            }
            catch(InvalidOperationException e)
            {
                feedback.Text=e.Message;
            }
        }
        void Advance(int requested)
        {
            if(!one.Enabled)return;
            int done=0;
            string limit=null;
            for(int n=0;n<requested;n++)
            {
                try
                {
                    var beforeSearch=state.LastHomeSearch;
                    state=includeReturn.Checked?state.VisitIncludingHomeSearch():state.VisitOnce();
                    done++;
                    if(state.LastHomeSearch!=null&&state.LastHomeSearch!=beforeSearch) Log("Home search: "+state.LastHomeSearch.CandidateCount+" candidate(s); "+(state.LastHomeSearch.FoundHome?"selected unit "+state.LastHomeSearch.ParentUnit+", link "+state.LastHomeSearch.Link:"no available home"));
                    if(state.LastReturn!=null)
                    {
                        var update=state.LastReturn;
                        Log("Visit "+state.Visits+": "+update.Before.X+","+update.Before.Y+" → "+update.After.X+","+update.After.Y+" · "+update.Outcome+(update.StartedReturn?" · return initiated":""));
                        if(update.Outcome==PracticeAircraftReturnOutcome.ReachedLockedHome)break;
                    }
                    else
                    {
                        var update=state.LastUpdate;
                        Log("Visit "+state.Visits+": "+update.Before.X+","+update.Before.Y+" → "+update.After.X+","+update.After.Y+" · "+update.Outcome+(state.LastEmptyHomeResult?" · no available home":""));
                        if(update.Outcome==PracticeAircraftWaypointOutcome.ReachedLockedWaypoint)break;
                    }
                    if(!state.Active||(!includeReturn.Checked&&state.Route.Count==0))break;
                }
                catch(NotSupportedException e)
                {
                    limit=e.Message;
                    break;
                }
                catch(InvalidDataException e)
                {
                    limit=e.Message;
                    break;
                }
            }
            RefreshState();
            feedback.Text=limit!=null?"Stopped after "+done+" visit(s): "+limit:"Advanced "+done+" explicit aircraft batch visit(s). "+(state.Stored?"Docked at the associated home. Apply mode 2 to reactivate, or reset.":!state.Active?"Group retired during the source resource pass. Reset to try another source preset.":state.ReturnState==1?"Returning to the associated home.":state.ReturnState==2&&state.Kind==5?"No available home. Remaining source resource continues to be consumed.":state.Route.Count==0?(includeReturn.Checked?"No route; further visits still consume resource96.":"Route complete."):"Other fleets remain fixed.");
        }
        void RefreshState()
        {
            if(IsDisposed||Disposing)return;
            bool active=state.Active,ready=state.ProfileReady;
            modeTwo.Enabled=active||state.Stored;
            prepareHome.Enabled=state.CanPrepareTestAirfield;
            queue.Enabled=active&&ready&&state.Route.Count<5;
            clear.Enabled=active&&state.Route.Count>0;
            one.Enabled=ten.Enabled=active&&(includeReturn.Checked?state.ExtendedReturnProfileReady:ready&&state.Route.Count>0);
            var members=state.Members;
            int low=int.MaxValue,high=int.MinValue;
            foreach(var unit in members)
            {
                int value=BitConverter.ToInt16(unit.CopyRecord(),96);
                low=Math.Min(low,value);
                high=Math.Max(high,value);
            }
            if(members.Count==0)low=high=0;
            summary.Text="Fleet "+state.FleetSlot+" · "+(active?"active":state.Stored?"stored / docked":"retired")+" · kind "+state.Kind+"\r\nWorld "+state.Origin.X+", "+state.Origin.Y+" · source speed "+state.Speed+"\r\nLive units "+members.Count+" · resource96 "+low+"–"+high+"\r\nTask "+state.Task+" · return state "+state.ReturnState+"\r\nRoute "+state.Route.Count+"/5 · explicit visits "+state.Visits+"\r\nHome results "+state.AvailableHomeResults+" available / "+state.EmptyHomeResults+" unavailable\r\n"+(state.Stored?"Docked; mode 2 can reactivate this copy.":state.ReturnState==2&&state.Kind==5?(includeReturn.Checked?"No home; resource96 still decreases.":"No home; enable return / idle visits."):state.ReturnState==1?(includeReturn.Checked?"Returning to the associated home.":"Returning; enable return visits to continue."):ready?"Manual stepping; no live clock.":active?"Apply mode2 for the admitted flight subset.":"Reset restores the starting aircraft.");
            var text=new StringBuilder();
            text.AppendLine("Copied live units:");
            foreach(var unit in members)text.AppendLine("Unit "+unit.Slot+" · HP "+unit.HitPoints+" · resource96 "+BitConverter.ToInt16(unit.CopyRecord(),96));
            if(members.Count==0)text.AppendLine("None remaining.");
            var score=state.CopyScoreRegion();
            text.AppendLine("Copied scores: "+BitConverter.ToInt32(score,0)+" / "+BitConverter.ToInt32(score,44));
            text.AppendLine("Route:");
            for(int n=0;n<state.Route.Count;n++)text.AppendLine("W"+(n+1)+": "+state.Route[n].X+", "+state.Route[n].Y);
            if(state.Route.Count==0)text.AppendLine("No supplied waypoints.");
            if(state.Stored)text.AppendLine("Source home waypoint is retained after docking.");
            text.AppendLine("Available-home results in this window: "+state.AvailableHomeResults);
            text.AppendLine("No-home results in this window: "+state.EmptyHomeResults);
            if(state.SuppliedTestAirfield)text.AppendLine("Supplied test airfield: unit100 / fleet30. Synthetic creation and placement; this copy only.");
            if(state.LastHomeSearch!=null)text.AppendLine("Most recent home search: "+state.LastHomeSearch.CandidateCount+" candidate(s); "+(state.LastHomeSearch.FoundHome?"selected unit "+state.LastHomeSearch.ParentUnit+", link "+state.LastHomeSearch.Link:"no available slot"));
            text.AppendLine("Recent actions:");
            foreach(string line in history)text.AppendLine(line);
            details.Text=text.ToString();
            details.AccessibleDescription=details.Text;
            summary.AccessibleDescription=summary.Text;
            canvas.Invalidate();
        }
        static Button FocusedButton(Control parent)
        {
            foreach(Control c in parent.Controls)
            {
                if(c.Focused)return c as Button;
                var nested=FocusedButton(c);
                if(nested!=null)return nested;
            }
            return null;
        }
        protected override bool ProcessCmdKey(ref Message message,Keys keys)
        {
            if(keys==Keys.Enter)
            {
                var button=FocusedButton(this);
                if(button!=null||canvas.ContainsFocus)
                {
                    bool repeat=(message.LParam.ToInt64()&0x40000000L)!=0;
                    if(!repeat&&held.Add(Keys.Enter))
                    {
                        if(button!=null&&button.Enabled)button.PerformClick();
                        else if(canvas.ContainsFocus&&queue.Enabled)queue.PerformClick();
                    }
                    return true;
                }
            }
            if(canvas.ContainsFocus&&(keys==Keys.Left||keys==Keys.Right||keys==Keys.Up||keys==Keys.Down))
            {
                targetX.Value=Math.Max(0,Math.Min(639,targetX.Value+(keys==Keys.Left?-1:keys==Keys.Right?1:0)));
                targetY.Value=Math.Max(0,Math.Min(639,targetY.Value+(keys==Keys.Up?-1:keys==Keys.Down?1:0)));
                return true;
            }
            return base.ProcessCmdKey(ref message,keys);
        }
        sealed class FlightCanvas:Control
        {
            readonly PracticeAircraftFlightStudy owner;
            internal FlightCanvas(PracticeAircraftFlightStudy form)
            {
                owner=form;
                DoubleBuffered=true;
                SetStyle(ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);
                BackColor=Color.FromArgb(12,23,35);
            }
            internal RectangleF World
            {
                get
                {
                    float size=Math.Max(10,Math.Min(ClientSize.Width-64,ClientSize.Height-90));
                    return new RectangleF((ClientSize.Width-size)/2,44,size,size);
                }
            }
            PointF Project(Point point)
            {
                var r=World;
                return new PointF(r.Left+point.X*r.Width/640,r.Top+point.Y*r.Height/640);
            }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if(e.Button!=MouseButtons.Left||!World.Contains(e.Location))return;
                Focus();
                owner.targetX.Value=Math.Max(0,Math.Min(639,(int)((e.X-World.Left)*640/World.Width)));
                owner.targetY.Value=Math.Max(0,Math.Min(639,(int)((e.Y-World.Top)*640/World.Height)));
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if(owner.IsDisposed)return;
                var g=e.Graphics;
                g.SmoothingMode=SmoothingMode.AntiAlias;
                var r=World;
                using(var ocean=new SolidBrush(Color.FromArgb(20,47,62)))g.FillRectangle(ocean,r);
                using(var grid=new Pen(Color.FromArgb(58,88,105)))
                {
                    for(int n=0;n<=8;n++)
                    {
                        float x=r.Left+n*r.Width/8,y=r.Top+n*r.Height/8;
                        g.DrawLine(grid,x,r.Top,x,r.Bottom);
                        g.DrawLine(grid,r.Left,y,r.Right,y);
                    }
                }
                using(var brush=new SolidBrush(Color.LightSteelBlue))
                {
                    g.DrawString("Schematic world · copied fleet origins",owner.Font,brush,8,8);
                    foreach(int value in new[]
                    {
                        0,160,320,480,639
                    })
                    {
                        var x=Project(new Point(value,0));
                        g.DrawString(value.ToString(),owner.Font,brush,x.X-8,r.Bottom+3);
                        var y=Project(new Point(0,value));
                        g.DrawString(value.ToString(),owner.Font,brush,Math.Max(0,r.Left-30),y.Y-7);
                    }
                    g.DrawString("Click to choose a test point; Queue adds it. Arrows / Enter also work.",owner.Font,brush,8,r.Bottom+30);
                }
                var state=owner.state;
                var f=state.CopyFleetPool();
                for(int slot=0;slot<250;slot++)
                {
                    if(slot==state.FleetSlot)continue;
                    int at=slot*492,kind=BitConverter.ToInt32(f,at);
                    if(kind==0||kind>=6&&kind<=8)continue;
                    int x=BitConverter.ToInt16(f,at+12),y=BitConverter.ToInt16(f,at+14);
                    if(x<0||x>=640||y<0||y>=640)continue;
                    var p=Project(new Point(x,y));
                    using(var brush=new SolidBrush(kind==11?Color.SlateGray:Color.SteelBlue))
                    {
                        g.FillEllipse(brush,p.X-3,p.Y-3,6,6);
                        if(kind!=11)g.DrawString(slot.ToString(),owner.Font,brush,p.X+4,p.Y+3);
                    }
                    if(slot==30&&state.SuppliedTestAirfield)
                    {
                        g.DrawRectangle(Pens.PaleTurquoise,p.X-6,p.Y-6,12,12);
                        g.DrawString("Test airfield",owner.Font,Brushes.PaleTurquoise,Math.Min(r.Right-90,p.X+8),Math.Min(r.Bottom-18,p.Y+20));
                    }
                }
                var origin=Project(state.Origin);
                var previous=origin;
                using(var route=new Pen(Color.Gold,1.5f))
                {
                    route.DashStyle=DashStyle.Dash;
                    for(int n=0;n<state.Route.Count;n++)
                    {
                        var p=Project(state.Route[n]);
                        g.DrawLine(route,previous,p);
                        g.DrawEllipse(Pens.Gold,p.X-5,p.Y-5,10,10);
                        g.DrawString("W"+(n+1),owner.Font,Brushes.Gold,p.X+6,p.Y-12);
                        previous=p;
                    }
                }
                var cursor=Project(new Point((int)owner.targetX.Value,(int)owner.targetY.Value));
                g.DrawEllipse(Pens.LightSkyBlue,cursor.X-7,cursor.Y-7,14,14);
                if(state.Active)
                {
                    PointF[] triangle;
                    switch(state.Facing)
                    {
                        case 1:triangle=new[]
                        {
                            new PointF(origin.X+10,origin.Y),new PointF(origin.X-7,origin.Y-6),new PointF(origin.X-7,origin.Y+6)
                        };
                        break;
                        case 2:triangle=new[]
                        {
                            new PointF(origin.X,origin.Y+10),new PointF(origin.X-6,origin.Y-7),new PointF(origin.X+6,origin.Y-7)
                        };
                        break;
                        case 3:triangle=new[]
                        {
                            new PointF(origin.X-10,origin.Y),new PointF(origin.X+7,origin.Y-6),new PointF(origin.X+7,origin.Y+6)
                        };
                        break;
                        default:triangle=new[]
                        {
                            new PointF(origin.X,origin.Y-10),new PointF(origin.X-6,origin.Y+7),new PointF(origin.X+6,origin.Y+7)
                        };
                        break;
                    }
                    g.FillPolygon(Brushes.Gold,triangle);
                    g.DrawPolygon(Pens.Black,triangle);
                }
                else if(state.Stored)
                {
                    g.DrawRectangle(Pens.PaleTurquoise,origin.X-7,origin.Y-7,14,14);
                    g.DrawString("Docked",owner.Font,Brushes.PaleTurquoise,origin.X+10,origin.Y+2);
                }
                else
                {
                    g.DrawLine(Pens.IndianRed,origin.X-7,origin.Y-7,origin.X+7,origin.Y+7);
                    g.DrawLine(Pens.IndianRed,origin.X-7,origin.Y+7,origin.X+7,origin.Y-7);
                }
                g.DrawString("Fleet "+state.FleetSlot,owner.Font,Brushes.Gold,origin.X+12,origin.Y-32);
            }
        }
    }
}
