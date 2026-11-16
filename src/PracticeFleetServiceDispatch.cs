using System;
using System.IO;
namespace FleetLab
{
    public enum PracticeFleetServiceMode
    {
        TaggedSelective,
        InitializedSelective,
        InitializedDispatcher
    }

    // RIP B9A8...6A569,41B5C0: one selected normalized312B context.
    // This is a data representation, not the original allocator or full init:
    // weight scaling,250 live contexts and64x64 heap rows do not run here.
    public static class PracticeFleetServiceContext
    {
        public static byte[] CreateDiagnostic(int fleet)
        {
            if(fleet<0||fleet>=250)throw new ArgumentOutOfRangeException("fleet");
            var task=new byte[312];
            // Original initialization leaves36 bytes untouched. The recorded
            // comparison supplies6D; it is not a natural game's starting value.
            for(int n=260;n<296;n++)task[n]=0x6d;
            for(int record=0;record<10;record++)
            {
                Put(task,record*20+8,-1);Put(task,record*20+12,-1);
            }
            Put(task,204,0x4b47a0+fleet*492);
            // Normalized guarded-table identity from the recorded allocator.
            // Never dereferenced as a real address by this maintained study.
            Put(task,208,unchecked((int)(0x70000000u+(uint)(fleet*6432+16))));
            Put(task,308,-1);return task;
        }
        static void Put(byte[] task,int at,int value){BitConverter.GetBytes(value).CopyTo(task,at);}
    }

    internal sealed partial class FleetServiceCore
    {
        PracticeFleetServiceMode mode;
        internal PracticeFleetServiceMode Mode { get { return mode; } }
        void VisitAppliedHandler()
        {
            if(mode==PracticeFleetServiceMode.InitializedDispatcher)VisitDispatcher();else TaskVisit();
        }
        // Reached paths of all six41C750 helpers for the recorded seed731
        // service profiles (native496). Other target/attachment/planning
        // branches reject transactionally through the outer copied core.
        // Active AI owner4E42D0=1 is supplied, distinct from viewed selection.
        void VisitDispatcher()
        {
            int actor=Actor(),at=actor*492;
            if(W(fleets,at+6)==-1)return;
            if(W(fleets,at+6)!=1)throw new NotSupportedException("Service dispatch admits active AI owner1 only.");
            if(I(fleets,at)==1)
            {
                int active=0;
                for(int member=0;member<6;member++)
                {
                    int unit=Ref(actor,member);if(unit<0)continue;int kind=I(ships,unit*268);
                    if(kind!=5&&kind!=6&&kind!=7)continue;
                    for(int link=0;link<4;link++)
                    {
                        uint reference=BitConverter.ToUInt32(ships,unit*268+56+link*4);if(reference==0)continue;
                        long delta=(long)reference-0x4b47a0;
                        if(delta<0||delta>=123000||delta%492!=0)throw new InvalidDataException("Service dispatch attached actor reference.");
                        if(I(fleets,(int)delta+472)==1)active++;
                    }
                }
                if(active>1)throw new NotSupportedException("Service dispatch task3 attachment branch is not reconstructed.");
            }
            if(fleets[at+480]!=0&&I(task,0)!=10)throw new NotSupportedException("Service dispatch visible-target branch is not reconstructed.");
            int contact=unchecked((sbyte)fleets[at+482]);
            if(contact>=0)
            {
                if(I(fleets,contact*492)!=2||W(fleets,contact*492+6)!=1)throw new NotSupportedException("Service dispatch admits the supplied friendly kind2 contact only.");
                if(I(task,0)==10)PopTask();
                if(I(task,0)!=11)PopTask();
            }
            else if(contact!=-1)throw new NotSupportedException("Service dispatch collision variant is not reconstructed.");
            if(I(fleets,at)==1&&I(task,0xc8)==4)throw new NotSupportedException("Service dispatch attachment-order branch is not reconstructed.");
            TaskVisit();
            if(W(fleets,at+40)!=-1&&I(task,0x130)==0&&I(fleets,at+476)!=2)throw new NotSupportedException("Service dispatch waypoint planning is not reconstructed.");
        }
    }
}
