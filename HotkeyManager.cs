using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace HashtagCopier;

public static class HotkeyManager
{
    // WinAPI константы
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT   = 0x0004;
    private const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID = 9000;
    private static HwndSource? _source;
    private static Action? _callback;

    /// <summary>
    /// Регистрирует Ctrl+Shift+H и вызывает callback при нажатии.
    /// </summary>
    public static void Register(Window window, Action callback)
    {
        var helper = new WindowInteropHelper(window);
        var handle = helper.Handle;

        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);
        _callback = callback;

        // VK для 'H' = 0x48
        bool ok = RegisterHotKey(handle, HOTKEY_ID,
                                 MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT,
                                 0x48);

        if (!ok)
        {
            MessageBox.Show(
                "Не удалось зарегистрировать хоткей Ctrl+Shift+H.\n" +
                "Возможно, он уже занят другой программой.",
                "Хоткей", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public static void Unregister(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        UnregisterHotKey(handle, HOTKEY_ID);
        _source?.RemoveHook(WndProc);
        _source = null;
        _callback = null;
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam,
                                  IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            _callback?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }
}