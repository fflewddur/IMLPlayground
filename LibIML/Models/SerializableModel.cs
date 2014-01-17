using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    /// <summary>
    /// Save all of the important data for our model in a single file.
    /// </summary>
    [Serializable]
    public class SerializableModel
    {
        #pragma warning disable 0649 // Disable warnings that these are never used/assigned to.
        public List<IClassifier> Classifiers;
        public IEnumerable<IInstance> FullTrainingSet;
        public IEnumerable<IInstance> TestSet;
        #pragma warning restore 0649
    }
}
