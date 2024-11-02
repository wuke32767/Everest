#!/usr/bin/env bash

# This script is used to completely install or upgrade an celeste installation from the source files,
# you must call this script first before you're able to use quick_patch.sh

# Feel free to modify it to where yours is
CELESTEGAMEPATH="../../_celestegame"

# It is sufficient to build only this two projects currently, all other ones will be build and copied as part of the build
# process of this two.
TARGET_PROJECTS_PATH=( "../../Celeste.Mod.mm/Celeste.Mod.mm.csproj" "../../MiniInstaller/MiniInstaller.csproj" )

ARTIFACT_DIR="../../artifacts/Everest"

# For TAS consistency it usually is necessary to build in Release mode, but for anything else Debug will suffice
# and provide better debugger support
CONFIGURATION="Debug"

mkdir -p $ARTIFACT_DIR || exit

echo "Building with dotnet"
for PROJECT in "${TARGET_PROJECTS_PATH[@]}" ; do
    echo "Building $PROJECT"
    dotnet publish "$PROJECT" -o $ARTIFACT_DIR -c $CONFIGURATION || exit
done

echo "Copying files"
cp -r $ARTIFACT_DIR/* ./$CELESTEGAMEPATH/ || exit

if [ "$2" != "--skipinstall" ]; then
  echo "Installing everest"
  cd $CELESTEGAMEPATH || exit
  echo Calling MiniInstaller-linux
  ./MiniInstaller-linux || exit
fi 
