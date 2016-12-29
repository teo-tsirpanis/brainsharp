// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

module Interpreter = 
    open Chessie.ErrorHandling
    open System
    open System.IO
    open System.Text
    
    let interpret memSize (readProc : unit -> char) writeProc program = 
        let memory = Array.replicate (memSize) 0uy
        let mutable pointer = 0
        let readMem() = 
            // (pointer, memory.[pointer]) ||> eprintfn "Memory at %i is %i."
            memory.[pointer]
        let writeMem ofs = 
            // (pointer, memory.[pointer], ofs) |||> eprintfn "Changing memory at %i, from %i by %i."
            memory.[pointer] <- memory.[pointer] + ofs
        
        let setPointer ofs = 
            // (pointer, ofs) ||> eprintfn "Setting pointer from %i, by %i"
            pointer <- match (pointer + ofs) % memSize with
                       | x when x < 0 -> memSize - x
                       | x -> x
        
        let rec interpretImpl = 
            function 
            | MemoryControl x -> writeMem x
            | PointerControl x -> setPointer x
            | IOWrite -> 
                (*(memory.[pointer], memory.[pointer] |> char, pointer) |||> eprintfn "Writing value %i (%c) at %i";*) writeProc (readMem() |> char)
            | IORead -> writeMem (readProc() |> byte)
            (*; (memory.[pointer], memory.[pointer] |> char, pointer) |||> eprintfn "Value %i (%c) was written at %i";*)
            | Loop x -> 
                // eprintfn "Entering loop..."
                while readMem() <> 0uy do
                    x |> List.iter interpretImpl
        
        program |> List.iter interpretImpl
    
    let interpretDelegate memSize (readProc : Func<char>) 
        (writeProc : Action<char>) program = 
        let readProc() = readProc.Invoke()
        let writeProc c = writeProc.Invoke c
        interpret memSize readProc writeProc program
    
    exception private EofException of unit
    
    let interpretEx memSize (reader : TextReader) (writer : TextWriter) program = 
        try 
            let readProc() = 
                match reader.Read() with
                | -1 -> raise (EofException())
                | x -> x |> char
            
            let writeProc (c : char) = writer.Write c
            interpret memSize readProc writeProc program
            ok()
        with EofException _ -> fail UnexpectedEndOfInput
    
    let interpretString memSize input program = 
        trial { 
            let reader = new StringReader(input)
            let writer = new StringWriter()
            do! interpretEx memSize reader writer program
            return writer.ToString()
        }
