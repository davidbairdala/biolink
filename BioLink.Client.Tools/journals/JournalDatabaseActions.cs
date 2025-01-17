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


namespace BioLink.Client.Tools {

    public class InsertJournalCommand : GenericDatabaseCommand<Journal> {

        public InsertJournalCommand(Journal model)
            : base(model) {
        }

        protected override void ProcessImpl(User user) {
            var service = new SupportService(user);
            Model.JournalID = service.InsertJournal(Model);
        }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SUPPORT_JOURNALS, PERMISSION_MASK.INSERT);
        }

    }

    public class UpdateJournalCommand : GenericDatabaseCommand<Journal> {

        public UpdateJournalCommand(Journal model)
            : base(model) {
        }

        protected override void ProcessImpl(User user) {
            var service = new SupportService(user);
            service.UpdateJournal(Model);
        }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SUPPORT_JOURNALS, PERMISSION_MASK.UPDATE);
        }

    }

    public class DeleteJournalCommand : GenericDatabaseCommand<Journal> {

        public DeleteJournalCommand(Journal model)
            : base(model) {
        }

        protected override void ProcessImpl(User user) {
            var service = new SupportService(user);
            service.DeleteJournal(Model.JournalID);
        }

        protected override void BindPermissions(PermissionBuilder required) {
            required.Add(PermissionCategory.SUPPORT_JOURNALS, PERMISSION_MASK.DELETE);
        }

    }
}
