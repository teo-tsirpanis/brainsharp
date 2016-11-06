namespace Brainsharp

/// <summary>Run-length Encoding functions.<summary/>
module RLE = 
    /// <summary>
    /// Encodes a list of objects using Run-length Encoding.
    /// </summary>
    /// <param name="l">The list of objects.</param>
    /// <returns>
    /// A tuple of an integer and an object.
    /// For example, <c>encode [7;7;7]<c/>, will return <c>(3, 7)<c/>.
    /// <returns/>
    let encode l = 
        let rec pack (l, b : (int * 'a) list) = 
            match l with
            | [] -> b
            | _ -> 
                let head = List.head l
                
                let count = 
                    l
                    |> List.takeWhile (fun t -> t = head)
                    |> List.length
                
                let newb = b @ [ (count, head) ]
                pack (l |> List.skip count, newb)
        pack (l, [])

    /// <summary>
    /// Decodes a pair of integers and objects returned by <c>encode<c/>.
    /// </summary>
    /// <param name="l"></param>
    let decode l = l |> List.collect (fun (count, item) -> List.replicate count item)
