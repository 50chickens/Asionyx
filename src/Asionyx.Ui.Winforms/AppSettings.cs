using System.Text.Json;

namespace AsioAudioRouter;

public class AppSettings
{
    public string InputDevice { get; set; } = "";
    public int InputChannels { get; set; } = 2;
    public int InputVolume { get; set; } = 100;
    
    public string OutputDevice { get; set; } = "";
    public int OutputChannels { get; set; } = 2;
    public int OutputVolume { get; set; } = 100;
    
    public string MP3Device { get; set; } = "";
    public int MP3Channels { get; set; } = 2;
    public int MP3Volume { get; set; } = 100;
    public string LastMP3File { get; set; } = "";
    
    public bool IncludeASIO4ALL { get; set; } = false;
    
    // Generic UI state: control name -> serialized value
    public Dictionary<string, string> UIState { get; set; } = new Dictionary<string, string>();
    
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AsioAudioRouter",
        "settings.json"
    );
    
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception)
        {
            // If loading fails, return default settings
        }
        
        return new AppSettings();
    }
    
    public void Save()
    {
        try
        {
            string directory = Path.GetDirectoryName(SettingsFilePath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
}