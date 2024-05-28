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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatClient.Controls {
    public sealed partial class TokenInput : UserControl {
        public static readonly DependencyProperty TokenProperty =
            DependencyProperty.Register("Token", typeof(string), typeof(TokenInput),
                new PropertyMetadata(string.Empty));

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

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("HeaderProperty", typeof(string), typeof(TokenInput),
                new PropertyMetadata(string.Empty));

        public string Header {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register("PlaceholderProperty", typeof(string), typeof(TokenInput),
                new PropertyMetadata(string.Empty));

        public string Placeholder {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public TokenInput() {
            this.InitializeComponent();
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
