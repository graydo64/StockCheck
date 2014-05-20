var globalDateOptions = {
    formatYear: "yy",
    startingDay: 1
};

var stockCheckControllers = angular.module('stockCheckControllers', []);

stockCheckControllers.controller('PeriodController', ['$scope', '$http', '$routeParams', '$location',
function PeriodController($scope, $http, $routeParams, $location) {
    $scope.loading = true;
    $scope.editMode = false;

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

    $scope.toggleEdit = function () {
        $scope.editMode = !$scope.editMode;
    };

    $scope.save = function () {
        $http.put('../api/period/', $scope.period).success(function (data) {
            alert("Saved Successfully");
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

    $scope.predicate = ["SalesItemLedgerCode", "SalesItemName"]
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
        $http.get('../api/salesitem/?Id=' + id).success(function (data) {
            $scope.salesitem = data;
            $scope.loading = false;
            $scope.idealGPpc = $window.Math.round(10000 * data.IdealGP) / 100;
            $scope.taxRatepc = $window.Math.round(100 * data.TaxRate);
        })
        .error(function () {
            $scope.error = "An Error has occurred while loading Sales Item."
            $scope.loading = false;
        });
    };

    var id = $routeParams.id;
    $scope.updateSalesItem(id);


    $scope.toggleEdit = function () {
        $scope.editMode = !$scope.editMode;
    };

    $scope.save = function () {
        $scope.salesitem.TaxRate = $scope.taxRatepc / 100;
        $http.put('../api/salesitem/', $scope.salesitem).success(function (data) {
            alert("Saved Successfully");
            $scope.updateSalesItem($scope.salesitem.Id);
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

    $scope.predicate = ["LedgerCode", "Name", "ContainerSize"]
}]);

stockCheckControllers.controller('GoodsInController', ['$scope', '$http', '$routeParams',
function GoodsInController($scope, $http, $routeParams) {
    $scope.loading = true;
    $scope.editMode = false;
    $scope.filter = '';

    $scope.quantity = 0;
    $scope.receivedDate = (new Date()).toLocaleDateString();
    $scope.invoicedAmountEx = 0;
    $scope.invoicedAmountInc = 0;
    $scope.selectedId = "";

    var periodId = $routeParams.id;

    $http.get('../api/salesitems/').success(function (data) {
        $scope.salesItems = data;
        $scope.loading = false;

        $scope.Views = [];
        for (var item in data) {
            var salesItem = data[item];
            $scope.Views.push({
                Key: salesItem.Id,
                Value: salesItem.LedgerCode + ", " + salesItem.Name + " (" + salesItem.ContainerSize + ")"
            })
        }
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading the Sales Item list."
        $scope.loading = false;
    });

    $scope.getSalesItem = function (val) {
        var values = $scope.Views.filter(function (salesItem) {
            return (salesItem.Value.toLowerCase().indexOf(val.toLowerCase()) > -1)
        });
        return values;
    };

    $scope.onSelect = function ($item, $model, $label) {
        $scope.selectedId = $item.Key;
    }

    $scope.toggleRow = function (salesitem) {
        $scope.active = salesitem;
    };

    $scope.save = function () {
        alert("Save me!");
    }

    $scope.open = function ($event) {
        $event.preventDefault();
        $event.stopPropagation();
        $scope.opened = true;
    };

    $scope.dateOptions = globalDateOptions;
}]);

stockCheckControllers.controller('InvoiceLineController', ['$scope',
function InvoiceLineController($scope) {

    $scope.setSalesItem = function () {
        if ($scope.line.SalesItemId != "") {
            for (var i in $scope.Views) {
                var si = $scope.Views[i]
                if (si.Key === $scope.line.SalesItemId) {
                    $scope.filter = si.Value;
                }
            }
        }
    }

    $scope.onSelect = function ($item, $model, $label) {
        $scope.line.SalesItemId = $item.Key;
    }

    $scope.getSalesItem = function (val) {
        var values = $scope.Views.filter(function (salesItem) {
            return (salesItem.Value.toLowerCase().indexOf(val.toLowerCase()) > -1)
        });
        return values;
    };

    $scope.checkQuantity = function(){
        if($scope.line.Quantity === "0"){
            $scope.line.InvoicedAmountEx = 0;
        }
    }
}]);

stockCheckControllers.controller('InvoiceController', ['$scope', '$http', '$routeParams',
function InvoiceController($scope, $http, $routeParams) {

    $scope.loading = true;
    var id = $routeParams.id;

    if (id === "0") {
        $scope.invoice = { InvoiceLines: [{InvoicedAmountEx: 0, InvoicedAmountInc: 0}]};
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
        $scope.Views = [];
        for (var item in data) {
            var salesItem = data[item];
            $scope.Views.push({
                Key: salesItem.Id,
                Value: salesItem.LedgerCode + ", " + salesItem.Name + " (" + salesItem.ContainerSize + ")"
            })
        };
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
            if ($scope.invoice.Supplier === $scope.suppliers[i].Name) {
                newSupplier = false;
            }
        }

        if (newSupplier) {
            $http.put('../api/supplier/', {Name : $scope.invoice.Supplier}).success(function (data) {
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
        $scope.invoice.InvoiceLines.push({InvoicedAmountEx : 0, InvoicedAmountInc : 0});
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