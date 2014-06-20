'use strict';

var stockCheckApp = angular.module('stockCheck', ['ngRoute', 'ngResource', 'stockCheckControllers', 'ui.bootstrap']);
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

            //$resource.defaults.stripTrailingSlashes = false;
        }]);

stockCheckApp.directive('focusMe', function () {
    return {
        link: function (scope, element, attrs) {
            element[0].focus();
        }
    };

});

stockCheckApp.value("appConfig", { pathBase: "api/", defaultTaxRate: 0.2 });

stockCheckApp.factory("Period", function ($resource) {
    return $resource("api/period/:id");
});

stockCheckApp.factory("SalesItem", function ($resource) {
    return $resource("api/salesitem/:id");
});

stockCheckApp.factory("Invoice", function ($resource) {
    return $resource("api/invoice/:id");
});

stockCheckApp.factory("Supplier", function ($resource) {
    return $resource("api/supplier/:id");
});