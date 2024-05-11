using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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

public sealed partial class SettingsPage : Page, INotifyPropertyChanged {
    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
    private bool _openaiVerified;

    public SettingsPage() {
        OpenaiVerified = (bool)(_localSettings.Values["OpenaiTokenVerified"] ?? false);
        InitializeComponent();
    }

    public bool OpenaiVerified {
        get => _openaiVerified;
        set {
            if (_openaiVerified != value) {
                _openaiVerified = value;
                _localSettings.Values["OpenaiTokenVerified"] = value;
                OnPropertyChanged(nameof(OpenaiVerified));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void HomeButton_OnClick(object sender, RoutedEventArgs e) {
        Process.Start("explorer.exe", ApplicationData.Current.LocalFolder.Path);
    }

    private async void OpenaiTokenButton_OnClick(object sender, RoutedEventArgs e) {
        var openAiService = new OpenAIService(new OpenAiOptions {
            ApiKey = OpenaiToken.Password
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
            _localSettings.Values["OpenaiToken"] = OpenaiToken.Password;
            OpenaiVerified = true;
            NotificationQueue.Show("Token verified", 2000);
        } catch (Exception ex) {
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Error;
            NotificationQueue.Show($"{ex.GetType().Name}: {ex.Message}", 5000, "Unable to verify token");
            OpenaiVerified = false;
            Debug.Print("Unable to verify token");
            Debug.Print(ex.StackTrace);
        }

        OpenaiTokenButton.IsEnabled = true;
    }

    private void OpenaiToken_OnPasswordChanged(object sender, RoutedEventArgs e) {
        OpenaiVerified = false;
    }
}