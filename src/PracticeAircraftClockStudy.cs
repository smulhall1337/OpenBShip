using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;
namespace FleetLab
{
    public sealed class PracticeAircraftClockStudy : Form
    {
        readonly PracticeAircraftClockScenario scenarios;
        PracticeAircraftWorldStudyState state;
        readonly ComboBox kind=new ComboBox(),weapon=new ComboBox(),preset=new ComboBox(),seed=new ComboBox();
        readonly Button load=new Button(),queue=new Button(),clear=new Button(),one=new Button(),many=new Button(),longRun=new Button(),pause=new Button();
        readonly Label summary=new Label(),feedback=new Label(),loaded=new Label();
        readonly TextBox details=new TextBox();
        readonly ClockCanvas canvas;
        readonly List<string> history=new List<string>();
        readonly HashSet<Keys> held=new HashSet<Keys>();
        int loadedWeapon=1,loadedKind=6;
        PracticeAircraftClockPreset loadedPreset;
        uint loadedSeed;
        bool pending,loadedRequests;
        uint loadedPhaseStart;
        PracticeAircraftRequestClock requestClock;
        readonly CheckBox useRequests=new CheckBox();
        readonly NumericUpDown phase=new NumericUpDown();
        readonly Button play=new Button();
        readonly Timer playback=new Timer
        {
            Interval=40
        };
        int playbackRemaining;
        bool playbackTick,playbackDisposed;
        internal PracticeAircraftWorldStudyState StudyState
        {
            get
            {
                return state;
            }
        }
        public PracticeAircraftClockStudy(string assets,string programs)
        {
            scenarios=new PracticeAircraftClockScenario(assets,programs);
            Text="Shared aircraft flight and AA clock · copied scenarios";
            Name="aircraftClockStudy";
            StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1120,820);
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
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,76));
            Controls.Add(layout);
            canvas=new ClockCanvas(this)
            {
                Dock=DockStyle.Fill,Name="aircraftClockCanvas",AccessibleName="Schematic world flight, AA projectiles and aircraft hull",TabStop=true
            };
            layout.Controls.Add(canvas,0,0);
            var scroll=new Panel
            {
                Dock=DockStyle.Fill,AutoScroll=true,Name="aircraftClockScroll"
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
                Text="Diagnostic copy of the first Practice map. Presets supply attachment, battery, visibility, routes and resources. Updates use recovered rules; no full AI, original application pacing, audio or missions."
            },90);
            Action<ComboBox,string,object[]> choice=delegate(ComboBox c,string text,object[] items)
            {
                add(new Label
                {
                    Text=text
                },22);
                c.DropDownStyle=ComboBoxStyle.DropDownList;
                c.Items.AddRange(items);
                c.SelectedIndex=0;
                c.AccessibleName=text;
                add(c,30);
            };
            kind.Name="clockKind";
            choice(kind,"Aircraft",new object[]
            {
                "Helicopters · four units","Planes · four units","AWACS · one unit"
            });
            weapon.Name="clockWeapon";
            choice(weapon,"Supplied AA equipment",new object[]
            {
                "AA equipment 1 · stock 2","AA equipment 2 · stock 2","AA equipment 3 · last round"
            });
            preset.Name="clockPreset";
            choice(preset,"Prepared scenario",new object[]
            {
                "Flight and fire","Moving battery and aircraft","Retire before impact · resource 0","Return before impact · resource 20","Clock wrap · flight and fire","AWACS · supplied airfield"
            });
            seed.Name="clockSeed";
            choice(seed,"Supplied random seed",new object[]
            {
                "0","731"
            });
            useRequests.Name="clockUseRequests";
            useRequests.Text="Generate AA on recovered phase 13";
            useRequests.AccessibleName=useRequests.Text;
            add(useRequests,42);
            add(new Label
            {
                Text="Initial request counter (applied on Load)"
            },22);
            phase.Name="clockPhase";
            phase.AccessibleName="Initial source request counter";
            phase.Minimum=0;
            phase.Maximum=uint.MaxValue;
            phase.Enabled=false;
            add(phase,30);
            load.Name="clockLoad";
            load.Text="Load / reset preset";
            load.AccessibleName=load.Text;
            add(load,36);
            loaded.Name="clockLoaded";
            loaded.AccessibleName="Applied preset";
            add(loaded,150);
            queue.Name="clockQueue";
            queue.Text="Queue one AA order (raw)";
            queue.AccessibleName=queue.Text;
            add(queue,36);
            clear.Name="clockClear";
            clear.Text="Clear unit 14 queue";
            clear.AccessibleName=clear.Text;
            add(clear,34);
            one.Name="clockOne";
            one.Text="1 source poll (+40 ms)";
            one.AccessibleName=one.Text;
            add(one,36);
            many.Name="clockMany";
            many.Text="32 source polls (+1.28 s)";
            many.AccessibleName=many.Text;
            add(many,36);
            longRun.Name="clockLong";
            longRun.Text="128 source polls (+5.12 s)";
            longRun.AccessibleName=longRun.Text;
            add(longRun,36);
            pause.Name="clockPause";
            pause.Text="25 paused steps (+1 s)";
            pause.AccessibleName=pause.Text;
            add(pause,36);
            play.Name="clockPlay";
            play.Text="Watch 32 polls (debug)";
            play.AccessibleName="Watch or stop 32 supplied source polls";
            add(play,36);
            add(new Label
            {
                Text="Watch supplies one 40 ms step per UI timer tick, then stops after 32. It can run slower under load. It stops on focus loss or another action. This is a viewing aid, not recovered application pacing."
            },104);
            add(new Label
            {
                Text="One poll: two fleet batches, optional phase-13 requests, AA launch, projectiles and effects. Aircraft move on their scheduler visit. Paused steps hold the phase, flight, queues and effects, and compensate projectile deadlines."
            },104);
            summary.Name="clockSummary";
            summary.AccessibleName="Shared clock, resources and combat state";
            add(summary,230);
            details.Name="clockDetails";
            details.AccessibleName="Copied aircraft records, orders and history";
            details.Multiline=true;
            details.ReadOnly=true;
            details.ScrollBars=ScrollBars.Vertical;
            details.BackColor=BackColor;
            details.ForeColor=ForeColor;
            add(details,270);
            var close=new Button
            {
                Text="Close",Name="clockClose",DialogResult=DialogResult.Cancel
            };
            add(close,36);
            CancelButton=close;
            feedback.Name="clockFeedback";
            feedback.AccessibleName="Shared clock study feedback";
            feedback.Dock=DockStyle.Fill;
            feedback.ForeColor=Color.LightSteelBlue;
            layout.Controls.Add(feedback,0,1);
            layout.SetColumnSpan(feedback,2);
            EventHandler changed=delegate
            {
                StopPlayback("Playback stopped because a selection changed.");
                pending=true;
                if(state!=null)
                {
                    loaded.Text="Selection changed. Load / reset preset to apply it.\r\nThe running copy still uses the applied preset below.";
                }
            };
            kind.SelectedIndexChanged+=changed;
            weapon.SelectedIndexChanged+=changed;
            preset.SelectedIndexChanged+=changed;
            seed.SelectedIndexChanged+=changed;
            useRequests.CheckedChanged+=delegate(object sender,EventArgs e)
            {
                phase.Enabled=useRequests.Checked;
                changed(sender,e);
            };
            phase.ValueChanged+=changed;
            load.Click+=delegate
            {
                LoadPreset();
            };
            queue.Click+=delegate
            {
                Act(delegate(PracticeAircraftWorldStudyState before)
                {
                    if(loadedRequests)throw new InvalidOperationException("Load a manual-request preset before adding a raw order.");var s=before.Combat.Objects.CopyShipPool();return before.Queue(14,13,loadedWeapon,BitConverter.ToInt16(s,19*268+8),BitConverter.ToInt16(s,19*268+10));
                },"One raw order queued. It is checked on a due source poll.");
            };
            clear.Click+=delegate
            {
                Act(delegate(PracticeAircraftWorldStudyState before)
                {
                    if(loadedRequests)
                    {
                        var next=requestClock.Clear(14);requestClock=next;return next.World;
                    }
                    return before.Clear(14);
                },"Copied battery queue cleared. Shots already in flight continue.");
            };
            one.Click+=delegate
            {
                Advance(1,false);
            };
            many.Click+=delegate
            {
                Advance(32,false);
            };
            longRun.Click+=delegate
            {
                Advance(128,false);
            };
            pause.Click+=delegate
            {
                Advance(25,true);
            };
            play.Click+=delegate
            {
                if(playbackRemaining>0)
                {
                    StopPlayback("Playback stopped. The current copy is preserved.");
                    return;
                }
                if(state==null||IsDisposed||Disposing)return;
                playbackRemaining=32;
                play.Text="Stop watching (32 left)";
                feedback.Text="Watching 32 supplied polls. Focus loss or another action stops playback; there is no catch-up or automatic resume.";
                playback.Start();
            };
            playback.Tick+=PlaybackTick;
            KeyUp+=delegate(object sender,KeyEventArgs e)
            {
                held.Remove(e.KeyCode);
            };
            Deactivate+=delegate
            {
                held.Clear();
                StopPlayback("Playback stopped on focus loss. Press Watch to start another 32 polls.");
            };
            LoadPreset();
        }
        void StopPlayback(string message)
        {
            bool wasRunning=playbackRemaining>0;
            if(!playbackDisposed)playback.Stop();
            playbackRemaining=0;
            if(!play.IsDisposed)play.Text="Watch 32 polls (debug)";
            if(wasRunning&&message!=null&&!feedback.IsDisposed)feedback.Text=message;
        }
        void PlaybackTick(object sender,EventArgs e)
        {
            if(playbackTick)return;
            if(IsDisposed||Disposing||playbackRemaining<=0)
            {
                StopPlayback(null);
                return;
            }
            playbackTick=true;
            try
            {
                if(!Advance(1,false))return;
                if(playbackRemaining==1)StopPlayback("Finished watching 32 supplied polls. The copy is ready for manual inspection.");
                else
                {
                    playbackRemaining--;
                    play.Text="Stop watching ("+playbackRemaining+" left)";
                }
            }
            finally
            {
                playbackTick=false;
            }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopPlayback(null);
            base.OnFormClosing(e);
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing&&!playbackDisposed)
            {
                StopPlayback(null);
                playback.Tick-=PlaybackTick;
                playback.Dispose();
                playbackDisposed=true;
            }
            base.Dispose(disposing);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(e.KeyCode!=Keys.Enter&&e.KeyCode!=Keys.Space&&e.KeyCode!=Keys.Escape)return;
            var button=ActiveControl as Button;
            var check=ActiveControl as CheckBox;
            if(e.KeyCode!=Keys.Escape&&button==null&&check==null)return;
            e.Handled=true;
            e.SuppressKeyPress=true;
            if(!held.Add(e.KeyCode))return;
            if(e.KeyCode==Keys.Escape)
            {
                Close();
                return;
            }
            if(check!=null&&check.Enabled)
            {
                check.Checked=!check.Checked;
                return;
            }
            if(button!=null&&button.Enabled)button.PerformClick();
        }
        void LoadPreset()
        {
            StopPlayback(null);
            try
            {
                int nextKind=6+kind.SelectedIndex,nextWeapon=weapon.SelectedIndex+1;
                var nextPreset=(PracticeAircraftClockPreset)preset.SelectedIndex;
                uint nextSeed=seed.SelectedIndex==0?0u:731u;
                var next=scenarios.Create(nextKind,nextWeapon,nextPreset,nextSeed);
                var nextClock=useRequests.Checked?new PracticeAircraftRequestClock(next,(uint)phase.Value):null;
                state=next;
                requestClock=nextClock;
                loadedRequests=useRequests.Checked;
                loadedPhaseStart=(uint)phase.Value;
                loadedKind=nextKind;
                loadedWeapon=nextWeapon;
                loadedPreset=nextPreset;
                loadedSeed=nextSeed;
                pending=false;
                history.Clear();
                history.Add("Loaded copied preset; no order queued.");
                RefreshState();
                feedback.Text=loadedRequests?"Preset loaded. Normal source polls generate AA requests on counter phase13; no manual queue is needed. Load / reset restores a fresh copy.":"Preset loaded. Queue one AA order, then use 1 source poll to launch it. Load / reset restores a fresh copy.";
            }
            catch(Exception e)
            {
                if(!Expected(e))throw;
                feedback.Text=e.Message;
            }
        }
        static bool Expected(Exception e)
        {
            return e is ArgumentException||e is InvalidDataException||e is InvalidOperationException||e is NotSupportedException;
        }
        bool Act(Func<PracticeAircraftWorldStudyState,PracticeAircraftWorldStudyState> action,string message)
        {
            if(!playbackTick)StopPlayback(null);
            try
            {
                var next=action(state);
                state=next;
                history.Add(message);
                if(history.Count>40)history.RemoveAt(0);
                RefreshState();
                feedback.Text=message;
                return true;
            }
            catch(Exception e)
            {
                if(!Expected(e))throw;
                StopPlayback(null);
                feedback.Text="Step rejected; current copy preserved. "+e.Message;
                return false;
            }
        }
        bool Advance(int count,bool paused)
        {
            return Act(delegate(PracticeAircraftWorldStudyState before)
            {
                if(loadedRequests)
                {
                    var next=requestClock;for(int n=0;n<count;n++)next=next.AdvanceFrame(paused);requestClock=next;return next.World;
                }
                var manual=before;for(int n=0;n<count;n++)manual=manual.AdvanceFrame(paused);return manual;
            },paused?"25 paused steps applied. Request phase, flight, resources, queues and effects held.":count+" source poll(s) applied. "+(count*40)+" ms supplied to the shared clock.");
        }
        static int W(byte[] b,int p)
        {
            return BitConverter.ToInt16(b,p);
        }
        static int I(byte[] b,int p)
        {
            return BitConverter.ToInt32(b,p);
        }
        static string PresetName(PracticeAircraftClockPreset value)
        {
            return value==PracticeAircraftClockPreset.SuppliedAirfield?"AWACS supplied airfield":value==PracticeAircraftClockPreset.MovingBattery?"Moving battery and aircraft":value==PracticeAircraftClockPreset.EmptyResource?"Retire before impact":value==PracticeAircraftClockPreset.ReturnBeforeImpact?"Return before impact":value==PracticeAircraftClockPreset.ClockWrap?"Clock wrap":"Flight and fire";
        }
        void RefreshState()
        {
            if(state==null||IsDisposed||Disposing)return;
            var c=state.Combat;
            var s=c.Objects.CopyShipPool();
            var f=c.Objects.CopyFleetPool();
            var t=c.Objects.CopyTimedPool();
            int at=13*492,k=I(f,at),active=0,live=0,resource=0;
            for(int n=0;n<80;n++)if(I(t,n*268)!=0)active++;
            var text=new StringBuilder();
            text.AppendLine("Applied: "+(loadedKind==6?"helicopters":loadedKind==7?"planes":"AWACS")+" / AA "+loadedWeapon+" / "+PresetName(loadedPreset)+" / RNG seed "+loadedSeed);
            text.AppendLine("Fixed map seed 143514006; initial attachment parent 7 / link 0.");
            text.AppendLine("Battery unit14, fleet "+W(s,14*268+12)+"; selected surface7.");
            for(int n=0;n<6;n++)
            {
                uint r=BitConverter.ToUInt32(f,at+16+n*4);
                if(r==0)continue;
                int u=(int)(r-0x4fc340);
                if(I(s,u)==0)continue;
                live++;
                resource=W(s,u+96);
                text.AppendLine("Unit "+u/268+" · HP "+W(s,u+16)+" · resource96 "+resource+" · local "+W(s,u+8)+", "+W(s,u+10));
            }
            var q=state.Orders.CopySecondaryOrders();
            text.AppendLine("Current associated home unit "+W(f,at+8)+" / link "+W(f,at+10));
            text.AppendLine("Battery queue head "+W(q,14*52+8)+", "+W(q,14*52+28)+" · deadline "+BitConverter.ToUInt32(q,14*52+4));
            text.AppendLine("Selected surface waypoint index "+state.SelectedWaypoint+" (supplied initial4).");
            for(int n=0;n<80;n++)if(I(t,n*268)!=0)text.AppendLine("Shot "+n+" at "+W(t,n*268+8)+", "+W(t,n*268+10)+" · deadline "+BitConverter.ToUInt32(t,n*268+24));
            text.AppendLine();
            if(loadedRequests)
            {
                text.AppendLine("Request counter "+requestClock.PhaseCounter+" · producer passes "+requestClock.ProducerPasses);
                text.AppendLine("Generated "+state.Orders.GeneratedOrders+" · accepted "+state.Orders.AcceptedRequests+" · most recent "+state.Orders.LastGenerated);
                foreach(var r in state.Orders.LastRequests)text.AppendLine("AA source "+r.SourceShip+" (fleet "+r.SourceFleet+") -> target unit "+r.TargetShip+" (fleet "+r.TargetFleet+") / weapon "+r.Weapon+" at "+r.X+", "+r.Y+" / queue return "+r.QueueReturnCode);
                text.AppendLine();
            }
            text.AppendLine("Recent actions:");
            foreach(var line in history)text.AppendLine(line);
            details.Text=text.ToString();
            summary.Text="Clock "+c.ClockMilliseconds+" ms · normal "+c.MovementFrames+" / paused "+c.PausedFrames+"\r\nNext batch "+state.Schedule.BatchStart+" · wraps "+state.Schedule.CompletedCycles+"\r\nAircraft "+(k==0?"retired":k>=6&&k<=8?"stored / docked":"active")+" · kind "+k+" · live units "+live+"\r\nWorld "+W(f,at+12)+", "+W(f,at+14)+" · resource96 "+resource+"\r\nAircraft visits "+state.AircraftVisits+" · surface visits "+state.SurfaceVisits+"\r\nAA stock "+W(s,14*268+196+loadedWeapon*2)+" · queued batteries "+state.Orders.QueuedSources+"\r\nShots "+active+" · effects "+c.ActiveEffects+"\r\nCreated "+state.Orders.CreatedObjects+" · expired "+c.ExpiredObjects+" · rejected "+state.Orders.RejectedOrders+"\r\nImpact "+(c.LastImpactOutcome.HasValue?c.LastImpactOutcome.ToString():"none")+" · destroyed "+c.DestroyedUnits+"\r\nRNG "+c.RandomState+" · movie counter "+c.MovieCounter;
            if(!pending)loaded.Text="Applied preset: "+PresetName(loadedPreset)+"\r\nMode2 · initial resource "+(loadedPreset==PracticeAircraftClockPreset.EmptyResource?0:loadedPreset==PracticeAircraftClockPreset.ReturnBeforeImpact||loadedPreset==PracticeAircraftClockPreset.SuppliedAirfield?20:2000)+" · nearby visible battery.\r\n"+(loadedPreset==PracticeAircraftClockPreset.SuppliedAirfield?"Copied airfield at 410, 370. AWACS starts nearby with resource 20. AA may destroy it before docking.":loadedPreset==PracticeAircraftClockPreset.ReturnBeforeImpact?(loadedKind==8?"AWACS has no airfield here; it will exhaust its resources and retire.":"Aircraft start at their carrier and dock before the shot arrives."):loadedPreset==PracticeAircraftClockPreset.EmptyResource?"Aircraft retire on their first scheduler visit.":"Aircraft route: 40 units east, 30 units south. Battery is one unit west at load.");
            summary.Text+="\r\n"+(loadedRequests?"Request phase "+requestClock.PhaseCounter+" · producer passes "+requestClock.ProducerPasses:"Requests: manual");
            if(!pending&&loadedRequests)loaded.Text+="\r\nAA requests: phase13 / initial counter "+loadedPhaseStart+".";
            queue.Enabled=!loadedRequests&&k>=3&&k<=5&&W(s,14*268+196+loadedWeapon*2)>0&&state.Orders.QueuedSources==0;
            clear.Enabled=W(q,14*52+8)!=-1;
            canvas.Invalidate();
        }
        sealed class ClockCanvas:Control
        {
            readonly PracticeAircraftClockStudy owner;
            readonly PointF start=new PointF(350,390);
            internal ClockCanvas(PracticeAircraftClockStudy owner)
            {
                this.owner=owner;
                DoubleBuffered=true;
                ResizeRedraw=true;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if(owner.state==null||Width<100||Height<150)return;
                e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
                var s=owner.state.Combat.Objects.CopyShipPool();
                var f=owner.state.Combat.Objects.CopyFleetPool();
                var t=owner.state.Combat.Objects.CopyTimedPool();
                var world=new RectangleF(30,42,Math.Max(1,Width-60),Math.Max(100,Height*.56f-54));
                e.Graphics.DrawString("World coordinates · 10-unit grid / schematic",owner.Font,Brushes.LightSteelBlue,10,8);
                int air=13*492,battery=W(s,14*268+12)*492;
                float minX=start.X-24,maxX=start.X+70,minY=start.Y-24,maxY=start.Y+65;
                Action<float,float> include=delegate(float x,float y)
                {
                    minX=Math.Min(minX,x-8);
                    maxX=Math.Max(maxX,x+8);
                    minY=Math.Min(minY,y-8);
                    maxY=Math.Max(maxY,y+8);
                };
                include(W(f,air+12),W(f,air+14));
                include(W(f,battery+12),W(f,battery+14));
                for(int n=0;n<5;n++)
                {
                    int x=W(f,air+40+n*2);
                    if(x==-1)break;
                    include(x,W(f,air+50+n*2));
                }
                if(I(f,30*492)==11)include(W(f,30*492+12),W(f,30*492+14));
                for(int n=0;n<80;n++)if(I(t,n*268)!=0)include(W(t,n*268+8),W(t,n*268+10));
                Func<float,float,PointF> plot=delegate(float x,float y)
                {
                    return new PointF(world.X+(x-minX)*world.Width/(maxX-minX),world.Y+(y-minY)*world.Height/(maxY-minY));
                };
                using(var grid=new Pen(Color.FromArgb(35,63,80)))for(int n=(int)minX/10*10;n<=maxX;n+=10)
                {
                    var a=plot(n,minY);
                    var b=plot(n,maxY);
                    e.Graphics.DrawLine(grid,a,b);
                }
                using(var grid=new Pen(Color.FromArgb(35,63,80)))for(int n=(int)minY/10*10;n<=maxY;n+=10)
                {
                    var a=plot(minX,n);
                    var b=plot(maxX,n);
                    e.Graphics.DrawLine(grid,a,b);
                }
                e.Graphics.DrawRectangle(Pens.SteelBlue,world.X,world.Y,world.Width,world.Height);
                int associated=W(f,air+8),homeFleet=associated>=0&&associated<1500?W(s,associated*268+12):7;
                if(homeFleet<0||homeFleet>=250)homeFleet=7;
                PointF ship=plot(W(f,battery+12),W(f,battery+14)),aircraft=plot(W(f,air+12),W(f,air+14)),home=plot(W(f,homeFleet*492+12),W(f,homeFleet*492+14));
                e.Graphics.FillRectangle(Brushes.LightSkyBlue,ship.X-5,ship.Y-5,10,10);
                e.Graphics.DrawString("Battery fleet "+W(f,battery+12)+", "+W(f,battery+14),owner.Font,Brushes.LightSkyBlue,world.X+8,world.Bottom-38);
                e.Graphics.DrawEllipse(Pens.LightSteelBlue,home.X-7,home.Y-7,14,14);
                e.Graphics.DrawString("Home "+W(f,homeFleet*492+12)+", "+W(f,homeFleet*492+14),owner.Font,Brushes.LightSteelBlue,world.X+8,world.Bottom-21);
                PointF prior=aircraft;
                for(int n=0;n<5;n++)
                {
                    int x=W(f,air+40+n*2);
                    if(x==-1)break;
                    var point=plot(x,W(f,air+50+n*2));
                    e.Graphics.DrawLine(Pens.Goldenrod,prior,point);
                    e.Graphics.DrawEllipse(Pens.Goldenrod,point.X-3,point.Y-3,6,6);
                    prior=point;
                }
                if(I(f,30*492)==11)
                {
                    var field=plot(W(f,30*492+12),W(f,30*492+14));
                    using(var pen=new Pen(Color.MediumAquamarine,2))e.Graphics.DrawRectangle(pen,field.X-7,field.Y-7,14,14);
                    e.Graphics.DrawString("Copied airfield "+W(f,30*492+12)+", "+W(f,30*492+14),owner.Font,Brushes.MediumAquamarine,world.X+8,world.Y+8);
                }
                if(I(f,air)==0)
                {
                    e.Graphics.DrawLine(Pens.Gray,aircraft.X-6,aircraft.Y-6,aircraft.X+6,aircraft.Y+6);
                    e.Graphics.DrawLine(Pens.Gray,aircraft.X-6,aircraft.Y+6,aircraft.X+6,aircraft.Y-6);
                }
                else e.Graphics.FillEllipse(I(f,air)>=6?Brushes.SlateGray:Brushes.Gold,aircraft.X-5,aircraft.Y-5,10,10);
                string aircraftLabel="Aircraft "+W(f,air+12)+", "+W(f,air+14);
                var labelSize=e.Graphics.MeasureString(aircraftLabel,owner.Font);
                float labelX=Math.Max(world.X+4,Math.Min(aircraft.X+8,world.Right-labelSize.Width-4)),labelY=Math.Max(world.Y+4,aircraft.Y-21);
                e.Graphics.DrawString(aircraftLabel,owner.Font,Brushes.Gold,labelX,labelY);
                for(int n=0;n<80;n++)if(I(t,n*268)!=0)
                {
                    var p=plot(W(t,n*268+8),W(t,n*268+10));
                    PointF origin=ship;
                    int source=W(t,n*268+16);
                    if(source>=0&&source<1500)
                    {
                        int parent=W(s,source*268+12);
                        if(parent>=0&&parent<250)origin=plot(W(f,parent*492+12)+W(s,source*268+8),W(f,parent*492+14)+W(s,source*268+10));
                    }
                    e.Graphics.DrawLine(Pens.SlateGray,origin,p);
                    e.Graphics.FillRectangle(Brushes.LightSkyBlue,origin.X-2,origin.Y-2,4,4);
                    e.Graphics.FillEllipse(Brushes.HotPink,p.X-4,p.Y-4,8,8);
                }
                e.Graphics.DrawString("Blue: battery · gold: aircraft / route · pink: AA shot",owner.Font,Brushes.LightSteelBlue,world.X,world.Bottom+6);
                float top=world.Bottom+44,size=Math.Max(20,Math.Min(Width-70,Height-top-34));
                var hull=new RectangleF((Width-size)/2,top,size,size);
                float cell=size/10;
                e.Graphics.DrawString("Aircraft hull · original cell data",owner.Font,Brushes.LightSteelBlue,10,top-22);
                for(int x=0;x<10;x++)for(int y=0;y<10;y++)
                {
                    int code=I(f,air+64+(x*10+y)*4);
                    using(var brush=new SolidBrush(code==8?Color.FromArgb(87,45,45):code==2?Color.FromArgb(93,161,197):code>=4&&code<=7?Color.FromArgb(161,120,61):Color.FromArgb(23,49,67)))e.Graphics.FillRectangle(brush,hull.X+x*cell,hull.Y+y*cell,cell,cell);
                }
                for(int n=0;n<=10;n++)
                {
                    e.Graphics.DrawLine(Pens.SlateGray,hull.X+n*cell,hull.Y,hull.X+n*cell,hull.Bottom);
                    e.Graphics.DrawLine(Pens.SlateGray,hull.X,hull.Y+n*cell,hull.Right,hull.Y+n*cell);
                }
                for(int n=0;n<6;n++)
                {
                    uint r=BitConverter.ToUInt32(f,air+16+n*4);
                    if(r==0)continue;
                    int u=(int)(r-0x4fc340);
                    if(I(s,u)==0)continue;
                    e.Graphics.DrawString((u/268).ToString(),owner.Font,Brushes.Black,hull.X+W(s,u+8)*cell+2,hull.Y+W(s,u+10)*cell+2);
                }
                e.Graphics.DrawString("Blue: intact · amber: equipment damage · red: lost row",owner.Font,Brushes.LightSteelBlue,10,Height-25);
            }
        }
    }
}
