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
using BioLink.Client.Utilities;
using BioLink.Client.Extensibility;
using BioLink.Data;
using BioLink.Data.Model;

namespace BioLink.Client.Material {
    /// <summary>
    /// Interaction logic for TaxaForSiteReportOptions.xaml
    /// </summary>
    public partial class TaxaForSiteReportOptions : Window {

        public TaxaForSiteReportOptions(User user, SiteExplorerNode node) {
            InitializeComponent();
            this.User = user;            

            txtRegion.BindUser(user, LookupType.SiteOrRegion);
            txtRegion.PreSelect(node.ObjectID, node.Name, MaterialExplorer.GetLookupTypeFromElemType(node.ElemType));

            txtRegion.WatermarkText = "You must select a site or region";
            txtTaxon.BindUser(user, LookupType.Taxon);
            txtTaxon.WatermarkText = "All taxa";
        }

        protected User User { get; private set; }

        public int? SiteRegionID {
            get { return txtRegion.ObjectID; }
        }

        public string SiteRegionName {
            get { return txtRegion.Text; } 
        }

        public string SiteRegionElemType { 
            get { return (txtRegion.SelectedObject != null ? txtRegion.SelectedObject.LookupType.ToString() : null); }
        }
        

        public int? TaxonID { 
            get { 
                return txtTaxon.ObjectID; 
            } 
        }

        public string TaxonName {
            get { return txtTaxon.Text; }
        }

        private void btnCnacel_Click(object sender, RoutedEventArgs e) {
            this.DialogResult = false;
            this.Close();
        }

        private void button1_Click(object sender, RoutedEventArgs e) {
            if (Validate()) {
                this.DialogResult = true;
                this.Close();
            }
        }

        private bool Validate() {

            if (txtRegion.SelectedObject == null) {
                ErrorMessage.Show("You must select a valid site or region!");
                return false;
            }

            return true;
        }

    }
}
