﻿using MessagePredictor.ViewModel;
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
    /// Interaction logic for CommandBar.xaml
    /// </summary>
    public partial class CommandBar : UserControl
    {
        public CommandBar()
        {
            InitializeComponent();
        }

        private void LabelMessage_Click(object sender, RoutedEventArgs e)
        {
            FileToMenu.IsOpen = false;
        }
    }
}
