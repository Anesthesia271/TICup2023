using System;
using System.Threading.Tasks;
using System.Windows;
using NLog;
using NLog.Config;
using TICup2023.Model;

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

    protected override void OnStartup(StartupEventArgs e)
    {
        InitializeNLog();
        RegisterEvents();
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        CameraManager.GetInstance().CloseCamera();
    }

    private static void InitializeNLog()
    {
        var config = new LoggingConfiguration();
        var fileTarget = new NLog.Targets.FileTarget();
        config.AddTarget("file", fileTarget);
        fileTarget.FileName = "${basedir}/log.txt";
        fileTarget.Layout = @"[${date:format=HH\:mm\:ss}][${logger}] ${message}";
        var rule = new LoggingRule("*", LogLevel.Debug, fileTarget);
        config.LoggingRules.Add(rule);
        LogManager.Configuration = config;
    }

    private void RegisterEvents()
    {
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            if (e.Exception is Exception exception)
            {
                HandleException(exception);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        finally
        {
            e.SetObserved();
        }
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception exception)
            {
                HandleException(exception);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }

    private static void App_DispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            HandleException(e.Exception);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        finally
        {
            e.Handled = true;
        }
    }

    private static void HandleException(Exception ex)
    {
        LogManager.GetLogger("GlobalLogger").Error(ex.ToString());
    }
}