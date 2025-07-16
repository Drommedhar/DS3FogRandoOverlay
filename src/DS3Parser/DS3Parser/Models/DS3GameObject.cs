using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3Parser.Models
{
    /// <summary>
    /// Represents a game object in Dark Souls 3
    /// </summary>
    public class DS3GameObject
    {
        /// <summary>
        /// The area where this object is located
        /// </summary>
        public string Area { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the object
        /// </summary>
        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// Descriptive text for this object
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Tags associated with this object
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Whether this is a DLC object
        /// </summary>
        public bool IsDLC => Tags.Contains("dlc1") || Tags.Contains("dlc2");

        /// <summary>
        /// Whether this is a boss object
        /// </summary>
        public bool IsBoss => Tags.Contains("boss");

        /// <summary>
        /// Whether this is a key object
        /// </summary>
        public bool IsKey => Tags.Contains("key");
    }
}
