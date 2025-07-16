using System;
using System.Collections.Generic;
using System.Linq;
using DS3FogRandoOverlay.Models;
using System.Linq;
using DS3Parser.Models;

namespace DS3FogRandoOverlay.Services
{
    public class AreaMapper
    {
        private readonly Dictionary<string, string> mapIdToAreaName;

        public AreaMapper()
        {
            mapIdToAreaName = InitializeAreaMappings();
        }

        private Dictionary<string, string> InitializeAreaMappings()
        {
            return new Dictionary<string, string>
            {
                // Main Game Areas (Based on official DS3 map list)
                
                // m30_00_00_00 - High Wall of Lothric
                { "m30_00_00_00", "High Wall of Lothric" },
                
                // m30_01_00_00 - Lothric Castle, Consumed King's Garden
                { "m30_01_00_00", "Lothric Castle" },
                
                // m31_00_00_00 - Undead Settlement
                { "m31_00_00_00", "Undead Settlement" },
                
                // m32_00_00_00 - Archdragon Peak  
                { "m32_00_00_00", "Archdragon Peak" },
                
                // m33_00_00_00 - Road of Sacrifices, Farron Keep
                { "m33_00_00_00", "Road of Sacrifices" },
                
                // m34_00_00_00 - Unused/Unknown
                { "m34_00_00_00", "Unknown Area" },
                
                // m34_01_00_00 - Grand Archives
                { "m34_01_00_00", "Grand Archives" },
                
                // m35_00_00_00 - Cathedral of the Deep
                { "m35_00_00_00", "Cathedral of the Deep" },
                
                // m37_00_00_00 - Irithyll of the Boreal Valley, Anor Londo
                { "m37_00_00_00", "Irithyll of the Boreal Valley" },
                
                // m38_00_00_00 - Catacombs of Carthus, Smouldering Lake
                { "m38_00_00_00", "Catacombs of Carthus" },
                
                // m39_00_00_00 - Irithyll Dungeon, Profaned Capital
                { "m39_00_00_00", "Irithyll Dungeon" },
                
                // m40_00_00_00 - Cemetery of Ash, Firelink Shrine, Untended Graves
                { "m40_00_00_00", "Cemetery of Ash" },
                
                // m41_00_00_00 - Kiln of the First Flame, Flameless Shrine
                { "m41_00_00_00", "Kiln of the First Flame" },
                
                // DLC1 - Ashes of Ariandel
                // m45_00_00_00 - Painted World of Ariandel
                { "m45_00_00_00", "Painted World of Ariandel" },
                
                // DLC1 - PvP Arenas
                // m46_00_00_00 - Arena - Grand Roof
                { "m46_00_00_00", "Arena - Grand Roof" },
                
                // m47_00_00_00 - Arena - Kiln of Flame
                { "m47_00_00_00", "Arena - Kiln of Flame" },
                
                // DLC2 - The Ringed City
                // m50_00_00_00 - Dreg Heap
                { "m50_00_00_00", "The Dreg Heap" },
                
                // m51_00_00_00 - The Ringed City
                { "m51_00_00_00", "The Ringed City" },
                
                // m51_01_00_00 - Filianore's Rest
                { "m51_01_00_00", "Filianore's Rest" },
                
                // m53_00_00_00 - Arena - Dragon Ruins
                { "m53_00_00_00", "Arena - Dragon Ruins" },
                
                // m54_00_00_00 - Arena - Round Plaza
                { "m54_00_00_00", "Arena - Round Plaza" },
                
                // Additional common area mappings for backwards compatibility
                // These help match spoiler log area names
                { "Cemetery of Ash", "Cemetery of Ash" },
                { "Firelink Shrine", "Cemetery of Ash" },
                { "Untended Graves", "Cemetery of Ash" },
                { "Iudex Gundyr", "Cemetery of Ash" },
                { "Champion Gundyr", "Cemetery of Ash" },
                { "High Wall of Lothric", "High Wall of Lothric" },
                { "Vordt of the Boreal Valley", "High Wall of Lothric" },
                { "Dancer of the Boreal Valley", "High Wall of Lothric" },
                { "Undead Settlement", "Undead Settlement" },
                { "Curse-rotted Greatwood", "Undead Settlement" },
                { "Road of Sacrifices", "Road of Sacrifices" },
                { "Farron Keep", "Road of Sacrifices" },
                { "Abyss Watchers", "Road of Sacrifices" },
                { "Cathedral of the Deep", "Cathedral of the Deep" },
                { "Deacons of the Deep", "Cathedral of the Deep" },
                { "Catacombs of Carthus", "Catacombs of Carthus" },
                { "Smouldering Lake", "Catacombs of Carthus" },
                { "Old Demon King", "Catacombs of Carthus" },
                { "Irithyll of the Boreal Valley", "Irithyll of the Boreal Valley" },
                { "Anor Londo", "Irithyll of the Boreal Valley" },
                { "Pontiff Sulyvahn", "Irithyll of the Boreal Valley" },
                { "Aldrich, Devourer of Gods", "Irithyll of the Boreal Valley" },
                { "Irithyll Dungeon", "Irithyll Dungeon" },
                { "Profaned Capital", "Irithyll Dungeon" },
                { "Yhorm the Giant", "Irithyll Dungeon" },
                { "Lothric Castle", "Lothric Castle" },
                { "Consumed King's Garden", "Lothric Castle" },
                { "Oceiros, the Consumed King", "Lothric Castle" },
                { "Grand Archives", "Grand Archives" },
                { "Twin Princes", "Grand Archives" },
                { "Archdragon Peak", "Archdragon Peak" },
                { "Ancient Wyvern", "Archdragon Peak" },
                { "Nameless King", "Archdragon Peak" },
                { "Kiln of the First Flame", "Kiln of the First Flame" },
                { "Soul of Cinder", "Kiln of the First Flame" },
                { "Painted World of Ariandel", "Painted World of Ariandel" },
                { "Sister Friede", "Painted World of Ariandel" },
                { "The Dreg Heap", "The Dreg Heap" },
                { "Demon Prince", "The Dreg Heap" },
                { "The Ringed City", "The Ringed City" },
                { "Halflight, Spear of the Church", "The Ringed City" },
                { "Darkeater Midir", "The Ringed City" },
                { "Slave Knight Gael", "Filianore's Rest" }
            };
        }

        public string? GetCurrentAreaName(string? mapId, Vector3? position)
        {
            if (string.IsNullOrEmpty(mapId))
                return null;

            // Direct map ID lookup
            if (mapIdToAreaName.TryGetValue(mapId, out string? areaName))
            {
                return areaName;
            }

            // Try to match partial map IDs for unknown variants
            foreach (var kvp in mapIdToAreaName)
            {
                if (mapId.StartsWith(kvp.Key.Substring(0, Math.Min(kvp.Key.Length, 8))))
                {
                    return kvp.Value;
                }
            }

            // Return the raw map ID if no mapping found
            return $"Unknown Area ({mapId})";
        }

        public string? GetSpoilerLogAreaName(string currentAreaName)
        {
            // Convert from our internal area names to spoiler log format
            // The spoiler log uses more specific names that match the fog gate descriptions

            switch (currentAreaName)
            {
                case "Cemetery of Ash":
                    return "Cemetery of Ash"; // Could also be "Firelink Shrine", "Iudex Gundyr", etc.
                case "High Wall of Lothric":
                    return "High Wall of Lothric";
                case "Undead Settlement":
                    return "Undead Settlement";
                case "Road of Sacrifices":
                    return "Road of Sacrifices"; // Could also be "Farron Keep"
                case "Cathedral of the Deep":
                    return "Cathedral of the Deep";
                case "Catacombs of Carthus":
                    return "Catacombs of Carthus"; // Could also be "Smouldering Lake"
                case "Irithyll of the Boreal Valley":
                    return "Anor Londo"; // The spoiler log uses "Anor Londo" for this map area
                case "Irithyll Dungeon":
                    return "Irithyll Dungeon"; // Could also be "Profaned Capital"
                case "Lothric Castle":
                    return "Lothric Castle"; // Could also be "Consumed King's Garden"
                case "Grand Archives":
                    return "Grand Archives";
                case "Archdragon Peak":
                    return "Archdragon Peak";
                case "Kiln of the First Flame":
                    return "Kiln of the First Flame";
                case "Painted World of Ariandel":
                    return "Painted World of Ariandel";
                case "The Dreg Heap":
                    return "The Dreg Heap";
                case "The Ringed City":
                    return "The Ringed City";
                case "Filianore's Rest":
                    return "Filianore's Rest";
                default:
                    return currentAreaName;
            }
        }

        public List<string> GetPossibleSpoilerLogAreaNames(string currentAreaName)
        {
            // Return all possible area names that could match in the spoiler log
            // Based on fog.txt configuration file area mappings
            switch (currentAreaName)
            {
                case "Cemetery of Ash":
                    return new List<string> {
                        "Cemetery of Ash",
                        "Firelink Shrine",
                        "Iudex Gundyr",
                        "before Firelink Shrine",
                        "Firelink Shrine with Coiled Sword",
                        "before Firelink Bell Tower",
                        "Firelink Shrine Roof",
                        "Firelink Bell Tower"
                    };

                case "High Wall of Lothric":
                    return new List<string> {
                        "High Wall",
                        "High Wall of Lothric",
                        "Emma's Cathedral",
                        "High Wall Lift Cell",
                        "Greirat's Cell",
                        "Vordt of the Boreal Valley",
                        "Dancer of the Boreal Valley",
                        "above Emma",
                        "above Emma from Lothric Castle bonfire"
                    };

                case "Undead Settlement":
                    return new List<string> {
                        "Undead Settlement",
                        "Undead Settlement Tower",
                        "Curse-Rotted Greatwood",
                        "Pit of Hollows"
                    };

                case "Road of Sacrifices":
                    return new List<string> {
                        "Road of Sacrifices",
                        "Road of Sacrifices start",
                        "Farron Keep",
                        "Crystal Sage",
                        "before Abyss Watchers",
                        "Abyss Watchers",
                        "after Abyss Watchers"
                    };

                case "Cathedral of the Deep":
                    return new List<string> {
                        "Cathedral of the Deep",
                        "Cathedral of the Deep Start",
                        "Rosaria's Room",
                        "Deacons of the Deep"
                    };

                case "Catacombs of Carthus":
                    return new List<string> {
                        "Catacombs of Carthus",
                        "Catacombs Wolnir Room",
                        "High Lord Wolnir",
                        "before Smouldering Lake",
                        "Smouldering Lake",
                        "Old Demon King"
                    };

                case "Irithyll of the Boreal Valley":
                    return new List<string> {
                        "before the bridge into Irithyll",
                        "the bridge into Irithyll",
                        "Central Irithyll",
                        "above Dorhys in Irithyll",
                        "Dorhys in Irithyll",
                        "Irithyll Distant Manor",
                        "Pontiff Sulyvahn",
                        "Yorshka's Prison Tower in Irithyll",
                        "immediately after Pontiff Sulyvahn",
                        "Anor Londo",
                        "above Pontiff",
                        "Aldrich, Devourer of Gods",
                        "above Aldrich",
                        "left elevator after Aldrich",
                        "right elevator after Aldrich"
                    };

                case "Irithyll Dungeon":
                    return new List<string> {
                        "Irithyll Dungeon",
                        "Irithyll Dungeon with Jailbreaker's Key",
                        "Irithyll Dungeon Jailer's Cells",
                        "Profaned Capital",
                        "Irithyll Dungeon Old Cell",
                        "Yhorm the Giant"
                    };

                case "Lothric Castle":
                    return new List<string> {
                        "Lothric Castle",
                        "Dragonslayer Armour",
                        "after Dragonslayer Armour",
                        "Consumed King's Gardens",
                        "Oceiros, the Consumed King"
                    };

                case "Grand Archives":
                    return new List<string> {
                        "Grand Archives",
                        "Grand Archives Bonfire",
                        "Twin Princes"
                    };

                case "Archdragon Peak":
                    return new List<string> {
                        "Archdragon Peak",
                        "Central Archdragon Peak",
                        "Ancient Wyvern",
                        "Nameless King",
                        "below Nameless King"
                    };

                case "Kiln of the First Flame":
                    return new List<string> {
                        "Kiln of the First Flame",
                        "Soul of Cinder"
                    };

                case "Painted World of Ariandel":
                    return new List<string> {
                        "Painted World of Ariandel",
                        "Sister Friede"
                    };

                case "The Dreg Heap":
                    return new List<string> {
                        "The Dreg Heap",
                        "Demon Prince"
                    };

                case "The Ringed City":
                    return new List<string> {
                        "The Ringed City",
                        "before Darkeater Midir",
                        "Darkeater Midir",
                        "after Halflight",
                        "Halflight, Spear of the Church"
                    };

                case "Filianore's Rest":
                    return new List<string> {
                        "Filianore's Rest",
                        "Slave Knight Gael"
                    };

                default:
                    return new List<string> { currentAreaName };
            }
        }

        public List<string> GetAllAreaNames()
        {
            return new List<string>(mapIdToAreaName.Values);
        }

        public string? GetMapIdForArea(string areaName)
        {
            foreach (var kvp in mapIdToAreaName)
            {
                if (kvp.Value.Equals(areaName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// Try to match an MSB fog gate with known area mapper locations
        /// </summary>
        public string? TryMatchMsbFogGateToLocation(MsbFogGate fogGate, string currentAreaName)
        {
            // First try entity ID matching with known fog gate mappings from events.txt
            var entityMatch = GetLocationByEntityId(fogGate.EntityId, fogGate.MapId);
            if (!string.IsNullOrEmpty(entityMatch))
            {
                return entityMatch;
            }

            var possibleLocations = GetPossibleSpoilerLogAreaNames(currentAreaName);
            
            // Try to match by name patterns
            var gateName = fogGate.Name.ToLower();
            var modelName = fogGate.ModelName.ToLower();
            
            foreach (var location in possibleLocations)
            {
                var locationLower = location.ToLower();
                
                // Direct name matching
                if (gateName.Contains(locationLower.Split(' ')[0]) || 
                    locationLower.Contains(gateName.Replace("_", " ")))
                {
                    return location;
                }
                
                // Boss name matching
                if (fogGate.IsBossFog)
                {
                    // Look for boss names in the location
                    var bossKeywords = new[] { "boss", "lord", "prince", "king", "wyvern", "watchers", "sage", "deacons", "pontiff", "aldrich", "yhorm", "dancer", "armour", "friede", "demon", "midir", "gael", "cinder", "champion", "vordt", "greatwood", "sulyvahn", "nameless", "oceiros" };
                    
                    if (bossKeywords.Any(keyword => locationLower.Contains(keyword) || gateName.Contains(keyword)))
                    {
                        return location;
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Get location name based on entity ID and map ID from events.txt data
        /// </summary>
        private string? GetLocationByEntityId(int entityId, string mapId)
        {
            // Create mapping database based on events.txt analysis
            var entityMappings = GetFogGateEntityMappings();
            
            var key = $"{mapId}_{entityId}";
            if (entityMappings.TryGetValue(key, out string? location))
            {
                return location;
            }
            
            // Try just entity ID if map-specific lookup fails
            if (entityMappings.TryGetValue(entityId.ToString(), out location))
            {
                return location;
            }
            
            return null;
        }

        /// <summary>
        /// Database of fog gate entity IDs to location mappings based on events.txt
        /// </summary>
        private Dictionary<string, string> GetFogGateEntityMappings()
        {
            return new Dictionary<string, string>
            {
                // High Wall of Lothric (m30_00_00_00)
                { "m30_00_00_00_3001800", "Vordt of the Boreal Valley" },
                { "m30_00_00_00_3001815", "High Wall" },
                { "m30_00_00_00_3001890", "Dancer of the Boreal Valley" },
                { "m30_00_00_00_3001895", "above Emma" },
                
                // Undead Settlement (m31_00_00_00) 
                { "m31_00_00_00_3101800", "Curse-Rotted Greatwood" },
                { "m31_00_00_00_3101813", "Undead Settlement" },
                
                // Road of Sacrifices (m33_00_00_00)
                { "m33_00_00_00_3301800", "Crystal Sage" },
                { "m33_00_00_00_3301850", "Abyss Watchers" },
                
                // Cathedral of the Deep (m35_00_00_00)
                { "m35_00_00_00_3501800", "Deacons of the Deep" },
                
                // Catacombs of Carthus (m38_00_00_00)
                { "m38_00_00_00_3801800", "High Lord Wolnir" },
                { "m38_00_00_00_3801830", "Old Demon King" },
                
                // Irithyll of the Boreal Valley (m37_00_00_00)
                { "m37_00_00_00_3701800", "Pontiff Sulyvahn" },
                { "m37_00_00_00_3701850", "Aldrich, Devourer of Gods" },
                
                // Irithyll Dungeon (m39_00_00_00)
                { "m39_00_00_00_3901800", "Yhorm the Giant" },
                
                // Lothric Castle (m30_01_00_00)
                { "m30_01_00_00_4001800", "Dragonslayer Armour" },
                { "m30_01_00_00_4101800", "Twin Princes" },
                
                // Grand Archives (m34_01_00_00)
                { "m34_01_00_00_4101800", "Twin Princes" },
                
                // Archdragon Peak (m32_00_00_00)
                { "m32_00_00_00_3201800", "Ancient Wyvern" },
                { "m32_00_00_00_3201786", "Nameless King" },
                
                // Kiln of the First Flame (m41_00_00_00)
                { "m41_00_00_00_4101800", "Soul of Cinder" },
                
                // Painted World of Ariandel (m45_00_00_00)
                { "m45_00_00_00_4501800", "Sister Friede" },
                { "m45_00_00_00_4501860", "Sister Friede" },
                
                // The Dreg Heap (m50_00_00_00)
                { "m50_00_00_00_5001800", "Demon Prince" },
                
                // The Ringed City (m51_00_00_00)
                { "m51_00_00_00_5101800", "Halflight, Spear of the Church" },
                { "m51_00_00_00_5101850", "Darkeater Midir" },
                
                // Filianore's Rest (m51_01_00_00)
                { "m51_01_00_00_5111800", "Slave Knight Gael" },
                { "m51_01_00_00_5111850", "Slave Knight Gael" },
                
                // General fog gate patterns (fallback)
                { "3001800", "Boss Fog Gate" },
                { "3101800", "Boss Fog Gate" },
                { "3201800", "Boss Fog Gate" },
                { "3301800", "Boss Fog Gate" },
                { "3501800", "Boss Fog Gate" },
                { "3701800", "Boss Fog Gate" },
                { "3801800", "Boss Fog Gate" },
                { "3901800", "Boss Fog Gate" },
                { "4001800", "Boss Fog Gate" },
                { "4101800", "Boss Fog Gate" },
                { "4501800", "Boss Fog Gate" },
                { "5001800", "Boss Fog Gate" },
                { "5101800", "Boss Fog Gate" },
                { "5111800", "Boss Fog Gate" },
                
                // Non-boss fog gates
                { "3001815", "High Wall transition" },
                { "3001890", "Dancer trigger" },
                { "3001895", "Emma's Cathedral" },
                { "3101813", "Settlement transition" },
                { "3301850", "Farron transition" },
                { "3801830", "Smouldering Lake" },
                { "3701850", "Anor Londo" },
                { "4501860", "Ariandel transition" },
                { "5101850", "Ringed City transition" },
                { "5111850", "Filianore transition" }
            };
        }

        /// <summary>
        /// Get enhanced fog gate information with distance and location matching
        /// </summary>
        public List<EnhancedFogGateInfo> GetEnhancedFogGateInfo(List<MsbFogGate> fogGates, string currentAreaName)
        {
            return GetEnhancedFogGateInfo(fogGates, currentAreaName, null);
        }

        /// <summary>
        /// Get enhanced fog gate information with distance and location matching, using events parser for better names
        /// </summary>
        public List<EnhancedFogGateInfo> GetEnhancedFogGateInfo(List<MsbFogGate> fogGates, string currentAreaName, EventsParser? eventsParser)
        {
            var enhancedInfo = new List<EnhancedFogGateInfo>();
            
            foreach (var fogGate in fogGates)
            {
                var enhancedGate = CreateEnhancedFogGateInfo(fogGate, currentAreaName, eventsParser);
                enhancedInfo.Add(enhancedGate);
            }
            
            return enhancedInfo.OrderBy(info => info.Distance).ToList();
        }

        /// <summary>
        /// Determine if a fog gate is a boss fog gate based on entity ID patterns
        /// </summary>
        public bool IsBossFogGate(int entityId, string mapId)
        {
            // Boss fog gates typically end with specific patterns
            var bossEntityIds = new HashSet<int>
            {
                // Main boss fog gates (ending in 800)
                3001800, 3101800, 3201800, 3301800, 3501800, 
                3701800, 3801800, 3901800, 4001800, 4101800,
                4501800, 5001800, 5101800, 5111800,
                
                // Secondary boss fog gates
                3001890, 3201786, 3301850, 3701850, 3801830,
                4501860, 5101850, 5111850
            };
            
            return bossEntityIds.Contains(entityId);
        }

        /// <summary>
        /// Get comprehensive fog gate information including boss status
        /// </summary>
        public EnhancedFogGateInfo CreateEnhancedFogGateInfo(MsbFogGate fogGate, string currentAreaName)
        {
            return CreateEnhancedFogGateInfo(fogGate, currentAreaName, null);
        }

        /// <summary>
        /// Get comprehensive fog gate information including boss status with events parser for better names
        /// </summary>
        public EnhancedFogGateInfo CreateEnhancedFogGateInfo(MsbFogGate fogGate, string currentAreaName, EventsParser? eventsParser)
        {
            var matchedLocation = TryMatchMsbFogGateToLocation(fogGate, currentAreaName);
            var isBoss = IsBossFogGate(fogGate.EntityId, fogGate.MapId);
            
            // Update the fog gate's boss status if we detected it
            if (isBoss && !fogGate.IsBossFog)
            {
                fogGate.IsBossFog = true;
            }

            // Try to get a better name from the events parser
            if (eventsParser != null && !string.IsNullOrEmpty(fogGate.Name))
            {
                var betterName = eventsParser.GetFogGateNameByEntityId(fogGate.EntityId, fogGate.Name);
                if (betterName != fogGate.Name)
                {
                    // Create a copy of the fog gate with the better name
                    var enhancedFogGate = new MsbFogGate
                    {
                        Name = betterName,
                        EntityId = fogGate.EntityId,
                        MapId = fogGate.MapId,
                        Position = fogGate.Position,
                        Rotation = fogGate.Rotation,
                        IsBossFog = isBoss,
                        IsMainFog = fogGate.IsMainFog,
                        DistanceToPlayer = fogGate.DistanceToPlayer
                    };
                    fogGate = enhancedFogGate;
                }
            }
            
            return new EnhancedFogGateInfo
            {
                FogGate = fogGate,
                MatchedLocationName = matchedLocation,
                Distance = fogGate.DistanceToPlayer ?? 0,
                IsLocationMatched = !string.IsNullOrEmpty(matchedLocation)
            };
        }
    }
}
