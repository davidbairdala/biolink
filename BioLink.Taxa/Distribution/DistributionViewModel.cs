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
using BioLink.Client.Extensibility;
using BioLink.Client.Utilities;
using BioLink.Data.Model;

namespace BioLink.Client.Taxa {

    public class DistributionPlaceholder : HierarchicalViewModelBase {
        private string _label;
        public DistributionPlaceholder(string label) {
            _label = label;
        }

        public override string DisplayLabel {
            get {
                return _label;
            }
        }

        public override int? ObjectID {
            get { return null; }
        }

    }

    public class DistributionViewModel : DistributionPlaceholder {

        public DistributionViewModel(TaxonDistribution model, string label) : base(label) {
            Model = model;
        }

        public TaxonDistribution Model { get; set; }

        public int TaxonID {
            get { return Model.TaxonID; }
            set { SetProperty(() => Model.TaxonID, Model, value); }
        }

        public int BiotaDistID {
            get { return Model.BiotaDistID; }
            set { SetProperty(() => Model.BiotaDistID, Model, value); }
        }

        public int DistRegionID {
            get { return Model.DistRegionID; }
            set { SetProperty(() => Model.DistRegionID, Model, value); }
        }

        public bool Introduced {
            get { return Model.Introduced; }
            set { SetProperty(() => Model.Introduced, Model, value); }
        }

        public bool Uncertain {
            get { return Model.Uncertain; }
            set { SetProperty(() => Model.Uncertain, Model, value); }
        }

        public bool ThroughoutRegion {
            get { return Model.ThroughoutRegion; }
            set { SetProperty(() => Model.ThroughoutRegion, Model, value); }
        }

        public string Qual {
            get { return Model.Qual; }
            set { SetProperty(() => Model.Qual, Model, value); }
        }

        public string DistRegionFullPath {
            get { return Model.DistRegionFullPath; }
            set { SetProperty(() => Model.DistRegionFullPath, Model, value); }
        }

        public override int? ObjectID {
            get { return Model.BiotaDistID; }
        }


    }
}
