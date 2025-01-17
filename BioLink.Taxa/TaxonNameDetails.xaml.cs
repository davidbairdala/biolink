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
using System.Windows;
using System.Windows.Controls;
using BioLink.Data;
using BioLink.Data.Model;
using BioLink.Client.Extensibility;
using BioLink.Client.Utilities;
using System.Collections.Generic;
using System;

namespace BioLink.Client.Taxa {
    /// <summary>
    /// Interaction logic for TaxonNameDetails.xaml
    /// </summary>
    public partial class TaxonNameDetails : DatabaseCommandControl {
        
        private TaxonRank _rank;
        private TaxonNameViewModel _model;
        private List<Kingdom> _kingdomList;
        private Action<TaxonNameViewModel> _successAction;

        #region DesignerConstructor
        public TaxonNameDetails() {
            InitializeComponent();
        }
        #endregion

        public TaxonNameDetails(int? taxonId, User user, Action<TaxonNameViewModel> successAction)  : base(user, "TaxonNameDetails::" + taxonId.Value) {
            _successAction = successAction;

            var service = new TaxaService(user);
            Taxon taxon = service.GetTaxon(taxonId.Value);
            _rank = service.GetTaxonRank(taxon);
            _kingdomList = service.GetKingdomList();
            Kingdom kingdom = _kingdomList.Find((k) => k.KingdomCode.Equals(taxon.KingdomCode));

            _model = new TaxonNameViewModel(taxon, kingdom, _rank);

            _model.DataChanged += new DataChangedHandler(_model_DataChanged);

            InitializeComponent();

            cmbKingdom.ItemsSource = _kingdomList;

            this.chkChangedCombination.Visibility = (_rank != null && _rank.Category == "S" ? Visibility.Visible : Visibility.Hidden);

            if (taxon.AvailableName.ValueOrFalse() || taxon.LiteratureName.ValueOrFalse()) {

                string phraseCategory = "ALN Name Status";
                chkChangedCombination.Visibility = System.Windows.Visibility.Hidden;
                if (taxon.AvailableName.ValueOrFalse()) {
                    TaxonRank rank = service.GetTaxonRank(taxon);
                    
                    if (rank != null) {
                        switch (rank.Category.ToLower()) {
                            case "g": phraseCategory = "GAN Name Status";
                                break;
                            case "s": phraseCategory = "SAN Name Status";
                                break;
                        }
                    }
                }

                txtNameStatus.BindUser(PluginManager.Instance.User,  PickListType.Phrase, phraseCategory, TraitCategoryType.Taxon);
            } else {
                txtNameStatus.Visibility = System.Windows.Visibility.Collapsed;
                lblNameStatus.Visibility = System.Windows.Visibility.Collapsed;
            }

            this.DataContext = _model;
            this.ChangesCommitted += new PendingChangesCommittedHandler(TaxonNameDetails_ChangesCommitted);
        }

        void TaxonNameDetails_ChangesCommitted(object sender) {
            if (_successAction != null) {
                _successAction(_model);
            }
        }

        void _model_DataChanged(ChangeableModelBase model) {
            RegisterUniquePendingChange(new UpdateTaxonCommand(_model.Taxon));
        }

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(TaxonNameDetails), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnIsReadOnlyChanged)));

        public bool IsReadOnly {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        private static void OnIsReadOnlyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
            var control = (TaxonNameDetails)obj;
            if (control != null) {
                var readOnly = (bool) args.NewValue;
                control.txtAuthor.IsReadOnly = readOnly;
                control.txtName.IsReadOnly = readOnly;
                control.txtNameStatus.IsReadOnly = readOnly;
                control.txtYear.IsReadOnly = readOnly;
                control.cmbKingdom.IsEnabled = !readOnly;
                control.chkChangedCombination.IsEnabled = !readOnly;
                control.chkVerified.IsEnabled = !readOnly;
            }
        }

    }

    public class TaxonNameViewModel : TaxonViewModel {

        private Kingdom _kingdom;
        private TaxonRank _rank;

        public TaxonNameViewModel(Taxon taxon, Kingdom kingdom, TaxonRank rank) : base(null, taxon, null) {
            _kingdom = kingdom;
            _rank = rank;
        }

        public Kingdom Kingdom {
            get { return _kingdom; }
            set { 
                SetProperty("Kingdom", ref _kingdom, value); 
                base.KingdomCode = _kingdom.KingdomCode;
            }
        }        

        public string RankLongName {
            get { return _rank == null ? "Unranked" : _rank.GetElementTypeLongName(this.Taxon); }            
        }

        public bool IsVerified {
            get { return !Taxon.Unverified.ValueOrFalse(); }
            set { SetProperty(()=> Taxon.Unverified, Taxon, !value); }
        }

    }
}
