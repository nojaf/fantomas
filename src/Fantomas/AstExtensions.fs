module Fantomas.AstExtensions

open FSharp.Compiler.Syntax
open FSharp.Compiler.Text
open FSharp.Compiler.Text.Range

type SynTypeDefnSig with
    /// Combines the range of type name and the body.
    member this.FullRange : Range =
        match this with
        | SynTypeDefnSig.SynTypeDefnSig (comp, _, _, r) -> mkRange r.FileName comp.Range.Start r.End

type SynModuleDecl with
    member decl.FullRange : Range =
        match decl with
        | SynModuleDecl.Let(bindings = SynBinding(attributes = ha :: _) :: _)
        | SynModuleDecl.Types (typeDefns = (SynTypeDefn(typeInfo = SynComponentInfo(attributes = ha :: _)) :: _)) ->
            mkRange decl.Range.FileName ha.Range.Start decl.Range.End
        | _ -> decl.Range

type SynSimplePats with
    member pat.Range : Range =
        match pat with
        | SynSimplePats.SimplePats (_, r)
        | SynSimplePats.Typed (_, _, r) -> r

type SynModuleSigDecl with
    member decl.FullRange : Range =
        match decl with
        | SynModuleSigDecl.Types (types = SynTypeDefnSig(typeInfo = SynComponentInfo(attributes = ha :: _)) :: _ as allTypes) ->
            match List.tryLast allTypes with
            | Some (SynTypeDefnSig (range = r)) -> mkRange decl.Range.FileName ha.Range.Start r.End
            | None -> mkRange decl.Range.FileName ha.Range.Start decl.Range.End
        | SynModuleSigDecl.Types (types = allTypes) ->
            match List.tryLast allTypes with
            | Some (SynTypeDefnSig (range = rEnd)) -> mkRange decl.Range.FileName decl.Range.Start rEnd.End
            | _ -> decl.Range
        | _ -> decl.Range

type SynModuleOrNamespace with
    member mn.FullRange : Range =
        match mn with
        | SynModuleOrNamespace(attribs = h :: _) -> mkRange mn.Range.FileName h.Range.Start mn.Range.End
        | _ -> mn.Range
