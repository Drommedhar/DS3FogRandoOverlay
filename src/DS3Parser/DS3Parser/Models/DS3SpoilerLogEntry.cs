using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3Parser.Models
{
    /// <summary>
    /// Represents a single entry in the spoiler log
    /// </summary>
    public class DS3SpoilerLogEntry
    {
        /// <summary>
        /// The name of the area
        /// </summary>
        public string AreaName { get; set; } = string.Empty;

        /// <summary>
        /// The scaling percentage for this area
        /// </summary>
        public float ScalingPercentage { get; set; } = 100f;

        /// <summary>
        /// Whether this is a boss area
        /// </summary>
        public bool IsBoss { get; set; }

        /// <summary>
        /// Connections from this area
        /// </summary>
        public List<DS3SpoilerLogConnection> Connections { get; set; } = new List<DS3SpoilerLogConnection>();

        /// <summary>
        /// Get random connections from this area
        /// </summary>
        public List<DS3SpoilerLogConnection> GetRandomConnections()
        {
            return Connections.Where(c => c.IsRandom).ToList();
        }

        /// <summary>
        /// Get preexisting connections from this area
        /// </summary>
        public List<DS3SpoilerLogConnection> GetPreexistingConnections()
        {
            return Connections.Where(c => !c.IsRandom).ToList();
        }
    }

    /// <summary>
    /// Represents a connection in the spoiler log
    /// </summary>
    public class DS3SpoilerLogConnection
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
        /// Description of the connection
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is a random connection
        /// </summary>
        public bool IsRandom { get; set; }

        /// <summary>
        /// The full connection text as it appears in the log
        /// </summary>
        public string FullText { get; set; } = string.Empty;
    }
}
