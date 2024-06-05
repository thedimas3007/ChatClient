using Windows.Storage;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Management;
using System.Runtime.CompilerServices;
using Windows.UI.Popups;
using Microsoft.Extensions.Logging;
using Serilog;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ChatClient;

public partial class App : Application {
    public Window Window { get; private set;  }
    private readonly string _localDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatClient");
    private readonly string[] _prefixes = { "", "Ki", "Mi", "Gi", "Ti" };
    private readonly string[] _quotes = {
        "Houston, we have a problem.",
        "Your plan is failing. Just admit it.",
        "You're gonna need a bigger boat.",
        "I don't... I don't believe it. That is why you fail.",
        "Hasta la vista, baby."
    };

    static string GetMemoryType(uint type) {
        switch (type) {
            case 20: return "DDR";
            case 21: return "DDR2";
            case 24: return "DDR3";
            case 26: return "DDR4";
            case 29: return "LPDDR";
            case 30: return "LPDDR2";
            case 31: return "LPDDR3";
            case 32: return "LPDDR4";
            default: return "Unknown";
        }
    }

    private string ConvertSize(ulong size) {
        if (size <= 0) return "0B";
        var power = (int) Math.Floor(Math.Log(size, 1024));
        var prefix = _prefixes[power];
        var newSize = Math.Round(size / Math.Pow(1024, power), 2);
        return $"{newSize}{prefix}B";
    }

    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App() {
        InitializeComponent();
        if (!Directory.Exists(_localDir)) {
            Directory.CreateDirectory(_localDir);
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(Path.Join(_localDir, "activity.log"))
            .CreateLogger();

        UnhandledException += (_, args) => {
            var rnd = new Random();
            int quoteId = rnd.Next(_quotes.Length);
            Log.Fatal("=== THE APP HAS JUST CRASHED ===");
            Log.Fatal(args.Exception, "Unhandled exception");
            File.Create(Path.Join(_localDir, "didCrash"));

            string now = DateTime.Now.ToString("s").Replace(":", "-");
            string nowFancy = DateTime.Now.ToString("R");
            var report = new StreamWriter(File.Create(Path.Join(_localDir, $"crash-report-{now}.txt")));
            report.WriteLine("=== THE APP HAS JUST CRASHED ===");
            report.WriteLine($"// {_quotes[quoteId]}");
            report.WriteLine();

            report.WriteLine("-- Info --");
            report.WriteLine($"Time: {nowFancy}");
            report.WriteLine($"Message: {args.Message}");
            report.WriteLine($"Exception: {args.Exception.GetType()}");
            report.WriteLine();

            report.WriteLine("-- Stacktrace  -");
            report.WriteLine(args.Exception.ToString());
            report.WriteLine();

            report.WriteLine("-- Hardware --");
            ManagementObjectSearcher cpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            report.WriteLine("CPU:");
            foreach (var obj in cpuSearcher.Get()) {
                report.WriteLine($"  {obj["Name"]}");
            }

            var ramSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            report.WriteLine("RAM: ");
            foreach (var obj in ramSearcher.Get()) {
                report.WriteLine(
                    $"  {ConvertSize((ulong)obj["Capacity"])} {obj["Speed"]}MHz {GetMemoryType((uint)obj["SMBIOSMemoryType"])}");
            }

            var gpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            report.WriteLine("GPU:");
            foreach (var obj in gpuSearcher.Get()) {
                report.WriteLine($"  {obj["Name"]}");
            }
            report.WriteLine();
            report.Flush();
        };
    }

    /// <summary>
    ///     Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args) {
        Window = new MainWindow();
        Window.Activate();
        Log.Information("App Started!");

        if (File.Exists(Path.Join(_localDir, "didCrash"))) { // Probably create sort of JSON report
            var dialog = new MessageDialog("It would be nice if you sent me latest logs and reports so I would be able to fix the issue.", "Application crashed");
            nint windowHandle = WindowNative.GetWindowHandle(Window);
            InitializeWithWindow.Initialize(dialog, windowHandle);
            dialog.Commands.Add(new UICommand("Close"));
            dialog.ShowAsync();
            File.Delete(Path.Join(_localDir, "didCrash"));
        }
    }
}