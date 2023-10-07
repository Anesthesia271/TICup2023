using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HandyControl.Controls;
using TICup2023.Model;

namespace TICup2023.ViewModel;

public partial class MatchContentViewModel : ObservableObject
{
    [ObservableProperty] private SerialManager _serialManager = SerialManager.GetInstance();
    [ObservableProperty] private MatchManager _matchManager = MatchManager.GetInstance();
    [ObservableProperty] private CameraManager _cameraManager = CameraManager.GetInstance();

    [ObservableProperty] private string _mapString = "11a 13A 74b 57B 25I 61i 33d 63D 66e 31E 41F 17f 75G 16g\\n";

    public MatchContentViewModel()
    {
        MatchManager.NewMsgProduced += NewMessage;
    }
    
    [RelayCommand]
    private void InitMap()
    {
        WeakReferenceMessenger.Default.Send(new MapMessage(MapString));
    }

    [RelayCommand]
    private void StartMatch()
    {
        if (!CameraManager.IsCameraOpened || !CameraManager.IsBoundariesSet)
        {
            Growl.Info("请先打开并配置摄像头再开始比赛！");
            return;
        }

        if (!SerialManager.SerialPort.IsOpen)
        {
            Growl.Info("请先配置串口再开始比赛！");
            return;
        }

        MatchManager.StartMatch();
        OnPropertyChanged(nameof(MatchManager));
        InitMapCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void StopMatch()
    {
        MatchManager.StopMatch();
        OnPropertyChanged(nameof(MatchManager));
        InitMapCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ChangeSize()
    {
        WeakReferenceMessenger.Default.Send(new LogMessage());
    }

    private bool CanInitMapExecute() => !MatchManager.IsMatchStarted;

    private void NewMessage(string _)
    {
        OnPropertyChanged(nameof(MatchManager));
    }
}