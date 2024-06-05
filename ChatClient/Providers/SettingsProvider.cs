using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatClient.Generation;
using Serilog;

namespace ChatClient.Providers {
    internal class SettingsProvider : INotifyPropertyChanged {
        private readonly string _localDir;
        private readonly string _configPath;
        private readonly string _filename;
        private Dictionary<string, JsonValue> _settings;

        #region Files

        public string LocalDir => _localDir;

        public string ConfigPath => _configPath;

        public string Filename => _filename;

        #endregion

        #region Tokens

        public string OpenAiToken {
            get => GetProperty<string>("OpenAI-Token");
            set => SetPropertyWithCheck("OpenAI-Token", value);
        }

        public bool OpenAiTokenVerified {
            get => GetProperty<bool>("OpenAI-Token-Verified");
            set => SetPropertyWithCheck("OpenAI-Token-Verified", value);
        }

        public string GoogleSearchId {
            get => GetProperty<string>("Google-Search-ID");
            set => SetPropertyWithCheck("Google-Search-ID", value);
        }

        public string GoogleSearchToken {
            get => GetProperty<string>("Google-Search-Token");
            set => SetPropertyWithCheck("Google-Search-Token", value);
        }

        public bool GoogleSearchVerified {
            get => GetProperty<bool>("Google-Search-Verified");
            set => SetPropertyWithCheck("Google-Search-Verified", value);
        }

        public string WolframToken {
            get => GetProperty<string>("Wolfram-Token");
            set => SetPropertyWithCheck("Wolfram-Token", value);
        }

        public bool WolframTokenVerified {
            get => GetProperty<bool>("Wolfram-Token-Verified");
            set => SetPropertyWithCheck("Wolfram-Token-Verified", value);
        }

        #endregion

        #region Generation Settings

        public bool Streaming {
            get => GetProperty<bool>("Streaming", true);
            set => SetPropertyWithCheck("Streaming", value);
        }

        public bool Functions {
            get => GetProperty<bool>("Functions");
            set => SetPropertyWithCheck("Functions", value);
        }

        public GenerationProvider Provider {
            get {
                string name = GetProperty<string>("Provider");
                if (string.IsNullOrEmpty(name)) {
                    return GenerationProvider.Providers.FirstOrDefault();
                }

                var provider = GenerationProvider.Providers.FirstOrDefault(p => p.Name == name);
                return provider ?? GenerationProvider.Providers.FirstOrDefault();
            }
            set => SetPropertyWithCheck("Provider", value.Name);
        }

        public Model Model {
            get {
                string id = GetProperty<string>("Model");
                if (string.IsNullOrEmpty(id)) {
                    return Provider.Models.FirstOrDefault();
                }

                var model = Provider.Models.FirstOrDefault(m => m.Id == id);
                return model ?? Provider.Models.FirstOrDefault();
            }
            set => SetPropertyWithCheck("Model", value.Id);
        }

        #endregion

        #region Model Parameters

        public float Temperature {
            get => GetProperty<float?>("Model-Temperature") ?? 1f;
            set => SetPropertyWithCheck("Model-Temperature", value);
        }

        public float TopP {
            get => GetProperty<float?>("Model-TopP") ?? 1f;
            set => SetPropertyWithCheck("Model-TopP", value);
        }

        public float FrequencyPenalty {
            get => GetProperty<float?>("Model-FrequencyPenalty") ?? 0f;
            set => SetPropertyWithCheck("Model-FrequencyPenalty", value);
        }

        public float PresencePenalty {
            get => GetProperty<float?>("Model-PresencePenalty") ?? 0f;
            set => SetPropertyWithCheck("Model-PresencePenalty", value);
        }

        #endregion

        #region Enabled Functions

        public bool GoogleEnabled {
            get => GetProperty<bool>("Google-Enabled", true);
            set => SetPropertyWithCheck("Google-Enabled", value);
        }

        public bool AskWebEnabled {
            get => GetProperty<bool>("AskWeb-Enabled", true);
            set => SetPropertyWithCheck("AskWeb-Enabled", value);
        }

        public bool WolframEnabled {
            get => GetProperty<bool>("Wolfram-Enabled", true);
            set => SetPropertyWithCheck("Wolfram-Enabled", value);
        }

        #endregion

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
            } catch (Exception ex) {
                File.WriteAllText(_configPath, "{}");
                Log.Warning(ex, "Unable to parse JSON");
                _settings = new();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Save() {
            string content = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, content);
        }

        private void Refresh() {
            string content = File.ReadAllText(ConfigPath);
            _settings = JsonSerializer.Deserialize<Dictionary<string, JsonValue>>(content);
        }

        private T GetProperty<T>([NotNull] string key, T def = default) {
            _settings.TryGetValue(key, out JsonValue value);
            if (value == null || !value.TryGetValue(out T output)) {
                return def;
            }

            return output;
        }

        private void SetProperty(string key, object value) {
            _settings[key] = JsonValue.Create(value);
            Save();
        }

        private void SetPropertyWithCheck<T>(string key, T value) {
            if (!EqualityComparer<T>.Default.Equals(GetProperty<T>(key), value)) {
                SetProperty(key, value);
                OnPropertyChanged(key);
            }
        }
    }
}
