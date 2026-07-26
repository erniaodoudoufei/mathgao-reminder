using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MathGaoReminder;

internal static class UiTheme
{
    public static readonly Color Page = Color.FromArgb(247, 249, 250);
    public static readonly Color Surface = Color.White;
    public static readonly Color Ink = Color.FromArgb(34, 45, 50);
    public static readonly Color Muted = Color.FromArgb(91, 105, 112);
    public static readonly Color Line = Color.FromArgb(219, 227, 231);
    public static readonly Color Teal = Color.FromArgb(15, 139, 141);
    public static readonly Color Coral = Color.FromArgb(242, 95, 92);
    public static readonly Color Gold = Color.FromArgb(255, 203, 71);
    public static readonly Color Chalk = Color.FromArgb(38, 71, 66);
    public static readonly Color SoftGreen = Color.FromArgb(229, 247, 243);
    public static readonly Color SoftGold = Color.FromArgb(255, 246, 218);

    public static Font Font(float size, FontStyle style = FontStyle.Regular) => new("Microsoft YaHei UI", size, style);

    public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        GraphicsPath path = new();

        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            path.CloseFigure();
            return path;
        }

        Rectangle arc = new(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static Icon CreateAppIcon()
    {
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "MathGaoReminder.ico");
        if (File.Exists(iconPath))
        {
            return new Icon(iconPath);
        }

        Icon? executableIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (executableIcon is not null)
        {
            return executableIcon;
        }

        return CreateFallbackAppIcon();
    }

    public static Image LoadLogoImage(int size)
    {
        string imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "MathGaoTeacherLogo.png");
        if (!File.Exists(imagePath))
        {
            return CreateFallbackLogoImage(size);
        }

        using Image source = Image.FromFile(imagePath);
        Bitmap bitmap = new(size, size);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);
        graphics.DrawImage(source, 0, 0, size, size);
        return bitmap;
    }

    private static Image CreateFallbackLogoImage(int size)
    {
        Bitmap bitmap = new(size, size);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using GraphicsPath path = RoundedRectangle(new Rectangle(0, 0, size - 1, size - 1), Math.Max(8, size / 7));
        using SolidBrush fill = new(Teal);
        graphics.FillPath(fill, path);

        using Font font = new("Segoe UI", size * 0.42f, FontStyle.Bold, GraphicsUnit.Pixel);
        TextRenderer.DrawText(graphics, "M", font, new Rectangle(0, 0, size, size), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bitmap;
    }

    private static Icon CreateFallbackAppIcon()
    {
        using Bitmap bitmap = new(64, 64);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using SolidBrush tealBrush = new(Teal);
        using SolidBrush goldBrush = new(Gold);
        using SolidBrush whiteBrush = new(Color.White);
        using Pen chalkPen = new(Color.FromArgb(210, Color.White), 3);
        graphics.FillEllipse(tealBrush, 4, 4, 56, 56);
        graphics.FillEllipse(goldBrush, 42, 8, 14, 14);
        graphics.DrawLine(chalkPen, 19, 45, 31, 20);
        graphics.DrawLine(chalkPen, 31, 20, 44, 45);
        graphics.DrawLine(chalkPen, 25, 35, 38, 35);

        using Font font = new("Segoe UI", 11, FontStyle.Bold);
        graphics.DrawString("M", font, whiteBrush, 22, 42);

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}

internal sealed class RoundedPanel : Panel
{
    public int Radius { get; set; } = 8;
    public Color BorderColor { get; set; } = UiTheme.Line;
    public Color FillColor { get; set; } = UiTheme.Surface;
    public int BorderThickness { get; set; } = 1;

    public RoundedPanel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        DoubleBuffered = true;
        BackColor = Color.Transparent;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateClipRegion();
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Color background = Parent?.BackColor ?? UiTheme.Page;
        using SolidBrush brush = new(background);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = UiTheme.RoundedRectangle(bounds, Radius);
        using SolidBrush fill = new(FillColor);
        using Pen border = new(BorderColor, BorderThickness);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        base.OnPaint(e);
    }

    private void UpdateClipRegion()
    {
        if (Width <= 1 || Height <= 1)
        {
            return;
        }

        using GraphicsPath path = UiTheme.RoundedRectangle(new Rectangle(0, 0, Width, Height), Radius);
        Region = new Region(path);
    }
}

internal sealed class AccentButton : Button
{
    public Color AccentColor { get; set; } = UiTheme.Teal;

    public AccentButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        ForeColor = Color.White;
        BackColor = UiTheme.Teal;
        Font = UiTheme.Font(10.5f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        Height = 42;
        UseVisualStyleBackColor = false;
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        Color fillColor = Enabled ? AccentColor : Color.FromArgb(164, 174, 179);
        using GraphicsPath path = UiTheme.RoundedRectangle(bounds, 8);
        using SolidBrush fill = new(fillColor);

        pevent.Graphics.FillPath(fill, path);
        TextRenderer.DrawText(
            pevent.Graphics,
            Text,
            Font,
            bounds,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class GhostButton : Button
{
    public GhostButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderColor = UiTheme.Line;
        FlatAppearance.BorderSize = 1;
        ForeColor = UiTheme.Ink;
        BackColor = Color.White;
        Font = UiTheme.Font(10f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        Height = 42;
        UseVisualStyleBackColor = false;
    }
}
