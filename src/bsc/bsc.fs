// Copyright (c) 2016 Theodore Tsirpanis
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

open Argu
open BFCode
open BFParser
open Chessie.ErrorHandling
open Interpreter
open Optimizer
open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Text

module Bsc = 
    [<Literal>]
    let DisplayExpectenFoundTreshold = 200
    
    let parser = ArgumentParser.Create<_>()
    let getArgsParser argv = parser.Parse argv |> ok
    
    let doRun (source, input, output : TextWriter, expected, memSize, doProfile, 
               doOptimize) = 
        trial { 
            use input = input
            use output = output
            let! theCode = parseFile source
                           |> lift makeCodeTree
                           |> lift (if doOptimize then optimize
                                    else id)
            let sw = new Stopwatch()
            sw.Start()
            let! stringOut, instructionsRun = interpretExTee memSize input 
                                                  output theCode
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
        | RunArgs(a, b, c, d, e, f, g) -> doRun (a, b, c, d, e, f, g)
        | NoArgs -> ok()
    
    let getMessage = 
        function 
        | ProfilingResults(time, instructionsRun) -> 
            sprintf 
                "Execution time (H:M:S:MS): %i:%i:%i:%i \nTotal instructions run: %i" 
                time.Hours time.Minutes time.Seconds time.Milliseconds 
                instructionsRun
        | FileExist x -> 
            sprintf "File %s already exists. It will be overwritten." x
        | FileNotExist x -> sprintf "File %s does not exist." x
        | ParseError(x, _) -> x
        | ShowVersion -> 
            AssemblyVersionInformation.AssemblyMetadata_Version_Message
        | TestFailure(expected, found) -> 
            if expected.Length + found.Length < DisplayExpectenFoundTreshold * 2 then 
                sprintf 
                    "Program output is expected to be\n\t%s\n but it was \n\t%s" 
                    expected found
            else 
                "Program output is different than the expected, but it is not shown, because of its size."
        | UnexpectedEndOfInput -> "Unexpected end of input."
    
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
                msgs |> List.iter (getMessage >> eprintfn "%s")
                eprintfn "Success"
                0
            | Bad msgs -> 
                eprintfn ""
                eprintfn "Errors:"
                msgs |> List.iter (getMessage >> eprintfn "%s")
                1
        with e -> 
            match e with
            | :? ArguParseException -> 
                eprintfn "%s" e.Message
                0
            | _ -> 
                eprintfn "%O" e
                1
