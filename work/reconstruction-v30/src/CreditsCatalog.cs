using System;using System.IO;using System.Text;using System.Collections.Generic;using System.Security.Cryptography;
namespace FleetLab {
 public sealed class CreditsCatalog {
  public readonly byte[] OriginalBytes;readonly string[] names=new string[137];readonly HashSet<string> lookup=new HashSet<string>(StringComparer.Ordinal);
  public string[] Names{get{return (string[])names.Clone();}}
  public static CreditsCatalog Load(string assets){return Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","CreditsMovies.seqbin")));}
  CreditsCatalog(byte[] bytes){OriginalBytes=(byte[])bytes.Clone();for(int id=0;id<138;id++){int at=id*9,n=0;while(n<9&&bytes[at+n]!=0)n++;if(n==0||n>8)throw new InvalidDataException("Movie name termination");for(int p=0;p<n;p++){byte c=bytes[at+p];if(!(c>=65&&c<=90||c>=48&&c<=57||c==95))throw new InvalidDataException("Movie name encoding");}for(int p=n;p<9;p++)if(bytes[at+p]!=0)throw new InvalidDataException("Movie name padding");string name=Encoding.ASCII.GetString(bytes,at,n);if(id==0){if(name!="NOWEB")throw new InvalidDataException("Movie sentinel");}else{if(!lookup.Add(name))throw new InvalidDataException("Duplicate movie name");names[id-1]=name;}}}
  public static CreditsCatalog Read(byte[] bytes){if(bytes==null||bytes.Length!=138*9)throw new InvalidDataException("Credits movie table size");using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","")!="9E51D032AD352CE3EDEF862C0A804D556DD2D35896C35DA13713903EBF11811D")throw new InvalidDataException("Unsupported Credits movie table");return new CreditsCatalog(bytes);}
  public string Name(int id){if(id<1||id>137)throw new ArgumentOutOfRangeException("id");return names[id-1];}
  public string Choose(NativeRandom random){if(random==null)throw new ArgumentNullException("random");return Name(random.Next()%137+1);}
  public string Directory(string name,bool high){if(name==null||!lookup.Contains(name))throw new InvalidDataException("Movie outside Credits catalog");return high?"HWEBS":"LWEBS";}
 }
}
