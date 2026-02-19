using CounterStrikeSharp.API.Core;
using System.Collections.Concurrent;
using MVPAnthem.Database;

namespace MVPAnthem;

public class CachedPlayerData
{
    public ulong SteamId { get; set; }
    public string? MVPName { get; set; }
    public string? MVPSound { get; set; }
}

public class PlayerCache(IDatabaseProvider database)
{
    private readonly ConcurrentDictionary<ulong, CachedPlayerData> _cache = new();
    private readonly ConcurrentDictionary<ulong, bool> _dirty = new();

    public async Task<CachedPlayerData?> GetPlayerDataAsync(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.SteamID == 0)
            return null;

        if (_cache.TryGetValue(player.SteamID, out var cached))
            return cached;

        var pref = await database.GetPlayerPreferenceAsync(player.SteamID);
        if (pref == null) return null;

        var data = new CachedPlayerData
        {
            SteamId = pref.SteamId,
            MVPName = pref.MVPName,
            MVPSound = pref.MVPSound
        };
        _cache[player.SteamID] = data;
        return data;
    }

    public (string? mvpName, string? mvpSound) GetMVP(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.SteamID == 0)
            return (null, null);

        return _cache.TryGetValue(player.SteamID, out var data)
            ? (data.MVPName, data.MVPSound)
            : (null, null);
    }

    public void SetMVP(CCSPlayerController player, string mvpName, string mvpSound)
    {
        if (player == null || !player.IsValid || player.SteamID == 0) return;

        var data = _cache.GetOrAdd(player.SteamID, _ => new CachedPlayerData { SteamId = player.SteamID });
        data.MVPName = mvpName;
        data.MVPSound = mvpSound;
        _dirty[player.SteamID] = true;
    }

    public void RemoveMVP(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.SteamID == 0) return;

        if (_cache.TryGetValue(player.SteamID, out var data))
        {
            data.MVPName = null;
            data.MVPSound = null;
            _dirty[player.SteamID] = true;
        }
    }

    public async Task FlushPlayerAsync(CCSPlayerController player)
    {
        if (player == null || player.SteamID == 0) return;
        await FlushPlayerAsync(player.SteamID);
    }

    public async Task FlushPlayerAsync(ulong steamId)
    {
        if (steamId == 0) return;

        if (_dirty.TryRemove(steamId, out _) && _cache.TryGetValue(steamId, out var data))
            await database.SavePlayerPreferenceAsync(data.SteamId, data.MVPName, data.MVPSound);
    }

    public async Task FlushAllAsync()
    {
        var tasks = _dirty.Keys
            .Where(id => _dirty.TryRemove(id, out _) && _cache.TryGetValue(id, out _))
            .Select(id => database.SavePlayerPreferenceAsync(_cache[id].SteamId, _cache[id].MVPName, _cache[id].MVPSound))
            .ToList();

        await Task.WhenAll(tasks);
    }

    public void RemovePlayer(CCSPlayerController player)
    {
        if (player == null || player.SteamID == 0) return;
        RemovePlayer(player.SteamID);
    }

    public void RemovePlayer(ulong steamId)
    {
        if (steamId == 0) return;
        _cache.TryRemove(steamId, out _);
        _dirty.TryRemove(steamId, out _);
    }

    public void ClearAll()
    {
        _cache.Clear();
        _dirty.Clear();
    }

    public int GetDirtyCount() => _dirty.Count;
}
