using DynamicNetwork.Domain.Configuration;
using DynamicNetwork.Domain.Enums;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Domain.Paths;
using DynamicNetwork.Infrastructure.DomainServices;

namespace DynamicNetwork.Tests;

[TestClass]
public class PathFindingDomainServiceTests
{
    private readonly PathFindingDomainService _service = new();

    [TestMethod]
    public void FindAllPaths_SingleGraphUndirectedEdge_FindsPath()
    {
        var graph = new TemporalGraph(0, new TimeInterval(0, 5),
            new[] { new Link("A", "B", LinkDirection.Undirected) },
            new[] { "A", "B" });

        var paths = _service.FindAllPaths(
            new[] { graph },
            Array.Empty<StructConfiguration>(),
            "A",
            "B",
            new TimeInterval(0, 5));

        Assert.AreEqual(1, paths.Count);
        Assert.AreEqual(2, paths[0].Nodes.Count);
        Assert.AreEqual(1, paths[0].Edges.Count);
        Assert.AreEqual(new TimeInterval(0, 5), paths[0].Interval);
    }

    [TestMethod]
    public void FindAllPaths_AcrossGraphs_FindsTimeTransitionPath()
    {
        var graph1 = new TemporalGraph(0, new TimeInterval(0, 5),
            new[] { new Link("A", "B", LinkDirection.Undirected) },
            new[] { "A", "B" });

        var graph2 = new TemporalGraph(1, new TimeInterval(5, 10),
            new[] { new Link("B", "C", LinkDirection.Undirected) },
            new[] { "B", "C" });

        var paths = _service.FindAllPaths(
            new[] { graph1, graph2 },
            Array.Empty<StructConfiguration>(),
            "A",
            "C",
            new TimeInterval(0, 10));

        Assert.AreEqual(1, paths.Count);
        Assert.AreEqual(4, paths[0].Nodes.Count);
        Assert.AreEqual("A", paths[0].Nodes[0]);
        Assert.AreEqual("C", paths[0].Nodes[^1]);
        Assert.AreEqual(2, paths[0].Edges.Count);
        Assert.AreEqual(0, paths[0].Interval.Start);
        Assert.AreEqual(10, paths[0].Interval.End);
    }
}
