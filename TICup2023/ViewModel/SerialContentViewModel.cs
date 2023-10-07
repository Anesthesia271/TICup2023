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
    [ObservableProperty] private MatchManager _matchManager = MatchManager.GetInstance();

    [ObservableProperty] private string[] _lineBreakList = { @"\r\n (CRLF)", @"\r (CR)", @"\n (LF)" };
    [ObservableProperty] private int _lineBreakSelectedIndex = 2;

    [ObservableProperty] private string _trainingTargetPos = "45";
    [ObservableProperty] private string _serialData = string.Empty;
    [ObservableProperty] private string _textToSend = string.Empty;
    [ObservableProperty] private bool _serialReceiveForward = true;
    [ObservableProperty] private bool _serialSendDisplay = true;
    [ObservableProperty] private bool _serialSendNewLine = true;
    [ObservableProperty] private bool _isOpening;

    private readonly string[] _lineBreaks = { "\r\n", "\r", "\n" };

    private bool IsPortOpen() => SerialManager.SerialPort.IsOpen;

    public SerialContentViewModel()
    {
        MatchManager.SerialSendText += SendText;
    }
    
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
            IsOpening = false;
        }
        catch (Exception e)
        {
            Growl.Warning($"打开串口失败，异常信息为：{e.Message}");
            IsOpening = false;
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
            await Task.Run(() => SerialManager.SendMsg("B\n"));
            if (!SerialSendDisplay) return;
            if (SerialData == string.Empty)
                SerialData += @"> B\n";
            else
                SerialData += @$"{Environment.NewLine}> B\n";
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
            await Task.Run(() => SerialManager.SendMsg("E\n"));
            if (!SerialSendDisplay) return;
            if (SerialData == string.Empty)
                SerialData += @"> E\n";
            else
                SerialData += @$"{Environment.NewLine}> E\n";
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
            await Task.Run(() => SerialManager.SendMsg($"{TrainingTargetPos}\n"));
            if (!SerialSendDisplay) return;
            if (SerialData == string.Empty)
                SerialData += @$"> {TrainingTargetPos}\n";
            else
                SerialData += @$"{Environment.NewLine}> {TrainingTargetPos}\n";
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
                var msg = $"{TextToSend.Replace(Environment.NewLine,
                    _lineBreaks[LineBreakSelectedIndex])}{_lineBreaks[LineBreakSelectedIndex]}";
                await Task.Run(() =>
                    SerialManager.SendMsg(msg));
                if (!SerialSendDisplay) return;
                if (SerialData == string.Empty)
                    SerialData += $"> {msg
                        .Replace("\r", @"\r")
                        .Replace("\n", @"\n")}";
                else
                    SerialData += $"{Environment.NewLine}> {msg
                        .Replace("\r", @"\r")
                        .Replace("\n", @"\n")}";
            }
            else
            {
                var msg = TextToSend.Replace(Environment.NewLine, _lineBreaks[LineBreakSelectedIndex]);
                await Task.Run(() => SerialManager.SendMsg(msg));
                if (!SerialSendDisplay) return;
                if (SerialData == string.Empty)
                    SerialData += $"> {msg
                        .Replace("\r", @"\r")
                        .Replace("\n", @"\n")}";
                else
                    SerialData += $"{Environment.NewLine}> {msg
                        .Replace("\r", @"\r")
                        .Replace("\n", @"\n")}";
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
            SerialData += $"< {msg.Replace("\r", @"\r").Replace("\n", @"\n")}";
        else
            SerialData += $"{Environment.NewLine}< {msg.Replace("\r", @"\r").Replace("\n", @"\n")}";
    }

    private void SendText(string msg)
    {
        if (!SerialManager.SerialPort.IsOpen) return;
        try
        {
            SerialManager.SendMsg(msg);
            if (!SerialSendDisplay) return;
            if (SerialData == string.Empty)
                SerialData += $"> {msg
                    .Replace("\r", @"\r")
                    .Replace("\n", @"\n")}";
            else
                SerialData += $"{Environment.NewLine}> {msg
                    .Replace("\r", @"\r")
                    .Replace("\n", @"\n")}";
        }
        catch (Exception e)
        {
            Growl.Warning($"发送信息失败，异常信息为：{e.Message}");
        }
    }
}