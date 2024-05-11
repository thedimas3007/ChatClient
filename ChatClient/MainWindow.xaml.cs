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
using ChatClient.Types;
using System.Reflection.Metadata;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatClient {
    public sealed partial class MainWindow : Window {
        private MessageRepository _messageRepository = new MessageRepository();

        public MainWindow() {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;

            NavigationViewControl.MenuItems.Clear();
            foreach (var chat in _messageRepository.Chats) {
                var item = new NavigationViewItem() {
                    Tag = chat.Id,
                    Icon = new FontIcon { Glyph = "\uE8BD" }
                };
                item.SetBinding(NavigationViewItem.ContentProperty, new Binding() {
                    Path = new PropertyPath("Title"),
                    Mode = BindingMode.OneWay,
                    Source = chat
                });
                NavigationViewControl.MenuItems.Add(item);
            }

            if (NavigationViewControl.MenuItems.Count > 0) {
                NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems.OfType<NavigationViewItem>().First();
            }
            SetTitleBar(AppTitleBar);

            _messageRepository.ChatCreated += (_, chat) => {
                var item = new NavigationViewItem() {
                    Tag = chat.Id,
                    Icon = new FontIcon { Glyph = "\uE8BD" }
                };
                item.SetBinding(NavigationViewItem.ContentProperty, new Binding() {
                    Path = new PropertyPath("Title"),
                    Mode = BindingMode.OneWay,
                    Source = chat
                });
                NavigationViewControl.MenuItems.Insert(0, item);
            };

            _messageRepository.ChatDeleted += (_, id) => {
                var items = NavigationViewControl.MenuItems;
                for (int i = 0; i < items.Count(); i++) {
                    var item = (NavigationViewItem)items[i];
                    if ((int)item.Tag == id) {
                        items.RemoveAt(i);
                    }
                }
            };
        }

        private async void NavigationViewControl_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args) {
            string selection = args.SelectedItemContainer?.Tag?.ToString();
            if (selection == null) { return; }
            Type selectedPage;
            object parameter = null;
            
            if (selection == "Settings") {
                selectedPage = typeof(Views.SettingsPage);
            } else if (Int32.TryParse(selection, result: out _)) {
                selectedPage = typeof(Views.ChatPage);
                parameter = new ChatParams(_messageRepository, await _messageRepository.GetChat(int.Parse(selection)));
            } else {
                selectedPage = Type.GetType(selection);
            }

            if (selectedPage == null) {
                return;
            }

            ContentFrame.Navigate(selectedPage, parameter, new EntranceNavigationTransitionInfo());
        }

        private async void NewChatButton_OnClick(object sender, RoutedEventArgs e) {
            Chat newChat = await _messageRepository.CreateChat("New Chat");
            ChatParams chatParams = new ChatParams(_messageRepository, newChat);
            //ContentFrame.Navigate(typeof(Views.ChatPage), chatParams, new EntranceNavigationTransitionInfo());
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems.OfType<NavigationViewItem>().First();
        }
    }
}
