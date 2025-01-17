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
using BioLink.Data.Model;
using BioLink.Data;

namespace BioLink.Client.Tools {

    public class UpdateTaxonRefLinkCommand : GenericDatabaseCommand<TaxonRefLink> {

        public UpdateTaxonRefLinkCommand(TaxonRefLink model)
            : base(model) {
        }

        protected override void ProcessImpl(User user) {
            var service = new SupportService(user);
            service.UpdateTaxonRefLink(Model);
        }


        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SPIN_TAXON, PERMISSION_MASK.UPDATE);
        }
    }

    public class InsertTaxonRefLinkCommand : GenericDatabaseCommand<TaxonRefLink> {

        public InsertTaxonRefLinkCommand(TaxonRefLink model)
            : base(model) {
        }

        protected override void ProcessImpl(User user) {
            var service = new SupportService(user);

            var reflink = new RefLink();
            reflink.RefLinkID = Model.RefLinkID;
            reflink.IntraCatID = Model.BiotaID;
            reflink.RefID = Model.RefID;
            reflink.RefPage = Model.RefPage;
            reflink.RefQual = Model.RefQual;
            reflink.RefLinkType = Model.RefLink;
            reflink.UseInReport = Model.UseInReports;

            Model.RefLinkID = service.InsertRefLink(reflink, TraitCategoryType.Taxon.ToString());
        }


        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SPIN_TAXON, PERMISSION_MASK.UPDATE);
        }
    }

    public class DeleteTaxonRefLinkCommand : GenericDatabaseCommand<TaxonRefLink> {
        public DeleteTaxonRefLinkCommand(TaxonRefLink model)
            : base(model) {
        }

        protected override void ProcessImpl(User user) {
            if (Model.RefLinkID >= 0) {
                var service = new SupportService(user);
                service.DeleteRefLink(Model.RefLinkID);
            }
        }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SPIN_TAXON, PERMISSION_MASK.UPDATE);
        }

    }
}
