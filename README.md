# Aardvark.FontProvider

![Publish](https://github.com/aardvark-community/Aardvark.FontProvider/workflows/Publish/badge.svg)
[![NuGet](https://badgen.net/nuget/v/Aardvark.FontProvider)](https://www.nuget.org/packages/Aardvark.FontProvider/)
[![NuGet](https://badgen.net/nuget/dt/Aardvark.FontProvider)](https://www.nuget.org/packages/Aardvark.FontProvider/)

This is a simple F# type provider.  It has separate design-time and runtime assemblies.

Paket is used to acquire the type provider SDK and build the nuget package (you can remove this use of paket if you like)

Building:

    dotnet tool restore
    dotnet paket update
    dotnet build -c release

    dotnet paket pack nuget --version 0.0.1
