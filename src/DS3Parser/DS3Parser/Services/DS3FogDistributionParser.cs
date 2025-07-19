using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS3Parser.Models;
using YamlDotNet.Serialization;

namespace DS3Parser.Services
{
    /// <summary>
    /// Service for parsing fog distribution data from fog.txt
    /// </summary>
    public class DS3FogDistributionParser
    {
        private readonly IDeserializer _deserializer;

        public DS3FogDistributionParser()
        {
            _deserializer = new DeserializerBuilder().Build();
        }

        /// <summary>
        /// Parse fog distribution data from fog.txt file
        /// </summary>
        /// <param name="fogFilePath">Path to the fog.txt file</param>
        /// <returns>Parsed fog distribution data</returns>
        public DS3FogDistribution ParseFogFile(string fogFilePath)
        {
            if (!File.Exists(fogFilePath))
            {
                throw new FileNotFoundException($"Fog file not found: {fogFilePath}");
            }

            var rawData = ParseRawFogData(fogFilePath);
            return ConvertToDS3FogDistribution(rawData);
        }

        /// <summary>
        /// Parse fog distribution data from the DS3 game directory
        /// </summary>
        /// <param name="gameDirectory">Path to the DS3 game directory</param>
        /// <returns>Parsed fog distribution data</returns>
        public DS3FogDistribution ParseFromGameDirectory(string gameDirectory)
        {
            var fogFilePath = Path.Combine(gameDirectory, "fog", "fogdist", "fog.txt");
            return ParseFogFile(fogFilePath);
        }

        /// <summary>
        /// Parse fog distribution data from the fog mod directory
        /// </summary>
        /// <param name="fogDirectory">Path to the fog mod directory</param>
        /// <returns>Parsed fog distribution data</returns>
        public DS3FogDistribution ParseFromFogDirectory(string fogDirectory)
        {
            var fogFilePath = Path.Combine(fogDirectory, "fogdist", "fog.txt");
            return ParseFogFile(fogFilePath);
        }

        private FogDistributionRaw ParseRawFogData(string fogFilePath)
        {
            using var reader = new StreamReader(fogFilePath);
            return _deserializer.Deserialize<FogDistributionRaw>(reader);
        }

        private DS3FogDistribution ConvertToDS3FogDistribution(FogDistributionRaw raw)
        {
            var distribution = new DS3FogDistribution
            {
                HealthScaling = raw.HealthScaling,
                DamageScaling = raw.DamageScaling,
                DefaultCosts = raw.DefaultCost ?? new Dictionary<string, float>(),
                MapSpecs = GetDS3MapSpecs()
            };

            // Convert areas
            if (raw.Areas != null)
            {
                foreach (var rawArea in raw.Areas)
                {
                    distribution.Areas.Add(ConvertArea(rawArea));
                }
            }

            // Convert entrances
            if (raw.Entrances != null)
            {
                foreach (var rawEntrance in raw.Entrances)
                {
                    distribution.Entrances.Add(ConvertFogGate(rawEntrance));
                }
            }

            // Convert warps
            if (raw.Warps != null)
            {
                foreach (var rawWarp in raw.Warps)
                {
                    distribution.Warps.Add(ConvertWarp(rawWarp));
                }
            }

            // Convert key items
            if (raw.KeyItems != null)
            {
                foreach (var rawItem in raw.KeyItems)
                {
                    distribution.KeyItems.Add(ConvertItem(rawItem));
                }
            }

            // Convert objects
            if (raw.Objects != null)
            {
                foreach (var rawObject in raw.Objects)
                {
                    distribution.Objects.Add(ConvertObject(rawObject));
                }
            }

            return distribution;
        }

        private DS3Area ConvertArea(AreaRaw raw)
        {
            return new DS3Area
            {
                Name = raw.Name ?? string.Empty,
                Text = raw.Text ?? string.Empty,
                Tags = ParseTags(raw.Tags),
                ScalingBase = raw.ScalingBase ?? string.Empty,
                BossTrigger = raw.BossTrigger,
                DefeatFlag = raw.DefeatFlag,
                TrapFlag = raw.TrapFlag,
                Connections = raw.To?.Select(ConvertSide).ToList() ?? new List<DS3FogSide>()
            };
        }

        private DS3FogGate ConvertFogGate(EntranceRaw raw)
        {
            return new DS3FogGate
            {
                Id = raw.ID,
                Name = raw.Name ?? string.Empty,
                Area = raw.Area ?? string.Empty,
                Text = raw.Text ?? string.Empty,
                Tags = ParseTags(raw.Tags),
                Comment = raw.Comment ?? string.Empty,
                DoorCondition = raw.DoorCond ?? string.Empty,
                AdjustHeight = raw.AdjustHeight,
                ASide = raw.ASide != null ? ConvertSide(raw.ASide) : new DS3FogSide(),
                BSide = raw.BSide != null ? ConvertSide(raw.BSide) : new DS3FogSide(),
                IsFixed = false // This will be determined by the randomizer
            };
        }

        private DS3Warp ConvertWarp(WarpRaw raw)
        {
            return new DS3Warp
            {
                Id = raw.ID,
                Area = raw.Area ?? string.Empty,
                Text = raw.Text ?? string.Empty,
                Tags = ParseTags(raw.Tags),
                AdjustHeight = raw.AdjustHeight,
                ASide = raw.ASide != null ? ConvertSide(raw.ASide) : new DS3FogSide(),
                BSide = raw.BSide != null ? ConvertSide(raw.BSide) : new DS3FogSide(),
                IsFixed = false // This will be determined by the randomizer
            };
        }

        private DS3FogSide ConvertSide(SideRaw raw)
        {
            return new DS3FogSide
            {
                Area = raw.Area ?? string.Empty,
                Text = raw.Text ?? string.Empty,
                Tags = ParseTags(raw.Tags),
                Flag = raw.Flag,
                TrapFlag = raw.TrapFlag,
                EntryFlag = raw.EntryFlag,
                BeforeWarpFlag = raw.BeforeWarpFlag,
                BossTrigger = raw.BossTrigger,
                BossTriggerArea = raw.BossTriggerArea ?? string.Empty,
                WarpFlag = raw.WarpFlag,
                BossDefeatName = raw.BossDefeatName ?? string.Empty,
                BossTrapName = raw.BossTrapName ?? string.Empty,
                BossTriggerName = raw.BossTriggerName ?? string.Empty,
                Cutscene = raw.Cutscene,
                Condition = raw.Cond ?? string.Empty,
                CustomWarp = raw.CustomWarp ?? string.Empty,
                CustomActionWidth = raw.CustomActionWidth,
                Col = raw.Col ?? string.Empty,
                ActionRegion = raw.ActionRegion,
                ExcludeIfRandomized = raw.ExcludeIfRandomized ?? string.Empty,
                DestinationMap = raw.DestinationMap ?? string.Empty,
                AdjustHeight = raw.AdjustHeight
            };
        }

        private DS3Item ConvertItem(ItemRaw raw)
        {
            return new DS3Item
            {
                Name = raw.Name ?? string.Empty,
                ID = raw.ID ?? string.Empty,
                Area = raw.Area ?? string.Empty,
                Tags = ParseTags(raw.Tags)
            };
        }

        private DS3GameObject ConvertObject(ObjectRaw raw)
        {
            return new DS3GameObject
            {
                Area = raw.Area ?? string.Empty,
                ID = raw.ID ?? string.Empty,
                Text = raw.Text ?? string.Empty,
                Tags = ParseTags(raw.Tags)
            };
        }

        private List<string> ParseTags(string? tags)
        {
            if (string.IsNullOrEmpty(tags))
                return new List<string>();

            return tags.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private List<DS3MapSpec> GetDS3MapSpecs()
        {
            return new List<DS3MapSpec>
            {
                DS3MapSpec.Create("m30_00_00_00", "highwall", 400, 402),
                DS3MapSpec.Create("m30_01_00_00", "lothric", 400, 402),
                DS3MapSpec.Create("m31_00_00_00", "settlement", 400, 402),
                DS3MapSpec.Create("m32_00_00_00", "archdragon", 400, 402),
                DS3MapSpec.Create("m33_00_00_00", "farronkeep", 400, 402),
                DS3MapSpec.Create("m34_01_00_00", "archives", 400, 402),
                DS3MapSpec.Create("m35_00_00_00", "cathedral", 400, 402),
                DS3MapSpec.Create("m37_00_00_00", "irithyll", 400, 402),
                DS3MapSpec.Create("m38_00_00_00", "catacombs", 400, 402),
                DS3MapSpec.Create("m39_00_00_00", "dungeon", 400, 402),
                DS3MapSpec.Create("m40_00_00_00", "firelink", 400, 402),
                DS3MapSpec.Create("m41_00_00_00", "kiln", 400, 402),
                DS3MapSpec.Create("m45_00_00_00", "ariandel", 400, 402),
                DS3MapSpec.Create("m50_00_00_00", "dregheap", 400, 402),
                DS3MapSpec.Create("m51_00_00_00", "ringedcity", 400, 402),
                DS3MapSpec.Create("m51_01_00_00", "filianore", 400, 402),
            };
        }
    }
}
