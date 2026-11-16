using System;
using System.IO;
namespace FleetLab
{
    // B9A8...6A569: 428EE0, 429060, 4297F0 and return/selection helpers.
    // Admitted copied BSSB202 seed731 profiles only. Three display endpoints
    // retain selection maintenance but count requests instead of rendering.
    // Discovery, distance checks, command UI, AI and source cadence are absent.
    public sealed class PracticeMapYardState
    {
        readonly PracticeUnitCatalog catalog;
        readonly byte[] ships,fleets;
        public int TargetUnit { get; private set; }
        public int Profile { get; private set; }
        public int OriginFleet { get; private set; }
        public int YardFleet { get; private set; }
        public int MarkerUnit { get; private set; }
        public int SelectedFleet { get; private set; }
        public int SelectedUnit { get; private set; }
        public int TextRequests { get; private set; }
        public int MapRequests { get; private set; }
        public int RefreshRequests { get; private set; }
        public int WorkerCalls { get; private set; }
        public bool Entered { get; private set; }
        public bool Returned { get { return Entered&&W(ships,TargetUnit*268+12)==OriginFleet; } }
        public int TargetHP { get { return W(ships,TargetUnit*268+16); } }
        public int TargetLength { get { return W(ships,TargetUnit*268+14); } }
        public int TargetAmmo { get { return Ammo(TargetUnit); } }
        public int DepotUnit { get { return PracticeInstallationLookup.FindFirstInAlignedBlock(ships,fleets,YardFleet,19); } }
        public int DepotAmmo { get { return Ammo(DepotUnit); } }
        public const int SelectedYard=3;
        public const short LocalOwner=1;

        static int I(byte[] b,int p){return BitConverter.ToInt32(b,p);}
        static int W(byte[] b,int p){return BitConverter.ToInt16(b,p);}
        static void I(byte[] b,int p,int value){BitConverter.GetBytes(value).CopyTo(b,p);}
        static void W(byte[] b,int p,int value){BitConverter.GetBytes(unchecked((short)value)).CopyTo(b,p);}
        int Ref(int fleet,int member)
        {
            uint reference=BitConverter.ToUInt32(fleets,fleet*492+16+member*4);
            if(reference==0)return -1;
            long delta=(long)reference-0x4fc340;
            if(delta<0||delta>=402000||delta%268!=0)throw new InvalidDataException("Map yard reached an invalid normalized unit reference.");
            return (int)(delta/268);
        }
        int Ammo(int unit)
        {
            if(unit<0)return 0;
            int total=0;for(int n=0;n<35;n++)total+=W(ships,unit*268+196+n*2);return total;
        }
        PracticeMapYardState(PracticeUnitCatalog catalog,byte[] ships,byte[] fleets)
        {this.catalog=catalog;this.ships=ships;this.fleets=fleets;}

        // Profile0: intact original stocks; 1: one state8, HP length-1, empty
        // ammo; 2: mixed supplied grid states, HP0, empty ammo. Only copied data
        // changes. The selected-yard byte is an explicit diagnostic command.
        public static PracticeMapYardState Create(string programs,PracticeMapStartup startup,int target,int profile)
        {
            if(startup==null)throw new ArgumentNullException("startup");
            if(startup.Seed!=731)throw new NotSupportedException("The copied map yard currently admits applied seed731 only.");
            if(Array.IndexOf(new[]{30,31,34},target)<0||profile<0||profile>2)throw new ArgumentOutOfRangeException("target/profile");
            var stage=startup.Stages[3];var s=stage.CopyShipPool();var f=stage.CopyFleetPool();
            var next=new PracticeMapYardState(PracticeUnitCatalog.Load(programs),s,f){TargetUnit=target,Profile=profile};
            int at=target*268,parent=W(s,at+12);next.OriginFleet=parent;next.SelectedFleet=parent;next.SelectedUnit=target;
            if(W(s,at+6)!=LocalOwner||I(f,parent*492)!=1)throw new InvalidDataException("Map yard requires the recovered owned source fleet.");
            f[parent*492+482]=SelectedYard;
            if(profile!=0)
            {
                int length=W(s,at+14),x=W(s,at+8),y=W(s,at+10);
                W(s,at+16,profile==1?length-1:0);
                for(int n=0;n<35;n++)W(s,at+196+n*2,0);
                for(int n=0;n<length;n++)I(f,parent*492+64+(x*10+y+n)*4,profile==1?(n==1?8:2):new[]{5,6,7,8,8,5}[n]);
            }
            var gate=next.CheckAdmission();
            if(!gate.Available||gate.MarkerMember<1)throw new InvalidDataException("Copied map has no admitted paired yard for this target.");
            next.YardFleet=gate.MarkerFleet;next.MarkerUnit=gate.MarkerUnit;
            return next;
        }
        PracticeMapYardState Copy()
        {
            return new PracticeMapYardState(catalog,(byte[])ships.Clone(),(byte[])fleets.Clone())
            {
                TargetUnit=TargetUnit,Profile=Profile,OriginFleet=OriginFleet,YardFleet=YardFleet,MarkerUnit=MarkerUnit,
                SelectedFleet=SelectedFleet,SelectedUnit=SelectedUnit,TextRequests=TextRequests,MapRequests=MapRequests,
                RefreshRequests=RefreshRequests,WorkerCalls=WorkerCalls,Entered=Entered
            };
        }
        public byte[] CopyShipPool(){return (byte[])ships.Clone();}
        public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
        public PracticeYardAdmissionResult CheckAdmission(){return PracticeYardAdmission.Check(ships,fleets,TargetUnit,LocalOwner);}

        void MaintainSelection()
        {
            if(SelectedFleet<0||SelectedFleet>=250||I(fleets,SelectedFleet*492)==0)throw new NotSupportedException("Map yard requires a live selected source fleet; inactive fallback is separate.");
            if(SelectedUnit>=0&&I(ships,SelectedUnit*268)!=0&&W(ships,SelectedUnit*268+12)==W(fleets,SelectedFleet*492+4))return;
            for(int member=0;member<6;member++)
            {
                int unit=Ref(SelectedFleet,member);if(unit<0||I(ships,unit*268)==0)continue;
                SelectedUnit=unit;fleets[SelectedFleet*492+468]=(byte)member;return;
            }
            SelectedUnit=Ref(SelectedFleet,0);
        }
        void Refresh(){MaintainSelection();RefreshRequests++;}
        void BlockStatus(int yard)
        {
            int first=yard&~3;bool live=false;
            for(int fleet=first;fleet<first+4;fleet++)for(int member=0;member<6;member++)
            {
                int unit=Ref(fleet,member);if(unit<0)continue;int type=I(ships,unit*268);
                if(type!=0&&type!=22&&type!=19&&type!=23&&type!=20)live=true;
            }
            int status=I(fleets,first*492+472);
            if(live&&status==4||!live&&status!=4)
            {
                for(int fleet=first;fleet<first+4;fleet++)I(fleets,fleet*492+472,live?0:4);
                TextRequests++;
            }
        }
        public PracticeMapYardState Enter()
        {
            if(Entered)throw new InvalidOperationException("Reset or load a fresh copy before entering again.");
            var gate=CheckAdmission();if(!gate.Available)throw new NotSupportedException(gate.Reason);
            var next=Copy();next.EnterCore(gate);next.Entered=true;return next;
        }
        void EnterCore(PracticeYardAdmissionResult gate)
        {
            int yard=gate.MarkerFleet,marker=gate.MarkerUnit,previous=gate.MarkerMember-1,sourceMember=-1;
            for(int member=0;member<6;member++)if(Ref(OriginFleet,member)==TargetUnit){sourceMember=member;break;}
            if(previous<0||sourceMember<0||Ref(yard,previous)<0||I(ships,Ref(yard,previous)*268)!=0)throw new NotSupportedException("Map yard requires an empty preceding bay and a referenced source target.");
            int at=TargetUnit*268,mk=marker*268,length=W(ships,at+14),x=W(ships,at+8),y=W(ships,at+10),mx=W(ships,mk+8),my=W(ships,mk+10);
            fleets[OriginFleet*492+490]=1;TextRequests++;
            W(ships,mk+196,OriginFleet);W(ships,mk+198,sourceMember);W(ships,mk+200,x);W(ships,mk+202,y);
            for(int n=0;n<length;n++)
            {
                W(ships,mk+204+n*2,I(fleets,yard*492+64+(mx*10+my+n)*4));
                I(fleets,yard*492+64+(mx*10+my+n)*4,I(fleets,OriginFleet*492+64+(x*10+y+n)*4));
                I(fleets,OriginFleet*492+64+(x*10+y+n)*4,0);
            }
            I(fleets,yard*492+16+previous*4,0x4fc340+at);W(ships,at+12,yard);W(ships,at+8,mx);W(ships,at+10,my);
            I(fleets,OriginFleet*492+472,3);I(fleets,OriginFleet*492+16+sourceMember*4,0);
            int active=0;for(;active<6;active++){int unit=Ref(OriginFleet,active);if(unit>=0&&I(ships,unit*268)!=0)break;}
            if(active==6)throw new NotSupportedException("Copied map yard requires a retained escort; empty-source return is separate.");
            fleets[OriginFleet*492+468]=(byte)active;
            if(SelectedFleet==OriginFleet){SelectedUnit=Ref(OriginFleet,active);Refresh();}
            MaintainSelection();MapRequests++;BlockStatus(yard);
        }
        void Release(int unit,int marker,int yard,int member)
        {
            int at=unit*268,mk=marker*268,origin=W(ships,mk+196),slot=W(ships,mk+198),x=W(ships,mk+200),y=W(ships,mk+202),length=W(ships,at+14),mx=W(ships,mk+8),my=W(ships,mk+10);
            if(origin!=OriginFleet||unit!=TargetUnit||marker!=MarkerUnit||yard!=YardFleet||I(fleets,origin*492)==0)throw new NotSupportedException("Map yard return left the admitted live source profile.");
            I(fleets,origin*492+16+slot*4,0x4fc340+at);W(ships,at+8,x);W(ships,at+10,y);fleets[origin*492+468]=(byte)slot;
            if(SelectedFleet==origin)SelectedUnit=unit;
            W(ships,at+16,length);W(ships,at+12,origin);
            for(int n=0;n<length;n++)
            {
                I(fleets,origin*492+64+(x*10+y+n)*4,2);
                I(fleets,yard*492+64+(mx*10+my+n)*4,W(ships,mk+204+n*2));
            }
            I(fleets,yard*492+16+member*4,0x4fc340+(W(ships,mk+4)-1)*268);
            bool other=false;int first=yard&~3;
            for(int fleet=first;fleet<first+4;fleet++)for(int m=1;m<6;m++)
            {
                int candidate=Ref(fleet,m),previous=Ref(fleet,m-1);
                if(candidate>=0&&previous>=0&&(I(ships,candidate*268)==22||I(ships,candidate*268)==23)&&I(ships,previous*268)!=0&&W(ships,candidate*268+196)==origin)other=true;
            }
            if(!other)
            {
                I(fleets,origin*492+472,0);
                // Source compares the marker unit ID with the selected fleet ID.
                if(SelectedFleet==W(ships,mk+4))Refresh();
            }
            int active=0;for(;active<6;active++){int candidate=Ref(yard,active);if(candidate>=0&&I(ships,candidate*268)!=0)break;}
            if(active<6){fleets[yard*492+468]=(byte)active;if(SelectedFleet==yard)SelectedUnit=Ref(yard,active);}
            Refresh();BlockStatus(yard);TextRequests++;
        }
        public PracticeMapYardState WorkerStep()
        {
            if(!Entered)throw new InvalidOperationException("Enter the copied yard before applying a worker pass.");
            var next=Copy();next.WorkerCore();next.WorkerCalls++;return next;
        }
        void WorkerCore()
        {
            for(int yard=0;yard<250;yard++)
            {
                if(I(fleets,yard*492)!=2)continue;
                for(int member=0;member<6;member++)
                {
                    int marker=Ref(yard,member);if(marker<0)continue;int type=I(ships,marker*268);if(type!=22&&type!=23)continue;
                    if(member==0)throw new NotSupportedException("Marker-first rosters are outside the admitted map yard profile.");
                    int unit=Ref(yard,member-1);if(unit<0||I(ships,unit*268)==0)continue;
                    if(unit!=TargetUnit||marker!=MarkerUnit||yard!=YardFleet)throw new NotSupportedException("Copied map worker admits one active bay only.");
                    var repair=PracticeInstallationRepair.StepOffscreen(ships,fleets,yard,unit);
                    Array.Copy(repair.CopyShipPool(),ships,ships.Length);Array.Copy(repair.CopyFleetPool(),fleets,fleets.Length);
                    int depot=PracticeInstallationLookup.FindFirstInAlignedBlock(ships,fleets,yard,19),at=unit*268,length=W(ships,at+14);bool supplied=false;
                    if(depot>=0)for(int row=0;row<length&&!supplied;row++)for(int column=0;column<4;column++)
                    {
                        int weapon=I(ships,at+100+(row*4+column)*4);if(weapon==0)continue;
                        if(weapon<0||weapon>39)throw new NotSupportedException("Copied map worker reached unsupported equipment.");
                        int occurrences=0;for(int n=0;n<length*4;n++)if(I(ships,at+100+n*4)==weapon)occurrences++;
                        int maximum=W(catalog.Ammunition,weapon*2)*occurrences,current=W(ships,at+196+weapon*2),available=W(ships,depot*268+196+weapon*2);
                        if(maximum<=current||available<=0)continue;
                        int sum=unchecked((short)(current+available));W(ships,at+196+weapon*2,sum);W(ships,depot*268+196+weapon*2,0);
                        if(sum>maximum){W(ships,depot*268+196+weapon*2,sum-maximum);W(ships,at+196+weapon*2,maximum);}
                        supplied=true;if(SelectedFleet==yard)Refresh();break;
                    }
                    if(!supplied)
                    {
                        Release(unit,marker,yard,member-1);
                        if(SelectedFleet==W(ships,at+12))Refresh();
                        if(SelectedFleet==yard)Refresh();
                    }
                }
            }
        }
    }
}
