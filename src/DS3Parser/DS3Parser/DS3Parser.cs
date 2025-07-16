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

            try
            {
                fogDistribution = _fogDistributionParser.ParseFromGameDirectory(gameDirectory);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DS3Parser] Error parsing fog distribution: {ex.Message}");
                // fogDistribution remains null
            }

            try
            {
                spoilerLog = _spoilerLogParser.ParseFromGameDirectory(gameDirectory);
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
                GameDirectory = gameDirectory
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

            var fogPath = Path.Combine(gameDirectory, "fog", "fogdist", "fog.txt");
            return File.Exists(fogPath);
        }

        /// <summary>
        /// Get the expected paths for fog randomizer files
        /// </summary>
        /// <param name="gameDirectory">Path to the DS3 game directory</param>
        /// <returns>Expected file paths</returns>
        public DS3FogRandomizerPaths GetExpectedPaths(string gameDirectory)
        {
            return new DS3FogRandomizerPaths
            {
                GameDirectory = gameDirectory,
                FogDirectory = Path.Combine(gameDirectory, "fog"),
                FogDistDirectory = Path.Combine(gameDirectory, "fog", "fogdist"),
                FogFile = Path.Combine(gameDirectory, "fog", "fogdist", "fog.txt"),
                LocationsFile = Path.Combine(gameDirectory, "fog", "fogdist", "locations.txt"),
                EventsFile = Path.Combine(gameDirectory, "fog", "fogdist", "events.txt"),
                SpoilerLogDirectory = Path.Combine(gameDirectory, "fog", "spoiler_logs"),
                FogModExecutable = Path.Combine(gameDirectory, "fog", "FogMod.exe")
            };
        }
    }
}
