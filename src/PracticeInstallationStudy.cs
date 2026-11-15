using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
namespace FleetLab
{
    public sealed class PracticeInstallationStudy : Form
    {
        PracticeInstallationStudyState state;
        readonly string programs;
        readonly ComboBox construction=new ComboBox();
        readonly NumericUpDown players=new NumericUpDown();
        readonly Button load=new Button(),initialize=new Button();
        readonly Label applied=new Label();
        readonly ComboBox fleet=new ComboBox(),query=new ComboBox();
        readonly ListBox members=new ListBox();
        readonly Button mixed=new Button(),boundary=new Button(),one=new Button(),many=new Button(),reset=new Button(),find=new Button();
        readonly Label summary=new Label(),feedback=new Label(),lookup=new Label();
        readonly TextBox details=new TextBox();
        readonly InstallationCanvas canvas;
        readonly HashSet<Keys> held=new HashSet<Keys>();
        bool changing,pending;
        internal PracticeInstallationStudyState StudyState
        {
            get
            {
                return state;
            }
        }
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
        void SuppressHeld()
        {
            foreach(var key in new[]
            {
                Keys.Enter,Keys.Space,Keys.Escape
            })if((GetAsyncKeyState((int)key)&0x8000)!=0)held.Add(key);
        }
        int SelectedUnit
        {
            get
            {
                return members.SelectedIndex<0?-1:(int)members.SelectedItem;
            }
        }
        public PracticeInstallationStudy(string programs)
        {
            this.programs=programs;
            state=new PracticeInstallationStudyState(programs);
            Text="Installation constructor and recovery leaves · diagnostic copy";
            Name="installationStudy";
            StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1100,800);
            MinimumSize=new Size(900,680);
            BackColor=Color.FromArgb(12,23,35);
            ForeColor=Color.WhiteSmoke;
            Font=new Font("Segoe UI",9);
            KeyPreview=true;
            var layout=new TableLayoutPanel
            {
                Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,330));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,74));
            Controls.Add(layout);
            canvas=new InstallationCanvas(this)
            {
                Dock=DockStyle.Fill,Name="installationCanvas",AccessibleName="Schematic original installation grids and source state values",TabStop=true
            };
            layout.Controls.Add(canvas,0,0);
            var scroll=new Panel
            {
                Dock=DockStyle.Fill,AutoScroll=true,Name="installationScroll"
            };
            layout.Controls.Add(scroll,1,0);
            var side=new FlowLayoutPanel
            {
                FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(4)
            };
            scroll.Controls.Add(side);
            Action<Control,int> add=delegate(Control c,int height)
            {
                c.Width=292;
                c.Height=height;
                c.Margin=new Padding(0,3,0,3);
                var b=c as Button;
                if(b!=null)
                {
                    b.UseVisualStyleBackColor=false;
                    b.FlatStyle=FlatStyle.Flat;
                    b.FlatAppearance.BorderColor=Color.SteelBlue;
                    b.BackColor=Color.FromArgb(28,55,76);
                    b.ForeColor=Color.WhiteSmoke;
                    b.EnabledChanged+=delegate
                    {
                        if(b.IsDisposed||b.Disposing)return;
                        b.BackColor=b.Enabled?Color.FromArgb(28,55,76):Color.LightGray;
                        b.ForeColor=b.Enabled?Color.WhiteSmoke:Color.Black;
                    };
                }
                side.Controls.Add(c);
            };
            add(new Label
            {
                Text="Original compound construction in fresh copied pools: four fleets, ten installation units. Location and damage below are supplied test inputs. These controls call recovered routines, not the active support system, AI or missions."
            },104);
            add(new Label
            {
                Text="Supplied construction layout"
            },22);
            construction.Name="installationConstruction";
            construction.AccessibleName="Supplied construction layout";
            construction.DropDownStyle=ComboBoxStyle.DropDownList;
            construction.Items.AddRange(new object[]
            {
                "Later diagnostic block 32–35","Early diagnostic block 0–3"
            });
            construction.SelectedIndex=0;
            add(construction,30);
            add(new Label
            {
                Text="Startup player count (applied on Load)"
            },22);
            players.Name="installationPlayers";
            players.AccessibleName="Startup player count";
            players.Minimum=0;
            players.Maximum=4;
            add(players,30);
            load.Name="installationLoad";
            load.Text="Load layout / player count";
            load.AccessibleName=load.Text;
            add(load,36);
            initialize.Name="installationInitialize";
            initialize.Text="Run recovered startup pass (once)";
            initialize.AccessibleName=initialize.Text;
            add(initialize,36);
            applied.Name="installationApplied";
            applied.AccessibleName="Applied construction and startup results";
            add(applied,112);
            add(new Label
            {
                Text="Copied fleet / inventory lookup anchor"
            },22);
            fleet.Name="installationFleet";
            fleet.DropDownStyle=ComboBoxStyle.DropDownList;
            fleet.Items.AddRange(new object[]
            {
                32,33,34,35
            });
            fleet.SelectedIndex=0;
            fleet.AccessibleName="Copied installation fleet";
            add(fleet,30);
            add(new Label
            {
                Text="Unit slots in selected fleet"
            },22);
            members.Name="installationMembers";
            members.AccessibleName="Installation unit slots";
            add(members,84);
            Action<Button,string,string> button=delegate(Button b,string name,string text)
            {
                b.Name=name;
                b.Text=text;
                b.AccessibleName=text;
                add(b,36);
            };
            var yard=new Button();
            button(yard,"installationYard","Paired yard study…");
            yard.Click+=delegate
            {
                try
                {
                    using(var child=new PracticeYardStudy(this.programs))child.ShowDialog(this);
                }
                catch(IOException e)
                {
                    feedback.Text=e.Message;
                }
                catch(NotSupportedException e)
                {
                    feedback.Text=e.Message;
                }
                finally
                {
                    SuppressHeld();
                }
            };
            button(mixed,"installationMixed","Supply mixed cells / HP 0");
            button(boundary,"installationBoundary","Supply one state 8 / HP length−1");
            button(one,"installationOne","One recovered cell step");
            button(many,"installationMany","Up to 16 recovered cell steps");
            button(reset,"installationReset","Reset original construction copy");
            add(new Label
            {
                Text="A step changes the first state5–8 cell only. 5→2;6→5;7→6;8→7 and HP+1. The leaf stops early when HP equals length. It does not restore equipment records or guarantee a fully repaired unit."
            },106);
            summary.Name="installationSummary";
            summary.AccessibleName="Installation selected unit and recovery state";
            add(summary,156);
            add(new Label
            {
                Text="Look up first referenced source type"
            },22);
            query.Name="installationQuery";
            query.AccessibleName="Inventory lookup type";
            query.DropDownStyle=ComboBoxStyle.DropDownList;
            query.Items.AddRange(new object[]
            {
                19,20,22,23,24,0
            });
            query.SelectedIndex=0;
            add(query,30);
            button(find,"installationFind","Find type in aligned four-fleet block");
            lookup.Name="installationLookup";
            lookup.AccessibleName="Read-only installation lookup result";
            add(lookup,80);
            details.Name="installationDetails";
            details.AccessibleName="Installation unit fields and applied recovery result";
            details.Multiline=true;
            details.ReadOnly=true;
            details.ScrollBars=ScrollBars.Vertical;
            details.BackColor=BackColor;
            details.ForeColor=ForeColor;
            add(details,260);
            var close=new Button
            {
                DialogResult=DialogResult.Cancel
            };
            button(close,"installationClose","Close");
            CancelButton=close;
            feedback.Name="installationFeedback";
            feedback.AccessibleName="Installation study feedback";
            feedback.Dock=DockStyle.Fill;
            feedback.ForeColor=Color.LightSteelBlue;
            layout.Controls.Add(feedback,0,1);
            layout.SetColumnSpan(feedback,2);
            fleet.SelectedIndexChanged+=delegate
            {
                if(!changing)RefreshMembers();
            };
            members.SelectedIndexChanged+=delegate
            {
                if(!changing)RefreshView();
            };
            mixed.Click+=delegate
            {
                Apply(delegate
                {
                    return state.SupplyDamage(SelectedUnit,false);
                },"Supplied mixed source cells and HP0 in this copy only. No recovery step has run.");
            };
            boundary.Click+=delegate
            {
                Apply(delegate
                {
                    return state.SupplyDamage(SelectedUnit,true);
                },"Supplied one state8 cell. One step raises HP to length; a second step then returns without changing state7.");
            };
            one.Click+=delegate
            {
                Recover(1);
            };
            many.Click+=delegate
            {
                Recover(16);
            };
            reset.Click+=delegate
            {
                state=state.Reset();
                lookup.Text="";
                RefreshMembers();
                feedback.Text="Original construction copy restored. Supplied damage and recovery history cleared.";
            };
            EventHandler selectionChanged=delegate
            {
                pending=true;
                RefreshView();
                feedback.Text="Layout/count changed. Load applies them to a fresh construction copy; the current copy still uses the applied values.";
            };
            construction.SelectedIndexChanged+=selectionChanged;
            players.ValueChanged+=selectionChanged;
            load.Click+=delegate
            {
                if(Apply(delegate
                {
                    return new PracticeInstallationStudyState(this.programs,construction.SelectedIndex==0?32:0,(int)players.Value);
                },"Fresh construction layout/count loaded. Startup pass has not run."))
                {
                    pending=false;
                    lookup.Text="";
                    RefreshFleetChoices();
                }
            };
            initialize.Click+=delegate
            {
                Apply(delegate
                {
                    return state.InitializeStartup();
                },"Startup pass applied: copied type19 stocks initialized; eligible stored AWACS data created. Reset or Load permits another study pass.");
            };
            find.Click+=delegate
            {
                int anchor=(int)fleet.SelectedItem,type=(int)query.SelectedItem;
                int result=state.FindType(anchor,type);
                lookup.Text="Aligned block "+(anchor&~3)+"–"+((anchor&~3)+3)+" / type "+type+"\r\n"+(result<0?"No matching referenced unit.":"First match: unit slot "+result+".")+"\r\nRead-only; source filters by type, not fleet kind or owner.";
                feedback.Text="Inventory lookup completed without changing copied state.";
            };
            KeyUp+=delegate(object s,KeyEventArgs e)
            {
                held.Remove(e.KeyCode);
            };
            Deactivate+=delegate
            {
                held.Clear();
            };
            RefreshMembers();
            feedback.Text="Construction is ready. Supply diagnostic cells, then step, or inspect the read-only lookup.";
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(e.KeyCode!=Keys.Enter&&e.KeyCode!=Keys.Space&&e.KeyCode!=Keys.Escape)return;
            var button=ActiveControl as Button;
            if(e.KeyCode!=Keys.Escape&&button==null)return;
            e.Handled=true;
            e.SuppressKeyPress=true;
            if(!held.Add(e.KeyCode))return;
            if(e.KeyCode==Keys.Escape)
            {
                Close();
                return;
            }
            if(button.Enabled)button.PerformClick();
        }
        bool Apply(Func<PracticeInstallationStudyState> action,string message)
        {
            try
            {
                var next=action();
                state=next;
                RefreshView();
                feedback.Text=message;
                return true;
            }
            catch(ArgumentException e)
            {
                feedback.Text=e.Message;
            }
            catch(InvalidDataException e)
            {
                feedback.Text=e.Message;
            }
            catch(NotSupportedException e)
            {
                feedback.Text=e.Message;
            }
            catch(InvalidOperationException e)
            {
                feedback.Text=e.Message;
            }
            catch(IOException e)
            {
                feedback.Text=e.Message;
            }
            return false;
        }
        void RefreshFleetChoices()
        {
            changing=true;
            fleet.Items.Clear();
            for(int n=0;n<4;n++)fleet.Items.Add(state.FirstFleetSlot+n);
            fleet.SelectedIndex=0;
            changing=false;
            RefreshMembers();
        }
        void Recover(int maximum)
        {
            int before=state.RecoveryCalls,beforeChanges=state.ChangedCells;
            Apply(delegate
            {
                return state.Recover(SelectedUnit,maximum);
            },"Recovery call applied to this copy.");
            if(state.RecoveryCalls!=before)
            {
                var r=state.LastRecovery;
                feedback.Text=(state.RecoveryCalls-before)+" call(s) applied / "+(state.ChangedCells-beforeChanges)+" cell(s) changed. Last call: "+(r.Changed?"Cell "+r.GridCell+": "+r.BeforeState+"→"+r.AfterState+"; HP "+r.BeforeHP+"→"+r.AfterHP+".":"Leaf returned without changing a cell. HP equality or no state5–8 cell can stop it.");
            }
        }
        void RefreshMembers()
        {
            if(IsDisposed||Disposing)return;
            int previous=SelectedUnit;
            changing=true;
            members.Items.Clear();
            var ships=state.CopyShipPool();
            int selected=(int)fleet.SelectedItem;
            for(int unit=0;unit<PracticeInstallationStudyState.UnitCount;unit++)if(BitConverter.ToInt16(ships,unit*268+12)==selected)members.Items.Add(unit);
            members.SelectedIndex=members.Items.Contains(previous)?members.Items.IndexOf(previous):members.Items.Count>0?0:-1;
            changing=false;
            RefreshView();
        }
        void RefreshView()
        {
            if(IsDisposed||Disposing)return;
            int unit=SelectedUnit;
            initialize.Enabled=!state.StartupApplied;
            initialize.Text=state.StartupApplied?"Startup pass applied":"Run recovered startup pass (once)";
            applied.Text=(pending?"Selection pending until Load.\r\n":"")+"Applied block "+state.FirstFleetSlot+"–"+(state.FirstFleetSlot+3)+" / startup players "+state.PlayerCount+".\r\n"+(state.StartupApplied?"Stocks written: "+state.LastInitialization.StockReferences+" / stored AWACS created: "+state.LastInitialization.CreatedAircraftFleets.Count+".":"Startup has not run. One supplied installation; no other player bases are created.");
            bool selected=unit>=0;
            mixed.Enabled=boundary.Enabled=one.Enabled=many.Enabled=selected;
            if(!selected)
            {
                summary.Text="Choose a unit.";
                details.Text="";
                canvas.Invalidate();
                return;
            }
            var ships=state.CopyShipPool();
            int at=unit*268,type=BitConverter.ToInt32(ships,at),length=BitConverter.ToInt16(ships,at+14),hp=BitConverter.ToInt16(ships,at+16);
            summary.Text="Unit "+unit+" / source type "+type+"\r\nFleet "+BitConverter.ToInt16(ships,at+12)+" / owner "+BitConverter.ToInt16(ships,at+6)+"\r\nLocal "+BitConverter.ToInt16(ships,at+8)+", "+BitConverter.ToInt16(ships,at+10)+" / length "+length+" / HP "+hp+"\r\nRecovery calls "+state.RecoveryCalls+" / changed cells "+state.ChangedCells+"\r\n"+(hp==length?"HP equals length: the leaf returns early.":"The first cell in states5–8 can change.");
            var text=new StringBuilder();
            text.AppendLine("Constructor"+state.FirstFleetSlot+"–"+(state.FirstFleetSlot+3)+" / origin400,370 / owner0 / raw byte1.");
            text.AppendLine("Original templates, allocation, ammo and grids. Map placement and active support caller remain separate.");
            for(int n=0;n<length;n++)
            {
                text.Append("Equipment row "+n+": ");
                for(int k=0;k<4;k++)text.Append(BitConverter.ToInt32(ships,at+100+(n*4+k)*4)+(k==3?"":" / "));
                text.AppendLine();
            }
            text.AppendLine("AA ammo1/2/3: "+BitConverter.ToInt16(ships,at+198)+" / "+BitConverter.ToInt16(ships,at+200)+" / "+BitConverter.ToInt16(ships,at+202));
            if(type==19)
            {
                text.AppendLine("Inventory stock (source ID:count):");
                for(int n=0;n<35;n++)
                {
                    int amount=BitConverter.ToInt16(ships,at+196+n*2);
                    if(amount!=0)text.Append(n+":"+amount+"  ");
                }
                text.AppendLine();
            }
            for(int n=0;n<4;n++)
            {
                uint link=BitConverter.ToUInt32(ships,at+56+n*4);
                if(link!=0)text.AppendLine("Association link "+n+": normalized "+link.ToString("X8")+".");
            }
            if(state.LastInitialization!=null)foreach(int child in state.LastInitialization.CreatedAircraftFleets)text.AppendLine("Startup created stored kind"+BitConverter.ToInt32(state.CopyFleetPool(),child*492)+" at fleet"+child+" (data only).");
            var r=state.LastRecovery;
            if(r!=null)text.AppendLine("Last call: "+(r.Changed?"cell "+r.GridCell+" / "+r.BeforeState+"→"+r.AfterState:"no changed cell")+" / HP "+r.BeforeHP+"→"+r.AfterHP+".");
            details.Text=text.ToString();
            canvas.Invalidate();
        }
        sealed class InstallationCanvas : Control
        {
            readonly PracticeInstallationStudy owner;
            internal InstallationCanvas(PracticeInstallationStudy owner)
            {
                this.owner=owner;
                DoubleBuffered=true;
            }
            static Color CellColor(int value)
            {
                return value==2?Color.SteelBlue:value==3?Color.FromArgb(35,53,66):value==5?Color.Goldenrod:value==6?Color.DarkOrange:value==7?Color.OrangeRed:value==8?Color.Firebrick:Color.MediumPurple;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if(owner.state==null)return;
                var g=e.Graphics;
                var ships=owner.state.CopyShipPool();
                var fleets=owner.state.CopyFleetPool();
                g.DrawString("Copied compound installation · source grids",owner.Font,Brushes.LightSteelBlue,10,8);
                float size=Math.Min((Width-36)/2f,(Height-180)/2f);
                size=Math.Max(50,size);
                int unit=owner.SelectedUnit;
                for(int n=0;n<4;n++)
                {
                    int slot=owner.state.FirstFleetSlot+n,at=slot*492;
                    float x=10+(n%2)*(size+14),y=50+(n/2)*(size+40),cell=size/10;
                    g.DrawString("Fleet "+slot+" · world "+BitConverter.ToInt16(fleets,at+12)+", "+BitConverter.ToInt16(fleets,at+14),owner.Font,Brushes.LightSteelBlue,x,y-23);
                    for(int gx=0;gx<10;gx++)for(int gy=0;gy<10;gy++)
                    {
                        int index=gx*10+gy,value=BitConverter.ToInt32(fleets,at+64+index*4);
                        var r=new RectangleF(x+gy*cell,y+gx*cell,cell,cell);
                        using(var brush=new SolidBrush(CellColor(value)))g.FillRectangle(brush,r);
                        g.DrawRectangle(Pens.SlateGray,r.X,r.Y,r.Width,r.Height);
                        if(value!=3)g.DrawString(value.ToString(),owner.Font,Brushes.White,r.X+2,r.Y+2);
                    }
                    if(unit>=0&&BitConverter.ToInt16(ships,unit*268+12)==slot)
                    {
                        int p=unit*268,sx=BitConverter.ToInt16(ships,p+8),sy=BitConverter.ToInt16(ships,p+10),length=BitConverter.ToInt16(ships,p+14);
                        using(var pen=new Pen(Color.Gold,3))g.DrawRectangle(pen,x+sy*cell+1,y+sx*cell+1,length*cell-2,cell-2);
                    }
                }
                g.DrawString("Rows = localX; columns = localY. Gold = selected unit.\r\n2 occupied · 3 empty · 5–8 supplied/recovered source states.",owner.Font,Brushes.LightSteelBlue,10,Height-65);
                if(owner.state.LastInitialization!=null&&owner.state.LastInitialization.CreatedAircraftFleets.Count>0)g.DrawString("Startup stored AWACS fleet "+owner.state.LastInitialization.CreatedAircraftFleets[0]+" · no flight updates here",owner.Font,Brushes.PaleTurquoise,10,Height-25);
            }
        }
    }
}
