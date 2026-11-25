using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace FleetLab
{
    public sealed class PracticeFleetCandidateStudy : Form
    {
        readonly string programs;readonly PracticeMapStartup startup;
        readonly ComboBox scenario=new ComboBox(),equipment=new ComboBox(),eligibility=new ComboBox(),distance=new ComboBox(),owner=new ComboBox();
        readonly Button load=new Button(),reset=new Button();readonly ListBox targets=new ListBox();readonly Label summary=new Label(),feedback=new Label();readonly TextBox details=new TextBox();
        readonly CandidateCanvas canvas;readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeFleetCandidates state;byte[] fleets;bool changing;
        internal PracticeFleetCandidates StudyState {get{return state;}}
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeFleetCandidateStudy(string programs,PracticeMapStartup startup)
        {
            this.programs=programs;this.startup=startup;Name="fleetCandidateStudy";Text="Fleet candidate scan · copied raw rules";StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1160,840);MinimumSize=new Size(960,720);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,360));Controls.Add(layout);
            canvas=new CandidateCanvas(this){Name="candidateCanvas",Dock=DockStyle.Fill};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Name="candidateScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control c,int height){c.Width=326;c.Height=height;c.Margin=new Padding(0,3,0,3);if(c is Button||c is ComboBox||c is ListBox){c.BackColor=Color.FromArgb(28,55,76);c.ForeColor=ForeColor;}var b=c as Button;if(b!=null){b.FlatStyle=FlatStyle.Flat;b.UseVisualStyleBackColor=false;}side.Controls.Add(c);};
            add(new Label{Text="Inspect one copied candidate scan. This does not fire, move fleets or run AI policy. Raw eligibility, equipment and test positions are supplied explicitly. The list's full gameplay role is unresolved."},82);
            add(new Label{Text="Inputs (pending until Load)"},22);
            Action<ComboBox,string,object[]> choices=delegate(ComboBox box,string name,object[] values){box.Name=name;box.DropDownStyle=ComboBoxStyle.DropDownList;box.Items.AddRange(values);box.SelectedIndex=0;add(box,30);box.SelectedIndexChanged+=delegate{if(!changing&&state!=null)feedback.Text="Inputs pending. Load applies them; Reset keeps the applied result.";};};
            var cases=new object[PracticeFleetCandidates.CaseNames.Count];for(int n=0;n<cases.Length;n++)cases[n]=PracticeFleetCandidates.CaseNames[n];choices(scenario,"candidateCase",cases);
            choices(equipment,"candidateEquipment",new object[]{"Original roster equipment","Clear admitted equipment slots","One dispatch weapon · roster position 0","One dispatch weapon · roster position 1"});
            choices(eligibility,"candidateEligibility",new object[]{"Original eligibility bytes (all zero)","Supply eligibility byte 1 on live fleets"});
            choices(distance,"candidateDistance",new object[]{"Original map coordinates","Test separation 0","Test separation 1","Test separation 128","Test separation 32767"});
            choices(owner,"candidateOwner",new object[]{"Active AI owner 1","Active AI owner 0"});
            load.Name="candidateLoad";load.Text="Load and scan selected copy";add(load,34);reset.Name="candidateReset";reset.Text="Reset applied scan (same supplied inputs)";add(reset,34);
            summary.Name="candidateSummary";add(summary,144);feedback.Name="candidateFeedback";add(feedback,76);
            add(new Label{Text="Returned fleet ids · source order"},22);targets.Name="candidateTargets";add(targets,150);
            details.Name="candidateDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,280);
            var close=new Button{Name="candidateClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};load.Click+=delegate{LoadCopy(false);};reset.Click+=delegate{LoadCopy(true);};targets.SelectedIndexChanged+=delegate{if(!changing)RefreshDetails();};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{foreach(Keys k in new[]{Keys.Enter,Keys.Space})if((GetAsyncKeyState((int)k)&0x8000)!=0)held.Add(k);};
            LoadCopy(false);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space)
            {
                var button=ActiveControl as Button;if(button!=null){e.Handled=e.SuppressKeyPress=true;if(held.Add(e.KeyCode)&&button.Enabled)button.PerformClick();return;}
            }
            base.OnKeyDown(e);
        }
        void LoadCopy(bool applied)
        {
            try
            {
                if(applied&&state==null)return;
                var next=PracticeFleetCandidates.Create(programs,startup,applied?state.CaseIndex:scenario.SelectedIndex,applied?state.EquipmentProfile:equipment.SelectedIndex,applied?state.SuppliedEligibility:eligibility.SelectedIndex==1,applied?state.DistanceProfile:PracticeFleetCandidates.Distances[distance.SelectedIndex],applied?state.ActiveOwner:owner.SelectedIndex==0?1:0);
                state=next;fleets=next.CopyFleetPool();feedback.ForeColor=Color.LightSteelBlue;feedback.Text=applied?"Applied inputs rescanned from a fresh copy. Pending controls are unchanged.":"One copied scan complete. Selecting a result only changes inspection.";Sync();
            }
            catch(ArgumentException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(UnauthorizedAccessException e){Reject(e.Message);}
        }
        void Reject(string reason){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.Orange;feedback.Text="Rejected; applied result preserved. "+reason;reset.Enabled=state!=null;}
        void Sync()
        {
            if(IsDisposed||Disposing||state==null)return;reset.Enabled=true;summary.Text="Applied: "+PracticeFleetCandidates.CaseNames[state.CaseIndex]+"\r\nEquipment profile "+state.EquipmentProfile+" · active owner "+state.ActiveOwner+"\r\nEligibility "+(state.SuppliedEligibility?"supplied 1":"original 0")+" · separation "+(state.DistanceProfile<0?"original":state.DistanceProfile.ToString())+"\r\nDispatch equipment found: "+state.EquipmentFound+"\r\nReturned ids: "+state.Candidates.Count+" · task "+state.TaskKind+"\r\nWaypoint "+state.SelectedWaypoint+" · route UI requests "+state.RouteUiRequests;
            changing=true;targets.Items.Clear();foreach(int id in state.Candidates)targets.Items.Add(id);if(targets.Items.Count>0)targets.SelectedIndex=0;changing=false;RefreshDetails();
        }
        void RefreshDetails()
        {
            if(state==null||IsDisposed||Disposing)return;var text=new StringBuilder();int selected=targets.SelectedItem==null?-1:(int)targets.SelectedItem;
            if(selected>=0&&selected<250)text.AppendLine("Viewed fleet "+selected+" · kind "+BitConverter.ToInt32(fleets,selected*492)+" · owner "+BitConverter.ToInt16(fleets,selected*492+6)+" · position "+BitConverter.ToInt16(fleets,selected*492+12)+", "+BitConverter.ToInt16(fleets,selected*492+14));
            text.AppendLine("Gold = actor; cyan = returned fleet. White ring = viewed id. Coincident test positions share a marker; the list retains every returned id.");
            text.AppendLine("Original scratch entries start at -1. The next scan uses raw !=0, so unmapped -1 passes and member position0 does not. Its range branch is raw range <= Chebyshev distance. Source range entry0=0 with unmapped scratch0, so these cases do not behave as an inside-range firing test.");
            text.AppendLine("Equipment absent: kinds5..8 leave task unchanged; other admitted kinds push11, clear five waypoints and request route UI. No shots are fired.");
            text.AppendLine("Source scratch · entry: roster position (-1 = unmapped)");var scratch=state.CopyScratchBytes();for(int n=0;n<41;n++)text.AppendLine(n+": "+BitConverter.ToInt32(scratch,n*4));
            text.AppendLine("Parent category filters and scenario8 owner exclusion are raw recovered rules. Positive category matches,250-entry capacity and full member-order/policy integration remain outside the original880-case evidence.");details.Text=text.ToString();canvas.Invalidate();
        }
        sealed class CandidateCanvas:Control
        {
            readonly PracticeFleetCandidateStudy owner;internal CandidateCanvas(PracticeFleetCandidateStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);var state=owner.state;if(state==null||Width<180||Height<180)return;var fleets=owner.fleets;float bound=Math.Min(Width-70,Height-150),left=(Width-bound)/2,top=54;int extent=640;
                for(int n=0;n<250;n++)if(BitConverter.ToInt32(fleets,n*492)!=0)extent=Math.Max(extent,Math.Max(Math.Abs((int)BitConverter.ToInt16(fleets,n*492+12)),Math.Abs((int)BitConverter.ToInt16(fleets,n*492+14))));
                e.Graphics.DrawString("Copied fleet positions · auto scale 0.."+extent,owner.Font,Brushes.LightSteelBlue,12,12);using(var pen=new Pen(Color.FromArgb(45,70,85)))for(int n=0;n<=8;n++){float p=bound*n/8;e.Graphics.DrawLine(pen,left+p,top,left+p,top+bound);e.Graphics.DrawLine(pen,left,top+p,left+bound,top+p);}
                Func<int,PointF> point=delegate(int id){return new PointF(left+BitConverter.ToInt16(fleets,id*492+12)*bound/extent,top+BitConverter.ToInt16(fleets,id*492+14)*bound/extent);};
                for(int n=0;n<250;n++)if(BitConverter.ToInt32(fleets,n*492)!=0){var p=point(n);e.Graphics.FillEllipse(Brushes.SlateGray,p.X-3,p.Y-3,6,6);}
                foreach(int id in state.Candidates)if(id>=0&&id<250){var p=point(id);e.Graphics.FillEllipse(Brushes.Cyan,p.X-5,p.Y-5,10,10);}
                var actor=point(state.Actor);using(var pen=new Pen(Color.Gold,3))e.Graphics.DrawRectangle(pen,actor.X-8,actor.Y-8,16,16);e.Graphics.DrawString("Actor "+state.Actor,owner.Font,Brushes.Gold,actor.X+10,actor.Y+5);
                if(owner.targets.SelectedItem!=null){int id=(int)owner.targets.SelectedItem;if(id>=0&&id<250){var p=point(id);using(var pen=new Pen(Color.White,2))e.Graphics.DrawEllipse(pen,p.X-12,p.Y-12,24,24);e.Graphics.DrawString("Fleet "+id,owner.Font,Brushes.Cyan,p.X+14,p.Y-20);}}
                e.Graphics.DrawString("Gold = actor · cyan = returned ids · white = viewed id\r\nCopied raw list; no movement, attack or AI policy runs.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-62,Width-24,58));
            }
        }
    }
}
