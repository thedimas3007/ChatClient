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
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using OpenAI_API.Moderation;
using CommunityToolkit.WinUI.Animations;
using System.Xml.Linq;
using Windows.UI.Core.AnimationMetrics;
using OpenAI_API.Completions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatClient.Views {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatPage : Page {
        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private OpenAIAPI openaiApi = new OpenAI_API.OpenAIAPI();
        private List<ChatMessage> messages = new List<ChatMessage>();
        private bool generating = false;

        public ChatPage() {
            this.InitializeComponent();
        }

        private void addMessage(ChatMessage message) {
            MarkdownTextBlock textBlock = new MarkdownTextBlock() {
                UseSyntaxHighlighting = true,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                Text = message.TextContent
            };

            Border border = new Border {
                Style = (Style)Application.Current.Resources[message.Role.Equals(ChatMessageRole.User) ? "UserChatBubbleStyle" : "BotChatBubbleStyle"],
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
            
            ChatMessage message = new ChatMessage(ChatMessageRole.User, text);
            messages.Add(message);
            addMessage(message);

            sendButton.IsEnabled = false;
            messageBox.IsEnabled = false;
            generating = true;

            openaiApi.Auth = new APIAuthentication(localSettings.Values["openaiToken"].ToString());
            Debug.WriteLine("Starting generating!");
            Debug.WriteLine(messages[^1].TextContent);

            ChatMessage newMessage = new ChatMessage(ChatMessageRole.Assistant, "");
            addMessage(newMessage);
            string response = "";
            //var result = await openaiApi.Chat.CreateChatCompletionAsync(messages, Model.ChatGPTTurbo);
            await foreach (ChatResult result in openaiApi.Chat.StreamChatEnumerableAsync(messages)) {
                updateLastMessage(result.ToString());
                response += result.ToString();
            }

            newMessage.TextContent = response;
            messages.Add(newMessage);

            sendButton.IsEnabled = true;
            messageBox.IsEnabled = true;
            generating = false;
            Debug.WriteLine("Finished generating!");
        }
    }
}
