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
using System.Threading;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;

namespace BioLink.Client.Gazetteer {
    /// <summary>
    /// Interaction logic for Gazetteer.xaml
    /// </summary>
    public partial class Gazetteer : UserControl, IDisposable {
        
        private GazetteerService _service;
        private ObservableCollection<PlaceNameViewModel> _searchModel = null;
        private GazetterPlugin _owner;
        private int _maximumSearchResults = 1000;

        private OffsetControl _offsetControl;
        private DistanceDirectionControl _dirDistControl;
        private Action<SelectionResult> _selectionCallback;
        private ObservableCollection<GazetteerFile> _fileMRU;
        private Timer _mapUpdateTimer;

        #region Designer CTOR
        public Gazetteer() {
            InitializeComponent();
            if (!this.IsDesignTime()) {
                throw new Exception("Wrong constructor!");
            }
        }
        #endregion

        public Gazetteer(GazetterPlugin owner) {
            InitializeComponent();
            _searchModel = new ObservableCollection<PlaceNameViewModel>();
            lstResults.ItemsSource = _searchModel;
            _owner = owner;
            btnDataInfo.IsEnabled = false;
            
            List<string> list = Config.GetUser(_owner.User, "gazetteer.recentlyUsedFiles", new List<string>());

            _fileMRU = new ObservableCollection<GazetteerFile>(list.ConvertAll((path) => {
                return new GazetteerFile(path);
            }));

            cmbFile.ItemsSource = _fileMRU;

            cmbFile.SelectionChanged += new SelectionChangedEventHandler(cmbFile_SelectionChanged);

            _offsetControl = new OffsetControl();
            _offsetControl.SelectedPlaceNameChanged += new Action<PlaceName>((place) => {
                UpdateMap();
            });

            _dirDistControl = new DistanceDirectionControl();

            lstResults.SelectionChanged += new SelectionChangedEventHandler(lstResults_SelectionChanged);

            optFindDistDir.Checked += new RoutedEventHandler(optFindDistDir_Checked);
            optFindLatLong.Checked += new RoutedEventHandler(optFindLatLong_Checked);

            optFindLatLong.IsChecked = true;

            Loaded += new RoutedEventHandler(Gazetteer_Loaded);

            ListBoxDragHelper.Bind(lstResults, CreateDragData);

            _mapUpdateTimer = new Timer((state) => {
                // Disable the timer...
                _mapUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
                UpdateMapAsync();
            }, null, Timeout.Infinite, Timeout.Infinite);            
        }

        void Gazetteer_Loaded(object sender, RoutedEventArgs e) {
            if (_service == null || cmbFile.SelectedItem == null) {
                string lastFile = Config.GetUser(_owner.User, "gazetteer.lastFile", "");
                if (!String.IsNullOrEmpty(lastFile)) {
                    var gazFile = FindOrAddToMRU(lastFile);
                    cmbFile.SelectedItem = gazFile;
                }
            }
        }

        private GazetteerFile FindOrAddToMRU(string filename) {
            var gazFile = _fileMRU.FirstOrDefault((m) => {
                return m.FullPath.Equals(filename, StringComparison.CurrentCultureIgnoreCase);
            });
            if (gazFile == null) {
                gazFile = AddFileToMRU(filename);
            }

            return gazFile;
        }

        void cmbFile_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selected = cmbFile.SelectedItem as GazetteerFile;
            if (selected != null) {
                LoadFile(selected.FullPath);
            }
        }

        void lstResults_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var place = lstResults.SelectedItem as PlaceNameViewModel;
            _offsetControl.DataContext = place;
            _dirDistControl.DataContext = place;
            UpdateMap();
        }

        void optFindLatLong_Checked(object sender, RoutedEventArgs e) {
            CalcOptionChanged();
        }

        void optFindDistDir_Checked(object sender, RoutedEventArgs e) {
            CalcOptionChanged();
        }

        private void CalcOptionChanged() {
            grpCalc.Content = null;
            if (optFindLatLong.IsChecked.GetValueOrDefault(false)) {
                grpCalc.Content = _offsetControl;
                grpCalc.Header = "Find Lat./Long. using Dist./Dir.";
            } else {
                grpCalc.Content = _dirDistControl;
                grpCalc.Header = "Find Dist./Dir. using Lat./Long.";
            }

        }

        private DataObject CreateDragData(ViewModelBase dragged) {
            var selected = dragged as PlaceNameViewModel;
            if (selected != null) {
                var data = new DataObject("Pinnable", selected);
                var pinnable = new PinnableObject(GazetterPlugin.GAZETTEER_PLUGIN_NAME, LookupType.PlaceName, 0, selected.Model);
                data.SetData(PinnableObject.DRAG_FORMAT_NAME, pinnable);
                data.SetData(DataFormats.Text, selected.DisplayLabel);
                return data;
            }

            return null;
        }

        private void LoadFile(string filename) {
            try {

                if (filename == null) {
                    return;
                }

                _service = new GazetteerService(filename);
                // cmbFile.Text = filename;
                btnDataInfo.IsEnabled = true;
                // now populate the Divisions combo box...
                var divisions = _service.GetDivisions();
                cmbDivision.ItemsSource = divisions;
                cmbDivision.SelectedIndex = 0;
                AddFileToMRU(filename);
                Config.SetUser(_owner.User, "gazetteer.lastFile", filename);
            } catch (Exception ex) {
                ErrorMessage.Show(ex.ToString());
            }
        }

        private GazetteerFile AddFileToMRU(string filename) {

            var gazFile = _fileMRU.FirstOrDefault((c) => {
                return c.FullPath.Equals(filename, StringComparison.CurrentCultureIgnoreCase);
            });

            if (gazFile == null) {
                gazFile = new GazetteerFile(filename);
                _fileMRU.Insert(0, gazFile);
            } 

            int mruLength = Config.GetGlobal("Gazetteer.MaxMRUItems", 4);
            while (_fileMRU.Count > mruLength) {
                _fileMRU.RemoveAt(mruLength);
            }

            return gazFile;
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e) {
            ChooseFile();            
        }

        private void ChooseFile() {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = _owner.GetCaption("Gazetteer.FileOpen.Title");
            dlg.Filter = "Gazetteer files (*.gaz)|*.gaz|All files (*.*)|*.*";
            bool? result = dlg.ShowDialog();
            if (result.GetValueOrDefault(false)) {
                LoadFile(dlg.FileName);
                var gazFile = FindOrAddToMRU(dlg.FileName);
                cmbFile.SelectedItem = gazFile;
            }
        }


        private void delayedTriggerTextbox1_TypingPaused(string text) {
            DoSearch(text);
        }

        private void DoSearch(string text) {
            try {

                if (_service == null) {
                    return;
                }

                lstResults.InvokeIfRequired(() => {
                    lstResults.Cursor = Cursors.Wait;
                    lblResults.Content = _owner.GetCaption("Gazetteer.Search.Searching");
                });


                if (!String.IsNullOrEmpty(text)) {
                    List<PlaceName> results = null;

                    bool limit = false;
                    chkLimit.InvokeIfRequired(() => {
                        limit = chkLimit.IsChecked.HasValue && chkLimit.IsChecked.Value;
                    });

                    string division = "";
                    if (limit) {
                        cmbDivision.InvokeIfRequired(() => {
                            CodeLabelPair selected = cmbDivision.SelectedItem as CodeLabelPair;
                            if (selected != null) {
                                division = selected.Code;
                            }
                        });
                    }

                    if (limit && (!String.IsNullOrEmpty(division))) {
                        results = _service.FindPlaceNamesLimited(text, division, _maximumSearchResults + 1);
                    } else {
                        results = _service.FindPlaceNames(text, _maximumSearchResults + 1);
                    }

                    lstResults.InvokeIfRequired(() => {
                        if (results.Count > _maximumSearchResults) {
                            lblResults.Content = _owner.GetCaption("Gazetteer.Search.Results.TooMany", _maximumSearchResults);
                        } else {
                            lblResults.Content = _owner.GetCaption("Gazetteer.Search.Results", results.Count);
                        }

                        _offsetControl.Clear();
                        _dirDistControl.Clear();
                        _searchModel.Clear();                        
                        foreach (PlaceName place in results) {
                            _searchModel.Add(new PlaceNameViewModel(place));
                        }
                    });
                } else {
                    lblResults.Content = "";
                }
            } catch (Exception ex) {
                GlobalExceptionHandler.Handle(ex);
            } finally {
                lstResults.InvokeIfRequired(() => {
                    lstResults.Cursor = Cursors.Arrow;
                });
            }

        }

        private void delayedTriggerTextbox1_TextChanged(object sender, TextChangedEventArgs e) {
            if (_searchModel != null) {
                _searchModel.Clear();
                lblResults.Content = "";
            }
        }

        private void delayedTriggerTextbox1_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Down) {
                lstResults.SelectedIndex = 0;
                if (lstResults.SelectedItem != null) {
                    ListBoxItem item = lstResults.ItemContainerGenerator.ContainerFromItem(lstResults.SelectedItem) as ListBoxItem;
                    item.Focus();
                }
            }
        }


        public void Dispose() {
            if (_service != null) {
                Config.SetUser(_owner.User, "gazetteer.lastFile", _service.FileName);
                Config.SetUser(_owner.User, "gazetteer.recentlyUsedFiles", new List<string>( _fileMRU.Select((m) => {
                    return m.FullPath;
                })));
                _service.Dispose();
            }
        }

        private void chkLimit_Checked(object sender, RoutedEventArgs e) {
            cmbDivision.IsEnabled = true;
            DoSearch(txtFind.Text);
        }

        private void chkLimit_Unchecked(object sender, RoutedEventArgs e) {
            cmbDivision.IsEnabled = false;
            DoSearch(txtFind.Text);
        }

        private void cmbDivision_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            DoSearch(txtFind.Text);
        }

        private void button1_Click(object sender, RoutedEventArgs e) {
            _offsetControl.Clear();
        }

        private void button2_Click(object sender, RoutedEventArgs e) {
            ShowMap();
        }

        private IMapProvider _map;

        public void ShowMap() {
            var providers = PluginManager.Instance.GetExtensionsOfType<IMapProvider>();
            if (providers != null && providers.Count > 0) {
                _map = providers[0];
                if (_map != null) {
                    _map.Show();
                    UpdateMap();
                }
            }
        }

        private void UpdateMap() {
            _mapUpdateTimer.Change(500, Timeout.Infinite);
        }

        private void UpdateMapAsync() {

            if (_map == null) {
                return;
            }

            this.InvokeIfRequired(() => {

                var selected = lstResults.SelectedItem as PlaceNameViewModel;
                bool drawOffsetPoint = _offsetControl.IsVisible && _offsetControl.OffsetPlace != null && !string.IsNullOrWhiteSpace(_offsetControl.OffsetPlace.Offset);
                var offset = _offsetControl.OffsetPlace;

                _map.HideAnchor();
                _map.ClearPoints();
                if (selected != null) {
                    _map.DropAnchor(selected.Longitude, selected.Latitude, selected.Name);

                    if (drawOffsetPoint) {
                        MapPoint p = new MapPoint();
                        p.Latitude = offset.Latitude;
                        p.Longitude = offset.Longitude;
                        p.Label = string.Format("{0} {1} {2} of {3}", offset.Offset, offset.Units, offset.Direction, offset.Name);
                        var set = new ListMapPointSet("_pointLayer");
                        set.DrawLabels = true;
                        set.Add(p);

                        _map.PlotPoints(set);
                    }
                }

            });
        }

        public void BindSelectCallback(Action<SelectionResult> selectionFunc) {
            if (selectionFunc != null) {
                btnSelect.Visibility = Visibility.Visible;
                btnSelect.IsEnabled = true;
                _selectionCallback = selectionFunc;
            } else {
                ClearSelectCallback();
            }
        }

        public void ClearSelectCallback() {
            _selectionCallback = null;
            btnSelect.Visibility = Visibility.Hidden;
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e) {
            DoSelect();
        }

        private void DoSelect() {

            PlaceName result = null;
            if (optFindLatLong.IsChecked.GetValueOrDefault(false)) {
                result = _offsetControl.OffsetPlace;
            }

            if (result == null) {
                var selected = lstResults.SelectedItem as PlaceNameViewModel;
                if (selected != null) {
                    result = selected.Model;
                }
            }

            if (result != null && _selectionCallback != null) {
                var selResult = new SelectionResult {
                    ObjectID = null,
                    DataObject = result,
                    Description = result.Name
                };

                _selectionCallback(selResult);
            }

        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e) {
            ShowGazetteerInfo();
        }

        private void ShowGazetteerInfo() {
            if (_service != null) {
                var info = _service.GetGazetteerInfo();
                if (info != null) {
                    var frm = new GazetteerInfoForm(info);
                    frm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    frm.Owner = PluginManager.Instance.ParentWindow;
                    frm.ShowDialog();
                }
            }
        }

        private void btnDataInfo_Click(object sender, RoutedEventArgs e) {
            ShowGazetteerInfo();
        }

        public GazetteerService Service { 
            get { return _service; } 
        }

        public PlaceName SelectedPlace {
            get {
                var selected = lstResults.SelectedItem as PlaceNameViewModel;
                if (selected != null) {
                    return selected.Model;
                }

                return null;
            }
        }

        private void btnFindNearestPlace_Click(object sender, RoutedEventArgs e) {
            _owner.ShowNearestNamedPlace();
        }

    }

    public class GazetteerFile {
        public GazetteerFile(string path) {
            FullPath = path;
            var info = new FileInfo(path);
            Name = info.Name;
        }

        public string FullPath { get; set; }

        public string Name { get; private set; }
        
    }

    public class GazFileComparer : IEqualityComparer<GazetteerFile> {

        public bool Equals(GazetteerFile x, GazetteerFile y) {
            return x.FullPath.Equals(y.FullPath, StringComparison.CurrentCultureIgnoreCase);
        }

        public int GetHashCode(GazetteerFile obj) {
            return obj.FullPath.GetHashCode();
        }
    }
    
}

