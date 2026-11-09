using System;
using System.Drawing;
using System.Windows.Forms;
namespace FleetLab {
    // Modern confirmation around the reconstructed battle -> title destination.
    // Owns no game/audio state. MainForm validates the captured battle identity.
    public sealed class PauseActions : UserControl {
        internal readonly Button AskButton=new Button(),CancelButton=new Button(),EndButton=new Button();
        readonly Label question=new Label();
        readonly FlowLayoutPanel choices=new FlowLayoutPanel();
        bool disposed;
        public event Action ConfirmationOpened,ConfirmationCanceled,EndConfirmed;
        internal bool Confirming {get;private set;}
        public PauseActions(){
            Dock=DockStyle.Fill;BackColor=Color.FromArgb(20,36,52);Height=76;
            AskButton.Text="End battle...";AskButton.Dock=DockStyle.Top;AskButton.Height=34;
            AskButton.AccessibleDescription="End this battle and return to the original title menu. Confirmation required.";
            question.Text="End this battle and return to title?";question.Dock=DockStyle.Top;question.Height=30;question.Font=new Font("Segoe UI",9);
            choices.Dock=DockStyle.Bottom;choices.Height=38;choices.WrapContents=false;
            CancelButton.Text="Keep battle";EndButton.Text="End battle";
            foreach(var b in new[]{AskButton,CancelButton,EndButton}){b.FlatStyle=FlatStyle.Flat;b.BackColor=Color.FromArgb(31,56,75);b.ForeColor=Color.WhiteSmoke;}
            foreach(var b in new[]{CancelButton,EndButton}){b.Width=105;b.Height=32;choices.Controls.Add(b);}
            Controls.Add(AskButton);Controls.Add(question);Controls.Add(choices);
            AskButton.Click+=delegate{if(disposed||!Enabled||Confirming)return;Confirming=true;ShowState();if(ConfirmationOpened!=null)ConfirmationOpened();CancelButton.Focus();};
            CancelButton.Click+=delegate{CancelConfirmation();AskButton.Focus();};
            EndButton.Click+=delegate{if(disposed||!Enabled||!Confirming)return;if(EndConfirmed!=null)EndConfirmed();};
            ShowState();
        }
        void ShowState(){AskButton.Visible=!Confirming;question.Visible=choices.Visible=Confirming;}
        internal void CancelConfirmation(){if(disposed)return;bool notify=Confirming;Confirming=false;ShowState();if(notify&&ConfirmationCanceled!=null)ConfirmationCanceled();}
        internal void SetAvailable(bool available){if(!available)CancelConfirmation();Enabled=Visible=available;}
        protected override void Dispose(bool disposing){if(disposing&&!disposed){CancelConfirmation();disposed=true;}base.Dispose(disposing);}
    }
}
