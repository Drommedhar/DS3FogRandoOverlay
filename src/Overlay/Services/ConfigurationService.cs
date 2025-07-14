using System;
using System.IO;
using System.Text.Json;

namespace DS3FogRandoOverlay.Services
{
    public class ConfigurationService
    {
        public class OverlayConfig
        {
            public string DarkSouls3Path { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III";
            public double WindowLeft { get; set; } = -1;
            public double WindowTop { get; set; } = -1;
            public double WindowWidth { get; set; } = 320;
            public double WindowHeight { get; set; } = 400;
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
                    var loadedConfig = JsonSerializer.Deserialize<OverlayConfig>(json);
                    
                    if (loadedConfig != null)
                    {
                        config = loadedConfig;
                        
                        // Handle migration from old FogModPath to new DarkSouls3Path
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
                        if (jsonElement.TryGetProperty("FogModPath", out var fogModPathElement))
                        {
                            var fogModPath = fogModPathElement.GetString();
                            if (!string.IsNullOrEmpty(fogModPath))
                            {
                                // Extract Dark Souls 3 base path from old fog mod path
                                var gameIndex = fogModPath.IndexOf(@"\Game\fog", StringComparison.OrdinalIgnoreCase);
                                if (gameIndex > 0)
                                {
                                    config.DarkSouls3Path = fogModPath.Substring(0, gameIndex);
                                }
                                else
                                {
                                    // Try to go up from fog directory to find DS3 base
                                    var fogIndex = fogModPath.LastIndexOf(@"\fog", StringComparison.OrdinalIgnoreCase);
                                    if (fogIndex > 0)
                                    {
                                        var gameDir = Path.GetDirectoryName(fogModPath.Substring(0, fogIndex));
                                        if (!string.IsNullOrEmpty(gameDir) && gameDir.EndsWith("DARK SOULS III", StringComparison.OrdinalIgnoreCase))
                                        {
                                            config.DarkSouls3Path = gameDir;
                                        }
                                    }
                                }
                                
                                // Save the migrated config
                                SaveConfig();
                            }
                        }
                    }
                }
                else
                {
                    config = new OverlayConfig();
                    
                    // Try to auto-detect Dark Souls 3 path
                    var autoDetectedPath = PathResolver.AutoDetectDarkSouls3Path();
                    if (!string.IsNullOrEmpty(autoDetectedPath))
                    {
                        config.DarkSouls3Path = autoDetectedPath;
                    }
                    
                    SaveConfig();
                }
            }
            catch
            {
                config = new OverlayConfig();
                
                // Try to auto-detect even on error
                var autoDetectedPath = PathResolver.AutoDetectDarkSouls3Path();
                if (!string.IsNullOrEmpty(autoDetectedPath))
                {
                    config.DarkSouls3Path = autoDetectedPath;
                }
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
