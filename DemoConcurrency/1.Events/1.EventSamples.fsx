#load "..\Utilities\AsyncHelpers.fs"
#load "..\Utilities\show-wpf40.fsx"
open System
open System.Drawing
open System.Windows.Forms
open System.Threading
open System.IO
open System.Windows.Forms
open AsyncHelpers

let fnt = new Font("Calibri", 24.0f)
let lbl = new System.Windows.Forms.Label(Dock = DockStyle.Fill, 
                                         TextAlign = ContentAlignment.MiddleCenter, 
                                         Font = fnt)

let form = new System.Windows.Forms.Form(ClientSize = Size(200, 100), Visible = true)
do form.Controls.Add(lbl)

let regsiter(ev) =  
    ev   
    |> Event.map (fun _ -> DateTime.Now) // Create events carrying the current time
    |> Event.scan (fun (_, dt : DateTime) ndt -> // Remembers the last time click was accepted
           if ((ndt - dt).TotalSeconds > 2.0) then // When the time is more than a second...
               (4, ndt)
           else (1, dt)) (0, DateTime.Now) // .. we return 1 and the new current time
    |> Event.map fst
    |> Event.scan (+) 0 // Sum the yielded numbers 
    |> Event.map (sprintf "Clicks: %d") // Format the output as a string 
    |> Event.add lbl.set_Text // Display the result...    

regsiter(lbl.MouseDown)

//  asynchronous loop
let rec loop (count) = 
    async { 
        // Wait for the next click
        let! ev = Async.AwaitEvent(lbl.MouseDown)
        lbl.Text <- sprintf "Clicks: %d" count
        do! Async.Sleep(1000)        
        return! loop (count + 1)
    }
let start = Async.StartImmediate(loop (1))

////////////////////////////////////////////////////////////////////////////////////////////////


let process' = new System.Diagnostics.ProcessStartInfo("ping.exe", "-t -n 3 127.0.0.1")
process'.UseShellExecute <- false
process'.RedirectStandardOutput <- true

let mProcess = new System.Diagnostics.Process()
mProcess.StartInfo <- process'
mProcess.EnableRaisingEvents <- true

mProcess.OutputDataReceived 
    |> Event.map (fun p -> p.Data) 
    |> Event.add(fun s -> printfn "Data from Process: %s" s)

mProcess.Start();
mProcess.BeginOutputReadLine()


