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
  "Settings": {
    "DisablePlayerDefaultMVP": true
  },
  "Database": {
    "Host": "localhost",
    "Port": 3306,
    "Database": "cs2_mvp",
    "User": "root",
    "Password": "",
    "SslMode": "None"
  },
  "MVPSettings": {
    "PUBLIC MVP": {
      "CategoryFlags": [],
      "MVPs": {
        "mvp.1": {
          "MVPName": "Flawless",
          "MVPSound": "MVP.001_bamia",
          "EnablePreview": true,
          "Flags": []
        }
      }
    }
  }
}
```

## Authors

- **T3Marius** - Original author
- **zhw1nq** - Refactoring & optimization

## License

This project is licensed under the GPL-3.0 License - see the [LICENSE](LICENSE) file for details.
