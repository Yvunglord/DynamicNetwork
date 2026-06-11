using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Domain.Enums;

namespace DynamicNetwork.Tests;

[TestClass]
public class LinkTests
{
    [TestMethod]
    public void ToString_ShowsArrowsPerDirection()
    {
        var a = new Link("A", "B", LinkDirection.Undirected);
        var b = new Link("A", "B", LinkDirection.Right);
        var c = new Link("A", "B", LinkDirection.Left);
        var d = new Link("A", "B", LinkDirection.None);

        Assert.IsTrue(a.ToString().Contains("↔"));
        Assert.IsTrue(b.ToString().Contains("→"));
        Assert.IsTrue(c.ToString().Contains("←"));
        Assert.IsTrue(d.ToString().Contains("—"));
    }

    [TestMethod]
    public void WithCycledDirection_CyclesThroughSequence()
    {
        var link = new Link("A", "B", LinkDirection.Undirected);

        link = link.WithCycledDirection(); // Undirected -> Right
        Assert.AreEqual(LinkDirection.Right, link.Direction);

        link = link.WithCycledDirection(); // Right -> Left
        Assert.AreEqual(LinkDirection.Left, link.Direction);

        link = link.WithCycledDirection(); // Left -> None
        Assert.AreEqual(LinkDirection.None, link.Direction);

        link = link.WithCycledDirection(); // None -> Undirected
        Assert.AreEqual(LinkDirection.Undirected, link.Direction);
    }

    [TestMethod]
    public void Equals_IgnoresDirection_And_HashCodeDependsOnNodes()
    {
        var a = new Link("A", "B", LinkDirection.Undirected);
        var b = new Link("A", "B", LinkDirection.Right);

        Assert.IsTrue(a.Equals(b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void WithDirection_CreatesNewLinkWithSpecifiedDirection()
    {
        var original = new Link("A", "B", LinkDirection.Left);
        var updated = original.WithDirection(LinkDirection.Right);

        Assert.AreEqual(LinkDirection.Left, original.Direction);
        Assert.AreEqual(LinkDirection.Right, updated.Direction);
        Assert.AreEqual(original.NodeA, updated.NodeA);
        Assert.AreEqual(original.NodeB, updated.NodeB);
    }

    [TestMethod]
    public void Equals_DifferentNodes_ReturnsFalse()
    {
        var a = new Link("A", "B", LinkDirection.Undirected);
        var b = new Link("B", "A", LinkDirection.Undirected);

        Assert.IsFalse(a.Equals(b));
    }
}
