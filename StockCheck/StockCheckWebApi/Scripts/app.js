'use strict';

var rmod = angular.module('my.resource', ['ngResource']);
rmod.factory('Resource', ['$resource', function ($resource) {
    return function (url, params, methods) {
        var defaults = {
            update: { method: 'put', isArray: false },
            create: { method: 'post' }
        };

        methods = angular.extend(defaults, methods);
        var resource = $resource(url, params, methods);
        resource.prototype.$save = function (params, success, failure) {
            if (!this.id) {
                return this.$create(params, success, failure);
            }
            else {
                return this.$update(params, success, failure);
            }
        };

        return resource;
    };
}]);

var stockCheckApp = angular.module('stockCheck', ['ngRoute', 'my.resource', 'stockCheckControllers', 'ui.bootstrap']);
stockCheckApp.config(['$routeProvider', 'datepickerConfig', 'datepickerPopupConfig',
        function ($routeProvider, datepickerConfig, datepickerPopupConfig) {
            $routeProvider.
                when('/sales-items', {
                    controller: 'SalesItemsController',
                    templateUrl: 'Views/sales-items.html'
                }).
                when('/periods', {
                    controller: 'PeriodsController',
                    templateUrl: 'Views/periods.html'
                }).
                when('/sales-item/', {
                    controller: 'SalesItemController',
                    templateUrl: 'Views/sales-item.html'
                }).
                when('/sales-item/:id', {
                    controller: 'SalesItemController',
                    templateUrl: 'Views/sales-item.html'
                }).
                when('/period/', {
                    controller: 'PeriodController',
                    templateUrl: 'Views/period.html'
                }).
                when('/period/:id', {
                    controller: 'PeriodController',
                    templateUrl: 'Views/period.html'
                }).
                when('/period/:action/:id', {
                    controller: 'PeriodController',
                    templateUrl: 'Views/period.html'
                }).
                when('/invoices', {
                    controller: 'InvoicesController',
                    templateUrl: 'Views/invoices.html'
                }).
                when('/invoice/:id', {
                    controller: 'InvoiceController',
                    templateUrl: 'Views/invoice.html'
                }).
                when('/help', {
                    templateUrl: 'Views/help.html'
                }).
                otherwise({
                    redirectTo: '/home'
                });
            datepickerConfig.showWeeks = false;
            datepickerConfig.initDate = new Date('2016-15-20');

            datepickerPopupConfig.datepickerPopup = "dd/MM/yyyy";
        }]);

stockCheckApp.directive('focusMe', function () {
    return {
        link: function (scope, element, attrs) {
            element[0].focus();
        }
    };
});

stockCheckApp.value("appConfig", { pathBase: "api/", defaultTaxRate: 0.2 });

stockCheckApp.factory("Period", ['Resource', function ($resource) {
    return $resource("api/period/:id", null,
        {
            'initfrom': { method: 'GET', url: 'api/period/init-from/:id' },
            'initclean': { method: 'GET', url: 'api/period/init-clean/:id' }
        });
}]);

stockCheckApp.factory("SalesItem", ['Resource', function ($resource) {
    return $resource("api/salesitem/:id");
}]);

stockCheckApp.factory("Invoice", ['Resource', function ($resource) {
    return $resource("api/invoice/:id",
        { id: "@id" },
        {
            'query': {
                method: 'GET',
                isArray: false,
                url: '/api/invoice/:pageSize/:pageNumber',
                params: { pageSize: '@pageSize', pageNumber: '@pageNumber' }
            }
        });
}]);

stockCheckApp.factory("Supplier", ['Resource', function ($resource) {
    return $resource("api/supplier/:id");
}]);

stockCheckApp.service("CtrlUtils", function () {
    this.writeError = function (action, objectName, statusCode, statusText) {
        return "An Error has occurred while ".concat(action, " ", objectName, ": ", statusCode, ", ", statusText);
    };
});

stockCheckApp.service("Parse", function () {
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

    this.operators = ["+", "-", "*", "/"];
    this.parser = PEG.buildParser(grammar);
});