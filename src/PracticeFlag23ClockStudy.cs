using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FleetLab
{
    internal sealed class PracticeFlag23ClockStudy : Form
    {
        readonly string programs;
        readonly PracticeAttachmentPatrolStudyBasis basis;
        readonly ComboBox preset=new ComboBox();
        readonly Button load=new Button(),reset=new Button(),one=new Button(),all=new Button();
        readonly Label summary=new Label(),feedback=new Label(),next=new Label();
        readonly TextBox details=new TextBox();
        readonly Label[,] clockCells=new Label[4,3];
        readonly Font mono=new Font("Consolas",10f);
        readonly HashSet<Keys> held=new HashSet<Keys>();
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        PracticeFlag23ClockStudyState state;
        internal PracticeFlag23ClockStudyState StudyState{get{return state;}}

        internal PracticeFlag23ClockStudy(string programs,PracticeMapStartup startup)
        {
            this.programs=programs;basis=PracticeAttachmentPatrolStudyBasis.Create(programs,startup);
            Text="Clock / aircraft policy · copied original rules";Name="clockPolicyStudy";
            BackColor=Color.FromArgb(12,25,36);ForeColor=Color.WhiteSmoke;Font=SystemFonts.MessageBoxFont;
            AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(1080,790);MinimumSize=SizeFromClientSize(new Size(800,580));StartPosition=FormStartPosition.CenterParent;KeyPreview=true;
            var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Name="clockPolicyScroll"};
            var content=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=1,Padding=new Padding(16),MinimumSize=new Size(780,0)};
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));scroll.Controls.Add(content);Controls.Add(scroll);
            var heading=new Label{Text="Copied clock header → whole AI driver → aircraft policy",AutoSize=true,Padding=new Padding(0,0,0,8)};content.Controls.Add(heading);
            var choices=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,WrapContents=true,Margin=new Padding(0,0,0,8)};
            choices.Controls.Add(new Label{Text="Recorded input",AutoSize=true,Padding=new Padding(0,8,8,0)});
            preset.Name="clockPolicyPreset";preset.DropDownStyle=ComboBoxStyle.DropDownList;preset.Width=410;preset.BackColor=Color.White;preset.ForeColor=Color.Black;foreach(string name in PracticeFlag23ClockStudyState.Presets)preset.Items.Add(name);preset.SelectedIndex=0;choices.Controls.Add(preset);
            Configure(load,"clockPolicyLoad","Load selected copy");Configure(reset,"clockPolicyReset","Reset applied copy");choices.Controls.Add(load);choices.Controls.Add(reset);content.Controls.Add(choices);
            summary.Name="clockPolicySummary";summary.Dock=DockStyle.Fill;summary.AutoSize=true;summary.Padding=new Padding(8);summary.BackColor=Color.FromArgb(26,51,66);summary.MinimumSize=new Size(740,68);content.Controls.Add(summary);
            var table=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=4,RowCount=5,Margin=new Padding(0,12,0,8),BackColor=Color.FromArgb(22,43,57)};
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));for(int n=0;n<3;n++)table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
            string[] headers={"Clock field (raw uint32)","Before frame","Body sees","After frame"};
            for(int col=0;col<4;col++)table.Controls.Add(Cell(headers[col],true),col,0);
            string[] fields={"Previous","Current sample","Game clock","Delta"};
            for(int row=0;row<4;row++){table.Controls.Add(Cell(fields[row],false),0,row+1);for(int col=0;col<3;col++){clockCells[row,col]=Cell("—",false);clockCells[row,col].Name="clockPolicyClock"+row+"_"+col;table.Controls.Add(clockCells[row,col],col+1,row+1);}}
            for(int row=0;row<5;row++)table.RowStyles.Add(new RowStyle(SizeType.Absolute,34));content.Controls.Add(table);
            next.Name="clockPolicyNext";next.AutoSize=true;next.Dock=DockStyle.Fill;next.Padding=new Padding(0,4,0,4);content.Controls.Add(next);
            var actions=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,Margin=new Padding(0,4,0,4)};
            Configure(one,"clockPolicyOne","1 supplied frame");Configure(all,"clockPolicyAll","Remaining recorded frames");actions.Controls.Add(one);actions.Controls.Add(all);content.Controls.Add(actions);
            feedback.Name="clockPolicyFeedback";feedback.AutoSize=true;feedback.Dock=DockStyle.Fill;feedback.Padding=new Padding(0,5,0,8);feedback.MinimumSize=new Size(740,38);content.Controls.Add(feedback);
            details.Name="clockPolicyDetails";details.ReadOnly=true;details.Multiline=true;details.ScrollBars=ScrollBars.Vertical;details.WordWrap=true;details.Dock=DockStyle.Fill;details.MinimumSize=new Size(740,225);details.BackColor=Color.FromArgb(9,20,30);details.ForeColor=ForeColor;details.Font=mono;details.BorderStyle=BorderStyle.FixedSingle;content.Controls.Add(details);
            var footer=new Panel{Dock=DockStyle.Bottom,Height=48,Padding=new Padding(12,6,12,6),BackColor=BackColor};var close=new Button{Dock=DockStyle.Right,Width=130,DialogResult=DialogResult.Cancel};Configure(close,"clockPolicyClose","Close");close.Click+=delegate{Close();};footer.Controls.Add(close);Controls.Add(footer);scroll.BringToFront();CancelButton=close;
            load.Click+=delegate{LoadCopy(preset.SelectedIndex);};reset.Click+=delegate{LoadCopy(state.Preset);};one.Click+=delegate{Advance(false);};all.Click+=delegate{Advance(true);};preset.SelectedIndexChanged+=delegate{Sync();};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Activated+=delegate{SampleHeld();};Deactivate+=delegate{SampleHeld();};
            LoadCopy(0);
        }
        void SampleHeld(){if(IsDisposed||Disposing)return;foreach(Keys key in new[]{Keys.Enter,Keys.Space,Keys.Escape}){if((GetAsyncKeyState((int)key)&0x8000)!=0)held.Add(key);else held.Remove(key);}}
        bool GateKey(Keys key)
        {
            if(IsDisposed||Disposing)return true;
            if(key!=Keys.Enter&&key!=Keys.Space&&key!=Keys.Escape)return false;
            var button=ActiveControl as Button;if(key!=Keys.Escape&&button==null)return false;
            if(held.Add(key)){if(key==Keys.Escape)Close();else if(button.Enabled)button.PerformClick();}return true;
        }
        protected override void OnKeyDown(KeyEventArgs e){if(e.Modifiers==Keys.None&&GateKey(e.KeyCode)){e.Handled=e.SuppressKeyPress=true;return;}base.OnKeyDown(e);}
        protected override bool ProcessDialogKey(Keys keyData){if((keyData&Keys.Modifiers)==Keys.None&&GateKey(keyData&Keys.KeyCode))return true;return base.ProcessDialogKey(keyData);}
        Label Cell(string text,bool header)
        {
            return new Label{Text=text,AutoSize=false,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(8,0,0,0),Font=header?Font:mono,BackColor=header?Color.FromArgb(33,66,83):Color.FromArgb(22,43,57),ForeColor=ForeColor};
        }
        static void Configure(Button button,string name,string text){button.Name=name;button.Text=text;button.AutoSize=true;button.MinimumSize=new Size(140,34);button.Padding=new Padding(7,2,7,2);button.UseVisualStyleBackColor=false;button.BackColor=Color.FromArgb(29,56,72);button.ForeColor=Color.WhiteSmoke;button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderColor=Color.FromArgb(100,130,150);button.EnabledChanged+=delegate{if(button.IsDisposed||button.Disposing)return;button.BackColor=button.Enabled?Color.FromArgb(29,56,72):Color.LightGray;button.ForeColor=button.Enabled?Color.WhiteSmoke:Color.Black;};}
        static string Value(uint n){return n>int.MaxValue?n+" ("+unchecked((int)n)+")":n.ToString();}
        static uint Field(OriginalFrameClock c,int row){return row==0?c.PreviousMilliseconds:row==1?c.CurrentMilliseconds:row==2?c.GameMilliseconds:c.DeltaMilliseconds;}
        void LoadCopy(int selected)
        {
            var prepared=new PracticeFlag23ClockStudyState(programs,basis,selected);state=prepared;
            feedback.Text="Prepared a fresh copied input. No clock frame has run.";Sync();
        }
        void Advance(bool remaining)
        {
            if(!state.CanAdvance)return;
            try
            {
                var applied=state;do{applied=applied.Advance();}while(remaining&&applied.CanAdvance);state=applied;
                feedback.Text=state.CanAdvance?"Applied one supplied frame. The body used the preceding delta.":"Recorded window complete. Reset or Load a copy to compare again.";Sync();
            }
            catch(InvalidDataException e){feedback.Text="Copy preserved: "+e.Message;}
            catch(NotSupportedException e){feedback.Text="Copy preserved: "+e.Message;}
        }
        void Sync()
        {
            if(IsDisposed||Disposing||state==null)return;
            bool pending=preset.SelectedIndex!=state.Preset;
            summary.Text="Applied: "+PracticeFlag23ClockStudyState.Presets[state.Preset]+"\r\nFrames "+state.Frames+" / "+state.WindowFrames+"     Child 23: "+(state.Kind==6?"stored (kind 6)":"active (kind 3)")+"     Timer "+state.Timer+"\r\nActivations "+state.Activations+"     UI requests "+state.UiRequests+"     Selected waypoint "+state.State.SelectedWaypoint+"     Neutral passes "+state.NeutralPasses;
            var last=state.LastUpdate;
            for(int row=0;row<4;row++)for(int col=0;col<3;col++)
            {
                OriginalFrameClock clock=last==null?(col==0?state.State.Clock:null):col==0?last.Before.Clock:col==1?last.BodyClock:state.State.Clock;
                clockCells[row,col].Text=clock==null?"—":Value(Field(clock,row));
            }
            next.Text=(state.CanAdvance?"Next supplied sample: "+Value(state.NextSample)+"; phase "+state.NextPhase:"No more recorded frames in this input.")+(pending?"   Pending preset requires Load.":"");
            one.Enabled=all.Enabled=state.CanAdvance;
            var text=new StringBuilder();
            text.AppendLine("This view runs reconstructed rules on an independent copy, not a live Practice battle.");
            text.AppendLine("The original header samples time before its body. The body sees the PRECEDING delta; only afterward does the header compute the next delta. A zero raw interval resets current/game/delta to 1. Wrapping inputs are deliberate comparison cases.");
            text.AppendLine();text.AppendLine("Whole driver / policy: scenario 1; empty events; raw owner-pass control 0; no active unowned task dispatcher. Parent 15, owner 0, policy 2; child 23 starts stored. No visible targets or active navigation. Sample values and phases 31 onward are supplied inputs.");
            text.AppendLine("Caller word 0; incoming frame word 0x2468ACE0. These diagnostic words do not establish the original caller's natural values.");
            if(last!=null)
            {
                int oldTimer=BitConverter.ToInt32(last.Before.Backing.CopyTaskRecord(23),252);
                text.AppendLine();text.AppendLine("Last body: timer "+oldTimer+" → "+state.Timer+"; delta "+Value(last.BodyClock.DeltaMilliseconds)+"; source UI requests "+last.Driver.RouteUiRequests+"; caller scratch 0x"+unchecked((uint)last.Driver.CallerScratchAfter).ToString("X8")+".");
                text.AppendLine("The exact-threshold input reaches 5000 on its SECOND frame, changes kind 6 to 3, requests one UI refresh and invalidates the selected waypoint to -1. Its third frame keeps -1 and requests no additional refresh.");
            }
            text.AppendLine();text.AppendLine("The table is modern diagnostic presentation. Full original before/after pools and clock fields support these fourteen recorded frames. Aircraft do not fly here; no movement, renderer, movies, audio, network, missions or natural application pacing runs. Controls stop at this preset's recorded limit. No automatic timer advances the copy.");
            details.Text=text.ToString();
        }
        protected override void Dispose(bool disposing){if(disposing)mono.Dispose();base.Dispose(disposing);}
    }
}
