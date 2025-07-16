using DS3Parser;
using DS3Parser.Models;
using DS3FogRandoOverlay.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DS3FogRandoOverlay.Services
{
    /// <summary>
    /// Service that provides fog gate data using the DS3Parser library
    /// </summary>
    public class FogGateService
    {
        private readonly ConfigurationService configService;
        private readonly DS3Parser.DS3Parser parser;
        private DS3FogDistribution? fogDistribution;
        private DS3SpoilerLog? spoilerLog;
        private DateTime lastUpdateTime = DateTime.MinValue;

        public FogGateService(ConfigurationService configService)
        {
            this.configService = configService;
            this.parser = new DS3Parser.DS3Parser();
        }

        /// <summary>
        /// Gets the current seed information from the spoiler log
        /// </summary>
        public string? GetCurrentSeed()
        {
            RefreshDataIfNeeded();
            return spoilerLog?.Seed.ToString();
        }

        /// <summary>
        /// Gets all fog gates in the specified area
        /// </summary>
        public List<DS3FogGate> GetFogGatesInArea(string areaName)
        {
            RefreshDataIfNeeded();
            
            if (fogDistribution == null)
            {
                System.Diagnostics.Debug.WriteLine($"[FogGateService] GetFogGatesInArea: fogDistribution is null");
                File.AppendAllText("ds3_debug.log", $"[FogGateService] GetFogGatesInArea: fogDistribution is null\n");
                return new List<DS3FogGate>();
            }

            // Map display area name to fog distribution area name
            var mappedAreaName = MapDisplayNameToFogDistributionName(areaName);

            var result = fogDistribution.Entrances
                .Where(fg => string.Equals(fg.Area, mappedAreaName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[FogGateService] GetFogGatesInArea({areaName} -> {mappedAreaName}): found {result.Count} fog gates");

            if (result.Count == 0)
            {
                // Log all available areas for debugging
                var allAreas = fogDistribution.Entrances.Select(fg => fg.Area).Distinct().ToList();
                System.Diagnostics.Debug.WriteLine($"[FogGateService] Available areas: {string.Join(", ", allAreas)}");
            }

            return result;
        }

        /// <summary>
        /// Gets all warps in the specified area
        /// </summary>
        public List<DS3Warp> GetWarpsInArea(string areaName)
        {
            RefreshDataIfNeeded();
            
            if (fogDistribution == null)
                return new List<DS3Warp>();

            // Map display area name to fog distribution area name
            var mappedAreaName = MapDisplayNameToFogDistributionName(areaName);

            return fogDistribution.Warps
                .Where(w => string.Equals(w.Area, mappedAreaName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Gets all connections for fog gates in the specified area
        /// </summary>
        public List<DS3Connection> GetConnectionsInArea(string areaName)
        {
            RefreshDataIfNeeded();
            
            if (spoilerLog == null)
                return new List<DS3Connection>();

            return spoilerLog.Connections
                .Where(c => ExtractAreaFromLocation(c.FromArea) == areaName || ExtractAreaFromLocation(c.ToArea) == areaName)
                .ToList();
        }

        /// <summary>
        /// Gets the connection destination for a specific fog gate
        /// </summary>
        public DS3Connection? GetConnectionForFogGate(string fogGateName)
        {
            RefreshDataIfNeeded();
            
            if (spoilerLog == null)
                return null;

            return spoilerLog.Connections
                .FirstOrDefault(c => c.FromArea.Contains(fogGateName) || c.ToArea.Contains(fogGateName));
        }

        /// <summary>
        /// Gets the connection destination for a specific fog gate by area and ID
        /// </summary>
        public DS3Connection? GetConnectionForFogGate(string areaName, int fogGateId)
        {
            var fogGateName = $"{areaName}_{fogGateId}";
            return GetConnectionForFogGate(fogGateName);
        }

        /// <summary>
        /// Gets fog gate information by area and ID
        /// </summary>
        public DS3FogGate? GetFogGate(string areaName, int fogGateId)
        {
            RefreshDataIfNeeded();
            
            if (fogDistribution == null)
                return null;

            return fogDistribution.Entrances
                .FirstOrDefault(fg => string.Equals(fg.Area, areaName, StringComparison.OrdinalIgnoreCase) && fg.Id == fogGateId);
        }

        /// <summary>
        /// Gets warp information by area and ID
        /// </summary>
        public DS3Warp? GetWarp(string areaName, int warpId)
        {
            RefreshDataIfNeeded();
            
            if (fogDistribution == null)
                return null;

            return fogDistribution.Warps
                .FirstOrDefault(w => string.Equals(w.Area, areaName, StringComparison.OrdinalIgnoreCase) && w.Id == warpId);
        }

        /// <summary>
        /// Gets all areas that have fog gates
        /// </summary>
        public List<string> GetAllAreas()
        {
            RefreshDataIfNeeded();
            
            if (fogDistribution == null)
                return new List<string>();

            var areas = new HashSet<string>();
            
            foreach (var fogGate in fogDistribution.Entrances)
            {
                areas.Add(fogGate.Area);
            }
            
            foreach (var warp in fogDistribution.Warps)
            {
                areas.Add(warp.Area);
            }

            return areas.ToList();
        }

        /// <summary>
        /// Checks if fog randomizer data is available
        /// </summary>
        public bool HasFogRandomizerData()
        {
            RefreshDataIfNeeded();
            var hasData = fogDistribution != null || spoilerLog != null;
            System.Diagnostics.Debug.WriteLine($"[FogGateService] HasFogRandomizerData: {hasData} (fogDistribution: {fogDistribution != null}, spoilerLog: {spoilerLog != null})");
            return hasData;
        }

        /// <summary>
        /// Forces a refresh of the fog gate data
        /// </summary>
        public void RefreshData()
        {
            lastUpdateTime = DateTime.MinValue;
            RefreshDataIfNeeded();
        }

        private void RefreshDataIfNeeded()
        {
            var now = DateTime.Now;
            
            // Only refresh every 30 seconds unless forced
            if (now - lastUpdateTime < TimeSpan.FromSeconds(30) && fogDistribution != null && spoilerLog != null)
            {
                System.Diagnostics.Debug.WriteLine($"[FogGateService] Throttling refresh - last update: {lastUpdateTime}, now: {now}, diff: {(now - lastUpdateTime).TotalSeconds}s");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[FogGateService] Refreshing data - last update: {lastUpdateTime}, now: {now}");

            try
            {
                var gameDirectory = configService.Config.DarkSouls3Path;
                if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory))
                {
                    System.Diagnostics.Debug.WriteLine($"[FogGateService] Game directory not found: {gameDirectory}");
                    fogDistribution = null;
                    spoilerLog = null;
                    return;
                }

                // Ensure we're using the Game subdirectory
                if (!gameDirectory.EndsWith("Game", StringComparison.OrdinalIgnoreCase))
                {
                    gameDirectory = Path.Combine(gameDirectory, "Game");
                }

                if (!Directory.Exists(gameDirectory))
                {
                    System.Diagnostics.Debug.WriteLine($"[FogGateService] Game subdirectory not found: {gameDirectory}");
                    fogDistribution = null;
                    spoilerLog = null;
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[FogGateService] Trying to parse from game directory: {gameDirectory}");
                File.AppendAllText("ds3_debug.log", $"[FogGateService] Trying to parse from game directory: {gameDirectory}\n");
                var result = parser.ParseFromGameDirectory(gameDirectory);
                if (result != null)
                {
                    fogDistribution = result.FogDistribution;
                    spoilerLog = result.SpoilerLog;
                    System.Diagnostics.Debug.WriteLine($"[FogGateService] Successfully parsed data - FogDistribution: {fogDistribution != null}, SpoilerLog: {spoilerLog != null}");
                    File.AppendAllText("ds3_debug.log", $"[FogGateService] Successfully parsed data - FogDistribution: {fogDistribution != null}, SpoilerLog: {spoilerLog != null}\n");
                    
                    if (fogDistribution != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FogGateService] Found {fogDistribution.Entrances.Count} entrances, {fogDistribution.Warps.Count} warps");
                        File.AppendAllText("ds3_debug.log", $"[FogGateService] Found {fogDistribution.Entrances.Count} entrances, {fogDistribution.Warps.Count} warps\n");
                    }
                    
                    if (spoilerLog != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FogGateService] Found {spoilerLog.Connections.Count} connections, Seed: {spoilerLog.Seed}");
                        File.AppendAllText("ds3_debug.log", $"[FogGateService] Found {spoilerLog.Connections.Count} connections, Seed: {spoilerLog.Seed}\n");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[FogGateService] Parser returned null result");
                    File.AppendAllText("ds3_debug.log", $"[FogGateService] Parser returned null result\n");
                    fogDistribution = null;
                    spoilerLog = null;
                }

                lastUpdateTime = now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FogGateService] Error during refresh: {ex.Message}");
                File.AppendAllText("ds3_debug.log", $"[FogGateService] Error during refresh: {ex.Message}\n");
                // If parsing fails, keep the existing data
                // This prevents the overlay from breaking if there's a temporary parsing issue
            }
        }

        private static string ExtractAreaFromLocation(string location)
        {
            // Extract area name from location strings like "m30_00_00_00 (0)"
            var match = System.Text.RegularExpressions.Regex.Match(location, @"^([^(]+)");
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
            return location;
        }

        /// <summary>
        /// Maps display area names to fog distribution area names
        /// </summary>
        private string MapDisplayNameToFogDistributionName(string displayName)
        {
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "High Wall of Lothric", "highwall" },
                { "Lothric Castle", "lothric" },
                { "Undead Settlement", "settlement" },
                { "Archdragon Peak", "archdragon" },
                { "Road of Sacrifices", "farronkeep" },
                { "Farron Keep", "farronkeep" },
                { "Grand Archives", "archives" },
                { "Cathedral of the Deep", "cathedral" },
                { "Irithyll of the Boreal Valley", "irithyll" },
                { "Anor Londo", "irithyll" },
                { "Catacombs of Carthus", "catacombs" },
                { "Smouldering Lake", "catacombs" },
                { "Irithyll Dungeon", "dungeon" },
                { "Profaned Capital", "dungeon" },
                { "Firelink Shrine", "firelink" },
                { "Untended Graves", "untended" },
                { "Kiln of the First Flame", "kiln" },
                { "Painted World of Ariandel", "ariandel" },
                { "The Dreg Heap", "dregheap" },
                { "The Ringed City", "ringedcity" },
                { "Filianore's Rest", "filianore" }
            };

            return mapping.TryGetValue(displayName, out var fogDistName) ? fogDistName : displayName.ToLower();
        }
    }
}
