using DynamicNetwork.Application.Dtos;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Infrastructure.Factories;

namespace DynamicNetwork.Tests;

[TestClass]
public class TemporalGraphFactoryTests
{
    [TestMethod]
    public void BuildGraphs_SplitsIntoActiveIntervals()
    {
        var rawLinks = new[]
        {
            new LinkParsingDto { NodeA = "A", NodeB = "B", Begin = 0, End = 5 },
            new LinkParsingDto { NodeA = "B", NodeB = "C", Begin = 3, End = 10 }
        };

        var factory = new TemporalGraphFactory();
        var graphs = factory.BuildGraphs(rawLinks);

        Assert.AreEqual(3, graphs.Count);
        Assert.AreEqual(new TimeInterval(0, 3), graphs[0].Interval);
        Assert.AreEqual(new TimeInterval(3, 5), graphs[1].Interval);
        Assert.AreEqual(new TimeInterval(5, 10), graphs[2].Interval);

        Assert.AreEqual(1, graphs[0].Links.Count);
        Assert.AreEqual("A", graphs[0].Links[0].NodeA);
        Assert.AreEqual("B", graphs[0].Links[0].NodeB);

        Assert.AreEqual(2, graphs[1].Links.Count);
        Assert.AreEqual(1, graphs[2].Links.Count);
    }

    [TestMethod]
    public void BuildGraphs_HandlesNonSortedRawLinksAndDuplicateNodes()
    {
        var rawLinks = new[]
        {
            new LinkParsingDto { NodeA = "C", NodeB = "D", Begin = 5, End = 15 },
            new LinkParsingDto { NodeA = "B", NodeB = "C", Begin = 0, End = 10 },
            new LinkParsingDto { NodeA = "C", NodeB = "D", Begin = 5, End = 15 }
        };

        var factory = new TemporalGraphFactory();
        var graphs = factory.BuildGraphs(rawLinks);

        Assert.AreEqual(3, graphs.Count);
        Assert.IsTrue(graphs[0].AllNetworkNodes.Contains("B"));
        Assert.IsTrue(graphs[0].AllNetworkNodes.Contains("C"));
        Assert.IsTrue(graphs[0].AllNetworkNodes.Contains("D"));
        Assert.AreEqual(1, graphs[0].Links.Count);
        Assert.AreEqual(3, graphs[1].Links.Count);
        Assert.AreEqual(2, graphs[2].Links.Count);
        Assert.AreEqual(new TimeInterval(0, 5), graphs[0].Interval);
        Assert.AreEqual(new TimeInterval(5, 10), graphs[1].Interval);
        Assert.AreEqual(new TimeInterval(10, 15), graphs[2].Interval);
    }
}
