using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TICup2023.Model;

namespace TICup2023.ViewModel;

public partial class CameraDebugContentViewModel : ObservableObject
{
    [ObservableProperty] private CameraManager _cameraManager = CameraManager.GetInstance();

    public CameraDebugContentViewModel()
    {
        CameraManager.FrameUpdated += () => OnPropertyChanged(nameof(CameraManager));
    }

    [RelayCommand]
    private void Update()
    {
    }
}