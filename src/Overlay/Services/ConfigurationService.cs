using System;
using System.IO;
using System.Text.Json;

namespace DS3FogRandoOverlay.Services
{
    public class ConfigurationService
    {
        public class OverlayConfig
        {
            public string FogModPath { get; set; } = @"c:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III\Game\fog";
            public double WindowLeft { get; set; } = -1;
            public double WindowTop { get; set; } = -1;
            public bool AlwaysOnTop { get; set; } = true;
            public double UpdateIntervalMs { get; set; } = 500;
        }

        private readonly string configPath;
        private OverlayConfig config = new OverlayConfig();

        public ConfigurationService()
        {
            configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                     "DS3FogRandoOverlay", "config.json");

            LoadConfig();
        }

        public OverlayConfig Config => config;

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    config = JsonSerializer.Deserialize<OverlayConfig>(json) ?? new OverlayConfig();
                }
                else
                {
                    config = new OverlayConfig();
                    SaveConfig();
                }
            }
            catch
            {
                config = new OverlayConfig();
            }
        }

        public void SaveConfig()
        {
            try
            {
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
