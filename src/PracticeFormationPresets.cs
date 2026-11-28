using System;using System.IO;using System.Collections.Generic;using System.Collections.ObjectModel;
namespace FleetLab
{
    public sealed class PracticeFormationPreset
    {
        readonly byte[] cells;
        public int Slot{get;private set;}public byte Kind{get;private set;}public byte RawOwner{get;private set;}
        internal PracticeFormationPreset(int slot,byte kind,byte owner,byte[] bytes){Slot=slot;Kind=kind;RawOwner=owner;cells=(byte[])bytes.Clone();}
        public int CellAt(int index){if(index<0||index>=100)throw new ArgumentOutOfRangeException("index");return BitConverter.ToInt32(cells,index*4);}
        public byte[] CopyCellBytes(){return(byte[])cells.Clone();}
        public override string ToString(){return "Slot "+Slot+" · tag "+Kind+" · owner byte "+RawOwner;}
    }
    public sealed class PracticeFormationPresetResult
    {
        readonly byte[] table,trailer,original;readonly ReadOnlyCollection<PracticeFormationPreset> records;
        public int LoadedRecordCount{get{return records.Count;}}public int VisitedSlots{get;private set;}public int ConsumedBytes{get;private set;}public int TotalInputBytes{get;private set;}public bool OpenSucceeded{get;private set;}
        public ReadOnlyCollection<PracticeFormationPreset> Records{get{return records;}}
        public int SourceReturnCode{get{return OpenSucceeded?0:-1;}}
        internal PracticeFormationPresetResult(byte[] table,byte[] source,int consumed,int slots,List<PracticeFormationPreset> records,bool opened){this.table=(byte[])table.Clone();original=(byte[])source.Clone();ConsumedBytes=consumed;VisitedSlots=slots;TotalInputBytes=source.Length;OpenSucceeded=opened;this.records=records.AsReadOnly();trailer=new byte[source.Length-consumed];Array.Copy(source,consumed,trailer,0,trailer.Length);}
        public byte[] CopyOriginalBytes(){return(byte[])original.Clone();}public byte[] CopyTable(){return(byte[])table.Clone();}public byte[] CopyTrailingBytes(){return(byte[])trailer.Clone();}
    }
    // Supported RIP:whole41B130/41B460, original1165. A tag1 terminator
    // ends parsing even when file bytes remain. Tags9/10 replace402-byte
    // slots; other tags advance one slot without writing its previous bytes.
    // Modern bounds require a terminator within the supplied capacity and
    // reject truncated/oversize input instead of reading outside buffers.
    public static class PracticeFormationPresets
    {
        public const int RecordBytes=402,MaximumSlots=4096,MaximumInputBytes=60000;
        static void CheckTable(byte[] before){if(before==null||before.Length==0||before.Length%RecordBytes!=0||before.Length/RecordBytes>MaximumSlots)throw new InvalidDataException("Formation table must contain1..4096 complete402-byte slots.");}
        public static PracticeFormationPresetResult FailedOpen(byte[] before){CheckTable(before);return new PracticeFormationPresetResult(before,new byte[0],0,0,new List<PracticeFormationPreset>(),false);}
        public static PracticeFormationPresetResult Read(byte[] source){return Apply(source,new byte[MaximumSlots*RecordBytes]);}
        public static PracticeFormationPresetResult Apply(byte[] source,byte[] before)
        {
            CheckTable(before);if(source==null)throw new ArgumentNullException("source");if(source.Length==0||source.Length>MaximumInputBytes)throw new InvalidDataException("Formation file outside the bounded60000-byte source read buffer.");
            var table=(byte[])before.Clone();var records=new List<PracticeFormationPreset>();int cursor=0;
            for(int slot=0;slot<table.Length/RecordBytes;slot++)
            {
                if(cursor>=source.Length)throw new InvalidDataException("Missing formation terminator.");byte tag=source[cursor++];if(tag==1)return new PracticeFormationPresetResult(table,source,cursor,slot,records,true);
                if(tag!=9&&tag!=10)continue;
                if(cursor>source.Length-401)throw new InvalidDataException("Truncated formation owner/cell block.");byte owner=source[cursor++];var cells=new byte[400];Array.Copy(source,cursor,cells,0,400);cursor+=400;int at=slot*RecordBytes;table[at]=tag;table[at+1]=owner;Array.Copy(cells,0,table,at+2,400);records.Add(new PracticeFormationPreset(slot,tag,owner,cells));
            }
            throw new InvalidDataException("Formation terminator exceeds the supplied table capacity; no partial result published.");
        }
    }
}
