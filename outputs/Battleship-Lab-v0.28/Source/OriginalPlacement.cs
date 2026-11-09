using System;using System.Drawing;using System.IO;
namespace FleetLab {
 public sealed class PlacementResult {public readonly Point[] Positions;public readonly int Calls,Restarts;internal PlacementResult(Point[] p,int calls,int restarts){Positions=(Point[])p.Clone();Calls=calls;Restarts=restarts;}}
 // Bs.exe B9A8...A569 41B820..41B99F. Fixed-Y, six pointer slots maximum.
 public static class OriginalPlacement {
  public static PlacementResult Generate(int[] lengths,Func<int> next,Point[] previous=null,int maximumRestarts=256,Action<Point[]> beforeRoll=null){
   if(lengths==null||lengths.Length<1||lengths.Length>6||next==null||maximumRestarts<0||maximumRestarts>100000||previous!=null&&previous.Length!=lengths.Length)throw new ArgumentException("Placement input bounds");
   int[] sizes=(int[])lengths.Clone();foreach(int size in sizes)if(size<0||size>10)throw new ArgumentException("Placement lengths must be0..10;0is inactive");
   var positions=previous==null?new Point[sizes.Length]:(Point[])previous.Clone();if(previous==null)for(int i=0;i<positions.Length;i++)positions[i]=new Point(-1,-1);Action clear=delegate{for(int i=0;i<sizes.Length;i++)if(sizes[i]!=0)positions[i]=new Point(-1,-1);};clear();int calls=0,restarts=0;
   Func<int> roll=delegate{if(beforeRoll!=null)beforeRoll((Point[])positions.Clone());calls++;int value=next();if(value<0||value>32767)throw new InvalidDataException("Placement RNG must return the native15-bit range");return value;};
   while(true){int selected=-1,longest=0;for(int i=0;i<sizes.Length;i++)if(sizes[i]>longest&&positions[i].X==-1){selected=i;longest=sizes[i];}if(selected<0)return new PlacementResult(positions,calls,restarts);
    bool placed=false;for(int attempt=0;attempt<100;attempt++){int x=roll()%10,y=roll()%(longest-11);bool collision=false;for(int i=0;i<sizes.Length;i++)if(sizes[i]!=0&&positions[i].X==x&&positions[i].X!=-1&&y>=positions[i].Y-longest+1&&y<=positions[i].Y+sizes[i]-1){collision=true;break;}if(!collision){positions[selected]=new Point(x,y);placed=true;break;}}
    if(!placed){clear();restarts++;if(restarts>maximumRestarts)throw new InvalidOperationException("Original fleet placement exceeded the modern restart limit; try another seed. The native loop is unbounded.");}
   }
  }
 }
}
