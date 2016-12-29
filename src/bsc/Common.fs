// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

open FParsec
open System

type BFError = 
    | ExecutionTime of TimeSpan
    | FileExist of string
    | FileNotExist of string
    | InvalidArguments
    | ParseError of string * ParserError
    | TestFailure of excpected : string * found : string
    | UnexpectedEndOfInput

[<AutoOpen>]
module Common = 
    let (||||>) (a, b, c, d) f = f a b c d
    
    let someOr def = 
        function 
        | Some x -> x
        | None -> def
    
    /// <summary>
    /// Overkills <c>state</c>. Literally.
    /// It applies <c>f</c> to <c>state</c> until <c>f</c> cannot change it.
    /// </summary>
    let rec overKill f state = 
        let state' = f state
        if state = state' then state
        else overKill f state'
