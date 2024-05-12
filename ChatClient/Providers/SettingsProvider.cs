using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ABI.Windows.System.Threading;

namespace ChatClient.Providers {
    internal class SettingsProvider : INotifyPropertyChanged {
        private readonly string _localDir;
        private readonly string _configPath;
        private readonly string _filename;
        private Dictionary<string, JsonValue> _settings;

        public string LocalDir {
            get => _localDir;
        }

        public string ConfigPath {
            get => _configPath;
        }

        public string Filename {
            get => _filename;
        }

        public string OpenAiToken {
            get => GetProperty<string>("OpenAI-Token");
            set {
                SetProperty("OpenAI-Token", value);
                OnPropertyChanged(nameof(OpenAiToken));
            }
        }

        public bool OpenAiTokenVerified {
            get => GetProperty<bool>("OpenAI-Token-Verified");
            set {
                SetProperty("OpenAI-Token-Verified", value);
                OnPropertyChanged(nameof(OpenAiTokenVerified));
            }
        }

        public SettingsProvider(string filename = "appdata.json") {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _localDir = Path.Combine(appData, "ChatClient");
            _configPath = Path.Combine(_localDir, filename);
            _filename = filename;

            if (!File.Exists(_configPath)) {
                File.WriteAllText(_configPath, "{}");
            }

            try {
                string content = File.ReadAllText(ConfigPath);
                _settings = JsonSerializer.Deserialize<Dictionary<string, JsonValue>>(content);
            } catch {
                File.WriteAllText(_configPath, "{}");
                Debug.Print("Invalid JSON");
                _settings = new();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Save() {
            string content = JsonSerializer.Serialize(_settings);
            File.WriteAllText(ConfigPath, content);
        }

        private void Refresh() {
            string content = File.ReadAllText(ConfigPath);
            _settings = JsonSerializer.Deserialize<Dictionary<string, JsonValue>>(content);
        }

        private T GetProperty<T>([NotNull] string key) {
            // Probably should refresh, but it may take some time, especially on slow drives
            _settings.TryGetValue(key, out JsonValue value);
            if (value == null || !value.TryGetValue(out T output)) {
                return default;
            }

            return output;
        }

        private void SetProperty(string key, object value) {
            _settings[key] = JsonValue.Create(value);
            Save();
        }
    }
}
