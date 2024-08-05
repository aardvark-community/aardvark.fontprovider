namespace Aardvark.FontProvider

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open FSharp.Quotations
open FSharp.Core.CompilerServices
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open System.Net.Http
open System
open Aardvark.Base.Fonts
open System.Text.Json
open System.Text.RegularExpressions 
  
  

module FontProviderHelper =
    type private Marker = Marker
    
    do System.AppDomain.CurrentDomain.add_AssemblyResolve(ResolveEventHandler(fun _ arg ->
        let file = AssemblyName(arg.Name).Name + ".dll"
        let path = Path.Combine(Path.GetDirectoryName(typeof<Marker>.Assembly.Location), file)
        if File.Exists path then
            try Assembly.LoadFile path
            with _ -> null
        else
            null
    ))
    

    let tempDir =
        let dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FontProviderCache")
        if not (Directory.Exists dir) then Directory.CreateDirectory dir |> ignore
        dir
        
    let sha = System.Security.Cryptography.SHA1.Create()

    let client = new HttpClient()

    let extractZip (pathOrUrl : string) =
        let cacheDir = Path.Combine(tempDir, "zip_" + (sha.ComputeHash (System.Text.Encoding.UTF8.GetBytes pathOrUrl) |> Array.map (sprintf "%02X") |> String.concat ""))
        if not (Directory.Exists cacheDir) then
            let binary = 
                let uri = System.Uri pathOrUrl
                match uri.Scheme with
                | "file" when File.Exists pathOrUrl ->
                    let ttf = File.ReadAllBytes pathOrUrl
                    ttf
                | _ ->
                    use c = new HttpClient()
                    let ttf = c.GetByteArrayAsync(pathOrUrl).Result
                    ttf
            

            let tmp = Path.GetTempFileName() + ".zip"
            File.WriteAllBytes(tmp, binary)
            
            Directory.CreateDirectory cacheDir |> ignore
            System.IO.Compression.ZipFile.ExtractToDirectory(tmp, cacheDir)
            ()
        cacheDir

    let createType (ns : string) (typeName : string) (path : string) (zipEntry : option<string>) =
        
        let ext = Path.GetExtension path

        let hash, binary = 
            match zipEntry with
            | Some entry ->
                let path = extractZip path
                let ttf = File.ReadAllBytes(Path.Combine(path, entry))

                let hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sprintf "%s:%s" path entry)) |> Array.map (sprintf "%02X") |> String.concat ""
                
                hash, ttf
            | None -> 
                let path = path.Trim()
                let hash = 
                    let path = 
                        match zipEntry with
                        | Some e -> sprintf "%s:%s" path e
                        | None -> path
                    sha.ComputeHash (System.Text.Encoding.UTF8.GetBytes path) 
                    |> Array.map (sprintf "%02X") 
                    |> String.concat ""
            
                let file = Path.Combine(tempDir, hash)
                

                let ttf = 
                    if File.Exists file then 
                        File.ReadAllBytes file
                    else
                        let uri = System.Uri path
                        match uri.Scheme with
                        | "file" when File.Exists path ->
                            let ttf = File.ReadAllBytes path
                            File.WriteAllBytes(file, ttf)
                            ttf
                        | _ ->
                            use c = new HttpClient()
                            let ttf = c.GetByteArrayAsync(path).Result
                            File.WriteAllBytes(file, ttf)
                            ttf
                hash, ttf

        let family, style, weight =
            use ms = new MemoryStream(binary)
            try
                //let font = Font(ms)
                let r = new Typography.OpenFont.OpenFontReader()
                let info = r.ReadPreview ms
                // ("Abhaya Libre", "Regular", 400us, REGULAR, null, null)
                //failwithf "info: %A" (info.Name, info.SubFamilyName, info.Weight, info.OS2TranslatedStyle, info.TypographicFamilyName, info.TypographicSubFamilyName)
                let mutable style = FontStyle.Regular
                
                if info.OS2TranslatedStyle.HasFlag Typography.OpenFont.Extensions.TranslatedOS2FontStyle.BOLD then
                    style <- style ||| FontStyle.Bold
                if info.OS2TranslatedStyle.HasFlag Typography.OpenFont.Extensions.TranslatedOS2FontStyle.ITALIC then
                    style <- style ||| FontStyle.Italic
                

                info.Name, int style, int info.Weight
                
                
            with e ->
                failwithf "not a readable font: %A" e
                
        let b64 = System.Convert.ToBase64String binary

        let asm = ProvidedAssembly()
        let myType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>, isErased=false)
        
        
        let family = ProvidedProperty("Family", typeof<string>, isStatic = true, getterCode = (fun args -> 
            <@@
                family
            @@>
        ))
        
        let style = ProvidedProperty("Style", typeof<FontStyle>, isStatic = true, getterCode = (fun args -> 
            <@@
                unbox<FontStyle> style
            @@>
        ))
        
        let weight = ProvidedProperty("Weight", typeof<int>, isStatic = true, getterCode = (fun args -> 
            <@@
                weight
            @@>
        ))
        
        let prop = ProvidedProperty("Font", typeof<Font>, isStatic = true, getterCode = (fun args -> 
            <@@
                FontLoader.FromBase64String(hash, b64)
            @@>
        ))
        
        let uri = System.Uri path
        match uri.Scheme with
        | "file" -> ()
        | _ ->
            myType.AddMember(
                ProvidedProperty("SourceUrl", typeof<string>, isStatic = true, getterCode = fun _ ->
                    <@@ path @@>
                )
            )
        
        let fileFormat = ProvidedProperty("FileFormat", typeof<string>, isStatic = true, getterCode = (fun args -> 
            <@@
                ext
            @@>
        ))
        

        let data = ProvidedProperty("Data", typeof<byte[]>, isStatic = true, getterCode = (fun args -> 
            <@@
                FontLoader.GetByteArray(hash, b64)
            @@>
        ))
        
        
        
        myType.AddMember(family)
        myType.AddMember(style)
        myType.AddMember(weight)
        myType.AddMember(prop)
        myType.AddMember(data)
        myType.AddMember(fileFormat)
        asm.AddTypes [ myType ]
        
        myType
    
type FontSquirrelDatabase() =
    let document = 
        let ass = typeof<FontSquirrelDatabase>.Assembly
        use stream = ass.GetManifestResourceStream(sprintf "%s.FontSquirrel.json" (ass.GetName().Name))
        JsonDocument.Parse stream
    
    let getZipEntries(url : string) =
        let path = FontProviderHelper.extractZip url
        Directory.GetFiles(path, "*", SearchOption.AllDirectories)
        |> Array.choose (fun e ->
            match Path.GetExtension(e).ToLower() with
            | ".ttf" | ".otf" -> Some (Path.GetFileName e)
            | _ -> None
        )
    
    let lookupTable =
        let dict = Dictionary<string, JsonElement>()
        for f in document.RootElement.EnumerateArray() do
            match f.TryGetProperty "family_name" with
            | (true, fam) ->
                dict.[fam.ToString().Trim()] <- f
            | _ ->
                ()
        dict
        
    member x.TryGetFontUrl(familyName : string, entryName : option<string>, bold : bool, italic : bool) =
        let familyName = familyName.Trim()
        match lookupTable.TryGetValue familyName with
        | (true, json) ->
            match json.TryGetProperty "family_urlname", json.TryGetProperty "font_filename" with
            | (true, urlName), (true, regularFile) ->
                let regularFile = regularFile.GetString()
                let urlName = urlName.GetString()
                let url = sprintf "https://www.fontsquirrel.com/fonts/download/%s" urlName
                
                let path = FontProviderHelper.extractZip url
                let names =
                    Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    |> Array.choose (fun e ->
                        match Path.GetExtension(e).ToLower() with
                        | ".ttf" | ".otf" -> Some e
                        | _ -> None
                    )


                let styleEntryName =    
                    match entryName with
                    | Some e ->
                        Some e
                    | None ->
                        if bold && italic then
                            names |> Array.tryFind (fun n ->
                                let n = Path.GetFileName(n).ToLower()
                                n.Contains("bolditalic") || n.Contains("bold-italic") || 
                                n.Contains("bold_italic") || n.Contains "italicbold" ||
                                n.Contains "italic-bold" || n.Contains "italic_bold"
                            )
                        elif bold then
                            names |> Array.tryFind (fun n ->
                                let n = Path.GetFileName(n).ToLower()
                                n.Contains("bold") || n.Contains "black" || n.Contains "700"
                            )
                        else
                            Some regularFile
                        
                match styleEntryName with
                | Some style ->
                    Ok(url, style)
                | None ->
                    let styleName =
                        if bold then
                            if italic then "bold italic"
                            else "bold"
                        elif italic then "italic"
                        else "regular"
                        
                    let existing =
                        names |> Array.map (Path.GetFileName >> sprintf "    '%s'") |> String.concat "\r\n"

                    Error (sprintf "could not get style '%s' for Font '%s'. Available Entries are:\r\n%s" styleName familyName existing)
                    
            | _ ->
                Error (sprintf "invalid Font entry for '%s'" familyName)

        | _ ->
            Error (sprintf "Could not resolve Font '%s'" familyName)

type GoogleFontsDatabase() =
    let document = 
        let ass = typeof<GoogleFontsDatabase>.Assembly
        use stream = ass.GetManifestResourceStream(sprintf "%s.GoogleFonts.json" (ass.GetName().Name))
        JsonDocument.Parse stream
        
    let lookupTable =
        let dict = Dictionary<string, JsonElement>()
        for f in document.RootElement.GetProperty("items").EnumerateArray() do
            match f.TryGetProperty "family" with
            | (true, fam) ->
                dict.[fam.ToString().Trim()] <- f
            | _ ->
                ()
        dict

    let variantRx = 
        Regex @"^(([0-9]+)(italic)?)|(regular)|(italic)$"

    let parseVariant (v : string) =
        let m = variantRx.Match v
        if m.Success then
            if m.Groups.[1].Success then
                let weight = int m.Groups.[2].Value
                let italic = m.Groups.[3].Success
                Some (weight, italic)
            elif m.Groups.[4].Success then
                Some (400, false)
            else
                Some (400, true)
        else
            None

    member x.TryGetFontUrl(familyName : string, variant : string) =
        let familyName = familyName.Trim()
        match lookupTable.TryGetValue familyName with
        | (true, json) ->
            match json.TryGetProperty "files" with
            | (true, files) ->
                match files.TryGetProperty(variant.ToLower()) with
                | (true, path) ->
                    Ok (path.GetString())
                | _ ->
                    let allVariantNames =
                        files.EnumerateObject()
                        |> Seq.map (fun a -> a.Name)
                        |> Seq.toArray
                        
                    let allVariants =
                        allVariantNames 
                        |> Array.choose parseVariant
                        
                        
                    let errorString = 
                        match parseVariant variant with
                        | Some (weight, italic) ->
                            let sameWeight = allVariants |> Array.filter (fun (w,_) -> w = weight)
                            let sameItalic = allVariants |> Array.filter (fun (_,i) -> i = italic)
                        
                            if sameWeight.Length > 0 then
                                let (_, isItalic) = sameWeight.[0]
                                let weightStr =
                                    match weight with
                                    | 400 -> "regular"
                                    | 700 -> "bold"
                                    | w -> sprintf "weight %d" w
                                if italic then
                                    sprintf "has no italic variant for weight %s" weightStr 
                                else
                                    sprintf "only has an italic variant for weight %s" weightStr 
                            elif sameItalic.Length > 0 then
                                let weights = sameItalic |> Array.map (fun (w,_) -> string w) |> String.concat ", "
                                if italic then
                                    sprintf "only has italic variants for weights %s" weights
                                else
                                    sprintf "only supports weights %s" weights
                            else
                                sprintf "has no variant '%s', available variants are:\r\n%s" variant (String.concat "\r\n" allVariantNames)
                        | _ ->
                            sprintf "has no variant '%s', available variants are:\r\n%s" variant (String.concat "\r\n" allVariantNames)
                  
                        
                    Error (sprintf "Font '%s' %s" familyName errorString)
            | _ ->
                Error (sprintf "Font '%s' has no files" familyName)
        | _ ->
            Error (sprintf "Could not resolve Font '%s'" familyName)
        
    
     

[<TypeProvider>]
type FontProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("Aardvark.FontProvider.DesignTime", "Aardvark.FontProvider.Runtime")])
    

    let ns = "Aardvark.FontProvider"
    let asm = Assembly.GetExecutingAssembly()
    
    let createType typeName (path:string) =
        FontProviderHelper.createType ns typeName path None

    let myParamType = 
        let t = ProvidedTypeDefinition(asm, ns, "FontProvider", Some typeof<obj>, isErased=false, isSealed = true)
        t.DefineStaticParameters( [ProvidedStaticParameter("PathOrUrl", typeof<string>)], fun typeName args -> createType typeName (unbox<string> args.[0]))
        t
    do
        this.AddNamespace(ns, [myParamType])

[<TypeProvider>]
type GoogleFontProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("Aardvark.FontProvider.DesignTime", "Aardvark.FontProvider.Runtime")])
    
    static let database = GoogleFontsDatabase()
   
    
    let ns = "Aardvark.FontProvider"
    let asm = Assembly.GetExecutingAssembly()
    
    let createType typeName (family:string) (variant:string) =
        match database.TryGetFontUrl(family, variant) with
        | Ok path ->
            
            FontProviderHelper.createType ns typeName path None
        | Error e ->
            failwith e
            
    let myParamType = 
        let t = ProvidedTypeDefinition(asm, ns, "GoogleFontProvider", Some typeof<obj>, isErased=false, isSealed = true)
        t.DefineStaticParameters( 
            [
                ProvidedStaticParameter("Family", typeof<string>)
                ProvidedStaticParameter("Variant", typeof<string>, "")
                ProvidedStaticParameter("Italic", typeof<bool>, false)
                ProvidedStaticParameter("Bold", typeof<bool>, false)
                ProvidedStaticParameter("Weight", typeof<int>, 0)
            ], 
            fun typeName args -> 
                let family = unbox<string> args.[0]
                let variant = unbox<string> args.[1]
                let italic = unbox<bool> args.[2]
                let bold = unbox<bool> args.[3]
                let weight = unbox<int> args.[4]
                
                let variant = 
                    if variant <> "" then 
                        variant
                    else
                        let weight =
                            if weight = 0 then
                                if bold then 700
                                else 400
                            else
                                weight
                                
                        if italic then
                            match weight with
                            | 400 -> "italic"
                            | w -> string w + "italic"

                        else
                            match weight with
                            | 400 -> "regular"
                            | w -> string w

                

                createType typeName family variant
        )
        t
    do
        this.AddNamespace(ns, [myParamType])
        
[<TypeProvider>]
type FontSquirrelProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("Aardvark.FontProvider.DesignTime", "Aardvark.FontProvider.Runtime")])
    
    static let database = FontSquirrelDatabase()
   
    
    let ns = "Aardvark.FontProvider"
    let asm = Assembly.GetExecutingAssembly()
    
    let createType typeName (family:string) (bold : bool) (italic : bool) (entry : option<string>)=
        match database.TryGetFontUrl(family, entry, bold, italic) with
        | Ok (path, entry) ->
            
            FontProviderHelper.createType ns typeName path (Some entry)
        | Error e ->
            failwith e
            
    let myParamType = 
        let t = ProvidedTypeDefinition(asm, ns, "FontSquirrelProvider", Some typeof<obj>, isErased=false, isSealed = true)
        t.DefineStaticParameters( 
            [
                ProvidedStaticParameter("Family", typeof<string>)
                ProvidedStaticParameter("Bold", typeof<bool>, false)
                ProvidedStaticParameter("Italic", typeof<bool>, false)
                ProvidedStaticParameter("Entry", typeof<string>, "")
            ], 
            fun typeName args -> 
                let family = unbox<string> args.[0]
                
                let bold = args.[1] :?> bool
                let italic = args.[2] :?> bool
                let entry =
                    match args.[3] :?> string with
                    | "" -> None
                    | s -> Some s
                    
                createType typeName family bold italic entry
        )
        t
    do
        this.AddNamespace(ns, [myParamType])
