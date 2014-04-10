using LibIML;
using LibIML.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor.Model
{
    public class UserAction
    {
        public enum ActionType
        {
            LabelMessage,
            AddFeature,
            RemoveFeature,
            AdjustFeaturePrior
        }

        private ActionType _type;
        private Feature _feature;
        private Label _label;
        private string _desc;

        public UserAction(ActionType type, Feature feature, Label label)
        {
            Type = type;
            Label = label;
            // Copy our feature; a reference won't work because the user is editing it
            Feature = new Feature(feature);
            Desc = BuildDescription(Type, Feature, Label);
        }

        #region Properties

        public ActionType Type
        {
            get { return _type; }
            private set { _type = value; }
        }

        public Feature Feature
        {
            get { return _feature; }
            private set { _feature = value; }
        }

        public Label Label
        {
            get { return _label; }
            private set { _label = value; }
        }

        public string Desc
        {
            get { return _desc; }
            private set { _desc = value; }
        }

        #endregion

        #region Private methods

        private string BuildDescription(ActionType type, Feature feature, Label label)
        {
            string desc;

            switch(type) {
                case ActionType.AdjustFeaturePrior:
                    desc = string.Format("Undo adjust importance of '{0}' to {1}", feature.Characters, label);
                    break;
                case ActionType.AddFeature:
                    desc = string.Format("Undo add '{0}' to list of important words", feature.Characters);
                    break;
                case ActionType.RemoveFeature:
                    desc = string.Format("Undo remove '{0}' from list of important words", feature.Characters);
                    break;
                default:
                    desc = "Unknown action";
                    break;
            }

            return desc;
        }

        #endregion
    }
}
