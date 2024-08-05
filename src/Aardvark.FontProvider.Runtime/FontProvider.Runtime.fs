namespace Aardvark.FontProvider

open System.IO
open Aardvark.Base
open Aardvark.Base.Fonts

[<AbstractClass; Sealed>]
type FontLoader private() =
    static let dataCache = Dict<string, byte[]>()
    static let cache = Dict<string, Font>()
    
    static member GetByteArray(cacheKey : string, b64 : string) = 
        dataCache.GetOrCreate(cacheKey, fun _ ->
            System.Convert.FromBase64String b64
        )

    static member FromBase64String(cacheKey : string, b64 : string) =
        cache.GetOrCreate(cacheKey, fun cacheKey ->
            let arr = FontLoader.GetByteArray(cacheKey, b64)
            use ms = new MemoryStream(arr)
            Font ms
        )
        



// Put the TypeProviderAssemblyAttribute in the runtime DLL, pointing to the design-time DLL
[<assembly:CompilerServices.TypeProviderAssembly("Aardvark.FontProvider.DesignTime.dll")>]
do ()
