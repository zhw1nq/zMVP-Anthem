using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using static MVPAnthem.MVPAnthem;

namespace MVPAnthem;

public static class Events
{
    private static Timer? _centerHtmlTimer;
    private static bool _isCenterHtmlActive;
    private static string _htmlMessage = "";
    private static string? _lastMvpSound;
    private static Timer? _tickTimer;

    public static void PlaySoundToPlayer(CCSPlayerController player, MVP_Settings mvpSettings)
    {
        if (!player.IsValid) return;
        player.EmitSound(mvpSettings.MVPSound, player, 1.0f);
    }

    public static void RegisterEvents()
    {
        Instance.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
        Instance.RegisterEventHandler<EventRoundMvp>(OnRoundMvp, HookMode.Pre);
        Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Instance.RegisterEventHandler<EventCsWinPanelMatch>(OnMapEnd);
        Instance.RegisterEventHandler<EventRoundStart>(OnRoundStart);
    }

    public static void Dispose()
    {
        _centerHtmlTimer?.Kill();
        _tickTimer?.Kill();
    }

    private static string? GetLocalizedMessage(string mvpKey, string messageType)
    {
        var localizer = Instance.Localizer;

        var specificKey = $"{mvpKey}.{messageType}";
        var msg = localizer[specificKey];
        if (!string.IsNullOrEmpty(msg) && msg != specificKey)
            return msg;

        var defaultKey = $"mvp.default.{messageType}";
        var defaultMsg = localizer[defaultKey];
        if (!string.IsNullOrEmpty(defaultMsg) && defaultMsg != defaultKey)
            return defaultMsg;

        return null;
    }

    private static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _lastMvpSound = null;
        _isCenterHtmlActive = false;
        _centerHtmlTimer?.Kill();
        _centerHtmlTimer = null;
        _tickTimer?.Kill();
        _tickTimer = null;

        return HookResult.Continue;
    }

    private static HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

        int slot = player.Slot;
        Instance.AddTimer(3.0f, () =>
        {
            var p = Utilities.GetPlayerFromSlot(slot);
            if (p == null || !p.IsValid || p.Connected != PlayerConnectedState.PlayerConnected) return;
            _ = Task.Run(async () => await Instance.PlayerCache.GetPlayerDataAsync(p));
        });

        return HookResult.Continue;
    }

    private static HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        ulong steamId = player.SteamID;
        _ = Task.Run(async () =>
        {
            await Instance.PlayerCache.FlushPlayerAsync(steamId);
            Server.NextFrame(() => Instance.PlayerCache.RemovePlayer(steamId));
        });

        return HookResult.Continue;
    }

    private static HookResult OnRoundMvp(EventRoundMvp @event, GameEventInfo info)
    {
        var mvpPlayer = @event.Userid;
        if (mvpPlayer == null || !mvpPlayer.IsValid) return HookResult.Continue;

        if (Instance.Config.Settings.DisablePlayerDefaultMVP)
            mvpPlayer.MVPs = 0;

        var (mvpName, mvpSound) = Instance.PlayerCache.GetMVP(mvpPlayer);
        if (string.IsNullOrEmpty(mvpSound) || string.IsNullOrEmpty(mvpName))
            return HookResult.Continue;

        info.DontBroadcast = true;

        MVP_Settings? mvpSettings = null;
        string? mvpKey = null;

        foreach (var cat in Instance.MVPSettings.MVPSettings)
        {
            foreach (var entry in cat.Value.MVPs)
            {
                if (entry.Value.MVPName == mvpName && entry.Value.MVPSound == mvpSound)
                {
                    mvpSettings = entry.Value;
                    mvpKey = entry.Key;
                    break;
                }
            }
            if (mvpSettings != null) break;
        }

        if (mvpSettings == null || string.IsNullOrEmpty(mvpKey))
            return HookResult.Continue;

        var localizer = Instance.Localizer;
        var timer = Instance.Config.Timer;
        _lastMvpSound = mvpSound;

        foreach (var p in Utilities.GetPlayers().Where(p => !p.IsBot && !p.IsHLTV))
        {
            if (p.IsValid)
                PlaySoundToPlayer(p, mvpSettings);

            if (mvpSettings.ShowChatMessage)
            {
                var msg = GetLocalizedMessage(mvpKey, "chat");
                if (msg != null)
                    p.PrintToChat(localizer["prefix"] + string.Format(msg, mvpPlayer.PlayerName, mvpSettings.MVPName));
            }

            if (mvpSettings.ShowHtmlMessage)
            {
                var msg = GetLocalizedMessage(mvpKey, "html");
                if (msg != null)
                {
                    _htmlMessage = string.Format(msg, mvpPlayer.PlayerName, mvpSettings.MVPName);
                    _isCenterHtmlActive = true;
                    _centerHtmlTimer?.Kill();
                    _centerHtmlTimer = Instance.AddTimer(timer.CenterHtmlDuration, () => { _isCenterHtmlActive = false; _centerHtmlTimer = null; });
                }
            }
        }

        if (_isCenterHtmlActive)
        {
            _tickTimer?.Kill();
            _tickTimer = Instance.AddTimer(0.1f, () =>
            {
                if (!_isCenterHtmlActive) { _tickTimer?.Kill(); _tickTimer = null; return; }

                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
                    p.PrintToCenterHtml($"{_htmlMessage}</div>");
            }, TimerFlags.REPEAT);
        }

        return HookResult.Continue;
    }

    private static HookResult OnMapEnd(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        Task.Run(async () =>
        {
            int dirty = Instance.PlayerCache.GetDirtyCount();
            if (dirty > 0)
            {
                Instance.Logger.LogInformation($"[MVP-Anthem] Flushing {dirty} preferences to database...");
                await Instance.PlayerCache.FlushAllAsync();
            }
            Server.NextFrame(() => Instance.PlayerCache.ClearAll());
        });

        return HookResult.Continue;
    }
}
