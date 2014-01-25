#load "..\Utilities\PSeq.fs"

open System.IO
open System
open System.Threading
open System.Threading.Tasks
open Microsoft.FSharp.Collections

let isPrime(n) =
    let top = int(sqrt(float(n)))
    let rec isPrimeUtil(i) =
        if i > top then true
        elif n % i = 0 then false
        else isPrimeUtil(i + 1)
    (n > 1) && isPrimeUtil(2)

// #time
[1000..10000000] 
    |> PSeq.filter isPrime 
    |> PSeq.length


[1000..10000000] |> List.filter isPrime |> List.length


let pfor nfrom nto f =
   Parallel.For(nfrom, nto + 1, Action<_>(f)) |> ignore
   

pfor 1000 10000000 (isPrime >> ignore)

[| 1000..10000000 |]
 |> Array.Parallel.map (isPrime)
 |> Array.toSeq 
 |> PSeq.filter(fun x -> x)
 |> PSeq.length

let arrPrimw, arrNotPrime = 
    [| 1000..10000000 |]
    |> Array.Parallel.partition(fun x -> isPrime(x))
arrPrimw |> Array.length
