using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS3Parser.Models;
using DS3Parser.Services;

namespace DS3Parser
{
    /// <summary>
    /// Main service for parsing DS3 fog randomizer data
    /// </summary>
    public class DS3Parser
    {
        private readonly DS3FogDistributionParser _fogDistributionParser;
        private readonly DS3SpoilerLogParser _spoilerLogParser;

        public DS3Parser()
        {
            _fogDistributionParser = new DS3FogDistributionParser();
            _spoilerLogParser = new DS3SpoilerLogParser();
        }

        /// <summary>
        /// Parse all fog randomizer data from the DS3 game directory
        /// </summary>
        /// <param name="gameDirectory">Path to the DS3 game directory</param>
        /// <returns>Combined fog randomizer data</returns>
        public DS3FogRandomizerData ParseFromGameDirectory(string gameDirectory)
        {
            if (!Directory.Exists(gameDirectory))
            {
                throw new DirectoryNotFoundException($"Game directory not found: {gameDirectory}");
            }

            DS3FogDistribution? fogDistribution = null;
            DS3SpoilerLog? spoilerLog = null;

            // First, try to find the fog randomizer directory dynamically
            var fogModDirectory = FindFogRandomizerDirectory(gameDirectory);
            if (string.IsNullOrEmpty(fogModDirectory))
            {
                System.Diagnostics.Debug.WriteLine($"[DS3Parser] No fog randomizer directory found in: {gameDirectory}");
                return new DS3FogRandomizerData
                {
                    FogDistribution = null,
                    SpoilerLog = null,
                    GameDirectory = gameDirectory
                };
            }

            System.Diagnostics.Debug.WriteLine($"[DS3Parser] Found fog randomizer directory: {fogModDirectory}");

            try
            {
                fogDistribution = _fogDistributionParser.ParseFromFogDirectory(fogModDirectory);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DS3Parser] Error parsing fog distribution: {ex.Message}");
                // fogDistribution remains null
            }

            try
            {
                spoilerLog = _spoilerLogParser.ParseFromFogDirectory(fogModDirectory);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DS3Parser] Error parsing spoiler log: {ex.Message}");
                // spoilerLog remains null
            }

            return new DS3FogRandomizerData
            {
                FogDistribution = fogDistribution,
                SpoilerLog = spoilerLog,
                GameDirectory = gameDirectory,
                FogModDirectory = fogModDirectory
            };
        }

        /// <summary>
        /// Parse fog distribution data from a specific fog.txt file
        /// </summary>
        /// <param name="fogFilePath">Path to the fog.txt file</param>
        /// <returns>Parsed fog distribution data</returns>
        public DS3FogDistribution ParseFogDistribution(string fogFilePath)
        {
            return _fogDistributionParser.ParseFogFile(fogFilePath);
        }

        /// <summary>
        /// Parse spoiler log data from a specific file
        /// </summary>
        /// <param name="spoilerLogPath">Path to the spoiler log file</param>
        /// <returns>Parsed spoiler log data</returns>
        public DS3SpoilerLog ParseSpoilerLog(string spoilerLogPath)
        {
            return _spoilerLogParser.ParseSpoilerLogFile(spoilerLogPath);
        }

        /// <summary>
        /// Find all spoiler log files in the game directory
        /// </summary>
        /// <param name="gameDirectory">Path to the DS3 game directory</param>
        /// <returns>List of spoiler log file paths</returns>
        public List<string> FindSpoilerLogFiles(string gameDirectory)
        {
            return _spoilerLogParser.FindSpoilerLogFiles(gameDirectory);
        }

        /// <summary>
        /// Check if the game directory contains fog randomizer data
        /// </summary>
        /// <param name="gameDirectory">Path to the DS3 game directory</param>
        /// <returns>True if fog randomizer data is found</returns>
        public bool HasFogRandomizerData(string gameDirectory)
        {
            if (!Directory.Exists(gameDirectory))
                return false;

            // Try to find fog randomizer data dynamically
            var fogModPath = FindFogRandomizerDirectory(gameDirectory);
            if (string.IsNullOrEmpty(fogModPath))
                return false;

            var fogPath = Path.Combine(fogModPath, "fogdist", "fog.txt");
            return File.Exists(fogPath);
        }

        /// <summary>
        /// Dynamically find the fog randomizer mod directory by searching for required files/folders
        /// </summary>
        /// <param name="baseDirectory">Base directory to search from (usually DS3 game directory)</param>
        /// <returns>Path to fog randomizer directory, or null if not found</returns>
        public string? FindFogRandomizerDirectory(string baseDirectory)
        {
            if (!Directory.Exists(baseDirectory))
                return null;

            return SearchForFogRandomizerDirectory(baseDirectory, maxDepth: 4);
        }

        /// <summary>
        /// Recursively search for fog randomizer directory by looking for key files and folders
        /// </summary>
        private string? SearchForFogRandomizerDirectory(string basePath, int maxDepth, int currentDepth = 0)
        {
            if (currentDepth >= maxDepth || !Directory.Exists(basePath))
                return null;

            try
            {
                // Check if current directory is a fog randomizer directory
                if (IsValidFogRandomizerDirectory(basePath))
                    return basePath;

                // Search subdirectories
                foreach (var subDir in Directory.GetDirectories(basePath))
                {
                    var result = SearchForFogRandomizerDirectory(subDir, maxDepth, currentDepth + 1);
                    if (result != null)
                        return result;
                }
            }
            catch
            {
                // Ignore access denied or other errors when searching
            }

            return null;
        }

        /// <summary>
        /// Check if a directory contains the required fog randomizer files and folders
        /// </summary>
        private bool IsValidFogRandomizerDirectory(string path)
        {
            if (!Directory.Exists(path))
                return false;

            // Check for key files that indicate this is a fog randomizer directory
            var requiredFiles = new[]
            {
                Path.Combine(path, "fogdist", "fog.txt"),
                Path.Combine(path, "fogdist", "locations.txt")
            };

            var requiredDirs = new[]
            {
                Path.Combine(path, "fogdist"),
                Path.Combine(path, "spoiler_logs")
            };

            // At least fog.txt must exist
            bool hasFogFile = File.Exists(requiredFiles[0]);
            if (!hasFogFile)
                return false;

            // At least fogdist directory must exist
            bool hasFogDistDir = Directory.Exists(requiredDirs[0]);
            if (!hasFogDistDir)
                return false;

            // Optional: Check for mapstudio directory which is often used
            var mapStudioPath = Path.Combine(path, "map", "mapstudio");
            bool hasMapStudio = Directory.Exists(mapStudioPath);

            // Return true if we have the essential files and at least some of the expected structure
            return hasFogFile && hasFogDistDir;
        }

        /// <summary>
        /// Get the expected paths for fog randomizer files
        /// </summary>
        /// <param name="gameDirectory">Path to the DS3 game directory</param>
        /// <returns>Expected file paths</returns>
        public DS3FogRandomizerPaths GetExpectedPaths(string gameDirectory)
        {
            var fogModDirectory = FindFogRandomizerDirectory(gameDirectory);
            
            if (string.IsNullOrEmpty(fogModDirectory))
            {
                // Return empty paths if no fog mod directory found
                return new DS3FogRandomizerPaths
                {
                    GameDirectory = gameDirectory,
                    FogDirectory = "",
                    FogDistDirectory = "",
                    FogFile = "",
                    LocationsFile = "",
                    EventsFile = "",
                    SpoilerLogDirectory = "",
                    FogModExecutable = ""
                };
            }

            return new DS3FogRandomizerPaths
            {
                GameDirectory = gameDirectory,
                FogDirectory = fogModDirectory,
                FogDistDirectory = Path.Combine(fogModDirectory, "fogdist"),
                FogFile = Path.Combine(fogModDirectory, "fogdist", "fog.txt"),
                LocationsFile = Path.Combine(fogModDirectory, "fogdist", "locations.txt"),
                EventsFile = Path.Combine(fogModDirectory, "fogdist", "events.txt"),
                SpoilerLogDirectory = Path.Combine(fogModDirectory, "spoiler_logs"),
                FogModExecutable = Path.Combine(fogModDirectory, "FogMod.exe")
            };
        }

        /// <summary>
        /// Get diagnostic information about fog randomizer detection
        /// </summary>
        /// <param name="gameDirectory">Path to the DS3 game directory</param>
        /// <returns>Diagnostic information string</returns>
        public string GetFogRandomizerDiagnostics(string gameDirectory)
        {
            var diagnostics = new List<string>();
            
            diagnostics.Add($"Searching for fog randomizer in: {gameDirectory}");
            
            var fogModDirectory = FindFogRandomizerDirectory(gameDirectory);
            if (string.IsNullOrEmpty(fogModDirectory))
            {
                diagnostics.Add("❌ No fog randomizer directory found");
                diagnostics.Add("Expected to find a directory containing:");
                diagnostics.Add("  - fogdist/fog.txt");
                diagnostics.Add("  - fogdist/locations.txt");
                diagnostics.Add("  - spoiler_logs/ directory");
                return string.Join("\n", diagnostics);
            }
            
            diagnostics.Add($"✅ Found fog mod directory: {fogModDirectory}");
            
            var paths = GetExpectedPaths(gameDirectory);
            diagnostics.Add($"✅ Fog file: {(paths.FogFileExists ? "Found" : "Missing")} - {paths.FogFile}");
            diagnostics.Add($"✅ Locations file: {(File.Exists(paths.LocationsFile) ? "Found" : "Missing")} - {paths.LocationsFile}");
            diagnostics.Add($"✅ Events file: {(File.Exists(paths.EventsFile) ? "Found" : "Missing")} - {paths.EventsFile}");
            diagnostics.Add($"✅ Spoiler logs dir: {(paths.SpoilerLogDirectoryExists ? "Found" : "Missing")} - {paths.SpoilerLogDirectory}");
            diagnostics.Add($"✅ FogMod executable: {(File.Exists(paths.FogModExecutable) ? "Found" : "Missing")} - {paths.FogModExecutable}");
            
            // Check for mapstudio directories
            var mapStudioPaths = new[]
            {
                Path.Combine(fogModDirectory, "map", "mapstudio"),
                Path.Combine(fogModDirectory, "mapstudio"),
                Path.Combine(fogModDirectory, "Maps", "mapstudio"),
                Path.Combine(fogModDirectory, "maps", "mapstudio")
            };
            
            var foundMapStudio = mapStudioPaths.FirstOrDefault(Directory.Exists);
            if (foundMapStudio != null)
            {
                var msbFiles = Directory.GetFiles(foundMapStudio, "*.msb.dcx").Length;
                diagnostics.Add($"✅ MapStudio directory: Found with {msbFiles} MSB files - {foundMapStudio}");
            }
            else
            {
                diagnostics.Add($"⚠️ MapStudio directory: Not found (distance calculations may not work)");
            }
            
            return string.Join("\n", diagnostics);
        }
    }
}
