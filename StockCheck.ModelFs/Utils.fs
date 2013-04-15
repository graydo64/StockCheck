namespace StockCheck.ModelFs

module Utils =
    let GrossProfit (sale: decimal, cost: decimal) =
        if sale = decimal 0 
        then decimal 0
        else (sale - cost)/sale

