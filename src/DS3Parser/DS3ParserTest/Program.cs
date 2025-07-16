using System;
using DS3Parser;
using DS3Parser.Models;

namespace DS3ParserTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new DS3Parser.DS3Parser();
            
            // Test with the known game directory
            var gameDirectory = @"c:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III\Game";
            
            Console.WriteLine($"Testing DS3Parser with directory: {gameDirectory}");
            Console.WriteLine($"Directory exists: {System.IO.Directory.Exists(gameDirectory)}");
            
            try
            {
                var paths = parser.GetExpectedPaths(gameDirectory);
                Console.WriteLine($"Expected paths:");
                Console.WriteLine($"  Fog directory: {paths.FogDirectory} (exists: {System.IO.Directory.Exists(paths.FogDirectory)})");
                Console.WriteLine($"  FogDist directory: {paths.FogDistDirectory} (exists: {System.IO.Directory.Exists(paths.FogDistDirectory)})");
                Console.WriteLine($"  Fog file: {paths.FogFile} (exists: {System.IO.File.Exists(paths.FogFile)})");
                Console.WriteLine($"  Spoiler log directory: {paths.SpoilerLogDirectory} (exists: {System.IO.Directory.Exists(paths.SpoilerLogDirectory)})");
                
                Console.WriteLine($"Has fog randomizer data: {parser.HasFogRandomizerData(gameDirectory)}");
                
                if (parser.HasFogRandomizerData(gameDirectory))
                {
                    Console.WriteLine("Attempting to parse fog randomizer data...");
                    var data = parser.ParseFromGameDirectory(gameDirectory);
                    
                    Console.WriteLine($"Parsing successful!");
                    Console.WriteLine($"  Fog distribution: {data.FogDistribution != null}");
                    Console.WriteLine($"  Spoiler log: {data.SpoilerLog != null}");
                    
                    if (data.FogDistribution != null)
                    {
                        Console.WriteLine($"  - Areas: {data.FogDistribution.Areas.Count}");
                        Console.WriteLine($"  - Entrances: {data.FogDistribution.Entrances.Count}");
                        Console.WriteLine($"  - Warps: {data.FogDistribution.Warps.Count}");
                    }
                    
                    if (data.SpoilerLog != null)
                    {
                        Console.WriteLine($"  - Seed: {data.SpoilerLog.Seed}");
                        Console.WriteLine($"  - Connections: {data.SpoilerLog.Connections.Count}");
                    }
                }
                else
                {
                    Console.WriteLine("No fog randomizer data found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
