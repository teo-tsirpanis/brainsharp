// Copyright (c) 2016 Theodore Tsirpanis
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

/// <summary>Run-length Encoding functions.</summary>
module RLE = 
    
    let groupAdjacentPairs f fnone l = 
        let rec pack l b = 
            let optEqual a b = 
                match a with
                | Some x -> x = b
                | None -> false
            if Seq.isEmpty l then b
            else 
                let head = Seq.head l
                let fhead = head |> f
                
                let newElement = 
                    if optEqual fnone fhead then [| head |]
                    else 
                        l
                        |> Seq.takeWhile (f >> ((=) fhead))
                        |> Array.ofSeq
                
                let newb = b @ [ newElement ]
                pack (Seq.skip newElement.Length l) newb
        pack l [] |> Array.ofSeq
    
    /// <summary>
    /// Encodes a sequence of objects using Run-length Encoding.
    /// </summary>
    /// <param name="l">The sequence of objects.</param>
    /// <returns>
    /// A tuple of an integer and an object.
    /// For example, <c>encode [7;7;7]</c>, will return <c>(3, 7)</c>.
    /// </returns>
    let encode l = 
        groupAdjacentPairs id None l |> Array.map (fun x -> x.Length, x.[0])
    
    /// <summary>
    /// Decodes a pair of integers and objects returned by <c>encode</c>.
    /// </summary>
    let decode l = 
        l
        |> Seq.filter (fun i -> fst i > 0)
        |> Seq.collect (fun (count, item) -> Seq.replicate count item)
        |> Array.ofSeq
