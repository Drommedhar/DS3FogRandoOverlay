using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3Parser.Services
{
    /// <summary>
    /// Raw data structures for YAML deserialization
    /// </summary>
    internal class FogDistributionRaw
    {
        public float HealthScaling { get; set; }
        public float DamageScaling { get; set; }
        public List<AreaRaw>? Areas { get; set; }
        public List<ItemRaw>? KeyItems { get; set; }
        public List<EntranceRaw>? Entrances { get; set; }
        public List<WarpRaw>? Warps { get; set; }
        public List<ObjectRaw>? Objects { get; set; }
        public Dictionary<string, float>? DefaultCost { get; set; }
    }

    internal class AreaRaw
    {
        public string? Name { get; set; }
        public string? Text { get; set; }
        public string? Tags { get; set; }
        public string? ScalingBase { get; set; }
        public int BossTrigger { get; set; }
        public int DefeatFlag { get; set; }
        public int TrapFlag { get; set; }
        public List<SideRaw>? To { get; set; }
    }

    internal class EntranceRaw
    {
        public string? Name { get; set; }
        public int ID { get; set; }
        public string? Area { get; set; }
        public string? Text { get; set; }
        public string? Tags { get; set; }
        public string? Comment { get; set; }
        public string? DoorCond { get; set; }
        public float AdjustHeight { get; set; }
        public SideRaw? ASide { get; set; }
        public SideRaw? BSide { get; set; }
    }

    internal class WarpRaw
    {
        public int ID { get; set; }
        public string? Area { get; set; }
        public string? Text { get; set; }
        public string? Tags { get; set; }
        public float AdjustHeight { get; set; }
        public SideRaw? ASide { get; set; }
        public SideRaw? BSide { get; set; }
    }

    internal class SideRaw
    {
        public string? Area { get; set; }
        public string? Text { get; set; }
        public string? Tags { get; set; }
        public int Flag { get; set; }
        public int TrapFlag { get; set; }
        public int EntryFlag { get; set; }
        public int BeforeWarpFlag { get; set; }
        public int BossTrigger { get; set; }
        public string? BossTriggerArea { get; set; }
        public int WarpFlag { get; set; }
        public string? BossDefeatName { get; set; }
        public string? BossTrapName { get; set; }
        public string? BossTriggerName { get; set; }
        public int Cutscene { get; set; }
        public string? Cond { get; set; }
        public string? CustomWarp { get; set; }
        public int CustomActionWidth { get; set; }
        public string? Col { get; set; }
        public int ActionRegion { get; set; }
        public string? ExcludeIfRandomized { get; set; }
        public string? DestinationMap { get; set; }
        public float AdjustHeight { get; set; }
    }

    internal class ItemRaw
    {
        public string? Name { get; set; }
        public string? ID { get; set; }
        public string? Area { get; set; }
        public string? Tags { get; set; }
    }

    internal class ObjectRaw
    {
        public string? Area { get; set; }
        public string? ID { get; set; }
        public string? Text { get; set; }
        public string? Tags { get; set; }
    }
}
