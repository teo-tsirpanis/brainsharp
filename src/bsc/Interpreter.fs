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
            #if DEBUG
            (pointer, memory.[pointer]) ||> eprintfn "Memory at %i is %i."
            #endif
            memory.[pointer]

        let writeMem ofs = 
            #if DEBUG
            (pointer, memory.[pointer], ofs) |||> eprintfn "Changing memory at %i, from %i by %i."
            #endif
            memory.[pointer] <- memory.[pointer] + ofs

        let setMem x =
            #if DEBUG
            (pointer, x) ||> eprintfn "Setting memory at %i to %i."
            #endif
            memory.[pointer] <- x
        
        let setPointer ofs =
            #if DEBUG
            (pointer, ofs) ||> eprintfn "Setting pointer from %i, by %i"
            #endif
            pointer <- match (pointer + ofs) % memSize with
                       | x when x < 0 -> memSize - x
                       | x -> x
        
        let rec interpretImpl = 
            function 
            | MemoryControl x -> writeMem x
            | MemorySet x -> setMem x
            | PointerControl x -> setPointer x
            | IOWrite -> 
                #if DEBUG
                (memory.[pointer], memory.[pointer] |> char, pointer) |||> eprintfn "Writing value %i (%c) at %i"
                #endif
                writeProc (readMem() |> char)
            | IORead -> writeMem (readProc() |> byte)
                        #if DEBUG
                        (memory.[pointer], memory.[pointer] |> char, pointer) |||> eprintfn "Value %i (%c) was written at %i";
                        #endif
            | Loop x -> 
                #if DEBUG
                eprintfn "Entering loop..."
                #endif
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
