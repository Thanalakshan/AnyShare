using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AnyShareWindows;

public partial class TaskbarWidgetWindow : Window
{
    private const int WidgetWidth = 150;
    private const int WidgetHeight = 38;

    private readonly DispatcherTimer _attachTimer = new();

    public TaskbarWidgetWindow()
    {
        InitializeComponent();

        Opened += (_, _) => AttachToTaskbar();

        _attachTimer.Interval = TimeSpan.FromSeconds(2);
        _attachTimer.Tick += (_, _) => AttachToTaskbar();
        _attachTimer.Start();
    }

    public void UpdateSpeed(string uploadSpeed, string downloadSpeed)
    {
        UploadText.Text = uploadSpeed;
        DownloadText.Text = downloadSpeed;
    }

    private void AttachToTaskbar()
    {
        var widgetHandle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (widgetHandle == IntPtr.Zero) return;

        var taskbarHandle = FindWindow("Shell_TrayWnd", null);
        if (taskbarHandle == IntPtr.Zero) return;

        var trayHandle = FindWindowEx(taskbarHandle, IntPtr.Zero, "TrayNotifyWnd", null);

        SetParent(widgetHandle, taskbarHandle);

        var style = GetWindowLongPtr(widgetHandle, GWL_STYLE).ToInt64();
        style &= ~WS_POPUP;
        style |= WS_CHILD;
        SetWindowLongPtr(widgetHandle, GWL_STYLE, new IntPtr(style));

        var exStyle = GetWindowLongPtr(widgetHandle, GWL_EXSTYLE).ToInt64();
        exStyle |= WS_EX_TOOLWINDOW;
        exStyle |= WS_EX_NOACTIVATE;
        exStyle &= ~WS_EX_APPWINDOW;
        SetWindowLongPtr(widgetHandle, GWL_EXSTYLE, new IntPtr(exStyle));

        GetWindowRect(taskbarHandle, out var taskbarRect);

        int x;
        int y;

        if (trayHandle != IntPtr.Zero)
        {
            GetWindowRect(trayHandle, out var trayRect);

            POINT trayPoint = new POINT
            {
                X = trayRect.Left,
                Y = trayRect.Top
            };

            ScreenToClient(taskbarHandle, ref trayPoint);

            x = trayPoint.X - WidgetWidth - 8;
            y = trayPoint.Y + ((trayRect.Bottom - trayRect.Top - WidgetHeight) / 2);
        }
        else
        {
            x = (taskbarRect.Right - taskbarRect.Left) - WidgetWidth - 250;
            y = ((taskbarRect.Bottom - taskbarRect.Top) - WidgetHeight) / 2;
        }

        if (x < 0) x = 5;
        if (y < 0) y = 5;

        SetWindowPos(
            widgetHandle,
            HWND_TOP,
            x,
            y,
            WidgetWidth,
            WidgetHeight,
            SWP_SHOWWINDOW
        );
    }

    protected override void OnClosed(EventArgs e)
    {
        _attachTimer.Stop();
        base.OnClosed(e);
    }

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;

    private const long WS_CHILD = 0x40000000L;
    private const long WS_POPUP = 0x80000000L;

    private const long WS_EX_TOOLWINDOW = 0x00000080L;
    private const long WS_EX_NOACTIVATE = 0x08000000L;
    private const long WS_EX_APPWINDOW = 0x00040000L;

    private static readonly IntPtr HWND_TOP = IntPtr.Zero;

    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(
        IntPtr hwndParent,
        IntPtr hwndChildAfter,
        string lpszClass,
        string? lpszWindow
    );

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags
    );

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : GetWindowLongPtr32(hWnd, nIndex);
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}