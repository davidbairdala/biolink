﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using BioLink.Data;

namespace BioLink.Client.Extensibility {

    public interface IBioLinkPlugin : IDisposable {
        string Name { get; }
        List<IWorkspaceContribution> Contributions { get; }
        User User { get; set; }
        PluginManager PluginManager { get; set; }
    }

    /// <summary>
    /// Marker interface for things that plugins can contribute to the BioLink client
    /// </summary>
    public interface IWorkspaceContribution {
        string Name { get; }
    }

    public abstract class WorkspaceContributionBase : IWorkspaceContribution {

        private string _name;

        protected WorkspaceContributionBase(IBioLinkPlugin owner, string name) {
            this._name = name;
            this.Owner = owner;
        }

        public virtual string Name {
            get { return _name; }
        }

        public IBioLinkPlugin Owner { get; private set; }
    }

    public interface IExplorerWorkspaceContribution : IWorkspaceContribution {
        String Title { get; set; }
        Control Content { get; }
        void InitializeContent();
    }

    /// <summary>
    /// Explorers get shown at startup, and are dockable...
    /// </summary>
    public class ExplorerWorkspaceContribution<T> : WorkspaceContributionBase, IExplorerWorkspaceContribution where T : Control {

        private ExplorerInitializerDelegate _initializer;
        private T _content;

        public ExplorerWorkspaceContribution(IBioLinkPlugin owner, string name, T content, string title, ExplorerInitializerDelegate initializer = null) : base(owner, name) {            
            _content = content;
            Title = title;
            _initializer = initializer;
            
        }

        public String Title { get; set; }

        public Control Content {
            get { return _content; }
        }

        public void InitializeContent() {
            if (_initializer != null) {
                _initializer(_content);
            }
        }

        public delegate void ExplorerInitializerDelegate(T content);
        
    }

    

    public class MenuWorkspaceContribution : WorkspaceContributionBase {

        public MenuItemDescriptor[] Path { get; private set; }        
        public RoutedEventHandler Action { get; private set; }

        public MenuWorkspaceContribution(IBioLinkPlugin owner, string name, RoutedEventHandler action, MenuItemDescriptor[] path) : base(owner, name) {
            this.Action = action;
            this.Path = path;
        }

        public MenuWorkspaceContribution(IBioLinkPlugin owner, string name, RoutedEventHandler action, params string[] path) : base(owner, name) {
            List<MenuItemDescriptor> items = new List<MenuItemDescriptor>();
            foreach (string pathdesc in path) {
                MenuItemDescriptor desc = null;
                if (pathdesc.StartsWith("{") && pathdesc.EndsWith("}")) {
                    desc = JsonConvert.DeserializeObject<MenuItemDescriptor>(pathdesc);
                } else {
                    desc = new MenuItemDescriptor();
                    desc.Name = pathdesc.StartsWith("_") ? pathdesc.Substring(1) : pathdesc;
                    desc.Header = pathdesc;
                }
                items.Add(desc);
            }            
            this.Action = action;
            this.Path = items.ToArray();
        }

        public override String Name {
            get { return Path[Path.Length -1].Name; }
        }

    }

    public class MenuItemDescriptor {
        public string Name { get; set; }
        public string Header { get; set; }
        public string InsertBefore { get; set; }
        public string InsertAfter { get; set; }
        public bool SeparatorBefore { get; set; }
        public bool SeparatorAfter { get; set; }
    }


}