using CommunityToolkit.Mvvm.ComponentModel;
using TICup2023.Model;

namespace TICup2023.ViewModel;

public partial class CameraDebugContentViewModel : ObservableObject
{
    [ObservableProperty] private CameraManager _cameraManager = CameraManager.GetInstance();

    public CameraDebugContentViewModel()
    {
        CameraManager.FrameUpdated += () => OnPropertyChanged(nameof(CameraManager));
    }
}