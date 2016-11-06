namespace Brainsharp

open RLE

module Bsc = 
    [<EntryPoint>]
    let main argv = 
        printfn "Hello world!!! Let's make some tests... \n"
        let list = [ 7; 7; 7; 7; 4; 6; 9; 9 ]
        
        (*let encoded = encode list
        let decoded = decode encoded*)
        let print s = 
            printfn "%A" s
            s
        list
        |> print
        |> encode
        |> print
        |> decode
        |> print
        |> ignore
        System.Console.ReadLine() |> ignore
        0 // return an integer exit code
