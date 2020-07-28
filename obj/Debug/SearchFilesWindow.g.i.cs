﻿#pragma checksum "..\..\SearchFilesWindow.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "BD148D903DE3C03006BEEDFA8CA8A3144F3E4ECC8FDF87AA4822E4883E80582D"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace Srch {
    
    
    /// <summary>
    /// SearchFilesWindow
    /// </summary>
    public partial class SearchFilesWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 21 "..\..\SearchFilesWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbSearchBox;
        
        #line default
        #line hidden
        
        
        #line 22 "..\..\SearchFilesWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbSearchBox;
        
        #line default
        #line hidden
        
        
        #line 23 "..\..\SearchFilesWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox cbSearchFilesSubDirectories;
        
        #line default
        #line hidden
        
        
        #line 24 "..\..\SearchFilesWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbFilePattern;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/Srch;component/searchfileswindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\SearchFilesWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 6 "..\..\SearchFilesWindow.xaml"
            ((Srch.SearchFilesWindow)(target)).PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.OnWindowKeyDown);
            
            #line default
            #line hidden
            
            #line 7 "..\..\SearchFilesWindow.xaml"
            ((Srch.SearchFilesWindow)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.OnMouseWheelTbSearchBox);
            
            #line default
            #line hidden
            
            #line 9 "..\..\SearchFilesWindow.xaml"
            ((Srch.SearchFilesWindow)(target)).Closing += new System.ComponentModel.CancelEventHandler(this.OnWindowClosing);
            
            #line default
            #line hidden
            return;
            case 2:
            this.cbSearchBox = ((System.Windows.Controls.ComboBox)(target));
            
            #line 21 "..\..\SearchFilesWindow.xaml"
            this.cbSearchBox.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.cbSearchBoxSelectionChanged);
            
            #line default
            #line hidden
            return;
            case 3:
            this.tbSearchBox = ((System.Windows.Controls.TextBox)(target));
            
            #line 22 "..\..\SearchFilesWindow.xaml"
            this.tbSearchBox.PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.OnMouseWheelTbSearchBox);
            
            #line default
            #line hidden
            return;
            case 4:
            this.cbSearchFilesSubDirectories = ((System.Windows.Controls.CheckBox)(target));
            
            #line 23 "..\..\SearchFilesWindow.xaml"
            this.cbSearchFilesSubDirectories.Checked += new System.Windows.RoutedEventHandler(this.cbSearchFilesSubDirectoriesCheckedChanged);
            
            #line default
            #line hidden
            
            #line 23 "..\..\SearchFilesWindow.xaml"
            this.cbSearchFilesSubDirectories.Unchecked += new System.Windows.RoutedEventHandler(this.cbSearchFilesSubDirectoriesCheckedChanged);
            
            #line default
            #line hidden
            return;
            case 5:
            this.tbFilePattern = ((System.Windows.Controls.TextBox)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

