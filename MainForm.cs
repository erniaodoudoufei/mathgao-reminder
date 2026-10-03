using System.Media;
using Timer = System.Windows.Forms.Timer;

namespace MathGaoReminder;

public sealed class MainForm : Form
{
    private readonly ReminderSettings _settings;
    private readonly Timer _timer;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _appIcon;
    private readonly Image _logoImage;
    private readonly Label _statusLabel;
    private readonly Label _timeLabel;
    private readonly Label _nextLabel;
    private readonly Label _cycleLabel;
    private readonly NumericUpDown _intervalInput;
    private readonly NumericUpDown _restSecondsInput;
    private readonly CheckBox _warningWindowBox;
    private readonly CheckBox _soundBox;
    private readonly CheckBox _floatingWindowBox;
    private readonly AccentButton _startButton;
    private readonly GhostButton _pauseButton;
    private readonly GhostButton _remindNowButton;
    private readonly GhostButton _hideButton;
    private CountdownWindow? _countdownWindow;
    private FloatingStatusWindow? _floatingWindow;
    private DateTime _nextReminderAt;
    private bool _running;
    private bool _warningShownThisCycle;
    private bool _allowExit;
    private int _currentCycleMinutes;

    public MainForm(bool showFloatingWindow = true)
    {
        _settings = ReminderSettings.Load();
        _currentCycleMinutes = Math.Max(1, _settings.WorkIntervalMinutes);
        _timer = new Timer { Interval = 500 };
        _timer.Tick += (_, _) => TimerTick();

        _appIcon = UiTheme.CreateAppIcon();
        _logoImage = UiTheme.LoadLogoImage(64);
        Icon = _appIcon;

        Text = "MathGao Reminder";
        MinimumSize = new Size(720, 520);
        ClientSize = new Size(760, 540);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UiTheme.Page;
        Font = UiTheme.Font(10f);
        DoubleBuffered = true;

        _statusLabel = new Label();
        _timeLabel = new Label();
        _nextLabel = new Label();
        _cycleLabel = new Label();
        _intervalInput = new NumericUpDown();
        _restSecondsInput = new NumericUpDown();
        _warningWindowBox = new CheckBox();
        _soundBox = new CheckBox();
        _floatingWindowBox = new CheckBox();
        _startButton = new AccentButton();
        _pauseButton = new GhostButton();
        _remindNowButton = new GhostButton();
        _hideButton = new GhostButton();

        BuildLayout();
        LoadSettingsIntoControls();
        WireSettingsEvents();

        _notifyIcon = CreateNotifyIcon();
        FormClosing += MainFormClosing;
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                HideToTray();
            }
            else
            {
                Invalidate(true);
            }
        };

        UpdateDisplay(TimeSpan.FromMinutes(_settings.WorkIntervalMinutes));
        UpdateActions();
        if (showFloatingWindow)
        {
            UpdateFloatingWindowVisibility();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _notifyIcon.Dispose();
            _appIcon.Dispose();
            _logoImage.Dispose();
            _countdownWindow?.Dispose();
            _floatingWindow?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == SingleInstance.ShowExistingInstanceMessage)
        {
            ShowMainWindow();
            return;
        }

        base.WndProc(ref m);
    }

    private void BuildLayout()
    {
        RoundedPanel heroPanel = new()
        {
            Radius = 8,
            FillColor = Color.White,
            BorderColor = UiTheme.Line,
            Location = new Point(24, 24),
            Size = new Size(712, 180),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        PictureBox appLogo = new()
        {
            Image = _logoImage,
            BackColor = Color.Transparent,
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(24, 21),
            Size = new Size(64, 64)
        };
        appLogo.Paint += (_, _) =>
        {
            using var path = UiTheme.RoundedRectangle(new Rectangle(0, 0, appLogo.Width - 1, appLogo.Height - 1), 10);
            appLogo.Region = new Region(path);
        };

        Label title = new()
        {
            Text = "你比这道题更重要",
            ForeColor = UiTheme.Ink,
            BackColor = Color.Transparent,
            Font = UiTheme.Font(22f, FontStyle.Bold),
            Location = new Point(112, 22),
            Size = new Size(420, 40)
        };

        Label subtitle = new()
        {
            Text = "网课老师的久坐轻提醒",
            ForeColor = UiTheme.Muted,
            BackColor = Color.Transparent,
            Font = UiTheme.Font(11f),
            Location = new Point(115, 66),
            Size = new Size(320, 24)
        };

        _statusLabel.Text = "未开始";
        _statusLabel.ForeColor = UiTheme.Teal;
        _statusLabel.BackColor = Color.Transparent;
        _statusLabel.Font = UiTheme.Font(11f, FontStyle.Bold);
        _statusLabel.TextAlign = ContentAlignment.MiddleRight;
        _statusLabel.Location = new Point(500, 30);
        _statusLabel.Size = new Size(176, 28);
        _statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _timeLabel.Text = "35:00";
        _timeLabel.ForeColor = UiTheme.Ink;
        _timeLabel.BackColor = Color.Transparent;
        _timeLabel.Font = UiTheme.Font(36f, FontStyle.Bold);
        _timeLabel.TextAlign = ContentAlignment.MiddleLeft;
        _timeLabel.Location = new Point(26, 104);
        _timeLabel.Size = new Size(210, 54);

        _nextLabel.Text = "准备开始下一轮提醒";
        _nextLabel.ForeColor = UiTheme.Muted;
        _nextLabel.BackColor = Color.Transparent;
        _nextLabel.Font = UiTheme.Font(10.5f);
        _nextLabel.Location = new Point(250, 116);
        _nextLabel.Size = new Size(250, 28);

        _cycleLabel.Text = "默认 35 分钟提醒，强制休息 0 秒";
        _cycleLabel.ForeColor = UiTheme.Muted;
        _cycleLabel.BackColor = Color.Transparent;
        _cycleLabel.Font = UiTheme.Font(10f);
        _cycleLabel.Location = new Point(250, 142);
        _cycleLabel.Size = new Size(360, 24);

        heroPanel.Controls.Add(appLogo);
        heroPanel.Controls.Add(title);
        heroPanel.Controls.Add(subtitle);
        heroPanel.Controls.Add(_statusLabel);
        heroPanel.Controls.Add(_timeLabel);
        heroPanel.Controls.Add(_nextLabel);
        heroPanel.Controls.Add(_cycleLabel);

        RoundedPanel settingsPanel = new()
        {
            Radius = 8,
            FillColor = Color.White,
            BorderColor = UiTheme.Line,
            Location = new Point(24, 224),
            Size = new Size(444, 270),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        Label settingsTitle = new()
        {
            Text = "提醒设置",
            ForeColor = UiTheme.Ink,
            BackColor = Color.Transparent,
            Font = UiTheme.Font(15f, FontStyle.Bold),
            Location = new Point(24, 22),
            Size = new Size(200, 30)
        };

        Label intervalLabel = CreateFieldLabel("每隔多少分钟提醒", 24, 72);
        ConfigureNumberInput(_intervalInput, 230, 68, 1, 240);

        Label restLabel = CreateFieldLabel("强制休息时间（秒）", 24, 116);
        ConfigureNumberInput(_restSecondsInput, 230, 112, 0, 3600);

        _warningWindowBox.Text = "最后15秒显示可关闭小窗";
        ConfigureCheckBox(_warningWindowBox, 24, 162);

        _soundBox.Text = "提醒时播放系统提示音";
        ConfigureCheckBox(_soundBox, 24, 192);

        _floatingWindowBox.Text = "常显示后台悬浮窗";
        ConfigureCheckBox(_floatingWindowBox, 24, 222);

        settingsPanel.Controls.Add(settingsTitle);
        settingsPanel.Controls.Add(intervalLabel);
        settingsPanel.Controls.Add(_intervalInput);
        settingsPanel.Controls.Add(restLabel);
        settingsPanel.Controls.Add(_restSecondsInput);
        settingsPanel.Controls.Add(_warningWindowBox);
        settingsPanel.Controls.Add(_soundBox);
        settingsPanel.Controls.Add(_floatingWindowBox);

        RoundedPanel actionsPanel = new()
        {
            Radius = 8,
            FillColor = Color.White,
            BorderColor = UiTheme.Line,
            Location = new Point(492, 224),
            Size = new Size(244, 236),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        Label classLabel = new()
        {
            Text = "动一动",
            ForeColor = UiTheme.Ink,
            BackColor = Color.Transparent,
            Font = UiTheme.Font(15f, FontStyle.Bold),
            Location = new Point(22, 22),
            Size = new Size(160, 30)
        };

        Label boardLabel = new()
        {
            Text = "是最便宜的养生",
            ForeColor = UiTheme.Muted,
            BackColor = Color.Transparent,
            Font = UiTheme.Font(10f),
            Location = new Point(23, 53),
            Size = new Size(190, 24)
        };

        _startButton.Text = "开始提醒";
        _startButton.AccentColor = UiTheme.Teal;
        _startButton.Location = new Point(22, 88);
        _startButton.Size = new Size(200, 42);
        _startButton.Click += (_, _) => StartReminder();

        _pauseButton.Text = "暂停";
        _pauseButton.Location = new Point(22, 140);
        _pauseButton.Size = new Size(94, 38);
        _pauseButton.Click += (_, _) => PauseReminder();

        _remindNowButton.Text = "立即提醒";
        _remindNowButton.Location = new Point(128, 140);
        _remindNowButton.Size = new Size(94, 38);
        _remindNowButton.Click += (_, _) => TriggerReminder(manual: true);

        _hideButton.Text = "隐藏到托盘";
        _hideButton.Location = new Point(22, 188);
        _hideButton.Size = new Size(200, 38);
        _hideButton.Click += (_, _) => HideToTray();

        actionsPanel.Controls.Add(classLabel);
        actionsPanel.Controls.Add(boardLabel);
        actionsPanel.Controls.Add(_startButton);
        actionsPanel.Controls.Add(_pauseButton);
        actionsPanel.Controls.Add(_remindNowButton);
        actionsPanel.Controls.Add(_hideButton);

        Controls.Add(heroPanel);
        Controls.Add(settingsPanel);
        Controls.Add(actionsPanel);
    }

    private static Label CreateFieldLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            ForeColor = UiTheme.Muted,
            BackColor = Color.Transparent,
            Font = UiTheme.Font(10.5f),
            Location = new Point(x, y),
            Size = new Size(190, 28)
        };
    }

    private static void ConfigureNumberInput(NumericUpDown input, int x, int y, int min, int max)
    {
        input.Minimum = min;
        input.Maximum = max;
        input.Location = new Point(x, y);
        input.Size = new Size(150, 31);
        input.Font = UiTheme.Font(11f, FontStyle.Bold);
        input.BorderStyle = BorderStyle.FixedSingle;
        input.TextAlign = HorizontalAlignment.Center;
    }

    private static void ConfigureCheckBox(CheckBox checkBox, int x, int y)
    {
        checkBox.ForeColor = UiTheme.Ink;
        checkBox.BackColor = Color.Transparent;
        checkBox.Font = UiTheme.Font(10f);
        checkBox.Location = new Point(x, y);
        checkBox.Size = new Size(250, 26);
        checkBox.Cursor = Cursors.Hand;
        checkBox.FlatStyle = FlatStyle.System;
    }

    private void LoadSettingsIntoControls()
    {
        _intervalInput.Value = Math.Clamp(_settings.WorkIntervalMinutes, 1, 240);
        _restSecondsInput.Value = Math.Clamp(_settings.RestSeconds, 0, 3600);
        _warningWindowBox.Checked = _settings.ShowWarningWindow;
        _soundBox.Checked = _settings.PlaySound;
        _floatingWindowBox.Checked = _settings.ShowFloatingWindow;
    }

    private void WireSettingsEvents()
    {
        _floatingWindowBox.CheckedChanged += (_, _) =>
        {
            SaveSettingsFromControls();
            UpdateFloatingWindowVisibility();
        };
    }

    private NotifyIcon CreateNotifyIcon()
    {
        ContextMenuStrip menu = new();
        ToolStripMenuItem showItem = new("显示主窗口", null, (_, _) => ShowMainWindow());
        ToolStripMenuItem startItem = new("开始/重新计时", null, (_, _) => StartReminder());
        ToolStripMenuItem pauseItem = new("暂停", null, (_, _) => PauseReminder());
        ToolStripMenuItem remindItem = new("立即提醒", null, (_, _) => TriggerReminder(manual: true));
        ToolStripMenuItem exitItem = new("退出", null, (_, _) => ExitApplication());
        menu.Items.Add(showItem);
        menu.Items.Add(startItem);
        menu.Items.Add(pauseItem);
        menu.Items.Add(remindItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        NotifyIcon notifyIcon = new()
        {
            Text = "MathGao Reminder",
            Icon = _appIcon,
            Visible = true,
            ContextMenuStrip = menu
        };
        notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
        return notifyIcon;
    }

    private void StartReminder()
    {
        SaveSettingsFromControls();
        _currentCycleMinutes = _settings.WorkIntervalMinutes;
        _nextReminderAt = DateTime.Now.AddMinutes(_currentCycleMinutes);
        _running = true;
        _warningShownThisCycle = false;
        CloseCountdownWindow();
        _timer.Start();
        UpdateActions();
        UpdateDisplay(_nextReminderAt - DateTime.Now);
    }

    private void PauseReminder()
    {
        _running = false;
        _timer.Stop();
        CloseCountdownWindow();
        UpdateActions();
        UpdateDisplay(TimeSpan.FromMinutes((double)_intervalInput.Value));
    }

    private void SaveSettingsFromControls()
    {
        _settings.WorkIntervalMinutes = (int)_intervalInput.Value;
        _settings.RestSeconds = (int)_restSecondsInput.Value;
        _settings.ShowWarningWindow = _warningWindowBox.Checked;
        _settings.PlaySound = _soundBox.Checked;
        _settings.ShowFloatingWindow = _floatingWindowBox.Checked;
        _settings.Save();
        _cycleLabel.Text = $"每隔 {_settings.WorkIntervalMinutes} 分钟提醒，强制休息 {_settings.RestSeconds} 秒";
    }

    private void TimerTick()
    {
        if (!_running)
        {
            return;
        }

        TimeSpan remaining = _nextReminderAt - DateTime.Now;
        if (remaining <= TimeSpan.Zero)
        {
            TriggerReminder(manual: false);
            return;
        }

        UpdateDisplay(remaining);

        if (_settings.ShowWarningWindow && remaining.TotalSeconds <= 15.5)
        {
            ShowOrUpdateCountdownWindow(remaining);
        }
    }

    private void ShowOrUpdateCountdownWindow(TimeSpan remaining)
    {
        if (_warningShownThisCycle && (_countdownWindow == null || _countdownWindow.IsDisposed))
        {
            return;
        }

        if (_countdownWindow == null || _countdownWindow.IsDisposed)
        {
            _warningShownThisCycle = true;
            _countdownWindow = new CountdownWindow();
            _countdownWindow.FormClosed += (_, _) => _countdownWindow = null;
            _countdownWindow.PlaceNearClock();
            _countdownWindow.Show();
        }

        _countdownWindow.SetRemaining(remaining);
    }

    private void TriggerReminder(bool manual)
    {
        bool shouldContinue = _running;
        _timer.Stop();
        CloseCountdownWindow();
        SaveSettingsFromControls();

        if (_settings.PlaySound)
        {
            SystemSounds.Exclamation.Play();
        }

        using FullScreenReminderForm reminderForm = new(_currentCycleMinutes, _settings.RestSeconds);
        reminderForm.ShowDialog();

        if (manual && !shouldContinue)
        {
            _running = false;
            UpdateActions();
            UpdateDisplay(TimeSpan.FromMinutes(_settings.WorkIntervalMinutes));
            return;
        }

        if (!shouldContinue)
        {
            return;
        }

        int nextMinutes = reminderForm.SnoozeMinutes > 0 ? reminderForm.SnoozeMinutes : _settings.WorkIntervalMinutes;
        _currentCycleMinutes = _settings.WorkIntervalMinutes;
        _nextReminderAt = DateTime.Now.AddMinutes(nextMinutes);
        _warningShownThisCycle = false;
        _timer.Start();
        UpdateActions();
        UpdateDisplay(_nextReminderAt - DateTime.Now);
    }

    private void UpdateDisplay(TimeSpan remaining)
    {
        remaining = remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        int totalSeconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        _timeLabel.Text = $"{minutes:00}:{seconds:00}";

        if (_running)
        {
            _statusLabel.Text = "计时中";
            _statusLabel.ForeColor = UiTheme.Teal;
            _nextLabel.Text = $"下次提醒：{_nextReminderAt:HH:mm}";
        }
        else
        {
            _statusLabel.Text = "未开始";
            _statusLabel.ForeColor = UiTheme.Coral;
            _nextLabel.Text = "准备开始下一轮提醒";
        }

        UpdateFloatingWindowStatus();
    }

    private void UpdateActions()
    {
        _startButton.Text = _running ? "重新计时" : "开始提醒";
        _pauseButton.Enabled = _running;
    }

    private void ShowMainWindow()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
    }

    private void MainFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowExit || !_settings.HideToTrayOnClose)
        {
            return;
        }

        e.Cancel = true;
        HideToTray();
    }

    private void ExitApplication()
    {
        _allowExit = true;
        _notifyIcon.Visible = false;
        CloseCountdownWindow();
        CloseFloatingWindow();
        Application.Exit();
    }

    private void UpdateFloatingWindowVisibility()
    {
        if (!_settings.ShowFloatingWindow || _allowExit)
        {
            CloseFloatingWindow();
            return;
        }

        if (_floatingWindow == null || _floatingWindow.IsDisposed)
        {
            _floatingWindow = new FloatingStatusWindow(_appIcon, ShowMainWindow,
                _settings.FloatingWindowPositionLocked, SaveFloatingWindowPreferences);
            _floatingWindow.FormClosed += (_, _) => _floatingWindow = null;
            Point? savedLocation = _settings.FloatingWindowX is int x && _settings.FloatingWindowY is int y
                ? new Point(x, y)
                : null;
            _floatingWindow.RestorePosition(savedLocation);
            _floatingWindow.Show();
        }

        UpdateFloatingWindowStatus();
    }

    private void SaveFloatingWindowPreferences(Point location, bool positionLocked)
    {
        _settings.FloatingWindowX = location.X;
        _settings.FloatingWindowY = location.Y;
        _settings.FloatingWindowPositionLocked = positionLocked;
        _settings.Save();
    }

    private void UpdateFloatingWindowStatus()
    {
        if (_floatingWindow == null || _floatingWindow.IsDisposed)
        {
            return;
        }

        _floatingWindow.SetStatus(_running, _timeLabel.Text);
    }

    private void CloseFloatingWindow()
    {
        if (_floatingWindow == null || _floatingWindow.IsDisposed)
        {
            return;
        }

        FloatingStatusWindow window = _floatingWindow;
        _floatingWindow = null;
        window.Close();
        window.Dispose();
    }

    private void CloseCountdownWindow()
    {
        if (_countdownWindow == null || _countdownWindow.IsDisposed)
        {
            return;
        }

        _countdownWindow.Close();
        _countdownWindow = null;
    }
}

