using DS3Parser.Models;

namespace DS3FogRandoOverlay.Models
{
    /// <summary>
    /// Enhanced fog gate information including distance and location matching
    /// </summary>
    public class EnhancedFogGateInfo
    {
        /// <summary>
        /// The original MSB fog gate data
        /// </summary>
        public MsbFogGate FogGate { get; set; } = new();

        /// <summary>
        /// The matched location name from the area mapper, if any
        /// </summary>
        public string? MatchedLocationName { get; set; }

        /// <summary>
        /// Distance to the player in units
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// Whether this fog gate was successfully matched to a known location
        /// </summary>
        public bool IsLocationMatched { get; set; }

        /// <summary>
        /// Display name combining fog gate name and matched location
        /// </summary>
        public string DisplayName 
        {
            get
            {
                if (IsLocationMatched && !string.IsNullOrEmpty(MatchedLocationName))
                {
                    // If it's a boss fog gate, emphasize it
                    if (FogGate.IsBossFog)
                    {
                        return $"👑 {MatchedLocationName}";
                    }
                    return $"{FogGate.Name} → {MatchedLocationName}";
                }
                
                // Fallback to fog gate name with boss indicator
                if (FogGate.IsBossFog)
                {
                    return $"👑 {FogGate.Name}";
                }
                
                return FogGate.Name;
            }
        }

        /// <summary>
        /// Formatted distance string
        /// </summary>
        public string DistanceString => $"{Distance:F1}";

        public override string ToString()
        {
            return $"{DisplayName} (Distance: {DistanceString})";
        }
    }
}
