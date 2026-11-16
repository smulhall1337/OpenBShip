using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace FleetLab
{
    public sealed class PracticeMapStartupStudy : Form
    {
        readonly string programs;
        readonly byte[] source;
        readonly NumericUpDown seed=new NumericUpDown();
        readonly Button load=new Button(),reset=new Button(),yard=new Button(),approach=new Button();
        readonly ComboBox stages=new ComboBox(),fleet=new ComboBox();
        readonly ListBox members=new ListBox();
        readonly Label summary=new Label(),feedback=new Label();
        readonly TextBox details=new TextBox();
        readonly TabControl views=new TabControl();
        readonly HashSet<Keys> held=new HashSet<Keys>();
        readonly StudyCanvas world,grid;
        PracticeMapStartup state;
        Scenario scene;
        byte[] ships,fleets;
        bool changing;
        internal PracticeMapStartup StudyState { get { return state; } }
        internal PracticeMapStartupStage AppliedStage { get { return state==null||stages.SelectedIndex<0?null:state.Stages[stages.SelectedIndex]; } }
        internal int SelectedFleet { get { return fleet.SelectedItem==null?-1:((FleetChoice)fleet.SelectedItem).Slot; } }
        internal int SelectedUnit
        {
            get
            {
                if(SelectedFleet<0||members.SelectedIndex<0)return -1;
                uint reference=BitConverter.ToUInt32(fleets,SelectedFleet*492+16+members.SelectedIndex*4);
                return reference==0?-1:(int)((reference-0x4fc340)/268);
            }
        }
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeMapStartupStudy(string programs,byte[] source)
        {
            if(source==null)throw new ArgumentNullException("source");
            this.programs=programs;this.source=(byte[])source.Clone();
            Name="mapStartupStudy";Text="BSSB202 startup · copied stages";StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1120,800);MinimumSize=new Size(940,680);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,350));Controls.Add(layout);
            views.Name="mapStartupViews";views.Dock=DockStyle.Fill;
            var mapPage=new TabPage("Map placement"){BackColor=BackColor};var gridPage=new TabPage("Fleet grid"){BackColor=BackColor};views.TabPages.Add(mapPage);views.TabPages.Add(gridPage);
            world=new StudyCanvas(this,true){Name="mapStartupWorld",Dock=DockStyle.Fill};grid=new StudyCanvas(this,false){Name="mapStartupGrid",Dock=DockStyle.Fill};mapPage.Controls.Add(world);gridPage.Controls.Add(grid);layout.Controls.Add(views,0,0);
            var scroll=new Panel{Name="mapStartupScroll",Dock=DockStyle.Fill,AutoScroll=true};
            var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);layout.Controls.Add(scroll,1,0);
            Action<Control,int> add=delegate(Control c,int height){c.Width=316;c.Height=height;c.Margin=new Padding(0,3,0,3);if(c is Button||c is ComboBox||c is ListBox){c.BackColor=Color.FromArgb(28,55,76);c.ForeColor=ForeColor;}var b=c as Button;if(b!=null){b.UseVisualStyleBackColor=false;b.FlatStyle=FlatStyle.Flat;}side.Controls.Add(c);};
            add(new Label{Text="Copied BSSB202 primary data, from map entities through grouped fleets, stock, placement and late aircraft/roster preparation. Stages are snapshots. No world clock, mission, AI or actual source display runs."},92);
            add(new Label{Text="Supplied seed (pending until Load)"},22);
            seed.Name="mapStartupSeed";seed.Minimum=0;seed.Maximum=uint.MaxValue;seed.Value=731;seed.DecimalPlaces=0;seed.ThousandsSeparator=false;add(seed,30);
            load.Name="mapStartupLoad";load.Text="Load entered seed";add(load,34);
            reset.Name="mapStartupReset";reset.Text="Reset applied seed";add(reset,34);
            summary.Name="mapStartupSummary";add(summary,110);
            feedback.Name="mapStartupFeedback";add(feedback,76);
            add(new Label{Text="Recovered data stage"},22);stages.Name="mapStartupStage";stages.DropDownStyle=ComboBoxStyle.DropDownList;add(stages,30);
            yard.Name="mapStartupYard";yard.Text="Copied map yard… (seed 731, stage 4)";yard.Enabled=false;add(yard,36);
            approach.Name="mapStartupApproach";approach.Text="Nearby yard approach… (seed 731)";approach.Enabled=false;add(approach,36);
            approach.Click+=delegate{if(state!=null&&state.Seed==731&&stages.SelectedIndex==3)using(var study=new PracticeYardApproachStudy(programs,state,SelectedUnit))study.ShowDialog(this);};
            add(new Label{Text="Nonempty primary fleet records"},22);fleet.Name="mapStartupFleet";fleet.DropDownStyle=ComboBoxStyle.DropDownList;add(fleet,30);
            members.Name="mapStartupMembers";members.AccessibleName="Six roster fields, with unit slot and type";add(members,110);
            details.Name="mapStartupDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,235);
            yard.Click+=delegate
            {
                if(state==null||state.Seed!=731||stages.SelectedIndex!=3)return;
                using(var study=new PracticeMapYardStudy(programs,state,SelectedUnit))study.ShowDialog(this);
            };
            var close=new Button{Name="mapStartupClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            load.Click+=delegate{ApplySeed((uint)seed.Value);};reset.Click+=delegate{if(state!=null)ApplySeed(state.Seed);};
            seed.ValueChanged+=delegate{if(state!=null&&!changing)feedback.Text="Seed selection is pending. Load applies it; Reset keeps the applied seed "+state.Seed+".";};
            stages.SelectedIndexChanged+=delegate{if(!changing)ShowStage(SelectedFleet);};fleet.SelectedIndexChanged+=delegate{if(!changing)RefreshMembers();};members.SelectedIndexChanged+=delegate{if(!changing)RefreshDetails();};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{held.Clear();};
            Activated+=delegate{foreach(var key in new[]{Keys.Enter,Keys.Space,Keys.Escape})if((GetAsyncKeyState((int)key)&0x8000)!=0)held.Add(key);};
            ApplySeed(731);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);if(e.KeyCode!=Keys.Enter&&e.KeyCode!=Keys.Space&&e.KeyCode!=Keys.Escape)return;var button=ActiveControl as Button;if(e.KeyCode!=Keys.Escape&&button==null)return;
            e.Handled=e.SuppressKeyPress=true;if(!held.Add(e.KeyCode))return;if(e.KeyCode==Keys.Escape)Close();else if(button.Enabled)button.PerformClick();
        }
        void ApplySeed(uint value)
        {
            try
            {
                var next=PracticeMapStartup.CreateBssb202(programs,source,value);var nextScene=Scenario.Load(next.CopySource(),"BSSB202.SCN");
                changing=true;state=next;scene=nextScene;stages.Items.Clear();foreach(var stage in state.Stages)stages.Items.Add(stage);stages.SelectedIndex=3;changing=false;
                stages.Enabled=fleet.Enabled=members.Enabled=reset.Enabled=true;
                feedback.ForeColor=ForeColor;feedback.Text="Copied startup rebuilt. Choose a stage and fleet; snapshots do not advance by themselves."+((uint)seed.Value==value?"":" Entered seed remains pending.");
                ShowStage(16);
            }
            catch(InvalidDataException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(ArgumentException e){Reject(e.Message);}catch(UnauthorizedAccessException e){Reject(e.Message);}
        }
        void Reject(string reason)
        {
            feedback.ForeColor=Color.Orange;feedback.Text="Startup rejected; existing copy preserved. "+reason;
            if(state==null){stages.Enabled=fleet.Enabled=members.Enabled=reset.Enabled=false;summary.Text="No startup copy loaded.";}
        }
        void ShowStage(int preferred)
        {
            if(AppliedStage==null||IsDisposed||Disposing)return;
            yard.Enabled=state.Seed==731&&stages.SelectedIndex==3;
            approach.Enabled=yard.Enabled;
            ships=AppliedStage.CopyShipPool();fleets=AppliedStage.CopyFleetPool();changing=true;fleet.Items.Clear();int selected=-1;
            for(int n=0;n<250;n++)if(BitConverter.ToInt32(fleets,n*492)!=0){if(n==preferred)selected=fleet.Items.Count;fleet.Items.Add(new FleetChoice(n,BitConverter.ToInt32(fleets,n*492),BitConverter.ToInt16(fleets,n*492+6)));}
            fleet.SelectedIndex=selected>=0?selected:fleet.Items.Count>0?0:-1;changing=false;
            summary.Text="Applied seed: "+state.Seed+" · stage "+(stages.SelectedIndex+1)+" / 4\r\n"+AppliedStage.NonemptyFleets+" fleets / "+AppliedStage.NonemptyUnits+" nonzero units\r\nNext inactive fleet: "+AppliedStage.NextInactiveFleet+" (derived)\r\nRNG: "+AppliedStage.RandomState+"\r\nRandom draws from seed: "+AppliedStage.RandomCalls+"\r\nTerrain: "+state.TerrainCount+" records / 500 slots";
            RefreshMembers();
        }
        void RefreshMembers()
        {
            changing=true;members.Items.Clear();
            if(SelectedFleet>=0)for(int m=0;m<6;m++){uint r=BitConverter.ToUInt32(fleets,SelectedFleet*492+16+m*4);if(r==0)members.Items.Add(m+" · null");else{int unit=(int)((r-0x4fc340)/268);members.Items.Add(m+" · unit "+unit+" · type "+BitConverter.ToInt32(ships,unit*268));}}
            members.SelectedIndex=members.Items.Count>0?0:-1;changing=false;RefreshDetails();
        }
        void RefreshDetails()
        {
            if(AppliedStage==null||IsDisposed||Disposing)return;var text=new StringBuilder();
            text.AppendLine("BSSB202 source SHA256: "+PracticeMapStartup.SupportedSourceHash);
            text.AppendLine("Owner counters (-1,0,1,2,3): "+string.Join(", ",Array.ConvertAll(AppliedStage.CopyOwnerCounters(),delegate(uint n){return n.ToString();})));
            if(SelectedFleet>=0)
            {
                int at=SelectedFleet*492;text.AppendLine("Fleet "+SelectedFleet+" / kind "+BitConverter.ToInt32(fleets,at)+" / owner "+BitConverter.ToInt16(fleets,at+6));text.AppendLine("World "+BitConverter.ToInt16(fleets,at+12)+", "+BitConverter.ToInt16(fleets,at+14)+" / flag "+fleets[at+484]);
                if(SelectedUnit>=0){int u=SelectedUnit*268;text.AppendLine("Unit "+SelectedUnit+" / type "+BitConverter.ToInt32(ships,u)+" / parent "+BitConverter.ToInt16(ships,u+12));text.AppendLine("Local "+BitConverter.ToInt16(ships,u+8)+", "+BitConverter.ToInt16(ships,u+10)+" / length "+BitConverter.ToInt16(ships,u+14)+" / HP "+BitConverter.ToInt16(ships,u+16));}
            }
            text.AppendLine("Primary setup snapshots plus the separate 53-record terrain scan. Other auxiliary pools, SCN writeback and preset globals are excluded.");text.AppendLine("Stored aircraft have data/attachments; flight and source UI/AI/mission callbacks do not run.");details.Text=text.ToString();world.Invalidate();grid.Invalidate();
        }
        sealed class FleetChoice
        {
            internal readonly int Slot,Kind,Owner;internal FleetChoice(int slot,int kind,int owner){Slot=slot;Kind=kind;Owner=owner;}
            public override string ToString(){return Slot+" · kind "+Kind+" · owner "+Owner;}
        }
        sealed class StudyCanvas : Control
        {
            readonly PracticeMapStartupStudy owner;readonly bool map;
            internal StudyCanvas(PracticeMapStartupStudy owner,bool map){this.owner=owner;this.map=map;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);if(owner.AppliedStage==null||Width<80||Height<160)return;var g=e.Graphics;float size=Math.Min(Width-50,Height-112),left=(Width-size)/2,top=24;
                if(map)
                {
                    float cell=size/64;
                    int selected=owner.SelectedFleet,sx=selected<0?int.MinValue:BitConverter.ToInt16(owner.fleets,selected*492+12),sy=selected<0?int.MinValue:BitConverter.ToInt16(owner.fleets,selected*492+14);
                    for(int x=0;x<64;x++)for(int y=0;y<64;y++){int kind=owner.scene.At(x,y).Kind;Color color=kind==2?Color.FromArgb(97,121,79):kind==4?Color.FromArgb(34,70,95):kind==1?Color.FromArgb(20,44,64):Color.FromArgb(117,100,84);using(var brush=new SolidBrush(color))g.FillRectangle(brush,left+x*cell,top+y*cell,cell+.1f,cell+.1f);}
                    for(int n=0;n<250;n++)
                    {
                        int at=n*492,kind=BitConverter.ToInt32(owner.fleets,at);if(kind==0||n==selected)continue;int x=BitConverter.ToInt16(owner.fleets,at+12),y=BitConverter.ToInt16(owner.fleets,at+14),player=BitConverter.ToInt16(owner.fleets,at+6);float px=left+x*cell/10,py=top+y*cell/10;
                        using(var brush=new SolidBrush(player==0?Color.LightSkyBlue:Color.Coral))g.FillEllipse(brush,px-3,py-3,6,6);
                        //Stored aircraft overlap their parents. Label the main
                        //fleets/objects and selected record rather than stacked IDs.
                        if((kind==1||kind==9||kind==11)&&(x!=sx||y!=sy))g.DrawString(n.ToString(),owner.Font,Brushes.White,Math.Min(left+size-22,px+3),Math.Max(top,py-15));
                    }
                    if(selected>=0){float px=left+sx*cell/10,py=top+sy*cell/10;g.FillEllipse(Brushes.Gold,px-4,py-4,8,8);g.DrawString(selected.ToString(),owner.Font,Brushes.Gold,Math.Min(left+size-22,px+5),Math.Max(top,py-17));}
                    g.DrawString("Schematic primary fleets · world X across / Y down\r\nGold = selected; owners 0 blue / 1 orange. Choose stored groups by fleet list.\r\nSCN cells are a backdrop; no secondary-pool or mission simulation runs.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-78,Math.Max(1,Width-24),74));
                }
                else
                {
                    if(owner.SelectedFleet<0)return;float cell=size/10;int at=owner.SelectedFleet*492;
                    for(int x=0;x<10;x++)for(int y=0;y<10;y++){int value=BitConverter.ToInt32(owner.fleets,at+64+(x*10+y)*4);using(var brush=new SolidBrush(value==2?Color.SteelBlue:Color.FromArgb(25,45,61)))g.FillRectangle(brush,left+y*cell,top+x*cell,cell-1,cell-1);if(value!=0&&value!=3)g.DrawString(value.ToString(),owner.Font,Brushes.White,left+y*cell+2,top+x*cell+2);}
                    for(int n=0;n<10;n++){g.DrawString(n.ToString(),owner.Font,Brushes.LightSteelBlue,left+n*cell,top-21);g.DrawString(n.ToString(),owner.Font,Brushes.LightSteelBlue,left-22,top+n*cell);}
                    if(owner.SelectedUnit>=0){int u=owner.SelectedUnit*268,x=BitConverter.ToInt16(owner.ships,u+8),y=BitConverter.ToInt16(owner.ships,u+10),length=BitConverter.ToInt16(owner.ships,u+14);if(BitConverter.ToInt32(owner.ships,u)!=0&&x>=0&&x<10&&y>=0&&length>0&&y+length<=10)using(var pen=new Pen(Color.Gold,3))g.DrawRectangle(pen,left+y*cell+1,top+x*cell+1,length*cell-3,cell-3);}
                    g.DrawString("Recovered grid · rows local X / columns local Y\r\nBlue2 = occupied; gold = selected nonempty unit.\r\nThese are immutable setup snapshots, with no active battle clock.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-78,Math.Max(1,Width-24),74));
                }
            }
        }
    }
}
