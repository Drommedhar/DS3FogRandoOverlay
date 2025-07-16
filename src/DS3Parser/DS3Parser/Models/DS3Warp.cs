using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3Parser.Models
{
    /// <summary>
    /// Represents a warp point in Dark Souls 3
    /// </summary>
    public class DS3Warp
    {
        /// <summary>
        /// The unique identifier for this warp
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The area where this warp is located
        /// </summary>
        public string Area { get; set; } = string.Empty;

        /// <summary>
        /// Descriptive text for this warp
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Tags associated with this warp
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// A side of the warp (entrance)
        /// </summary>
        public DS3FogSide ASide { get; set; } = new DS3FogSide();

        /// <summary>
        /// B side of the warp (exit)
        /// </summary>
        public DS3FogSide BSide { get; set; } = new DS3FogSide();

        /// <summary>
        /// Whether this warp is fixed (not randomized)
        /// </summary>
        public bool IsFixed { get; set; }

        /// <summary>
        /// Height adjustment for the warp
        /// </summary>
        public float AdjustHeight { get; set; }

        /// <summary>
        /// The full name combining area and id
        /// </summary>
        public string FullName => $"{Area}_{Id}";

        /// <summary>
        /// Whether this is a DLC warp
        /// </summary>
        public bool IsDLC => Tags.Contains("dlc1") || Tags.Contains("dlc2");

        /// <summary>
        /// Whether this is a unique warp
        /// </summary>
        public bool IsUnique => Tags.Contains("unique");

        /// <summary>
        /// Whether this is a kiln warp
        /// </summary>
        public bool IsKiln => Tags.Contains("kiln");

        /// <summary>
        /// Get both sides of the warp
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
