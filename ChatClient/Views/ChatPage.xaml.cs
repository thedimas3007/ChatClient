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
        private List<ChatMessage> _messages = new();
        private bool _generating = false;

        public ChatPage() {
            InitializeComponent();
            _openaiApi = new OpenAIService(new OpenAiOptions() {
                ApiKey = _localSettings.Values["openaiToken"].ToString()
            });
        }

        private void AddMessage(ChatMessage message) {
            MarkdownTextBlock textBlock = new() {
                UseSyntaxHighlighting = true,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                Text = message.Content
            };

            Border border = new() {
                Style = (Style)Application.Current.Resources[message.Role == "user" ? "UserChatBubbleStyle" : "BotChatBubbleStyle"],
                Child = textBlock
            };
            MessagesPanel.Children.Add(border);
            ChatScroller.UpdateLayout();
            ChatScroller.ChangeView(null, ChatScroller.ScrollableHeight, null, false);
        }

        private void UpdateLastMessage(string token) {
            ((MessagesPanel.Children[^1] as Border).Child as MarkdownTextBlock).Text += token;
            ChatScroller.UpdateLayout();
            ChatScroller.ChangeView(null, ChatScroller.ScrollableHeight, null, false);
        }

        private async void SendButton_OnClick(object sender, RoutedEventArgs e) {
            string text = MessageBox.Text;
            MessageBox.Text = "";
            if (text.Trim() == "" || _generating) {
                return;
            }
            
            ChatMessage message = new("user", text);
            _messages.Add(message);
            AddMessage(message);

            SendButton.IsEnabled = false;
            MessageBox.IsEnabled = false;
            _generating = true;

            Debug.WriteLine("Starting generating!");
            Debug.WriteLine(_messages[^1].Content);

            ChatMessage newMessage = new("assistant", "");
            AddMessage(newMessage);
            string response = "";
            var call = _openaiApi.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest() {
                Messages = _messages,
                Model = Models.Gpt_3_5_Turbo
            });
            await foreach (var result in call) {
                string res = result.Choices.FirstOrDefault()?.Message.Content;
                UpdateLastMessage(res);
                response += res;
            }

            newMessage.Content = response;
            _messages.Add(newMessage);

            SendButton.IsEnabled = true;
            MessageBox.IsEnabled = true;
            _generating = false;
            Debug.WriteLine("Finished generating!");
            MessageBox.Focus(FocusState.Programmatic);
        }

        private void MessageBox_OnKeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key == VirtualKey.Enter) {
                SendButton_OnClick(sender, e);
            }
        }
    }
}
