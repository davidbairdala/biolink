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
using System.Collections.ObjectModel;

namespace BioLink.Client.Material {
    /// <summary>
    /// Interaction logic for MaterialRDEControl.xaml
    /// </summary>
    public partial class MaterialRDEControl : UserControl {

        private TraitControl _traits;        
        private RDEMaterialViewModel _currentMaterial;
        private MaterialPartsControl _subpartsFull;
        private OneToManyControl _associates;

        public MaterialRDEControl(User user) {
            InitializeComponent();
            this.User = user;

            txtIdentification.BindUser(user, LookupType.Taxon);
            txtClassifiedBy.BindUser(User, "tblMaterial", "vchrIDBy");

            txtAccessionNo.BindUser(User, "MaterialAccessionNo", "tblMaterial", "vchrAccessionNo");
            txtRegistrationNo.BindUser(User, "MaterialRegNo", "tblMaterial", "vchrRegNo");
            txtCollectorNo.BindUser(User, "MaterialCollectorNo", "tblMaterial", "vchrCollectorNo");

            txtSource.BindUser(user, PickListType.Phrase, "Material Source", TraitCategoryType.Material);
            txtInstitution.BindUser(user, PickListType.Phrase, "Institution", TraitCategoryType.Material);
            txtCollectionMethod.BindUser(user, PickListType.Phrase, "Collection Method", TraitCategoryType.Material);
            txtMacroHabitat.BindUser(user, PickListType.Phrase, "Macro Habitat", TraitCategoryType.Material);
            txtMicroHabitat.BindUser(user, PickListType.Phrase, "Micro Habitat", TraitCategoryType.Material);

            txtTrap.BindUser(User, LookupType.Trap);

            _traits = new TraitControl(user, TraitCategoryType.Material, null, true);
            tabTraits.Content = _traits;

            _subpartsFull = new MaterialPartsControl(user, null, true);
            tabSubparts.Content = _subpartsFull;

            _associates = new OneToManyControl(new AssociatesControl(user, TraitCategoryType.Material, null), true);
            tabAssociates.Content = _associates;
            this.IsEnabled = false;
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(MaterialRDEControl_DataContextChanged);
        }

        void MaterialRDEControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var mat = DataContext as RDEMaterialViewModel;
            if (mat != null) {
                this.IsEnabled = true;
                if (_currentMaterial != null) {
                    // although the database actions are registered for new/modified traits, we need to keep track of them so we can
                    // redisplay them as the user flips around the different material.
                    _currentMaterial.Traits = _traits.GetModel();
                }
                _traits.BindModel(mat.Traits, mat);
                _currentMaterial = mat;

                grpSubParts.Items = _currentMaterial.SubParts;
                _subpartsFull.SetModel(_currentMaterial, _currentMaterial.SubParts);
                _associates.SetModel(_currentMaterial, _currentMaterial.Associates);
            } else {
                this.IsEnabled = false;
            }
        }

        public void GenerateAutoNumbers() {
            txtAccessionNo.GenerateNumber();
            txtRegistrationNo.GenerateNumber();
            txtCollectorNo.GenerateNumber();

        }

        internal User User { get; private set; }

        private void grpSubParts_IsUnlockedChanged(ItemsGroupBox obj, bool newValue) {
            var subpart = grpSubParts.SelectedItem as MaterialPartViewModel;
            if (subpart != null) {
                subpart.Locked = !newValue;
            }
        }

        public List<Trait> GetTraits() {
            return _traits.GetModel();
        }

        public List<Associate> GetAssociates() {
            var m = _associates.GetModel();

            return new List<Associate>( m.Select((vmb) => {
                var assocVM = vmb as AssociateViewModel;
                if (assocVM != null) {
                    return assocVM.Model;
                }
                return null;
            }));            
        }

        public List<MaterialPart> GetSubParts() {
            var parts = _subpartsFull.Model;

            return new List<MaterialPart>(parts.Select((vmb) => {
                var subpart = vmb as MaterialPartViewModel;
                if (subpart != null) {
                    return subpart.Model;
                }
                return null;
            }));
        }

    }
}