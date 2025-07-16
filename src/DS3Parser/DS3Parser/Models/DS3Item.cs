using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3Parser.Models
{
    /// <summary>
    /// Represents a key item in Dark Souls 3
    /// </summary>
    public class DS3Item
    {
        /// <summary>
        /// The name of the item
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the item
        /// </summary>
        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// The area where this item is found
        /// </summary>
        public string Area { get; set; } = string.Empty;

        /// <summary>
        /// Tags associated with this item
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Whether this is a DLC item
        /// </summary>
        public bool IsDLC => Tags.Contains("dlc1") || Tags.Contains("dlc2");

        /// <summary>
        /// Whether this is a boss item
        /// </summary>
        public bool IsBoss => Tags.Contains("boss");

        /// <summary>
        /// Whether this is a key item
        /// </summary>
        public bool IsKey => Tags.Contains("key");
    }
}
