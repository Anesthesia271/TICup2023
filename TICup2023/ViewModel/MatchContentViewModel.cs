using CommunityToolkit.Mvvm.ComponentModel;
using TICup2023.Model;

namespace TICup2023.ViewModel;

public partial class MatchContentViewModel : ObservableObject
{
    [ObservableProperty] private MatchManager _matchManager = MatchManager.GetInstance();
    [ObservableProperty] private CameraManager _cameraManager = CameraManager.GetInstance();
}