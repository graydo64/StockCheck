'use strict';

angular.module('stockCheck', []).
    config(['$routeProvider', function ($routeProvider) {
        $routeProvider.
            when('/sales-items', {
                controller: SalesItemsController,
                templateUrl: 'Views/sales-items.html'
            }).
            when('/periods', {
                controller: PeriodsController,
                templateUrl: 'Views/periods.html'
            }).
            when('/help', {
                templateUrl: 'Views/help.html'
            });
    }]);