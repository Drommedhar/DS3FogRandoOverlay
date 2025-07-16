using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3Parser.Models
{
    /// <summary>
    /// Represents a fog gate in Dark Souls 3
    /// </summary>
    public class DS3FogGate
    {
        /// <summary>
        /// The unique identifier for this fog gate
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the fog gate (e.g., "o000400_0000")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The area where this fog gate is located
        /// </summary>
        public string Area { get; set; } = string.Empty;

        /// <summary>
        /// Descriptive text for this fog gate
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Tags associated with this fog gate
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// A side of the fog gate (entrance)
        /// </summary>
        public DS3FogSide ASide { get; set; } = new DS3FogSide();

        /// <summary>
        /// B side of the fog gate (exit)
        /// </summary>
        public DS3FogSide BSide { get; set; } = new DS3FogSide();

        /// <summary>
        /// Whether this fog gate is fixed (not randomized)
        /// </summary>
        public bool IsFixed { get; set; }

        /// <summary>
        /// Height adjustment for the fog gate
        /// </summary>
        public float AdjustHeight { get; set; }

        /// <summary>
        /// Comment for internal use
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// The condition for opening this door (if it's a door)
        /// </summary>
        public string DoorCondition { get; set; } = string.Empty;

        /// <summary>
        /// The full name combining area and id
        /// </summary>
        public string FullName => $"{Area}_{Id}";

        /// <summary>
        /// Whether this is a boss fog gate
        /// </summary>
        public bool IsBoss => Tags.Contains("boss");

        /// <summary>
        /// Whether this is a door
        /// </summary>
        public bool IsDoor => Tags.Contains("door");

        /// <summary>
        /// Whether this is a warp
        /// </summary>
        public bool IsWarp => Tags.Contains("warp");

        /// <summary>
        /// Get both sides of the fog gate
        /// </summary>
        public List<DS3FogSide> GetSides()
        {
            var sides = new List<DS3FogSide>();
            if (ASide != null) sides.Add(ASide);
            if (BSide != null) sides.Add(BSide);
            return sides;
        }
    }
}
