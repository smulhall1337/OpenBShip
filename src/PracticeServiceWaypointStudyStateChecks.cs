using System;using System.IO;using System.Security.Cryptography;
namespace FleetLab
{
    internal static class PracticeServiceWaypointStudyStateChecks
    {
        static void Need(bool b,string s){if(!b)throw new InvalidDataException(s);}static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static bool Reject(Action a){try{a();return false;}catch(ArgumentException){return true;}catch(InvalidDataException){return true;}catch(NotSupportedException){return true;}catch(InvalidOperationException){return true;}}
        internal static void Run(Action<string,Action> check,string assets)
        {
            string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var source=File.ReadAllBytes(Path.Combine(assets,"Graphics","BSSB202.SCN"));Func<PracticeMapStartup> startup=delegate{return PracticeMapStartup.CreateBssb202(programs,source,731);};
            check("Service waypoint study presets expose nearest tie cache gap and skip outcomes without travel",delegate
            {
                var basis=PracticeServiceWaypointStudyBasis.Create(programs,startup());int[] yards={3,2,-1,2,-1,-1,-1,3,3},indices={2,1,2,0,-1,-1,-1,4,0};for(int p=0;p<PracticeServiceWaypointStudyBasis.Presets.Count;p++){var before=basis.CreateFixture(p);var after=before.Execute();Need(after.Executed&&after.Preset==p&&after.Result.ReplacedWaypoint==indices[p]&&(after.Result.Destination==null?-1:after.Result.Destination.SelectedYard)==yards[p]&&BitConverter.ToInt16(after.CopyFleetPool(),16*492+12)==before.OriginX&&BitConverter.ToInt16(after.CopyFleetPool(),16*492+14)==before.OriginY,"Study preset output "+p);Need(BitConverter.ToInt32(after.Backing.CopyTaskRecord(16),200)==(p>=4&&p<=6?4:2),"Study goal "+p);}var cached=basis.CreateFixture(2).Execute();Need(BitConverter.ToInt16(cached.CopyFleetPool(),16*492+44)==550&&BitConverter.ToInt16(cached.CopyFleetPool(),16*492+54)==290,"Readable supplied cache output");var gap=basis.CreateFixture(3).Execute();Need(BitConverter.ToInt16(gap.CopyFleetPool(),16*492+44)==120&&BitConverter.ToInt16(gap.CopyFleetPool(),16*492+48)==140,"Opaque post-gap points lost");
            });
            check("Service waypoint study reset preserves applied copy and all published snapshots are owned",delegate
            {
                var map=startup();string mapHash=Hash(map.Stages[3].CopyFleetPool());var basis=PracticeServiceWaypointStudyBasis.Create(programs,map);var before=basis.CreateFixture(0);string fh=Hash(before.CopyFleetPool()),rh=Hash(before.Backing.CopyRegion()),sh=Hash(before.CopyShipPool());var after=before.Execute();var reset=after.Reset();Need(!reset.Executed&&reset.Preset==0&&Hash(reset.CopyFleetPool())==fh&&Hash(reset.Backing.CopyRegion())==rh&&Hash(before.CopyFleetPool())==fh&&Hash(before.CopyShipPool())==sh&&Hash(map.Stages[3].CopyFleetPool())==mapHash,"Study reset changed basis/startup");var copy=after.CopyFleetPool();copy[0]^=255;copy=after.CopyShipPool();copy[0]^=255;copy=after.Backing.CopyRegion();copy[copy.Length-1]^=255;Need(Hash(after.CopyFleetPool())==Hash(reset.Execute().CopyFleetPool())&&Hash(after.CopyShipPool())==sh&&Hash(after.Backing.CopyRegion())==Hash(reset.Execute().Backing.CopyRegion()),"Study accessor alias");
            });
            check("Service waypoint study bounds callback repeats unsupported seeds and invalid presets",delegate
            {
                var basis=PracticeServiceWaypointStudyBasis.Create(programs,startup());var state=basis.CreateFixture(0).Execute();Need(Reject(delegate{state.Execute();})&&Reject(delegate{basis.CreateFixture(-1);})&&Reject(delegate{basis.CreateFixture(9);})&&Reject(delegate{PracticeServiceWaypointStudyBasis.Create(programs,null);})&&Reject(delegate{PracticeServiceWaypointStudyBasis.Create(programs,PracticeMapStartup.CreateBssb202(programs,source,732));}),"Unsupported study operation admitted");Need(state.Reset().Execute().Result.Destination.SelectedYard==3,"Repeat after reset failed");
            });
        }
    }
}
