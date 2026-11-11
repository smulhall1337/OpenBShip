using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace FleetLab {
    // Bs.exe B9A8AEFD... 4201F0. This is text formatting only; glyph rendering
    // and the modern menu's readable font remain separate presentation choices.
    public static class ClassicMenuText {
        public const int RowCount=48, RowStride=33, TableBytes=RowCount*RowStride;
        public static byte[] Format(string text,byte[] previous=null){
            if(text==null||text.Length>8192)throw new InvalidDataException("Invalid original menu text length");
            foreach(char c in text)if(c<32||c>126)throw new InvalidDataException("Original menu text requires printable ASCII");
            if(previous!=null&&previous.Length!=TableBytes)throw new InvalidDataException("Original menu text table requires 1584 bytes");
            var result=previous==null?new byte[TableBytes]:(byte[])previous.Clone();
            for(int row=0;row<RowCount;row++)result[row*RowStride]=0;
            int at=0,line=0;
            while(at<text.Length){
                int count=text.Length-at;
                if(count>RowStride){
                    count=RowStride-1;
                    while(count>=0&&text[at+count]!=' ')count--;
                    if(count<0)throw new InvalidDataException("Original menu text has no space in its 33-byte wrapping window");
                }
                int output=line*RowStride;
                if(output+count>=TableBytes)throw new InvalidDataException("Original menu text terminator exceeds the last row");
                for(int i=0;i<count;i++)result[output+i]=(byte)text[at+i];
                result[output+count]=0;
                at+=count;
                if(at<text.Length)at++;
                line=(line+1)%RowCount;
                result[line*RowStride]=0;
            }
            return result;
        }
        public static string[] ReadRows(byte[] table){
            if(table==null||table.Length!=TableBytes)throw new InvalidDataException("Original menu text table requires 1584 bytes");
            var result=new string[RowCount];
            for(int row=0;row<RowCount;row++){
                int at=row*RowStride,count=0;
                while(count<RowStride&&table[at+count]!=0){
                    if(table[at+count]<32||table[at+count]>126)throw new InvalidDataException("Original menu text row contains unsupported bytes");
                    count++;
                }
                if(count==RowStride&&(row==RowCount-1||table[at+count]!=0))throw new InvalidDataException("Original menu text row lacks a bounded terminator");
                result[row]=Encoding.ASCII.GetString(table,at,count);
            }
            return result;
        }
        public static void ExportPractice(string assets,string output){
            string input=Path.Combine(assets,"Graphics","SCNTEXT.TXT");
            string[] descriptions=File.ReadAllLines(input);
            if(descriptions.Length!=21)throw new InvalidDataException("Expected 21 original Practice descriptions");
            var lines=new List<string>{"Recovered original Practice descriptions","Formatter: 33-byte window and 48 rows; glyph rendering is not exported."};
            using(var hash=SHA256.Create())lines.Add("SCNTEXT.TXT SHA256="+BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(input))).Replace("-",""));
            for(int index=0;index<descriptions.Length;index++){
                lines.Add("");lines.Add("[Description "+index+"]");
                string[] rows=ReadRows(Format(descriptions[index]));
                int last=rows.Length-1;while(last>=0&&rows[last].Length==0)last--;
                for(int row=0;row<=last;row++)lines.Add(rows[row]);
            }
            string path=Path.GetFullPath(output);Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path,lines.ToArray(),new UTF8Encoding(false));
        }
    }
}
