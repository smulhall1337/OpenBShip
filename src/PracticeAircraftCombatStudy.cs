using System;
using System.IO;
using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
namespace FleetLab
{
    public sealed class PracticeAircraftCombatStudy : Form
    {
        sealed class QueueModeCheckBox : CheckBox
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if(Enabled)return;
                // The Windows themed disabled caption can become black on
                // this dark study surface. Keep the native glyph and a legible
                // caption while the pending-shot mode lock remains in force.
                int left=CheckBoxRenderer.GetGlyphSize(e.Graphics,System.Windows.Forms.VisualStyles.CheckBoxState.CheckedDisabled).Width+5;
                var textBounds=new Rectangle(left,0,Math.Max(0,Width-left),Height);
                Color background=BackColor.A==255?BackColor:Parent==null?Color.FromArgb(12,23,35):Parent.BackColor;
                using(var brush=new SolidBrush(background))e.Graphics.FillRectangle(brush,textBounds);
                TextRenderer.DrawText(e.Graphics,Text,Font,textBounds,Color.LightSteelBlue,
                    TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
            }
        }
        sealed class Choice
        {
            internal int Id;
            internal string Text;
            public override string ToString()
            {
                return Text;
            }
        }
        readonly PracticeTimedStudyState initial;
        readonly PracticeTimedWeaponCatalog weapons;
        readonly PracticeUnitCatalog units;
        readonly ClassicMessages names;
        readonly string programsRoot;
        readonly int targetFleet;
        PracticeTimedStudyState state;
        PracticeAircraftOrderStudyState orderStudy;
        readonly ComboBox source=new ComboBox(),weapon=new ComboBox(),target=new ComboBox();
        readonly NumericUpDown stock=new NumericUpDown(),row=new NumericUpDown();
        readonly Button equip=new Button(),fire=new Button(),one=new Button(),second=new Button(),paused=new Button(),boundary=new Button(),liveRow=new Button(),reset=new Button();
        readonly CheckBox queueMode=new QueueModeCheckBox();
        readonly Button nearby=new Button(),produce=new Button(),clearOrders=new Button();
        readonly Label queueHelp=new Label(),queueSummary=new Label();
        readonly TextBox requestRecords=new TextBox();
        Control[] queueControls;
        readonly Label summary=new Label(),battery=new Label(),feedback=new Label();
        readonly TextBox records=new TextBox(),outcome=new TextBox();
        readonly CombatCanvas canvas;
        readonly HashSet<Keys> held=new HashSet<Keys>();
        bool changing,equipped;
        int equippedSource=-1,equippedWeapon=-1,lastTargetId=-1,lastX,lastY;
        internal PracticeTimedStudyState StudyState
        {
            get
            {
                return state;
            }
        }
        internal bool ActiveShots
        {
            get
            {
                byte[] t=state.Objects.CopyTimedPool();
                for(int n=0;n<80;n++)if(BitConverter.ToInt32(t,n*268)!=0)return true;
                return false;
            }
        }
        int SourceId
        {
            get
            {
                return source.SelectedItem==null?-1:((Choice)source.SelectedItem).Id;
            }
        }
        int WeaponId
        {
            get
            {
                return weapon.SelectedItem==null?-1:((Choice)weapon.SelectedItem).Id;
            }
        }
        int TargetId
        {
            get
            {
                return target.SelectedItem==null?-1:((Choice)target.SelectedItem).Id;
            }
        }
        public PracticeAircraftCombatStudy(string programs,PracticeAircraftStudyState aircraft,ClassicMessages messages)
        {
            if(aircraft==null||!aircraft.HasGroup||aircraft.Stored)throw new ArgumentException("Activate an aircraft group before opening timed combat.","aircraft");
            names=messages;
            programsRoot=programs;
            targetFleet=aircraft.FleetSlot;
            weapons=PracticeTimedWeaponCatalog.Load(programs);
            units=PracticeUnitCatalog.Load(programs);
            state=initial=PracticeTimedStudyState.ForAircraft(weapons,units,File.ReadAllBytes(Path.Combine(programs,"ClassicVisuals.seqbin")),aircraft.CopyShipPool(),aircraft.CopyFleetPool(),0,731,PracticeDefenseTables.Load(programs),PracticeAircraftDestructionTable.Load(programs));
            Text="Timed aircraft combat · fleet "+targetFleet;
            Name="aircraftCombatStudy";
            StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1120,820);
            MinimumSize=new Size(880,640);
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
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,78));
            Controls.Add(layout);
            canvas=new CombatCanvas(this)
            {
                Dock=DockStyle.Fill,Name="airCombatCanvas",TabStop=true,AccessibleName="Schematic aircraft target grid and projectile travel"
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
                var b=c as Button;
                if(b!=null)
                {
                    b.FlatStyle=FlatStyle.Flat;
                    b.UseVisualStyleBackColor=false;
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
                Text="Copied AA test battery and fixed aircraft. The mount and ammunition are supplied test inputs. Shots use the recovered raw launch path; range/visibility admission, AI and live aircraft movement are separate."
            },90);
            add(new Label
            {
                Text="Opposing surface unit"
            },22);
            source.Name="airCombatSource";
            source.AccessibleName="Copied battery surface unit";
            source.DropDownStyle=ComboBoxStyle.DropDownList;
            add(source,30);
            byte[] ships=state.Objects.CopyShipPool(),fleets=state.Objects.CopyFleetPool();
            int owner=BitConverter.ToInt16(fleets,targetFleet*492+6);
            for(int n=0;n<1500;n++)
            {
                int type=BitConverter.ToInt32(ships,n*268);
                if(type<1||type>4||BitConverter.ToInt16(ships,n*268+16)<1||BitConverter.ToInt16(ships,n*268+6)==owner)continue;
                source.Items.Add(new Choice
                {
                    Id=n,Text="Unit "+n+" · "+UnitName(type)+" · player "+(BitConverter.ToInt16(ships,n*268+6)+1)
                });
            }
            if(source.Items.Count>0)
            {
                source.SelectedIndex=0;
                for(int n=0;n<source.Items.Count;n++)if(((Choice)source.Items[n]).Id==(owner==0?14:7))source.SelectedIndex=n;
            }
            add(new Label
            {
                Text="AA equipment"
            },22);
            weapon.Name="airCombatWeapon";
            weapon.AccessibleName="Test AA equipment";
            weapon.DropDownStyle=ComboBoxStyle.DropDownList;
            for(int n=1;n<=3;n++)weapon.Items.Add(new Choice
            {
                Id=n,Text="Equipment "+n
            });
            weapon.SelectedIndex=0;
            add(weapon,30);
            add(new Label
            {
                Text="Copied AA stock (1–20)"
            },22);
            stock.Name="airCombatStock";
            stock.AccessibleName="Copied test ammunition";
            stock.Minimum=1;
            stock.Maximum=20;
            stock.Value=2;
            add(stock,30);
            equip.Name="airCombatEquip";
            equip.Text="Equip / rearm copied battery";
            add(equip,34);
            battery.AccessibleName="Copied battery ammunition and mount";
            add(battery,44);
            add(new Label
            {
                Text="Aircraft target"
            },22);
            target.Name="airCombatTarget";
            target.AccessibleName="Aircraft target unit";
            target.DropDownStyle=ComboBoxStyle.DropDownList;
            add(target,30);
            add(new Label
            {
                Text="Hull row (1–length)"
            },22);
            row.Name="airCombatRow";
            row.AccessibleName="Selected aircraft hull row";
            row.Minimum=1;
            row.Maximum=6;
            row.Value=1;
            add(row,30);
            liveRow.Name="airCombatLiveRow";
            liveRow.Text="Choose live hull row";
            add(liveRow,32);
            fire.Name="airCombatFire";
            fire.Text="Fire test AA shot (raw)";
            add(fire,36);
            boundary.Name="airCombatBoundary";
            boundary.Text="Advance to next deadline";
            add(boundary,36);
            one.Name="airCombatOne";
            one.Text="+40 ms";
            add(one,32);
            second.Name="airCombatSecond";
            second.Text="+1 second";
            add(second,32);
            paused.Name="airCombatPaused";
            paused.Text="Pause +1 second";
            add(paused,32);
            queueMode.Name="airCombatQueueMode";
            queueMode.Text="Use recovered AA request queue";
            queueMode.AccessibleName="Optional recovered AA queue mode";
            add(queueMode,34);
            queueHelp.Text="Optional copied study. Prepare moves this aircraft fleet beside the battery fleet and sets that fleet's three visibility bytes to 255. Poll scans all eligible copied units. AI poll timing is separate.";
            add(queueHelp,94);
            nearby.Name="airCombatNearby";
            nearby.Text="Prepare nearby visible test target";
            add(nearby,36);
            produce.Name="airCombatProduce";
            produce.Text="Poll recovered request producer";
            add(produce,36);
            clearOrders.Name="airCombatClearOrders";
            clearOrders.Text="Clear all pending requests";
            add(clearOrders,34);
            queueSummary.Name="airCombatQueueSummary";
            queueSummary.AccessibleName="Firing admission and request queue totals";
            add(queueSummary,126);
            requestRecords.Name="airCombatRequestRecords";
            requestRecords.AccessibleName="Pending secondary queues and last generated requests";
            requestRecords.Multiline=true;
            requestRecords.ReadOnly=true;
            requestRecords.ScrollBars=ScrollBars.Vertical;
            requestRecords.BackColor=BackColor;
            requestRecords.ForeColor=ForeColor;
            add(requestRecords,160);
            queueControls=new Control[]
            {
                queueHelp,nearby,produce,clearOrders,queueSummary,requestRecords
            };
            foreach(var c in queueControls)c.Visible=false;
            summary.Name="airCombatSummary";
            summary.AccessibleName="Combat clock projectiles and copied scores";
            add(summary,116);
            outcome.Name="airCombatOutcome";
            outcome.AccessibleName="Aircraft impact outcome";
            outcome.Multiline=true;
            outcome.ReadOnly=true;
            outcome.ScrollBars=ScrollBars.Vertical;
            outcome.BackColor=BackColor;
            outcome.ForeColor=ForeColor;
            add(outcome,106);
            records.Name="airCombatRecords";
            records.AccessibleName="Active projectile and effect records";
            records.Multiline=true;
            records.ReadOnly=true;
            records.ScrollBars=ScrollBars.Vertical;
            records.BackColor=BackColor;
            records.ForeColor=ForeColor;
            add(records,172);
            reset.Name="airCombatReset";
            reset.Text="Reset combat study";
            add(reset,34);
            var close=new Button
            {
                Name="airCombatClose",Text="Close",DialogResult=DialogResult.Cancel
            };
            add(close,34);
            CancelButton=close;
            feedback.Name="airCombatFeedback";
            feedback.Dock=DockStyle.Fill;
            feedback.ForeColor=Color.LightSteelBlue;
            feedback.AccessibleName="Timed aircraft combat feedback";
            layout.Controls.Add(feedback,0,1);
            layout.SetColumnSpan(feedback,2);
            equip.Click+=delegate
            {
                Configure();
            };
            fire.Click+=delegate
            {
                Fire();
            };
            one.Click+=delegate
            {
                Advance(1,false);
            };
            second.Click+=delegate
            {
                Advance(25,false);
            };
            paused.Click+=delegate
            {
                Advance(25,true);
            };
            boundary.Click+=delegate
            {
                AdvanceBoundary();
            };
            liveRow.Click+=delegate
            {
                ChooseLiveRow();
            };
            queueMode.CheckedChanged+=delegate
            {
                ChangeQueueMode();
            };
            nearby.Click+=delegate
            {
                PrepareNearby();
            };
            produce.Click+=delegate
            {
                ProduceRequests();
            };
            clearOrders.Click+=delegate
            {
                ClearRequests();
            };
            reset.Click+=delegate
            {
                state=initial;
                orderStudy=null;
                changing=true;
                queueMode.Checked=false;
                changing=false;
                equipped=false;
                equippedSource=equippedWeapon=-1;
                canvas.CancelPick();
                RefreshState();
                feedback.Text="Combat pools restored. Parent aircraft study, battle and settings were preserved.";
            };
            source.SelectedIndexChanged+=delegate
            {
                if(!changing)RefreshSelection();
            };
            weapon.SelectedIndexChanged+=delegate
            {
                if(!changing)RefreshSelection();
            };
            target.SelectedIndexChanged+=delegate
            {
                if(!changing)
                {
                    row.Value=1;
                    RefreshSelection();
                }
            };
            row.ValueChanged+=delegate
            {
                if(!changing)RefreshSelection();
            };
            KeyUp+=delegate(object sender,KeyEventArgs e)
            {
                held.Remove(e.KeyCode);
            };
            Deactivate+=delegate
            {
                held.Clear();
                canvas.CancelPick();
            };
            RefreshState();
            feedback.Text="Equip the copied battery, choose a hull row, then fire and advance time. Display and effect records are schematic; no video or audio is played.";
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
                var found=FocusedButton(child);
                if(found!=null)return found;
            }
            return null;
        }
        protected override bool ProcessCmdKey(ref Message m,Keys keyData)
        {
            var b=FocusedButton(this);
            if(keyData==Keys.Enter&&b!=null)
            {
                bool repeat=(m.LParam.ToInt64()&0x40000000L)!=0;
                if(!repeat&&held.Add(Keys.Enter)&&b.Enabled)b.PerformClick();
                return true;
            }
            return base.ProcessCmdKey(ref m,keyData);
        }
        void ChangeQueueMode()
        {
            if(changing)return;
            try
            {
                if(ActiveShots||(orderStudy!=null&&orderStudy.QueuedSources>0))throw new InvalidOperationException("Resolve or clear pending requests and projectiles before changing queue mode.");
                orderStudy=queueMode.Checked?PracticeAircraftOrderStudyState.ForStudy(state,weapons,units, PracticeFiringRangeTable.Load(programsRoot),PracticeVisibility.Load(programsRoot),PracticeFiringRateTable.Load(programsRoot)):null;
                RefreshState();
                feedback.Text=orderStudy==null?"Raw test-shot mode selected.":"Queue mode selected. Prepare a nearby visible copied target, queue a shot or poll the recovered producer, then advance time. Poll timing remains explicit.";
            }
            catch(IOException e)
            {
                RestoreQueueMode("Queue resources unavailable. Run Setup again. "+e.Message);
            }
            catch(InvalidDataException e)
            {
                RestoreQueueMode("Queue resources unavailable. Run Setup again. "+e.Message);
            }
            catch(UnauthorizedAccessException e)
            {
                RestoreQueueMode("Queue resources unavailable. Run Setup again. "+e.Message);
            }
            catch(NotSupportedException e)
            {
                RestoreQueueMode(e.Message);
            }
            catch(InvalidOperationException e)
            {
                RestoreQueueMode(e.Message);
            }
        }
        void RestoreQueueMode(string message)
        {
            changing=true;
            queueMode.Checked=orderStudy!=null;
            changing=false;
            RefreshState();
            feedback.Text=message;
        }
        void PrepareNearby()
        {
            if(!nearby.Enabled)return;
            byte[] ships=state.Objects.CopyShipPool(),fleets=state.Objects.CopyFleetPool();
            int parent=BitConverter.ToInt16(ships,SourceId*268+12);
            int x=BitConverter.ToInt16(fleets,parent*492+12),y=BitConverter.ToInt16(fleets,parent*492+14);
            int previousX=BitConverter.ToInt16(fleets,targetFleet*492+12),previousY=BitConverter.ToInt16(fleets,targetFleet*492+14);
            int nearbyX=x==short.MaxValue?x-1:x+1;
            BitConverter.GetBytes((short)nearbyX).CopyTo(fleets,targetFleet*492+12);
            BitConverter.GetBytes((short)y).CopyTo(fleets,targetFleet*492+14);
            for(int n=464;n<=466;n++)fleets[parent*492+n]=255;
            var edited=state.WithFleetPool(fleets);
            var next=orderStudy.WithCombatForStudy(edited);
            orderStudy=next;
            state=next.Combat;
            RefreshState();
            feedback.Text="Copied aircraft origin "+previousX+","+previousY+" → "+nearbyX+","+y+". Copied battery fleet visibility bytes set to 255. Parent study and source material preserved.";
        }
        void ProduceRequests()
        {
            if(!produce.Enabled)return;
            try
            {
                var next=orderStudy.ProduceRequests();
                orderStudy=next;
                state=next.Combat;
                RefreshState();
                feedback.Text="Explicit producer poll "+next.RequestPolls+": "+next.LastGenerated+" requests, "+next.LastAccepted+" accepted, "+next.LastRequestDraws+" RNG draws. All eligible copied units were scanned.";
            }
            catch(InvalidDataException e)
            {
                feedback.Text=e.Message;
            }
            catch(NotSupportedException e)
            {
                feedback.Text=e.Message;
            }
        }
        void ClearRequests()
        {
            if(!clearOrders.Enabled)return;
            var next=orderStudy;
            byte[] pending=next.CopySecondaryOrders();
            int count=0;
            for(int n=0;n<1500;n++)if(BitConverter.ToInt16(pending,n*52+8)!=-1)
            {
                next=next.Clear(n);
                count++;
            }
            orderStudy=next;
            state=next.Combat;
            RefreshState();
            feedback.Text="Cleared "+count+" copied request queues. Active projectiles continue.";
        }
        void AdvanceQueueEvent()
        {
            UseWaitCursor=true;
            try
            {
                var next=orderStudy;
                int beforeCallbacks=state.ExpiryCallbacks,beforeEffects=state.ActiveEffects,count=0;
                for(;count<5000;)
                {
                    next=next.AdvanceFrame(false);
                    count++;
                    if(next.LastConsumed>0||next.LastRejected>0||next.Combat.ExpiryCallbacks!=beforeCallbacks||next.Combat.ActiveEffects!=beforeEffects)break;
                }
                orderStudy=next;
                state=next.Combat;
                RefreshState();
                feedback.Text="Advanced "+count+" source updates. Queued shots created "+next.CreatedObjects+" · rejected orders "+next.RejectedOrders+" · impact callbacks "+state.ExpiryCallbacks+".";
            }
            catch(InvalidDataException e)
            {
                feedback.Text=e.Message;
            }
            catch(NotSupportedException e)
            {
                feedback.Text=e.Message;
            }
            finally
            {
                UseWaitCursor=false;
            }
        }
        void RefreshQueueRecords()
        {
            if(orderStudy==null)
            {
                queueSummary.Text="";
                requestRecords.Text="";
                return;
            }
            string admission="No selected source or live target";
            if(SourceId>=0&&TargetId>=0)
            {
                try
                {
                    var check=orderStudy.CheckFiring(SourceId,targetFleet,WeaponId);
                    admission=check.Allowed?"Allowed":check.Denial.ToString();
                    if(check.Distance>=0)admission+=" · distance "+check.Distance+" / range "+check.Range;
                }
                catch(InvalidDataException e)
                {
                    admission=e.Message;
                }
                catch(NotSupportedException e)
                {
                    admission=e.Message;
                }
            }
            queueSummary.Text="Clock "+state.ClockMilliseconds+" ms · RNG "+state.RandomState+"\r\nSelection admission: "+admission+"\r\nQueued sources "+orderStudy.QueuedSources+" · fired "+orderStudy.CreatedObjects+"\r\nConsumed "+orderStudy.ConsumedOrders+" · rejected "+orderStudy.RejectedOrders+"\r\nExplicit producer polls "+orderStudy.RequestPolls+"\r\nLast poll: "+orderStudy.LastGenerated+" requests / "+orderStudy.LastAccepted+" accepted / "+orderStudy.LastRequestDraws+" draws";
            var text=new StringBuilder("Pending secondary queues:\r\n");
            byte[] pending=orderStudy.CopySecondaryOrders();
            for(int n=0;n<1500;n++)
            {
                int at=n*52;
                if(BitConverter.ToInt16(pending,at+8)==-1)continue;
                text.Append("Unit "+n+" · AA "+BitConverter.ToInt32(pending,at)+" · timer "+BitConverter.ToInt32(pending,at+4)+": ");
                for(int p=0;p<10;p++)
                {
                    int x=BitConverter.ToInt16(pending,at+8+p*2);
                    if(x!=-1)text.Append("("+x+","+BitConverter.ToInt16(pending,at+28+p*2)+") ");
                }
                text.AppendLine();
            }
            text.AppendLine("Last recovered producer poll:");
            foreach(var choice in orderStudy.LastRequests)text.AppendLine("Unit "+choice.SourceShip+" → fleet "+choice.TargetFleet+" / unit "+choice.TargetShip+" · AA "+choice.Weapon+" · "+choice.X+","+choice.Y+" · return "+choice.QueueReturnCode);
            requestRecords.Text=text.ToString();
        }
        void Configure()
        {
            if(!equip.Enabled)return;
            try
            {
                var before=state.Objects.CopyShipPool();
                int previous=BitConverter.ToInt32(before,SourceId*268+100);
                var configured=PracticeAircraftTestBattery.Configure(state,weapons,units,SourceId,WeaponId,(int)stock.Value);
                var nextOrders=orderStudy==null?null:orderStudy.WithCombatForStudy(configured);
                state=configured;
                orderStudy=nextOrders;
                equipped=true;
                equippedSource=SourceId;
                equippedWeapon=WeaponId;
                RefreshState();
                feedback.Text="Copied unit "+SourceId+": first equipment cell "+previous+" → "+WeaponId+", AA stock set to "+stock.Value+". Source material was preserved.";
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
        void Fire()
        {
            if(!fire.Enabled)return;
            try
            {
                var ships=state.Objects.CopyShipPool();
                lastTargetId=TargetId;
                lastX=BitConverter.ToInt16(ships,TargetId*268+8);
                lastY=BitConverter.ToInt16(ships,TargetId*268+10)+(int)row.Value-1;
                if(orderStudy!=null)
                {
                    orderStudy=orderStudy.Queue(SourceId,targetFleet,WeaponId,lastX,lastY);
                    state=orderStudy.Combat;
                    RefreshState();
                    feedback.Text="Queued copied AA request: return "+orderStudy.LastQueueReturnCode+", slot "+orderStudy.LastQueueSlot+". Admission occurs when its timer is due.";
                    return;
                }
                state=state.LaunchAntiAircraft(WeaponId,SourceId,targetFleet,lastX,lastY);
                RefreshState();
                feedback.Text=state.LastCreationOutcome==PracticeTimedCreationOutcome.Created?"Created projectile "+state.LastCreatedSlot+". Advance time to travel and impact; original rounding may hold early coordinates.":"No projectile created: "+state.LastCreationOutcome+".";
            }
            catch(InvalidDataException e)
            {
                feedback.Text=e.Message;
            }
            catch(NotSupportedException e)
            {
                feedback.Text=e.Message;
            }
        }
        void Advance(int count,bool pause)
        {
            if(count<1||count>5000)return;
            UseWaitCursor=true;
            try
            {
                if(orderStudy==null)
                {
                    var next=state;
                    for(int n=0;n<count;n++)next=next.AdvanceFrame(pause);
                    state=next;
                }
                else
                {
                    var next=orderStudy;
                    for(int n=0;n<count;n++)next=next.AdvanceFrame(pause);
                    orderStudy=next;
                    state=next.Combat;
                }
                RefreshState();
                feedback.Text=pause?"Clock advanced while flight, effects and RNG were held; active deadlines were compensated.":"Advanced "+count+" source updates. Impact callbacks "+state.ExpiryCallbacks+" · destroyed aircraft "+state.DestroyedUnits+".";
            }
            catch(InvalidDataException e)
            {
                feedback.Text=e.Message;
            }
            catch(NotSupportedException e)
            {
                feedback.Text=e.Message;
            }
            finally
            {
                UseWaitCursor=false;
            }
        }
        void AdvanceBoundary()
        {
            if(!boundary.Enabled)return;
            if(orderStudy!=null)
            {
                AdvanceQueueEvent();
                return;
            }
            byte[] timed=state.Objects.CopyTimedPool();
            int least=int.MaxValue;
            for(int n=0;n<80;n++)
            {
                int at=n*268;
                if(BitConverter.ToInt32(timed,at)==0)continue;
                int remaining=unchecked((int)(BitConverter.ToUInt32(timed,at+24)-state.ClockMilliseconds));
                least=Math.Min(least,remaining);
            }
            int frames=least<=0?1:(int)Math.Min(5000,((long)least+39)/40);
            Advance(frames,false);
        }
        void ChooseLiveRow()
        {
            if(TargetId<0||ActiveShots)return;
            var ships=state.Objects.CopyShipPool();
            var fleets=state.Objects.CopyFleetPool();
            int u=TargetId*268,x=BitConverter.ToInt16(ships,u+8),y=BitConverter.ToInt16(ships,u+10),length=BitConverter.ToInt16(ships,u+14);
            for(int n=0;n<length;n++)if(BitConverter.ToInt32(fleets,targetFleet*492+64+(x*10+y+n)*4)!=8)
            {
                row.Value=n+1;
                feedback.Text="Selected live hull row "+(n+1)+". No game data changed.";
                return;
            }
        }
        void RefreshState()
        {
            int previous=TargetId;
            changing=true;
            target.Items.Clear();
            var ships=state.Objects.CopyShipPool();
            var fleets=state.Objects.CopyFleetPool();
            for(int n=0;n<6;n++)
            {
                uint r=BitConverter.ToUInt32(fleets,targetFleet*492+16+n*4);
                if(r<0x4fc340||r>=0x4fc340+402000||(r-0x4fc340)%268!=0)continue;
                int slot=(int)(r-0x4fc340)/268,type=BitConverter.ToInt32(ships,slot*268);
                if(type<8||type>10)continue;
                int added=target.Items.Add(new Choice
                {
                    Id=slot,Text="Unit "+slot+" · "+UnitName(type)+" · HP "+BitConverter.ToInt16(ships,slot*268+16)
                });
                if(slot==previous)target.SelectedIndex=added;
            }
            if(target.SelectedIndex<0&&target.Items.Count>0)target.SelectedIndex=0;
            changing=false;
            byte[] timed=state.Objects.CopyTimedPool();
            int active=0;
            var text=new StringBuilder();
            for(int n=0;n<80;n++)
            {
                int at=n*268;
                if(BitConverter.ToInt32(timed,at)==0)continue;
                active++;
                text.AppendLine("Shot "+n+" · AA "+BitConverter.ToInt32(timed,at)+" · "+BitConverter.ToInt16(timed,at+8)+","+BitConverter.ToInt16(timed,at+10)+" · deadline "+BitConverter.ToUInt32(timed,at+24));
            }
            text.AppendLine("Effect records:");
            var effects=state.CopyEffectPool();
            for(int n=0;n<40;n++)
            {
                int at=n*20;
                if(BitConverter.ToInt16(effects,at)==-1)continue;
                text.AppendLine("Effect "+n+" · type "+BitConverter.ToInt32(effects,at+16)+" · counter "+BitConverter.ToInt16(effects,at+12));
            }
            records.Text=text.ToString();
            byte[] score=state.CopyScoreRegion();
            summary.Text="Clock "+state.ClockMilliseconds+" ms · "+active+" projectiles\r\nEffects "+state.ActiveEffects+" · expired "+state.ExpiredObjects+"\r\nRNG "+state.RandomState+" · draws "+state.RandomCalls+"\r\nScores P1 "+BitConverter.ToInt32(score,0)+" / P2 "+BitConverter.ToInt32(score,44)+"\r\nMovie-selection counter "+state.MovieCounter;
            outcome.Text=state.LastImpactOutcome.HasValue?"Last outcome: "+state.LastImpactOutcome+"\r\nVictim unit "+state.LastVictimShip+" · points "+state.LastImpactPoints+"\r\nDestroyed aircraft "+state.DestroyedUnits+" · scatter attempts "+state.LastScatterAttempts:"No impact callback yet.";
            if(active>0)canvas.CancelPick();
            RefreshSelection();
        }
        void RefreshSelection()
        {
            if(changing)return;
            bool active=ActiveShots;
            bool queued=orderStudy!=null&&orderStudy.QueuedSources>0;
            source.Enabled=weapon.Enabled=stock.Enabled=equip.Enabled=!active&&!queued&&source.Items.Count>0;
            target.Enabled=row.Enabled=liveRow.Enabled=!active&&TargetId>=0;
            boundary.Enabled=active||queued;
            queueMode.Enabled=!active&&!queued;
            // Showing previously uncreated WinForms children can change their
            // z-order, which FlowLayoutPanel also uses as its layout order.
            // Restore the optional block immediately after its checkbox.
            int queueIndex=queueMode.Parent.Controls.GetChildIndex(queueMode)+1;
            foreach(var c in queueControls) {
                c.Visible=orderStudy!=null;
                queueMode.Parent.Controls.SetChildIndex(c,queueIndex++);
            }
            nearby.Enabled=orderStudy!=null&&!active&&!queued&&SourceId>=0&&TargetId>=0;
            produce.Enabled=orderStudy!=null&&equipped;
            clearOrders.Enabled=orderStudy!=null&&queued;
            fire.Text=orderStudy==null?"Fire test AA shot (raw)":"Queue selected AA request (raw)";
            boundary.Text=orderStudy==null?"Advance to next deadline":"Advance to next firing / impact";
            var ships=state.Objects.CopyShipPool();
            if(TargetId>=0)
            {
                int length=BitConverter.ToInt16(ships,TargetId*268+14);
                changing=true;
                row.Maximum=Math.Max(1,length);
                if(row.Value>row.Maximum)row.Value=row.Maximum;
                changing=false;
            }
            int count=SourceId<0||WeaponId<1?0:BitConverter.ToInt16(ships,SourceId*268+196+WeaponId*2);
            fire.Enabled=equipped&&equippedSource==SourceId&&equippedWeapon==WeaponId&&TargetId>=0&&(orderStudy!=null||!active||TargetId==lastTargetId)&&(orderStudy!=null||count==-1||count>0);
            battery.Text=SourceId<0?"No opposing surface unit is available.":"Copied unit "+SourceId+" · AA stock "+count+"\r\n"+(equipped&&equippedSource==SourceId&&equippedWeapon==WeaponId?"Copied mount prepared.":"Equip / rearm the copied battery before firing.");
            RefreshQueueRecords();
            canvas.Invalidate();
        }
        void PickCell(Point cell)
        {
            if(ActiveShots)return;
            var ships=state.Objects.CopyShipPool();
            for(int n=0;n<target.Items.Count;n++)
            {
                int id=((Choice)target.Items[n]).Id,u=id*268,x=BitConverter.ToInt16(ships,u+8),y=BitConverter.ToInt16(ships,u+10),length=BitConverter.ToInt16(ships,u+14);
                if(cell.X==x&&cell.Y>=y&&cell.Y<y+length)
                {
                    target.SelectedIndex=n;
                    row.Value=cell.Y-y+1;
                    return;
                }
            }
        }
        // Canvas implementation below keeps all drawing schematic and reads
        // copied source coordinates without changing the simulation.
        sealed class CombatCanvas : Control
        {
            readonly PracticeAircraftCombatStudy owner;
            Point? pending;
            internal CombatCanvas(PracticeAircraftCombatStudy value)
            {
                owner=value;
                DoubleBuffered=true;
                ResizeRedraw=true;
            }
            internal RectangleF GridBounds
            {
                get
                {
                    float size=Math.Max(0,Math.Min(Width-28,Height-195));
                    return new RectangleF((Width-size)/2,38,size,size);
                }
            }
            internal void CancelPick()
            {
                pending=null;
                if(IsHandleCreated&&!IsDisposed)Capture=false;
            }
            Point? Cell(Point p)
            {
                var b=GridBounds;
                if(b.Width<10||!b.Contains(p))return null;
                return new Point((int)((p.X-b.X)*10/b.Width),(int)((p.Y-b.Y)*10/b.Height));
            }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if(e.Button==MouseButtons.Left&&!owner.ActiveShots)
                {
                    Focus();
                    pending=Cell(e.Location);
                    Capture=pending.HasValue;
                }
            }
            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if(e.Button!=MouseButtons.Left)return;
                var first=pending;
                var last=Cell(e.Location);
                CancelPick();
                if(first.HasValue&&last.HasValue&&first.Value==last.Value&&!owner.ActiveShots)owner.PickCell(last.Value);
            }
            protected override void OnLostFocus(EventArgs e)
            {
                CancelPick();
                base.OnLostFocus(e);
            }
            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if(e.KeyCode!=Keys.Left&&e.KeyCode!=Keys.Right&&e.KeyCode!=Keys.Up&&e.KeyCode!=Keys.Down)return;
                e.Handled=true;
                e.SuppressKeyPress=true;
                if(owner.ActiveShots)return;
                if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Right)
                {
                    if(owner.target.Items.Count>0)owner.target.SelectedIndex=Math.Max(0,Math.Min(owner.target.Items.Count-1,owner.target.SelectedIndex+(e.KeyCode==Keys.Left?-1:1)));
                }
                else owner.row.Value=Math.Max(owner.row.Minimum,Math.Min(owner.row.Maximum,owner.row.Value+(e.KeyCode==Keys.Up?-1:1)));
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var b=GridBounds;
                if(b.Width<10)return;
                float step=b.Width/10;
                var ships=owner.state.Objects.CopyShipPool();
                var fleets=owner.state.Objects.CopyFleetPool();
                int fleet=owner.targetFleet*492;
                e.Graphics.DrawString("Aircraft target fleet · schematic",owner.Font,Brushes.LightSteelBlue,8,8);
                for(int x=0;x<10;x++)for(int y=0;y<10;y++)
                {
                    int code=BitConverter.ToInt32(fleets,fleet+64+(x*10+y)*4);
                    Color color=code==8?Color.FromArgb(87,45,45):code==2?Color.FromArgb(93,161,197):code>=4&&code<=7?Color.FromArgb(161,120,61):Color.FromArgb(23,49,67);
                    using(var brush=new SolidBrush(color))e.Graphics.FillRectangle(brush,b.X+x*step,b.Y+y*step,step,step);
                }
                using(var pen=new Pen(Color.FromArgb(63,97,115)))for(int n=0;n<=10;n++)
                {
                    e.Graphics.DrawLine(pen,b.X+n*step,b.Y,b.X+n*step,b.Bottom);
                    e.Graphics.DrawLine(pen,b.X,b.Y+n*step,b.Right,b.Y+n*step);
                }
                for(int n=0;n<owner.target.Items.Count;n++)
                {
                    int id=((Choice)owner.target.Items[n]).Id,u=id*268,x=BitConverter.ToInt16(ships,u+8),y=BitConverter.ToInt16(ships,u+10),length=BitConverter.ToInt16(ships,u+14);
                    e.Graphics.DrawString(id.ToString(),owner.Font,Brushes.Black,b.X+x*step+3,b.Y+y*step+3);
                    if(id==owner.TargetId)
                    {
                        using(var pen=new Pen(Color.Gold,2))
                        {
                            e.Graphics.DrawRectangle(pen,b.X+x*step+1,b.Y+y*step+1,step-2,length*step-2);
                            e.Graphics.DrawRectangle(pen,b.X+x*step+3,b.Y+(y+(int)owner.row.Value-1)*step+3,step-6,step-6);
                        }
                    }
                }
                e.Graphics.DrawString("Blue: hull · amber: equipment damage · red: lost row",owner.Font,Brushes.LightSteelBlue,b.X,b.Bottom+8);
                var world=new RectangleF(16,b.Bottom+46,Math.Max(1,Width-32),86);
                using(var pen=new Pen(Color.SteelBlue))e.Graphics.DrawRectangle(pen,world.X,world.Y,world.Width,world.Height);
                if(owner.SourceId<0)return;
                int source=owner.SourceId*268,parent=BitConverter.ToInt16(ships,source+12);
                Point start=new Point(BitConverter.ToInt16(fleets,parent*492+12)+BitConverter.ToInt16(ships,source+8),BitConverter.ToInt16(fleets,parent*492+14)+BitConverter.ToInt16(ships,source+10));
                int tx=owner.lastX,ty=owner.lastY;
                if(owner.TargetId>=0)
                {
                    int u=owner.TargetId*268;
                    tx=BitConverter.ToInt16(ships,u+8);
                    ty=BitConverter.ToInt16(ships,u+10)+(int)owner.row.Value-1;
                }
                Point end=new Point(BitConverter.ToInt16(fleets,fleet+12)+tx,BitConverter.ToInt16(fleets,fleet+14)+ty);
                var timed=owner.state.Objects.CopyTimedPool();
                var starts=new List<Point>();var ends=new List<Point>();var positions=new List<Point>();
                for(int n=0;n<80;n++)if(BitConverter.ToInt32(timed,n*268)!=0)
                {
                    int at=n*268,unit=BitConverter.ToInt16(timed,at+16),target=BitConverter.ToInt16(timed,at+12);
                    positions.Add(new Point(BitConverter.ToInt16(timed,at+8),BitConverter.ToInt16(timed,at+10)));
                    if(unit<0||unit>=1500||target<0||target>=250)continue;
                    int origin=BitConverter.ToInt16(ships,unit*268+12);if(origin<0||origin>=250)continue;
                    starts.Add(new Point(BitConverter.ToInt16(fleets,origin*492+12)+BitConverter.ToInt16(ships,unit*268+8),BitConverter.ToInt16(fleets,origin*492+14)+BitConverter.ToInt16(ships,unit*268+10)));
                    ends.Add(new Point(BitConverter.ToInt16(fleets,target*492+12)+BitConverter.ToInt16(timed,at+18),BitConverter.ToInt16(fleets,target*492+14)+BitConverter.ToInt16(timed,at+20)));
                }
                if(starts.Count==0){starts.Add(start);ends.Add(end);}
                int minX=starts[0].X,maxX=minX,minY=starts[0].Y,maxY=minY;
                Action<Point> include=delegate(Point p){minX=Math.Min(minX,p.X);maxX=Math.Max(maxX,p.X);minY=Math.Min(minY,p.Y);maxY=Math.Max(maxY,p.Y);};
                foreach(var p in starts)include(p);foreach(var p in ends)include(p);foreach(var p in positions)include(p);
                Func<Point,PointF> plot=delegate(Point p)
                {
                    return new PointF(world.X+18+(p.X-minX)*(world.Width-36)/Math.Max(1,maxX-minX),world.Y+18+(p.Y-minY)*(world.Height-36)/Math.Max(1,maxY-minY));
                };
                for(int n=0;n<starts.Count;n++) {
                    var a=plot(starts[n]);var z=plot(ends[n]);e.Graphics.DrawLine(Pens.SlateGray,a,z);
                    e.Graphics.FillEllipse(Brushes.LightSkyBlue,a.X-4,a.Y-4,8,8);e.Graphics.FillEllipse(Brushes.Gold,z.X-4,z.Y-4,8,8);
                    e.Graphics.DrawString("S",owner.Font,Brushes.WhiteSmoke,a.X,a.Y+4);e.Graphics.DrawString("T",owner.Font,Brushes.WhiteSmoke,z.X,z.Y+4);
                }
                foreach(var position in positions){var p=plot(position);e.Graphics.FillEllipse(Brushes.HotPink,p.X-4,p.Y-4,8,8);}
                e.Graphics.DrawString("World travel · blue: sources · gold: targets · pink: shots",owner.Font,Brushes.LightSteelBlue,world.X,world.Bottom+4);
            }
        }
    }
}
