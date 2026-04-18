using DynamicNetwork.Application.Interfaces.Providers;
using DynamicNetwork.Application.Interfaces.Repositories;
using DynamicNetwork.Application.Interfaces.UseCases.Configuration;
using DynamicNetwork.Domain.Configuration;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Presentation.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DynamicNetwork.Presentation.ViewModels.Configuration;

public class StructConfigurationViewModel : ViewModelBase
{
    private readonly MainViewModel _parent;
    private readonly IStructConfigurationRepository _configRepository;
    private readonly IEditStructConfigurationUseCase _editUseCase;
    private readonly IFunctionLibraryProvider _libraryProvider;

    private TimeInterval _selectedInterval = TimeInterval.Empty;
    private StructConfiguration? _currentConfiguration;
    private NodeConfigurationViewModel? _selectedNodeConfig;
    private LinkConfigurationViewModel? _selectedLinkConfig;

    public ObservableCollection<TimeInterval> AvailableIntervals { get; }
        = new ObservableCollection<TimeInterval>();

    public ObservableCollection<NodeConfigurationViewModel> NodeConfigurations { get; }
        = new ObservableCollection<NodeConfigurationViewModel>();

    public ObservableCollection<LinkConfigurationViewModel> LinkConfigurations { get; }
        = new ObservableCollection<LinkConfigurationViewModel>();

    public ObservableCollection<string> AvailableProcesses { get; }
        = new ObservableCollection<string>();

    public ObservableCollection<string> AvailableTransports { get; }
        = new ObservableCollection<string>();

    public ObservableCollection<string> AvailableStorages { get; }
        = new ObservableCollection<string>();

    public ObservableCollection<string> AvailableFlows { get; }
        = new ObservableCollection<string>();

    public event Action<TimeInterval>? IntervalChanged;

    public TimeInterval SelectedInterval
    {
        get => _selectedInterval;
        set
        {
            SetField(ref _selectedInterval, value);
            if (value != TimeInterval.Empty)
            {
                LoadConfigurationForInterval(value);
                IntervalChanged?.Invoke(value);
            }
        }
    }

    public NodeConfigurationViewModel? SelectedNodeConfig
    {
        get => _selectedNodeConfig;
        set
        {
            SetField(ref _selectedNodeConfig, value);
            if (value != null)
            {
                LoadAvailableProcesses();
                LoadAvailableStorages();
                LoadAvailableFlows();
            }
        }
    }

    public LinkConfigurationViewModel? SelectedLinkConfig
    {
        get => _selectedLinkConfig;
        set
        {
            SetField(ref _selectedLinkConfig, value);
            if (value != null)
            {
                LoadAvailableTransports();
            }
        }
    }

    public ICommand LoadFromLibraryCommand => new RelayCommand(LoadFromLibrary);

    public StructConfigurationViewModel(
        MainViewModel parent,
        IStructConfigurationRepository configRepository,
        IEditStructConfigurationUseCase editUseCase,
        IFunctionLibraryProvider libraryProvider)
    {
        _parent = parent;
        _configRepository = configRepository;
        _editUseCase = editUseCase;
        _libraryProvider = libraryProvider;
    }

    public void SetAvailableIntervals(IEnumerable<TemporalGraph> temporalGraphs)
    {
        AvailableIntervals.Clear();
        foreach (var graph in temporalGraphs)
        {
            AvailableIntervals.Add(graph.Interval);
        }

        if (AvailableIntervals.Count > 0 && SelectedInterval == TimeInterval.Empty)
        {
            SelectedInterval = AvailableIntervals[0];
        }
    }

    private void LoadConfigurationForInterval(TimeInterval interval)
    {
        NodeConfigurations.Clear();
        LinkConfigurations.Clear();

        _currentConfiguration = _configRepository.GetByInterval(interval);

        if (_currentConfiguration == null)
        {
            _currentConfiguration = CreateInitialConfigurationForInterval(interval);
            _configRepository.Add(_currentConfiguration);
        }

        foreach (var nodeConfig in _currentConfiguration.Nodes)
        {
            var nodeVM = new NodeConfigurationViewModel(nodeConfig, OnNodeConfigurationChanged);
            NodeConfigurations.Add(nodeVM);
        }

        foreach (var linkConfig in _currentConfiguration.Links)
        {
            var linkVM = new LinkConfigurationViewModel(linkConfig, OnLinkConfigurationChanged);
            LinkConfigurations.Add(linkVM);
        }

        LoadAvailableProcesses();
        LoadAvailableTransports();
        LoadAvailableStorages();
    }

    public void ResetForNewSession(IEnumerable<TemporalGraph> graphs)
    {
        NodeConfigurations.Clear();
        LinkConfigurations.Clear();
        AvailableIntervals.Clear();
        _currentConfiguration = null;
        _selectedInterval = TimeInterval.Empty;
        SelectedNodeConfig = null;
        SelectedLinkConfig = null;

        ClearRepositoryConfigurations();

        SetAvailableIntervals(graphs);

        CreateBaseConfigurationsForAllIntervals(graphs);

        if (AvailableIntervals.Count > 0)
        {
            SelectedInterval = AvailableIntervals[0];
        }
    }

    private void ClearRepositoryConfigurations()
    {
        var allConfigs = _configRepository.GetAll().ToList();
        foreach (var config in allConfigs)
        {
            _configRepository.Delete(config.Interval);
        }
    }

    private void CreateBaseConfigurationsForAllIntervals(IEnumerable<TemporalGraph> graphs)
    {
        foreach (var graph in graphs)
        {
            var interval = graph.Interval;

            if (_configRepository.Exists(interval))
                continue;

            var baseConfig = CreateInitialConfigurationForInterval(interval, graph);
            _configRepository.Add(baseConfig);
        }
    }

    private StructConfiguration CreateInitialConfigurationForInterval(TimeInterval interval, TemporalGraph? graph = null)
    {
        graph ??= _parent?.GetGraphByInterval(interval);

        if (graph == null)
        {
            _parent?.DialogService.ShowWarning(
                $"Не найден граф для интервала [{interval.Start} — {interval.End}]. Создана пустая конфигурация.");

            return new StructConfiguration(
                interval,
                Enumerable.Empty<NodeConfiguration>(),
                Enumerable.Empty<LinkConfiguration>());
        }

        var nodeConfigs = graph.AllNetworkNodes.Select(nodeId =>
            new NodeConfiguration(
                nodeId,
                Enumerable.Empty<string>(),
                new Dictionary<string, double>(),
                Enumerable.Empty<string>(),
                new Dictionary<string, double>(),
                Enumerable.Empty<string>()
            )).ToList();

        var linkConfigs = graph.Links.Select(link =>
            new LinkConfiguration(
                link.NodeA,
                link.NodeB,
                Enumerable.Empty<string>(),
                Enumerable.Empty<string>()
            )).ToList();

        return new StructConfiguration(interval, nodeConfigs, linkConfigs);
    }

    private StructConfiguration? SaveConfigurationToRepository()
    {
        if (_selectedInterval == TimeInterval.Empty || _currentConfiguration == null)
            return null;

        try
        {
            var nodeConfigs = NodeConfigurations.Select(n => n.ToDomainModel()).ToList();
            var linkConfigs = LinkConfigurations.Select(l => l.ToDomainModel()).ToList();

            var updatedConfig = new StructConfiguration(
                _selectedInterval,
                nodeConfigs,
                linkConfigs
            );

            _currentConfiguration = _editUseCase.Edit(updatedConfig.Interval, updatedConfig);

            return _currentConfiguration;
        }
        catch (Exception ex)
        {
            _parent.DialogService.ShowError($"Ошибка сохранения конфигурации: {ex.Message}");
            return null;
        }
    }

    private void OnNodeConfigurationChanged()
    {
        var config = SaveConfigurationToRepository();
        if (config != null)
        {
            _parent.NotifyConfigChanged(config);
        }
    }

    private void OnLinkConfigurationChanged()
    {
        var config = SaveConfigurationToRepository();
        if (config != null)
        {
            _parent.NotifyConfigChanged(config);
        }
    }

    private void LoadAvailableProcesses()
    {
        AvailableProcesses.Clear();
        var library = _libraryProvider.GetCurrent();
        foreach (var process in library.Processes)
        {
            AvailableProcesses.Add(process.Id);
        }
    }

    private void LoadAvailableTransports()
    {
        AvailableTransports.Clear();
        var library = _libraryProvider.GetCurrent();
        foreach (var transport in library.Transports)
        {
            AvailableTransports.Add(transport.Id);
        }
    }

    private void LoadAvailableStorages()
    {
        AvailableStorages.Clear();
        var library = _libraryProvider.GetCurrent();
        foreach (var storage in library.Storages)
        {
            AvailableStorages.Add(storage.Id);
        }
    }

    private void LoadAvailableFlows()
    {
        AvailableFlows.Clear();
        var library = _libraryProvider.GetCurrent();
        foreach (var flow in library.Flows)
        {
            AvailableFlows.Add(flow.Id);
        }
    }

    private void LoadFromLibrary()
    {
        LoadAvailableProcesses();
        LoadAvailableTransports();
        LoadAvailableStorages();
    }
}