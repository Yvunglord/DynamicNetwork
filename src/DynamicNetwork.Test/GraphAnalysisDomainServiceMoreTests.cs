using DynamicNetwork.Infrastructure.DomainServices;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Domain.Enums;
using DynamicNetwork.Domain.Analysis;

namespace DynamicNetwork.Tests;

[TestClass]
public class GraphAnalysisDomainServiceMoreTests
{
    private GraphAnalysisDomainService _service = null!;

    [TestInitialize]
    public void Setup() => _service = new GraphAnalysisDomainService();

    [TestMethod]
    public void AdjacencyMatrix_UndirectedK3_IsSymmetric()
    {
        var nodes = new List<string> { "A", "B", "C" };
        var links = new List<Link>
        {
            new Link("A","B", LinkDirection.Undirected),
            new Link("B","C", LinkDirection.Undirected),
            new Link("C","A", LinkDirection.Undirected)
        };

        var graph = new TemporalGraph(0, new TimeInterval(0,1), links, nodes);

        var result = _service.Analyze(graph);

        var matrix = result.AdjacencyMatrix;
        Assert.AreEqual(3, matrix.Rows);
        Assert.AreEqual(3, matrix.Columns);

        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                Assert.AreEqual(matrix.Values[i][j], matrix.Values[j][i]);
    }

    [TestMethod]
    public void IncidenceMatrix_DirectedEdge_HasMinusAndPlus()
    {
        var nodes = new List<string> { "A", "B" };
        var links = new List<Link>
        {
            new Link("A","B", LinkDirection.Right)
        };

        var graph = new TemporalGraph(0, new TimeInterval(0,1), links, nodes);
        var result = _service.Analyze(graph);

        var inc = result.IncidenceMatrix;
        Assert.AreEqual(2, inc.Rows);
        Assert.AreEqual(1, inc.Columns);

        // For Right direction: source has -1, target has +1
        Assert.AreEqual(-1, inc.Values[0][0]);
        Assert.AreEqual(1, inc.Values[1][0]);
    }

    [TestMethod]
    public void Density_UndirectedVsDirected_Behaviour()
    {
        var nodes = new List<string>{"A","B"};

        var undirected = new TemporalGraph(0, new TimeInterval(0,1),
            new List<Link>{ new Link("A","B", LinkDirection.Undirected) }, nodes);

        var directed = new TemporalGraph(0, new TimeInterval(0,1),
            new List<Link>{ new Link("A","B", LinkDirection.Right) }, nodes);

        var r1 = _service.Analyze(undirected);
        var r2 = _service.Analyze(directed);

        Assert.AreEqual(1d, r1.Density);
        Assert.AreEqual(0.5d, r2.Density);
    }

    [TestMethod]
    public void Diameter_ChainOf3_Is2()
    {
        var nodes = new List<string>{"A","B","C"};
        var links = new List<Link>
        {
            new Link("A","B", LinkDirection.Undirected),
            new Link("B","C", LinkDirection.Undirected)
        };

        var graph = new TemporalGraph(0, new TimeInterval(0,1), links, nodes);
        var result = _service.Analyze(graph);

        Assert.AreEqual(2, result.Diameter);
    }

    [TestMethod]
    public void StronglyConnectedComponents_DirectedCycle_ReturnsOne()
    {
        var nodes = new List<string>{"A","B","C"};
        var links = new List<Link>
        {
            new Link("A","B", LinkDirection.Right),
            new Link("B","C", LinkDirection.Right),
            new Link("C","A", LinkDirection.Right)
        };

        var graph = new TemporalGraph(0, new TimeInterval(0,1), links, nodes);
        var result = _service.Analyze(graph);

        Assert.AreEqual(1, result.StronglyConnectedComponentsCount);
    }
}
