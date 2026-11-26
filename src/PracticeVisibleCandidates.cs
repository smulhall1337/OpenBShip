using System;
using System.IO;
using System.Security.Cryptography;
namespace FleetLab
{
    // Original584 / maintained585 join: the produced owner flags feed whole
    //41BFB0 lookup unchanged. No candidate implies a launched/admitted attack.
    public sealed class PracticeVisibleCandidates
    {
        public PracticeOwnerVisibilityStudyState Visibility {get;private set;}
        public PracticeFleetCandidates Scan {get;private set;}
        PracticeVisibleCandidates(){}
        static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        static byte[] Table(string programs,string name,string hash){var b=File.ReadAllBytes(Path.Combine(programs,name));if(Hash(b)!=hash)throw new InvalidDataException("Visibility candidate table identity: "+name);return b;}
        public static PracticeVisibleCandidates Create(string programs,PracticeMapStartup startup,PracticeOwnerVisibilityStudyState visibility)
        {
            if(startup==null||visibility==null)throw new ArgumentNullException("copied visibility input");
            if(startup.Seed!=731||!visibility.Executed||visibility.Skipped||visibility.Preset<0||visibility.Preset>1||visibility.Mode!=0||(visibility.Owner!=0&&visibility.Owner!=1))throw new NotSupportedException("The joined candidate view admits completed copied-map normal passes (presets0/1),owners0/1,seed731 only. Controlled sensor and local-mode fixtures remain separate.");
            var metadata=Table(programs,"EquipmentDispatchKinds.seqbin","02CE3E390AC8866AD1F584102B9D10011B3D62663642AF1B117DE8A8D5A63A74");
            var ranges=Table(programs,"AiCandidateRawRanges.seqbin","BAD7B5737AE23238DF5F15E582D54426208E9A226AFF78F48696198B702DB68F");
            int actor=visibility.Owner==0?14:16;var task=PracticeFleetServiceContext.CreateDiagnostic(actor);
            var scan=PracticeFleetCandidates.Evaluate(startup.Stages[3].CopyShipPool(),visibility.CopyFleetPool(),task,metadata,ranges,actor,1,visibility.Owner);
            if(!scan.EquipmentFound||scan.RouteUiRequests!=0)throw new NotSupportedException("Joined view requires original equipped surface fleet; service-task fallback is outside this join.");
            return new PracticeVisibleCandidates{Visibility=visibility,Scan=scan};
        }
    }
}
