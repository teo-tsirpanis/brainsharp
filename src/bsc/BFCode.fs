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
    open System
    open System.IO

    let parse (code: string) =
        let code = code.ToCharArray() |> List.ofArray
        let tokenIndex =      ['+', MemoryControl 1uy
                               '-', MemoryControl 255uy
                               '<', PointerControl -1
                               '>', PointerControl 1
                               '.', IOWrite
                               ',', IORead] |> Map.ofList
        let rec parseImpl body depth = ok >> bind (function
            | x :: xs ->
                match x with
                    | x when tokenIndex.ContainsKey x -> parseImpl (tokenIndex.[x] :: body) depth xs
                    | '[' -> let loopBody = parseImpl [] (depth + 1u) xs |> lift Loop
                             let xs = xs |> List.skipWhile ((<>) ']') |> (function | [] -> [] | x -> x |> List.skip 1)
                             loopBody >>= (fun loopBody -> parseImpl [] depth xs >>= (fun rest -> loopBody :: rest |> ok))
                    | ']' -> match depth with
                                | 0u -> UnmatchedBracket |> CompilationError |> fail
                                | _ -> body |> List.rev |> ok
                    | _ -> parseImpl body depth xs
            | [] ->
                match depth with
                        | 0u -> body |> List.rev |> ok
                        | _ -> UnmatchedBracket |> CompilationError |> fail)
        parseImpl [] 0u code

    let parseFile source = source |> File.ReadAllText |> parse