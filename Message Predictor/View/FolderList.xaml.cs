using MessagePredictor.Model;
using MessagePredictor.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MessagePredictor.View
{
    /// <summary>
    /// Interaction logic for FolderList.xaml
    /// </summary>
    public partial class FolderList : UserControl
    {
        public FolderList()
        {
            InitializeComponent();
        }
    }

    public class FolderListItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate UnknownTemplate { get; set; }
        public DataTemplate TopicTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FolderViewModel folder = item as FolderViewModel;
            if (folder != null) {
                if (folder.Label.UserLabel == "Unknown") {
                    return UnknownTemplate;
                } else {
                    return TopicTemplate;
                }
            } else {
                // Something went wrong, return the default datatemplate
                return base.SelectTemplate(item, container);
            }
        }
    }
}
