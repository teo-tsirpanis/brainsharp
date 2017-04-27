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
    
    let interpret memSize (readProc : unit -> char option) writeProc program = 
        let memory = Array.replicate (memSize) 0uy
        let mutable pointer = 0
        let mutable instructionsRun = 0UL
        let readMem() = memory.[pointer]
        let writeMem ofs = memory.[pointer] <- memory.[pointer] + ofs
        let setMem x = memory.[pointer] <- x
        
        let setPointer ofs = 
            pointer <- match (pointer + ofs) % memSize with
                       | x when x < 0 -> memSize - x
                       | x -> x
        
        let rec interpretImpl' loopAction = 
            function 
            | MemoryControl x -> writeMem x
            | MemorySet x -> setMem x
            | PointerControl x -> setPointer x
            | IOWrite -> writeProc (readMem() |> char)
            | IORead -> 
                match readProc() with
                | Some x -> writeMem (x |> byte)
                | None -> ()
            | Loop x -> 
                while readMem() <> 0uy do
                    x |> List.iter loopAction
        
        let rec interpretImpl x = 
            instructionsRun <- instructionsRun + 1UL
            interpretImpl' interpretImpl x
        
        program |> List.iter interpretImpl
        instructionsRun
    
    exception private EofException of unit
    
    let interpretEx memSize (reader : TextReader) (writer : TextWriter) program = 
        let strOut = StringBuilder()
        let readProc() = 
            match reader.Read() with
            | -1 -> None
            | x -> 
                x
                |> char
                |> Some
        
        let writeProc (c: char) =
            writer.Write c
            strOut.Append c |> ignore
        
        strOut.ToString(), interpret memSize readProc writeProc program
    
    let interpretString memSize input program = 
        let reader = new StringReader(input)
        let writer = new StringWriter()
        let ic = interpretEx memSize reader writer program
        writer.ToString(), ic
    