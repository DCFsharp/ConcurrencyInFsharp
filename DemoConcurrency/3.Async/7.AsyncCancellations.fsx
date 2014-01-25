(*  When you execute code asynchronously, it is important to have a 
    cancellation mechanism just in case the user notices things 
    going awry or gets impatient *)

(*  Asynchronous workflows can be cancelled, but unlike Thread.Abort, 
    cancelling an async workflow is simply a request. The task does
    not immediately terminate, but rather the next time a let!, do!, 
    is executed, the rest of the computation will not be run and a 
    cancellation handler will be executed instead. *)

open System
open System.Threading

let cancelableTask =    async { printfn "Waiting 10 seconds..."
                                for i = 1 to 10 do
                                    printfn "%d..." i
                                    do! Async.Sleep(1000)
                                printfn "Finished!" }

// Callback used when the operation is canceled
let cancelHandler (ex : OperationCanceledException) =
    printfn "The task has been canceled."

Async.TryCancelled(cancelableTask, cancelHandler)
|> Async.Start

Async.CancelDefaultToken()

(*  If you want to be able to cancel an arbitrary asynchronous workflow, 
    then you’ll want to create and keep track of a CancellationTokenSource object. 
    A CancellationTokenSource is what signals the cancellation, 
    which in turn updates all of its associated CancellationTokens  *)

let computation = Async.TryCancelled(cancelableTask, cancelHandler)
let cancellationSource = new CancellationTokenSource()

Async.Start(computation, cancellationSource.Token)

cancellationSource.Cancel()


let timer callBack = async {    use! cancel = callBack
                                while true do
                                printfn "Computing..."
                                do! Async.Sleep 100 }

let cancelToken = new System.Threading.CancellationTokenSource()
let cancelCallback = Async.OnCancel(fun _ -> printfn "Cancel!!")

Async.Start(timer cancelCallback, cancelToken.Token)
System.Threading.Thread.Sleep (2 * 1000)
cancelToken.Cancel()



let startDisposable(op:Async<unit>) =
    let ct = new System.Threading.CancellationTokenSource()
    Async.Start(op, ct.Token)
    { new IDisposable with 
          member x.Dispose() = ct.Cancel() }

let dispose = startDisposable (async {    do! Async.Sleep 2000    })
dispose.Dispose()