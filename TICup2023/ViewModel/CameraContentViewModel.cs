using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using TICup2023.Model;

namespace TICup2023.ViewModel;

public partial class CameraContentViewModel : ObservableObject
{
    [ObservableProperty] private CameraManager _cameraManager = CameraManager.GetInstance();
    [ObservableProperty] private bool _isCameraStateChanging;
    
    [RelayCommand]
    private async Task UpdateCameraDevicesAsync()
    {
        await Task.Run(() => CameraManager.UpdateCameraDevices());
        OnPropertyChanged(nameof(CameraManager));
    }
    
    [RelayCommand]
    private async Task OpenCameraAsync()
    {
        try
        {
            await Task.Run(() => CameraManager.OpenCamera());
            OnPropertyChanged(nameof(CameraManager));
            IsCameraStateChanging = false;
        }
        catch (Exception e)
        {
            Growl.Warning($"打开摄像头失败，异常信息为：{e.Message}");
            IsCameraStateChanging = false;
        }
    }
    
    [RelayCommand]
    private async Task CloseCameraAsync()
    {
        await Task.Run(() => CameraManager.CloseCamera());
        OnPropertyChanged(nameof(CameraManager));
        IsCameraStateChanging = false;
    }
}