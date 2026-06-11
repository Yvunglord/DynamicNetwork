using DynamicNetwork.Application.Interfaces.Factories;
using DynamicNetwork.Application.Interfaces.Ports;
using DynamicNetwork.Application.UseCases.Graphs;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Application.Dtos;

namespace DynamicNetwork.Tests;

[TestClass]
public class LoadTemporalGraphsUseCaseTests
{
    [TestMethod]
    public void Execute_DelegatesToDataSourceAndFactory()
    {
        var rawLinks = new[]
        {
            new LinkParsingDto { NodeA = "A", NodeB = "B", Begin = 0, End = 5 }
        };

        var expectedGraphs = new[] { new TemporalGraph(0, new TimeInterval(0, 5), new List<Link> { new Link("A", "B") }, new[] { "A", "B" }) };
        var useCase = new LoadTemporalGraphsUseCase(new FakePort(rawLinks), new FakeFactory(expectedGraphs));

        var result = useCase.Execute("somePath");

        Assert.AreSame(expectedGraphs, result);
    }

    private sealed class FakePort : ITemporalDataSourcePort
    {
        private readonly IEnumerable<LinkParsingDto> _rawLinks;

        public FakePort(IEnumerable<LinkParsingDto> rawLinks) => _rawLinks = rawLinks;

        public IReadOnlyCollection<LinkParsingDto> LoadRawLinks(string path)
        {
            Assert.AreEqual("somePath", path);
            return _rawLinks.ToList().AsReadOnly();
        }
    }

    private sealed class FakeFactory : ITemporalGraphFactory
    {
        private readonly IReadOnlyList<TemporalGraph> _graphs;

        public FakeFactory(IReadOnlyList<TemporalGraph> graphs) => _graphs = graphs;

        public IReadOnlyList<TemporalGraph> BuildGraphs(IEnumerable<LinkParsingDto> rawLinks)
        {
            return _graphs;
        }
    }
}
