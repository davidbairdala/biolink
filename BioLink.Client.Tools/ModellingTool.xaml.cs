﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BioLink.Client.Extensibility;
using BioLink.Client.Utilities;
using BioLink.Data;
using BioLink.Data.Model;

namespace BioLink.Client.Tools {
    /// <summary>
    /// Interaction logic for ModellingTool.xaml
    /// </summary>
    public partial class ModellingTool : UserControl {

        public ModellingTool(User user, ToolsPlugin owner) {
            InitializeComponent();
            this.User = user;
            this.Owner = owner;
        }

        protected User User { get; private set; }

        protected ToolsPlugin Owner { get; private set; }

        private void btnCancel_Click(object sender, RoutedEventArgs e) {
            this.FindParentWindow().Close();
        }
    }
}