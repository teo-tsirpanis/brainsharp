// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

open Argu
open Chessie.ErrorHandling
open System
open System.IO
open System.Text

type Arguments = 
    | [<ExactlyOnce; AltCommandLine("-s")>] SourceFile of path : string
    | [<Unique; AltCommandLine("-i")>] InputFile of path : string
    | [<Unique; AltCommandLine("-o")>] OutputFile of path : string
    | [<Unique; AltCommandLine("-e")>] ExpectedOutput of path : string
    interface IArgParserTemplate with
        member s.Usage = 
            match s with
            | SourceFile _ -> "The file containing the source code to be run."
            | InputFile _ -> 
                "The file containing the input to the program. If not specified, it will be read from stdin."
            | OutputFile _ -> 
                "The file to contain the output of the program. If not specified, it will be written to stdout"
            | ExpectedOutput _ -> 
                "The file that contains the expected output of the program. Used for testing purposes."

module Bsc = 
    let parser = ArgumentParser.Create<Arguments>()
    
    let getArgsParser argv = 
        try 
            parser.Parse argv |> ok
        with _ -> fail InvalidArguments
    
    let parseArgs (a : ParseResults<Arguments>) = 
        trial { 
            let someOr def = 
                function 
                | Some x -> x
                | None -> def
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
            let! output = a.TryPostProcessResult(<@ OutputFile @>, 
                                                 fun s -> 
                                                     if File.Exists(s) then 
                                                         new StreamWriter(s, 
                                                                          false, 
                                                                          Encoding.ASCII) :> TextWriter 
                                                         |> ok
                                                     else fail (FileNotExist s))
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
            return source, input, output, expected
        }
    
    let printMessage = 
        function 
        | InvalidArguments -> eprintfn "Usage: %s" (parser.PrintUsage())
        | _ -> ()
    
    let doResult r = eprintfn "%A" r
    
    [<EntryPoint>]
    let main argv = 
        let doIt argv = 
            argv
            |> getArgsParser
            >>= parseArgs
        match doIt argv with
        | Pass res -> 
            eprintfn "Success."
            doResult res
            0
        | Warn(res, msgs) -> 
            eprintfn "Warnings:"
            msgs |> List.iter printMessage
            doResult res
            0
        | Fail msgs -> 
            eprintfn "Errors:"
            msgs |> List.iter printMessage
            1
