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
using BioLink.Data;
using BioLink.Data.Model;
using BioLink.Client.Extensibility;

namespace BioLink.Client.Material {

    public class RenameSiteVisitCommand : GenericDatabaseCommand<SiteExplorerNode> {

        public RenameSiteVisitCommand(SiteExplorerNode model) : base(model) { }

        protected override void ProcessImpl(User user) {
            var service = new MaterialService(user);
            service.RenameSiteVisit(Model.ElemID, Model.Name);
        }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SPARC_SITEVISIT, PERMISSION_MASK.UPDATE);
        }

    }

    public class DeleteSiteVisitCommand : DatabaseCommand {

        public DeleteSiteVisitCommand(int siteVisitID) {
            this.SiteVisitID = siteVisitID;
        }

        protected override void ProcessImpl(User user) {
            var service = new MaterialService(user);
            service.DeleteSiteVisit(SiteVisitID);
        }

        public int SiteVisitID { get; private set; }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SPARC_SITEVISIT, PERMISSION_MASK.DELETE);
        }

    }

    public class InsertSiteVisitCommand : AbstractSiteExplorerCommand {

        public InsertSiteVisitCommand(SiteExplorerNode model, SiteExplorerNodeViewModel viewModel, int templateId = -1) : base(model, viewModel) {
            TemplateID = templateId;
        }

        protected override void ProcessImpl(User user) {
            var service = new MaterialService(user);
            Model.ElemID = service.InsertSiteVisit(Model.ParentID, TemplateID);
            UpdateChildrenParentID();
        }

        public int TemplateID { get; private set; }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SPARC_SITEVISIT, PERMISSION_MASK.INSERT);
        }

    }

    public class UpdateSiteVisitCommand : GenericDatabaseCommand<SiteVisit> {
        public UpdateSiteVisitCommand(SiteVisit model)
            : base(model) {
        }

        protected override void ProcessImpl(User user) {
            var service = new MaterialService(user);
            if (Model.DateStart.GetValueOrDefault(-1) < 0 && Model.DateEnd.GetValueOrDefault(-1) < 0) {
                Model.DateType = 2;
            } else {
                Model.DateType = 1;
            }

            if (Preferences.AutoGenerateSiteVisitNames.Value) {
                Model.SiteVisitName = NameFormatter.FormatSiteVisitName(Model);
            }

            service.UpdateSiteVisit(Model);
        }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SPARC_SITEVISIT, PERMISSION_MASK.UPDATE);
        }

    }

    public class MergeSiteVisitCommand : GenericDatabaseCommand<SiteExplorerNode> {

        public MergeSiteVisitCommand(SiteExplorerNode source, SiteExplorerNode dest)
            : base(source) {
            Dest = dest;
        }

        protected override void ProcessImpl(User user) {
            var service = new MaterialService(user);
            service.MergeSiteVisit(Model.ElemID, Dest.ElemID);
        }

        public SiteExplorerNode Dest { get; private set; }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SPARC_EXPLORER, PERMISSION_MASK.ALLOW);
        }

    }

    public class MoveSiteVisitCommand : GenericDatabaseCommand<SiteExplorerNode> {

        public MoveSiteVisitCommand(SiteExplorerNode source, SiteExplorerNode dest)
            : base(source) {
            Dest = dest;
        }

        protected override void ProcessImpl(User user) {
            var service = new MaterialService(user);            
            service.MoveSiteVisit(Model.ElemID, Dest.ElemID);
        }

        public SiteExplorerNode Dest { get; private set; }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SPARC_EXPLORER, PERMISSION_MASK.ALLOW);
        }

    }

    public class InsertSiteVisitTemplateCommand : GenericDatabaseCommand<SiteExplorerNode> {
        public InsertSiteVisitTemplateCommand(SiteExplorerNode model)
            : base(model) {
        }

        protected override void ProcessImpl(User user) {
            var service = new MaterialService(user);
            Model.ElemID = service.InsertSiteVisitTemplate();
        }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SPARC_SITEVISIT, PERMISSION_MASK.INSERT);
        }

    }

    public class InsertRDESiteVisitCommand : GenericDatabaseCommand<RDESiteVisit> {

        public InsertRDESiteVisitCommand(RDESiteVisit model, RDESite owner) : base(model) {
            this.Owner = owner;
        }

        protected override void ProcessImpl(User user) {
            var service = new MaterialService(user);
            Model.SiteID = Owner.SiteID;
            Model.SiteVisitID = service.InsertSiteVisit(Model.SiteID);
            var update = new UpdateRDESiteVisitCommand(Model);
            update.Process(user);            
        }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SPARC_SITEVISIT, PERMISSION_MASK.INSERT);
        }

        protected RDESite Owner { get; private set; }

    }

    public class UpdateRDESiteVisitCommand : GenericDatabaseCommand<RDESiteVisit> {

        public UpdateRDESiteVisitCommand(RDESiteVisit model) : base(model) { }

        protected override void ProcessImpl(User user) {
            var service = new MaterialService(user);

            if (string.IsNullOrEmpty(Model.VisitName) || Preferences.AutoGenerateSiteVisitNames.Value) {
                Model.VisitName = NameFormatter.FormatSiteVisitName(Model);
            }

            service.UpdateSiteVisitRDE(Model);
        }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SPARC_SITEVISIT, PERMISSION_MASK.UPDATE);
        }

    }

}
