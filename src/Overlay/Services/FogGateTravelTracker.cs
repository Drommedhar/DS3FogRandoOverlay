using DS3FogRandoOverlay.Models;
using DS3Parser.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace DS3FogRandoOverlay.Services
{
    /// <summary>
    /// Service for tracking fog gate usage and storing travel data
    /// </summary>
    public class FogGateTravelTracker
    {
        private readonly FogGateService fogGateService;
        private readonly ConfigurationService configurationService;
        private readonly Dictionary<string, List<FogGateTravel>> travelDataBySpoilerLog;
        private readonly string travelDataFilePath;

        // State tracking for fog gate detection
        private DS3Parser.Models.DS3FogGate? lastClosestGate;
        private DS3FogRandoOverlay.Services.Vector3? lastPlayerPosition;
        private string? lastMapId;
        private DateTime lastPositionUpdate = DateTime.MinValue;
        private bool isWaitingForDestination = false;
        private FogGateTravel? pendingTravel;
        private DateTime lastMapChange = DateTime.MinValue;

        // Constants for detection
        private const float GATE_APPROACH_DISTANCE = 5.0f; // Distance considered "close" to a gate
        private const float MOVEMENT_THRESHOLD = 50.0f; // Distance that indicates a teleport/load
        private const double LOADING_SCREEN_MIN_TIME = 2.0; // Minimum time for a loading screen (seconds)
        private const double LOADING_SCREEN_MAX_TIME = 30.0; // Maximum time we'll wait for destination

        public FogGateTravelTracker(FogGateService fogGateService, ConfigurationService configurationService)
        {
            this.fogGateService = fogGateService;
            this.configurationService = configurationService;
            this.travelDataBySpoilerLog = new Dictionary<string, List<FogGateTravel>>();
            
            // Store travel data in AppData directory
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                         "DS3FogRandoOverlay");
            this.travelDataFilePath = Path.Combine(appDataPath, "fog_gate_travels.json");
            
            LoadTravelData();
        }

        /// <summary>
        /// Update the tracker with current player state
        /// </summary>
        public void UpdatePlayerState(string? mapId, DS3FogRandoOverlay.Services.Vector3? playerPosition, DS3MemoryReader memoryReader)
        {
            if (string.IsNullOrEmpty(mapId) || playerPosition == null)
                return;

            var currentTime = DateTime.Now;
            
            // Check if map changed (possible teleport/loading screen)
            bool mapChanged = lastMapId != null && lastMapId != mapId;
            if (mapChanged)
            {
                LogDebug($"Map changed from {lastMapId} to {mapId}");
                lastMapChange = currentTime;
                
                // If we were waiting for a destination, check if this is it
                if (isWaitingForDestination && pendingTravel != null)
                {
                    ProcessPossibleDestination(mapId, playerPosition, memoryReader);
                }
            }

            // Check for large position jumps (teleport within same map)
            bool positionJumped = false;
            if (lastPlayerPosition != null && lastMapId == mapId)
            {
                float deltaX = lastPlayerPosition.X - playerPosition.X;
                float deltaY = lastPlayerPosition.Y - playerPosition.Y;
                float deltaZ = lastPlayerPosition.Z - playerPosition.Z;
                float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
                positionJumped = distance > MOVEMENT_THRESHOLD;
                
                if (positionJumped)
                {
                    LogDebug($"Large position jump detected: {distance:F1} units");
                    
                    // If we were waiting for a destination, check if this is it
                    if (isWaitingForDestination && pendingTravel != null)
                    {
                        ProcessPossibleDestination(mapId, playerPosition, memoryReader);
                    }
                }
            }

            // Check for approaching fog gates (only when not waiting for destination)
            if (!isWaitingForDestination)
            {
                CheckForFogGateApproach(mapId, playerPosition, memoryReader);
            }

            // Clean up old pending travels that have timed out
            if (isWaitingForDestination && pendingTravel != null)
            {
                var timeSinceStart = (currentTime - pendingTravel.TravelTime).TotalSeconds;
                if (timeSinceStart > LOADING_SCREEN_MAX_TIME)
                {
                    LogDebug($"Pending travel timed out after {timeSinceStart:F1} seconds");
                    ClearPendingTravel();
                }
            }

            // Update state
            lastPlayerPosition = playerPosition;
            lastMapId = mapId;
            lastPositionUpdate = currentTime;
        }

        /// <summary>
        /// Check if the player is approaching a fog gate
        /// </summary>
        private void CheckForFogGateApproach(string mapId, DS3FogRandoOverlay.Services.Vector3 playerPosition, DS3MemoryReader memoryReader)
        {
            var fogGatesInArea = fogGateService.GetFogGatesInArea(mapId);
            
            // Find the closest fog gate within approach distance
            DS3Parser.Models.DS3FogGate? closestGate = null;
            float closestDistance = float.MaxValue;
            
            foreach (var fogGate in fogGatesInArea)
            {
                var distance = fogGateService.GetDistanceToFogGate(fogGate, playerPosition);
                if (distance.HasValue && distance.Value < GATE_APPROACH_DISTANCE && distance.Value < closestDistance)
                {
                    closestGate = fogGate;
                    closestDistance = distance.Value;
                }
            }

            // If we found a close gate and it's different from the last one
            if (closestGate != null && (lastClosestGate == null || lastClosestGate.Id != closestGate.Id))
            {
                LogDebug($"Player approaching fog gate: {closestGate.Name} (distance: {closestDistance:F1})");
                
                // Determine approach side
                var approachSide = DetermineApproachSide(closestGate, playerPosition);
                LogDebug($"Approach side: {approachSide}");
                
                // Start monitoring for potential fog gate usage
                StartMonitoringGateUsage(closestGate, playerPosition, approachSide, mapId);
            }

            lastClosestGate = closestGate;
        }

        /// <summary>
        /// Start monitoring a fog gate for potential usage
        /// </summary>
        private void StartMonitoringGateUsage(DS3Parser.Models.DS3FogGate gate, DS3FogRandoOverlay.Services.Vector3 playerPosition, FogGateApproachSide approachSide, string mapId)
        {
            // Create a pending travel record
            pendingTravel = new FogGateTravel
            {
                SourceGate = gate,
                SourceApproachSide = approachSide,
                SourcePlayerPosition = playerPosition,
                TravelTime = DateTime.Now,
                SpoilerLogPath = GetCurrentSpoilerLogPath()
            };

            // Get the connection info from the spoiler log
            pendingTravel.Connection = fogGateService.GetConnectionForFogGate(mapId, gate.Id);

            isWaitingForDestination = true;
            LogDebug($"Started monitoring fog gate usage: {gate.Name}");
        }

        /// <summary>
        /// Process a possible destination after a loading screen or teleport
        /// </summary>
        private void ProcessPossibleDestination(string mapId, DS3FogRandoOverlay.Services.Vector3 playerPosition, DS3MemoryReader memoryReader)
        {
            if (pendingTravel == null)
                return;

            var fogGatesInArea = fogGateService.GetFogGatesInArea(mapId);
            
            // Find the closest fog gate to where the player spawned
            DS3Parser.Models.DS3FogGate? destinationGate = null;
            float closestDistance = float.MaxValue;
            
            foreach (var fogGate in fogGatesInArea)
            {
                var distance = fogGateService.GetDistanceToFogGate(fogGate, playerPosition);
                if (distance.HasValue && distance.Value < GATE_APPROACH_DISTANCE && distance.Value < closestDistance)
                {
                    destinationGate = fogGate;
                    closestDistance = distance.Value;
                }
            }

            if (destinationGate != null)
            {
                LogDebug($"Found destination gate: {destinationGate.Name} (distance: {closestDistance:F1})");
                
                // Determine spawn side
                var spawnSide = DetermineApproachSide(destinationGate, playerPosition);
                
                // Complete the travel record
                pendingTravel.DestinationGate = destinationGate;
                pendingTravel.DestinationSpawnSide = spawnSide;
                pendingTravel.DestinationPlayerPosition = playerPosition;

                // Verify this makes sense with the spoiler log connection
                if (ValidateConnection(pendingTravel))
                {
                    // Store the completed travel
                    StoreTravelData(pendingTravel);
                    LogDebug($"Stored fog gate travel: {pendingTravel.DisplayName}");
                }
                else
                {
                    LogDebug($"Travel validation failed for: {pendingTravel.DisplayName}");
                }

                ClearPendingTravel();
            }
        }

        /// <summary>
        /// Determine which side of a fog gate the player is on
        /// </summary>
        private FogGateApproachSide DetermineApproachSide(DS3Parser.Models.DS3FogGate gate, DS3FogRandoOverlay.Services.Vector3 playerPosition)
        {
            try
            {
                // Get the actual gate object with position/rotation data from the combiner
                var gateObject = fogGateService.GetGateObject(gate);
                if (gateObject == null)
                {
                    LogDebug($"Could not get gate object for {gate.Name}, returning Unknown approach side");
                    return FogGateApproachSide.Unknown;
                }

                // Get position and rotation from the gate object
                var gatePos = gateObject.Position;
                var gateRot = gateObject.Rotation;

                // Calculate the forward vector from the gate's rotation
                var forwardVector = GetForwardVectorFromRotation(gateRot);
                
                // Vector from gate to player
                var gateToPlayerX = playerPosition.X - gatePos.X;
                var gateToPlayerY = playerPosition.Y - gatePos.Y;
                var gateToPlayerZ = playerPosition.Z - gatePos.Z;
                
                // Normalize the gate to player vector
                var length = (float)Math.Sqrt(gateToPlayerX * gateToPlayerX + gateToPlayerY * gateToPlayerY + gateToPlayerZ * gateToPlayerZ);
                if (length > 0)
                {
                    gateToPlayerX /= length;
                    gateToPlayerY /= length;
                    gateToPlayerZ /= length;
                }
                
                // Dot product to determine if player is in front or behind
                var dotProduct = forwardVector.X * gateToPlayerX + forwardVector.Y * gateToPlayerY + forwardVector.Z * gateToPlayerZ;
                
                // Positive dot product means player is in front (forward direction)
                // Negative means player is behind (reverse direction)
                return dotProduct > 0 ? FogGateApproachSide.Forward : FogGateApproachSide.Reverse;
            }
            catch (Exception ex)
            {
                LogDebug($"Error determining approach side for gate {gate.Name}: {ex.Message}");
                return FogGateApproachSide.Unknown;
            }
        }

        /// <summary>
        /// Determine which side of a fog gate the player is on using SoulsFormats MSB object
        /// </summary>
        private FogGateApproachSide DetermineApproachSide(SoulsFormats.MSB3.Part.Object gateObject, DS3FogRandoOverlay.Services.Vector3 playerPosition)
        {
            try
            {
                // Get position and rotation from the gate object
                var gatePos = gateObject.Position;
                var gateRot = gateObject.Rotation;

                // Calculate the forward vector from the gate's rotation
                var forwardVector = GetForwardVectorFromRotation(gateRot);
                
                // Vector from gate to player
                var gateToPlayerX = playerPosition.X - gatePos.X;
                var gateToPlayerY = playerPosition.Y - gatePos.Y;
                var gateToPlayerZ = playerPosition.Z - gatePos.Z;
                
                // Normalize the gate to player vector
                var length = (float)Math.Sqrt(gateToPlayerX * gateToPlayerX + gateToPlayerY * gateToPlayerY + gateToPlayerZ * gateToPlayerZ);
                if (length > 0)
                {
                    gateToPlayerX /= length;
                    gateToPlayerY /= length;
                    gateToPlayerZ /= length;
                }
                
                // Dot product to determine if player is in front or behind
                var dotProduct = forwardVector.X * gateToPlayerX + forwardVector.Y * gateToPlayerY + forwardVector.Z * gateToPlayerZ;
                
                // Positive dot product means player is in front (forward direction)
                // Negative means player is behind (reverse direction)
                return dotProduct > 0 ? FogGateApproachSide.Forward : FogGateApproachSide.Reverse;
            }
            catch (Exception ex)
            {
                LogDebug($"Error determining approach side for gate {gateObject.Name}: {ex.Message}");
                return FogGateApproachSide.Unknown;
            }
        }

        /// <summary>
        /// Convert rotation to forward vector
        /// </summary>
        private System.Numerics.Vector3 GetForwardVectorFromRotation(System.Numerics.Vector3 rotation)
        {
            // DS3 uses radians for rotation, Y is the primary rotation axis for fog gates
            var yRotation = rotation.Y;
            
            // Forward vector in DS3 coordinate system
            return new System.Numerics.Vector3(
                (float)Math.Sin(yRotation),  // X component
                0,                           // Y component (up/down)
                (float)Math.Cos(yRotation)   // Z component  
            );
        }

        /// <summary>
        /// Validate that the travel makes sense based on the spoiler log
        /// </summary>
        private bool ValidateConnection(FogGateTravel travel)
        {
            if (travel.Connection == null)
            {
                LogDebug("No connection data available for validation");
                return true; // Store anyway, might be useful
            }

            // For now, just log the validation - we can add more sophisticated checks later
            LogDebug($"Validating connection: {travel.Connection.Description}");
            return true;
        }

        /// <summary>
        /// Store travel data and save to file
        /// </summary>
        private void StoreTravelData(FogGateTravel travel)
        {
            var spoilerLogPath = travel.SpoilerLogPath;
            
            if (!travelDataBySpoilerLog.ContainsKey(spoilerLogPath))
            {
                travelDataBySpoilerLog[spoilerLogPath] = new List<FogGateTravel>();
            }

            travelDataBySpoilerLog[spoilerLogPath].Add(travel);
            SaveTravelData();
        }

        /// <summary>
        /// Get traveled connections for the current spoiler log
        /// </summary>
        public List<FogGateTravel> GetTraveledConnections(string? spoilerLogPath = null)
        {
            spoilerLogPath ??= GetCurrentSpoilerLogPath();
            
            if (travelDataBySpoilerLog.TryGetValue(spoilerLogPath, out var travels))
            {
                return travels.ToList();
            }

            return new List<FogGateTravel>();
        }

        /// <summary>
        /// Check if a specific connection has been traveled FROM this gate
        /// </summary>
        public bool HasTraveledConnection(string mapId, int gateId, string? spoilerLogPath = null)
        {
            var travels = GetTraveledConnections(spoilerLogPath);
            return travels.Any(t => t.SourceGate.Area == GetAreaNameFromMapId(mapId) && t.SourceGate.Id == gateId);
        }

        /// <summary>
        /// Check if this gate has been traveled TO (as a destination)
        /// </summary>
        public bool HasBeenTraveledTo(string mapId, int gateId, string? spoilerLogPath = null)
        {
            var travels = GetTraveledConnections(spoilerLogPath);
            return travels.Any(t => t.DestinationGate.Area == GetAreaNameFromMapId(mapId) && t.DestinationGate.Id == gateId);
        }

        /// <summary>
        /// Get the destination for a specific traveled connection FROM this gate
        /// </summary>
        public string? GetTraveledDestination(string mapId, int gateId, string? spoilerLogPath = null)
        {
            var travels = GetTraveledConnections(spoilerLogPath);
            var travel = travels.FirstOrDefault(t => t.SourceGate.Area == GetAreaNameFromMapId(mapId) && t.SourceGate.Id == gateId);
            return travel?.DestinationGate.Name;
        }

        /// <summary>
        /// Get the source for a specific gate that has been traveled TO
        /// </summary>
        public string? GetTraveledSource(string mapId, int gateId, string? spoilerLogPath = null)
        {
            var travels = GetTraveledConnections(spoilerLogPath);
            var travel = travels.FirstOrDefault(t => t.DestinationGate.Area == GetAreaNameFromMapId(mapId) && t.DestinationGate.Id == gateId);
            return travel?.SourceGate.Name;
        }

        /// <summary>
        /// Get the source gate object for a specific gate that has been traveled TO
        /// </summary>
        public DS3FogGate? GetTraveledSourceGate(string mapId, int gateId, string? spoilerLogPath = null)
        {
            var travels = GetTraveledConnections(spoilerLogPath);
            var travel = travels.FirstOrDefault(t => t.DestinationGate.Area == GetAreaNameFromMapId(mapId) && t.DestinationGate.Id == gateId);
            return travel?.SourceGate;
        }

        /// <summary>
        /// Get relevant travel information, prioritizing side detection and showing only data for the current side
        /// </summary>
        public (bool hasTravel, string? travelInfo, bool isSource) GetRelevantTravelInfo(string mapId, int gateId, DS3FogRandoOverlay.Services.Vector3 playerPosition, string? spoilerLogPath = null)
        {
            var travels = GetTraveledConnections(spoilerLogPath);
            var areaName = GetAreaNameFromMapId(mapId);
            
            LogDebug($"GetRelevantTravelInfo - mapId: {mapId}, gateId: {gateId}, area: {areaName}");
            
            // Find ALL travels involving this gate
            var asSourceTravels = travels.Where(t => t.SourceGate.Area == areaName && t.SourceGate.Id == gateId).ToList();
            var asDestinationTravels = travels.Where(t => t.DestinationGate.Area == areaName && t.DestinationGate.Id == gateId).ToList();
            
            LogDebug($"Found {asSourceTravels.Count} source travels, {asDestinationTravels.Count} destination travels");
            
            // Log details of each travel for debugging
            for (int i = 0; i < asSourceTravels.Count; i++)
            {
                var travel = asSourceTravels[i];
                LogDebug($"  Source travel {i + 1}: {travel.SourceGate.Name}({travel.SourceApproachSide}) -> {travel.DestinationGate.Name} at {travel.TravelTime:HH:mm:ss}");
            }
            for (int i = 0; i < asDestinationTravels.Count; i++)
            {
                var travel = asDestinationTravels[i];
                LogDebug($"  Destination travel {i + 1}: {travel.SourceGate.Name} -> {travel.DestinationGate.Name}({travel.DestinationSpawnSide}) at {travel.TravelTime:HH:mm:ss}");
            }
            
            // If no travels found, return no info
            if (!asSourceTravels.Any() && !asDestinationTravels.Any())
            {
                LogDebug("No travels found for this gate");
                return (false, null, false);
            }
            
            // FIRST PRIORITY: Determine which side the player is currently on
            var fogGate = fogGateService.GetFogGate(areaName, gateId);
            if (fogGate == null)
            {
                LogDebug("Could not find fog gate object - cannot determine side, using fallback");
                return GetFallbackTravelInfo(asSourceTravels, asDestinationTravels);
            }

            var gateObject = fogGateService.GetGateObject(fogGate);
            if (gateObject == null)
            {
                LogDebug("Could not get gate object - cannot determine side, using fallback");
                return GetFallbackTravelInfo(asSourceTravels, asDestinationTravels);
            }
            
            // Determine which side the player is currently on
            var currentSide = DetermineApproachSide(gateObject, playerPosition);
            LogDebug($"Player current side: {currentSide}");
            
            if (currentSide == FogGateApproachSide.Unknown)
            {
                LogDebug("Unknown side detected, using fallback");
                return GetFallbackTravelInfo(asSourceTravels, asDestinationTravels);
            }
            
            // SECOND PRIORITY: Find travels that match the current side exactly
            // Check destination travels where we spawned on the current side (arrived from)
            var matchingDestinationTravels = asDestinationTravels.Where(t => t.DestinationSpawnSide == currentSide).ToList();
            if (matchingDestinationTravels.Any())
            {
                var travel = matchingDestinationTravels.OrderByDescending(t => t.TravelTime).First();
                var sourceText = travel.SourceGate.Text;
                if (string.IsNullOrWhiteSpace(sourceText))
                    sourceText = travel.SourceGate.Name;
                LogDebug($"Found matching destination travel for {currentSide} side - showing source: {sourceText}");
                return (true, sourceText, false);
            }
            
            // Check source travels where we approached from the current side (went to)
            var matchingSourceTravels = asSourceTravels.Where(t => t.SourceApproachSide == currentSide).ToList();
            if (matchingSourceTravels.Any())
            {
                var travel = matchingSourceTravels.OrderByDescending(t => t.TravelTime).First();
                var destinationText = travel.DestinationGate.Text;
                if (string.IsNullOrWhiteSpace(destinationText))
                    destinationText = travel.DestinationGate.Name;
                LogDebug($"Found matching source travel for {currentSide} side - showing destination: {destinationText}");
                return (true, destinationText, true);
            }
            
            // THIRD PRIORITY: Check for travels with unknown sides that could apply to current side
            var unknownSideDestTravels = asDestinationTravels.Where(t => t.DestinationSpawnSide == FogGateApproachSide.Unknown).ToList();
            if (unknownSideDestTravels.Any())
            {
                var travel = unknownSideDestTravels.OrderByDescending(t => t.TravelTime).First();
                var sourceText = travel.SourceGate.Text;
                if (string.IsNullOrWhiteSpace(sourceText))
                    sourceText = travel.SourceGate.Name;
                LogDebug($"Using destination travel with unknown side for {currentSide} side: {sourceText}");
                return (true, sourceText, false);
            }
            
            var unknownSideSourceTravels = asSourceTravels.Where(t => t.SourceApproachSide == FogGateApproachSide.Unknown).ToList();
            if (unknownSideSourceTravels.Any())
            {
                var travel = unknownSideSourceTravels.OrderByDescending(t => t.TravelTime).First();
                var destinationText = travel.DestinationGate.Text;
                if (string.IsNullOrWhiteSpace(destinationText))
                    destinationText = travel.DestinationGate.Name;
                LogDebug($"Using source travel with unknown side for {currentSide} side: {destinationText}");
                return (true, destinationText, true);
            }
            
            // FINAL FALLBACK: No travels match the current side
            LogDebug($"No travels found for {currentSide} side - no travel info to show");
            return (false, null, false);
        }
        
        /// <summary>
        /// Fallback method when side detection fails - prioritizes "arrived from" information
        /// </summary>
        private (bool hasTravel, string? travelInfo, bool isSource) GetFallbackTravelInfo(List<FogGateTravel> asSourceTravels, List<FogGateTravel> asDestinationTravels)
        {
            LogDebug("Using fallback travel info selection");
            
            // Prioritize "arrived from" information when side detection fails
            if (asDestinationTravels.Any())
            {
                var travel = asDestinationTravels.OrderByDescending(t => t.TravelTime).First();
                var sourceText = travel.SourceGate.Text;
                if (string.IsNullOrWhiteSpace(sourceText))
                    sourceText = travel.SourceGate.Name;
                LogDebug($"Fallback: showing most recent destination travel: {sourceText}");
                return (true, sourceText, false);
            }
            
            if (asSourceTravels.Any())
            {
                var travel = asSourceTravels.OrderByDescending(t => t.TravelTime).First();
                var destinationText = travel.DestinationGate.Text;
                if (string.IsNullOrWhiteSpace(destinationText))
                    destinationText = travel.DestinationGate.Name;
                LogDebug($"Fallback: showing most recent source travel: {destinationText}");
                return (true, destinationText, true);
            }
            
            return (false, null, false);
        }

        /// <summary>
        /// Clear pending travel state
        /// </summary>
        private void ClearPendingTravel()
        {
            pendingTravel = null;
            isWaitingForDestination = false;
        }

        /// <summary>
        /// Get the current spoiler log file path
        /// </summary>
        private string GetCurrentSpoilerLogPath()
        {
            // Try to get the current spoiler log from the fog gate service
            try
            {
                // This is a placeholder - we'll need to add a method to get the current spoiler log path
                return fogGateService.GetCurrentSpoilerLogPath() ?? "default";
            }
            catch
            {
                return "default";
            }
        }

        /// <summary>
        /// Load travel data from file
        /// </summary>
        private void LoadTravelData()
        {
            try
            {
                if (File.Exists(travelDataFilePath))
                {
                    var json = File.ReadAllText(travelDataFilePath);
                    var data = JsonConvert.DeserializeObject<Dictionary<string, List<FogGateTravel>>>(json);
                    
                    if (data != null)
                    {
                        foreach (var kvp in data)
                        {
                            travelDataBySpoilerLog[kvp.Key] = kvp.Value;
                        }
                        LogDebug($"Loaded travel data for {travelDataBySpoilerLog.Count} spoiler logs");
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error loading travel data: {ex.Message}");
            }
        }

        /// <summary>
        /// Save travel data to file
        /// </summary>
        private void SaveTravelData()
        {
            try
            {
                var json = JsonConvert.SerializeObject(travelDataBySpoilerLog, Formatting.Indented);
                File.WriteAllText(travelDataFilePath, json);
                LogDebug("Travel data saved successfully");
            }
            catch (Exception ex)
            {
                LogDebug($"Error saving travel data: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all travel data for a specific spoiler log
        /// </summary>
        public void ClearTravelData(string spoilerLogPath)
        {
            if (travelDataBySpoilerLog.ContainsKey(spoilerLogPath))
            {
                travelDataBySpoilerLog.Remove(spoilerLogPath);
                SaveTravelData();
                LogDebug($"Cleared travel data for spoiler log: {spoilerLogPath}");
            }
        }

        /// <summary>
        /// Clear all travel data
        /// </summary>
        public void ClearAllTravelData()
        {
            travelDataBySpoilerLog.Clear();
            SaveTravelData();
            LogDebug("Cleared all travel data");
        }

        /// <summary>
        /// Convert mapId to area name for comparison with DS3FogGate.Area
        /// </summary>
        private string GetAreaNameFromMapId(string mapId)
        {
            // This is a simple mapping - in a more complete implementation
            // you might want to use the AreaMapper or a more sophisticated lookup
            var mapping = new Dictionary<string, string>
            {
                { "m30_00_00_00", "highwall" },
                { "m30_01_00_00", "lothric" },
                { "m31_00_00_00", "settlement" },
                { "m32_00_00_00", "archdragon" },
                { "m33_00_00_00", "farronkeep" },
                { "m34_01_00_00", "archives" },
                { "m35_00_00_00", "cathedral" },
                { "m37_00_00_00", "irithyll" },
                { "m38_00_00_00", "catacombs" },
                { "m39_00_00_00", "dungeon" },
                { "m40_00_00_00", "firelink" },
                { "m40_01_00_00", "untended" },
                { "m41_00_00_00", "kiln" },
                { "m45_00_00_00", "ariandel" },
                { "m50_00_00_00", "dregheap" },
                { "m51_00_00_00", "ringedcity" },
                { "m51_01_00_00", "filianore" }
            };

            return mapping.TryGetValue(mapId, out var areaName) ? areaName : mapId;
        }

        /// <summary>
        /// Debug logging
        /// </summary>
        private void LogDebug(string message)
        {
            try
            {
                var logMessage = $"[FogGateTravelTracker] {DateTime.Now:HH:mm:ss.fff} - {message}";
                File.AppendAllText("ds3_debug.log", logMessage + Environment.NewLine);
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}
