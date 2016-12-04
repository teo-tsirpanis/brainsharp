// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

open BFParser
open FParsec

module Bsc = 
    [<EntryPoint>]
    let main argv = 
        printfn "Hello world!!! Let's make some tests... \n"
        let print s = 
            printfn "%A" s
            s
        
        let s = "++--<<><<<>>,..,[,.,....]"
        let p = run BrainfuckParser s
        match p with
        | Success(result, _, _) -> printfn "Success %A" result
        | Failure(msg, _, _) -> printfn "Failure %s" msg
        System.Console.ReadLine() |> ignore
        0 // return an integer exit code
