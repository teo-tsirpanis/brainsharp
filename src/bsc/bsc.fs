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
open System
open System.Diagnostics
open System.IO
open System.Text

type Arguments = 
    | [<ExactlyOnce; AltCommandLine("-s")>] SourceFile of path : string
    | [<Unique; AltCommandLine("-i")>] InputFile of path : string
    | [<Unique; AltCommandLine("-o")>] OutputFile of path : string
    | [<Unique; AltCommandLine("-e")>] ExpectedOutput of path : string
    | [<Unique>] MemorySize of bytes : int
    | Profile
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
            | MemorySize _ -> 
                "The size of the memory the program will have. Default is 65536 bytes. On negative numbers, the absolute value will be used."
            | Profile -> 
                "Enables measurement of the execution time of the program."

module Bsc = 
    [<Literal>]
    let DisplayExpectenFoundTreshold = 200
    
    let defaultMemorySize = UInt16.MaxValue
    let parser = ArgumentParser.Create<Arguments>()
    
    let getArgsParser argv = 
        try 
            parser.Parse argv |> ok
        with _ -> fail InvalidArguments
    
    let parseArgs (a : ParseResults<Arguments>) = 
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
                | None | Some 0 -> defaultMemorySize |> int
                | Some x -> x |> abs
            
            let doProfile = a.Contains(<@ Profile @>)
            return source, input, output, expected, memSize, doProfile
        }
    
    let parseAndInterpret (source, input, output : TextWriter, expected, memSize, 
                           doProfile) = 
        trial { 
            use input = input
            use output = output
            let! theCode = parseFile source |> lift makeCodeTree
            // eprintfn "Code is parsed."
            let stringOut = new StringWriter()
            let sw = new Stopwatch()
            sw.Start()
            do! interpretEx memSize input stringOut theCode
            sw.Stop()
            do! (match doProfile with
                 | true -> 
                     sw.Elapsed
                     |> ExecutionTime
                     |> warn
                     <| ()
                 | false -> ok())
            let stringOut = stringOut.ToString()
            output.Write(stringOut)
            do! (match expected with
                 | Some x -> 
                     if x <> stringOut then fail (TestFailure(x, stringOut))
                     else ok()
                 | None -> ok())
            return ()
        }
    
    let getMessage = 
        function 
        | ExecutionTime x -> 
            (x.Hours, x.Minutes, x.Seconds, x.Milliseconds) 
            ||||> sprintf "Execution time (H:M:S:MS): %i:%i:%i:%i"
        | FileExist x -> 
            sprintf "File %s already exists. It will be overwritten." x
        | FileNotExist x -> sprintf "File %s does not exist." x
        | InvalidArguments -> sprintf "Usage: %s" (parser.PrintUsage())
        | ParseError(x, _) -> x
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
        let doIt argv = 
            argv
            |> getArgsParser
            >>= parseArgs
            >>= parseAndInterpret
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
