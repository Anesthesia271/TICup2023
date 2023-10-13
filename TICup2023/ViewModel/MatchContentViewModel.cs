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

    [ObservableProperty]
    private string _mapString = "11b 14B 44c 49C 69e 91E 41f 19F 75G 16g 25I 61i 65i 99j 47j 22J 53m 57m 88M\\n";

    public MatchContentViewModel()
    {
        MatchManager.NewMsgProduced += NewMessage;
    }

    [RelayCommand]
    private void InitMap()
    {
        WeakReferenceMessenger.Default.Send(new MapMessage(MapString.Replace("\\n", "\n")));
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
    private static void ChangeSize()
    {
        WeakReferenceMessenger.Default.Send(new ChangeSizeMessage());
    }

    private void NewMessage(string _)
    {
        OnPropertyChanged(nameof(MatchManager));
    }
}