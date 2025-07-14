using System;
using System.Collections.Generic;

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
    }
}
