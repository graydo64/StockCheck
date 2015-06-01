namespace StockCheck.Model

open System

type salesUnitType =
    | Pint
    | Unit
    | Spirit
    | Fortified
    | Wine
    | Other
    with
        member this.toString() =
            match this with
            | Pint -> "Pint"
            | Unit -> "Unit"
            | Spirit -> "Spirit"
            | Fortified -> "Fortified"
            | Wine -> "Wine"
            | Other -> "Other"
        static member fromString s =
            match s with
            | "Pint" -> Pint
            | "Unit" -> Unit
            | "Spirit" -> Spirit
            | "Fortified" -> Fortified
            | "Wine" -> Wine
            | "Other" -> Other
            | _ -> Unit
