using System;
using System.IO;
using System.Text.Json;
using gamelibrary4.Models;

namespace gamelibrary4.Services
{
    public static class SettingsService
    {
        private static string DataDir
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "gamelibrary4");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string MusicDir
        {
            get
            {
                var dir = Path.Combine(DataDir, "music");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        private static string SettingsFile => Path.Combine(DataDir, "settings.json");

        private static AppSettings _current;

        public static AppSettings Current
        {
            get
            {
                if (_current == null) Load();
                return _current;
            }
        }

        public static void Load()
        {
            if (!File.Exists(SettingsFile))
            {
                _current = new AppSettings();
                return;
            }
            try
            {
                var json = File.ReadAllText(SettingsFile);
                _current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                _current = new AppSettings();
            }
        }

        public static void Save()
        {
            if (_current == null) return;
            try
            {
                var json = JsonSerializer.Serialize(_current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch { /* ignore */ }
        }
    }
}