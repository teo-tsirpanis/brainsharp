// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

module BFParser = 
    open Chessie.ErrorHandling
    open FParsec
    
    type BFSyntax = 
        | Plus
        | Minus
        | Left
        | Right
        | Dot
        | Comma
        | BracketLoop of BFSyntax list
    
    let private whiteSpace = optional (noneOf "+-<>[],.")
    let private strr v c = whiteSpace >>. stringReturn v c
    let private str v = whiteSpace >>. pstring v
    let private p, pref = createParserForwardedToRef<BFSyntax list, unit>()
    
    pref := choice [ strr "+" Plus
                     strr "-" Minus
                     strr "<" Left
                     strr ">" Right
                     strr "." Dot
                     strr "," Comma
                     between (str "[") (str "]") p |>> BracketLoop ]
            |> many
    
    let brainfuckParser = whiteSpace >>. p .>> whiteSpace .>> eof
    
    let private makeParseResult x = 
        match x with
        | Success(x, _, _) -> ok x
        | Failure(x, y, _) -> Trial.fail (ParseError(x, y))
    
    let parseFile path = 
        runParserOnFile brainfuckParser () path System.Text.Encoding.ASCII 
        |> makeParseResult
    
    let parseString s streamName = 
        runParserOnString brainfuckParser () (match streamName with
                                              | Some s -> s
                                              | None -> "") s
