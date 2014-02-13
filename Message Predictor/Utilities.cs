using LibIML;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MessagePredictor.Utils
{
    public static class Utilities
    {
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }

        // From http://stackoverflow.com/questions/2336989/enumerate-itemscontrol-items-as-uielements
        public static T FindVisualChild<T>(this DependencyObject instance) where T : DependencyObject
        {
            T control = default(T);

            if (instance != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(instance); i++)
                {
                    if ((control = VisualTreeHelper.GetChild(instance, i) as T) != null)
                    {
                        break;
                    }

                    control = FindVisualChild<T>(VisualTreeHelper.GetChild(instance, i));
                }
            }

            return control;
        }
    }
}
