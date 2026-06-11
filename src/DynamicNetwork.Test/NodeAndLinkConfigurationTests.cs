using DynamicNetwork.Domain.Configuration;

namespace DynamicNetwork.Tests;

[TestClass]
public class NodeAndLinkConfigurationTests
{
    [TestMethod]
    public void WithActiveProcesses_FiltersToEnabledOnly()
    {
        var node = new NodeConfiguration(
            "A",
            new[] { "P1", "P2" },
            new Dictionary<string, double>(),
            Array.Empty<string>(),
            new Dictionary<string, double>(),
            Array.Empty<string>());

        var updated = node.WithActiveProcesses(new[] { "P1", "Unknown" });

        CollectionAssert.AreEqual(new[] { "P1" }, updated.ActiveProcesses.ToList());
    }

    [TestMethod]
    public void WithActiveTransports_FiltersToEnabledOnly()
    {
        var link = new LinkConfiguration(
            "A", "B",
            new[] { "T1", "T2" },
            Array.Empty<string>());

        var updated = link.WithActiveTransports(new[] { "T2", "T3" });

        CollectionAssert.AreEqual(new[] { "T2" }, updated.ActiveTransports.ToList());
    }
}
