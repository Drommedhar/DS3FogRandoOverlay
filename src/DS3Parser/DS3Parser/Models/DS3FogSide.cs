using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3Parser.Models
{
    /// <summary>
    /// Represents one side of a fog gate
    /// </summary>
    public class DS3FogSide
    {
        /// <summary>
        /// The area this side connects to
        /// </summary>
        public string Area { get; set; } = string.Empty;

        /// <summary>
        /// Descriptive text for this side
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Tags associated with this side
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Flag to escape (DS1 legacy)
        /// </summary>
        public int Flag { get; set; }

        /// <summary>
        /// Trap flag (DS1 legacy)
        /// </summary>
        public int TrapFlag { get; set; }

        /// <summary>
        /// Entry flag (DS1 legacy)
        /// </summary>
        public int EntryFlag { get; set; }

        /// <summary>
        /// Flag to set before warping
        /// </summary>
        public int BeforeWarpFlag { get; set; }

        /// <summary>
        /// Boss trigger region
        /// </summary>
        public int BossTrigger { get; set; }

        /// <summary>
        /// Boss trigger area
        /// </summary>
        public string BossTriggerArea { get; set; } = string.Empty;

        /// <summary>
        /// Warp flag
        /// </summary>
        public int WarpFlag { get; set; }

        /// <summary>
        /// Name of the boss defeat flag area
        /// </summary>
        public string BossDefeatName { get; set; } = string.Empty;

        /// <summary>
        /// Name of the boss trap flag area
        /// </summary>
        public string BossTrapName { get; set; } = string.Empty;

        /// <summary>
        /// Name of the boss trigger area
        /// </summary>
        public string BossTriggerName { get; set; } = string.Empty;

        /// <summary>
        /// Cutscene to play when warping
        /// </summary>
        public int Cutscene { get; set; }

        /// <summary>
        /// Condition for traversing this side
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Custom warp location
        /// </summary>
        public string CustomWarp { get; set; } = string.Empty;

        /// <summary>
        /// Custom fog gate width
        /// </summary>
        public int CustomActionWidth { get; set; }

        /// <summary>
        /// The collision stepped on before warping
        /// </summary>
        public string Col { get; set; } = string.Empty;

        /// <summary>
        /// Pre-existing trigger region
        /// </summary>
        public int ActionRegion { get; set; }

        /// <summary>
        /// Don't include this side if the given entrance is not randomized
        /// </summary>
        public string ExcludeIfRandomized { get; set; } = string.Empty;

        /// <summary>
        /// The destination map for warps
        /// </summary>
        public string DestinationMap { get; set; } = string.Empty;

        /// <summary>
        /// Height adjustment for this side
        /// </summary>
        public float AdjustHeight { get; set; }

        /// <summary>
        /// Whether this side has a player spawn
        /// </summary>
        public bool HasPlayerSpawn => Tags.Contains("player");

        /// <summary>
        /// Whether this side is an insta-warp
        /// </summary>
        public bool IsInstaWarp => Tags.Contains("instawarp");
    }
}
