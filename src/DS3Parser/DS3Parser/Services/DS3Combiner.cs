using DS3Parser.Models;
using SoulsFormats;

namespace DS3Parser.Services;

public class DS3Combiner
{
    private Dictionary<string, Dictionary<DS3FogGate, MSB3.Part.Object>> _areaToGates = new();
    private string _gameDirectory = string.Empty;
    
    /// <summary>
    /// Gets the combined gate data for all areas
    /// </summary>
    public IReadOnlyDictionary<string, Dictionary<DS3FogGate, MSB3.Part.Object>> AreaToGates => _areaToGates;
    
    /// <summary>
    /// Gets the fog gate object for a specific gate in an area
    /// </summary>
    /// <param name="areaId">The area ID (e.g., "m30_00_00_00")</param>
    /// <param name="gateId">The fog gate ID</param>
    /// <returns>The MSB3 object for the gate, or null if not found</returns>
    public MSB3.Part.Object? GetGateObject(string areaId, int gateId)
    {
        if (!_areaToGates.ContainsKey(areaId))
            return null;
            
        var gateToObj = _areaToGates[areaId];
        var gate = gateToObj.Keys.FirstOrDefault(g => g.Id == gateId);
        
        return gate != null ? gateToObj[gate] : null;
    }
    
    /// <summary>
    /// Gets the fog gate object for a specific gate
    /// </summary>
    /// <param name="gate">The fog gate</param>
    /// <returns>The MSB3 object for the gate, or null if not found</returns>
    public MSB3.Part.Object? GetGateObject(DS3FogGate gate)
    {
        var areaId = DS3Area.GetAreaId(gate.Area);
        if (string.IsNullOrEmpty(areaId) || !_areaToGates.ContainsKey(areaId))
            return null;
            
        var gateToObj = _areaToGates[areaId];
        return gateToObj.ContainsKey(gate) ? gateToObj[gate] : null;
    }
    
    /// <summary>
    /// Calculates the distance between two 3D points
    /// </summary>
    /// <param name="pos1">First position</param>
    /// <param name="pos2">Second position</param>
    /// <returns>Distance in world units</returns>
    public static float CalculateDistance(System.Numerics.Vector3 pos1, System.Numerics.Vector3 pos2)
    {
        float deltaX = pos2.X - pos1.X;
        float deltaY = pos2.Y - pos1.Y;
        float deltaZ = pos2.Z - pos1.Z;
        
        return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
    }
    
    public void CombineData(string gameDirectory, DS3FogRandomizerData fogData)
    {
        _gameDirectory = gameDirectory;
        
        File.AppendAllText("ds3_debug.log", $"[DS3Combiner] CombineData called with directory: {gameDirectory}\n");
        
        if (fogData.FogDistribution is null)
        {
            File.AppendAllText("ds3_debug.log", $"[DS3Combiner] FogDistribution is null\n");
            return;
        }
        
        var gatesByArea = fogData.FogDistribution.Entrances.GroupBy(e => e.Area).
            ToDictionary(g => g.Key, g => g.ToList());

        File.AppendAllText("ds3_debug.log", $"[DS3Combiner] Found {gatesByArea.Count} areas with fog gates\n");

        foreach (var (area, entrances)  in gatesByArea)
        {
            var areaId = DS3Area.GetAreaId(area);
            if (string.IsNullOrEmpty(areaId))
            {
                File.AppendAllText("ds3_debug.log", $"[DS3Combiner] No areaId found for area: {area}\n");
                continue;
            }
            
            File.AppendAllText("ds3_debug.log", $"[DS3Combiner] Processing area: {area} -> {areaId} with {entrances.Count} entrances\n");
            ParseAreaEntrances(entrances, areaId);
        }
        
        File.AppendAllText("ds3_debug.log", $"[DS3Combiner] Finished processing. Total areas processed: {_areaToGates.Count}\n");
    }

    private void ParseAreaEntrances(List<DS3FogGate> gates, string areaId)
    {
        Dictionary<DS3FogGate, MSB3.Part.Object> gateToObj = new();
        
        // Try multiple MSB files for this area (base area and variations)
        var msbFilesToTry = GetMsbFilesForArea(areaId);
        
        foreach (var filePath in msbFilesToTry)
        {
            File.AppendAllText("ds3_debug.log", $"[DS3Combiner] Looking for MSB file: {filePath}\n");
            
            if (!File.Exists(filePath))
            {
                File.AppendAllText("ds3_debug.log", $"[DS3Combiner] MSB file not found: {filePath}\n");
                continue;
            }
            
            try
            {
                // Read and decompress the DCX file
                byte[] msbData = DCX.Decompress(filePath);
                        
                // Parse the MSB data
                var msb = MSB3.Read(msbData);
                File.AppendAllText("ds3_debug.log", $"[DS3Combiner] MSB loaded successfully. Objects count: {msb.Parts.Objects.Count}\n");
                
                // Log all available objects in this area for debugging
                var allObjects = msb.Parts.Objects.Select(o => o.Name).ToList();
                File.AppendAllText("ds3_debug.log", $"[DS3Combiner] Available objects in {Path.GetFileName(filePath)}: {string.Join(", ", allObjects.Take(20))}{(allObjects.Count > 20 ? "..." : "")}\n");
                
                // Try to find each gate's object in this MSB
                foreach (var gate in gates)
                {
                    if (gateToObj.ContainsKey(gate))
                        continue; // Already found this gate in a previous MSB file
                    
                    var gateObj = msb.Parts.Objects.FirstOrDefault(o => o.Name.Equals(gate.Name, StringComparison.OrdinalIgnoreCase));
                    if (gateObj != null)
                    {
                        gateToObj[gate] = gateObj;
                        File.AppendAllText("ds3_debug.log", $"[DS3Combiner] Found object for gate: {gate.Name}\n");
                    }
                    else
                    {
                        File.AppendAllText("ds3_debug.log", $"[DS3Combiner] Object not found for gate: {gate.Name} in {Path.GetFileName(filePath)}\n");
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("ds3_debug.log", $"[DS3Combiner] Error reading MSB file {filePath}: {ex.Message}\n");
            }
        }
        
        // Merge gates if this area ID already exists (multiple areas can map to same MSB)
        if (_areaToGates.ContainsKey(areaId))
        {
            var existingGates = _areaToGates[areaId];
            foreach (var kvp in gateToObj)
            {
                if (!existingGates.ContainsKey(kvp.Key))
                {
                    existingGates[kvp.Key] = kvp.Value;
                    File.AppendAllText("ds3_debug.log", $"[DS3Combiner] Added additional gate: {kvp.Key.Name} to existing area {areaId}\n");
                }
            }
            File.AppendAllText("ds3_debug.log", $"[DS3Combiner] Merged {gateToObj.Count} gates into existing area {areaId} (total: {existingGates.Count})\n");
        }
        else
        {
            _areaToGates[areaId] = gateToObj;
            File.AppendAllText("ds3_debug.log", $"[DS3Combiner] Added {gateToObj.Count} gates for new area {areaId}\n");
        }
    }

    /// <summary>
    /// Gets a list of MSB files to try for a given area ID, including variations
    /// </summary>
    private List<string> GetMsbFilesForArea(string areaId)
    {
        var msbFiles = new List<string>();
        var baseDir = Path.Combine(_gameDirectory, "fog", "map", "mapstudio");
        
        // Add the base area file
        msbFiles.Add(Path.Combine(baseDir, areaId + ".msb.dcx"));
        
        // Add variations for this area
        var baseAreaCode = ExtractBaseAreaCode(areaId);
        if (!string.IsNullOrEmpty(baseAreaCode))
        {
            // Look for variations like m34_01_00_00, m34_02_00_00, etc.
            var pattern = $"{baseAreaCode}_*.msb.dcx";
            if (Directory.Exists(baseDir))
            {
                var variationFiles = Directory.GetFiles(baseDir, pattern);
                foreach (var variationFile in variationFiles)
                {
                    if (!msbFiles.Contains(variationFile))
                    {
                        msbFiles.Add(variationFile);
                    }
                }
            }
        }
        
        return msbFiles;
    }
    
    /// <summary>
    /// Extracts the base area code from a map ID (e.g., "m34" from "m34_00_00_00")
    /// </summary>
    private string ExtractBaseAreaCode(string mapId)
    {
        if (string.IsNullOrEmpty(mapId))
            return string.Empty;
        
        var parts = mapId.Split('_');
        return parts.Length > 0 ? parts[0] : string.Empty;
    }
}