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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using ChatClient.Repositories;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatClient {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems.OfType<NavigationViewItem>().First();
            SetTitleBar(AppTitleBar);
            LoadChats().GetAwaiter().GetResult();
        }

        private async Task LoadChats() {
            await MessageRepository.Load();
            var chats = await MessageRepository.GetChats();
            foreach (var chat in chats) {
                HistoryNavigationItem.MenuItems.Add(new NavigationViewItem() {
                    Content = chat.Title,
                    Tag = $"OpenChat-{chat.Id}",
                    Icon = new FontIcon() {
                        Glyph = "\uE8F2"
                    }
                });
            }

        }

        private void NavigationViewControl_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args) {
            string selection = args.SelectedItemContainer.Tag?.ToString();
            if (selection == null) { return; }
            NavigationViewControl.IsBackEnabled = ContentFrame.CanGoBack;
            Type selectedPage;
            object parameter = null;
            if (selection == "Settings") {
                selectedPage = typeof(Views.SettingsPage);
            } else if (selection.StartsWith("OpenChat")) {
                selectedPage = typeof(Views.ChatPage);
                int id = Int32.Parse(selection.Split('-')[1]);
                parameter = id;
            } else {
                selectedPage = Type.GetType(selection);
            }

            if (selectedPage == null) {
                return;
            }

            ContentFrame.Navigate(selectedPage, parameter, new EntranceNavigationTransitionInfo());
            //NavigationViewControl.Header = ((NavigationViewItem)NavigationViewControl.SelectedItem)?.Content?.ToString();
        }

        private void NavigationViewControl_OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) {
            if (ContentFrame.CanGoBack) ContentFrame.GoBack();
        }

    }
}
