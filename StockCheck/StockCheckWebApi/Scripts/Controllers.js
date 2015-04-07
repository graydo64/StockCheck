'use strict';

var globalDateOptions = {
    formatYear: "yy",
    startingDay: 1
};

var stockCheckControllers = angular.module('stockCheckControllers', []);

stockCheckControllers.controller('PeriodItemController', ['$scope', 'Parse',
function PeriodItemController($scope, Parse) {
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
            if (Parse.operators.indexOf($scope.item.closingStockExpr.slice(-1)) == -1)
                try {
                    $scope.item.closingStock = Parse.parser.parse($scope.item.closingStockExpr);
                }
                catch (err) {
                }
        }
    }
}]);

stockCheckControllers.controller('PeriodController', ['$scope', '$routeParams', 'Period', 'SalesItem', 'CtrlUtils',
function PeriodController($scope, $routeParams, Period, SalesItem, CtrlUtils) {
    $scope.loading = true;
    $scope.editMode = false;

    SalesItem.query(function (data) {
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
    }, function (data) {
        $scope.error = CtrlUtils.writeError("loading", "SalesItems", data.status, data.statusText);
        $scope.loading = false;
    });

    var id = $routeParams.id;

    if ($routeParams.action === 'init-from') {
        Period.initfrom({ id: id }, function (data) {
            $scope.period = data;
            $scope.loading = false;
        }, function (data) {
            $scope.error = CtrlUtils.writeError("loading", "Period", data.status, data.statusText);
            $scope.loading = false;
        });
    }
    else if ($routeParams.action === 'init-clean') {
        Period.initclean({ id: id }, function (data) {
            $scope.period = data;
            $scope.loading = false;
        }, function (data) {
            $scope.error = CtrlUtils.writeError("loading", "Period", data.status, data.statusText);
            $scope.loading = false;
        });
    }
    else if (id === undefined) {
        $scope.period = new Period({ items: [] });
        $scope.loading = false
    }
    else {
        Period.get({ id: id }, function (data) {
            $scope.period = data;
            $scope.loading = false;
        }, function (data) {
            $scope.error = CtrlUtils.writeError("loading", "Period", data.status, data.statusText);
            $scope.loading = false;
        });
    }

    $scope.getItemCount = function () {
        return $scope.period.items.length;
    }
    $scope.toggleEdit = function () {
        $scope.editMode = !$scope.editMode;
        $scope.predicate = basePredicate;
    };

    $scope.save = function () {
        $scope.period.$save(function (data) {
            alert("Saved Successfully");
            $scope.predicate = basePredicate;
        }, function (data) {
            $scope.error = CtrlUtils.writeError("saving", "Period", data.status, data.statusText);
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
        $scope.predicate = [];
        $scope.period.items.push({ salesItemName: $scope.searchText, openingStock: 0, closingStock: 0, itemsReceived: 0, salesQty: 0 });
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

stockCheckControllers.controller('PeriodsController', ['$scope', 'Period', 'CtrlUtils',
function PeriodsController($scope, Period, CtrlUtils) {
    $scope.loading = true;
    $scope.editMode = false;

    Period.query(function (data) {
        $scope.periods = data;
        $scope.loading = false;
    }, function (data) {
        $scope.error = CtrlUtils.writeError("loading", "Periods", data.status, data.statusText);
        $scope.loading = false;
    });
}]);

stockCheckControllers.controller('SalesItemController', ['$scope', '$http', '$routeParams', '$window', 'appConfig', 'SalesItem', 'CtrlUtils',
function SalesItemController($scope, $http, $routeParams, $window, appConfig, SalesItem, CtrlUtils) {
    $scope.loading = true;
    $scope.editMode = true;
    $scope.otherSalesUnitMode = false;

    $scope.updateSalesItem = function (id) {
        SalesItem.get({ id: id }, function (data) {
            $scope.salesitem = data;
            $scope.loading = false;
            $scope.idealGPpc = $window.Math.round(10000 * data.idealGP) / 100;
            $scope.taxRatepc = $window.Math.round(100 * data.taxRate);
        }, function (data) {
            $scope.error = CtrlUtils.writeError("loading", "Sales Item", data.status, data.statusText);
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
        $scope.salesitem = new SalesItem({ taxRate: appConfig.defaultTaxRate });
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
        $scope.salesitem.$save(function (data) {
            alert("Saved Successfully");
            if (id !== undefined) {
                $scope.updateSalesItem($scope.salesitem.id);
            }
        }, function (data) {
            $scope.error = CtrlUtils.writeError("saving", "Sales Item", data.status, data.statusText);
            $scope.loading = false;
        });
    };
}]);

stockCheckControllers.controller('SalesItemsController', ['$scope', 'SalesItem', 'CtrlUtils',
function SalesItemsController($scope, SalesItem, CtrlUtils) {
    $scope.loading = true;
    $scope.editMode = false;

    SalesItem.query(function (data) {
        $scope.salesitems = data;
        $scope.loading = false;
    }, function (data) {
        $scope.error = CtrlUtils.writeError("loading", "SalesItems", data.status, data.statusText);
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
        $scope.selectedSalesItem = $scope.salesItemsHash[$item.Key];
    }

    $scope.getSalesItem = function (val) {
        var values = $scope.salesItemsViews.filter(function (salesItem) {
            return (salesItem.Value.toLowerCase().indexOf(val.toLowerCase()) > -1)
        });
        return values;
    };

    $scope.checkQuantity = function () {
        if ($scope.line.quantity === 0) {
            $scope.line.invoicedAmountEx = 0;
        }
        else if ($scope.line.quantity > 0) {
            var cost = $scope.selectedSalesItem.costPerContainer;
            var qty = $scope.line.quantity;
            var amountEx = cost * qty;
            $scope.line.invoicedAmountEx = Math.round(amountEx * 100) / 100;
        }
    };
}]);

stockCheckControllers.controller('InvoiceController', ['$scope', '$modal', '$log', '$routeParams', 'Invoice', 'SalesItem', 'Supplier', 'CtrlUtils',
function InvoiceController($scope, $modal, $log, $routeParams, Invoice, SalesItem, Supplier, CtrlUtils) {
    $scope.loading = true;
    $scope.newInvoice = true;
    $scope.priceVariantItems = [];

    var id = $routeParams.id;

    if (id === "0") {
        $scope.invoice = new Invoice({ invoiceLines: [{ invoicedAmountEx: 0, invoicedAmountInc: 0 }] });
    }
    else {
        Invoice.get({ id: id }, function (data) {
            $scope.invoice = data;
            $scope.loading = false;
            $scope.newInvoice = false;
        }, function (data) {
            $scope.error = CtrlUtils.writeError("loading", "Invoice", data.status, data.statusText);
            $scope.loading = false;
        })
    }

    $scope.invoiceTotalEx = function () {
        var totalEx = 0;
        for (var lineIndex in $scope.invoice.invoiceLines) {
            var line = $scope.invoice.invoiceLines[lineIndex];
            totalEx += line.invoicedAmountEx;
        }
        return totalEx;
    };

    $scope.invoiceTotalInc = function () {
        var totalInc = 0;
        for (var lineIndex in $scope.invoice.invoiceLines) {
            var line = $scope.invoice.invoiceLines[lineIndex];
            totalInc += line.invoicedAmountInc;
        }
        return totalInc;
    };

    SalesItem.query(function (data) {
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
    }, function (data) {
        $scope.error = CtrlUtils.writeError("loading", "SalesItems", data.status, data.statusText);
        $scope.loading = false;
    });

    Supplier.query(function (data) {
        $scope.suppliers = data;
        $scope.loading = false;
    }, function (data) {
        $scope.error = CtrlUtils.writeError("loading", "Suppliers", data.status, data.statusText);
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
        $scope.priceVariantItems = [];
        for (var i in $scope.suppliers) {
            if ($scope.invoice.supplier === $scope.suppliers[i].name) {
                newSupplier = false;
            }
        }

        var a = $scope.invoice;
        for (var i in $scope.invoice.invoiceLines) {
            var line = $scope.invoice.invoiceLines[i];
            var item = $scope.salesItemsHash[line.salesItemId];
            var cpc = line.invoicedAmountEx / line.quantity;
            if (cpc != item.costPerContainer) {
                var dto = { salesItemId: item.id, description: line.salesItemDescription, cpc: cpc, ocpc: item.costPerContainer, update: false };
                $scope.priceVariantItems[$scope.priceVariantItems.length] = dto;
            };
        };

        if ($scope.priceVariantItems.length > 0) {
            $scope.open();
        }
        else {
            $scope.saveInvoice();
        };
    };

    $scope.saveInvoice = function () {
        var newSupplier = true;
        for (var i in $scope.suppliers) {
            if ($scope.invoice.supplier === $scope.suppliers[i].name) {
                newSupplier = false;
            }
        }

        if (newSupplier) {
            var supplier = new Supplier({ name: $scope.invoice.supplier });
            supplier.$save(
                function (data) { },
                function (data) {
                    $scope.error = CtrlUtils.writeError("saving", "Supplier", data.status, data.statusText);
                    $scope.loading = false;
                });
        }

        $scope.invoice.$save(function (data) {
            alert("Saved Successfully");
            $scope.newInvoice = false;
            $scope.invoice.id = data.id;
        }, function (data) {
            $scope.error = CtrlUtils.writeError("saving", "Invoice", data.status, data.statusText);
            $scope.loading = false;
        });
    };

    $scope.newLine = function () {
        $scope.invoice.invoiceLines.push({ invoicedAmountEx: 0, invoicedAmountInc: 0 });
        invoiceForm.children[invoiceForm.children.length - 1].scrollIntoView(true);
    };

    $scope.setDeliveryDate = function () {
        if ($scope.invoice.deliveryDate == "" || $scope.invoice.deliveryDate == undefined) {
            $scope.invoice.deliveryDate = $scope.invoice.invoiceDate;
        }
    };

    $scope.findPriceVariantItem = function (id) {
        for (var i = 0; i < $scope.priceVariantItems.length; i++) {
            var pvItem = $scope.priceVariantItems[i];
            if (pvItem.salesItemId === id) {
                return pvItem;
            };
        };
        return;
    };

    $scope.open = function (size) {
        var modalInstance = $modal.open({
            templateUrl: 'myModalContent.html',
            controller: 'ModalInstanceCtrl',
            size: size,
            resolve: {
                salesItems: function () {
                    return $scope.priceVariantItems;
                }
            }
        });

        modalInstance.result.then(function (priceVariantItems) {
            $scope.priceVariantItems = priceVariantItems;
            for (var i = 0; i < priceVariantItems.length; i++) {
                var pvItem = priceVariantItems[i];
                if (pvItem.update) {
                    SalesItem.get({ id: pvItem.salesItemId }, function (data) {
                        $scope.salesitem = data;
                        var pvItem = $scope.findPriceVariantItem($scope.salesitem.id);
                        $scope.salesitem.costPerContainer = pvItem.cpc;
                        $scope.salesitem.$save(function (data) { }, function (data) { });
                    }, function (data) {
                        $scope.error = CtrlUtils.writeError("loading", "Sales Item", data.status, data.statusText);
                        $scope.loading = false;
                    });
                };
            };
            $scope.saveInvoice();
        }, function () {
            $log.info('Modal dismissed at: ' + new Date());
        });
    };
}]);

stockCheckControllers.controller('InvoicesController', ['$scope', 'Invoice', 'CtrlUtils',
function InvoicesController($scope, Invoice, CtrlUtils) {
    $scope.loading = true;
    $scope.editMode = false;

    Invoice.query({ pageSize: 10, pageNumber: 1 }, function (data) {
        for (var i in data.invoices) {
            var inv = data.invoices[i];
            inv.invoiceDateDate = new Date(inv.invoiceDate);
            inv.deliveryDateDate = new Date(inv.deliveryDate);
        }
        $scope.totalItems = data.totalCount;
        $scope.pageCount = data.totalPages;
        $scope.invoices = data.invoices;
        $scope.loading = false;
    }, function () {
        $scope.error = CtrlUtils.writeError("loading", "Invoices", data.status, data.statusText);
        $scope.loading = false;
    });

    $scope.predicate = ["-invoiceDateDate", "deliveryDateDate"]

    $scope.pageChanged = function () {
        Invoice.query({ pageSize: 10, pageNumber: $scope.currentPage }, function (data) {
            for (var i in data.invoices) {
                var inv = data.invoices[i];
                inv.invoiceDateDate = new Date(inv.invoiceDate);
                inv.deliveryDateDate = new Date(inv.deliveryDate);
            }
            $scope.totalItems = data.totalCount;
            $scope.pageCount = data.totalPages;
            $scope.invoices = data.invoices;
            $scope.loading = false;
        });
    };
}]);

stockCheckControllers.controller('ModalInstanceCtrl', function ($scope, $modalInstance, salesItems) {
    $scope.salesItems = salesItems;

    $scope.ok = function () {
        $modalInstance.close($scope.salesItems);
    };

    $scope.cancel = function () {
        $modalInstance.dismiss('cancel');
    };
});