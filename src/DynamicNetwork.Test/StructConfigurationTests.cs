using DynamicNetwork.Domain.Configuration;
using DynamicNetwork.Domain.Graph;

namespace DynamicNetwork.Tests;

[TestClass]
public class StructConfigurationTests
{
    [TestMethod]
    public void WithUpdatedNode_ReplacesSpecifiedNode()
    {
        var node = new NodeConfiguration(
            "A",
            new[] { "P1" },
            new Dictionary<string, double>(),
            Array.Empty<string>(),
            new Dictionary<string, double>(),
            new[] { "P1" });

        var config = new StructConfiguration(
            new TimeInterval(0, 10),
            new[] { node },
            Array.Empty<LinkConfiguration>());

        var updated = config.WithUpdatedNode("A", n => new NodeConfiguration(
            n.NodeId,
            n.EnabledProcesses,
            n.InputsVolumes.ToDictionary(),
            n.Outputs,
            n.StorageCapacities.ToDictionary(),
            new[] { "P1" }));

        Assert.AreEqual(1, updated.Nodes.Count);
        Assert.AreEqual("A", updated.Nodes.First().NodeId);
    }

    [TestMethod]
    public void WithUpdatedNode_ThrowsWhenNodeMissing()
    {
        var config = new StructConfiguration(
            new TimeInterval(0, 10),
            Array.Empty<NodeConfiguration>(),
            Array.Empty<LinkConfiguration>());

        Assert.ThrowsException<InvalidOperationException>(() =>
            config.WithUpdatedNode("X", n => n));
    }

    [TestMethod]
    public void WithUpdatedLink_ReplacesExistingLink()
    {
        var link = new LinkConfiguration("A", "B", new[] { "T1" }, new[] { "T1" });
        var config = new StructConfiguration(
            new TimeInterval(0, 10),
            Array.Empty<NodeConfiguration>(),
            new[] { link });

        var updated = config.WithUpdatedLink("A", "B", l => l.WithActiveTransports(new[] { "T1" }));

        Assert.AreEqual(1, updated.Links.Count);
        Assert.AreEqual("A", updated.Links.First().NodeA);
    }

    [TestMethod]
    public void WithUpdatedLink_ThrowsWhenLinkMissing()
    {
        var config = new StructConfiguration(
            new TimeInterval(0, 10),
            Array.Empty<NodeConfiguration>(),
            Array.Empty<LinkConfiguration>());

        Assert.ThrowsException<InvalidOperationException>(() =>
            config.WithUpdatedLink("A", "B", l => l));
    }

    [TestMethod]
    public void WithUpdatedNode_ReplacesOnlyTargetedNode()
    {
        var nodeA = new NodeConfiguration("A", Array.Empty<string>(), new Dictionary<string, double>(), Array.Empty<string>(), new Dictionary<string, double>(), Array.Empty<string>());
        var nodeB = new NodeConfiguration("B", Array.Empty<string>(), new Dictionary<string, double>(), Array.Empty<string>(), new Dictionary<string, double>(), Array.Empty<string>());
        var config = new StructConfiguration(new TimeInterval(0, 10), new[] { nodeA, nodeB }, Array.Empty<LinkConfiguration>());

        var updated = config.WithUpdatedNode("A", n => new NodeConfiguration(n.NodeId, n.EnabledProcesses, n.InputsVolumes.ToDictionary(), n.Outputs, n.StorageCapacities.ToDictionary(), new[] { "P1" }));

        Assert.AreEqual(2, updated.Nodes.Count);
        Assert.IsTrue(updated.Nodes.Any(n => n.NodeId == "A"));
        Assert.IsTrue(updated.Nodes.Any(n => n.NodeId == "B"));
        Assert.AreEqual(1, updated.Nodes.First(n => n.NodeId == "A").ActiveProcesses.Count);
    }

    [TestMethod]
    public void WithUpdatedLink_ReplacesOnlySpecifiedLink()
    {
        var linkA = new LinkConfiguration("A", "B", new[] { "T1" }, new[] { "T1" });
        var linkB = new LinkConfiguration("B", "C", new[] { "T2" }, new[] { "T2" });
        var config = new StructConfiguration(new TimeInterval(0, 10), Array.Empty<NodeConfiguration>(), new[] { linkA, linkB });

        var updated = config.WithUpdatedLink("A", "B", l => l.WithActiveTransports(Array.Empty<string>()));

        Assert.AreEqual(2, updated.Links.Count);
        Assert.AreEqual(0, updated.Links.First(l => l.NodeA == "A" && l.NodeB == "B").ActiveTransports.Count);
        Assert.AreEqual(1, updated.Links.First(l => l.NodeA == "B" && l.NodeB == "C").ActiveTransports.Count);
    }
}
