using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS3Parser.Models;
using SoulsFormats;

namespace DS3Parser.Services
{
    /// <summary>
    /// Service for parsing MSB (MapStudio Binary) files to extract fog gate information
    /// </summary>
    public class MsbParser
    {
        private readonly Dictionary<string, List<MsbFogGate>> _cachedFogGates = new();
        private readonly string? _mapStudioPath;
        private readonly string _darkSouls3Path;

        public MsbParser(string darkSouls3Path, string? mapStudioPath = null)
        {
            _darkSouls3Path = darkSouls3Path;
            
            if (!string.IsNullOrEmpty(mapStudioPath))
            {
                _mapStudioPath = mapStudioPath;
            }
        }

        /// <summary>
        /// Get all fog gates from all MSB files
        /// </summary>
        public Dictionary<string, List<MsbFogGate>> GetAllFogGates()
        {
            if (_cachedFogGates.Any())
                return _cachedFogGates;

            if (string.IsNullOrEmpty(_mapStudioPath) || !Directory.Exists(_mapStudioPath))
            {
                File.AppendAllText("ds3_debug.log",
                    $"[MsbParser] {DateTime.Now:HH:mm:ss.fff} - MapStudio path not found: {_mapStudioPath ?? "null"}\n");
                return _cachedFogGates;
            }

            var msbFiles = Directory.GetFiles(_mapStudioPath, "*.msb.dcx");
            
            foreach (var msbFile in msbFiles)
            {
                try
                {
                    var mapId = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(msbFile));
                    var fogGates = ParseMsbFile(msbFile, mapId);
                    
                    if (fogGates.Any())
                    {
                        _cachedFogGates[mapId] = fogGates;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing MSB file {msbFile}: {ex.Message}");
                }
            }

            return _cachedFogGates;
        }

        /// <summary>
        /// Get fog gates for a specific map
        /// </summary>
        public List<MsbFogGate> GetFogGatesForMap(string mapId)
        {
            var allFogGates = GetAllFogGates();
            return allFogGates.GetValueOrDefault(mapId, new List<MsbFogGate>());
        }

        /// <summary>
        /// Parse a single MSB file and extract fog gate information
        /// </summary>
        private List<MsbFogGate> ParseMsbFile(string filePath, string mapId)
        {
            var fogGates = new List<MsbFogGate>();

            try
            {
                // Read and decompress the DCX file
                byte[] msbData = DCX.Decompress(filePath);
                
                // Parse the MSB data
                var msb = MSB3.Read(msbData);

                // Look for objects that might be fog gates
                foreach (var part in msb.Parts.Objects)
                {
                    if (IsFogGateObject(part))
                    {
                        var fogGate = CreateFogGateFromPart(part, mapId);
                        if (fogGate != null)
                        {
                            fogGates.Add(fogGate);
                        }
                    }
                }

                // Also check collision parts for fog gates
                foreach (var part in msb.Parts.Collisions)
                {
                    if (IsFogGateCollision(part))
                    {
                        var fogGate = CreateFogGateFromCollision(part, mapId);
                        if (fogGate != null)
                        {
                            fogGates.Add(fogGate);
                        }
                    }
                }

                // Remove duplicates based on position (with small tolerance for floating point comparison)
                fogGates = DistinctByPosition(fogGates);
                
                Console.WriteLine($"Found {fogGates.Count} fog gates in {mapId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing MSB file {filePath}: {ex.Message}");
            }

            return fogGates;
        }

        /// <summary>
        /// Check if an object part represents a fog gate
        /// </summary>
        private bool IsFogGateObject(MSB3.Part.Object part)
        {
            var name = part.Name?.ToLower() ?? "";
            var modelName = part.ModelName?.ToLower() ?? "";

            // Common fog gate identifiers
            return name.Contains("fog") ||
                   name.Contains("gate") ||
                   name.Contains("door") ||
                   name.Contains("barrier") ||
                   modelName.Contains("fog") ||
                   modelName.Contains("gate") ||
                   modelName.Contains("door") ||
                   // DS3 specific fog gate model names
                   modelName.Contains("o000400") || // Common fog gate model
                   modelName.Contains("o000401") || // Common fog gate model
                   modelName.Contains("o000402"); // Common fog gate model
        }

        /// <summary>
        /// Check if a collision part represents a fog gate
        /// </summary>
        private bool IsFogGateCollision(MSB3.Part.Collision part)
        {
            var name = part.Name?.ToLower() ?? "";
            
            return name.Contains("fog") ||
                   name.Contains("gate") ||
                   name.Contains("barrier");
        }

        /// <summary>
        /// Convert SoulsFormats Vector3 to System.Numerics Vector3
        /// </summary>
        private System.Numerics.Vector3 ConvertVector3(float x, float y, float z)
        {
            return new System.Numerics.Vector3(x, y, z);
        }

        /// <summary>
        /// Create a MsbFogGate from an object part
        /// </summary>
        private MsbFogGate? CreateFogGateFromPart(MSB3.Part.Object part, string mapId)
        {
            try
            {
                return new MsbFogGate
                {
                    Name = part.Name ?? "Unknown",
                    Position = ConvertVector3(part.Position.X, part.Position.Y, part.Position.Z),
                    Rotation = ConvertVector3(part.Rotation.X, part.Rotation.Y, part.Rotation.Z),
                    Scale = ConvertVector3(part.Scale.X, part.Scale.Y, part.Scale.Z),
                    MapId = mapId,
                    EntityId = part.EntityID,
                    ModelName = part.ModelName ?? ""
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating fog gate from object part: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a MsbFogGate from a collision part
        /// </summary>
        private MsbFogGate? CreateFogGateFromCollision(MSB3.Part.Collision part, string mapId)
        {
            try
            {
                return new MsbFogGate
                {
                    Name = part.Name ?? "Unknown",
                    Position = ConvertVector3(part.Position.X, part.Position.Y, part.Position.Z),
                    Rotation = ConvertVector3(part.Rotation.X, part.Rotation.Y, part.Rotation.Z),
                    Scale = ConvertVector3(part.Scale.X, part.Scale.Y, part.Scale.Z),
                    MapId = mapId,
                    EntityId = part.EntityID,
                    ModelName = part.ModelName ?? ""
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating fog gate from collision part: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Remove duplicate fog gates based on position with tolerance for floating point comparison
        /// </summary>
        private List<MsbFogGate> DistinctByPosition(List<MsbFogGate> fogGates, float tolerance = 0.1f)
        {
            var distinctFogGates = new List<MsbFogGate>();
            
            foreach (var fogGate in fogGates)
            {
                // Check if we already have a fog gate at this position (within tolerance)
                bool isDuplicate = distinctFogGates.Any(existing => 
                    System.Numerics.Vector3.Distance(existing.Position, fogGate.Position) < tolerance);
                
                if (!isDuplicate)
                {
                    distinctFogGates.Add(fogGate);
                }
            }
            
            return distinctFogGates;
        }

        /// <summary>
        /// Clear the cache to force re-parsing of MSB files
        /// </summary>
        public void ClearCache()
        {
            _cachedFogGates.Clear();
        }
    }
}
