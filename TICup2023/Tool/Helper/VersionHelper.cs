using System.Diagnostics;

namespace TICup2023.Tool.Helper;

public static class VersionHelper
{
    public static string GetVersion() =>
        $"V{Process.GetCurrentProcess().MainModule?.FileVersionInfo.ProductVersion} NET 70";

    public static string GetCopyRight() =>
        Process.GetCurrentProcess().MainModule?.FileVersionInfo.LegalCopyright ?? string.Empty;
}