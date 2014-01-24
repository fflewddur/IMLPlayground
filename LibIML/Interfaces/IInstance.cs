using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    public interface IInstance
    {
        string AllText { get; }
        Dictionary<string, int> TokenCounts { get; set; }
        Label Label { get; set; }
        SparseVector Features { get; }
        Prediction Prediction { get; set; }

        void ComputeFeatureVector(Vocabulary vocab);
    }
}
