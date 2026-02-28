using System.IO;
using System.Text.Json;
using DarshanPlayer.Models;

namespace DarshanPlayer.Services
{
    public class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "DarshanPlayer", "settings.json");

        public AppSettings Current { get; private set; } = new();

        public void Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { Current = new AppSettings(); }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        public void AddRecentFile(string path)
        {
            Current.RecentFiles.Remove(path);
            Current.RecentFiles.Insert(0, path);
            if (Current.RecentFiles.Count > 20)
                Current.RecentFiles.RemoveAt(20);
            Save();
        }
    }
}
