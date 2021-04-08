module Fantomas.CoreGlobalTool.Daemon

open System
open System.Diagnostics
open System.IO
open LspTypes
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Serialization
open StreamJsonRpc
open System.Threading
open StreamJsonRpc

type Path with
    /// Stolen from FsAutoComplete
    /// handles unifying the local-path logic for windows and non-windows paths,
    /// without doing a check based on what the current system's OS is.
    static member FileUriToLocalPath(uriString: string) =
        /// a test that checks if the start of the line is a windows-style drive string, for example
        /// /d:, /c:, /z:, etc.
        let isWindowsStyleDriveLetterMatch (s: string) =
            match s.[0..2].ToCharArray() with
            | [||]
            | [| _ |]
            | [| _; _ |] -> false
            // 26 windows drive letters allowed, only
            | [| '/'; driveLetter; ':' |] when Char.IsLetter driveLetter -> true
            | _ -> false

        let initialLocalPath = Uri(uriString).LocalPath

        let fn =
            if isWindowsStyleDriveLetterMatch initialLocalPath then
                initialLocalPath.TrimStart('/')
            else
                initialLocalPath

        if uriString.StartsWith "untitled:" then
            (fn + ".fsx")
        else
            fn

let private countLines (text: string) : uint =
    if String.IsNullOrEmpty(text) then
        0u
    else
        let count =
            text.Length
            - text.Replace("\n", String.Empty).Length
            |> (uint)

        if text.[text.Length - 1] = '\n' then
            count + 1u
        else
            count

type FantomasLSPServer(sender: Stream, reader: Stream) as this =
    
    let rpc : JsonRpc = JsonRpc.Attach(sender, reader, this)
    do
        // hook up request/response logging for debugging
        rpc.TraceSource <- TraceSource(typeof<FantomasLSPServer>.Name, SourceLevels.Verbose)
        rpc.TraceSource.Listeners.Add(new SerilogTraceListener.SerilogTraceListener(typeof<FantomasLSPServer>.Name))
        |> ignore<int>
    
    let disconnectEvent = new ManualResetEvent(false)

    let exit () =
        disconnectEvent.Set() |> ignore
        Environment.Exit(0)

    do rpc.Disconnected.Add(fun _ -> exit ())

    interface IDisposable with
        member this.Dispose() =
            disconnectEvent.Dispose()

    /// returns a hot task that resolves when the stream has terminated
    member this.WaitForClose = rpc.Completion

    [<JsonRpcMethod(Methods.InitializeName)>]
    member this.Initialize () : obj =
        let capabilities = ServerCapabilities()
        capabilities.DocumentFormattingProvider <- SumType<_, DocumentFormattingOptions>(DocumentFormattingOptions())

        let initializeResult =
            InitializeResult(Capabilities = capabilities)

        let json =
            Newtonsoft.Json.JsonConvert.SerializeObject(initializeResult)

        json :> obj

    [<JsonRpcMethod(Methods.InitializedName)>]
    member this.Initialized(arg: JToken) : unit = ()

    [<JsonRpcMethod(Methods.TextDocumentFormattingName, UseSingleObjectParameterDeserialization = true)>]
    member this.TextDocumentFormatting(options: DocumentFormattingParams, ctx: CancellationToken) : TextEdit =
        let filePath =
            Path.FileUriToLocalPath options.TextDocument.Uri

        let fileContents = File.ReadAllText(filePath) // TODO, format
        let range = Range()
        range.Start <- (Position(0u, 0u))
        range.End <- (Position(countLines fileContents, 0u)) // TODO, correct column
        let response : TextEdit = TextEdit()
        response.Range <- range
        response.NewText <- fileContents
        response