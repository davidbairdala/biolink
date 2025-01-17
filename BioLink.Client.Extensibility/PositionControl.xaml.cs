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
using BioLink.Data.Model;
using BioLink.Data;

namespace BioLink.Client.Extensibility {
    /// <summary>
    /// Interaction logic for PositionControl.xaml
    /// </summary>
    public partial class PositionControl : UserControl {

        public PositionControl() {
            InitializeComponent();
            lon.CoordinateValueChanged += new CoordinateValueChangedHandler(lon_CoordinateValueChanged);
            lat.CoordinateValueChanged += new CoordinateValueChangedHandler(lat_CoordinateValueChanged);

            AllowDrop = true;

            this.PreviewDragOver += new DragEventHandler(PositionControl_PreviewDragEnter);
            this.PreviewDragEnter += new DragEventHandler(PositionControl_PreviewDragEnter);
            this.Drop += new DragEventHandler(PositionControl_Drop);
            this.DragEnter += new DragEventHandler(PositionControl_DragEnter);
            this.DragOver += new DragEventHandler(PositionControl_DragEnter);

            HookLatLongControl(lat);
            HookLatLongControl(lon);

            grid.RowDefinitions[0].Height = new GridLength(0);
            grid.RowDefinitions[1].Height = new GridLength(0);
            grid.ColumnDefinitions[1].Width = new GridLength(68);
            lblLon.Visibility = System.Windows.Visibility.Visible;

            if (GoogleEarth.IsInstalled()) {
                grid.ColumnDefinitions[5].Width = new GridLength(4);
                grid.ColumnDefinitions[6].Width = new GridLength(23);
            } else {
                grid.ColumnDefinitions[5].Width = new GridLength(0);
                grid.ColumnDefinitions[6].Width = new GridLength(0);
            }

        }

        private void HookLatLongControl(LatLongInput ctl) {
            HookTextBox(ctl.txtDegrees);
            HookTextBox(ctl.txtMinutes);
            HookTextBox(ctl.txtSeconds);
        }

        public void Clear() {
            lon.Clear();
            lat.Clear();
        }

        private void HookTextBox(System.Windows.Controls.TextBox box) {
            box.AllowDrop = true;

            box.PreviewDragEnter += new DragEventHandler(PositionControl_PreviewDragEnter);
            box.PreviewDragOver += new DragEventHandler(PositionControl_PreviewDragEnter);
            box.PreviewDrop += new DragEventHandler(PositionControl_Drop);
        }

        void PositionControl_DragEnter(object sender, DragEventArgs e) {
            e.Handled = true;
        }

        void PositionControl_PreviewDragEnter(object sender, DragEventArgs e) {

            var pinnable = e.Data.GetData(PinnableObject.DRAG_FORMAT_NAME) as PinnableObject;
            if (pinnable != null) {
                if (pinnable.LookupType == LookupType.PlaceName) {
                    e.Effects = DragDropEffects.Link;
                }
            } else {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        void PositionControl_Drop(object sender, DragEventArgs e) {
            var pinnable = e.Data.GetData(PinnableObject.DRAG_FORMAT_NAME) as PinnableObject;
            if (pinnable != null && pinnable.LookupType == LookupType.PlaceName) {
                PlaceName placeName = pinnable.GetState<PlaceName>();
                this.lat.Value = placeName.Latitude;
                lon.Value = placeName.Longitude;
                if (LocationChanged != null) {
                    var locality = placeName.Name;
                    if (placeName.PlaceNameType == PlaceNameType.OffsetAndDirection) {
                        locality = string.Format("{0} {1} {2} of {3}", placeName.Offset, placeName.Units, placeName.Direction, placeName.Name);
                    }
                    LocationChanged(placeName.Latitude, placeName.Longitude, null, null, locality, "EGaz");
                }

            }
            e.Handled = true;
        }

        void lat_CoordinateValueChanged(object source, double value) {
            this.Latitude = lat.Value;
        }

        void lon_CoordinateValueChanged(object source, double value) {
            this.Longitude = lon.Value;
        }

        public LatLongMode Mode {
            get { return lon.Mode; }

            set {
                lon.Mode = value;
                lat.Mode = value;
            }
        }

        public static readonly DependencyProperty LatitudeProperty = DependencyProperty.Register("Latitude", typeof(double), typeof(PositionControl), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(LatChanged)));

        public double Latitude {
            get { return (double) GetValue(LatitudeProperty); }
            set { SetValue(LatitudeProperty, value); }
        }

        private static void LatChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
            var control = obj as PositionControl;
            if (control != null) {
                control.lat.Value = (double)args.NewValue;
            }

        }

        public static readonly DependencyProperty LongitudeProperty = DependencyProperty.Register("Longitude", typeof(double), typeof(PositionControl), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(LonChanged)));

        public double Longitude {
            get { return (double)GetValue(LongitudeProperty); }
            set { SetValue(LongitudeProperty, value); }
        }

        private static void LonChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
            var control = obj as PositionControl;
            if (control != null) {
                control.lon.Value = (double)args.NewValue;
            }
        }

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PositionControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsReadOnlyChanged));

        public bool IsReadOnly {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        private static void OnIsReadOnlyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {

            var control = obj as PositionControl;
            if (control != null) {
                bool val = (bool)args.NewValue;
                control.lat.IsReadOnly = val;
                control.lon.IsReadOnly = val;
                control.btnEgaz.IsEnabled = !val;
                control.btnGoogleCode.IsEnabled = !val;
            }

        }

        public static readonly DependencyProperty ShowHeaderLabelsProperty = DependencyProperty.Register("ShowHeaderLabels", typeof(bool), typeof(PositionControl), new FrameworkPropertyMetadata(false, OnShowHeaderLabelsChanged));

        public bool ShowHeaderLabels {
            get { return (bool)GetValue(ShowHeaderLabelsProperty); }
            set { SetValue(ShowHeaderLabelsProperty, value); }
        }

        private static void OnShowHeaderLabelsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {

            var control = obj as PositionControl;
            if (control != null) {
                bool val = (bool)args.NewValue;

                if (val) {
                    control.grid.RowDefinitions[0].Height = new GridLength(18);
                    control.grid.RowDefinitions[1].Height = new GridLength(2);
                    control.grid.ColumnDefinitions[1].Width = new GridLength(4);
                    control.lblLon.Visibility = System.Windows.Visibility.Collapsed;
                } else {
                    control.grid.RowDefinitions[0].Height = new GridLength(0);
                    control.grid.RowDefinitions[1].Height = new GridLength(0);
                    control.grid.ColumnDefinitions[1].Width = new GridLength(68);
                    control.lblLon.Visibility = System.Windows.Visibility.Visible;
                }
            }

        }


        private void btnEgaz_Click(object sender, RoutedEventArgs e) {
            LaunchGazetteer();
        }

        private void LaunchGazetteer() {

            var selectOptions = new NamedPlaceSelectionOptions { PlaceNameSeed = "" };

            if (BeforeLocationSelection != null) {
                BeforeLocationSelection(selectOptions);
            }

            PluginManager.Instance.StartSelect<PlaceName>((result) => {
                var place = result.DataObject as PlaceName;
                if (place != null) {
                    lat.Value = place.Latitude;
                    lon.Value = place.Longitude;
                    if (LocationChanged != null) {
                        var locality = place.Name;
                        if (place.PlaceNameType == PlaceNameType.OffsetAndDirection) {
                            locality = string.Format("{0} {1} {2} of {3}", place.Offset, place.Units, place.Direction, place.Name);
                        }
                        LocationChanged(place.Latitude, place.Longitude, null, null, locality, "EGaz");
                    }
                }
            }, LookupOptions.None, selectOptions);
        }

        private void btnGoogleCode_Click(object sender, RoutedEventArgs e) {
            GoogleEarth.GeoTag((lat, lon, altitude) => {
                this.lat.Value = lat;
                this.lon.Value = lon;
                if (this.LocationChanged != null) {
                    LocationChanged(lat, lon, altitude, "m", null, "Google Earth");
                }
            }, this.lat.Value, this.lon.Value);
        }

        public event LocationSelectedEvent LocationChanged;

        public event BeforeNamedPlaceSelectionEvent BeforeLocationSelection;

    }

    public delegate void LocationSelectedEvent(double latitude, double longitude, int? altitude, string altitudeUnits, string locality, string source);

    public delegate void BeforeNamedPlaceSelectionEvent(NamedPlaceSelectionOptions options);

    public class NamedPlaceSelectionOptions : SelectOptions {
        public String PlaceNameSeed { get; set; }
    }

}
