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
    | _ -> "0.1" // or retrieve from CI server

let buildDir = "./build/"
let deployDir = "./deploy/"
let deployItem = deployDir + appName + "_" + version + ".zip"

let fantomasConfig = 
    { FormatConfig.Default with PageWidth = 80
                                ReorderOpenDeclaration = true }

// Filesets
let appReferences = !!"/**/*.csproj" ++ "/**/*.fsproj"
let sourceFiles = !!"src/**/*.fs" ++ "src/**/*.fsx" ++ "build.fsx"
let binaryFiles = !!(buildDir + "**/*.*") -- "build/**/*.pdb" -- "build/**/*.xml" -- "build/**/*.zip"

// Targets
Target "Clean" (fun _ -> CleanDirs [ buildDir; deployDir ])

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
Target "Deploy" (fun _ -> 
    let zipFileName = deployItem
    File.Create(zipFileName).Dispose()
    binaryFiles |> Zip buildDir zipFileName)
Target "FormatCode" (fun _ -> 
    sourceFiles
    |> formatCode fantomasConfig
    |> Log "Formatted Files: ")
Target "AppVeyor" (fun _ -> PushArtifact(fun p -> { p with Path = deployItem }))
// Build order
"Clean" ?=> "Debug"
"Clean" ?=> "Release" ==> "Deploy" ==> "AppVeyor"
// start build
RunTargetOrDefault "Debug"
