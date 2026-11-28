using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FleetLab
{
    public sealed class PracticeParticipantNameRequest
    {
        public int Slot { get; private set; }
        public bool Numbered { get; private set; }
        public int SourceArgument { get; private set; }
        public string SuppliedLabel { get; private set; }
        internal PracticeParticipantNameRequest(int slot,bool numbered)
        {
            Slot=slot;Numbered=numbered;SourceArgument=numbered?slot:-1;
            SuppliedLabel=numbered?"CPU "+slot:"CPU";
        }
    }

    public sealed class PracticeParticipantCompletionResult
    {
        readonly byte[] before,after;
        readonly ReadOnlyCollection<PracticeParticipantNameRequest> names;
        readonly ReadOnlyCollection<int> formations;
        public int ConfiguredCount { get; private set; }
        public int InitialCount { get; private set; }
        public int FinalCount { get; private set; }
        public int Mode { get; private set; }
        public bool CanUseCompletedCounts { get { return FinalCount==ConfiguredCount; } }
        public ReadOnlyCollection<PracticeParticipantNameRequest> NameRequests { get { return names; } }
        // Requests only. The source403D30 formation routine is NOT executed.
        public ReadOnlyCollection<int> FormationRequests { get { return formations; } }
        internal PracticeParticipantCompletionResult(byte[] before,byte[] after,int configured,int initial,int final,int mode,List<PracticeParticipantNameRequest> names,List<int> formations)
        {
            this.before=(byte[])before.Clone();this.after=(byte[])after.Clone();ConfiguredCount=configured;InitialCount=initial;FinalCount=final;Mode=mode;
            this.names=names.AsReadOnly();this.formations=formations.AsReadOnly();
        }
        public byte[] CopyBeforeRecords(){return (byte[])before.Clone();}
        public byte[] CopyRecords(){return (byte[])after.Clone();}
        public int KindAt(int slot){return BitConverter.ToInt32(after,Offset(slot));}
        public int IdAt(int slot){return BitConverter.ToInt16(after,Offset(slot)+4);}
        public string DisplayNameAt(int slot)
        {
            int at=Offset(slot)+24;var text=new StringBuilder();
            for(int n=0;n<20&&after[at+n]!=0;n++){byte b=after[at+n];text.Append(b>=32&&b<=126?(char)b:'?');}
            return text.ToString(); // Inspection of supplied ASCII fixtures only.
        }
        static int Offset(int slot){if(slot<0||slot>=4)throw new ArgumentOutOfRangeException("slot");return slot*44;}
        internal void WriteOriginalPacket(BinaryWriter writer)
        {
            writer.Write(after);writer.Write(FinalCount);writer.Write(names.Count);
            foreach(var request in names){writer.Write(request.Slot);writer.Write(request.Numbered?1:0);writer.Write(request.SourceArgument);writer.Write(request.SuppliedLabel.Length);}
            writer.Write(formations.Count);foreach(int slot in formations)writer.Write(slot);
        }
    }

    // B9A8...6A569, whole42B4B0: original1116 / independent1117 / fresh1119.
    // Source4390D0 formatting uses explicitly supplied CPU/CPU %d labels.
    // Modes0/20 skip formation natively;mode5 retains admission requests only.
    public static class PracticeParticipantCompletion
    {
        public static PracticeParticipantCompletionResult Complete(byte[] records,int configured,int current,int mode)
        {
            if(records==null||records.Length!=176)throw new InvalidDataException("Participant completion requires four complete44-byte records.");
            if(configured<1||configured>4||current<0||current>4)throw new ArgumentOutOfRangeException("configured/current","The compared participant counts are configured1..4/current0..4.");
            if(mode!=0&&mode!=20&&mode!=5)throw new NotSupportedException("Only compared modes0/20 andmode5 with a recorded,unexecuted formation request are admitted.");
            byte[] output=(byte[])records.Clone();var names=new List<PracticeParticipantNameRequest>();var formations=new List<int>();
            // The single-vs-numbered decision is made once before the loop.
            bool numbered=configured-current!=1;int next=current;
            while(next<configured)
            {
                int at=next*44;WriteInt(output,at,2);WriteWord(output,at+4,next);WriteInt(output,at+8,0);WriteWord(output,at+12,0);WriteInt(output,at+16,0);
                var request=new PracticeParticipantNameRequest(next,numbered);byte[] label=Encoding.ASCII.GetBytes(request.SuppliedLabel+"\0");
                if(label.Length>20)throw new InvalidDataException("The supplied study label exceeds its source record.");
                Array.Copy(label,0,output,at+24,label.Length);names.Add(request);
                if(mode!=0&&mode!=20)formations.Add(next);
                next++;
            }
            // Oversubscription is retained. Only the kind word of unused
            // records is cleared; the remaining40bytes stay opaque.
            for(int slot=next;slot<4;slot++)WriteInt(output,slot*44,0);
            return new PracticeParticipantCompletionResult(records,output,configured,current,next,mode,names,formations);
        }

        // Modern inspection input, not recovered natural record initialization.
        public static byte[] CreateInspectionFixture(int current,byte pattern)
        {
            if(current<0||current>4)throw new ArgumentOutOfRangeException("current");
            if(pattern!=0x6d&&pattern!=0xa7)throw new ArgumentOutOfRangeException("pattern");
            var records=new byte[176];for(int n=0;n<records.Length;n++)records[n]=(byte)(pattern^(n%11));
            for(int slot=0;slot<current;slot++){WriteInt(records,slot*44,1);WriteWord(records,slot*44+4,slot);byte[] name=Encoding.ASCII.GetBytes("P"+slot+"\0");Array.Copy(name,0,records,slot*44+24,name.Length);}
            return records;
        }
        static void WriteInt(byte[] bytes,int at,int value){BitConverter.GetBytes(value).CopyTo(bytes,at);}
        static void WriteWord(byte[] bytes,int at,int value){BitConverter.GetBytes((short)value).CopyTo(bytes,at);}
    }
}
