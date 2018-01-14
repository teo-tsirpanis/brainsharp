// Copyright (c) 2016 Theodore Tsirpanis
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
// include Fake libs
#r "./packages/build/FAKE/tools/FakeLib.dll"
#r "./packages/build/Fantomas/lib/FantomasLib.dll"
#r "./packages/build/FSharp.Compiler.Service/lib/net45/FSharp.Compiler.Service.dll"

open Fake
open Fake.AppVeyor
open Fake.AssemblyInfoFile
open Fake.DotNetCli
open Fake.Git
open Fantomas.FakeHelpers
open Fantomas.FormatConfig
open System
open System.IO

// Directories
[<Literal>]
let AppName = "Brainsharp"

type Framework =
    | Net
    | NetCore
    with
    override x.ToString() = match x with | Net -> "net47" | NetCore -> "netcoreapp2.0"

let runtimes = [
    "", NetCore // .NET Core Framework-dependent deployment
    ]

[<Literal>]
let AppVersionMessage =
    "Brainsharp Version {0}. \nGit commit hash: {1}. \nBuilt on {2} (UTC)."
    + "\nCreated by Theodore Tsirpanis and licensed under the MIT License."
    + "\nCheck out new releases on https://github.com/teo-tsirpanis/brainsharp/releases"

// version info
[<Literal>]
let BuildVersion = "1.3"

let version =
    match buildServer with
    | AppVeyor -> AppVeyorEnvironment.BuildVersion
    | _ -> BuildVersion // or retrieve from CI server

let buildDir = currentDirectory @@ "build"

let fantomasConfig =
    { FormatConfig.Default with PageWidth = 80
                                ReorderOpenDeclaration = true }

// Filesets
let appReferences = !!"/**/*.csproj" ++ "/**/*.fsproj"
let sourceFiles = !!"src/**/*.fs" ++ "src/**/*.fsx" ++ "build.fsx"
let resourceFiles = !!"src/**/resources/**.*"

let makeResource file =
    let content = File.ReadAllText file
    let file = file |> Path.GetFileNameWithoutExtension
    sprintf "let %s = \"\"\"\n%s\n\"\"\"" file content

let attributes =
    let gitHash = Information.getCurrentHash()
    let buildDate = DateTime.UtcNow.ToString()
    [ Attribute.Title "Brainsharp"
      Attribute.Description "A Brainfuck toolchain written in F#."

      Attribute.Copyright
          "Licensed under the MIT License. Created by Theodore Tsirpanis."
      Attribute.Metadata("Git Hash", gitHash)
      Attribute.Metadata("Build Date", buildDate)

      Attribute.Metadata
          ("Version Message",
           String.Format(AppVersionMessage, version, gitHash, buildDate))
      Attribute.Version version ]

// Targets
Target "Clean" (fun _ -> //DotNetCli.RunCommand id "clean"
                         DeleteDir buildDir)

Target "MakeResources" (fun _ ->
                            let content = resourceFiles |> Seq.map makeResource |> String.concat "\n" |> sprintf "module Brainsharp.Resources\n%s"
                            File.WriteAllText("./src/bsc/Resources.fs", content))

Target "AssemblyInfo" (fun _ -> CreateFSharpAssemblyInfo "./src/bsc/AssemblyInfo.fs" attributes)

Target "Restore" (fun _ -> DotNetCli.Restore id)

Target "Build" (fun _ ->
    Build (fun p -> {p with Configuration = "Release"; AdditionalArgs = ["--no-restore"]}))

Target "Publish" (fun _ ->
    runtimes
    |> Seq.map (fun (x, y) -> x, string y)
    |> Array.ofSeq
    |> Array.Parallel.iter (fun (rt, fx) ->
        let outFileName = sprintf "%s-%s-%s" AppName version (if rt <> "" then rt else "netcore")
        let output = buildDir @@ outFileName
        Publish (fun p ->
            {p with
                Runtime = rt
                Output = output
                Framework = fx
                AdditionalArgs = ["--no-restore"]})
        Zip output (sprintf "%s.zip" output) (!! output)
        DeleteDir output))

Target "FormatCode" (fun _ ->
    sourceFiles
    |> formatCode fantomasConfig
    |> Log "Formatted Files: ")

// Build order
"Clean"
    ==> "AssemblyInfo"
    ==> "Restore"
    ==> "Build"
    ==> "Publish"
"MakeResources" ==> "Build"
// start build
RunTargetOrDefault "Build"
