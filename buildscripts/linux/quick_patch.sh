#!/usr/bin/env bash

# This script is used to quickly build and setup all the necessary files for any changes in Celeste.Mod.mm to be 
# applied,
# Note: there are some edge cases, such as modifying patches for FNA, that are not covered by this script by 
# default. This specific edge case can be fixed by adding "fna" to the `MiniInstaller-linux` call, refer to
# fast-mode documentation for more information.

# Feel free to modify it to where yours is
CELESTEGAMEPATH="../../_celestegame"

ARTIFACT_DIR="../../artifacts/quick_patch"

TARGET_PROJECT_PATH="../../Celeste.Mod.mm/Celeste.Mod.mm.csproj"

# For TAS consistency it usually is necessary to build in Release mode, but for anything else Debug will suffice
# and provide better debugger support
CONFIGURATION="Debug"

mkdir -p $ARTIFACT_DIR || exit

if [ "$1" == "--first-run" ] || [ ! -f $ARTIFACT_DIR/MonoMod.Backports.dll ]; then
  echo Doing a complete build given that dependencies are missing
  dotnet build $TARGET_PROJECT_PATH --output $ARTIFACT_DIR -c $CONFIGURATION || exit
fi

echo Building $TARGET_PROJECT_PATH without restore nor dependencies
dotnet build $TARGET_PROJECT_PATH --no-restore --no-dependencies --output $ARTIFACT_DIR  -c $CONFIGURATION || exit

echo Copying artifacts into game dir
cp -r $ARTIFACT_DIR/* ./$CELESTEGAMEPATH/ || exit

cd $CELESTEGAMEPATH || exit

if [ ! -f "./MiniInstaller-linux" ]; then
  echo "Could not find MiniInstaller-linux! Run complete_build_and_install.sh in order to set it up."
  exit
fi

echo Calling MiniInstaller-linux
./MiniInstaller-linux --fastmode maingame
