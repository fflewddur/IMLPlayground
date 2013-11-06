using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    /// <summary>
    /// Save all of the important data for our model in a single file.
    /// </summary>
    [Serializable]
    class SerializableModel
    {
        public IClassifier Classifier;
        public IInstances FullTrainingSet;
        public IInstances TestSet;
    }
}
