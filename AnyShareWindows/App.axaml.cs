using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace AnyShareWindows;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private bool _allowExit = false;

    public ICommand OpenCommand { get; }
    public ICommand ExitCommand { get; }

    public App()
    {
        OpenCommand = new SimpleCommand(OpenWindow);
        ExitCommand = new SimpleCommand(ExitApp);
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        DataContext = this;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _mainWindow = new MainWindow();
            desktop.MainWindow = _mainWindow;

            desktop.ShutdownRequested += (_, e) =>
            {
                if (!_allowExit)
                {
                    e.Cancel = true;
                    _mainWindow?.Hide();
                }
            };

            _mainWindow.Closing += (_, e) =>
            {
                if (!_allowExit)
                {
                    e.Cancel = true;
                    _mainWindow.Hide();
                }
            };

            _mainWindow.Show();

            Dispatcher.UIThread.Post(() =>
            {
                _mainWindow.Hide();
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OpenWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow();
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ExitApp()
    {
        _allowExit = true;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}

public class SimpleCommand : ICommand
{
    private readonly Action _execute;

    public SimpleCommand(Action execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}