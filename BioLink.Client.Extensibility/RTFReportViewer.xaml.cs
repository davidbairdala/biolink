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
using System.IO;
using Microsoft.Win32;

namespace BioLink.Client.Extensibility {
    /// <summary>
    /// Interaction logic for RTFReportViewer.xaml
    /// </summary>
    public partial class RTFReportViewer : UserControl {
        public RTFReportViewer() {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e) {
            SaveRTF();
        }

        private void SaveRTF() {
            var dlg = new SaveFileDialog();
            dlg.Filter = "RTF files (*.rtf)|*.rtf|All files (*.*)|*.*";
            if (dlg.ShowDialog().GetValueOrDefault(false)) {

                try {
                    this.Cursor = Cursors.Wait;
                    using (StreamWriter writer = new StreamWriter(dlg.FileName)) {
                        writer.Write(RTF);
                    }
                } finally {
                    this.Cursor = Cursors.Arrow;
                }
            }
        }

        public String RTF {
            get {
                return rtf.Rtf;
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e) {
            rtf.Print();
        }


        private void btnCopy_Click(object sender, RoutedEventArgs e) {
            var data = new DataObject();
            data.SetText(RTF, TextDataFormat.Rtf);
            data.SetText(rtf.Text, TextDataFormat.Text);
            Clipboard.SetDataObject(data);
        }

        public String ReportName { get; set; }
    }

    public class RTFReportViewerSource : IReportViewerSource {
        public string Name {
            get { return "RTF"; }
        }

        public FrameworkElement ConstructView(IBioLinkReport report, Data.DataMatrix reportData, Utilities.IProgressObserver progress) {
            var viewer = new RTFReportViewer();
            viewer.ReportName = report.Name;
            viewer.rtf.Rtf = reportData.Rows[0][0] as string;
            return viewer;
        }
    }
}
