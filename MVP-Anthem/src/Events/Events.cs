using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using System.Text;
using Microsoft.Extensions.Logging;
using static MVPAnthem.MVPAnthem;

namespace MVPAnthem;

public static class Events
{
    private static Timer? _centerHtmlTimer, _centerTimer, _alertTimer;

    // Helper method to get localized message with fallback to mvp.default
    private static string GetLocalizedMessage(string mvpKey, string messageType)
    {
        var localizer = Instance.Localizer;
        var specificKey = $"{mvpKey}.{messageType}";
        var defaultKey = $"mvp.default.{messageType}";

        // Try specific key first (e.g., "mvp.1.chat")
        var msg = localizer[specificKey];
        if (!string.IsNullOrEmpty(msg))
            return msg;

        // Fallback to default (e.g., "mvp.default.chat")
        return localizer[defaultKey];
    }
    private static bool _isCenterHtmlActive, _isCenterActive, _isAlertActive;
    private static string _htmlMessage = "", _centerMessage = "", _alertMessage = "";

    public static void Initialize()
    {
        Instance.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
        Instance.RegisterEventHandler<EventRoundMvp>(OnRoundMvp);
        Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Instance.RegisterEventHandler<EventCsWinPanelMatch>(OnMapEnd);
    }

    public static void Dispose()
    {
        _centerHtmlTimer?.Kill();
        _centerTimer?.Kill();
        _alertTimer?.Kill();
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

        _ = Task.Run(async () =>
        {
            await Instance.PlayerCache.FlushPlayerAsync(player);
            Server.NextFrame(() => Instance.PlayerCache.RemovePlayer(player));
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

        // Find matching MVP settings
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

        foreach (var p in Utilities.GetPlayers().Where(p => !p.IsBot && !p.IsHLTV))
        {
            // Play sound
            Server.NextFrame(() =>
            {
                if (p.IsValid && p.PawnIsAlive)
                    p.EmitSound(mvpSound, p, 1.0f);
            });

            // Chat message
            if (mvpSettings.ShowChatMessage)
            {
                var msg = GetLocalizedMessage(mvpKey, "chat");
                if (!string.IsNullOrEmpty(msg))
                    p.PrintToChat(localizer["prefix"] + string.Format(msg, mvpPlayer.PlayerName, mvpSettings.MVPName));
            }

            // Center message
            if (mvpSettings.ShowCenterMessage)
            {
                var msg = GetLocalizedMessage(mvpKey, "center");
                if (!string.IsNullOrEmpty(msg))
                {
                    _centerMessage = string.Format(msg, mvpPlayer.PlayerName, mvpSettings.MVPName);
                    _isCenterActive = true;
                    _centerTimer?.Kill();
                    _centerTimer = Instance.AddTimer(timer.CenterDuration, () => { _isCenterActive = false; _centerTimer = null; });
                }
            }

            // Alert message
            if (mvpSettings.ShowAlertMessage)
            {
                var msg = GetLocalizedMessage(mvpKey, "alert");
                if (!string.IsNullOrEmpty(msg))
                {
                    _alertMessage = string.Format(msg, mvpPlayer.PlayerName, mvpSettings.MVPName);
                    _isAlertActive = true;
                    _alertTimer?.Kill();
                    _alertTimer = Instance.AddTimer(timer.AlertDuration, () => { _isAlertActive = false; _alertTimer = null; });
                }
            }

            // HTML message
            if (mvpSettings.ShowHtmlMessage)
            {
                var msg = GetLocalizedMessage(mvpKey, "html");
                if (!string.IsNullOrEmpty(msg))
                {
                    _htmlMessage = string.Format(msg, mvpPlayer.PlayerName, mvpSettings.MVPName);
                    _isCenterHtmlActive = true;
                    _centerHtmlTimer?.Kill();
                    _centerHtmlTimer = Instance.AddTimer(timer.CenterHtmlDuration, () => { _isCenterHtmlActive = false; _centerHtmlTimer = null; });
                }
            }
        }

        if (_isCenterHtmlActive || _isCenterActive || _isAlertActive)
            TickMessages();

        return HookResult.Continue;
    }

    private static void TickMessages()
    {
        Server.NextFrame(() =>
        {
            if (!_isCenterHtmlActive && !_isCenterActive && !_isAlertActive) return;

            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
            {
                if (_isCenterHtmlActive)
                    p.PrintToCenterHtml($"{_htmlMessage}</div>");

                if (_isCenterActive)
                    p.PrintToCenter(_centerMessage);

                if (_isAlertActive)
                    p.PrintToCenter(_alertMessage);
            }

            if (_isCenterHtmlActive || _isCenterActive || _isAlertActive)
                TickMessages();
        });
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
            Instance.PlayerCache.ClearAll();
        });

        return HookResult.Continue;
    }
}