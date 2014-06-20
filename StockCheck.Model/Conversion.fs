namespace StockCheck.Model

[<Measure>] type l
[<Measure>] type shot
[<Measure>] type double
[<Measure>] type gal
[<Measure>] type pt
[<Measure>] type ``175``

module Conv =

    let ptPerGal : float<pt/gal> = 8.<pt/gal>
    let shotsPerLtr : float<shot/l> = 1.0<shot>/0.035<l>
    let doublesPerLtr : float<double/l> = 1.0<double>/0.05<l>
    let wineGlassPerLtr : float<``175``/l> = 1.0<``175``>/0.175<l>

    let convertLitresToShots (x : float<l>) = x * shotsPerLtr
    let convertGallonsToPints (x : float<gal>) = x * ptPerGal



