﻿open System
open System.IO
open System.Threading
open System.Collections.Generic

type Agent<'T> = MailboxProcessor<'T>

(*  Blocking queue with limited size
    Block reader when queue is empty
    Block adder when queue is full
    
    Uses of blocking queue agent
    The producer consumer pattern
    Immediate buffer in pipeline processing *)

//  new BlockingQueueAgent : int -> BlockingQueueAgent
//  member AsyncGet        : unit -> Async<'T>
//  member AsyncAdd        : 'T -> Async<unit>


type BlockingAgentMessage<'T> = 
  | Add of 'T * AsyncReplyChannel<unit> 
  | Get of AsyncReplyChannel<'T>


/// Agent that implements an asynchronous blocking queue
type BlockingQueueAgent<'T>(maxLength) =
  // We keep the number of elements in the queue in a local field to
  // make it immediately available (so that 'Count' property doesn't 
  // have to use messages - which would be a bit slower)
  [<VolatileField>]
  let mutable count = 0

  let agent = Agent.Start(fun agent ->
    // List of items and list of pending 'Put' calls
    let queue = new Queue<_>()
    let pending = new Queue<_>()

    // If the queue is empty, we cannot handle 'Get' message
    let rec emptyQueue() = 
      agent.Scan(fun msg ->
        match msg with 
        | Add(value, reply) -> Some <| async {  queue.Enqueue(value)
                                                count <- queue.Count
                                                reply.Reply()
                                                return! nonEmptyQueue() }
        | _ -> None )

    // If the queue is non-empty, we handle all messages
    and nonEmptyQueue() = async {
      let! msg = agent.Receive()
      match msg with 
      | Add(value, reply) -> 
          // If the queue has space, we add item and notif caller back;
          // if it is full, we enqueue the request (and block caller)
          if queue.Count < maxLength then 
            queue.Enqueue(value)
            count <- queue.Count
            reply.Reply()
          else 
            pending.Enqueue(value, reply) 
          return! nonEmptyQueue()
      | Get(reply) -> 
          let item = queue.Dequeue()
          // We took item from the queue - check if there are any blocked callers
          // and values that were not added to the queue because it was full
          while queue.Count < maxLength && pending.Count > 0 do
            let itm, caller = pending.Dequeue()
            queue.Enqueue(itm)
            caller.Reply()
          count <- queue.Count
          reply.Reply(item)
          // If the queue is empty then switch the state, otherwise loop
          if queue.Count = 0 then return! emptyQueue()
          else return! nonEmptyQueue() }

    // Start with an empty queue
    emptyQueue() )


  /// Returns the number of items in the queue (immediately)
  /// (excluding items that are being added by callers that have been
  /// blocked because the queue was full)
  member x.Count = count

  /// Asynchronously adds item to the queue. The operation ends when
  /// there is a place for the item. If the queue is full, the operation
  /// will block until some items are removed.
  member x.AsyncAdd(v:'T, ?timeout) = 
    agent.PostAndAsyncReply((fun ch -> Add(v, ch)), ?timeout=timeout)

  /// Asynchronously gets item from the queue. If there are no items
  /// in the queue, the operation will block unitl items are added.
  member x.AsyncGet(?timeout) = 
    agent.PostAndAsyncReply(Get, ?timeout=timeout)
