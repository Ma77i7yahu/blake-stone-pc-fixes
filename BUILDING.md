# Building

You don't need any of this to install the mod. It's for anyone who wants to rebuild it or port the changes.

## 1. The modded BStone

The source is the `bstone-mods` branch of this repository: BStone v1.3.4 plus the mod commits.

```
git clone --branch bstone-mods https://github.com/Ma77i7yahu/blake-stone-pc-fixes.git bstone-mods
```

The release exe was built in an MSYS2 **MINGW64** shell with the `mingw-w64-x86_64-gcc`, `mingw-w64-x86_64-cmake` and `mingw-w64-x86_64-ninja` packages. It uses BStone's bundled SDL2 and is fully static, so it needs no DLLs:

```
sh src/build_release.sh /path/to/bstone-mods
```

The result is `bstone-mods/build/release/src/bstone/bstone.exe`. The script refuses a source tree with uncommitted changes, and it configures from scratch so that the version string (`BStone v1.3.4+<commit>`) names the commit it was built from.

BStone's own build instructions (MSVC, Linux and so on) are in that branch's `README.md`. The mod adds no dependencies. The first mod commit removes one MSVC-only manifest line from `src/bstone/CMakeLists.txt` that newer CMake rejects with MinGW.

To apply the changes to your own BStone checkout instead, apply the patch series to the `v1.3.4` tag:

```
git checkout v1.3.4
git am /path/to/patches/00*.patch
```

## 2. The installer

The installer embeds the modded `bstone.exe` as a resource. It builds with the C# compiler that ships with Windows (.NET Framework 4):

```
src\build.cmd path\to\bstone.exe
```

This writes `src\BlakeStoneMods.exe`.

## Tests

`test/harness.cs` loads the installer by reflection and checks it on scratch folders:
- install over a stock BStone, then reinstall and remove
- install into a folder with no BStone, then remove
- foreign exes and an existing backup are left alone
- game detection for Aliens of Gold and Planet Strike
- config edits change only the chosen lines, add missing ones and back up the file
- the embedded exe matches the tested exe

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /out:harness.exe /r:System.Windows.Forms.dll test\harness.cs
harness.exe BlakeStoneMods.exe <stock bstone.exe> <modded bstone.exe> <a bstone_config.txt to copy> <scratch dir>
```

The last two checks read the current user's real BStone config and installed games; they only read them.
