using System.Windows;
using System.Windows.Controls;
using TICup2023.View;

namespace TICup2023.UserControl;

public partial class NonClientAreaContent
{
    public NonClientAreaContent()
    {
        InitializeComponent();
    }

    private void ButtonColor_OnClick(object sender, RoutedEventArgs e)
    {
        PopupConfig.IsOpen = false;
        if (e.OriginalSource is not Button { Tag: string colorName }) return;
        PopupConfig.IsOpen = false;
        var resStr = $"pack://application:,,,/Resource/Style/Primary/{colorName}.xaml";
        ((App)Application.Current).UpdateResourceDictionary(resStr, 6);
    }

    private void ButtonTheme_OnClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Button { Tag: string themeName }) return;
        PopupConfig.IsOpen = false;
        var resStr = $"pack://application:,,,/Resource/Style/Theme/Base{themeName}.xaml";
        ((App)Application.Current).UpdateResourceDictionary(resStr, 5);
    }

    private void ButtonLang_OnClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Button { Tag: string langName }) return;
        PopupConfig.IsOpen = false;
        var resStr = $"pack://application:,,,/Resource/Lang/Lang.{langName}.xaml";
        ((App)Application.Current).UpdateResourceDictionary(resStr, 7);
    }

    private void ButtonAbout_OnClick(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = Application.Current.MainWindow }.ShowDialog();
    }
}