using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;
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

    private readonly Thread _cameraThread;
    private FrameSource _frameSource = new();
    public BitmapSource? CurrentFrame { get; private set; }

    public int MaxHue { get; set; }
    public int MinHue { get; set; }
    public int MinSaturation { get; set; }
    public int MaxLightness { get; set; }
    public int MinLightness { get; set; }
    public int MinArea { get; set; }
    public int GridCount { get; set; }
    public bool ShowGrid { get; set; }

    public FrameUpdated? FrameUpdated { get; set; }

    /// <summary>
    /// 构造方法，初始化ICamera的单例，包括CameraDevices的初始化
    /// </summary>
    private CameraManager()
    {
        _cameraThread = new Thread(UpdateFrame);

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
        _frameSource = Cv2.CreateFrameSource_Camera(SelectedCameraIndex);
        _cameraThread.Start();
    }

    private void UpdateFrame()
    {
        Mat src = new();
        while (true)
        {
            _frameSource.NextFrame(src);
            // TODO: 处理图像
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentFrame = src.ToBitmapSource();
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
}