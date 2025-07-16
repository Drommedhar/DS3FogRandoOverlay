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
    public void Parser_CombineGateIdsWithObject()
    {
        var parser = new DS3Parser.DS3Parser();
        Assert.That(parser, Is.Not.Null);
        
        var data = parser.ParseFromGameDirectory(_testGameDirectory);
        
       DS3Combiner combiner = new DS3Combiner();
       combiner.CombineData(_testGameDirectory, data);
    }
}