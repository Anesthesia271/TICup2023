using System;
using System.Collections.Generic;
using System.Diagnostics;
using HandyControl.Controls;

namespace TICup2023.Model;

public delegate void NewMsgProduced(string msg);

public delegate void SerialSendText(string msg);

public struct Node
{
    public int X;
    public int Y;
    public char Value;
}

public enum Status
{
    Idle,
    Init,
    Running,
    WaitAnother,
    WaitConnMsg,
    WaitMsg
}

public class MatchManager
{
    private static readonly MatchManager Instance = new();

    private SerialManager SerialManager { get; } = SerialManager.GetInstance();

    private Status CurrentStatus { get; set; } = Status.Idle;
    private string _mapString = string.Empty;
    public List<Node> NodeList { get; } = new();
    public char[][] Map { get; set; } = new char[8][];

    public int[] MapSizeList { get; } = { 7, 8, 10 };
    public int MapSize { get; set; } = 8;
    public int StartResendTime { get; set; } = 3000;
    public int NormalResendTime { get; set; } = 1000;
    public int FailedTimes { get; set; } = 10;

    public bool IsMatchStarted { get; set; }

    public float ErrorRange { get; set; } = 0.3f;
    public int Score { get; private set; }
    public string Time { get; private set; } = "0:00.00";
    public int LeftStep { get; private set; } = 40;

    private readonly Stopwatch _stopwatch = new();
    private TimeSpan _lastTimeRecord;
    private TimeSpan _lastPosUpdateRecord;
    private int _failedTimes;

    private char _firstValue = ' ';
    private string _currentResendMsg = string.Empty;

    public float PosX { get; set; }
    public float PosY { get; set; }

    public int LastIntPosX { get; set; }
    public int LastIntPosY { get; set; }

    public int LastCommandIntPosX { get; set; }

    public int LastCommandIntPosY { get; set; }

    public int IntPosX { get; set; }
    public int IntPosY { get; set; }

    public NewMsgProduced? NewMsgProduced { get; set; }
    public SerialSendText? SerialSendText { get; set; }

    public static MatchManager GetInstance() => Instance;

    public bool InitMap(string mapString)
    {
        NodeList.Clear();
        for (var i = 0; i < mapString.Length - 3; i += 4)
        {
            var node = new Node
            {
                X = mapString[i] - '0',
                Y = mapString[i + 1] - '0',
                Value = mapString[i + 2]
            };
            if (node.X < 0 || node.X >= MapSize || node.Y < 0 || node.Y >= MapSize || node.Value < 'A' ||
                node.Value > 'z')
            {
                NodeList.Clear();
                return false;
            }

            NodeList.Add(node);
        }

        _mapString = mapString.Replace("\\n", "\n");

        Map = new char[MapSize][];
        for (var i = 0; i < MapSize; i++)
        {
            Map[i] = new char[MapSize];
            for (var j = 0; j < MapSize; j++)
            {
                Map[i][j] = ' ';
            }
        }

        foreach (var node in NodeList)
        {
            Map[node.X][node.Y] = node.Value;
        }

        return true;
    }

    public static int GetScore(char ch)
    {
        return ch is >= 'A' and <= 'Z' ? ch - 'A' + 1 : ch - 'a' + 1;
    }

    public void StartMatch()
    {
        if (!IsMapInitialized())
        {
            Growl.Info("请先初始化地图！");
            return;
        }

        SerialManager.DataReceived -= SerialDataReceived;
        SerialManager.DataReceived += SerialDataReceived;

        Score = 0;
        LeftStep = 40;
        _failedTimes = 0;
        IntPosX = 0;
        IntPosY = 0;
        LastIntPosX = 0;
        LastIntPosY = 0;
        LastCommandIntPosX = 0;
        LastCommandIntPosY = 0;
        IsMatchStarted = true;
        _lastTimeRecord = new TimeSpan();
        _lastPosUpdateRecord = new TimeSpan();
        NewMsgProduced?.Invoke("clear");
        NewMsgProduced?.Invoke("比赛开始");
        _stopwatch.Start();

        SerialSendText?.Invoke(_mapString);
        CurrentStatus = Status.Init;
        _lastTimeRecord = _stopwatch.Elapsed;
        _failedTimes = 0;
    }

    public void StopMatch()
    {
        IsMatchStarted = false;
        CurrentStatus = Status.Idle;
        _stopwatch.Stop();
        _stopwatch.Reset();
        NewMsgProduced?.Invoke("比赛结束");
        NewMsgProduced?.Invoke(string.Empty);
    }

    public void UpdatePos(float x, float y)
    {
        if (!IsMatchStarted) return;

        Time = _stopwatch.Elapsed.ToString(@"m\:ss\.ff");
        NewMsgProduced?.Invoke(string.Empty);

        PosX = x;
        PosY = MapSize - y - 1;

        if (Math.Abs((int)Math.Round(PosX) - IntPosX) <= 1 && Math.Abs((int)Math.Round(PosY) - IntPosY) <= 1)
        {
            LastIntPosX = IntPosX;
            LastIntPosY = IntPosY;
            IntPosX = (int)Math.Round(PosX);
            IntPosY = (int)Math.Round(PosY);
        }

        if (IntPosX != LastIntPosX || IntPosY != LastIntPosY)
        {
            if (_stopwatch.Elapsed - _lastPosUpdateRecord < TimeSpan.FromMilliseconds(100))
            {
                IntPosX = LastIntPosX;
                IntPosY = LastIntPosY;
            }
            else
            {
                if (IntPosX < 0 || IntPosX >= MapSize || IntPosY < 0 || IntPosY >= MapSize)
                {
                    NewMsgProduced?.Invoke($"小车位置 ({IntPosX}, {IntPosY})");
                    NewMsgProduced?.Invoke("小车离开了网格区域");
                    SerialSendText?.Invoke("S\n");
                    NewMsgProduced?.Invoke("""第1次发送"S\n"指令...""");
                    CurrentStatus = Status.WaitMsg;
                    _currentResendMsg = "S\n";
                    _lastTimeRecord = _stopwatch.Elapsed;
                    _failedTimes = 0;
                    StopMatch();
                    return;
                }

                LeftStep--;
                NewMsgProduced?.Invoke($"小车到达({IntPosX},{IntPosY})");
                if (LeftStep < 0)
                {
                    NewMsgProduced?.Invoke("小车剩余步数用完");
                    SerialSendText?.Invoke("E\n");
                    NewMsgProduced?.Invoke("""第1次发送"E\n"指令...""");
                    CurrentStatus = Status.WaitMsg;
                    _currentResendMsg = "E\n";
                    _lastTimeRecord = _stopwatch.Elapsed;
                    _failedTimes = 0;
                    StopMatch();
                    return;
                }
            }

            _lastPosUpdateRecord = _stopwatch.Elapsed;
        }

        if (Math.Abs(PosX - IntPosX) > ErrorRange && Math.Abs(PosY - IntPosY) > ErrorRange)
        {
            NewMsgProduced?.Invoke("小车离开了网格区域");
            SerialSendText?.Invoke("S\n");
            NewMsgProduced?.Invoke("""第1次发送"S\n"指令...""");
            CurrentStatus = Status.WaitMsg;
            _currentResendMsg = "S\n";
            _lastTimeRecord = _stopwatch.Elapsed;
            _failedTimes = 0;
            StopMatch();
            return;
        }

        switch (CurrentStatus)
        {
            case Status.Idle:
                break;
            case Status.Init:
            {
                StatusInit();
                break;
            }
            case Status.Running:
            {
                StatusRunning();
                break;
            }
            case Status.WaitAnother:
            {
                StatusWaitAnother();
                break;
            }
            case Status.WaitConnMsg:
            {
                StatusWaitConnMsg();
                break;
            }
            case Status.WaitMsg:
                StatusWaitMsg();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(CurrentStatus), "当前状态不在范围内");
        }
    }

    private void SerialDataReceived(string msg)
    {
        if (msg.Contains('B'))
        {
            if (CurrentStatus != Status.Init) return;
            NewMsgProduced?.Invoke("小车已接收完地图信息");
            CurrentStatus = Status.Running;
        }

        if (msg.Contains('1'))
        {
            if (Map[IntPosX][IntPosY] != ' ' && CurrentStatus == Status.WaitConnMsg)
            {
                LastCommandIntPosX = IntPosX;
                LastCommandIntPosY = IntPosY;
                SerialSendText?.Invoke("A\n");
                NewMsgProduced?.Invoke("""第1次发送"A\n"指令...""");
                CurrentStatus = Status.WaitMsg;
                _currentResendMsg = "A\n";
                _lastTimeRecord = _stopwatch.Elapsed;
                _failedTimes = 0;

                _firstValue = Map[IntPosX][IntPosY];
            }
            else
            {
                SerialSendText?.Invoke("F\n");
                NewMsgProduced?.Invoke("小车发送连接1指令位置不合法");
                NewMsgProduced?.Invoke("""第1次发送"F\n"指令...""");
                CurrentStatus = Status.WaitMsg;
                _currentResendMsg = "F\n";
                _lastTimeRecord = _stopwatch.Elapsed;
                _failedTimes = 0;
            }
        }

        if (msg.Contains('2'))
        {
            if (Math.Abs(_firstValue - Map[IntPosX][IntPosY]) == 32 && CurrentStatus == Status.WaitConnMsg)
            {
                LastCommandIntPosX = IntPosX;
                LastCommandIntPosY = IntPosY;
                SerialSendText?.Invoke("A\n");
                NewMsgProduced?.Invoke("""第1次发送"A\n"指令...""");
                CurrentStatus = Status.WaitMsg;
                _currentResendMsg = "A\n";
                _lastTimeRecord = _stopwatch.Elapsed;
                _failedTimes = 0;
            }
            else
            {
                SerialSendText?.Invoke("F\n");
                NewMsgProduced?.Invoke("小车发送连接2指令位置不合法");
                NewMsgProduced?.Invoke("""第1次发送"F\n"指令...""");
                CurrentStatus = Status.WaitMsg;
                _currentResendMsg = "F\n";
                _lastTimeRecord = _stopwatch.Elapsed;
                _failedTimes = 0;
            }
        }

        if (msg.Contains('F'))
        {
            NewMsgProduced?.Invoke("小车主动结束了比赛");
            StopMatch();
        }

        if (msg.Contains('R'))
        {
            if (CurrentStatus != Status.WaitMsg) return;
            NewMsgProduced?.Invoke("收到小车回复");
            if (_currentResendMsg == "A\n")
            {
                if (_firstValue == Map[IntPosX][IntPosY])
                {
                    NewMsgProduced?.Invoke("小车连接1成功");
                    CurrentStatus = Status.WaitAnother;
                }
                else
                {
                    NewMsgProduced?.Invoke("小车连接2成功");
                    Score += GetScore(Map[IntPosX][IntPosY]);
                    CurrentStatus = Status.Running;
                }
            }
            else if (_currentResendMsg == "F\n")
            {
                NewMsgProduced?.Invoke("收到小车回复");
                CurrentStatus = Status.Running;
            }
            else if (_currentResendMsg == "S\n")
            {
                NewMsgProduced?.Invoke("收到小车回复");
                StopMatch();
            }
            else if (_currentResendMsg == "E\n")
            {
                NewMsgProduced?.Invoke("收到小车回复");
                StopMatch();
            }
        }

        if (msg.Contains('P'))
        {
            SerialSendText?.Invoke($"{IntPosX}{IntPosY}\n");
            LeftStep--;
        }
    }

    private bool IsMapInitialized() => NodeList.Count != 0;

    private void StatusWaitConnMsg()
    {
        if (Map[IntPosX][IntPosY] == ' ' && Map[LastIntPosX][LastIntPosY] != ' ')
        {
            NewMsgProduced?.Invoke($"小车经过了功能点但并未发送连接指令，扣除2分");
            Score -= 2;
        }
    }

    private void StatusInit()
    {
        if (_stopwatch.Elapsed - _lastTimeRecord > TimeSpan.FromMilliseconds(StartResendTime))
        {
            if (_failedTimes >= FailedTimes)
            {
                NewMsgProduced?.Invoke($"第{_failedTimes}次发送地图信息失败");
                StopMatch();
            }

            SerialSendText?.Invoke(_mapString);
            _lastTimeRecord = _stopwatch.Elapsed;
            _failedTimes++;
            NewMsgProduced?.Invoke($"第{_failedTimes + 1}次发送地图信息...");
        }

        if (IntPosX == 0 && IntPosY == 0) return;
        NewMsgProduced?.Invoke("小车未确认地图信息便开始移动");
        StopMatch();
    }

    private void StatusRunning()
    {
        if (Map[IntPosX][IntPosY] != ' ' && (IntPosX != LastCommandIntPosX || IntPosY != LastCommandIntPosY))
        {
            CurrentStatus = Status.WaitConnMsg;
            NewMsgProduced?.Invoke("小车进入杆塔范围，等待小车发送连接指令...");
        }
    }

    private void StatusWaitAnother()
    {
        if (Map[IntPosX][IntPosY] != ' ' && (IntPosX != LastCommandIntPosX || IntPosY != LastCommandIntPosY))
        {
            CurrentStatus = Status.WaitConnMsg;
            NewMsgProduced?.Invoke("小车进入杆塔范围，等待小车发送连接指令...");
        }
    }

    private void StatusWaitMsg()
    {
        if (_stopwatch.Elapsed - _lastTimeRecord > TimeSpan.FromMilliseconds(NormalResendTime))
        {
            if (_failedTimes >= FailedTimes)
            {
                NewMsgProduced?.Invoke($"第{_failedTimes}次发送\"{_currentResendMsg.Replace("\n", @"\n")}\"指令失败，比赛中止");
                SerialSendText?.Invoke("S\n");
                StopMatch();
            }

            SerialSendText?.Invoke(_currentResendMsg);
            _lastTimeRecord = _stopwatch.Elapsed;
            _failedTimes++;
            NewMsgProduced?.Invoke($"第{_failedTimes}次发送\"{_currentResendMsg.Replace("\n", @"\n")}\"指令...");
        }
    }
}