using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DS3Parser.Models;

namespace DS3Parser.Services
{
    /// <summary>
    /// Service for parsing spoiler log files
    /// </summary>
    public class DS3SpoilerLogParser
    {
        private static readonly Regex SeedRegex = new Regex(@"Seed: (\d+)\. Options: (.+)", RegexOptions.Compiled);
        private static readonly Regex KeyItemRegex = new Regex(@"Key item hash: (.+)", RegexOptions.Compiled);
        private static readonly Regex AreaHeaderRegex = new Regex(@"^(.+) \(scaling: (\d+)%\)(?: <----)?$", RegexOptions.Compiled);
        private static readonly Regex ConnectionRegex = new Regex(@"^\s\s(Random|Preexisting): (.+)$", RegexOptions.Compiled);
        private static readonly Regex RequiredAreasRegex = new Regex(@"Areas required before (.+): (.+)", RegexOptions.Compiled);

        /// <summary>
        /// Parse a spoiler log file
        /// </summary>
        /// <param name="filePath">Path to the spoiler log file</param>
        /// <returns>Parsed spoiler log data</returns>
        public DS3SpoilerLog ParseSpoilerLogFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Spoiler log file not found: {filePath}");
            }

            var lines = File.ReadAllLines(filePath);
            return ParseSpoilerLogLines(lines);
        }

        /// <summary>
        /// Parse spoiler log from the DS3 game directory
        /// </summary>
        /// <param name="gameDirectory">Path to the DS3 game directory</param>
        /// <returns>Parsed spoiler log data, or null if no spoiler log found</returns>
        public DS3SpoilerLog? ParseFromGameDirectory(string gameDirectory)
        {
            var spoilerLogDir = Path.Combine(gameDirectory, "fog", "spoiler_logs");
            if (!Directory.Exists(spoilerLogDir))
            {
                return null;
            }

            var logFiles = Directory.GetFiles(spoilerLogDir, "*.txt")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            if (logFiles.Count == 0)
            {
                return null;
            }

            // Get the most recent log file
            return ParseSpoilerLogFile(logFiles.First());
        }

        /// <summary>
        /// Find all spoiler log files in the game directory
        /// </summary>
        /// <param name="gameDirectory">Path to the DS3 game directory</param>
        /// <returns>List of spoiler log file paths</returns>
        public List<string> FindSpoilerLogFiles(string gameDirectory)
        {
            var spoilerLogDir = Path.Combine(gameDirectory, "fog", "spoiler_logs");
            if (!Directory.Exists(spoilerLogDir))
            {
                return new List<string>();
            }

            return Directory.GetFiles(spoilerLogDir, "*.txt")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();
        }

        private DS3SpoilerLog ParseSpoilerLogLines(string[] lines)
        {
            var spoilerLog = new DS3SpoilerLog();
            DS3SpoilerLogEntry? currentEntry = null;
            bool inRequiredAreas = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // Stop processing if we hit the "Finished" line which indicates end of spoiler log
                if (line.StartsWith("Finished"))
                    break;
                
                if (string.IsNullOrEmpty(line.Trim()))
                    continue;

                // Parse seed and options
                var seedMatch = SeedRegex.Match(line);
                if (seedMatch.Success)
                {
                    spoilerLog.Seed = long.Parse(seedMatch.Groups[1].Value);
                    spoilerLog.Options = seedMatch.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                    continue;
                }

                // Parse key item info
                if (line.Contains("No key items randomized"))
                {
                    spoilerLog.KeyItemsRandomized = false;
                    continue;
                }

                var keyItemMatch = KeyItemRegex.Match(line);
                if (keyItemMatch.Success)
                {
                    spoilerLog.KeyItemsRandomized = true;
                    spoilerLog.KeyItemHash = keyItemMatch.Groups[1].Value;
                    continue;
                }

                // Parse required areas
                var requiredMatch = RequiredAreasRegex.Match(line);
                if (requiredMatch.Success)
                {
                    var requiredAreas = requiredMatch.Groups[2].Value.Split(';')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    spoilerLog.RequiredAreas = requiredAreas;
                    inRequiredAreas = true;
                    continue;
                }

                // Check for end of required areas section
                if (inRequiredAreas && line.StartsWith("Other areas"))
                {
                    inRequiredAreas = false;
                    continue;
                }

                // Check for section separator
                if (line.StartsWith(">>>>>>>"))
                {
                    continue;
                }

                // Parse area headers
                var areaMatch = AreaHeaderRegex.Match(line);
                if (areaMatch.Success)
                {
                    currentEntry = new DS3SpoilerLogEntry
                    {
                        AreaName = areaMatch.Groups[1].Value,
                        ScalingPercentage = float.Parse(areaMatch.Groups[2].Value),
                        IsBoss = line.Contains("<----")
                    };
                    spoilerLog.Entries.Add(currentEntry);
                    continue;
                }

                // Parse connections
                var connectionMatch = ConnectionRegex.Match(line);
                if (connectionMatch.Success && currentEntry != null)
                {
                    var isRandom = connectionMatch.Groups[1].Value == "Random";
                    var connectionText = connectionMatch.Groups[2].Value;
                    
                    var connection = ParseConnectionText(connectionText, currentEntry.AreaName, isRandom);
                    currentEntry.Connections.Add(connection);
                    
                    // Also add to the main connections list
                    spoilerLog.Connections.Add(new DS3Connection
                    {
                        FromArea = connection.FromArea,
                        ToArea = connection.ToArea,
                        Description = connection.Description,
                        IsRandom = connection.IsRandom,
                        ScalingPercentage = currentEntry.ScalingPercentage,
                        IsBoss = currentEntry.IsBoss
                    });
                }
            }

            return spoilerLog;
        }

private DS3SpoilerLogConnection ParseConnectionText(string connectionText, string currentArea, bool isRandom)
        {
            var connection = new DS3SpoilerLogConnection
            {
                IsRandom = isRandom,
                FullText = connectionText
            };

            // Try format: "From X (details) to Y (details)"
            var fromToMatch = Regex.Match(connectionText, @"From (.+?) \((.+?)\) to (.+?) \((.+?)\)");
            if (fromToMatch.Success)
            {
                connection.FromArea = fromToMatch.Groups[1].Value;
                connection.ToArea = fromToMatch.Groups[3].Value;
                connection.Description = $"{fromToMatch.Groups[2].Value} -> {fromToMatch.Groups[4].Value}";
                return connection;
            }

            // Try format: "From X to Y (details)"
            var fromToSimpleMatch = Regex.Match(connectionText, @"From (.+?) to (.+?) \((.+?)\)");
            if (fromToSimpleMatch.Success)
            {
                connection.FromArea = fromToSimpleMatch.Groups[1].Value;
                connection.ToArea = fromToSimpleMatch.Groups[2].Value;
                connection.Description = fromToSimpleMatch.Groups[3].Value;
                return connection;
            }

            // Try format: "From X to Y"
            var fromToBasicMatch = Regex.Match(connectionText, @"From (.+?) to (.+)");
            if (fromToBasicMatch.Success)
            {
                connection.FromArea = fromToBasicMatch.Groups[1].Value;
                connection.ToArea = fromToBasicMatch.Groups[2].Value;
                connection.Description = connectionText;
                return connection;
            }

            // Fallback parsing for other formats
            connection.FromArea = currentArea;
            connection.ToArea = "Unknown";
            connection.Description = connectionText;
            return connection;
        }
    }
}
