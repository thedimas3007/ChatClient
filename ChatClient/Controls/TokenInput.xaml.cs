using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

namespace ChatClient.Controls {
    public sealed partial class TokenInput : SettingsCard {
        public static readonly DependencyProperty TokenProperty =
            DependencyProperty.Register("Token", typeof(string), typeof(TokenInput), new PropertyMetadata(string.Empty));

        public string Token {
            get => (string)GetValue(TokenProperty) ?? "non-set";
            set => SetValue(TokenProperty, value);
        }

        public static readonly DependencyProperty TokenVerifiedProperty =
            DependencyProperty.Register("TokenVerified", typeof(bool), typeof(TokenInput), new PropertyMetadata(false));

        public bool TokenVerified {
            get => (bool)GetValue(TokenVerifiedProperty);
            set => SetValue(TokenVerifiedProperty, value);
        }

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register("Placeholder", typeof(string), typeof(TokenInput), new PropertyMetadata(string.Empty));

        public string Placeholder {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public TokenInput() {
            InitializeComponent();
        }

        public event EventHandler<string> TokenVerificationRequested;

        private void TokenPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            Token = TokenPasswordBox.Password;
            TokenVerified = false;
        }

        private void TokenButton_Click(object sender, RoutedEventArgs e) {
            IsEnabled = false;
            TokenVerificationRequested?.Invoke(this, Token);
            IsEnabled = true;
        }
    }
}