namespace DynamicNetwork.Application.Dtos;

/// <summary>
/// DTO для передачи данных графа в Cytoscape.js
/// Не зависит от библиотек визуализации
/// </summary>
public class CytoscapeGraphData
{
    public List<CytoscapeElement> Elements { get; set; } = new();
    public string Layout { get; set; } = "cose"; // cose, dagre, grid, circle
    public CytoscapeStyle[] Styles { get; set; } = Array.Empty<CytoscapeStyle>();
}

public class CytoscapeElement
{
    /// <summary>
    /// Все данные внутри одного словаря — как требует Cytoscape
    /// Ключи: id, label, source, target
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

public class CytoscapeStyle
{
    public string Selector { get; set; } = string.Empty;
    public Dictionary<string, object> Style { get; set; } = new();
}