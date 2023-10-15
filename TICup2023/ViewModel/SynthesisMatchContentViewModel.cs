using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using HandyControl.Controls;
using TICup2023.Model;

namespace TICup2023.ViewModel;

public record ChangeSizeMessage;

public record MapMessage(string MapString);

public record NewLineMessage(int X1, int Y1, int X2, int Y2, int Score);

public partial class SynthesisMatchContentViewModel : ObservableObject
{
    [ObservableProperty] private SerialManager _serialManager = SerialManager.GetInstance();
    [ObservableProperty] private CameraManager _cameraManager = CameraManager.GetInstance();
    [ObservableProperty] private MatchManager _matchManager = MatchManager.GetInstance();

    [ObservableProperty] private Canvas _backgroundCanvas = new();
    [ObservableProperty] private Canvas _foregroundCanvas = new();

    [ObservableProperty] private string _matchInfo = string.Empty;

    private Dictionary<int, int> _colorIndexDict = new();

    private readonly Path _playerPath;

    private readonly SolidColorBrush[] _brushes =
    {
        new(Color.FromRgb(250, 208, 196)),
        new(Color.FromRgb(255, 209, 255)),
        new(Color.FromRgb(180, 144, 202)),
        new(Color.FromRgb(132, 250, 176)),
        new(Color.FromRgb(143, 211, 244)),
        new(Color.FromRgb(94, 231, 223)),
        new(Color.FromRgb(61, 182, 177)),
        new(Color.FromRgb(236, 140, 105)),
        new(Color.FromRgb(231, 98, 125)),
        new(Color.FromRgb(184, 35, 90)),
        new(Color.FromRgb(155, 35, 234)),
        new(Color.FromRgb(252, 0, 255)),
        new(Color.FromRgb(69, 58, 148))
    };

    public SynthesisMatchContentViewModel()
    {
        _playerPath = new Path
        {
            Height = 50,
            Stretch = Stretch.Uniform,
            Data = Application.Current.FindResource("PlayerGeometry") as Geometry
        };
        _playerPath.SetResourceReference(Shape.FillProperty, "PrimaryBrush");
        Panel.SetZIndex(_playerPath, 100);

        WeakReferenceMessenger.Default.Register<ChangeSizeMessage>(this, (_, _) => RePaintCanvas());
        WeakReferenceMessenger.Default.Register<MapMessage>(this, (_, message) => InitMap(message.MapString));

        CameraManager.FrameUpdated += () =>
        {
            OnPropertyChanged(nameof(CameraManager));
            _playerPath.SetValue(Canvas.LeftProperty, (double)30 + CameraManager.CurrentPointX * 100);
            _playerPath.SetValue(Canvas.TopProperty, (double)CameraManager.CurrentPointY * 100);
            MatchManager.UpdatePos(CameraManager.CurrentPointX, CameraManager.CurrentPointY);
        };

        MatchManager.NewMsgProduced += NewMessage;

        WeakReferenceMessenger.Default.Register<NewLineMessage>(this, (_, message) =>
        {
            // var line = new Line
            // {
            //     X1 = message.X1 * 100 + 50,
            //     Y1 = (MatchManager.MapSize - message.Y1 - 1) * 100 + 20,
            //     X2 = message.X2 * 100 + 50,
            //     Y2 = (MatchManager.MapSize - message.Y2 - 1) * 100 + 20,
            //     StrokeThickness = 3
            // };
            // line.Stroke = _brushes[_colorIndexDict[message.Score]];
            // ForegroundCanvas.Children.Add(line);
            
            var geometry = new PathGeometry();

            var pathLine = new Path
            {
                Stroke = _brushes[_colorIndexDict[message.Score]],
                StrokeThickness = 3,
                Data = geometry
            };
            var p2 = new Point(message.X1 * 100 + 50, (MatchManager.MapSize - message.Y1 - 1) * 100 + 20);
            var p1 = new Point(message.X2 * 100 + 50, (MatchManager.MapSize - message.Y2 - 1) * 100 + 20);

            var pathFigure = new PathFigure();
            geometry.Figures.Add(pathFigure);
            pathFigure.StartPoint = p1;

            var bezierSegment = new BezierSegment(p1,
                GetControlPoint(p1, p2), p2, true);

            pathFigure.Segments.Add(bezierSegment);

            ForegroundCanvas.Children.Add(pathLine);
        });

        RePaintCanvas();
    }

    private void RePaintCanvas()
    {
        BackgroundCanvas.Children.Clear();
        BackgroundCanvas.Height = MatchManager.MapSize * 100;
        BackgroundCanvas.Width = MatchManager.MapSize * 100;
        for (var i = 0; i < MatchManager.MapSize; i++)
        {
            var lineHorizontal = new Line
            {
                X1 = 50,
                Y1 = i * 100 + 50,
                X2 = MatchManager.MapSize * 100 - 50,
                Y2 = i * 100 + 50,
                StrokeThickness = 3
            };
            lineHorizontal.SetResourceReference(Shape.StrokeProperty, "BorderBrush");
            BackgroundCanvas.Children.Add(lineHorizontal);
            var lineVertical = new Line
            {
                X1 = i * 100 + 50,
                Y1 = 50,
                X2 = i * 100 + 50,
                Y2 = MatchManager.MapSize * 100 - 50,
                StrokeThickness = 3
            };
            lineVertical.SetResourceReference(Shape.StrokeProperty, "BorderBrush");
            BackgroundCanvas.Children.Add(lineVertical);
        }

        ForegroundCanvas.Children.Clear();
        ForegroundCanvas.Height = MatchManager.MapSize * 100;
        ForegroundCanvas.Width = MatchManager.MapSize * 100;

        ForegroundCanvas.Children.Add(_playerPath);
        _playerPath.SetValue(Canvas.LeftProperty, (double)36);
        _playerPath.SetValue(Canvas.TopProperty, (double)MatchManager.MapSize * 100 - 97);
    }

    private void InitMap(string mapString)
    {
        if (!MatchManager.InitMap(mapString))
        {
            Growl.Warning("地图初始化失败，请检查地图字符串是否正确！");
            return;
        }

        RePaintCanvas();

        _colorIndexDict = new Dictionary<int, int>();

        var colorIndex = 0;
        var lastScore = 0;

        foreach (var node in MatchManager.NodeList)
        {
            if (MatchManager.GetScore(node.Value) != lastScore)
            {
                colorIndex = colorIndex == 12 ? 0 : colorIndex + 1;
            }

            if (node.Value is >= 'a' and <= 'z')
            {
                var path = new Path
                {
                    Height = 55,
                    Stretch = Stretch.Uniform,
                    Fill = _brushes[colorIndex],
                    Data = Application.Current.FindResource("TowerGeometry") as Geometry
                };
                ForegroundCanvas.Children.Add(path);
                path.SetValue(Canvas.LeftProperty, (double)node.X * 100 + 25);
                path.SetValue(Canvas.TopProperty, (double)(MatchManager.MapSize - node.Y - 1) * 100);
            }
            else
            {
                var path = new Path
                {
                    Height = 55,
                    Stretch = Stretch.Uniform,
                    Fill = _brushes[colorIndex],
                    Data = Application.Current.FindResource("PowerStationGeometry") as Geometry
                };
                ForegroundCanvas.Children.Add(path);
                path.SetValue(Canvas.LeftProperty, (double)node.X * 100 + 25);
                path.SetValue(Canvas.TopProperty, (double)(MatchManager.MapSize - node.Y - 1) * 100);
            }

            if (!_colorIndexDict.ContainsKey(MatchManager.GetScore(node.Value)))
            {
                _colorIndexDict.Add(MatchManager.GetScore(node.Value), colorIndex);
            }

            lastScore = MatchManager.GetScore(node.Value);
        }

        Growl.Success("地图初始化成功！");


        // var line = new Line
        // {
        //     X1 = 1 * 100 + 50,
        //     Y1 = (MatchManager.MapSize - 1 - 1) * 100 + 20,
        //     X2 = 8 * 100 + 50,
        //     Y2 = (MatchManager.MapSize - 8 - 1) * 100 + 20,
        //     StrokeThickness = 3
        // };
        // line.SetResourceReference(Shape.StrokeProperty, "PrimaryBrush");
        // ForegroundCanvas.Children.Add(line);
    }

    private Point GetControlPoint(Point p1, Point p2)
    {
        double minY = Math.Min(p1.Y, p2.Y),
            maxY = Math.Max(p1.Y, p2.Y);

        return new Point
        {
            X = (p1.X + p2.X) / 2,
            Y = maxY / 4 * 3 + minY / 4 + Math.Abs(p1.X - p2.X) * 0.1
        };
    }

    private void NewMessage(string msg)
    {
        OnPropertyChanged(nameof(MatchManager));
        if (msg == string.Empty) return;
        if (msg == "clear")
        {
            MatchInfo = string.Empty;
            return;
        }

        if (MatchInfo == string.Empty)
            MatchInfo += $"{DateTime.Now:MM/dd HH:mm:ss} {msg}";
        else
            MatchInfo += $"{Environment.NewLine}{DateTime.Now:MM/dd HH:mm:ss} {msg}";
    }
}