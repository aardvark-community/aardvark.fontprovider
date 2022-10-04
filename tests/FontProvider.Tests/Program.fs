open Aardvark.FontProvider
open Aardvark.Rendering.Text


module Fonts = 
    type RobotoMonoViaUrl = FontProvider<"http://fonts.gstatic.com/s/robotomono/v22/L0xuDF4xlVMF-BfR8bXMIhJHg45mwgGEFl0_3vqPQ--5Ip2sSQ.ttf">
    type RobotoMono = GoogleFontProvider<"Roboto Mono">
    type RobotoMonoBoldItalic = GoogleFontProvider<Family = "Roboto Mono", Bold = true, Italic = true>
    
    type AmitaBold = FontSquirrelProvider<Family = "Amita", Entry = "amita-bold.ttf">
    type HackBoldItalic = FontSquirrelProvider<Family = "Hack", Italic = true, Bold = true>
       
    type NotoSans900 = GoogleFontProvider<Family = "Noto Sans", Weight = 900>
    type InconsolataBold = GoogleFontProvider<"Inconsolata", Bold = true>
        

[<EntryPoint>]
let main args  =
    printfn "%A" Fonts.RobotoMono.FileFormat
    printfn "%A" Fonts.RobotoMono.SourceUrl
    printfn "%A" Fonts.RobotoMono.Family
    printfn "%A" Fonts.RobotoMono.Style
    printfn "%A" Fonts.RobotoMono.Weight
    printfn "%A" Fonts.RobotoMono.Font
    printfn "%A" Fonts.RobotoMono.Data
    0