# zMVP-Anthem

A Counter-Strike 2 plugin that allows players to select custom MVP anthems with a modern menu interface.

## Features

- 🎵 Custom MVP anthems per player
- 📂 Category-based MVP organization with flag/permission support
- 🎮 In-game menu powered by KitsuneMenu
- 💾 MySQL database for persistent preferences
- ⚡ In-memory cache with dirty-flag batching (writes only on disconnect/map end)
- 🔐 Per-MVP access control via SteamID, flags, or groups

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
- [KitsuneMenu](https://github.com/KitsuneLab-Development/KitsuneMenu)
- MySQL / MariaDB

## Installation

1. Download the latest release from [Releases](../../releases)
2. Extract to your CS2 server directory (`addons/counterstrikesharp/plugins/zMVP-Anthem/`)
3. Configure `config.json` in `addons/counterstrikesharp/configs/plugins/zMVP-Anthem/`
4. Restart server or run `css_plugins load zMVP-Anthem`

## Commands

| Command  | Description                 |
| -------- | --------------------------- |
| `!mvp`   | Open the MVP selection menu |
| `!music` | Alias for `!mvp`            |

## Configuration

```json
{
  "Version": "3.0.1",
  "Settings": {
    "DisablePlayerDefaultMVP": true,
    "CDN_URL": "https://cdn.vhming.com/json/fetch/mvp.json"
  },
  "Database": {
    "Host": "localhost",
    "Port": 3306,
    "Database": "cs2_mvp",
    "User": "root",
    "Password": "",
    "SslMode": "None"
  },
  "Commands": {
    "MVPCommands": [
       "mvp",
       "music"
    ]
  },
  "Timer": {
    "CenterHtmlDuration": 7,
    "CenterDuration": 7,
    "AlertDuration": 7
  }
}
```

## Changelog

### v3.0.2

- **Removed:** `ShowCenterMessage` and `ShowAlertMessage` (only Chat + HTML messages remain)
- **Removed:** `CenterDuration` and `AlertDuration` timer configs
- **Fixed:** Localization fallback — missing MVP-specific keys now auto-fallback to `mvp.default.*`

### v3.0.1

- **Fixed:** Critical thread safety issue causing "Native invoked on a non-main thread" errors during load and command execution
- **Added:** Configurable `CDN_URL` in `config.json` to customize MVP settings source
- **Improved:** Code quality and stability optimizations

## Authors

- **T3Marius** - Original author
- **zhw1nq** - Refactoring & optimization

## License

This project is licensed under the GPL-3.0 License - see the [LICENSE](LICENSE) file for details.
