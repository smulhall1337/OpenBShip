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
        readonly ClassicMessages messages;
        PracticeTimedStudyState state;
        readonly ComboBox source = new ComboBox(), weapon = new ComboBox(), target = new ComboBox();
        readonly NumericUpDown offsetX = new NumericUpDown(), offsetY = new NumericUpDown();
        readonly ListBox objects = new ListBox();
        readonly ListBox impacts = new ListBox();
        readonly Label clock = new Label(), ammo = new Label(), status = new Label(), targetInfo = new Label();
        readonly Label impactSummary = new Label();
        readonly TextBox details = new TextBox();
        readonly Button create = new Button();
        readonly Button sourceLaunch = new Button();
        readonly CheckBox showFiringOrders = new CheckBox();
        readonly CheckBox secondaryQueue = new CheckBox();
        readonly Label queueRules = new Label();
        readonly FlowLayoutPanel firingPanel = new FlowLayoutPanel();
        readonly Label firingInfo = new Label();
        readonly TextBox firingQueue = new TextBox();
        readonly Button queueOrder = new Button(), clearOrders = new Button(), pollOrders = new Button(), findOrder = new Button();
        readonly CheckBox moveWithClock = new CheckBox();
        readonly FlowLayoutPanel movementPanel = new FlowLayoutPanel();
        readonly NumericUpDown waypointX = new NumericUpDown(), waypointY = new NumericUpDown();
        readonly Label movementInfo = new Label();
        readonly TextBox waypointQueue = new TextBox();
        readonly Button addWaypoint = new Button(), clearWaypoints = new Button(), deselectWaypoint = new Button();
        readonly Scenario scene;
        readonly PracticeWorldStudyState initialWorld;
        PracticeWorldStudyState world;
        readonly string movementProblem;
        readonly string firingImportProblem;
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
            PracticeUnitCatalog unitCatalog;PracticeDefenseTables math;PracticeDestructionTables destruction;byte[] visualData;
            try { catalog=PracticeTimedWeaponCatalog.Load(programs);unitCatalog=PracticeUnitCatalog.Load(programs);
                math=PracticeDefenseTables.Load(programs);destruction=PracticeDestructionTables.Load(programs);visualData=File.ReadAllBytes(Path.Combine(programs,"ClassicVisuals.seqbin")); }
            catch (IOException e) { throw new InvalidDataException("Run Setup again to import the timed-object, combat and destruction tables.",e); }
            this.messages=messages;
            this.scene=scene;
            initial = state = new PracticeTimedStudyState(catalog,unitCatalog,visualData,
                deployment.CopyShipPool(),deployment.CopyFleetPool(),0,0,math,destruction);
            try {
                initial=state=new PracticeTimedStudyState(catalog,unitCatalog,visualData,
                    deployment.CopyShipPool(),deployment.CopyFleetPool(),0,0,math,destruction,
                    PracticeFiringRangeTable.Load(programs),PracticeVisibility.Load(programs),PracticeFiringRateTable.Load(programs));
            }
            catch(InvalidDataException e){firingImportProblem=e.Message;}
            catch(IOException e){firingImportProblem=e.Message;}
            if(state.IncludesFiringOrders&&selected.Kind==1) {
                try{initialWorld=world=PracticeWorldStudyState.ForStudy(state,deployment,selected.FleetSlot);}
                catch(InvalidDataException e){movementProblem=e.Message;}
                catch(NotSupportedException e){movementProblem=e.Message;}
            }else movementProblem=state.IncludesFiringOrders?"Movement requires a player 1 kind-1 fleet.":"Run Setup again to import the firing tables.";
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
            add(clock,94);
            impactSummary.AccessibleName="Last impact and copied scores";add(impactSummary,76);
            moveWithClock.Text="Enable fleet movement with clock";moveWithClock.Name="timedMoveWithClock";moveWithClock.AccessibleName=moveWithClock.Text;
            moveWithClock.Enabled=world!=null;add(moveWithClock,36);
            if(world==null)add(new Label{Text=movementProblem,AccessibleName="Fleet movement prerequisite"},60);
            movementPanel.FlowDirection=FlowDirection.TopDown;movementPanel.WrapContents=false;movementPanel.AutoSize=true;
            movementPanel.AutoSizeMode=AutoSizeMode.GrowAndShrink;add(movementPanel,0);movementPanel.Visible=false;
            Action<Control,int> addMovement=delegate(Control c,int height){c.Width=270;c.Height=height;c.Margin=new Padding(0,2,0,2);movementPanel.Controls.Add(c);};
            movementInfo.AccessibleName="Fleet movement position and batch phase";addMovement(movementInfo,100);
            addMovement(new Label{Text="Waypoint world X/Y (0–639). Map clicks still choose firing offsets."},40);
            var waypointCoordinates=new FlowLayoutPanel{WrapContents=false};
            foreach(var numeric in new[]{waypointX,waypointY}){numeric.Minimum=0;numeric.Maximum=639;numeric.Width=100;waypointCoordinates.Controls.Add(numeric);}
            waypointX.AccessibleName="Movement waypoint world X";waypointY.AccessibleName="Movement waypoint world Y";addMovement(waypointCoordinates,32);
            addWaypoint.Text="Add / select waypoint";addWaypoint.Name="timedAddWaypoint";addMovement(addWaypoint,32);
            deselectWaypoint.Text="Deselect waypoint";deselectWaypoint.Name="timedDeselectWaypoint";addMovement(deselectWaypoint,32);
            clearWaypoints.Text="Clear movement queue";clearWaypoints.Name="timedClearWaypoints";addMovement(clearWaypoints,32);
            waypointQueue.Multiline=true;waypointQueue.ReadOnly=true;waypointQueue.ScrollBars=ScrollBars.Vertical;
            waypointQueue.BackColor=BackColor;waypointQueue.ForeColor=ForeColor;waypointQueue.AccessibleName="Five movement waypoint slots";addMovement(waypointQueue,100);
            var movementPolls=new Button{Text="32 polls (+1.28 s)",Name="timedMovementPolls"};addMovement(movementPolls,32);
            addMovement(new Label{Text="Two source batches per normal clock step. Unchecking freezes movement and keeps its queue/phase. Paused steps hold movement. No AI or missions."},84);
            add(new Label { Text="Source unit" },20); add(source,28);
            add(new Label { Text="Test weapon (original ammo data)" },20); add(weapon,28); add(ammo,38);
            showFiringOrders.Text="Show firing orders";showFiringOrders.AccessibleName="Show source firing order controls";add(showFiringOrders,28);
            add(new Label { Text="Target fleet" },20); add(target,28);
            add(new Label { Text="Target offset X/Y. Click the map to choose a world point relative to that fleet." },40);
            var offsets = new FlowLayoutPanel { WrapContents=false };
            foreach(var control in new[]{offsetX,offsetY}){control.Minimum=-639;control.Maximum=639;control.Width=100;}
            offsetX.AccessibleName="Timed target offset X";offsetY.AccessibleName="Timed target offset Y";
            offsets.Controls.Add(offsetX);offsets.Controls.Add(offsetY);add(offsets,32);
            add(targetInfo,78);
            firingPanel.FlowDirection=FlowDirection.TopDown;firingPanel.WrapContents=false;firingPanel.AutoSize=true;
            firingPanel.AutoSizeMode=AutoSizeMode.GrowAndShrink;add(firingPanel,0);firingPanel.Visible=false;
            var pickMiss = new Button { Text="Choose supported nonship cell",Name="timedPickMiss" };add(pickMiss,32);
            var pickShip = new Button { Text="Choose supported ship cell",Name="timedPickShip" };add(pickShip,32);
            create.Text="Create test object";add(create,32);
            sourceLaunch.Text="Create source launch pattern";sourceLaunch.Name="timedLaunch";add(sourceLaunch,32);
            add(new Label { Text=ClassicWeapons.Name(20)+" uses adjacent rows Y−1, Y, Y+1; other test weapons request one object. Expiry outside the 10 × 10 grid remains unsupported." },68);
            Action<Control,int> addFiring=delegate(Control c,int height){c.Width=270;c.Height=height;c.Margin=new Padding(0,2,0,2);firingPanel.Controls.Add(c);};
            firingInfo.AccessibleName="Source firing eligibility and queue totals";addFiring(firingInfo,140);
            secondaryQueue.Text="Use secondary request queue (study)";secondaryQueue.Name="timedSecondaryQueue";
            secondaryQueue.AccessibleName=secondaryQueue.Text;secondaryQueue.Enabled=world!=null;addFiring(secondaryQueue,40);
            findOrder.Text="Find admitted test shot";findOrder.Name="timedFindOrder";addFiring(findOrder,32);
            queueOrder.Text="Queue test order (raw)";queueOrder.Name="timedQueueOrder";addFiring(queueOrder,32);
            pollOrders.Text="Run firing poll (same clock)";pollOrders.Name="timedPollOrders";addFiring(pollOrders,32);
            clearOrders.Text="Clear this unit's orders";clearOrders.Name="timedClearOrders";addFiring(clearOrders,32);
            firingQueue.Multiline=true;firingQueue.ReadOnly=true;firingQueue.ScrollBars=ScrollBars.Vertical;
            firingQueue.BackColor=BackColor;firingQueue.ForeColor=ForeColor;firingQueue.AccessibleName="Selected unit raw firing queue";addFiring(firingQueue,132);
            queueRules.AccessibleName="Selected firing queue rules";addFiring(queueRules,132);
            var steps = new FlowLayoutPanel { WrapContents=false };
            var one = new Button { Text="+40 ms",Width=100,Height=30,Name="timedOne" };
            var second = new Button { Text="+1 s",Width=100,Height=30,Name="timedSecond" };steps.Controls.Add(one);steps.Controls.Add(second);add(steps,36);
            var boundary = new Button { Text="Advance to expiry boundary",Name="timedBoundary" };add(boundary,32);
            var paused = new Button { Text="Advance paused +1 s",Name="timedPaused" };add(paused,32);
            add(new Label { Text="Active projectiles" },20);
            objects.IntegralHeight=false;objects.BackColor=BackColor;objects.ForeColor=ForeColor;objects.AccessibleName="Active timed objects";add(objects,80);
            add(new Label { Text="Expiry effects (schematic)" },20);
            impacts.IntegralHeight=false;impacts.BackColor=BackColor;impacts.ForeColor=ForeColor;impacts.AccessibleName="Active expiry effects";add(impacts,72);
            details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;details.AccessibleName="Selected timed object data";add(details,128);
            var reset = new Button { Text="Reset study",Name="timedReset" };add(reset,32);
            var close = new Button { Text="Close",DialogResult=DialogResult.Cancel };add(close,30);CancelButton=close;
            // Account for the vertical scrollbar at each window size. Fixed
            // child widths otherwise create a second scrollbar and clip text.
            Action fitSide = delegate {
                int available = Math.Max(180,scroll.ClientSize.Width-side.Padding.Horizontal-8);
                side.MaximumSize = new Size(Math.Max(1,scroll.ClientSize.Width),0);
                foreach(Control child in side.Controls)child.Width=available;
                firingPanel.MaximumSize=new Size(available,0);
                foreach(Control child in firingPanel.Controls)child.Width=Math.Max(160,available-6);
                movementPanel.MaximumSize=new Size(available,0);
                foreach(Control child in movementPanel.Controls)child.Width=Math.Max(160,available-6);
            };
            scroll.SizeChanged += delegate { fitSide(); }; fitSide();
            showFiringOrders.CheckedChanged+=delegate {
                firingPanel.Visible=showFiringOrders.Checked;
                // Creating a formerly hidden child handle can append it to a
                // FlowLayoutPanel. Restore its position after target details.
                side.Controls.SetChildIndex(firingPanel,side.Controls.GetChildIndex(targetInfo)+1);
                fitSide();side.PerformLayout();if(firingPanel.Visible)scroll.ScrollControlIntoView(firingPanel);
            };
            moveWithClock.CheckedChanged+=delegate {
                movementPanel.Visible=moveWithClock.Checked;
                side.Controls.SetChildIndex(movementPanel,side.Controls.GetChildIndex(moveWithClock)+1);
                fitSide();side.PerformLayout();UpdateMovementControls();canvas.Invalidate();
                SetStatus(moveWithClock.Checked?"Movement enabled: normal clock steps run two fleet batches before projectile and firing updates.":"Movement frozen; queued waypoints and batch phase are preserved. Projectiles and firing orders still advance.",false);
                if(movementPanel.Visible)scroll.ScrollControlIntoView(movementPanel);
            };
            addWaypoint.Click+=delegate { EditWaypoints(0); };
            deselectWaypoint.Click+=delegate { EditWaypoints(1); };
            clearWaypoints.Click+=delegate { EditWaypoints(2); };
            movementPolls.Click+=delegate { AdvanceFrames(32,false); };
            queueOrder.Click+=delegate { QueueTestOrder(); };
            secondaryQueue.CheckedChanged+=delegate { UpdateSelection(); };
            pollOrders.Click+=delegate {try{
                if(secondaryQueue.Checked&&world!=null){var before=world;world=world.WithCombat(state).ProcessSecondaryOrders();state=world.Combat;RefreshState();SetStatus("Secondary poll: consumed "+(world.SecondaryConsumedOrders-before.SecondaryConsumedOrders)+", rejected "+(world.SecondaryRejectedOrders-before.SecondaryRejectedOrders)+", created "+(world.SecondaryCreatedObjects-before.SecondaryCreatedObjects)+". A timer equal to the clock waits.",false);}
                else{state=state.ProcessOrders();RefreshState();SetStatus("Firing poll: consumed "+state.LastOrdersConsumed+", rejected "+state.LastOrdersRejected+", created "+state.LastOrdersCreated+". A timer equal to the clock waits; use +40 ms to advance.",false);}
            }catch(NotSupportedException e){SetStatus(e.Message,true);}catch(InvalidDataException e){SetStatus(e.Message,true);}};
            clearOrders.Click+=delegate {if(SourceSlot<0)return;if(secondaryQueue.Checked&&world!=null){world=world.WithCombat(state).ClearSecondaryOrders(SourceSlot);state=world.Combat;}else state=state.ClearOrders(SourceSlot);RefreshState();SetStatus("Cleared this unit's selected-queue X sentinels; its timer, Y values and existing projectiles are preserved.",false);};
            findOrder.Click+=delegate { FindAdmittedOrder(); };
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
            objects.SelectedIndexChanged += delegate { if(!changing){if(objects.SelectedIndex>=0){changing=true;impacts.SelectedIndex=-1;changing=false;}UpdateSelection();} };
            impacts.SelectedIndexChanged += delegate { if(!changing){if(impacts.SelectedIndex>=0){changing=true;objects.SelectedIndex=-1;changing=false;}UpdateSelection();} };
            pickMiss.Click += delegate { PickNonshipCell(); };
            pickShip.Click += delegate { PickShipCell(); };
            create.Click += delegate { CreateObject(); };one.Click += delegate { AdvanceFrames(1,false); };second.Click += delegate { AdvanceFrames(25,false); };
            sourceLaunch.Click += delegate { CreateObject(true); };
            paused.Click += delegate { AdvanceFrames(25,true); };boundary.Click += delegate { AdvanceToBoundary(); };
            reset.Click += delegate { state=initial;world=initialWorld;RefreshState();SetStatus("Reset to the copied startup pools, movement phase and clock0.",false); };
            KeyUp += delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate += delegate { held.Clear();canvas.CancelPick(); };
            if(source.Items.Count>0)source.SelectedIndex=Math.Max(0,Math.Min(source.Items.Count-1,selectedUnit));
            if(target.Items.Count>0)target.SelectedIndex=0;RefreshState();SetStatus(firingImportProblem==null?
                "Create an object, or expand Show firing orders for queued shots. Closing preserves the active battle and deployment.":
                "Firing orders unavailable: run Setup again to import the three firing tables. Manual test creation remains available.",false);
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
            if(SourceSlot>=0)for(int id=14;id<=30;id++){int count=BitConverter.ToInt16(ships,SourceSlot*268+196+id*2);int index=weapon.Items.Add(new Choice { Id=id,Text=ClassicWeapons.Name(id)+" · "+(count==-1?"infinite":count.ToString()) });if(id==previous)chosen=index;else if(chosen<0&&(count==-1||count>=1))chosen=index;}
            if(weapon.Items.Count>0)weapon.SelectedIndex=chosen<0?0:chosen;changing=false;UpdateSelection();
        }
        void SetStatus(string message,bool boundary) { status.ForeColor=boundary?Color.Gold:Color.LightSteelBlue;status.Text=message+"\r\nManual study: 40 ms steps, fixture seed0, "+(moveWithClock.Checked?"movement enabled":"movement frozen")+". Manual creation bypasses admission; queued orders use source visibility/ammo/mount/range checks. No live firing media. Missions/endings, attached objects and special expiry branches remain unsupported."; }
        void EditWaypoints(int action) {
            if(world==null||!moveWithClock.Checked)return;
            try {
                var next=world.WithCombat(state);string result;
                if(action==1){next=next.DeselectWaypoint(scene);result="Waypoint deselected; another point can be appended.";}
                else if(action==2){next=next.ClearWaypoints(scene);result="Movement queue cleared; position and batch phase preserved.";}
                else{PracticeWaypointResult outcome;next=next.AppendOrSelectWaypoint(scene,new Point((int)waypointX.Value,(int)waypointY.Value),out outcome);result="Waypoint: "+outcome+". Coordinates snap to multiples of ten.";}
                world=next;state=next.Combat;RefreshState();SetStatus(result,false);
            }catch(NotSupportedException e){SetStatus(e.Message,true);}catch(InvalidDataException e){SetStatus(e.Message,true);}
        }
        void UpdateMovementControls() {
            canvas.MovementFleet=moveWithClock.Checked&&world!=null?world.SelectedFleet:-1;
            if(world==null){moveWithClock.Text="Fleet movement unavailable";moveWithClock.AccessibleDescription=movementProblem;return;}
            byte[] fleets=state.Objects.CopyFleetPool();int at=world.SelectedFleet*492;
            bool live=BitConverter.ToInt32(fleets,at)==1&&BitConverter.ToInt16(fleets,at+6)==0;
            addWaypoint.Enabled=clearWaypoints.Enabled=deselectWaypoint.Enabled=live;
            movementInfo.Text="Fleet "+(world.SelectedFleet+1)+" · world "+BitConverter.ToInt16(fleets,at+12)+", "+BitConverter.ToInt16(fleets,at+14)+"\r\nNext batch "+world.Schedule.BatchStart+" · cycles "+world.Schedule.CompletedCycles+"\r\nMoving updates "+world.MovedFleetUpdates+"\r\n"+(live?(world.LastSelectedOutcome.HasValue?world.LastSelectedOutcome.Value.ToString():"Fleet waits for its batch visit."):"Fleet inactive; Reset restores it.");
            string rows="";for(int n=0;n<5;n++){int x=BitConverter.ToInt16(fleets,at+40+n*2);rows+=(n+1)+": "+(x==-1?"empty":x+", "+BitConverter.ToInt16(fleets,at+50+n*2))+(n==world.SelectedWaypoint?" · selected":"")+"\r\n";}waypointQueue.Text=rows;
        }
        static int UnitAt(byte[] ships,byte[] fleets,int fleet,int x,int y) {
            for(int slot=0;slot<6;slot++){
                uint reference=BitConverter.ToUInt32(fleets,fleet*492+16+slot*4);long delta=(long)reference-0x4fc340;
                if(reference==0||delta<0||delta>=402000||delta%268!=0)continue;int at=(int)delta;
                if(BitConverter.ToInt32(ships,at)==0||BitConverter.ToInt16(ships,at+8)!=x)continue;
                int start=BitConverter.ToInt16(ships,at+10),length=BitConverter.ToInt16(ships,at+14);
                if(y>=start&&y<start+length)return at/268;
            }
            return -1;
        }
        string UnitName(byte[] ships,int unit) {
            int type=BitConverter.ToInt32(ships,unit*268);
            return messages!=null&&type>=0&&type<13?messages.ShipName(type):"type "+type;
        }
        void PickShipCell() {
            if(target.SelectedItem==null)return;int fleet=((Choice)target.SelectedItem).Id;
            byte[] f=state.Objects.CopyFleetPool(),ships=state.Objects.CopyShipPool();
            if(BitConverter.ToInt32(f,fleet*492)==0){SetStatus("This target fleet is inactive. Choose another fleet or reset the study.",true);return;}
            for(int x=0;x<10;x++)for(int y=0;y<10;y++){
                int code=BitConverter.ToInt32(f,fleet*492+64+(x*10+y)*4);
                if(code!=2&&code!=4&&code!=5&&code!=6&&code!=7)continue;
                int unit=UnitAt(ships,f,fleet,x,y);if(unit<0||BitConverter.ToInt16(ships,unit*268+16)<1)continue;
                changing=true;offsetX.Value=x;offsetY.Value=y;changing=false;UpdateSelection();
                SetStatus("Selected existing ship cell "+x+","+y+" on unit "+(unit+1)+". Original defense may intercept or scatter the shot.",false);return;
            }
            SetStatus("No supported live ship cell remains in this fleet. Choose another target or reset.",true);
        }
        string ImpactText() {
            if(!state.LastImpactOutcome.HasValue)return "No impact resolved yet.";
            switch(state.LastImpactOutcome.Value){
                case PracticeImpactOutcome.NonshipCell:return "Nonship impact resolved.";
                case PracticeImpactOutcome.PrimaryStopped:return "Primary defense stopped the shot; no damage.";
                case PracticeImpactOutcome.ScatteredMiss:return "Shot missed the ship; nearby effect after "+state.LastScatterAttempts+" scatter attempt(s).";
                case PracticeImpactOutcome.PassiveCell:return "Cell8: projectile retired without new damage.";
                case PracticeImpactOutcome.NoVictim:return "No matching unit; source retains the active projectile.";
                default:byte[] ships=state.Objects.CopyShipPool();return "Unit "+(state.LastVictimShip+1)+(state.LastVictimDestroyed?" destroyed":" damaged; HP "+BitConverter.ToInt16(ships,state.LastVictimShip*268+16))+". Source point value "+state.LastImpactPoints+".";
            }
        }
        void PickNonshipCell() {
            if(target.SelectedItem==null)return;int fleet=((Choice)target.SelectedItem).Id;byte[] f=state.Objects.CopyFleetPool();
            if(BitConverter.ToInt32(f,fleet*492)==0){SetStatus("This target fleet is inactive. Choose another fleet or reset the study.",true);return;}
            for(int x=0;x<10;x++)for(int y=0;y<10;y++){int code=BitConverter.ToInt32(f,fleet*492+64+(x*10+y)*4);if(code!=0&&code!=1&&code!=3)continue;
                changing=true;offsetX.Value=x;offsetY.Value=y;changing=false;UpdateSelection();SetStatus("Selected existing cell "+x+","+y+" with original code "+code+" for the supported nonship expiry branch.",false);return;}
            SetStatus("This fleet has no supported nonship cell. Choose another fixed target.",true);
        }
        void QueueTestOrder() {
            if(!queueOrder.Enabled)return;
            try {
                int code,slot;
                if(secondaryQueue.Checked&&world!=null){world=world.WithCombat(state).QueueSecondaryOrder(WeaponId,SourceSlot,((Choice)target.SelectedItem).Id,(int)offsetX.Value,(int)offsetY.Value);state=world.Combat;code=world.LastSecondaryReturnCode;slot=world.LastSecondarySlot;}
                else{state=state.QueueOrder(WeaponId,SourceSlot,((Choice)target.SelectedItem).Id,(int)offsetX.Value,(int)offsetY.Value);code=state.LastQueueReturnCode;slot=state.LastQueueSlot;}
                RefreshState();SetStatus(code==1?"The selected unit's "+(secondaryQueue.Checked?"five":"ten")+"-slot queue is full.":
                    (int)offsetX.Value==-1?"Stored X=−1: this is an empty sentinel, so no shot became active.":
                    "Stored request in slot "+(slot+1)+". Admission is checked when due. Use +40 ms or +1 s; a timer equal to the clock waits.",false);
            }
            catch(NotSupportedException e){SetStatus(e.Message,true);}catch(InvalidDataException e){SetStatus(e.Message,true);}
        }
        static List<int> PreferredIndices(ComboBox choices) {
            var result=new List<int>();if(choices.SelectedIndex>=0)result.Add(choices.SelectedIndex);
            for(int n=0;n<choices.Items.Count;n++)if(n!=choices.SelectedIndex)result.Add(n);return result;
        }
        void FindAdmittedOrder() {
            if(!state.IncludesFiringOrders)return;
            var candidates=new List<int>();if(WeaponId>=14&&WeaponId<=30)candidates.Add(WeaponId);
            for(int id=14;id<=30;id++)if(id!=WeaponId)candidates.Add(id);
            try {
                foreach(int sourceIndex in PreferredIndices(source))foreach(int targetIndex in PreferredIndices(target))foreach(int id in candidates) {
                    if(id==19||id==21)continue; // Their expiry remains unsupported.
                    int unit=((Choice)source.Items[sourceIndex]).Id,fleet=((Choice)target.Items[targetIndex]).Id;
                    if(!state.CheckFiring(unit,fleet,id).Allowed)continue;
                    Point? safe=NonshipPattern(state.Objects.CopyFleetPool(),fleet,id);if(!safe.HasValue)continue;
                    source.SelectedIndex=sourceIndex;target.SelectedIndex=targetIndex;
                    for(int n=0;n<weapon.Items.Count;n++)if(((Choice)weapon.Items[n]).Id==id){weapon.SelectedIndex=n;break;}
                    changing=true;offsetX.Value=safe.Value.X;offsetY.Value=safe.Value.Y;changing=false;UpdateSelection();
                    SetStatus("Selected an admitted shot with supported nonship launch offsets. Fleet positions and pools are unchanged. Queue it, then advance the clock.",false);return;
                }
                SetStatus("No admitted supported shot exists for the source units and enemy fleets in this fixed preview. The eligibility readout explains the current selection; manual test creation remains available.",true);
            }
            catch(NotSupportedException e){SetStatus(e.Message,true);}catch(InvalidDataException e){SetStatus(e.Message,true);}
        }
        static Point? NonshipPattern(byte[] fleets,int fleet,int weapon) {
            for(int x=0;x<10;x++)for(int y=0;y<10;y++) {
                bool supported=true;int radius=weapon==20?1:0;
                for(int row=y-radius;row<=y+radius;row++){if(row<0||row>=10){supported=false;break;}int code=BitConverter.ToInt32(fleets,fleet*492+64+(x*10+row)*4);if(code!=0&&code!=1&&code!=3){supported=false;break;}}
                if(supported)return new Point(x,y);
            }
            return null;
        }
        void UpdateFiringControls(byte[] ships,byte[] fleets) {
            bool available=state.IncludesFiringOrders;showFiringOrders.Enabled=available;
            byte[] primaryOrders=state.PauseTimers.CopyShipTimers(),orders=secondaryQueue.Checked&&world!=null?world.CopySecondaryOrders():primaryOrders;int pending=world==null?0:world.PendingSecondaryUnits;
            for(int unit=0;unit<1500;unit++)if(BitConverter.ToInt16(primaryOrders,unit*52+8)!=-1)pending++;
            showFiringOrders.Text=available?"Show firing orders ("+pending+" pending units)":"Firing orders unavailable: run Setup";
            showFiringOrders.AccessibleDescription=available?"Shows controls; hiding them keeps queued orders active.":firingImportProblem??"Reset this study to restore firing rules.";
            bool chosen=available&&SourceSlot>=0&&WeaponId>=14&&target.SelectedItem!=null;
            queueOrder.Enabled=chosen&&BitConverter.ToInt32(ships,SourceSlot*268)!=0;
            clearOrders.Enabled=available&&SourceSlot>=0;pollOrders.Enabled=findOrder.Enabled=available;
            secondaryQueue.Enabled=world!=null&&available;
            queueRules.Text=secondaryQueue.Checked?"Secondary: five enqueue slots; shifts ten. Runs before impacts. Paused clock steps keep its deadlines unchanged, so requests can become due on resume. Kind/target changes retarget existing entries. Hiding keeps both queues active.":"Primary: ten slots; runs after impacts. Paused steps shift its deadlines. A timer equal to the clock waits. Hiding keeps both queues active. Raw X=−1 is the empty sentinel.";
            firingInfo.Text=available?"Choose source, weapon and target.":"Run Setup again to import the three firing tables.";
            if(chosen) {
                try {
                    var check=state.CheckFiring(SourceSlot,((Choice)target.SelectedItem).Id,WeaponId);
                    string reason=check.Allowed?"allowed":check.Denial==PracticeFiringDenial.InactiveSource?"source inactive":check.Denial==PracticeFiringDenial.InactiveTarget?"target inactive":check.Denial==PracticeFiringDenial.HiddenTarget?"target hidden":check.Denial==PracticeFiringDenial.NoAmmunition?"no ammunition":check.Denial==PracticeFiringDenial.NoMount?"weapon not mounted":"out of range";
                    firingInfo.Text="Unit "+(SourceSlot+1)+" · "+ClassicWeapons.Name(WeaponId)+"\r\nTarget fleet "+(((Choice)target.SelectedItem).Id+1)+"\r\nAdmission: "+reason+" · visibility "+check.VisibilityLevel;
                    if(check.Distance>=0)firingInfo.Text+="\r\nFleet distance "+check.Distance+" / range "+check.Range;
                }
                catch(NotSupportedException e){firingInfo.Text="Unsupported admission: "+e.Message;}
                catch(InvalidDataException e){firingInfo.Text=e.Message;}
            }
            if(secondaryQueue.Checked&&world!=null)firingInfo.Text+="\r\nSecondary consumed "+world.SecondaryConsumedOrders+" · rejected "+world.SecondaryRejectedOrders+"\r\nObjects from secondary: "+world.SecondaryCreatedObjects;
            else firingInfo.Text+="\r\nPolls "+state.FiringPolls+" · consumed "+state.ConsumedOrders+" · rejected "+state.RejectedOrders+"\r\nObjects from orders: "+state.ObjectsFromOrders;
            if(SourceSlot<0){firingQueue.Text="No source unit selected.";return;}
            int at=SourceSlot*52;var text=new System.Text.StringBuilder();
            text.AppendLine("Timer "+BitConverter.ToInt32(orders,at+4)+" · clock "+unchecked((int)state.ClockMilliseconds));
            text.AppendLine("Raw order kind "+BitConverter.ToInt32(orders,at));
            for(int n=0;n<10;n++){int x=BitConverter.ToInt16(orders,at+8+n*2),y=BitConverter.ToInt16(orders,at+28+n*2);text.AppendLine((n+1)+": "+(x==-1?"empty (stored Y="+y+")":x+", "+y));}
            firingQueue.Text=text.ToString();
        }
        void CreateObject(bool useSourcePattern=false) {
            if(!create.Enabled)return;
            try { state=useSourcePattern?state.Launch(WeaponId,SourceSlot,((Choice)target.SelectedItem).Id,(int)offsetX.Value,(int)offsetY.Value):state.Create(WeaponId,SourceSlot,((Choice)target.SelectedItem).Id,(int)offsetX.Value,(int)offsetY.Value);int created=state.LastCreatedSlot;RefreshState();if(created>=0)for(int i=0;i<objects.Items.Count;i++)if(((Choice)objects.Items[i]).Id==created){objects.SelectedIndex=i;break;}SetStatus(useSourcePattern?"Source launch: created "+state.LastLaunchCount+" / "+state.LastLaunchAttempts+" requested objects; last-round updates "+state.LastLaunchDepletions+".":state.LastCreationOutcome==PracticeTimedCreationOutcome.Created?"Created test object "+(created+1)+".":state.LastCreationOutcome==PracticeTimedCreationOutcome.PoolFull?"The 80-record pool is full. Reset to clear it.":"This unit has no ammunition for that weapon.",false); }
            catch(NotSupportedException e){SetStatus(e.Message,true);}catch(InvalidDataException e){SetStatus(e.Message,true);}
        }
        void AdvanceToBoundary() { if(objects.Items.Count==0){SetStatus("Create an object before advancing to its expiry boundary.",false);return;}AdvanceFrames(5000,false,true); }
        void AdvanceFrames(int count,bool paused,bool stopAtExpiry=false) {
            int completed=0,expired=state.ExpiryCallbacks;string limit=null;
            for(;completed<count;completed++)try{
                if(world!=null&&(moveWithClock.Checked||world.PendingSecondaryUnits>0)){var next=world.WithCombat(state).AdvanceFrame(paused,moveWithClock.Checked);world=next;state=next.Combat;}
                else state=state.AdvanceFrame(paused);
                if(stopAtExpiry&&state.ExpiryCallbacks>expired){completed++;break;}
            }catch(NotSupportedException e){limit="Stopped before unsupported update: "+e.Message;break;}catch(InvalidDataException e){limit=e.Message;break;}
            RefreshState();SetStatus("Advanced "+completed+" × 40 ms"+(paused?" paused: primary/object deadlines shifted; secondary deadlines unchanged.":".")+(limit==null?(state.ExpiryCallbacks>expired?" "+ImpactText():stopAtExpiry&&completed==count?" Reached the 5000-step guard; advance again to continue.":""):" "+limit),limit!=null);
        }
        void RefreshState() {
            int selected=SelectedObject,effect=impacts.SelectedItem==null?-1:((Choice)impacts.SelectedItem).Id;changing=true;objects.Items.Clear();impacts.Items.Clear();byte[] timed=state.Objects.CopyTimedPool(),visuals=state.CopyEffectPool();
            for(int i=0;i<80;i++)if(BitConverter.ToInt32(timed,i*268)!=0){int index=objects.Items.Add(new Choice { Id=i,Text="#"+(i+1)+" · "+BitConverter.ToInt16(timed,i*268+8)+","+BitConverter.ToInt16(timed,i*268+10) });if(i==selected)objects.SelectedIndex=index;}
            for(int i=0;i<40;i++)if(BitConverter.ToInt16(visuals,i*20)!=-1){int index=impacts.Items.Add(new Choice { Id=i,Text="Effect "+(i+1)+" · type "+BitConverter.ToInt32(visuals,i*20+16)+" · counter "+BitConverter.ToInt16(visuals,i*20+12) });if(i==effect)impacts.SelectedIndex=index;}
            if(objects.SelectedIndex<0&&objects.Items.Count>0&&impacts.SelectedIndex<0)objects.SelectedIndex=0;
            if(objects.SelectedIndex<0&&impacts.SelectedIndex<0&&impacts.Items.Count>0)impacts.SelectedIndex=0;changing=false;
            clock.Text="Clock "+state.ClockMilliseconds+" ms · "+objects.Items.Count+"/80 objects · "+state.ActiveEffects+"/40 effects\r\nUpdates "+state.MovementFrames+" · paused "+state.PausedFrames+" · expired "+state.ExpiredObjects+"\r\nFixture RNG "+state.RandomState+" · calls "+state.RandomCalls+" · dropped effects "+state.DroppedEffects+"\r\nDestroyed units "+state.DestroyedUnits+" · movie-selection counter "+state.MovieCounter;
            clock.Text+="\r\nLast source launch "+state.LastLaunchCount+" / "+state.LastLaunchAttempts+" objects · last-round updates "+state.LastLaunchDepletions;
            byte[] score=state.CopyScoreRegion();impactSummary.Text=ImpactText()+"\r\nCopied scores: P1 "+BitConverter.ToInt32(score,0)+" · P2 "+BitConverter.ToInt32(score,44);
            byte[] targetFleets=state.Objects.CopyFleetPool();foreach(Choice choice in target.Items){int at=choice.Id*492;choice.Text="Fleet "+(choice.Id+1)+" · player "+(BitConverter.ToInt16(targetFleets,at+6)+1)+" · "+BitConverter.ToInt16(targetFleets,at+12)+","+BitConverter.ToInt16(targetFleets,at+14);}target.Refresh();
            canvas.SetPools(state.Objects.CopyFleetPool(),timed,visuals);LoadWeapons();UpdateSelection();
        }
        void UpdateSelection() {
            if(changing)return;byte[] currentShips=state.Objects.CopyShipPool(),currentFleets=state.Objects.CopyFleetPool();int count=0;if(SourceSlot>=0&&WeaponId>=14)count=BitConverter.ToInt16(currentShips,SourceSlot*268+196+WeaponId*2);
            bool sourceLive=SourceSlot>=0&&BitConverter.ToInt32(currentShips,SourceSlot*268)!=0,targetLive=target.SelectedItem!=null&&BitConverter.ToInt32(currentFleets,((Choice)target.SelectedItem).Id*492)!=0;
            create.Enabled=sourceLive&&WeaponId>=14&&targetLive&&(count==-1||count>=1);
            sourceLaunch.Enabled=create.Enabled;
            ammo.Text=!sourceLive?"Source unit was destroyed. Reset the study to restore it.":!targetLive?"Target fleet is inactive. Choose another fleet or reset.":count==-1?"Infinite ammunition.":count==1?"Last round: includes order reset and equipment reevaluation.":count==0?"No ammunition for this weapon.":"Ammunition: "+count;
            int slot=SelectedObject;canvas.Selected=slot;
            if(target.SelectedItem!=null){int id=((Choice)target.SelectedItem).Id,x=(int)offsetX.Value,y=(int)offsetY.Value;byte[] fleet=state.Objects.CopyFleetPool();canvas.TargetWorld=new Point(BitConverter.ToInt16(fleet,id*492+12)+x,BitConverter.ToInt16(fleet,id*492+14)+y);
                targetInfo.Text=x<0||x>=10||y<0||y>=10?"Offset is outside the 10 × 10 fleet grid. Impact will stop.":"Target cell code "+BitConverter.ToInt32(fleet,id*492+64+(x*10+y)*4)+" at "+x+","+y+".";
                byte[] ships=state.Objects.CopyShipPool();int victim=UnitAt(ships,fleet,id,x,y);
                if(victim>=0){int at=victim*268,row=y-BitConverter.ToInt16(ships,at+10),equipped=0;for(int column=0;column<4;column++)if(BitConverter.ToInt32(ships,at+100+row*16+column*4)!=0)equipped++;targetInfo.Text+="\r\nUnit "+(victim+1)+" · "+UnitName(ships,victim)+" · HP "+BitConverter.ToInt16(ships,at+16)+" · row equipment "+equipped+"/4.";}
                else targetInfo.Text+="\r\nNonship expiry codes: 0, 1, 3.";
                if(WeaponId==19||WeaponId==21)targetInfo.Text="This test weapon's special expiry branch is unsupported.";}
            if(!targetLive)targetInfo.Text="Target fleet inactive; creation disabled.\r\nStale ship cells can keep shots active.\r\nReset restores the fleet.";
            if(slot<0&&impacts.SelectedItem!=null){byte[] e=state.CopyEffectPool();int at=((Choice)impacts.SelectedItem).Id*20;details.Text="Schematic effect "+(((Choice)impacts.SelectedItem).Id+1)+" · type "+BitConverter.ToInt32(e,at+16)+"\r\nFleet reference "+(BitConverter.ToInt16(e,at)+1)+" · local "+BitConverter.ToInt16(e,at+2)+", "+BitConverter.ToInt16(e,at+4)+"\r\nSprite bank "+BitConverter.ToInt32(e,at+8)+" · counter "+BitConverter.ToInt16(e,at+12)+"\r\nRing geometry is a modern schematic, not the original sprite renderer.";}
            else if(slot<0)details.Text="No active timed object or expiry effect.\r\nThis study is isolated from the active Classic battle.";
            else { byte[] t=state.Objects.CopyTimedPool();int at=slot*268;uint deadline=BitConverter.ToUInt32(t,at+24);details.Text="Object "+(slot+1)+" · weapon "+BitConverter.ToInt32(t,at)+"\r\nWorld "+BitConverter.ToInt16(t,at+8)+", "+BitConverter.ToInt16(t,at+10)+"\r\nSource unit "+(BitConverter.ToInt16(t,at+16)+1)+" · target fleet "+(BitConverter.ToInt16(t,at+12)+1)+"\r\nDeadline "+deadline+" ms\r\nRemaining "+unchecked((int)(deadline-state.ClockMilliseconds))+" ms\r\nLast early return: "+(state.LastStoppedAtRecord<0?"none":"object "+(state.LastStoppedAtRecord+1)); }
            UpdateFiringControls(currentShips,currentFleets);
            UpdateMovementControls();
            canvas.Invalidate();
        }
        sealed class TimedCanvas : Control {
            readonly byte[] kinds=new byte[4096];byte[] fleets,timed,effects;bool pendingPick;internal int Selected=-1,MovementFleet=-1;internal Point TargetWorld;internal event Action<Point> Picked;
            internal TimedCanvas(Scenario scene){DoubleBuffered=true;BackColor=Color.FromArgb(8,19,30);for(int i=0;i<4096;i++)kinds[i]=scene.Cells[i].Kind;}
            RectangleF BoundsMap { get { float size=Math.Max(0,Math.Min(Width-24,Height-44));return new RectangleF((Width-size)/2,8,size,size); } }
            PointF Pixel(int x,int y){RectangleF b=BoundsMap;return new PointF(b.Left+x*b.Width/640f,b.Top+y*b.Height/640f);}
            internal void SetPools(byte[] f,byte[] t,byte[] v){fleets=f;timed=t;effects=v;Invalidate();}
            internal void CancelPick(){pendingPick=false;if(!IsDisposed&&!Disposing)Capture=false;}
            protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left&&BoundsMap.Contains(e.Location)){pendingPick=true;Capture=true;}}
            protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)pendingPick=false;}
            protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);RectangleF b=BoundsMap;bool activate=pendingPick&&e.Button==MouseButtons.Left&&b.Width>0&&b.Contains(e.Location);CancelPick();if(activate&&Picked!=null)Picked(new Point(Math.Max(0,Math.Min(639,(int)((e.X-b.Left)*640/b.Width))),Math.Max(0,Math.Min(639,(int)((e.Y-b.Top)*640/b.Height)))));}
            protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);RectangleF b=BoundsMap;if(b.Width<1||fleets==null||timed==null)return;Graphics g=e.Graphics;float cell=b.Width/64;
                for(int y=0;y<64;y++)for(int x=0;x<64;x++){byte k=kinds[y*64+x];using(var brush=new SolidBrush(k==2?Color.FromArgb(123,145,91):k==4?Color.FromArgb(40,84,106):Color.FromArgb(24,55,77)))g.FillRectangle(brush,b.Left+x*cell,b.Top+y*cell,cell+.1f,cell+.1f);}
                var saved=g.Save();g.SetClip(b);
                for(int i=0;i<250;i++)if(BitConverter.ToInt32(fleets,i*492)!=0){PointF p=Pixel(BitConverter.ToInt16(fleets,i*492+12),BitConverter.ToInt16(fleets,i*492+14));int owner=BitConverter.ToInt16(fleets,i*492+6);g.FillEllipse(owner==0?Brushes.LightSteelBlue:Brushes.Orange,p.X-4,p.Y-4,8,8);g.DrawString((i+1).ToString(),Font,Brushes.WhiteSmoke,p.X+4,p.Y-8);}
                if(MovementFleet>=0&&BitConverter.ToInt32(fleets,MovementFleet*492)!=0){int at=MovementFleet*492;PointF last=Pixel(BitConverter.ToInt16(fleets,at+12),BitConverter.ToInt16(fleets,at+14));g.DrawEllipse(Pens.Gold,last.X-7,last.Y-7,14,14);using(var pen=new Pen(Color.Gold)){pen.DashStyle=System.Drawing.Drawing2D.DashStyle.Dash;for(int n=0;n<5;n++){int x=BitConverter.ToInt16(fleets,at+40+n*2);if(x==-1)continue;PointF p=Pixel(x,BitConverter.ToInt16(fleets,at+50+n*2));g.DrawLine(pen,last,p);g.DrawRectangle(Pens.Gold,p.X-3,p.Y-3,6,6);g.DrawString("W"+(n+1),Font,Brushes.Gold,p.X+4,p.Y+3);last=p;}}}
                PointF target=Pixel(TargetWorld.X,TargetWorld.Y);g.DrawLine(Pens.Cyan,target.X-6,target.Y,target.X+6,target.Y);g.DrawLine(Pens.Cyan,target.X,target.Y-6,target.X,target.Y+6);
                for(int i=0;i<80;i++)if(BitConverter.ToInt32(timed,i*268)!=0){PointF p=Pixel(BitConverter.ToInt16(timed,i*268+8),BitConverter.ToInt16(timed,i*268+10));g.FillEllipse(Brushes.Gold,p.X-3,p.Y-3,6,6);if(i==Selected){g.DrawEllipse(Pens.Gold,p.X-7,p.Y-7,14,14);g.DrawString("#"+(i+1),Font,Brushes.Gold,p.X+7,p.Y+2);}}
                if(effects!=null)for(int i=0;i<40;i++){int at=i*20,fleet=BitConverter.ToInt16(effects,at);if(fleet<0||fleet>=250)continue;PointF p=Pixel(BitConverter.ToInt16(fleets,fleet*492+12)+BitConverter.ToInt16(effects,at+2),BitConverter.ToInt16(fleets,fleet*492+14)+BitConverter.ToInt16(effects,at+4));float radius=Math.Max(3,Math.Min(20,3+BitConverter.ToInt16(effects,at+12)*.35f));g.DrawEllipse(Pens.LightSkyBlue,p.X-radius,p.Y-radius,radius*2,radius*2);g.DrawString("E"+(i+1),Font,Brushes.LightSkyBlue,p.X+radius,p.Y+radius);}
                g.Restore(saved);g.DrawString("Schematic · blue/orange: fleets · gold: projectiles · blue rings: effects · cyan: target",Font,Brushes.LightSteelBlue,new RectangleF(8,Height-31,Math.Max(0,Width-16),30));
            }
        }
    }
}
