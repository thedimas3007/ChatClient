using System;
using System.Linq;
using ChatClient.Repositories;
using ChatClient.Types;
using ChatClient.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Animation;

namespace ChatClient;

public sealed partial class MainWindow : Window {
    private readonly MessageRepository _messageRepository = new();

    public MainWindow() {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;

        NavigationViewControl.MenuItems.Clear();
        foreach (var chat in _messageRepository.Chats) {
            var item = new NavigationViewItem {
                Tag = chat.Id,
                Icon = new FontIcon { Glyph = "\uE8BD" }
            };
            item.SetBinding(ContentControl.ContentProperty, new Binding {
                Path = new PropertyPath("Title"),
                Mode = BindingMode.OneWay,
                Source = chat
            });
            NavigationViewControl.MenuItems.Add(item);
        }

        if (NavigationViewControl.MenuItems.Count > 0)
            NavigationViewControl.SelectedItem =
                NavigationViewControl.MenuItems.OfType<NavigationViewItem>().FirstOrDefault();
        SetTitleBar(AppTitleBar);

        _messageRepository.ChatCreated += (_, chat) => {
            var item = new NavigationViewItem {
                Tag = chat.Id,
                Icon = new FontIcon { Glyph = "\uE8BD" }
            };
            item.SetBinding(ContentControl.ContentProperty, new Binding {
                Path = new PropertyPath("Title"),
                Mode = BindingMode.OneWay,
                Source = chat
            });
            NavigationViewControl.MenuItems.Insert(0, item);
        };

        _messageRepository.ChatDeleted += (_, id) => {
            var items = NavigationViewControl.MenuItems;
            for (var i = 0; i < items.Count; i++) {
                var item = (NavigationViewItem)items[i];
                if ((int)item.Tag == id) items.RemoveAt(i);
            }

            NavigationViewControl.SelectedItem =
                NavigationViewControl.MenuItems.OfType<NavigationViewItem>().FirstOrDefault();
        };
    }

    private async void NavigationViewControl_OnSelectionChanged(NavigationView sender,
        NavigationViewSelectionChangedEventArgs args) {
        var selection = ((NavigationViewItem)sender.SelectedItem)?.Tag?.ToString();
        if (selection == null) {
            ContentFrame.Navigate(typeof(Page), null, new EntranceNavigationTransitionInfo());
            return;
        }

        Type selectedPage;
        object parameter = null;

        if (selection == "Settings") {
            selectedPage = typeof(SettingsPage);
        } else if (int.TryParse(selection, out _)) {
            selectedPage = typeof(ChatPage);
            parameter = new ChatParams(_messageRepository, await _messageRepository.GetChat(int.Parse(selection)));
        } else {
            selectedPage = Type.GetType(selection);
        }

        if (selectedPage == null) return;

        ContentFrame.Navigate(selectedPage, parameter, new EntranceNavigationTransitionInfo());
    }

    private async void NewChatButton_OnClick(object sender, RoutedEventArgs e) {
        var newChat = await _messageRepository.CreateChat("New Chat");
        var chatParams = new ChatParams(_messageRepository, newChat);
        //ContentFrame.Navigate(typeof(Views.ChatPage), chatParams, new EntranceNavigationTransitionInfo());
        NavigationViewControl.SelectedItem =
            NavigationViewControl.MenuItems.OfType<NavigationViewItem>().FirstOrDefault();
    }
}