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
using BioLink.Data;
using BioLink.Data.Model;
using System.Collections.ObjectModel;
using BioLink.Client.Utilities;

namespace BioLink.Client.Extensibility {
    /// <summary>
    /// Interaction logic for TraitControl.xaml
    /// </summary>
    public partial class TraitControl : DatabaseActionControl<SupportService> {

        private List<TraitViewModel> _model;

        #region Designer Constructor
        public TraitControl() {
            InitializeComponent();
        }
        #endregion

        public TraitControl(User user, TraitCategoryType category, int? intraCatId) :base(new SupportService(user), "Traits:" + category.ToString() + ":" + intraCatId.Value) {

            this.User = user;
            this.TraitCategory = category;
            this.IntraCatID = intraCatId.Value;

            InitializeComponent();

            if (intraCatId.HasValue) {
                SupportService s = new SupportService(user);
                var list = s.GetTraits(category.ToString(), intraCatId.Value);
                var modellist = list.Select((t) => {
                    return new TraitViewModel(t);
                });
                _model = new List<TraitViewModel>(modellist);
            }

            ReloadTraitPanel();            
        }

        private void ReloadTraitPanel() {

            traitsPanel.Children.Clear();

            _model.Sort(new Comparison<TraitViewModel>((a , b) => {
                return a.Name.CompareTo(b.Name);
            }));

            foreach (TraitViewModel m in _model) {
                AddTraitEditor(m);
            }
        }

        private void AddTraitEditor(TraitViewModel model) {
            var itemControl = new TraitElementControl(User, model);
            itemControl.TraitChanged += new TraitElementControl.TraitEventHandler((source, trait) => {
                RegisterUniquePendingAction(new UpdateTraitDatabaseAction(trait.Model));
            });

            itemControl.TraitDeleted += new TraitElementControl.TraitEventHandler((source, trait) => {
                _model.Remove(trait);
                ReloadTraitPanel();
                RegisterPendingAction(new DeleteTraitDatabaseAction(trait.Model));                
            });
            traitsPanel.Children.Add(itemControl);
        }

        private void btnAddTrait_Click(object sender, RoutedEventArgs e) {
            AddNewTrait();
        }

        private void AddNewTrait() {
            var frm = new AddNewTraitWindow(User, TraitCategory);
            frm.Owner = this.FindParentWindow();
            if (frm.ShowDialog().GetValueOrDefault(false)) {
                Trait t = new Trait();
                t.TraitID = -1;
                t.Value = "<New Trait Value>";
                t.Category = TraitCategory.ToString();
                t.IntraCatID = IntraCatID;
                t.Name = frm.TraitName;

                TraitViewModel viewModel = new TraitViewModel(t);
                _model.Add(viewModel);
                RegisterUniquePendingAction(new UpdateTraitDatabaseAction(t));
                ReloadTraitPanel();
            }
        }

        #region Properties

        public User User { get; private set; }

        public TraitCategoryType TraitCategory { get; private set; }

        public int IntraCatID { get; private set; }

        #endregion
    }

}