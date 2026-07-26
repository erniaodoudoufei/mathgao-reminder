using System.Text.Json;

namespace MathGaoReminder;

public sealed class ReminderSettings
{
    public int WorkIntervalMinutes { get; set; } = 35;

    public int RestSeconds { get; set; } = 0;

    public bool ShowWarningWindow { get; set; } = true;

    public bool PlaySound { get; set; } = true;

    public bool ShowFloatingWindow { get; set; }

    public bool HideToTrayOnClose { get; set; } = true;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SettingsPath
    {
        get
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(basePath, "MathGaoReminder", "settings.json");
        }
    }

    public static ReminderSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new ReminderSettings();
            }

            string json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<ReminderSettings>(json) ?? new ReminderSettings();
        }
        catch
        {
            return new ReminderSettings();
        }
    }

    public void Save()
    {
        string? directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
    }
}

