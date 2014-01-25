namespace  AsyncTask

open System
open System.IO
open System.Net

module Task =
    let getData(uri:string) =
        Async.StartAsTask <|
        async { let request = WebRequest.Create uri
                use! response = request.AsyncGetResponse()
                return [    use stream = response.GetResponseStream()
                            use reader = new StreamReader(stream)
                            while not reader.EndOfStream
                                do yield reader.ReadLine() ] }

