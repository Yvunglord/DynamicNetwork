using DynamicNetwork.Application.Dtos;
using DynamicNetwork.Application.Interfaces.Factories;
using DynamicNetwork.Application.Interfaces.Ports;
using DynamicNetwork.Application.Interfaces.Repositories;
using DynamicNetwork.Application.Session;
using DynamicNetwork.Application.UseCases.Analysis;
using DynamicNetwork.Application.UseCases.Graphs;
using DynamicNetwork.Application.UseCases.Reachability;
using DynamicNetwork.Domain.Analysis;
using DynamicNetwork.Domain.Configuration;
using DynamicNetwork.Domain.Enums;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Domain.Paths;
using DynamicNetwork.Domain.Services;
using DynamicNetwork.Infrastructure.DomainServices;
using DynamicNetwork.Infrastructure.Factories;

namespace DynamicNetwork.Tests;

[TestClass]
public class IntegrationTests
{
    [TestMethod]
    public void LoadTemporalGraphsUseCase_RealFactory_BuildsOrderedIntervals()
    {
        var rawLinks = new[]
        {
            new LinkParsingDto { NodeA = "A", NodeB = "B", Begin = 1, End = 5 },
            new LinkParsingDto { NodeA = "B", NodeB = "C", Begin = 3, End = 7 }
        };

        var useCase = new LoadTemporalGraphsUseCase(
            new FakeDataSource(rawLinks),
            new TemporalGraphFactory());

        var graphs = useCase.Execute("ignored");

        Assert.AreEqual(3, graphs.Count);
        Assert.AreEqual(new TimeInterval(1, 3), graphs[0].Interval);
        Assert.AreEqual(new TimeInterval(3, 5), graphs[1].Interval);
        Assert.AreEqual(new TimeInterval(5, 7), graphs[2].Interval);
        Assert.AreEqual(1, graphs[0].Links.Count);
        Assert.AreEqual(2, graphs[1].Links.Count);
        Assert.AreEqual(1, graphs[2].Links.Count);
    }

    [TestMethod]
    public void CheckReachabilityUseCase_RealPathFinder_ReturnsReachabilityDto()
    {
        var graphs = new[]
        {
            new TemporalGraph(0, new TimeInterval(0, 5),
                new[] { new Link("A", "B", LinkDirection.Undirected) },
                new[] { "A", "B" }),
            new TemporalGraph(1, new TimeInterval(5, 10),
                new[] { new Link("B", "C", LinkDirection.Undirected) },
                new[] { "B", "C" })
        };

        var useCase = new CheckReachabilityUseCase(
            new FakeConfigRepository(),
            new PathFindingDomainService());

        var result = useCase.Execute(graphs, new ReachabilityRequest
        {
            SourceNode = "A",
            TargetNodes = new List<string> { "C" },
            CustomInterval = new TimeInterval(0, 10)
        });

        Assert.IsTrue(result.IsReachable);
        Assert.AreEqual(1, result.AllPaths.Count);
        Assert.AreEqual(3, result.ShortestPathLength);
        Assert.IsTrue(result.Message.Contains("Найдено 1 путей"));
    }

    [TestMethod]
    public void AnalyzeGraphStructureUseCase_RealAnalyzer_ConvertsMatrixToDto()
    {
        var graph = new TemporalGraph(0, new TimeInterval(0, 10),
            new[] { new Link("A", "B", LinkDirection.Undirected) },
            new[] { "A", "B" });

        var useCase = new AnalyzeGraphStructureUseCase(new GraphAnalysisDomainService());
        var result = useCase.Execute(graph);

        Assert.AreEqual(2, result.VertexCount);
        Assert.AreEqual(1, result.EdgeCount);
        Assert.IsTrue(result.IsConnected);
        Assert.AreEqual(2, result.AdjacencyMatrixRows.Count);
        Assert.AreEqual("A", result.AdjacencyMatrixRows[0].RowHeader);
    }

    [TestMethod]
    public void GraphSessionManager_RealFactory_StoresAndUpdatesGraphsAcrossSession()
    {
        var rawLinks = new[]
        {
            new LinkParsingDto { NodeA = "A", NodeB = "B", Begin = 0, End = 5 }
        };

        var manager = new GraphSessionManager(
            new FakeDataSource(rawLinks),
            new TemporalGraphFactory());

        var sessionId = manager.CreateSession("ignored");
        var graphs = manager.GetGraphs(sessionId);

        Assert.AreEqual(1, graphs.Count);
        Assert.AreEqual("A", graphs[0].Links[0].NodeA);

        var updatedGraph = manager.UpdateLinkDirection(sessionId, 0, "A", "B", LinkDirection.Right);
        Assert.AreEqual(LinkDirection.Right, updatedGraph.Links[0].Direction);
    }

    private sealed class FakeDataSource : ITemporalDataSourcePort
    {
        private readonly IReadOnlyCollection<LinkParsingDto> _rawLinks;

        public FakeDataSource(IReadOnlyCollection<LinkParsingDto> rawLinks) => _rawLinks = rawLinks;

        public IReadOnlyCollection<LinkParsingDto> LoadRawLinks(string path)
        {
            Assert.AreEqual("ignored", path);
            return _rawLinks;
        }
    }

    private sealed class FakeConfigRepository : IStructConfigurationRepository
    {
        public bool Add(StructConfiguration config) => throw new NotImplementedException();
        public void Update(StructConfiguration config) => throw new NotImplementedException();
        public void Delete(TimeInterval interval) => throw new NotImplementedException();
        public StructConfiguration? GetByInterval(TimeInterval interval) => null;
        public IReadOnlyList<StructConfiguration> GetByTimeRange(TimeInterval range) => Array.Empty<StructConfiguration>();
        public IReadOnlyList<StructConfiguration> GetAll() => Array.Empty<StructConfiguration>();
        public bool Exists(TimeInterval interval) => false;
    }
}
