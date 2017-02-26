// Copyright (c) 2017 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT
namespace Brainsharp

open Chessie.ErrorHandling
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp

module Compiler = 
    let compileToFile name (destination : string) memSize program = 
        trial { 
            let trees = 
                CodeEmitter.emitProgram memSize program
                |> CSharpSyntaxTree.ParseText
                |> Seq.singleton
            
            let mutable options = 
                CSharpCompilationOptions OutputKind.ConsoleApplication
            options <- options.WithOptimizationLevel OptimizationLevel.Release
            options <- options.WithPlatform Platform.AnyCpu
            let references = 
                typeof<System.Console>.Assembly.Location 
                |> MetadataReference.CreateFromFile :> MetadataReference 
                |> Seq.singleton
            let compilation = 
                CSharpCompilation.Create(name, trees, references, options)
            let result = destination |> compilation.Emit
            
            let diagnostics = 
                result.Diagnostics |> Seq.map (fun d -> 
                                          let message = d.GetMessage()
                                          match d.Severity with
                                          | DiagnosticSeverity.Error -> 
                                              message
                                              |> CompilationError
                                              |> fail
                                          | _ -> 
                                              message
                                              |> CompilationMessage
                                              |> warn
                                              <| ())
            for d in diagnostics do
                do! d
        }
