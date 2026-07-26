using System.Runtime.InteropServices;

namespace MathGaoReminder;

internal static class SingleInstance
{
    internal const string MutexName = @"Local\MathGaoReminder.SingleInstance";

    private const string MainWindowTitle = "MathGao Reminder";
    private const string ShowExistingInstanceMessageName = "MathGaoReminder.ShowExistingInstance";

    internal static readonly int ShowExistingInstanceMessage = RegisterWindowMessage(ShowExistingInstanceMessageName);

    internal static bool TryNotifyExistingInstance()
    {
        IntPtr mainWindow = FindWindow(null, MainWindowTitle);
        if (mainWindow == IntPtr.Zero || ShowExistingInstanceMessage == 0)
        {
            return false;
        }

        return PostMessage(mainWindow, ShowExistingInstanceMessage, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegisterWindowMessage(string lpString);
}
