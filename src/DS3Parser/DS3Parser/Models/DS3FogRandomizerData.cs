using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3Parser.Models
{
    /// <summary>
    /// Combined fog randomizer data from both fog distribution and spoiler log
    /// </summary>
    public class DS3FogRandomizerData
    {
        /// <summary>
        /// The fog distribution data from fog.txt
        /// </summary>
        public DS3FogDistribution? FogDistribution { get; set; }

        /// <summary>
        /// The spoiler log data
        /// </summary>
        public DS3SpoilerLog? SpoilerLog { get; set; }

        /// <summary>
        /// The game directory path used to load this data
        /// </summary>
        public string GameDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Whether both fog distribution and spoiler log data are available
        /// </summary>
        public bool HasCompleteData => FogDistribution != null && SpoilerLog != null;

        /// <summary>
        /// Whether any fog randomizer data is available
        /// </summary>
        public bool HasAnyData => FogDistribution != null || SpoilerLog != null;
    }
}
