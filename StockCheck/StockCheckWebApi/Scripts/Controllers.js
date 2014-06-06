'use strict';

var globalDateOptions = {
    formatYear: "yy",
    startingDay: 1
};

var operators = ["+", "-", "*", "/"];
var grammar = 'start\n' +
'  = additive\n' +
'\n' +
'additive\n' +
'  = left:multiplicative "+" right:additive { return left + right; }\n' +
'  / left:multiplicative "-" right:additive { return left - right; }\n' +
'  / multiplicative\n' +
'\n' +
'multiplicative\n' +
'  = left:primary "*" right:multiplicative { return left * right; }\n' +
'  / left:primary "/" right:multiplicative { return left / right; }\n' +
'  / primary\n' +
'\n' +
'primary\n' +
'  = float\n' +
'  / integer\n' +
'  / "(" additive:additive ")" { return additive; }\n' +
'\n' +
'integer "integer"\n' +
'  = digits:[0-9]+ { return parseInt(digits.join(""), 10); }\n' +
'\n' +
'float "float"\n' +
'  = before:[0-9]* "." after:[0-9]+ { return parseFloat(before.join("") + "." + after.join("")); }';

var parser = PEG.buildParser(grammar);

var stockCheckControllers = angular.module('stockCheckControllers', []);

stockCheckControllers.controller('PeriodItemController', ['$scope',
function PeriodItemController($scope) {

    var views = $scope.$parent.getSalesItemsViews();
    for (var i in views) {
        var salesItem = views[i];
        if (salesItem.Key === $scope.item.salesItemId) {
            $scope.salesItemDescription = salesItem.Value;
            break;
        }
    }

    $scope.onSelect = function ($item, $model, $label) {
        var views = $scope.getSalesItems();
        for (var i in views) {
            var si = views[i];
            if (si.id === $item.Key) {
                $scope.item.salesItemId = si.id;
                $scope.item.salesItemName = si.name;
                $scope.item.salesItemLedgerCode = si.ledgerCode;
                $scope.item.container = si.containerSize;
                break;
            }
        }
    }

    $scope.checkExpression = function () {
        if ($scope.item.closingStockExpr != undefined) {
            if (operators.indexOf($scope.item.closingStockExpr.slice(-1)) == -1)
                try {
                    $scope.item.closingStock = parser.parse($scope.item.closingStockExpr);
                }
                catch (err) {
                }
        }
    }
}]);

stockCheckControllers.controller('PeriodController', ['$scope', '$http', '$routeParams', 'appConfig',
function PeriodController($scope, $http, $routeParams, appConfig) {
    $scope.loading = true;
    $scope.editMode = false;

    $http.get(appConfig.pathBase + 'salesitems/').success(function (data) {
        $scope.salesItems = data;
        $scope.loading = false;

        $scope.salesItemsViews = [];
        for (var item in data) {
            var salesItem = data[item];
            $scope.salesItemsViews.push({
                Key: salesItem.id,
                Value: salesItem.ledgerCode + ", " + salesItem.name + " (" + salesItem.containerSize + ")"
            })
        }
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading the Sales Item list."
        $scope.loading = false;
    });

    var id = $routeParams.id;
    if ($routeParams.action === 'init-from') {
        var action = $routeParams.action;
        if (action === 'init-from') {
            $http.get(appConfig.pathBase + 'period/init-from/' + id).success(function (data) {
                $scope.period = data;
                $scope.loading = false;
            })
            .error(function () {
                $scope.error = "An Error has occurred while loading Period."
                $scope.loading = false;
            });
        }
    }
    else if (id === undefined) {
        $scope.period = { items: []}
        $scope.loading = false
    }
    else {
        $http.get(appConfig.pathBase + 'period/' + id).success(function (data) {
            $scope.period = data;
            $scope.loading = false;
        })
        .error(function () {
            $scope.error = "An Error has occurred while loading Period."
            $scope.loading = false;
        });
    }

    $scope.getItemCount = function () {
        return $scope.period.items.length;
    }
    $scope.toggleEdit = function () {
        $scope.editMode = !$scope.editMode;
        if (!$scope.editMode) {
            $scope.predicate = basePredicate;
        }
        else {
            $scope.predicate = [];
        }
    };

    $scope.save = function () {
        $http.put(appConfig.pathBase + 'period/', $scope.period).success(function (data) {
            alert("Saved Successfully");
            $scope.predicate = basePredicate;
        }).error(function (data) {
            $scope.error = "An error occurred while saving the Period." + data;
            $scope.loading = false;
        });
    };

    $scope.openStart = function ($event) {
        $event.preventDefault();
        $event.stopPropagation();
        $scope.openedStart = true;
    };

    $scope.openEnd = function ($event) {
        $event.preventDefault();
        $event.stopPropagation();
        $scope.openedEnd = true;
    };

    $scope.dateOptions = globalDateOptions;

    var basePredicate = ["salesItemLedgerCode", "salesItemName"]
    $scope.predicate = basePredicate;

    $scope.toggleNewMode = function () {
        $scope.period.items.push({ openingStock: 0, closingStock: 0, itemsReceived: 0, salesQty: 0 });
        $scope.newMode = true;
        if ($scope.newMode) {
            periodForm.children[periodForm.children.length - 1].scrollIntoView(true);
        }
    }

    $scope.getSalesItems = function () {
        return $scope.salesItems;
    }

    $scope.getSalesItemsViews = function () {
        return $scope.salesItemsViews;
    }
}]);

stockCheckControllers.controller('PeriodsController', ['$scope', '$http', 'appConfig',
function PeriodsController($scope, $http, appConfig) {
    $scope.loading = true;
    $scope.editMode = false;

    $http.get(appConfig.pathBase + 'periods/').success(function (data) {
        $scope.periods = data;
        $scope.loading = false;
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading Periods."
        $scope.loading = false;
    });

    $scope.toggleEdit = function () {
        $scope.editMode = !$scope.editMode;
    };

    $scope.save = function () {
        $http.put(appConfig.pathBase + 'periods/', $scope.periods).success(function (data) {
            alert("Saved Successfully");
        }).error(function (data) {
            $scope.error = "An error occurred while saving Periods." + data;
            $scope.loading = false;
        });
    };
}]);

stockCheckControllers.controller('SalesItemController', ['$scope', '$http', '$routeParams', '$window', 'appConfig',
function SalesItemController($scope, $http, $routeParams, $window, appConfig) {
    $scope.loading = true;
    $scope.editMode = true;
    $scope.otherSalesUnitMode = false;

    $scope.updateSalesItem = function (id) {
        $http.get(appConfig.pathBase + 'salesitem/?id=' + id).success(function (data) {
            $scope.salesitem = data;
            $scope.loading = false;
            $scope.idealGPpc = $window.Math.round(10000 * data.idealGP) / 100;
            $scope.taxRatepc = $window.Math.round(100 * data.taxRate);
        })
        .error(function () {
            $scope.error = "An Error has occurred while loading Sales Item."
            $scope.loading = false;
        });
    };

    $http.get(appConfig.pathBase + 'salesunit').success(function (data) {
        $scope.salesUnits = data;
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading Sales Units."
        $scope.loading = false;
    });

    var id = $routeParams.id;
    if (id === undefined) {
        $scope.salesitem = {taxRate: appConfig.defaultTaxRate};
        $scope.taxRatepc = $window.Math.round(100 * appConfig.defaultTaxRate);
        $scope.loading = false;
    }
    else {
        $scope.updateSalesItem(id);
    }

    $scope.checkOtherMode = function () {
        $scope.otherSalesUnitMode = $scope.salesitem.salesUnitType === "Other";
    };


    $scope.toggleEdit = function () {
        $scope.editMode = !$scope.editMode;
    };

    $scope.save = function () {
        $scope.salesitem.taxRate = $scope.taxRatepc / 100;
        $http.put(appConfig.pathBase + 'salesitem/', $scope.salesitem).success(function (data) {
            alert("Saved Successfully");
            if (id !== undefined) {
                $scope.updateSalesItem($scope.salesitem.id);
            }
        }).error(function (data) {
            $scope.error = "An error occurred while saving the Sales Item." + data;
            $scope.loading = false;
        });
    };
}]);

stockCheckControllers.controller('SalesItemsController', ['$scope', '$http', 'appConfig',
function SalesItemsController($scope, $http, appConfig) {
    $scope.loading = true;
    $scope.editMode = false;

    $http.get(appConfig.pathBase + 'salesitems/').success(function (data) {
        $scope.salesitems = data;
        $scope.loading = false;
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading Sales Items."
        $scope.loading = false;
    });

    $scope.predicate = ["ledgerCode", "name", "containerSize"]
}]);

stockCheckControllers.controller('InvoiceLineController', ['$scope',
function InvoiceLineController($scope) {

    $scope.setSalesItem = function () {
        if ($scope.line.salesItemId != "") {
            for (var i in $scope.salesItemsViews) {
                var si = $scope.salesItemsViews[i]
                if (si.Key === $scope.line.salesItemId) {
                    $scope.filter = si.Value;
                }
            }
        }
    }

    $scope.onSelect = function ($item, $model, $label) {
        $scope.line.salesItemId = $item.Key;
    }

    $scope.getSalesItem = function (val) {
        var values = $scope.salesItemsViews.filter(function (salesItem) {
            return (salesItem.Value.toLowerCase().indexOf(val.toLowerCase()) > -1)
        });
        return values;
    };

    $scope.checkQuantity = function(){
        if($scope.line.quantity === 0){
            $scope.line.invoicedAmountEx = 0;
        }
        else if ($scope.line.quantity > 0) {
            if ($scope.line.invoicedAmountEx === 0) {
                var cost = $scope.salesItemsHash[$scope.line.salesItemId].costPerContainer;
                var qty = $scope.line.quantity;
                var amountEx = cost * qty;
                $scope.line.invoicedAmountEx = Math.round(amountEx * 100)/100;
            }
        }
    }
}]);

stockCheckControllers.controller('InvoiceController', ['$scope', '$http', '$routeParams', 'appConfig',
function InvoiceController($scope, $http, $routeParams, appConfig) {

    $scope.loading = true;
    var id = $routeParams.id;

    if (id === "0") {
        $scope.invoice = { invoiceLines: [{ invoicedAmountEx: 0, invoicedAmountInc: 0 }] };
    }
    else {
        $http.get(appConfig.pathBase + 'invoice/' + id).success(function (data) {
            $scope.invoice = data;
            $scope.loading = false;
        })
        .error(function () {
            $scope.error = "An Error has occurred while loading the Invoice."
            $scope.loading = false;
        });
    }

    $http.get(appConfig.pathBase + 'salesitems/').success(function (data) {
        $scope.salesItems = data;
        $scope.loading = false;

        $scope.salesItemsViews = [];
        $scope.salesItemsHash = new Array();
        for (var item in data) {
            var salesItem = data[item];
            $scope.salesItemsViews.push({
                Key: salesItem.id,
                Value: salesItem.ledgerCode + ", " + salesItem.name + " (" + salesItem.containerSize + ")"
            })
            $scope.salesItemsHash[salesItem.id] = salesItem;
        }
        $scope.setSalesItem();
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading the Sales Item list."
        $scope.loading = false;
    });

    $http.get(appConfig.pathBase + 'suppliers/').success(function (data) {
        $scope.suppliers = data;
        $scope.loading = false;
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading the Suppliers list."
        $scope.loading = false;
    });

    $scope.openInvoiceDate = function ($event) {
        $event.preventDefault();
        $event.stopPropagation();
        $scope.openedInvoiceDate = true;
    };

    $scope.openDeliveryDate = function ($event) {
        $event.preventDefault();
        $event.stopPropagation();
        $scope.openedDeliveryDate = true;
    };

    $scope.dateOptions = globalDateOptions;

    $scope.submitAll = function () {
        var newSupplier = true;
        for (var i in $scope.suppliers) {
            if ($scope.invoice.supplier === $scope.suppliers[i].name) {
                newSupplier = false;
            }
        }

        if (newSupplier) {
            $http.put(appConfig.pathBase + 'supplier/', {name : $scope.invoice.supplier}).success(function (data) {
            }).error(function (data) {
                $scope.error = "An error occurred while saving the Sales Item." + data;
                $scope.loading = false;
            });
        }

        $http.put(appConfig.pathBase + 'invoice/', $scope.invoice).success(function (data) {
            alert("Saved Successfully");
        }).error(function (data) {
            $scope.error = "An error occurred while saving the Sales Item." + data;
            $scope.loading = false;
        });
    };

    $scope.newLine = function () {
        $scope.invoice.invoiceLines.push({invoicedAmountEx : 0, invoicedAmountInc : 0});
    }
}]);

stockCheckControllers.controller('InvoicesController', ['$scope', '$http', 'appConfig',
function InvoicesController($scope, $http, appConfig) {
    $scope.loading = true;
    $scope.editMode = false;

    $http.get(appConfig.pathBase + 'invoices/').success(function (data) {
        for (var i in data) {
            var inv = data[i];
            inv.invoiceDateDate = new Date(inv.invoiceDate);
            inv.deliveryDateDate = new Date(inv.deliveryDate);
        }

        $scope.invoices = data;
        $scope.loading = false;
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading Invoices."
        $scope.loading = false;
    });

    $scope.predicate = ["-invoiceDateDate", "deliveryDateDate"]
}]);