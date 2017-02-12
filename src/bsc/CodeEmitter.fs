// Copyright (c) 2017 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT
namespace Brainsharp

open BFCode

module CodeEmitter = 
    let emitMemoryControl = sprintf "mem[p] += %d;"
    let emitMemorySet = sprintf "mem[p] = %u;"
    let emitPointerControl = sprintf "SetPointer (%d);"
    let emitIOWrite = "Console.Write((char) mem[p]);"
    let emitIORead = "c = Comsole.Read(); if (c != -1) {mem[x] = c;}"
    
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
    
    let emitProgram memSize program = 
        [ "using System;"
          "public static class Program"
          "{"
          sprintf "static byte[] mem = new byte[%u];" memSize
          "static int p = 0;"
          "static int c = 0;"
          
          sprintf 
              "public static void SetPointer (int ofs) {var temp = (p + ofs) %% %u; if (temp < 0) {p = %d - temp;} else {p = temp;}}" 
              memSize memSize
          "public static void Main()"
          "{"
          program
          |> List.map (getEmitter 1)
          |> String.concat ""
          "}"
          "}" ]
        |> String.concat "\n"
