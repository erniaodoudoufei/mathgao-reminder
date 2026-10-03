using System.Threading;

namespace MathGaoReminder;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Contains("--smoke-test"))
        {
            using Icon appIcon = UiTheme.CreateAppIcon();
            using MainForm mainForm = new(showFloatingWindow: false);
            using CountdownWindow countdownWindow = new();
            using FullScreenReminderForm fullScreenReminderForm = new(35, 0);
            using FloatingStatusWindow floatingStatusWindow = new(appIcon, static () => { });
            floatingStatusWindow.SetStatus(running: true, "35:00");
            return;
        }

        using Mutex singleInstanceMutex = new(true, SingleInstance.MutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            if (!SingleInstance.TryNotifyExistingInstance())
            {
                MessageBox.Show(
                    "MathGao Reminder 已经打开了，请在任务栏或右下角托盘中查看。",
                    "已经在运行",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return;
        }

        Application.Run(new MainForm());
    }
}

