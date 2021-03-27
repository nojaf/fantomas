module Fantomas.AstExtensions

open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.Text
open FSharp.Compiler.Text.Range

type SynTypeDefnSig with
    /// Combines the range of type name and the body.
    member this.FullRange : Range =
        match this with
        | SynTypeDefnSig.TypeDefnSig (comp, _, _, r) -> mkRange r.FileName comp.Range.Start r.End

type SynModuleDecl with
    member decl.FullRange : Range =
        match decl with
        | SynModuleDecl.Let(bindings = Binding(attributes = ha :: _) :: _)
        | SynModuleDecl.Types (typeDefns = (TypeDefn(typeInfo = ComponentInfo(attributes = ha :: _)) :: _)) ->
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
        | SynModuleSigDecl.Types (types = (TypeDefnSig(typeInfo = ComponentInfo(attributes = ha :: _)) :: _ as allTypes)) ->
            match List.tryLast allTypes with
            | Some (TypeDefnSig (range = r)) -> Range.mkRange decl.Range.FileName ha.Range.Start r.End
            | None -> Range.mkRange decl.Range.FileName ha.Range.Start decl.Range.End
        | SynModuleSigDecl.Types (types = allTypes) ->
            match List.tryLast allTypes with
            | Some (TypeDefnSig (range = rEnd)) -> Range.mkRange decl.Range.FileName decl.Range.Start rEnd.End
            | _ -> decl.Range
        | _ -> decl.Range
