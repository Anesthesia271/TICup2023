using CommunityToolkit.Mvvm.ComponentModel;
using TICup2023.Tool.Helper;

namespace TICup2023.ViewModel;

public partial class AboutWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _productVersion = VersionHelper.GetVersion();
    [ObservableProperty] private string _copyright = VersionHelper.GetCopyRight();
}