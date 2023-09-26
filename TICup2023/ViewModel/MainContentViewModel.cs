using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TICup2023.UserControl;

namespace TICup2023.ViewModel;

public partial class MainContentViewModel : ObservableObject
{
    private CameraDebugContent CameraDebugContent { get; } = new();
    private SynthesisMatchContent SynthesisMatchContent { get; } = new();

    [ObservableProperty] private string _contentTitle = "视频调试";
    [ObservableProperty] private object _subContent;

    public MainContentViewModel()
    {
        SubContent = CameraDebugContent;
    }

    [RelayCommand]
    private void Switch()
    {
        if (SubContent is CameraDebugContent)
        {
            ContentTitle = "综合";
            SubContent = SynthesisMatchContent;
        }
        else
        {
            ContentTitle = "视频调试";
            SubContent = CameraDebugContent;
        }
    }
}