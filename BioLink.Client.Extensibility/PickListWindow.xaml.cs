﻿/*******************************************************************************
 * Copyright (C) 2011 Atlas of Living Australia
 * All Rights Reserved.
 * 
 * The contents of this file are subject to the Mozilla Public
 * License Version 1.1 (the "License"); you may not use this file
 * except in compliance with the License. You may obtain a copy of
 * the License at http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS
 * IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
 * implied. See the License for the specific language governing
 * rights and limitations under the License.
 ******************************************************************************/
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BioLink.Client.Utilities;
using BioLink.Data;
using BioLink.Data.Model;
using System.Collections.Generic;
using System.Windows.Media;


namespace BioLink.Client.Extensibility {
    /// <summary>
    /// Interaction logic for PickListWindow.xaml
    /// </summary>
    public partial class PickListWindow : Window {

        #region DesignerConstructor
        public PickListWindow() {
            InitializeComponent();
        }
        #endregion

        private Func<IEnumerable<object>> _itemsFunc;
        private Func<String, bool> _addItemFunc;

        private ObservableCollection<Object> _model;

        protected Control LaunchingControl { get; private set; }

        public PickListWindow(User user, string caption, Func<IEnumerable<object>> itemsFunc, Func<String, bool> addItemFunc, string initialFilter = "", Control PositionUnder = null, Control PositionUnderAncestor = null) {
            _itemsFunc = itemsFunc;
            _addItemFunc = addItemFunc;
            this.User = user;
            InitializeComponent();
            LaunchingControl = PositionUnder;

            if (PositionUnder == null || PositionUnderAncestor == null) {
                Config.RestoreWindowPosition(user, this);
            } else {
                Owner = PositionUnder.FindParentWindow();
                GeneralTransform transform = PositionUnder.TransformToAncestor(PositionUnderAncestor);
                var rootPoint = PositionUnder.PointToScreen(transform.Transform(new Point(0, 0)));
                Top = rootPoint.Y + PositionUnder.ActualHeight;
                Left = rootPoint.X;
                Width = PositionUnder.ActualWidth;
                Height = 250;
            }

            Title = caption;
            LoadModel();

            if (!string.IsNullOrWhiteSpace(initialFilter)) {
                txtFilter.Text = initialFilter;
            }

            btnAddNew.Visibility = System.Windows.Visibility.Hidden;

            if (_addItemFunc != null) {
                btnAddNew.Visibility = Visibility.Visible;
                btnAddNew.Click += new RoutedEventHandler((source, e) => {
                    string prefill = txtFilter.Text;
                    InputBox.Show(this, "Add a new value", "Enter the new value, and click OK", prefill, (text) => {
                        if (_addItemFunc(text)) {
                            _model.Add(text);
                            lst.SelectedItem = text;
                            this.DialogResult = true;
                            this.Hide();
                        }
                    });
                });
            }
            
        }

        public void LoadModel() {
            var list = _itemsFunc();
                        
            _model = new ObservableCollection<Object>();
            foreach (object item in list) {
                if (item != null) {
                    if (item is string) {
                        if (!String.IsNullOrWhiteSpace(item as string)) {
                            _model.Add((item as string).Trim());
                        }
                    } else {
                        _model.Add(item);
                    }
                }

            }
            lst.ItemsSource = _model;

            lst.SelectedIndex = 0;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) {
            this.Hide();
        }

        private void txtFilter_TypingPaused(string text) {
            FilterList(text);
        }

        private void FilterList(string text) {
            ListCollectionView dataView = CollectionViewSource.GetDefaultView(lst.ItemsSource) as ListCollectionView;

            if (String.IsNullOrEmpty(text)) {
                dataView.Filter = null;
                dataView.Refresh();
                return;
            }

            String searchTerm = text.ToLower();
            
            dataView.Filter = (obj) => {
                string test = null;
                if (obj is string) {
                    test = obj as string;
                } else if (obj is ViewModelBase) {
                    test = (obj as ViewModelBase).DisplayLabel;
                } else if (obj != null) {
                    test = obj.ToString();
                }
                if (test != null) {
                    return test.ToLower().Contains(searchTerm);
                }
                return false;
            };

            dataView.Refresh();

            lst.SelectedIndex = 0;
        }

        public object SelectedValue {
            get { return lst.SelectedItem; }
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e) {
            if (lst.SelectedItem != null) {
                this.DialogResult = true;
                this.Hide();
            }
        }

        private void txtFilter_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Down) {
                lst.SelectedIndex = 0;
                if (lst.SelectedItem != null) {
                    ListBoxItem item = lst.ItemContainerGenerator.ContainerFromItem(lst.SelectedItem) as ListBoxItem;
                    item.Focus();
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {            
            lst.Focus();
        }


        private void Window_Deactivated(object sender, EventArgs e) {
            if (LaunchingControl == null) {
                Config.SaveWindowPosition(User, this);
            }
        }

        private void lst_MouseDoubleClick(object sender, MouseButtonEventArgs e) {

            DependencyObject src = (DependencyObject)(e.OriginalSource);
            while (!(src is Control)) {
                src = VisualTreeHelper.GetParent(src);
            }

            if (src != null && src is ListViewItem) {
                if (lst.SelectedItem != null) {
                    this.DialogResult = true;
                    this.Hide();
                }
            }
        }

        protected User User { get; private set; }

    }
}
