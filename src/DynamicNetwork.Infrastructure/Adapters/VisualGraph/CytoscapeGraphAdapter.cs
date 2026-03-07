using DynamicNetwork.Application.Dtos;
using DynamicNetwork.Domain.Configuration;
using DynamicNetwork.Domain.Enums;
using DynamicNetwork.Domain.Graph;

namespace DynamicNetwork.Infrastructure.Adapters.VisualGraph;

/// <summary>
/// Адаптирует TemporalGraph в формат для Cytoscape.js
/// </summary>
public class CytoscapeGraphAdapter
{
    private const string DefaultNodeColor = "#ff4d6d";
    private const string EdgeColor = "#9ca3af";
    private const string InputNodeColor = "#10c939";
    private const string OutputNodeColor = "#102fc9";

    /// <summary>
    /// Конвертирует временной граф в DTO для Cytoscape
    /// </summary>
    public CytoscapeGraphData Convert(
        TemporalGraph temporalGraph,
        StructConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(temporalGraph);

        var result = new CytoscapeGraphData
        {
            Layout = "cose",
            Styles = GetDefaultStyles()
        };

        foreach (var nodeId in temporalGraph.AllNetworkNodes)
        {
            result.Elements.Add(new CytoscapeElement
            {
                Data = new Dictionary<string, object>
                {
                    ["id"] = nodeId,
                    ["label"] = nodeId
                }
            });
        }

        foreach (var link in temporalGraph.Links)
        {
            if (link.Direction == LinkDirection.None)
            {
                continue;
            }

            var edgeId = $"edge_{link.NodeA}_{link.NodeB}";

            var element = new CytoscapeElement
            {
                Data = new Dictionary<string, object>
                {
                    ["id"] = edgeId,
                    ["source"] = link.NodeA,
                    ["target"] = link.NodeB,
                    ["direction"] = link.Direction.ToString(),
                    ["is-conditional"] = link.Direction == LinkDirection.Conditional
                }
            };

            ApplyEdgeStyle(element.Data, link);
            result.Elements.Add(element);
        }

        if (configuration?.Nodes != null)
        {
            ApplyNodeConfiguration(result.Elements, configuration.Nodes);
        }

        return result;
    }

    private static void ApplyEdgeStyle(Dictionary<string, object> data, Link link)
    {
        data["direction"] = link.Direction.ToString();
        data["is-conditional"] = link.Direction == LinkDirection.Conditional;
    }

    private static void ApplyNodeConfiguration(
        List<CytoscapeElement> elements,
        IEnumerable<NodeConfiguration> configurations)
    {
        var configMap = configurations.ToDictionary(c => c.NodeId, c => c);

        foreach (var element in elements)
        {
            if (element.Data.TryGetValue("id", out var idObj) &&
                idObj is string nodeId &&
                configMap.TryGetValue(nodeId, out var nodeConfig))
            {
                element.Data["is-input"] = nodeConfig.InputsVolumes.Any();
                element.Data["is-output"] = nodeConfig.Outputs.Any();

                element.Data["config-type"] = nodeConfig.InputsVolumes.Any() ? "input" :
                                              nodeConfig.Outputs.Any() ? "output" : "normal";
            }
        }
    }

    private static CytoscapeStyle[] GetDefaultStyles()
    {
        return new[]
        {
            new CytoscapeStyle
            {
                Selector = "node",
                Style = new Dictionary<string, object>
                {
                    ["background-color"] = DefaultNodeColor,
                    ["label"] = "data(label)",
                    ["color"] = "#fff",
                    ["text-valign"] = "center",
                    ["text-halign"] = "center",
                    ["width"] = 60,
                    ["height"] = 60,
                    ["font-size"] = "11px",
                    ["shape"] = "ellipse"
                }
            },
        
            new CytoscapeStyle
            {
                Selector = "edge",
                Style = new Dictionary<string, object>
                {
                    ["width"] = 2,
                    ["curve-style"] = "bezier",
                    ["line-color"] = "#9ca3af",
                    ["target-arrow-color"] = "#9ca3af",
                    ["source-arrow-color"] = "#9ca3af",
                    ["target-arrow-shape"] = "none",
                    ["source-arrow-shape"] = "none"
                }
            },
        
            new CytoscapeStyle
            {
                Selector = ":selected",
                Style = new Dictionary<string, object>
                {
                    ["background-color"] = "#fbbf24",
                    ["border-width"] = 3,
                    ["border-color"] = "#f59e0b"
                }
            },
            new CytoscapeStyle
            {
                Selector = "node[shape = \"rectangle\"]",
                Style = new Dictionary<string, object>
                {
                    ["width"] = 80,
                    ["height"] = 40
                }
            }
        };
    }
}