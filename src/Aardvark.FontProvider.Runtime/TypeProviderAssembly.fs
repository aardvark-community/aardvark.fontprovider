namespace Aardvark.FontProvider

open FSharp.Core.CompilerServices

// Put the TypeProviderAssemblyAttribute in the runtime DLL, pointing to the design-time DLL
[<assembly: TypeProviderAssembly("Aardvark.FontProvider.DesignTime")>]
do ()
