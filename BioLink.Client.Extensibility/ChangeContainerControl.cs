﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using BioLink.Data;
using BioLink.Client.Utilities;

namespace BioLink.Client.Extensibility {

    public class ChangeContainerControl : UserControl, IChangeContainer {

        private ChangeContainerImpl _impl;

        #region designer ctor
        public ChangeContainerControl() : base() {
        }
        #endregion

        public ChangeContainerControl(User user) : base() {
            this.User = user;
            _impl = new ChangeContainerImpl(user);
            _impl.ChangeRegistered += new PendingChangedRegisteredHandler(_impl_ChangeRegistered);
            _impl.ChangesCommitted += new PendingChangesCommittedHandler(_impl_ChangesCommitted);
        }

        void _impl_ChangesCommitted(object sender) {
            if (this.ChangesCommitted != null) {
                ChangesCommitted(sender);
            }
        }

        void _impl_ChangeRegistered(object sender, object action) {
            if (this.ChangeRegistered != null) {
                ChangeRegistered(sender, action);
            }
        }

        public bool HasPendingChanges {
            get { return _impl.HasPendingChanges; }
        }

        public void RegisterPendingChange(DatabaseAction action, object contributer) {
            _impl.RegisterPendingChange(action, contributer);
        }

        public bool RegisterUniquePendingChange(DatabaseAction action, object contributer) {
            return _impl.RegisterUniquePendingChange(action, contributer);
        }

        public void RegisterPendingChanges(List<DatabaseAction> actions, object contributer) {
            _impl.RegisterPendingChanges(actions, contributer);
        }

        public void ClearPendingChanges() {
            _impl.ClearPendingChanges();
        }

        public void ClearMatchingPendingChanges(Predicate<DatabaseAction> predicate) {
            _impl.ClearMatchingPendingChanges(predicate);
        }


        public void CommitPendingChanges(Action successAction = null) {
            _impl.CommitPendingChanges(successAction);
        }

        public List<DatabaseAction> PendingChanges {
            get { return _impl.PendingChanges; }
        }

        public bool RequestClose() {
            if (HasPendingChanges) {
                return this.Question("You have unsaved changes. Are you sure you want to discard those changes?", "Discard changes?");
            }
            return true;
        }

        public User User { get; protected set; }

        public event PendingChangesCommittedHandler ChangesCommitted;

        public event PendingChangedRegisteredHandler ChangeRegistered;

    }
}