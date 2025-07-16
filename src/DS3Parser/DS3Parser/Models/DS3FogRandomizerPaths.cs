using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3Parser.Models
{
    /// <summary>
    /// Contains the expected paths for fog randomizer files in a DS3 installation
    /// </summary>
    public class DS3FogRandomizerPaths
    {
        /// <summary>
        /// Path to the game directory
        /// </summary>
        public string GameDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Path to the fog directory
        /// </summary>
        public string FogDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Path to the fogdist directory
        /// </summary>
        public string FogDistDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Path to the fog.txt file
        /// </summary>
        public string FogFile { get; set; } = string.Empty;

        /// <summary>
        /// Path to the locations.txt file
        /// </summary>
        public string LocationsFile { get; set; } = string.Empty;

        /// <summary>
        /// Path to the events.txt file
        /// </summary>
        public string EventsFile { get; set; } = string.Empty;

        /// <summary>
        /// Path to the spoiler logs directory
        /// </summary>
        public string SpoilerLogDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Path to the FogMod.exe file
        /// </summary>
        public string FogModExecutable { get; set; } = string.Empty;

        /// <summary>
        /// Whether the fog.txt file exists
        /// </summary>
        public bool FogFileExists => System.IO.File.Exists(FogFile);

        /// <summary>
        /// Whether the spoiler logs directory exists
        /// </summary>
        public bool SpoilerLogDirectoryExists => System.IO.Directory.Exists(SpoilerLogDirectory);

        /// <summary>
        /// Whether the fog directory exists
        /// </summary>
        public bool FogDirectoryExists => System.IO.Directory.Exists(FogDirectory);

        /// <summary>
        /// Whether the fogdist directory exists
        /// </summary>
        public bool FogDistDirectoryExists => System.IO.Directory.Exists(FogDistDirectory);
    }
}
