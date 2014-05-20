using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LibIML
{
    public interface IInstance
    {
        int Id { get; }
        int Order { get; }
        string AllText { get; }
        Dictionary<string, int> TokenCounts { get; set; }
        Label UserLabel { get; set; }
        Label GroundTruthLabel { get; set; }
        SparseVector Features { get; }
        Prediction Prediction { get; set; }
        Prediction PreviousPrediction { get; }
        bool RecentlyChanged { get; }
        
        bool TokenizeForPattern(Regex pattern, string token);
        void ComputeFeatureVector(Vocabulary vocab, bool isRestricted);
    }
}
