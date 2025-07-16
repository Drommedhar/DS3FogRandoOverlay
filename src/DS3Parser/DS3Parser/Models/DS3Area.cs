using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3Parser.Models
{
    /// <summary>
    /// Represents an area in Dark Souls 3
    /// </summary>
    public class DS3Area
    {
        /// <summary>
        /// The name of the area
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Display text for the area
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Tags associated with this area
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// The base area for scaling calculations
        /// </summary>
        public string ScalingBase { get; set; } = string.Empty;

        /// <summary>
        /// Boss trigger flag
        /// </summary>
        public int BossTrigger { get; set; }

        /// <summary>
        /// Boss defeat flag
        /// </summary>
        public int DefeatFlag { get; set; }

        /// <summary>
        /// Boss trap flag
        /// </summary>
        public int TrapFlag { get; set; }

        /// <summary>
        /// Connections to other areas
        /// </summary>
        public List<DS3FogSide> Connections { get; set; } = new List<DS3FogSide>();

        /// <summary>
        /// Whether this is a boss area
        /// </summary>
        public bool IsBoss => Tags.Contains("boss");

        /// <summary>
        /// Whether this is a small area
        /// </summary>
        public bool IsSmall => Tags.Contains("small");

        /// <summary>
        /// Whether this is a trivial area
        /// </summary>
        public bool IsTrivial => Tags.Contains("trivial");

        /// <summary>
        /// Whether this is a DLC area
        /// </summary>
        public bool IsDLC => Tags.Contains("dlc1") || Tags.Contains("dlc2");

        public static string GetAreaId(string name)
        {
            return name switch
            {
                "highwall" => "m30_00_00_00",
                "lothric" => "m30_00_00_00",
                "settlement" => "m31_00_00_00",
                "archdragon" => "m32_00_00_00",
                "farronkeep" => "m33_00_00_00",
                "archives" => "m34_00_00_00",
                "cathedral" => "m35_00_00_00",
                "irithyll" => "m37_00_00_00",
                "catacombs" => "m38_00_00_00",
                "dungeon" => "m39_00_00_00",
                "firelink" => "m40_00_00_00",
                "untended" => "m40_00_00_00",
                "kiln" => "m41_00_00_00",
                "ariandel" => "m45_00_00_00",
                "dregheap" => "m50_00_00_00",
                "ringedcity" => "m51_00_00_00",
                "filianore" => "m51_01_00_00",
                _ => ""
            };
        }
    }
}
