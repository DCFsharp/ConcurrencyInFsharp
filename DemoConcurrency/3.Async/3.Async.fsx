(*  Asynchronous programming is when you begin an operation on a background 
    thread to execute concurrently, and have it terminate at some later time; 
    either ignoring the new task to complete in the background, or polling it 
    at a later time to check on its return value    *)
#r "FSharp.PowerPack.dll"
open System
open System.IO
open System.Threading
open System.Net

module Version1 =  // No asynchrony yet, operations started explicitly

    let prog = 
        async {     printfn "Starting ..." 
                    return 1 }

    let result = prog |> Async.RunSynchronously


module Version2 =  // let!, non-blocking in F# Interactive, StartAsTask gets a .NET4.0 Task object

    let prog = async {  printfn "Starting ..." 
                        do! Async.Sleep(5000)
                        printfn "Finishing ..." 
                        return 1 }

    let result = prog |> Async.StartAsTask

    // Can evaluate in interactive while async is running
    1+1

    let  sendToTPL task = Async.StartAsTask <| async { return task }

module Version3 =  // asycns are first-class, parallelism by combining asyncs

    let prog(i) = async {   printfn "Starting %d ..." i
                            do! Async.Sleep((i%5) * 1000)
                            printfn "Finishing %d ..." i
                            return 1 }

    let result = 
        [1..10]
        |> Seq.map (fun i -> prog(i))
        |> Async.Parallel
        |> Async.StartAsTask

module Version4 =  // asyncs are very cheap, 100000 running concurrently, only a couple of threads used

    let prog(i) = async {   printfn "Starting %d ..." i
                            do! Async.Sleep((i%5) * 1000)
                            printfn "Finishing %d ..." i
                            return 1 }

    let result = 
        [1..100000]
        |> Seq.map (fun i -> prog(i))
        |> Async.Parallel
        |> Async.StartAsTask


module Vesrion5 =  

    let urls = [ "http://www.meetup.com/DC-fsharp"; "http://www.meetup.com/DC-fsharp"; "http://fsharp.org"]
    
    urls 
    |> List.map (fun url -> async {
            let client = new System.Net.WebClient()
            let! html = client.AsyncDownloadString(new System.Uri(url))
            printfn "Site len %d" html.Length })
    |> Async.Parallel
    |> Async.RunSynchronously
    


////////////// Async AwaitIAsyncResult
let streamWriter1 = File.CreateText("test1.txt")
let count = 10000000
let buffer = Array.init count (fun index -> byte (index % 256)) 

printfn "Writing to file test1.txt." 
let asyncResult = streamWriter1.BaseStream.BeginWrite(buffer, 0, count, null, null)

// Read a file, but use AwaitIAsyncResult to wait for the write operation 
// to be completed before reading. 
let readFile filename asyncResult count = 
    async { let! returnValue = Async.AwaitIAsyncResult(asyncResult)
            printfn "Reading from file test1.txt." 
            // Close the file.
            streamWriter1.Close()
            // Now open the same file for reading. 
            let streamReader1 = File.OpenText(filename)
            let! newBuffer = streamReader1.BaseStream.AsyncRead(count)
            return newBuffer }

let bufferResult = readFile "test1.txt" asyncResult count
                   |> Async.RunSynchronously

////////// Composition

let parallel2 (job1, job2) = async {
    let! task1 = Async.StartChild job1
    let! task2 = Async.StartChild job2
    let! result1 = task1
    let! result2 = task2
    return (result1, result2) }

let task1 = async { do! Async.Sleep 2000
                    return 1 }
let task2 = async { do! Async.Sleep 1000
                    return "Ciao" }

Async.StartWithContinuations( parallel2(task1, task2),
                                (fun res ->printfn "Operation completed ( %d ; %s)" (fst res) (snd res)),
                                (fun ex -> printfn "Operation failed."),
                                (fun can -> printfn "Operation canceled."))

//////////////////////////////////////////

let wc = new WebClient()

wc.DownloadProgressChanged.Add(
  fun args -> printfn "Percent complete %d" args.ProgressPercentage)

wc.DownloadStringCompleted
  |> Event.filter(fun args -> args.Error = null && not args.Cancelled)  
  |> Event.map(fun args -> args.Result)
  |> Event.add(printfn "%s")


let bufferData = Array.zeroCreate<byte> 100000000

let async1 (label:System.Windows.Forms.Label) filename =
     Async.StartWithContinuations(
         async {    label.Text <- "Operation started." 
                    use outputFile = System.IO.File.Create(filename)
                    do! outputFile.AsyncWrite(bufferData)   },
         (fun _ -> label.Text <- "Operation completed."),
         (fun _ -> label.Text <- "Operation failed."),
         (fun _ -> label.Text <- "Operation canceled."))

// ~~~~~~~~~~~~  Fetch FILE

let getFile filePath = async {
    use! stream = File.AsyncOpenRead filePath
    use reader = new StreamReader(stream)
    return! reader.AsyncReadToEnd() }

let files = Directory.GetFiles("..\Data", "*.csv")

files
|> Array.map getFile
|> Async.Parallel
|> Async.RunSynchronously
         
// ~~~~~~~~~~~~  Fetch HTML

let getHtml (url : string) = async {
        let req = WebRequest.Create(url)
        let! rsp = req.AsyncGetResponse()
        use stream = rsp.GetResponseStream()
        use reader = new StreamReader(stream)
        return! reader.AsyncReadToEnd() }
    
let html =getHtml "http://en.wikipedia.org/wiki/F_Sharp_programming_language"
          |> Async.RunSynchronously


let webPages : string[] =
    [ "http://www.bing.com"; "http://www.google.com"; "http://www.yahoo.com" ]
    |> List.map getHtml
    |> Async.Parallel
    |> Async.RunSynchronously


