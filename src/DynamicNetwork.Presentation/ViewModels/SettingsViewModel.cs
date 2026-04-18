namespace DynamicNetwork.Presentation.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private string _selectedLanguage = "ru";

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetField(ref _selectedLanguage, value))
            {
                App.ChangeLanguage(value);
            }
        }
    }

    public List<string> AvailableLanguages { get; } = new()
    {
        "ru",
        "en" 
    }; 
}
