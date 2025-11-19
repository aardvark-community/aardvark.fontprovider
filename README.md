# Aardvark.FontProvider

[![Publish](https://github.com/aardvark-community/Aardvark.FontProvider/actions/workflows/publish.yml/badge.svg)](https://github.com/aardvark-community/Aardvark.FontProvider/actions/workflows/publish.yml)
[![Nuget](https://img.shields.io/nuget/vpre/Aardvark.FontProvider)](https://www.nuget.org/packages/Aardvark.FontProvider/)
[![Downloads](https://img.shields.io/nuget/dt/Aardvark.FontProvider)](https://www.nuget.org/packages/Aardvark.FontProvider/)

Collection of type providers for retrieving and bundling custom fonts with an Aardvark application at compile time. Currently includes providers for [Font Squirrel](https://www.fontsquirrel.com/), [Google Fonts](https://fonts.google.com/), and loading any custom URL or file path.

## Basic Usage
Define the fonts you want to use by using one of the providers:
```fsharp
module MyFonts =

    module internal Types =
        type Alger            = FontProvider<PathOrUrl = @"C:\Windows\Fonts\ALGER.ttf">
        type NotoSans900      = GoogleFontProvider<Family = "Noto Sans", Weight = 900>
        type CourierPrimeBold = FontSquirrelProvider<Family = "Courier Prime", Bold = true, Italic = false>

    let Alger            = Types.Alger.Font
    let NotoSans900      = Types.NotoSans900.Font
    let CourierPrimeBold = Types.CourierPrimeBold.Font
```
