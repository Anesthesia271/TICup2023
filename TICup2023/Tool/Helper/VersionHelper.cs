using System.Diagnostics;
using System.Reflection;

namespace TICup2023.Tool.Helper;

public static class VersionHelper
{
    public static string GetVersion() =>
        $"V{FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()?.Location!).ProductVersion} NET 70";
    
    public static string GetCopyRight() =>
        FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()?.Location!).LegalCopyright!;
}