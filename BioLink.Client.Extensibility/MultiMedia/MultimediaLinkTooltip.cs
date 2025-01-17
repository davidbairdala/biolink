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
using BioLink.Client.Utilities;
using BioLink.Client.Extensibility;
using BioLink.Data;
using BioLink.Data.Model;
using System.Windows;
using System.Windows.Controls;

namespace BioLink.Client.Extensibility {

    public class MultimediaLinkTooltip : TooltipContentBase {

        public MultimediaLinkTooltip(MultimediaLinkViewModel viewModel) : base(viewModel.ObjectID.Value, viewModel) { }

        protected override System.Windows.FrameworkElement GetDetailContent(BioLinkDataObject model) {
            var vm = ViewModel as MultimediaLinkViewModel;
            var grid = new Grid { Margin = new Thickness(3) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength() });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength() });

            if (vm != null && !string.IsNullOrWhiteSpace(vm.TempFilename)) {
                var img = new Image { SnapsToDevicePixels = true, UseLayoutRounding = true, Stretch = System.Windows.Media.Stretch.None };
                img.Source = GraphicsUtils.GenerateThumbnail(vm.TempFilename, 300);
                grid.Children.Add(img);
            }

            var builder = new TextTableBuilder();
            builder.Add("Type", vm.MultimediaType);
            builder.Add("Caption", RTFUtils.StripMarkup(vm.Caption));

            var details = builder.GetAsContent();
            Grid.SetRow(details, 1);

            grid.Children.Add(details);

            return grid;
        }

        protected override void GetDetailText(Data.Model.BioLinkDataObject model, TextTableBuilder builder) {
            throw new NotImplementedException();
        }

        protected override Data.Model.BioLinkDataObject GetModel() {
            var vm = ViewModel as MultimediaLinkViewModel;
            return vm.Model;
        }
    }
}
