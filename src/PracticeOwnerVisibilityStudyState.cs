using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace FleetLab
{
    // Applied copied inputs, separate from the pure producer. Presets map to
    // original576 cases; synthetic records are visibility-only, not world rosters.
    public sealed class PracticeOwnerVisibilityStudyState
    {
        static readonly byte[] synthetic=MakeSynthetic();
        readonly PracticeOwnerVisibilityRules rules;readonly byte[] before;
        PracticeOwnerVisibilityResult result;
        public int Preset {get;private set;}public int Owner {get;private set;}public int Mode {get;private set;}
        public short LocalOwner {get;private set;}public int SelectedWeapon {get;private set;}public int SelectedFleet {get;private set;}
        public int ObserverKind {get;private set;}public int RadiusProfile {get;private set;}public int Distance {get;private set;}public int Packed {get;private set;}
        public bool Executed {get{return result!=null;}}public bool Skipped {get{return result!=null&&result.Skipped;}}
        public byte[] CopyInputFleetPool(){return (byte[])before.Clone();}
        public byte[] CopyFleetPool(){return result==null?(byte[])before.Clone():result.CopyFleetPool();}
        public PracticeOwnerFleetVisibility Fleet(int slot){if(slot<0||slot>=250)throw new ArgumentOutOfRangeException("slot");return result==null?new PracticeOwnerFleetVisibility(before,slot):result.Fleet(slot);}
        public PracticeOwnerFleetVisibility Before(int slot){if(slot<0||slot>=250)throw new ArgumentOutOfRangeException("slot");return new PracticeOwnerFleetVisibility(before,slot);}
        public int ChangedCount {get{return result==null?0:result.ChangedFleets.Count;}}
        public ReadOnlyCollection<PracticeOwnerFleetVisibility> ActiveFleets
        {
            get{if(result!=null)return result.ActiveFleets;var list=new List<PracticeOwnerFleetVisibility>();for(int slot=0;slot<250;slot++)if(BitConverter.ToInt32(before,slot*492)!=0)list.Add(new PracticeOwnerFleetVisibility(before,slot));return list.AsReadOnly();}
        }
        PracticeOwnerVisibilityStudyState(PracticeOwnerVisibilityRules rules,byte[] before,int preset,int owner,int mode,short local,int weapon,int selected,int observer,int radius,int distance,int packed)
        {this.rules=rules;this.before=before;Preset=preset;Owner=owner;Mode=mode;LocalOwner=local;SelectedWeapon=weapon;SelectedFleet=selected;ObserverKind=observer;RadiusProfile=radius;Distance=distance;Packed=packed;}
        static void I(byte[] b,int at,int value){BitConverter.GetBytes(value).CopyTo(b,at);}static void W(byte[] b,int at,int value){BitConverter.GetBytes(checked((short)value)).CopyTo(b,at);}
        static byte[] MakeSynthetic(){var f=new byte[123000];for(int n=0;n<f.Length;n++)f[n]=(byte)((n*17+9)%251);for(int slot=0;slot<250;slot++){I(f,slot*492,0);W(f,slot*492+4,slot);W(f,slot*492+6,-2);}return f;}
        public static PracticeOwnerVisibilityStudyState Create(string programs,PracticeMapStartup startup,int preset,int owner,int weapon,int observer,int radius,int distance,int packed)
        {return CreateWithRules(PracticeOwnerVisibilityRules.Load(programs),startup,preset,owner,weapon,observer,radius,distance,packed);}
        internal static PracticeOwnerVisibilityStudyState CreateWithRules(PracticeOwnerVisibilityRules rules,PracticeMapStartup startup,int preset,int owner,int weapon,int observer,int radius,int distance,int packed)
        {
            if(rules==null||startup==null)throw new ArgumentNullException("visibility copy");if(startup.Seed!=731)throw new NotSupportedException("This owner-visibility study admits BSSB202 seed731 only.");
            if(preset<0||preset>4)throw new ArgumentOutOfRangeException("preset");if(owner< -1||owner>3)throw new ArgumentOutOfRangeException("owner");if(weapon!=0&&weapon!=14&&weapon!=19&&weapon!=34)throw new ArgumentOutOfRangeException("weapon");
            if(observer<0||observer>12)throw new ArgumentOutOfRangeException("observer");if(radius<0||radius>3)throw new ArgumentOutOfRangeException("radius");
            if(preset==3&&(Array.IndexOf(new[]{0,10,11,30,31,255,256},distance)<0||Array.IndexOf(new[]{0,4,85,170,255},packed)<0))throw new ArgumentOutOfRangeException("sensor fixture");
            if(preset==4&&(owner<0||(observer!=1&&observer!=12)||Array.IndexOf(new[]{0,20,21,500,501},distance)<0||Array.IndexOf(new[]{0,85,170},packed)<0))throw new ArgumentOutOfRangeException("local filter fixture");
            var f=preset<3?startup.Stages[3].CopyFleetPool():(byte[])synthetic.Clone();int mode=preset==2||preset==4?2:0;short local=mode==2?(short)owner:(short)-2;int selected=preset==4?249:preset==3?0:16;int appliedWeapon=mode==2?weapon:0;
            if(preset==1||preset==2)for(int slot=0;slot<250;slot++){f[slot*492+467]=(byte)(slot%4);f[slot*492+480]=(byte)(slot%2+1);f[slot*492+481]=(byte)((slot*37+11)&255);}
            if(preset>=3)
            {
                I(f,0,observer);W(f,4,0);W(f,6,owner);W(f,12,100);W(f,14,100);
                int appliedRadius=preset==4?3:radius;int[][] radii={new[]{10,20,30},new[]{30,20,10},new[]{0,255,0},new[]{255,0,0}};
                f[464]=(byte)radii[appliedRadius][0];f[465]=(byte)radii[appliedRadius][1];f[466]=(byte)radii[appliedRadius][2];f[467]=3;f[480]=2;f[481]=(byte)packed;
                for(int slot=1;slot<=25;slot++){int at=slot*492;I(f,at,slot);W(f,at+4,slot);W(f,at+6,owner==0?1:0);W(f,at+12,100+(preset==3?distance:0));W(f,at+14,100);f[at+467]=(byte)(slot%4);f[at+480]=(byte)(slot%2+1);f[at+481]=(byte)packed;}
                if(preset==4){W(f,249*492+12,100+distance);W(f,249*492+14,100);}radius=appliedRadius;
            }
            return new PracticeOwnerVisibilityStudyState(rules,f,preset,owner,mode,local,appliedWeapon,selected,observer,radius,distance,packed);
        }
        public PracticeOwnerVisibilityStudyState Execute()
        {
            if(Executed)throw new InvalidOperationException("This copied visibility pass is complete; reset or load another copy.");
            var next=(PracticeOwnerVisibilityStudyState)MemberwiseClone();next.result=rules.Apply(before,Owner,Mode,LocalOwner,SelectedWeapon,SelectedFleet);return next;
        }
    }
}
