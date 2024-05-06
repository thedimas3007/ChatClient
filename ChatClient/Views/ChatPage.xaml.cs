using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using CommunityToolkit.WinUI.UI.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatClient.Views {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatPage : Page {
        private List<string> messages;
        public ChatPage() {
            this.InitializeComponent();
            this.messages = new List<string>() {
                "*a*", "`b`", "**c**", "> d", "[e](https://google.com)", "f", "\\[ 2 + 2 \\]", "[ 2 + 2 ]", "$$ \\pi $$", "```python\nprint('hello world')\n```"
            };

            for (int i = 0; i < messages.Count; i++) {
                MarkdownTextBlock textBlock = new MarkdownTextBlock();
                textBlock.UseSyntaxHighlighting = true;
                textBlock.Background = new SolidColorBrush(Color.FromArgb(0,0,0,0));
                textBlock.Text = messages[i];
                Border border = new Border();
                border.Style = (Style)Application.Current.Resources[i % 2 == 0 ? "UserChatBubbleStyle" : "BotChatBubbleStyle"];
                border.Child = textBlock;
                messagesPanel.Children.Add(border);
            }
        }
    }
}
