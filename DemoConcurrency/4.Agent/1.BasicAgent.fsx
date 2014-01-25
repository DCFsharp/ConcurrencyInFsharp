open System
open System.IO
open System.Threading
open System.Collections.Generic

type Agent<'T> = MailboxProcessor<'T>

let myEvent = new Event<int>()

let ctx = System.Threading.SynchronizationContext.Current
let cancellationToken = new System.Threading.CancellationTokenSource()

 
let oneAgent =
       Agent.Start(fun inbox ->
         async { while true do
                   let! msg = inbox.Receive()
                   printfn "got message '%s'" msg } )
 
oneAgent.Post "hi"


// 100k agents
let alloftheagents =
        [ for i in 0 .. 100000 ->
           Agent.Start(fun inbox ->
             async { while true do
                       let! msg = inbox.Receive()
                       if i % 10000 = 0 then
                           printfn "agent %d got message '%s'" i msg })]
 
for agent in alloftheagents do
    agent.Post "ping!"

// error handling
let errorAgent =
       Agent<int * System.Exception>.Start(fun inbox ->
         async { while true do
                   let! (agentId, err) = inbox.Receive()
                   printfn "an error '%s' occurred in agent %d" err.Message agentId })
 
let agents10000 =
       [ for agentId in 0 .. 10000 ->
            let agent =
                new Agent<string>(fun inbox ->
                   async { while true do
                             let! msg = inbox.Receive()
                             if msg.Contains("agent 99") then
                                 failwith "fail!" })
            agent.Error.Add(fun error -> errorAgent.Post (agentId,error))
            agent.Start()
            (agentId, agent) ]
 
for (agentId, agent) in agents10000 do
    agent.Post (sprintf "message to agent %d" agentId )



let agents = [1..100 * 1000]
             |> List.map( fun i ->
                    use a = new Agent<_>((fun n ->  async {
                        while true do
                            let! msg = n.Receive()
                            if i % 20000 = 0 then 
                                printfn "agent %d got message %s" i msg 
                                ctx.Post((fun _ -> myEvent.Trigger i), null) 
                            if i % 40000 = 0 then 
                                raise <| new System.Exception("My Error!") }), cancellationToken.Token )
                    a.Error.Add(fun _ -> printfn "Something wrong with agent %d" i)
                    a.Start()  
                    (a, { new System.IDisposable with
                            member x.Dispose() =
                                printfn "Disposing agent %d" i
                                cancellationToken.Cancel() }) )  
                            

for (agent,idisposable) in agents do
    agent.Post "ciao"

for (agent,idisposable) in agents do    
    idisposable.Dispose()
    (agent :> System.IDisposable).Dispose()
                        
//~~~~~~~~~~~~~~~~~~ Agent Get Reply

type Message = string * AsyncReplyChannel<string>

let postAndReply = 
        let formatString = "Received message: {0}" 

        let agent = Agent<Message>.Start(fun inbox ->
            let rec loop () =
                async {
                        let! (message, replyChannel) = inbox.Receive();
                        replyChannel.Reply(String.Format(formatString, message))
                        do! loop ()
                }
            loop ())

        printfn "Mailbox Processor Test"
        printfn "Type some text and press Enter to submit a message." 

        while true do
            printf "> " 
            let input = Console.ReadLine()

            //PostAndReply blocks
            let messageAsync = agent.PostAndAsyncReply(fun replyChannel -> input, replyChannel)

            Async.StartWithContinuations(messageAsync, 
                 (fun reply -> printfn "Reply received: %s" reply), //continuation
                 (fun _ -> ()), //exception
                 (fun _ -> ())) //cancellation



//~~~~~~~~~~~~~~~~~~ Agent LockFree

type Fetch<'T> = AsyncReplyChannel<'T>

type Msg<'key,'value> = 
    | Push of 'key * 'value
    | Pull of 'key * Fetch<'value>

module LockFree = 
    let (lockfree:Agent<Msg<string,string>>) = Agent.Start(fun sendingInbox -> 
        let cache = System.Collections.Generic.Dictionary<string, string>()
        let rec loop () = async {
            let! message = sendingInbox.Receive()
            match message with 
                | Push (key,value) -> cache.[key] <- value
                | Pull (key,fetch) -> fetch.Reply cache.[key]
            return! loop ()
            }
        loop ())

//~~~~~~~~~~~~~~~~~~ Agent Counter

let counter =new Agent<_>(fun inbox ->
                let rec loop n =
                    async {printfn "n = %d, waiting..." n
                           let! msg = inbox.Receive()
                           return! loop (n + msg)}
                loop 0)

counter.Start();;
//n = 0, waiting...
counter.Post(1);;
//n = 1, waiting...
counter.Post(2);;
//n = 3, waiting...
counter.Post(1);;
//n = 4, waiting...


type internal Message = 
        | Increment of int 
        | Fetch of AsyncReplyChannel<int> 
        | Stop
        | Pause
        | Resume

let cancel = new System.Threading.CancellationTokenSource()

type CountingAgent() =
    let counter = MailboxProcessor.Start((fun inbox ->
         let rec blocked(n) =           
            printfn "Blocking"
            inbox.Scan(fun msg ->
            match msg with
            | Resume -> Some(async {
                printfn "Resuming"
                return! processing(n) })
            | _ -> None)
         and processing(n) = async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Increment m -> return! processing(n + m)
                    | Stop -> return ()
                    | Resume -> return! processing(n)
                    | Pause -> return! blocked(n)
                    | Fetch replyChannel  ->    do replyChannel.Reply n
                                                return! processing(n) }
         processing(0)), cancel.Token)

    member a.Increment(n) = counter.Post(Increment n)
    member a.Stop() = counter.Post Stop
    member a.Fetch() = counter.PostAndReply(fun replyChannel -> Fetch replyChannel)


let counterInc = new CountingAgent();;
counterInc.Increment(1);;
counterInc.Fetch();;
counterInc.Increment(2);;
counterInc.Fetch();;
counterInc.Stop();;
