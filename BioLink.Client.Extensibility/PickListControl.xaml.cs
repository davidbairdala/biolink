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
using BioLink.Data;
using BioLink.Data.Model;


namespace BioLink.Client.Extensibility {
    /// <summary>
    /// Interaction logic for PhraseSelector.xaml
    /// </summary>
    public partial class PickListControl : UserControl {

        
        public PickListControl() {
            InitializeComponent();
            GotFocus += new RoutedEventHandler(PickListControl_GotFocus);
        }

        public PickListControl(User user, PickListType type, string phraseCategory, TraitCategoryType traitCategory) {
            InitializeComponent();
            BindUser(user, type, phraseCategory, traitCategory);
            GotFocus += new RoutedEventHandler(PickListControl_GotFocus);
        }

        public PickListControl(User user, string tableName, string fieldName) {
            InitializeComponent();
            BindUser(user, tableName, fieldName);

            GotFocus += new RoutedEventHandler(PickListControl_GotFocus);
        }

        void PickListControl_GotFocus(object sender, RoutedEventArgs e) {            
            txt.Focus();
            e.Handled = true;
        }

        public void BindUser(User user, PickListType pickListType, String categoryName, TraitCategoryType traitCategory) {
            this.User = user;
            this.CategoryName = categoryName;
            this.TraitCategory = traitCategory;
            this.PickListType = pickListType;

            if (pickListType == Extensibility.PickListType.MultimediaType) {
                txt.IsReadOnly = true;
            }

            this.Service = new SupportService(user);

            txt.PreviewKeyDown += new KeyEventHandler(txt_PreviewKeyDown);
        }

        void txt_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Space && (Keyboard.Modifiers & ModifierKeys.Control) > 0) {
                e.Handled = true;
                txt.SelectAll(); // We do this so that the selection will overwrite the initial filter that was typed in...
                ShowPickList(true, txt.Text);
            }

        }

        public void BindUser(User user, string tableName, string fieldName) {
            this.User = user;
            this.PickListType = Extensibility.PickListType.DistinctList;
            this.TableName = tableName;
            this.FieldName = fieldName;
            txt.GotFocus += new RoutedEventHandler((source, e) => {
                txt.SelectAll();
            });

            this.Service = new SupportService(user);
            txt.PreviewKeyDown += new KeyEventHandler(txt_PreviewKeyDown);
        }


        private void btn_Click(object sender, RoutedEventArgs e) {
            ShowPickList(false);
        }

        public static string ShowDistinctList(User user, string table, string field, string filter, Control ownerControl = null, Control ownerAncestor = null) {
            var service = new SupportService(user);
            Func<IEnumerable<string>> itemsFunc = () => {
                return service.GetDistinctValues(table, field);
            };

            PickListWindow frm = new PickListWindow(user, String.Format("Distinct values for {0}_{1}",table, field), itemsFunc, null, filter, ownerControl, ownerAncestor);

            if (frm.ShowDialog().GetValueOrDefault(false)) {
                return frm.SelectedValue as string;
            };

            return null;
        }

        public static string ShowPickList(User user, PickListType type, string categoryName, TraitCategoryType traitCategory, string filter = null, Control owner = null, Control ownerAncestor = null) {
            Func<IEnumerable<string>> itemsFunc = null;
            Func<string, bool> addItemFunc = null;
            string caption = "Select a value";
            var service = new SupportService(user);
            switch (type) {
                case PickListType.Phrase:
                    int phraseCategoryID = service.GetPhraseCategoryId(categoryName, true);
                    caption = String.Format("Values for '{0}'", categoryName);
                    itemsFunc = () => service.GetPhrases(phraseCategoryID).ConvertAll((phrase) => phrase.PhraseText);

                    addItemFunc = (newphrase) => {
                        Phrase phrase = new Phrase();
                        phrase.PhraseID = -1;
                        phrase.PhraseCatID = phraseCategoryID;
                        phrase.PhraseText = newphrase;
                        // Save the new phrase value...
                        service.InsertPhrase(phrase);
                        return true;
                    };
                    break;
                case PickListType.DistinctTraitList:
                    caption = String.Format("Values for '{0}'", categoryName);
                    itemsFunc = () => service.GetTraitValues(categoryName, traitCategory.ToString());
                    break;
                case PickListType.DistinctList:
                    
                    break;
                case PickListType.Trait:
                    caption = String.Format("Existing trait names for {0}", traitCategory.ToString());
                    itemsFunc = () => service.GetTraitNamesForCategory(traitCategory.ToString());
                    break;
                case PickListType.MultimediaType:
                    caption = "Select a multimedia type...";
                    itemsFunc = () => {
                        return service.GetMultimediaTypes().ConvertAll((mmtype) => mmtype.Name);
                    };
                    break;
                case PickListType.RefLinkType:
                    caption = "Reference Link types";
                    itemsFunc = () => {
                        var list = service.GetRefLinkTypes();    
                        SortedDictionary<string, string> filtered = new SortedDictionary<string, string>();
                        // Remove the duplicates...Something really dodgey is going on when inserting ref links, it looks like
                        // duplicate ref link types are being created.
                        foreach (string item in list) {
                            filtered[item] = item;
                        }
                        return filtered.Keys;
                    };
                    addItemFunc = (newval) => {
                        service.InsertRefLinkType(newval, traitCategory.ToString());
                        return true;
                    };
                    break;
                default:
                    throw new Exception("Unhandled pick list type: " + type);
            }

            PickListWindow frm = new PickListWindow(user, caption, itemsFunc, addItemFunc, filter, owner, ownerAncestor);

            if (frm.ShowDialog().GetValueOrDefault(false)) {
                return frm.SelectedValue as string;
            };

            return null;
        }

        private void ShowPickList(bool positionControl, string filter = "") {
            string value = null;
            if (PickListType == Extensibility.PickListType.DistinctList) {
                value = ShowDistinctList(User, TableName, FieldName, filter, (positionControl ? txt : null), (positionControl ? this : null));
            } else {
                value = ShowPickList(User, PickListType, CategoryName, TraitCategory, filter, (positionControl ? txt : null), (positionControl ? this : null));
            }

            if (value != null) {
                txt.Text = value;
            }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(PickListControl), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnTextChanged)));

        private static void OnTextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
            var control = (PickListControl) obj;
            control.txt.Text = args.NewValue as String;
            control.FireValueChanged(control.txt.Text);
        }

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PickListControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsReadOnlyChanged));

        public bool IsReadOnly {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        private static void OnIsReadOnlyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {

            var control = obj as PickListControl;
            if (control != null) {
                bool val = (bool)args.NewValue;
                control.txt.IsReadOnly = val;
                control.btn.IsEnabled = !val;
            }

        }


        protected void FireValueChanged(string text) {
            if (this.ValueChanged != null) {
                ValueChanged(this, text);
            }
        }

        public String Text {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        protected User User { get; private set; }

        protected string CategoryName { get; private set; }

        protected TraitCategoryType TraitCategory { get; private set; }

        protected PickListType PickListType { get; private set; }

        private void txt_TextChanged(object sender, TextChangedEventArgs e) {
            Text = txt.Text;
        }

        private void txt_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) > 0) {
                ShowPickList(true, txt.Text);
                e.Handled = true;
            }
        }

        public event TextChangedHandler ValueChanged;

        protected SupportService Service { get; private set; }

        protected String TableName { get; set; }

        protected String FieldName { get; set; }

    }

    public enum PickListType {
        Phrase,
        Trait,
        Keyword,
        DistinctList,
        DistinctTraitList,        
        MultimediaType, 
        RefLinkType
    }

    public delegate void TextChangedHandler(object source, string value);
}
