using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace FleetLab
{
    public sealed class PracticeVisibleCandidateStudy:Form
    {
        readonly ListBox candidates=new ListBox();readonly Label summary=new Label(),feedback=new Label();readonly TextBox details=new TextBox();readonly CandidateCanvas canvas;
        PracticeVisibleCandidates state;
        internal PracticeVisibleCandidates StudyState {get{return state;}}
        public PracticeVisibleCandidateStudy(string programs,PracticeMapStartup startup,PracticeOwnerVisibilityStudyState visibility)
        {
            Name="visibleCandidateStudy";Text="Produced visibility → fleet candidates";StartPosition=FormStartPosition.CenterParent;Size=new Size(1120,800);MinimumSize=new Size(960,700);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,360));Controls.Add(layout);canvas=new CandidateCanvas(this){Name="visibleCandidateCanvas",Dock=DockStyle.Fill};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Name="visibleCandidateScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control c,int height){c.Width=326;c.Height=height;c.Margin=new Padding(0,3,0,3);side.Controls.Add(c);};
            add(new Label{Text="One recovered candidate lookup after the completed owner-visibility producer. This read-only child preserves its parent. It issues no orders, changes no ammunition and runs no movement, timer or firing update."},84);
            summary.Name="visibleCandidateSummary";add(summary,110);feedback.Name="visibleCandidateFeedback";add(feedback,78);add(new Label{Text="Candidate ids · recovered descending scan order"},24);candidates.Name="visibleCandidateList";candidates.BackColor=BackColor;candidates.ForeColor=ForeColor;candidates.HorizontalScrollbar=true;add(candidates,138);details.Name="visibleCandidateDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,310);
            var close=new Button{Name="visibleCandidateClose",Text="Close",BackColor=Color.FromArgb(28,55,76),ForeColor=ForeColor,FlatStyle=FlatStyle.Flat,UseVisualStyleBackColor=false};add(close,34);close.Click+=delegate{Close();};CancelButton=close;candidates.SelectedIndexChanged+=delegate{RefreshDetails();};
            try{state=PracticeVisibleCandidates.Create(programs,startup,visibility);summary.Text="Owner "+state.Visibility.Owner+" · surface actor "+state.Scan.Actor+"\r\nVisibility preset "+state.Visibility.Preset+" · normal mode\r\nCandidates "+state.Scan.Candidates.Count+" · original equipment retained\r\nVisibility → candidate scan completed once";feedback.Text="Candidate ids are raw lookup results, not confirmation of a valid shot or an AI attack decision. Positions and visibility flags are held.";foreach(int id in state.Scan.Candidates)candidates.Items.Add(id);if(candidates.Items.Count>0)candidates.SelectedIndex=0;else RefreshDetails();}
            catch(ArgumentException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(UnauthorizedAccessException e){Reject(e.Message);}
        }
        void Reject(string text){feedback.ForeColor=Color.Orange;feedback.Text="Joined candidate copy rejected: "+text;summary.Text="No admitted candidate copy.";}
        void RefreshDetails()
        {
            if(state==null||IsDisposed||Disposing)return;string selected="No candidate selected.";
            if(candidates.SelectedItem!=null){int id=(int)candidates.SelectedItem;var item=state.Visibility.Fleet(id);var actor=state.Visibility.Fleet(state.Scan.Actor);int distance=Math.Max(Math.Abs(item.X-actor.X),Math.Abs(item.Y-actor.Y));selected="Candidate "+id+" · kind "+item.Kind+" · owner "+item.Owner+"\r\nWorld "+item.X+","+item.Y+" · actor distance "+distance+"\r\nProduced current level "+item.Level+"\r\nPacked history 0x"+item.HistoryByte.ToString("X2")+" · far-contact byte "+item.ContactByte;}
            details.Text=selected+"\r\n\r\nGold = candidate; white = viewed candidate; cyan = acting fleet. Gray records are other copied map fleets. The list preserves every returned id, including cached-history candidates."+
                "\r\n\r\nThe source equipment scratch stores roster-member indexes and retains -1 sentinels. The raw candidate loop checks entries for nonzero and compares signed raw ranges <= distance. These unusual tests are preserved; they do not implement the separate numeric shot-range admission."+
                "\r\n\r\nOwner 1 / actor 16 naturally finds fleet 15 in this copied map. Owner 0 / actor 14 finds fleet 9. Prefilled history is an explicit fixture and can retain additional candidates. No original application entry runs. Full AI policy, target selection, member preparation, attack admission and firing remain separate.";canvas.Invalidate();
        }
        sealed class CandidateCanvas:Control
        {
            readonly PracticeVisibleCandidateStudy owner;internal CandidateCanvas(PracticeVisibleCandidateStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);if(owner.state==null||Width<200||Height<250)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float size=Math.Min(Width-68,Height-145),left=(Width-size)/2,top=54;Func<int,int,PointF> point=delegate(int x,int y){return new PointF(left+x*size/630f,top+y*size/630f);};
                g.DrawString("Produced visibility → candidates · copied map / X across, Y down",owner.Font,Brushes.WhiteSmoke,new RectangleF(12,14,Width-24,35));using(var pen=new Pen(Color.FromArgb(35,61,80)))for(int n=0;n<=6;n++){var a=point(n*100,0);var b=point(n*100,630);g.DrawLine(pen,a,b);a=point(0,n*100);b=point(630,n*100);g.DrawLine(pen,a,b);}g.DrawRectangle(Pens.SlateGray,left,top,size,size);
                foreach(var item in owner.state.Visibility.ActiveFleets){var p=point(item.X,item.Y);bool candidate=owner.state.Scan.Candidates.Contains(item.Fleet);g.FillEllipse(candidate?Brushes.Gold:item.Fleet==owner.state.Scan.Actor?Brushes.Cyan:Brushes.SlateGray,p.X-4,p.Y-4,8,8);if(candidate)g.DrawEllipse(Pens.Gold,p.X-8,p.Y-8,16,16);if(item.Fleet==owner.state.Scan.Actor||owner.candidates.SelectedItem!=null&&item.Fleet==(int)owner.candidates.SelectedItem){g.DrawEllipse(Pens.White,p.X-11,p.Y-11,22,22);g.DrawString((item.Fleet==owner.state.Scan.Actor?"Actor ":"Candidate ")+item.Fleet,owner.Font,Brushes.WhiteSmoke,new PointF(Math.Max(4,Math.Min(Width-130,p.X+12)),p.Y-12));}}
                g.DrawString("Gold = raw candidates · cyan = actor · white = viewed\r\nNo shot or AI decision runs; this copy does not advance.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-67,Width-24,62));
            }
        }
    }
}
