using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS3Parser.Models;

namespace DS3Parser.Models
{
    /// <summary>
    /// Represents the fog distribution data loaded from fog.txt
    /// </summary>
    public class DS3FogDistribution
    {
        /// <summary>
        /// Health scaling factor
        /// </summary>
        public float HealthScaling { get; set; } = 1.0f;

        /// <summary>
        /// Damage scaling factor
        /// </summary>
        public float DamageScaling { get; set; } = 1.0f;

        /// <summary>
        /// All areas in the game
        /// </summary>
        public List<DS3Area> Areas { get; set; } = new List<DS3Area>();

        /// <summary>
        /// All fog gates/entrances
        /// </summary>
        public List<DS3FogGate> Entrances { get; set; } = new List<DS3FogGate>();

        /// <summary>
        /// All warps
        /// </summary>
        public List<DS3Warp> Warps { get; set; } = new List<DS3Warp>();

        /// <summary>
        /// All key items
        /// </summary>
        public List<DS3Item> KeyItems { get; set; } = new List<DS3Item>();

        /// <summary>
        /// All game objects
        /// </summary>
        public List<DS3GameObject> Objects { get; set; } = new List<DS3GameObject>();

        /// <summary>
        /// Map specifications
        /// </summary>
        public List<DS3MapSpec> MapSpecs { get; set; } = new List<DS3MapSpec>();

        /// <summary>
        /// Default cost values for different operations
        /// </summary>
        public Dictionary<string, float> DefaultCosts { get; set; } = new Dictionary<string, float>();

        /// <summary>
        /// Get area by name
        /// </summary>
        public DS3Area? GetArea(string name)
        {
            return Areas.FirstOrDefault(a => a.Name == name);
        }

        /// <summary>
        /// Get fog gate by ID
        /// </summary>
        public DS3FogGate? GetFogGate(int id)
        {
            return Entrances.FirstOrDefault(e => e.Id == id);
        }

        /// <summary>
        /// Get warp by ID
        /// </summary>
        public DS3Warp? GetWarp(int id)
        {
            return Warps.FirstOrDefault(w => w.Id == id);
        }

        /// <summary>
        /// Get map spec by name
        /// </summary>
        public DS3MapSpec? GetMapSpec(string name)
        {
            return MapSpecs.FirstOrDefault(m => m.Name == name);
        }

        /// <summary>
        /// Get all boss areas
        /// </summary>
        public List<DS3Area> GetBossAreas()
        {
            return Areas.Where(a => a.IsBoss).ToList();
        }

        /// <summary>
        /// Get all boss fog gates
        /// </summary>
        public List<DS3FogGate> GetBossFogGates()
        {
            return Entrances.Where(e => e.IsBoss).ToList();
        }

        /// <summary>
        /// Get all DLC areas
        /// </summary>
        public List<DS3Area> GetDLCAreas()
        {
            return Areas.Where(a => a.IsDLC).ToList();
        }

        /// <summary>
        /// Get all DLC warps
        /// </summary>
        public List<DS3Warp> GetDLCWarps()
        {
            return Warps.Where(w => w.IsDLC).ToList();
        }
    }
}
