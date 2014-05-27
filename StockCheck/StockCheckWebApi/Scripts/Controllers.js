var globalDateOptions = {
    formatYear: "yy",
    startingDay: 1
};

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
}]);

stockCheckControllers.controller('PeriodController', ['$scope', '$http', '$routeParams', '$location',
function PeriodController($scope, $http, $routeParams, $location) {
    $scope.loading = true;
    $scope.editMode = false;

    $http.get('../api/salesitems/').success(function (data) {
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
            $http.get('../api/period/init-from/' + id).success(function (data) {
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
        $http.get('../api/period/' + id).success(function (data) {
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
        $http.put('../api/period/', $scope.period).success(function (data) {
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
        $scope.newMode = !$scope.newMode;
    }

    $scope.getSalesItems = function () {
        return $scope.salesItems;
    }

    $scope.getSalesItemsViews = function () {
        return $scope.salesItemsViews;
    }
}]);

stockCheckControllers.controller('PeriodsController', ['$scope', '$http',
function PeriodsController($scope, $http) {
    $scope.loading = true;
    $scope.editMode = false;

    $http.get('../api/periods/').success(function (data) {
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
        $http.put('../api/periods/', $scope.periods).success(function (data) {
            alert("Saved Successfully");
        }).error(function (data) {
            $scope.error = "An error occurred while saving Periods." + data;
            $scope.loading = false;
        });
    };
}]);

stockCheckControllers.controller('SalesItemController', ['$scope', '$http', '$routeParams', '$window',
function SalesItemController($scope, $http, $routeParams, $window) {
    $scope.loading = true;
    $scope.editMode = false;

    $scope.updateSalesItem = function (id) {
        $http.get('../api/salesitem/?id=' + id).success(function (data) {
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

    var id = $routeParams.id;
    if (id === undefined) {
        $scope.salesitem = {};
        $scope.loading = false;
    }
    else {
        $scope.updateSalesItem(id);
    }


    $scope.toggleEdit = function () {
        $scope.editMode = !$scope.editMode;
    };

    $scope.save = function () {
        $scope.salesitem.taxRate = $scope.taxRatepc / 100;
        $http.put('../api/salesitem/', $scope.salesitem).success(function (data) {
            alert("Saved Successfully");
            $scope.updateSalesItem($scope.salesitem.id);
        }).error(function (data) {
            $scope.error = "An error occurred while saving the Sales Item." + data;
            $scope.loading = false;
        });
    };
}]);

stockCheckControllers.controller('SalesItemsController', ['$scope', '$http',
function SalesItemsController($scope, $http) {
    $scope.loading = true;
    $scope.editMode = false;

    $http.get('../api/salesitems/').success(function (data) {
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
        if($scope.line.quantity === "0"){
            $scope.line.invoicedAmountEx = 0;
        }
    }
}]);

stockCheckControllers.controller('InvoiceController', ['$scope', '$http', '$routeParams',
function InvoiceController($scope, $http, $routeParams) {

    $scope.loading = true;
    var id = $routeParams.id;

    if (id === "0") {
        $scope.invoice = { invoiceLines: [{invoicedAmountEx: 0, invoicedAmountInc: 0}]};
    }
    else {
        $http.get('../api/invoice/' + id).success(function (data) {
            $scope.invoice = data;
            $scope.loading = false;
        })
        .error(function () {
            $scope.error = "An Error has occurred while loading the Invoice."
            $scope.loading = false;
        });
    }

    $http.get('../api/salesitems/').success(function (data) {
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
        $scope.setSalesItem();
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading the Sales Item list."
        $scope.loading = false;
    });

    $http.get('../api/suppliers/').success(function (data) {
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
            if ($scope.invoice.Supplier === $scope.suppliers[i].name) {
                newSupplier = false;
            }
        }

        if (newSupplier) {
            $http.put('../api/supplier/', {name : $scope.invoice.supplier}).success(function (data) {
            }).error(function (data) {
                $scope.error = "An error occurred while saving the Sales Item." + data;
                $scope.loading = false;
            });
        }

        $http.put('../api/invoice/', $scope.invoice).success(function (data) {
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

stockCheckControllers.controller('InvoicesController', ['$scope', '$http',
function InvoicesController($scope, $http) {
    $scope.loading = true;
    $scope.editMode = false;

    $http.get('../api/invoices/').success(function (data) {
        $scope.invoices = data;
        $scope.loading = false;
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading Invoices."
        $scope.loading = false;
    });
}]);