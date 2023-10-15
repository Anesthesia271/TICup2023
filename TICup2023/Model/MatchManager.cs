using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using HandyControl.Controls;
using TICup2023.ViewModel;

namespace TICup2023.Model;

public struct Node
{
    public int X;
    public int Y;
    public char Value;
}

public struct NodeWithTopology
{
    public int X;
    public int Y;
    public char Value;
    public List<NodeWithTopology> ConnectedNodes;
}

public struct PosInt
{
    public int X;
    public int Y;
}

public struct PosFloat
{
    public float X;
    public float Y;
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
    private Dictionary<int, List<NodeWithTopology>> TopologyList { get; } = new();
    private char[][] Map { get; set; } = new char[8][];
    private bool[][] Connected { get; set; } = new bool[8][];

    public int[] MapSizeList { get; } = { 7, 8, 9, 10 };
    public int MapSize { get; set; } = 10;
    public int StartResendTime { get; set; } = 3000;
    public int NormalResendTime { get; set; } = 1000;
    public int MaxFailedTimes { get; set; } = 10;

    public bool IsMatchStarted { get; set; }

    public float ErrorRange { get; set; } = 0.4f;
    public float Score { get; private set; }

    private float _subScore;

    public string Time { get; private set; } = "0:00.00";
    public int LeftStep { get; private set; } = 55;

    private readonly Stopwatch _stopwatch = new();
    private TimeSpan _lastTimeRecord;
    private int _failedTimes;

    private PosInt _firstPos = new() { X = 0, Y = 0 };
    private string _currentResendMsg = string.Empty;

    private float PosX { get; set; }
    private float PosY { get; set; }
    private int LastIntPosX { get; set; }
    private int LastIntPosY { get; set; }
    private int LastCommandIntPosX { get; set; }
    private int LastCommandIntPosY { get; set; }
    private int IntPosX { get; set; }
    private int IntPosY { get; set; }

    public Action<string>? NewMsgProduced { get; set; }

    public static MatchManager GetInstance() => Instance;

    public bool InitMap(string mapString)
    {
        NodeList.Clear();
        for (var i = 0; i < mapString.Length - 2; i += 4)
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

        TopologyList.Clear();
        for (var i = 0; i < NodeList.Count; i++)
        {
            var node = new NodeWithTopology
            {
                X = NodeList[i].X,
                Y = NodeList[i].Y,
                Value = NodeList[i].Value,
                ConnectedNodes = new List<NodeWithTopology>()
            };
            if (TopologyList.TryGetValue(GetScore(node.Value), out var value))
            {
                value.Add(node);
            }
            else
            {
                TopologyList.Add(GetScore(node.Value), new List<NodeWithTopology> { node });
            }
        }

        _mapString = mapString.Replace("\\n", "\n");

        Map = new char[MapSize][];
        for (var i = 0; i < MapSize; i++)
        {
            Map[i] = new char[MapSize];
            Array.Fill(Map[i], ' ');
        }

        foreach (var node in NodeList)
        {
            Map[node.X][node.Y] = node.Value;
        }

        Connected = new bool[MapSize][];
        for (var i = 0; i < MapSize; i++)
        {
            Connected[i] = new bool[MapSize];
            Array.Fill(Connected[i], false);
        }

        return true;
    }

    public static int GetScore(char ch) => ch is >= 'A' and <= 'Z' ? ch - 'A' + 1 : ch - 'a' + 1;

    public async void StartMatch()
    {
        if (!IsMapInitialized())
        {
            Growl.Info("请先初始化地图！");
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            WeakReferenceMessenger.Default.Send(new MapMessage(_mapString));
        });

        SerialManager.DataReceived -= SerialDataReceived;
        SerialManager.DataReceived += SerialDataReceived;

        Score = 0;
        _subScore = 0;
        LeftStep = 55;
        _failedTimes = 0;
        IntPosX = 0;
        IntPosY = 0;
        LastIntPosX = 0;
        LastIntPosY = 0;
        LastCommandIntPosX = 0;
        LastCommandIntPosY = 0;

        TopologyList.Clear();
        for (var i = 0; i < NodeList.Count; i++)
        {
            var node = new NodeWithTopology
            {
                X = NodeList[i].X,
                Y = NodeList[i].Y,
                Value = NodeList[i].Value,
                ConnectedNodes = new List<NodeWithTopology>()
            };
            if (TopologyList.TryGetValue(GetScore(node.Value), out var value))
            {
                value.Add(node);
            }
            else
            {
                TopologyList.Add(GetScore(node.Value), new List<NodeWithTopology> { node });
            }
        }

        NewMsgProduced?.Invoke("尝试发送硬重置请求");
        SerialManager.SendMsgAsync("R\n");
        await Task.Run(() => Thread.Sleep(3000));

        IsMatchStarted = true;
        _lastTimeRecord = new TimeSpan();
        NewMsgProduced?.Invoke("clear");
        NewMsgProduced?.Invoke("比赛开始");
        _stopwatch.Start();
        SerialManager.SendMsgAsync(_mapString);
        NewMsgProduced?.Invoke("第1次发送地图信息...");
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

        if (Math.Abs((int)Math.Round(PosX) - IntPosX) <= 1 && Math.Abs((int)Math.Round(PosY) - IntPosY) <= 1 &&
            (PosX - Math.Floor(PosX) < ErrorRange || PosX - Math.Floor(PosX) > 1 - ErrorRange) &&
            (PosY - Math.Floor(PosY) < ErrorRange || PosY - Math.Floor(PosY) > 1 - ErrorRange))
        {
            LastIntPosX = IntPosX;
            LastIntPosY = IntPosY;
            IntPosX = (int)Math.Round(PosX);
            IntPosY = (int)Math.Round(PosY);
        }

        if (IntPosX != LastIntPosX || IntPosY != LastIntPosY)
        {
            if (IntPosX < 0 || IntPosX >= MapSize || IntPosY < 0 || IntPosY >= MapSize)
            {
                NewMsgProduced?.Invoke($"小车位置 ({IntPosX}, {IntPosY})");
                NewMsgProduced?.Invoke("小车离开了网格区域");
                SerialManager.SendMsgAsync("S\n");
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
                SerialManager.SendMsgAsync("E\n");
                NewMsgProduced?.Invoke("""第1次发送"E\n"指令...""");
                CurrentStatus = Status.WaitMsg;
                _currentResendMsg = "E\n";
                _lastTimeRecord = _stopwatch.Elapsed;
                _failedTimes = 0;
                StopMatch();
                return;
            }
        }

        if (Math.Abs(PosX - IntPosX) > ErrorRange && Math.Abs(PosY - IntPosY) > ErrorRange)
        {
            NewMsgProduced?.Invoke("小车离开了网格区域");
            SerialManager.SendMsgAsync("S\n");
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
        if (!IsMatchStarted) return;
        if (msg.Contains('B'))
        {
            if (CurrentStatus != Status.Init) return;
            NewMsgProduced?.Invoke("小车已接收完地图信息");
            CurrentStatus = Status.Running;
        }

        if (msg.Contains('1'))
        {
            if (Map[IntPosX][IntPosY] != ' ')
            {
                LastCommandIntPosX = IntPosX;
                LastCommandIntPosY = IntPosY;
                SerialManager.SendMsgAsync("A\n");
                NewMsgProduced?.Invoke("""第1次发送"A\n"指令...""");
                CurrentStatus = Status.WaitMsg;
                _currentResendMsg = "A\n";
                _lastTimeRecord = _stopwatch.Elapsed;
                _failedTimes = 0;

                _firstPos.X = IntPosX;
                _firstPos.Y = IntPosY;
            }
            else
            {
                NewMsgProduced?.Invoke("小车发送连接1指令位置不合法");
                SerialManager.SendMsgAsync("F\n");
                NewMsgProduced?.Invoke("""第1次发送"F\n"指令...""");
                CurrentStatus = Status.WaitMsg;
                _currentResendMsg = "F\n";
                _lastTimeRecord = _stopwatch.Elapsed;
                _failedTimes = 0;
            }
        }

        if (msg.Contains('2'))
        {
            if (Math.Abs(Map[_firstPos.X][_firstPos.Y] - Map[IntPosX][IntPosY]) is 0 or 32 &&
                (_firstPos.X != IntPosX || _firstPos.Y != IntPosY) &&
                CurrentStatus == Status.WaitConnMsg)
            {
                LastCommandIntPosX = IntPosX;
                LastCommandIntPosY = IntPosY;
                SerialManager.SendMsgAsync("A\n");
                NewMsgProduced?.Invoke("""第1次发送"A\n"指令...""");
                CurrentStatus = Status.WaitMsg;
                _currentResendMsg = "A\n";
                _lastTimeRecord = _stopwatch.Elapsed;
                _failedTimes = 0;
            }
            else
            {
                NewMsgProduced?.Invoke("小车发送连接2指令位置不合法");
                SerialManager.SendMsgAsync("F\n");
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
                if (_firstPos.X == IntPosX && _firstPos.Y == IntPosY)
                {
                    NewMsgProduced?.Invoke("小车连接1成功");
                    CurrentStatus = Status.WaitAnother;
                }
                else
                {
                    NewMsgProduced?.Invoke("小车连接2成功");

                    var firstNodeWithTopology = TopologyList[GetScore(Map[_firstPos.X][_firstPos.Y])]
                        .Find(node => node.X == _firstPos.X && node.Y == _firstPos.Y);

                    var nodeWithTopology = TopologyList[GetScore(Map[IntPosX][IntPosY])]
                        .Find(node => node.X == IntPosX && node.Y == IntPosY);

                    if (firstNodeWithTopology.X != nodeWithTopology.X || firstNodeWithTopology.Y != nodeWithTopology.Y)
                    {
                        if (!firstNodeWithTopology.ConnectedNodes.Contains(nodeWithTopology))
                        {
                            firstNodeWithTopology.ConnectedNodes.Add(nodeWithTopology);
                        }

                        if (!nodeWithTopology.ConnectedNodes.Contains(firstNodeWithTopology))
                        {
                            nodeWithTopology.ConnectedNodes.Add(firstNodeWithTopology);
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        WeakReferenceMessenger.Default.Send(new NewLineMessage(_firstPos.X, _firstPos.Y, IntPosX,
                            IntPosY, GetScore(Map[_firstPos.X][_firstPos.Y])));
                    });

                    Score = CalculateScore() - _subScore;
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
            SerialManager.SendMsgAsync($"{IntPosX}{IntPosY}\n");
            LeftStep--;
        }
    }

    private float CalculateScore() => TopologyList.Sum(item => (
        from node in item.Value
        where node.ConnectedNodes.Count != 0 && node.Value is >= 'a' and <= 'z'
        select CalculateScoreHelper(node)
        into connectedPower
        where connectedPower != 0
        select (float)(item.Key * Math.Pow(1.2, connectedPower - 1))).Sum());

    private static int CalculateScoreHelper(NodeWithTopology node)
    {
        var visitedNodes = new HashSet<NodeWithTopology>();
        return CalculateScoreDfs(node, visitedNodes);
    }

    private static int CalculateScoreDfs(NodeWithTopology node, ISet<NodeWithTopology> visitedNodes)
    {
        visitedNodes.Add(node);
        return (node.Value is >= 'A' and <= 'Z' ? 1 : 0) + node.ConnectedNodes
            .Where(connectedNode => !visitedNodes.Contains(connectedNode))
            .Sum(connectedNode => CalculateScoreDfs(connectedNode, visitedNodes));
    }

    private bool IsMapInitialized() => NodeList.Count != 0;

    private void StatusWaitConnMsg()
    {
        if (LastIntPosX == IntPosX && LastIntPosY == IntPosY) return;
        if (Map[LastIntPosX][LastIntPosY] == ' ') return;
        NewMsgProduced?.Invoke("小车经过了功能点但并未发送连接指令，扣除2分");
        _subScore += 2;
        Score = CalculateScore() - _subScore;
        CurrentStatus = Status.Running;
    }

    private void StatusInit()
    {
        if (_stopwatch.Elapsed - _lastTimeRecord > TimeSpan.FromMilliseconds(StartResendTime))
        {
            if (_failedTimes >= MaxFailedTimes)
            {
                NewMsgProduced?.Invoke($"第{_failedTimes + 1}次发送地图信息失败");
                StopMatch();
            }

            SerialManager.SendMsgAsync(_mapString);
            _lastTimeRecord = _stopwatch.Elapsed;
            _failedTimes++;
            NewMsgProduced?.Invoke($"第{_failedTimes + 1}次发送地图信息...");
        }

        if (IntPosX == 0 && IntPosY == 0) return;
        // TODO
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
            if (_failedTimes >= MaxFailedTimes)
            {
                NewMsgProduced?.Invoke($"第{_failedTimes + 1}次发送\"{_currentResendMsg.Replace("\n", @"\n")}\"指令失败，比赛中止");
                SerialManager.SendMsgAsync("S\n");
                StopMatch();
            }

            SerialManager.SendMsgAsync(_currentResendMsg);
            _lastTimeRecord = _stopwatch.Elapsed;
            _failedTimes++;
            NewMsgProduced?.Invoke($"第{_failedTimes + 1}次发送\"{_currentResendMsg.Replace("\n", @"\n")}\"指令...");
        }
    }
}