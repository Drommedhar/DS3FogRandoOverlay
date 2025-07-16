using System.Numerics;

namespace DS3Parser.Models
{
    /// <summary>
    /// Represents a fog gate extracted from EMEVD files
    /// </summary>
    public class MsbFogGate
    {
        /// <summary>
        /// Name or identifier of the fog gate
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Position in 3D space (if available)
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Rotation in 3D space (if available)
        /// </summary>
        public Vector3 Rotation { get; set; }

        /// <summary>
        /// Scale in 3D space (if available)
        /// </summary>
        public Vector3 Scale { get; set; } = Vector3.One;

        /// <summary>
        /// Map ID where this fog gate is located
        /// </summary>
        public string MapId { get; set; } = string.Empty;

        /// <summary>
        /// Entity ID of the fog gate
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Model name (if available)
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is a boss fog gate
        /// </summary>
        public bool IsBossFog { get; set; }

        /// <summary>
        /// Whether this is a main progression fog gate
        /// </summary>
        public bool IsMainFog { get; set; }

        /// <summary>
        /// Reaction angle in degrees for interaction
        /// </summary>
        public float ReactionAngleDeg { get; set; }

        /// <summary>
        /// Dummy polygon ID for interaction point
        /// </summary>
        public short DummyPolyId { get; set; }

        /// <summary>
        /// Reaction distance for interaction
        /// </summary>
        public float ReactionDistance { get; set; }

        /// <summary>
        /// Help message ID shown when interacting
        /// </summary>
        public int HelpMessageId { get; set; }

        /// <summary>
        /// Reaction type for interaction
        /// </summary>
        public byte ReactionType { get; set; }

        /// <summary>
        /// Pad ID for input handling
        /// </summary>
        public int PadId { get; set; }

        /// <summary>
        /// Distance to the player (calculated dynamically)
        /// </summary>
        public float? DistanceToPlayer { get; set; }

        /// <summary>
        /// Calculate distance to a given position
        /// </summary>
        public float CalculateDistanceTo(Vector3 position)
        {
            return Vector3.Distance(this.Position, position);
        }

        public override string ToString()
        {
            return $"{Name} (Map: {MapId}, Entity: {EntityId}, Boss: {IsBossFog})";
        }
    }
}
