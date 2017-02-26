// Copyright (c) 2017 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT
namespace Brainsharp

open BFCode
open System

module CodeEmitter = 
    let emitMemoryControl = sprintf "mem[p] += %d;"
    let emitMemorySet = sprintf "mem[p] = %u;"
    let emitPointerControl = sprintf "SetPointer (%d);"
    let emitIOWrite = "sb.Append((char) mem[p]);"
    let emitIORead = "c = readProc(); if (c != -1) {mem[x] = c;}"
    
    let emitLoop loopAction x = 
        sprintf "while (mem[p] != 0) {\n%s}" (x
                                              |> List.map loopAction
                                              |> String.concat "")
    
    let rec getEmitter indentLevel x = 
        let getEmitterImpl = 
            function 
            | MemoryControl x -> emitMemoryControl x
            | MemorySet x -> emitMemorySet x
            | PointerControl x -> emitPointerControl x
            | IOWrite -> emitIOWrite
            | IORead -> emitIORead
            | Loop x -> 
                let indentLevel' = indentLevel + 1
                emitLoop (getEmitter indentLevel') x
        
        let tabs = indentLevel * 4
                   |> String.replicate
                   <| " "
        sprintf "%s%s\n" tabs (getEmitterImpl x)
    
    let emitPayload program = 
        let theProgram = 
            program
            |> List.map (fun x -> (String.replicate 4 " ") + (getEmitter 1 x))
            |> String.concat ""
        
        let theTemplate = 
            System.AssemblyVersionInformation.AssemblyMetadata_MethodTemplate
        String.replace "@ThePayload" theProgram <| theTemplate
    
    let emitProgram memSize program = 
        let thePayload = emitPayload program
        let theTemplate = 
            System.AssemblyVersionInformation.AssemblyMetadata_CompiledProgram
        [ "@MemorySize", sprintf "%u" memSize
          "@TheMethod", thePayload ]
        |> String.replaceMany true
        <| theTemplate
