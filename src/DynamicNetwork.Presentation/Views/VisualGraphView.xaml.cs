using DynamicNetwork.Presentation.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DynamicNetwork.Presentation.Views
{
    /// <summary>
    /// Логика взаимодействия для VisualGraphView.xaml
    /// </summary>
    public partial class VisualGraphView : UserControl
    {
        public VisualGraphView()
        {
            InitializeComponent();
        }

        public void AttachVisualizationService(IGraphVisualizationService service)
        {
            if (service is WebViewGraphService webViewService)
            {
                webViewService.AttachView(WebViewControl);

                if (this.IsLoaded)
                {
                    _ = InitializeWebViewAsync(webViewService);
                }
                else
                {
                    this.Loaded += async (s, e) => await InitializeWebViewAsync(webViewService);
                }
            }
        }

        private async Task InitializeWebViewAsync(WebViewGraphService service)
        {
            try
            {
                await service.InitializeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
            }
        }
    }
}
