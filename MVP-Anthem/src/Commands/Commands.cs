using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using static MVPAnthem.MVPAnthem;

namespace MVPAnthem;

public static class Commands
{
    public static void Initialize()
    {
        foreach (var cmd in Instance.Config.Commands.MVPCommands)
            Instance.AddCommand($"css_{cmd}", "Opens the MVP Menu", OnMVPCommand);
    }

    private static void OnMVPCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid) return;
        KitsuneMenuManager.DisplayMVP(player);
    }
}