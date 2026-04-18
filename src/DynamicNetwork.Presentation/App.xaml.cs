using DynamicNetwork.Application.Interfaces.Providers;
using DynamicNetwork.Application.Interfaces.Repositories;
using DynamicNetwork.Domain.Functions;
using DynamicNetwork.Infrastructure.Adapters.VisualGraph;
using DynamicNetwork.Infrastructure.DependencyInjection;
using DynamicNetwork.Presentation.Services;
using DynamicNetwork.Presentation.ViewModels;
using DynamicNetwork.Presentation.Views;
using DynamicNetwork.Presentation.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Windows;

namespace DynamicNetwork.Presentation
{
    public partial class App : System.Windows.Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();
        }

        public static void ChangeLanguage(string cultureCode)
        {
            var dict = new ResourceDictionary();

            switch(cultureCode)
            {
                case "ru":
                    dict.Source = new Uri("Assets/Lang.ru.xaml", UriKind.Relative);
                    break;

                case "en":
                    dict.Source = new Uri("Assets/Lang.en.xaml", UriKind.Relative);
                    break;

                default:
                    return;
            }

            var appResources = Current.Resources.MergedDictionaries;

            var existing = appResources
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Lang"));

            if (existing != null)
                appResources.Remove(existing);

            appResources.Add(dict);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddDynamicNetworkSynthesis();

            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<CytoscapeGraphAdapter>();
            services.AddSingleton<IGraphVisualizationService, WebViewGraphService>();
            services.AddTransient<VisualGraphViewModel>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _host.Start();

            InitializeSystem();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
            mainWindow.DataContext = mainViewModel;
            mainWindow.Show();

            AttachVisualizationService(mainWindow);

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(1));
            }
            base.OnExit(e);
        }

        private void InitializeSystem()
        {
            var libraryProvider = _host.Services.GetRequiredService<IFunctionLibraryProvider>();
            var currentLibrary = libraryProvider.GetCurrent();

            if (currentLibrary.Processes.Count == 0 &&
                currentLibrary.Transports.Count == 0 &&
                currentLibrary.Storages.Count == 0)
            {
                var testLibrary = CreateTestFunctionLibrary();
                libraryProvider.Update(testLibrary);

                System.Diagnostics.Debug.WriteLine(
                    $"Инициализирована тестовая библиотека: " +
                    $"{testLibrary.Processes.Count} процессов, " +
                    $"{testLibrary.Transports.Count} транспортов, " +
                    $"{testLibrary.Storages.Count} хранилищ");
            }
        }

        private FunctionLibrary CreateTestFunctionLibrary()
        {
            return new FunctionLibrary(
                new[]
                {
                    new ProcessType("1", 0.2, "1", "2", 1.0),
                    new ProcessType("2", 0.04, "1", "2", 1.0),
                    new ProcessType("3", 0.06666666666666667, "1", "2", 1.0),
                    new ProcessType("4", 0.02, "1", "2", 1.0)
                },
                new[]
                {
                    new TransportType("1", 1.0, "2", 20.0),
                    new TransportType("2", 1.0, "1", 20.0)
                },
                new[]
                {
                    new StorageType("1", new[] { "1", "2" })
                },
                new[]
                {
                    new FlowType("1"),
                    new FlowType("2")
                }
            );
        }

        private void AttachVisualizationService(MainWindow window)
        {
            if (window.GraphView is VisualGraphView graphView)
            {
                var visualizationService = _host.Services.GetRequiredService<IGraphVisualizationService>();

                graphView.AttachVisualizationService(visualizationService);
            }
            else
            {
                Debug.WriteLine("GraphView не найден или неверный тип!");
            }
        }
    }
}