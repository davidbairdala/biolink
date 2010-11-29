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
using BioLink.Data;
using BioLink.Data.Model;
using BioLink.Client.Utilities;

namespace BioLink.Client.Material {
    /// <summary>
    /// Interaction logic for SiteDetails.xaml
    /// </summary>
    public partial class SiteDetails : DatabaseActionControl {

        private SiteViewModel _viewModel;

        #region Designer Constructor
        public SiteDetails() {
            InitializeComponent();
        }
        #endregion

        public SiteDetails(User user, int siteID) : base(user, "Site:" + siteID) {
            InitializeComponent();
            this.SiteID = siteID;

            // Radio button checked event handlers
            optNearestPlace.Checked += new RoutedEventHandler((s, e) => {
                txtLocality.IsEnabled = false;
                txtDirectionFrom.IsEnabled = true;
                txtDistanceFrom.IsEnabled = true;
                txtFrom.IsEnabled = true;
            });

            optLocality.Checked += new RoutedEventHandler((s, e) => {
                txtLocality.IsEnabled = true;
                txtDirectionFrom.IsEnabled = false;
                txtDistanceFrom.IsEnabled = false;
                txtFrom.IsEnabled = false;
            });

            var service = new MaterialService(user);
            var model = service.GetSite(siteID);
            _viewModel = new SiteViewModel(model);
            this.DataContext = _viewModel;

            tabSite.AddTabItem("Traits", new TraitControl(user, TraitCategoryType.Site, siteID));
            tabSite.AddTabItem("Notes", new NotesControl(user, TraitCategoryType.Site, siteID));
            tabSite.AddTabItem("Multimedia", new MultimediaControl(user, TraitCategoryType.Site, siteID));
            tabSite.AddTabItem("Ownership", new OwnershipDetails(_viewModel.Model));

            txtPosSource.BindUser(User, PickListType.Phrase, "Source", TraitCategoryType.Site);
            txtPosWho.BindUser(User, "tblSite", "vchrPosWho");
            txtPosOriginal.BindUser(User, PickListType.Phrase, "OriginalDetermination", TraitCategoryType.Site);

            txtElevUnits.BindUser(User, PickListType.Phrase, "Units", TraitCategoryType.Site);
            txtElevSource.BindUser(User, PickListType.Phrase, "Source", TraitCategoryType.Site);

            txtGeoEra.BindUser(User, PickListType.Phrase, "Geological Era", TraitCategoryType.Site);
            txtGeoPlate.BindUser(User, PickListType.Phrase, "Geological Plate", TraitCategoryType.Site);
            txtGeoStage.BindUser(User, PickListType.Phrase, "Geological State", TraitCategoryType.Site);

            txtGeoFormation.BindUser(User, PickListType.Phrase, "Geological Formation", TraitCategoryType.Site);
            txtGeoMember.BindUser(User, PickListType.Phrase, "Geological Member", TraitCategoryType.Site);
            txtGeoBed.BindUser(User, PickListType.Phrase, "Geological Bed", TraitCategoryType.Site);


            _viewModel.DataChanged += new DataChangedHandler(_viewModel_DataChanged);

            txtPoliticalRegion.BindUser(user, LookupType.Region);

            UpdateMiniMap(model.PosY1, model.PosX1);
        }

        private void UpdateMiniMap(double latitude, double longitude) {

            if (imgMap.Width == 0 || imgMap.Height == 0) {
                return;
            }

            double meridian = imgMap.Width / 2.0;
            double equator = imgMap.Height / 2.0;
            double x = meridian + ((longitude / 180) * meridian);
            double y = equator - ((latitude / 90) * equator);

            string assemblyName = this.GetType().Assembly.GetName().Name;
            var img = ImageCache.GetPackedImage(@"images\World.png", assemblyName);

            RenderTargetBitmap bmp = new RenderTargetBitmap((int) img.Width, (int) img.Height, 96, 96, PixelFormats.Pbgra32);
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext dc = drawingVisual.RenderOpen();

            dc.DrawImage(img, new Rect(0, 0, (int)imgMap.Width, (int) imgMap.Height));
            var brush = new SolidColorBrush(Colors.Red);
            var pen = new Pen(brush, 1);

            dc.DrawEllipse(brush, pen, new Point(x, y), 2, 2);

            dc.Close();
            bmp.Render(drawingVisual);

            imgMap.Source = bmp;
        }

        void _viewModel_DataChanged(ChangeableModelBase viewmodel) {
            RegisterUniquePendingChange(new UpdateSiteAction(_viewModel));
        }

        #region Properties

        public int SiteID { get; private set; }

        #endregion
    
    }

}