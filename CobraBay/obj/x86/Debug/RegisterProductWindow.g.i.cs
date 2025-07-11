﻿#pragma checksum "..\..\..\RegisterProductWindow.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "E3DEE37B6F6CA767026025C7C11DCEEE8F7C0EE667852E47C597D64C147FEFBD"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using CBViewModel;
using LocalResources.Properties;
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


namespace CobraBay {
    
    
    /// <summary>
    /// RegisterProductWindow
    /// </summary>
    public partial class RegisterProductWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 36 "..\..\..\RegisterProductWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Image FrontierLogo;
        
        #line default
        #line hidden
        
        
        #line 72 "..\..\..\RegisterProductWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock DialogTitle;
        
        #line default
        #line hidden
        
        
        #line 81 "..\..\..\RegisterProductWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock LinkIntroBlock;
        
        #line default
        #line hidden
        
        
        #line 132 "..\..\..\RegisterProductWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button RegisterButton;
        
        #line default
        #line hidden
        
        
        #line 169 "..\..\..\RegisterProductWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button LinkButton;
        
        #line default
        #line hidden
        
        
        #line 189 "..\..\..\RegisterProductWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Documents.Hyperlink LinkHelp;
        
        #line default
        #line hidden
        
        
        #line 192 "..\..\..\RegisterProductWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Documents.Run AccountLinkText;
        
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
            System.Uri resourceLocater = new System.Uri("/EDLaunch;component/registerproductwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\RegisterProductWindow.xaml"
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
            
            #line 13 "..\..\..\RegisterProductWindow.xaml"
            ((CobraBay.RegisterProductWindow)(target)).KeyDown += new System.Windows.Input.KeyEventHandler(this.KeyPressed);
            
            #line default
            #line hidden
            return;
            case 2:
            this.FrontierLogo = ((System.Windows.Controls.Image)(target));
            return;
            case 3:
            
            #line 46 "..\..\..\RegisterProductWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.OnClose);
            
            #line default
            #line hidden
            return;
            case 4:
            this.DialogTitle = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 5:
            this.LinkIntroBlock = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 6:
            this.RegisterButton = ((System.Windows.Controls.Button)(target));
            
            #line 137 "..\..\..\RegisterProductWindow.xaml"
            this.RegisterButton.Click += new System.Windows.RoutedEventHandler(this.OnRegisterClick);
            
            #line default
            #line hidden
            return;
            case 7:
            this.LinkButton = ((System.Windows.Controls.Button)(target));
            
            #line 174 "..\..\..\RegisterProductWindow.xaml"
            this.LinkButton.Click += new System.Windows.RoutedEventHandler(this.OnLoginClick);
            
            #line default
            #line hidden
            return;
            case 8:
            this.LinkHelp = ((System.Windows.Documents.Hyperlink)(target));
            
            #line 191 "..\..\..\RegisterProductWindow.xaml"
            this.LinkHelp.Click += new System.Windows.RoutedEventHandler(this.OnSupportClick);
            
            #line default
            #line hidden
            return;
            case 9:
            this.AccountLinkText = ((System.Windows.Documents.Run)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

