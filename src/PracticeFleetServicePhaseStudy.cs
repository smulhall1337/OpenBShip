using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace FleetLab
{
    public sealed class PracticeFleetServicePhaseStudy : Form
    {
        readonly string programs;
        readonly PracticeMapStartup startup;
        readonly ComboBox scenario=new ComboBox(),kind=new ComboBox(),counter=new ComboBox(),viewUnit=new ComboBox(),bay=new ComboBox();
        readonly CheckBox pause=new CheckBox();
        readonly Button load=new Button(),reset=new Button(),one=new Button(),thirteen=new Button(),batch=new Button(),nextPhase=new Button();
        readonly Label summary=new Label(),feedback=new Label();
        readonly TextBox details=new TextBox();
        readonly PhaseCanvas canvas;
        readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeFleetServicePhaseClock state,initial;
        byte[] ships,fleets;
        bool changing;
        internal PracticeFleetServicePhaseClock StudyState {get{return state;}}
        int ViewedUnit {get{return viewUnit.SelectedItem==null?-1:(int)viewUnit.SelectedItem;}}
        int Bay {get{return bay.SelectedIndex==0?2:3;}}
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeFleetServicePhaseStudy(string programs,PracticeMapStartup startup)
        {
            this.programs=programs;this.startup=startup;
            Name="fleetServicePhaseStudy";Text="Service phase clock · copied source gates";StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1160,840);MinimumSize=new Size(960,720);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,360));Controls.Add(layout);
            canvas=new PhaseCanvas(this){Name="servicePhaseCanvas",Dock=DockStyle.Fill};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Name="servicePhaseScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);
            var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control control,int height)
            {
                control.Width=326;control.Height=height;control.Margin=new Padding(0,3,0,3);
                if(control is Button||control is ComboBox){control.BackColor=Color.FromArgb(28,55,76);control.ForeColor=ForeColor;}
                var button=control as Button;if(button!=null){button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;button.EnabledChanged+=delegate{if(button.IsDisposed||button.Disposing)return;button.BackColor=button.Enabled?Color.FromArgb(28,55,76):Color.LightGray;button.ForeColor=button.Enabled?ForeColor:Color.Black;};}side.Controls.Add(control);
            };
            add(new Label{Text="Separate copied source phases: task dispatch every 32 counts, depot support at phase 13 of 64. Pause holds both. Contact/owner and presets remain supplied. No full AI policy, movement, AA or natural application clock runs."},96);
            add(new Label{Text="Case / task / counter (pending until Load)"},22);
            scenario.Name="servicePhaseCase";scenario.DropDownStyle=ComboBoxStyle.DropDownList;foreach(string id in PracticeFleetServicePhaseClock.CaseIds)scenario.Items.Add(new CaseChoice(id));scenario.SelectedIndex=0;add(scenario,30);
            kind.Name="servicePhaseKind";kind.DropDownStyle=ComboBoxStyle.DropDownList;kind.Items.AddRange(new object[]{"11 · friendly contact, then service","12 · supplied lone service task"});kind.SelectedIndex=0;add(kind,30);
            counter.Name="servicePhaseCounter";counter.DropDownStyle=ComboBoxStyle.DropDownList;foreach(uint value in PracticeFleetServicePhaseClock.InitialCounters)counter.Items.Add(new CounterChoice(value));counter.SelectedIndex=0;add(counter,30);
            load.Name="servicePhaseLoad";load.Text="Load selected clock copy";add(load,34);reset.Name="servicePhaseReset";reset.Text="Reset applied clock copy";add(reset,34);
            summary.Name="servicePhaseSummary";add(summary,196);feedback.Name="servicePhaseFeedback";add(feedback,84);
            pause.Name="servicePhasePause";pause.Text="Pause source polls (hold counter and data)";add(pause,30);
            one.Name="servicePhaseOne";one.Text="1 source poll";add(one,34);thirteen.Name="servicePhaseThirteen";thirteen.Text="13 source polls";add(thirteen,34);batch.Name="servicePhaseBatch";batch.Text="32 source polls";add(batch,34);nextPhase.Name="servicePhaseNext";nextPhase.Text="Advance to next scheduled phase";add(nextPhase,34);
            add(new Label{Text="View ship / lower bay (inspection only)"},22);viewUnit.Name="servicePhaseUnit";viewUnit.DropDownStyle=ComboBoxStyle.DropDownList;add(viewUnit,30);
            bay.Name="servicePhaseBay";bay.DropDownStyle=ComboBoxStyle.DropDownList;bay.Items.AddRange(new object[]{"Bay fleet 2","Bay fleet 3"});bay.SelectedIndex=1;add(bay,30);
            details.Name="servicePhaseDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,270);
            var close=new Button{Name="servicePhaseClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            load.Click+=delegate{LoadCopy();};reset.Click+=delegate{if(initial!=null){state=initial;Feedback("Applied clock copy reset. Pending case/task/counter choices and Pause are unchanged.");Sync(-1);}};
            one.Click+=delegate{Advance(1);};thirteen.Click+=delegate{Advance(13);};batch.Click+=delegate{Advance(32);};nextPhase.Click+=delegate{if(state!=null&&!pause.Checked)Advance(Math.Min(state.NextDispatcherIn,state.NextWorkerIn));};
            scenario.SelectedIndexChanged+=delegate{Pending();};kind.SelectedIndexChanged+=delegate{Pending();};counter.SelectedIndexChanged+=delegate{Pending();};
            pause.CheckedChanged+=delegate{if(state!=null){Feedback(pause.Checked?"Polls are paused: requested poll count still advances inside the384-poll window; phase counter and service data hold.":"Polls are unpaused. Gates use the incremented source counter; kind0 does not stop scheduled visits.");Sync(ViewedUnit);}};
            viewUnit.SelectedIndexChanged+=delegate{if(!changing){FollowViewedBay();RefreshDetails();}};bay.SelectedIndexChanged+=delegate{if(!changing)RefreshDetails();};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{held.Clear();};Activated+=delegate{foreach(var key in new[]{Keys.Enter,Keys.Space,Keys.Escape})if((GetAsyncKeyState((int)key)&0x8000)!=0)held.Add(key);};
            LoadCopy();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);if(e.KeyCode!=Keys.Enter&&e.KeyCode!=Keys.Space&&e.KeyCode!=Keys.Escape)return;var button=ActiveControl as Button;if(e.KeyCode!=Keys.Escape&&button==null)return;
            e.Handled=e.SuppressKeyPress=true;if(!held.Add(e.KeyCode))return;if(e.KeyCode==Keys.Escape)Close();else if(button.Enabled)button.PerformClick();
        }
        void Feedback(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=ForeColor;feedback.Text=text;}
        void Pending(){if(state!=null)Feedback("Case/task/counter is pending. Load applies it; Reset restores the applied copy.");}
        void LoadCopy()
        {
            try{var clock=PracticeFleetServicePhaseClock.Create(programs,startup,((CaseChoice)scenario.SelectedItem).Id,kind.SelectedIndex+11,((CounterChoice)counter.SelectedItem).Value);state=initial=clock;Feedback("Copied initialized task loaded. Advance to source phases; the first worker may run before admission.");Sync(-1);}
            catch(ArgumentException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(UnauthorizedAccessException e){Reject(e.Message);}
        }
        void Reject(string reason){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.Orange;feedback.Text="Rejected; current copy preserved. "+reason;Sync(ViewedUnit);}
        void Advance(int requested)
        {
            if(state==null)return;
            try
            {
                int preferred=ViewedUnit;var before=state.CopyShipPool();var next=state.AdvanceMany(Math.Min(requested,state.RemainingPolls),pause.Checked);var after=next.CopyShipPool();
                foreach(int unit in next.CopyOriginalUnits())if(BitConverter.ToInt16(before,unit*268+12)==next.SourceFleet&&BitConverter.ToInt16(after,unit*268+12)!=next.SourceFleet)preferred=unit;
                state=next;Feedback(next.RemainingPolls==0?"Captured384-poll window complete. Reset to repeat; task0 is not mission completion.":pause.Checked?"Paused polls recorded; source counter and simulation data held.":"Source phases applied. Worker return and task queue completion can occur on different polls.");Sync(preferred);
            }
            catch(InvalidOperationException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(ArgumentException e){Reject(e.Message);}
        }
        void Sync(int preferred)
        {
            if(IsDisposed||Disposing)return;reset.Enabled=state!=null;one.Enabled=thirteen.Enabled=batch.Enabled=state!=null&&state.RemainingPolls>0;nextPhase.Enabled=one.Enabled&&!pause.Checked;
            if(state==null){summary.Text="No admitted service phase copy loaded.";return;}ships=state.CopyShipPool();fleets=state.CopyFleetPool();changing=true;viewUnit.Items.Clear();int selected=-1;
            foreach(int unit in state.CopyOriginalUnits()){if(unit==preferred)selected=viewUnit.Items.Count;viewUnit.Items.Add(unit);}viewUnit.SelectedIndex=selected>=0?selected:viewUnit.Items.Count>0?0:-1;changing=false;FollowViewedBay();
            var text=new StringBuilder();text.AppendLine("Applied: "+PracticeFleetService.DescribeCase(state.CaseId));text.AppendLine("Initial task "+state.InitialTaskKind+" · initial counter "+state.InitialCounter);text.AppendLine("Source counter "+state.PhaseCounter+" · task "+state.TaskKind);text.AppendLine("Requested polls "+state.Polls+" /384 · paused "+state.PausedPolls);text.AppendLine("Task visits "+state.TaskVisits+" · worker calls "+state.WorkerCalls);text.AppendLine("Next task in "+state.NextDispatcherIn+" / support in "+state.NextWorkerIn+" unpaused polls");
            foreach(int unit in state.CopyOriginalUnits())text.AppendLine("Unit "+unit+": parent "+BitConverter.ToInt16(ships,unit*268+12)+", HP "+BitConverter.ToInt16(ships,unit*268+16)+", ammo "+Ammo(unit));text.AppendLine("Depot unit2 ammunition: "+Ammo(2));summary.Text=text.ToString();RefreshDetails();
        }
        int Ammo(int unit){int sum=0;for(int n=0;n<35;n++)sum+=BitConverter.ToInt16(ships,unit*268+196+n*2);return sum;}
        void FollowViewedBay(){if(ViewedUnit<0||ships==null)return;int parent=BitConverter.ToInt16(ships,ViewedUnit*268+12);if(parent==2||parent==3){changing=true;bay.SelectedIndex=parent-2;changing=false;}}
        void RefreshDetails()
        {
            if(state==null||IsDisposed||Disposing)return;var text=new StringBuilder();if(ViewedUnit>=0)text.AppendLine("Gold: viewed unit "+ViewedUnit+" · current parent "+BitConverter.ToInt16(ships,ViewedUnit*268+12));text.AppendLine("Source selection: fleet "+state.SelectedFleet+" / unit "+state.SelectedUnit+" / waypoint "+state.SelectedWaypoint);text.AppendLine("Requests: text "+state.TextRequests+", map "+state.MapRequests+", refresh "+state.RefreshRequests+", route UI "+state.RouteUiRequests);
            var task=state.CopyTaskRecord();for(int n=0;n<10;n++)text.AppendLine("Queue "+n+": kind "+BitConverter.ToInt32(task,n*20));text.AppendLine("Increment unpaused counter first. Task gate: low5bits0. Support gate: low6bits13. Taskkind0 does not disable this copied phase clock.");text.AppendLine("Supplied active AI owner1/contact;diagnostic untouched context fill and normalized table identity. No full outer AI driver,policy,movement,AA,originalheap or wall-clock playback.");text.AppendLine("Orange outlines mark restored bay cells. Gold follows the viewed ship; inspection does not change source selection.");details.Text=text.ToString();canvas.Invalidate();
        }
        sealed class CaseChoice{internal readonly string Id;internal CaseChoice(string id){Id=id;}public override string ToString(){return PracticeFleetService.DescribeCase(Id);}}
        sealed class CounterChoice{internal readonly uint Value;internal CounterChoice(uint value){Value=value;}public override string ToString(){return Value.ToString()+(Value==0xfffffffeu?" · wrap case":"");}}
        sealed class PhaseCanvas : Control
        {
            readonly PracticeFleetServicePhaseStudy owner;internal PhaseCanvas(PracticeFleetServicePhaseStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);if(owner.state==null||Width<100||Height<250)return;float size=Math.Min(Width-70,(Height-164)/2f),left=(Width-size)/2;
                PracticeFleetServiceGrid.Draw(e.Graphics,owner.Font,owner.ships,owner.fleets,owner.ViewedUnit,owner.state.SourceFleet,left,42,size,"Source fleet "+owner.state.SourceFleet+" · kind "+owner.state.SourceKind);
                PracticeFleetServiceGrid.Draw(e.Graphics,owner.Font,owner.ships,owner.fleets,owner.ViewedUnit,owner.Bay,left,size+106,size,"Bay fleet "+owner.Bay+" · orange outlines = bay markers");
                e.Graphics.DrawString("Copied source phases · no automatic wall-clock playback\r\nGold = viewed ship. Orange = bay marker retained after return.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-48,Math.Max(1,Width-24),46));
            }
        }
    }
}
