using DynamicNetwork.Application.Dtos;
using DynamicNetwork.Application.Interfaces.Providers;
using DynamicNetwork.Application.Interfaces.Repositories;
using DynamicNetwork.Application.Interfaces.Session;
using DynamicNetwork.Application.Interfaces.UseCases.Analysis;
using DynamicNetwork.Application.Interfaces.UseCases.Configuration;
using DynamicNetwork.Application.Interfaces.UseCases.Graphs;
using DynamicNetwork.Application.Interfaces.UseCases.Library;
using DynamicNetwork.Application.Interfaces.UseCases.Reachability;
using DynamicNetwork.Domain.Enums;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Infrastructure.Adapters.VisualGraph;
using DynamicNetwork.Presentation.Commands;
using DynamicNetwork.Presentation.Services;
using DynamicNetwork.Presentation.ViewModels.Configuration;
using System.Collections.ObjectModel;
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
    private string? _currentSessionId;

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
                VisualGraphViewModel.SetGraph(value);
                AnalysisResult = _analyzerUseCase.Execute(value);
            }
        }
    }

    public Link? SelectedLink
    {
        get => _selectedLink;
        set => SetField(ref _selectedLink, value);
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
                _structConfigViewModel = new StructConfigurationViewModel(
                    this,
                    _configRepository,
                    _editStructUseCase,
                    _libraryProvider
                );
            return _structConfigViewModel;
        }
    }

    public ICommand LoadTemporalGraphCommand => new RelayCommand(LoadTemporalGraph);
    public ICommand LoadFunctionLibraryCommand => new RelayCommand(LoadFunctionLibrary);
    public ICommand MakeRightDirectionCommand => new RelayCommand(MakeRightDirection);
    public ICommand MakeLeftCommand => new RelayCommand(MakeLeftDirection);
    public ICommand MakeUndirectedCommand => new RelayCommand(MakeUndirected);
    public ICommand CycleDirectionCommand => new RelayCommand<Link>(CycleDirection);
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
        IManageFunctionLibraryUseCase manageLibraryUseCase)
    {
        MsaglGraphAdapter adapter = new MsaglGraphAdapter();
        _visualGraphViewModel = new VisualGraphViewModel(adapter);

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

    #region Link direction editing (ИСПРАВЛЕНО: иммутабельность)

    private void MakeRightDirection()
    {
        if (SelectedLink == null || _currentSessionId == null || CurrentGraph == null) return;

        var updatedGraph = _graphSessionManager.UpdateLinkDirection(
            _currentSessionId,
            CurrentGraph.Index,
            SelectedLink.NodeA,
            SelectedLink.NodeB,
            LinkDirection.Right);

        UpdateGraphInCollection(updatedGraph);
    }

    private void MakeUndirected()
    {
        if (SelectedLink == null || _currentSessionId == null || CurrentGraph == null) return;

        var updatedGraph = _graphSessionManager.UpdateLinkDirection(
            _currentSessionId,
            CurrentGraph.Index,
            SelectedLink.NodeA,
            SelectedLink.NodeB,
            LinkDirection.Undirected);

        UpdateGraphInCollection(updatedGraph);
    }

    private void MakeLeftDirection()
    {
        if (SelectedLink == null || _currentSessionId == null || CurrentGraph == null) return;

        var updatedGraph = _graphSessionManager.UpdateLinkDirection(
            _currentSessionId,
            CurrentGraph.Index,
            SelectedLink.NodeA,
            SelectedLink.NodeB,
            LinkDirection.Left);

        UpdateGraphInCollection(updatedGraph);
    }

    private void CycleDirection(Link? link)
    {
        if (link == null || _currentSessionId == null || CurrentGraph == null) return;

        var nextDirection = link.Direction switch
        {
            LinkDirection.Undirected => LinkDirection.Right,
            LinkDirection.Right => LinkDirection.Left,
            LinkDirection.Left => LinkDirection.Undirected,
            _ => LinkDirection.Undirected
        };

        var updatedGraph = _graphSessionManager.UpdateLinkDirectionCycled(
            _currentSessionId,
            CurrentGraph.Index,
            link.NodeA,
            link.NodeB);

        UpdateGraphInCollection(updatedGraph);
    }

    private void UpdateGraphInCollection(TemporalGraph updatedGraph)
    {
        var index = TemporalGraphs.IndexOf(CurrentGraph!);
        if (index >= 0)
        {
            TemporalGraphs[index] = updatedGraph;
            CurrentGraph = null;
            CurrentGraph = updatedGraph;
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
}