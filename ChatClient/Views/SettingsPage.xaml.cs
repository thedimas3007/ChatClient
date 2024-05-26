using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using ChatClient.Controls;
using ChatClient.Generation;
using ChatClient.Providers;
using ChatClient.Repositories;
using ChatClient.Types;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using OpenAI;
using OpenAI.Managers;

namespace ChatClient.Views;

public class BoolToFontIconConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        var isTrue = (bool)value;
        var glyph = isTrue ? "\xE73E" : "\xE711";
        return new FontIcon { Glyph = glyph };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}

public sealed partial class SettingsPage : Page {
    public double Increment => 0.01;
    private SettingsProvider _settingsProvider; 

    public SettingsPage() {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) {
        if (e.Parameter != null) {
            _settingsProvider = (SettingsProvider)e.Parameter;
        }

        base.OnNavigatedTo(e);
    }

    private void HomeButton_OnClick(object sender, RoutedEventArgs e) {
        Process.Start("explorer.exe", _settingsProvider.LocalDir);
    }

    private async void OpenAiTokenInput_OnTokenVerificationRequested(object sender, string e) {
        TokenInput tokenInput = (TokenInput)sender;
        var openAiService = new OpenAIService(new OpenAiOptions {
            ApiKey = string.IsNullOrEmpty(tokenInput.Token) ? "non-set" : tokenInput.Token,
        });

        try {
            var result = await openAiService.Models.ListModel();
            tokenInput.IsEnabled = true;
            if (!result.Successful) {
                Debug.Fail(result.Error?.Message);
                return;
            }

            _settingsProvider.OpenAiToken = tokenInput.Token;
            _settingsProvider.OpenAiTokenVerified = true;
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Success;
            NotificationQueue.Show("Token verified", 2000);
        } catch (Exception ex) {
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Error;
            NotificationQueue.Show($"{ex.GetType().Name}: {ex.Message}", 5000, "Unable to verify token");
            _settingsProvider.OpenAiTokenVerified = false;
            Debug.Print("Unable to verify token");
            Debug.Print(ex.StackTrace);
        }
    }
    
    private async void GoogleSearchTokens_OnTokenVerificationRequested(object sender, string e) {
        TokenInput tokenInput = (TokenInput)sender;
        try {
            await GenerationProvider.Providers.FirstOrDefault()
                .GoogleAsync("Test", GoogleSearchId.Token, GoogleSearchToken.Token);
            _settingsProvider.GoogleSearchId = GoogleSearchId.Token;
            _settingsProvider.GoogleSearchToken = GoogleSearchToken.Token;
            _settingsProvider.GoogleSearchVerified = true;
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Success;
            NotificationQueue.Show("Token verified", 2000);
        } catch (Exception ex) {
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Error;
            NotificationQueue.Show($"{ex.GetType().Name}: {ex.Message}", 5000, "Unable to verify token");
            _settingsProvider.OpenAiTokenVerified = false;
            Debug.Print("Unable to verify token");
            Debug.Print(ex.StackTrace);
            _settingsProvider.GoogleSearchVerified = false;
        }
    }

    private void ResetButton_OnClick(object sender, RoutedEventArgs e) {
        _settingsProvider.Temperature = 1f;
        _settingsProvider.TopP = 1f;
        _settingsProvider.FrequencyPenalty = 0f;
        _settingsProvider.PresencePenalty = 0f;
    }

    private void StreamingToggle_OnToggled(object sender, RoutedEventArgs e) {
        FunctionsToggle.IsOn = false;
    }

    private void FunctionsToggle_OnToggled(object sender, RoutedEventArgs e) {
        StreamingToggle.IsOn = false;
    }
}