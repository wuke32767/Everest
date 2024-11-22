module Config

open System.Diagnostics
open System
open System.IO
open System.Runtime.InteropServices
open System.Text.RegularExpressions

#load "helper.fsx"
open Helper

let SCRIPT_PATH = arg[1] |> Path.GetFullPath

let SCRIPT_DIR = Path.Combine(SCRIPT_PATH, "..") |> Path.GetFullPath

Environment.CurrentDirectory <- SCRIPT_DIR

// Feel free to adjust these

// Assume default script location
let EVERESTPATH = envDefault "../.." "EVERESTPATH"

// Setting this variable is mandatory for the scripts to function, you can either set it before running the script
// or hardcode it in here.
// CELESTEGAMEPATH=/path/to/the/game
let CELESTEGAMEPATH = env "CELESTEGAMEPATH"

if CELESTEGAMEPATH = null then
    echo "$CELESTEGAMEPATH is not set! Please assign it with your target celeste game directory."
    exit 1

// For TAS consistency it usually is necessary to build in Release mode, but for anything else Debug will suffice
// and provide better debugger support
let CONFIGURATION = envDefault "Debug" "CONFIGURATION"

let ARTIFACT_DIR = envDefault $"{EVERESTPATH}/artifacts" "ARTIFACT_DIR"
