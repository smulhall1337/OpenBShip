using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace FleetLab
{
    public sealed class PracticeProducedMissStudy:Form
    {
        readonly string programs;readonly PracticeProducedFlight boundary;
        readonly ComboBox mode=new ComboBox();readonly Button load=new Button(),reset=new Button(),step=new Button(),batch=new Button(),transition=new Button(),finish=new Button();
        readonly Label summary=new Label(),feedback=new Label();readonly ListBox projectiles=new ListBox(),effects=new ListBox();readonly TextBox details=new TextBox();readonly MissCanvas canvas;readonly HashSet<Keys> held=new HashSet<Keys>();
        PracticeProducedMisses initial,state;bool changing;
        internal PracticeProducedMisses StudyState {get{return state;}}
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        public PracticeProducedMissStudy(string programs,PracticeProducedFlight boundary)
        {
            this.programs=programs;this.boundary=boundary;Name="producedMissStudy";Text="Original-map weapon22 · water misses and effects";StartPosition=FormStartPosition.CenterParent;Size=new Size(1180,860);MinimumSize=new Size(980,740);Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(12,23,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(8)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,370));Controls.Add(layout);
            canvas=new MissCanvas(this){Name="producedMissCanvas",Dock=DockStyle.Fill};layout.Controls.Add(canvas,0,0);var scroll=new Panel{Name="producedMissScroll",Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,1,0);
            var side=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4)};scroll.Controls.Add(side);
            Action<Control> style=delegate(Control c){c.BackColor=Color.FromArgb(28,55,76);c.ForeColor=ForeColor;var b=c as Button;if(b!=null){b.UseVisualStyleBackColor=false;b.FlatStyle=FlatStyle.Flat;b.EnabledChanged+=delegate{if(b.IsDisposed||b.Disposing)return;b.BackColor=b.Enabled?Color.FromArgb(28,55,76):Color.LightGray;b.ForeColor=b.Enabled?ForeColor:Color.Black;};}};
            Action<Control,int> add=delegate(Control c,int height){c.Width=336;c.Height=height;c.Margin=new Padding(0,3,0,3);if(c is Button||c is ComboBox||c is ListBox)style(c);side.Controls.Add(c);};
            add(new Label{Text="Two natural-map weapon22 shots through water impacts and effect expiry. Starts at the pre-impact boundary. Blue rings are schematic; no ship damage, movie or audio runs."},80);
            mode.Name="producedMissMode";mode.DropDownStyle=ComboBoxStyle.DropDownList;mode.DropDownWidth=500;mode.Items.AddRange(new object[]{"First visit: exact deadline 35211","First visit: launch +40 lattice 35225","Diagnostic first visit: boundary +40 =35250"});mode.SelectedIndex=0;add(mode,30);
            load.Name="producedMissLoad";load.Text="Load selected timing copy from boundary";add(load,34);reset.Name="producedMissReset";reset.Text="Reset applied timing copy";add(reset,34);
            Action<Button,Button> row=delegate(Button a,Button b){var panel=new FlowLayoutPanel{FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Padding=Padding.Empty};foreach(var button in new[]{a,b}){style(button);button.Width=162;button.Height=38;button.Margin=new Padding(0,0,6,0);panel.Controls.Add(button);}add(panel,40);};
            step.Name="producedMissStep";step.Text="1 update (+40)";batch.Name="producedMissBatch";batch.Text="Up to 32 updates";row(step,batch);transition.Name="producedMissTransition";transition.Text="Next transition";finish.Name="producedMissFinish";finish.Text="Finish copied history";row(transition,finish);
            summary.Name="producedMissSummary";add(summary,148);feedback.Name="producedMissFeedback";add(feedback,82);add(new Label{Text="Active projectile records"},22);projectiles.Name="producedMissObjects";projectiles.HorizontalScrollbar=true;add(projectiles,76);add(new Label{Text="Active water effects (schematic blue rings)"},22);effects.Name="producedMissEffects";effects.HorizontalScrollbar=true;add(effects,76);
            details.Name="producedMissDetails";details.Multiline=true;details.ReadOnly=true;details.ScrollBars=ScrollBars.Vertical;details.BackColor=BackColor;details.ForeColor=ForeColor;add(details,300);
            var close=new Button{Name="producedMissClose",Text="Close"};add(close,34);close.Click+=delegate{Close();};
            mode.SelectedIndexChanged+=delegate{if(state!=null)Feedback("Timing selection pending. Load applies it; Reset keeps the applied mode.");};load.Click+=delegate{LoadCopy();};reset.Click+=delegate{if(initial!=null){state=initial;Feedback("Applied boundary/timing copy restored; pending timing selection is unchanged.");Sync();}};
            step.Click+=delegate{Advance(0);};batch.Click+=delegate{Advance(1);};transition.Click+=delegate{Advance(2);};finish.Click+=delegate{Advance(3);};
            projectiles.SelectedIndexChanged+=delegate{if(changing)return;changing=true;if(projectiles.SelectedIndex>=0)effects.SelectedIndex=-1;changing=false;RefreshDetails();};effects.SelectedIndexChanged+=delegate{if(changing)return;changing=true;if(effects.SelectedIndex>=0)projectiles.SelectedIndex=-1;changing=false;RefreshDetails();};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{foreach(Keys k in new[]{Keys.Enter,Keys.Space})if((GetAsyncKeyState((int)k)&0x8000)!=0)held.Add(k);};LoadCopy();
        }
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){var b=ActiveControl as Button;if(b!=null){e.Handled=e.SuppressKeyPress=true;if(held.Add(e.KeyCode)&&b.Enabled)b.PerformClick();return;}}base.OnKeyDown(e);}
        void Feedback(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.LightSteelBlue;feedback.Text=text;}
        void Reject(string text){if(IsDisposed||Disposing)return;feedback.ForeColor=Color.Orange;feedback.Text="Rejected; applied copy preserved. "+text;Sync();}
        void LoadCopy(){try{var next=PracticeProducedMisses.Create(programs,boundary,mode.SelectedIndex);state=initial=next;Feedback("Copied boundary loaded. Requests stay empty; only these projectiles and their effects advance.");Sync();}catch(ArgumentException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}catch(IOException e){Reject(e.Message);}catch(UnauthorizedAccessException e){Reject(e.Message);}}
        void Advance(int action)
        {
            if(state==null)return;try{state=action==0?state.Step():action==1?state.UpTo(32):action==2?state.NextTransition():state.Finish();Feedback(state.Complete?"Copied projectile/effect history complete. Both shots resolved as water misses; the battle remains outside this copy.":state.ResolvedMisses>0?"Water miss resolved; source effect counters continue. Remaining firing requests stay held.":"Numeric flight updated; no impact yet.");Sync();}catch(InvalidOperationException e){Reject(e.Message);}catch(NotSupportedException e){Reject(e.Message);}catch(InvalidDataException e){Reject(e.Message);}
        }
        void Sync()
        {
            if(IsDisposed||Disposing)return;step.Enabled=batch.Enabled=transition.Enabled=finish.Enabled=state!=null&&!state.Complete;reset.Enabled=state!=null;
            if(state==null){summary.Text="No admitted weapon22 pre-impact boundary loaded.";return;}
            step.Text=state.Updates==0?"Run first-clock diagnostic":"1 update (+40)";
            summary.Text="Applied timing "+state.Mode+" · raw clock "+state.Clock+"\r\nUpdates "+state.Updates+" · water misses "+state.ResolvedMisses+"\r\nProjectiles "+state.Projectiles.Count+" · effects "+state.Effects.Count+
                "\r\nRNG "+state.RandomState+" · calls "+state.RandomCalls+"\r\nMovie counter "+state.MovieCounter+" · stopped slot "+state.LastStoppedAtRecord+"\r\nHeld requests 0 / 0 · ammo 7 / 7\r\n"+(state.Complete?"Copied history finished.":"Manual source clock; no automatic pacing.");
            int selectedP=projectiles.SelectedItem==null?-1:((PracticeMemberProjectile)projectiles.SelectedItem).Slot,selectedE=effects.SelectedItem==null?-1:((PracticeMemberMissEffect)effects.SelectedItem).Slot;
            changing=true;projectiles.BeginUpdate();effects.BeginUpdate();projectiles.Items.Clear();effects.Items.Clear();foreach(var p in state.Projectiles){int at=projectiles.Items.Add(p);if(p.Slot==selectedP)projectiles.SelectedIndex=at;}foreach(var e in state.Effects){int at=effects.Items.Add(e);if(e.Slot==selectedE)effects.SelectedIndex=at;}
            if(projectiles.SelectedIndex<0&&effects.SelectedIndex<0){if(effects.Items.Count>0)effects.SelectedIndex=0;else if(projectiles.Items.Count>0)projectiles.SelectedIndex=0;}projectiles.EndUpdate();effects.EndUpdate();changing=false;RefreshDetails();
        }
        void RefreshDetails()
        {
            if(state==null||IsDisposed||Disposing)return;var p=projectiles.SelectedItem as PracticeMemberProjectile;var e=effects.SelectedItem as PracticeMemberMissEffect;
            details.Text=(p!=null?p+"\r\nCurrent "+p.X+","+p.Y+"; local destination "+p.OffsetX+","+p.OffsetY+"; deadline "+p.Deadline:e!=null?e+"\r\nType "+e.Type+" · sprite bank "+e.SpriteBank:"No active projectile or effect remains.")+
                "\r\nCyan/orange = live projectile positions; blue rings = schematic water effects; white ring = viewed record. Effect size is a modern inspection aid, not recovered sprite rendering."+
                "\r\nThree supplied first-clock visits start at35211,35225 or35250; subsequent visits use +40. Prior delta stays40. Impact callback runs before the original quotient-zero return. Late callbacks can still update retired coordinate bytes."+
                "\r\nEffect aging follows impact in the same update. Weapon22 water callbacks consume no random draws; RNG and movie counter hold. Both copied target cells are water; no defense, hit, damage or destruction is reached."+
                "\r\nRequests remain empty and ammo stays at 7 / 7 after launch. No further consumer, fleet motion, AI policy or full battle progression runs. Only the two verified water cells are admitted; ship hits remain outside this study. Reset/load/close preserve the parent launch, active battle and settings.";canvas.Invalidate();
        }
        sealed class MissCanvas:Control
        {
            readonly PracticeProducedMissStudy owner;internal MissCanvas(PracticeProducedMissStudy owner){this.owner=owner;DoubleBuffered=true;ResizeRedraw=true;}
            protected override void OnPaint(PaintEventArgs args)
            {
                base.OnPaint(args);if(owner.state==null)return;var state=owner.state;var p=owner.projectiles.SelectedItem as PracticeMemberProjectile;var selected=owner.effects.SelectedItem as PracticeMemberMissEffect;var fleets=state.CopyFleetPool();
                var point=PracticeMemberFlightDiagram.Draw(args.Graphics,owner.Font,ClientRectangle,fleets,state.Launch.Projectiles,state.Projectiles,p==null?-1:p.Slot,true,"Original-map weapon22 water impacts · blue rings are schematic effects.","Two copied shots only; requests stay empty.\r\nEffect counters are recovered; ring geometry is a modern inspection aid.");if(point==null)return;
                foreach(var effect in state.Effects)
                {
                    var where=point(BitConverter.ToInt16(fleets,effect.TargetFleet*492+12)+effect.X,BitConverter.ToInt16(fleets,effect.TargetFleet*492+14)+effect.Y);float radius=Math.Max(4,Math.Min(20,4+effect.Counter*.4f));using(var pen=new Pen(Color.LightSkyBlue,2))args.Graphics.DrawEllipse(pen,where.X-radius,where.Y-radius,2*radius,2*radius);args.Graphics.DrawString("E"+effect.Slot,owner.Font,Brushes.LightSkyBlue,where.X+radius,where.Y+radius);
                    if(selected!=null&&selected.Slot==effect.Slot)using(var pen=new Pen(Color.White,2))args.Graphics.DrawEllipse(pen,where.X-radius-3,where.Y-radius-3,2*radius+6,2*radius+6);
                }
            }
        }
    }
}

