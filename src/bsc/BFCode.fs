// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

type BFToken = 
    | MemoryControl of byte
    | MemorySet of byte
    | PointerControl of int
    | IOWrite
    | IORead
    | Loop of BFToken list

module BFCode =
    open Chessie.ErrorHandling
    open FParsec
    open System
    open System.IO

    let brainfuckParser =
        let whiteSpace = "+-<>[].," |> skipNoneOf |> many
        let stringReturn v c = whiteSpace >>. stringReturn v c
        let pstring v = whiteSpace >>. pstring v        
        let p, pref = createParserForwardedToRef<BFToken list, unit>()
        let symbols =     [ "+", MemoryControl 1uy
                            "-", MemoryControl 255uy
                            "<", PointerControl -1
                            ">", PointerControl 1
                            ".", IOWrite
                            ",", IORead ] 
                            |> List.map ((<||) stringReturn)
                            |> choice
                            <|> (between (pstring "[") (pstring "]") p |>> Loop)
        pref := whiteSpace >>. symbols .>> whiteSpace |> many
        p .>> eof

    let private makeParseResult = 
        function 
        | Success(x, _, _) -> ok x
        | Failure(x, _, _) -> x |> ParseError |> Trial.fail
    
    let parseFile path = 
        runParserOnFile brainfuckParser () path System.Text.Encoding.ASCII 
        |> makeParseResult
    
    let parseString s streamName = 
        runParserOnString brainfuckParser () (match streamName with
                                              | Some s -> s
                                              | None -> "") s
