using System.Collections.Generic;

namespace DS3Parser.Models
{
    public class FogGate
    {
        public string Name { get; set; } = string.Empty;
        public string FromArea { get; set; } = string.Empty;
        public string ToArea { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ScalingPercent { get; set; }
        public bool IsBoss { get; set; }
        public bool IsRandom { get; set; }
        public bool IsPreexisting { get; set; }
        public bool IsUsed { get; set; }
    }

    public class Area
    {
        public string Name { get; set; } = string.Empty;
        public List<FogGate> FogGates { get; set; } = new List<FogGate>();
        public int ScalingPercent { get; set; }
        public bool IsBoss { get; set; }
    }

    public class SpoilerLogData
    {
        public string Seed { get; set; } = string.Empty;
        public string Options { get; set; } = string.Empty;
        public List<Area> Areas { get; set; } = new List<Area>();
        public Dictionary<string, string> FogGateConnections { get; set; } = new Dictionary<string, string>();
    }
}
