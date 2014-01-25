(*  Observable.add 
        simply subscribes to the event. Typically this method 
        is used at the end of a series of pipe-forward operations 
        and is a cleaner way to subscribe to events than calling 
        AddHandler on the event object
        
    Observable.filter 
        function creates an event that’s triggered when the source 
        event produces a value that matches the given predicate

    Observable.merge
        Observable.merge takes two input events and produces a 
        single output event, which will be fired whenever either 
        of its input events is raised.

    Observable.map 
        allows you to convert an event with a given argument 
        type into another.      *)

open System
open System.Drawing
open System.Windows.Forms
open System.Threading
open System.IO
open System.Windows.Forms

let fillellipseform = new Form(Text="Draw with Obseravbles", Visible=true, TopMost=true)  
fillellipseform.BackColor<-Color.Gray

let exitbutton=new Button(Top=0,Left=0)
exitbutton.Text<-"Unsubscribe"  
exitbutton.BackColor<-Color.Ivory
fillellipseform.Controls.Add(exitbutton)  

let crgraphics=fillellipseform.CreateGraphics()  

let (observableEvent1, observableEvent2) =   
    fillellipseform.MouseDown  
    |> Observable.merge fillellipseform.MouseMove
    |> Observable.filter(fun ev -> ev.Button = MouseButtons.Left)
    |> Observable.partition(fun ev ->ev.X > (fillellipseform.Width /2))

let obsEvent1Disposable = observableEvent1 |> Observable.subscribe(fun move->crgraphics.FillEllipse(Brushes.Red,new Rectangle(move.X,move.Y,5,5)))                                                                                                                 
let obsEvent2Disposable = observableEvent2 |> Observable.subscribe(fun move->crgraphics.FillEllipse(Brushes.Green,new Rectangle(move.X,move.Y,5,5)))                                                                                                                 

exitbutton.Click.Add(fun close-> obsEvent1Disposable.Dispose()
                                 obsEvent2Disposable.Dispose())

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


[<Measure>]
type minute

[<Measure>]
type bpm = 1/minute

type MusicGenre = Classical | Pop | HipHop | Rock | Latin | Country

type Song = { Title : string; Genre : MusicGenre; BPM : int<bpm> }

type SongChangeArgs(title : string, genre : MusicGenre, bpm : int<bpm>) =
    inherit System.EventArgs()

    member this.Title = title
    member this.Genre = genre
    member this.BeatsPerMinute = bpm

type SongChangeDelegate = delegate of obj * SongChangeArgs -> unit



type JukeBox() =
    let m_songStartedEvent = new Event<SongChangeDelegate, SongChangeArgs>()

    member this.PlaySong(song) =
        m_songStartedEvent.Trigger(this,
                new SongChangeArgs(song.Title, song.Genre, song.BPM))

    [<CLIEvent>]
    member this.SongStartedEvent = m_songStartedEvent.Publish


let jb = JukeBox()
let fastSongEvent, slowSongEvent =
    jb.SongStartedEvent
    // Filter event to just dance music
    |> Observable.filter(fun songArgs ->
            match songArgs.Genre with
            | Pop | HipHop | Latin | Country -> true
            | _ -> false)
    // Split the event into 'fast song' and 'slow song'
    |> Observable.partition(fun songChangeArgs ->
            songChangeArgs.BeatsPerMinute >= 120<bpm>);;

slowSongEvent.Add(fun args -> printfn"You hear '%s' and start to dance slowly..."
                                  args.Title)

fastSongEvent.Add(fun args -> printfn "You hear '%s' and start to dance fast!" args.Title);;

jb.PlaySong( { Title = "Burnin Love"; Genre = Pop; BPM = 120<bpm> } );;

jb.PlaySong( { Title = "Country Song"; Genre = Country; BPM = 58<bpm> } );;
