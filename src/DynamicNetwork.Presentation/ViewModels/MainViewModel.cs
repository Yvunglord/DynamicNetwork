using DynamicNetwork.Application.Dtos;
using DynamicNetwork.Application.Interfaces.Providers;
using DynamicNetwork.Application.Interfaces.Repositories;
using DynamicNetwork.Application.Interfaces.Session;
using DynamicNetwork.Application.Interfaces.UseCases.Analysis;
using DynamicNetwork.Application.Interfaces.UseCases.Configuration;
using DynamicNetwork.Application.Interfaces.UseCases.Graphs;
using DynamicNetwork.Application.Interfaces.UseCases.Library;
using DynamicNetwork.Application.Interfaces.UseCases.Reachability;
using DynamicNetwork.Domain.Configuration;
using DynamicNetwork.Domain.Enums;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Infrastructure.Adapters.VisualGraph;
using DynamicNetwork.Presentation.Commands;
using DynamicNetwork.Presentation.Services;
using DynamicNetwork.Presentation.ViewModels.Configuration;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace DynamicNetwork.Presentation.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly ILoadTemporalGraphsUseCase _loadGraphsUseCase;
    private readonly IGraphSessionManager _graphSessionManager;
    private readonly IFunctionLibraryProvider _libraryProvider;
    private readonly IStructConfigurationRepository _configRepository;
    private readonly ICheckReachabilityUseCase _reachabilityUseCase;
    private readonly IAnalyzeGraphStructureUseCase _analyzerUseCase;
    private readonly IExportConfigurationUseCase _exportUseCase;
    private readonly IExportFunctionLibraryUseCase _exportLibraryUseCase;
    private readonly IEditStructConfigurationUseCase _editStructUseCase;
    private readonly ISynthesizeConfigurationUseCase _synthesizeUseCase;
    private readonly IImportFunctionLibraryUseCase _importLibraryUseCase;
    private readonly IManageFunctionLibraryUseCase _manageLibraryUseCase;

    private VisualGraphViewModel _visualGraphViewModel;
    private IntervalsViewModel? _intervalsViewModel;
    private TemporalGraph? _currentGraph;
    private Link? _selectedLink;
    private TopologyReachabilityViewModel? _reachabilityViewModel;
    private AnalysisResult? _analysisResult;
    private FunctionalLibraryViewModel? _libraryViewModel;
    private StructConfigurationViewModel? _structConfigViewModel;
    private SettingsViewModel? _settingsViewModel;
    private string? _currentSessionId;
    private bool _applyToAllGraphs = false;
    private bool _applyToSelectedGraphs = false;

    public IDialogService DialogService => _dialogService;

    public ObservableCollection<TemporalGraph> TemporalGraphs { get; }
        = new ObservableCollection<TemporalGraph>();

    public VisualGraphViewModel VisualGraphViewModel => _visualGraphViewModel;

    public IntervalsViewModel? IntervalsViewModel
    {
        get => _intervalsViewModel;
        set => SetField(ref _intervalsViewModel, value);
    }

    public TemporalGraph? CurrentGraph
    {
        get => _currentGraph;
        set
        {
            SetField(ref _currentGraph, value);
            if (value != null)
            {
                var config = CurrentGraph != null ? _configRepository.GetByInterval(CurrentGraph.Interval) : null;
                VisualGraphViewModel.SetGraph(value, config);
                AnalysisResult = _analyzerUseCase.Execute(value);
            }
        }
    }

    public Link? SelectedLink
    {
        get => _selectedLink;
        set => SetField(ref _selectedLink, value);
    }

    public bool ApplyToAllGraphs
    {
        get => _applyToAllGraphs;
        set
        {
            if (_applyToSelectedGraphs)
            {
                ApplyToSelectedGraphs = false;
            }
            SetField(ref _applyToAllGraphs, value);
        } 
    }

    public bool ApplyToSelectedGraphs
    {
        get => _applyToSelectedGraphs;
        set
        {
            if (_applyToAllGraphs)
            { 
                ApplyToAllGraphs = false;
            }
            SetField(ref _applyToSelectedGraphs, value);
        } 
    }

    public TopologyReachabilityViewModel ReachabilityViewModel
    {
        get
        {
            if (_reachabilityViewModel == null)
                _reachabilityViewModel = new TopologyReachabilityViewModel(this, _reachabilityUseCase);
            return _reachabilityViewModel;
        }
    }

    public AnalysisResult? AnalysisResult
    {
        get => _analysisResult;
        set => SetField(ref _analysisResult, value);
    }

    public FunctionalLibraryViewModel LibraryViewModel
    {
        get
        {
            if (_libraryViewModel == null)
            {
                _libraryViewModel = new FunctionalLibraryViewModel(this, _libraryProvider, _manageLibraryUseCase);
                OnPropertyChanged(nameof(LibraryViewModel));
            }
            return _libraryViewModel;
        }
    }

    public StructConfigurationViewModel StructConfigViewModel
    {
        get
        {
            if (_structConfigViewModel == null)
            {
                _structConfigViewModel = new StructConfigurationViewModel(
                    this,
                    _configRepository,
                    _editStructUseCase,
                    _libraryProvider
                );

                _structConfigViewModel.IntervalChanged += interval =>
                {
                    var graph = GetGraphByInterval(interval);
                    if (graph != null && CurrentGraph != graph)
                    {
                        CurrentGraph = graph;
                    }
                };
            }

            return _structConfigViewModel;
        }
    }

    public SettingsViewModel SettingsViewModel
    {
        get
        {
            if (_settingsViewModel == null)
                _settingsViewModel = new SettingsViewModel();

            return _settingsViewModel;
        }
    }

    public ICommand LoadTemporalGraphCommand => new RelayCommand(LoadTemporalGraph);
    public ICommand LoadFunctionLibraryCommand => new RelayCommand(LoadFunctionLibrary);
    public ICommand ExportFunctionalLibraryCommand => new RelayCommand(ExportFunctionLibrary);
    public ICommand CycleDirectionCommand => new RelayCommand<Link>(CycleDirection);
    public ICommand SetDirectionCommand => new RelayCommand<LinkDirection>(SetDirection);
    public ICommand ExportXmlCommand => new RelayCommand(ExportXml);

    public MainViewModel(
        IDialogService dialogService,
        ILoadTemporalGraphsUseCase loadGraphsUseCase,
        IGraphSessionManager graphSessionManager,
        IFunctionLibraryProvider libraryProvider,
        IStructConfigurationRepository configRepository,
        ICheckReachabilityUseCase reachabilityUseCase,
        IAnalyzeGraphStructureUseCase analyzerUseCase,
        IExportConfigurationUseCase exportUseCase,
        IEditStructConfigurationUseCase editStructUseCase,
        ISynthesizeConfigurationUseCase synthesizeUseCase,
        IImportFunctionLibraryUseCase importLibraryUseCase,
        IExportFunctionLibraryUseCase exportLibraryUseCase,
        IManageFunctionLibraryUseCase manageLibraryUseCase,
        CytoscapeGraphAdapter cytoscapeAdapter,
        IGraphVisualizationService visualizationService)
    {
        _visualGraphViewModel = new VisualGraphViewModel(cytoscapeAdapter, visualizationService);

        _dialogService = dialogService;
        _loadGraphsUseCase = loadGraphsUseCase;
        _graphSessionManager = graphSessionManager;
        _libraryProvider = libraryProvider;
        _configRepository = configRepository;
        _reachabilityUseCase = reachabilityUseCase;
        _analyzerUseCase = analyzerUseCase;
        _exportUseCase = exportUseCase;
        _editStructUseCase = editStructUseCase;
        _synthesizeUseCase = synthesizeUseCase;
        _importLibraryUseCase = importLibraryUseCase;
        _exportLibraryUseCase = exportLibraryUseCase;
        _manageLibraryUseCase = manageLibraryUseCase;
    }

    public TemporalGraph? GetGraphByInterval(TimeInterval interval)
    {
        return TemporalGraphs.FirstOrDefault(g => g.Interval.Equals(interval));
    }

    private void LoadTemporalGraph()
    {
        var path = _dialogService.ShowOpenFileDialog(
            "JSON files (*.json)|*.json|All files (*.*)|*.*",
            "C:\\");

        if (path == null) return;

        try
        {
            _currentSessionId = _graphSessionManager.CreateSession(path);
            var graphs = _graphSessionManager.GetGraphs(_currentSessionId);

            TemporalGraphs.Clear();
            foreach (var g in graphs)
                TemporalGraphs.Add(g);

            IntervalsViewModel = new IntervalsViewModel(TemporalGraphs);
            IntervalsViewModel.GraphSelected += (graph) => CurrentGraph = graph;

            if (TemporalGraphs.Count > 0)
                IntervalsViewModel.Selected = TemporalGraphs[0];

            StructConfigViewModel.ResetForNewSession(TemporalGraphs);
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Ошибка загрузки графов: {ex.Message}");
        }
    }

    private void LoadFunctionLibrary()
    {
        var path = _dialogService.ShowOpenFileDialog(
            "XML files (*.xml)|*.xml|All files (*.*)|*.*",
            "C:\\");

        if (path == null) return;

        try
        {
            _importLibraryUseCase.Execute(path);
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Ошибка загрузки библиотеки: {ex.Message}");
        }
    }

    private void ExportFunctionLibrary()
    {
        var path = _dialogService.ShowSaveFileDialog(
            "lib",
            "XML files (*.xml)|*.xml|All files (*.*)|*.*",
            "C:\\");

        if (path == null) return;

        try
        {
            _exportLibraryUseCase.Execute(path);   
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Ошибка выгрузки библиотеки: {ex.Message}");
        }
    }

    #region Link direction editing

    private void CycleDirection(Link? link)
    {
        if (link == null || _currentSessionId == null || CurrentGraph == null) return;

        var updatedGraph = _graphSessionManager.UpdateLinkDirectionCycled(
            _currentSessionId,
            CurrentGraph.Index,
            link.NodeA,
            link.NodeB);

        UpdateGraphInCollection(updatedGraph);
    }

    private void SetDirection(LinkDirection direction)
    {
        if (!ValidateEditContext()) return;

        TemporalGraph? updatedGraph = null;
        List<TemporalGraph>? updatedGraphs = null;

        if (ApplyToAllGraphs)
        {
            updatedGraphs = _graphSessionManager.UpdateSameLinkDirection(
                _currentSessionId!,
                CurrentGraph!.Index,
                SelectedLink!.NodeA,
                SelectedLink!.NodeB,
                direction);

            UpdateAllGraphsInCollection(updatedGraphs);
        }
        else
        {
            updatedGraph = _graphSessionManager.UpdateLinkDirection(
                _currentSessionId!,
                CurrentGraph!.Index,
                SelectedLink!.NodeA,
                SelectedLink!.NodeB,
                direction);

            UpdateGraphInCollection(updatedGraph);
        }
    }

    private bool ValidateEditContext()
    {
        return SelectedLink != null && _currentSessionId != null && CurrentGraph != null;
    }

    private void UpdateGraphInCollection(TemporalGraph updatedGraph)
    {
        var index = TemporalGraphs.IndexOf(CurrentGraph!);
        if (index >= 0)
        {
            TemporalGraphs[index] = updatedGraph;

            CurrentGraph = updatedGraph;
        }

        if (IntervalsViewModel != null)
        {
            IntervalsViewModel.Selected = CurrentGraph;
        }
    }

    private void UpdateAllGraphsInCollection(List<TemporalGraph> updatedGraphs)
    {
        var currentInterval = CurrentGraph?.Interval;

        TemporalGraphs.Clear();
        foreach (var g in updatedGraphs)
            TemporalGraphs.Add(g);

        if (currentInterval != null)
        {
            CurrentGraph = updatedGraphs.FirstOrDefault(g => g.Interval.Equals(currentInterval));
        }

        if (IntervalsViewModel != null)
        {
            IntervalsViewModel.Selected = CurrentGraph;
        }
    }


    #endregion

    private void ExportXml()
    {
        var path = _dialogService.ShowSaveFileDialog(
            "result.xml",
            "C:\\",
            "XML files (*.xml)|*.xml|All files (*.*)|*.*");

        if (path == null) return;

        try
        {
            _exportUseCase.Execute(path);
            _dialogService.ShowInfo($"Экспорт завершён: {path}");
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Ошибка экспорта: {ex.Message}");
        }
    }

    public void NotifyConfigChanged(StructConfiguration newConfig)
    {
        if (CurrentGraph != null && CurrentGraph.Interval.Equals(newConfig.Interval))
        {
            VisualGraphViewModel.UpdateConfiguration(newConfig);
        }
    }
}