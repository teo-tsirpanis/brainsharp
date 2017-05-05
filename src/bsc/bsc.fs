// Copyright (c) 2016 Theodore Tsirpanis
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

open Argu
open BFCode
open Chessie.ErrorHandling
open CodeEmitter
open Interpreter
open Optimizer
open System
open System.Diagnostics
open System.IO

module Bsc = 
    let parser = ArgumentParser.Create<_>()
    let getArgsParser argv = parser.Parse argv |> ok
    
    let doBuild (source, outputFile, assemblyName, memSize, doOptimize, doProfile, doExportSource) = 
        trial { 
            let! theCode = parseFile source |> lift (if doOptimize then optimize else id)
            if doExportSource then
                let theSource = CodeEmitter.emitProgram memSize theCode
                File.WriteAllText (outputFile, theSource)
            else
                do! Compiler.compileToFile assemblyName outputFile memSize doProfile theCode
        }
    
    let doRun (source, input, output : TextWriter, expected, memSize, doProfile, 
               doOptimize) = 
        trial { 
            use input = input
            use output = output
            let! theCode = parseFile source
                           |> lift (if doOptimize then optimize
                                    else id)
            let sw = Stopwatch()
            sw.Start()
            let stringOut, instructionsRun = 
                interpretEx memSize input output theCode
            sw.Stop()
            do! (match doProfile with
                 | true -> 
                     (sw.Elapsed, instructionsRun)
                     |> ProfilingResults
                     |> warn
                     <| ()
                 | false -> ok())
            do! (match expected with
                 | Some x -> 
                     if x <> stringOut then fail (TestFailure(x, stringOut))
                     else ok()
                 | None -> ok())
            return ()
        }
    
    let splitArgs = 
        function 
        | BuildArgs(a, b, c, d, e, f, g) -> doBuild (a, b, c, d, e, f, g)
        | RunArgs(a, b, c, d, e, f, g) -> doRun (a, b, c, d, e, f, g)
        | NoArgs -> ok()
    
    [<EntryPoint>]
    let main argv = 
        try 
            let doIt argv = 
                argv
                |> getArgsParser
                >>= parseArguments
                >>= splitArgs
            match doIt argv with
            | Ok(_, msgs) -> 
                eprintfn ""
                msgs |> List.iter (eprintfn "%O")
                eprintfn "Success"
                0
            | Bad msgs -> 
                eprintfn ""
                eprintfn "Errors:"
                msgs |> List.iter (eprintfn "%O")
                1
        with e -> 
            match e with
            | :? ArguParseException -> 
                eprintfn "%s" e.Message
                0
            | _ -> 
                eprintfn "%O" e
                1
