// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"
#r "./packages/Fantomas/lib/FantomasLib.dll"
#r "./packages/FSharp.Compiler.Service/lib/net45/FSharp.Compiler.Service.dll"

open Fake
open Fake.AppVeyor
open Fake.AssemblyInfoFile
open Fake.Git
open Fantomas.FakeHelpers
open Fantomas.FormatConfig
open System.IO

// Directories
let appName = "BrainSharp"

// version info
let version = 
    match buildServer with
    | AppVeyor -> AppVeyorEnvironment.BuildVersion
    | _ -> "1.1" // or retrieve from CI server

let buildDir = "./build/"

let fantomasConfig = 
    { FormatConfig.Default with PageWidth = 80
                                ReorderOpenDeclaration = true }

// Filesets
let appReferences = !!"/**/*.csproj" ++ "/**/*.fsproj"
let sourceFiles = !!"src/**/*.fs" ++ "src/**/*.fsx" ++ "build.fsx"

// Targets
Target "Clean" (fun _ -> CleanDir buildDir)

let DoBuild f = 
    CreateFSharpAssemblyInfo "./src/bsc/AssemblyInfo.fs" 
        [ Attribute.Title "A Brainfuck toolchain for .NET"
          
          Attribute.Description 
              "A Brainfuck toolchain for .NET. Brainsharp aims to be very fast."
          
          Attribute.Copyright 
              "Licensed under the MIT License. Created by Theodore Tsirpanis."
          Attribute.Metadata("Git Hash", Information.getCurrentHash())
          Attribute.Version version ]
    f buildDir "Build" appReferences |> Log "AppBuild-Output: "

Target "Debug" (fun _ -> DoBuild MSBuildDebug)
Target "Release" (fun _ -> DoBuild MSBuildRelease)
Target "FormatCode" (fun _ -> 
    sourceFiles
    |> formatCode fantomasConfig
    |> Log "Formatted Files: ")
// Build order
"Clean" ?=> "Debug"
"Clean" ?=> "Release"
// start build
RunTargetOrDefault "Debug"
