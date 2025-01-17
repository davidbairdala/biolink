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
using BioLink.Data;
using BioLink.Data.Model;
using BioLink.Client.Utilities;

using System.Collections.ObjectModel;

namespace BioLink.Client.Extensibility {
    /// <summary>
    /// Interaction logic for ReferencesControl.xaml
    /// </summary>
    public partial class ReferencesControl : DatabaseCommandControl {

        private ObservableCollection<RefLinkViewModel> _model;

        #region Designer Constructor
        public ReferencesControl() {
            InitializeComponent();
        }
        #endregion

        public ReferencesControl(User user, TraitCategoryType category, int? intraCategoryID) : base(user, String.Format("References::{0}::{1}", category, intraCategoryID)) {
            InitializeComponent();

            this.Category = category;
            this.IntraCategoryID = intraCategoryID.Value;

            txtRefType.Click += new RoutedEventHandler((source, e) => {
                var reftype = SelectRefType();
                if (!string.IsNullOrEmpty(reftype)) {
                    txtRefType.Text = reftype;
                }

            });

            txtReference.BindUser(user, LookupType.Reference);
            txtReference.ObjectSelected += new ObjectSelectedHandler(txtReference_ObjectSelected);

            var list = Service.GetReferenceLinks(category.ToString(), IntraCategoryID);
            _model = new ObservableCollection<RefLinkViewModel>(list.ConvertAll((rl) => {
                var vm = new RefLinkViewModel(rl);
                vm.DataChanged += new DataChangedHandler((changed) => {
                    RegisterUniquePendingChange(new UpdateRefLinkCommand(vm.Model, Category.ToString()));
                });
                return vm;
            }));

            gridRefLink.IsEnabled = false;
            
            lstReferences.ItemsSource = _model;
            lstReferences.SelectionChanged += new SelectionChangedEventHandler((source, e) => {
                if (!gridRefLink.IsEnabled) {
                    gridRefLink.IsEnabled = true;
                }
                gridRefLink.DataContext = lstReferences.SelectedItem;
            });
        }

        void txtReference_ObjectSelected(object source, SelectionResult result) {
            var current = lstReferences.SelectedItem as RefLinkViewModel;

            if (current != null && result.ObjectID.HasValue) {
                current.FullRTF = "";
                var searchResult = result.DataObject as ReferenceSearchResult;
                if (searchResult != null) {
                    current.FullRTF = searchResult.RefRTF;
                } else {
                    var service = new SupportService(User);
                    var reference = service.GetReference(result.ObjectID.Value);
                    if (reference != null) {
                        current.FullRTF = reference.FullRTF;
                    }
                }
            }            
        }


        private string SelectRefType() {
            var frm = new PickListWindow(User, "Reference Link types", () => {
                var list = Service.GetRefLinkTypes();    
                SortedDictionary<string, string> filtered = new SortedDictionary<string, string>();
                // Remove the duplicates...Something really dodgey is going on when inserting ref links, it looks like
                // duplicate ref link types are being created.
                foreach (string item in list) {
                    filtered[item] = item;
                }
                return filtered.Keys;
            }, 
                
                (newval) => {
                Service.InsertRefLinkType(newval, Category.ToString());
                return true;
            });

            if (frm.ShowDialog().ValueOrFalse()) {
                return frm.SelectedValue as string;
            }

            return null;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e) {
            DeleteSelectedRefLink();
        }

        private void DeleteSelectedRefLink() {
            var item = lstReferences.SelectedItem as RefLinkViewModel;
            if (item != null) {
                RegisterPendingChange(new DeleteRefLinkCommand(item.Model));
                _model.Remove(item);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e) {
            AddNewRefLink();
        }

        private void AddNewRefLink() {

            var refLinkType = SelectRefType();
            if (!string.IsNullOrEmpty(refLinkType)) {
                var data = new RefLink();
                data.RefLinkID = -1;
                data.RefLinkType = refLinkType;
                data.IntraCatID = IntraCategoryID;
                data.UseInReport = true;

                RegisterPendingChange(new InsertRefLinkCommand(data, Category.ToString()));

                var viewModel = new RefLinkViewModel(data);
                viewModel.RefCode = NextNewName("<New {0}>", _model, () => viewModel.RefCode);
                _model.Add(viewModel);

                viewModel.DataChanged += new DataChangedHandler((changed) => {
                    RegisterUniquePendingChange(new UpdateRefLinkCommand(viewModel.Model, Category.ToString()));
                });

                lstReferences.SelectedItem = viewModel;
                lstReferences.ScrollIntoView(viewModel);
            }
        }

        #region Properties

        public TraitCategoryType Category { get; private set; }

        public int IntraCategoryID { get; private set; }

        public SupportService Service { get { return new SupportService(User); } }

        #endregion

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(ReferencesControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnIsReadOnlyChanged)));

        public bool IsReadOnly {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        private static void OnIsReadOnlyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
            var control = (ReferencesControl)obj;
            if (control != null) {
                var readOnly = (bool)args.NewValue;
                control.txtPage.IsReadOnly = readOnly;
                control.txtQual.IsReadOnly = readOnly;
                control.txtReference.IsReadOnly = readOnly;
                control.txtRefType.IsReadOnly = readOnly;

                control.chkUseInReports.IsEnabled = !readOnly;
                control.btnAdd.IsEnabled = !readOnly;
                control.btnDelete.IsEnabled = !readOnly;
            }
        }

    }
}
