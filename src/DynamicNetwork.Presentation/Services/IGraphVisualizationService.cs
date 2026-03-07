using DynamicNetwork.Application.Dtos;

namespace DynamicNetwork.Presentation.Services;


/// <summary>
/// Сервис для инициализации и управления визуализацией графа через WebView2
/// </summary>
public interface IGraphVisualizationService
{
    /// <summary>
    /// Инициализирует WebView2 (должен вызываться после загрузки View)
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Отрисовывает граф в WebView
    /// </summary>
    void RenderGraph(CytoscapeGraphData graphData);

    /// <summary>
    /// Событие: пользователь кликнул на узел
    /// </summary>
    event EventHandler<NodeClickedEventArgs> NodeClicked;
}

public class NodeClickedEventArgs : EventArgs
{
    public string NodeId { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}
