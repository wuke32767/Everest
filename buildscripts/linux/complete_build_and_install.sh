#!/usr/bin/env bash

set -e

# This script is used to completely install or upgrade an celeste installation from the source files,
# you must call this script first before you're able to use quick_patch.sh

source config.sh

# It is sufficient to build only this two projects currently, all other ones will be build and copied as part of the build
# process of this two.
TARGET_PROJECTS_PATH=( 
    "$EVERESTPATH/Celeste.Mod.mm/Celeste.Mod.mm.csproj" 
    "$EVERESTPATH/MiniInstaller/MiniInstaller.csproj" 
)

OUT_DIR="$ARTIFACT_DIR/Everest"

mkdir -p "$OUT_DIR"

echo "Building with dotnet"
for PROJECT in "${TARGET_PROJECTS_PATH[@]}" ; do
    echo "Building $PROJECT"
    dotnet publish "$PROJECT" -o "$OUT_DIR" -c $CONFIGURATION
done

echo "Copying files"
cp -r "$OUT_DIR"/* ./"$CELESTEGAMEPATH"/

if [ "$2" != "--skipinstall" ]; then
  echo "Installing everest"
  cd "$CELESTEGAMEPATH"
  echo Calling MiniInstaller-linux
  ./MiniInstaller-linux
fi 
