using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
namespace FleetLab
{
    // Read-only modern presentation of the ordinary map base-construction pass.
    public sealed class PracticeMapInstallationStudy : Form
    {
        readonly string assets;
        readonly PracticeAirfieldFactory factory;
        readonly ComboBox maps=new ComboBox(),fleet=new ComboBox();
        readonly ListBox bases=new ListBox(),members=new ListBox();
        readonly Button load=new Button();
        readonly Label status=new Label(),feedback=new Label(),zonesSummary=new Label();
        readonly TextBox details=new TextBox();
        readonly TabControl views=new TabControl();
        readonly StudyCanvas world,grid,zoneView;
        readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeMapInstallations state;
        PracticeSpawnZoneResult zones;
        Scenario scene;
        byte[] ships,fleets;
        bool changing;
        internal PracticeMapInstallations StudyState { get { return state; } }
        internal PracticeSpawnZoneResult SpawnZones { get { return zones; } }
        internal int SelectedFleet { get { return bases.SelectedIndex<0||fleet.SelectedIndex<0?-1:((PracticeMapInstallation)bases.SelectedItem).FirstFleet+fleet.SelectedIndex; } }
        internal int SelectedUnit { get { return members.SelectedIndex<0?-1:(int)members.SelectedItem; } }
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeMapInstallationStudy(string assets,string programs)
        {
            this.assets=assets;
            if(!File.Exists(Path.Combine(assets,"Graphics","BSSB202.SCN")))throw new InvalidDataException("Run Setup.ps1 again to import the SCN map library for this study.");
            factory=PracticeAirfieldFactory.Load(programs);
            Text="Map installations · construction and spawn-zone studies";
            Name="mapInstallationStudy";
            StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1100,800);
            MinimumSize=new Size(900,680);
            Font=new Font("Segoe UI",9);
            BackColor=Color.FromArgb(12,23,35);
            ForeColor=Color.WhiteSmoke;
            KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,324));
            Controls.Add(layout);
            views.Name="mapInstallationViews";
            views.Dock=DockStyle.Fill;
            var placement=new TabPage("Map placement"){BackColor=BackColor};
            var occupancy=new TabPage("Fleet grid"){BackColor=BackColor};
            var spawn=new TabPage("Spawn zones (study)"){BackColor=BackColor};
            views.TabPages.Add(placement);
            views.TabPages.Add(occupancy);
            views.TabPages.Add(spawn);
            world=new StudyCanvas(this,true){Dock=DockStyle.Fill,Name="mapInstallationWorld",AccessibleName="Schematic map and constructed installation footprints"};
            grid=new StudyCanvas(this,false){Dock=DockStyle.Fill,Name="mapInstallationGrid",AccessibleName="Selected fleet grid, rows local X, columns local Y"};
            zoneView=new StudyCanvas(this,true,true){Dock=DockStyle.Fill,Name="mapSpawnZones",AccessibleName="Recovered spawn sectors with supplied base and terrain reservations"};
            placement.Controls.Add(world);
            occupancy.Controls.Add(grid);
            spawn.Controls.Add(zoneView);
            layout.Controls.Add(views,0,0);
            var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Name="mapInstallationScroll"};
            var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};
            scroll.Controls.Add(side);
            layout.Controls.Add(scroll,1,0);
            Action<Control,int> add=delegate(Control c,int height){c.Width=292;c.Height=height;c.Margin=new Padding(0,3,0,3);if(c is Button||c is ComboBox||c is ListBox){c.BackColor=Color.FromArgb(28,55,76);c.ForeColor=Color.WhiteSmoke;}var button=c as Button;if(button!=null){button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;}side.Controls.Add(c);};
            add(new Label{Text="Copied ordinary SCN base pass. Recovered placements, templates and grids. Scenario-specific setup, other entities, startup inventory, live support and missions are excluded."},96);
            maps.Name="mapInstallationSource";
            maps.AccessibleName="SCN source for ordinary base construction";
            maps.DropDownStyle=ComboBoxStyle.DropDownList;
            string[] files=Directory.GetFiles(Path.Combine(assets,"Graphics"),"*.scn");
            Array.Sort(files,StringComparer.OrdinalIgnoreCase);
            foreach(string file in files)maps.Items.Add(Path.GetFileName(file));
            add(maps,30);
            load.Text="Load map base pass";
            load.Name="mapInstallationLoad";
            add(load,34);
            status.Name="mapInstallationStatus";
            status.AccessibleName="Applied map and construction counts";
            add(status,94);
            zonesSummary.Name="mapSpawnZoneSummary";
            zonesSummary.AccessibleName="Spawn-zone player counts and supplied reservation scope";
            zonesSummary.Visible=false;
            add(zonesSummary,104);
            feedback.Name="mapInstallationFeedback";
            feedback.AccessibleName="Map load result";
            add(feedback,70);
            add(new Label{Text="Constructed installations"},22);
            bases.Name="mapInstallationBases";
            bases.AccessibleName="Constructed four-fleet installations";
            add(bases,94);
            add(new Label{Text="Fleet within selected installation"},22);
            fleet.Name="mapInstallationFleet";
            fleet.AccessibleName="Installation fleet quadrant";
            fleet.DropDownStyle=ComboBoxStyle.DropDownList;
            fleet.Items.AddRange(new object[]{"0 · airfield / type24","1 · inventory / type21","2 · type22 markers","3 · type23 markers"});
            fleet.SelectedIndex=0;
            add(fleet,30);
            members.Name="mapInstallationMembers";
            members.AccessibleName="Referenced unit slots";
            add(members,82);
            details.Name="mapInstallationDetails";
            details.AccessibleName="Map, selected fleet and unit data";
            details.Multiline=true;
            details.ReadOnly=true;
            details.ScrollBars=ScrollBars.Vertical;
            details.BackColor=BackColor;
            details.ForeColor=ForeColor;
            add(details,240);
            var close=new Button{Text="Close",Name="mapInstallationClose"};
            add(close,34);
            close.Click+=delegate{Close();};
            load.Click+=delegate{LoadSelected();};
            maps.SelectedIndexChanged+=delegate{if(state!=null)feedback.Text="Source selection is pending. Load applies it; the current copy is preserved on failure.";};
            bases.SelectedIndexChanged+=delegate{if(!changing)RefreshMembers();};
            fleet.SelectedIndexChanged+=delegate{if(!changing)RefreshMembers();};
            members.SelectedIndexChanged+=delegate{if(!changing)RefreshView();};
            views.SelectedIndexChanged+=delegate{zonesSummary.Visible=views.SelectedIndex==2;};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};
            Deactivate+=delegate{held.Clear();};
            Activated+=delegate{foreach(var key in new[]{Keys.Enter,Keys.Space,Keys.Escape})if((GetAsyncKeyState((int)key)&0x8000)!=0)held.Add(key);};
            string initial=Array.Find(files,delegate(string file){return string.Equals(Path.GetFileName(file),"BSSB202.SCN",StringComparison.OrdinalIgnoreCase);});
            if(initial!=null)maps.SelectedItem=Path.GetFileName(initial);
            if(maps.SelectedIndex<0&&maps.Items.Count>0)maps.SelectedIndex=0;
            LoadSelected();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(e.KeyCode!=Keys.Enter&&e.KeyCode!=Keys.Space&&e.KeyCode!=Keys.Escape)return;
            var button=ActiveControl as Button;
            if(e.KeyCode!=Keys.Escape&&button==null)return;
            e.Handled=e.SuppressKeyPress=true;
            if(!held.Add(e.KeyCode))return;
            if(e.KeyCode==Keys.Escape)Close();
            else if(button.Enabled)button.PerformClick();
        }
        void LoadSelected()
        {
            if(maps.SelectedItem==null){feedback.Text="No SCN sources are available.";return;}
            try
            {
                string name=(string)maps.SelectedItem;
                byte[] input=File.ReadAllBytes(Path.Combine(assets,"Graphics",name));
                var next=PracticeMapInstallations.Create(factory,input,name);
                var nextScene=Scenario.Load(input,name);
                var nextZones=PracticeSpawnZones.ForMapBaseStudy(nextScene,next.CopyFleetPool());
                changing=true;
                state=next;
                scene=nextScene;
                zones=nextZones;
                ships=state.CopyShipPool();
                fleets=state.CopyFleetPool();
                bases.Items.Clear();
                foreach(var item in state.Installations)bases.Items.Add(item);
                bases.SelectedIndex=bases.Items.Count>0?0:-1;
                fleet.SelectedIndex=0;
                changing=false;
                status.Text="Applied: "+state.SourceName+"\r\n"+state.CandidateCells+" tagged cells / "+state.DuplicateCells+" duplicates\r\n"+state.Installations.Count+" installations / "+state.NextFleetSlot+" fleets / "+state.UnitCount+" units\r\nNo random draws or running simulation.";
                feedback.Text=state.Installations.Count==0?"No base tags in this map. Empty construction is expected; grouped fleets are outside this pass.":"Copied base pass loaded. Select an installation and fleet; use Fleet grid to inspect its roster.";
                var counts=zones.CopyOwnerCounts();
                var ownerText=new StringBuilder();
                for(int n=0;n<zones.PlayerCount;n++)ownerText.Append((n==0?"":" / ")+n+":"+counts[n]);
                zonesSummary.Text="Spawn-zone study · "+zones.PlayerCount+" players\r\n"+zones.AvailableCells+" open cells · owners "+ownerText+"\r\nReservations: "+zones.PrimaryReservations+" base fleets + "+zones.SecondaryReservations+" terrain cells. Other map objects are not reserved.";
                RefreshMembers();
            }
            catch(InvalidDataException e){feedback.Text="Load rejected; current copy preserved. "+e.Message;}
            catch(IOException e){feedback.Text="Load rejected; current copy preserved. "+e.Message;}
            catch(ArgumentException e){feedback.Text="Load rejected; current copy preserved. "+e.Message;}
            catch(NotSupportedException e){feedback.Text="Load rejected; current copy preserved. "+e.Message;}
            catch(UnauthorizedAccessException e){feedback.Text="Load rejected; current copy preserved. "+e.Message;}
        }
        void RefreshMembers()
        {
            if(IsDisposed||Disposing)return;
            changing=true;
            members.Items.Clear();
            int selected=SelectedFleet;
            if(selected>=0)for(int n=0;n<6;n++)
            {
                uint reference=BitConverter.ToUInt32(fleets,selected*492+16+n*4);
                if(reference!=0)members.Items.Add((int)((reference-0x4fc340)/268));
            }
            members.SelectedIndex=members.Items.Count>0?0:-1;
            fleet.Enabled=bases.SelectedIndex>=0;
            changing=false;
            RefreshView();
        }
        void RefreshView()
        {
            if(IsDisposed||Disposing||state==null)return;
            var text=new StringBuilder();
            text.AppendLine("Source SHA256: "+state.SourceHash);
            var counters=state.CopyOwnerCounters();
            text.AppendLine("Source owner counters (-1,0,1,2,3): "+string.Join(", ",Array.ConvertAll(counters,delegate(uint n){return n.ToString();})));
            if(SelectedFleet>=0)
            {
                var item=(PracticeMapInstallation)bases.SelectedItem;
                int at=SelectedFleet*492;
                text.AppendLine("Source cell "+item.SourceCell.X+", "+item.SourceCell.Y+" / tag "+item.SourceTag+" / opaque argument "+item.TagArgument);
                text.AppendLine("Fleet "+SelectedFleet+" / owner "+BitConverter.ToInt16(fleets,at+6)+" / world "+BitConverter.ToInt16(fleets,at+12)+", "+BitConverter.ToInt16(fleets,at+14)+" / flag "+fleets[at+484]);
                if(SelectedUnit>=0)
                {
                    int u=SelectedUnit*268;
                    text.AppendLine("Unit "+SelectedUnit+" / type "+BitConverter.ToInt32(ships,u)+" / parent "+BitConverter.ToInt16(ships,u+12));
                    text.AppendLine("Local "+BitConverter.ToInt16(ships,u+8)+", "+BitConverter.ToInt16(ships,u+10)+" / length "+BitConverter.ToInt16(ships,u+14)+" / HP "+BitConverter.ToInt16(ships,u+16));
                }
                text.AppendLine("Raw constructor roster; active yard support stays disabled. No startup inventory pass was applied.");
            }
            else text.AppendLine("No constructed installation to select.");
            details.Text=text.ToString();
            world.Invalidate();
            grid.Invalidate();
            zoneView.Invalidate();
        }
        sealed class StudyCanvas : Control
        {
            static readonly Color[] ZoneColors={Color.FromArgb(35,95,130),Color.FromArgb(168,94,35),Color.FromArgb(48,115,81),Color.FromArgb(111,65,128)};
            readonly PracticeMapInstallationStudy owner;
            readonly bool placement,spawn;
            public StudyCanvas(PracticeMapInstallationStudy owner,bool placement,bool spawn=false){this.owner=owner;this.placement=placement;this.spawn=spawn;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if(owner.state==null)return;
                var g=e.Graphics;
                float size=Math.Max(1,Math.Min(Width-40,Height-110)),left=(Width-size)/2,top=20;
                if(placement)
                {
                    float cell=size/64;
                    for(int x=0;x<64;x++)for(int y=0;y<64;y++)
                    {
                        var c=owner.scene.At(x,y);
                        Color color=c.Kind==1?Color.FromArgb(24,55,77):c.Kind==2?Color.FromArgb(123,145,91):c.Kind==4?Color.FromArgb(44,84,106):c.Kind==7?Color.Turquoise:Color.IndianRed;
                        if(spawn)
                        {
                            int? sectorOwner=owner.zones.OwnerAt(x,y);
                            color=sectorOwner.HasValue?ZoneColors[sectorOwner.Value]:Color.FromArgb(25,30,38);
                        }
                        using(var brush=new SolidBrush(color))g.FillRectangle(brush,left+x*cell,top+y*cell,cell+.1f,cell+.1f);
                    }
                    for(int n=0;n<owner.state.Installations.Count;n++)
                    {
                        var item=owner.state.Installations[n];
                        Color color=n==owner.bases.SelectedIndex?Color.Gold:Color.LightCyan;
                        using(var pen=new Pen(color,n==owner.bases.SelectedIndex?3:1))g.DrawRectangle(pen,left+item.SourceCell.X*cell,top+item.SourceCell.Y*cell,2*cell,2*cell);
                        string label=(n+1).ToString();
                        float lx=Math.Max(left,Math.Min(left+size-24,left+item.SourceCell.X*cell)),ly=Math.Max(top,top+item.SourceCell.Y*cell-18);
                        using(var brush=new SolidBrush(color))g.DrawString(label,owner.Font,brush,lx,ly);
                    }
                    g.DrawString(spawn?"Recovered sectors · owners 0 blue / 1 orange / 2 green / 3 violet\r\nDark = blocked; gold = selected base. World X across, Y down.\r\nSupplied base/terrain reservations; other objects and missions excluded.":"Schematic SCN cells · gold = selected compound footprint\r\nWorld X across, Y down; one cell = 10 source units.\r\nOnly base construction is applied; map objects and missions are absent.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-78,Math.Max(1,Width-24),72));
                }
                else
                {
                    if(owner.SelectedFleet<0){g.DrawString("This map has no constructed base fleet.",owner.Font,Brushes.LightSteelBlue,12,20);return;}
                    float cell=size/10;
                    int at=owner.SelectedFleet*492;
                    for(int x=0;x<10;x++)for(int y=0;y<10;y++)
                    {
                        int value=BitConverter.ToInt32(owner.fleets,at+64+(x*10+y)*4);
                        var r=new RectangleF(left+y*cell,top+x*cell,cell,cell);
                        using(var brush=new SolidBrush(value==2?Color.SteelBlue:Color.FromArgb(35,53,66)))g.FillRectangle(brush,r);
                        g.DrawRectangle(Pens.SlateGray,r.X,r.Y,r.Width,r.Height);
                        if(value!=3)g.DrawString(value.ToString(),owner.Font,Brushes.White,r.X+3,r.Y+3);
                    }
                    if(owner.SelectedUnit>=0)
                    {
                        int u=owner.SelectedUnit*268,x=BitConverter.ToInt16(owner.ships,u+8),y=BitConverter.ToInt16(owner.ships,u+10),length=BitConverter.ToInt16(owner.ships,u+14);
                        using(var pen=new Pen(Color.Gold,3))g.DrawRectangle(pen,left+y*cell+1,top+x*cell+1,length*cell-2,cell-2);
                    }
                    g.DrawString("Fleet "+owner.SelectedFleet+" · rows = local X; columns = local Y\r\n2 occupied · 3 empty · gold = selected unit\r\nOriginal grid data; read-only constructor stage.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-78,Math.Max(1,Width-24),72));
                }
            }
        }
    }
}
