using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace TICup2023.ViewModel;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _testStr = "CORE字体测试";
    [ObservableProperty] private BitmapImage _camImage = new();

    private int _colorFlag;
    private int _themeFlag;

    public ICommand ChangeThemeCommand { get; }
    public ICommand ChangeColorCommand { get; }

    [RelayCommand]
    private void ChangeStr()
    {
        TestStr += "s";
    }

    public MainWindowViewModel()
    {
        ChangeColorCommand = new RelayCommand(() =>
        {
            _colorFlag = (_colorFlag + 1) % 4;
            var resStr = _colorFlag switch
            {
                1 => "pack://application:,,,/Resource/Style/Primary/Gold.xaml",
                2 => "pack://application:,,,/Resource/Style/Primary/Violet.xaml",
                3 => "pack://application:,,,/Resource/Style/Primary/Magenta.xaml",
                _ => "pack://application:,,,/Resource/Style/Primary/Primary.xaml"
            };
            ((App)Application.Current).UpdateResourceDictionary(resStr, 6);
        });

        ChangeThemeCommand = new RelayCommand(() =>
        {
            _themeFlag = (_themeFlag + 1) % 2;
            var resourceStr = "pack://application:,,,/Resource/Style/Theme/BaseLight.xaml";
            if (_themeFlag == 1)
            {
                resourceStr = "pack://application:,,,/Resource/Style/Theme/BaseDark.xaml";
            }

            ((App)Application.Current).UpdateResourceDictionary(resourceStr, 5);
        });

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