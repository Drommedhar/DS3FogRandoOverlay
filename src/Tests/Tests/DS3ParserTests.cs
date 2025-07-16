using DS3Parser;
using DS3Parser.Models;
using DS3Parser.Services;

namespace Tests;

public class DS3ParserTests
{
    private readonly string _testGameDirectory = @"c:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III\Game";

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Parser_CanBeCreated()
    {
        var parser = new DS3Parser.DS3Parser();
        Assert.That(parser, Is.Not.Null);
    }

    [Test]
    public void Parser_CanCheckForFogRandomizerData()
    {
        var parser = new DS3Parser.DS3Parser();
        
        // This should work if the game directory exists and has fog randomizer installed
        bool hasFogData = parser.HasFogRandomizerData(_testGameDirectory);
        
        // We don't assert true/false here since it depends on the actual game installation
        // Just verify the method doesn't throw
        Assert.DoesNotThrow(() => parser.HasFogRandomizerData(_testGameDirectory));
    }

    [Test]
    public void Parser_CanGetExpectedPaths()
    {
        var parser = new DS3Parser.DS3Parser();
        var paths = parser.GetExpectedPaths(_testGameDirectory);
        
        Assert.That(paths, Is.Not.Null);
        Assert.That(paths.GameDirectory, Is.EqualTo(_testGameDirectory));
        Assert.That(paths.FogFile, Does.EndWith("fog.txt"));
        Assert.That(paths.LocationsFile, Does.EndWith("locations.txt"));
        Assert.That(paths.EventsFile, Does.EndWith("events.txt"));
        Assert.That(paths.FogModExecutable, Does.EndWith("FogMod.exe"));
    }

    [Test]
    public void Parser_CanFindSpoilerLogFiles()
    {
        var parser = new DS3Parser.DS3Parser();
        var spoilerLogs = parser.FindSpoilerLogFiles(_testGameDirectory);
        
        Assert.That(spoilerLogs, Is.Not.Null);
        // Should return empty list if no spoiler logs found, not throw
    }

    [Test]
    public void FogDistributionParser_CanParseFromGameDirectory()
    {
        var parser = new DS3FogDistributionParser();
        
        // Skip test if fog randomizer is not installed
        var fogFile = Path.Combine(_testGameDirectory, "fog", "fogdist", "fog.txt");
        if (!File.Exists(fogFile))
        {
            Assert.Ignore("Fog randomizer not installed in test game directory");
        }
        
        var fogDistribution = parser.ParseFromGameDirectory(_testGameDirectory);
        
        Assert.That(fogDistribution, Is.Not.Null);
        Assert.That(fogDistribution.Areas, Is.Not.Empty);
        Assert.That(fogDistribution.Entrances, Is.Not.Empty);
        Assert.That(fogDistribution.Warps, Is.Not.Empty);
        Assert.That(fogDistribution.MapSpecs, Is.Not.Empty);
        
        // Verify some expected areas exist
        var firelinkArea = fogDistribution.GetArea("firelink");
        Assert.That(firelinkArea, Is.Not.Null);
        Assert.That(firelinkArea.Name, Is.EqualTo("firelink"));
        
        var highwallArea = fogDistribution.GetArea("highwall");
        Assert.That(highwallArea, Is.Not.Null);
        Assert.That(highwallArea.Name, Is.EqualTo("highwall"));
    }

    [Test]
    public void SpoilerLogParser_CanParseFromGameDirectory()
    {
        var parser = new DS3SpoilerLogParser();
        
        // This will return null if no spoiler log exists, which is fine for testing
        var spoilerLog = parser.ParseFromGameDirectory(_testGameDirectory);
        
        if (spoilerLog != null)
        {
            Assert.That(spoilerLog.Seed, Is.GreaterThan(0));
            Assert.That(spoilerLog.Options, Is.Not.Empty);
            Assert.That(spoilerLog.Entries, Is.Not.Empty);
            
            // Verify some basic structure
            var firelinkEntry = spoilerLog.GetEntry("Firelink Shrine");
            if (firelinkEntry != null)
            {
                Assert.That(firelinkEntry.ScalingPercentage, Is.GreaterThan(0));
            }
        }
    }

    [Test]
    public void DS3Parser_CanParseFullGameData()
    {
        var parser = new DS3Parser.DS3Parser();
        
        // Skip test if fog randomizer is not installed
        if (!parser.HasFogRandomizerData(_testGameDirectory))
        {
            Assert.Ignore("Fog randomizer not installed in test game directory");
        }
        
        var data = parser.ParseFromGameDirectory(_testGameDirectory);
        
        Assert.That(data, Is.Not.Null);
        Assert.That(data.FogDistribution, Is.Not.Null);
        Assert.That(data.GameDirectory, Is.EqualTo(_testGameDirectory));
        
        // Verify we have the expected data
        Assert.That(data.GetAreas(), Is.Not.Empty);
        Assert.That(data.GetFogGates(), Is.Not.Empty);
        Assert.That(data.GetWarps(), Is.Not.Empty);
        
        // Check if randomized (spoiler log exists)
        if (data.IsRandomized)
        {
            Assert.That(data.SpoilerLog, Is.Not.Null);
            Assert.That(data.GetConnections(), Is.Not.Empty);
        }
    }

    [Test]
    public void DS3FogGate_HasCorrectProperties()
    {
        var fogGate = new DS3FogGate
        {
            Id = 3001800,
            Name = "o000401_2000",
            Area = "highwall",
            Text = "between Vordt and High Wall",
            Tags = new List<string> { "boss" },
            ASide = new DS3FogSide { Area = "highwall_vordt" },
            BSide = new DS3FogSide { Area = "highwall" }
        };
        
        Assert.That(fogGate.FullName, Is.EqualTo("highwall_3001800"));
        Assert.That(fogGate.IsBoss, Is.True);
        Assert.That(fogGate.IsDoor, Is.False);
        Assert.That(fogGate.IsWarp, Is.False);
        
        var sides = fogGate.GetSides();
        Assert.That(sides, Has.Count.EqualTo(2));
        Assert.That(sides[0].Area, Is.EqualTo("highwall_vordt"));
        Assert.That(sides[1].Area, Is.EqualTo("highwall"));
    }

    [Test]
    public void DS3Warp_HasCorrectProperties()
    {
        var warp = new DS3Warp
        {
            Id = 3000980,
            Area = "firelink",
            Text = "warp location added after Coiled Sword",
            Tags = new List<string> { "unique", "dlc1" },
            ASide = new DS3FogSide { Area = "firelink" },
            BSide = new DS3FogSide { Area = "highwall" }
        };
        
        Assert.That(warp.FullName, Is.EqualTo("firelink_3000980"));
        Assert.That(warp.IsUnique, Is.True);
        Assert.That(warp.IsDLC, Is.True);
        Assert.That(warp.IsKiln, Is.False);
        
        var sides = warp.GetSides();
        Assert.That(sides, Has.Count.EqualTo(2));
    }

    [Test]
    public void DS3Area_HasCorrectProperties()
    {
        var area = new DS3Area
        {
            Name = "firelink_iudexgundyr",
            Text = "Iudex Gundyr",
            Tags = new List<string> { "boss" },
            DefeatFlag = 14000800,
            TrapFlag = 14000801,
            BossTrigger = 14005805
        };
        
        Assert.That(area.IsBoss, Is.True);
        Assert.That(area.IsSmall, Is.False);
        Assert.That(area.IsTrivial, Is.False);
        Assert.That(area.IsDLC, Is.False);
    }

    [Test]
    public void DS3FogSide_HasCorrectProperties()
    {
        var side = new DS3FogSide
        {
            Area = "highwall_vordt",
            Text = "after defeating Vordt",
            Tags = new List<string> { "player", "instawarp" },
            BossDefeatName = "area",
            BossTrapName = "area",
            Cutscene = 31000010,
            Condition = "smalllothricbanner"
        };
        
        Assert.That(side.HasPlayerSpawn, Is.True);
        Assert.That(side.IsInstaWarp, Is.True);
    }
}