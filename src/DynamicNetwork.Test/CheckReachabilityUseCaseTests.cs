using DynamicNetwork.Application.Dtos;
using DynamicNetwork.Application.Interfaces.Repositories;
using DynamicNetwork.Application.UseCases.Reachability;
using DynamicNetwork.Domain.Configuration;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Domain.Paths;
using DynamicNetwork.Domain.Services;

namespace DynamicNetwork.Tests;

[TestClass]
public class CheckReachabilityUseCaseTests
{
    [TestMethod]
    public void Execute_NoGraphsInInterval_ReturnsMessage()
    {
        var useCase = new CheckReachabilityUseCase(new FakeRepo(), new FakePathFinder());
        var result = useCase.Execute(
            new List<TemporalGraph>(),
            new ReachabilityRequest { SourceNode = "A", TargetNodes = new List<string> { "B" }, CustomInterval = new TimeInterval(0, 5) });

        Assert.IsFalse(result.IsReachable);
        Assert.AreEqual("Нет графов в заданном интервале времени", result.Message);
    }

    [TestMethod]
    public void Execute_SourceNodeMissing_ReturnsErrorMessage()
    {
        var graph = new TemporalGraph(0, new TimeInterval(0, 10), new List<Link>(), new[] { "B" });
        var useCase = new CheckReachabilityUseCase(new FakeRepo(), new FakePathFinder());

        var result = useCase.Execute(
            new[] { graph },
            new ReachabilityRequest { SourceNode = "A", TargetNodes = new List<string> { "B" }, CustomInterval = new TimeInterval(0, 10) });

        Assert.IsFalse(result.IsReachable);
        StringAssert.Contains(result.Message, "Исходный узел 'A' не найден");
    }

    [TestMethod]
    public void Execute_TargetNodeMissing_ReturnsErrorMessage()
    {
        var graph = new TemporalGraph(0, new TimeInterval(0, 10), new List<Link>(), new[] { "A" });
        var useCase = new CheckReachabilityUseCase(new FakeRepo(), new FakePathFinder());

        var result = useCase.Execute(
            new[] { graph },
            new ReachabilityRequest { SourceNode = "A", TargetNodes = new List<string> { "B" }, CustomInterval = new TimeInterval(0, 10) });

        Assert.IsFalse(result.IsReachable);
        StringAssert.Contains(result.Message, "Следующие целевые узлы не найдены: B");
    }

    [TestMethod]
    public void Execute_PathFinderReturnsPaths_MapsResultsCorrectly()
    {
        var graph = new TemporalGraph(0, new TimeInterval(0, 10),
            new List<Link> { new Link("A", "B") }, new[] { "A", "B" });
        var req = new ReachabilityRequest
        {
            SourceNode = "A",
            TargetNodes = new List<string> { "B" },
            CustomInterval = new TimeInterval(0, 10)
        };

        var path = new ReachabilityPath(
            new[] { "A", "B" },
            new[] { 0 },
            new[] { new EdgeTraversal { FromNode = "A", ToNode = "B", GraphIndex = 0, Link = new Link("A", "B") } },
            new TimeInterval(0, 10));

        var useCase = new CheckReachabilityUseCase(new FakeRepo(), new FakePathFinder(new[] { path }));
        var result = useCase.Execute(new[] { graph }, req);

        Assert.IsTrue(result.IsReachable);
        Assert.AreEqual(1, result.AllPaths.Count);
        Assert.AreEqual(1, result.ShortestPathLength);
        StringAssert.Contains(result.Message, "Найдено 1 путей");
    }

    private sealed class FakeRepo : IStructConfigurationRepository
    {
        private readonly StructConfiguration? _config;

        public FakeRepo(StructConfiguration? config = null)
        {
            _config = config;
        }

        public bool Add(StructConfiguration config) => throw new NotImplementedException();
        public void Update(StructConfiguration config) => throw new NotImplementedException();
        public void Delete(TimeInterval interval) => throw new NotImplementedException();
        public StructConfiguration? GetByInterval(TimeInterval interval) => _config;
        public IReadOnlyList<StructConfiguration> GetByTimeRange(TimeInterval range) => Array.Empty<StructConfiguration>();
        public IReadOnlyList<StructConfiguration> GetAll() => Array.Empty<StructConfiguration>();
        public bool Exists(TimeInterval interval) => _config != null;
    }

    private sealed class FakePathFinder : IPathFindingDomainService
    {
        private readonly IReadOnlyList<ReachabilityPath> _paths;

        public FakePathFinder(IReadOnlyList<ReachabilityPath>? paths = null)
        {
            _paths = paths ?? Array.Empty<ReachabilityPath>();
        }

        public IReadOnlyList<ReachabilityPath> FindAllPaths(IReadOnlyList<TemporalGraph> graphs, IReadOnlyList<StructConfiguration> configurations, string sourceNode, string targetNode, TimeInterval timeWindow)
        {
            return _paths;
        }
    }
}
