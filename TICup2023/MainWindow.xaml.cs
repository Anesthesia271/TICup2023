using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HandyControl.Tools;
using HandyControl.Tools.Extension;
using TICup2023.UserControl;

namespace TICup2023;

public partial class MainWindow
{
    private GridLength _columnDefinitionWidth;

    public MainWindow()
    {
        InitializeComponent();
        NonClientAreaContent = new NonClientAreaContent();
    }

    private void OnLeftMainContentShiftOut(object sender, RoutedEventArgs e)
    {
        ButtonShiftOut.Collapse();

        var targetValue = -ColumnDefinitionLeft.Width.Value;
        _columnDefinitionWidth = ColumnDefinitionLeft.Width;

        var animation = AnimationHelper.CreateAnimation(targetValue, milliseconds: 200);
        animation.FillBehavior = FillBehavior.Stop;
        animation.Completed += OnAnimationCompleted!;
        LeftMainContent.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        return;

        void OnAnimationCompleted(object _, EventArgs args)
        {
            animation.Completed -= OnAnimationCompleted!;
            LeftMainContent.RenderTransform.SetCurrentValue(TranslateTransform.XProperty, targetValue);

            Grid.SetColumn(MainContent, 0);
            Grid.SetColumnSpan(MainContent, 2);

            ColumnDefinitionLeft.MinWidth = 0;
            ColumnDefinitionLeft.Width = new GridLength();
            ButtonShiftIn.Show();
        }
    }

    private void OnLeftMainContentShiftIn(object sender, RoutedEventArgs e)
    {
        ButtonShiftIn.Collapse();

        var targetValue = ColumnDefinitionLeft.Width.Value;
        // LeftMainContent.Width = 224;
        var animation = AnimationHelper.CreateAnimation(targetValue, milliseconds: 200);
        animation.FillBehavior = FillBehavior.Stop;
        animation.Completed += OnAnimationCompleted!;
        LeftMainContent.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        return;

        void OnAnimationCompleted(object _, EventArgs args)
        {
            animation.Completed -= OnAnimationCompleted!;
            LeftMainContent.RenderTransform.SetCurrentValue(TranslateTransform.XProperty, targetValue);

            Grid.SetColumn(MainContent, 1);
            Grid.SetColumnSpan(MainContent, 1);

            ColumnDefinitionLeft.MinWidth = 240;
            ColumnDefinitionLeft.Width = _columnDefinitionWidth;
            ButtonShiftOut.Show();
        }
    }
}