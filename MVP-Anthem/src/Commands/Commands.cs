using CounterStrikeSharp.API;
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
        ExecuteSettingsRefresh(player, "Fetching MVP settings from CDN...", "forced MVP settings fetch");
    }

    private static void OnMVPReloadCommand(CCSPlayerController? player, CommandInfo info)
    {
        ExecuteSettingsRefresh(player, "Reloading MVP settings from local file...", "reloaded MVP settings");
    }

    private static void ExecuteSettingsRefresh(CCSPlayerController? player, string startMsg, string logAction)
    {
        if (player == null || !player.IsValid) return;

        // Check admin permission
        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            player.PrintToChat($"{Instance.Localizer["prefix"]}You don't have permission to use this command.");
            return;
        }

        player.PrintToChat($"{Instance.Localizer["prefix"]}{startMsg}");

        int slot = player.Slot;
        Task.Run(async () =>
        {
            try
            {
                var newSettings = await MVPSettingsLoader.LoadOrFetchAsync();
                Instance.MVPSettings = newSettings;
                Server.NextFrame(() =>
                {
                    var p = Utilities.GetPlayerFromSlot(slot);
                    if (p != null && p.IsValid && p.Connected == PlayerConnectedState.PlayerConnected)
                        p.PrintToChat($"{Instance.Localizer["prefix"]}MVP settings updated successfully! Version: {newSettings.Version}");
                });
                Instance.Logger.LogInformation($"[MVP-Anthem] Admin {player.PlayerName} {logAction}");
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                {
                    var p = Utilities.GetPlayerFromSlot(slot);
                    if (p != null && p.IsValid && p.Connected == PlayerConnectedState.PlayerConnected)
                        p.PrintToChat($"{Instance.Localizer["prefix"]}Failed: {ex.Message}");
                });
                Instance.Logger.LogError($"[MVP-Anthem] Error: {ex.Message}");
            }
        });
    }
}