using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using Point = OpenCvSharp.Point;

namespace TICup2023.Model;

public class CameraManager
{
    private static readonly CameraManager Instance = new();

    public FilterInfoCollection LocalWebCams { get; private set; } = new(FilterCategory.VideoInputDevice);
    public VideoCaptureDevice LocalWebCamPre { get; private set; }
    private VideoCaptureDevice LocalWebCam { get; set; } = new();
    public int SelectedCameraIndex { get; set; }
    public int SelectedResolutionIndex { get; set; }

    public bool IsCameraOpened { get; set; }

    public BitmapSource? CurrentFrame { get; private set; }

    public double MinH { get; set; } = 145;
    public double MaxH { get; set; } = 190;
    public double MinS { get; set; } = 137;
    public double MaxS { get; set; } = 255;
    public double MinV { get; set; } = 137;
    public double MaxV { get; set; } = 255;
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }
    public int MinArea { get; set; } = 100;
    public int MaxArea { get; set; } = 2000;
    public Point2f[] Boundaries { get; } = { new(-1, -1), new(-1, -1), new(-1, -1), new(-1, -1) };
    public bool IsBoundariesSet => Boundaries[3].X >= 0;
    public int GridCount { get; set; } = 10;
    public bool ShowGrid { get; set; } = true;

    public float CurrentPointX { get; private set; } = -1;
    public float CurrentPointY { get; private set; } = -1;

    public Action? FrameUpdated { get; set; }

    private CameraManager()
    {
        if (LocalWebCams.Count > 0)
        {
            SelectedCameraIndex = 0;
            LocalWebCamPre = new VideoCaptureDevice(LocalWebCams[SelectedCameraIndex].MonikerString);
            SelectedResolutionIndex = 0;
        }
        else
        {
            LocalWebCamPre = new VideoCaptureDevice();
        }
    }

    public static CameraManager GetInstance() => Instance;

    public void UpdateCameraDevices()
    {
        LocalWebCams = new FilterInfoCollection(FilterCategory.VideoInputDevice);
    }

    public void OpenCamera()
    {
        if (LocalWebCams.Count == 0)
            throw new Exception("当前设备无可用的摄像头");
        LocalWebCam = new VideoCaptureDevice(LocalWebCams[SelectedCameraIndex].MonikerString);
        if (SelectedResolutionIndex == -1)
        {
            LocalWebCam.VideoResolution = LocalWebCam.VideoCapabilities[0];
            SelectedResolutionIndex = 0;
        }
        else
        {
            LocalWebCam.VideoResolution = LocalWebCam.VideoCapabilities[SelectedResolutionIndex];
        }

        LocalWebCam.NewFrame += NewFrameAsync;
        LocalWebCam.Start();
        IsCameraOpened = true;
    }

    public void ChangeCamera()
    {
        LocalWebCamPre = new VideoCaptureDevice(LocalWebCams[SelectedCameraIndex].MonikerString);
    }

    public void CloseCamera()
    {
        LocalWebCam.SignalToStop();
        LocalWebCam.WaitForStop();
        IsCameraOpened = false;
    }

    public void ResetBoundaries()
    {
        for (var i = 0; i < 4; i++)
        {
            Boundaries[i].X = -1;
            Boundaries[i].Y = -1;
        }
    }

    private async void NewFrameAsync(object sender, NewFrameEventArgs eventArgs)
    {
        if (eventArgs.Frame.Clone() is not Bitmap bitmap) return;
        var frame = await FrameTransform(bitmap);
        Application.Current?.Dispatcher.Invoke(() =>
        {
            CurrentFrame = frame.ToBitmapSource();
            FrameUpdated?.Invoke();
        });
    }

    private Task<Mat> FrameTransform(Bitmap bitmap) =>
        Task.Run(() =>
        {
            var src = bitmap.ToMat();
            if (FlipX) Cv2.Flip(src, src, FlipMode.X);
            if (FlipY) Cv2.Flip(src, src, FlipMode.Y);
            return FrameProcessing(src);
        });

    private Mat FrameProcessing(Mat src)
    {
        var hsv = new Mat();
        Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV); // 转化为HSV

        var dst = new Mat();
        var scL = new Scalar(MinH, MinS, MinV);
        var scH = new Scalar(MaxH, MaxS, MaxV);
        Cv2.InRange(hsv, scL, scH, dst); // 获取HSV处理图片
        var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(20, 20),
            new Point(-1, -1));
        Cv2.Threshold(dst, dst, 0, 255, ThresholdTypes.Binary); // 二值化
        Cv2.Dilate(dst, dst, kernel); // 膨胀
        Cv2.Erode(dst, dst, kernel); // 腐蚀
        Cv2.FindContours(dst, out var contours, out _, RetrievalModes.External,
            ContourApproximationModes.ApproxTC89KCOS);

        PaintBoundaries(ref src);

        PaintGrid(ref src);

        PaintContours(ref src, contours.ToList());

        return src;
    }

    private void PaintContours(ref Mat src, List<Point[]> contours)
    {
        var perspectiveMatrix = Cv2.GetPerspectiveTransform(Boundaries, new[]
        {
            new Point2f(0, 0),
            new Point2f(GridCount - 1, 0),
            new Point2f(GridCount - 1, GridCount - 1),
            new Point2f(0, GridCount - 1)
        });

        for (var i = 0; i < contours.Count; i++)
        {
            var area = Cv2.ContourArea(contours[i]);
            if (area > MaxArea || area < MinArea)
            {
                contours.RemoveAt(i);
                i--;
                continue;
            }

            if (!IsBoundariesSet) continue;

            var moments = Cv2.Moments(contours[i]);
            var srcPoint = new Point2f((float)(moments.M10 / moments.M00), (float)(moments.M01 / moments.M00));
            var dstPoint = Cv2.PerspectiveTransform(new[] { srcPoint }, perspectiveMatrix)[0];
            if (dstPoint.X > -1 && dstPoint.X < GridCount &&
                dstPoint.Y > -1 && dstPoint.Y < GridCount)
            {
                Cv2.Circle(src, new Point(moments.M10 / moments.M00, moments.M01 / moments.M00),
                    2, new Scalar(0, 0, 255), -1);
            }
            else
            {
                contours.RemoveAt(i);
                i--;
            }
        }

        if (!contours.Any()) return;

        var momentsZero = Cv2.Moments(contours[0]);
        var srcZeroPoint = new Point2f((float)(momentsZero.M10 / momentsZero.M00),
            (float)(momentsZero.M01 / momentsZero.M00));
        var dstZeroPoint = Cv2.PerspectiveTransform(new[] { srcZeroPoint }, perspectiveMatrix)[0];

        CurrentPointX = dstZeroPoint.X;
        CurrentPointY = dstZeroPoint.Y;

        Cv2.DrawContours(src, contours, -1, new Scalar(0x85, 0x91, 0xf9));
        Cv2.DrawContours(src, contours, 0, new Scalar(77, 184, 45), 2);
    }

    private void PaintBoundaries(ref Mat src)
    {
        foreach (var boundary in Boundaries)
        {
            if (boundary is not { X: >= 0, Y: >= 0 }) continue;
            var x = boundary.X;
            var y = boundary.Y;
            Cv2.Line(src, new Point(x - 10, y - 10), new Point(x + 10, y + 10),
                new Scalar(243, 108, 50));
            Cv2.Line(src, new Point(x - 10, y + 10), new Point(x + 10, y - 10),
                new Scalar(243, 108, 50));
        }
    }

    private void PaintGrid(ref Mat src)
    {
        if (!ShowGrid || !IsBoundariesSet) return;

        var perspectiveMatrix = Cv2.GetPerspectiveTransform(new[]
        {
            new Point2f(0, 0),
            new Point2f(GridCount - 1, 0),
            new Point2f(GridCount - 1, GridCount - 1),
            new Point2f(0, GridCount - 1)
        }, Boundaries);

        for (var i = 0; i < GridCount; i++)
        {
            var srcPoint = new Point2f[]
            {
                new(0, i),
                new(GridCount - 1, i),
                new(i, 0),
                new(i, GridCount - 1)
            };
            var dstPoint = Cv2.PerspectiveTransform(srcPoint, perspectiveMatrix);
            Cv2.Line(src, dstPoint[0].ToPoint(), dstPoint[1].ToPoint(), new Scalar(243, 108, 50));
            Cv2.Line(src, dstPoint[2].ToPoint(), dstPoint[3].ToPoint(), new Scalar(243, 108, 50));
        }
    }

    public static bool IsBoundariesValid(Point2f leftTop, Point2f rightTop, Point2f rightBottom, Point2f leftBottom)
    {
        var vec1 = rightTop - leftTop;
        var vec2 = rightBottom - rightTop;
        var vec3 = leftBottom - rightBottom;
        var vec4 = leftTop - leftBottom;
        var cross1 = vec1.X * vec2.Y - vec1.Y * vec2.X;
        var cross2 = vec2.X * vec3.Y - vec2.Y * vec3.X;
        var cross3 = vec3.X * vec4.Y - vec3.Y * vec4.X;
        var cross4 = vec4.X * vec1.Y - vec4.Y * vec1.X;
        if (cross1 < 0 || cross2 < 0 || cross3 < 0 || cross4 < 0) return false;
        return !(leftTop.X > rightTop.X) &&
               !(rightTop.Y > rightBottom.Y) &&
               !(rightBottom.X < leftBottom.X) &&
               !(leftBottom.Y < leftTop.Y);
    }
}