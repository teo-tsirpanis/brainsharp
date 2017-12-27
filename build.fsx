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

[<Literal>]
let BuildDir = "./build/"

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
Target "Clean" (fun _ -> DotNetCli.RunCommand id "clean"
                         DeleteDir BuildDir)

Target "MakeResources" (fun _ -> 
                            let content = resourceFiles |> Seq.map makeResource |> String.concat "\n" |> sprintf "module Brainsharp.Resources\n%s"
                            File.WriteAllText("./src/bsc/Resources.fs", content))

Target "AssemblyInfo" (fun _ -> CreateFSharpAssemblyInfo "./src/bsc/AssemblyInfo.fs" attributes)

Target "Build" (fun _ -> 
    DotNetCli.Restore id
    Build (fun p -> {p with Output = sprintf "./../../%s" BuildDir}))

Target "FormatCode" (fun _ -> 
    sourceFiles
    |> formatCode fantomasConfig
    |> Log "Formatted Files: ")

// Build order
"Clean" 
    ?=> "AssemblyInfo"
    ==> "Build"
"MakeResources" ==> "Build"
// start build
RunTargetOrDefault "Build"
