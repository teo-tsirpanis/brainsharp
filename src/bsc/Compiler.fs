// Copyright (c) 2017 Theodore Tsirpanis
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT
namespace Brainsharp

open Chessie.ErrorHandling
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open System
open System.Diagnostics
open System.Reflection

module Compiler =
    let compileToFile name (destination : string) memSize doProfile program =
        trial {
            let parseOptions =  [(if doProfile then Some "PROFILE" else None)]
                                |> Seq.choose id
                                |> CSharpParseOptions.Default.WithPreprocessorSymbols
            let source = CodeEmitter.emitProgram memSize program
            let trees =
                CSharpSyntaxTree.ParseText (source, parseOptions)
                |> Seq.singleton

            let mutable options =
                CSharpCompilationOptions OutputKind.ConsoleApplication
            options <- options.WithOptimizationLevel OptimizationLevel.Release
            options <- options.WithPlatform Platform.AnyCpu
            let references =
                [ typeof<Console>
                  typeof<Func<_>>
                  typeof<Action<_>> 
                  typeof<Stopwatch>]
                |> Seq.map
                       (fun t ->
                       t.GetTypeInfo().Assembly.Location |> MetadataReference.CreateFromFile :> MetadataReference)
                |> Seq.distinct
            let compilation =
                CSharpCompilation.Create(name, trees, references, options)
            let result = destination |> compilation.Emit

            let diagnostics =
                result.Diagnostics
                |> Seq.filter (fun d -> d.Severity <> DiagnosticSeverity.Hidden)
                |> Seq.map (fun d ->
                       let message = d.ToString()
                       match d.Severity with
                       | DiagnosticSeverity.Error ->
                           message
                           |> RoslynError
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
