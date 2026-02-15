using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using Microsoft.Extensions.Logging;
using static MVPAnthem.MVPAnthem;

namespace MVPAnthem;

public static class Commands
{
    public static void Initialize()
    {
        foreach (var cmd in Instance.Config.Commands.MVPCommands)
            Instance.AddCommand($"css_{cmd}", "Opens the MVP Menu", OnMVPCommand);

        // Admin commands
        Instance.AddCommand("css_mvp_fetch", "Force fetch MVP settings from CDN (Admin only)", OnMVPFetchCommand);
        Instance.AddCommand("css_mvp_reload", "Reload MVP settings from local file (Admin only)", OnMVPReloadCommand);
    }

    private static void OnMVPCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid) return;
        KitsuneMenuManager.DisplayMVP(player);
    }

    private static void OnMVPFetchCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid) return;

        // Check admin permission
        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            player.PrintToChat($"{Instance.Localizer["prefix"]}You don't have permission to use this command.");
            return;
        }

        player.PrintToChat($"{Instance.Localizer["prefix"]}Fetching MVP settings from CDN...");

        Task.Run(async () =>
        {
            try
            {
                var newSettings = await MVPSettingsLoader.LoadOrFetchAsync();
                Instance.MVPSettings = newSettings;

                // Precache new sounds
                Server.NextFrame(() => Instance.PrecacheMVPSounds());

                player.PrintToChat($"{Instance.Localizer["prefix"]}MVP settings updated successfully! Version: {newSettings.Version}");
                Instance.Logger.LogInformation($"[MVP-Anthem] Admin {player.PlayerName} forced MVP settings fetch");
            }
            catch (Exception ex)
            {
                player.PrintToChat($"{Instance.Localizer["prefix"]}Failed to fetch MVP settings: {ex.Message}");
                Instance.Logger.LogError($"[MVP-Anthem] Error fetching MVP settings: {ex.Message}");
            }
        });
    }

    private static void OnMVPReloadCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid) return;

        // Check admin permission
        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            player.PrintToChat($"{Instance.Localizer["prefix"]}You don't have permission to use this command.");
            return;
        }

        player.PrintToChat($"{Instance.Localizer["prefix"]}Reloading MVP settings from local file...");

        Task.Run(async () =>
        {
            try
            {
                var newSettings = await MVPSettingsLoader.LoadOrFetchAsync();
                Instance.MVPSettings = newSettings;
                player.PrintToChat($"{Instance.Localizer["prefix"]}MVP settings reloaded successfully! Version: {newSettings.Version}");
                Instance.Logger.LogInformation($"[MVP-Anthem] Admin {player.PlayerName} reloaded MVP settings");
            }
            catch (Exception ex)
            {
                player.PrintToChat($"{Instance.Localizer["prefix"]}Failed to reload MVP settings: {ex.Message}");
                Instance.Logger.LogError($"[MVP-Anthem] Error reloading MVP settings: {ex.Message}");
            }
        });
    }
}