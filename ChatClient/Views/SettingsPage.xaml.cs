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

public sealed partial class SettingsPage : Page, INotifyPropertyChanged {
    public double Increment => 0.01;
    private SettingsProvider _settingsProvider;
    private bool _isUpdatingToggles;
    private bool _streamingAvailable;
    private bool _streamingEnabled;
    private bool _functionsAvailable;
    private bool _functionsEnabled;

    public bool StreamingAvailable {
        get => _streamingAvailable;
        set {
            _streamingAvailable = value;
            OnPropertyChanged(nameof(StreamingAvailable));
        }
    }
    public bool StreamingEnabled {
        get => _streamingEnabled;
        set {
            _streamingEnabled = value;
            OnPropertyChanged(nameof(StreamingEnabled));
        }
    }

    public bool FunctionsAvailable {
        get => _functionsAvailable;
        set {
            _functionsAvailable = value;
            OnPropertyChanged(nameof(FunctionsAvailable));
        }
    }

    public bool FunctionsEnabled {
        get => _functionsEnabled;
        set {
            _functionsEnabled = value;
            OnPropertyChanged(nameof(FunctionsEnabled));
        }
    }

    public SettingsPage() {
        InitializeComponent();
    }

    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    protected override void OnNavigatedTo(NavigationEventArgs e) {
        if (e.Parameter != null) {
            _settingsProvider = (SettingsProvider)e.Parameter;
        }
        StreamingAvailable = _settingsProvider.Provider.SupportsStreaming;
        StreamingEnabled = _settingsProvider.Streaming && StreamingAvailable;
        _settingsProvider.Streaming = StreamingEnabled;

        FunctionsAvailable = _settingsProvider.Provider.SupportsFunctions;
        FunctionsEnabled = _settingsProvider.Functions && FunctionsAvailable;
        _settingsProvider.Functions = FunctionsEnabled;

        base.OnNavigatedTo(e);
    }

    private void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void HomeButton_OnClick(object sender, RoutedEventArgs e) {
        Process.Start("explorer.exe", _settingsProvider.LocalDir);
    }

    private void ResetButton_OnClick(object sender, RoutedEventArgs e) {
        _settingsProvider.Temperature = 1f;
        _settingsProvider.TopP = 1f;
        _settingsProvider.FrequencyPenalty = 0f;
        _settingsProvider.PresencePenalty = 0f;
    }

    private void StreamingToggle_OnToggled(object sender, RoutedEventArgs e) {
        _settingsProvider.Streaming = StreamingToggle.IsOn;

        if (_isUpdatingToggles) return;

        _isUpdatingToggles = true;
        FunctionsEnabled = false;
        _isUpdatingToggles = false;
    }

    private void FunctionsToggle_OnToggled(object sender, RoutedEventArgs e) {
        _settingsProvider.Functions = FunctionsToggle.IsOn;
        
        if (_isUpdatingToggles) return;

        _isUpdatingToggles = true;
        StreamingEnabled = false;
        _isUpdatingToggles = false;
    }

    #endregion

    #region Verification

    private async void OpenAiTokenInput_OnTokenVerificationRequested(object sender, string e) {
        TokenInput tokenInput = (TokenInput)sender;
        var openAiService = new OpenAIService(new OpenAiOptions {
            ApiKey = string.IsNullOrEmpty(tokenInput.Token) ? "non-set" : tokenInput.Token,
        });

        try {
            var result = await openAiService.Models.ListModel();
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
            Debug.Print("Unable to verify token");
            Debug.Print(ex.StackTrace);
            _settingsProvider.OpenAiTokenVerified = false;
        }
    }
    
    private async void GoogleSearchTokens_OnTokenVerificationRequested(object sender, string e) {
        try {
            await Tools.GoogleAsync("Test", GoogleSearchIdInput.Token, GoogleSearchTokenInput.Token);
            _settingsProvider.GoogleSearchId = GoogleSearchIdInput.Token;
            _settingsProvider.GoogleSearchToken = GoogleSearchTokenInput.Token;
            _settingsProvider.GoogleSearchVerified = true;
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Success;
            NotificationQueue.Show("Token verified", 2000);
        } catch (Exception ex) {
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Error;
            NotificationQueue.Show($"{ex.GetType().Name}: {ex.Message}", 5000, "Unable to verify token");
            Debug.Print("Unable to verify token");
            Debug.Print(ex.StackTrace);
            _settingsProvider.GoogleSearchVerified = false;
        }
    }
    
    private async void WolframToken_OnTokenVerificationRequested(object sender, string e) {
        try {
            await Tools.AskWolfram("2+2", WolframTokenInput.Token);
            _settingsProvider.WolframToken = WolframTokenInput.Token;
            _settingsProvider.WolframTokenVerified = true;
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Success;
            NotificationQueue.Show("Token verified", 2000);
        } catch (Exception ex) {
            NotificationQueue.AssociatedObject.Severity = InfoBarSeverity.Error;
            NotificationQueue.Show($"{ex.GetType().Name}: {ex.Message}", 5000, "Unable to verify token");
            Debug.Print("Unable to verify token");
            Debug.Print(ex.StackTrace);
            _settingsProvider.WolframTokenVerified = false;
        }
    }
 
    #endregion
}