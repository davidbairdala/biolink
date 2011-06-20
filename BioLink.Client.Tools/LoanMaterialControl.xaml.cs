﻿using System;
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
using BioLink.Client.Extensibility;
using BioLink.Client.Utilities;
using BioLink.Data;
using BioLink.Data.Model;

namespace BioLink.Client.Tools {
    /// <summary>
    /// Interaction logic for LoanMaterialControl.xaml
    /// </summary>
    public partial class LoanMaterialControl : OneToManyDetailControl {

        protected Loan Loan { get; private set; }

        public LoanMaterialControl(User user, int loanId) : base(user, "LoanMaterial:" + loanId) {
            InitializeComponent();

            txtMaterial.BindUser(user, LookupType.Material);
            txtTaxon.BindUser(user, LookupType.Taxon);
            txtTaxon.EnforceLookup = false; // Any taxon name will do!

            LoanID = loanId;
            var service = new LoanService(user);
            this.Loan = service.GetLoan(loanId);

            lblClosed.Visibility = System.Windows.Visibility.Collapsed;
            chkReturned.IsEnabled = true;
            if (Loan.LoanClosed.HasValue && Loan.LoanClosed.Value) {
                lblClosed.Visibility = System.Windows.Visibility.Visible;
                chkReturned.IsEnabled = false;
            }

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(LoanMaterialControl_DataContextChanged);
        }

        void LoanMaterialControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var material = e.NewValue as LoanMaterialViewModel;
            if (material != null) {
                if (Loan.LoanClosed.ValueOrFalse() && !material.DateReturned.HasValue) {
                    material.Model.DateReturned = Loan.DateClosed;
                }
            }
        }

        public override ViewModelBase AddNewItem(out DatabaseAction addAction) {            
            var model = new LoanMaterial() { LoanID = this.LoanID, Returned = false };
            addAction = new InsertLoanMaterialAction(model);
            return new LoanMaterialViewModel(model);
        }


        public override DatabaseAction PrepareDeleteAction(ViewModelBase viewModel) {
            return new DeleteLoanMaterialAction((viewModel as LoanMaterialViewModel).Model);
        }

        public override List<ViewModelBase> LoadModel() {
            var service = new LoanService(User);
            var list = service.GetLoanMaterial(LoanID);
            var model = new List<ViewModelBase>(list.Select((m) => {
                return new LoanMaterialViewModel(m);
            }));
            return model;
        }

        public override DatabaseAction PrepareUpdateAction(ViewModelBase viewModel) {
            return new UpdateLoanMaterialAction((viewModel as LoanMaterialViewModel).Model);
        }

        public override FrameworkElement FirstControl {
            get { return txtMaterial; }
        }

        protected int LoanID { get; private set; }

    }

    public class DeleteLoanMaterialAction : GenericDatabaseAction<LoanMaterial> {

        public DeleteLoanMaterialAction(LoanMaterial model) : base(model) { }

        protected override void ProcessImpl(User user) {
            var service = new LoanService(user);
            service.DeleteLoanMaterial(Model.LoanMaterialID);
        }

    }

    public class InsertLoanMaterialAction : GenericDatabaseAction<LoanMaterial> {

        public InsertLoanMaterialAction(LoanMaterial model) : base(model) { }

        protected override void ProcessImpl(User user) {
            var service = new LoanService(user);
            Model.LoanMaterialID = service.InsertLoanMaterial(Model);
        }
    }

    public class UpdateLoanMaterialAction : GenericDatabaseAction<LoanMaterial> {
        public UpdateLoanMaterialAction(LoanMaterial model) : base(model) { }

        protected override void ProcessImpl(User user) {
            var service = new LoanService(user);
            service.UpdateLoanMaterial(Model);
        }
    }

    public class LoanMaterialViewModel : GenericViewModelBase<LoanMaterial> {

        public LoanMaterialViewModel(LoanMaterial model) : base(model, () => model.LoanMaterialID) { }

        public int LoanMaterialID {
            get { return Model.LoanMaterialID; }
            set { SetProperty(() => Model.LoanMaterialID, value); } 
        }

        public int LoanID {
            get { return Model.LoanID; }
            set { SetProperty(() => Model.LoanID, value); }
        }
        
        public int MaterialID {
            get { return Model.MaterialID; }
            set { SetProperty(() => Model.MaterialID, value); }
        }

        public string NumSpecimens {
            get { return Model.NumSpecimens; }
            set { SetProperty(() => Model.NumSpecimens, value); }
        }

        public string TaxonName { 
            get { return Model.TaxonName; }
            set { SetProperty(() => Model.TaxonName, value); }
        }

        public string MaterialDescription {
            get { return Model.MaterialDescription; }
            set { SetProperty(() => Model.MaterialDescription, value); }
        }

        public DateTime? DateAdded {
            get { return Model.DateAdded; }
            set { SetProperty(() => Model.DateAdded, value); }
        }

        public DateTime? DateReturned {
            get { return Model.DateReturned; }
            set { SetProperty(() => Model.DateReturned, value); }
        }

        public bool? Returned {
            get { return Model.Returned; }
            set { SetProperty(() => Model.Returned, value); }
        }

        public string MaterialName {
            get { return Model.MaterialName; }
            set { SetProperty(() => Model.MaterialName, value); }
        }

        public override string ToString() {
            return string.Format("{0}  {1}  ({2} specimens)", (MaterialID == 0 ? "<No Name>" : MaterialName), TaxonName, NumSpecimens);
        }

    }
}