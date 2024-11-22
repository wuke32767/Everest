open System.Diagnostics
open System.Threading
open System
open System.Runtime.InteropServices

// This script is used to completely install or upgrade an celeste installation
// from the source files; you must call this script first before you're able to
// use quick_patch.sh

#load "config.fsx"

open Helper

open Config

// It is sufficient to build only this two projects currently, all other ones will be build and copied as part of the build
// process of this two.
let TARGET_PROJECTS_PATH =
    [| $"{EVERESTPATH}/Celeste.Mod.mm/Celeste.Mod.mm.csproj"
       $"{EVERESTPATH}/MiniInstaller/MiniInstaller.csproj" |]

let OUT_DIR = $"{ARTIFACT_DIR}/Everest"

mkdir OUT_DIR

echo "Building with dotnet"

for project in TARGET_PROJECTS_PATH do
    echo $"Building {project}"
    commandLine $"""dotnet publish "{project}" -o "{OUT_DIR}" -c {CONFIGURATION}"""

echo "Copying files"
cpfolder OUT_DIR CELESTEGAMEPATH

if Seq.contains "--skipinstall" arg |> not then
    echo "Installing everest"
    cd CELESTEGAMEPATH
    echo "Calling MiniInstaller"

    commandLine currentInstaller
