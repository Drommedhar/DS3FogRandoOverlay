using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3Parser.Models
{
    /// <summary>
    /// Represents a connection between areas in the randomized game
    /// </summary>
    public class DS3Connection
    {
        /// <summary>
        /// The source area
        /// </summary>
        public string FromArea { get; set; } = string.Empty;

        /// <summary>
        /// The destination area
        /// </summary>
        public string ToArea { get; set; } = string.Empty;

        /// <summary>
        /// The fog gate or warp ID used for this connection
        /// </summary>
        public int GateId { get; set; }

        /// <summary>
        /// The name of the fog gate or warp
        /// </summary>
        public string GateName { get; set; } = string.Empty;

        /// <summary>
        /// Description of the connection
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is a random connection or pre-existing
        /// </summary>
        public bool IsRandom { get; set; }

        /// <summary>
        /// Whether this is a boss connection
        /// </summary>
        public bool IsBoss { get; set; }

        /// <summary>
        /// Whether this is a warp connection
        /// </summary>
        public bool IsWarp { get; set; }

        /// <summary>
        /// Scaling percentage for the destination area
        /// </summary>
        public float ScalingPercentage { get; set; } = 100f;

        /// <summary>
        /// The full name of the connection
        /// </summary>
        public string FullName => $"{FromArea} -> {ToArea}";
    }
}
