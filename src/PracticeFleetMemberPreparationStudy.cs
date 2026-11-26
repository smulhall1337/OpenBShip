using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace FleetLab
{
    public sealed class PracticeFleetMemberPreparationStudy:Form
    {
        readonly string programs;readonly PracticeMapStartup startup;readonly PracticeVisibleCandidates joined;
        readonly ComboBox equipment=new ComboBox(),eligibility=new ComboBox(),task=new ComboBox(),difficulty=new ComboBox(),player=new ComboBox(),comparison=new ComboBox(),view=new ComboBox();
        readonly Button load=new Button(),reset=new Button(),next=new Button(),requests=new Button();readonly Label summary=new Label(),feedback=new Label();readonly TextBox details=new TextBox();readonly PreparationCanvas canvas;readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeFleetMemberPreparation state,initial;byte[] ships,fleets;
        internal PracticeFleetMemberPreparation StudyState {get{return state;}}
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeFleetMemberPreparationStudy(string programs,PracticeMapStartup startup):this(programs,startup,null){}
        public PracticeFleetMemberPreparationStudy(string programs,PracticeMapStartup startup,PracticeVisibleCandidates joined)
        {
            this.programs=programs;this.startup=startup;this.joined=joined;Name="fleetMemberPreparationStudy";Text=joined==null?"Fleet member preparation · stops before firing":"Produced visibility → member preparation";StartPosition=FormStartPosition.CenterParent;Size=new Size(1160,840);MinimumSize=new Size(960,720);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,360));Controls.Add(layout);canvas=new PreparationCanvas(this){Name="memberPrepareCanvas",Dock=DockStyle.Fill};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Name="memberPrepareScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control c,int height){c.Width=326;c.Height=height;c.Margin=new Padding(0,3,0,3);if(c is ComboBox||c is Button){c.BackColor=Color.FromArgb(28,55,76);c.ForeColor=ForeColor;}var b=c as Button;if(b!=null){b.UseVisualStyleBackColor=false;b.FlatStyle=FlatStyle.Flat;b.EnabledChanged+=delegate{if(b.IsDisposed||b.Disposing)return;b.BackColor=b.Enabled?Color.FromArgb(28,55,76):Color.LightGray;b.ForeColor=b.Enabled?ForeColor:Color.Black;};}side.Controls.Add(c);};
            add(new Label{Text="Copied equipment choice and waiting states for fleet 16. Up to three manual visits; stop before any firing-state execution. No shot, movement or full AI policy runs. Clock numbers are raw source values."},84);
            add(new Label{Text="Inputs (pending until Load)"},22);Action<ComboBox,string,object[],int> choices=delegate(ComboBox box,string name,object[] values,int selected){box.Name=name;box.DropDownStyle=ComboBoxStyle.DropDownList;box.Items.AddRange(values);box.SelectedIndex=selected;add(box,30);box.SelectedIndexChanged+=delegate{if(state!=null)feedback.Text="Inputs pending. Load applies them; Reset restores the applied prefix.";};};
            choices(equipment,"memberPrepareEquipment",new object[]{"Original equipment and ammunition","Clear equipment and ammunition","F2 id 14 · unit 30 only","F2 id 14 · both units","F3 id 19 · both units","F2 unit 30 / F3 unit 31"},3);
            choices(eligibility,"memberPrepareEligibility",new object[]{joined==null?"Original eligibility: no candidates":"Produced owner-1 visibility (held)","Supply fleet 14 eligible","Supply fleets 14 and 15 eligible","Supply all live fleets eligible"},joined==null?2:0);
            if(joined!=null){equipment.SelectedIndex=0;equipment.Enabled=eligibility.Enabled=false;}
            choices(task,"memberPrepareTask",new object[]{"Supplied initial task 0","Supplied initial task 8"},0);choices(difficulty,"memberPrepareDifficulty",new object[]{"Difficulty 0","Difficulty 1","Difficulty 2","Difficulty 3"},2);choices(player,"memberPreparePlayer",new object[]{"Player-record override 0","Player-record override 1"},0);
            add(new Label{Text="Caller comparison word is supplied for diagnosis. Its natural caller value is unresolved."},46);choices(comparison,"memberPrepareComparison",new object[]{"Supplied comparison word 0","Supplied comparison word 1"},0);
            load.Name="memberPrepareLoad";load.Text="Load selected preparation copy";add(load,34);reset.Name="memberPrepareReset";reset.Text="Reset applied preparation copy";add(reset,34);summary.Name="memberPrepareSummary";add(summary,128);feedback.Name="memberPrepareFeedback";add(feedback,80);next.Name="memberPrepareNext";add(next,40);
            requests.Name="memberPrepareRequests";requests.Text="Copy to one request visit… (joined only)";requests.Enabled=false;add(requests,40);requests.Click+=delegate{if(state!=null&&state.CanCopyProducedRequests)using(var child=new PracticeFleetMemberRequestStudy(programs,startup,state))child.ShowDialog(this);};
            add(new Label{Text="Gold unit (inspection only)"},22);view.Name="memberPrepareView";view.DropDownStyle=ComboBoxStyle.DropDownList;view.Items.AddRange(new object[]{"Unit 30","Unit 31"});view.SelectedIndex=0;add(view,30);view.SelectedIndexChanged+=delegate{canvas.Invalidate();};
            details.Name="memberPrepareDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,280);var close=new Button{Name="memberPrepareClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            load.Click+=delegate{LoadCopy();};reset.Click+=delegate{if(initial!=null){state=initial;Feedback("Applied initial copy restored. Pending selectors are unchanged.");Sync();}};next.Click+=delegate{if(state==null)return;try{state=state.Advance();Feedback(state.FiringBoundary?"Firing boundary reached. This study stops before executing state 2; no shot was queued.":state.CanAdvance?"One original preparation visit applied to this copy.":"Three-visit captured prefix complete. Reset to repeat.");Sync();}catch(InvalidOperationException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{foreach(Keys k in new[]{Keys.Enter,Keys.Space})if((GetAsyncKeyState((int)k)&0x8000)!=0)held.Add(k);};LoadCopy();
        }
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){var b=ActiveControl as Button;if(b!=null){e.Handled=e.SuppressKeyPress=true;if(held.Add(e.KeyCode)&&b.Enabled)b.PerformClick();return;}}base.OnKeyDown(e);}
        void Feedback(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.LightSteelBlue;feedback.Text=text;}
        void Reject(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.Orange;feedback.Text="Rejected; applied copy preserved. "+text;Sync();}
        void LoadCopy(){try{var c=joined==null?PracticeFleetMemberPreparation.Create(programs,startup,equipment.SelectedIndex,eligibility.SelectedIndex,task.SelectedIndex==0?0:8,difficulty.SelectedIndex,player.SelectedIndex==1,comparison.SelectedIndex):PracticeFleetMemberPreparation.CreateFromVisibility(programs,startup,joined,task.SelectedIndex==0?0:8,difficulty.SelectedIndex,player.SelectedIndex==1,comparison.SelectedIndex);state=initial=c;Feedback(joined==null?"Copied source roster loaded. No preparation visit has run yet.":"Original equipment and produced owner-1 flags copied unchanged. Caller word, task, override and manual clock remain explicit inputs.");Sync();}catch(ArgumentException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(UnauthorizedAccessException e){Reject(e.Message);}}
        void Sync()
        {
            if(IsDisposed||Disposing)return;reset.Enabled=state!=null;next.Enabled=state!=null&&state.CanAdvance;requests.Enabled=state!=null&&state.CanCopyProducedRequests;if(state==null){summary.Text="No admitted preparation copy loaded.";next.Text="No captured prefix available";return;}ships=state.CopyShipPool();fleets=state.CopyFleetPool();
            summary.Text=(state.UsesProducedVisibility?"Original equipment / produced visibility":"Applied: equipment "+state.EquipmentProfile+" / eligibility "+state.EligibilityProfile)+"\r\nInitial task "+state.InitialTask+" · difficulty "+state.Difficulty+"\r\nPlayer override "+(state.PlayerOverride?1:0)+" · comparison word "+state.ComparisonWord+"\r\nVisits "+state.Visits+" / 3 · raw clock "+state.Clock+"\r\nF2 delay "+state.F2Delay+" / F3 delay "+state.F3Delay+"\r\nTask "+state.TaskKind+" · waypoint "+state.SelectedWaypoint+" · route UI "+state.RouteUiRequests;
            next.Text=!state.CanAdvance?state.FiringBoundary?"Stopped before firing-state execution":"Captured prefix complete":state.Visits==0?"1 · Choose equipment and targets":state.Visits==1?"2 · Visit just before first deadline":"3 · Visit at first deadline";
            details.Text="Returned candidate ids: "+(state.HasScanned?Ids(state):"not scanned")+"\r\nState 0: no active preparation. State 1: waiting. State 2: firing branch is next; this study refuses to enter it.\r\nWeapon +54 fields retain their prior value until a choice writes them. Unassigned target words remain opaque diagnostic bytes; the display does not treat them as fleet ids.\r\nChoice uses signed source combat scores and nonzero ammunition. F3 metadata is admitted for supplied task 8. Difficulty scales the supplied initial AI delays by 2^level and changes candidate allocation.\r\nThe source waiting-state loop reads a comparison word before its state-0 path writes it. Supplied word 0/1 makes that unresolved dependency visible; it is not a recovered natural caller value.\r\nPlayer-record override 1 replaces a waiting due field with 2. Raw clock and supplied boundaries are not natural wall-clock playback.\r\nOnly the reached fleet-16 preparation prefix is reconstructed. No actual attacks, ammo consumption, movement, AA, full member firing or recurring policy runs.";canvas.Invalidate();
        }
        static string Ids(PracticeFleetMemberPreparation c){var values=new List<string>();foreach(int id in c.Candidates)values.Add(id.ToString());return values.Count==0?"none":string.Join(", ",values.ToArray());}
        sealed class PreparationCanvas:Control
        {
            readonly PracticeFleetMemberPreparationStudy owner;internal PreparationCanvas(PracticeFleetMemberPreparationStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);var c=owner.state;if(c==null||Width<200||Height<350)return;for(int n=0;n<2;n++)
                {
                    int state=c.MemberState(n),y=18+n*88;using(var brush=new SolidBrush(state==2?Color.FromArgb(80,60,20):Color.FromArgb(24,45,62)))e.Graphics.FillRectangle(brush,12,y,Width-24,76);
                    string target=c.MemberTarget(n)<0?"unassigned":c.MemberTarget(n).ToString();e.Graphics.DrawString("Unit "+(30+n)+" · state "+state+" · target "+target,owner.Font,state==2?Brushes.Gold:Brushes.WhiteSmoke,22,y+8);e.Graphics.DrawString("Weapon field "+c.WeaponField(n)+" · due "+c.MemberDue(n)+" · raw clock "+c.Clock,owner.Font,Brushes.LightSteelBlue,22,y+30);e.Graphics.DrawString(state==2?"Firing boundary · execution stopped":state==1?"Waiting for source due comparison":"No active preparation",owner.Font,Brushes.LightSteelBlue,22,y+51);
                }
                float size=Math.Min(Width-64,Height-300),left=(Width-size)/2;PracticeFleetServiceGrid.Draw(e.Graphics,owner.Font,owner.ships,owner.fleets,30+Math.Max(0,owner.view.SelectedIndex),16,left,242,size,"Source fleet 16 · gold = viewed unit");
                e.Graphics.DrawString("Preparation prefix only · no shot, ammo use or movement\r\nState 2 is observable; the firing branch is not executed.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-48,Width-24,46));
            }
        }
    }
}
