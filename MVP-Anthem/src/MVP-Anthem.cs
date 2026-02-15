using CounterStrikeSharp.API.Core;
using MVPAnthem.Database;
using Menu;
using Microsoft.Extensions.Logging;

namespace MVPAnthem;

public partial class MVPAnthem : BasePlugin
{
    public override string ModuleAuthor => "T3Marius & zhw1nq";
    public override string ModuleName => "zMVP-Anthem";
    public override string ModuleVersion => "3.0.2";

    public static MVPAnthem Instance { get; set; } = new();
    public PluginConfig Config { get; set; } = new();
    public MVPSettingsConfig MVPSettings { get; set; } = new();
    public KitsuneMenu? Menu { get; set; }
    public IDatabaseProvider? DatabaseProvider { get; set; }
    public PlayerCache PlayerCache { get; set; } = null!;

    public override void Load(bool hotReload)
    {
        Instance = this;
        Menu = new KitsuneMenu(this);
        Config = ConfigLoader.Load();

        // Force static constructor to run on the main thread
        // (it calls Server.GameDirectory which is a native CS2 API)
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(MVPSettingsLoader).TypeHandle);

        // Load MVP settings from CDN or local file
        Task.Run(async () =>
        {
            MVPSettings = await MVPSettingsLoader.LoadOrFetchAsync();
            Logger.LogInformation("[MVP-Anthem] MVP settings loaded successfully");
        });

        InitializeDatabase();
        Events.Initialize();
        Commands.Initialize();
    }

    public override void Unload(bool hotReload) => Events.Dispose();

    private void InitializeDatabase()
    {
        var db = Config.Database;
        var connStr = $"Server={db.Host};Port={db.Port};Database={db.Database};" +
                      $"User={db.User};Password={db.Password};SslMode={db.SslMode};";

        DatabaseProvider = new MySqlDatabaseProvider(connStr, Logger);
        PlayerCache = new PlayerCache(DatabaseProvider);

        Task.Run(async () =>
        {
            if (await DatabaseProvider.TestConnectionAsync())
            {
                Logger.LogInformation("[MVP-Anthem] Database connection successful");
                await DatabaseProvider.InitializeAsync();
            }
            else
            {
                Logger.LogError("[MVP-Anthem] Failed to connect to database!");
            }
        });
    }
}
