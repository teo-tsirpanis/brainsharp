// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

open BFCode
open Microsoft.FSharp.Reflection

module Optimizer = 
    let getCodeTag = 
        function 
        | MemoryControl _ -> 1
        | PointerControl _ -> 2
        | _ -> 0
    
    let rec optimizeRLE program = 
        program
        |> RLE.groupAdjacentPairs getCodeTag (Some 0)
        |> List.collect (fun x -> 
               match x with
               | PointerControl _ :: _ -> 
                   x
                   |> List.sumBy (function 
                          | PointerControl x -> x
                          | _ -> 0)
                   |> PointerControl
                   |> List.singleton
               | MemoryControl _ :: _ -> 
                   x
                   |> List.sumBy (function 
                          | MemoryControl x -> x |> int
                          | _ -> 0)
                   |> byte
                   |> MemoryControl
                   |> List.singleton
               | [ Loop x ] -> 
                   x
                   |> optimizeRLE
                   |> Loop
                   |> List.singleton
               | x -> x)
    
    let rec optimizeClearLoops program = 
        program |> List.map (function 
                       | Loop([ MemoryControl x ]) when x = 1uy || x = 255uy -> 
                           MemorySet 0uy
                       | Loop x -> 
                           x
                           |> optimizeClearLoops
                           |> Loop
                       | x -> x)
    
    let optimize program = 
        program |> overKill (optimizeRLE >> optimizeClearLoops)
