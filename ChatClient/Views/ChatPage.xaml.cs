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
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private OpenAiOptions openaiOptions = new OpenAiOptions();
        private OpenAIService openaiApi;
        private List<ChatMessage> messages = new List<ChatMessage>();
        private bool generating = false;

        public ChatPage() {
            InitializeComponent();
            openaiApi = new OpenAIService(new OpenAiOptions() {
                ApiKey = localSettings.Values["openaiToken"].ToString()
            });
        }

        private void addMessage(ChatMessage message) {
            MarkdownTextBlock textBlock = new MarkdownTextBlock() {
                UseSyntaxHighlighting = true,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                Text = message.Content
            };

            Border border = new Border {
                Style = (Style)Application.Current.Resources[message.Role == "user" ? "UserChatBubbleStyle" : "BotChatBubbleStyle"],
                Child = textBlock
            };
            messagesPanel.Children.Add(border);
            chatScroller.UpdateLayout();
            chatScroller.ChangeView(null, chatScroller.ScrollableHeight, null, false);
        }

        private void updateLastMessage(string token) {
            ((messagesPanel.Children[^1] as Border).Child as MarkdownTextBlock).Text += token;
            chatScroller.UpdateLayout();
            chatScroller.ChangeView(null, chatScroller.ScrollableHeight, null, false);
        }

        private async void SendButton_OnClick(object sender, RoutedEventArgs e) {
            string text = messageBox.Text;
            messageBox.Text = "";
            if (text.Trim() == "" || generating) {
                return;
            }
            
            ChatMessage message = new ChatMessage("user", text);
            messages.Add(message);
            addMessage(message);

            sendButton.IsEnabled = false;
            messageBox.IsEnabled = false;
            generating = true;

            Debug.WriteLine("Starting generating!");
            Debug.WriteLine(messages[^1].Content);

            ChatMessage newMessage = new ChatMessage("assistant", "");
            addMessage(newMessage);
            string response = "";
            var call = openaiApi.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest() {
                Messages = messages,
                Model = Models.Gpt_3_5_Turbo
            });
            await foreach (var result in call) {
                string res = result.Choices.FirstOrDefault()?.Message.Content;
                updateLastMessage(res);
                response += res;
            }

            newMessage.Content = response;
            messages.Add(newMessage);

            sendButton.IsEnabled = true;
            messageBox.IsEnabled = true;
            generating = false;
            Debug.WriteLine("Finished generating!");
            messageBox.Focus(FocusState.Programmatic);
        }

        private void MessageBox_OnKeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key == VirtualKey.Enter) {
                SendButton_OnClick(sender, e);
            }
        }
    }
}
