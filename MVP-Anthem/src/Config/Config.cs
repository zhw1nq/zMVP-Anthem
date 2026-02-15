using CounterStrikeSharp.API;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MVPAnthem;

public class PluginConfig
{
    public string Version { get; set; } = "3.0.1";
    public Settings_Config Settings { get; set; } = new();
    public Database_Config Database { get; set; } = new();
    public Commands_Config Commands { get; set; } = new();
    public Timer_Config Timer { get; set; } = new();
}

public class MVPSettingsConfig
{
    public string Version { get; set; } = "1.0.0";
    public Dictionary<string, CategorySettings> MVPSettings { get; set; } = new();
}

public class Settings_Config
{
    [JsonPropertyName("DisablePlayerDefaultMVP")]
    public bool DisablePlayerDefaultMVP { get; set; } = true;

    [JsonPropertyName("CDN_URL")]
    public string CDN_URL { get; set; } = "https://cdn.vhming.com/json/fetch/mvp.json";
}

public class Database_Config
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = "cs2_mvp";
    public string User { get; set; } = "root";
    public string Password { get; set; } = "";
    public string SslMode { get; set; } = "None";
}

public class Timer_Config
{
    [JsonPropertyName("CenterHtmlDuration")]
    public int CenterHtmlDuration { get; set; } = 7;

    [JsonPropertyName("CenterDuration")]
    public int CenterDuration { get; set; } = 7;

    [JsonPropertyName("AlertDuration")]
    public int AlertDuration { get; set; } = 7;
}

public class CategorySettings
{
    public List<string> CategoryFlags { get; set; } = new();
    public Dictionary<string, MVP_Settings> MVPs { get; set; } = new();
}

public class MVP_Settings
{
    public string MVPName { get; set; } = string.Empty;
    public string MVPSound { get; set; } = string.Empty;
    public bool EnablePreview { get; set; } = true;
    public bool ShowChatMessage { get; set; } = true;
    public bool ShowCenterMessage { get; set; } = true;
    public bool ShowAlertMessage { get; set; } = true;
    public bool ShowHtmlMessage { get; set; } = true;
    public string SteamID { get; set; } = string.Empty;
    public List<string> Flags { get; set; } = new();
}

public class Commands_Config
{
    public List<string> MVPCommands { get; set; } = new() { "mvp", "music" };
}

public static class ConfigLoader
{
    private static readonly string ConfigPath;

    static ConfigLoader()
    {
        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;

        ConfigPath = Path.Combine(
            Server.GameDirectory,
            "csgo",
            "addons",
            "counterstrikesharp",
            "configs",
            "plugins",
            assemblyName,
            "config.json"
        );
    }

    public static PluginConfig Load()
    {
        if (!File.Exists(ConfigPath))
        {
            CreateDefaultConfig();
        }

        return LoadConfigFromFile();
    }

    private static PluginConfig LoadConfigFromFile()
    {
        try
        {
            string configText = File.ReadAllText(ConfigPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                WriteIndented = true
            };

            var config = JsonSerializer.Deserialize<PluginConfig>(configText, options);
            return config ?? new PluginConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MVP-Anthem] Error loading config: {ex.Message}");
            return new PluginConfig();
        }
    }

    private static void CreateDefaultConfig()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);

        var defaultConfig = new PluginConfig
        {
            Version = "3.0.1",
            Settings = new Settings_Config
            {
                DisablePlayerDefaultMVP = true,
                CDN_URL = "https://cdn.vhming.com/json/fetch/mvp.json"
            },
            Database = new Database_Config
            {
                Host = "localhost",
                Port = 3306,
                Database = "cs2_mvp",
                User = "root",
                Password = "",
                SslMode = "None"
            },
            Commands = new Commands_Config
            {
                MVPCommands = new List<string> { "mvp", "music" }
            },
            Timer = new Timer_Config
            {
                CenterHtmlDuration = 7,
                CenterDuration = 7,
                AlertDuration = 7
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        string json = JsonSerializer.Serialize(defaultConfig, options);
        File.WriteAllText(ConfigPath, json);
    }
}

public static class MVPSettingsLoader
{
    private static readonly string MVPSettingsPath;
    private static readonly HttpClient HttpClient = new();

    static MVPSettingsLoader()
    {
        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;

        MVPSettingsPath = Path.Combine(
            Server.GameDirectory,
            "csgo",
            "addons",
            "counterstrikesharp",
            "configs",
            "plugins",
            assemblyName,
            "mvp-settings.json"
        );

        HttpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public static async Task<MVPSettingsConfig> LoadOrFetchAsync()
    {
        MVPSettingsConfig? localConfig = null;
        bool hasLocalFile = File.Exists(MVPSettingsPath);

        // Load local file if exists
        if (hasLocalFile)
        {
            localConfig = LoadFromFile();
        }

        // Get CDN URL from config
        string cdnUrl = MVPAnthem.Instance.Config.Settings.CDN_URL;

        // Try to fetch from CDN
        try
        {
            Console.WriteLine($"[MVP-Anthem] Checking for MVP settings updates from CDN: {cdnUrl}");
            var cdnConfig = await FetchFromCDNAsync(cdnUrl);

            if (cdnConfig != null)
            {
                // Compare versions
                if (localConfig == null || CompareVersions(cdnConfig.Version, localConfig.Version) > 0)
                {
                    Console.WriteLine($"[MVP-Anthem] New version available: {cdnConfig.Version} (current: {localConfig?.Version ?? "none"})");
                    Console.WriteLine("[MVP-Anthem] Downloading and saving new MVP settings...");

                    SaveToFile(cdnConfig);
                    return cdnConfig;
                }
                else
                {
                    Console.WriteLine($"[MVP-Anthem] MVP settings are up to date (version: {localConfig.Version})");
                    return localConfig;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MVP-Anthem] Failed to fetch from CDN: {ex.Message}");
        }

        // Fallback to local file or create default
        if (localConfig != null)
        {
            Console.WriteLine("[MVP-Anthem] Using local MVP settings file");
            return localConfig;
        }

        Console.WriteLine("[MVP-Anthem] Creating default MVP settings file");
        var defaultConfig = CreateDefaultMVPSettings();
        SaveToFile(defaultConfig);
        return defaultConfig;
    }

    private static async Task<MVPSettingsConfig?> FetchFromCDNAsync(string cdnUrl)
    {
        try
        {
            var response = await HttpClient.GetAsync(cdnUrl);
            response.EnsureSuccessStatusCode();

            string jsonContent = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            return JsonSerializer.Deserialize<MVPSettingsConfig>(jsonContent, options);
        }
        catch
        {
            return null;
        }
    }

    private static MVPSettingsConfig? LoadFromFile()
    {
        try
        {
            string configText = File.ReadAllText(MVPSettingsPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            return JsonSerializer.Deserialize<MVPSettingsConfig>(configText, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MVP-Anthem] Error loading MVP settings file: {ex.Message}");
            return null;
        }
    }

    private static void SaveToFile(MVPSettingsConfig config)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MVPSettingsPath)!);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            };

            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(MVPSettingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MVP-Anthem] Error saving MVP settings file: {ex.Message}");
        }
    }

    private static int CompareVersions(string version1, string version2)
    {
        try
        {
            var v1 = new Version(version1);
            var v2 = new Version(version2);
            return v1.CompareTo(v2);
        }
        catch
        {
            return string.Compare(version1, version2, StringComparison.Ordinal);
        }
    }

    private static MVPSettingsConfig CreateDefaultMVPSettings()
    {
        return new MVPSettingsConfig
        {
            Version = "1.0.0",
            MVPSettings = new Dictionary<string, CategorySettings>
            {
                {
                    "PUBLIC MVP", new CategorySettings
                    {
                        CategoryFlags = new List<string>(),
                        MVPs = new Dictionary<string, MVP_Settings>
                        {
                            {
                                "mvp.1", new MVP_Settings
                                {
                                    MVPName = "Flawless",
                                    MVPSound = "MVP.001_bamia",
                                    EnablePreview = true,
                                    ShowChatMessage = true,
                                    ShowCenterMessage = false,
                                    ShowAlertMessage = false,
                                    ShowHtmlMessage = true,
                                    SteamID = "",
                                    Flags = new List<string>()
                                }
                            },
                            {
                                "mvp.2", new MVP_Settings
                                {
                                    MVPName = "Ace",
                                    MVPSound = "MVP.002_ace",
                                    EnablePreview = true,
                                    ShowChatMessage = true,
                                    ShowCenterMessage = false,
                                    ShowAlertMessage = false,
                                    ShowHtmlMessage = true,
                                    SteamID = "",
                                    Flags = new List<string>()
                                }
                            }
                        }
                    }
                },
                {
                    "VIP MVP", new CategorySettings
                    {
                        CategoryFlags = new List<string> { "@css/vip" },
                        MVPs = new Dictionary<string, MVP_Settings>
                        {
                            {
                                "mvp.vip.1", new MVP_Settings
                                {
                                    MVPName = "VIP Exclusive",
                                    MVPSound = "MVP.vip_001",
                                    EnablePreview = true,
                                    ShowChatMessage = true,
                                    ShowCenterMessage = false,
                                    ShowAlertMessage = false,
                                    ShowHtmlMessage = true,
                                    SteamID = "",
                                    Flags = new List<string>()
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}