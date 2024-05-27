using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;
using ChatClient.Generation;
using ChatClient.Providers;
using ChatClient.Repositories;
using ChatClient.Types;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using Microsoft.UI.Input;
using Windows.UI.Core;

namespace ChatClient.Views;

public sealed partial class ChatPage : Page, INotifyPropertyChanged {
    private bool _generating;
    private MessageRepository _messageRepository;
    private SettingsProvider _settingsProvider;
    private OpenAIService _openaiApi;
    private Chat _selectedChat;

    public ChatPage() {
        InitializeComponent();
        PropertyChanged += ChatPage_PropertyChanged;
    }

    private Chat SelectedChat {
        get => _selectedChat;
        set {
            if (_selectedChat != value) {
                _selectedChat = value;
                OnPropertyChanged(nameof(SelectedChat));
            }
        }
    }

    private bool Generating {
        get => _generating;
        set {
            if (_generating != value) {
                _generating = value;
                OnPropertyChanged(nameof(Generating));
            }
        }
    }

    private string ParseDomain(string url) {
        return Regex.Match(url, @"^(([^:/?#]+):)?(//([^/?#]*))?([^?#]*)(\?([^#]*))?(#(.*))?").Groups[4].Value;
    }

    public async Task<string> GenerateTitle(string startMessage) { // TODO: create Utils class with all functions
        var response = await _openaiApi.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest {
            Messages = new List<ChatMessage> {
                ChatMessage.FromSystem(
                    "Generate short, catchy title (2-4 words) for initiating a chat conversation. These titles should be engaging and inviting."),
                ChatMessage.FromUser(startMessage)
            },
            Model = Models.Gpt_3_5_Turbo,
            MaxTokens = 16 // probably increase for non-English languages
        });
        return response.Choices[0].Message.Content?.Replace("\"", "").Replace("'", "");
    }

    public async Task GenerateResult() {
        var messages = await _messageRepository.GetMessages(SelectedChat.Id);
        var settings = new GenerationSettings() {
            Model = _settingsProvider.Model,
            Temperature = _settingsProvider.Temperature,
            TopP = _settingsProvider.TopP,
            FrequencyPenalty = _settingsProvider.FrequencyPenalty,
            PresencePenalty = _settingsProvider.PresencePenalty,
            Token = _settingsProvider.OpenAiToken,
        };
        GenerationProvider provider = new OpenAIProvider();

        if (_settingsProvider.Streaming) {
            var message = new ChatMessage("assistant", "");
            AddMessageElement(message);

            var call = provider.GenerateResponseAsStreamAsync(messages, settings, _settingsProvider.Functions);

            string response = "";
            await foreach (var result in call) {
                UpdateLastMessageElement(result.Content);
                response += result.Content;
            }

            message.Content = response;
            await _messageRepository.CreateMessage(SelectedChat.Id, message);
        } else {
            var result = await provider.GenerateResponseAsync(messages, settings, _settingsProvider.Functions);
            var message = new ChatMessage(result.Role, result.Content, result.Name, result.ToolCalls,
                result.ToolCallId);
            AddMessageElement(message);
            await _messageRepository.CreateMessage(SelectedChat.Id, message);
            if (result.ToolCalls != null) {
                foreach (var toolCall in result.ToolCalls) {
                    var funcResult = "";
                    try {
                        var name = toolCall.FunctionCall.Name;
                        var args = toolCall.FunctionCall.ParseArguments();

                        var sb = new StringBuilder();
                        foreach (var kv in args) {
                            sb.AppendLine($"{kv.Key}: {kv.Value}");
                        }

                        AddToolCall(toolCall);

                        funcResult = "Unavailable";
                        switch (toolCall.FunctionCall.Name.ToLower()) {
                            case "google":
                                funcResult = await provider.GoogleAsync(args["query"].ToString(),
                                    _settingsProvider.GoogleSearchId, _settingsProvider.GoogleSearchToken);
                                break;
                            case "ask_web":
                                funcResult = await provider.AskWebpage(args["url"].ToString(),
                                    args["prompt"].ToString(), _settingsProvider.OpenAiToken);
                                break;
                        }
                    } catch (Exception ex) {
                        funcResult = $"Unable to use {toolCall.FunctionCall.Name}: {ex.GetType().Name} - {ex.Message}";
                        NotificationQueue.Show($"{ex.GetType().Name}: {ex.Message}", 5000,
                            $"Unable to use {toolCall.FunctionCall.Name}");
                        Debug.Print(ex.StackTrace);
                    }

                    await _messageRepository.CreateMessage(SelectedChat.Id,
                        ChatMessage.FromTool(funcResult, toolCall.Id));
                }

                await GenerateResult();
            }
        }
    }

    #region Rendering

    private void AddToolCall(ToolCall toolCall) {
        var name = toolCall.FunctionCall.Name;
        var args = toolCall.FunctionCall.ParseArguments();

        var text = $"Using {name}";
        var iconGlyph = "\uE90F";

        var param = string.Join('\n',
            args.Select((kv, _) => $"{kv.Key}: {kv.Value}"));

        switch (name) {
            case "google":
                text = $"Googling '{args["query"]}'";
                iconGlyph = "\uE721";
                break;
            case "ask_web":
                text = $"Analyzing {ParseDomain(args["url"].ToString())}";
                iconGlyph = "\uE774";
                break;
        }

        var icon = new FontIcon {
            Glyph = iconGlyph,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
        };

        var textBlock = new TextBlock {
            Text = text,
            Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
        };

        var stackPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { icon, textBlock }
        };

        var toolTip = new ToolTip { Content = param };
        ToolTipService.SetToolTip(stackPanel, toolTip);
        ListView.Items.Add(stackPanel);
    }

    private void AddMessageElement(ChatMessage message, bool render = false) {
        if (render && message.ToolCalls != null) {
            foreach (var toolCall in message.ToolCalls) {
                AddToolCall(toolCall);
            }
        } else if (message.Content != null && message.ToolCallId == null) {
            var textBlock = new TextBlock() {
                Text = message.Content,
                TextWrapping = TextWrapping.Wrap
            };

            var border = new Border {
                Style = (Style)Application.Current.Resources[
                    message.Role == "user" ? "UserChatBubbleStyle" : "BotChatBubbleStyle"],
                Child = textBlock
            };
            ListView.Items.Add(border);
        }

        if (render) {
            RenderResult();
        }
    }

    private void UpdateLastMessageElement(string token) {
        var border = ListView.Items[^1] as Border;
        var element = border.Child as TextBlock;
        element.Text += token;
        ListView.UpdateLayout();
    }

    private void RenderResult() {
        var border = ListView.Items[^1] as Border;
        if (border == null) return;
        var oldElement = border.Child as TextBlock;

        var regex = new Regex(@"(?s)\$\$(.+?)\$\$|\$(.+?)\$|\\\[(.+?)\\\]|\\\(.*?\\\)");
        string replacement = "![{0}](https://latex.codecogs.com/png.image?{1})";

        var result = regex.Replace(oldElement.Text, match => {
            string expression = "";
            for (int i = 1; i <= 4; i++) {
                expression = match.Groups[i].Value;
                if (!string.IsNullOrEmpty(expression))
                    break;
            }

            if (string.IsNullOrEmpty(expression))
                return match.Value;

            return string.Format(replacement, expression,
                Uri.EscapeDataString(
                    (ActualTheme == ElementTheme.Dark ? @"\fg_white" : @"\fg_black") + " " + expression));
        });

        var textBlock = new MarkdownTextBlock {
            UseSyntaxHighlighting = true,
            Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
            Text = result
        };

        textBlock.LinkClicked += async (sender, e) => { await Launcher.LaunchUriAsync(new Uri(e.Link)); };
        textBlock.ImageClicked += async (sender, e) => {
            var dialog = new ContentDialog {
                XamlRoot = XamlRoot,
                Title = "Image",
                Content = new Image { Source = new BitmapImage(new Uri(e.Link)) },
                CloseButtonText = "Close"
            };
            await dialog.ShowAsync();
        };
        border.Child = textBlock;
    }

    #endregion

    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    private async void ChatPage_PropertyChanged(object sender, PropertyChangedEventArgs e) {
        switch (e.PropertyName) {
            case nameof(SelectedChat):
                ListView.Items.Clear();
                foreach (var message in await _messageRepository.GetMessages(SelectedChat.Id))
                    AddMessageElement(message.AsChatMessage(), true);
                break;
        }
    }

    private void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) {
        if (e.Parameter != null) {
            var chatParams = (ChatParams)e.Parameter;
            _messageRepository = chatParams.Repository;
            _settingsProvider = chatParams.Settings;
            _openaiApi = new OpenAIService(new OpenAiOptions {
                ApiKey =
                    string.IsNullOrEmpty(_settingsProvider.OpenAiToken) ? "non-set" : _settingsProvider.OpenAiToken,
            });
            SelectedChat = chatParams.Chat;
        }

        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e) {
        base.OnNavigatedFrom(e);
        PropertyChanged -= ChatPage_PropertyChanged;
    }

    private async void SendButton_OnClick(object sender, RoutedEventArgs e) {
        if (!_settingsProvider.OpenAiTokenVerified || _settingsProvider.OpenAiToken == null) {
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Error;
            NotificationQueue.Show("Token is either invalid or not set", 2000);
            return;
        }

        var text = MessageBox.Text;
        MessageBox.Text = "";
        if (string.IsNullOrEmpty(text) || Generating) return;
        var message = new ChatMessage("user", text);
        AddMessageElement(message);

        if (!await _messageRepository.HasMessages(SelectedChat.Id)) {
            var title = await GenerateTitle(text);
            await _messageRepository.UpdateChat(SelectedChat.Id, "title", title);
            HeaderTextBlock.Text = title;
        }

        await _messageRepository.CreateMessage(SelectedChat.Id, message);
        Generating = true;
        await GenerateResult();
        RenderResult();
        Generating = false;
        MessageBox.Focus(FocusState.Programmatic);
    }

    private void MessageBox_OnKeyDown(object sender, KeyRoutedEventArgs e) {
        if (e.Key != VirtualKey.Enter) return;
        bool ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(CoreVirtualKeyStates.Down);
        bool shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
            .HasFlag(CoreVirtualKeyStates.Down);

        if (!ctrlPressed && !shiftPressed) {
            SendButton_OnClick(sender, e);
            e.Handled = true;
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

    private async void DeleteButton_OnClick(object sender, RoutedEventArgs e) {
        await _messageRepository.DeleteChat(_selectedChat.Id);
    }

    #endregion
}