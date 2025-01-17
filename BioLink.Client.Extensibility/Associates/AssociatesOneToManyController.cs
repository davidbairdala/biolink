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
using BioLink.Data;
using BioLink.Data.Model;
using System.Windows;
using System.Windows.Controls;

namespace BioLink.Client.Extensibility {

    public class AssociatesOneToManyController : IOneToManyDetailController {

        public AssociatesOneToManyController(User user, TraitCategoryType category, ViewModelBase owner) {
            this.User = user;
            this.Category = category;
            this.Owner = owner;
        }

        public List<ViewModelBase> LoadModel() {
            var service = new SupportService(User);
            var list = service.GetAssociates(Category.ToString(), Owner.ObjectID.Value);
            return list.ConvertAll((model) => {
                return (ViewModelBase)new AssociateViewModel(model);
            });

        }

        public ViewModelBase AddNewItem(out DatabaseCommand addAction) {
            var model = new Associate();
            model.AssociateID = -1;
            model.FromIntraCatID = Owner.ObjectID.Value;
            model.FromCategory = Category.ToString();
            model.Direction = "FromTo";

            var viewModel = new AssociateViewModel(model);
            addAction = new InsertAssociateCommand(model, Owner);
            return viewModel;
        }

        public DatabaseCommand PrepareDeleteAction(ViewModelBase viewModel) {
            var a = viewModel as AssociateViewModel;
            if (a != null) {
                return new DeleteAssociateCommand(a.Model);
            }
            return null;
        }

        public DatabaseCommand PrepareUpdateAction(ViewModelBase viewModel) {
            var a = viewModel as AssociateViewModel;
            if (a != null) {
                return new UpdateAssociateCommand(a.Model);
            }
            return null;
        }

        public bool UsePestHostControl(AssociateViewModel associate) {

            if (associate != null && Preferences.UseSimplifiedAssociates.Value) {

                // New associate, or the relationships haven't been set yet - we can use the pest/host control in this case...
                if (string.IsNullOrWhiteSpace(associate.RelationFromTo) && string.IsNullOrWhiteSpace(associate.RelationToFrom)) {
                    return true;
                }
                // Otherwise make sure the associate relationships are either Pest or Host only
                if (string.IsNullOrWhiteSpace(associate.RelationFromTo) || string.IsNullOrWhiteSpace(associate.RelationToFrom)) {
                    return false;
                }

                if (!associate.RelationFromTo.Equals("pest", StringComparison.CurrentCultureIgnoreCase) && !associate.RelationFromTo.Equals("host", StringComparison.CurrentCultureIgnoreCase)) {
                    return false;
                }

                if (!associate.RelationToFrom.Equals("pest", StringComparison.CurrentCultureIgnoreCase) && !associate.RelationToFrom.Equals("host", StringComparison.CurrentCultureIgnoreCase)) {
                    return false;
                }

                return true;
            }

            return false;
        }

        public UIElement GetDetailEditor(ViewModelBase selectedItem) {

            var associate = selectedItem as AssociateViewModel;
            if (UsePestHostControl(associate)) {
                // If there are no relationships defined, and we are using the pest/host control, we can prefill the relationships...
                if (string.IsNullOrWhiteSpace(associate.RelationFromTo) && string.IsNullOrWhiteSpace(associate.RelationToFrom)) {
                    associate.RelationFromTo = "Host";
                    associate.RelationToFrom = "Pest";
                }
                return new PestHostAssociateControl(PluginManager.Instance.User, Category, Owner);
            } else {
                return new AssociatesControl(Category, Owner);
            }

        }

        public TraitCategoryType Category { get; private set; }

        public User User { get; private set; }

        public ViewModelBase Owner { get; set; }

        public OneToManyControl Host { get; set; }

        public bool AcceptDroppedPinnable(PinnableObject pinnable) {
            if (pinnable.LookupType == LookupType.Material || pinnable.LookupType == LookupType.Taxon) {
                return true;
            }
            return false;
        }

        public void PopulateFromPinnable(ViewModelBase viewModel, PinnableObject pinnable) {
            var associate = viewModel as AssociateViewModel;
            if (associate != null) {            
                var pinnableViewModel = PluginManager.Instance.GetViewModel(pinnable);
                if (pinnableViewModel != null) {
                    associate.AssocName = pinnableViewModel.DisplayLabel;
                    associate.RelativeCatID = TraitCategoryTypeHelper.GetCategoryIDFromLookupType(pinnable.LookupType);
                    associate.RelativeIntraCatID = pinnable.ObjectID;
                }
            }            
        }

    }

}
