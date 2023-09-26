using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TICup2023.Model;

namespace TICup2023.ViewModel;

public partial class CameraContentViewModel : ObservableObject
{
    [ObservableProperty] private CameraManager _cameraManager = CameraManager.GetInstance();
    
    [RelayCommand]
    private async Task UpdateCameraDevicesAsync()
    {
        await Task.Run(() => CameraManager.UpdateCameraDevices());
        OnPropertyChanged(nameof(CameraManager));
    }
    
    [RelayCommand]
    private void OpenCamera()
    {
        CameraManager.OpenCamera();
    }
}