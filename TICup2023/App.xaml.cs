using System;
using System.Windows;

namespace TICup2023;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public void UpdateResourceDictionary(string resStr, int pos)
    {
        if (pos is < 5 or > 7) return;
        var resource = new ResourceDictionary { Source = new Uri(resStr) };
        Resources.MergedDictionaries.RemoveAt(pos);
        Resources.MergedDictionaries.Insert(pos, resource);
    }
}