'use strict';

var stockCheckApp = angular.module('stockCheck', ['ngRoute', 'stockCheckControllers', 'ui.bootstrap']);
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
                when('/sales-item/:id', {
                    controller: 'SalesItemController',
                    templateUrl: 'Views/sales-item.html'
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