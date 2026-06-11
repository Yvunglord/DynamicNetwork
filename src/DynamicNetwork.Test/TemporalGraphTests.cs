using DynamicNetwork.Domain.Graph;

namespace DynamicNetwork.Tests;

[TestClass]
public class TemporalGraphTests
{
    [TestMethod]
    public void Constructor_NullArguments_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new TemporalGraph(0, new TimeInterval(0,1), null!, new List<string>()));

        Assert.ThrowsException<ArgumentNullException>(() =>
            new TemporalGraph(0, new TimeInterval(0,1), new List<Link>(), null!));
    }

    [TestMethod]
    public void ActiveNodes_ComputedFromLinks_SortedAndDistinct()
    {
        var links = new List<Link>
        {
            new Link("B","A"),
            new Link("C","A"),
            new Link("B","C")
        };

        var allNodes = new List<string> { "A", "B", "C", "D" };
        var graph = new TemporalGraph(1, new TimeInterval(0,1), links, allNodes);

        var active = graph.ActiveNodes;

        CollectionAssert.AreEqual(new List<string>{"A","B","C"}, active.ToList());
        Assert.AreEqual(3, graph.LinkCount);
    }

    [TestMethod]
    public void Equals_SameGraphValues_ReturnsTrue()
    {
        var links = new List<Link> { new Link("A", "B"), new Link("B", "C") };
        var graph1 = new TemporalGraph(1, new TimeInterval(0, 5), links, new[] { "A", "B", "C" });
        var graph2 = new TemporalGraph(1, new TimeInterval(0, 5), links, new[] { "A", "B", "C" });

        Assert.IsTrue(graph1.Equals(graph2));
        Assert.AreEqual(graph1.GetHashCode(), graph2.GetHashCode());
    }

    [TestMethod]
    public void Equals_DifferentIntervalOrIndex_ReturnsFalse()
    {
        var links = new List<Link> { new Link("A", "B") };
        var graph1 = new TemporalGraph(1, new TimeInterval(0, 5), links, new[] { "A", "B" });
        var graph2 = new TemporalGraph(2, new TimeInterval(5, 10), links, new[] { "A", "B" });

        Assert.IsFalse(graph1.Equals(graph2));
    }
}
