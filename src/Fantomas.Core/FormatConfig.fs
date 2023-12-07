namespace Fantomas.Core

open System
open System.ComponentModel
open Fantomas.FCS.Parse

exception ParseException of diagnostics: FSharpParserDiagnostic list

type FormatException(msg: string) =
    inherit Exception(msg)
