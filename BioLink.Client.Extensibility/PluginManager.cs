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
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using BioLink.Data;
using BioLink.Data.Model;
using System.Windows.Controls;

namespace BioLink.Client.Extensibility {

    public class PluginManager : IDisposable {

        private static PluginManager _instance;

        private List<IBioLinkExtension> _extensions;
        private ResourceTempFileManager _resourceTempFiles = new ResourceTempFileManager();
        private List<ControlHostWindow> _hostWindows = new List<ControlHostWindow>();

        public static void Initialize(User user, Window parentWindow) {
            if (_instance != null) {
                _instance.Dispose();
            }
            _instance = new PluginManager(user, parentWindow);
        }

        public static PluginManager Instance {
            get {
                if (_instance == null) {
                    throw new Exception("The Plugin Manager has not been initialized!");
                }
                return _instance;
            }
        }

        public IBioLinkPlugin GetLookupTypeOwner(LookupType lookupType) {
            IBioLinkPlugin result = null;
            TraversePlugins((plugin) => {
                if (plugin.CanEditObjectType(lookupType)) {
                    result = plugin;
                }
            });
            return result;
        }

        public PinnableObject GetPinnableForLookupType(LookupType lookupType, int? objectId) {
            var plugin = GetLookupTypeOwner(lookupType);
            if (plugin != null && objectId.HasValue) {
                var pinnable = new PinnableObject(plugin.Name, lookupType, objectId.Value);
                return pinnable;
            }
            return null;
        }

        private PluginManager(User user, Window parentWindow) {
            User = user;            
            _extensions = new List<IBioLinkExtension>();
            this.ParentWindow = parentWindow;
        }

        public ResourceTempFileManager ResourceTempFileManager { 
            get { return _resourceTempFiles; } 
        }

        public List<IBioLinkExtension> Extensions {
            get { return _extensions; }
        }

        public Window ParentWindow { get; private set; }

        public void LoadPlugins(PluginAction pluginAction) {

            var path1 = string.Format("{0}|^BioLink[.].*[.]dll$", AppDomain.CurrentDomain.BaseDirectory);
            var path2 = string.Format("{0}/plugins", AppDomain.CurrentDomain.BaseDirectory);

            LoadPlugins(pluginAction, path1, path2);

            NotifyProgress("Loading html...", 99, ProgressEventType.Update);

            var service = new SupportService(User);
            var links = service.GetMultimediaItems(TraitCategoryType.Biolink.ToString(), SupportService.BIOLINK_HTML_INTRA_CAT_ID);
            if (links.Count > 0) {

                var directory = new DirectoryInfo(Path.Combine(SystemUtils.GetUserDataPath(), ".BioLink"));
                if (directory.Exists) {
                    directory.Delete(true);                    
                }

                directory.Create();


                string htmlfile = "";
                foreach (MultimediaLink link in links) {
                    var filename = Path.Combine(directory.FullName, link.Name + "." + link.Extension);
                    var bytes = service.GetMultimediaBytes(link.MultimediaID);
                    File.WriteAllBytes(filename, bytes);

                    if (link.Extension.StartsWith("html", StringComparison.CurrentCultureIgnoreCase)) {
                        htmlfile = filename;
                    }
                }

                if (!string.IsNullOrWhiteSpace(htmlfile)) {
                    BioLinkCorePlugin core = GetExtensionsOfType<BioLinkCorePlugin>()[0];
                    var browser = new WebBrowser();
                    AddDocumentContent(core, browser, new DockableContentOptions { Title = "Welcome" });
                    browser.Navigate(string.Format("file:///{0}", htmlfile));
                }

            }

            
        }

        public event ProgressHandler ProgressEvent;

        private void LoadPlugins(PluginAction pluginAction, params string[] paths) {
            using (new CodeTimer("Plugin loader")) {
                FileSystemTraverser t = new FileSystemTraverser();
                NotifyProgress("Searching for extensions...", -1, ProgressEventType.Start);

                List<Type> extensionTypes = new List<Type>();

                foreach (string pathelement in paths) {
                    string path = pathelement;
                    string filterexpr = ".*[.]dll$";
                    if (path.Contains("|")) {
                        path = pathelement.Substring(0, pathelement.IndexOf("|"));
                        filterexpr = pathelement.Substring(pathelement.IndexOf("|") + 1);
                    }

                    Regex regex = new Regex(filterexpr, RegexOptions.IgnoreCase);

                    FileSystemTraverser.Filter filter = (fileinfo) => {
                        return regex.IsMatch(fileinfo.Name);
                    };

                    Logger.Debug("LoadPlugins: Scanning: {0}", path);
                    t.FilterFiles(path, filter, fileinfo => { ProcessAssembly(fileinfo, extensionTypes); }, false);
                }

                NotifyProgress("Loading plugins...", 0, ProgressEventType.Start);
                int i = 0;

                foreach (Type type in extensionTypes) {
                    try {
                        Logger.Debug("Instantiating type {0}", type.FullName);

                        IBioLinkExtension extension = InstantiateExtension(type);

                        if (extension != null) {
                            if (extension is IBioLinkPlugin) {

                                IBioLinkPlugin plugin = extension as IBioLinkPlugin;
                                Logger.Debug("Initializing Plugin {0}...", plugin.Name);
                                plugin.InitializePlugin(User, this, this.ParentWindow);

                                Logger.Debug("Integrating Plugin...", plugin.Name);
                                // Allow the consumer to process this plugin...
                                if (pluginAction != null) {
                                    pluginAction(plugin);
                                }
                            }

                            _extensions.Add(extension);

                            double percentComplete = ((double)++i / (double)extensionTypes.Count) * 100.0;
                            NotifyProgress(extension.Name, percentComplete, ProgressEventType.Update);
                        }

                        DoEvents();
                    } catch (Exception ex) {
                        GlobalExceptionHandler.Handle(ex);
                    }
                }

                // Fire an event signalling plugin loading is complete, and all plugins have been initialized
                if (this.PluginsLoaded != null) {
                    PluginsLoaded(this);
                }

                NotifyProgress("Plugin loading complete", 100, ProgressEventType.End);
            }
        }

        public IBioLinkPlugin PluginByName(string pluginName) {
            return _extensions.FirstOrDefault((ext) => {
                return ext.Name == pluginName;
            }) as IBioLinkPlugin;
        }

        private IBioLinkExtension InstantiateExtension(Type type) {
            ConstructorInfo ctor = type.GetConstructor(new Type[] { });
            IBioLinkExtension extension = null;
            if (ctor != null) {
                extension = Activator.CreateInstance(type) as IBioLinkExtension;
            } else {
                throw new Exception(String.Format("Could not load extension {0} - no default constructor", type.FullName));
            }

            return extension;
        }

        public void AddDocumentContent(IBioLinkPlugin plugin, FrameworkElement content, DockableContentOptions options) {

            if (DocumentContentAdded != null) {
                DocumentContentAdded(plugin, content, options);
            }

        }

        public ControlHostWindow GetWindowForContent(IIdentifiableContent control) {
            if (control == null) {
                return null;
            }

            foreach (ControlHostWindow f in _hostWindows) {
                if (f.Control is IIdentifiableContent) {
                    var host = f.Control as IIdentifiableContent;
                    if (host.ContentIdentifier == control.ContentIdentifier) {
                        return f;
                    }
                }
            }
            return null;
        }

        public ControlHostWindow AddNonDockableContent(IBioLinkPlugin plugin, Control content, string title, SizeToContent sizeToContent, bool autoSavePosition = true, Action<ControlHostWindow> initFunc = null) {

            // First check to see if this content is already being shown...
            if (content is IIdentifiableContent) {
                
                var window = GetWindowForContent(content as IIdentifiableContent);
                if (window != null) {                    
                    var existing = window.Control as IIdentifiableContent;
                    window.Show();
                    if (window.WindowState == WindowState.Minimized) {
                        window.WindowState = WindowState.Normal;
                    }
                    window.Focus();
                    existing.RefreshContent();
                    return window;
                }
            }

            bool hideButtons = !(content is DatabaseCommandControl);
            ControlHostWindow form = new ControlHostWindow(User, content, sizeToContent, hideButtons);
            form.Owner = ParentWindow;
            form.Title = title;
            form.Name = "HostFor_" + content.GetType().Name;
            form.SizeToContent = sizeToContent;

            if (content is DatabaseCommandControl) {
                var dbcontrol = content as DatabaseCommandControl;
                dbcontrol.NotifyChangeContainerSet();
            }

            if (content is IIconHolder) {
                var icon = (content as IIconHolder).Icon;
                if (icon != null) {
                    form.Icon = icon;
                }
            }

            if (content is IPreferredSizeHolder) {
                var psh = content as IPreferredSizeHolder;
                form.Height = psh.PreferredHeight;
                form.Width = psh.PreferredWidth;
            } 

            if (autoSavePosition) {

                Config.RestoreWindowPosition(User, form);

                form.Closing += new System.ComponentModel.CancelEventHandler((source, e) => {
                    Config.SaveWindowPosition(User, form);
                });

            }
           
            if (initFunc != null) {
                initFunc(form);
            }

            form.Closed += new EventHandler((source, e) => {
                _hostWindows.Remove(form);
                form.Dispose();
            });

            _hostWindows.Add(form);
            form.Show();
            return form;
        }

        public IMapProvider GetMap() {
            var maps = GetExtensionsOfType<IMapProvider>();
            if (maps != null && maps.Count > 0) {
                return maps[0];
            }
            return null;
        }

        public void RunReport(IBioLinkPlugin owner, IBioLinkReport report) {

            if (report.DisplayOptions(User, ParentWindow)) {
                var results = new ReportResults(report);
                AddDocumentContent(owner, results, new DockableContentOptions { Title=report.Name, IsFloating = Preferences.OpenReportResultsInFloatingWindow.Value });
            }
        }


        private bool NotifyProgress(ProgressHandler handler, string format, params object[] args) {
            return NotifyProgress(String.Format(format, args), -1, ProgressEventType.Update);
        }

        private bool NotifyProgress(string message, double percentComplete, ProgressEventType eventType) {
            if (ProgressEvent != null) {
                return ProgressEvent(message, percentComplete, eventType);
            }
            return true;
        }

        public void EnsureVisible(IBioLinkPlugin plugin, string contentName) {
            if (RequestShowContent != null) {
                RequestShowContent(plugin, contentName);
            }
        }

        private void ProcessAssembly(FileInfo assemblyFileInfo, List<Type> discovered) {

            try {
                Logger.Debug("Checking assembly: {0}", assemblyFileInfo.FullName);
                Assembly candidateAssembly = Assembly.LoadFrom(assemblyFileInfo.FullName);
                foreach (Type candidate in candidateAssembly.GetExportedTypes()) {
                    // Logger.Debug("testing type {0}", candidate.FullName);
                    if (candidate.GetInterface("IBioLinkExtension") != null && !candidate.IsAbstract) {
                        Logger.Debug("Found extension type: {0}", candidate.Name);
                        discovered.Add(candidate);
                    }
                }
            } catch (Exception ex) {
                Logger.Debug(ex.ToString());
            }
        }

        public List<Command> SolicitCommandsForObjects(List<ViewModelBase> selected) {
            var list = new List<Command>();

            if (selected != null && selected.Count > 0) {
                TraversePlugins((p) => {
                    var l = p.GetCommandsForSelected(selected);
                    if (l != null) {
                        list.AddRange(l);
                    }
                });
            }

            return list;
        }

        public List<Command> SolicitCommandsForObjectSet(List<int> objectIds, LookupType type) {
            var list = new List<Command>();

            if (objectIds != null && objectIds.Count > 0) {
                TraversePlugins((p) => {
                    var l = p.GetCommandsForObjectSet(objectIds, type);
                    if (l != null) {
                        list.AddRange(l);
                    }
                });
            }

            return list;
        }

        public void TraversePlugins(PluginAction action) {
            _extensions.ForEach(ext => {
                if (ext is IBioLinkPlugin) {
                    action(ext as IBioLinkPlugin);
                }
            });
        }

        public void PinObject(PinnableObject pinnable) {
            BioLinkCorePlugin core = GetExtensionsOfType<BioLinkCorePlugin>()[0];
            core.PinObject(pinnable);
        }

        public void RefreshPinBoard() {
            BioLinkCorePlugin core = GetExtensionsOfType<BioLinkCorePlugin>()[0];
            core.RefreshPinBoard();            
        }

        public bool StartSelect<T>(Action<SelectionResult> successAction, LookupOptions lookupOptions = LookupOptions.None, SelectOptions selectOptions = null) {

            var list = new List<IBioLinkPlugin>();

            TraversePlugins((p) => {
                if (p.CanSelect<T>()) {
                    list.Add(p);
                }
            });

            if (list.Count == 1) {
                list[0].Select<T>(lookupOptions, successAction, selectOptions);
                return true;
            }

            return false;
        }

        public bool EditLookupObject(LookupType objectType, int objectID) {

            var list = new List<IBioLinkPlugin>();

            if (objectID <= 0) {
                return false;
            }

            TraversePlugins((p) => {
                if (p.CanEditObjectType(objectType)) {
                    list.Add(p);
                }
            });

            if (list.Count == 1) {
                list[0].EditObject(objectType, objectID);
                return true;
            }

            return false;

        }

        public void ShowRegionSelector(List<RegionDescriptor> regions, Action<List<RegionDescriptor>> updateAction) {
            var selectors = GetExtensionsOfType<IRegionSelector>();
            if (selectors != null && selectors.Count > 0) {
                selectors[0].SelectRegions(regions, updateAction);
            } else {
                throw new Exception("Failed to launch to region map. Map plugin not found.");
            }
        }

        static void DoEvents() {
            DispatcherFrame frame = new DispatcherFrame(true);
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, (SendOrPostCallback)delegate(object arg) {
                var f = arg as DispatcherFrame;
                f.Continue = false;
            },frame);
            Dispatcher.PushFrame(frame);
        }

        public void Dispose(Boolean disposing) {
            if (disposing) {
                
                var frm = new ShutdownProgess();
                try {
                    frm.Show();
                    Logger.Debug("Disposing the Plugin Manager");
                    List<ControlHostWindow> temp = new List<ControlHostWindow>(_hostWindows);

                    var itemCount = temp.Count + _extensions.Count;
                    frm.progressBar.Maximum = itemCount;
                    frm.progressBar.Value = 0;
                    foreach (ControlHostWindow window in temp) {
                        Logger.Debug("Disposing control host window '{0}'...", window.Title);
                        frm.StatusMessage("Closing window " + window.Title);
                        DoEvents();
                        try {
                            window.Close();
                            window.Dispose();
                        } catch (Exception ex) {
                            Logger.Warn("Exception occured whilst disposing host window '{0}' : {1}", window.Title, ex);
                        }
                        
                        frm.progressBar.Value += 1;
                        DoEvents();
                    }

                    _extensions.ForEach((ext) => {
                        Logger.Debug("Disposing extension '{0}'", ext);
                        frm.StatusMessage("Unloading extension " + ext.Name);
                        DoEvents();
                        try {
                            ext.Dispose();
                        } catch (Exception ex) {
                            Logger.Warn("Exception occured whilst disposing plugin '{0}' : {1}", ext, ex);
                        }
                        frm.progressBar.Value += 1;
                        DoEvents();
                    });
                    _extensions.Clear();
                    frm.StatusMessage("Cleaning up temporary files...");
                    DoEvents();
                    Logger.Debug("Cleaning up resource temp files...");
                    _resourceTempFiles.CleanUp();
                    // Purge any temporary files that were created during the session
                    Logger.Debug("Cleaning up generic temp files...");
                    TempFileManager.CleanUp();

                    TraitCategoryTypeHelper.Reset();

                } finally {
                    frm.Close();    
                }
            }
        }

        ~PluginManager() {
            Dispose(false);
        }
        
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public User User { get; private set; }

        internal List<IBioLinkPlugin> PlugIns {
            get {
                return GetExtensionsOfType<IBioLinkPlugin>();                
            }
        }

        public List<T> GetExtensionsOfType<T>() {
            return _extensions.FindAll((ext) => { return ext is T; }).ConvertAll((ext) => { return (T) ext; });
        }

        public IBioLinkPlugin GetPluginByName(string name) {
            foreach (IBioLinkPlugin plugin in PlugIns) {
                if (plugin.Name == name) {
                    return plugin;
                }
            }

            return null;
        }

        public bool RequestShutdown() {

            foreach (ControlHostWindow window in _hostWindows) {
                if (!window.RequestClose()) {
                    return false;
                }
            }

            foreach (IBioLinkPlugin plugin in PlugIns) {
                if (!plugin.RequestShutdown()) {
                    return false;
                }
            }

            return true;
        }

        internal void CloseContent(FrameworkElement content) {
            if (DockableContentClosed != null) {
                DockableContentClosed(content);
            }
        }

        public event ShowDockableContributionDelegate RequestShowContent;

        public event AddDockableContentDelegate DocumentContentAdded;

        public event CloseDockableContentDelegate DockableContentClosed;

        public event Action<PluginManager> PluginsLoaded;
        
        public delegate void PluginAction(IBioLinkPlugin plugin);

        public delegate void ShowDockableContributionDelegate(IBioLinkPlugin plugin, string name);

        public delegate void AddDockableContentDelegate(IBioLinkPlugin plugin, FrameworkElement content, DockableContentOptions options);

        public delegate void CloseDockableContentDelegate(FrameworkElement content);


        public T FindAdaptorForPinnable<T>(PinnableObject pinnable) {
            List<T> candidates = new List<T>();

            TraversePlugins((plugin) => {
                var candidate = plugin.GetAdaptorForPinnable<T>(pinnable);
                if (candidate != null) {
                    candidates.Add(candidate);
                }
            });

            if (candidates.Count > 0) {
                return candidates[0];
            }

            return default(T);
        }


        public bool CheckSearchResults(System.Collections.ICollection list) {
            if (list != null) {
                var limit = Preferences.MaxSearchResults.Value;                
                if (list.Count > limit) {
                    ErrorMessage.Show("Your search returned too many results (more than {0} rows). Please refine your search criteria and try again.", limit);
                    return false;
                }
            }

            return true;
        }

        public ViewModelBase GetViewModel(PinnableObject pinnable) {
            if (pinnable != null) {
                return GetViewModel(pinnable.LookupType, pinnable.ObjectID);
            }
            return null;
        }

        public ViewModelBase GetViewModel(LookupType t, int objectId) {

            var candidates = new List<ViewModelBase>();
            TraversePlugins((plugin) => {
                if (plugin.CanEditObjectType(t)) {
                    var pinnable = new PinnableObject(plugin.Name, t, objectId);
                    candidates.Add(plugin.CreatePinnableViewModel(pinnable));
                }
            });

            if (candidates.Count > 0) {
                return candidates[0];
            }
            return null;
        }
    }

}
