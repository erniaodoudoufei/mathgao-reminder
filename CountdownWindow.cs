using System.Drawing.Drawing2D;

namespace MathGaoReminder;

internal sealed class CountdownWindow : Form
{
    private readonly Label _secondsLabel;
    private bool _dragging;
    private Point _dragOffset;

    public CountdownWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        Size = new Size(288, 124);
        StartPosition = FormStartPosition.Manual;
        BackColor = UiTheme.Coral;
        Opacity = 0.96;
        DoubleBuffered = true;

        _secondsLabel = new Label
        {
            AutoSize = false,
            Text = "15",
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = UiTheme.Font(30f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(16, 18),
            Size = new Size(72, 72)
        };

        Label titleLabel = new()
        {
            AutoSize = false,
            Text = "马上课间休息",
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = UiTheme.Font(13f, FontStyle.Bold),
            Location = new Point(96, 24),
            Size = new Size(150, 26)
        };

        Label detailLabel = new()
        {
            AutoSize = false,
            Text = "可以关闭，不影响提醒",
            ForeColor = Color.FromArgb(245, 255, 255, 255),
            BackColor = Color.Transparent,
            Font = UiTheme.Font(9.5f),
            Location = new Point(97, 55),
            Size = new Size(160, 22)
        };

        Button closeButton = new()
        {
            Text = "X",
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.Coral,
            ForeColor = Color.White,
            Font = UiTheme.Font(10f, FontStyle.Bold),
            Size = new Size(30, 28),
            Location = new Point(250, 8),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.Click += (_, _) => Close();

        Controls.Add(_secondsLabel);
        Controls.Add(titleLabel);
        Controls.Add(detailLabel);
        Controls.Add(closeButton);

        MouseDown += BeginDrag;
        MouseMove += ContinueDrag;
        MouseUp += EndDrag;
        foreach (Control control in Controls)
        {
            control.MouseDown += BeginDrag;
            control.MouseMove += ContinueDrag;
            control.MouseUp += EndDrag;
        }
    }

    public void SetRemaining(TimeSpan remaining)
    {
        int seconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        _secondsLabel.Text = seconds.ToString();
    }

    public void PlaceNearClock()
    {
        Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        Location = new Point(workingArea.Right - Width - 24, workingArea.Bottom - Height - 24);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExToolWindow = 0x80;
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= wsExToolWindow;
            return cp;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = UiTheme.RoundedRectangle(bounds, 8);
        Region = new Region(path);
        using LinearGradientBrush brush = new(bounds, UiTheme.Coral, Color.FromArgb(226, 77, 94), LinearGradientMode.ForwardDiagonal);
        e.Graphics.FillPath(brush, path);
        using Pen pen = new(Color.FromArgb(60, Color.White), 1);
        e.Graphics.DrawPath(pen, path);

        using Pen ring = new(Color.FromArgb(70, Color.White), 2);
        e.Graphics.DrawEllipse(ring, 17, 20, 68, 68);
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
