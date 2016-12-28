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
open System.IO
open System.Text

type Arguments = 
    | [<ExactlyOnce; AltCommandLine("-s")>] SourceFile of path : string
    | [<Unique; AltCommandLine("-i")>] InputFile of path : string
    | [<Unique; AltCommandLine("-o")>] OutputFile of path : string
    | [<Unique; AltCommandLine("-e")>] ExpectedOutput of path : string
    | [<Unique>] MemorySize of bytes : int
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
            | MemorySize _ -> "The size of the memory the program will have. Default is 30000 bytes. On negative numbers, the absolute value will be used."

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
            let memSize = (match a.GetResult (<@ MemorySize @>, defaultValue = 30000) with | 0 -> 30000 | x -> x) |> abs
            return source, input, output, expected, memSize
        }
    
    let parseAndInterpret (source, input, output : TextWriter, expected, memSize) = 
        trial { 
            use input = input
            use output = output
            let! theCode = parseFile source |> lift makeCodeTree
            let stringOut = new StringWriter()
            do! interpretEx memSize input stringOut theCode
            let stringOut = stringOut.ToString().Trim() //stringOut has a newline at the end.
            output.Write(stringOut)
            let! _ = match expected with
                     | Some x -> 
                         if x <> stringOut then fail (TestFailure(x, stringOut))
                         else ok()
                     | None -> ok()
            return ()
        }
    
    let getMessage = 
        function 
        | FileExist x -> sprintf "File %s already exists. It will be overwritten." x
        | FileNotExist x -> sprintf "File %s does not exist." x
        | InvalidArguments -> sprintf "Usage: %s" (parser.PrintUsage())
        | ParseError(x, _) -> x
        | TestFailure(expected, found) -> 
            sprintf "Program output is expected to be\n\t%s\n but it was \n\t%s" 
                expected found
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
            msgs |> List.iter (getMessage >> eprintfn "%s")
            eprintfn "Success"
            0
        | Bad msgs -> 
            eprintfn "Errors:"
            msgs |> List.iter (getMessage >> eprintfn "%s")
            1
