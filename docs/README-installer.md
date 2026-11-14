# Blake Stone Mods v1.0

A modded build of the [BStone](https://github.com/bibendovsky/bstone) source port (version 1.3.4) for **Blake Stone: Aliens of Gold** and **Blake Stone: Planet Strike** on Windows, plus an installer.

## What it adds

- **Aiming crosshair**: a small white cross with a black outline in the centre of the screen. Works with both renderers.
- **Smooth turning**: the original engine turns in whole degrees, so mouse turning steps. The view now turns in fractions of a degree. The game logic is unchanged.
- **Smooth motion**: the game runs at 70 ticks a second. Frames are now drawn at your display's refresh rate, with the camera moving smoothly between ticks.
- **Movement styles**, chosen in the installer or in game:
  - **Duke Nukem 3D style** (default): you strafe and backpedal at full speed and move faster on diagonals. You start and stop instantly, with no slide. Forward speed is 200% of the Duke scale (about 9 tiles a second when running), strafe speed 100%.
  - **Simple momentum**: a short speed-up (120 ms) and slow-down (150 ms). It can be combined with **slow strafe**, which moves sideways at 40% speed so the crosshair stays on target while strafing.
  - **Original**: Blake Stone's own movement.
- **Secrets on the automap**: push walls and hidden areas always show on the automap and the teleporter map, even before you have found them.
- **Shootable downed Star Troopers**: Star Troopers that drop and play dead can't normally be hit until they get up. Now they can, and one hit finishes them.

## Requirements

- Windows 7 or later with .NET Framework 4 (built into Windows 8 and later).
- Your own copy of Blake Stone: Aliens of Gold or Planet Strike. GOG and Steam both work, as does the Aliens of Gold shareware. **No game files are included.**
- You don't need BStone already: the installer brings its own `bstone.exe`.

## Install

1. Run `BlakeStoneMods.exe`. It finds GOG and Steam copies by itself. Otherwise click **Browse** and pick the game folder, the one that holds `VSWAP.BS6` (Aliens of Gold) or `VSWAP.VSI` (Planet Strike).
2. Choose the options and click **Install**.
3. Start the game with the new desktop shortcut, or run `bstone.exe` from the game folder.

If the game folder already has a `bstone.exe`, it is kept as `bstone.original.exe`. The options go into BStone's config file, `%APPDATA%\bibendovsky\bstone\bstone_config.txt`. Before the first change, a copy of it is saved as `bstone_config.txt.before-mods.bak`.

Run the installer again to change the options. You can also change them in game under **Options > Switches2**: Show Crosshair, Smooth Motion, Momentum (cycles Off, Simple, Duke Nukem 3D) and Slow Strafe. The automap secrets and Star Trooper options are only in the config file (`gp_automap_secrets`, `gp_possum_shootable`; `"1"` is on, `"0"` is off).

If BStone finds more than one game, it asks which one to play when it starts. That is BStone's normal behaviour.

**Tip:** smooth motion needs BStone to run at your display's refresh rate. If BStone's video options or `vid_refresh_rate` in the config are set lower than your display (for example 60 on a 120 Hz screen), raise them.

## Advanced settings (config file only)

| Setting | Default | What it does |
|---|---|---|
| `gp_duke_speed_pct` | 200 | Duke style forward/back speed, % of the Duke scale (25-400) |
| `gp_duke_strafe_pct` | 100 | Duke style strafe speed (25-400) |
| `gp_duke_slide` | 0 | 1 = Duke's real acceleration and slide instead of instant start/stop |
| `gp_momentum_accel_ms` | 120 | Simple momentum: time to reach full speed (0-2000) |
| `gp_momentum_friction_ms` | 150 | Simple momentum: time to stop (0-2000) |
| `gp_strafe_speed_pct` | 40 | Slow strafe speed, % of normal (10-100) |

## Remove

Run the installer, pick the folder and click **Remove**. Your original `bstone.exe` comes back. If there wasn't one, the mod's `bstone.exe` is deleted. The extra lines in the config file do no harm: stock BStone ignores them and drops them the next time it saves.

## Notes

- Demos play with the original movement, so they stay in sync.
- Tested on the GOG version of Aliens of Gold. Planet Strike starts and the menus fit, but it hasn't been played through.
- Source code, the patches and how to build: https://github.com/Ma77i7yahu/blake-stone-pc-fixes

## Credits and licence

BStone is by Boris I. Bendovsky and contributors. Blake Stone's source code was released by Apogee under the GPL. This modded build is under the GNU General Public License v2 (or later), the same as BStone. The full source is in the GitHub repository above.

Unofficial fan-made mod. It is not affiliated with or endorsed by Apogee, 3D Realms, JAM Productions, GOG, Steam or the BStone authors. Use at your own risk.
