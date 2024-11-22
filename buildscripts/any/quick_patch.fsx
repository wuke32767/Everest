open System.IO

// This script is used to quickly build and setup all the necessary files for any changes in Celeste.Mod.mm to be
// applied,
// Note: there are some edge cases, such as modifying patches for FNA, that are not covered by this script by
// default. This specific edge case can be fixed by adding "fna" to the `MiniInstaller-linux` call, refer to
// fast-mode documentation for more information.

#load "config.fsx"

open Helper

open Config

let TARGET_PROJECT_PATH = $"{EVERESTPATH}/Celeste.Mod.mm/Celeste.Mod.mm.csproj"

let OUT_DIR = $"{ARTIFACT_DIR}/quick_patch"

mkdir OUT_DIR

if
    Seq.contains "--first-run" arg
    || OUT_DIR + "/MonoMod.Backports.dll" |> File.Exists |> not
then
    echo "Doing a complete build given that dependencies are missing"
    commandLine $"""dotnet build "{TARGET_PROJECT_PATH}" --output "{OUT_DIR}" -c {CONFIGURATION}"""


echo $"Building {TARGET_PROJECT_PATH} without restore nor dependencies"

commandLine
    $"""dotnet build "{TARGET_PROJECT_PATH}" --no-restore --no-dependencies --output "{OUT_DIR}"  -c {CONFIGURATION}"""

echo "Copying artifacts into game dir"
cpfolder OUT_DIR CELESTEGAMEPATH

cd CELESTEGAMEPATH

if currentInstaller |> File.Exists |> not then
    echo $"Could not find {Path.GetFileName currentInstaller}! Run complete_build_and_install.sh in order to set it up."
    exit 1


echo "Calling %s" currentInstaller
commandLine $"{currentInstaller} --fastmode maingame"
