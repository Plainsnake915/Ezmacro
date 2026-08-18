using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SharpHook.Data;

namespace Ezmacro
{
    public class MacroSettings
    {
        private static string SettingsFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Ezmacro",
        "settings.json"
    );
        public bool AutoMinimize { get; set; } = true;
        public int PlaybackSpeedMultiplier { get; set; } = 1; // 1x speed by default
        public KeyCode RecordStopKey { get; set; } = KeyCode.VcF10;
        public KeyCode PlaybackKey { get; set; } = KeyCode.VcF11;
        public bool ContinuosPlayback { get; set; } = false;
        public bool MousetrackingEnabled { get; set; } = true;
        public bool HideMouseMoves { get; set; } = false;
        public long Samplingrate { get; set; } = 50; // Default sampling rate for mouse tracking
        public bool HideGlow {  get; set; } = false;

        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save settings: {ex.Message}");
            }
        }
        public static MacroSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var loadedSettings = JsonSerializer.Deserialize<MacroSettings>(json);
                    if (loadedSettings != null)
                    {
                        return loadedSettings;
                    }
                }
            }
            catch
            {
                // Fall back to default settings if file reading fails
            }

            return new MacroSettings();
        }
    }
}
