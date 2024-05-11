using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace ChatClient.Views {
    public class BoolToFontIconConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            bool isTrue = (bool)value;
            string glyph = isTrue ? "\xE73E" : "\xE711";
            return new FontIcon { Glyph = glyph };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
    public sealed partial class SettingsPage : Page, INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        private ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private bool _openaiVerified = false;

        public bool OpenaiVerified {
            get { return _openaiVerified; }
            set {
                if (_openaiVerified != value) {
                    _openaiVerified = value;
                    _localSettings.Values["OpenaiTokenVerified"] = value;
                    OnPropertyChanged(nameof(OpenaiVerified));
                }
            }
        }

        public SettingsPage() {
            OpenaiVerified = (bool)(_localSettings.Values["OpenaiTokenVerified"] ?? false);
            InitializeComponent();
        }

        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void HomeButton_OnClick(object sender, RoutedEventArgs e) {
            Process.Start("explorer.exe", ApplicationData.Current.LocalFolder.Path);
        }

        private async void OpenaiTokenButton_OnClick(object sender, RoutedEventArgs e) {
            var openAiService = new OpenAIService(new OpenAiOptions() {
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
}
