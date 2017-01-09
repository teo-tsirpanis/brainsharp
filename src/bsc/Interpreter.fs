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
        let mutable instructionsRun = 0
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
            | IORead -> writeMem (readProc() |> byte)
            | Loop x -> 
                while readMem() <> 0uy do
                    x |> List.iter loopAction
        
        let rec interpretImpl x = 
            instructionsRun <- instructionsRun + 1
            interpretImpl' interpretImpl x
        
        program |> List.iter interpretImpl
        instructionsRun
    
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
            interpret memSize readProc writeProc program |> ok
        with EofException _ -> fail UnexpectedEndOfInput
    
    let interpretString memSize input program = 
        trial { 
            let reader = new StringReader(input)
            let writer = new StringWriter()
            let! ic = interpretEx memSize reader writer program
            return writer.ToString(), ic
        }
    
    let interpretExTee memSize reader (writer : TextWriter) program = 
        trial { 
            let mutable strOut = ""
            
            let newWriter = 
                { new TextWriter() with
                      member x.Close() = writer.Close()
                      member x.Encoding = Encoding.ASCII
                      member x.Write(y : char) = 
                          strOut <- strOut + string y
                          writer.Write(y) }
            let! ic = interpretEx memSize reader newWriter program
            return strOut, ic
        }
