using DynamicNetwork.Application.Dtos;
using DynamicNetwork.Application.Interfaces.Factories;
using DynamicNetwork.Application.Interfaces.Ports;
using DynamicNetwork.Application.Session;
using DynamicNetwork.Domain.Enums;
using DynamicNetwork.Domain.Graph;

namespace DynamicNetwork.Tests;

[TestClass]
public class GraphSessionManagerTests
{
    [TestMethod]
    public void CreateSession_StoresGraphsAndReturnsSessionId()
    {
        var sourcePath = "path";
        var graphs = new[] { new TemporalGraph(0, new TimeInterval(0, 5), new[] { new Link("A", "B") }, new[] { "A", "B" }) };
        var manager = new GraphSessionManager(new FakeDataSource(sourcePath), new FakeFactory(graphs));

        var id = manager.CreateSession(sourcePath);
        var loaded = manager.GetGraphs(id);

        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual("A", loaded[0].Links[0].NodeA);
    }

    [TestMethod]
    public void UpdateLinkDirection_ChangesSingleGraphLinkDirection()
    {
        var graphs = new[] { new TemporalGraph(0, new TimeInterval(0, 5), new[] { new Link("A", "B") }, new[] { "A", "B" }) };
        var manager = new GraphSessionManager(new FakeDataSource("path"), new FakeFactory(graphs));
        var id = manager.CreateSession("path");

        var updated = manager.UpdateLinkDirection(id, 0, "A", "B", LinkDirection.Right);

        Assert.AreEqual(LinkDirection.Right, updated.Links[0].Direction);
    }

    [TestMethod]
    public void UpdateLinkDirectionCycled_CyclesTheDirection()
    {
        var graphs = new[] { new TemporalGraph(0, new TimeInterval(0, 5), new[] { new Link("A", "B", LinkDirection.Undirected) }, new[] { "A", "B" }) };
        var manager = new GraphSessionManager(new FakeDataSource("path"), new FakeFactory(graphs));
        var id = manager.CreateSession("path");

        var updated = manager.UpdateLinkDirectionCycled(id, 0, "A", "B");

        Assert.AreEqual(LinkDirection.Right, updated.Links[0].Direction);
    }

    [TestMethod]
    public void UpdateSameLinkDirection_UpdatesLinkInAllGraphs()
    {
        var graphs = new[]
        {
            new TemporalGraph(0, new TimeInterval(0, 5), new[] { new Link("A", "B") }, new[] { "A", "B" }),
            new TemporalGraph(1, new TimeInterval(5, 10), new[] { new Link("B", "A") }, new[] { "A", "B" })
        };
        var manager = new GraphSessionManager(new FakeDataSource("path"), new FakeFactory(graphs));
        var id = manager.CreateSession("path");

        var updatedGraphs = manager.UpdateSameLinkDirection(id, 0, "A", "B", LinkDirection.Left);

        Assert.AreEqual(2, updatedGraphs.Count);
        Assert.AreEqual(LinkDirection.Left, updatedGraphs[0].Links[0].Direction);
        Assert.AreEqual(LinkDirection.Left, updatedGraphs[1].Links[0].Direction);
    }

    [TestMethod]
    public void CloseSession_RemovesSession()
    {
        var graphs = new[] { new TemporalGraph(0, new TimeInterval(0, 5), new[] { new Link("A", "B") }, new[] { "A", "B" }) };
        var manager = new GraphSessionManager(new FakeDataSource("path"), new FakeFactory(graphs));
        var id = manager.CreateSession("path");

        manager.CloseSession(id);

        Assert.ThrowsException<KeyNotFoundException>(() => manager.GetGraphs(id));
    }

    private sealed class FakeDataSource : ITemporalDataSourcePort
    {
        private readonly string _expectedPath;

        public FakeDataSource(string expectedPath) => _expectedPath = expectedPath;

        public IReadOnlyCollection<LinkParsingDto> LoadRawLinks(string path)
        {
            Assert.AreEqual(_expectedPath, path);
            return Array.Empty<LinkParsingDto>().ToList().AsReadOnly();
        }
    }

    private sealed class FakeFactory : ITemporalGraphFactory
    {
        private readonly IReadOnlyList<TemporalGraph> _graphs;

        public FakeFactory(IReadOnlyList<TemporalGraph> graphs) => _graphs = graphs;

        public IReadOnlyList<TemporalGraph> BuildGraphs(IEnumerable<LinkParsingDto> rawLinks) => _graphs;
    }
}
