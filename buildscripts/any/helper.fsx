module Helper

open System.Diagnostics
open System
open System.IO
open System.Runtime.InteropServices
open System.Text.RegularExpressions

// helpers.
// f# is not designed as script language anyway.

let arg = Environment.GetCommandLineArgs()
let env = Environment.GetEnvironmentVariable
let envDefault def = env >> defaultIfNull def

let currentInstaller =
    if RuntimeInformation.IsOSPlatform OSPlatform.Linux then
        "./MiniInstaller-linux"
    else if RuntimeInformation.IsOSPlatform OSPlatform.OSX then
        "./MiniInstaller-osx"
    else if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
        match RuntimeInformation.OSArchitecture with
        | Architecture.X86 -> "./MiniInstaller-win.exe"
        | Architecture.X64 -> "./MiniInstaller-win64.exe"

        | _ -> "./MiniInstaller-win.exe"
    else
        "can't recognize your platform" |> PlatformNotSupportedException |> raise


let commandLine (cmd: string) =
    let p = new Process()
    let split = cmd.IndexOf ' '

    if split < 0 then
        p.StartInfo.FileName <- cmd
    else
        p.StartInfo.FileName <- cmd[0 .. split - 1]
        p.StartInfo.Arguments <- cmd[split..]

    p.StartInfo.WorkingDirectory <- Environment.CurrentDirectory
    p.Start() |> ignore
    p.WaitForExit()

let mkdir = Directory.CreateDirectory >> ignore

let cp from tos = File.Copy(from, tos, true)

let echo = printfn

/// <summary>
/// copy all files from "from" to "tar"
/// <code>cp -r from/* tar</code>
/// yep, a little different from the actual command
/// </summary>
let rec cpfolder from tar =
    mkdir tar

    for path in Directory.GetFileSystemEntries from do
        let name = Path.GetFileName path
        let target = Path.Combine [| tar; name |]

        if File.Exists path then
            cp path target
        else if Directory.Exists path then
            cpfolder path target
        else //how would this happen
            printf $"path is not File or Directory!\n"

let cd = Directory.SetCurrentDirectory
