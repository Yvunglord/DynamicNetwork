using DynamicNetwork.Application.Dtos;
using DynamicNetwork.Application.UseCases.Analysis;
using DynamicNetwork.Domain.Analysis;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Domain.Services;

namespace DynamicNetwork.Tests;

[TestClass]
public class AnalyzeGraphStructureUseCaseTests
{
    [TestMethod]
    public void Execute_MapsDomainAnalysisToDto()
    {
        var matrix = new Matrix<int>(new[] { "A" }, new[] { "B" }, new List<IReadOnlyList<int>> { new[] { 1 } });
        var domainResult = new GraphAnalysisResult
        {
            VertexCount = 1,
            EdgeCount = 0,
            IsConnected = true,
            HasCycles = false,
            Density = 0,
            Diameter = 0,
            DirectedLinksCount = 0,
            UndirectedLinksCount = 0,
            StronglyConnectedComponentsCount = 1,
            AdjacencyMatrix = matrix,
            IncidenceMatrix = matrix,
            DegreeCentrality = new Dictionary<string, double> { ["A"] = 0 },
            BetweennessCentrality = new Dictionary<string, double> { ["A"] = 0 }
        };

        var useCase = new AnalyzeGraphStructureUseCase(new FakeAnalyzer(domainResult));
        var result = useCase.Execute(new TemporalGraph(0, new TimeInterval(0, 10), Array.Empty<Link>(), new[] { "A" }));

        Assert.AreEqual(1, result.VertexCount);
        Assert.AreEqual("Анализ завершён: 1 вершин, 0 рёбер", result.Message);
        Assert.AreEqual("A", result.AdjacencyMatrixRows[0].RowHeader);
        Assert.AreEqual("1", result.AdjacencyMatrixRows[0].Cells[0].Value);
    }

    private sealed class FakeAnalyzer : IGraphAnalysisDomainService
    {
        private readonly GraphAnalysisResult _result;

        public FakeAnalyzer(GraphAnalysisResult result) => _result = result;

        public GraphAnalysisResult Analyze(TemporalGraph graph) => _result;
    }
}
