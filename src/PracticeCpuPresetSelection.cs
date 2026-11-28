using System;using System.IO;using System.Collections.Generic;using System.Collections.ObjectModel;
namespace FleetLab
{
    public sealed class PracticeCpuPresetRequest
    {
        public int Owner{get;private set;}public int ConstructorKind{get{return 9;}}public int TemplateSlot{get;private set;}public int RandomDraws{get;private set;}
        internal PracticeCpuPresetRequest(int owner,int slot,int draws){Owner=owner;TemplateSlot=slot;RandomDraws=draws;}
    }
    public sealed class PracticeCpuPresetSelectionResult
    {
        readonly byte[] participants;readonly ReadOnlyCollection<PracticeCpuPresetRequest> requests;
        public uint InitialRandomState{get;private set;}public uint FinalRandomState{get;private set;}public int RandomDraws{get;private set;}
        public ReadOnlyCollection<PracticeCpuPresetRequest> Requests{get{return requests;}}
        internal PracticeCpuPresetSelectionResult(byte[] p,uint initial,uint final,int draws,List<PracticeCpuPresetRequest> requests){participants=(byte[])p.Clone();InitialRandomState=initial;FinalRandomState=final;RandomDraws=draws;this.requests=requests.AsReadOnly();}
        public byte[] CopyParticipants(){return(byte[])participants.Clone();}
    }
    // Whole403D30/41B130/439790, original1176R4. The constructor callback
    // only records requests and PRESERVES RNG. This is not the random history
    // of real403210 placement, which is not executed by this model.
    public static class PracticeCpuPresetSelection
    {
        public const int MaximumRandomDraws=4096;
        public static PracticeCpuPresetSelectionResult Evaluate(PracticeFormationPresetResult presets,byte[] participants,int configured,int mode,int difficulty,uint seed)
        {
            if(presets==null||!presets.OpenSucceeded||presets.LoadedRecordCount==0)throw new InvalidDataException("A successfully loaded nonempty preset table is required before CPU selection.");if(participants==null||participants.Length!=176)throw new InvalidDataException("Complete four44-byte participant records required.");if(configured<1||configured>4)throw new ArgumentOutOfRangeException("configured");if(mode!=0&&mode!=4&&mode!=5&&mode!=20)throw new NotSupportedException("Compared modes are0,4,5and20; only4 enters the selection loop.");if(difficulty< -2||difficulty>3)throw new ArgumentOutOfRangeException("difficulty","Compared raw difficulty is-2..3.");
            var requests=new List<PracticeCpuPresetRequest>();uint random=seed;int draws=0;if(mode!=4)return new PracticeCpuPresetSelectionResult(participants,seed,random,draws,requests);byte[] table=presets.CopyTable();int count=presets.LoadedRecordCount;bool candidate=false;for(int n=0;n<count;n++)if(table[n*402]==10)candidate=true;if(!candidate)throw new NotSupportedException("No tag10 in the source modulo-count range; selection would not complete.");
            for(int owner=0;owner<configured;owner++)
            {
                if(BitConverter.ToInt32(participants,owner*44)!=2)continue;
                for(int n=0;n<difficulty+2;n++)
                {
                    int start=draws,slot;do{if(draws++>=MaximumRandomDraws)throw new InvalidOperationException("Modern bounded RNG study guard reached; no partial result published.");random=unchecked(random*214013u+2531011u);slot=(int)(((random>>16)&32767u)%(uint)count);}while(table[slot*402]!=10);
                    requests.Add(new PracticeCpuPresetRequest(owner,slot,draws-start));
                }
            }
            return new PracticeCpuPresetSelectionResult(participants,seed,random,draws,requests);
        }
    }
}
