using Windows.Storage;
using Microsoft.UI.Xaml;
using System;
using System.Configuration;
using System.IO;

namespace ChatClient;

public partial class App : Application {
    private Window m_window;

    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App() {
        InitializeComponent();
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string localDir = Path.Join(appData, "ChatClient");
        if (!Directory.Exists(localDir)) {
            Directory.CreateDirectory(localDir);
        }
    }

    /// <summary>
    ///     Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args) {
        m_window = new MainWindow();
        m_window.Activate();
    }
}