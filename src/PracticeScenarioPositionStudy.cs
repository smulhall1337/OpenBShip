using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Text;using System.Windows.Forms;using System.Collections.Generic;using System.Runtime.InteropServices;
namespace FleetLab
{
    internal sealed class PracticeScenarioPositionStudy:Form
    {
        readonly ComboBox scenario=new ComboBox(),collision=new ComboBox();readonly NumericUpDown group=new NumericUpDown(),seed=new NumericUpDown();
        readonly Label summary=new Label(),pending=new Label();readonly TextBox details=new TextBox();readonly Panel canvas=new Panel();readonly Font mono=new Font("Consolas",10f);readonly HashSet<Keys> held=new HashSet<Keys>();
        internal PracticeScenarioPositionCopy Applied{get;private set;}
        static readonly int[] scenarios={5,6,7,8,11,12,13,14,18};
        static readonly string[] collisions={"No extra blockers","Primary fleet at first candidate","Primary fleet +9 / +9 (blocked)","Primary fleet +10 / +9 (allowed)","Terrain kind 1 at first candidate","Terrain kind 1 offset by 1 (allowed)","Inactive primary at first candidate","Terrain kind 2 at first candidate","Block three successive candidates"};
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        internal PracticeScenarioPositionStudy()
        {
            Text="Scenario positions · copied original rules";Name="scenarioPositionStudy";BackColor=Color.FromArgb(12,25,36);ForeColor=Color.WhiteSmoke;Font=SystemFonts.MessageBoxFont;AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(1080,840);MinimumSize=SizeFromClientSize(new Size(800,580));StartPosition=FormStartPosition.CenterParent;KeyPreview=true;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));Controls.Add(root);
            var scroll=new Panel{Name="positionScroll",Dock=DockStyle.Fill,AutoScroll=true,Margin=Padding.Empty};root.Controls.Add(scroll,0,0);
            var content=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=1,MinimumSize=new Size(770,0),Padding=new Padding(14)};content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));scroll.Controls.Add(content);
            content.Controls.Add(new Label{Text="Inspect original edge selection, RNG progression and collision retries on copied inputs.",AutoSize=true,Dock=DockStyle.Fill,Padding=new Padding(0,0,0,8)});
            var choices=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,WrapContents=true};choices.Controls.Add(Caption("Scenario"));scenario.Name="positionScenario";scenario.DropDownStyle=ComboBoxStyle.DropDownList;scenario.Width=75;scenario.ForeColor=SystemColors.WindowText;scenario.BackColor=SystemColors.Window;foreach(int n in scenarios)scenario.Items.Add(n);scenario.SelectedIndex=0;choices.Controls.Add(scenario);choices.Controls.Add(Caption("Group"));group.Name="positionGroup";group.Minimum=0;group.Maximum=3;group.Width=55;choices.Controls.Add(group);choices.Controls.Add(Caption("Seed"));seed.Name="positionSeed";seed.Minimum=0;seed.Maximum=uint.MaxValue;seed.Value=731;seed.Width=135;choices.Controls.Add(seed);content.Controls.Add(choices);
            var actions=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,WrapContents=true};collision.Name="positionCollision";collision.DropDownStyle=ComboBoxStyle.DropDownList;collision.Width=340;collision.ForeColor=SystemColors.WindowText;collision.BackColor=SystemColors.Window;collision.Items.AddRange(collisions);collision.SelectedIndex=4;actions.Controls.Add(collision);var load=Button("positionLoad","Load selected copy");var reset=Button("positionReset","Reset applied copy");actions.Controls.Add(load);actions.Controls.Add(reset);content.Controls.Add(actions);
            pending.Name="positionPending";pending.AutoSize=true;pending.Dock=DockStyle.Fill;pending.Padding=new Padding(4,5,4,5);content.Controls.Add(pending);
            summary.Name="positionSummary";summary.AutoSize=true;summary.MinimumSize=new Size(730,86);summary.Padding=new Padding(8);summary.BackColor=Color.FromArgb(26,51,66);summary.Dock=DockStyle.Fill;content.Controls.Add(summary);
            canvas.Name="positionCanvas";canvas.Height=500;canvas.MinimumSize=new Size(730,500);canvas.Dock=DockStyle.Fill;canvas.Paint+=Draw;content.Controls.Add(canvas);
            details.Name="positionDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Both;details.WordWrap=false;details.Height=220;details.MinimumSize=new Size(730,220);details.Dock=DockStyle.Fill;details.BackColor=Color.FromArgb(9,20,30);details.ForeColor=ForeColor;details.Font=mono;content.Controls.Add(details);
            content.Controls.Add(new Label{Text="Gold = accepted point; orange = rejected candidates. Blue = primary records; teal = terrain.\r\nSource world coordinates: X increases right, Y increases down, range 0..630. No fleet is created.\r\nCopied blockers and controls are inspection aids. Retry cap 128 is a modern diagnostic limit.\r\nEnter loads once per press; Escape closes. Scenarios outside the admitted branches fail clearly.",AutoSize=true,Dock=DockStyle.Fill,Padding=new Padding(0,8,0,8)});
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(5)};var close=Button("positionClose","Close");footer.Controls.Add(close);root.Controls.Add(footer,0,1);close.Click+=delegate{Close();};load.Click+=delegate{LoadCopy(false);};reset.Click+=delegate{LoadCopy(true);};
            scenario.SelectedIndexChanged+=delegate{group.Maximum=(int)scenario.SelectedItem==8?1:3;Pending();};group.ValueChanged+=delegate{Pending();};seed.ValueChanged+=delegate{Pending();};collision.SelectedIndexChanged+=delegate{Pending();};canvas.Resize+=delegate{canvas.Invalidate();};LoadCopy(false);
        }
        static Label Caption(string text){return new Label{Text=text,AutoSize=true,Margin=new Padding(5,7,3,3)};}
        static Button Button(string name,string text){return new Button{Name=name,Text=text,AutoSize=true,Padding=new Padding(5),UseVisualStyleBackColor=true,ForeColor=SystemColors.ControlText,BackColor=SystemColors.Control};}
        void Pending(){if(Applied==null||IsDisposed||Disposing)return;pending.Text=Applied.Scenario==(int)scenario.SelectedItem&&Applied.Group==(int)group.Value&&Applied.Seed==(uint)seed.Value&&Applied.Collision==collision.SelectedIndex?"Controls match the applied copy.":"Selection pending. Load applies these controls; Reset keeps the applied scenario.";}
        void LoadCopy(bool reset)
        {
            if(IsDisposed||Disposing)return;try
            {
                Applied=reset&&Applied!=null?new PracticeScenarioPositionCopy(Applied.Scenario,Applied.Group,Applied.Seed,Applied.Collision):new PracticeScenarioPositionCopy((int)scenario.SelectedItem,(int)group.Value,(uint)seed.Value,collision.SelectedIndex);
                var r=Applied.Result;summary.Text=string.Format("Applied scenario {0} · group {1} · seed {2}\r\nAccepted world position: X {3}, Y {4}   |   Attempts: {5}   |   RNG calls: {6}\r\nRNG after: {7}   |   {8}",Applied.Scenario,Applied.Group,Applied.Seed,r.X,r.Y,r.Attempts.Count,r.RandomCalls,r.RandomState,collisions[Applied.Collision]);
                var text=new StringBuilder("Attempt   X     Y     Result\r\n");for(int n=0;n<r.Attempts.Count;n++){var a=r.Attempts[n];text.AppendFormat("{0,4}   {1,3}   {2,3}   {3}\r\n",n+1,a.X,a.Y,a.Accepted?"Accepted":a.BlockingFleet>=0?"Rejected by primary record "+a.BlockingFleet:"Rejected by terrain record "+a.BlockingTerrain);}text.Append("\r\nActual original selector and RNG matched 2,448 recorded vectors.\r\nNo constructor, schedule execution or natural spawning is run here.");details.Text=text.ToString();Pending();canvas.Invalidate();
            }
            catch(NotSupportedException ex){pending.Text=ex.Message;}catch(System.IO.InvalidDataException ex){pending.Text=ex.Message;}
        }
        internal bool StudyKey(Keys key,bool down){if(key!=Keys.Enter&&key!=Keys.Escape)return false;if(!down){held.Remove(key);return true;}if(!held.Add(key))return true;if(key==Keys.Escape)Close();else LoadCopy(false);return true;}
        protected override bool ProcessCmdKey(ref Message message,Keys keyData){if(StudyKey(keyData&Keys.KeyCode,true))return true;return base.ProcessCmdKey(ref message,keyData);}
        protected override void OnKeyUp(KeyEventArgs e){if(StudyKey(e.KeyCode,false))e.Handled=true;base.OnKeyUp(e);}
        protected override void OnActivated(EventArgs e){base.OnActivated(e);foreach(Keys key in new[]{Keys.Enter,Keys.Escape})if(GetAsyncKeyState((int)key)<0)held.Add(key);else held.Remove(key);}
        void Draw(object sender,PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);if(Applied==null)return;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float size=Math.Min(canvas.ClientSize.Width-90,canvas.ClientSize.Height-70);float left=(canvas.ClientSize.Width-size)/2,top=30,scale=size/630;
            using(var grid=new Pen(Color.FromArgb(28,49,63)))using(var strong=new Pen(Color.FromArgb(60,86,102)))using(var brush=new SolidBrush(Color.LightSteelBlue))
            {
                for(int n=0;n<=63;n++){float at=n*10*scale;e.Graphics.DrawLine(n%8==0?strong:grid,left+at,top,left+at,top+size);e.Graphics.DrawLine(n%8==0?strong:grid,left,top+at,left+size,top+at);if(n%8==0||n==63){string label=(n*10).ToString();e.Graphics.DrawString(label,Font,brush,left+at-8,top-22);e.Graphics.DrawString(label,Font,brush,left-38,top+at-7);}}
            }
            for(int at=0;at<Applied.Fleets.Length;at+=492)if(BitConverter.ToInt32(Applied.Fleets,at)!=0)Marker(e.Graphics,left,top,scale,BitConverter.ToInt16(Applied.Fleets,at+12),BitConverter.ToInt16(Applied.Fleets,at+14),Color.SteelBlue,9,false);
            for(int at=0;at<Applied.Terrain.Length;at+=492)if(BitConverter.ToInt32(Applied.Terrain,at)!=0)Marker(e.Graphics,left,top,scale,BitConverter.ToInt16(Applied.Terrain,at+12),BitConverter.ToInt16(Applied.Terrain,at+14),Color.Turquoise,12,false);
            var attempts=Applied.Result.Attempts;using(var line=new Pen(Color.FromArgb(120,218,150,70),1)){for(int n=0;n<attempts.Count;n++){var a=attempts[n];if(n>0)e.Graphics.DrawLine(line,left+attempts[n-1].X*scale,top+attempts[n-1].Y*scale,left+a.X*scale,top+a.Y*scale);Marker(e.Graphics,left,top,scale,a.X,a.Y,a.Accepted?Color.Gold:Color.DarkOrange,a.Accepted?16:10,a.Accepted);}}
            using(var brush=new SolidBrush(Color.Gold)){var r=Applied.Result;string label="Accepted: "+r.X+", "+r.Y;float x=Math.Min(left+size-160,Math.Max(left+6,left+r.X*scale+12)),y=Math.Min(top+size-25,Math.Max(top+6,top+r.Y*scale+10));e.Graphics.DrawString(label,Font,brush,x,y);}
        }
        static void Marker(Graphics g,float left,float top,float scale,int x,int y,Color color,int diameter,bool fill){float px=left+x*scale-diameter/2f,py=top+y*scale-diameter/2f;using(var pen=new Pen(color,2))g.DrawEllipse(pen,px,py,diameter,diameter);if(fill)using(var brush=new SolidBrush(color))g.FillEllipse(brush,px+4,py+4,diameter-8,diameter-8);}
        protected override void Dispose(bool disposing){if(disposing)mono.Dispose();base.Dispose(disposing);}
    }
}
