using DynamicNetwork.Application.Dtos;
using DynamicNetwork.Application.Interfaces;
using DynamicNetwork.Presentation.Services;
using DynamicNetwork.Domain.Configuration;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Infrastructure.Adapters.VisualGraph;

namespace DynamicNetwork.Presentation.ViewModels;

public sealed class VisualGraphViewModel : ViewModelBase
{
    private readonly CytoscapeGraphAdapter _adapter;
    private readonly IGraphVisualizationService _visualizationService;

    private TemporalGraph? _currentGraph;
    private StructConfiguration? _currentConfiguration;

    public VisualGraphViewModel(
        CytoscapeGraphAdapter adapter,
        IGraphVisualizationService visualizationService)
    {
        _adapter = adapter;
        _visualizationService = visualizationService;

        _visualizationService.NodeClicked += OnNodeClicked;
    }

    private void OnNodeClicked(object? sender, NodeClickedEventArgs e)
    {
        NodeSelected?.Invoke(this, e);
    }

    public event EventHandler<NodeClickedEventArgs>? NodeSelected;

    public void SetGraph(TemporalGraph graph, StructConfiguration? configuration)
    {
        _currentGraph = graph;
        _currentConfiguration = configuration;
        RebuildVisualGraph();
    }

    public void UpdateConfiguration(StructConfiguration? configuration)
    {
        if (_currentGraph == null) return;
        _currentConfiguration = configuration;
        RebuildVisualGraph();
    }

    private void RebuildVisualGraph()
    {
        if (_currentGraph == null) return;

        var cytoscapeData = _adapter.Convert(_currentGraph, _currentConfiguration);
        _visualizationService.RenderGraph(cytoscapeData);
    }
}