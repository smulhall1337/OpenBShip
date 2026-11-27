using System;using System.IO;using System.Text;using System.Drawing;using System.Windows.Forms;using System.Collections.Generic;using System.Runtime.InteropServices;
namespace FleetLab
{
    public sealed class PracticeNavigationMovementStudy : Form
    {
        readonly ComboBox profile=new ComboBox();readonly Button load=new Button(),reset=new Button(),one=new Button(),many=new Button(),all=new Button(),nextMove=new Button(),watchButton=new Button();
        readonly Label summary=new Label(),feedback=new Label(),cell=new Label();readonly TextBox details=new TextBox();readonly HashSet<Keys> held=new HashSet<Keys>();readonly GridCanvas canvas;
        PracticeGoalRouteStudyBasis basis;PracticeNavigationMovementStudyState state;Timer watch;int watchRemaining,selectedX=13,selectedY=12;bool closing;
        internal PracticeNavigationMovementStudyState StudyState {get{return state;}}
        internal bool Watching {get{return watchRemaining>0;}}
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeNavigationMovementStudy(string programs,PracticeMapStartup startup)
        {
            Name="navigationMovementStudy";Text="Navigation and movement · copied original polls";StartPosition=FormStartPosition.CenterParent;Size=new Size(1180,850);MinimumSize=new Size(980,740);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Padding=new Padding(8)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,385));Controls.Add(layout);
            canvas=new GridCanvas(this){Name="navigationMoveCanvas",Dock=DockStyle.Fill};layout.Controls.Add(canvas,0,0);
            var scroll=new Panel{Name="navigationMoveScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);
            var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control control,int height)
            {
                control.Width=350;control.Height=height;control.Margin=new Padding(0,3,0,3);
                if(control is ComboBox||control is Button){control.BackColor=Color.FromArgb(28,55,76);control.ForeColor=ForeColor;}
                var button=control as Button;if(button!=null){button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;button.EnabledChanged+=delegate{if(button.IsDisposed||button.Disposing)return;button.BackColor=button.Enabled?Color.FromArgb(28,55,76):Color.LightGray;button.ForeColor=button.Enabled?ForeColor:Color.Black;};}
                side.Controls.Add(control);
            };
            add(new Label{Text="Run copied navigation followed by the normal two-batch movement scheduler. Gold moves only on fleet visits. Clock/phase and the empty world are supplied inputs; missions and opponent AI do not run."},86);
            add(new Label{Text="Pending preset (apply with Load)"},20);profile.Name="navigationMoveProfile";profile.DropDownStyle=ComboBoxStyle.DropDownList;profile.DropDownWidth=510;
            foreach(string name in PracticeNavigationMovementStudyState.Presets)profile.Items.Add(name);profile.SelectedIndex=0;add(profile,30);
            load.Name="navigationMoveLoad";load.Text="Load selected copy";add(load,34);reset.Name="navigationMoveReset";reset.Text="Reset applied copy";add(reset,34);
            one.Name="navigationMoveOne";one.Text="1 supplied poll";add(one,36);many.Name="navigationMoveMany";many.Text="32 supplied polls";add(many,36);
            nextMove.Name="navigationMoveNext";nextMove.Text="Until pose changes · up to 64 polls";add(nextMove,38);
            all.Name="navigationMoveAll";all.Text="Up to 256 supplied polls / end of window";add(all,38);
            watchButton.Name="navigationMoveWatch";watchButton.Text="Watch 32 polls (debug)";add(watchButton,36);
            summary.Name="navigationMoveSummary";add(summary,246);feedback.Name="navigationMoveFeedback";add(feedback,92);cell.Name="navigationMoveCell";add(cell,54);
            details.Name="navigationMoveDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.WordWrap=true;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,360);
            var close=new Button{Name="navigationMoveClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            watch=new Timer{Interval=40};watch.Tick+=WatchTick;
            profile.SelectedIndexChanged+=delegate{StopWatching();};
            load.Click+=delegate{StopWatching();if(basis==null)return;try{state=new PracticeNavigationMovementStudyState(basis,profile.SelectedIndex);PreparedFeedback();Sync();}catch(ArgumentException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}};
            reset.Click+=delegate{StopWatching();if(state==null)return;state=state.Reset();PreparedFeedback();Sync();};
            one.Click+=delegate{Apply(1,false);};many.Click+=delegate{Apply(32,false);};all.Click+=delegate{Apply(256,false);};nextMove.Click+=delegate{Apply(64,true);};
            watchButton.Click+=delegate{if(Watching){StopWatching();feedback.Text="Viewing stopped. No automatic resume.";}else if(state!=null&&state.CanAdvance){watchRemaining=Math.Min(32,PracticeNavigationMovementStudyState.MaximumPolls-state.Polls);watch.Start();Sync();}};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};
            Activated+=delegate{foreach(var key in new[]{Keys.Enter,Keys.Space,Keys.Escape})if((GetAsyncKeyState((int)key)&0x8000)!=0)held.Add(key);};
            Deactivate+=delegate{StopWatching();held.Clear();};
            try{basis=PracticeGoalRouteStudyBasis.Create(programs,startup);state=new PracticeNavigationMovementStudyState(basis,0);PreparedFeedback();}
            catch(ArgumentException e){feedback.Text=e.Message;}catch(NotSupportedException e){feedback.Text=e.Message;}catch(IOException e){feedback.Text=e.Message;}
            Sync();
        }
        void PreparedFeedback(){feedback.Text="Prepared queued destination: "+QueueText(state.State.CopyFleetPool())+". The cached target 600,610 is deliberately stale; start replaces it. Try Until pose changes, then inspect the visits and queue.";}
        void Reject(string message){feedback.Text="No change: "+message;Sync();}
        void Apply(int count,bool untilMove)
        {
            StopWatching();if(state==null||!state.CanAdvance)return;
            try
            {
                var before=state;state=untilMove?state.AdvanceUntilPositionChanges():state.Advance(count);int applied=state.Polls-before.Polls;
                feedback.Text=untilMove?(state.Position!=before.Position?"Pose changed after "+applied+" supplied polls.":state.NavigationCompletions>before.NavigationCompletions?"Navigation completed with the pose held.":"No pose change within "+applied+" supplied polls; inspect the goal, retry and scheduler."):applied+" supplied poll(s) applied. "+LastMovementText();
                Sync();
            }
            catch(ArgumentException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(InvalidOperationException e){Reject(e.Message);}
        }
        void WatchTick(object sender,EventArgs e)
        {
            if(closing||IsDisposed||Disposing||state==null||!state.CanAdvance||watchRemaining<=0){StopWatching();return;}
            try{state=state.Advance(1);watchRemaining--;feedback.Text="Bounded viewing: "+watchRemaining+" poll(s) left. "+LastMovementText();if(watchRemaining==0||!state.CanAdvance)StopWatching();Sync();}
            catch(ArgumentException error){StopWatching();Reject(error.Message);}catch(NotSupportedException error){StopWatching();Reject(error.Message);}catch(IOException error){StopWatching();Reject(error.Message);}catch(InvalidOperationException error){StopWatching();Reject(error.Message);}
        }
        internal void StepWatchForCheck(){WatchTick(this,EventArgs.Empty);}
        internal void StopWatching(){if(watch!=null)watch.Stop();watchRemaining=0;Sync();}
        string LastMovementText()
        {
            if(state==null||state.LastUpdate==null)return "No poll yet.";
            var visits=state.LastUpdate.Movement.FleetUpdates;return visits.Count==0?"Actor was not visited in the last poll.":"Last actor visit: "+visits[visits.Count-1].Update.Outcome+".";
        }
        static string QueueText(byte[] f){int x=BitConverter.ToInt16(f,16*492+40),y=BitConverter.ToInt16(f,16*492+50);return x==-1?"none; held Y="+y:x+","+y;}
        internal void SelectCell(int x,int y){if(x<0||x>63||y<0||y>63)return;selectedX=x;selectedY=y;Sync();}
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);if(e.KeyCode!=Keys.Enter&&e.KeyCode!=Keys.Space&&e.KeyCode!=Keys.Escape)return;
            var button=ActiveControl as Button;if(e.KeyCode!=Keys.Escape&&button==null)return;e.Handled=true;e.SuppressKeyPress=true;
            if(!held.Add(e.KeyCode))return;if(e.KeyCode==Keys.Escape)Close();else if(button.Enabled)button.PerformClick();
        }
        protected override void OnFormClosing(FormClosingEventArgs e){closing=true;StopWatching();base.OnFormClosing(e);if(e.Cancel)closing=false;}
        void ReleaseWatch(){watchRemaining=0;if(watch==null)return;var timer=watch;watch=null;timer.Stop();timer.Tick-=WatchTick;timer.Dispose();}
        protected override void OnFormClosed(FormClosedEventArgs e){ReleaseWatch();base.OnFormClosed(e);}
        protected override void Dispose(bool disposing){if(disposing){closing=true;ReleaseWatch();}base.Dispose(disposing);}
        void Sync()
        {
            if(closing||IsDisposed||Disposing)return;
            load.Enabled=basis!=null;reset.Enabled=state!=null;bool enabled=state!=null&&state.CanAdvance;one.Enabled=many.Enabled=all.Enabled=nextMove.Enabled=enabled;
            watchButton.Enabled=enabled||Watching;watchButton.Text=Watching?"Stop watching ("+watchRemaining+" left)":"Watch 32 polls (debug)";
            if(state==null){summary.Text="No admitted copy. Close remains available.";canvas.Invalidate();return;}
            var f=state.State.CopyFleetPool();var task=state.State.Backing.CopyTaskRecord(16);var bytes=state.State.Grid.CopyCells();int blocked=0,unvisited=0,costs=0;
            foreach(byte value in bytes){if(value==255)blocked++;else if(value==254)unvisited++;else if(value>=1&&value<=127)costs++;}
            string navigation=state.LastUpdate==null?"prepared":state.LastUpdate.Navigation==null?"Dormant":state.LastUpdate.Navigation.Outcome.ToString();
            summary.Text="Applied: "+PracticeNavigationMovementStudyState.Presets[state.Preset]+"\r\nPolls / supplied phase: "+state.Polls+" / 256\r\nLast clock "+state.LastSourceClock+"; next "+state.NextSourceClock+"\r\nClock step: "+state.ClockStep+" ms (supplied)\r\nFleet position: "+state.Position.X+","+state.Position.Y+"\r\nCached target: "+BitConverter.ToInt32(task,296)+","+BitConverter.ToInt32(task,300)+"\r\nFirst point: "+QueueText(f)+"\r\nGoal "+BitConverter.ToInt32(task,200)+"; nav "+BitConverter.ToInt32(task,304)+"; collision "+f[16*492+482].ToString("X2")+"\r\nScheduler start "+state.State.Schedule.BatchStart+"; cycles "+state.State.Schedule.CompletedCycles+"\r\nActor visits "+state.ActorVisits+"; pose changes "+state.PositionChanges+"\r\nQueue consumed "+state.QueueConsumptions+"; nav completed "+state.NavigationCompletions+"\r\nLast navigation: "+navigation+"\r\nCosts "+costs+"; FF "+blocked+"; FE "+unvisited;
            byte selected=state.State.Grid.Cell(selectedX,selectedY);cell.Text="Cell X="+selectedX+", Y="+selectedY+" · world "+(selectedX*10)+","+(selectedY*10)+"\r\nByte "+selected+" (0x"+selected.ToString("X2")+")"+(!state.HasGridOutput?" · supplied CD input fill":"");
            var text=new StringBuilder();text.AppendLine("Queue pairs (X=-1 means empty; Y can remain held):");for(int n=0;n<5;n++)text.AppendLine((n+1)+": "+BitConverter.ToInt16(f,16*492+40+n*2)+","+BitConverter.ToInt16(f,16*492+50+n*2));
            text.AppendLine();text.AppendLine(LastMovementText());text.AppendLine("Raw speed "+BitConverter.ToInt16(f,16*492+60)+" / movement flag "+f[16*492+485]+"; retry "+task[284]+"; deadline "+BitConverter.ToInt32(task,4)+".");
            text.AppendLine("Selected-other fleet 249, raw waypoint counter 4. Gold trail is a modern inspection aid; cyan is the first queued point, green the cached navigation target.");
            text.AppendLine();text.AppendLine("One supplied poll: original navigation prephase, verified inactive selector-1 middle call, then two four-slot movement batches. Only scheduler visits move this actor. Source task navigation=1 locks arrival until a later navigation visit; completion keeps goal 4. Unlocked arrivals then consume the copied queue. ReachedWaypoint names entry into the arrival callback; inspect the queue and navigation fields for completion.");
            text.AppendLine();text.AppendLine("Five bounded original histories support this view. The first uses a held clock; others supply +40 ms per poll. Initial target 600,610 and collision FE are deliberate inputs. The world has inactive terrain/secondary records; the blocked preset supplies stationary primary blockers. This is not a mission or complete owner AI.");
            text.AppendLine();text.AppendLine("Watch supplies one poll per UI timer tick, up to 32. Slow rendering can take longer; there is no catch-up or automatic resume. Focus loss, pending selector changes and any manual action stop watching. Reset restores the applied preset; Load applies the pending selection.");
            text.AppendLine();text.AppendLine("Field[x,y] uses x*64+y, ten source units per cell. Markers/trail use continuous world positions; the recovered cost grid truncates coordinates. Nonmultiple input is diagnostic; natural UI admission is unverified. No original renderer, audio, network, opponent controller or original clock sampler runs. Existing battle/settings are separate.");
            details.Text=text.ToString();canvas.Invalidate();
        }
        sealed class GridCanvas:Control
        {
            readonly PracticeNavigationMovementStudy owner;RectangleF plot;
            internal GridCanvas(PracticeNavigationMovementStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left&&plot.Contains(e.Location))owner.SelectCell((int)((e.X-plot.Left)*64/plot.Width),(int)((e.Y-plot.Top)*64/plot.Height));}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);var study=owner.state;if(study==null)return;var f=study.State.CopyFleetPool();var task=study.State.Backing.CopyTaskRecord(16);int x=BitConverter.ToInt16(f,16*492+40),y=BitConverter.ToInt16(f,16*492+50);
                plot=PracticeRouteGridDrawing.Paint(e.Graphics,ClientSize,owner.Font,new PracticeRouteGridView{Grid=study.State.Grid,HasOutput=study.HasGridOutput,Origin=study.Position,Target=new Point(BitConverter.ToInt32(task,296),BitConverter.ToInt32(task,300)),Waypoint=x==-1?(Point?)null:new Point(x,y),Trail=study.CopyTrail(),ContinuousPosition=true,OriginOnTop=true,SelectedX=owner.selectedX,SelectedY=owner.selectedY,
                    Header="Recovered navigation + movement · supplied normal polls",Subheader="Gold fleet / trail · cyan first point · green cached target",Footer="Costs 1..127 · red FF blocked · dark FE unvisited\r\nGold moves on source scheduler visits. Clock and phase are supplied."});
            }
        }
    }
}
