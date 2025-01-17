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
using System.Collections.ObjectModel;

namespace BioLink.Client.Tools {
    /// <summary>
    /// Interaction logic for LoanContactsControl.xaml
    /// </summary>
    public partial class LoanContactsControl : DatabaseCommandControl, ISelectionHostControl {

        private ObservableCollection<ContactViewModel> _findModel;

        private ContactBrowsePage _page;
        private TabItem _previousPage;

        public LoanContactsControl(User user, ToolsPlugin plugin) : base(user, "LoanContactsManager") {
            InitializeComponent();
            this.Plugin = plugin;
            var list = Enum.GetValues(typeof(ContactSearchType));
            cmbFindWhat.ItemsSource = list;
            cmbFindWhat.SelectedIndex = 0;
            lvwFind.KeyUp += new KeyEventHandler(lvwFind_KeyUp);
            lvwFind.MouseDoubleClick += new MouseButtonEventHandler(lvwFind_MouseDoubleClick);
            txtFilter.PreviewKeyDown += new KeyEventHandler(txtFilter_PreviewKeyDown);

            ListViewDragHelper.Bind(lvwFind, ListViewDragHelper.CreatePinnableGenerator(ToolsPlugin.TOOLS_PLUGIN_NAME, LookupType.Contact));

            lvwFind.MouseRightButtonUp += new MouseButtonEventHandler(lvwFind_MouseRightButtonUp);

            string[] ranges = new string[] { "A-C", "D-F", "G-I", "J-L", "M-O", "P-R", "S-U", "V-X", "Y-Z" };

            _page = new ContactBrowsePage(user);
            _page.ContextMenuRequested += new Action<FrameworkElement, ContactViewModel>(_page_ContextMenuRequested);
            _page.LoadPage("A-C");

            foreach (string range in ranges) {
                AddTabPage(range);
            }

            Loaded += new RoutedEventHandler(LoanContactsControl_Loaded);
        }

        void LoanContactsControl_Loaded(object sender, RoutedEventArgs e) {
            txtFilter.Focus();
        }

        void _page_ContextMenuRequested(FrameworkElement sender, ContactViewModel obj) {
            ShowContextMenu(sender, obj);
        }

        private void AddTabPage(string range) {
            string[] bits = range.Split('-');
            if (bits.Length == 2) {
                char from = bits[0][0];
                char to = bits[1][0];
                string caption = "";
                for (char ch = from; ch <= to; ch++) {
                    caption += ch;
                }

                TabItem item = new TabItem();
                item.Header = caption.ToUpper();
                item.Tag = range;

                item.RequestBringIntoView += new RequestBringIntoViewEventHandler(item_RequestBringIntoView);
                item.LayoutTransform = new RotateTransform(90);
                tabBrowse.Items.Add(item);

            } else {
                throw new Exception("Invalid page range!: " + range);
            }

        }

        void item_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e) {
            if (sender == _previousPage) {
                return;
            }

            // Detach the page from the previous tab
            if (_previousPage != null && _previousPage != sender) {
                _previousPage.Content = null;
            }

            var selected = sender as TabItem;
            if (selected != null) {
                // Load the page with the correct items for the selected tab
                _page.LoadPage(selected.Tag as string);
                // Attach the page to the current tab
                selected.Content = _page;
            }
            _previousPage = selected;
           
        }

        void lvwFind_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            var selected = lvwFind.SelectedItem as ContactViewModel;
            ShowContextMenu(lvwFind, selected);
        }

        private void ShowContextMenu(FrameworkElement sender, ContactViewModel selected) {
            var builder = new ContextMenuBuilder(null);

            if (selected != null) {
                builder.New("_Contact details...").Handler(() => { EditSelectedContact(); }).End();
                builder.New("_Delete contact").Handler(() => { DeleteContact(selected); }).End();
                builder.New("_Loans...").Handler(() => { ShowLoansForContact(selected); }).End();
            }

            if (builder.HasItems) {
                builder.Separator();
            }

            builder.New("Add new contact...").Handler(() => { AddNewContact(); }).End();

            if (builder.HasItems) {
                sender.ContextMenu = builder.ContextMenu;
            }
        }

        private void AddNewContact() {
            var ctl = new ContactDetails(User, -1);
            PluginManager.Instance.AddNonDockableContent(Plugin, ctl, "Contact details: <New contact>", SizeToContent.Manual);            
        }

        private void ShowLoansForContact(ContactViewModel model) {
            if (model != null) {
                Plugin.ShowLoansForContact(model.ContactID);
            }            
        }

        void lvwFind_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            EditSelectedContact();
        }

        void txtFilter_PreviewKeyDown(object sender, KeyEventArgs e) {             
            if (e.Key == Key.Down) {
                lvwFind.SelectedIndex = 0;
                lvwFind.Focus();
                e.Handled = true;
            }            
        }

        void lvwFind_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                EditSelectedContact();
            }
        }

        private ContactViewModel GetSelectedContact() {
            ContactViewModel selected = null;
            if (tabContacts.SelectedIndex == 0) {
                selected = lvwFind.SelectedItem as ContactViewModel;
            } else {
                selected = _page.SelectedItem;
            }

            return selected;
        }

        private void DeleteSelectedContact() {
            var selected = GetSelectedContact();
            if (selected != null) {
                DeleteContact(selected);
            }
        }

        private void EditSelectedContact() {
            var selected = GetSelectedContact();
            if (selected != null) {
                Plugin.EditContact(selected.ContactID);
            }
        }

        private void DeleteContact(ContactViewModel contact) {
            
            if (contact == null) {
                return;
            }

            if (this.Question("Are you sure you wish to permanently delete this contact?", "Delete '" + contact.FullName + "'?")) {
                contact.IsDeleted = true;
                RegisterPendingChange(new DeleteContactCommand(contact.Model));
                if (_findModel.Contains(contact)) {
                    _findModel.Remove(contact);
                }
            }

        }

        protected ToolsPlugin Plugin { get; private set; }

        private void textBox1_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                DoFind();
            }
        }

        private void DoFind() {            
            var searchType = (ContactSearchType)cmbFindWhat.SelectedItem;
            var filter = txtFilter.Text;
            if (!string.IsNullOrWhiteSpace(filter)) {
                var service = new LoanService(User);
                var list = service.FindContacts(filter, searchType);
                _findModel = new ObservableCollection<ContactViewModel>(list.Select((m) => {
                    return new ContactViewModel(m);
                }));
                lvwFind.ItemsSource = _findModel;
                if (list.Count == 0) {
                    InfoBox.Show("No matching results found.", "No results", this);
                }
            }
        }

        private void btnFind_Click(object sender, RoutedEventArgs e) {
            DoFind();
        }

        private void btnNewContact_Click(object sender, RoutedEventArgs e) {
            AddNewContact();
        }

        private void btnDetails_Click(object sender, RoutedEventArgs e) {
            EditSelectedContact();
        }

        private void btnDeleteContact_Click(object sender, RoutedEventArgs e) {
            DeleteSelectedContact();
        }

        private void btnContactLoans_Click(object sender, RoutedEventArgs e) {
            ShowLoansForContact(GetSelectedContact());
        }


        public SelectionResult Select() {
            var selected = GetSelectedContact();
            if (selected != null) {
                return new SelectionResult { DataObject = selected, Description = selected.FullName, LookupType = LookupType.Contact, ObjectID = selected.ContactID };
            }
            return null;
        }
    }

    public class ContactViewModel : GenericViewModelBase<Contact> {
        public ContactViewModel(Contact model) : base(model, () => model.ContactID) { }

        public override ImageSource Icon {
            get {
                if (base.Icon == null) {
                    return ImageCache.GetImage("pack://application:,,,/BioLink.Client.Extensibility;component/images/Contact.png");
                }
                return base.Icon;
            }
            set {
                base.Icon = value;
            }
        }

        public override FrameworkElement TooltipContent {
            get { return new ContactTooltipContent(ContactID, this); }
        }

        public override string DisplayLabel {
            get { return FullName; }
        }

        public int ContactID {
            get { return Model.ContactID; }
            set { SetProperty(() => Model.ContactID, value); }
        }

        public string Name {
            get { return Model.Name; }
            set { SetProperty(() => Model.Name, value); }
        }

        public string Title {
            get { return Model.Title; }
            set { SetProperty(() => Model.Title, value); }
        }

        public string GivenName {
            get { return Model.GivenName; }
            set { SetProperty(() => Model.GivenName, value); }
        }

        public string PostalAddress {
            get { return Model.PostalAddress; }
            set { SetProperty(()=>Model.PostalAddress, value); } 
        }

        public string StreetAddress {
            get { return Model.StreetAddress; }
            set { 
                SetProperty(() => Model.StreetAddress, value);
                if (_postalAsStreet) {
                    SetProperty(() => Model.PostalAddress, value);
                }
            }
        }

        public string Institution {
            get { return Model.Institution; }
            set { SetProperty(()=>Model.Institution, value); }
        }

        public string JobTitle {
            get { return Model.JobTitle; }
            set { SetProperty(() => Model.JobTitle, value); }
        }

        public string WorkPh {
            get { return Model.WorkPh; }
            set { SetProperty(() => Model.WorkPh, value); }
        }

        public string WorkFax {
            get { return Model.WorkFax; }
            set { SetProperty(() => Model.WorkFax, value); }
        }

        public string HomePh {
            get { return Model.HomePh; }
            set { SetProperty(() => Model.HomePh, value); }
        }

        public string EMail {
            get { return Model.EMail; }
            set { SetProperty(() => Model.EMail, value); }
        }

        public string FullName {
            get { return LoanService.FormatName(Title, GivenName, Name); }
        }

        private bool _postalAsStreet;

        public bool PostalSameAsStreet {
            get { return _postalAsStreet; }
            set { 
                SetProperty("PostalSameAsStreet", ref _postalAsStreet, value);
                if (value) {
                    PostalAddress = StreetAddress;
                }
            }
        }

    }

    public class ContactTooltipContent : TooltipContentBase {

        public ContactTooltipContent(int contactId, ContactViewModel viewModel) : base(contactId, viewModel) { }

        protected override void GetDetailText(BioLinkDataObject model, TextTableBuilder builder) {
            var viewModel = ViewModel as ContactViewModel;

            builder.Add("Job title", viewModel.JobTitle);
            builder.Add("E-mail", viewModel.EMail);
            builder.Add("Work phone", viewModel.WorkPh);
            builder.Add("Work Fax", viewModel.WorkFax);
            builder.Add("Home phone", viewModel.HomePh);
            builder.Add("Institution", viewModel.Institution);
            builder.Add("Street Address", viewModel.StreetAddress);
            if (viewModel.StreetAddress == null || !viewModel.StreetAddress.Equals(viewModel.PostalAddress,StringComparison.CurrentCultureIgnoreCase)) {
                builder.Add("Postal Address", viewModel.PostalAddress);
            }
        }

        protected override BioLinkDataObject GetModel() {
            var service = new LoanService(User);
            return service.GetLoan(ObjectID);
        }
    }
}
