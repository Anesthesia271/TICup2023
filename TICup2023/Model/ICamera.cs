using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using OpenCvSharp;

namespace TICup2023.Model;

/// <summary>
/// 对OpenCV的单例二次封装
/// </summary>
public class ICamera
{
    private static readonly ICamera _instance = new ICamera();
    
    public List<string> CameraDevices { get; set; }
    public string SelectedCameraDevice { get; set; }
    
    public int MaxHue { get; set; }
    public int MinHue { get; set; }
    public int MinSaturation { get; set; }
    public int MaxLightness { get; set; }
    public int MinLightness { get; set; }
    public int MinArea { get; set; }
    public int GridCount { get; set; }
    public bool ShowGrid { get; set; }
    
    /// <summary>
    /// 构造方法，初始化ICamera的单例，包括CameraDevices的初始化
    /// </summary>
    private ICamera()
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// 获取当前CameraHelper的实例
    /// </summary>
    /// <returns>返回当前CameraHelper的实例</returns>
    public static ICamera GetInstance()
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// 更新目前实例中存储的摄像头设备的名称
    /// </summary>
    public static void UpdateCameraDevices()
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// 获取当前帧的图像（识别经过识别处理过后的），若目前无SelectedCameraDevice，则返回null
    /// </summary>
    /// <returns>BitmapImage格式的图像</returns>
    public BitmapImage GetImage()
    {
        throw new NotImplementedException();
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