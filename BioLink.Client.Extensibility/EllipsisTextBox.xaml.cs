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

namespace BioLink.Client.Extensibility {
    /// <summary>
    /// Interaction logic for EllipsisTextBox.xaml
    /// </summary>
    public partial class EllipsisTextBox : UserControl {
        public EllipsisTextBox() {
            InitializeComponent();
        }

        private void btn_Click(object sender, RoutedEventArgs e) {

            if (this.Click != null) {
                Click(this, e);
            }

        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(EllipsisTextBox), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnTextChanged)));

        private static void OnTextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
            var control = (EllipsisTextBox)obj;
            control.txt.Text = args.NewValue as String;
            control.FireValueChanged(control.txt.Text);            
        }

        protected void FireValueChanged(string text) {
            if (this.TextChanged != null) {
                TextChanged(this, text);
            }
        }

        public String Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public event RoutedEventHandler Click;

        public event TextChangedHandler TextChanged;

        private void txt_TextChanged(object sender, TextChangedEventArgs e) {
            this.Text = txt.Text;
        }
    }
}