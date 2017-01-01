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
    
    let optimizeRLE program = 
        program
        |> RLE.groupAdjacentPairs getCodeTag (Some 0)
        |> Array.map (fun x -> 
               match Array.head x with
               | PointerControl _ -> 
                   x
                   |> Array.sumBy (function 
                          | PointerControl x -> x
                          | _ -> 0)
                   |> PointerControl
               | MemoryControl _ -> 
                   x
                   |> Array.map (function 
                          | MemoryControl x -> x
                          | _ -> 0uy)
                   |> Array.sumBy int
                   |> byte
                   |> MemoryControl
               | x -> x)
        |> List.ofArray
    
    let optimizeClearLoops program = 
        program |> List.map (function 
                       | Loop([ MemoryControl x ]) when x = 1uy || x = 255uy -> 
                           MemorySet 0uy
                       | x -> x)
    
    let optimize program = 
        program |> overKill (optimizeRLE >> optimizeClearLoops)
