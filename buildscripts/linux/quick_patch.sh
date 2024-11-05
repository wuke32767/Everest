#!/usr/bin/env bash

set -e

# This script is used to quickly build and setup all the necessary files for any changes in Celeste.Mod.mm to be 
# applied,
# Note: there are some edge cases, such as modifying patches for FNA, that are not covered by this script by 
# default. This specific edge case can be fixed by adding "fna" to the `MiniInstaller-linux` call, refer to
# fast-mode documentation for more information.

source config.sh

TARGET_PROJECT_PATH="$EVERESTPATH/Celeste.Mod.mm/Celeste.Mod.mm.csproj"

OUT_DIR="$ARTIFACT_DIR/quick_patch"

mkdir -p "$OUT_DIR"

if [ "$1" == "--first-run" ] || [ ! -f "$OUT_DIR"/MonoMod.Backports.dll ]; then
  echo Doing a complete build given that dependencies are missing
  dotnet build "$TARGET_PROJECT_PATH" --output "$OUT_DIR" -c $CONFIGURATION
fi

echo Building $TARGET_PROJECT_PATH without restore nor dependencies
dotnet build "$TARGET_PROJECT_PATH" --no-restore --no-dependencies --output "$OUT_DIR"  -c $CONFIGURATION

echo Copying artifacts into game dir
cp -r "$OUT_DIR"/* ./"$CELESTEGAMEPATH"/

cd "$CELESTEGAMEPATH"

if [ ! -f "./MiniInstaller-linux" ]; then
  echo "Could not find MiniInstaller-linux! Run complete_build_and_install.sh in order to set it up."
  exit
fi

echo Calling MiniInstaller-linux
./MiniInstaller-linux --fastmode maingame
