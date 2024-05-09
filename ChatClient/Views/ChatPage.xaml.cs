using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using CommunityToolkit.WinUI.Animations;
using System.Xml.Linq;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core.AnimationMetrics;
using ChatClient.Repositories;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatClient.Views {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatPage : Page {
        private ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private OpenAIService _openaiApi;
        private Chat _selectedChat = new Chat(-1, "New Chat", DateTime.Now, DateTime.Now);
        private bool _generating = false;

        public ChatPage() {
            InitializeComponent();
            MessageRepository.Load();
            _openaiApi = new OpenAIService(new OpenAiOptions() {
                ApiKey = _localSettings.Values["openaiToken"].ToString()
            });
        }

        private void AddMessageElement(ChatMessage message) {
            var textBlock = new MarkdownTextBlock() {
                UseSyntaxHighlighting = true,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                Text = message.Content
            };

            var border = new Border() {
                Style = (Style)Application.Current.Resources[message.Role == "user" ? "UserChatBubbleStyle" : "BotChatBubbleStyle"],
                Child = textBlock
            };
            ListView.Items.Add(border);
        }

        private void UpdateLastMessageElement(string token) {
            var border = ListView.Items[^1] as Border;
            MarkdownTextBlock element = border.Child as MarkdownTextBlock;
            element.Text += token;
            ListView.UpdateLayout();
        }

        private async void SendButton_OnClick(object sender, RoutedEventArgs e) {
            string text = MessageBox.Text;
            MessageBox.Text = "";
            if (text.Trim() == "" || _generating) {
                return;
            }
            
            if (_selectedChat.Id == -1) {
                _selectedChat = await MessageRepository.CreateChat("Chat!");
            }

            var message = new ChatMessage("user", text);
            AddMessageElement(message);
            await _selectedChat.CreateMessage(message);

            SendButton.IsEnabled = false;
            MessageBox.IsEnabled = false;
            _generating = true;

            ChatMessage newMessage = new("assistant", "");
            AddMessageElement(newMessage);
            string response = "";
            var call = _openaiApi.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest() {
                Messages = await _selectedChat.GetChatMessages(),
                Model = Models.Gpt_3_5_Turbo
            });
            await foreach (var result in call) {
                string res = result.Choices.FirstOrDefault()?.Message.Content;
                UpdateLastMessageElement(res);
                response += res;
            }

            newMessage.Content = response;
            await _selectedChat.CreateMessage(newMessage);

            SendButton.IsEnabled = true;
            MessageBox.IsEnabled = true;
            _generating = false;
            MessageBox.Focus(FocusState.Programmatic);
        }

        private void MessageBox_OnKeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key == VirtualKey.Enter) {
                SendButton_OnClick(sender, e);
            }
        }

        private void FileButton_OnClick(object sender, RoutedEventArgs e) {
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Warning;
            NotificationQueue.Show("Files are not yet implemented", 2000, "Not implemented");
        }

        private void RenameButton_OnClick(object sender, RoutedEventArgs e) {
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Warning;
            NotificationQueue.Show("Chat renaming is not yet implemented", 2000, "Not implemented");
        }
    }
}
