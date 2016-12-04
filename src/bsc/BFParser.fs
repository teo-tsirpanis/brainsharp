// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

module BFParser = 
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
    
    let BrainfuckParser = whiteSpace >>. p .>> whiteSpace .>> eof
