using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TICup2023.ViewModel;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _testStr = "CORE字体测试";
    [ObservableProperty] private BitmapImage _camImage = new();

    [RelayCommand]
    private void ChangeStr()
    {
        TestStr += "s";
    }

    public MainWindowViewModel()
    {
        // new Thread(() =>
        // {
        //     var src = new Mat();
        //     var frame = Cv2.CreateFrameSource_Camera(0);
        //     while (true)
        //     {
        //         frame.NextFrame(src);
        //         var bm = src.ToBitmap();
        //         Application.Current.Dispatcher.Invoke(() => CamImage = BitmapToBitmapImage(bm));
        //         // CamImage = BitmapToBitmapImage(bm);
        //     }
        // }).Start();
    }

    private BitmapImage BitmapToBitmapImage(Image bitmap)
    {
        var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        var bit3 = new BitmapImage();
        bit3.BeginInit();
        bit3.StreamSource = ms;
        bit3.EndInit();
        return bit3;
    }
}