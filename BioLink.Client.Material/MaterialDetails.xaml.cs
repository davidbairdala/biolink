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

namespace BioLink.Client.Material {
    /// <summary>
    /// Interaction logic for MaterialDetails.xaml
    /// </summary>
    public partial class MaterialDetails : DatabaseActionControl {

        private IdentificationHistoryControl _historyControl;

        private MaterialViewModel _viewModel;

        #region Designer Constructor
        public MaterialDetails() {
            InitializeComponent();
        }
        #endregion

        public MaterialDetails(User user, int materialID) : base(user, "Material:" + materialID) {
            InitializeComponent();
            var service = new MaterialService(user);
            var model = service.GetMaterial(materialID);
            _viewModel = new MaterialViewModel(model);
            _viewModel.DataChanged += new DataChangedHandler(viewModel_DataChanged);
            this.DataContext = _viewModel;

            // General tab
            txtAccessionNumber.BindUser(User, "MaterialAccessionNo", "tblMaterial", "vchrAccessionNo");            
            txtRegistrationNumber.BindUser(User, "MaterialRegNo", "tblMaterial", "vchrRegNo" );
            txtCollectorNo.BindUser(User, "MaterialCollectorNo", "tblMaterial", "vchrCollectorNo");

            txtAbundance.BindUser(user, PickListType.Phrase, "Material Abundance", TraitCategoryType.Material);
            txtSource.BindUser(user, PickListType.Phrase, "Material Source", TraitCategoryType.Material);
            txtInstitution.BindUser(user, PickListType.Phrase, "Institution", TraitCategoryType.Material);
            txtCollectionMethod.BindUser(user, PickListType.Phrase, "Collection Method", TraitCategoryType.Material);
            txtMacroHabitat.BindUser(user, PickListType.Phrase, "Macro Habitat", TraitCategoryType.Material);
            txtMicroHabitat.BindUser(user, PickListType.Phrase, "Micro Habitat", TraitCategoryType.Material);

            txtTrap.BindUser(User, LookupType.Trap);

            // Identification tab
            txtIdentification.BindUser(User, LookupType.Taxon);
            txtIdentification.ObjectIDChanged += new ObjectIDChangedHandler(txtIdentification_ObjectIDChanged);
            txtIdentifiedBy.BindUser(User, "tblMaterial", "vchrIDBy");
            txtReference.BindUser(User, LookupType.Reference);
            txtAccuracy.BindUser(User, PickListType.Phrase, "Identification Accuracy", TraitCategoryType.Material);
            txtMethod.BindUser(User, PickListType.Phrase, "Identification Method", TraitCategoryType.Material);
            txtNameQual.BindUser(User, PickListType.Phrase, "Identification Qualifier", TraitCategoryType.Material);

            _historyControl = new IdentificationHistoryControl(user, materialID);
            _historyControl.Margin = new Thickness(0);
            tabIDHistory.Content = _historyControl;

            tabMaterial.AddTabItem("Subparts", new MaterialPartsControl(User, materialID));
            tabMaterial.AddTabItem("Traits", new TraitControl(User, TraitCategoryType.Material, materialID));
            tabMaterial.AddTabItem("Notes", new NotesControl(User, TraitCategoryType.Material, materialID));
            tabMaterial.AddTabItem("Multimedia", new MultimediaControl(User, TraitCategoryType.Material, materialID));
            tabMaterial.AddTabItem("Ownership", new OwnershipDetails(model));

        }

        void txtIdentification_ObjectIDChanged(object source, int? objectID) {
            if (this.Question("Do you wish to record a history of this identification change?", "Create identification history?")) {
                _historyControl.AddHistoryFromMaterial(_viewModel);
                // Clear id fields...
                _viewModel.IdentificationAccuracy = "";
                _viewModel.IdentificationDate = null;
                _viewModel.IdentificationMethod = "";
                _viewModel.IdentificationNameQualification = "";
                _viewModel.IdentificationNotes = "";
                _viewModel.IdentificationReferenceID = 0;
                _viewModel.IdentificationRefPage = "";
                _viewModel.IdentifiedBy = "";


            }
        }

        void viewModel_DataChanged(ChangeableModelBase viewmodel) {
            var mvm = viewmodel as MaterialViewModel;
            if (mvm != null) {
                RegisterUniquePendingChange(new UpdateMaterialAction(mvm));
            }

        }
    }
}