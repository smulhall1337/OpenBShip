using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace FleetLab {
    // Modern diagnostic UI over copied source state; no autonomous mission.
    public sealed class PracticeTimedStudy : Form {
        sealed class Choice {
            internal int Id; internal string Text;
            public override string ToString() { return Text; }
        }
        readonly PracticeTimedStudyState initial;
        PracticeTimedStudyState state;
        readonly ComboBox source = new ComboBox(), weapon = new ComboBox(), target = new ComboBox();
        readonly NumericUpDown offsetX = new NumericUpDown(), offsetY = new NumericUpDown();
        readonly ListBox objects = new ListBox();
        readonly Label clock = new Label(), ammo = new Label(), status = new Label();
        readonly TextBox details = new TextBox();
        readonly Button create = new Button();
        readonly TimedCanvas canvas;
        readonly HashSet<Keys> held = new HashSet<Keys>();
        bool changing;
        internal PracticeTimedStudyState StudyState { get { return state; } }
        internal int SelectedObject { get { return objects.SelectedItem == null ? -1 : ((Choice)objects.SelectedItem).Id; } }

        public PracticeTimedStudy(Scenario scene, PracticeFleetSnapshot selected, PracticeDeployment deployment,
                                  string programs, int selectedUnit, ClassicMessages messages = null) {
            if (scene == null || selected == null || deployment == null || !deployment.IncludesMapObjects ||
                selected.Owner != 0 || (selected.Kind != 1 && selected.Kind != 9))
                throw new ArgumentException("Choose a player 1 grouped fleet in the first Practice startup preview.");
            PracticeTimedWeaponCatalog catalog;
            try { catalog = PracticeTimedWeaponCatalog.Load(programs); }
            catch (IOException) { throw new InvalidDataException("Run Setup again to import the timed-weapon table."); }
            initial = state = new PracticeTimedStudyState(catalog, deployment.CopyShipPool(), deployment.CopyFleetPool(), 0);
            Text = "Recovered timed objects · manual study"; Size = new Size(1120, 820); MinimumSize = new Size(900, 640);
            StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(12,23,35);
            ForeColor = Color.WhiteSmoke; Font = new Font("Segoe UI",9); KeyPreview = true;
            var layout = new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(12) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,310));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,92)); Controls.Add(layout);
            canvas = new TimedCanvas(scene) { Dock=DockStyle.Fill,AccessibleName="Schematic timed-object map" }; layout.Controls.Add(canvas,0,0);
            canvas.Picked += delegate(Point point) { if (target.SelectedItem == null) return; int slot=((Choice)target.SelectedItem).Id; byte[] f=state.Objects.CopyFleetPool(); offsetX.Value=Math.Max(-639,Math.Min(639,point.X-BitConverter.ToInt16(f,slot*492+12))); offsetY.Value=Math.Max(-639,Math.Min(639,point.Y-BitConverter.ToInt16(f,slot*492+14))); };
            var scroll = new Panel { Dock=DockStyle.Fill,AutoScroll=true }; layout.Controls.Add(scroll,1,0);
            var side = new FlowLayoutPanel { Dock=DockStyle.Top,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(8,0,0,0) }; scroll.Controls.Add(side);
            Action<Control,int> add = delegate(Control c,int height) { c.Width=278;c.Height=height;c.Margin=new Padding(3,2,3,2);side.Controls.Add(c); };
            add(clock,38);
            add(new Label { Text="Source unit" },20); add(source,28);
            add(new Label { Text="Test weapon (original ammo data)" },20); add(weapon,28); add(ammo,38);
            add(new Label { Text="Fixed target fleet" },20); add(target,28);
            add(new Label { Text="Target offset X/Y. Click the map to choose a world point relative to that fleet." },40);
            var offsets = new FlowLayoutPanel { WrapContents=false };
            foreach(var control in new[]{offsetX,offsetY}){control.Minimum=-639;control.Maximum=639;control.Width=100;}
            offsetX.AccessibleName="Timed target offset X";offsetY.AccessibleName="Timed target offset Y";
            offsets.Controls.Add(offsetX);offsets.Controls.Add(offsetY);add(offsets,32);
            create.Text="Create test object";add(create,32);
            var steps = new FlowLayoutPanel { WrapContents=false };
            var one = new Button { Text="+40 ms",Width=100,Height=30,Name="timedOne" };
            var second = new Button { Text="+1 s",Width=100,Height=30,Name="timedSecond" };steps.Controls.Add(one);steps.Controls.Add(second);add(steps,36);
            var boundary = new Button { Text="Advance to expiry boundary",Name="timedBoundary" };add(boundary,32);
            var paused = new Button { Text="Advance paused +1 s",Name="timedPaused" };add(paused,32);
            objects.IntegralHeight=false;objects.AccessibleName="Active timed objects";add(objects,100);
            details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;details.AccessibleName="Selected timed object data";add(details,128);
            var reset = new Button { Text="Reset study",Name="timedReset" };add(reset,32);
            var close = new Button { Text="Close",DialogResult=DialogResult.Cancel };add(close,30);CancelButton=close;
            status.Dock=DockStyle.Fill;status.Text="Create an object, then advance the supplied clock.";status.ForeColor=Color.LightSteelBlue;
            layout.Controls.Add(status,0,1);layout.SetColumnSpan(status,2);
            foreach(var combo in new[]{source,weapon,target}){combo.DropDownStyle=ComboBoxStyle.DropDownList;combo.DropDownWidth=360;}
            source.AccessibleName="Timed source unit";weapon.AccessibleName="Timed test weapon";target.AccessibleName="Timed fixed target fleet";
            foreach(var unit in selected.Units)source.Items.Add(new Choice { Id=unit.Slot,Text="Unit "+(unit.Slot+1)+" · "+(messages!=null&&unit.Type>=0&&unit.Type<13?messages.ShipName(unit.Type):"type "+unit.Type) });
            foreach(var fleet in deployment.Fleets)if(fleet.Owner>=0&&fleet.Owner!=selected.Owner&&(fleet.Kind==1||fleet.Kind==9))
                target.Items.Add(new Choice { Id=fleet.FleetSlot,Text="Fleet "+(fleet.FleetSlot+1)+" · player "+(fleet.Owner+1)+" · "+fleet.WorldOrigin.X+","+fleet.WorldOrigin.Y });
            source.SelectedIndexChanged += delegate { if(!changing)LoadWeapons(); };
            weapon.SelectedIndexChanged += delegate { if(!changing)UpdateSelection(); };
            target.SelectedIndexChanged += delegate { if(!changing)UpdateSelection(); };
            offsetX.ValueChanged += delegate { UpdateSelection(); };offsetY.ValueChanged += delegate { UpdateSelection(); };
            objects.SelectedIndexChanged += delegate { if(!changing)UpdateSelection(); };
            create.Click += delegate { CreateObject(); };one.Click += delegate { AdvanceFrames(1,false); };second.Click += delegate { AdvanceFrames(25,false); };
            paused.Click += delegate { AdvanceFrames(25,true); };boundary.Click += delegate { AdvanceToBoundary(); };
            reset.Click += delegate { state=initial;RefreshState();SetStatus("Reset to the copied startup pools and clock0.",false); };
            KeyUp += delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate += delegate { held.Clear();canvas.CancelPick(); };
            if(source.Items.Count>0)source.SelectedIndex=Math.Max(0,Math.Min(source.Items.Count-1,selectedUnit));
            if(target.Items.Count>0)target.SelectedIndex=0;RefreshState();SetStatus("Create an object, then advance the supplied clock. Closing preserves the active battle and deployment.",false);
        }
        int SourceSlot { get { return source.SelectedItem==null?-1:((Choice)source.SelectedItem).Id; } }
        int WeaponId { get { return weapon.SelectedItem==null?-1:((Choice)weapon.SelectedItem).Id; } }
        static Button FocusedButton(Control owner) {
            foreach(Control child in owner.Controls){if(child.Focused)return child as Button;Button nested=FocusedButton(child);if(nested!=null)return nested;}return null;
        }
        protected override bool ProcessCmdKey(ref Message message,Keys keyData) {
            Button button=FocusedButton(this);
            if(keyData==Keys.Enter&&button!=null){
                bool repeat=(message.LParam.ToInt64()&0x40000000L)!=0;
                if(!repeat&&held.Add(Keys.Enter)&&button.Enabled)button.PerformClick();
                return true;
            }
            return base.ProcessCmdKey(ref message,keyData);
        }
        void LoadWeapons() {
            int previous=WeaponId;changing=true;weapon.Items.Clear();int chosen=-1;byte[] ships=state.Objects.CopyShipPool();
            if(SourceSlot>=0)for(int id=14;id<=30;id++){int count=BitConverter.ToInt16(ships,SourceSlot*268+196+id*2);int index=weapon.Items.Add(new Choice { Id=id,Text=ClassicWeapons.Name(id)+" · "+(count==-1?"infinite":count.ToString()) });if(id==previous)chosen=index;else if(chosen<0&&(count==-1||count>=2))chosen=index;}
            if(weapon.Items.Count>0)weapon.SelectedIndex=chosen<0?0:chosen;changing=false;UpdateSelection();
        }
        void SetStatus(string message,bool boundary) { status.ForeColor=boundary?Color.Gold:Color.LightSteelBlue;status.Text=message+"\r\nManual copied-state study: supplied 40 ms steps; fixed fleet targets. Test creation bypasses normal firing admission and media. Last-round depletion and expiry/impacts are unsupported."; }
        void CreateObject() {
            if(!create.Enabled)return;
            try { state=state.Create(WeaponId,SourceSlot,((Choice)target.SelectedItem).Id,(int)offsetX.Value,(int)offsetY.Value);int created=state.LastCreatedSlot;RefreshState();if(created>=0)for(int i=0;i<objects.Items.Count;i++)if(((Choice)objects.Items[i]).Id==created){objects.SelectedIndex=i;break;}SetStatus(state.LastCreationOutcome==PracticeTimedCreationOutcome.Created?"Created test object "+(created+1)+".":state.LastCreationOutcome==PracticeTimedCreationOutcome.PoolFull?"The 80-record pool is full. Reset to clear it.":"This unit has no ammunition for that weapon.",false); }
            catch(NotSupportedException e){SetStatus(e.Message,true);}catch(InvalidDataException e){SetStatus(e.Message,true);}
        }
        void AdvanceToBoundary() { if(objects.Items.Count==0){SetStatus("Create an object before advancing to its expiry boundary.",false);return;}AdvanceFrames(5000,false); }
        void AdvanceFrames(int count,bool paused) {
            int completed=0;string limit=null;
            for(;completed<count;completed++)try{state=state.AdvanceFrame(paused);}catch(NotSupportedException){limit="Stopped before object expiry; impact behavior is not reconstructed.";break;}catch(InvalidDataException e){limit=e.Message;break;}
            RefreshState();SetStatus("Advanced "+completed+" × 40 ms"+(paused?" with paused deadline compensation.":".")+(limit==null?(count==5000?" Reached the 5000-step guard; advance again to continue.":""):" "+limit),limit!=null);
        }
        void RefreshState() {
            int selected=SelectedObject;changing=true;objects.Items.Clear();byte[] timed=state.Objects.CopyTimedPool();
            for(int i=0;i<80;i++)if(BitConverter.ToInt32(timed,i*268)!=0){int index=objects.Items.Add(new Choice { Id=i,Text="#"+(i+1)+" · "+BitConverter.ToInt16(timed,i*268+8)+","+BitConverter.ToInt16(timed,i*268+10) });if(i==selected)objects.SelectedIndex=index;}
            if(objects.SelectedIndex<0&&objects.Items.Count>0)objects.SelectedIndex=0;changing=false;
            clock.Text="Clock "+state.ClockMilliseconds+" ms · "+objects.Items.Count+"/80 objects\r\nUpdates "+state.MovementFrames+" · paused "+state.PausedFrames;
            canvas.SetPools(state.Objects.CopyFleetPool(),timed);LoadWeapons();UpdateSelection();
        }
        void UpdateSelection() {
            if(changing)return;int count=0;if(SourceSlot>=0&&WeaponId>=14)count=BitConverter.ToInt16(state.Objects.CopyShipPool(),SourceSlot*268+196+WeaponId*2);
            create.Enabled=SourceSlot>=0&&WeaponId>=14&&target.SelectedItem!=null&&(count==-1||count>=2);
            ammo.Text=count==-1?"Infinite ammunition.":count==1?"Last round: depletion callback is unsupported.":count==0?"No ammunition for this weapon.":"Ammunition: "+count;
            int slot=SelectedObject;canvas.Selected=slot;
            if(target.SelectedItem!=null){int id=((Choice)target.SelectedItem).Id;byte[] fleet=state.Objects.CopyFleetPool();canvas.TargetWorld=new Point(BitConverter.ToInt16(fleet,id*492+12)+(int)offsetX.Value,BitConverter.ToInt16(fleet,id*492+14)+(int)offsetY.Value);}
            if(slot<0)details.Text="No active timed object.\r\nCreation and movement are isolated from the active Classic battle.";
            else { byte[] t=state.Objects.CopyTimedPool();int at=slot*268;uint deadline=BitConverter.ToUInt32(t,at+24);details.Text="Object "+(slot+1)+" · weapon "+BitConverter.ToInt32(t,at)+"\r\nWorld "+BitConverter.ToInt16(t,at+8)+", "+BitConverter.ToInt16(t,at+10)+"\r\nSource unit "+(BitConverter.ToInt16(t,at+16)+1)+" · target fleet "+(BitConverter.ToInt16(t,at+12)+1)+"\r\nDeadline "+deadline+" ms\r\nRemaining "+unchecked((int)(deadline-state.ClockMilliseconds))+" ms\r\nLast early return: "+(state.LastStoppedAtRecord<0?"none":"object "+(state.LastStoppedAtRecord+1)); }
            canvas.Invalidate();
        }
        sealed class TimedCanvas : Control {
            readonly byte[] kinds=new byte[4096];byte[] fleets,timed;bool pendingPick;internal int Selected=-1;internal Point TargetWorld;internal event Action<Point> Picked;
            internal TimedCanvas(Scenario scene){DoubleBuffered=true;BackColor=Color.FromArgb(8,19,30);for(int i=0;i<4096;i++)kinds[i]=scene.Cells[i].Kind;}
            RectangleF BoundsMap { get { float size=Math.Max(0,Math.Min(Width-24,Height-44));return new RectangleF((Width-size)/2,8,size,size); } }
            PointF Pixel(int x,int y){RectangleF b=BoundsMap;return new PointF(b.Left+x*b.Width/640f,b.Top+y*b.Height/640f);}
            internal void SetPools(byte[] f,byte[] t){fleets=f;timed=t;Invalidate();}
            internal void CancelPick(){pendingPick=false;if(!IsDisposed&&!Disposing)Capture=false;}
            protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left&&BoundsMap.Contains(e.Location)){pendingPick=true;Capture=true;}}
            protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)pendingPick=false;}
            protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);RectangleF b=BoundsMap;bool activate=pendingPick&&e.Button==MouseButtons.Left&&b.Width>0&&b.Contains(e.Location);CancelPick();if(activate&&Picked!=null)Picked(new Point(Math.Max(0,Math.Min(639,(int)((e.X-b.Left)*640/b.Width))),Math.Max(0,Math.Min(639,(int)((e.Y-b.Top)*640/b.Height)))));}
            protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);RectangleF b=BoundsMap;if(b.Width<1||fleets==null||timed==null)return;Graphics g=e.Graphics;float cell=b.Width/64;
                for(int y=0;y<64;y++)for(int x=0;x<64;x++){byte k=kinds[y*64+x];using(var brush=new SolidBrush(k==2?Color.FromArgb(123,145,91):k==4?Color.FromArgb(40,84,106):Color.FromArgb(24,55,77)))g.FillRectangle(brush,b.Left+x*cell,b.Top+y*cell,cell+.1f,cell+.1f);}
                var saved=g.Save();g.SetClip(b);
                for(int i=0;i<250;i++)if(BitConverter.ToInt32(fleets,i*492)!=0){PointF p=Pixel(BitConverter.ToInt16(fleets,i*492+12),BitConverter.ToInt16(fleets,i*492+14));int owner=BitConverter.ToInt16(fleets,i*492+6);g.FillEllipse(owner==0?Brushes.LightSteelBlue:Brushes.Orange,p.X-4,p.Y-4,8,8);g.DrawString((i+1).ToString(),Font,Brushes.WhiteSmoke,p.X+4,p.Y-8);}
                PointF target=Pixel(TargetWorld.X,TargetWorld.Y);g.DrawLine(Pens.Cyan,target.X-6,target.Y,target.X+6,target.Y);g.DrawLine(Pens.Cyan,target.X,target.Y-6,target.X,target.Y+6);
                for(int i=0;i<80;i++)if(BitConverter.ToInt32(timed,i*268)!=0){PointF p=Pixel(BitConverter.ToInt16(timed,i*268+8),BitConverter.ToInt16(timed,i*268+10));g.FillEllipse(Brushes.Gold,p.X-3,p.Y-3,6,6);if(i==Selected){g.DrawEllipse(Pens.Gold,p.X-7,p.Y-7,14,14);g.DrawString("#"+(i+1),Font,Brushes.Gold,p.X+7,p.Y+2);}}
                g.Restore(saved);g.DrawString("Schematic · blue/orange: fixed fleets · gold: timed objects · cyan: test target",Font,Brushes.LightSteelBlue,new RectangleF(8,Height-31,Math.Max(0,Width-16),30));
            }
        }
    }
}
