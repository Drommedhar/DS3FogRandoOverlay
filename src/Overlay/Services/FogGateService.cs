using DS3Parser;
using DS3Parser.Models;
using DS3Parser.Services;
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
        private readonly DS3Combiner combiner;
        private DS3FogDistribution? fogDistribution;
        private DS3SpoilerLog? spoilerLog;
        private bool dataLoaded = false;
        private bool combinedDataInitialized = false;

        public FogGateService(ConfigurationService configService)
        {
            this.configService = configService;
            this.parser = new DS3Parser.DS3Parser();
            this.combiner = new DS3Combiner();
        }

        /// <summary>
        /// Gets the current seed information from the spoiler log
        /// </summary>
        public string? GetCurrentSeed()
        {
            LoadDataIfNeeded();
            return spoilerLog?.Seed.ToString();
        }

        /// <summary>
        /// Gets all fog gates in the specified area by map ID
        /// </summary>
        public List<DS3FogGate> GetFogGatesInArea(string mapId)
        {
            LoadDataIfNeeded();
            
            if (fogDistribution == null)
            {
                System.Diagnostics.Debug.WriteLine($"[FogGateService] GetFogGatesInArea: fogDistribution is null");
                File.AppendAllText("ds3_debug.log", $"[FogGateService] GetFogGatesInArea: fogDistribution is null\n");
                return new List<DS3FogGate>();
            }

            // Get all fog gates that match the map ID or map variations
            var result = fogDistribution.Entrances
                .Where(fg => {
                    var fgAreaId = DS3Area.GetAreaId(fg.Area);
                    return fgAreaId == mapId || AreMapVariations(fgAreaId, mapId);
                })
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[FogGateService] GetFogGatesInArea(mapId: {mapId}): found {result.Count} fog gates");
            File.AppendAllText("ds3_debug.log", $"[FogGateService] GetFogGatesInArea(mapId: {mapId}): found {result.Count} fog gates\n");

            if (result.Count == 0)
            {
                // Log all available areas for debugging
                var allAreas = fogDistribution.Entrances.Select(fg => fg.Area).Distinct().ToList();
                System.Diagnostics.Debug.WriteLine($"[FogGateService] Available areas: {string.Join(", ", allAreas)}");
                File.AppendAllText("ds3_debug.log", $"[FogGateService] Available areas: {string.Join(", ", allAreas)}\n");
                
                // Also log the mapped area IDs
                var mappedAreaIds = allAreas.Select(area => $"{area} -> {DS3Area.GetAreaId(area)}").ToList();
                File.AppendAllText("ds3_debug.log", $"[FogGateService] Mapped area IDs: {string.Join(", ", mappedAreaIds)}\n");
            }

            return result;
        }

        /// <summary>
        /// Gets all warps in the specified area by map ID
        /// </summary>
        public List<DS3Warp> GetWarpsInArea(string mapId)
        {
            LoadDataIfNeeded();
            
            if (fogDistribution == null)
                return new List<DS3Warp>();

            return fogDistribution.Warps
                .Where(w => {
                    var warpAreaId = DS3Area.GetAreaId(w.Area);
                    return warpAreaId == mapId || AreMapVariations(warpAreaId, mapId);
                })
                .ToList();
        }

        /// <summary>
        /// Gets all connections for fog gates in the specified area
        /// </summary>
        public List<DS3Connection> GetConnectionsInArea(string areaName)
        {
            LoadDataIfNeeded();
            
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
            LoadDataIfNeeded();
            
            if (spoilerLog == null)
                return null;

            return spoilerLog.Connections
                .FirstOrDefault(c => c.FromArea.Contains(fogGateName) || c.ToArea.Contains(fogGateName));
        }

        /// <summary>
        /// Gets the connection destination for a specific fog gate by map ID and gate ID
        /// </summary>
        public DS3Connection? GetConnectionForFogGate(string mapId, int fogGateId)
        {
            LoadDataIfNeeded();
            
            File.AppendAllText("ds3_debug.log", $"[FogGateService] GetConnectionForFogGate called with mapId: {mapId}, fogGateId: {fogGateId}\n");
            
            if (fogDistribution == null || spoilerLog == null)
            {
                File.AppendAllText("ds3_debug.log", $"[FogGateService] No data available - fogDistribution: {fogDistribution != null}, spoilerLog: {spoilerLog != null}\n");
                return null;
            }

            // Find the fog gate in the distribution data by map ID
            var fogGate = fogDistribution.Entrances
                .FirstOrDefault(fg => {
                    var fgAreaId = DS3Area.GetAreaId(fg.Area);
                    return (fgAreaId == mapId || AreMapVariations(fgAreaId, mapId)) && fg.Id == fogGateId;
                });

            if (fogGate == null)
            {
                File.AppendAllText("ds3_debug.log", $"[FogGateService] No fog gate found for mapId '{mapId}' with ID {fogGateId}\n");
                return null;
            }

            File.AppendAllText("ds3_debug.log", $"[FogGateService] Found fog gate: {fogGate.Name} with text: {fogGate.Text}\n");

            // Find the connection in the spoiler log that matches this fog gate
            File.AppendAllText("ds3_debug.log", $"[FogGateService] Looking for connection with fog gate name: {fogGate.Name}\n");
            File.AppendAllText("ds3_debug.log", $"[FogGateService] Fog gate text: {fogGate.Text}\n");
            
            // Try different matching approaches
            var connection = spoilerLog.Connections.FirstOrDefault(c => 
                c.FromArea.Contains(fogGate.Name) || 
                c.FromArea.Contains(fogGate.Text) ||
                c.FromArea.Contains($"{fogGate.Area}_{fogGateId}") ||
                c.ToArea.Contains(fogGate.Name) ||
                c.ToArea.Contains(fogGate.Text) ||
                c.ToArea.Contains($"{fogGate.Area}_{fogGateId}"));
            
            // If no exact match, try matching by text fragments
            if (connection == null)
            {
                // Try matching by keywords from the fog gate text
                if (fogGate.Text.Contains("Emma"))
                {
                    connection = spoilerLog.Connections.FirstOrDefault(c => 
                        c.FromArea.Contains("Emma") || c.ToArea.Contains("Emma"));
                }
                else if (fogGate.Text.Contains("Vordt"))
                {
                    connection = spoilerLog.Connections.FirstOrDefault(c => 
                        c.FromArea.Contains("Vordt") || c.ToArea.Contains("Vordt"));
                }
                else if (fogGate.Text.Contains("Oceiros"))
                {
                    connection = spoilerLog.Connections.FirstOrDefault(c => 
                        c.FromArea.Contains("Oceiros") || c.ToArea.Contains("Oceiros"));
                }
                else if (fogGate.Text.Contains("Consumed"))
                {
                    connection = spoilerLog.Connections.FirstOrDefault(c => 
                        c.FromArea.Contains("Consumed") || c.ToArea.Contains("Consumed"));
                }
                
                File.AppendAllText("ds3_debug.log", $"[FogGateService] Keyword matching result: {connection?.FromArea} -> {connection?.ToArea}\n");
            }

            if (connection != null)
            {
                File.AppendAllText("ds3_debug.log", $"[FogGateService] Found connection: {connection.FromArea} -> {connection.ToArea}\n");
            }
            else
            {
                File.AppendAllText("ds3_debug.log", $"[FogGateService] No connection found for fog gate {fogGate.Name}\n");
            }

            return connection;
        }

        /// <summary>
        /// Gets fog gate information by area and ID
        /// </summary>
        public DS3FogGate? GetFogGate(string areaName, int fogGateId)
        {
            LoadDataIfNeeded();
            
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
            LoadDataIfNeeded();
            
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
            LoadDataIfNeeded();
            
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
            LoadDataIfNeeded();
            var hasData = fogDistribution != null || spoilerLog != null;
            System.Diagnostics.Debug.WriteLine($"[FogGateService] HasFogRandomizerData: {hasData} (fogDistribution: {fogDistribution != null}, spoilerLog: {spoilerLog != null})");
            return hasData;
        }

        /// <summary>
        /// Forces a reload of the fog gate data (useful for when switching to a new randomizer setup)
        /// </summary>
        public void ReloadData()
        {
            dataLoaded = false;
            combinedDataInitialized = false;
            fogDistribution = null;
            spoilerLog = null;
            LoadDataIfNeeded();
        }

        private void LoadDataIfNeeded()
        {
            if (dataLoaded)
                return;

            System.Diagnostics.Debug.WriteLine($"[FogGateService] Loading fog randomizer data");

            try
            {
                var gameDirectory = configService.Config.DarkSouls3Path;
                if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory))
                {
                    System.Diagnostics.Debug.WriteLine($"[FogGateService] Game directory not found: {gameDirectory}");
                    dataLoaded = true; // Mark as loaded to prevent repeated attempts
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
                    dataLoaded = true; // Mark as loaded to prevent repeated attempts
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[FogGateService] Parsing from game directory: {gameDirectory}");
                File.AppendAllText("ds3_debug.log", $"[FogGateService] Parsing from game directory: {gameDirectory}\n");
                
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
                }

                dataLoaded = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FogGateService] Error during data loading: {ex.Message}");
                File.AppendAllText("ds3_debug.log", $"[FogGateService] Error during data loading: {ex.Message}\n");
                dataLoaded = true; // Mark as loaded to prevent repeated attempts
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
        /// Checks if two map IDs are variations of the same base area
        /// </summary>
        /// <param name="mapId1">First map ID</param>
        /// <param name="mapId2">Second map ID</param>
        /// <returns>True if they are variations of the same base area</returns>
        private static bool AreMapVariations(string mapId1, string mapId2)
        {
            if (string.IsNullOrEmpty(mapId1) || string.IsNullOrEmpty(mapId2))
                return false;

            // Extract base map ID (e.g., "m34_00_00_00" and "m34_01_00_00" both have base "m34")
            var baseId1 = ExtractBaseMapId(mapId1);
            var baseId2 = ExtractBaseMapId(mapId2);

            return baseId1 == baseId2 && !string.IsNullOrEmpty(baseId1);
        }

        /// <summary>
        /// Extracts the base map ID from a full map ID
        /// </summary>
        /// <param name="mapId">Full map ID like "m34_01_00_00"</param>
        /// <returns>Base map ID like "m34"</returns>
        private static string ExtractBaseMapId(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
                return string.Empty;

            // Extract the base part (e.g., "m34" from "m34_01_00_00")
            var parts = mapId.Split('_');
            return parts.Length > 0 ? parts[0] : string.Empty;
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

        /// <summary>
        /// Gets the distance to a fog gate from the player's current position
        /// </summary>
        /// <param name="fogGate">The fog gate to calculate distance to</param>
        /// <param name="playerPosition">The player's current position</param>
        /// <returns>Distance in world units, or null if fog gate position is not available</returns>
        public float? GetDistanceToFogGate(DS3FogGate fogGate, Vector3 playerPosition)
        {
            File.AppendAllText("ds3_debug.log", $"[FogGateService] GetDistanceToFogGate called for gate: {fogGate.Name} (ID: {fogGate.Id})\n");
            
            EnsureCombinedDataInitialized();
            
            var gateObject = combiner.GetGateObject(fogGate);
            if (gateObject == null)
            {
                File.AppendAllText("ds3_debug.log", $"[FogGateService] No gate object found for gate: {fogGate.Name}\n");
                return null;
            }
            
            File.AppendAllText("ds3_debug.log", $"[FogGateService] Gate object found: {gateObject.Name}\n");
            
            // Check if the gate object has position data
            try
            {
                // Convert positions to System.Numerics.Vector3 for calculation
                var gatePosition = new System.Numerics.Vector3(gateObject.Position.X, gateObject.Position.Y, gateObject.Position.Z);
                var playerPos = new System.Numerics.Vector3(playerPosition.X, playerPosition.Y, playerPosition.Z);
                
                float distance = DS3Combiner.CalculateDistance(playerPos, gatePosition);
                File.AppendAllText("ds3_debug.log", $"[FogGateService] Distance calculated: {distance:F2} units\n");
                
                return distance;
            }
            catch (Exception ex)
            {
                File.AppendAllText("ds3_debug.log", $"[FogGateService] Error calculating distance: {ex.Message}\n");
                return null;
            }
        }

        /// <summary>
        /// Gets the distance to a fog gate from the player's current position by map ID and gate ID
        /// </summary>
        /// <param name="mapId">The map ID (e.g., "m40_00_00_00")</param>
        /// <param name="gateId">The fog gate ID</param>
        /// <param name="playerPosition">The player's current position</param>
        /// <returns>Distance in world units, or null if fog gate position is not available</returns>
        public float? GetDistanceToFogGate(string mapId, int gateId, Vector3 playerPosition)
        {
            var fogGate = GetFogGatesInArea(mapId).FirstOrDefault(g => g.Id == gateId);
            if (fogGate == null)
            {
                return null;
            }
            
            return GetDistanceToFogGate(fogGate, playerPosition);
        }

        /// <summary>
        /// Ensures the combined data is initialized for distance calculations
        /// </summary>
        private void EnsureCombinedDataInitialized()
        {
            File.AppendAllText("ds3_debug.log", $"[FogGateService] EnsureCombinedDataInitialized called - initialized: {combinedDataInitialized}\n");
            
            if (!combinedDataInitialized)
            {
                LoadDataIfNeeded();
                
                if (fogDistribution != null && spoilerLog != null)
                {
                    // The DarkSouls3Path points to the base DS3 directory, but we need the Game subdirectory
                    var gameDirectory = Path.Combine(configService.Config.DarkSouls3Path, "Game");
                    File.AppendAllText("ds3_debug.log", $"[FogGateService] Initializing combiner with game directory: {gameDirectory}\n");
                    
                    var fogData = new DS3FogRandomizerData
                    {
                        FogDistribution = fogDistribution,
                        SpoilerLog = spoilerLog,
                        GameDirectory = gameDirectory
                    };
                    
                    combiner.CombineData(gameDirectory, fogData);
                    combinedDataInitialized = true;
                    
                    File.AppendAllText("ds3_debug.log", $"[FogGateService] Combiner initialized successfully\n");
                }
                else
                {
                    File.AppendAllText("ds3_debug.log", $"[FogGateService] Cannot initialize combiner - fogDistribution: {fogDistribution != null}, spoilerLog: {spoilerLog != null}\n");
                }
            }
        }
    }
}
