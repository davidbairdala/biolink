﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using BioLink.Client.Utilities;
using System.IO;
using BioLink.Data;


namespace BioLink.Client.Extensibility.Export {

    public class Excel2003Exporter : TabularDataExporter {

        protected override object GetOptions(Window parentWindow) {

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Export"; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.OverwritePrompt = false;
            dlg.Filter = "XML Excel Workbook (.xml)|*.xml|All files (*.*)|*.*"; // Filter files by extension
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true) {
                ExcelExporterOptions options = new ExcelExporterOptions();
                options.Filename = dlg.FileName;
                return options;
            }

            return null;
        }

        public override void ExportImpl(Window parentWindow, Data.DataMatrix matrix, object optionsObj) {
            ExcelExporterOptions options = optionsObj as ExcelExporterOptions;

            if (options == null) {
                return;
            }
            
            if (FileExistsAndNotOverwrite(options.Filename)) {
                return;
            }

            ProgressStart("Preparing to export...");

            int totalRows = matrix.Rows.Count;

            using (StreamWriter writer = new StreamWriter(options.Filename)) {
                writer.WriteLine("<?xml version=\"1.0\"?><ss:Workbook xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
                writer.WriteLine("<ss:Worksheet ss:Name=\"Exported Data\">");
                writer.WriteLine("<ss:Table>");
                int currentRow = 0;                
                foreach (MatrixRow row in matrix.Rows) {
                    writer.WriteLine("<ss:Row>");
                    for (int i = 0; i < matrix.Columns.Count; ++i) {
                        object val = row[i];
                        writer.Write("<ss:Cell><ss:Data ss:Type=\"String\">");
                        String str = (val == null ? "" : val.ToString());
                        writer.Write(Escape(str));
                        writer.Write("</ss:Data></ss:Cell>");
                    }
                    if (++currentRow % 1000 == 0) {
                        double percent = ((double)currentRow / (double)totalRows) * 100.0;
                        ProgressMessage(String.Format("Exported {0} of {1} rows...", currentRow, totalRows), percent);
                    }

                    writer.WriteLine("</ss:Row>");
                }

                writer.WriteLine("</ss:Table>");
                writer.WriteLine("</ss:Worksheet>");
                writer.WriteLine("</ss:Workbook>");
            }

            ProgressEnd(String.Format("{0} rows exported.", totalRows));
        }

        public override void Dispose() {
        }

        #region Properties

        public override string Description {
            get { return "Export data as a Microsoft Excel 2003 XML Worksheet"; }
        }

        public override string Name {
            get { return "Excel 2003 XML"; }
        }

        public override BitmapSource Icon {
            get {
                return ImageCache.GetPackedImage("images/excel2003_exporter.png", GetType().Assembly.GetName().Name);
            }
        }

        #endregion

    }


    public class ExcelExporterOptions {

        public string Filename { get; set; }

    }
}