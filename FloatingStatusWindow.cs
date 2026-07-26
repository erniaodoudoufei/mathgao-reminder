using System.Drawing.Drawing2D;

namespace MathGaoReminder;

internal sealed class FloatingStatusWindow : Form
{
    private readonly Label _stateLabel;
    private readonly Label _timeLabel;
    private readonly Label _hintLabel;
    private readonly Action _showMainWindow;
    private bool _dragging;
    private Point _dragOffset;

    public FloatingStatusWindow(Icon icon, Action showMainWindow)
    {
        _showMainWindow = showMainWindow;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(190, 74);
        BackColor = UiTheme.Surface;
        Opacity = 0.94;
        Icon = icon;
        DoubleBuffered = true;

        _stateLabel = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.Teal,
            Font = UiTheme.Font(9.5f, FontStyle.Bold),
            Location = new Point(18, 11),
            Size = new Size(98, 22),
            Text = "后台运行"
        };

        _timeLabel = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.Ink,
            Font = UiTheme.Font(21f, FontStyle.Bold),
            Location = new Point(15, 31),
            Size = new Size(104, 34),
            Text = "35:00"
        };

        _hintLabel = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.Muted,
            Font = UiTheme.Font(9f),
            Location = new Point(112, 18),
            Size = new Size(62, 40),
            Text = "双击\n打开",
            TextAlign = ContentAlignment.MiddleCenter
        };

        Controls.Add(_stateLabel);
        Controls.Add(_timeLabel);
        Controls.Add(_hintLabel);

        ContextMenuStrip menu = new();
        menu.Items.Add("显示主窗口", null, (_, _) => _showMainWindow());
        ContextMenuStrip = menu;

        MouseDown += BeginDrag;
        MouseMove += ContinueDrag;
        MouseUp += EndDrag;
        DoubleClick += (_, _) => _showMainWindow();
        foreach (Control control in Controls)
        {
            control.MouseDown += BeginDrag;
            control.MouseMove += ContinueDrag;
            control.MouseUp += EndDrag;
            control.DoubleClick += (_, _) => _showMainWindow();
        }
    }

    public void SetStatus(bool running, string remainingText)
    {
        _stateLabel.Text = running ? "计时中" : "后台运行";
        _stateLabel.ForeColor = running ? UiTheme.Teal : UiTheme.Coral;
        _timeLabel.Text = remainingText;
    }

    public void PlaceNearClock()
    {
        Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        Location = new Point(workingArea.Right - Width - 18, workingArea.Bottom - Height - 18);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExToolWindow = 0x80;
            const int wsExNoActivate = 0x08000000;
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= wsExToolWindow | wsExNoActivate;
            return cp;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using GraphicsPath path = UiTheme.RoundedRectangle(new Rectangle(0, 0, Width, Height), 10);
        Region = new Region(path);
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.Transparent);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = UiTheme.RoundedRectangle(bounds, 10);
        using SolidBrush fill = new(Color.FromArgb(248, 255, 255, 255));
        using Pen border = new(Color.FromArgb(205, 222, 226, 229), 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);

        using SolidBrush dot = new(_stateLabel.ForeColor);
        e.Graphics.FillEllipse(dot, 128, 11, 8, 8);
        base.OnPaint(e);
    }

    private void BeginDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        Control control = sender as Control ?? this;
        Point screenPoint = control.PointToScreen(e.Location);
        _dragOffset = new Point(screenPoint.X - Left, screenPoint.Y - Top);
        _dragging = true;
    }

    private void ContinueDrag(object? sender, MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        Point cursor = Cursor.Position;
        Location = new Point(cursor.X - _dragOffset.X, cursor.Y - _dragOffset.Y);
    }

    private void EndDrag(object? sender, MouseEventArgs e)
    {
        _dragging = false;
    }
}
