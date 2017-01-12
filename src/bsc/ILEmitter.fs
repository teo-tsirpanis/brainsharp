// Copyright (c) 2017 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT
namespace Brainsharp

open BFCode
open Microsoft.FSharp.Reflection
open System
open System.Reflection.Emit

module ILEmitter = 
    let consoleWrite = 
        let console = typeof<Console>
        console.GetMethod("Write", [| typeof<char> |])
    
    let emitMemoryControl x (gen : ILGenerator) = ()
    let emitMemorySet x (gen : ILGenerator) = ()
    let emitPointerControl x (gen : ILGenerator) = ()
    let emitIOWrite (gen : ILGenerator) = ()
    let emitIORead (gen : ILGenerator) = ()
    let emitLoop x (gen : ILGenerator) = ()
    
    let getEmitter = 
        function 
        | MemoryControl x -> emitMemoryControl x
        | MemorySet x -> emitMemorySet x
        | PointerControl x -> emitPointerControl x
        | IOWrite -> emitIOWrite
        | IORead -> emitIORead
        | Loop x -> emitLoop x
    
    let emitProgram (memSize : int) (gen : ILGenerator) program = 
        let mem = gen.DeclareLocal(typeof<byte []>)
        gen.Emit(OpCodes.Ldc_I4, memSize |> abs)
        gen.Emit(OpCodes.Conv_I)
        gen.Emit(OpCodes.Newarr, typeof<byte>)
        gen.Emit(OpCodes.Stloc_0)
        program |> List.iter (fun x -> x
                                       |> getEmitter
                                       <| gen)
        gen.Emit(OpCodes.Ret)
