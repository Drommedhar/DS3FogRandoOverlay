using System;
using DS3FogRandoOverlay.Services;

namespace FogGateServiceTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing FogGateService...");
            
            var configService = new ConfigurationService();
            var fogGateService = new FogGateService(configService);
            
            Console.WriteLine($"DS3 Path from config: {configService.Config.DarkSouls3Path}");
            Console.WriteLine($"Directory exists: {System.IO.Directory.Exists(configService.Config.DarkSouls3Path)}");
            
            // Test the Game subdirectory path
            var gameDir = configService.Config.DarkSouls3Path;
            if (!gameDir.EndsWith("Game", StringComparison.OrdinalIgnoreCase))
            {
                gameDir = System.IO.Path.Combine(gameDir, "Game");
            }
            Console.WriteLine($"Game directory: {gameDir}");
            Console.WriteLine($"Game directory exists: {System.IO.Directory.Exists(gameDir)}");
            
            // Test fog randomizer data detection
            Console.WriteLine($"Has fog randomizer data: {fogGateService.HasFogRandomizerData()}");
            
            // Test seed retrieval
            var seed = fogGateService.GetCurrentSeed();
            Console.WriteLine($"Current seed: {seed ?? "null"}");
            
            // Test area retrieval
            var areas = fogGateService.GetAllAreas();
            Console.WriteLine($"Total areas: {areas.Count}");
            foreach (var area in areas.Take(10))
            {
                Console.WriteLine($"  - {area}");
            }
            if (areas.Count > 10)
            {
                Console.WriteLine($"  ... and {areas.Count - 10} more");
            }
            
            // Test fog gates in a specific area
            if (areas.Count > 0)
            {
                var testArea = areas.First();
                var fogGates = fogGateService.GetFogGatesInArea(testArea);
                var warps = fogGateService.GetWarpsInArea(testArea);
                Console.WriteLine($"Test area '{testArea}': {fogGates.Count} fog gates, {warps.Count} warps");
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
