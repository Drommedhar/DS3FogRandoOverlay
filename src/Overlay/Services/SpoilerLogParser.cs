using DS3FogRandoOverlay.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DS3FogRandoOverlay.Services
{
    public class SpoilerLogParser
    {
        private readonly ConfigurationService configService;

        public SpoilerLogParser()
        {
            configService = new ConfigurationService();
        }

        public SpoilerLogParser(ConfigurationService configService)
        {
            this.configService = configService;
        }

        public SpoilerLogData? ParseLatestSpoilerLog()
        {
            var darkSouls3Path = configService.Config.DarkSouls3Path;
            if (string.IsNullOrEmpty(darkSouls3Path) || !Directory.Exists(darkSouls3Path))
                return null;

            // Use PathResolver to find the fog directory
            var pathResolver = new PathResolver(darkSouls3Path);
            var fogModPath = pathResolver.FindFogDirectory();
            
            if (string.IsNullOrEmpty(fogModPath) || !Directory.Exists(fogModPath))
                return null;

            var spoilerLogsPath = Path.Combine(fogModPath, "spoiler_logs");
            if (!Directory.Exists(spoilerLogsPath))
                return null;

            // Get the latest spoiler log file
            var latestFile = Directory.GetFiles(spoilerLogsPath, "*.txt")
                .OrderByDescending(f => File.GetCreationTime(f))
                .FirstOrDefault();

            if (latestFile == null)
                return null;

            return ParseSpoilerLogFile(latestFile);
        }

        public SpoilerLogData? ParseSpoilerLogFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                var lines = File.ReadAllLines(filePath);
                var data = new SpoilerLogData();

                // Parse seed and options from first line
                var firstLine = lines.FirstOrDefault();
                if (firstLine != null && firstLine.StartsWith("Seed:"))
                {
                    var match = Regex.Match(firstLine, @"Seed: (\d+)\. Options: (.+)");
                    if (match.Success)
                    {
                        data.Seed = match.Groups[1].Value;
                        data.Options = match.Groups[2].Value;
                    }
                }

                Area? currentArea = null;
                bool inMainSection = false;

                foreach (var line in lines)
                {
                    if (line.Contains(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"))
                    {
                        inMainSection = true;
                        continue;
                    }

                    if (!inMainSection)
                        continue;

                    if (line.StartsWith("Writing ") || string.IsNullOrWhiteSpace(line))
                        break;

                    // Parse area header
                    var areaMatch = Regex.Match(line, @"^([^(]+)\s+\(scaling:\s+(\d+)%\)(\s+<----)?");
                    if (areaMatch.Success)
                    {
                        currentArea = new Area
                        {
                            Name = areaMatch.Groups[1].Value.Trim(),
                            ScalingPercent = int.Parse(areaMatch.Groups[2].Value),
                            IsBoss = areaMatch.Groups[3].Success // Has <---- marker
                        };
                        data.Areas.Add(currentArea);
                        continue;
                    }

                    // Parse fog gate connections
                    if (currentArea != null && (line.Trim().StartsWith("Random:") || line.Trim().StartsWith("Preexisting:")))
                    {
                        var connectionMatch = Regex.Match(line,
                            @"(Random|Preexisting):\s+From\s+([^(]+)\s+\([^)]+\)\s+to\s+([^(]+)\s+\(([^)]+)\)");

                        if (connectionMatch.Success)
                        {
                            var fogGate = new FogGate
                            {
                                Name = currentArea.Name,
                                FromArea = connectionMatch.Groups[2].Value.Trim(),
                                ToArea = connectionMatch.Groups[3].Value.Trim(),
                                Description = connectionMatch.Groups[4].Value.Trim(),
                                ScalingPercent = currentArea.ScalingPercent,
                                IsBoss = currentArea.IsBoss,
                                IsRandom = connectionMatch.Groups[1].Value == "Random",
                                IsPreexisting = connectionMatch.Groups[1].Value == "Preexisting"
                            };

                            currentArea.FogGates.Add(fogGate);

                            // Add to connections dictionary for quick lookup
                            var connectionKey = $"{fogGate.FromArea} -> {fogGate.ToArea}";
                            if (!data.FogGateConnections.ContainsKey(connectionKey))
                            {
                                data.FogGateConnections[connectionKey] = fogGate.Description;
                            }
                        }
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing spoiler log: {ex.Message}");
                return null;
            }
        }

        public string? GetFogModPath()
        {
            var darkSouls3Path = configService.Config.DarkSouls3Path;
            if (string.IsNullOrEmpty(darkSouls3Path))
                return null;
                
            var pathResolver = new PathResolver(darkSouls3Path);
            return pathResolver.FindFogDirectory();
        }
    }
}
