using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace FleetLab
{
    public sealed class PracticeFleetServiceStudy : Form
    {
        readonly string programs;
        readonly PracticeMapStartup startup;
        readonly ComboBox scenario=new ComboBox(),kind=new ComboBox(),mode=new ComboBox(),viewUnit=new ComboBox(),bay=new ComboBox();
        readonly Button load=new Button(),reset=new Button(),visit=new Button(),one=new Button(),many=new Button();
        readonly Label summary=new Label(),feedback=new Label();
        readonly TextBox details=new TextBox();
        readonly ServiceCanvas canvas;
        readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeFleetService state,initial;
        byte[] ships,fleets;
        bool changing;
        internal PracticeFleetService StudyState { get { return state; } }
        int ViewedUnit { get { return viewUnit.SelectedItem==null?-1:(int)viewUnit.SelectedItem; } }
        int Bay { get { return bay.SelectedIndex==1?3:2; } }
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeFleetServiceStudy(string programs,PracticeMapStartup startup):this(programs,startup,PracticeFleetServiceMode.TaggedSelective){}
        public PracticeFleetServiceStudy(string programs,PracticeMapStartup startup,PracticeFleetServiceMode initialMode)
        {
            if(initialMode<PracticeFleetServiceMode.TaggedSelective||initialMode>PracticeFleetServiceMode.InitializedDispatcher)throw new ArgumentOutOfRangeException("initialMode");
            this.programs=programs;this.startup=startup;
            Name="fleetServiceStudy";Text="Fleet service task · copied original rules";StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1160,840);MinimumSize=new Size(960,720);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,360));Controls.Add(layout);
            canvas=new ServiceCanvas(this){Dock=DockStyle.Fill,Name="fleetServiceCanvas"};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Name="fleetServiceScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);
            var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control control,int height)
            {
                control.Width=326;control.Height=height;control.Margin=new Padding(0,3,0,3);
                if(control is Button||control is ComboBox){control.BackColor=Color.FromArgb(28,55,76);control.ForeColor=ForeColor;}
                var button=control as Button;if(button!=null){button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;button.EnabledChanged+=delegate{if(button.IsDisposed||button.Disposing)return;button.BackColor=button.Enabled?Color.FromArgb(28,55,76):Color.LightGray;button.ForeColor=button.Enabled?ForeColor:Color.Black;};}side.Controls.Add(control);
            };
            add(new Label{Text="Copied service rules. Compare tagged selective tasks, source-initialized selective tasks, or reached dispatcher paths. Contact and manual cadence are supplied; 20-update bound. No full AI, movement or world clock runs."},96);
            add(new Label{Text="Supplied case / initial task (pending until Load)"},22);
            scenario.Name="fleetServiceCase";scenario.DropDownStyle=ComboBoxStyle.DropDownList;int defaultCase=0;
            foreach(string id in PracticeFleetService.CaseIds){if(id=="30-1-ordinary")defaultCase=scenario.Items.Count;scenario.Items.Add(new CaseChoice(id));}scenario.SelectedIndex=defaultCase;add(scenario,30);
            kind.Name="fleetServiceKind";kind.DropDownStyle=ComboBoxStyle.DropDownList;kind.Items.AddRange(new object[]{"11 · friendly contact, then service","12 · service / wait"});kind.SelectedIndex=0;add(kind,30);
            mode.Name="fleetServiceMode";mode.DropDownStyle=ComboBoxStyle.DropDownList;mode.Items.AddRange(new object[]{"Tagged selective · v0.94 baseline","Initialized selective · comparison","Initialized dispatcher · reached paths"});mode.SelectedIndex=(int)initialMode;add(mode,30);
            load.Name="fleetServiceLoad";load.Text="Load selected service copy";add(load,34);reset.Name="fleetServiceReset";reset.Text="Reset applied service copy";add(reset,34);
            summary.Name="fleetServiceSummary";add(summary,156);feedback.Name="fleetServiceFeedback";add(feedback,90);
            visit.Name="fleetServiceVisit";visit.Text="Visit initial recovered task";add(visit,34);one.Name="fleetServiceOne";one.Text="One worker + task update";add(one,34);many.Name="fleetServiceMany";many.Text="Up to 20 updates / stop on task 0";add(many,34);
            add(new Label{Text="View original unit / lower bay (view only)"},22);viewUnit.Name="fleetServiceUnit";viewUnit.DropDownStyle=ComboBoxStyle.DropDownList;add(viewUnit,30);
            bay.Name="fleetServiceBay";bay.DropDownStyle=ComboBoxStyle.DropDownList;bay.Items.AddRange(new object[]{"Bay fleet 2","Bay fleet 3"});bay.SelectedIndex=1;add(bay,30);
            details.Name="fleetServiceDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,290);
            var close=new Button{Name="fleetServiceClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            load.Click+=delegate{LoadCopy(((CaseChoice)scenario.SelectedItem).Id,kind.SelectedIndex+11);};reset.Click+=delegate{if(initial!=null){state=initial;Feedback("Applied service copy reset; pending case/task choices are unchanged.");Sync(-1);}};
            visit.Click+=delegate{Change(delegate{return state.VisitTask();});};one.Click+=delegate{Change(delegate{return state.StepWorkerAndTask();});};
            many.Click+=delegate{Change(delegate{var next=state;while(!next.QueueEmpty&&next.WorkerPasses<20)next=next.StepWorkerAndTask();return next;});};
            scenario.SelectedIndexChanged+=delegate{Pending();};kind.SelectedIndexChanged+=delegate{Pending();};mode.SelectedIndexChanged+=delegate{Pending();};viewUnit.SelectedIndexChanged+=delegate{if(!changing){FollowViewedBay();RefreshDetails();}};bay.SelectedIndexChanged+=delegate{if(!changing)RefreshDetails();};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{held.Clear();};Activated+=delegate{foreach(var key in new[]{Keys.Enter,Keys.Space,Keys.Escape})if((GetAsyncKeyState((int)key)&0x8000)!=0)held.Add(key);};
            LoadCopy("30-1-ordinary",11);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);if(e.KeyCode!=Keys.Enter&&e.KeyCode!=Keys.Space&&e.KeyCode!=Keys.Escape)return;var button=ActiveControl as Button;if(e.KeyCode!=Keys.Escape&&button==null)return;
            e.Handled=e.SuppressKeyPress=true;if(!held.Add(e.KeyCode))return;if(e.KeyCode==Keys.Escape)Close();else if(button.Enabled)button.PerformClick();
        }
        void Feedback(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=ForeColor;feedback.Text=text;}
        void Pending(){if(state!=null)Feedback("Case/task/mode choice is pending. Load applies it; Reset restores the applied copy.");}
        void LoadCopy(string id,int initialKind)
        {
            try{var next=PracticeFleetService.Create(programs,startup,id,initialKind,(PracticeFleetServiceMode)mode.SelectedIndex);state=initial=next;Feedback("Visit the initial task. The recovered command chooses which ship enters; viewing a unit does not change that choice.");Sync(-1);}
            catch(ArgumentException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(UnauthorizedAccessException e){Reject(e.Message);}
        }
        void Reject(string reason){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.Orange;feedback.Text="Rejected; existing copy preserved. "+reason;Sync(ViewedUnit);}
        void Change(Func<PracticeFleetService> action)
        {
            try
            {
                int preferred=ViewedUnit;var before=state.CopyShipPool();var next=action();var after=next.CopyShipPool();
                foreach(int unit in next.CopyOriginalUnits())if(BitConverter.ToInt16(before,unit*268+12)==next.SourceFleet&&BitConverter.ToInt16(after,unit*268+12)!=next.SourceFleet)preferred=unit;
                state=next;Feedback(next.QueueEmpty?"Task queue reached kind 0. This is the end of the supplied task slice, not a completed mission.":next.WorkerPasses>=20?"Twenty-update limit reached. Held contact and stock shortage can cause repeated admission in this driver; reset to inspect again.":"Recovered task/worker update applied. Contact remains supplied; no movement preamble resets it.");Sync(preferred);
            }
            catch(InvalidOperationException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}
        }
        void Sync(int preferred)
        {
            if(IsDisposed||Disposing)return;reset.Enabled=state!=null;visit.Enabled=state!=null&&state.TaskVisits==0;one.Enabled=many.Enabled=state!=null&&state.TaskVisits>0&&!state.QueueEmpty&&state.WorkerPasses<20;
            if(state==null){summary.Text="No admitted fleet service copy loaded.";return;}ships=state.CopyShipPool();fleets=state.CopyFleetPool();changing=true;viewUnit.Items.Clear();int selected=-1;
            foreach(int unit in state.CopyOriginalUnits()){if(unit==preferred)selected=viewUnit.Items.Count;viewUnit.Items.Add(unit);}viewUnit.SelectedIndex=selected>=0?selected:viewUnit.Items.Count>0?0:-1;
            if(ViewedUnit>=0){int parent=BitConverter.ToInt16(ships,ViewedUnit*268+12);if(parent==2||parent==3)bay.SelectedIndex=parent-2;}changing=false;
            var text=new StringBuilder();text.AppendLine("Applied: "+PracticeFleetService.DescribeCase(state.CaseId));text.AppendLine("Mode: "+ModeName(state.Mode));text.AppendLine("Task "+state.TaskKind+" · visits "+state.TaskVisits+" · worker updates "+state.WorkerPasses+" / 20");text.AppendLine("Source fleet "+state.SourceFleet+" · kind "+state.SourceKind+" · state "+state.SourceState);
            foreach(int unit in state.CopyOriginalUnits())text.AppendLine("Unit "+unit+": parent "+BitConverter.ToInt16(ships,unit*268+12)+", HP "+BitConverter.ToInt16(ships,unit*268+16)+"/"+BitConverter.ToInt16(ships,unit*268+14)+", ammo "+Ammo(unit));
            text.AppendLine("Depot unit 2 ammunition: "+Ammo(2));summary.Text=text.ToString();RefreshDetails();
        }
        int Ammo(int unit){int sum=0;for(int n=0;n<35;n++)sum+=BitConverter.ToInt16(ships,unit*268+196+n*2);return sum;}
        static string ModeName(PracticeFleetServiceMode value){return value==PracticeFleetServiceMode.TaggedSelective?"Tagged selective":value==PracticeFleetServiceMode.InitializedSelective?"Initialized selective":"Initialized dispatcher";}
        void FollowViewedBay()
        {
            if(ViewedUnit<0||ships==null)return;int parent=BitConverter.ToInt16(ships,ViewedUnit*268+12);
            if(parent==2||parent==3){changing=true;bay.SelectedIndex=parent-2;changing=false;}
        }
        void RefreshDetails()
        {
            if(state==null||IsDisposed||Disposing)return;var text=new StringBuilder();if(ViewedUnit>=0)text.AppendLine("Gold: viewed unit "+ViewedUnit+" · current parent "+BitConverter.ToInt16(ships,ViewedUnit*268+12));text.AppendLine("Source selection: fleet "+state.SelectedFleet+" / unit "+state.SelectedUnit+" / waypoint "+state.SelectedWaypoint);
            text.AppendLine("Counted requests: text "+state.TextRequests+", map "+state.MapRequests+", refresh "+state.RefreshRequests+", route UI "+state.RouteUiRequests);
            text.AppendLine("Contact byte: "+unchecked((sbyte)fleets[state.SourceFleet*492+482])+" (held supplied input).");
            text.AppendLine(state.Mode==PracticeFleetServiceMode.TaggedSelective?"Tagged queue: 10 records × 20 bytes. Payload/tail tags are comparison fixtures.":"Source-initialized queue: 10 records × 20 bytes. Untouched36 bytes use supplied6D fill; table pointer is normalized diagnostic data, with no runtime heap allocated.");var task=state.CopyTaskRecord();
            for(int n=0;n<10;n++)text.AppendLine("Queue "+n+": kind "+BitConverter.ToInt32(task,n*20));
            text.AppendLine("Damage admission chooses the last roster member with positive grid damage sum. The ammo path may admit multiple ships. Worker return does not guarantee full ammunition if the needed depot stock is exhausted.");
            text.AppendLine(state.Mode==PracticeFleetServiceMode.InitializedDispatcher?"Reached dispatcher paths use supplied active AI owner1. Friendly-yard contact can pop task12 before selective service. Other target/attachment/planning branches reject. Full AI driver, world clock, AA and movement remain outside this copy.":"Only the selective task11/12 handler, copied pools and manual cadence run. Full AI/controller, world clock, AA and movement remain outside this copy.");text.AppendLine("Orange outlines are bay markers restored after departure. Gold follows the viewed ship's current parent.");details.Text=text.ToString();canvas.Invalidate();
        }
        sealed class CaseChoice{internal readonly string Id;internal CaseChoice(string id){Id=id;}public override string ToString(){return PracticeFleetService.DescribeCase(Id);}}
        sealed class ServiceCanvas : Control
        {
            readonly PracticeFleetServiceStudy owner;internal ServiceCanvas(PracticeFleetServiceStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);if(owner.state==null||Width<100||Height<250)return;float size=Math.Min(Width-70,(Height-164)/2f),left=(Width-size)/2;
                Grid(e.Graphics,owner.state.SourceFleet,left,42,size,"Source fleet "+owner.state.SourceFleet+" · kind "+owner.state.SourceKind);Grid(e.Graphics,owner.Bay,left,size+106,size,"Bay fleet "+owner.Bay+" · orange outlines = bay markers");
                e.Graphics.DrawString("Copied service task · rows local X / columns local Y\r\nGold = viewed ship. Orange = bay marker, retained after return.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-48,Math.Max(1,Width-24),46));
            }
            void Grid(Graphics g,int fleet,float left,float top,float size,string title)
            {
                PracticeFleetServiceGrid.Draw(g,owner.Font,owner.ships,owner.fleets,owner.ViewedUnit,fleet,left,top,size,title);
            }
        }
    }
}
