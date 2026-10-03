using System.Reflection;
using System.Text.Json;
using MathGaoReminder;

internal static class Program
{
    private static readonly Type WindowType = typeof(MainForm).Assembly.GetType("MathGaoReminder.FloatingStatusWindow")!;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int _assertions;

    [STAThread]
    private static int Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        try
        {
            TestSettingsCompatibility();
            TestScreenBounds();
            TestDragLockAndRestore();
            Console.WriteLine($"PASS: {_assertions} floating-window regression assertions.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void TestSettingsCompatibility()
    {
        ReminderSettings old = JsonSerializer.Deserialize<ReminderSettings>(
            "{\"WorkIntervalMinutes\":42,\"RestSeconds\":0,\"ShowFloatingWindow\":true}")!;
        Check(!old.FloatingWindowPositionLocked && old.FloatingWindowX == null && old.FloatingWindowY == null,
            "Old settings must default to unlocked without a saved position.");
        Check(old.WorkIntervalMinutes == 42 && old.ShowFloatingWindow, "Old reminder preferences must survive.");
    }

    private static void TestScreenBounds()
    {
        MethodInfo constrain = WindowType.GetMethod("ConstrainPosition", BindingFlags.Static | BindingFlags.NonPublic)!;
        Point Clamp(Point point, Size size, Rectangle bounds) =>
            (Point)constrain.Invoke(null, new object[] { point, size, bounds })!;
        Size windowSize = new(190, 74);
        Rectangle primary = new(0, 0, 1920, 1040);
        Rectangle secondary = new(-1920, -120, 1920, 1080);
        Check(Clamp(new Point(200, 300), windowSize, primary) == new Point(200, 300), "Visible position must stay fixed.");
        Check(Clamp(new Point(5000, 3000), windowSize, primary) == new Point(1730, 966), "Offscreen position must return inside the work area.");
        Check(Clamp(new Point(-1800, -50), windowSize, secondary) == new Point(-1800, -50), "Negative monitor coordinates must survive.");
        Check(Clamp(new Point(-10, 1000), windowSize, secondary) == new Point(-190, 886), "All window edges must fit on the secondary screen.");
        Check(Clamp(new Point(50, 50), windowSize, new Rectangle(0, 0, 100, 50)) == Point.Empty, "An unusually small work area must not throw.");
    }

    private static void TestDragLockAndRestore()
    {
        ReminderSettings settings = new() { WorkIntervalMinutes = 42, ShowFloatingWindow = true };
        string serialized = "";
        int saves = 0;
        int opened = 0;
        Action<Point, bool> save = (point, locked) =>
        {
            settings.FloatingWindowX = point.X;
            settings.FloatingWindowY = point.Y;
            settings.FloatingWindowPositionLocked = locked;
            serialized = JsonSerializer.Serialize(settings);
            saves++;
        };
        using Form window = NewWindow(() => opened++, false, save);
        Restore(window, null);
        Rectangle work = Screen.PrimaryScreen!.WorkingArea;
        Check(window.Location == new Point(work.Right - window.Width - 18, work.Bottom - window.Height - 18),
            "First use must start near the primary-screen clock.");
        window.Show();
        Application.DoEvents();
        ToolStripMenuItem lockItem = (ToolStripMenuItem)window.ContextMenuStrip!.Items[1];
        Check(lockItem.CheckOnClick && !lockItem.Checked, "Lock menu must initially be unchecked.");

        foreach (Control target in Targets(window))
        {
            Check(target.ContextMenuStrip == window.ContextMenuStrip, "Text and background must share the context menu.");
            window.Location = new Point(work.Left + 100, work.Top + 100);
            Point before = window.Location;
            Drag(target, new Point(24, 16));
            Check(window.Location == before + new Size(24, 16),
                $"Unlocked dragging must work on {target.GetType().Name} ({target.Text}): expected {before + new Size(24, 16)}, got {window.Location}.");
            Check(settings.FloatingWindowX == window.Left && settings.FloatingWindowY == window.Top,
                "Mouse release must save the final position.");
        }

        lockItem.PerformClick();
        Check(lockItem.Checked && settings.FloatingWindowPositionLocked, "Menu click must lock and save immediately.");
        foreach (Control target in Targets(window))
        {
            Point before = window.Location;
            Drag(target, new Point(30, 20));
            Check(window.Location == before, "Locked dragging must never move the window.");
            Raise(target, "OnDoubleClick", EventArgs.Empty);
        }
        window.ContextMenuStrip.Items[0].PerformClick();
        Check(opened == window.Controls.Count + 2, "Double-click and show-main menu must work while locked.");
        WindowType.GetMethod("SetStatus")!.Invoke(window, new object[] { true, "29:16" });
        Check(window.Controls.Cast<Control>().Any(control => control.Text == "29:16"), "Timer display must still update while locked.");

        ReminderSettings restored = JsonSerializer.Deserialize<ReminderSettings>(serialized)!;
        using (Form reopened = NewWindow(() => { }, restored.FloatingWindowPositionLocked, null))
        {
            Restore(reopened, new Point(restored.FloatingWindowX!.Value, restored.FloatingWindowY!.Value));
            Check(reopened.Location == window.Location, "Recreated window must restore saved position.");
            Check(((ToolStripMenuItem)reopened.ContextMenuStrip!.Items[1]).Checked, "Recreated window must restore the lock.");
            Check(restored.WorkIntervalMinutes == 42 && restored.ShowFloatingWindow, "Saving window state must preserve reminder preferences.");
            Restore(reopened, new Point(100000, 100000));
            Check(Screen.FromPoint(reopened.Location).WorkingArea.Contains(reopened.Bounds),
                "Disconnected-monitor coordinates must restore entirely onscreen even when locked.");
        }

        lockItem.PerformClick();
        Check(!lockItem.Checked && !settings.FloatingWindowPositionLocked, "Unlock must save immediately.");
        Point unlockedPosition = window.Location;
        Drag(window.Controls[0], new Point(10, 10));
        Check(window.Location != unlockedPosition, "Unlock must restore dragging.");

        Begin(window.Controls[0]);
        Check(window.Controls[0].Capture, "Dragging must capture the initiating control.");
        lockItem.PerformClick();
        Point lockedPosition = window.Location;
        Raise(window.Controls[0], "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, 30, 30, 0));
        Check(window.Location == lockedPosition && !window.Controls[0].Capture,
            "Locking during a drag must stop movement and release capture.");

        window.Location = new Point(100000, 100000);
        WindowType.GetMethod("DisplaySettingsChanged", PrivateInstance)!.Invoke(window, new object?[] { null, EventArgs.Empty });
        Application.DoEvents();
        Check(Screen.FromPoint(window.Location).WorkingArea.Contains(window.Bounds),
            "A live display-change notification must return the locked window onscreen.");
        Check(settings.FloatingWindowX == window.Left && settings.FloatingWindowY == window.Top,
            "The corrected display-change position must be saved.");

        lockItem.PerformClick();
        Begin(window.Controls[0]);
        window.Controls[0].Capture = false;
        Point captureLostPosition = window.Location;
        Raise(window.Controls[0], "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, 16, 16, 0));
        Check(window.Location == captureLostPosition, "Losing mouse capture must stop dragging.");
        Check(saves > 5, "Window interactions must persist preferences.");
    }

    private static Form NewWindow(Action show, bool locked, Action<Point, bool>? save) =>
        (Form)Activator.CreateInstance(WindowType, new object?[] { SystemIcons.Application, show, locked, save })!;

    private static void Restore(Form window, Point? position) =>
        WindowType.GetMethod("RestorePosition")!.Invoke(window, new object?[] { position });

    private static IEnumerable<Control> Targets(Form window) =>
        new Control[] { window }.Concat(window.Controls.Cast<Control>());

    private static void Begin(Control target)
    {
        Raise(target, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 6, 6, 0));
    }

    private static void Drag(Control target, Point delta)
    {
        Begin(target);
        Raise(target, "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, 6 + delta.X, 6 + delta.Y, 0));
        Raise(target, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 6 + delta.X, 6 + delta.Y, 0));
    }

    private static void Raise(Control target, string name, EventArgs args) =>
        typeof(Control).GetMethod(name, PrivateInstance)!.Invoke(target, new object[] { args });

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }

        _assertions++;
    }
}
