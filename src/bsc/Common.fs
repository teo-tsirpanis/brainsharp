// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

open FParsec
open System

type BFError = 
    | ProfilingResults of TimeSpan * uint64
    | FileExist of string
    | FileNotExist of string
    | ParseError of string * ParserError
    | ShowVersion
    | TestFailure of excpected : string * found : string
    override x.ToString() = 
        match x with 
        | ProfilingResults(time, instructionsRun) -> 
            sprintf 
                "Execution time (H:M:S:MS): %i:%i:%i:%i \nTotal instructions run: %i" 
                time.Hours time.Minutes time.Seconds time.Milliseconds 
                instructionsRun
        | FileExist x -> 
            sprintf "File %s already exists. It will be overwritten." x
        | FileNotExist x -> sprintf "File %s does not exist." x
        | ParseError(x, _) -> x
        | ShowVersion -> 
            AssemblyVersionInformation.AssemblyMetadata_Version_Message
        | TestFailure(expected, found) -> 
            if expected.Length + found.Length < 200 * 2 then 
                sprintf 
                    "Program output is expected to be\n\t%s\n but it was \n\t%s" 
                    expected found
            else 
                "Program output is different than the expected, but it is not shown, because of its size."

[<AutoOpen>]
module Common = 
    let (||||>) (a, b, c, d) f = f a b c d
    
    let someOr def = 
        function 
        | Some x -> x
        | None -> def
    
    /// <summary>
    /// Overkills <c>state</c>. Literally.
    /// It applies <c>f</c> to <c>state</c> until <c>f</c> cannot change it.
    /// </summary>
    let rec overKill f state = 
        let state' = f state
        if state = state' then state
        else overKill f state'
