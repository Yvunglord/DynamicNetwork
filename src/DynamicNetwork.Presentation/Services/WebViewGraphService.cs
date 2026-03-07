using DynamicNetwork.Application.Dtos;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace DynamicNetwork.Presentation.Services;

/// <summary>
/// Cервис для работы с WebView2 + Cytoscape.js
/// </summary>
public class WebViewGraphService : IGraphVisualizationService
{
    private WebView2? _webView;
    private bool _isInitialized;
    private CytoscapeGraphData? _pendingGraphData;

    public event EventHandler<NodeClickedEventArgs>? NodeClicked;

    /// <summary>
    /// Привязывает сервис к экземпляру WebView2 из View
    /// </summary>
    public void AttachView(WebView2 webView)
    {
        _webView = webView;
    }

    public async Task InitializeAsync()
    {
        if (_webView == null || _isInitialized) return;

        await _webView.EnsureCoreWebView2Async();

        // Загружаем локальный HTML
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var indexPath = Path.Combine(basePath, "wwwroot", "index.html");

        if (File.Exists(indexPath))
        {
            _webView.Source = new Uri(Path.GetFullPath(indexPath));
        }

        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        _isInitialized = true;

        if (_pendingGraphData != null)
        {
            RenderGraph(_pendingGraphData);
            _pendingGraphData = null;
        }
    }

    public void RenderGraph(CytoscapeGraphData graphData)
    {
        if (!_isInitialized)
        {
            _pendingGraphData = graphData;
            return;
        }

        var message = new
        {
            Type = "LOAD_GRAPH",
            Data = graphData
        };

        var json = JsonConvert.SerializeObject(message);
        _webView?.CoreWebView2?.PostWebMessageAsJson(json);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.TryGetWebMessageAsString();
            var msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (msg?.TryGetValue("Type", out var typeObj) == true &&
                typeObj?.ToString() == "NODE_CLICKED" &&
                msg.TryGetValue("Data", out var dataObj) &&
                dataObj is Dictionary<string, object> data)
            {
                var args = new NodeClickedEventArgs
                {
                    NodeId = data.TryGetValue("id", out var id) ? id.ToString() ?? "" : "",
                    Label = data.TryGetValue("label", out var label) ? label.ToString() ?? "" : ""
                };

                // Вызываем событие в UI-потоке
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    NodeClicked?.Invoke(this, args));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WebViewGraphService error: {ex.Message}");
        }
    }
}