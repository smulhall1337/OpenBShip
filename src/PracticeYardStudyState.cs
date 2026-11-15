using System;
using System.IO;
namespace FleetLab
{
    // Explicit paired-yard fixture and bounded data-only command/worker path.
    // RIP SHA256 B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569.
    // Source VA 428EE0/429060/4297F0/4294F0/429730/429BF0; 268-byte units, 492-byte fleets.
    // Source4085D0 selection maintenance retained; rendering/camera work and original
    // discovery/UI/mission flow are separate. Fixture starts selected fleet7/unitnull.
    public sealed class PracticeYardStudyState
    {
        readonly PracticeUnitCatalog units;
        readonly byte[] ships,fleets;
        public int SelectedFleet
        {
            get;
            private set;
        }
        public int SelectedUnit
        {
            get;
            private set;
        }
        public int TextRequests
        {
            get;
            private set;
        }
        public int MapRequests
        {
            get;
            private set;
        }
        public int RefreshRequests
        {
            get;
            private set;
        }
        public int WorkerCalls
        {
            get;
            private set;
        }
        public bool Entered
        {
            get;
            private set;
        }
        public bool Returned
        {
            get
            {
                return Entered&&W(ships,12)==7;
            }
        }
        public const int SourceFleet=7,YardFleet=34,TargetUnit=0,EmptyUnit=100,MarkerUnit=101,StockUnit=102;
        static int I(byte[] b,int p)
        {
            return BitConverter.ToInt32(b,p);
        }
        static int W(byte[] b,int p)
        {
            return BitConverter.ToInt16(b,p);
        }
        static void I(byte[] b,int p,int v)
        {
            BitConverter.GetBytes(v).CopyTo(b,p);
        }
        static void W(byte[] b,int p,int v)
        {
            BitConverter.GetBytes(unchecked((short)v)).CopyTo(b,p);
        }
        static int Ref(byte[] f,int fleet,int member)
        {
            uint reference=BitConverter.ToUInt32(f,fleet*492+16+member*4);
            if(reference==0)return -1;
            long at=(long)reference-0x4fc340;
            if(at<0||at>=402000||at%268!=0)throw new InvalidDataException("Paired-yard member reference is outside its complete ship pool.");
            return (int)at/268;
        }
        // Only these supplied profiles are admitted: hull 0=intact,1=one8,2=mixed;
        // ammo 0=constructor,1=empty; supplies 0=empty,1=startup table. No arbitrary pools.
        public static PracticeYardStudyState Create(string programs,int type,int markerType,int hull,int ammo,int supplies,bool escort)
        {
            if(Array.IndexOf(new[]
            {
                1,2,4,6,11,12
            },type)<0||(markerType!=22&&markerType!=23)||hull<0||hull>2||ammo<0||ammo>1||supplies<0||supplies>1)throw new ArgumentOutOfRangeException("yard profile");
            var units=PracticeUnitCatalog.Load(programs);
            var parts=PracticeAirfieldFactory.Load(programs).CreateFresh(32,new System.Drawing.Point(400,370),0,1);
            var partShips=parts.CopyShipPool();
            if(supplies!=0)partShips=PracticeInstallationInitialization.Load(programs).ApplyOffscreen(partShips,parts.CopyFleetPool(),0).CopyShipPool();
            var s=new byte[402000];
            var f=new byte[123000];
            for(int n=0;n<1500;n++)W(s,n*268+4,n);
            for(int n=0;n<250;n++)PracticeFleetSnapshot.InitializeRootFleet(f,n*492,n);
            I(f,7*492,type==11||type==12?9:1);
            W(f,7*492+12,400);
            W(f,7*492+14,370);
            f[7*492+482]=34;
            f[7*492+485]=2;
            I(f,34*492,2);
            W(f,34*492+12,400);
            W(f,34*492+14,380);
            I(f,33*492,2);
            PracticeAircraftFactory.Construct(units,s,0,type,0,7,2,1);
            if(escort)PracticeAircraftFactory.Construct(units,s,1,2,0,7,7,1);
            Array.Copy(partShips,(markerType==22?4:8)*268,s,101*268,268);
            W(s,101*268+4,101);
            W(s,101*268+8,4);
            W(s,101*268+10,3);
            W(s,101*268+12,34);
            Array.Copy(partShips,2*268,s,102*268,268);
            W(s,102*268+4,102);
            W(s,102*268+12,33);
            int length=W(s,14),capacity=W(s,101*268+14);
            if(length>capacity)throw new InvalidOperationException("The selected ship does not fit this marker's source capacity.");
            if(ammo!=0)for(int n=0;n<35;n++)W(s,196+n*2,0);
            W(s,16,hull==0?length:hull==1?length-1:0);
            for(int n=0;n<length;n++)I(f,7*492+64+(21+n)*4,hull==0?2:hull==1?(n==1?8:2):new[]
            {
                5,6,7,8,8,5
            }
            [n]);
            if(escort)for(int n=0;n<3;n++)I(f,7*492+64+(71+n)*4,2);
            for(int n=0;n<100;n++)I(f,34*492+64+n*4,3);
            for(int n=0;n<capacity;n++)I(f,34*492+64+(43+n)*4,2);
            I(f,7*492+16,0x4fc340);
            if(escort)I(f,7*492+20,0x4fc340+268);
            I(f,34*492+16,0x4fc340+100*268);
            I(f,34*492+20,0x4fc340+101*268);
            I(f,33*492+16,0x4fc340+102*268);
            return new PracticeYardStudyState(units,s,f);
        }
        PracticeYardStudyState(PracticeUnitCatalog units,byte[] ships,byte[] fleets)
        {
            this.units=units;
            this.ships=ships;
            this.fleets=fleets;
            SelectedFleet=7;
            SelectedUnit=-1;
        }
        PracticeYardStudyState Copy()
        {
            return new PracticeYardStudyState(units,(byte[])ships.Clone(),(byte[])fleets.Clone())
            {
                SelectedFleet=SelectedFleet,SelectedUnit=SelectedUnit,TextRequests=TextRequests,MapRequests=MapRequests,RefreshRequests=RefreshRequests,WorkerCalls=WorkerCalls,Entered=Entered
            };
        }
        public byte[] CopyShipPool()
        {
            return (byte[])ships.Clone();
        }
        public byte[] CopyFleetPool()
        {
            return (byte[])fleets.Clone();
        }
        void BlockStatus(int fleet)
        {
            int first=fleet&~3;
            bool live=false;
            for(int n=first;n<first+4;n++)for(int m=0;m<6;m++)
            {
                int unit=Ref(fleets,n,m);
                if(unit<0)continue;
                int kind=I(ships,unit*268);
                if(kind!=0&&kind!=22&&kind!=19&&kind!=23&&kind!=20)
                {
                    live=true;
                    break;
                }
            }
            int current=I(fleets,first*492+472);
            if(live&&current==4||!live&&current!=4)
            {
                for(int n=first;n<first+4;n++)I(fleets,n*492+472,live?0:4);
                TextRequests++;
            }
        }
        // Both source display paths call 4085D0 before their rendering work.
        // Preserve that data/selection maintenance; selected-fleet fallback
        // 42C380 is deliberately outside this valid-source-fleet fixture.
        void MaintainSelection()
        {
            if(SelectedFleet<0||SelectedFleet>=250||I(fleets,SelectedFleet*492)==0)throw new NotSupportedException("Selected-fleet fallback is outside the paired-yard profile.");
            if(SelectedUnit>=0&&I(ships,SelectedUnit*268)!=0&&W(ships,SelectedUnit*268+12)==W(fleets,SelectedFleet*492+4))return;
            for(int member=0;member<6;member++)
            {
                int unit=Ref(fleets,SelectedFleet,member);
                if(unit<0||I(ships,unit*268)==0)continue;
                SelectedUnit=unit;
                fleets[SelectedFleet*492+468]=(byte)member;
                return;
            }
            SelectedUnit=Ref(fleets,SelectedFleet,0);
        }
        void Refresh()
        {
            MaintainSelection();
            RefreshRequests++;
        }
        public PracticeYardAdmissionResult CheckAdmission()
        {
            return PracticeYardAdmission.Check(ships,fleets,TargetUnit,0);
        }
        public PracticeYardStudyState Enter()
        {
            if(Entered)throw new InvalidOperationException("Reset the prepared fixture before admitting it again.");
            if(!CheckAdmission().Available)throw new NotSupportedException("The read-only source gate rejects this prepared copy.");
            if(I(ships,0)==0||(I(fleets,7*492)!=1&&I(fleets,7*492)!=9)||fleets[7*492+482]!=34||I(fleets,34*492)!=2||W(fleets,34*492+6)!=0||I(ships,100*268)!=0||W(ships,14)>W(ships,101*268+14))throw new NotSupportedException("Only the prepared, fitting paired-yard admission is supported.");
            var next=Copy();
            var s=next.ships;
            var f=next.fleets;
            int marker=101*268,length=W(s,14),x=W(s,8),y=W(s,10),mx=W(s,marker+8),my=W(s,marker+10);
            f[7*492+490]=1;
            next.TextRequests++;
            W(s,marker+196,7);
            W(s,marker+198,0);
            W(s,marker+200,x);
            W(s,marker+202,y);
            for(int n=0;n<length;n++)
            {
                W(s,marker+204+n*2,I(f,34*492+64+(mx*10+my+n)*4));
                I(f,34*492+64+(mx*10+my+n)*4,I(f,7*492+64+(x*10+y+n)*4));
                I(f,7*492+64+(x*10+y+n)*4,0);
            }
            I(f,34*492+16,0x4fc340);
            W(s,12,34);
            W(s,8,mx);
            W(s,10,my);
            I(f,7*492+472,3);
            I(f,7*492+16,0);
            int member=0;
            for(;member<6;member++)
            {
                int unit=Ref(f,7,member);
                if(unit>=0&&I(s,unit*268)!=0)break;
            }
            if(member<6)
            {
                f[7*492+468]=(byte)member;
                if(next.SelectedFleet==7)
                {
                    next.SelectedUnit=Ref(f,7,member);
                    next.Refresh();
                }
            }
            else
            {
                I(f,7*492,0);
                next.SelectedFleet=34;
                next.Refresh();
            }
            next.MaintainSelection();
            next.MapRequests++;
            next.BlockStatus(34);
            next.Entered=true;
            return next;
        }
        void Release(int target,int markerUnit,int yard,int targetMember)
        {
            int unit=target*268,mark=markerUnit*268,origin=W(ships,mark+196),slot=W(ships,mark+198),x=W(ships,mark+200),y=W(ships,mark+202),length=W(ships,unit+14),mx=W(ships,mark+8),my=W(ships,mark+10);
            if(origin!=7||slot!=0||yard!=34||target!=0||markerUnit!=101||targetMember!=0)throw new NotSupportedException("Only the prepared yard's saved return mapping is admitted.");
            if(I(fleets,origin*492)==0)I(fleets,origin*492,I(ships,unit)==11||I(ships,unit)==12?9:1);
            I(fleets,origin*492+16+slot*4,0x4fc340+unit);
            W(ships,unit+8,x);
            W(ships,unit+10,y);
            fleets[origin*492+468]=(byte)slot;
            if(SelectedFleet==origin)SelectedUnit=target;
            W(ships,unit+16,length);
            W(ships,unit+12,origin);
            for(int n=0;n<length;n++)
            {
                I(fleets,origin*492+64+(x*10+y+n)*4,2);
                I(fleets,yard*492+64+(mx*10+my+n)*4,W(ships,mark+204+n*2));
            }
            I(fleets,yard*492+16+targetMember*4,0x4fc340+(W(ships,mark+4)-1)*268);
            bool other=false;
            for(int n=32;n<36;n++)for(int m=1;m<6;m++)
            {
                int candidate=Ref(fleets,n,m),previous=Ref(fleets,n,m-1);
                if(candidate>=0&&previous>=0&&(I(ships,candidate*268)==22||I(ships,candidate*268)==23)&&I(ships,previous*268)!=0&&W(ships,candidate*268+196)==origin)other=true;
            }
            if(!other)
            {
                I(fleets,origin*492+472,0);
                // 4297D3 compares the marker unit ID to the selected fleet ID.
                if(SelectedFleet==W(ships,mark+4))Refresh();
            }
            int active=0;
            for(;active<6;active++)
            {
                int candidate=Ref(fleets,yard,active);
                if(candidate>=0&&I(ships,candidate*268)!=0)break;
            }
            if(active<6)
            {
                fleets[yard*492+468]=(byte)active;
                if(SelectedFleet==yard)SelectedUnit=Ref(fleets,yard,active);
            }
            Refresh();
            BlockStatus(yard);
            TextRequests++;
        }
        public PracticeYardStudyState WorkerStep()
        {
            if(!Entered)throw new InvalidOperationException("Admit the prepared ship before applying a worker pass.");
            var next=Copy();
            var s=next.ships;
            var f=next.fleets;
            for(int fleet=0;fleet<250;fleet++)
            {
                if(I(f,fleet*492)!=2)continue;
                for(int member=0;member<6;member++)
                {
                    int markerUnit=Ref(f,fleet,member);
                    if(markerUnit<0)continue;
                    int kind=I(s,markerUnit*268);
                    if(kind!=22&&kind!=23)continue;
                    if(member==0)throw new NotSupportedException("Marker-first constructor rosters are not an admitted worker profile.");
                    int target=Ref(f,fleet,member-1);
                    if(target<0||I(s,target*268)==0)continue;
                    if(target!=0||markerUnit!=101||fleet!=34||member!=1)throw new NotSupportedException("The worker currently admits the single prepared paired bay only.");
                    var repaired=PracticeInstallationRepair.StepOffscreen(s,f,fleet,target);
                    Array.Copy(repaired.CopyShipPool(),s,s.Length);
                    Array.Copy(repaired.CopyFleetPool(),f,f.Length);
                    int depot=PracticeInstallationLookup.FindFirstInAlignedBlock(s,f,fleet,19),at=target*268,length=W(s,at+14);
                    bool supplied=false;
                    if(depot>=0)for(int row=0;row<length&&!supplied;row++)for(int section=0;section<4;section++)
                    {
                        int weapon=I(s,at+100+(row*4+section)*4);
                        if(weapon==0)continue;
                        if(weapon<0||weapon>39)throw new NotSupportedException("Unsupported paired-yard equipment.");
                        int occurrences=0;
                        for(int n=0;n<length*4;n++)if(I(s,at+100+n*4)==weapon)occurrences++;
                        int maximum=W(units.Ammunition,weapon*2)*occurrences,current=W(s,at+196+weapon*2),available=W(s,depot*268+196+weapon*2);
                        if(maximum<=current||available<=0)continue;
                        int sum=unchecked((short)(current+available));
                        W(s,at+196+weapon*2,sum);
                        W(s,depot*268+196+weapon*2,0);
                        if(sum>maximum)
                        {
                            W(s,depot*268+196+weapon*2,sum-maximum);
                            W(s,at+196+weapon*2,maximum);
                        }
                        supplied=true;
                        if(next.SelectedFleet==fleet)next.Refresh();
                        break;
                    }
                    if(!supplied)
                    {
                        next.Release(target,markerUnit,fleet,member-1);
                        if(next.SelectedFleet==W(s,at+12))next.Refresh();
                        if(next.SelectedFleet==fleet)next.Refresh();
                    }
                }
            }
            next.WorkerCalls++;
            return next;
        }
    }
}
