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
    [ObservableProperty] private bool _serialReceiveDisplay = true;
    [ObservableProperty] private bool _serialSendDisplay = true;
    [ObservableProperty] private bool _serialSendNewLine = true;
    [ObservableProperty] private bool _isOpening;

    private readonly string[] _lineBreaks = { "\r\n", "\r", "\n" };

    private bool IsPortOpen() => SerialManager.SerialPort.IsOpen;

    public SerialContentViewModel()
    {
        SerialManager.DataReceived += TextReceivedDisplay;
        SerialManager.DataSent += TextSentDisplay;
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
            await Task.Run(SerialManager.OpenPort);
            NotifyAllPropertyChanged();
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
        if (MatchManager.IsMatchStarted)
        {
            Growl.Warning("请先停止比赛再关闭串口！");
            return;
        }

        try
        {
            SerialManager.ClosePort();
            NotifyAllPropertyChanged();
        }
        catch (Exception e)
        {
            Growl.Warning($"关闭串口失败，异常信息为：{e.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(IsPortOpen))]
    private void SendPos()
    {
        if (TrainingTargetPos == string.Empty) return;
        try
        {
            SerialManager.SendMsgAsync($"{TrainingTargetPos}\n");
        }
        catch (Exception e)
        {
            Growl.Warning($"发送信息失败，异常信息为：{e.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(IsPortOpen))]
    private void SendText()
    {
        if (TextToSend == string.Empty) return;
        try
        {
            if (SerialSendNewLine)
            {
                var msg = $"{TextToSend.Replace(Environment.NewLine,
                    _lineBreaks[LineBreakSelectedIndex])}{_lineBreaks[LineBreakSelectedIndex]}";
                SerialManager.SendMsgAsync(msg);
            }
            else
            {
                var msg = TextToSend.Replace(Environment.NewLine, _lineBreaks[LineBreakSelectedIndex]);
                SerialManager.SendMsgAsync(msg);
            }
        }
        catch (Exception e)
        {
            Growl.Warning($"发送信息失败，异常信息为：{e.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(IsPortOpen))]
    private void SendTextModify(string par)
    {
        if (par == string.Empty) return;
        try
        {
            SerialManager.SendMsgAsync(par);
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
        if (!SerialReceiveDisplay || msg == string.Empty) return;
        if (SerialData == string.Empty)
            SerialData += $"< {msg
                .Replace("\r", @"\r")
                .Replace("\n", @"\n")}";
        else
            SerialData += $"{Environment.NewLine}< {msg
                .Replace("\r", @"\r")
                .Replace("\n", @"\n")}";
    }

    private void TextSentDisplay(string msg)
    {
        if (!SerialSendDisplay || msg == string.Empty) return;
        if (SerialData == string.Empty)
            SerialData += $"> {msg.
                Replace("\r", @"\r")
                .Replace("\n", @"\n")}";
        else
            SerialData += $"{Environment.NewLine}> {msg
                .Replace("\r", @"\r")
                .Replace("\n", @"\n")}";
    }

    private void NotifyAllPropertyChanged()
    {
        OnPropertyChanged(nameof(SerialManager));
        OnPropertyChanged(nameof(MatchManager));
        SendPosCommand.NotifyCanExecuteChanged();
        SendTextCommand.NotifyCanExecuteChanged();
        SendTextModifyCommand.NotifyCanExecuteChanged();
    }
}