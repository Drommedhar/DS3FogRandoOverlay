using DS3FogRandoOverlay.Services;
using System;

class Program
{
    static void Main()
    {
        var areaMapper = new AreaMapper();
        var spoilerParser = new SpoilerLogParser();

        // Test current map ID that we know is being detected
        string mapId = "m37_00_00_00";
        string? currentArea = areaMapper.GetCurrentAreaName(mapId, null);

        Console.WriteLine($"Map ID: {mapId}");
        Console.WriteLine($"Detected Area: {currentArea}");

        if (currentArea != null)
        {
            var possibleNames = areaMapper.GetPossibleSpoilerLogAreaNames(currentArea);
            Console.WriteLine($"Possible spoiler log area names:");
            foreach (var name in possibleNames)
            {
                Console.WriteLine($"  - {name}");
            }
        }

        // Test spoiler log parsing
        var spoilerData = spoilerParser.ParseLatestSpoilerLog();
        if (spoilerData != null)
        {
            Console.WriteLine($"\nSpoiler log loaded successfully!");
            Console.WriteLine($"Seed: {spoilerData.Seed}");
            Console.WriteLine($"Total areas: {spoilerData.Areas.Count}");

            // Look for Anor Londo connections
            foreach (var area in spoilerData.Areas)
            {
                foreach (var fogGate in area.FogGates)
                {
                    if (fogGate.FromArea.Contains("Anor Londo", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Found fog gate: {fogGate.FromArea} -> {fogGate.ToArea}");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("Failed to load spoiler log!");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
