using System.Drawing.Drawing2D;
using Timer = System.Windows.Forms.Timer;

namespace MathGaoReminder;

internal sealed class FullScreenReminderForm : Form
{
    private readonly int _workMinutes;
    private readonly int _restSeconds;
    private readonly Timer _timer;
    private readonly Label _countdownLabel;
    private readonly AccentButton _doneButton;
    private int _remainingSeconds;

    public int SnoozeMinutes { get; private set; }

    public FullScreenReminderForm(int workMinutes, int restSeconds)
    {
        _workMinutes = workMinutes;
        _restSeconds = restSeconds;
        _remainingSeconds = restSeconds > 0 ? restSeconds : 15;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        TopMost = true;
        BackColor = Color.White;
        DoubleBuffered = true;
        KeyPreview = true;

        Label brandLabel = new()
        {
            AutoSize = false,
            Text = "你比这道题更重要",
            Font = UiTheme.Font(15f, FontStyle.Bold),
            ForeColor = UiTheme.Teal,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label headlineLabel = new()
        {
            AutoSize = false,
            Text = "该站起来活动一下了",
            Font = UiTheme.Font(34f, FontStyle.Bold),
            ForeColor = UiTheme.Ink,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label subLabel = new()
        {
            AutoSize = false,
            Text = $"你已经连续授课或备课 {_workMinutes} 分钟",
            Font = UiTheme.Font(14f),
            ForeColor = UiTheme.Muted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _countdownLabel = new Label
        {
            AutoSize = false,
            Font = UiTheme.Font(15f, FontStyle.Bold),
            ForeColor = UiTheme.Coral,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _doneButton = new AccentButton
        {
            Text = restSeconds > 0 ? "休息完成" : "我已经起来了",
            AccentColor = UiTheme.Teal,
            Width = 168
        };
        _doneButton.Click += (_, _) => Close();

        GhostButton snoozeButton = new()
        {
            Text = "5分钟后再提醒",
            Width = 168
        };
        snoozeButton.Click += (_, _) =>
        {
            SnoozeMinutes = 5;
            Close();
        };

        RoundedPanel stretchPanel = CreateStretchPanel();

        Controls.Add(brandLabel);
        Controls.Add(headlineLabel);
        Controls.Add(subLabel);
        Controls.Add(_countdownLabel);
        Controls.Add(stretchPanel);
        Controls.Add(_doneButton);
        Controls.Add(snoozeButton);
        RegisterDismissClick(this);

        Layout += (_, _) => LayoutContent(brandLabel, headlineLabel, subLabel, stretchPanel, snoozeButton);
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape && _restSeconds == 0)
            {
                Close();
            }
        };

        _timer = new Timer { Interval = 1000 };
        _timer.Tick += (_, _) => TickCountdown();
        UpdateCountdownText();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _timer.Start();
        Activate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _timer.Stop();
        base.OnFormClosing(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = ClientRectangle;
        using LinearGradientBrush background = new(bounds, Color.FromArgb(255, 253, 247), Color.FromArgb(232, 246, 243), LinearGradientMode.ForwardDiagonal);
        e.Graphics.FillRectangle(background, bounds);

        DrawTeacherScene(e.Graphics, bounds);
        base.OnPaint(e);
    }

    private void LayoutContent(Label brandLabel, Label headlineLabel, Label subLabel, RoundedPanel stretchPanel, Control snoozeButton)
    {
        int centerX = ClientSize.Width / 2;
        int top = Math.Max(70, ClientSize.Height / 2 - 235);

        brandLabel.SetBounds(centerX - 170, top, 340, 34);
        headlineLabel.SetBounds(centerX - 420, top + 50, 840, 68);
        subLabel.SetBounds(centerX - 360, top + 122, 720, 32);
        _countdownLabel.SetBounds(centerX - 250, top + 164, 500, 34);
        stretchPanel.SetBounds(centerX - 342, top + 220, 684, 118);
        _doneButton.SetBounds(centerX - 178, top + 374, 168, 44);
        snoozeButton.SetBounds(centerX + 10, top + 374, 168, 44);
    }

    private RoundedPanel CreateStretchPanel()
    {
        RoundedPanel panel = new()
        {
            Radius = 8,
            FillColor = Color.FromArgb(252, 255, 254),
            BorderColor = Color.FromArgb(202, 224, 220)
        };

        string[] titles = { "远眺屏幕外", "肩颈放松", "喝口水" };
        string[] details = { "看向窗外20秒", "绕肩和伸展手腕", "让嗓子缓一缓" };
        Color[] colors = { UiTheme.Teal, UiTheme.Coral, Color.FromArgb(176, 132, 22) };

        for (int i = 0; i < 3; i++)
        {
            int left = 26 + i * 216;
            Label dot = new()
            {
                AutoSize = false,
                Text = (i + 1).ToString(),
                BackColor = colors[i],
                ForeColor = Color.White,
                Font = UiTheme.Font(12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(left, 28),
                Size = new Size(36, 36)
            };

            Label title = new()
            {
                AutoSize = false,
                Text = titles[i],
                BackColor = Color.Transparent,
                Font = UiTheme.Font(12f, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                Location = new Point(left + 50, 25),
                Size = new Size(138, 24)
            };

            Label detail = new()
            {
                AutoSize = false,
                Text = details[i],
                BackColor = Color.Transparent,
                Font = UiTheme.Font(9.5f),
                ForeColor = UiTheme.Muted,
                Location = new Point(left + 50, 54),
                Size = new Size(138, 24)
            };

            panel.Controls.Add(dot);
            panel.Controls.Add(title);
            panel.Controls.Add(detail);
        }

        panel.Resize += (_, _) =>
        {
            foreach (Control control in panel.Controls)
            {
                if (control is Label label && label.BackColor != Color.Transparent)
                {
                    using GraphicsPath path = UiTheme.RoundedRectangle(new Rectangle(0, 0, label.Width, label.Height), 18);
                    label.Region = new Region(path);
                }
            }
        };

        return panel;
    }

    private void TickCountdown()
    {
        _remainingSeconds--;

        if (_remainingSeconds <= 0)
        {
            if (_restSeconds > 0)
            {
                _doneButton.Enabled = true;
                _doneButton.Text = "继续上课";
            }

            Close();
            return;
        }

        UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        if (_restSeconds > 0)
        {
            _doneButton.Enabled = true;
            _countdownLabel.Text = $"强制休息还剩 {_remainingSeconds} 秒";
        }
        else
        {
            _doneButton.Enabled = true;
            _countdownLabel.Text = $"{_remainingSeconds} 秒后自动收起，也可以立即关闭";
        }
    }

    private void RegisterDismissClick(Control control)
    {
        if (control is Button)
        {
            return;
        }

        control.Click += (_, _) => Close();
        foreach (Control child in control.Controls)
        {
            RegisterDismissClick(child);
        }
    }
    private static void DrawTeacherScene(Graphics graphics, Rectangle bounds)
    {
        int boardWidth = Math.Min(360, bounds.Width / 4);
        int boardHeight = 210;
        Rectangle board = new(bounds.Left + 70, bounds.Bottom - boardHeight - 70, boardWidth, boardHeight);
        using GraphicsPath boardPath = UiTheme.RoundedRectangle(board, 8);
        using SolidBrush boardFill = new(UiTheme.Chalk);
        using Pen boardPen = new(Color.FromArgb(42, 104, 95), 4);
        graphics.FillPath(boardFill, boardPath);
        graphics.DrawPath(boardPen, boardPath);

        using Font boardFont = UiTheme.Font(23f, FontStyle.Bold);
        using Font smallFont = UiTheme.Font(13f, FontStyle.Regular);
        using SolidBrush chalkBrush = new(Color.FromArgb(233, 246, 237));
        graphics.DrawString("x + y = ?", boardFont, chalkBrush, board.Left + 38, board.Top + 42);
        graphics.DrawString("下节课继续高效讲解", smallFont, chalkBrush, board.Left + 40, board.Top + 104);
        using Pen chalkLine = new(Color.FromArgb(180, 238, 249, 233), 3);
        graphics.DrawLine(chalkLine, board.Left + 40, board.Top + 148, board.Right - 40, board.Top + 148);

        Rectangle camera = new(bounds.Right - 260, bounds.Top + 86, 148, 106);
        using GraphicsPath cameraPath = UiTheme.RoundedRectangle(camera, 8);
        using SolidBrush cameraFill = new(Color.FromArgb(255, 246, 218));
        using Pen cameraPen = new(Color.FromArgb(235, 196, 80), 2);
        graphics.FillPath(cameraFill, cameraPath);
        graphics.DrawPath(cameraPen, cameraPath);

        using Pen iconPen = new(UiTheme.Coral, 6)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawRectangle(iconPen, camera.Left + 34, camera.Top + 34, 54, 38);
        Point[] lens =
        {
            new(camera.Left + 90, camera.Top + 46),
            new(camera.Left + 118, camera.Top + 32),
            new(camera.Left + 118, camera.Top + 78)
        };
        using SolidBrush coralBrush = new(UiTheme.Coral);
        graphics.FillPolygon(coralBrush, lens);
    }
}




