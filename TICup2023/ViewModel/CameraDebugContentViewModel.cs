using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HandyControl.Controls;
using OpenCvSharp;
using TICup2023.Model;

namespace TICup2023.ViewModel;

public record ResetBoundaryMessage;

public partial class CameraDebugContentViewModel : ObservableObject
{
    [ObservableProperty] private CameraManager _cameraManager = CameraManager.GetInstance();

    private int _clickTimes;

    public CameraDebugContentViewModel()
    {
        CameraManager.FrameUpdated += () => OnPropertyChanged(nameof(CameraManager));
        WeakReferenceMessenger.Default.Register<ResetBoundaryMessage>(this, (_, _) => _clickTimes = 0);
    }

    [RelayCommand]
    private void SetBoundary(MouseEventArgs e)
    {
        if (!CameraManager.IsCameraOpened)
        {
            Growl.Info("请先打开摄像头再标记边界点！");
            return;
        }

        if (CameraManager.IsBoundariesSet)
        {
            Growl.Info("请先手动重置边界点再重新标记！");
            return;
        }

        var point = e.GetPosition(e.Source as IInputElement);
        // point.Y = (e.Source as Image)!.ActualHeight - point.Y;

        if (_clickTimes == 3)
        {
            if (CameraManager.IsBoundariesValid(CameraManager.Boundaries[0],
                    CameraManager.Boundaries[1],
                    CameraManager.Boundaries[2],
                    new Point2f((float)point.X, (float)point.Y)))
            {
                CameraManager.Boundaries[_clickTimes] = new Point2f((float)point.X, (float)point.Y);
                _clickTimes = 0;
                Growl.Success("设置成功！");
            }
            else
            {
                CameraManager.ResetBoundaries();
                _clickTimes = 0;
                Growl.Warning("选择的边界点不合法！");
            }
        }
        else
        {
            CameraManager.Boundaries[_clickTimes] = new Point2f((float)point.X, (float)point.Y);
            _clickTimes++;
        }
    }
}