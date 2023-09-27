using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Point = OpenCvSharp.Point;

namespace TICup2023.Model;

public delegate void FrameUpdated();

/// <summary>
/// 对OpenCV的单例二次封装
/// </summary>
public class CameraManager
{
    private static readonly CameraManager Instance = new();

    public List<string> CameraDevices { get; } = new();
    public int SelectedCameraIndex { get; set; }
    public bool IsCameraOpened { get; set; }


    private CancellationTokenSource _tokenSource = new();
    private CancellationToken _token;

    private FrameSource _frameSource = new();
    public BitmapSource? CurrentFrame { get; private set; }

    public double MinH { get; set; } = 35;
    public double MaxH { get; set; } = 77;
    public double MinS { get; set; } = 43;
    public double MaxS { get; set; } = 255;
    public double MinV { get; set; } = 46;
    public double MaxV { get; set; } = 255;
    public int MinArea { get; set; }
    public int GridCount { get; set; }
    public bool ShowGrid { get; set; }

    public FrameUpdated? FrameUpdated { get; set; }

    /// <summary>
    /// 构造方法，初始化ICamera的单例，包括CameraDevices的初始化
    /// </summary>
    private CameraManager()
    {
        var searcher = new ManagementObjectSearcher(
            "SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')");
        foreach (var device in searcher.Get())
        {
            CameraDevices.Add(device["Name"].ToString() ?? string.Empty);
        }
    }

    /// <summary>
    /// 获取当前CameraHelper的实例
    /// </summary>
    /// <returns>返回当前CameraHelper的实例</returns>
    public static CameraManager GetInstance() => Instance;

    /// <summary>
    /// 更新目前实例中存储的摄像头设备的名称
    /// </summary>
    public void UpdateCameraDevices()
    {
        var searcher = new ManagementObjectSearcher(
            "SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')");
        CameraDevices.Clear();
        foreach (var device in searcher.Get())
        {
            CameraDevices.Add(device["Name"].ToString() ?? string.Empty);
        }
    }

    public void OpenCamera()
    {
        if (CameraDevices.Count == 0)
            throw new Exception("当前设备无可用的摄像头");
        _tokenSource = new CancellationTokenSource();
        _token = _tokenSource.Token;
        _frameSource = Cv2.CreateFrameSource_Camera(SelectedCameraIndex + 700);
        IsCameraOpened = true;
        Task.Run(UpdateFrame, _token);
    }

    public void CloseCamera()
    {
        _tokenSource.Cancel();
        // _frameSource.Reset();
        _frameSource.Dispose();
        IsCameraOpened = false;
    }

    private void UpdateFrame()
    {
        Mat src = new();
        while (!_token.IsCancellationRequested)
        {
            _frameSource.NextFrame(src);
            var frame = src.Flip(FlipMode.Y);
            var img = ShowHsvProcess(frame, MinH, MaxH, MinS, MaxS, MinV, MaxV);
            // TODO: 处理图像
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CurrentFrame = img.ToBitmapSource();
                FrameUpdated?.Invoke();
            });
        }
    }

    /// <summary>
    /// 设置网格边界点
    /// </summary>
    /// <param name="leftTop">左上边界点坐标</param>
    /// <param name="leftBottom">左下边界点坐标</param>
    /// <param name="rightTop">右上边界点坐标</param>
    /// <param name="rightBottom">右下边界点坐标</param>
    public void SetGrid(Point leftTop, Point leftBottom, Point rightTop, Point rightBottom)
    {
        throw new NotImplementedException();
    }

    private static Mat ShowHsvProcess(Mat src, double hMin, double hMax, double sMin, double sMax, double vMin, double vMax)
    {
        var hsv = new Mat();
        Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV); //转化为HSV

        var dst = new Mat();
        var scL = new Scalar(hMin, sMin, vMin);
        var scH = new Scalar(hMax, sMax, vMax);
        Cv2.InRange(hsv, scL, scH, dst); //获取HSV处理图片
        var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(20, 20),
            new Point(-1, -1));
        Cv2.Threshold(dst, dst, 0, 255, ThresholdTypes.Binary); //二值化
        Cv2.Dilate(dst, dst, kernel); //膨胀
        Cv2.Erode(dst, dst, kernel); //腐蚀
        Cv2.FindContours(dst, out var contours, out _, RetrievalModes.CComp,
            ContourApproximationModes.ApproxSimple);
        
        if (contours.Length <= 0) return src;
        
        var boxes = contours.Select(Cv2.BoundingRect).Where(w => w is { Height: >= 10, Width: > 10 });
        var imgTar = src.Clone();
        foreach (var rect in boxes)
        {
            Cv2.Rectangle(imgTar, new Point(rect.X, rect.Y),
                new Point(rect.X + rect.Width, rect.Y + rect.Height), new Scalar(0, 0, 255));
        }
        return imgTar;

    }
}