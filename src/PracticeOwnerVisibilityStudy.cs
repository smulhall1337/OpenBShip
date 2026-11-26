using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace FleetLab
{
    public sealed class PracticeOwnerVisibilityStudy:Form
    {
        sealed class Choice
        {
            internal int Value;readonly string text;internal Choice(int value,string text){Value=value;this.text=text;}public override string ToString(){return text;}
        }
        readonly string programs;readonly PracticeMapStartup startup;
        readonly ComboBox preset=new ComboBox(),owner=new ComboBox(),weapon=new ComboBox(),observer=new ComboBox(),radii=new ComboBox(),distance=new ComboBox(),packed=new ComboBox();
        readonly Button load=new Button(),reset=new Button(),execute=new Button(),candidateScan=new Button();readonly Label summary=new Label(),feedback=new Label();readonly ListBox fleets=new ListBox();readonly TextBox details=new TextBox();readonly VisibilityCanvas canvas;readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeOwnerVisibilityStudyState state,initial;byte[] pool;bool changing;
        internal PracticeOwnerVisibilityStudyState StudyState {get{return state;}}
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        static int Value(ComboBox box){return ((Choice)box.SelectedItem).Value;}
        static void Fill(ComboBox box,Choice[] choices,int fallback)
        {
            int old=box.SelectedItem==null?fallback:Value(box),at=-1;box.BeginUpdate();box.Items.Clear();foreach(var c in choices){int n=box.Items.Add(c);if(c.Value==old)at=n;}if(at<0)for(int n=0;n<box.Items.Count;n++)if(((Choice)box.Items[n]).Value==fallback)at=n;box.SelectedIndex=at<0?0:at;box.EndUpdate();
        }
        public PracticeOwnerVisibilityStudy(string programs,PracticeMapStartup startup)
        {
            this.programs=programs;this.startup=startup;Name="ownerVisibilityStudy";Text="Owner visibility · copied producer and history";StartPosition=FormStartPosition.CenterParent;Size=new Size(1160,860);MinimumSize=new Size(960,740);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,360));Controls.Add(layout);canvas=new VisibilityCanvas(this){Name="ownerVisibilityCanvas",Dock=DockStyle.Fill};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Name="ownerVisibilityScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control c,int height){c.Width=326;c.Height=height;c.Margin=new Padding(0,3,0,3);if(c is ComboBox||c is Button||c is ListBox){c.BackColor=Color.FromArgb(28,55,76);c.ForeColor=ForeColor;}var b=c as Button;if(b!=null){b.UseVisualStyleBackColor=false;b.FlatStyle=FlatStyle.Flat;b.EnabledChanged+=delegate{if(b.IsDisposed||b.Disposing)return;b.BackColor=b.Enabled?Color.FromArgb(28,55,76):Color.LightGray;b.ForeColor=b.Enabled?ForeColor:Color.Black;};}side.Controls.Add(c);};
            add(new Label{Text="One recovered owner-visibility pass. It produces current levels, far-contact flags and packed history. Copied map and explicit sensor fixtures are separate. No attack, movement or full owner/AI loop runs."},84);
            Action<ComboBox,string,Choice[],int> choice=delegate(ComboBox box,string name,Choice[] choices,int selected){box.Name=name;box.DropDownStyle=ComboBoxStyle.DropDownList;box.DropDownWidth=440;Fill(box,choices,selected);add(box,30);box.SelectedIndexChanged+=delegate{if(!changing){if(box==preset)Configure();if(state!=null)Feedback("Inputs pending. Load prepares them; Reset keeps the applied inputs.");}};};
            choice(preset,"ownerVisibilityPreset",new[]{new Choice(0,"Copied stage 4 · normal producer"),new Choice(1,"Copied stage 4 · prefilled history"),new Choice(2,"Copied stage 4 · local mode-2 filter"),new Choice(3,"Controlled sensor / packed history"),new Choice(4,"Controlled local range filter")},0);
            choice(owner,"ownerVisibilityOwner",OwnerChoices(),1);
            choice(weapon,"ownerVisibilityWeapon",new[]{new Choice(0,"No selected weapon (mode-2 early return)"),new Choice(14,"Selected id 14 · range 20"),new Choice(19,"Selected id 19 · range 500"),new Choice(34,"Selected id 34")},0);
            choice(observer,"ownerVisibilityObserver",ObserverChoices(false),1);
            choice(radii,"ownerVisibilityRadii",new[]{new Choice(0,"Near 10 / middle 20 / far 30"),new Choice(1,"Near 30 / middle 20 / far 10"),new Choice(2,"Near 0 / middle 255 / far 0"),new Choice(3,"Near 255 / middle 0 / far 0")},0);
            choice(distance,"ownerVisibilityDistance",DistanceChoices(false),0);choice(packed,"ownerVisibilityPacked",PackedChoices(false),0);
            load.Name="ownerVisibilityLoad";load.Text="Load selected input copy";add(load,34);reset.Name="ownerVisibilityReset";reset.Text="Reset applied copy before pass";add(reset,34);execute.Name="ownerVisibilityExecute";execute.Text="Apply one owner visibility pass";add(execute,40);summary.Name="ownerVisibilitySummary";add(summary,154);feedback.Name="ownerVisibilityFeedback";add(feedback,82);
            candidateScan.Name="ownerVisibilityCandidates";candidateScan.Text="Scan produced-visibility candidates…";candidateScan.Enabled=false;add(candidateScan,40);candidateScan.Click+=delegate{if(candidateScan.Enabled&&state!=null)using(var child=new PracticeVisibleCandidateStudy(programs,startup,state))child.ShowDialog(this);};
            add(new Label{Text="Active records · select to inspect stored fields"},22);fleets.Name="ownerVisibilityFleets";fleets.HorizontalScrollbar=true;add(fleets,146);fleets.SelectedIndexChanged+=delegate{if(!changing)RefreshDetails();};details.Name="ownerVisibilityDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,310);var close=new Button{Name="ownerVisibilityClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            load.Click+=delegate{LoadCopy();};reset.Click+=delegate{if(initial!=null){state=initial;Feedback("Applied input copy restored before the pass; pending selectors are unchanged.");Sync(true);}};execute.Click+=delegate{if(state==null)return;try{state=state.Execute();Feedback(state.Skipped?"Source returned before clearing any flag: local mode 2 has no selected weapon. All bytes hold.":"One owner pass complete. Current levels and cached history are data; no firing or world update ran.");Sync(false);}catch(ArgumentException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(InvalidOperationException e){Reject(e.Message);}};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{foreach(Keys k in new[]{Keys.Enter,Keys.Space})if((GetAsyncKeyState((int)k)&0x8000)!=0)held.Add(k);};Configure();LoadCopy();
        }
        static Choice[] OwnerChoices(){return new[]{new Choice(-1,"Neutral owner -1"),new Choice(0,"Owner 0"),new Choice(1,"Owner 1"),new Choice(2,"Owner 2"),new Choice(3,"Owner 3")};}
        static Choice[] ObserverChoices(bool local){if(local)return new[]{new Choice(1,"Observer kind 1"),new Choice(12,"Observer kind 12 · empty source row")};var list=new List<Choice>();for(int n=0;n<=12;n++)list.Add(new Choice(n,n==0?"Observer kind 0 · inactive":n==6||n==7||n==8?"Kind "+n+" · excluded stored observer":n==12?"Observer kind 12 · empty source row":"Observer kind "+n));return list.ToArray();}
        static Choice[] DistanceChoices(bool local){var list=new List<Choice>();foreach(int n in local?new[]{0,20,21,500,501}:new[]{0,10,11,30,31,255,256})list.Add(new Choice(n,(local?"Selected-origin distance ":"Observer distance ")+n));return list.ToArray();}
        static Choice[] PackedChoices(bool local){var list=new List<Choice>();foreach(int n in local?new[]{0,85,170}:new[]{0,4,85,170,255})list.Add(new Choice(n,"Supplied packed history 0x"+n.ToString("X2")));return list.ToArray();}
        void Configure()
        {
            changing=true;int p=Value(preset);Fill(observer,ObserverChoices(p==4),1);Fill(distance,DistanceChoices(p==4),0);Fill(packed,PackedChoices(p==4),0);
            if(p==4&&Value(owner)<0)owner.SelectedIndex=1;if(p==4)radii.SelectedIndex=3;
            weapon.Enabled=p==2||p==4;observer.Enabled=p>=3;radii.Enabled=p==3;distance.Enabled=packed.Enabled=p>=3;changing=false;
        }
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){var b=ActiveControl as Button;if(b!=null){e.Handled=e.SuppressKeyPress=true;if(held.Add(e.KeyCode)&&b.Enabled)b.PerformClick();return;}}base.OnKeyDown(e);}
        void Feedback(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.LightSteelBlue;feedback.Text=text;}
        void Reject(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.Orange;feedback.Text="Rejected; applied copy preserved. "+text;Sync(false);}
        void LoadCopy(){try{var next=PracticeOwnerVisibilityStudyState.Create(programs,startup,Value(preset),Value(owner),Value(weapon),Value(observer),Value(radii),Value(distance),Value(packed));state=initial=next;Feedback("Input copy prepared. Apply runs one pass; prefilled tags are not computed visibility until then.");Sync(true);}catch(ArgumentException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(UnauthorizedAccessException e){Reject(e.Message);}}
        void Sync(bool defaultView)
        {
            if(IsDisposed||Disposing)return;execute.Enabled=state!=null&&!state.Executed;reset.Enabled=state!=null;candidateScan.Enabled=state!=null&&state.Executed&&!state.Skipped&&state.Preset<2&&(state.Owner==0||state.Owner==1);if(state==null){summary.Text="No admitted owner-visibility copy loaded.";return;}
            int viewed=defaultView?(state.Preset<3?15:1):fleets.SelectedItem==null?-1:((PracticeOwnerFleetVisibility)fleets.SelectedItem).Fleet;pool=state.CopyFleetPool();int nonzero=0;foreach(var f in state.ActiveFleets)if(f.Level!=0)nonzero++;
            summary.Text="Applied: owner "+state.Owner+" · preset "+state.Preset+"\r\nMode "+state.Mode+" · local "+state.LocalOwner+" · weapon "+state.SelectedWeapon+"\r\nPass "+(!state.Executed?"pending":state.Skipped?"early return":"complete")+" · changed records "+state.ChangedCount+"\r\nNonzero active levels "+nonzero+"\r\nFleet 14 / 15 levels "+state.Fleet(14).Level+" / "+state.Fleet(15).Level+"\r\n"+(state.Preset<3?"Positions from copied stage 4.":"Positions/radii/history are supplied fixtures.");
            changing=true;fleets.BeginUpdate();fleets.Items.Clear();foreach(var item in state.ActiveFleets){int at=fleets.Items.Add(item);if(item.Fleet==viewed)fleets.SelectedIndex=at;}if(fleets.SelectedIndex<0&&fleets.Items.Count>0)fleets.SelectedIndex=0;fleets.EndUpdate();changing=false;RefreshDetails();
        }
        void RefreshDetails()
        {
            if(state==null||IsDisposed||Disposing)return;var item=fleets.SelectedItem as PracticeOwnerFleetVisibility;string viewed="No viewed active record.";
            if(item!=null){var b=state.Before(item.Fleet);viewed=item+"\r\nWorld "+item.X+","+item.Y+"\r\nCurrent level "+b.Level+" -> "+item.Level+"\r\nFar-contact byte "+b.ContactByte+" -> "+item.ContactByte+"\r\nPacked history 0x"+b.HistoryByte.ToString("X2")+" -> 0x"+item.HistoryByte.ToString("X2");if(state.Owner>=0)viewed+="\r\nInput packed >> "+(state.Owner*2)+" = "+(b.HistoryByte>>(state.Owner*2));}
            details.Text=viewed+"\r\nGray/blue/green/gold = raw stored levels 0/1/2/3. Orange outline = nonzero far-contact byte; white = viewed record. Before-pass fixture tags and source early returns remain raw data, not freshly computed visibility."+
                "\r\nCurrent levels and far-contact flags reset on a normal pass. Near/middle/far radii are tested independently. Stored observer kinds 6/7/8 are excluded. Kind 12 has a verified empty source table row, so its later special branch is not reached."+
                "\r\nKinds 2/10/25 merge packed per-owner history. The original upgrade test compares the shifted byte WITHOUT masking the other owner fields first. Preserve that behavior; cached levels can survive without a current observation. Owner -1 skips the cache merge."+
                "\r\nLocal mode 2 with weapon 0 returns before any clear. With a selected weapon it also filters from the supplied selected-fleet origin and original range/combat table. This is one owner's copied pass, not the whole owner iteration or full AI policy. Parent map, battle and settings are preserved.";canvas.Invalidate();
        }
        sealed class VisibilityCanvas:Control
        {
            readonly PracticeOwnerVisibilityStudy owner;internal VisibilityCanvas(PracticeOwnerVisibilityStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);if(owner.state==null||Width<200||Height<250)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float size=Math.Min(Width-68,Height-170),left=(Width-size)/2,top=58;Func<int,int,PointF> point=delegate(int x,int y){return new PointF(left+x*size/630f,top+y*size/630f);};
                g.DrawString((owner.state.Preset<3?"Copied stage-4 records":"Supplied visibility-only records")+" · X across / Y down",owner.Font,Brushes.WhiteSmoke,new PointF(12,14));g.DrawString(owner.state.Executed?(owner.state.Skipped?"Source early return: stored bytes unchanged.":"One owner pass applied; colors show stored data."):"Before the pass: prepared flags may be diagnostic tags.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,34,Width-24,28));
                using(var pen=new Pen(Color.FromArgb(35,61,80)))for(int n=0;n<=6;n++){var a=point(n*100,0);var b=point(n*100,630);g.DrawLine(pen,a,b);a=point(0,n*100);b=point(630,n*100);g.DrawLine(pen,a,b);}g.DrawRectangle(Pens.SlateGray,left,top,size,size);
                var selected=owner.fleets.SelectedItem as PracticeOwnerFleetVisibility;
                foreach(var item in owner.state.ActiveFleets)
                {
                    var p=point(item.X,item.Y);Brush brush=item.Level==3?Brushes.Gold:item.Level==2?Brushes.MediumSpringGreen:item.Level==1?Brushes.LightSkyBlue:Brushes.SlateGray;g.FillEllipse(brush,p.X-4,p.Y-4,8,8);if(item.ContactByte!=0)g.DrawEllipse(Pens.Orange,p.X-7,p.Y-7,14,14);
                    if(selected!=null&&selected.Fleet==item.Fleet){using(var pen=new Pen(Color.White,2))g.DrawEllipse(pen,p.X-11,p.Y-11,22,22);g.DrawString("Fleet "+item.Fleet+" · level "+item.Level,owner.Font,Brushes.WhiteSmoke,new PointF(Math.Max(4,Math.Min(Width-150,p.X+13)),Math.Max(66,p.Y-14)));}
                }
                if(owner.state.Mode==2){int at=owner.state.SelectedFleet*492;var p=point(BitConverter.ToInt16(owner.pool,at+12),BitConverter.ToInt16(owner.pool,at+14));using(var pen=new Pen(Color.Violet,2)){g.DrawLine(pen,p.X-6,p.Y,p.X+6,p.Y);g.DrawLine(pen,p.X,p.Y-6,p.X,p.Y+6);}g.DrawString("Supplied filter origin "+owner.state.SelectedFleet,owner.Font,Brushes.Violet,new PointF(Math.Max(4,Math.Min(Width-190,p.X-30)),p.Y+14));}
                g.DrawString("Raw levels: gray 0 · blue 1 · green 2 · gold 3\r\nOrange = far-contact byte · white = viewed record.\r\nCoincident records share a marker; the list retains every id.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-82,Width-24,78));
            }
        }
    }
}
