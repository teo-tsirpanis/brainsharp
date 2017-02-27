// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

open Argu
open System.IO

type BuildArguments = 
    | [<MainCommand; ExactlyOnce>] SourceFile of path : string
    | [<ExactlyOnce; AltCommandLine("-o")>] OutputFile of path : string
    | [<Unique; AltCommandLine("-n")>] AssemblyName of string
    | [<Unique>] MemorySize of bytes : int
    | [<Unique>] Optimize of bool
    | Profile
    interface IArgParserTemplate with
        member s.Usage = 
            match s with
            | SourceFile _ -> 
                "The file containing the source code to be compiled."
            | OutputFile _ -> "The file to contain the produced program."
            | AssemblyName _ -> 
                "The name of the resulting assembly. Defaults to the name of the source file."
            | MemorySize _ -> 
                "The size of the memory the program will have. Defaults to 65536 bytes. On negative numbers, the absolute value will be used."
            | Optimize _ -> 
                "Controls optimization of the program. Defaults to true."
            | Profile -> "The generated program will display execution time in stderr."

type RunArguments = 
    | [<MainCommand; ExactlyOnce>] SourceFile of path : string
    | [<Unique; AltCommandLine("-i")>] InputFile of path : string
    | [<Unique; AltCommandLine("-o")>] OutputFile of path : string
    | [<Unique; AltCommandLine("-e")>] ExpectedOutput of path : string
    | [<Unique>] MemorySize of bytes : int
    | Profile
    | [<Unique>] Optimize of bool
    interface IArgParserTemplate with
        member s.Usage = 
            match s with
            | SourceFile _ -> "The file containing the source code to be run."
            | InputFile _ -> 
                "The file containing the input to the program. If not specified, it will be read from stdin."
            | OutputFile _ -> 
                "The file to contain the output of the program. If not specified, it will be written to stdout."
            | ExpectedOutput _ -> 
                "The file that contains the expected output of the program. Used for testing purposes."
            | MemorySize _ -> 
                "The size of the memory the program will have. Defaults to 65536 bytes. On negative numbers, the absolute value will be used."
            | Profile -> "Enables performance measurement of the program."
            | Optimize _ -> 
                "Controls optimization of the program. Defaults to true."

type CliArguments = 
    | [<CliPrefix(CliPrefix.Dash)>] V
    | [<CliPrefix(CliPrefix.None)>] Run of ParseResults<RunArguments>
    | [<CliPrefix(CliPrefix.None)>] Build of ParseResults<BuildArguments>
    interface IArgParserTemplate with
        member s.Usage = 
            match s with
            | V -> "Displays version."
            | Build _ -> 
                "Compiles a brainfuck program to a .NET executable file."
            | Run _ -> "Runs a brainfuck program."

type ResultArgs = 
    | BuildArgs of sourceFile : string * outputFile : string * assemblyName : string * memorySize : int * doOptimize : bool * doProfile: bool
    | RunArgs of sourceFile : string * input : TextReader * output : TextWriter * expectedOutput : string option * memorySize : int * doProfile : bool * doOptimize : bool
    | NoArgs

[<AutoOpen>]
module Arguments = 
    open Chessie.ErrorHandling
    open System
    open System.IO
    open System.Text
    
    [<Literal>]
    let DefaultMemorySize = UInt16.MaxValue
    
    let parseBuild (a : ParseResults<BuildArguments>) = 
        trial { 
            let! source = a.PostProcessResult(<@ BuildArguments.SourceFile @>, 
                                              fun s -> 
                                                  if File.Exists s then ok s
                                                  else fail (FileNotExist s))
            let! outputFile = a.PostProcessResult(<@ BuildArguments.OutputFile @>, 
                                                  fun s -> 
                                                      if File.Exists s |> not then 
                                                          ok s
                                                      else warn (FileExist s) s)
            let assemblyName = 
                a.GetResult
                    (<@ AssemblyName @>, 
                     Path.GetFileNameWithoutExtension outputFile)
            
            let memSize = 
                match a.TryGetResult(<@ BuildArguments.MemorySize @>) with
                | None | Some 0 -> DefaultMemorySize |> int
                | Some x -> x |> abs
            
            let doOptimize = a.GetResult(<@ BuildArguments.Optimize @>, true)
            let doProfile = a.Contains (<@ BuildArguments.Profile @>)
            return (source, outputFile, assemblyName, memSize, doOptimize, doProfile) 
                   |> BuildArgs
        }
    
    let parseRun (a : ParseResults<RunArguments>) = 
        trial { 
            let! source = a.PostProcessResult(<@ SourceFile @>, 
                                              fun s -> 
                                                  if File.Exists(s) then ok s
                                                  else fail (FileNotExist s))
            let! input = a.TryPostProcessResult(<@ InputFile @>, 
                                                fun s -> 
                                                    if File.Exists(s) then 
                                                        new StreamReader(s, 
                                                                         Encoding.ASCII) :> TextReader 
                                                        |> ok
                                                    else fail (FileNotExist s))
                         |> someOr (Console.In |> ok)
            let! output = a.TryPostProcessResult
                              (<@ OutputFile @>, 
                               fun s -> 
                                   let sw = 
                                       new StreamWriter(s, false, Encoding.ASCII) :> TextWriter
                                   if File.Exists(s) |> not then sw |> ok
                                   else sw |> warn (FileExist s))
                          |> someOr (Console.Out |> ok)
            let! expected = a.TryPostProcessResult
                                (<@ ExpectedOutput @>, 
                                 fun s -> 
                                     if File.Exists(s) then 
                                         File.ReadAllText(s, Encoding.ASCII)
                                         |> Some
                                         |> ok
                                     else None |> warn (FileNotExist(s)))
                            |> someOr (None |> ok)
            let memSize = 
                match a.TryGetResult(<@ MemorySize @>) with
                | None | Some 0 -> DefaultMemorySize |> int
                | Some x -> x |> abs
            
            let doProfile = a.Contains(<@ Profile @>)
            let doOptimize = a.GetResult(<@ Optimize @>, true)
            return (source, input, output, expected, memSize, doProfile, 
                    doOptimize) |> RunArgs
        }
    
    let parseArguments (a : ParseResults<CliArguments>) = 
        trial { 
            let! res = match a.GetSubCommand() with
                       | Run x -> parseRun x
                       | Build x -> parseBuild x
                       | _ -> NoArgs |> ok
            if a.Contains(<@ V @>) then return! ShowVersion
                                                |> warn
                                                <| NoArgs
            else return res
        }
