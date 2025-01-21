# Aardvark.FontProvider

![Publish](https://github.com/aardvark-community/Aardvark.FontProvider/workflows/Publish/badge.svg)
[![NuGet](https://badgen.net/nuget/v/Aardvark.FontProvider)](https://www.nuget.org/packages/Aardvark.FontProvider/)
[![NuGet](https://badgen.net/nuget/dt/Aardvark.FontProvider)](https://www.nuget.org/packages/Aardvark.FontProvider/)

Collection of type providers for retrieving and bundling custom fonts with an Aardvark application at compile time. Currently includes providers for [Font Squirrel](https://www.fontsquirrel.com/), [Google Fonts](https://fonts.google.com/), and loading any custom URL or file path.

## Basic Usage
Define the fonts you want to use by using one of the providers
```fsharp
module MyFonts =

    module CourierPrime =

        module Types =
            let [<Literal>] private family = "Courier Prime"
            type Regular    = FontSquirrelProvider<Family = family, Bold = false, Italic = false>
            type Bold       = FontSquirrelProvider<Family = family, Bold = true,  Italic = false>
            type Italic     = FontSquirrelProvider<Family = family, Bold = false, Italic = true>
            type BoldItalic = FontSquirrelProvider<Family = family, Bold = true,  Italic = true>

        let Regular    = Types.Regular.Font
        let Bold       = Types.Bold.Font
        let Italic     = Types.Italic.Font
        let BoldItalic = Types.BoldItalic.Font
```
