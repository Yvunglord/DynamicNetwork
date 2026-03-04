namespace DynamicNetwork.Presentation.ViewModels.Configuration;

public class InputVolumesViewModel : ViewModelBase
{
    private readonly Action _onChanged;
    private string _flowId;
    private double _volume;

    public string FlowId
    {
        get => _flowId;
        set
        {
            if (SetField(ref _flowId, value))
                _onChanged?.Invoke();
        }
    }

    public double Volume
    {
        get => _volume;
        set
        {
            if (SetField(ref _volume, value))
                _onChanged?.Invoke();
        }
    }

    public InputVolumesViewModel(string flowId, double volume, Action onChanged)
    {
        _flowId = flowId;
        _volume = volume;
        _onChanged = onChanged;
    }
}
