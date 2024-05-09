using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatClient {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window {
        public MainWindow() {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems.OfType<NavigationViewItem>().First();
            SetTitleBar(AppTitleBar);
        }
        private void NavigationViewControl_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args) {
            NavigationViewControl.IsBackEnabled = ContentFrame.CanGoBack;
            Type selectedPage;
            if (args.SelectedItemContainer.Tag.ToString() == "Settings") {
                selectedPage = typeof(Views.SettingsPage);
            } else {
                selectedPage = Type.GetType(args.SelectedItemContainer.Tag.ToString());
            }

            if (selectedPage == null) {
                return;
            }

            ContentFrame.Navigate(selectedPage, null, new EntranceNavigationTransitionInfo());
            //NavigationViewControl.Header = ((NavigationViewItem)NavigationViewControl.SelectedItem)?.Content?.ToString();
        }

        private void NavigationViewControl_OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) {
            if (ContentFrame.CanGoBack) ContentFrame.GoBack();
        }

    }
}
