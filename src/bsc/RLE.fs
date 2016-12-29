// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

/// <summary>Run-length Encoding functions.</summary>
module RLE = 
    /// <summary>
    /// Encodes a sequence of objects using Run-length Encoding.
    /// </summary>
    /// <param name="l">The sequence of objects.</param>
    /// <returns>
    /// A tuple of an integer and an object.
    /// For example, <c>encode [7;7;7]</c>, will return <c>(3, 7)</c>.
    /// </returns>
    let encode l = 
        let rec pack l b = 
            if Seq.isEmpty l then b
            else 
                let head = Seq.head l
                
                let count = 
                    l
                    |> Seq.takeWhile (fun t -> t = head)
                    |> Seq.length
                
                let newb = Seq.append b [ (count, head) ]
                pack (Seq.skip count l) newb
        pack l [] |> Array.ofSeq
    
    /// <summary>
    /// Decodes a pair of integers and objects returned by <c>encode</c>.
    /// </summary>
    let decode l = 
        l
        |> Seq.filter (fun i -> fst i > 0)
        |> Seq.collect (fun (count, item) -> Seq.replicate count item)
        |> Array.ofSeq
