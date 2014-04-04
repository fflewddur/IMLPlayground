using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor.Model
{
    class UserAction
    {
        public enum ActionType
        {
            LabelMessage,
            AddFeature,
            RemoveFeature,
            AdjustFeaturePrior
        }
    }
}
