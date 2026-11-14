#!/bin/sh
# Clean release build of the modded BStone (run in an MSYS2 MINGW64 shell). Fresh build dir so the
# embedded git hash matches HEAD. Usage: build_release.sh <bstone-mods checkout> [build dir]
set -e
SRC=${1:?usage: build_release.sh <bstone-mods checkout> [build dir]}
OUT=${2:-$SRC/build/release}
git -C "$SRC" diff --quiet HEAD || { echo "source tree has uncommitted changes"; exit 1; }
rm -rf "$OUT"
cmake -S "$SRC" -B "$OUT" -G Ninja -DCMAKE_BUILD_TYPE=Release \
  -DBSTONE_INTERNAL_SDL2=ON -DBSTONE_USE_STATIC_LINKING=ON \
  -DCMAKE_EXE_LINKER_FLAGS="-static -static-libgcc -static-libstdc++"
cmake --build "$OUT"
ls -la "$OUT/src/bstone/bstone.exe"
