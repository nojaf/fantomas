module Fantomas.CoreGlobalTool.Daemon

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open FSharp.Compiler.SourceCodeServices
open Fantomas
open Fantomas.SourceOrigin
open LspTypes
open StreamJsonRpc
open System.Threading

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

type FormatSourceOptions =
    { SourceCode: string
      Config: Dictionary<string, string>
      TextDocument: TextDocumentIdentifier }

type FormatSourceRange =
    class
    end

type FantomasLSPServer(sender: Stream, reader: Stream) as this =
    let rpc : JsonRpc = JsonRpc.Attach(sender, reader, this)

    do
        // hook up request/response logging for debugging
        rpc.TraceSource <- TraceSource(typeof<FantomasLSPServer>.Name, SourceLevels.Verbose)

        rpc.TraceSource.Listeners.Add(new SerilogTraceListener.SerilogTraceListener(typeof<FantomasLSPServer>.Name))
        |> ignore<int>

    let disconnectEvent = new ManualResetEvent(false)

    let exit () = disconnectEvent.Set() |> ignore

    do rpc.Disconnected.Add(fun _ -> exit ())

    interface IDisposable with
        member this.Dispose() = disconnectEvent.Dispose()

    /// returns a hot task that resolves when the stream has terminated
    member this.WaitForClose = rpc.Completion

    [<JsonRpcMethod(Methods.InitializeName)>]
    member this.Initialize() : InitializeResult =
        let capabilities = ServerCapabilities()
        capabilities.DocumentFormattingProvider <- SumType<_, DocumentFormattingOptions>(DocumentFormattingOptions())
        InitializeResult(Capabilities = capabilities)

    [<JsonRpcMethod(Methods.InitializedName)>]
    member this.Initialized() : unit = ()

    /// Fantomas uses the LSP protocol but does initially not aim to function as fully fledged LSP server.
    /// Custom RPC methods are introduced to take no dependency on the file system and not required the constant communication of file events.
    [<JsonRpcMethod("fantomas/formatSource", UseSingleObjectParameterDeserialization = true)>]
    member this.FormatSource(options: FormatSourceOptions) : TextEdit = // TODO: later Task
        let filePath =
            Path.FileUriToLocalPath options.TextDocument.Uri

        let response : TextEdit = TextEdit()
        let range = Range()
        range.Start <- (Position(0u, 0u))
        range.End <- (Position(countLines options.SourceCode, 0u))
        response.Range <- range

        let parsingOptions =
            { FSharpParsingOptions.Default with
                  SourceFiles = [| filePath |] }

        let checker =
            Fantomas.Extras.FakeHelpers.sharedChecker.Value

        response.NewText <-
            CodeFormatter.FormatDocumentAsync(
                filePath,
                SourceString options.SourceCode,
                FormatConfig.FormatConfig.Default,
                parsingOptions,
                checker
            )
            |> Async.RunSynchronously

        response

    [<JsonRpcMethod("fantomas/formatSourceRange")>]
    member this.FormatSourceRange(options: FormatSourceOptions) : TextEdit = failwith "not yet implemented"
