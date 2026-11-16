using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace FleetLab
{
    // Independent diagnostic copy. The base scan does not contain grouped
    // carriers or startup stock; this view never substitutes a complete mission.
    public sealed class PracticeLateConfigurationStudy : Form
    {
        readonly PracticeMapInstallations source;
        readonly PracticeUnitCatalog catalog;
        readonly Button prepare=new Button(),reset=new Button(),select=new Button();
        readonly ComboBox fleet=new ComboBox(),owner=new ComboBox();
        readonly ListBox members=new ListBox();
        readonly Label summary=new Label(),feedback=new Label(),selection=new Label();
        readonly TextBox details=new TextBox();
        readonly HashSet<Keys> held=new HashSet<Keys>();
        readonly GridCanvas grid;
        byte[] ships,fleets;
        bool changing;
        PracticeLateConfigurationResult late;
        PracticeConfigurationSelectionResult chosen;
        internal bool Prepared { get { return late!=null; } }
        internal PracticeLateConfigurationResult LateState { get { return late; } }
        internal PracticeConfigurationSelectionResult SourceSelection { get { return chosen; } }
        internal PracticeMapInstallations SourceState { get { return source; } }
        internal byte[] CopyShipPool(){return (byte[])ships.Clone();}
        internal byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeLateConfigurationStudy(string programs,PracticeMapInstallations source)
        {
            if(source==null)throw new ArgumentNullException("source");
            this.source=source;catalog=PracticeUnitCatalog.Load(programs);
            Name="lateConfigurationStudy";Text="Late configuration · copied base pass";
            StartPosition=FormStartPosition.CenterParent;Size=new Size(1080,780);MinimumSize=new Size(900,660);
            Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,350));Controls.Add(layout);
            grid=new GridCanvas(this){Dock=DockStyle.Fill,Name="lateConfigurationGrid",AccessibleName="Schematic copied fleet grid; rows local X and columns local Y"};layout.Controls.Add(grid,0,0);
            var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Name="lateConfigurationScroll"};
            var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};
            scroll.Controls.Add(side);layout.Controls.Add(scroll,1,0);
            Action<Control,int> add=delegate(Control c,int height){c.Width=316;c.Height=height;c.Margin=new Padding(0,3,0,3);if(c is Button||c is ComboBox||c is ListBox){c.BackColor=Color.FromArgb(28,55,76);c.ForeColor=ForeColor;}var b=c as Button;if(b!=null){b.UseVisualStyleBackColor=false;b.FlatStyle=FlatStyle.Flat;}side.Controls.Add(c);};
            add(new Label{Text="Independent copy of the applied map's base construction. Prepare runs recovered carrier/marker data rules with offscreen owner −2. This input has no grouped carriers, stock pass, missions or active support."},94);
            summary.Name="lateConfigurationSummary";add(summary,82);
            prepare.Name="lateConfigurationPrepare";prepare.Text="Prepare late roster (once)";add(prepare,34);
            reset.Name="lateConfigurationReset";reset.Text="Reset this copy to base pass";add(reset,34);
            feedback.Name="lateConfigurationFeedback";add(feedback,78);
            add(new Label{Text="Inspect fleet (includes inactive records)"},22);
            fleet.Name="lateConfigurationFleet";fleet.DropDownStyle=ComboBoxStyle.DropDownList;add(fleet,30);
            members.Name="lateConfigurationMembers";members.AccessibleName="Six roster fields, including null and empty units";add(members,110);
            add(new Label{Text="Independent source selection input: owner"},22);
            owner.Name="lateConfigurationOwner";owner.DropDownStyle=ComboBoxStyle.DropDownList;owner.Items.AddRange(new object[]{-2,-1,0,1,2,3});owner.SelectedItem=source.Installations.Count>0?source.Installations[0].Owner:0;add(owner,30);
            select.Name="lateConfigurationSelect";select.Text="Select owned fleet (copy)";add(select,34);
            selection.Name="lateConfigurationSelection";add(selection,78);
            details.Name="lateConfigurationDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,230);
            var close=new Button{Text="Close",Name="lateConfigurationClose"};add(close,34);close.Click+=delegate{Close();};
            prepare.Click+=delegate{Prepare();};reset.Click+=delegate{ResetCopy();};select.Click+=delegate{SelectOwned();};
            fleet.SelectedIndexChanged+=delegate{if(!changing)RefreshMembers();};members.SelectedIndexChanged+=delegate{if(!changing)RefreshDetails();};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{held.Clear();};
            Activated+=delegate{foreach(var key in new[]{Keys.Enter,Keys.Space,Keys.Escape})if((GetAsyncKeyState((int)key)&0x8000)!=0)held.Add(key);};
            ResetCopy();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);if(e.KeyCode!=Keys.Enter&&e.KeyCode!=Keys.Space&&e.KeyCode!=Keys.Escape)return;
            var button=ActiveControl as Button;if(e.KeyCode!=Keys.Escape&&button==null)return;
            e.Handled=e.SuppressKeyPress=true;if(!held.Add(e.KeyCode))return;
            if(e.KeyCode==Keys.Escape)Close();else if(button.Enabled)button.PerformClick();
        }
        void ResetCopy()
        {
            ships=source.CopyShipPool();fleets=source.CopyFleetPool();late=null;chosen=null;prepare.Enabled=true;
            feedback.Text="Raw base constructor copy. Preparing clears even marker types; grid and marker payload bytes remain. Reset always restores this applied source.";
            selection.Text="No source selection applied. Choose an owner and click Select; this does not start simulation.";
            RefreshFleets(0);RefreshSummary();
        }
        void Prepare()
        {
            if(late!=null)return;
            try
            {
                var result=PracticeLateConfiguration.ApplyOffscreen(catalog,ships,fleets,-2);
                ships=result.CopyShipPool();fleets=result.CopyFleetPool();late=result;prepare.Enabled=false;
                feedback.Text="Prepared this copy: "+late.ClearedMarkerUnits.Count+" even markers cleared; "+late.CarrierGroups.Count+" stored carrier groups. Source arrays and parent study are unchanged.";
                RefreshFleets(Math.Max(0,fleet.SelectedIndex));RefreshSummary();
            }
            catch(InvalidDataException e){feedback.Text="Preparation rejected; copy preserved. "+e.Message;}
            catch(NotSupportedException e){feedback.Text="Preparation rejected; copy preserved. "+e.Message;}
            catch(ArgumentException e){feedback.Text="Preparation rejected; copy preserved. "+e.Message;}
        }
        void SelectOwned()
        {
            try
            {
                short local=Convert.ToInt16(owner.SelectedItem);
                var result=PracticeConfigurationSelection.Select(ships,fleets,local,chosen==null?-1:chosen.Fleet,chosen==null?-1:chosen.Unit);
                chosen=result;
                selection.Text="Owner "+local+": "+(result.FoundOwnedFleet?"owned record found":"no owned record; prior selection retained")+"\r\nFleet "+(result.Fleet<0?"null":result.Fleet.ToString())+" / unit "+(result.Unit<0?"null":result.Unit.ToString())+". Inactive/dead fallback is retained by the source.";
                if(result.Fleet>=0)
                {
                    fleet.SelectedIndex=result.Fleet;
                    int found=-1;
                    for(int n=0;n<6;n++){uint r=BitConverter.ToUInt32(fleets,result.Fleet*492+16+n*4);if(r!=0&&(int)((r-0x4fc340)/268)==result.Unit){found=n;break;}}
                    members.SelectedIndex=found;
                }
                RefreshDetails();
            }
            catch(InvalidDataException e){feedback.Text="Selection rejected; prior selection preserved. "+e.Message;}
            catch(ArgumentException e){feedback.Text="Selection rejected; prior selection preserved. "+e.Message;}
        }
        void RefreshSummary()
        {
            summary.Text=source.SourceName+" · "+source.Installations.Count+" base compounds\r\nPhase: "+(Prepared?"late data pass applied":"raw base construction")+"\r\n"+(Prepared?late.ClearedMarkerUnits.Count+" markers cleared / next inactive fleet "+late.NextInactiveFleet:"No inventory pass or world clock.");
        }
        void RefreshFleets(int selected)
        {
            changing=true;fleet.Items.Clear();
            for(int n=0;n<250;n++)fleet.Items.Add(n+" · kind "+BitConverter.ToInt32(fleets,n*492)+" · owner "+BitConverter.ToInt16(fleets,n*492+6));
            fleet.SelectedIndex=selected;changing=false;RefreshMembers();
        }
        void RefreshMembers()
        {
            if(IsDisposed||Disposing||fleet.SelectedIndex<0)return;
            changing=true;members.Items.Clear();int at=fleet.SelectedIndex*492;
            for(int n=0;n<6;n++)
            {
                uint reference=BitConverter.ToUInt32(fleets,at+16+n*4);
                if(reference==0)members.Items.Add(n+" · null");
                else{int unit=(int)((reference-0x4fc340)/268);members.Items.Add(n+" · unit "+unit+" · type "+BitConverter.ToInt32(ships,unit*268));}
            }
            members.SelectedIndex=0;changing=false;RefreshDetails();
        }
        void RefreshDetails()
        {
            if(IsDisposed||Disposing||fleet.SelectedIndex<0)return;
            int index=fleet.SelectedIndex,at=index*492;var text=new StringBuilder();
            text.AppendLine("Source SHA256: "+source.SourceHash);
            text.AppendLine("Fleet "+index+" · kind "+BitConverter.ToInt32(fleets,at)+" · owner "+BitConverter.ToInt16(fleets,at+6));
            text.AppendLine("World "+BitConverter.ToInt16(fleets,at+12)+", "+BitConverter.ToInt16(fleets,at+14)+" · source flag "+fleets[at+484]);
            if(members.SelectedIndex>=0)
            {
                int member=members.SelectedIndex;uint reference=BitConverter.ToUInt32(fleets,at+16+member*4);
                if(reference==0)text.AppendLine("Roster "+member+": null reference.");
                else
                {
                    int unit=(int)((reference-0x4fc340)/268),u=unit*268;
                    text.AppendLine("Roster "+member+" · unit "+unit+" · type "+BitConverter.ToInt32(ships,u));
                    text.AppendLine("Parent "+BitConverter.ToInt16(ships,u+12)+" · local "+BitConverter.ToInt16(ships,u+8)+", "+BitConverter.ToInt16(ships,u+10));
                    text.AppendLine("Length "+BitConverter.ToInt16(ships,u+14)+" · HP "+BitConverter.ToInt16(ships,u+16));
                }
            }
            text.AppendLine(Prepared?"Type0 is an empty record; the even marker payload and all grid bytes are retained.":"Raw duplicate marker pairs are not an active worker profile.");
            text.AppendLine("Base-only input: no carrier fleet, startup stock, support update or mission.");
            details.Text=text.ToString();grid.Invalidate();
        }
        sealed class GridCanvas : Control
        {
            readonly PracticeLateConfigurationStudy owner;
            internal GridCanvas(PracticeLateConfigurationStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);if(owner.fleets==null||owner.fleet.SelectedIndex<0)return;
                var g=e.Graphics;float size=Math.Max(1,Math.Min(Width-54,Height-145)),cell=size/10,left=(Width-size)/2,top=35;int fleet=owner.fleet.SelectedIndex,at=fleet*492;
                for(int x=0;x<10;x++)for(int y=0;y<10;y++)
                {
                    int value=BitConverter.ToInt32(owner.fleets,at+64+(x*10+y)*4);Color color=value==2?Color.FromArgb(54,112,148):value==3?Color.FromArgb(25,63,83):Color.FromArgb(20,32,43);
                    using(var brush=new SolidBrush(color))g.FillRectangle(brush,left+y*cell,top+x*cell,cell-1,cell-1);
                }
                for(int n=0;n<10;n++){g.DrawString(n.ToString(),owner.Font,Brushes.LightSteelBlue,left+n*cell,top-23);g.DrawString(n.ToString(),owner.Font,Brushes.LightSteelBlue,left-23,top+n*cell);}
                for(int n=0;n<6;n++)
                {
                    uint reference=BitConverter.ToUInt32(owner.fleets,at+16+n*4);if(reference==0)continue;int unit=(int)((reference-0x4fc340)/268),u=unit*268,type=BitConverter.ToInt32(owner.ships,u);if(type==0)continue;
                    int x=BitConverter.ToInt16(owner.ships,u+8),y=BitConverter.ToInt16(owner.ships,u+10),length=BitConverter.ToInt16(owner.ships,u+14);if(x<0||x>9||y<0||length<1||y+length>10)continue;
                    using(var pen=new Pen(n==owner.members.SelectedIndex?Color.Gold:Color.LightCyan,n==owner.members.SelectedIndex?3:1))g.DrawRectangle(pen,left+y*cell+2,top+x*cell+2,length*cell-5,cell-5);
                    g.DrawString(unit+":"+type,owner.Font,Brushes.White,left+y*cell+3,top+x*cell+3);
                }
                g.DrawString("Schematic copied grid · rows local X / columns local Y\r\nBlue2 = occupied; dark3 = base grid; gold = selected live roster.\r\nPreparing clears even marker types; it leaves grid/payload bytes intact.\r\nManual data inspection only; no world clock or active support.",owner.Font,Brushes.LightSteelBlue,new RectangleF(12,Height-100,Math.Max(1,Width-24),94));
            }
        }
    }
}
