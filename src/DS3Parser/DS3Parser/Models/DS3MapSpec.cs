using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3Parser.Models
{
    /// <summary>
    /// Represents map specification data for Dark Souls 3
    /// </summary>
    public class DS3MapSpec
    {
        /// <summary>
        /// The map file name (e.g., "m30_00_00_00")
        /// </summary>
        public string Map { get; set; } = string.Empty;

        /// <summary>
        /// The friendly name of the map (e.g., "highwall")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Start range for fog gate IDs
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// End range for fog gate IDs
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// Create a new map spec
        /// </summary>
        public static DS3MapSpec Create(string map, string name, int start, int end)
        {
            return new DS3MapSpec
            {
                Map = map,
                Name = name,
                Start = start,
                End = end
            };
        }
    }
}
