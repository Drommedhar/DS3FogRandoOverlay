using DS3FogRandoOverlay.Services;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            RunTest();
        }

        [Test]
        public void TestEventsParser()
        {
            Console.WriteLine("=== DS3 Events Parser Test ===");

            try
            {
                var eventsParser = new EventsParser();
                
                // Test with some known fog gate IDs from the events.txt
                var testIds = new[]
                {
                    "o000401_4000", // Emma's Room to High Wall
                    "o000401_2000", // Vordt to High Wall  
                    "o000401_5000", // Oceiros to Consumed King's Gardens (but also used elsewhere)
                    "o000402_1000", // Different meanings in different maps
                    "nonexistent_id" // Test fallback
                };

                Console.WriteLine("Testing by fog gate ID only:");
                foreach (var fogGateId in testIds)
                {
                    var displayName = eventsParser.GetFogGateName(fogGateId);
                    Console.WriteLine($"  {fogGateId} -> {displayName}");
                }

                Console.WriteLine("\nTesting by entity ID + fog gate ID (more accurate):");
                
                // Test some specific entity ID + fog gate ID combinations
                var testCombinations = new[]
                {
                    (3001890, "o000401_4000"), // highwall - Emma's Room to High Wall
                    (3001800, "o000401_2000"), // highwall - Vordt to High Wall
                    (3001830, "o000401_5000"), // highwall - Oceiros to Consumed King's Gardens
                    (3301800, "o000401_5000"), // farronkeep - Abyss Watchers to Farron Keep (same fog gate ID, different meaning!)
                    (3101800, "o000402_1000"), // settlement - Cursed-rotted Greatwood to Undead Settlement
                    (3201800, "o000402_1000"), // archdragon - Ancient Wyvern to the start of Archdragon Peak (same fog gate ID, different meaning!)
                    (999999, "o000401_4000")   // Non-existent entity ID - should fallback to fog gate ID
                };

                foreach (var (entityId, fogGateId) in testCombinations)
                {
                    var displayName = eventsParser.GetFogGateNameByEntityId(entityId, fogGateId);
                    Console.WriteLine($"  Entity {entityId} + {fogGateId} -> {displayName}");
                }

                var allFogGateMappings = eventsParser.GetAllFogGateMappings();
                var allEntityMappings = eventsParser.GetAllEntityIdMappings();
                Console.WriteLine($"\nTotal fog gate ID mappings loaded: {allFogGateMappings.Count}");
                Console.WriteLine($"Total entity ID mappings loaded: {allEntityMappings.Count}");
                
                if (allEntityMappings.Count > 0)
                {
                    Console.WriteLine("Sample entity ID mappings:");
                    foreach (var mapping in allEntityMappings.Take(5))
                    {
                        Console.WriteLine($"  {mapping.Key} -> {mapping.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing events parser: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void RunTest()
        {
            Console.WriteLine("=== DS3 Fog Rando MSB Parser Test ===");

            try
            {
                // Initialize the MSB parser
                var msbParser = new MsbParser();

                Console.WriteLine("Parsing MSB files...");

                // Get all fog gates
                var allFogGates = msbParser.GetAllFogGates();

                Console.WriteLine($"Found fog gates in {allFogGates.Count} maps:");

                foreach (var mapEntry in allFogGates)
                {
                    var mapId = mapEntry.Key;
                    var fogGates = mapEntry.Value;

                    Console.WriteLine($"\n{mapId} ({GetAreaName(mapId)}):");
                    Console.WriteLine($"  Total fog gates found: {fogGates.Count}");

                    var bossFogs = fogGates.Where(fg => fg.IsBossFog).ToList();
                    var mainFogs = fogGates.Where(fg => fg.IsMainFog && !fg.IsBossFog).ToList();
                    var otherFogs = fogGates.Where(fg => !fg.IsBossFog && !fg.IsMainFog).ToList();

                    if (bossFogs.Any())
                    {
                        Console.WriteLine($"  Boss fog gates: {bossFogs.Count}");
                        foreach (var fog in bossFogs.Take(3)) // Show first 3
                        {
                            Console.WriteLine($"    - {fog.Name} at ({fog.Position.X:F1}, {fog.Position.Y:F1}, {fog.Position.Z:F1})");
                        }
                    }

                    if (mainFogs.Any())
                    {
                        Console.WriteLine($"  Main fog gates: {mainFogs.Count}");
                        foreach (var fog in mainFogs.Take(3)) // Show first 3
                        {
                            Console.WriteLine($"    - {fog.Name} at ({fog.Position.X:F1}, {fog.Position.Y:F1}, {fog.Position.Z:F1})");
                        }
                    }

                    if (otherFogs.Any())
                    {
                        Console.WriteLine($"  Other fog gates: {otherFogs.Count}");
                        foreach (var fog in otherFogs.Take(2)) // Show first 2
                        {
                            Console.WriteLine($"    - {fog.Name} at ({fog.Position.X:F1}, {fog.Position.Y:F1}, {fog.Position.Z:F1})");
                        }
                    }
                }

                // Test nearby fog gates functionality
                Console.WriteLine("\n=== Testing Nearby Fog Gates ===");
                if (allFogGates.Any())
                {
                    var firstMap = allFogGates.First();
                    var mapId = firstMap.Key;
                    var fogGates = firstMap.Value;

                    if (fogGates.Any())
                    {
                        var testPosition = fogGates.First().Position;
                        Console.WriteLine($"\nTesting nearby fog gates for map {mapId} around position ({testPosition.X:F1}, {testPosition.Y:F1}, {testPosition.Z:F1}):");

                        var nearbyFogs = msbParser.GetNearbyFogGates(mapId, testPosition, 50f);
                        Console.WriteLine($"Found {nearbyFogs.Count} fog gates within 50 units:");

                        foreach (var fog in nearbyFogs.Take(5))
                        {
                            var distance = System.Numerics.Vector3.Distance(testPosition, fog.Position);
                            Console.WriteLine($"  - {fog.Name} at distance {distance:F1}");
                        }
                    }
                }

                Console.WriteLine("\n=== MSB Parser Test Complete ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during MSB parsing test: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Get human-readable area name from map ID
        /// </summary>
        private static string GetAreaName(string mapId)
        {
            return mapId switch
            {
                "m30_00_00_00" => "Firelink Shrine",
                "m30_01_00_00" => "Cemetery of Ash",
                "m31_00_00_00" => "High Wall of Lothric",
                "m32_00_00_00" => "Undead Settlement",
                "m33_00_00_00" => "Road of Sacrifices / Crucifixion Woods",
                "m34_01_00_00" => "Cathedral of the Deep",
                "m35_00_00_00" => "Farron Keep",
                "m37_00_00_00" => "Catacombs of Carthus / Smouldering Lake",
                "m38_00_00_00" => "Irithyll of the Boreal Valley",
                "m39_00_00_00" => "Irithyll Dungeon / Profaned Capital",
                "m40_00_00_00" => "Anor Londo",
                "m41_00_00_00" => "Lothric Castle",
                "m45_00_00_00" => "The Grand Archives",
                "m50_00_00_00" => "Untended Graves",
                "m51_00_00_00" => "Archdragon Peak",
                "m51_01_00_00" => "Kiln of the First Flame",
                _ => "Unknown Area"
            };
        }
    }
}