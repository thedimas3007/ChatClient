using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
    private SettingsProvider _settingsProvider;

    public SettingsPage() {
        InitializeComponent();
    }
    protected override void OnNavigatedTo(NavigationEventArgs e) {
        if (e.Parameter != null) {
            _settingsProvider = (SettingsProvider)e.Parameter; ;
        }

        base.OnNavigatedTo(e);
    }

    private void HomeButton_OnClick(object sender, RoutedEventArgs e) {
        Process.Start("explorer.exe", _settingsProvider.LocalDir);
    }

    private async void OpenaiTokenButton_OnClick(object sender, RoutedEventArgs e) {
        var openAiService = new OpenAIService(new OpenAiOptions {
            ApiKey = OpenaiToken.Password ?? "not-set"
        });
        OpenaiTokenButton.IsEnabled = false;
        try {
            var result = await openAiService.Models.ListModel();
            OpenaiTokenButton.IsEnabled = true;
            if (!result.Successful) {
                Debug.Fail(result.Error?.Message);
                return;
            }

            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Success;
            _settingsProvider.OpenAiToken = OpenaiToken.Password;
            _settingsProvider.OpenAiTokenVerified = true;
            NotificationQueue.Show("Token verified", 2000);
        } catch (Exception ex) {
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Error;
            NotificationQueue.Show($"{ex.GetType().Name}: {ex.Message}", 5000, "Unable to verify token");
            _settingsProvider.OpenAiTokenVerified = false;
            Debug.Print("Unable to verify token");
            Debug.Print(ex.StackTrace);
        }

        OpenaiTokenButton.IsEnabled = true;
    }

    private void OpenaiToken_OnPasswordChanged(object sender, RoutedEventArgs e) {
        _settingsProvider.OpenAiTokenVerified = false;
    }
}