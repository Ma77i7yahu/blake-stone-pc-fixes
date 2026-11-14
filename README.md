# Blake Stone (PC) – crosshair, smooth motion and movement mods

A modded build of the [BStone](https://github.com/bibendovsky/bstone) source port (1.3.4) for **Blake Stone: Aliens of Gold** and **Blake Stone: Planet Strike**, with a Windows installer:

- **Aiming crosshair**
- **Smooth turning**: the view turns in fractions of a degree, not whole-degree steps.
- **Smooth motion**: frames are drawn at your display's refresh rate, with the camera moving smoothly between the game's 70 Hz ticks.
- **Movement styles**:
  - **Duke Nukem 3D style** (default): full-speed strafe and backpedal, instant start and stop.
  - **Simple momentum**, optionally with **slow strafe**.
  - The **original** movement.
- **Secrets on the automap**: push walls and hidden areas always show.
- **Shootable downed Star Troopers**: one hit finishes a trooper that's playing dead.

All of these can be switched off in the installer or in game (**Options > Switches2**).

**No game files are included.** You need your own copy of the game (GOG, Steam or the Aliens of Gold shareware).

## Download

Get `BlakeStoneMods-v1.0.zip` from the [**Releases**](https://github.com/Ma77i7yahu/blake-stone-pc-fixes/releases/latest) page. `SHA256SUMS.txt` on the release page lets you check your download.

Full instructions, settings and removal: [docs/README-installer.md](docs/README-installer.md).

## Source

- **Full source of the modded BStone**: the [`bstone-mods`](https://github.com/Ma77i7yahu/blake-stone-pc-fixes/tree/bstone-mods) branch of this repo. It is BStone's own history up to the v1.3.4 release, plus the mod commits on top. The exe in the release is built from its tip.
- `patches/`: the same changes as a `git format-patch` series against BStone v1.3.4, plus one combined diff (`bstone-1.3.4-mods-combined.diff`). Patches 0006/0007 add and then revert an experiment, and 0009/0010 do the same.
- `src/`: the installer (`Installer.cs`, `build.cmd`) and `build_release.sh`, the clean build of the modded BStone.
- `test/harness.cs`: tests for the installer.

See [BUILDING.md](BUILDING.md).

## How it works (short)

All changes are in `src/bstone/src` and are controlled by new archived cvars (`gp_show_crosshair`, `gp_smooth_motion`, `gp_momentum`, `gp_momentum_style`, `gp_slow_strafe`, `gp_automap_secrets`, `gp_possum_shootable` and a few tuning values):

- **Crosshair**: drawn by `render_crosshair()` in the hardware renderer (`bstone_hw_video.cpp`) and by `DrawCrosshair()` in the software renderer (`3d_draw.cpp`).
- **Smooth turning**: the remainder in `anglefrac` gives a render-only view angle. Game logic still uses whole degrees.
- **Smooth motion**: `PlayLoop` draws extra frames that don't advance the game, until a whole tic is due. The camera is interpolated one tic behind.
- **Momentum**: `ControlMovement`'s thrusts go through `player_thrust()`, which collects a wish vector. `momentum_end()` then applies it with the chosen model. Duke style uses Duke Nukem 3D's movement rules: forward and strafe are clamped separately, and with `gp_duke_slide` on, Duke's friction is converted from 30 Hz to 70 Hz.
- **Automap**: `ShowOverhead` draws push wall tiles and hidden-area floors even if they haven't been seen.
- **Star Troopers**: they stay shootable and solid while down (`DamageActor` in `3d_state.cpp`).

All movement changes are off during demo playback and recording, so demos stay in sync.

## Licence

BStone, and so this modded build, is under the [GNU General Public License v2](LICENSE) (or later). The installer and scripts here are under the same licence. The game data isn't covered and isn't included.

## Disclaimer

Unofficial fan-made mod. It is not affiliated with or endorsed by Apogee, 3D Realms, JAM Productions, GOG, Steam or the BStone authors. Thanks to Boris I. Bendovsky and the BStone contributors for the port. Use at your own risk, and keep the backups the installer makes.
