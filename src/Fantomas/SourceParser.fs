module internal Fantomas.SourceParser

open System
open FSharp.Compiler.SourceCodeServices.PrettyNaming
open FSharp.Compiler.SourceCodeServices.FSharpKeywords
open FSharp.Compiler.Text
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.XmlDoc
open Fantomas
open Fantomas.Context
open Fantomas.AstExtensions
open Fantomas.TriviaTypes

type Composite<'a, 'b> =
    | Pair of 'b * 'b
    | Single of 'a

[<Literal>]
let MaxLength = 512

[<Literal>]
let private MangledGlobalName : string = "`global`"

let (|Ident|) (s: Ident) =
    let ident = s.idText

    match ident with
    | "`global`" -> "global"
    | "_" -> "_" // workaround for https://github.com/dotnet/fsharp/issues/7681
    | _ -> QuoteIdentifierIfNeeded ident

let (|LongIdent|) (li: LongIdent) =
    li
    |> Seq.map
        (fun x ->
            if x.idText = MangledGlobalName then
                "global"
            else
                (|Ident|) x)
    |> String.concat "."
    |> fun s ->
        // Assume that if it starts with base, it's going to be the base keyword
        if String.startsWithOrdinal "``base``." s then
            String.Join("", "base.", s.[9..])
        else
            s

let (|LongIdentPieces|_|) =
    function
    | SynExpr.LongIdent (_, LongIdentWithDots (lids, _), _, _) ->
        lids
        |> List.map
            (fun x ->
                if x.idText = MangledGlobalName then
                    "global", x.idRange
                else
                    (|Ident|) x, x.idRange)
        |> Some
    | _ -> None


let (|LongIdentWithDotsPieces|_|) =
    function
    | LongIdentWithDots (lids, _) ->
        lids
        |> List.map
            (fun x ->
                if x.idText = MangledGlobalName then
                    "global", x.idRange
                else
                    (|Ident|) x, x.idRange)
        |> Some

let inline (|LongIdentWithDots|) (LongIdentWithDots (LongIdent s, _)) = s

type Identifier =
    | Id of Ident
    | LongId of LongIdent
    member x.Text =
        match x with
        | Id x -> x.idText
        | LongId xs ->
            xs
            |> Seq.map
                (fun x ->
                    if x.idText = MangledGlobalName then
                        "global"
                    else
                        x.idText)
            |> String.concat "."

    member x.Ranges =
        match x with
        | Id x -> List.singleton x.idRange
        | LongId xs -> List.map (fun (x: Ident) -> x.idRange) xs

/// Different from (|Ident|), this pattern also accepts keywords
let inline (|IdentOrKeyword|) (s: Ident) = Id s

let (|LongIdentOrKeyword|) (li: LongIdent) = LongId li

/// Use infix operators in the short form
let (|OpName|) (x: Identifier) =
    let s = x.Text
    let s' = DecompileOpName s

    if IsActivePatternName s then
        sprintf "(%s)" s'
    elif IsPrefixOperator s then
        if s'.[0] = '~' && s'.Length >= 2 && s'.[1] <> '~' then
            s'.[1..]
        else
            s'
    else
        match x with
        | Id (Ident s)
        | LongId (LongIdent s) -> DecompileOpName s

let (|OpNameFullInPattern|) (x: Identifier) =
    let r = x.Ranges
    let s = x.Text
    let s' = DecompileOpName s

    (if IsActivePatternName s
        || IsInfixOperator s
        || IsPrefixOperator s
        || IsTernaryOperator s
        || s = "op_Dynamic" then
         /// Use two spaces for symmetry
         if String.startsWithOrdinal "*" s' && s' <> "*" then
             sprintf "( %s )" s'
         else
             sprintf "(%s)" s'
     else
         match x with
         | Id (Ident s)
         | LongId (LongIdent s) -> DecompileOpName s)
    |> fun s -> (s, r)


/// Operators in their declaration form
let (|OpNameFull|) (x: Identifier) =
    let r = x.Ranges
    let s = x.Text
    let s' = DecompileOpName s

    (if IsActivePatternName s then
         s
     elif IsInfixOperator s
          || IsPrefixOperator s
          || IsTernaryOperator s
          || s = "op_Dynamic" then
         s'
     else
         match x with
         | Id (Ident s)
         | LongId (LongIdent s) -> DecompileOpName s)
    |> fun s -> (s, r)

// Type params

let inline (|Typar|) (SynTypar.Typar (Ident s, req, _)) =
    match req with
    | NoStaticReq -> (s, false)
    | HeadTypeStaticReq -> (s, true)

let inline (|ValTyparDecls|) (SynValTyparDecls (tds, b, tcs)) = (tds, b, tcs)

// Literals

let rec (|RationalConst|) =
    function
    | SynRationalConst.Integer i -> string i
    | SynRationalConst.Rational (numerator, denominator, _) -> sprintf "(%i/%i)" numerator denominator
    | SynRationalConst.Negate (RationalConst s) -> sprintf "- %s" s

let (|Measure|) x =
    let rec loop =
        function
        | SynMeasure.Var (Typar (s, _), _) -> s
        | SynMeasure.Anon _ -> "_"
        | SynMeasure.One -> "1"
        | SynMeasure.Product (m1, m2, _) ->
            let s1 = loop m1
            let s2 = loop m2
            sprintf "%s*%s" s1 s2
        | SynMeasure.Divide (m1, m2, _) ->
            let s1 = loop m1
            let s2 = loop m2
            sprintf "%s/%s" s1 s2
        | SynMeasure.Power (m, RationalConst n, _) ->
            let s = loop m
            sprintf "%s^%s" s n
        | SynMeasure.Seq (ms, _) -> List.map loop ms |> String.concat " "
        | SynMeasure.Named (LongIdent s, _) -> s

    sprintf "<%s>" <| loop x

/// Lose information about kinds of literals
let rec (|Const|) c =
    match c with
    | SynConst.Measure (Const c, Measure m) -> c + m
    | SynConst.UserNum (num, ty) -> num + ty
    | SynConst.Unit -> "()"
    | SynConst.Bool b -> sprintf "%A" b
    | SynConst.SByte s -> sprintf "%A" s
    | SynConst.Byte b -> sprintf "%A" b
    | SynConst.Int16 i -> sprintf "%A" i
    | SynConst.UInt16 u -> sprintf "%A" u
    | SynConst.Int32 i -> sprintf "%A" i
    | SynConst.UInt32 u -> sprintf "%A" u
    | SynConst.Int64 i -> sprintf "%A" i
    | SynConst.UInt64 u -> sprintf "%A" u
    | SynConst.IntPtr i -> sprintf "%in" i
    | SynConst.UIntPtr u -> sprintf "%iun" u
    | SynConst.Single s -> sprintf "%A" s
    | SynConst.Double d -> sprintf "%A" d
    | SynConst.Char c -> sprintf "%A" c
    | SynConst.Decimal d -> sprintf "%A" d
    | SynConst.String (s, _) ->
        // Naive check for verbatim strings
        if not <| String.IsNullOrEmpty(s)
           && s.Contains("\\")
           && not <| s.Contains(@"\\") then
            sprintf "@%A" s
        else
            sprintf "%A" s
    | SynConst.Bytes (bs, _) -> sprintf "%A" bs
    // Auto print may cut off the array
    | SynConst.UInt16s us -> sprintf "%A" us

let (|String|_|) e =
    match e with
    | SynExpr.Const (SynConst.String (s, _), _) -> Some s
    | _ -> None

let (|Unresolved|) (Const s as c, r) = (c, r, s)

// File level patterns

let (|ImplFile|SigFile|) =
    function
    | ParsedInput.ImplFile im -> ImplFile im
    | ParsedInput.SigFile si -> SigFile si

let (|ParsedImplFileInput|) (ParsedImplFileInput.ParsedImplFileInput (_, _, _, _, hs, mns, _)) = (hs, mns)

let (|ParsedSigFileInput|) (ParsedSigFileInput.ParsedSigFileInput (_, _, _, hs, mns)) = (hs, mns)

let (|ModuleOrNamespace|)
    (SynModuleOrNamespace.SynModuleOrNamespace (LongIdent s, isRecursive, isModule, mds, px, ats, ao, _))
    =
    (ats, px, ao, s, mds, isRecursive, isModule)

let (|SigModuleOrNamespace|)
    (SynModuleOrNamespaceSig.SynModuleOrNamespaceSig (LongIdent s, isRecursive, isModule, mds, px, ats, ao, _))
    =
    (ats, px, ao, s, mds, isRecursive, isModule)

let (|Attribute|) (a: SynAttribute) =
    let (LongIdentWithDots s) = a.TypeName
    (s, a.ArgExpr, Option.map (fun (i: Ident) -> i.idText) a.Target)

// Access modifiers

let (|Access|) =
    function
    | SynAccess.Public -> "public"
    | SynAccess.Internal -> "internal"
    | SynAccess.Private -> "private"

let (|PreXmlDoc|) (px: PreXmlDoc) =
    px.ToXmlDoc(false, None).UnprocessedLines

// Module declarations (11 cases)
let (|Open|_|) =
    function
    | SynModuleDecl.Open (SynOpenDeclTarget.ModuleOrNamespace (LongIdent s, _m), _) -> Some s
    | _ -> None

let (|OpenType|_|) =
    function
    // TODO: are there other SynType causes that need to be handled here?
    | SynModuleDecl.Open (SynOpenDeclTarget.Type (SynType.LongIdent (LongIdentWithDots s), _m), _) -> Some s
    | _ -> None

let (|ModuleAbbrev|_|) =
    function
    | SynModuleDecl.ModuleAbbrev (Ident s1, LongIdent s2, _) -> Some(s1, s2)
    | _ -> None

let (|HashDirective|_|) =
    function
    | SynModuleDecl.HashDirective (p, _) -> Some p
    | _ -> None

let (|NamespaceFragment|_|) =
    function
    | SynModuleDecl.NamespaceFragment m -> Some m
    | _ -> None

let (|Attributes|_|) =
    function
    | SynModuleDecl.Attributes (ats, _) -> Some(ats)
    | _ -> None

let (|Let|_|) =
    function
    | SynModuleDecl.Let (false, [ x ], _) -> Some x
    | _ -> None

let (|LetRec|_|) =
    function
    | SynModuleDecl.Let (true, xs, _) -> Some xs
    | _ -> None

let (|DoExpr|_|) =
    function
    | SynModuleDecl.DoExpr (_, x, _) -> Some x
    | _ -> None

let (|Types|_|) =
    function
    | SynModuleDecl.Types (xs, _) -> Some xs
    | _ -> None

let (|NestedModule|_|) =
    function
    | SynModuleDecl.NestedModule (SynComponentInfo.ComponentInfo (ats, _, _, LongIdent s, px, _, ao, _),
                                  isRecursive,
                                  xs,
                                  _,
                                  _) -> Some(ats, px, ao, s, isRecursive, xs)
    | _ -> None

let (|Exception|_|) =
    function
    | SynModuleDecl.Exception (ed, _) -> Some ed
    | _ -> None

// Module declaration signatures (9 cases)

let (|SigOpen|_|) =
    function
    | SynModuleSigDecl.Open (SynOpenDeclTarget.ModuleOrNamespace (LongIdent s, _), _) -> Some s
    | _ -> None

let (|SigOpenType|_|) =
    function
    | SynModuleSigDecl.Open (SynOpenDeclTarget.Type (SynType.LongIdent (LongIdentWithDots s), _), _) -> Some s
    | _ -> None

let (|SigModuleAbbrev|_|) =
    function
    | SynModuleSigDecl.ModuleAbbrev (Ident s1, LongIdent s2, _) -> Some(s1, s2)
    | _ -> None

let (|SigHashDirective|_|) =
    function
    | SynModuleSigDecl.HashDirective (p, _) -> Some p
    | _ -> None

let (|SigNamespaceFragment|_|) =
    function
    | SynModuleSigDecl.NamespaceFragment m -> Some m
    | _ -> None

let (|SigVal|_|) =
    function
    | SynModuleSigDecl.Val (v, _) -> Some v
    | _ -> None

let (|SigTypes|_|) =
    function
    | SynModuleSigDecl.Types (tds, _) -> Some tds
    | _ -> None

let (|SigNestedModule|_|) =
    function
    | SynModuleSigDecl.NestedModule (SynComponentInfo.ComponentInfo (ats, _, _, LongIdent s, px, _, ao, _), _, xs, _) ->
        Some(ats, px, ao, s, xs)
    | _ -> None

let (|SigException|_|) =
    function
    | SynModuleSigDecl.Exception (es, _) -> Some es
    | _ -> None

// Exception definitions

let (|ExceptionDefRepr|) (SynExceptionDefnRepr.SynExceptionDefnRepr (ats, uc, _, px, ao, _)) = (ats, px, ao, uc)

let (|SigExceptionDefRepr|) (SynExceptionDefnRepr.SynExceptionDefnRepr (ats, uc, _, px, ao, _)) = (ats, px, ao, uc)

let (|ExceptionDef|)
    (SynExceptionDefn.SynExceptionDefn (SynExceptionDefnRepr.SynExceptionDefnRepr (ats, uc, _, px, ao, _), ms, _))
    =
    (ats, px, ao, uc, ms)

let (|SigExceptionDef|)
    (SynExceptionSig.SynExceptionSig (SynExceptionDefnRepr.SynExceptionDefnRepr (ats, uc, _, px, ao, _), ms, _))
    =
    (ats, px, ao, uc, ms)

let (|UnionCase|) (SynUnionCase.UnionCase (ats, Ident s, uct, px, ao, _)) = (ats, px, ao, s, uct)

let (|UnionCaseType|) =
    function
    | SynUnionCaseType.UnionCaseFields fs -> fs
    | SynUnionCaseType.UnionCaseFullType _ -> failwith "UnionCaseFullType should be used internally only."

let (|Field|) (SynField.Field (ats, isStatic, ido, t, isMutable, px, ao, _)) =
    (ats, px, ao, isStatic, isMutable, t, Option.map (|Ident|) ido)

let (|EnumCase|) (SynEnumCase.EnumCase (ats, Ident s, c, px, r)) = (ats, px, s, (c, r))

// Member definitions (11 cases)

let (|MDNestedType|_|) =
    function
    | SynMemberDefn.NestedType (td, ao, _) -> Some(td, ao)
    | _ -> None

let (|MDOpen|_|) =
    function
    | SynMemberDefn.Open (SynOpenDeclTarget.ModuleOrNamespace (LongIdent s, _), _) -> Some s
    | _ -> None

let (|MDOpenType|_|) =
    function
    | SynMemberDefn.Open (SynOpenDeclTarget.Type (SynType.LongIdent (LongIdentWithDots s), _), _) -> Some s
    | _ -> None

let (|MDImplicitInherit|_|) =
    function
    | SynMemberDefn.ImplicitInherit (t, e, ido, _) -> Some(t, e, Option.map (|Ident|) ido)
    | _ -> None

let (|MDInherit|_|) =
    function
    | SynMemberDefn.Inherit (t, ido, _) -> Some(t, Option.map (|Ident|) ido)
    | _ -> None

let (|MDValField|_|) =
    function
    | SynMemberDefn.ValField (f, _) -> Some f
    | _ -> None

let (|MDImplicitCtor|_|) =
    function
    | SynMemberDefn.ImplicitCtor (ao, ats, ps, ido, _docs, _) -> Some(ats, ao, ps, Option.map (|Ident|) ido)
    | _ -> None

let (|MDMember|_|) =
    function
    | SynMemberDefn.Member (b, _) -> Some b
    | _ -> None

let (|MDLetBindings|_|) =
    function
    | SynMemberDefn.LetBindings (es, isStatic, isRec, _) -> Some(isStatic, isRec, es)
    | _ -> None

let (|MDAbstractSlot|_|) =
    function
    | SynMemberDefn.AbstractSlot (ValSpfn (ats, Ident s, tds, t, vi, _, _, px, ao, _, _), mf, _) ->
        Some(ats, px, ao, s, t, vi, tds, mf)
    | _ -> None

let (|MDInterface|_|) =
    function
    | SynMemberDefn.Interface (t, mdo, range) -> Some(t, mdo, range)
    | _ -> None

let (|MDAutoProperty|_|) =
    function
    | SynMemberDefn.AutoProperty (ats, isStatic, Ident s, typeOpt, mk, memberKindToMemberFlags, px, ao, e, _, _) ->
        Some(ats, px, ao, mk, e, s, isStatic, typeOpt, memberKindToMemberFlags)
    | _ -> None

// Interface impl

let (|InterfaceImpl|) (SynInterfaceImpl.InterfaceImpl (t, bs, range)) = (t, bs, range)

// Bindings

let (|PropertyGet|_|) =
    function
    | MemberKind.PropertyGet -> Some()
    | _ -> None

let (|PropertySet|_|) =
    function
    | MemberKind.PropertySet -> Some()
    | _ -> None

let (|PropertyGetSet|_|) =
    function
    | MemberKind.PropertyGetSet -> Some()
    | _ -> None

let (|MFProperty|_|) (mf: MemberFlags) =
    match mf.MemberKind with
    | MemberKind.PropertyGet
    | MemberKind.PropertySet
    | MemberKind.PropertyGetSet as mk -> Some mk
    | _ -> None

let (|MFMemberFlags|) (mf: MemberFlags) = mf.MemberKind

/// This pattern finds out which keyword to use
let (|MFMember|MFStaticMember|MFConstructor|MFOverride|) (mf: MemberFlags) =
    match mf.MemberKind with
    | MemberKind.ClassConstructor
    | MemberKind.Constructor -> MFConstructor()
    | MemberKind.Member
    | MemberKind.PropertyGet
    | MemberKind.PropertySet
    | MemberKind.PropertyGetSet as mk ->
        if mf.IsInstance && mf.IsOverrideOrExplicitImpl then
            MFOverride mk
        elif mf.IsInstance then
            MFMember mk
        else
            MFStaticMember mk

let (|DoBinding|LetBinding|MemberBinding|PropertyBinding|ExplicitCtor|) =
    function
    | SynBinding.Binding (ao, _, _, _, ats, px, SynValData (Some MFConstructor, _, ido), pat, _, expr, _, _) ->
        ExplicitCtor(ats, px, ao, pat, expr, Option.map (|Ident|) ido)
    | SynBinding.Binding (ao, _, isInline, _, ats, px, SynValData (Some (MFProperty _ as mf), _, _), pat, _, expr, _, _) ->
        PropertyBinding(ats, px, ao, isInline, mf, pat, expr)
    | SynBinding.Binding (ao, _, isInline, _, ats, px, SynValData (Some mf, synValInfo, _), pat, _, expr, _, _) ->
        MemberBinding(ats, px, ao, isInline, mf, pat, expr, synValInfo)
    | SynBinding.Binding (_, DoBinding, _, _, ats, px, _, _, _, expr, _, _) -> DoBinding(ats, px, expr)
    | SynBinding.Binding (ao, _, isInline, isMutable, attrs, px, SynValData (_, valInfo, _), pat, _, expr, _, _) ->
        LetBinding(attrs, px, ao, isInline, isMutable, pat, expr, valInfo)

// Expressions (55 cases, lacking to handle 11 cases)

let (|TraitCall|_|) =
    function
    | SynExpr.TraitCall (tps, msg, expr, _) -> Some(tps, msg, expr)
    | _ -> None

/// isRaw = true with <@@ and @@>
let (|Quote|_|) =
    function
    | SynExpr.Quote (e1, isRaw, e2, _, _) -> Some(e1, e2, isRaw)
    | _ -> None

let (|Paren|_|) =
    function
    | SynExpr.Paren (e, lpr, rpr, r) -> Some(lpr, e, rpr, r)
    | _ -> None

type ExprKind =
    | InferredDowncast
    | InferredUpcast
    | Lazy
    | Assert
    | AddressOfSingle
    | AddressOfDouble
    | Yield
    | Return
    | YieldFrom
    | ReturnFrom
    | Do
    | DoBang
    override x.ToString() =
        match x with
        | InferredDowncast -> "downcast "
        | InferredUpcast -> "upcast "
        | Lazy -> "lazy "
        | Assert -> "assert "
        | AddressOfSingle -> "&"
        | AddressOfDouble -> "&&"
        | Yield -> "yield "
        | Return -> "return "
        | YieldFrom -> "yield! "
        | ReturnFrom -> "return! "
        | Do -> "do "
        | DoBang -> "do! "

let (|SingleExpr|_|) =
    function
    | SynExpr.InferredDowncast (e, _) -> Some(InferredDowncast, e)
    | SynExpr.InferredUpcast (e, _) -> Some(InferredUpcast, e)
    | SynExpr.Lazy (e, _) -> Some(Lazy, e)
    | SynExpr.Assert (e, _) -> Some(Assert, e)
    | SynExpr.AddressOf (true, e, _, _) -> Some(AddressOfSingle, e)
    | SynExpr.AddressOf (false, e, _, _) -> Some(AddressOfDouble, e)
    | SynExpr.YieldOrReturn ((true, _), e, _) -> Some(Yield, e)
    | SynExpr.YieldOrReturn ((false, _), e, _) -> Some(Return, e)
    | SynExpr.YieldOrReturnFrom ((true, _), e, _) -> Some(YieldFrom, e)
    | SynExpr.YieldOrReturnFrom ((false, _), e, _) -> Some(ReturnFrom, e)
    | SynExpr.Do (e, _) -> Some(Do, e)
    | SynExpr.DoBang (e, _) -> Some(DoBang, e)
    | _ -> None

type TypedExprKind =
    | TypeTest
    | Downcast
    | Upcast
    | Typed

let (|TypedExpr|_|) =
    function
    | SynExpr.TypeTest (e, t, _) -> Some(TypeTest, e, t)
    | SynExpr.Downcast (e, t, _) -> Some(Downcast, e, t)
    | SynExpr.Upcast (e, t, _) -> Some(Upcast, e, t)
    | SynExpr.Typed (e, t, _) -> Some(Typed, e, t)
    | _ -> None

let (|While|_|) =
    function
    | SynExpr.While (_, e1, e2, _) -> Some(e1, e2)
    | _ -> None

let (|For|_|) =
    function
    | SynExpr.For (_, Ident s, e1, isUp, e2, e3, _) -> Some(s, e1, e2, e3, isUp)
    | _ -> None

let (|NullExpr|_|) =
    function
    | SynExpr.Null _ -> Some()
    | _ -> None

let (|ConstExpr|_|) =
    function
    | SynExpr.Const (x, r) -> Some(x, r)
    | _ -> None

let (|TypeApp|_|) =
    function
    | SynExpr.TypeApp (e, _, ts, _, _, _, _) -> Some(e, ts)
    | _ -> None

let (|Match|_|) =
    function
    | SynExpr.Match (_, e, cs, _) -> Some(e, cs)
    | _ -> None

let (|MatchBang|_|) =
    function
    | SynExpr.MatchBang (_, e, cs, _) -> Some(e, cs)
    | _ -> None

let (|Sequential|_|) =
    function
    | SynExpr.Sequential (_, isSeq, e1, e2, _) -> Some(e1, e2, isSeq)
    | _ -> None

let rec (|Sequentials|_|) =
    function
    | Sequential (e, Sequentials es, _) -> Some(e :: es)
    | Sequential (e1, e2, _) -> Some [ e1; e2 ]
    | _ -> None

let (|SimpleExpr|_|) =
    function
    | SynExpr.Null _
    | SynExpr.Ident _
    | SynExpr.LongIdent _
    | SynExpr.Const (Const _, _) as e -> Some e
    | _ -> None

/// Only recognize numbers; strings are ignored
let rec (|SequentialSimple|_|) =
    function
    | Sequential (SimpleExpr e, SequentialSimple es, true) -> Some(e :: es)
    | Sequential (SimpleExpr e1, SimpleExpr e2, true) -> Some [ e1; e2 ]
    | _ -> None

let (|CompExpr|_|) =
    function
    | SynExpr.CompExpr (isArray, _, expr, _) -> Some(isArray, expr)
    | _ -> None

let isCompExpr =
    function
    | CompExpr _ -> true
    | _ -> false

let (|ArrayOrListOfSeqExpr|_|) =
    function
    | SynExpr.ArrayOrListOfSeqExpr (isArray, expr, _) -> Some(isArray, expr)
    | _ -> None

/// This pattern only includes arrays and lists in computation expressions
let (|ArrayOrList|_|) =
    function
    | ArrayOrListOfSeqExpr (isArray, CompExpr (_, SequentialSimple xs)) -> Some(isArray, xs, true)
    | SynExpr.ArrayOrList (isArray, xs, _)
    | ArrayOrListOfSeqExpr (isArray, CompExpr (_, Sequentials xs)) -> Some(isArray, xs, false)
    | _ -> None

let (|Tuple|_|) =
    function
    | SynExpr.Tuple (false, exprs, _, tupleRange) -> Some(exprs, Some tupleRange)
    | _ -> None

let (|StructTuple|_|) =
    function
    | SynExpr.Tuple (true, exprs, _, _) -> Some exprs
    | _ -> None

let (|IndexedVar|_|) =
    function
    // We might have to narrow scope of this pattern to avoid incorrect usage
    | SynExpr.App (_, _, SynExpr.LongIdent (_, LongIdentWithDots "Microsoft.FSharp.Core.Some", _, _), e, _) ->
        Some(Some e)
    | SynExpr.LongIdent (_, LongIdentWithDots "Microsoft.FSharp.Core.None", _, _) -> Some None
    | _ -> None

let (|Indexer|) =
    function
    | SynIndexerArg.Two (e1, e1FromEnd, e2, e2FromEnd, _, _) -> Pair((e1, e1FromEnd), (e2, e2FromEnd))
    | SynIndexerArg.One (e, fromEnd, _) -> Single(e, fromEnd)

let (|OptVar|_|) =
    function
    | SynExpr.Ident (IdentOrKeyword (OpNameFull (s, r))) -> Some(s, false, r)
    | SynExpr.LongIdent (isOpt, LongIdentWithDots.LongIdentWithDots (LongIdentOrKeyword (OpNameFull (s, r)), _), _, _) ->
        Some(s, isOpt, r)
    | _ -> None

/// This pattern is escaped by using OpName
let (|Var|_|) =
    function
    | SynExpr.Ident (IdentOrKeyword (OpName s)) -> Some s
    | SynExpr.LongIdent (_, LongIdentWithDots.LongIdentWithDots (LongIdentOrKeyword (OpName s), _), _, _) -> Some s
    | _ -> None

// Compiler-generated patterns often have "_arg" prefix
let (|CompilerGeneratedVar|_|) =
    function
    | SynExpr.Ident (IdentOrKeyword (OpName s)) when String.startsWithOrdinal "_arg" s -> Some s
    | SynExpr.LongIdent (_, LongIdentWithDots.LongIdentWithDots (LongIdentOrKeyword (OpName s), _), opt, _) ->
        match opt with
        | Some _ -> Some s
        | None ->
            if String.startsWithOrdinal "_arg" s then
                Some s
            else
                None
    | _ -> None

/// Get all application params at once
let (|App|_|) e =
    let rec loop =
        function
        // function application is left-recursive
        | SynExpr.App (_, _, e, e2, _) ->
            let e1, es = loop e
            (e1, e2 :: es)
        | e -> (e, [])

    match loop e with
    | _, [] -> None
    | e, es -> Some(e, List.rev es)

// captures application with single parenthesis argument
let (|AppSingleParenArg|_|) =
    function
    | App (SynExpr.DotGet _, [ (Paren (_, Tuple _, _, _)) ]) -> None
    | App (e, [ Paren (_, singleExpr, _, _) as px ]) ->
        match singleExpr with
        | SynExpr.Lambda _
        | SynExpr.MatchLambda _ -> None
        | _ -> Some(e, px)
    | _ -> None

let (|AppOrTypeApp|_|) e =
    match e with
    | App (TypeApp (e, ts), es) -> Some(e, Some ts, es)
    | App (e, es) -> Some(e, None, es)
    | _ -> None

let (|NewTuple|_|) =
    function
    | SynExpr.New (_, t, (Paren _ as px), _) -> Some(t, px)
    | SynExpr.New (_, t, (ConstExpr (SynConst.Unit, _) as px), _) -> Some(t, px)
    | _ -> None

let (|CompApp|_|) =
    function
    | SynExpr.App (_, _, Var "seq", (SynExpr.App _ as e), _) -> Some("seq", e)
    | _ -> None

/// Only process prefix operators here
let (|PrefixApp|_|) =
    function
    // Var pattern causes a few prefix operators appear as infix operators
    | SynExpr.App (_, false, SynExpr.Ident (IdentOrKeyword s), e2, _)
    | SynExpr.App (_,
                   false,
                   SynExpr.LongIdent (_, LongIdentWithDots.LongIdentWithDots (LongIdentOrKeyword s, _), _, _),
                   e2,
                   _) when IsPrefixOperator(DecompileOpName s.Text) -> Some((|OpName|) s, e2)
    | _ -> None

let (|InfixApp|_|) synExpr =
    match synExpr with
    | SynExpr.App (_, true, (Var "::" as e), Tuple ([ e1; e2 ], _), _) -> Some("::", e, e1, e2)
    // Range operators need special treatments, so we exclude them here
    | SynExpr.App (_, _, SynExpr.App (_, true, (Var s as e), e1, _), e2, _) when s <> ".." -> Some(s, e, e1, e2)
    | _ -> None

let (|NewlineInfixApp|_|) =
    function
    | InfixApp (text, operatorExpr, e1, e2) when (newLineInfixOps.Contains(text)) -> Some(text, operatorExpr, e1, e2)
    | _ -> None

let (|NewlineInfixApps|_|) e =
    let rec loop synExpr =
        match synExpr with
        | NewlineInfixApp (s, opE, e, e2) ->
            let e1, es = loop e
            (e1, (s, opE, e2) :: es)
        | e -> (e, [])

    match loop e with
    | e, es when (List.length es > 1) -> Some(e, List.rev es)
    | _ -> None

let (|SameInfixApps|_|) e =
    let rec loop operator synExpr =
        match synExpr with
        | InfixApp (s, opE, e, e2) when (s = operator) ->
            let e1, es = loop operator e
            (e1, (s, opE, e2) :: es)
        | e -> (e, [])

    match e with
    | InfixApp (operatorText, _, _, _) ->
        match loop operatorText e with
        | e, es when (List.length es > 1) -> Some(e, List.rev es)
        | _ -> None
    | _ -> None

let (|TernaryApp|_|) =
    function
    | SynExpr.App (_, _, SynExpr.App (_, _, SynExpr.App (_, true, Var "?<-", e1, _), e2, _), e3, _) -> Some(e1, e2, e3)
    | _ -> None

let (|MatchLambda|_|) =
    function
    | SynExpr.MatchLambda (isMember, _, pats, _, _) -> Some(pats, isMember)
    | _ -> None

let (|JoinIn|_|) =
    function
    | SynExpr.JoinIn (e1, _, e2, _) -> Some(e1, e2)
    | _ -> None

let (|LetOrUse|_|) =
    function
    | SynExpr.LetOrUse (isRec, isUse, xs, e, _) -> Some(isRec, isUse, xs, e)
    | _ -> None

/// Unfold a list of let bindings
/// Recursive and use properties have to be determined at this point
let rec (|LetOrUses|_|) =
    function
    | SynExpr.LetOrUse (isRec, isUse, xs, LetOrUses (ys, e), _) ->
        let prefix =
            if isUse then "use "
            elif isRec then "let rec "
            else "let "

        let xs' =
            List.mapi
                (fun i x ->
                    if i = 0 then
                        (prefix, x)
                    else
                        ("and ", x))
                xs

        Some(xs' @ ys, e)
    | SynExpr.LetOrUse (isRec, isUse, xs, e, _) ->
        let prefix =
            if isUse then "use "
            elif isRec then "let rec "
            else "let "

        let xs' =
            List.mapi
                (fun i x ->
                    if i = 0 then
                        (prefix, x)
                    else
                        ("and ", x))
                xs

        Some(xs', e)
    | _ -> None

type ComputationExpressionStatement =
    | LetOrUseStatement of prefix: string * binding: SynBinding
    | LetOrUseBangStatement of isUse: bool * SynPat * SynExpr * range
    | AndBangStatement of SynPat * SynExpr * range
    | OtherStatement of SynExpr

let rec collectComputationExpressionStatements e : ComputationExpressionStatement list =
    match e with
    | LetOrUses (bindings, body) ->
        let letBindings = bindings |> List.map LetOrUseStatement

        let returnExpr =
            collectComputationExpressionStatements body

        letBindings @ returnExpr
    | SynExpr.LetOrUseBang (_, isUse, _, pat, expr, andBangs, body, r) ->
        let letOrUseBang =
            LetOrUseBangStatement(isUse, pat, expr, r)

        let andBangs =
            andBangs
            |> List.map (fun (_, _, _, ap, ae, andRange) -> AndBangStatement(ap, ae, andRange))

        let bodyStatements =
            collectComputationExpressionStatements body

        [ letOrUseBang
          yield! andBangs
          yield! bodyStatements ]
    | SynExpr.Sequential (_, _, e1, e2, _) ->
        [ yield! collectComputationExpressionStatements e1
          yield! collectComputationExpressionStatements e2 ]
    | expr -> [ OtherStatement expr ]

/// Matches if the SynExpr has some or of computation expression member call inside.
let rec (|CompExprBody|_|) expr =
    match expr with
    | SynExpr.LetOrUse (_, _, _, CompExprBody _, _) -> Some(collectComputationExpressionStatements expr)
    | SynExpr.LetOrUseBang _ -> Some(collectComputationExpressionStatements expr)
    | SynExpr.Sequential (_, _, _, SynExpr.YieldOrReturn _, _) -> Some(collectComputationExpressionStatements expr)
    | SynExpr.Sequential (_, _, _, SynExpr.LetOrUse _, _) -> Some(collectComputationExpressionStatements expr)
    | SynExpr.Sequential (_, _, SynExpr.DoBang _, SynExpr.LetOrUseBang _, _) ->
        Some(collectComputationExpressionStatements expr)
    | _ -> None

let (|ForEach|_|) =
    function
    | SynExpr.ForEach (_, SeqExprOnly true, _, pat, e1, SingleExpr (Yield, e2), _) -> Some(pat, e1, e2, true)
    | SynExpr.ForEach (_, SeqExprOnly isArrow, _, pat, e1, e2, _) -> Some(pat, e1, e2, isArrow)
    | _ -> None

let (|DotIndexedSet|_|) =
    function
    | SynExpr.DotIndexedSet (e1, es, e2, _, _, _) -> Some(e1, es, e2)
    | _ -> None

let (|NamedIndexedPropertySet|_|) =
    function
    | SynExpr.NamedIndexedPropertySet (LongIdentWithDots ident, e1, e2, _) -> Some(ident, e1, e2)
    | _ -> None

let (|DotNamedIndexedPropertySet|_|) =
    function
    | SynExpr.DotNamedIndexedPropertySet (e, LongIdentWithDots ident, e1, e2, _) -> Some(e, ident, e1, e2)
    | _ -> None

let (|DotIndexedGet|_|) =
    function
    | SynExpr.DotIndexedGet (e1, es, _, _) -> Some(e1, es)
    | _ -> None

let (|DotGet|_|) =
    function
    | SynExpr.DotGet (e, _, LongIdentWithDotsPieces lids, _) -> Some(e, lids)
    | _ -> None

/// Match function call followed by Property
let (|DotGetAppParen|_|) e =
    match e with
    //| App(e, [DotGet (Paren _ as p, (s,r))]) -> Some (e, p, s, r)
    | DotGet (App (e, [ Paren (_, Tuple _, _, _) as px ]), lids) -> Some(e, px, lids)
    | DotGet (App (e, [ Paren (_, singleExpr, _, _) as px ]), lids) ->
        match singleExpr with
        | SynExpr.Lambda _
        | SynExpr.MatchLambda _ -> None
        | _ -> Some(e, px, lids)
    | DotGet (App (e, [ ConstExpr (SynConst.Unit, _) as px ]), lids) -> Some(e, px, lids)
    | _ -> None

/// Gather series of application for line breaking
let rec (|DotGetApp|_|) =
    function
    | SynExpr.App (_, _, DotGet (DotGetApp (e, es), s), e', _) -> Some(e, [ yield! es; yield (s, e', []) ])
    | SynExpr.App (_, _, DotGet (e, s), e', _) -> Some(e, [ (s, e', []) ])
    | SynExpr.App (_, _, SynExpr.TypeApp (DotGet (DotGetApp (e, es), s), _, ts, _, _, _, _), e', _) ->
        Some(e, [ yield! es; yield (s, e', ts) ])
    | SynExpr.App (_, _, SynExpr.TypeApp (DotGet (e, s), _, ts, _, _, _, _), e', _) -> Some(e, [ (s, e', ts) ])
    | _ -> None

let (|DotSet|_|) =
    function
    | SynExpr.DotSet (e1, LongIdentWithDots s, e2, _) -> Some(e1, s, e2)
    | _ -> None

let (|IfThenElse|_|) =
    function
    | SynExpr.IfThenElse (e1, e2, e3, _, _, mIfToThen, _) -> Some(e1, e2, e3, mIfToThen)
    | _ -> None

let rec (|ElIf|_|) =
    function
    | SynExpr.IfThenElse (e1, e2, Some (ElIf (es, e3)), _, _, r, fullRange) as node ->
        Some((e1, e2, r, fullRange, node) :: es, e3)
    | SynExpr.IfThenElse (e1, e2, e3, _, _, r, fullRange) as node -> Some([ (e1, e2, r, fullRange, node) ], e3)
    | _ -> None

let (|Record|_|) =
    function
    | SynExpr.Record (inheritOpt, eo, xs, _) ->
        let inheritOpt =
            inheritOpt
            |> Option.map (fun (typ, expr, _, _, _) -> (typ, expr))

        Some(inheritOpt, xs, Option.map fst eo)
    | _ -> None

let (|AnonRecord|_|) =
    function
    | SynExpr.AnonRecd (isStruct, copyInfo, fields, _) -> Some(isStruct, fields, Option.map fst copyInfo)
    | _ -> None

let (|ObjExpr|_|) =
    function
    | SynExpr.ObjExpr (t, eio, bd, ims, _, range) -> Some(t, eio, bd, ims, range)
    | _ -> None

let (|LongIdentSet|_|) =
    function
    | SynExpr.LongIdentSet (LongIdentWithDots s, e, r) -> Some(s, e, r)
    | _ -> None

let (|TryWith|_|) =
    function
    | SynExpr.TryWith (e, _, cs, _, _, _, _) -> Some(e, cs)
    | _ -> None

let (|TryFinally|_|) =
    function
    | SynExpr.TryFinally (e1, e2, _, _, _) -> Some(e1, e2)
    | _ -> None

let (|ParsingError|_|) =
    function
    | SynExpr.ArbitraryAfterError (_, r)
    | SynExpr.FromParseError (_, r)
    | SynExpr.DiscardAfterMissingQualificationAfterDot (_, r) -> Some r
    | _ -> None

let (|ILEmbedded|_|) =
    function
    | SynExpr.LibraryOnlyILAssembly (_, _, _, _, r) -> Some(r)
    | _ -> None

let (|UnsupportedExpr|_|) =
    function
    // Temprorarily ignore these cases not often used outside FSharp.Core
    | SynExpr.LibraryOnlyStaticOptimization (_, _, _, r)
    | SynExpr.LibraryOnlyUnionCaseFieldGet (_, _, _, r)
    | SynExpr.LibraryOnlyUnionCaseFieldSet (_, _, _, _, r) -> Some r
    | _ -> None

// Patterns (18 cases, lacking to handle 2 cases)

let (|PatOptionalVal|_|) =
    function
    | SynPat.OptionalVal (Ident s, _) -> Some s
    | _ -> None

let (|PatAttrib|_|) =
    function
    | SynPat.Attrib (p, ats, _) -> Some(p, ats)
    | _ -> None

let (|PatOr|_|) =
    function
    | SynPat.Or (p1, p2, _) -> Some(p1, p2)
    | _ -> None

let rec (|PatOrs|_|) =
    function
    | PatOr (PatOrs pats, p2) -> Some [ yield! pats; yield p2 ]
    | PatOr (p1, p2) -> Some [ p1; p2 ]
    | _ -> None

let (|PatAnds|_|) =
    function
    | SynPat.Ands (ps, _) -> Some ps
    | _ -> None

type PatNullaryKind =
    | PatNull
    | PatWild

let (|PatNullary|_|) =
    function
    | SynPat.Null _ -> Some PatNull
    | SynPat.Wild _ -> Some PatWild
    | _ -> None

let (|PatTuple|_|) =
    function
    | SynPat.Tuple (false, ps, _) -> Some ps
    | _ -> None

let (|PatStructTuple|_|) =
    function
    | SynPat.Tuple (true, ps, _) -> Some ps
    | _ -> None

type SeqPatKind =
    | PatArray
    | PatList

let (|PatSeq|_|) =
    function
    | SynPat.ArrayOrList (true, ps, _) -> Some(PatArray, ps)
    | SynPat.ArrayOrList (false, ps, _) -> Some(PatList, ps)
    | _ -> None

let (|PatTyped|_|) =
    function
    | SynPat.Typed (p, t, _) -> Some(p, t)
    | _ -> None

let (|PatNamed|_|) =
    function
    | SynPat.Named (p, IdentOrKeyword (OpNameFullInPattern (s, _)), _, ao, _) -> Some(ao, p, s)
    | _ -> None

let (|PatLongIdent|_|) =
    function
    | SynPat.LongIdent (LongIdentWithDots.LongIdentWithDots (LongIdentOrKeyword (OpNameFullInPattern (s, _)), _),
                        _,
                        tpso,
                        xs,
                        ao,
                        _) ->
        match xs with
        | SynArgPats.Pats ps -> Some(ao, s, List.map (fun p -> (None, p)) ps, tpso)
        | SynArgPats.NamePatPairs (nps, _) ->
            Some(ao, s, List.map (fun ((Ident ident), p) -> (Some ident, p)) nps, tpso)
    | _ -> None

let (|PatParen|_|) =
    function
    | SynPat.Paren (p, _) -> Some p
    | _ -> None

let (|PatRecord|_|) =
    function
    | SynPat.Record (xs, _) -> Some xs
    | _ -> None

let (|PatConst|_|) =
    function
    | SynPat.Const (c, r) -> Some(c, r)
    | _ -> None

let (|PatIsInst|_|) =
    function
    | SynPat.IsInst (t, _) -> Some t
    | _ -> None

let (|PatQuoteExpr|_|) =
    function
    | SynPat.QuoteExpr (e, _) -> Some e
    | _ -> None

let (|PatExplicitCtor|_|) =
    function
    | SynPat.LongIdent (LongIdentWithDots.LongIdentWithDots ([ newIdent ], _),
                        _,
                        _,
                        SynArgPats.Pats [ PatParen _ as pat ],
                        ao,
                        _) when (newIdent.idText = "new") -> Some(ao, pat)
    | _ -> None

// Members

let (|SPAttrib|SPId|SPTyped|) =
    function
    | SynSimplePat.Attrib (sp, ats, _) -> SPAttrib(ats, sp)
    // Not sure compiler generated SPIds are used elsewhere.
    | SynSimplePat.Id (Ident s, _, isGen, _, isOptArg, _) -> SPId(s, isOptArg, isGen)
    | SynSimplePat.Typed (sp, t, _) -> SPTyped(sp, t)

let (|SimplePats|SPSTyped|) =
    function
    | SynSimplePats.SimplePats (ps, _) -> SimplePats ps
    | SynSimplePats.Typed (ps, t, _) -> SPSTyped(ps, t)

let (|RecordField|) =
    function
    | SynField.Field (ats, _, ido, _, _, px, ao, _) -> (ats, px, ao, Option.map (|Ident|) ido)

let (|Clause|) (SynMatchClause.Clause (p, eo, e, _, _)) = (p, e, eo)

/// Process compiler-generated matches in an appropriate way
let (|Lambda|_|) =
    function
    | SynExpr.Lambda (_, _, _, _, Some (pats, body), range) ->
        // find the body expression from the last lambda
        let rec visit (e: SynExpr) : SynExpr =
            match e with
            | SynExpr.Match (matchSeqPoint = NoDebugPointAtInvisibleBinding; clauses = [ Clause (_, expr, _) ])
            | SynExpr.Lambda (_, _, _, SynExpr.Match(clauses = [ Clause (_, expr, _) ]), _, _) -> visit expr
            | _ -> e

        Some(pats, visit body, range)
    | _ -> None

// Type definitions

let (|TDSREnum|TDSRUnion|TDSRRecord|TDSRNone|TDSRTypeAbbrev|TDSRException|) =
    function
    | SynTypeDefnSimpleRepr.Enum (ecs, _) -> TDSREnum ecs
    | SynTypeDefnSimpleRepr.Union (ao, xs, _) -> TDSRUnion(ao, xs)
    | SynTypeDefnSimpleRepr.Record (ao, fs, _) -> TDSRRecord(ao, fs)
    | SynTypeDefnSimpleRepr.None _ -> TDSRNone()
    | SynTypeDefnSimpleRepr.TypeAbbrev (_, t, _) -> TDSRTypeAbbrev t
    | SynTypeDefnSimpleRepr.General _ -> failwith "General should not appear in the parse tree"
    | SynTypeDefnSimpleRepr.LibraryOnlyILAssembly _ -> failwith "LibraryOnlyILAssembly is not supported yet"
    | SynTypeDefnSimpleRepr.Exception repr -> TDSRException repr

let (|Simple|ObjectModel|ExceptionRepr|) =
    function
    | SynTypeDefnRepr.Simple (tdsr, _) -> Simple tdsr
    | SynTypeDefnRepr.ObjectModel (tdk, mds, range) -> ObjectModel(tdk, mds, range)
    | SynTypeDefnRepr.Exception repr -> ExceptionRepr repr

let (|MemberDefnList|) mds =
    // Assume that there is at most one implicit constructor
    let impCtor =
        List.tryFind
            (function
            | MDImplicitCtor _ -> true
            | _ -> false)
            mds
    // Might need to sort so that let and do bindings come first
    let others =
        List.filter
            (function
            | MDImplicitCtor _ -> false
            | _ -> true)
            mds

    (impCtor, others)

let (|SigSimple|SigObjectModel|SigExceptionRepr|) =
    function
    | SynTypeDefnSigRepr.Simple (tdsr, _) -> SigSimple tdsr
    | SynTypeDefnSigRepr.ObjectModel (tdk, mds, _) -> SigObjectModel(tdk, mds)
    | SynTypeDefnSigRepr.Exception repr -> SigExceptionRepr repr

type TypeDefnKindSingle =
    | TCUnspecified
    | TCClass
    | TCInterface
    | TCStruct
    | TCRecord
    | TCUnion
    | TCAbbrev
    | TCHiddenRepr
    | TCAugmentation
    | TCILAssemblyCode

let (|TCSimple|TCDelegate|) =
    function
    | TyconUnspecified -> TCSimple TCUnspecified
    | TyconClass -> TCSimple TCClass
    | TyconInterface -> TCSimple TCInterface
    | TyconStruct -> TCSimple TCStruct
    | TyconRecord -> TCSimple TCRecord
    | TyconUnion -> TCSimple TCUnion
    | TyconAbbrev -> TCSimple TCAbbrev
    | TyconHiddenRepr -> TCSimple TCHiddenRepr
    | TyconAugmentation -> TCSimple TCAugmentation
    | TyconILAssemblyCode -> TCSimple TCILAssemblyCode
    | TyconDelegate (t, vi) -> TCDelegate(t, vi)

let (|TypeDef|)
    (SynTypeDefn.TypeDefn (SynComponentInfo.ComponentInfo (ats, tds, tcs, LongIdent s, px, preferPostfix, ao, _),
                           tdr,
                           ms,
                           _))
    =
    (ats, px, ao, tds, tcs, tdr, ms, s, preferPostfix)

let (|SigTypeDef|)
    (SynTypeDefnSig.TypeDefnSig (SynComponentInfo.ComponentInfo (ats, tds, tcs, LongIdent s, px, preferPostfix, ao, _),
                                 tdr,
                                 ms,
                                 _) as node)
    =
    (ats, px, ao, tds, tcs, tdr, ms, s, preferPostfix, node.FullRange)

let (|TyparDecl|) (SynTyparDecl.TyparDecl (ats, tp)) = (ats, tp)

// Types (15 cases)

let (|THashConstraint|_|) =
    function
    | SynType.HashConstraint (t, _) -> Some t
    | _ -> None

let (|TMeasurePower|_|) =
    function
    | SynType.MeasurePower (t, RationalConst n, _) -> Some(t, n)
    | _ -> None

let (|TMeasureDivide|_|) =
    function
    | SynType.MeasureDivide (t1, t2, _) -> Some(t1, t2)
    | _ -> None

let (|TStaticConstant|_|) =
    function
    | SynType.StaticConstant (c, r) -> Some(c, r)
    | _ -> None

let (|TStaticConstantExpr|_|) =
    function
    | SynType.StaticConstantExpr (c, _) -> Some c
    | _ -> None

let (|TStaticConstantNamed|_|) =
    function
    | SynType.StaticConstantNamed (t1, t2, _) -> Some(t1, t2)
    | _ -> None

let (|TArray|_|) =
    function
    | SynType.Array (n, t, _) -> Some(t, n)
    | _ -> None

let (|TAnon|_|) =
    function
    | SynType.Anon _ -> Some()
    | _ -> None

let (|TVar|_|) =
    function
    | SynType.Var (tp, _) -> Some tp
    | _ -> None

let (|TFun|_|) =
    function
    | SynType.Fun (t1, t2, _) -> Some(t1, t2)
    | _ -> None

// Arrow type is right-associative
let rec (|TFuns|_|) =
    function
    | TFun (t1, TFuns ts) -> Some [ yield t1; yield! ts ]
    | TFun (t1, t2) -> Some [ t1; t2 ]
    | _ -> None

let (|TApp|_|) =
    function
    | SynType.App (t, _, ts, _, _, isPostfix, _) -> Some(t, ts, isPostfix)
    | _ -> None

let (|TLongIdentApp|_|) =
    function
    | SynType.LongIdentApp (t, LongIdentWithDots s, _, ts, _, _, _) -> Some(t, s, ts)
    | _ -> None

let (|TTuple|_|) =
    function
    | SynType.Tuple (false, ts, _) -> Some ts
    | _ -> None

let (|TStructTuple|_|) =
    function
    | SynType.Tuple (true, ts, _) -> Some ts
    | _ -> None

let (|TWithGlobalConstraints|_|) =
    function
    | SynType.WithGlobalConstraints (t, tcs, _) -> Some(t, tcs)
    | _ -> None

let (|TLongIdent|_|) =
    function
    | SynType.LongIdent (LongIdentWithDots s) -> Some s
    | _ -> None

let (|TAnonRecord|_|) =
    function
    | SynType.AnonRecd (isStruct, fields, _) -> Some(isStruct, fields)
    | _ -> None

let (|TParen|_|) =
    function
    | SynType.Paren (innerType, _) -> Some(innerType)
    | _ -> None
// Type parameter

type SingleTyparConstraintKind =
    | TyparIsValueType
    | TyparIsReferenceType
    | TyparIsUnmanaged
    | TyparSupportsNull
    | TyparIsComparable
    | TyparIsEquatable
    override x.ToString() =
        match x with
        | TyparIsValueType -> "struct"
        | TyparIsReferenceType -> "not struct"
        | TyparIsUnmanaged -> "unmanaged"
        | TyparSupportsNull -> "null"
        | TyparIsComparable -> "comparison"
        | TyparIsEquatable -> "equality"

let (|TyparSingle|TyparDefaultsToType|TyparSubtypeOfType|TyparSupportsMember|TyparIsEnum|TyparIsDelegate|) =
    function
    | WhereTyparIsValueType (tp, _) -> TyparSingle(TyparIsValueType, tp)
    | WhereTyparIsReferenceType (tp, _) -> TyparSingle(TyparIsReferenceType, tp)
    | WhereTyparIsUnmanaged (tp, _) -> TyparSingle(TyparIsUnmanaged, tp)
    | WhereTyparSupportsNull (tp, _) -> TyparSingle(TyparSupportsNull, tp)
    | WhereTyparIsComparable (tp, _) -> TyparSingle(TyparIsComparable, tp)
    | WhereTyparIsEquatable (tp, _) -> TyparSingle(TyparIsEquatable, tp)
    | WhereTyparDefaultsToType (tp, t, _) -> TyparDefaultsToType(tp, t)
    | WhereTyparSubtypeOfType (tp, t, _) -> TyparSubtypeOfType(tp, t)
    | WhereTyparSupportsMember (tps, msg, _) ->
        TyparSupportsMember(
            List.choose
                (function
                | SynType.Var (tp, _) -> Some tp
                | _ -> None)
                tps,
            msg
        )
    | WhereTyparIsEnum (tp, ts, _) -> TyparIsEnum(tp, ts)
    | WhereTyparIsDelegate (tp, ts, _) -> TyparIsDelegate(tp, ts)

let (|MSMember|MSInterface|MSInherit|MSValField|MSNestedType|) =
    function
    | SynMemberSig.Member (vs, mf, _) -> MSMember(vs, mf)
    | SynMemberSig.Interface (t, _) -> MSInterface t
    | SynMemberSig.Inherit (t, _) -> MSInherit t
    | SynMemberSig.ValField (f, _) -> MSValField f
    | SynMemberSig.NestedType (tds, _) -> MSNestedType tds

let (|Val|)
    (ValSpfn (ats, (IdentOrKeyword (OpNameFullInPattern (s, _)) as ident), tds, t, vi, isInline, _, px, ao, _, _))
    =
    (ats, px, ao, s, ident.idRange, t, vi, isInline, tds)

// Misc

let (|RecordFieldName|) ((LongIdentWithDots s, _): RecordFieldName, eo: SynExpr option, _) = (s, eo)

let (|AnonRecordFieldName|) (Ident s: Ident, e: SynExpr) = (s, e)
let (|AnonRecordFieldType|) (Ident s: Ident, t: SynType) = (s, t)

let (|PatRecordFieldName|) ((LongIdent s1, Ident s2), p) = (s1, s2, p)

let (|ValInfo|) (SynValInfo (aiss, ai)) = (aiss, ai)

let (|ArgInfo|) (SynArgInfo (attribs, isOpt, ido)) =
    (attribs, Option.map (|Ident|) ido, isOpt)

/// Extract function arguments with their associated info
let (|FunType|) (t, ValInfo (argTypes, returnType)) =
    // Parse arg info by attach them into relevant types.
    // The number of arg info will determine semantics of argument types.
    let rec loop =
        function
        | TFun (t1, t2), argType :: argTypes -> (t1, argType) :: loop (t2, argTypes)
        | t, [] -> [ (t, [ returnType ]) ]
        | _ -> []

    loop (t, argTypes)

/// A rudimentary recognizer for extern functions
/// Probably we should use lexing information to improve its accuracy
let (|Extern|_|) =
    function
    | Let (LetBinding (ats, px, ao, _, _, PatLongIdent (_, s, [ _, PatTuple ps ], _), TypedExpr (Typed, _, t), _)) ->
        let hasDllImportAttr =
            ats
            |> List.exists
                (fun { Attributes = attrs } ->
                    attrs
                    |> List.exists (fun (Attribute (name, _, _)) -> name.EndsWith("DllImport")))

        if hasDllImportAttr then
            Some(ats, px, ao, t, s, ps)
        else
            None
    | _ -> None

let private collectAttributesRanges (a: SynAttributes) =
    [ yield! (List.map (fun (al: SynAttributeList) -> al.Range) a)
      yield! (List.collect (fun a -> a.Attributes |> List.map (fun a -> a.Range)) a) ]

let getRangesFromAttributesFromModuleDeclaration (mdl: SynModuleDecl) : Range list =
    match mdl with
    | SynModuleDecl.Let (_, bindings, _) ->
        bindings
        |> List.collect (fun (Binding (_, _, _, _, attrs, _, _, _, _, _, _, _)) -> collectAttributesRanges attrs)
    | SynModuleDecl.Types (types, _) ->
        types
        |> List.collect
            (fun t ->
                match t with
                | SynTypeDefn.TypeDefn (SynComponentInfo.ComponentInfo (attrs, _, _, _, _, _, _, _), _, _, _) ->
                    collectAttributesRanges attrs)
    | SynModuleDecl.NestedModule (SynComponentInfo.ComponentInfo (attrs, _, _, _, _, _, _, _), _, _, _, _) ->
        collectAttributesRanges attrs
    | _ -> List.empty

let getRangesFromAttributesFromSynModuleSigDeclaration (sdl: SynModuleSigDecl) =
    match sdl with
    | SynModuleSigDecl.NestedModule (SynComponentInfo.ComponentInfo (attrs, _, _, _, _, _, _, _), _, _, _)
    | SynModuleSigDecl.Types (SynTypeDefnSig.TypeDefnSig (SynComponentInfo.ComponentInfo (attrs, _, _, _, _, _, _, _),
                                                          _,
                                                          _,
                                                          _) :: _,
                              _) -> collectAttributesRanges attrs
    | _ -> List.empty

let getRangesFromAttributesFromSynTypeDefnSig (TypeDefnSig (comp, _, _, _)) =
    match comp with
    | SynComponentInfo.ComponentInfo (attrs, _, _, _, _, _, _, _) -> collectAttributesRanges attrs

let getRangesFromAttributesFromSynBinding (sb: SynBinding) =
    match sb with
    | SynBinding.Binding (_, _, _, _, attrs, _, _, _, _, _, _, _) -> attrs |> List.map (fun a -> a.Range)

let getRangesFromAttributesFromSynValSig (valSig: SynValSig) =
    match valSig with
    | SynValSig.ValSpfn (attrs, _, _, _, _, _, _, _, _, _, _) -> attrs |> List.map (fun a -> a.Range)

let getRangesFromAttributesFromSynMemberDefinition (mdn: SynMemberDefn) =
    match mdn with
    | SynMemberDefn.Member (mb, _) -> getRangesFromAttributesFromSynBinding mb
    | SynMemberDefn.AbstractSlot (valSig, _, _) -> getRangesFromAttributesFromSynValSig valSig
    | SynMemberDefn.LetBindings (lb :: _, _, _, _) -> getRangesFromAttributesFromSynBinding lb
    | _ -> []

let rec (|UppercaseSynExpr|LowercaseSynExpr|) (synExpr: SynExpr) =
    let upperOrLower (v: string) =
        let isUpper =
            Seq.tryHead v
            |> Option.map Char.IsUpper
            |> Option.defaultValue false

        if isUpper then
            UppercaseSynExpr
        else
            LowercaseSynExpr

    match synExpr with
    | SynExpr.Ident (Ident id) -> upperOrLower id

    | SynExpr.LongIdent (_, LongIdentWithDots lid, _, _) ->
        let lastPart = Array.tryLast (lid.Split('.'))

        match lastPart with
        | Some lp -> upperOrLower lp
        | None -> LowercaseSynExpr

    | SynExpr.DotGet (_, _, LongIdentWithDots lid, _) -> upperOrLower lid

    | SynExpr.DotIndexedGet (expr, _, _, _)
    | SynExpr.TypeApp (expr, _, _, _, _, _, _) -> (|UppercaseSynExpr|LowercaseSynExpr|) expr
    | _ -> failwithf "cannot determine if synExpr %A is uppercase or lowercase" synExpr

let rec (|UppercaseSynType|LowercaseSynType|) (synType: SynType) =
    let upperOrLower (v: string) =
        let isUpper =
            Seq.tryHead v
            |> Option.map Char.IsUpper
            |> Option.defaultValue false

        if isUpper then
            UppercaseSynType
        else
            LowercaseSynType

    match synType with
    | SynType.LongIdent (LongIdentWithDots lid) -> lid.Split('.') |> Seq.last |> upperOrLower
    | SynType.Var (Typar (s, _), _) -> upperOrLower s
    | SynType.App (st, _, _, _, _, _, _) -> (|UppercaseSynType|LowercaseSynType|) st
    | _ -> failwithf "cannot determine if synType %A is uppercase or lowercase" synType

let isFunctionBinding (p: SynPat) =
    match p with
    | PatLongIdent (_, _, ps, _) when (List.isNotEmpty ps) -> true
    | _ -> false

let (|ElmishReactWithoutChildren|_|) e =
    match e with
    | App (OptVar (ident, _, _), [ ArrayOrList (isArray, children, _) as aolEx ])
    | App (OptVar (ident, _, _), [ ArrayOrListOfSeqExpr (isArray, CompExpr (_, Sequentials children)) as aolEx ]) ->
        Some(ident, isArray, children, aolEx.Range)
    | App (OptVar (ident, _, _), [ ArrayOrListOfSeqExpr (isArray, CompExpr (_, singleChild)) as aolEx ]) ->
        Some(ident, isArray, [ singleChild ], aolEx.Range)
    | _ -> None

let (|ElmishReactWithChildren|_|) e =
    match e with
    | App (OptVar ident, [ ArrayOrList _ as attributes; ArrayOrList (isArray, children, _) as childrenNode ]) ->
        Some(ident, attributes, (isArray, children, childrenNode.Range))
    | App (OptVar ident,
           [ ArrayOrListOfSeqExpr _ as attributes;
             ArrayOrListOfSeqExpr (isArray, CompExpr (_, Sequentials children)) as childrenNode ]) ->
        Some(ident, attributes, (isArray, children, childrenNode.Range))
    | App (OptVar ident,
           [ ArrayOrListOfSeqExpr _ as attributes;
             ArrayOrListOfSeqExpr (isArray, CompExpr (_, singleChild)) as childrenNode ])
    | App (OptVar ident,
           [ ArrayOrList _ as attributes; ArrayOrListOfSeqExpr (isArray, CompExpr (_, singleChild)) as childrenNode ]) ->
        Some(ident, attributes, (isArray, [ singleChild ], childrenNode.Range))
    | App (OptVar ident, [ ArrayOrListOfSeqExpr _ as attributes; ArrayOrList (isArray, [], _) as childrenNode ]) ->
        Some(ident, attributes, (isArray, [], childrenNode.Range))

    | _ -> None

let isIfThenElseWithYieldReturn e =
    match e with
    | SynExpr.IfThenElse (_, SynExpr.YieldOrReturn _, None, _, _, _, _)
    | SynExpr.IfThenElse (_, SynExpr.YieldOrReturn _, Some (SynExpr.YieldOrReturn _), _, _, _, _)
    | SynExpr.IfThenElse (_, SynExpr.YieldOrReturnFrom _, None, _, _, _, _)
    | SynExpr.IfThenElse (_, SynExpr.YieldOrReturn _, Some (SynExpr.YieldOrReturnFrom _), _, _, _, _) -> true
    | _ -> false

let isSynExprLambda =
    function
    | SynExpr.Lambda _ -> true
    | _ -> false

let (|AppParenTupleArg|_|) e =
    match e with
    | AppSingleParenArg (a, Paren (lpr, Tuple (ts, tr), rpr, pr)) -> Some(a, lpr, ts, tr, rpr, pr)
    | _ -> None

let (|AppParenSingleArg|_|) e =
    match e with
    | AppSingleParenArg (a, Paren (lpr, p, rpr, pr)) -> Some(a, lpr, p, rpr, pr)
    | _ -> None

let (|AppParenArg|_|) e =
    match e with
    | AppParenTupleArg t -> Choice1Of2 t |> Some
    | AppParenSingleArg s -> Choice2Of2 s |> Some
    | _ -> None

let private shouldNotIndentBranch e es =
    let isShortIfBranch e =
        match e with
        | SimpleExpr _
        | Sequential (_, _, true)
        | App (SimpleExpr _, [ SimpleExpr _ ]) -> true
        | _ -> false

    let isLongElseBranch e =
        match e with
        | LetOrUses _
        | Sequential _
        | Match _
        | TryWith _
        | App (_, [ ObjExpr _ ]) -> true
        | _ -> false

    List.forall isShortIfBranch es
    && isLongElseBranch e

let (|KeepIndentMatch|_|) (e: SynExpr) =
    let mapClauses matchExpr clauses range t =
        match clauses with
        | [] -> None
        | [ (Clause (_, lastClause, _)) ] ->
            if shouldNotIndentBranch lastClause [] then
                Some(matchExpr, clauses, range, t)
            else
                None
        | clauses ->
            let firstClauses =
                clauses
                |> List.take (clauses.Length - 1)
                |> List.map (fun (Clause (_, expr, _)) -> expr)

            let (Clause (_, lastClause, _)) = List.last clauses

            if shouldNotIndentBranch lastClause firstClauses then
                Some(matchExpr, clauses, range, t)
            else
                None

    match e with
    | Match (matchExpr, clauses) -> mapClauses matchExpr clauses e.Range SynExpr_Match
    | MatchBang (matchExpr, clauses) -> mapClauses matchExpr clauses e.Range SynExpr_MatchBang
    | _ -> None

let (|KeepIndentIfThenElse|_|) (e: SynExpr) =
    match e with
    | ElIf (branches, Some elseExpr) ->
        let branchBodies =
            branches |> List.map (fun (_, e, _, _, _) -> e)

        if shouldNotIndentBranch elseExpr branchBodies then
            Some(branches, elseExpr, e.Range)
        else
            None
    | _ -> None
