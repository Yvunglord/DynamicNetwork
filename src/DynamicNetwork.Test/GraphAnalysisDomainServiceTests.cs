using DynamicNetwork.Infrastructure.DomainServices;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Domain.Enums;

namespace DynamicNetwork.Tests;

[TestClass]
public class GraphAnalysisDomainServiceTests
{
    private GraphAnalysisDomainService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new GraphAnalysisDomainService();
    }

    [TestMethod]
    public void Analyze_CompleteGraphK3_ReturnsCorrectMetrics()
    {
        var nodes = new List<string> { "Node1", "Node2", "Node3" };
        var links = new List<Link>
        {
            new Link("Node1", "Node2", LinkDirection.Undirected),
            new Link("Node2", "Node3", LinkDirection.Undirected),
            new Link("Node3", "Node1", LinkDirection.Undirected)
        };
        var interval = new TimeInterval(0, 1);
        var graph = new TemporalGraph(0, interval, links, nodes);

        var result = _service.Analyze(graph);

        Assert.AreEqual(3, result.VertexCount, "Vertex count should be 3 for K3 graph.");
        Assert.AreEqual(3, result.EdgeCount, "Edge count should be 3 for K3 graph.");
        Assert.IsTrue(result.IsConnected, "Graph should be identified as connected.");
        Assert.IsTrue(result.HasCycles, "Graph should be identified as having cycles.");
    }

    [TestMethod]
    public void Analyze_DisconnectedGraph_ReturnsIsConnectedFalse()
    {
        var nodes = new List<string> { "Node1", "Node2" };
        var links = new List<Link>();
        var graph = new TemporalGraph(0, new TimeInterval(0, 1), links, nodes);

        var result = _service.Analyze(graph);

        Assert.IsFalse(result.IsConnected);
    }
}