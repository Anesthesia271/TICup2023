using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using Point = OpenCvSharp.Point;

namespace TICup2023.Model;

public delegate void FrameUpdated();

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

    public double MinH { get; set; } = 35;
    public double MaxH { get; set; } = 77;
    public double MinS { get; set; } = 43;
    public double MaxS { get; set; } = 255;
    public double MinV { get; set; } = 46;
    public double MaxV { get; set; } = 255;
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }
    public int MinArea { get; set; } = 100;
    public int MaxArea { get; set; } = 2000;
    public Point2f[] Boundaries { get; } = { new(-1, -1), new(-1, -1), new(-1, -1), new(-1, -1) };
    public bool IsBoundariesSet => Boundaries[3].X >= 0;
    public int GridCount { get; set; } = 8;
    public bool ShowGrid { get; set; } = true;

    public FrameUpdated? FrameUpdated { get; set; }

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

        LocalWebCam.NewFrame += NewFrame;
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

    private void NewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        var frame = FrameTransform(eventArgs.Frame.ToMat());
        Application.Current?.Dispatcher.Invoke(() =>
        {
            CurrentFrame = frame.ToBitmapSource();
            FrameUpdated?.Invoke();
        });
    }

    private Mat FrameTransform(Mat src)
    {
        if (FlipX) Cv2.Flip(src, src, FlipMode.X);

        if (FlipY) Cv2.Flip(src, src, FlipMode.Y);

        return ShowHsvProcess(src);
    }

    private Mat ShowHsvProcess(Mat src)
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
        Cv2.FindContours(dst, out var contours, out _, RetrievalModes.CComp,
            ContourApproximationModes.ApproxSimple);

        foreach (var boundary in Boundaries)
        {
            if (boundary is not { X: >= 0, Y: >= 0 }) continue;
            var x = boundary.X;
            var y = LocalWebCam.VideoResolution.FrameSize.Height - boundary.Y;
            // Cv2.Circle(src, new Point(x,y), 5,
            //     new Scalar(212, 188, 0), -1);
            Cv2.Line(src, new Point(x - 10, y -10), new Point(x + 10, y + 10),
                new Scalar(212, 188, 0));
            Cv2.Line(src, new Point(x - 10, y + 10), new Point(x + 10, y - 10),
                new Scalar(212, 188, 0));
        }

        if (ShowGrid && IsBoundariesSet)
        {
            var height = LocalWebCam.VideoResolution.FrameSize.Height;
            for (var i = 0; i < GridCount + 1; i++)
            {
                Cv2.Line(src, 
                    new Point(Boundaries[0].X + (Boundaries[1].X - Boundaries[0].X) / GridCount * i, 
                        height - (Boundaries[0].Y + (Boundaries[1].Y - Boundaries[0].Y) / GridCount * i)),
                    new Point(Boundaries[3].X + (Boundaries[2].X - Boundaries[3].X) / GridCount * i, 
                        height - (Boundaries[3].Y + (Boundaries[2].Y - Boundaries[3].Y) / GridCount * i)),
                    new Scalar(212, 188, 0));
                Cv2.Line(src,
                    new Point(Boundaries[0].X + (Boundaries[3].X - Boundaries[0].X) / GridCount * i,
                        height - (Boundaries[0].Y + (Boundaries[3].Y - Boundaries[0].Y) / GridCount * i)),
                    new Point(Boundaries[1].X + (Boundaries[2].X - Boundaries[1].X) / GridCount * i,
                        height - (Boundaries[1].Y + (Boundaries[2].Y - Boundaries[1].Y) / GridCount * i)),
                    new Scalar(212, 188, 0));
            }
        }

        if (contours.Length <= 0) return src;

        var boxes = contours.Select(Cv2.BoundingRect)
            .Where(w => w.Width * w.Height >= MinArea && w.Width * w.Height <= MaxArea);

        var imgTar = src.Clone();
        foreach (var rect in boxes)
            Cv2.Rectangle(imgTar,
                new Point(rect.X, rect.Y),
                new Point(rect.X + rect.Width, rect.Y + rect.Height),
                new Scalar(0, 0, 255));

        return imgTar;
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
        if (cross1 > 0 || cross2 > 0 || cross3 > 0 || cross4 > 0) return false;
        return !(leftTop.X > rightTop.X) &&
               !(rightTop.Y < rightBottom.Y) &&
               !(rightBottom.X < leftBottom.X) &&
               !(leftBottom.Y > leftTop.Y);
    }
}