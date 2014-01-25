#r "FSharp.PowerPack.dll"
open System
open System.IO
open System.Threading
open System.Net

type Agent<'T> = MailboxProcessor<'T>

type internal ThrottlingAgentMessage = 
  | Completed
  | Work of Async<unit>
    
/// Represents an agent that runs operations in concurrently. When the number
/// of concurrent operations exceeds 'limit', they are queued and processed later
type ThrottlingAgent(limit) = 
  let agent = MailboxProcessor.Start(fun agent -> 
    let rec waiting () = 
      agent.Scan(function
        | Completed -> Some(working (limit - 1))
        | _ -> None)
    and working count = async { 
      let! msg = agent.Receive()
      match msg with 
      | Completed -> return! working (count - 1)
      | Work work ->  async { try do! work 
                              finally agent.Post(Completed) }
                      |> Async.Start
                      if count < limit then return! working (count + 1)
                      else return! waiting () }
    working 0)      

  member x.DoWork(work) = agent.Post(Work work)


let agent= ThrottlingAgent(2)

let httpAsync(url:string) = 
    async { let req = WebRequest.Create(url)                 
            let! resp = req.AsyncGetResponse()
            use stream = resp.GetResponseStream() 
            use reader = new StreamReader(stream) 
            let! http = reader.AsyncReadToEnd()
            printfn "Thread id %d - htto len %d" Thread.CurrentThread.ManagedThreadId http.Length }

let urls = 
    [ "http://www.live.com"; 
        "http://news.live.com"; 
        "http://www.yahoo.com"; 
        "http://news.yahoo.com"; 
        "http://www.google.com"; 
        "http://news.google.com"; ] 

[ for url in urls -> httpAsync url ]
|> List.iter(fun h -> agent.DoWork(h))