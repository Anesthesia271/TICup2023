using CommunityToolkit.Mvvm.ComponentModel;
using TICup2023.Tool.Helper;

namespace TICup2023.ViewModel;

public partial class NonClientAreaContentViewModel : ObservableObject
{
    [ObservableProperty] private string _versionInfo = VersionHelper.GetVersion();
}