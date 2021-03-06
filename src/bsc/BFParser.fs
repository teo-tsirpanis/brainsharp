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
    
    let private whiteSpace = 
        noneOf "+-<>[],."
        |> many
        |> optional
    
    let private strr v c = whiteSpace >>. stringReturn v c
    let private str v = whiteSpace >>. pstring v
    let private p, pref = createParserForwardedToRef<BFSyntax list, unit>()
    
    pref 
    := whiteSpace >>. choice [ strr "+" Plus
                               strr "-" Minus
                               strr "<" Left
                               strr ">" Right
                               strr "." Dot
                               strr "," Comma
                               between (str "[") (str "]") p |>> BracketLoop ] 
       .>> whiteSpace |> many
    
    let brainfuckParser = whiteSpace >>. p .>> whiteSpace .>> eof
    
    let private makeParseResult = 
        function 
        | Success(x, _, _) -> ok x
        | Failure(x, y, _) -> Trial.fail (ParseError(x, y))
    
    let parseFile path = 
        runParserOnFile brainfuckParser () path System.Text.Encoding.ASCII 
        |> makeParseResult
    
    let parseString s streamName = 
        runParserOnString brainfuckParser () (match streamName with
                                              | Some s -> s
                                              | None -> "") s
