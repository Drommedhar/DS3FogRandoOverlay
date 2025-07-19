using System;
using DS3Parser.Models;

namespace DS3FogRandoOverlay.Models
{
    /// <summary>
    /// Represents data about a fog gate that was used by the player
    /// </summary>
    public class FogGateTravel
    {
        /// <summary>
        /// The source fog gate that was used
        /// </summary>
        public DS3FogGate SourceGate { get; set; } = new();

        /// <summary>
        /// The destination fog gate where the player appeared
        /// </summary>
        public DS3FogGate DestinationGate { get; set; } = new();

        /// <summary>
        /// The side of the source gate the player approached from
        /// </summary>
        public FogGateApproachSide SourceApproachSide { get; set; }

        /// <summary>
        /// The side of the destination gate the player was spawned on
        /// </summary>
        public FogGateApproachSide DestinationSpawnSide { get; set; }

        /// <summary>
        /// The connection information from the spoiler log
        /// </summary>
        public DS3Connection? Connection { get; set; }

        /// <summary>
        /// When this travel occurred
        /// </summary>
        public DateTime TravelTime { get; set; } = DateTime.Now;

        /// <summary>
        /// The spoiler log file this travel data belongs to
        /// </summary>
        public string SpoilerLogPath { get; set; } = string.Empty;

        /// <summary>
        /// Player position when approaching the source gate
        /// </summary>
        public DS3FogRandoOverlay.Services.Vector3? SourcePlayerPosition { get; set; }

        /// <summary>
        /// Player position when spawning at the destination gate
        /// </summary>
        public DS3FogRandoOverlay.Services.Vector3? DestinationPlayerPosition { get; set; }

        /// <summary>
        /// Display name for the connection
        /// </summary>
        public string DisplayName => $"{SourceGate.Name} → {DestinationGate.Name}";

        /// <summary>
        /// Whether this connection was confirmed by actual travel
        /// </summary>
        public bool IsConfirmed => DestinationGate.Id != 0;
    }

    /// <summary>
    /// Enum representing which side of a fog gate the player approached from or spawned on
    /// </summary>
    public enum FogGateApproachSide
    {
        /// <summary>
        /// Unknown or could not be determined
        /// </summary>
        Unknown,

        /// <summary>
        /// Approached from the forward direction (same as fog gate's forward vector)
        /// </summary>
        Forward,

        /// <summary>
        /// Approached from the reverse direction (opposite to fog gate's forward vector)
        /// </summary>
        Reverse
    }
}
