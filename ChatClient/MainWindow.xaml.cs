using System;
using System.IO;
using System.Linq;
using Windows.System;
using ChatClient.Providers;
using ChatClient.Repositories;
using ChatClient.Types;
using ChatClient.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using CommunityToolkit.WinUI.UI.Controls;
using Windows.UI.Popups;
using Serilog;

namespace ChatClient;

public sealed partial class MainWindow : Window {
    private readonly MessageRepository _messageRepository = new();
    private readonly SettingsProvider _settingsProvider = new();
    private readonly string _localDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatClient");

    public MainWindow() {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;

        NavigationViewControl.MenuItems.Clear();
        foreach (var chat in _messageRepository.Chats) {
            AddChat(chat);
        }

        if (NavigationViewControl.MenuItems.Count > 0)
            NavigationViewControl.SelectedItem =
                NavigationViewControl.MenuItems.OfType<NavigationViewItem>().FirstOrDefault();
        SetTitleBar(AppTitleBar);

        _messageRepository.ChatCreated += (_, chat) => {
            AddChat(chat);
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
            parameter = _settingsProvider;
        } else if (int.TryParse(selection, out _)) {
            selectedPage = typeof(ChatPage);
            parameter = new ChatParams(_messageRepository, _settingsProvider, await _messageRepository.GetChat(int.Parse(selection)));
        } else {
            selectedPage = Type.GetType(selection);
        }

        if (selectedPage == null) return;

        ContentFrame.Navigate(selectedPage, parameter, new EntranceNavigationTransitionInfo());
    }

    private async void NewChatButton_OnClick(object sender, RoutedEventArgs e) {
        var newChat = await _messageRepository.CreateChat("New Chat");
        var chatParams = new ChatParams(_messageRepository, _settingsProvider, newChat);
        //ContentFrame.Navigate(typeof(Views.ChatPage), chatParams, new EntranceNavigationTransitionInfo());
        NavigationViewControl.SelectedItem =
            NavigationViewControl.MenuItems.OfType<NavigationViewItem>().FirstOrDefault();
    }

    private void AddChat(Chat chat) {
        var infoItem = new MenuFlyoutItem {
            Text = "Info",
            Icon = new FontIcon { Glyph = "\uE946" }
        };
        infoItem.Click += async (_, _) => {
            var stackPanel = new StackPanel {
                Children = {
                    new TextBlock { Text = $"Id: {chat.Id}" },
                    new TextBlock { Text = $"Title: {chat.Title}" },
                    new TextBlock { Text = $"Last Accessed: {chat.LastAccessed}" },
                    new TextBlock { Text = $"Created At: {chat.CreatedAt}" },
                    new TextBlock { Text = $"Total Messaged: {(await _messageRepository.GetMessages(chat.Id)).Count}"}
                }
            };

            var dialog = new ContentDialog {
                XamlRoot = Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "Chat info",
                CloseButtonText = "Close",
                Content = stackPanel
            };
            await dialog.ShowAsync();

        };

        var deleteItem = new MenuFlyoutItem {
            Text = "Delete",
            //KeyboardAccelerators = {
            //    new KeyboardAccelerator { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.D }
            //},
            Icon = new FontIcon { Glyph = "\uE74D" }
        };
        deleteItem.Click += async (_, _) => {
            await _messageRepository.DeleteChat(chat.Id);
        };


        var item = new NavigationViewItem {
            Tag = chat.Id,
            Icon = new FontIcon { Glyph = "\uE8BD" },
            ContextFlyout = new MenuFlyout {
                Items = { infoItem, deleteItem }
            }
        };

        item.SetBinding(ContentControl.ContentProperty, new Binding {
            Path = new PropertyPath("Title"),
            Mode = BindingMode.OneWay,
            Source = chat
        });
        NavigationViewControl.MenuItems.Insert(0, item);

    }
}