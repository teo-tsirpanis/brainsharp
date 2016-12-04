// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"
#r "./packages/Fantomas/lib/FantomasLib.dll"
#r "./packages/FSharp.Compiler.Service/lib/net45/FSharp.Compiler.Service.dll"

open Fake
open Fantomas.FakeHelpers
open Fantomas.FormatConfig

// Directories
let buildDir = "./build/"
let deployDir = "./deploy/"

let fantomasConfig = 
    { FormatConfig.Default with PageWidth = 80
                                ReorderOpenDeclaration = true }

// Filesets
let appReferences = !!"/**/*.csproj" ++ "/**/*.fsproj"
let sourceFiles = !!"src/**/*.fs" ++ "src/**/*.fsx" ++ "build.fsx"
let appName = "BrainSharp"
// version info
let version = "0.1" // or retrieve from CI server

// Targets
Target "Clean" (fun _ -> CleanDirs [ buildDir; deployDir ])

let DoBuild f = f buildDir "Build" appReferences |> Log "AppBuild-Output: "

Target "Debug" (fun _ -> DoBuild MSBuildDebug)
Target "Release" (fun _ -> DoBuild MSBuildRelease)
Target "Deploy" 
    (fun _ -> 
    !!(buildDir + "/**/*.*") -- "*.zip" 
    |> Zip buildDir (deployDir + appName + version + ".zip"))
Target "CheckCode" (fun _ -> sourceFiles |> checkCode fantomasConfig)
Target "FormatCode" (fun _ -> 
    sourceFiles
    |> formatCode fantomasConfig
    |> Log "Formatted Files: ")
// Build order
"Clean" ==> "Debug"
"Clean" ==> "Release" ==> "Deploy"
// start build
RunTargetOrDefault "Build"
