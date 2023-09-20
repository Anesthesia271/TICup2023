using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using TICup2023.Model;

namespace TICup2023.ViewModel;

public partial class SerialContentViewModel : ObservableObject
{
    [ObservableProperty] private SerialManager _serialManager = SerialManager.GetInstance();

    [ObservableProperty] private string _trainingTargetPos = "45";
    [ObservableProperty] private string _serialData = string.Empty;
    [ObservableProperty] private string _textToSend = string.Empty;
    [ObservableProperty] private bool _serialReceiveForward = true;
    [ObservableProperty] private bool _serialSendDisplay = true;
    [ObservableProperty] private bool _serialSendNewLine = true;

    private bool IsPortOpen() => SerialManager.SerialPort.IsOpen;

    [RelayCommand]
    private void UpdatePortNameList()
    {
        SerialManager.UpdatePortNameList();
        if (SerialManager.PortNameList.Length == 0)
        {
            SerialManager.SerialPort.PortName = " ";
        }

        OnPropertyChanged(nameof(SerialManager));
    }

    [RelayCommand]
    private async Task OpenPortAsync()
    {
        try
        {
            SerialManager.DataReceived -= TextReceivedDisplay;
            SerialManager.DataReceived += TextReceivedDisplay;
            await Task.Run(SerialManager.OpenPort);
            OnPropertyChanged(nameof(SerialManager));
            SendStartCommand.NotifyCanExecuteChanged();
            SendEndCommand.NotifyCanExecuteChanged();
            SendPosCommand.NotifyCanExecuteChanged();
            SendTextCommand.NotifyCanExecuteChanged();
        }
        catch (Exception e)
        {
            Growl.Warning($"打开串口失败，异常信息为：{e.Message}");
        }
    }

    [RelayCommand]
    private void ClosePort()
    {
        try
        {
            SerialManager.ClosePort();
            OnPropertyChanged(nameof(SerialManager));
            SendStartCommand.NotifyCanExecuteChanged();
            SendEndCommand.NotifyCanExecuteChanged();
            SendPosCommand.NotifyCanExecuteChanged();
            SendTextCommand.NotifyCanExecuteChanged();
        }
        catch (Exception e)
        {
            Growl.Warning($"关闭串口失败，异常信息为：{e.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(IsPortOpen))]
    private async Task SendStartAsync()
    {
        try
        {
            await Task.Run(() => SerialManager.SendMsgLine("B"));
            if (!SerialSendDisplay) return;
            if (SerialData == string.Empty)
                SerialData += "> B\\n";
            else
                SerialData += "\n> B\\n";
        }
        catch (Exception e)
        {
            Growl.Warning($"发送信息失败，异常信息为：{e.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(IsPortOpen))]
    private async Task SendEndAsync()
    {
        try
        {
            await Task.Run(() => SerialManager.SendMsgLine("E"));
            if (!SerialSendDisplay) return;
            if (SerialData == string.Empty)
                SerialData += "> E\\n";
            else
                SerialData += "\n> E\\n";
        }
        catch (Exception e)
        {
            Growl.Warning($"发送信息失败，异常信息为：{e.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(IsPortOpen))]
    private async Task SendPosAsync()
    {
        try
        {
            await Task.Run(() => SerialManager.SendMsgLine(TrainingTargetPos));
            if (!SerialSendDisplay) return;
            if (SerialData == string.Empty)
                SerialData += $"> {TrainingTargetPos}\\n";
            else
                SerialData += $"\n> {TrainingTargetPos}\\n";
        }
        catch (Exception e)
        {
            Growl.Warning($"发送信息失败，异常信息为：{e.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(IsPortOpen))]
    private async Task SendTextAsync()
    {
        try
        {
            if (SerialSendNewLine)
            {
                await Task.Run(() => SerialManager.SendMsgLine(TextToSend.Replace("\r\n", "\n")));
                if (!SerialSendDisplay) return;
                if (SerialData == string.Empty)
                    SerialData += $"> {TextToSend.Replace("\r\n", "\n").Replace("\n", "\\n")}\\n";
                else
                    SerialData += $"\n> {TextToSend.Replace("\r\n", "\n").Replace("\n", "\\n")}\\n";
            }
            else
            {
                await Task.Run(() => SerialManager.SendMsg(TextToSend.Replace("\r\n", "\n")));
                if (!SerialSendDisplay) return;
                if (SerialData == string.Empty)
                    SerialData += $"> {TextToSend.Replace("\r\n", "\n").Replace("\n", "\\n")}";
                else
                    SerialData += $"\n> {TextToSend.Replace("\r\n", "\n").Replace("\n", "\\n")}";
            }
        }
        catch (Exception e)
        {
            Growl.Warning($"发送信息失败，异常信息为：{e.Message}");
        }
    }

    [RelayCommand]
    private void ClearSerialData()
    {
        SerialData = string.Empty;
    }

    private void TextReceivedDisplay(string msg)
    {
        if (!SerialReceiveForward || msg == string.Empty) return;
        if (SerialData == string.Empty)
            SerialData += $"< {msg.Replace("\r", "\\r").Replace("\n", "\\n")}";
        else
            SerialData += $"\n< {msg.Replace("\r", "\\r").Replace("\n", "\\n")}";
    }
}