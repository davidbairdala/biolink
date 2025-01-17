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
using BioLink.Data.Model;
using System.Windows.Media.Imaging;
using System.Windows;
using BioLink.Client.Utilities;
using System.IO;

namespace BioLink.Client.Extensibility {

    public class MultimediaLinkViewModel : GenericViewModelBase<MultimediaLink> {

        public MultimediaLinkViewModel(MultimediaLink model) : base(model, ()=>model.MultimediaLinkID) { }

        public override FrameworkElement TooltipContent {
            get { return new MultimediaLinkTooltip(this); }
        }

        public override string DisplayLabel {
            get {
                long size = SizeInBytes;
                if (!string.IsNullOrWhiteSpace(TempFilename)) {
                    var finfo = new FileInfo(TempFilename);
                    size = finfo.Length;
                }
                var sizeStr = ByteLengthConverter.FormatBytes(size);
                return string.Format("{0}.{1} ({2})", Name, Extension, sizeStr);
            }
        }

        public int MultimediaID {
            get { return Model.MultimediaID; }
            set { SetProperty(() => Model.MultimediaID, value); }
        }

        public int MultimediaLinkID {
            get { return Model.MultimediaLinkID; }
            set { SetProperty(() => Model.MultimediaLinkID, value); }
        }

        public string MultimediaType {
            get { return Model.MultimediaType; }
            set { SetProperty(() => Model.MultimediaType, value); }
        }

        public string Name {
            get { return Model.Name; }
            set { SetProperty(() => Model.Name, value); }
        }

        public string Caption {
            get { return Model.Caption; }
            set { SetProperty(() => Model.Caption, value); }
        }

        public string Extension {
            get { return Model.Extension; }
            set { SetProperty(() => Model.Extension, value); }
        }

        public int SizeInBytes {
            get { return Model.SizeInBytes; }
            set { SetProperty(() => Model.SizeInBytes, value); }
        }

        public int Changes {
            get { return Model.Changes; }
            set { SetProperty(() => Model.Changes, value); }
        }

        public int BlobChanges {
            get { return Model.BlobChanges; }
            set { SetProperty(() => Model.BlobChanges, value); }
        }

        public static readonly DependencyProperty ThumbnailProperty = DependencyProperty.Register("Thumbnail", typeof(BitmapSource), typeof(MultimediaLinkViewModel), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // Not a change registering property
        public BitmapSource Thumbnail {
            get { return (BitmapSource) GetValue(ThumbnailProperty); }
            set { SetValue(ThumbnailProperty, value); }
        }

        public string FileInfo {
            get { return string.Format("{0} {1}", this.Extension, ByteLengthConverter.FormatBytes(SizeInBytes)); }
        }

        public string Fullname {
            get { return string.Format("{0}.{1}", Name, Extension); }
        }

        public string TempFilename { get; set; }
        
    }
}
