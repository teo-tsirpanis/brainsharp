// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

module Interpreter = 
    open System
    
    let interpret (readProc : unit -> char) writeProc program = 
        let memory = Array.replicate (UInt16.MaxValue |> int) 0uy
        let mutable pointer = 0us
        let readMem() = memory.[pointer |> int]
        let writeMem ofs = 
            memory.[(int pointer)] <- memory.[(int pointer)] + ofs
        
        let rec interpretImpl s = 
            match s with
            | MemoryControl x -> writeMem x
            | PointerControl x -> pointer <- +x
            | IOWrite -> writeProc (readMem() |> char)
            | IORead -> writeMem (readProc() |> byte)
            | Loop x -> 
                (if readMem() <> 0uy then x |> List.iter interpretImpl)
        program |> List.iter interpretImpl
    
    let interpretDelegate (readProc : Func<char>) (writeProc : Action<char>) 
        program = 
        let readProc() = readProc.Invoke()
        let writeProc c = writeProc.Invoke c
        interpret readProc writeProc program
