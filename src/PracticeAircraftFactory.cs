using System;using System.IO;using System.Collections.Generic;using System.Collections.ObjectModel;
namespace FleetLab {
 public sealed class PracticeAircraftFactoryResult {
  readonly byte[] ships,fleets;readonly ReadOnlyCollection<int> allocated;
  internal PracticeAircraftFactoryResult(byte[] s,byte[] f,List<int> slots){ships=s;fleets=f;allocated=slots.AsReadOnly();}
  public ReadOnlyCollection<int> AllocatedUnits {get{return allocated;}}
  public byte[] CopyShipPool(){return (byte[])ships.Clone();}public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
 }
 // Pinned RIP B9A8...6A569:42C810/42C980/42CAF0 plus42CEF0/42CFE0
 // and their ammo/equipment/speed/visibility/grid finalizers. Caller-supplied
 // parent/destination/link only; no attachment discovery, flight or missions.
 public static class PracticeAircraftFactory {
  static int I(byte[] b,int p){return BitConverter.ToInt32(b,p);}static int W(byte[] b,int p){return BitConverter.ToInt16(b,p);}static void I(byte[] b,int p,int v){BitConverter.GetBytes(v).CopyTo(b,p);}static void W(byte[] b,int p,int v){BitConverter.GetBytes(unchecked((short)v)).CopyTo(b,p);}
  static int Reference(uint value){long at=(long)value-0x4fc340;if(at<0||at>=402000||at%268!=0)throw new InvalidDataException("Invalid stored-aircraft roster reference.");return (int)at;}
  static int Allocate(byte[] ships,byte[] fleets){
   var reserved=new HashSet<int>();for(int f=0;f<250;f++){int at=f*492,kind=I(fleets,at);if(kind!=2&&kind!=10)continue;for(int n=0;n<6;n++){uint r=BitConverter.ToUInt32(fleets,at+16+n*4);if(r==0)continue;int p=Reference(r);if(I(ships,p)==0)reserved.Add(W(ships,p+4));}}
   for(int unit=0;unit<1500;unit++){int at=unit*268;if(I(ships,at)!=0||unit<1499&&(I(ships,at+268)==22||I(ships,at+268)==23)||reserved.Contains(unit))continue;return unit;}throw new InvalidDataException("Stored-aircraft creation has no admitted free unit slot; no partial result was published.");
  }
  static void Construct(PracticeUnitCatalog catalog,byte[] ships,int slot,int type,int owner,int fleet,int x,int y){
   int at=slot*268;Array.Copy(catalog.Templates,type*268,ships,at,268);W(ships,at+4,slot);W(ships,at+6,owner);W(ships,at+8,x);W(ships,at+10,y);W(ships,at+12,fleet);int length=W(ships,at+14);if(length<1||length>6)throw new InvalidDataException("Unsupported aircraft template extent.");
   for(int n=0;n<35;n++)W(ships,at+196+n*2,0);for(int n=0;n<length*4;n++){int weapon=I(ships,at+100+n*4);if(weapon<0||weapon>39)throw new InvalidDataException("Unsupported stored-aircraft equipment.");int destination=at+196+weapon*2;if(destination>ships.Length-2)throw new InvalidDataException("Aircraft ammunition crosses the pool boundary.");int amount=W(catalog.Ammunition,weapon*2);W(ships,destination,amount==-1?-1:W(ships,destination)+amount);}PracticeUnitDepletion.Reevaluate(catalog,ships,at);
  }
  public static PracticeAircraftFactoryResult CreateOffscreen(PracticeUnitCatalog catalog,byte[] shipPool,byte[] fleetPool,int parentShip,int parentFleet,int destination,int kind,int link,short localOwner){
   if(catalog==null)throw new ArgumentNullException("catalog");if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000)throw new InvalidDataException("Aircraft creation requires complete ship and fleet pools.");if(parentShip<0||parentShip>=1500||parentFleet<0||parentFleet>=250||destination<0||destination>=250||destination==parentFleet||kind<6||kind>8||link<0||link>3)throw new ArgumentOutOfRangeException("destination");
   int at=destination*492,origin=parentFleet*492,owner=W(fleetPool,origin+6);if(I(fleetPool,at)!=0||W(fleetPool,at+4)!=destination)throw new InvalidDataException("Aircraft destination requires an initialized inactive fleet record.");if(I(shipPool,parentShip*268)==0)throw new InvalidDataException("Aircraft associated parent must be a live unit in this profile.");if(owner==localOwner)throw new NotSupportedException("Local-owner equipment UI continuation is outside the offscreen aircraft profile.");
   byte[] ships=(byte[])shipPool.Clone(),fleets=(byte[])fleetPool.Clone();I(fleets,at,kind);W(fleets,at+8,parentShip);W(fleets,at+10,link);I(ships,parentShip*268+56+link*4,0x4b47a0+destination*492);W(fleets,at+12,W(fleets,origin+12));W(fleets,at+14,W(fleets,origin+14));W(fleets,at+6,owner);fleets[at+63]=1;fleets[at+484]=(byte)(link+1);
   int type=kind==6?10:kind==7?9:8;int[] xs=kind==6?new[]{3,6,3,6}:kind==7?new[]{4,1,4,7}:new[]{4};int[] ys=kind==6?new[]{1,3,5,7}:kind==7?new[]{0,5,5,5}:new[]{3};var allocated=new List<int>();for(int n=0;n<xs.Length;n++){int unit=Allocate(ships,fleets);Construct(catalog,ships,unit,type,owner,destination,xs[n],ys[n]);I(fleets,at+16+n*4,0x4fc340+unit*268);allocated.Add(unit);}
   byte speed=0;for(int n=0;n<6;n++){uint r=BitConverter.ToUInt32(fleets,at+16+n*4);if(r==0)continue;int p=Reference(r);if(I(ships,p)==0)continue;int current=W(ships,p+72);if(speed==0)speed=unchecked((byte)current);if(speed>current)speed=unchecked((byte)current);}W(fleets,at+60,speed);Array.Copy(catalog.Colors,kind*3,fleets,at+464,3);
   for(int n=0;n<100;n++)I(fleets,at+64+n*4,0);for(int n=0;n<6;n++){uint r=BitConverter.ToUInt32(fleets,at+16+n*4);if(r==0)continue;int p=Reference(r);if(I(ships,p)==0)continue;int start=W(ships,p+8)*10+W(ships,p+10),length=W(ships,p+14);if(length<0||length>6||start<0||start+length>100)throw new InvalidDataException("Aircraft occupancy crosses the admitted grid.");for(int q=0;q<length;q++)I(fleets,at+64+(start+q)*4,2);}
   return new PracticeAircraftFactoryResult(ships,fleets,allocated);
  }
 }
}
